namespace ADOMore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    public static class ReflectionExtensions
    {
        public static bool IncludeInDbReflection(this Type propertyType)
        {
            return (propertyType.IsPrimitive && typeof(IConvertible).IsAssignableFrom(propertyType)) || propertyType.IsValueType || propertyType == typeof(string);
        }

        public static Type ResolveSettableType(this Type propertyType)
        {
            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return Nullable.GetUnderlyingType(propertyType);
            }

            return propertyType;
        }
    }
}
