using System.Text;
using SquidORM.InstructionsCleaner;
using SquidORM.ModelsInformations.Models;
using SquidORM.ModelsPatterns;
using SquidORM.RequestConstructions.Abstractions;

namespace SquidORM.RequestConstructions
{
    public sealed class SelectRequestConstructor<TRecord> : RequestConstructor
        where TRecord : DatabaseRecord, new()
    {
        private const string SelectCommand = "SELECT * FROM ";
        private const string OrderByKeyWord = " ORDER BY ";
        private const string LimitKeyWord = " LIMIT ";

        private readonly ModelInformations _modelInformations;
        private readonly IReadOnlyCollection<WhereInstructionCleaner<TRecord>> _whereInstructions;
        private readonly IReadOnlyCollection<OrderInstructionCleaner<TRecord>> _orderInstructions;
        private readonly int? _limit;

        public SelectRequestConstructor(ModelInformations modelInformations,
            IReadOnlyCollection<WhereInstructionCleaner<TRecord>> whereInstructions,
            IReadOnlyCollection<OrderInstructionCleaner<TRecord>> orderInstructions,
            int? limit)
        {
            _modelInformations = modelInformations;
            _whereInstructions = whereInstructions;
            _orderInstructions = orderInstructions;
            _limit = limit;
        }

        public override string Construct()
        {
            var strBuilder = new StringBuilder(SelectCommand)
                .Append(NameChar)
                .Append(_modelInformations.DatabaseName)
                .Append(NameChar)
                .Append(Dot)
                .Append(NameChar)
                .Append(_modelInformations.TableName)
                .Append(NameChar);

            if (_whereInstructions.Any())
            {
                strBuilder.Append(WhereKeyWord);

                foreach (var whereCondition in _whereInstructions)
                    strBuilder.Append(whereCondition.ToString());
            }

            if (_orderInstructions.Any())
            {
                strBuilder.Append(OrderByKeyWord)
                    .Append(string.Join(Comma, _orderInstructions));
            }

            if (_limit is not null)
                strBuilder.Append(LimitKeyWord)
                    .Append(_limit.Value);

            return new(strBuilder.ToString());
        }
    }
}
