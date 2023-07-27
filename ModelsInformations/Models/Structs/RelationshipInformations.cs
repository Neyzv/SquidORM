using SquidORM.Attributes;
using SquidORM.ModelsPatterns;

namespace SquidORM.ModelsInformations.Models.Structs
{
    internal readonly struct RelationshipInformations
    {
        public DatabaseRecord Record { get; init; }

        public IReadOnlyCollection<RelationshipAttribute> RelationshipAttributes { get; init; }

        public ModelInformations ModelInformations { get; init; }
    }
}
