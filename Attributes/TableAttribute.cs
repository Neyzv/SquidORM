namespace SquidORM.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class TableAttribute : Attribute
    {

        public string TableName { get; }

        public string? DatabaseName { get; }

        public TableAttribute(string tableName, string? databaseName = null)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentNullException(nameof(tableName));

            if (databaseName is not null && string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentNullException(nameof(databaseName));

            TableName = tableName;
            DatabaseName = databaseName;
        }
    }
}
