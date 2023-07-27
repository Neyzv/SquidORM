using System.Reflection;

namespace SquidORM.ModelsInformations.Models.ValueConvertions.Abstractions
{
    internal abstract class BaseValueConvertor
    {
        protected readonly MethodInfo _conversionMethod;
        protected readonly object?[] _parameters;

        public BaseValueConvertor(MethodInfo conversionMethod, object?[]? parameters = null) =>
            (_conversionMethod, _parameters) = (conversionMethod, parameters is null ? Array.Empty<object?>() : parameters);

        public object? Convert(object value) =>
            _conversionMethod.Invoke(null, new object[] { value }.Concat(_parameters).ToArray());
    }
}
