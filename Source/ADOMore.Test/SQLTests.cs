namespace ADOMore.Test
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Data;
    using System.Data.SqlClient;
    using System.IO;
    using NUnit.Framework;

    [TestFixture]
    public class SQLTests
    {
        [Test]
        public void CanCreateRecord()
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

            using (SqlConnection connection = new SqlConnection(Setup.ConnectionString))
            {
                connection.Open();
                CreateRecord(instance, connection);
                AssertGetTestInstanceFromDatabase(instance, connection);
            }
        }

        [Test]
        public void TestDicitionaryExtenstion()
        {
            using (SqlConnection connection = new SqlConnection(Setup.ConnectionString))
            {
                connection.Open();

                using (IDbCommand command = connection.CreateCommand(new Dictionary<string, object> { { "@Id", Guid.NewGuid() } }, "SELECT * FROM [Test] WHERE [Id] = @Id", null))
                {
                    
                }
            }
        }

        [Test]
        public void TestTableExists()
        {
            int rowCount = 0;
            SqlDataReader reader = null;
            
            const string Sql =
@"SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Test]') AND type = N'U'";

            try
            {
                using (SqlConnection connection = new SqlConnection(Setup.ConnectionString))
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
