using System.Collections.Concurrent;
using SquidORM.ModelsInformations.Models.Structs;
using SquidORM.ModelsPatterns;

namespace SquidORM.Records
{
    internal sealed class ModelReferences
    {
        public DatabaseRecord Record { get; set; }

        public ConcurrentStack<RelationshipInformations> ParentElements { get; }

        public ModelReferences(DatabaseRecord record)
        {
            Record = record;
            ParentElements = new ConcurrentStack<RelationshipInformations>();
        }
    }
}
