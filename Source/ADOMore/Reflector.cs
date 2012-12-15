    namespace ADOMore
    {
        using System;
        using System.Collections.Generic;
        using System.Data;
        using System.Linq;
        using System.Reflection;
        using System.Text;

        /// <summary>
        /// Contains ADO helper methods for converting to and from model objects
        /// </summary>
        /// <typeparam name="T">The type of the model object</typeparam>
        public class Reflector<T>
        {
            private List<PropertyInfo> typeProperties;
            private Type myType = typeof(T);

            /// <summary>
            /// Gets the collection of properties associated with type <typeparamref name="T"/>
            protected IEnumerable<PropertyInfo> TypeProperties
            {
                get
                {
                    if (this.typeProperties == null)
                    {
                        this.typeProperties = new List<PropertyInfo>();
                        this.typeProperties.AddRange(myType.GetProperties());
                    }

                    return this.typeProperties.AsReadOnly();
                }
            }

            /// <summary>
            /// Gets a collection of items of type <typeparamref name="T"/> from the provided datareader
            /// </summary>
            /// <param name="datareader">The datareader</param>
            /// <returns>The collection</returns>
            public IEnumerable<T> ToCollection(IDataReader datareader)
            {
                if (datareader == null)
                {
                    throw new ArgumentNullException("dataReader", "dataReader cannot be null");
                }

                List<T> coll = new List<T>();

                while (datareader.Read())
                {
                    coll.Add(this.ToModel((IDataRecord)datareader));
                }

                return coll.AsReadOnly();
            }

            /// <summary>
            /// Converts the provided record to an instance of <typeparamref name="T"/>
            /// </summary>
            /// <param name="dataReader">An <see cref="IDataReader"/></param>
            /// <param name="readFirst">If true then the reader will be read first</param>
            /// <returns>An instance of <typeparamref name="T"/></returns>
            public T ToModel(IDataReader datareader, bool readFirst)
            {
                if (readFirst)
                {
                    datareader.Read();
                }

                return this.ToModel((IDataRecord)datareader);
            }

            /// <summary>
            /// Converts the provided record to an instance of <typeparamref name="T"/>
            /// </summary>
            /// <param name="dataRecord">An <see cref="IDataRecord"/></param>
            /// <returns>An instance of <typeparamref name="T"/></returns>
            public T ToModel(IDataRecord dataRecord)
            {
                T model;
                Dictionary<string, int> fieldDictionary;
                IEnumerable<PropertyInfo> settable;

                model = Activator.CreateInstance<T>();
                settable = this.TypeProperties.Where(p => p.CanWrite).ToArray();
                fieldDictionary = new Dictionary<string, int>();

                for (int i = 0, c = dataRecord.FieldCount; i < c; i++)
                {
                    fieldDictionary.Add(dataRecord.GetName(i).ToUpperInvariant(), i);
                }

                foreach (PropertyInfo property in settable)
                {
                    Type propertyType = property.PropertyType.ResolveSettableType();

                    if (propertyType.IncludeInDbReflection())
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
            /// <returns>A data command</returns>
            public IDbCommand CreateCommand(string sql, T model, IDbConnection connection)
            {
                return this.CreateCommand(sql, model, connection, CommandType.Text);
            }

            /// <summary>
            /// Creates a data command from a text command, a POCO object and a data connection
            /// </summary>
            /// <param name="sql">The SQL command</param>
            /// <param name="model">The POCO object</param>
            /// <param name="connection">The data connection</param>
            /// <param name="commandType">A command type</param>
            /// <returns>A data command</returns>
            public IDbCommand CreateCommand(string sql, T model, IDbConnection connection, CommandType commandType)
            {
                return this.CreateCommand(sql, model, connection, commandType, null);
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
            public IDbCommand CreateCommand(string sql, T model, IDbConnection connection, CommandType commandType, IDbTransaction transaction)
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

                foreach (PropertyInfo property in this.TypeProperties)
                {
                    Type propertyType = property.PropertyType.ResolveSettableType();

                    if (propertyType.IncludeInDbReflection())
                    {
                        IDbDataParameter parameter = command.CreateParameter();
                        parameter.ParameterName = string.Concat("@", property.Name);
                        parameter.Value = property.GetValue(model, null);
                    
                        if (parameter.Value == null)
                        {
                            parameter.Value = DBNull.Value;
                        }

                        command.Parameters.Add(parameter);
                    }
                }

                return command;
            }
        }
    }
