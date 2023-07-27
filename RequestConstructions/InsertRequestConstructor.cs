using System.Text;
using SquidORM.ModelsInformations.Models;
using SquidORM.RequestConstructions.Abstractions;

namespace SquidORM.RequestConstructions
{
    public class InsertRequestConstructor : RequestConstructor
    {
        private const string InsertSQLInstruction = "INSERT INTO ";

        protected readonly ModelInformations _modelInformations;

        public InsertRequestConstructor(ModelInformations modelInformations) =>
            _modelInformations = modelInformations;

        public override string Construct() =>
            new StringBuilder(InsertSQLInstruction)
                .Append(NameChar)
                .Append(_modelInformations.DatabaseName)
                .Append(NameChar)
                .Append(Dot)
                .Append(NameChar)
                .Append(_modelInformations.TableName)
                .Append(NameChar)
                .Append(Blank)
                .Append(_modelInformations.SQLInsertion)
                .ToString();
    }
}
