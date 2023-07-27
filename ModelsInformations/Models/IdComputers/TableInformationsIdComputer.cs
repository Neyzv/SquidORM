namespace SquidORM.ModelsInformations.Models.Utils
{
    public static class TableInformationsIdComputer
    {
        private const byte BaseHashCodeNumber = 17;
        private const byte HashCodeMultiplicator = 23;

        public static int ComputeId(string tableName, string databaseName) =>
            BaseHashCodeNumber + tableName.GetHashCode() * HashCodeMultiplicator +
                databaseName.GetHashCode() * HashCodeMultiplicator;
    }
}
