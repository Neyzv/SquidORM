namespace SquidORM.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class NameAttribute : Attribute
    {
        public string Name { get; }

        public NameAttribute(string name) =>
            Name = name;
    }
}
