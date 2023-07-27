using System.Reflection;
using SquidORM.ModelsPatterns;

namespace SquidORM.ModelsInformations.Models.Abstractions
{
    public abstract class BaseModelColumnInformations
    {
        protected readonly PropertyInfo _propertyInfo;
        protected readonly ModelInformationsCache _modelInformationsCache;

        public BaseModelColumnInformations(PropertyInfo propertyInfo, ModelInformationsCache modelInformationsCache) =>
            (_propertyInfo, _modelInformationsCache) = (propertyInfo, modelInformationsCache);

        public object? GetValue(DatabaseRecord instance) =>
            _propertyInfo.GetValue(instance);
    }
}
