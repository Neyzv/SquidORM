using SquidORM.Attributes.Enums;

namespace SquidORM.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ForeignKeyAttribute : Attribute
    {
        public Type ReferedType { get; }

        public string PropertyName { get; }

        public FkAction OnDelete { get; }

        public FkAction OnUpdate { get; }

        public ForeignKeyAttribute(Type referedType, string propertyName,
            FkAction onDelete = FkAction.Restrict, FkAction onUpdate = FkAction.Restrict)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentNullException(nameof(propertyName));

            ReferedType = referedType;
            PropertyName = propertyName;
            OnDelete = onDelete;
            OnUpdate = onUpdate;
        }
    }
}
