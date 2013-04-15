namespace ADOMore
{
    using System;
    using System.Globalization;

    public sealed class ValueProvider : IValueProvider
    {
        public bool CanProvideValue(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type", "type cannot be null.");
            }

            return (type.IsPrimitive 
                && typeof(IConvertible).IsAssignableFrom(type)) 
                || type.IsValueType 
                || type == typeof(string)
                || type == typeof(Guid)
                || type == typeof(Version)
                || type == typeof(Uri);
        }

        public object ReadValue(Type type, object value)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type", "type cannot be null.");
            }

            object result = null;

            if (value != null && value != DBNull.Value)
            {
                if (type.IsEnum)
                {
                    result = Enum.ToObject(type, value);
                }
                else if (typeof(Guid).IsAssignableFrom(type))
                {
                    result = new Guid(value.ToString());
                }
                else if (typeof(Version).IsAssignableFrom(type))
                {
                    result = new Version(value.ToString());
                }
                else if (typeof(Uri).IsAssignableFrom(type))
                {
                    result = new Uri(value.ToString());
                }
                else
                {
                    result = Convert.ChangeType(value, type);
                }
            }

            return result;
        }

        public object WriteValue(Type type, object value)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type", "type cannot be null.");
            }

            object result = value;

            if (value == null)
            {
                result = DBNull.Value;
            }
            else if (type == typeof(char))
            {
                result = ((char)value).ToString(CultureInfo.InvariantCulture);
            }
            else if (typeof(Guid).IsAssignableFrom(type)
                || typeof(Version).IsAssignableFrom(type)
                || typeof(Uri).IsAssignableFrom(type))
            {
                result = value.ToString();
            }

            return result;
        }
    }
}