using SquidORM.ModelsInformations.Models.Utils;

namespace SquidORM.ModelsInformations.Models
{
    public class TableInformations
    {

        public string TableName { get; }

        public string DatabaseName { get; }

        public int Id { get; }

        public TableInformations(string databaseName, string tableName)
        {
            Id = TableInformationsIdComputer.ComputeId(tableName, databaseName);
            TableName = tableName;
            DatabaseName = databaseName;
        }
    }
}
