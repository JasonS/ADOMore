namespace ADOMore.Test
{
    using System;
    using System.Data;
#if !MONO
    using System.Data.SQLite;
#endif
    using System.Linq;
#if MONO
    using Mono.Data.Sqlite;
#endif
    using NUnit.Framework;

    [TestFixture]
    public sealed class DataExtensionsTests : IDisposable
    {
        private string connectionString, path;
        private bool disposed;

        ~DataExtensionsTests()
        {
            this.Dispose(false);
        }

        [Test]
        public void DataExtensionsCreateCommand()
        {
            const string Sql = @"SELECT * FROM [Test];";

            using (IDbConnection connection = DatabaseHelpers.OpenConnection(this.connectionString))
            {
                using (IDbCommand command = connection.CreateCommand(Sql))
                {
                    Assert.IsNotNull(command);
                    Assert.AreEqual(Sql, command.CommandText);
                    Assert.AreEqual(0, command.Parameters.Count);
                    Assert.IsNull(command.Transaction);
                }

                Guid id = Guid.NewGuid();

                using (IDbCommand command = connection.CreateCommand(Sql, new TestClass() { SetGuid = id }))
                {
                    Assert.IsNotNull(command);
                    Assert.IsTrue(0 < command.Parameters.Count);

                    IDbDataParameter p = command.Parameters["@SetGuid"] as IDbDataParameter;
                    Assert.IsNotNull(p);
                    Assert.AreEqual(id, p.Value);
                }

                using (IDbCommand command = connection.CreateCommand(Sql, new { Id = id }))
                {
                    Assert.IsNotNull(command);
                    Assert.IsTrue(0 < command.Parameters.Count);

                    IDbDataParameter p = command.Parameters["@Id"] as IDbDataParameter;
                    Assert.IsNotNull(p);
                    Assert.AreEqual(id, p.Value);
                }

                using (IDbTransaction transaction = connection.BeginTransaction())
                {
                    using (IDbCommand command = connection.CreateCommand(Sql, null, transaction))
                    {
                        Assert.IsNotNull(command.Transaction);
                    }
                }
            }
        }

        [Test]
        public void DataExtensionsExecute()
        {
            const string Sql =
@"INSERT INTO [Test]([SetGuid])
VALUES(@SetGuid);";

            using (IDbConnection connection = DatabaseHelpers.OpenConnection(this.connectionString))
            {
                Assert.AreEqual(0, connection.Execute(";"));
                Assert.AreEqual(1, connection.Execute(Sql, new TestClass() { SetGuid = Guid.NewGuid() }));
                Assert.AreEqual(1, connection.Execute(Sql, new { SetGuid = Guid.NewGuid() }));
            }
        }

        [Test]
        public void DataSetExtensionsQuery()
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

            using (IDbConnection connection = DatabaseHelpers.OpenConnection(this.connectionString))
            {
                connection.Execute(Sql, instance);
                Assert.IsTrue(connection.Query<TestClass>("SELECT * FROM [Test];").Any());

                TestClass fetched = connection.Query<TestClass>("SELECT * FROM [Test];").First();
                Assert.AreEqual(instance.SetBool, fetched.SetBool);
                Assert.AreEqual(instance.SetChar, fetched.SetChar);
                Assert.AreEqual(instance.SetDateTime, fetched.SetDateTime);
                Assert.AreEqual(instance.SetDecimal, fetched.SetDecimal);
                Assert.AreEqual(instance.SetDouble, fetched.SetDouble);
                Assert.AreEqual(instance.SetGuid, fetched.SetGuid);
                Assert.AreEqual(instance.SetInt16, fetched.SetInt16);
                Assert.AreEqual(instance.SetInt32, fetched.SetInt32);
                Assert.AreEqual(instance.SetInt64, fetched.SetInt64);
                Assert.AreEqual(instance.SetNullBool, fetched.SetNullBool);
                Assert.AreEqual(instance.SetNullChar, fetched.SetNullChar);
                Assert.AreEqual(instance.SetNullDateTime, fetched.SetNullDateTime);
                Assert.AreEqual(instance.SetNullDecimal, fetched.SetNullDecimal);
                Assert.AreEqual(instance.SetNullDouble, fetched.SetNullDouble);
                Assert.AreEqual(instance.SetNullGuid, fetched.SetNullGuid);
                Assert.AreEqual(instance.SetNullInt32, fetched.SetNullInt32);
                Assert.AreEqual(instance.SetNullSingle, fetched.SetNullSingle);
                Assert.AreEqual(instance.SetNullTestType, fetched.SetNullTestType);
                Assert.AreEqual(instance.SetTestType, fetched.SetTestType);
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
            this.path = DatabaseHelpers.GetRandomPath();
            this.connectionString = DatabaseHelpers.CreateConnectionString(this.path);
            DatabaseHelpers.CreateDatabase(this.connectionString);
        }

        [TearDown]
        public void Teardown()
        {
            if (!string.IsNullOrEmpty(this.connectionString))
            {
                DatabaseHelpers.DestroyDatabase(this.connectionString);
            }

            this.connectionString = null;
            this.path = null;
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