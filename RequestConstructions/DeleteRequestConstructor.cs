using System.Text;
using SquidORM.ModelsInformations.Models;
using SquidORM.RequestConstructions.Abstractions;

namespace SquidORM.RequestConstructions
{
    internal class DeleteRequestConstructor : RequestConstructor
    {
        private readonly ModelInformations _modelInformations;

        public DeleteRequestConstructor(ModelInformations modelInformations) =>
            _modelInformations = modelInformations;

        public override string Construct() =>
            new StringBuilder(DeleteSQLInstruction)
                .Append(FromKeyWord)
                .Append(NameChar)
                .Append(_modelInformations.DatabaseName)
                .Append(NameChar)
                .Append(Dot)
                .Append(NameChar)
                .Append(_modelInformations.TableName)
                .Append(NameChar)
                .Append(WhereKeyWord)
                .Append(_modelInformations.SQLIdentificationConditions)
                .Append(Semicolon)
                .ToString();
    }
}
