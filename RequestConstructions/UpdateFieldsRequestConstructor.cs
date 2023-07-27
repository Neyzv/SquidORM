using System.Text;
using SquidORM.ModelsInformations.Models;
using SquidORM.RequestConstructions.Abstractions;

namespace SquidORM.RequestConstructions
{
    public sealed class UpdateFieldsRequestConstructor : RequestConstructor
    {
        private readonly IEnumerable<ModelColumnInformations> _valuesInformations;

        public UpdateFieldsRequestConstructor(IEnumerable<ModelColumnInformations> valuesInformations) =>
            _valuesInformations = valuesInformations;

        public override string Construct() =>
            new StringBuilder()
                .Append(string.Join(Comma, _valuesInformations
                    .Select(x => new StringBuilder()
                        .Append(NameChar)
                        .Append(x.NameInTable)
                        .Append(NameChar)
                        .Append(Equal)
                        .Append(At)
                        .Append(x.NameInTable)
                    )
                ))
                .ToString();
    }
}
