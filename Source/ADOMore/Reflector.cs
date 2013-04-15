namespace ADOMore
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;

    internal sealed class Reflector
    {
        private IEnumerable<IValueProvider> valueProviders;
        private IDictionary<PropertyInfo, Type> typeProperties;
        private IValueProvider valueTypeProvider;
        private bool isValueType;

        internal Reflector(Type type, IEnumerable<IValueProvider> valueProviders)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type", "type cannot be null.");
            }

            if (valueProviders == null)
            {
                throw new ArgumentNullException("valueProviders", "valueProviders cannot be null.");
            }

            this.valueProviders = valueProviders;
            this.valueTypeProvider = valueProviders.FirstOrDefault(p => p.CanProvideValue(type.UnderlyingType()));

            if (this.valueTypeProvider != null)
            {
                this.isValueType = true;
            }
            else
            {
                this.typeProperties = GetProperties(type);
            }
        }

        public T ToObject<T>(IDataRecord dataRecord)
        {
            T model;

            if (this.isValueType)
            {
                if (dataRecord.FieldCount > 0)
                {
                    model = (T)this.valueTypeProvider.ReadValue(typeof(T).UnderlyingType(), dataRecord.GetValue(0));
                }
                else
                {
                    model = default(T);
                }
            }
            else
            {
                Dictionary<string, int> fieldDictionary;
                IEnumerable<PropertyInfo> settable;
                model = Activator.CreateInstance<T>();
                settable = this.typeProperties.Keys.Where(p => p.CanWrite).ToArray();
                fieldDictionary = new Dictionary<string, int>();

                for (int i = 0, c = dataRecord.FieldCount; i < c; i++)
                {
                    fieldDictionary.Add(dataRecord.GetName(i).ToUpperInvariant(), i);
                }

                foreach (PropertyInfo property in settable)
                {
                    Type propertyType = this.typeProperties[property];
                    IValueProvider provider = this.valueProviders.FirstOrDefault(p => p.CanProvideValue(propertyType));

                    if (provider != null)
                    {
                        string upperName = property.Name.ToUpperInvariant();

                        if (fieldDictionary.ContainsKey(upperName))
                        {
                            object fieldValue = dataRecord.GetValue(fieldDictionary[upperName]);
                            property.SetValue(model, provider.ReadValue(propertyType, fieldValue), null);
                        }
                    }
                }
            }

            return model;
        }

        internal IDbCommand CreateCommand(string sql, object model, IDbConnection connection, CommandType commandType, IDbTransaction transaction)
        {
            IDbCommand command = null;

            if (connection == null)
            {
                throw new ArgumentNullException("connection", "connection cannot be null");
            }

            if (model == null)
            {
                throw new ArgumentNullException("model", "model cannot be null");
            }

            if (string.IsNullOrEmpty(sql))
            {
                throw new ArgumentNullException("sql", "sql cannot be null or empty");
            }

            command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandType = commandType;

            if (transaction != null)
            {
                command.Transaction = transaction;
            }

            foreach (PropertyInfo property in this.typeProperties.Keys)
            {
                Type propertyType = this.typeProperties[property];
                IValueProvider provider = this.valueProviders.FirstOrDefault(p => p.CanProvideValue(propertyType));

                if (provider != null)
                {
                    IDbDataParameter parameter = command.CreateParameter();
                    parameter.ParameterName = string.Concat("@", property.Name);
                    parameter.Value = provider.WriteValue(propertyType, property.GetValue(model, null));
                    command.Parameters.Add(parameter);
                }
            }

            return command;
        }

        private static IDictionary<PropertyInfo, Type> GetProperties(Type type)
        {
            Dictionary<PropertyInfo, Type> dict = new Dictionary<PropertyInfo, Type>();

            foreach (PropertyInfo prop in type.GetProperties())
            {
                dict.Add(prop, prop.PropertyType.UnderlyingType());
            }

            return dict;
        }
    }
}