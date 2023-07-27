namespace SquidORM.RequestConstructions.Abstractions
{
    public abstract class BaseRequestConstructor
    {
        protected const char Comma = ',';
        protected const char Blank = ' ';
        protected const char NameChar = '`';
        protected const char Semicolon = ';';
        protected const char OpenedParenthese = '(';
        protected const char ClosedParathese = ')';
        protected const string Equal = " = ";
        protected const char Dot = '.';

        protected const string UpdateSQLInstruction = "UPDATE ";
        protected const string SetKeyWord = " SET ";
        protected const string DeleteSQLInstruction = "DELETE ";
        protected const string FromKeyWord = "FROM ";
        protected const string CreateTableSQLInstruction = "CREATE TABLE IF NOT EXISTS ";

        public const char At = '@';
        public const string WhereKeyWord = " WHERE ";
        public const string AndSQLOperand = " AND ";
    }
}
