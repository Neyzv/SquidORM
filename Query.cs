using System.Linq.Expressions;
using MySqlConnector;
using SquidORM.InstructionsCleaner;
using SquidORM.ModelsInformations;
using SquidORM.ModelsInformations.Models;
using SquidORM.ModelsPatterns;
using SquidORM.PreparedParametersRouting;
using SquidORM.RequestConstructions;

namespace SquidORM
{
    public sealed class Query<TRecord>
        where TRecord : DatabaseRecord, new()
    {
        private const byte UniqueSelectionNumber = 1;

        private readonly Func<MySqlConnection> _connectionFactory;
        private readonly ModelInformations _modelInformations;
        private readonly ModelInformationsCache _modelInformationsCache;
        private readonly CancellationToken _ct;
        private readonly List<WhereInstructionCleaner<TRecord>> _whereInstructions;
        private readonly List<OrderInstructionCleaner<TRecord>> _orderInstructions;

        public Query(Func<MySqlConnection> connectionFactory, ModelInformations modelInformations,
            ModelInformationsCache modelInformationsCache, CancellationToken ct)
        {
            _connectionFactory = connectionFactory;
            _modelInformationsCache = modelInformationsCache;
            _modelInformations = modelInformations;
            _ct = ct;

            _whereInstructions = new List<WhereInstructionCleaner<TRecord>>();
            _orderInstructions = new List<OrderInstructionCleaner<TRecord>>();
        }

        public Query<TRecord> Where(Expression<Func<TRecord, bool>> p)
        {
            _whereInstructions.Add(new WhereInstructionCleaner<TRecord>(p, _modelInformations));

            return this;
        }

        public Query<TRecord> OrderBy(Expression<Func<TRecord, IComparable>> order)
        {
            _orderInstructions.Add(new OrderInstructionCleaner<TRecord>(order, _modelInformations, false));

            return this;
        }

        public Query<TRecord> OrderByDescending(Expression<Func<TRecord, IComparable>> order)
        {
            _orderInstructions.Add(new OrderInstructionCleaner<TRecord>(order, _modelInformations, true));

            return this;
        }

        private async Task<MySqlDataReader> CreateReaderAsync(MySqlConnection connection, int? resultLimit)
        {
            var request = new PreparedParametersRouter(
                    new SelectRequestConstructor<TRecord>(_modelInformations, _whereInstructions, _orderInstructions, resultLimit)
                        .Construct()
                );

            using var command = connection.CreateCommand();
            command.CommandText = request.PreparedRequest;

            foreach (var (param, value) in request)
                command.Parameters.AddWithValue(param, value);

            return await command.ExecuteReaderAsync(_ct)
                .ConfigureAwait(false);
        }

        public async Task<TRecord?> GetSingleAsync()
        {
            using var connection = _connectionFactory();
            await connection.OpenAsync()
                .ConfigureAwait(false);

            using var reader = await CreateReaderAsync(connection, UniqueSelectionNumber)
                    .ConfigureAwait(false);

            var result = await reader.ReadAsync().ConfigureAwait(false) ?
                await _modelInformations.CreateInstanceFromReaderAsync(new TRecord()
                {
                    IsDirty = false
                }, reader, _ct).ConfigureAwait(false)
                : default;

            if (result is not null)
                _modelInformationsCache.ModelsCache?.RegisterOrUpdateRecord(result, _modelInformations);

            return result;
        }

        public async Task<TRecord[]> GetResultAsync(int? resultLimit = null)
        {
            using var connection = _connectionFactory();
            await connection.OpenAsync()
                .ConfigureAwait(false);

            using var reader = await CreateReaderAsync(connection, resultLimit)
                    .ConfigureAwait(false);
            var tasks = new List<Task<TRecord>>();

            var i = 0;
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                tasks.Add(_modelInformations.CreateInstanceFromReaderAsync(new TRecord()
                {
                    IsDirty = false
                }, reader, _ct));

                if (resultLimit.HasValue && ++i == resultLimit.Value)
                    break;
            }

            var result = await Task.WhenAll(tasks)
                .ConfigureAwait(false);

            if (_modelInformationsCache.ModelsCache is not null)
                await Parallel.ForEachAsync(result, _ct, async (record, ct) =>
                    await Task.Run(() => _modelInformationsCache.ModelsCache?.RegisterOrUpdateRecord(record, _modelInformations), ct)
                        .ConfigureAwait(false)
                ).ConfigureAwait(false);

            return result;
        }
    }
}
