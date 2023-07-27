using System.Linq.Expressions;
using System.Text;
using SquidORM.InstructionsCleaner.Abstractions;
using SquidORM.ModelsInformations.Models;
using SquidORM.ModelsPatterns;

namespace SquidORM.InstructionsCleaner
{
    public sealed class OrderInstructionCleaner<TRecord> : RequestInstructionCleaner<TRecord, IComparable>
        where TRecord : DatabaseRecord, new()
    {
        private const string ASCSQLOperand = " ASC";
        private const string DESCSQLOperand = " DESC";

        private readonly bool _reverse;

        public OrderInstructionCleaner(Expression<Func<TRecord, IComparable>> expression,
            ModelInformations modelInformations, bool reverse)
            : base(expression, modelInformations) =>
            _reverse = reverse;

        protected override void Parse(Expression<Func<TRecord, IComparable>> expression) =>
            _content = new StringBuilder(_content)
                .Append(_reverse ? DESCSQLOperand : ASCSQLOperand)
                .ToString();
    }
}
