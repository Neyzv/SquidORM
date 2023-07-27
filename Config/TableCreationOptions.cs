namespace SquidORM.Config
{
    public readonly struct TableCreationOptions
    {
        public DatabaseAccessor Accessor { get; init; }

        public TableCreationMod CreationMod { get; init; }
    }
}
