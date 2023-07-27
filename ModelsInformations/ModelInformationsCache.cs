using System.Collections.Concurrent;
using System.Reflection;
using MySqlConnector;
using SquidORM.Attributes;
using SquidORM.Caching;
using SquidORM.Config;
using SquidORM.ModelsInformations.Models;
using SquidORM.ModelsInformations.Models.Utils;
using SquidORM.ModelsPatterns;

namespace SquidORM.ModelsInformations
{
    public sealed class ModelInformationsCache
    {
        private static readonly Type _databaseRecordType;

        private readonly Func<MySqlConnection> _connectionFactory;
        private readonly TableCreationOptions? _tableCreationOptions;
        private readonly string? _defaultDatabaseName;
        private readonly CancellationToken _ct;
        private readonly ConcurrentDictionary<int, ModelInformations> _modelsInfos;

        public ModelsCache? ModelsCache { get; }

        static ModelInformationsCache() =>
            _databaseRecordType = typeof(DatabaseRecord);

        public ModelInformationsCache(Func<MySqlConnection> connectionFactory, TableCreationOptions? tableCreationOptions,
            string? defaultDatabaseName, ModelsCache? modelsCache, CancellationToken ct)
        {
            _connectionFactory = connectionFactory;
            _tableCreationOptions = tableCreationOptions;
            _defaultDatabaseName = defaultDatabaseName;
            _ct = ct;

            ModelsCache = modelsCache;

            _modelsInfos = new ConcurrentDictionary<int, ModelInformations>();
        }

        public ModelInformations? GetModelInformationsFromTableInformations(TableInformations tableInformations) =>
            _modelsInfos.TryGetValue(tableInformations.Id, out var modelInformations) ? modelInformations : null;

        public async Task<ModelInformations> RegisterAndGetModelInformationsAsync(Type type)
        {
            if (type.GetCustomAttribute<TableAttribute>() is not { } tableAttribute)
                throw new MissingMemberException($"Can not find the {nameof(TableAttribute)} of record {type.Name}...");

            if (!_modelsInfos.TryGetValue(TableInformationsIdComputer.ComputeId(tableAttribute.TableName,
                    (tableAttribute.DatabaseName ?? _defaultDatabaseName) ?? throw new Exception($"Model {type.Name} doesn't have a database name provided, and no database name have been provided in the configuration...")),
                out ModelInformations? result))
            {
                if (!type.IsSubclassOf(_databaseRecordType))
                    throw new ArgumentException($"Provided type {type.Name} must be a subclass of {nameof(DatabaseRecord)}...");

                result = new ModelInformations(this, type, tableAttribute, _connectionFactory, _defaultDatabaseName, _ct);
                _modelsInfos.TryAdd(result.Id, result);

                await result.AnalyseRecordAsync(ModelsCache).ConfigureAwait(false);

                if (_tableCreationOptions.HasValue)
                    await _tableCreationOptions.Value.Accessor.CreateTableAsync(type, _tableCreationOptions.Value.CreationMod)
                        .ConfigureAwait(false);
            }

            return result;
        }
    }
}
