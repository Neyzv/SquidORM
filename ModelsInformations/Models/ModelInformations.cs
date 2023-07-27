using System.Collections;
using System.Data;
using System.Reflection;
using MySqlConnector;
using SquidORM.Attributes;
using SquidORM.Caching;
using SquidORM.ModelsInformations.Models.Structs;
using SquidORM.ModelsPatterns;
using SquidORM.RequestConstructions;
using SquidORM.RequestConstructions.Abstractions;

namespace SquidORM.ModelsInformations.Models
{
    public sealed class ModelInformations : TableInformations
    {
        private static readonly Type _stringType;
        private static readonly Type _databaseRecordType;

        private readonly ModelInformationsCache _cache;
        private readonly Func<MySqlConnection> _connectionFactory;
        private readonly CancellationToken _ct;
        private readonly Dictionary<string, ModelColumnInformations> _keyColumnsInformations;
        private readonly Dictionary<string, ModelColumnInformations> _columnsInformations;
        private readonly List<RelationshipColumnInformations> _relationshipInformations;

        private ModelColumnInformations? _autoIncrementKey;

        public Type Type { get; }

        private string? _sqlIdentificationConditions;
        public string SQLIdentificationConditions =>
            _sqlIdentificationConditions ??=
                new IdentificationRequestConstructor(GetKeysInformations()).Construct();

        private string? _sqlInsertion;
        public string SQLInsertion =>
            _sqlInsertion ??= BuildSQLInsertion();

        private string? _sqlUpdate;
        public string SQLUpdate =>
            _sqlUpdate ??=
                new UpdateFieldsRequestConstructor(GetValuesInformations()).Construct();

        public string? AutoIncrementAttributeName =>
            _autoIncrementKey?.Name;

        static ModelInformations() =>
            (_stringType, _databaseRecordType) = (typeof(string), typeof(DatabaseRecord));

        public ModelInformations(ModelInformationsCache cache, Type type, TableAttribute tableAttribute,
            Func<MySqlConnection> connectionFactory, string? defaultDatabaseName, CancellationToken ct)
            : base(tableAttribute.DatabaseName ?? defaultDatabaseName ?? throw new Exception($"No default database name have been provided, and table {tableAttribute.TableName} doesn't have a database name provided..."),
                  tableAttribute.TableName)
        {
            _cache = cache;
            _connectionFactory = connectionFactory;
            _ct = ct;

            _keyColumnsInformations = new Dictionary<string, ModelColumnInformations>();
            _columnsInformations = new Dictionary<string, ModelColumnInformations>();
            _relationshipInformations = new List<RelationshipColumnInformations>();

            Type = type;
        }

        private string BuildSQLInsertion()
        {
            var columns = new List<string>();
            var values = new List<string>();

            foreach (var columnInformations in GetColumnsInformations())
            {
                columns.Add(columnInformations.NameInTable);
                values.Add($"{BaseRequestConstructor.At}{columnInformations.NameInTable}");
            }

            return new InsertValuesRequestConstructor(columns, values)
                .Construct();
        }

        public async Task AnalyseRecordAsync(ModelsCache? modelsCache)
        {
            foreach (var property in from property in Type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                     where property.GetCustomAttribute<IgnoreAttribute>() is null
                                     select property)
            {
                if ((!property.PropertyType.IsClass || property.PropertyType == _stringType) && !property.PropertyType.IsArray)
                {
                    var recordColumnInfos = new ModelColumnInformations(_cache, property);

                    if (recordColumnInfos.IsAutoIncrement)
                    {
                        if (_autoIncrementKey is null)
                            _autoIncrementKey = recordColumnInfos;
                        else
                            throw new Exception($"Can not have multiple keys with auto incrementation, for record {Type.Name}...");
                    }
                    else if (recordColumnInfos.IsKey)
                        _keyColumnsInformations.Add(recordColumnInfos.Name, recordColumnInfos);
                    else
                        _columnsInformations.Add(recordColumnInfos.Name, recordColumnInfos);
                }
                else
                {
                    if (property.GetCustomAttributes<RelationshipAttribute>() is { } relationAttributes)
                    {
                        var propertyModelType = property.PropertyType.GenericTypeArguments.Length is 0 ?
                            property.PropertyType.IsArray ? property.PropertyType.GetElementType()! : property.PropertyType
                        : property.PropertyType.GenericTypeArguments.Last();

                        if (propertyModelType.IsSubclassOf(_databaseRecordType))
                            _relationshipInformations.Add(new(property, relationAttributes.ToArray(),
                                    await _cache.RegisterAndGetModelInformationsAsync(propertyModelType).ConfigureAwait(false),
                                    _cache, _connectionFactory, modelsCache, _ct
                                ));
                        else
                            throw new Exception($"Invalid property type {propertyModelType.Name} to be a relationship for property {property.Name} of class {Type.Name}...");
                    }
                }
            }
        }

        private IEnumerable<ModelColumnInformations> GetKeysInformations()
        {
            if (_autoIncrementKey is not null)
                yield return _autoIncrementKey;

            foreach (var keyColumnInformations in _keyColumnsInformations.Values)
                yield return keyColumnInformations;
        }

        private IEnumerable<ModelColumnInformations> GetValuesInformations()
        {
            foreach (var columnInformations in _columnsInformations.Values)
                yield return columnInformations;
        }

        private IEnumerable<ModelColumnInformations> GetColumnsInformations()
        {
            foreach (var keyColumnInformations in GetKeysInformations())
                yield return keyColumnInformations;

            foreach (var columnInformations in GetValuesInformations())
                yield return columnInformations;
        }

        public async ValueTask<string> GetSQLCreateColumns() =>
            await new ColumnsCreationRequestConstructor(GetColumnsInformations()).ConstructAsync()
                .ConfigureAwait(false);

        internal IEnumerable<RelationshipInformations> GetRelationshipInstances(DatabaseRecord instance)
        {
            foreach (var relationship in _relationshipInformations)
            {
                var relationshipValue = relationship.GetValue(instance);

                if (relationshipValue is not null)
                {
                    if (relationshipValue is IDictionary dict)
                        relationshipValue = dict.Values;

                    if (relationshipValue is IEnumerable enumerable)
                    {
                        foreach (var value in enumerable)
                            if (value is DatabaseRecord record)
                                yield return new RelationshipInformations()
                                {
                                    Record = record,
                                    RelationshipAttributes = relationship.RelationshipAttributes,
                                    ModelInformations = relationship.ModelInformations
                                };
                    }
                    else if (relationshipValue is DatabaseRecord record)
                        yield return new RelationshipInformations()
                        {
                            Record = record,
                            RelationshipAttributes = relationship.RelationshipAttributes,
                            ModelInformations = relationship.ModelInformations
                        };
                }
            }
        }

        private ModelColumnInformations GetModelColumnInformations(string columnName)
        {
            var result = default(ModelColumnInformations?);

            if (_columnsInformations.TryGetValue(columnName, out var modelColumnInformations) ||
                _keyColumnsInformations.TryGetValue(columnName, out modelColumnInformations))
                result = modelColumnInformations;
            else if (_autoIncrementKey?.Name == columnName)
                result = _autoIncrementKey;

            return result ?? throw new Exception($"Unknown column {columnName} in class {Type.Name}...");
        }

        public string? GetColumnNameInTable(string columnName) =>
            GetModelColumnInformations(columnName)?.NameInTable;

        public object? GetColumnValue(DatabaseRecord instance, string columnName) =>
            GetModelColumnInformations(columnName)?.GetValue(instance);

        public void SetColumnValue(DatabaseRecord instance, string columnName, object? value) =>
            GetModelColumnInformations(columnName)?.BindValueToInstance(instance, value);

        public void UpdatePropertiesNamesForDatabase(ref string input)
        {
            foreach (var columnInformations in GetColumnsInformations())
                input = columnInformations.ChangeNameForDatabase(input);
        }

        private void BindValueToInstanceFromDictionary(ModelColumnInformations col, DatabaseRecord instance,
            Dictionary<string, object> values)
        {
            if (values.TryGetValue(col.NameInTable, out var value))
                try
                {
                    col.BindValueToInstance(instance, value);
                }
                catch
                {
                    throw new Exception($"Wrong value type for column {col.NameInTable} in table {TableName}...");
                }
            else
                throw new Exception($"Can not find column {col.NameInTable} for table {TableName}...");
        }

        public async Task<TRecord> CreateInstanceFromReaderAsync<TRecord>(TRecord instance, MySqlDataReader reader, CancellationToken ct)
            where TRecord : DatabaseRecord, new()
        {
            var values = new Dictionary<string, object>();

            for (var i = 0; i < reader.FieldCount; i++)
                values.Add(reader.GetName(i), reader.GetValue(i));

            await Task.WhenAll(
                Parallel.ForEachAsync(_keyColumnsInformations.Values, ct, async (col, ct) =>
                    await Task.Run(() => BindValueToInstanceFromDictionary(col, instance, values), ct).ConfigureAwait(false)
                ),
                Parallel.ForEachAsync(_columnsInformations.Values, ct, async (col, ct) =>
                    await Task.Run(() => BindValueToInstanceFromDictionary(col, instance, values), ct).ConfigureAwait(false)
                ),
                _autoIncrementKey is null ? Task.CompletedTask : Task.Run(() =>
                    BindValueToInstanceFromDictionary(_autoIncrementKey, instance, values), ct)
                ).ConfigureAwait(false);

            if (_relationshipInformations.Count is not 0)
                await Parallel.ForEachAsync(_relationshipInformations, ct, async (col, ct) =>
                    await Task.Run(async () => await col.BindValueToInstanceAsync(instance, values).ConfigureAwait(false), ct).ConfigureAwait(false)
                );

            return instance;
        }

        public void BindAutoIncrementedValueToInstance(DatabaseRecord instance, object value) =>
            _autoIncrementKey?.BindValueToInstance(instance, value);

        public bool IsAutoIncrementedKeyBinded(DatabaseRecord instance) =>
            _autoIncrementKey?.GetValue(instance) != default;

        public void BindCommandParametersFromInstance(DatabaseRecord instance, MySqlCommand command,
            string? prefix = null)
        {
            foreach (var columnInformations in GetColumnsInformations())
                columnInformations.BindValueFromInstance(instance, command, prefix);
        }
    }
}
