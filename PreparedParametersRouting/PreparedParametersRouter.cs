using System.Collections;
using System.Text.RegularExpressions;

namespace SquidORM.PreparedParametersRouting
{
    internal sealed class PreparedParametersRouter
        : IEnumerable<KeyValuePair<string, string>>
    {
        private const string ParametizerCleaner = "${attribute}${comparator} @${attribute}";
        private const string AttributeGroupLabel = "attribute";
        private const string ValueGroupLabel = "value";

        private static readonly Regex _parametizerRegex;

        private readonly Dictionary<string, string> _routingTable;

        public string PreparedRequest { get; private set; }

        static PreparedParametersRouter() =>
            _parametizerRegex = new Regex($"(?<=`)(?<attribute>[^()\\s]+?)(?<comparator>`\\s*(?:[<>]=?|=))\\s*['\"]?(?<value>\\b(?!(?i)(?:null|true|false)(?!['\"]))[^().\\s]+?)['\"]?(?=[)\\s])",
                RegexOptions.Compiled);

        public PreparedParametersRouter(string input)
        {
            _routingTable = new Dictionary<string, string>();
            PreparedRequest = input;
            Parse();
        }

        private void Parse()
        {
            foreach (var result in _parametizerRegex.Matches(PreparedRequest))
            {
                if (result is Match match && match.Success)
                    _routingTable.Add(match.Groups[AttributeGroupLabel].Value, match.Groups[ValueGroupLabel].Value);
            }

            PreparedRequest = _parametizerRegex.Replace(PreparedRequest, ParametizerCleaner);
        }

        public string? this[string name]
        {
            get => _routingTable.GetValueOrDefault(name);
        }

        IEnumerator IEnumerable.GetEnumerator() =>
            _routingTable.GetEnumerator();

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            foreach (var pair in _routingTable)
                yield return pair;
        }
    }
}
