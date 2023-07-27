namespace SquidORM.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class RelationshipAttribute : Attribute
    {
        public string ModelPropertyName { get; }

        public string ReferencialPropertyName { get; }

        public string? ReferencialKeyNameForDictRelationship { get; }

        public RelationshipAttribute(string modelPropertyName, string referencialPropertyName,
            string? referencialKeyNameForDictRelationship = null)
        {
            if (string.IsNullOrWhiteSpace(modelPropertyName))
                throw new ArgumentNullException(nameof(modelPropertyName));

            if (string.IsNullOrWhiteSpace(referencialPropertyName))
                throw new ArgumentNullException(nameof(referencialPropertyName));

            if (referencialPropertyName == referencialKeyNameForDictRelationship)
                throw new Exception("Can not have the same referencial for the model and the dictionary...");

            ModelPropertyName = modelPropertyName;
            ReferencialPropertyName = referencialPropertyName;
            ReferencialKeyNameForDictRelationship = referencialKeyNameForDictRelationship;
        }
    }
}
