namespace ADOMore.Test
{
    using System;
    using System.Globalization;
    using System.Data;
    using System.Data.SqlClient;
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SQLTests
    {
        private static string connectionString;

        [TestMethod]
        public void CanCreateRecord()
        {
            EnsureConnectionString();

            TestClass instance = new TestClass()
            {
                SetGuid = Guid.NewGuid(),
                SetNullGuid = Guid.NewGuid(),
                SetBool = true,
                SetNullBool = true,
                SetChar = 's',
                SetDateTime = DateTime.Now.Date,
                SetNullDateTime = DateTime.Now.Date,
                SetDecimal = 6.0M,
                SetNullDecimal = 6.0M,
                SetDouble = 6.0,
                SetNullDouble = 6.0,
                SetInt16 = 6,
                SetInt32 = 6,
                SetInt64 = 6,
                SetNullChar = 's',
                SetString = "sukut",
                SetNullInt32 = 6,
                SetTestType = TestType.Four,
                SetSingle = (Single)6.0,
                SetNullSingle = (Single)6.0,
                SetNullTestType = TestType.Four | TestType.Eight
            };

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                CreateRecord(instance, connection);
                AssertGetTestInstanceFromDatabase(instance, connection);
            }
        }

        [TestMethod]
        public void TestTableExists()
        {
            int rowCount = 0;
            SqlDataReader reader = null;
            EnsureConnectionString();
            const string Sql =
@"SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Test]') AND type = N'U'";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = Sql;
                        command.CommandType = CommandType.Text;
                        reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            rowCount++;
                        }

                        Assert.IsTrue(rowCount > 0);
                    }
                }
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                    reader = null;
                }
            }
        }

        private static void CreateRecord(TestClass instance, SqlConnection connection)
        {
            Stream stream = null;
            IDbCommand command = null;
            Reflector<TestClass> repo = new Reflector<TestClass>();
            
            try
            {
                stream = typeof(SQLTests).Assembly.GetManifestResourceStream("ADOMore.Test.Insert.sql");

                using (StreamReader reader = new StreamReader(stream))
                {
                    command = repo.CreateCommand(reader.ReadToEnd(), instance, connection);
                    command.ExecuteNonQuery();
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

        [AssemblyInitialize]
        public static void Bootstrap(TestContext testContext)
        {
            EnsureConnectionString();
            Stream stream = null;
            SqlCommand command = null;
            string createSql = string.Empty;
            string filePath = string.Empty;
            SqlConnectionStringBuilder masterConn = new SqlConnectionStringBuilder(connectionString);

            filePath = masterConn.AttachDBFilename;
            masterConn.AttachDBFilename = string.Empty;
            masterConn.InitialCatalog = "master";
            createSql = string.Format(CultureInfo.InvariantCulture, "CREATE DATABASE [Test] ON (NAME=N'Test', FILENAME='{0}')", filePath);

            try
            {
                using (SqlConnection connection = new SqlConnection(masterConn.ConnectionString))
                {
                    connection.Open();
                    Destroy(connection);
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
                using (SqlConnection connection = new SqlConnection(connectionString))
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

        [AssemblyCleanup]
        public static void Destroy()
        {
            EnsureConnectionString();
            SqlConnectionStringBuilder masterConn = new SqlConnectionStringBuilder(connectionString);
            masterConn.AttachDBFilename = string.Empty;
            masterConn.InitialCatalog = "master";

            using (SqlConnection connection = new SqlConnection(masterConn.ConnectionString))
            {
                connection.Open();
                Destroy(connection);
            }
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

            using(SqlCommand command = connection.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText = DropSql;
                command.ExecuteNonQuery();
            }
        }

        private static void EnsureConnectionString()
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                var path = ResolvePath((Path.GetRandomFileName().Replace(".", string.Empty) + ".mdf"));
                connectionString = string.Format(CultureInfo.InvariantCulture, @"Server=(localdb)\v11.0;AttachDbFileName={0};Initial Catalog=Test;Integrated Security=true", path);
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

        private void AssertGetTestInstanceFromDatabase(TestClass instance, SqlConnection connection)
        {
            string Sql = string.Format("SELECT * FROM [Test] WHERE [SetGuid] = '{0}'", instance.SetGuid);
            IDbCommand command = null;
            IDataReader reader = null;
            IDataRecord record = null;
            TestClass fetch = null;
            bool success = false;

            try
            {
                Reflector<TestClass> repo = new Reflector<TestClass>();
                command = repo.CreateCommand(Sql, instance, connection);
                reader = command.ExecuteReader();
                reader.Read();
                record = (IDataRecord)reader;
                fetch = repo.ToModel(record);
                success =
                    fetch.SetBool = instance.SetBool &&
                    fetch.SetChar == instance.SetChar &&
                    fetch.SetDateTime == instance.SetDateTime &&
                    fetch.SetDecimal == instance.SetDecimal &&
                    fetch.SetDouble == instance.SetDouble &&
                    fetch.SetGuid == instance.SetGuid &&
                    fetch.SetInt16 == instance.SetInt16 &&
                    fetch.SetInt32 == instance.SetInt32 &&
                    fetch.SetInt64 == instance.SetInt64 &&
                    fetch.SetNullBool == instance.SetNullBool &&
                    fetch.SetNullChar == instance.SetNullChar &&
                    fetch.SetNullDateTime == instance.SetNullDateTime &&
                    fetch.SetNullDecimal == instance.SetNullDecimal &&
                    fetch.SetNullDouble == instance.SetNullDouble &&
                    fetch.SetNullGuid == instance.SetNullGuid &&
                    fetch.SetNullSingle == instance.SetNullSingle &&
                    fetch.SetString == instance.SetString &&
                    fetch.SetTestType == instance.SetTestType &&
                    fetch.SetNullTestType == instance.SetNullTestType;

                Assert.IsTrue(success);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Dispose();
                    reader = null;
                }
                if (command != null)
                {
                    command.Dispose();
                    command = null;
                }
            }

        }
    }
}
