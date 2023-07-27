using System.Text;
using SquidORM.RequestConstructions.Abstractions;

namespace SquidORM.RequestConstructions
{
    public sealed class InsertValuesRequestConstructor : RequestConstructor
    {
        private const string ValuesKeyWord = " VALUES ";

        private readonly IReadOnlyCollection<string> _columns;
        private readonly IReadOnlyCollection<string> _values;

        public InsertValuesRequestConstructor(IReadOnlyCollection<string> columns,
            IReadOnlyCollection<string> values) =>
            (_columns, _values) = (columns, values);

        public override string Construct() =>
            new StringBuilder()
                .Append(OpenedParenthese)
                .Append(string.Join(Comma, _columns))
                .Append(ClosedParathese)
                .Append(ValuesKeyWord)
                .Append(OpenedParenthese)
                .Append(string.Join(Comma, _values))
                .Append(ClosedParathese)
                .ToString();
    }
}
