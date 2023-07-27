using System.Text;
using SquidORM.Config;
using SquidORM.ModelsInformations;
using SquidORM.ModelsInformations.Models;
using SquidORM.RequestConstructions.Abstractions;

namespace SquidORM.RequestConstructions
{
    public sealed class TableCreationRequestConstructor : AsyncRequestConstructor
    {
        private const string DropTableSQLInstruction = "DROP TABLE IF EXISTS ";
        private const string ForeignKeyChecksSQLInstruction = "SET FOREIGN_KEY_CHECKS = ";
        private const byte Disable = 0;
        private const byte Enable = 1;

        private readonly ModelInformationsCache _modelInformationsCache;
        private readonly IEnumerable<TableInformations> _tablesInformations;
        private readonly TableCreationMod _tableCreationMod;

        public TableCreationRequestConstructor(ModelInformationsCache modelInformationsCache,
            IEnumerable<TableInformations> tablesInformations, TableCreationMod tableCreationMod)
        {
            _modelInformationsCache = modelInformationsCache;
            _tablesInformations = tablesInformations;
            _tableCreationMod = tableCreationMod;
        }

        public override async ValueTask<string> ConstructAsync()
        {
            var strBuilder = new StringBuilder();

            if (_tableCreationMod > TableCreationMod.Create)
                strBuilder.Append(ForeignKeyChecksSQLInstruction)
                        .Append(Disable)
                        .Append(Semicolon);

            foreach (var tableInformations in _tablesInformations)
            {
                if (_tableCreationMod > TableCreationMod.Create)
                    strBuilder.Append(DropTableSQLInstruction)
                        .Append(NameChar)
                        .Append(tableInformations.DatabaseName)
                        .Append(NameChar)
                        .Append(Dot)
                        .Append(NameChar)
                        .Append(tableInformations.TableName)
                        .Append(NameChar)
                        .Append(Semicolon);

                if (_modelInformationsCache.GetModelInformationsFromTableInformations(tableInformations) is { } modelInformations)
                    strBuilder.Append(CreateTableSQLInstruction)
                        .Append(NameChar)
                        .Append(tableInformations.DatabaseName)
                        .Append(NameChar)
                        .Append(Dot)
                        .Append(NameChar)
                        .Append(tableInformations.TableName)
                        .Append(NameChar)
                        .Append(await modelInformations.GetSQLCreateColumns().ConfigureAwait(false))
                        .Append(Semicolon);
            }

            if (_tableCreationMod > TableCreationMod.Create)
                strBuilder.Append(ForeignKeyChecksSQLInstruction)
                    .Append(Enable);


            return strBuilder.ToString();
        }
    }
}
