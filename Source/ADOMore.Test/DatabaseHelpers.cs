namespace ADOMore.Test
{
    using System;
    using System.Data;
#if !MONO
    using System.Data.SQLite;
#endif
    using System.Globalization;
    using System.IO;
#if MONO
    using Mono.Data.Sqlite;
#endif

    internal static class DatabaseHelpers
    {
        public static string CreateConnectionString(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path", "path must contain a value.");
            }

            return string.Format(CultureInfo.InvariantCulture, "data source={0};journal mode=Off;synchronous=Off;version=3", path);
        }

        public static void CreateDatabase(string connectionString)
        {
            const string Schema =
@"CREATE TABLE [Test]
(
    [SetGuid] UNIQUEIDENTIFIER NOT NULL,
    [SetNullGuid] UNIQUEIDENTIFIER,
    [SetBool] BOOLEAN NOT NULL DEFAULT(1),
    [SetNullBool] BOOLEAN,
    [SetString] VARCHAR(50),
    [SetChar] CHAR(1),
    [SetNullChar] CHAR(1),
    [SetInt16] INTEGER,
    [SetInt32] INTEGER,
    [SetNullInt32] INTEGER,
    [SetInt64] INTEGER,
    [SetSingle] FLOAT,
    [SetNullSingle] FLOAT,
    [SetDouble] FLOAT,
    [SetNullDouble] FLOAT,
    [SetDecimal] FLOAT,
    [SetNullDecimal] FLOAT,
    [SetDateTime] DATETIME,
    [SetNullDateTime] DATETIME,
    [SetTestType] INTEGER,
    [SetNullTestType] INTEGER
);";

            using (IDbConnection connection = DatabaseHelpers.OpenConnection(connectionString))
            {
                using (IDbCommand command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = Schema;
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void DestroyDatabase(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException("connectionString", "connectionString must contain a value.");
            }

            string path = null;

#if MONO
            path = new SqliteConnectionStringBuilder(connectionString).DataSource;
#else
            path = new SQLiteConnectionStringBuilder(connectionString).DataSource;
#endif

            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    File.Delete(path);
                }
                catch (FileNotFoundException)
                {
                }
            }
        }

        public static IDbConnection OpenConnection(string connectionString)
        {
            IDbConnection connection = null;

            try
            {
#if MONO
                connection = new SqliteConnection(connectionString);
#else
                connection = new SQLiteConnection(connectionString);
#endif
                connection.Open();
            }
            catch
            {
                if (connection != null)
                {
                    connection.Dispose();
                }

                throw;
            }

            return connection;
        }

        public static string GetRandomPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetRandomFileName().Replace(".", string.Empty) + ".sqlite");
        }
    }
}
