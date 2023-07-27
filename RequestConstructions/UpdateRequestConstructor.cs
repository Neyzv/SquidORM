using System.Text;
using SquidORM.ModelsInformations.Models;
using SquidORM.RequestConstructions.Abstractions;

namespace SquidORM.RequestConstructions
{
    public sealed class UpdateRequestConstructor : RequestConstructor
    {
        private readonly ModelInformations _modelInformations;

        public UpdateRequestConstructor(ModelInformations modelInformations) =>
            _modelInformations = modelInformations;

        public override string Construct() =>
            new StringBuilder(UpdateSQLInstruction)
                .Append(NameChar)
                .Append(_modelInformations.DatabaseName)
                .Append(NameChar)
                .Append(Dot)
                .Append(NameChar)
                .Append(_modelInformations.TableName)
                .Append(NameChar)
                .Append(SetKeyWord)
                .Append(_modelInformations.SQLUpdate)
                .Append(WhereKeyWord)
                .Append(_modelInformations.SQLIdentificationConditions)
                .ToString();
    }
}
