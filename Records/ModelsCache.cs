using System.Collections.Concurrent;
using SquidORM.ModelsInformations.Models;
using SquidORM.ModelsInformations.Models.Structs;
using SquidORM.ModelsPatterns;
using SquidORM.Records;

namespace SquidORM.Caching
{
    public sealed class ModelsCache
    {
        private readonly ConcurrentDictionary<Guid, List<ModelReferences>> _cache;

        public ModelsCache() =>
            _cache = new ConcurrentDictionary<Guid, List<ModelReferences>>();

        public TRecord? RetrieveModel<TRecord>(Func<TRecord, bool> predicate)
            where TRecord : DatabaseRecord, new() =>
             (TRecord?)(_cache.TryGetValue(typeof(TRecord).GUID, out var values) ? values.FirstOrDefault(x => x.Record is TRecord record && predicate(record))?
                    .Record
                : null);

        public TRecord[] RetrieveModels<TRecord>(Func<TRecord, bool> predicate)
            where TRecord : DatabaseRecord, new() =>
            (TRecord[])(_cache.TryGetValue(typeof(TRecord).GUID, out var values) ? values.Where(x => x.Record is TRecord record && predicate(record))
                    .Select(x => x.Record).ToArray()
                : Array.Empty<TRecord>());

        public void RegisterOrUpdateRecord(DatabaseRecord record, ModelInformations modelInformations)
        {
            var models = _cache.GetOrAdd(modelInformations.Type.GUID, _ => new List<ModelReferences>());

            lock (models)
            {
                if (models.FirstOrDefault(x => x.Record == record) is { } modelReferences)
                {
                    foreach (var relationshipInformations in modelReferences.ParentElements)
                        foreach (var relationshipAttribute in relationshipInformations.RelationshipAttributes)
                            relationshipInformations.ModelInformations.SetColumnValue(relationshipInformations.Record,
                                    relationshipAttribute.ModelPropertyName,
                                    modelInformations.GetColumnValue(record, relationshipAttribute.ReferencialPropertyName)
                                );

                }
                else
                    models.Add(new ModelReferences(record));
            }

            foreach (var relationshipInformations in modelInformations.GetRelationshipInstances(record))
            {
                RegisterOrUpdateRecord(relationshipInformations.Record, relationshipInformations.ModelInformations);

                if (_cache.TryGetValue(relationshipInformations.ModelInformations.Type.GUID, out var relationshipModels))
                {
                    if (relationshipModels.FirstOrDefault(x => x.Record == relationshipInformations.Record) is { } relationshipModelReferences)
                        relationshipModelReferences.ParentElements.Push(new RelationshipInformations()
                        {
                            Record = record,
                            RelationshipAttributes = relationshipInformations.RelationshipAttributes,
                            ModelInformations = modelInformations
                        });
                }
                else
                    throw new Exception($"Can not find models of record {relationshipInformations.ModelInformations.Type.Name} in the cache...");
            }
        }

        public void UnregisterRecord(DatabaseRecord record, ModelInformations modelInformations)
        {
            if (_cache.TryGetValue(modelInformations.Type.GUID, out var typeStorage))
            {
                lock (typeStorage)
                {
                    var index = typeStorage.FindIndex(x => x.Record == record);

                    if (index is not -1)
                        typeStorage.RemoveAt(index);
                }
            }
        }
    }
}
