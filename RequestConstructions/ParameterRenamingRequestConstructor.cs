using System.Text.RegularExpressions;
using SquidORM.RequestConstructions.Abstractions;

namespace SquidORM.RequestConstructions
{
    internal class ParameterRenamingRequestConstructor : RequestConstructor
    {
        private const string ReplaceString = "@{0}.";
        private static readonly Regex _parameterRenaming;

        private readonly string _content;
        private readonly string _discriminator;

        static ParameterRenamingRequestConstructor() =>
            _parameterRenaming = new Regex("@", RegexOptions.Compiled);

        public ParameterRenamingRequestConstructor(string content, string discriminator) =>
            (_content, _discriminator) = (content, discriminator);

        public override string Construct() =>
            _parameterRenaming.Replace(_content, string.Format(ReplaceString, _discriminator));
    }
}