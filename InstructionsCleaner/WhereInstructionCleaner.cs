using System.Linq.Expressions;
using System.Text.RegularExpressions;
using SquidORM.InstructionsCleaner.Abstractions;
using SquidORM.ModelsInformations.Models;
using SquidORM.ModelsPatterns;
using SquidORM.RequestConstructions.Abstractions;

namespace SquidORM.InstructionsCleaner
{
    public sealed class WhereInstructionCleaner<TRecord> : RequestInstructionCleaner<TRecord, bool>
        where TRecord : DatabaseRecord, new()
    {
        private const string OrSQLOperand = " OR ";
        private const string NotSQLOperator = "not ${attribute}";

        private static readonly Regex _andOperandCleanerRegex;
        private static readonly Regex _orOperandCleanerRegex;
        private static readonly Regex _notOperatorCleanerRegex;

        static WhereInstructionCleaner()
        {
            _andOperandCleanerRegex = new Regex("\\sAndAlso\\s", RegexOptions.Compiled);
            _orOperandCleanerRegex = new Regex("\\sOrElse\\s", RegexOptions.Compiled);
            _notOperatorCleanerRegex = new Regex("Not\\((?<attribute>.+?)\\)", RegexOptions.Compiled);
        }

        public WhereInstructionCleaner(Expression<Func<TRecord, bool>> expression,
            ModelInformations modelInformations)
            : base(expression, modelInformations) { }

        protected override void Parse(Expression<Func<TRecord, bool>> expression)
        {
            _content = _orOperandCleanerRegex.Replace(
                    _andOperandCleanerRegex.Replace(
                            _notOperatorCleanerRegex.Replace(_content, NotSQLOperator),
                    BaseRequestConstructor.AndSQLOperand),
                OrSQLOperand);
        }
    }
}
