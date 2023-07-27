using System.Text;
using SquidORM.Attributes;
using SquidORM.ModelsInformations.Models;
using SquidORM.RequestConstructions.Abstractions;

namespace SquidORM.RequestConstructions
{
    internal class ColumnCreationRequestConstructor : RequestConstructor
    {
        private const string UnsignedSQLKeyWord = "UNSIGNED";
        private const string NotNullSQLKeyWord = "NOT NULL";
        private const string AutoIncrementSQLKeyWord = "AUTO_INCREMENT";

        private readonly string _databaseName;
        private readonly DatabaseTypeInformations _typeInformations;
        private readonly bool _nullable;
        private readonly KeyAttribute? _keyAttribute;
        private readonly LengthAttribute? _lengthAttribute;

        public ColumnCreationRequestConstructor(string databaseName, DatabaseTypeInformations typeInformations,
            bool nullable, KeyAttribute? keyAttribute, LengthAttribute? lengthAttribute)
        {
            _databaseName = databaseName;
            _keyAttribute = keyAttribute;
            _lengthAttribute = lengthAttribute;
            _typeInformations = typeInformations;
            _nullable = nullable;
        }

        public override string Construct()
        {
            var strBuilder = new StringBuilder(_databaseName);

            strBuilder.Append(Blank)
            .Append(_typeInformations.TypeName);

            if (_lengthAttribute is not null)
                strBuilder.Append(OpenedParenthese)
                    .Append(_lengthAttribute.Length)
                    .Append(ClosedParathese);
            else if (_typeInformations.Limit.HasValue)
                strBuilder.Append(OpenedParenthese)
                    .Append(_typeInformations.Limit.Value)
                    .Append(ClosedParathese);

            if (_typeInformations.Unsigned)
                strBuilder.Append(Blank)
                    .Append(UnsignedSQLKeyWord);

            if (!_nullable)
                strBuilder.Append(Blank)
                    .Append(NotNullSQLKeyWord);

            if (_keyAttribute?.AutoIncrement == true)
                strBuilder.Append(Blank)
                    .Append(AutoIncrementSQLKeyWord);

            return strBuilder.ToString();
        }
    }
}
