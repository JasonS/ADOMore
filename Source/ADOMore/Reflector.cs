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
        private Type type;
        private IDictionary<PropertyInfo, Type> typeProperties;
        
        internal Reflector(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type", "type cannot be null.");
            }

            this.type = type;
            this.typeProperties = GetProperties(type);
        }

        public T ToObject<T>(IDataRecord dataRecord)
        {
            T model;
            
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

                if (propertyType.IsDatabaseCompatible())
                {
                    string upperName = property.Name.ToUpperInvariant();

                    if (fieldDictionary.ContainsKey(upperName))
                    {
                        object fieldValue = dataRecord.GetValue(fieldDictionary[upperName]);

                        if (fieldValue != null && fieldValue != DBNull.Value)
                        {
                            if (propertyType.IsEnum)
                            {
                                property.SetValue(model, Enum.ToObject(propertyType, fieldValue), null);
                            }
                            else
                            {
                                property.SetValue(model, Convert.ChangeType(fieldValue, propertyType), null);
                            }
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

                if (propertyType.IsDatabaseCompatible())
                {
                    object value = property.GetValue(model, null);
                    
                    if (value == null)
                    {
                        value = DBNull.Value;
                    }
                    else if (value.GetType() == typeof(char))
                    {
                        value = ((char)value).ToString(CultureInfo.InvariantCulture);
                    }
                    
                    IDbDataParameter parameter = command.CreateParameter();
                    parameter.ParameterName = string.Concat("@", property.Name);
                    parameter.Value = value;
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