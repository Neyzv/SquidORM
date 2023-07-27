namespace SquidORM.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class KeyAttribute : Attribute
    {
        public bool AutoIncrement { get; }

        public KeyAttribute(bool autoIncrement = false) =>
            AutoIncrement = autoIncrement;
    }
}
