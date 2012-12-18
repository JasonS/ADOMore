namespace ADOMore
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    /// <summary>
    /// Contains ADO helper methods for converting to and from model objects
    /// </summary>
    /// <typeparam name="T">The type of the model object</typeparam>
    internal sealed class Reflector
    {
        private PropertyInfo[] typeProperties;
        private Type myType;
        private readonly object typeLocker = new object();

        internal Reflector(Type objectType)
        {
            this.myType = objectType;
            this.EnsureTypeProperties();
        }

        /// <summary>
        /// Gets a collection of items of type <typeparamref name="T"/> from the provided datareader
        /// </summary>
        /// <param name="datareader">The datareader</param>
        /// <returns>The collection</returns>
        internal IEnumerable<T> ToCollection<T>(IDataReader datareader)
        {
            if (datareader == null)
            {
                throw new ArgumentNullException("dataReader", "dataReader cannot be null");
            }

            while (datareader.Read())
            {
                yield return this.ToObject<T>((IDataRecord)datareader);
            }
        }

        /// <summary>
        /// Converts the provided record to an instance of <typeparamref name="T"/>
        /// </summary>
        /// <param name="dataRecord">An <see cref="IDataRecord"/></param>
        /// <returns>An instance of <typeparamref name="T"/></returns>
        public T ToObject<T>(IDataRecord dataRecord)
        {
            T model;
            
            Dictionary<string, int> fieldDictionary;
            IEnumerable<PropertyInfo> settable;
            model = Activator.CreateInstance<T>();
            settable = this.typeProperties.Where(p => p.CanWrite).ToArray();
            fieldDictionary = new Dictionary<string, int>();

            for (int i = 0, c = dataRecord.FieldCount; i < c; i++)
            {
                fieldDictionary.Add(dataRecord.GetName(i).ToUpperInvariant(), i);
            }

            foreach (PropertyInfo property in settable)
            {
                Type propertyType = property.PropertyType.UnderlyingType();

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

        /// <summary>
        /// Creates a data command from a text command, a POCO object and a data connection
        /// </summary>
        /// <param name="sql">The SQL command</param>
        /// <param name="model">The POCO object</param>
        /// <param name="connection">The data connection</param>
        /// <param name="commandType">A command type</param>
        /// <param name="transaction">An optional transaction</param>
        /// <returns>A data command</returns>
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

            foreach (PropertyInfo property in this.typeProperties)
            {
                Type propertyType = property.PropertyType.UnderlyingType();

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

        private void EnsureTypeProperties()
        {
            lock (this.typeLocker)
            {
                if (this.typeProperties == null)
                {
                    if (this.typeProperties == null)
                    {
                        this.typeProperties = myType.GetProperties().ToArray();
                    }
                }
            }
        }
    }
}
