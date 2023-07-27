using SquidORM.ModelsInformations.Models.ValueConvertions.Abstractions;

namespace SquidORM.ModelsInformations.Models.ValueConvertions
{
    internal sealed class ListValueConvertor : BaseValueConvertor
    {
        private const string ToListMethodName = "ToList";

        public ListValueConvertor(Type enumerableType, Type modelType)
            : base(enumerableType.GetMethod(ToListMethodName)!.MakeGenericMethod(modelType) ??
                    throw new Exception($"An error has occured, can not find method {ToListMethodName} in class {enumerableType.Name}..."))
        { }
    }
}
