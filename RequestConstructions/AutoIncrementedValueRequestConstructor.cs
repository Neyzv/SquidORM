using System.Text;
using SquidORM.RequestConstructions.Abstractions;

namespace SquidORM.RequestConstructions
{
    internal class AutoIncrementedValueRequestConstructor : RequestConstructor
    {
        private const string GetAutoIncrementedValueSQLRequest = "SELECT LAST_INSERT_ID();";

        private readonly InsertRequestConstructor _insertConstructor;

        public AutoIncrementedValueRequestConstructor(InsertRequestConstructor insertConstructor) =>
            _insertConstructor = insertConstructor;

        public override string Construct() =>
            new StringBuilder(_insertConstructor.Construct())
                .Append(Semicolon)
                .Append(GetAutoIncrementedValueSQLRequest)
                .ToString();
    }
}
