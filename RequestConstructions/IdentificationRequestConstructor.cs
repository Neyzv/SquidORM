using System.Text;
using SquidORM.ModelsInformations.Models;
using SquidORM.RequestConstructions.Abstractions;

namespace SquidORM.RequestConstructions
{
    public sealed class IdentificationRequestConstructor : RequestConstructor
    {
        private readonly IEnumerable<ModelColumnInformations> _keysInformations;

        public IdentificationRequestConstructor(IEnumerable<ModelColumnInformations> keys) =>
            _keysInformations = keys;

        public override string Construct() =>
            string.Join(AndSQLOperand, _keysInformations
                .Select(x =>
                    new StringBuilder()
                        .Append(NameChar)
                        .Append(x.NameInTable)
                        .Append(NameChar)
                        .Append(Equal)
                        .Append(At)
                        .Append(x.NameInTable)
                ));
    }
}
