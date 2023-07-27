using SquidORM.Attributes;

namespace SquidORM.ModelsPatterns
{
    public abstract record DatabaseRecord
    {
        [Ignore]
        public bool IsDirty { get; set; }

        public DatabaseRecord() =>
            IsDirty = true;
    }
}
