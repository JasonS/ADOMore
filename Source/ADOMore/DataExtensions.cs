namespace ADOMore
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text;

    public static class DataExtensions
    {
        /// <summary>
        /// Creates and instance of type T from the provided data reader
        /// </summary>
        /// <typeparam name="T">The type of the model to create</typeparam>
        /// <param name="reader">The data reader</param>
        /// <param name="readFirst">Should the reader be read before reflecting?</param>
        /// <returns>The model</returns>
        public static T ToModel<T>(this IDataReader reader, bool readFirst)
        {
            Reflector<T> reflector = new Reflector<T>();
            return reflector.ToModel(reader, readFirst);
        }

        /// <summary>
        /// Creates and instance of type T from the provided data record
        /// </summary>
        /// <typeparam name="T">The type of the model to create</typeparam>
        /// <param name="dataRecord">The data record</param>
        /// <returns>The model</returns>
        public static T ToModel<T>(this IDataRecord dataRecord)
        {
            Reflector<T> reflector = new Reflector<T>();
            return reflector.ToModel(dataRecord);
        }

        /// <summary>
        /// Create a collection of models from the data reader
        /// </summary>
        /// <typeparam name="T">The type of the models in the collection</typeparam>
        /// <param name="reader">A data reader</param>
        /// <returns>The collection</returns>
        public static IEnumerable<T> ToModelCollection<T>(this IDataReader reader)
        {
            Reflector<T> reflector = new Reflector<T>();
            return reflector.ToCollection(reader);
        }

        /// <summary>
        /// Creates a sql command from parameterized sql text and a model of type T
        /// </summary>
        /// <typeparam name="T">The type of the model</typeparam>
        /// <param name="connection">A connection</param>
        /// <param name="sql">A sql string</param>
        /// <param name="model">The model to inject values from</param>
        /// <param name="transaction">An optional transaction</param>
        /// <returns>The command</returns>
        public static IDbCommand CreateCommand<T>(this IDbConnection connection, string sql, T model, IDbTransaction transaction)
        {
            Reflector<T> reflector = new Reflector<T>();
            return reflector.CreateCommand(sql, model, connection, CommandType.Text, transaction);
        }
    }
}
