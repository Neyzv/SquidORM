using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.RegularExpressions;
using MySqlConnector;
using SquidORM.Attributes;
using SquidORM.ModelsInformations.Models.Abstractions;
using SquidORM.ModelsInformations.Models.Structs;
using SquidORM.ModelsPatterns;
using SquidORM.RequestConstructions;

namespace SquidORM.ModelsInformations.Models
{
    public sealed class ModelColumnInformations : BaseModelColumnInformations
    {
        private const string RequestPrefixString = "{0}.{1}";

        private static readonly IReadOnlyDictionary<string, DatabaseTypeInformations> _typeToSQLTypeMapping =
            new Dictionary<string, DatabaseTypeInformations>()
            {
                ["String"] = new("VARCHAR", limit: 255),
                ["Int16"] = new("SMALLINT"),
                ["UInt16"] = new("SMALLINT", true),
                ["Int32"] = new("INT"),
                ["UInt32"] = new("INT", true),
                ["Int64"] = new("BIGINT"),
                ["UInt64"] = new("BIGINT", true),
                ["Single"] = new("FLOAT"),
                ["Boolean"] = new("TINYINT", limit: 1),
                ["Byte"] = new("TINYINT", true),
                ["DateTime"] = new("DATETIME"),
                ["TinyText"] = new("TINYTEXT"),
                ["Text"] = new("TEXT"),
                ["MediumText"] = new("MEDIUMTEXT"),
                ["LongText"] = new("LONGTEXT"),
            };

        private readonly KeyAttribute? _keyAttribute;
        private readonly Regex _nameChangeForDatabaseRegex;
        private readonly string? _customNameInTable;

        private LengthAttribute? _lengthAttribute;
        private MysqlTypeAttribute? _mysqlTypeAttribute;

        public string Name =>
            _propertyInfo.Name;

        public string NameInTable =>
            _customNameInTable ?? Name;

        [MemberNotNullWhen(true, nameof(_keyAttribute))]
        public bool IsKey =>
            _keyAttribute is not null;

        public bool IsAutoIncrement =>
            IsKey && _keyAttribute!.AutoIncrement;

        public ModelColumnInformations(ModelInformationsCache cache, PropertyInfo propertyInfo)
            : base(propertyInfo, cache)
        {
            _customNameInTable = propertyInfo.GetCustomAttribute<NameAttribute>()?.Name;
            _nameChangeForDatabaseRegex = new Regex($"{Name}", RegexOptions.Compiled);

            _keyAttribute = propertyInfo.GetCustomAttribute<KeyAttribute>();
        }

        private string GetPropertyRealTypeName(out bool nullable)
        {
            string result;
            nullable = false;

            if (_mysqlTypeAttribute is not null)
                result = _mysqlTypeAttribute.DbType.ToString();
            else if (_propertyInfo.PropertyType.IsGenericType && (nullable = true))
                result = _propertyInfo.PropertyType.GenericTypeArguments[0].Name;
            else if (_propertyInfo.PropertyType.IsEnum)
                result = Enum.GetUnderlyingType(_propertyInfo.PropertyType).Name;
            else
                result = _propertyInfo.PropertyType.Name;

            return result;
        }

        public string ChangeNameForDatabase(string input) =>
            _nameChangeForDatabaseRegex.Replace(input, NameInTable);

        public string GetSQLCreationScript()
        {
            _mysqlTypeAttribute ??= _propertyInfo.GetCustomAttribute<MysqlTypeAttribute>();

            if (!_typeToSQLTypeMapping.TryGetValue(GetPropertyRealTypeName(out var nullable), out var typeInformations))
                throw new Exception($"Unhandled type {_propertyInfo.PropertyType.Name} for database...");

            _lengthAttribute ??= _propertyInfo.GetCustomAttribute<LengthAttribute>();

            return new ColumnCreationRequestConstructor(NameInTable, typeInformations, nullable,
                _keyAttribute, _lengthAttribute)
                .Construct();
        }

        public async Task<string> GetForeignKeyScript()
        {
            var fkAttribute = _propertyInfo.GetCustomAttribute<ForeignKeyAttribute>();

            return new ForeignKeyRequestConstructor(this, fkAttribute is null ? null : new ForeignKeyInformations()
            {
                ForeignKeyAttribute = fkAttribute,
                ModelInformations = await _modelInformationsCache.RegisterAndGetModelInformationsAsync(fkAttribute.ReferedType)
                    .ConfigureAwait(false)
            }).Construct();
        }

        public object? GetValueFromInstance(DatabaseRecord instance) =>
            _propertyInfo.GetValue(instance);

        public void BindValueToInstance(DatabaseRecord instance, object? value)
        {
            if (value != DBNull.Value)
                _propertyInfo.SetValue(instance, IsKey && _keyAttribute.AutoIncrement ? Convert.ChangeType(value, _propertyInfo.PropertyType) : value);
        }

        public void BindValueFromInstance(DatabaseRecord instance, MySqlCommand command, string? prefix) =>
            command.Parameters.AddWithValue(prefix is null ? NameInTable : string.Format(RequestPrefixString, prefix, NameInTable),
                GetValueFromInstance(instance));
    }
}
