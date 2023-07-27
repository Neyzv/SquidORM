using System.Linq.Expressions;
using System.Reflection;
using SquidORM.Attributes;
using SquidORM.ModelsInformations.Models.ValueConvertions.Abstractions;

namespace SquidORM.ModelsInformations.Models.ValueConvertions
{
    internal sealed class DictionaryValueConvertor : BaseValueConvertor
    {
        private const string ToDictionaryMethodName = "ToDictionary";

        public DictionaryValueConvertor(Type enumerableType, ModelInformations modelInformations, PropertyInfo propertyInfo, Type funcType,
            IReadOnlyCollection<RelationshipAttribute> relationshipAttributes, ParameterExpression parameterExpression)
            : base(
                enumerableType.GetMethods(BindingFlags.Static | BindingFlags.Public).FirstOrDefault(x =>
                    x.Name == ToDictionaryMethodName && x.GetParameters().Length is 2
                )?.MakeGenericMethod(modelInformations.Type, propertyInfo.PropertyType.GenericTypeArguments[0]) ??
                    throw new Exception($"An error has occured, can not find method {ToDictionaryMethodName} in class {enumerableType.Name}..."),
                new[]
                {
                    Expression.Lambda(funcType.MakeGenericType(modelInformations.Type, propertyInfo.PropertyType.GenericTypeArguments[0]),
                        Expression.Property(parameterExpression,
                            relationshipAttributes.FirstOrDefault(x => x.ReferencialKeyNameForDictRelationship is not null)?
                                .ReferencialKeyNameForDictRelationship ?? modelInformations.AutoIncrementAttributeName ??
                                throw new Exception($"Can not find a referencial key name for a dictionary relationship, no autoincrement key or nothing have been precised...")
                            ),
                        parameterExpression
                    ).Compile()
                })
        { }
    }
}
