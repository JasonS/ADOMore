namespace ADOMore
{
    using System;

    public interface IValueProvider
    {
        bool CanProvideValue(Type type);

        object ReadValue(Type type, object value);

        object WriteValue(Type type, object value);
    }
}