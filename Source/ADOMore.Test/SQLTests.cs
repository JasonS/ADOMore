namespace ADOMore.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Globalization;
    using System.Data;
    using System.IO;
#if MONO
    using Mono.Data.Sqlite;
#endif
    using NUnit.Framework;
#if !MONO
    using System.Data.SQLite;
#endif

    [TestFixture]
    public class SqlTests : IDisposable
    {
        private string connectionString, path;
        private bool disposed;

        ~SqlTests()
        {
            this.Dispose(false);
        }

        [Test]
        public void AnonymousTypes()
        {
            const string Sql =
@"SELECT * 
FROM [Test] 
WHERE 
    [SetGuid] = @Id";

            using (IDbConnection connection = SqlTests.CreateConnection(this.connectionString))
            {
                Guid id = Guid.NewGuid();

                using (IDbCommand command = connection.CreateCommand(Sql, new { Id = id }, null))
                {
                    Assert.AreEqual(1, command.Parameters.Count);
                    
                    IDbDataParameter parameter = command.Parameters[0] as IDbDataParameter;
                    Assert.IsNotNull(parameter);
                    Assert.AreEqual("@Id", parameter.ParameterName);
                    Assert.AreEqual(id, parameter.Value);
                }

                //var command2 = connection.CreateCommand(Sql, new { Id = Guid.NewGuid() }, null);
            }
        }

        [Test]
        public void CreateRecord()
        {
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

            using (IDbConnection connection = SqlTests.CreateConnection(this.connectionString))
            {
                connection.Open();
                Assert.IsTrue(CreateRecord(instance, connection) == 1);
                AssertGetTestInstanceFromDatabase(instance, connection);
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        [SetUp]
        public void Setup()
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

            this.path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetRandomFileName().Replace(".", string.Empty) + ".sqlite");
            this.connectionString = string.Format(CultureInfo.InvariantCulture, "data source={0};journal mode=Off;synchronous=Off;version=3", this.path);

            using (IDbConnection connection = SqlTests.CreateConnection(this.connectionString))
            {
                connection.Open();

                using (IDbCommand command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = Schema;
                    command.ExecuteNonQuery();
                }
            }
        }

        [Test]
        public void TableExists()
        {
            const string Sql =
@"SELECT COUNT(*) 
FROM [sqlite_master] 
WHERE 
    [type] = 'table'
    AND [name] = 'Test'";

            using (IDbConnection connection = SqlTests.CreateConnection(this.connectionString))
            {
                connection.Open();
                
                using (IDbCommand command = connection.CreateCommand())
                {
                    command.CommandText = Sql;
                    command.CommandType = CommandType.Text;
                    Assert.AreEqual(1, Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture));
                }
            }
        }

        [TearDown]
        public void Teardown()
        {
            if (!string.IsNullOrEmpty(this.path))
            {
                try
                {
                    File.Delete(this.path);
                }
                catch (FileNotFoundException)
                {
                }
            }

            this.connectionString = null;
            this.path = null;
        }

        private static IDbConnection CreateConnection(string connectionString)
        {
#if MONO
            return new SqliteConnection(connectionString);
#else
            return new SQLiteConnection(connectionString);
#endif
        }

        private static int CreateRecord(TestClass instance, IDbConnection connection)
        {
            const string Sql =
@"INSERT INTO [Test]
(
    [SetGuid],
    [SetNullGuid],
    [SetBool],
    [SetNullBool],
    [SetString],
    [SetChar],
    [SetNullChar],
    [SetInt16],
    [SetInt32],
    [SetNullInt32],
    [SetInt64],
    [SetSingle],
    [SetNullSingle],
    [SetDouble],
    [SetNullDouble],
    [SetDecimal],
    [SetNullDecimal],
    [SetDateTime],
    [SetNullDateTime],
    [SetTestType],
    [SetNullTestType]
)
VALUES
(
    @SetGuid,
    @SetNullGuid,
    @SetBool,
    @SetNullBool,
    @SetString,
    @SetChar,
    @SetNullChar,
    @SetInt16,
    @SetInt32,
    @SetNullInt32,
    @SetInt64,
    @SetSingle,
    @SetNullSingle,
    @SetDouble,
    @SetNullDouble,
    @SetDecimal,
    @SetNullDecimal,
    @SetDateTime,
    @SetNullDateTime,
    @SetTestType,
    @SetNullTestType
);";
            return connection.Execute(Sql, instance, null);
        }

        private void AssertGetTestInstanceFromDatabase(TestClass instance, IDbConnection connection)
        {
            const string Sql =
@"SELECT * 
FROM [Test] 
WHERE 
    [SetGuid] = @SetGuid";

            
            TestClass fetch = connection.Query<TestClass>(Sql, instance, null).FirstOrDefault();
            Assert.IsNotNull(fetch);
            Assert.AreEqual(instance.SetBool, fetch.SetBool);
            Assert.AreEqual(instance.SetChar, fetch.SetChar);
            Assert.AreEqual(instance.SetDateTime, fetch.SetDateTime);
            Assert.AreEqual(instance.SetDecimal, fetch.SetDecimal);
            Assert.AreEqual(instance.SetDouble, fetch.SetDouble);
            Assert.AreEqual(instance.SetGuid, fetch.SetGuid);
            Assert.AreEqual(instance.SetInt16, fetch.SetInt16);
            Assert.AreEqual(instance.SetInt32, fetch.SetInt32);
            Assert.AreEqual(instance.SetInt64, fetch.SetInt64);
            Assert.AreEqual(instance.SetNullBool, fetch.SetNullBool);
            Assert.AreEqual(instance.SetNullChar, fetch.SetNullChar);
            Assert.AreEqual(instance.SetNullDateTime, fetch.SetNullDateTime);
            Assert.AreEqual(instance.SetNullDecimal, fetch.SetNullDecimal);
            Assert.AreEqual(instance.SetNullDouble, fetch.SetNullDouble);
            Assert.AreEqual(instance.SetNullGuid, fetch.SetNullGuid);
            Assert.AreEqual(instance.SetNullInt32, fetch.SetNullInt32);
            Assert.AreEqual(instance.SetNullSingle, fetch.SetNullSingle);
            Assert.AreEqual(instance.SetNullTestType, fetch.SetNullTestType);
            Assert.AreEqual(instance.SetTestType, fetch.SetTestType);
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.Teardown();
                }

                this.disposed = true;
            }
        }
    }
}