namespace ADOMore.Test
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Globalization;
    using System.IO;
    using NUnit.Framework;

    [SetUpFixture]
    public sealed class Setup
    {
        private static readonly string CS = Setup.CreateConnectionString();

        public static string ConnectionString
        {
            get { return Setup.CS; }
        }

        [SetUp]
        public void Initialize()
        {
            Stream stream = null;
            SqlCommand command = null;
            string createSql = string.Empty;
            string filePath = string.Empty;
            SqlConnectionStringBuilder masterConn = new SqlConnectionStringBuilder(Setup.ConnectionString);

            filePath = masterConn.AttachDBFilename;
            masterConn.AttachDBFilename = string.Empty;
            masterConn.InitialCatalog = "master";
            createSql = string.Format(CultureInfo.InvariantCulture, "CREATE DATABASE [Test] ON (NAME=N'Test', FILENAME='{0}')", filePath);

            try
            {
                using (SqlConnection connection = new SqlConnection(masterConn.ConnectionString))
                {
                    connection.Open();
                    Setup.Destroy(connection);
                    command = connection.CreateCommand();
                    command.CommandType = CommandType.Text;
                    command.CommandText = createSql;
                    command.ExecuteNonQuery(); ;
                }
            }
            finally
            {
                if (command != null)
                {
                    command = null;
                }
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(Setup.ConnectionString))
                {
                    connection.Open();
                    stream = typeof(SQLTests).Assembly.GetManifestResourceStream("ADOMore.Test.Schema.sql");

                    using (StreamReader reader = new StreamReader(stream))
                    {
                        command = connection.CreateCommand();
                        command.CommandType = CommandType.Text;
                        command.CommandText = reader.ReadToEnd();
                        command.ExecuteNonQuery();
                    }
                }
            }
            finally
            {
                if (stream != null)
                {
                    stream.Dispose();
                    stream = null;
                }
                if (command != null)
                {
                    command.Dispose();
                    command = null;
                }
            }
        }

        [TearDown]
        public void Teardown()
        {
            SqlConnectionStringBuilder masterConn = new SqlConnectionStringBuilder(Setup.ConnectionString);
            masterConn.AttachDBFilename = string.Empty;
            masterConn.InitialCatalog = "master";

            using (SqlConnection connection = new SqlConnection(masterConn.ConnectionString))
            {
                connection.Open();
                Setup.Destroy(connection);
            }
        }

        private static string CreateConnectionString()
        {
            var path = Setup.ResolvePath((Path.GetRandomFileName().Replace(".", string.Empty) + ".mdf"));
            return string.Format(CultureInfo.InvariantCulture, @"Server=(localdb)\v11.0;AttachDbFileName={0};Initial Catalog=Test;Integrated Security=true", path);
        }

        private static void Destroy(SqlConnection connection)
        {
            const string DropSql =
@"IF EXISTS (SELECT * FROM sys.databases WHERE [name] = 'Test')
BEGIN
    BEGIN TRY
        ALTER DATABASE [Test] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    END TRY
    BEGIN CATCH
    END CATCH
    DROP DATABASE [Test];
END";

            using (SqlCommand command = connection.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText = DropSql;
                command.ExecuteNonQuery();
            }
        }

        private static string ResolvePath(string path)
        {
            const string DataDirectory = "|DataDirectory|";

            if (string.IsNullOrWhiteSpace(path))
            {
                path = AppDomain.CurrentDomain.BaseDirectory;
            }

            if (path.StartsWith(DataDirectory, StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(DataDirectory.Length);
                path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", path);
            }

            if (!Path.IsPathRooted(path))
            {
                path = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path));
            }

            if (!path.StartsWith(AppDomain.CurrentDomain.BaseDirectory, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("path must be within the current application directory.", "path");
            }

            return path;
        }
    }
}