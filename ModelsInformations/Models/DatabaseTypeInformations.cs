namespace SquidORM.ModelsInformations.Models
{
    public readonly struct DatabaseTypeInformations
    {
        public string TypeName { get; }

        public bool Unsigned { get; }

        public long? Limit { get; }

        public DatabaseTypeInformations(string typeName, bool unsigned = false, long? limit = null)
        {
            TypeName = typeName;
            Unsigned = unsigned;
            Limit = limit;
        }
    }
}
