using SquidORM.Attributes.Enums;

namespace SquidORM.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class MysqlTypeAttribute : Attribute
    {
        public MysqlType DbType { get; }

        public MysqlTypeAttribute(MysqlType dbType) =>
            DbType = dbType;
    }
}
