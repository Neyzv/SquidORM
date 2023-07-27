using System.Linq.Expressions;
using System.Reflection;
using MySqlConnector;
using SquidORM.Attributes;
using SquidORM.Caching;
using SquidORM.ModelsInformations.Models.Abstractions;
using SquidORM.ModelsInformations.Models.ValueConvertions;
using SquidORM.ModelsInformations.Models.ValueConvertions.Abstractions;
using SquidORM.ModelsPatterns;

namespace SquidORM.ModelsInformations.Models
{
    public sealed class RelationshipColumnInformations : BaseModelColumnInformations
    {
        private const string WhereMethodName = "Where";
        private const string GetSingleAsyncMethodName = "GetSingleAsync";
        private const string GetResultAsyncMethodName = "GetResultAsync";
        private const string ResultPropertyName = "Result";
        private const string ExpressionParameterName = "x";
        private const string AnyMethodName = "Any";
        private const string RetrieveModelMethodName = "RetrieveModel";
        private const string RetrieveModelsMethodName = "RetrieveModels";

        private static readonly Type _queryType;
        private static readonly Type _funcType;
        private static readonly Type _boolType;
        private static readonly Type _taskType;
        private static readonly Type _enumerableType;
        private static readonly Type _listType;
        private static readonly Type _dictionaryType;

        private readonly Func<MySqlConnection> _connectionFactory;
        private readonly CancellationToken _ct;
        private readonly ModelsCache? _modelsCache;

        private Type? _modelQueryType;
        private Type? _modelFuncType;
        private MethodInfo? _whereMethod;
        private ParameterExpression? _modelParameterExpression;
        private PropertyInfo? _resultProperty;
        private MethodInfo? _getQueryResultMethod;
        private object?[]? _getQueryResultParameters;
        private BaseValueConvertor? _valueConvertor;
        private MethodInfo? _modelRetrieverMethod;
        private MethodInfo? _anyMethod;

        public ModelInformations ModelInformations { get; }

        public IReadOnlyCollection<RelationshipAttribute> RelationshipAttributes { get; }

        static RelationshipColumnInformations()
        {
            _queryType = typeof(Query<>);
            _funcType = typeof(Func<,>);
            _boolType = typeof(bool);
            _taskType = typeof(Task<>);
            _enumerableType = typeof(Enumerable);
            _listType = typeof(List<>);
            _dictionaryType = typeof(Dictionary<,>);
        }

        public RelationshipColumnInformations(PropertyInfo propertyInfo, RelationshipAttribute[] relationshipAttributes,
            ModelInformations modelInformations, ModelInformationsCache modelInformationsCache,
            Func<MySqlConnection> connectionFactory, ModelsCache? modelsCache, CancellationToken ct)
            : base(propertyInfo, modelInformationsCache)
        {
            ModelInformations = modelInformations;
            _connectionFactory = connectionFactory;
            _ct = ct;
            _modelsCache = modelsCache;

            RelationshipAttributes = relationshipAttributes;
        }

        private void InitializeBufferedProperties()
        {
            _modelQueryType = _queryType.MakeGenericType(ModelInformations.Type);

            _modelFuncType = _funcType.MakeGenericType(ModelInformations.Type, _boolType);

            _whereMethod = _modelQueryType!.GetMethod(WhereMethodName)
                ?? throw new Exception($"An error has occured, can not find method {WhereMethodName} in class Query...");

            _modelParameterExpression = Expression.Parameter(ModelInformations.Type, ExpressionParameterName);

            if (_propertyInfo.PropertyType == ModelInformations.Type)
            {
                _resultProperty = _taskType.MakeGenericType(ModelInformations.Type).GetProperty(ResultPropertyName) ??
                    throw new Exception($"An error has occured, can not find property {ResultPropertyName} in class Task...");

                _getQueryResultParameters = null;
                _getQueryResultMethod = _modelQueryType!.GetMethod(GetSingleAsyncMethodName) ??
                    throw new Exception($"An error has occured, can not find method {GetSingleAsyncMethodName} in class Query...");

                if (_modelsCache is not null)
                    _modelRetrieverMethod ??= typeof(ModelsCache).GetMethod(RetrieveModelMethodName)?.MakeGenericMethod(ModelInformations.Type)
                        ?? throw new Exception($"An error has occured, can not find method {RetrieveModelMethodName} in class {nameof(ModelsCache)}...");
            }
            else
            {
                _resultProperty = _taskType.MakeGenericType(ModelInformations.Type.MakeArrayType())
                        .GetProperty(ResultPropertyName) ??
                        throw new Exception($"An error has occured, can not find property {ResultPropertyName} in class Task...");

                _getQueryResultParameters = new object?[] { null };
                _getQueryResultMethod = _modelQueryType!.GetMethod(GetResultAsyncMethodName) ??
                    throw new Exception($"An error has occured, can not find method {GetResultAsyncMethodName} in class Query...");

                if (_propertyInfo.PropertyType.IsGenericType)
                {
                    var genericTypeDefinition = _propertyInfo.PropertyType.GetGenericTypeDefinition();

                    if (genericTypeDefinition == _listType)
                        _valueConvertor = new ListValueConvertor(_enumerableType, ModelInformations.Type);
                    else if (genericTypeDefinition == _dictionaryType)
                        _valueConvertor = new DictionaryValueConvertor(_enumerableType, ModelInformations, _propertyInfo,
                            _funcType, RelationshipAttributes, _modelParameterExpression);
                }

                if (_modelsCache is not null)
                    _modelRetrieverMethod ??= typeof(ModelsCache).GetMethod(RetrieveModelsMethodName)?.MakeGenericMethod(ModelInformations.Type)
                        ?? throw new Exception($"An error has occured, can not find method {RetrieveModelsMethodName} in class {nameof(ModelsCache)}...");

                if (!_propertyInfo.PropertyType.IsArray && _valueConvertor is null)
                    throw new Exception($"An error has occured, can not convert {_propertyInfo.PropertyType} from an {_enumerableType.Name} instance...");
            }
        }

        public async Task BindValueToInstanceAsync(DatabaseRecord instance, Dictionary<string, object> datas)
        {
            if (_modelFuncType is null)
                InitializeBufferedProperties();

            var predicateExpression = default(Expression?);
            foreach (var relationshipAttribute in RelationshipAttributes)
            {
                if (datas.TryGetValue(relationshipAttribute.ModelPropertyName, out var value))
                {
                    if (value != DBNull.Value)
                    {
                        var bufferedExpression = Expression.Equal(
                                Expression.Property(_modelParameterExpression!, relationshipAttribute.ReferencialPropertyName),
                                Expression.Constant(value)
                            );

                        predicateExpression = predicateExpression is null ? bufferedExpression : Expression.AndAlso(predicateExpression, bufferedExpression);
                    }
                }
                else
                    throw new Exception($"Can not find property {relationshipAttribute.ModelPropertyName} in class {_propertyInfo.Name}...");
            }

            if (predicateExpression is not null)
            {
                var lambdaExpression = Expression.Lambda(_modelFuncType!, predicateExpression!, _modelParameterExpression!);

                object? result = null;
                if (_modelRetrieverMethod is not null)
                {
                    result = _modelRetrieverMethod.Invoke(_modelsCache, new[]
                    {
                        lambdaExpression.Compile()
                    });
                }

                if (result is null || (_modelRetrieverMethod is not null && _propertyInfo.PropertyType != ModelInformations.Type &&
                        (!(bool)(_anyMethod ??= _enumerableType.GetMethods().FirstOrDefault(x => x.Name == AnyMethodName && x.GetParameters().Length is 1)?
                            .MakeGenericMethod(ModelInformations.Type)
                                ?? throw new Exception($"Can not find method {AnyMethodName} in class {_enumerableType.Name}...")
                        ).Invoke(null, new[] { result })!)))
                {
                    var queryInstance = Activator.CreateInstance(_modelQueryType!,
                        _connectionFactory, ModelInformations, _modelInformationsCache, _ct);

                    _whereMethod!.Invoke(queryInstance, new[]
                    {
                        lambdaExpression
                    });

                    var task = (_getQueryResultMethod!.Invoke(queryInstance, _getQueryResultParameters) as Task)!;

                    await task.ConfigureAwait(false);

                    result = _resultProperty!.GetValue(task);
                }

                if (result is not null)
                    _propertyInfo.SetValue(instance, _valueConvertor is null ? result
                        : _valueConvertor.Convert(result));
            }
        }
    }
}
