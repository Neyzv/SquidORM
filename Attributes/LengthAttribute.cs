namespace SquidORM.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class LengthAttribute : Attribute
    {
        public long Length { get; }

        public LengthAttribute(long length) =>
            Length = length;
    }
}
