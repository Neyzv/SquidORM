using System.Text;
using SquidORM.ModelsInformations.Models;
using SquidORM.RequestConstructions.Abstractions;

namespace SquidORM.RequestConstructions
{
    public sealed class ColumnsCreationRequestConstructor : AsyncRequestConstructor
    {
        private const string PrimaryKeySQLKeyWord = "PRIMARY KEY";

        private readonly IEnumerable<ModelColumnInformations> _columnsInformations;

        public ColumnsCreationRequestConstructor(IEnumerable<ModelColumnInformations> columnsInformations) =>
            _columnsInformations = columnsInformations;

        public override async ValueTask<string> ConstructAsync()
        {
            var strBuilder = new StringBuilder()
                .Append(OpenedParenthese);

            var fkBuilder = new StringBuilder();
            var pkBuilder = new StringBuilder();

            foreach (var columnInformation in _columnsInformations)
            {
                strBuilder.Append(columnInformation.GetSQLCreationScript())
                    .Append(Comma);

                if (await columnInformation.GetForeignKeyScript().ConfigureAwait(false) is { Length: not 0 } fkScript)
                    fkBuilder.Append(fkScript)
                        .Append(Comma);

                if (columnInformation.IsKey)
                    pkBuilder.Append(NameChar)
                        .Append(columnInformation.NameInTable)
                        .Append(NameChar)
                        .Append(Comma);
            }

            strBuilder.Append(fkBuilder);

            if (pkBuilder.Length is not 0)
                strBuilder.Append(PrimaryKeySQLKeyWord)
                    .Append(OpenedParenthese)
                    .Append(pkBuilder.Remove(pkBuilder.Length - 1, 1))
                    .Append(ClosedParathese);
            else
                strBuilder.Remove(strBuilder.Length - 1, 1);

            strBuilder.Append(ClosedParathese);

            return strBuilder.ToString();
        }
    }
}
