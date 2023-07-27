using System.Collections.Concurrent;
using MySqlConnector;
using SquidORM.Caching;
using SquidORM.Config;
using SquidORM.ModelsInformations;
using SquidORM.ModelsInformations.Models;
using SquidORM.ModelsPatterns;
using SquidORM.RequestConstructions;
using SquidORM.RequestConstructions.Abstractions;
using SquidORM.Session;

namespace SquidORM
{
    public sealed class DatabaseAccessor : IDisposable
    {
        private const string FkTableNamesRequest = "SELECT DISTINCT k.TABLE_SCHEMA, k.TABLE_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS k WHERE k.REFERENCED_TABLE_NAME = @tableName AND k.REFERENCED_TABLE_SCHEMA = @dbName";
        private const string FkDbNameParameterLabel = "@dbName";
        private const string FkTableNameParameterLabel = "@tableName";

        private readonly DatabaseConfiguration _configuration;
        private readonly CancellationTokenSource _cts;
        private readonly ModelInformationsCache _modelsInformationsCache;
        private readonly ModelsCache? _cache;

        private bool _isDisposed;

        public CancellationToken Token =>
            _cts.Token;

        private readonly Lazy<DatabaseSession> _session;
        public DatabaseSession Session =>
            _session.Value;

        public DatabaseAccessor(DatabaseConfiguration configuration)
        {
            _cts = new CancellationTokenSource();
            _configuration = configuration;
            _cache = configuration.RecordCaching ? new ModelsCache() : default;
            _modelsInformationsCache = new ModelInformationsCache(CreateConnection, configuration.TableCreationMod.HasValue ?
                new()
                {
                    Accessor = this,
                    CreationMod = configuration.TableCreationMod.Value,
                }
                : null, configuration.DatabaseName, _cache, Token);

            _session = new Lazy<DatabaseSession>(() => new DatabaseSession(CreateConnection(), _modelsInformationsCache, Token),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        private MySqlConnection CreateConnection() =>
            new(_configuration.ConnectionString);

        private async Task<List<TableInformations>> GetForeignKeyTablesNamesAsync(TableInformations tableInformations)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync()
                .ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandText = FkTableNamesRequest;

            command.Parameters.AddWithValue(FkDbNameParameterLabel, tableInformations.DatabaseName);
            command.Parameters.AddWithValue(FkTableNameParameterLabel, tableInformations.TableName);

            using var reader = await command.ExecuteReaderAsync(Token)
                .ConfigureAwait(false);

            var result = new List<TableInformations>();

            do
            {
                while (await reader.ReadAsync(Token).ConfigureAwait(false))
                    result.Add(new TableInformations(reader.GetString(0), reader.GetString(1)));
            }
            while (await reader.NextResultAsync(Token).ConfigureAwait(false));

            return result;
        }

        public async Task<Query<TRecord>> QueryAsync<TRecord>()
            where TRecord : DatabaseRecord, new() =>
            new(CreateConnection, await _modelsInformationsCache.RegisterAndGetModelInformationsAsync(typeof(TRecord))
                .ConfigureAwait(false), _modelsInformationsCache, _cts.Token);

        public async Task<bool> InsertAsync<TRecord>(TRecord record)
            where TRecord : DatabaseRecord, new()
        {
            if (!_configuration.UseDirtySystem || record.IsDirty)
            {
                var connection = CreateConnection();

                await connection.OpenAsync()
                    .ConfigureAwait(false);

                var modelInformations = await _modelsInformationsCache
                    .RegisterAndGetModelInformationsAsync(typeof(TRecord))
                    .ConfigureAwait(false);

                using var command = connection.CreateCommand();
                command.CommandText = new AutoIncrementedValueRequestConstructor(
                        new InsertRequestConstructor(modelInformations)
                    ).Construct();

                modelInformations.BindCommandParametersFromInstance(record, command);

                var result = await command.ExecuteScalarAsync(Token)
                    .ConfigureAwait(false);

                if (result is not null)
                {
                    modelInformations.BindAutoIncrementedValueToInstance(record, result);
                    _cache?.RegisterOrUpdateRecord(record, modelInformations);
                }

                await connection.DisposeAsync()
                    .ConfigureAwait(false);

                return result is not null;
            }

            return false;
        }

        public async Task<bool> UpdateAsync<TRecord>(TRecord record)
            where TRecord : DatabaseRecord, new()
        {
            var result = false;

            if (!_configuration.UseDirtySystem || record.IsDirty)
            {
                var connection = CreateConnection();
                await connection.OpenAsync()
                    .ConfigureAwait(false);

                var modelInformations = await _modelsInformationsCache
                    .RegisterAndGetModelInformationsAsync(typeof(TRecord))
                    .ConfigureAwait(false);

                using var command = connection.CreateCommand();
                command.CommandText = new UpdateRequestConstructor(modelInformations)
                    .Construct();

                modelInformations.BindCommandParametersFromInstance(record, command);
                _cache?.RegisterOrUpdateRecord(record, modelInformations);

                result = await command.ExecuteNonQueryAsync(Token)
                    .ConfigureAwait(false) > 0;

                await connection.DisposeAsync()
                    .ConfigureAwait(false);
            }

            return result;
        }

        public async Task<bool> InsertOrUpdateAsync<TRecord>(TRecord record)
            where TRecord : DatabaseRecord, new()
        {
            var result = false;

            if (!_configuration.UseDirtySystem || record.IsDirty)
            {
                var connection = CreateConnection();
                await connection.OpenAsync()
                    .ConfigureAwait(false);

                var modelInformations = await _modelsInformationsCache
                    .RegisterAndGetModelInformationsAsync(typeof(TRecord))
                    .ConfigureAwait(false);

                RequestConstructor requestConstructor = new InsertOrUpdateRequestConstructor(modelInformations);
                var autoIncrementKeyValueNeedToBeBinded = !modelInformations.IsAutoIncrementedKeyBinded(record);
                if (autoIncrementKeyValueNeedToBeBinded)
                    requestConstructor = new AutoIncrementedValueRequestConstructor((requestConstructor as InsertOrUpdateRequestConstructor)!);

                using var command = connection.CreateCommand();
                command.CommandText = requestConstructor.Construct();

                modelInformations.BindCommandParametersFromInstance(record, command);

                if (autoIncrementKeyValueNeedToBeBinded)
                {
                    var autoIncrementKeyValue = await command.ExecuteScalarAsync(Token)
                        .ConfigureAwait(false);

                    if (autoIncrementKeyValue is not null)
                    {
                        modelInformations.BindAutoIncrementedValueToInstance(record, result);
                        _cache?.RegisterOrUpdateRecord(record, modelInformations);
                    }

                    result = autoIncrementKeyValue is not null;
                }
                else
                    result = await command.ExecuteNonQueryAsync(Token)
                        .ConfigureAwait(false) > 0;

                await connection.DisposeAsync()
                    .ConfigureAwait(false);
            }

            return result;
        }

        public async Task<bool> DeleteAsync<TRecord>(TRecord record)
            where TRecord : DatabaseRecord, new()
        {
            var modelInformations = await _modelsInformationsCache
                .RegisterAndGetModelInformationsAsync(typeof(TRecord))
                .ConfigureAwait(false);

            using var connection = CreateConnection();
            await connection.OpenAsync()
                .ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandText = new DeleteRequestConstructor(modelInformations)
                .Construct();

            modelInformations.BindCommandParametersFromInstance(record, command);

            _cache?.UnregisterRecord(record, modelInformations);

            return await command.ExecuteNonQueryAsync(Token)
                .ConfigureAwait(false) > 0;
        }

        public async Task<bool> CreateTableAsync(Type type,
            TableCreationMod creationMode = TableCreationMod.Create)
        {
            var modelInformations = await _modelsInformationsCache
                .RegisterAndGetModelInformationsAsync(type)
                .ConfigureAwait(false);

            using var connection = CreateConnection();
            await connection.OpenAsync()
                .ConfigureAwait(false);

            var tablesInformations = creationMode > TableCreationMod.Create ? new Dictionary<int, TableInformations>
            {
                [modelInformations.Id] = modelInformations
            } : new();

            if (creationMode > TableCreationMod.Create)
            {
                var tablesToTest = new ConcurrentQueue<TableInformations>();
                tablesToTest.Enqueue(tablesInformations.First().Value);

                await Parallel.ForEachAsync(tablesToTest, Token, async (tableInformations, token) =>
                {
                    foreach (var referencedTableInformations in await GetForeignKeyTablesNamesAsync(tableInformations)
                        .ConfigureAwait(false))
                    {
                        if (!tablesInformations.ContainsKey(referencedTableInformations.Id))
                        {
                            tablesToTest.Enqueue(referencedTableInformations);
                            tablesInformations.Add(referencedTableInformations.Id, referencedTableInformations);
                        }
                    }
                }).ConfigureAwait(false);
            }

            using var command = connection.CreateCommand();
            command.CommandText = await new TableCreationRequestConstructor(_modelsInformationsCache, tablesInformations.Values.Reverse(), creationMode)
                .ConstructAsync()
                .ConfigureAwait(false);

            return await command.ExecuteNonQueryAsync(Token)
                .ConfigureAwait(false) > 0;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                _cts.Cancel();
                _cts.Dispose();
            }

            GC.SuppressFinalize(this);
        }
    }
}
