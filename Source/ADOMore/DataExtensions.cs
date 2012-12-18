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
        /// <param name="connection">This db connection</param>
        /// <param name="sql">A sql string</param>
        /// <returns>The command</returns>
        public static IDbCommand CreateCommand(this IDbConnection connection, string sql)
        {
            return connection.CreateCommand(sql, null, null);
        }

        /// <summary>
        /// Creates a sql command from parameterized sql text and a model of type T
        /// </summary>
        /// <param name="connection">This db connection</param>
        /// <param name="sql">A sql string</param>
        /// <param name="parameters">An object to use when paraterizing a sql query</param>
        /// <returns>The command</returns>
        public static IDbCommand CreateCommand(this IDbConnection connection, string sql, object parameters)
        {
            return connection.CreateCommand(sql, parameters, null);
        }

        /// <summary>
        /// Creates a sql command from parameterized sql text and a model of type T
        /// </summary>
        /// <param name="connection">This db connection</param>
        /// <param name="sql">A sql string</param>
        /// <param name="parameters">An object to use when paraterizing a sql query</param>
        /// <param name="transaction">An optional transaction</param>
        /// <returns>The command</returns>
        public static IDbCommand CreateCommand(this IDbConnection connection, string sql, object parameters, IDbTransaction transaction)
        {
            IDbCommand result = null;

            try
            {
                if (parameters != null)
                {
                    result = CheckReflectorCache(parameters.GetType()).CreateCommand(sql, parameters, connection, CommandType.Text, transaction);
                }
                else
                {
                    result = connection.CreateCommand();
                    result.CommandText = sql;
                    result.CommandType = CommandType.Text;

                    if (transaction != null)
                    {
                        result.Transaction = transaction;
                    }
                }
            }
            catch
            {
                if (result != null)
                {
                    result.Dispose();
                    result = null;
                }

                throw;
            }

            return result;
        }

        /// <summary>
        /// Executes a query agains a database
        /// </summary>
        /// <param name="connection">This sql connection</param>
        /// <param name="sql">The sql string to execute</param>
        /// <returns>The number of affected rows</returns>
        public static int Execute(this IDbConnection connection, string sql)
        {
            return connection.Execute(sql, null, null);
        }

        /// <summary>
        /// Executes a query agains a database
        /// </summary>
        /// <param name="connection">This sql connection</param>
        /// <param name="sql">The sql string to execute</param>
        /// <param name="parameters">An object representing query parameters</param>
        /// <returns>The number of affected rows</returns>
        public static int Execute(this IDbConnection connection, string sql, object parameters)
        {
            return connection.Execute(sql, parameters, null);
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
            using (IDbCommand command = connection.CreateCommand(sql, parameters, transaction))
            {
                return command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Queries the database with the provided sql connection
        /// </summary>
        /// <typeparam name="T">The type of model to return from the query</typeparam>
        /// <param name="connection">A sql connection</param>
        /// <param name="sql">The sql to query with</param>
        /// <returns>A collection of model objects</returns>
        public static IEnumerable<T> Query<T>(this IDbConnection connection, string sql)
        {
            return connection.Query<T>(sql, null, null);
        }

        /// <summary>
        /// Queries the database with the provided sql connection
        /// </summary>
        /// <typeparam name="T">The type of model to return from the query</typeparam>
        /// <param name="connection">A sql connection</param>
        /// <param name="sql">The sql to query with</param>
        /// <returns>A collection of model objects</returns>
        public static IEnumerable<T> Query<T>(this IDbConnection connection, string sql, object parameters)
        {
            return connection.Query<T>(sql, parameters, null);
        }

        /// <summary>
        /// Queries the database with the provided sql connection
        /// </summary>
        /// <typeparam name="T">The type of model to return from the query</typeparam>
        /// <param name="connection">A sql connection</param>
        /// <param name="sql">The sql to query with</param>
        /// <param name="transaction">An optional transaction</param>
        /// <returns>A collection of model objects</returns>
        public static IEnumerable<T> Query<T>(this IDbConnection connection, string sql, object parameters, IDbTransaction transaction)
        {
            using (IDbCommand command = connection.CreateCommand(sql, parameters, transaction))
            {
                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return CheckReflectorCache(typeof(T)).ToObject<T>(reader);
                    }
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
