namespace ADOMore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    internal static class ReflectionExtensions
    {
        internal static bool IncludeInDbReflection(this Type propertyType)
        {
            return (propertyType.IsPrimitive && typeof(IConvertible).IsAssignableFrom(propertyType)) || propertyType.IsValueType || propertyType == typeof(string);
        }

        internal static Type ResolveSettableType(this Type propertyType)
        {
            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return Nullable.GetUnderlyingType(propertyType);
            }

            return propertyType;
        }
    }
}
