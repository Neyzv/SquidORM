using System.Collections.Concurrent;
using System.Text;
using MySqlConnector;
using SquidORM.ModelsInformations;
using SquidORM.ModelsInformations.Models;
using SquidORM.ModelsPatterns;
using SquidORM.RequestConstructions;
using SquidORM.Session.Enums;

namespace SquidORM.Session
{
    public sealed class DatabaseSession
    {
        private record DatabaseSessionAction(DatabaseRecord Record, SessionAction Action,
            ModelInformations Model)
        {
            public SessionAction Action { get; set; } = Action;
        }

        private readonly MySqlConnection _connection;
        private readonly ModelInformationsCache _modelInformationsCache;
        private readonly CancellationToken _ct;
        private readonly ConcurrentQueue<DatabaseSessionAction> _records;

        public DatabaseSession(MySqlConnection connection,
            ModelInformationsCache modelInformationsCache,
            CancellationToken ct)
        {
            _connection = connection;
            _modelInformationsCache = modelInformationsCache;
            _ct = ct;
            _records = new ConcurrentQueue<DatabaseSessionAction>();
        }

        private async Task ManageDatabaseSessionAction<TRecord>(TRecord record, SessionAction action)
            where TRecord : DatabaseRecord, new()
        {
            if (_records.FirstOrDefault(x => x.Record == record) is { } rec)
                rec.Action = action;
            else
            {
                _records.Enqueue(new(record, action,
                     await _modelInformationsCache.RegisterAndGetModelInformationsAsync(typeof(TRecord)).ConfigureAwait(false)));
            }
        }

        public async Task AddAsync<TRecord>(TRecord record)
            where TRecord : DatabaseRecord, new() =>
             await ManageDatabaseSessionAction(record, SessionAction.Add).ConfigureAwait(false);

        public async Task RemoveAsync<TRecord>(TRecord record)
            where TRecord : DatabaseRecord, new() =>
             await ManageDatabaseSessionAction(record, SessionAction.Delete).ConfigureAwait(false);

        public async Task<int> CommitAsync()
        {
            var queryString = new StringBuilder();

            await _connection.OpenAsync()
                .ConfigureAwait(false);

            using var command = _connection.CreateCommand();

            var i = 0;
            foreach (var record in _records)
            {
                var prefix = i.ToString();
                if (record.Action is SessionAction.Add)
                    queryString.Append(new ParameterRenamingRequestConstructor(
                            new AutoIncrementedValueRequestConstructor(
                                new InsertOrUpdateRequestConstructor(record.Model)).Construct(), prefix
                            ).Construct());
                else
                    queryString.Append(new ParameterRenamingRequestConstructor(
                            new DeleteRequestConstructor(record.Model).Construct(), prefix
                        ).Construct());

                record.Model.BindCommandParametersFromInstance(record.Record, command, prefix);
                i++;
            }

            command.CommandText = queryString.ToString();

            using var result = await command.ExecuteReaderAsync(_ct)
                .ConfigureAwait(false);

            var recordIndex = 0;
            do
            {
                while (await result.ReadAsync(_ct).ConfigureAwait(false))
                    while (_records.TryDequeue(out var record))
                        if (record.Action is SessionAction.Add)
                        {
                            record.Model.BindAutoIncrementedValueToInstance(record.Record, result[0]);
                            recordIndex++;

                            break;
                        }
            }
            while (await result.NextResultAsync(_ct).ConfigureAwait(false));

            await _connection.CloseAsync()
                .ConfigureAwait(false);

            return i;
        }
    }
}
