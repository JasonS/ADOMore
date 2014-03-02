namespace ADOMore
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Provides extensions for parameterizing <see cref="IDbCommands"/> and
    /// materializing objects from query results.
    /// </summary>
    public static class DataExtensions
    {
        private static readonly object SyncRoot = new object();
        private static readonly HybridDictionary ReflectorCacheInstance = new HybridDictionary();
        private static readonly IValueProvider[] ValueProviders = new[] { new ValueProvider() };

        internal static HybridDictionary ReflectorCache
        {
            get { return ReflectorCacheInstance; }
        }

        /// <summary>
        /// Creates a parameterized <see cref="IDbCommand"/>.
        /// </summary>
        /// <param name="connection">The <see cref="IDbConnection"/> to create the command for.</param>
        /// <param name="sql">A SQL string.</param>
        /// <returns>The created <see cref="IDbCommand"/>.</returns>
        public static IDbCommand CreateCommand(this IDbConnection connection, string sql)
        {
            return connection.CreateCommand(sql, null, null);
        }

        /// <summary>
        /// Creates a parameterized <see cref="IDbCommand"/>.
        /// </summary>
        /// <param name="connection">The <see cref="IDbConnection"/> to create the command for.</param>
        /// <param name="sql">A SQL string.</param>
        /// <param name="parameters">An object providing parameters to the command.</param>
        /// <returns>The created <see cref="IDbCommand"/>.</returns>
        public static IDbCommand CreateCommand(this IDbConnection connection, string sql, object parameters)
        {
            return connection.CreateCommand(sql, parameters, null);
        }

        /// <summary>
        /// Creates a parameterized <see cref="IDbCommand"/>.
        /// </summary>
        /// <param name="connection">The <see cref="IDbConnection"/> to create the command for.</param>
        /// <param name="sql">A SQL string.</param>
        /// <param name="parameters">An object providing parameters to the command.</param>
        /// <param name="transaction">The <see cref="IDbTransaction"/> to use.</param>
        /// <returns>The created <see cref="IDbCommand"/>.</returns>
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Reviewed.")]
        public static IDbCommand CreateCommand(this IDbConnection connection, string sql, object parameters, IDbTransaction transaction)
        {
            IDbCommand result = null;

            try
            {
                if (parameters != null)
                {
                    result = GetReflector(parameters.GetType()).CreateCommand(sql, parameters, connection, CommandType.Text, transaction);
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
        /// Executes a SQL command.
        /// </summary>
        /// <param name="connection">The <see cref="IDbConnection"/> to execute the command with.</param>
        /// <param name="sql">A SQL string.</param>
        /// <param name="parameters">An object providing parameters to the command.</param>
        /// <param name="transaction">The <see cref="IDbTransaction"/> to use.</param>
        /// <returns>The number of affected rows.</returns>
        public static int Execute(this IDbConnection connection, string sql, object parameters = null, IDbTransaction transaction = null)
        {
            if (parameters.IsCollection())
            {
                int result = 0;

                foreach (object p in (IEnumerable)parameters)
                {
                    result += connection.ExecuteImpl(sql, p, transaction);
                }

                return result;
            }
            else
            {
                return connection.ExecuteImpl(sql, parameters, transaction);
            }
        }

        /// <summary>
        /// Executes a SQL query.
        /// </summary>
        /// <typeparam name="T">The type of objects to return from the query.</typeparam>
        /// <param name="connection">The <see cref="IDbConnection"/> to execute the query with.</param>
        /// <param name="sql">A SQL string.</param>
        /// <param name="parameters">An object providing parameters to the command.</param>
        /// <param name="transaction">The <see cref="IDbTransaction"/> to use.</param>
        /// <param name="iterator">A function invoked for each row returned from the query.</param>
        /// <returns>A collection representing the results of the query.</returns>
        public static IEnumerable<T> Query<T>(this IDbConnection connection, string sql, object parameters = null, IDbTransaction transaction = null, Action<object, T> iterator = null)
        {
            if (parameters.IsCollection())
            {
                List<T> result = new List<T>();

                foreach (object p in (IEnumerable)parameters)
                {
                    result.AddRange(connection.QueryImpl<T>(sql, p, transaction, iterator));
                }

                return result;
            }
            else
            {
                return connection.QueryImpl<T>(sql, parameters, transaction, iterator);
            }
        }

        /// <summary>
        /// Reads a strongly-typed collection from the given <see cref="IDataReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="IDataReader"/> to read from.</param>
        /// <typeparam name="T">The type of objects to read.</typeparam>
        public static IEnumerable<T> Read<T>(this IDataReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader", "reader cannot be null.");
            }
            
            List<T> results = new List<T>();
            Reflector reflector = GetReflector(typeof(T));
            
            while (reader.Read())
            {
                results.Add(reflector.ToObject<T>(reader));
            }
            
            return results;
        }

        /// <summary>
        /// Creates an object instance from the provided <see cref="IDataRecord"/>.
        /// </summary>
        /// <typeparam name="T">The type of the object to create.</typeparam>
        /// <param name="dataRecord">The data record to create the object from.</param>
        /// <returns>The created object.</returns>
        public static T ToObject<T>(this IDataRecord dataRecord)
        {
            return GetReflector(typeof(T)).ToObject<T>(dataRecord);
        }

        internal static int ExecuteImpl(this IDbConnection connection, string sql, object parameters, IDbTransaction transaction)
        {
            using (IDbCommand command = connection.CreateCommand(sql, parameters, transaction))
            {
                return command.ExecuteNonQuery();
            }
        }

        internal static Reflector GetReflector(Type type)
        {
            lock (DataExtensions.ReflectorCacheInstance)
            {
                Reflector result = ReflectorCacheInstance[type] as Reflector;
    
                if (result == null)
                {
                    result = new Reflector(type, DataExtensions.ValueProviders);
                    ReflectorCacheInstance[type] = result;
                }
    
                return result;
            }
        }

        internal static bool IsCollection(this object value)
        {
            if (value != null)
            {
                Type type = value.GetType();

                if (type.IsArray
                    || (typeof(IEnumerable).IsAssignableFrom(type)
                    && Type.GetTypeCode(type) != TypeCode.String))
                {
                    return true;
                }
            }

            return false;
        }

        internal static IEnumerable<T> QueryImpl<T>(this IDbConnection connection, string sql, object parameters, IDbTransaction transaction, Action<object, T> iterator)
        {
            using (IDbCommand command = connection.CreateCommand(sql, parameters, transaction))
            {
                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        T result = GetReflector(typeof(T)).ToObject<T>(reader);

                        if (iterator != null)
                        {
                            iterator(parameters, result);
                        }

                        yield return result;
                    }
                }
            }
        }
    }
}