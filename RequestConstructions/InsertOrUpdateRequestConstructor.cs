using System.Text;
using SquidORM.ModelsInformations.Models;

namespace SquidORM.RequestConstructions
{
    internal class InsertOrUpdateRequestConstructor : InsertRequestConstructor
    {
        private const string DuplicateKeySQLKeyWord = " ON DUPLICATE KEY UPDATE ";

        public InsertOrUpdateRequestConstructor(ModelInformations modelInformations)
            : base(modelInformations) { }

        public override string Construct()
        {
            var strBuilder = new StringBuilder(base.Construct());

            if (!string.IsNullOrWhiteSpace(_modelInformations.SQLUpdate))
                strBuilder.Append(DuplicateKeySQLKeyWord)
                .Append(_modelInformations.SQLUpdate);

            return strBuilder.ToString();
        }
    }
}
