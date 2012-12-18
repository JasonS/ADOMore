namespace ADOMore
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    public static class DataExtensions
    {
        public static readonly object dictionaryLock = new object();
        private static readonly Dictionary<string, Reflector> typeCache = new Dictionary<string, Reflector>();

        /// <summary>
        /// Creates a sql command from parameterized sql text and a model of type T
        /// </summary>
        /// <typeparam name="T">The type of the model</typeparam>
        /// <param name="connection">This db connection</param>
        /// <param name="sql">A sql string</param>
        /// <param name="model">The model to inject values from</param>
        /// <param name="transaction">An optional transaction</param>
        /// <returns>The command</returns>
        public static IDbCommand CreateCommand(this IDbConnection connection, string sql, object model, IDbTransaction transaction)
        {
            return new Reflector(model.GetType()).CreateCommand(sql, model, connection, CommandType.Text, transaction);
        }

        /// <summary>
        /// Creates a sql command from parameterized sql text and a model of type T
        /// </summary>
        /// <typeparam name="T">The type of the model</typeparam>
        /// <param name="connection">This db connection</param>
        /// <param name="sql">A sql string</param>
        /// <param name="model">The model to inject values from</param>
        /// <param name="transaction">An optional transaction</param>
        /// <returns>The command</returns>
        public static IDbCommand CreateCommand<T>(this IDbConnection connection, string sql, T model, IDbTransaction transaction)
        {
            return CheckReflectorCache(typeof(T)).CreateCommand(sql, model, connection, CommandType.Text, transaction);
        }

        /// <summary>
        /// Executes a query agains a database
        /// </summary>
        /// <param name="connection">This sql connection</param>
        /// <param name="sql">The sql string to execute</param>
        /// <param name="parameters">An object representing query parameters</param>
        /// <param name="transaction">An optional transaction</param>
        /// <returns>The number of affected rows</returns>
        public static int Execute(this IDbConnection connection, string sql, object parameters, IDbTransaction transaction)
        {
            using (IDbCommand command = new Reflector(parameters.GetType()).CreateCommand(sql, parameters, connection, CommandType.Text, transaction))
            {
                using (IDataReader reader = command.ExecuteReader())
                {
                    return reader.RecordsAffected;
                }
            }
        }

        /// <summary>
        /// Queries the database with the provided sql connection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="sql"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public static IEnumerable<T> Query<T>(this IDbConnection connection, string sql, T parameters, IDbTransaction transaction)
        {
            Reflector reflector = CheckReflectorCache(typeof(T));

            using (IDbCommand command = reflector.CreateCommand(sql, parameters, connection, CommandType.Text, transaction))
            {
                using (IDataReader reader = command.ExecuteReader())
                {
                    return reflector.ToCollection<T>(reader);
                }
            }
        }

        /// <summary>
        /// Creates and instance of type T from the provided data record
        /// </summary>
        /// <typeparam name="T">The type of the model to create</typeparam>
        /// <param name="dataRecord">The data record</param>
        /// <returns>The model</returns>
        public static T ToObject<T>(this IDataRecord dataRecord)
        {
            return CheckReflectorCache(typeof(T)).ToObject<T>(dataRecord);
        }

        private static Reflector CheckReflectorCache(Type type)
        {
            lock (dictionaryLock)
            {
                if (!typeCache.ContainsKey(type.Name))
                {
                    typeCache.Add(type.Name, new Reflector(type));
                }
            }

            return typeCache[type.Name];
        }
    }
}
