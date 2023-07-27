using System.Text;
using SquidORM.Attributes.Enums;
using SquidORM.ModelsInformations.Models;
using SquidORM.ModelsInformations.Models.Structs;
using SquidORM.RequestConstructions.Abstractions;

namespace SquidORM.RequestConstructions
{
    internal sealed class ForeignKeyRequestConstructor : RequestConstructor
    {
        private static readonly Dictionary<FkAction, string> _fkActionsToString = new()
        {
            [FkAction.Restrict] = "RESTRICT",
            [FkAction.Cascade] = "CASCADE",
            [FkAction.SetNull] = "SET NULL",
            [FkAction.NoAction] = "NO ACTION"
        };

        private const string ForeignKeySQLInstruction = "FOREIGN KEY ";
        private const string ReferencesSQLInstruction = " REFERENCES ";
        private const string OnKeyWord = " ON ";

        private readonly ModelColumnInformations _modelInformations;
        private readonly ForeignKeyInformations? _foreignKeyInformations;

        public ForeignKeyRequestConstructor(ModelColumnInformations modelInformations,
            ForeignKeyInformations? foreignKeyInformations) =>
            (_modelInformations, _foreignKeyInformations) = (modelInformations, foreignKeyInformations);

        public override string Construct() =>
            _foreignKeyInformations.HasValue ?
                new StringBuilder()
                    .Append(ForeignKeySQLInstruction)
                    .Append(OpenedParenthese)
                    .Append(NameChar)
                    .Append(_modelInformations.NameInTable)
                    .Append(NameChar)
                    .Append(ClosedParathese)
                    .Append(ReferencesSQLInstruction)
                    .Append(NameChar)
                    .Append(_foreignKeyInformations.Value.ModelInformations.DatabaseName)
                    .Append(NameChar)
                    .Append(Dot)
                    .Append(NameChar)
                    .Append(_foreignKeyInformations.Value.ModelInformations.TableName)
                    .Append(NameChar)
                    .Append(OpenedParenthese)
                    .Append(_foreignKeyInformations.Value.ModelInformations.GetColumnNameInTable(_foreignKeyInformations.Value.ForeignKeyAttribute.PropertyName))
                    .Append(ClosedParathese)
                    .Append(OnKeyWord)
                    .Append(DeleteSQLInstruction)
                    .Append(_fkActionsToString[_foreignKeyInformations.Value.ForeignKeyAttribute.OnDelete])
                    .Append(OnKeyWord)
                    .Append(UpdateSQLInstruction)
                    .Append(_fkActionsToString[_foreignKeyInformations.Value.ForeignKeyAttribute.OnUpdate])
                    .ToString()
            : string.Empty;
    }
}
