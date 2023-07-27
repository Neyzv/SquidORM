using System.Linq.Expressions;
using System.Text.RegularExpressions;
using SquidORM.ModelsInformations.Models;
using SquidORM.ModelsPatterns;

namespace SquidORM.InstructionsCleaner.Abstractions
{
    public abstract class RequestInstructionCleaner<TRecord, TResult>
        where TRecord : DatabaseRecord, new()
        where TResult : IComparable
    {
        private const string EqualSQLComparator = "=";
        private const string EnumCleaner = "${attribute}";
        private const string AttributeNameCleaner = "(`${attribute}`";

        private static readonly Regex _lambdaParameterCleanerRegex;
        private static readonly Regex _doubleEqualityCleanerRegex;
        private static readonly Regex _enumCleanerRegex;
        private static readonly Regex _attributeNameCharRegex;

        protected readonly ModelInformations _modelInformations;

        protected string _content;

        static RequestInstructionCleaner()
        {
            _lambdaParameterCleanerRegex = new Regex("[^\\(\\)\\s]+?\\.", RegexOptions.Compiled);
            _doubleEqualityCleanerRegex = new Regex("==", RegexOptions.Compiled);
            _enumCleanerRegex = new Regex("Convert\\((?<attribute>.+?),\\s.+?\\)", RegexOptions.Compiled);
            _attributeNameCharRegex = new Regex("\\((?<attribute>[^\\s]+)", RegexOptions.Compiled);
        }

        protected RequestInstructionCleaner(Expression<Func<TRecord, TResult>> expression,
            ModelInformations modelInformations)
        {
            _modelInformations = modelInformations;
            _content = _attributeNameCharRegex.Replace(
                    _lambdaParameterCleanerRegex.Replace(
                        _doubleEqualityCleanerRegex.Replace(
                            _enumCleanerRegex.Replace(GetExpressionBody(expression), EnumCleaner),
                        EqualSQLComparator),
                    string.Empty)
                , AttributeNameCleaner);

            _modelInformations.UpdatePropertiesNamesForDatabase(ref _content);

            if (string.IsNullOrEmpty(_content))
                throw new Exception("Invalid predicate, it musn't be null or empty...");

            Parse(expression);
        }

        protected static string GetExpressionBody(Expression<Func<TRecord, TResult>> expression) =>
            expression.Body switch
            {
                UnaryExpression unaryExpression => unaryExpression.Operand.ToString(),
                BinaryExpression binaryExpression => binaryExpression.ToString(),
                _ => throw new NotImplementedException("This type of predicate isn't supported yet..."),
            };

        protected abstract void Parse(Expression<Func<TRecord, TResult>> expression);

        public override string ToString() =>
            _content;
    }
}
