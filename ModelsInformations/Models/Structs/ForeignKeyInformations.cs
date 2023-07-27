using SquidORM.Attributes;

namespace SquidORM.ModelsInformations.Models.Structs
{
    internal readonly struct ForeignKeyInformations
    {
        public ForeignKeyAttribute ForeignKeyAttribute { get; init; }

        public ModelInformations ModelInformations { get; init; }
    }
}
