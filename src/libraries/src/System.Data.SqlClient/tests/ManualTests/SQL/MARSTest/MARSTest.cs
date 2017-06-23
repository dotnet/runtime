// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class MARSTest
    {
        private static readonly string _connStr = (new SqlConnectionStringBuilder(DataTestUtility.TcpConnStr) { MultipleActiveResultSets = true }).ConnectionString;

        [CheckConnStrSetupFact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void NamedPipesMARSTest()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(DataTestUtility.NpConnStr);
            builder.MultipleActiveResultSets = true;
            builder.ConnectTimeout = 5;

            using (SqlConnection conn = new SqlConnection(builder.ConnectionString))
            {
                conn.Open();
                using (SqlCommand command = new SqlCommand("SELECT @@SERVERNAME", conn))
                {
                    var result = command.ExecuteScalar();
                    Assert.NotNull(result);
                }
            }
        }

#if DEBUG
        [CheckConnStrSetupFact]
        public static void MARSAsyncTimeoutTest()
        {
            using (SqlConnection connection = new SqlConnection(_connStr))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("WAITFOR DELAY '01:00:00';SELECT 1", connection);
                command.CommandTimeout = 1;
                Task<object> result = command.ExecuteScalarAsync();

                Assert.True(((IAsyncResult)result).AsyncWaitHandle.WaitOne(30 * 1000), "Expected timeout after one second, but no results after 30 seconds");
                Assert.True(result.IsFaulted, string.Format("Expected task result to be faulted, but instead it was {0}", result.Status));
                Assert.True(connection.State == ConnectionState.Open, string.Format("Expected connection to be open after soft timeout, but it was {0}", connection.State));

                Type type = typeof(SqlDataReader).GetTypeInfo().Assembly.GetType("System.Data.SqlClient.TdsParserStateObject");
                FieldInfo field = type.GetField("_skipSendAttention", BindingFlags.NonPublic | BindingFlags.Static);

                Assert.True(field != null, "Error: This test cannot succeed on retail builds because it uses the _skipSendAttention test hook");

                field.SetValue(null, true);
                try
                {
                    SqlCommand command2 = new SqlCommand("WAITFOR DELAY '01:00:00';SELECT 1", connection);
                    command2.CommandTimeout = 1;
                    result = command2.ExecuteScalarAsync();

                    Assert.True(((IAsyncResult)result).AsyncWaitHandle.WaitOne(30 * 1000), "Expected timeout after six or so seconds, but no results after 30 seconds");
                    Assert.True(result.IsFaulted, string.Format("Expected task result to be faulted, but instead it was {0}", result.Status));

                    // Pause here to ensure that the async closing is completed
                    Thread.Sleep(200);
                    Assert.True(connection.State == ConnectionState.Closed, string.Format("Expected connection to be closed after hard timeout, but it was {0}", connection.State));
                }
                finally
                {
                    field.SetValue(null, false);
                }
            }
        }

        [CheckConnStrSetupFact]
        public static void MARSSyncTimeoutTest()
        {
            using (SqlConnection connection = new SqlConnection(_connStr))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("WAITFOR DELAY '01:00:00';SELECT 1", connection);
                command.CommandTimeout = 1;
                bool hitException = false;
                try
                {
                    object result = command.ExecuteScalar();
                }
                catch (Exception e)
                {
                    Assert.True(e is SqlException, "Expected SqlException but found " + e);
                    hitException = true;
                }
                Assert.True(hitException, "Expected a timeout exception but ExecutScalar succeeded");

                Assert.True(connection.State == ConnectionState.Open, string.Format("Expected connection to be open after soft timeout, but it was {0}", connection.State));

                Type type = typeof(SqlDataReader).GetTypeInfo().Assembly.GetType("System.Data.SqlClient.TdsParserStateObject");
                FieldInfo field = type.GetField("_skipSendAttention", BindingFlags.NonPublic | BindingFlags.Static);

                Assert.True(field != null, "Error: This test cannot succeed on retail builds because it uses the _skipSendAttention test hook");

                field.SetValue(null, true);
                hitException = false;
                try
                {
                    SqlCommand command2 = new SqlCommand("WAITFOR DELAY '01:00:00';SELECT 1", connection);
                    command2.CommandTimeout = 1;
                    try
                    {
                        object result = command2.ExecuteScalar();
                    }
                    catch (Exception e)
                    {
                        Assert.True(e is SqlException, "Expected SqlException but found " + e);
                        hitException = true;
                    }
                    Assert.True(hitException, "Expected a timeout exception but ExecutScalar succeeded");

                    Assert.True(connection.State == ConnectionState.Closed, string.Format("Expected connection to be closed after hard timeout, but it was {0}", connection.State));
                }
                finally
                {
                    field.SetValue(null, false);
                }
            }
        }
#endif

        [CheckConnStrSetupFact]
        public static void MARSSyncBusyReaderTest()
        {
            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                conn.Open();

                using (SqlDataReader reader1 = (new SqlCommand("select * from Orders", conn)).ExecuteReader())
                {
                    int rows1 = 0;
                    while (reader1.Read())
                    {
                        rows1++;
                        if (rows1 == 415)
                            break;
                    }
                    Assert.True(rows1 == 415, "MARSSyncBusyReaderTest Failure, #1");

                    using (SqlDataReader reader2 = (new SqlCommand("select * from Orders", conn)).ExecuteReader())
                    {
                        int rows2 = 0;
                        while (reader2.Read())
                        {
                            rows2++;
                            if (rows2 == 415)
                                break;
                        }
                        Assert.True(rows2 == 415, "MARSSyncBusyReaderTest Failure, #2");

                        for (int i = 415; i < 830; i++)
                        {
                            Assert.True(reader1.Read() && reader2.Read(), "MARSSyncBusyReaderTest Failure #3");
                            Assert.True(reader1.GetInt32(0) == reader2.GetInt32(0),
                                        "MARSSyncBusyReaderTest, Failure #4" + "\n" +
                                        "reader1.GetInt32(0): " + reader1.GetInt32(0) + "\n" +
                                        "reader2.GetInt32(0): " + reader2.GetInt32(0));
                        }

                        Assert.False(reader1.Read() || reader2.Read(), "MARSSyncBusyReaderTest, Failure #5");
                    }
                }
            }
        }

        [CheckConnStrSetupFact]
        public static void MARSSyncExecuteNonQueryTest()
        {
            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                conn.Open();

                using (SqlCommand comm1 = new SqlCommand("select * from Orders", conn))
                using (SqlCommand comm2 = new SqlCommand("select * from Orders", conn))
                using (SqlCommand comm3 = new SqlCommand("select * from Orders", conn))
                using (SqlCommand comm4 = new SqlCommand("select * from Orders", conn))
                using (SqlCommand comm5 = new SqlCommand("select * from Orders", conn))
                {
                    comm1.ExecuteNonQuery();
                    comm2.ExecuteNonQuery();
                    comm3.ExecuteNonQuery();
                    comm4.ExecuteNonQuery();
                    comm5.ExecuteNonQuery();
                }
            }
        }

        [CheckConnStrSetupFact]
        public static void MARSSyncExecuteReaderTest1()
        {
            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                conn.Open();

                using (SqlDataReader reader1 = (new SqlCommand("select * from Orders", conn)).ExecuteReader())
                using (SqlDataReader reader2 = (new SqlCommand("select * from Orders", conn)).ExecuteReader())
                using (SqlDataReader reader3 = (new SqlCommand("select * from Orders", conn)).ExecuteReader())
                using (SqlDataReader reader4 = (new SqlCommand("select * from Orders", conn)).ExecuteReader())
                using (SqlDataReader reader5 = (new SqlCommand("select * from Orders", conn)).ExecuteReader())
                {
                    int rows = 0;
                    while (reader1.Read())
                    {
                        rows++;
                    }
                    Assert.True(rows == 830, "MARSSyncExecuteReaderTest1 failure, #1");

                    rows = 0;
                    while (reader2.Read())
                    {
                        rows++;
                    }
                    Assert.True(rows == 830, "MARSSyncExecuteReaderTest1 failure, #2");

                    rows = 0;
                    while (reader3.Read())
                    {
                        rows++;
                    }
                    Assert.True(rows == 830, "MARSSyncExecuteReaderTest1 failure, #3");

                    rows = 0;
                    while (reader4.Read())
                    {
                        rows++;
                    }
                    Assert.True(rows == 830, "MARSSyncExecuteReaderTest1 failure, #4");

                    rows = 0;
                    while (reader5.Read())
                    {
                        rows++;
                    }
                    Assert.True(rows == 830, "MARSSyncExecuteReaderTest1 failure, #5");
                }
            }
        }


        [CheckConnStrSetupFact]
        public static void MARSSyncExecuteReaderTest2()
        {
            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                conn.Open();

                using (SqlDataReader reader1 = (new SqlCommand("select * from Orders", conn)).ExecuteReader())
                using (SqlDataReader reader2 = (new SqlCommand("select * from Orders", conn)).ExecuteReader())
                using (SqlDataReader reader3 = (new SqlCommand("select * from Orders", conn)).ExecuteReader())
                using (SqlDataReader reader4 = (new SqlCommand("select * from Orders", conn)).ExecuteReader())
                using (SqlDataReader reader5 = (new SqlCommand("select * from Orders", conn)).ExecuteReader())
                {
                    for (int i = 0; i < 830; i++)
                    {
                        Assert.True(reader1.Read() && reader2.Read() && reader3.Read() && reader4.Read() && reader5.Read(), "MARSSyncExecuteReaderTest2 Failure #1");
                    }

                    Assert.False(reader1.Read() || reader2.Read() || reader3.Read() || reader4.Read() || reader5.Read(), "MARSSyncExecuteReaderTest2 Failure #2");
                }
            }
        }

        [CheckConnStrSetupFact]
        public static void MARSSyncExecuteReaderTest3()
        {
            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                conn.Open();

                using (SqlDataReader reader1 = (new SqlCommand("select * from Orders", conn)).ExecuteReader())
                using (SqlDataReader reader2 = (new SqlCommand("select * from Orders", conn)).ExecuteReader())
                using (SqlDataReader reader3 = (new SqlCommand("select * from Orders", conn)).ExecuteReader())
                using (SqlDataReader reader4 = (new SqlCommand("select * from Orders", conn)).ExecuteReader())
                using (SqlDataReader reader5 = (new SqlCommand("select * from Orders", conn)).ExecuteReader())
                {
                    for (int i = 0; i < 830; i++)
                    {
                        Assert.True(reader1.Read() && reader2.Read() && reader3.Read() && reader4.Read() && reader5.Read(), "MARSSyncExecuteReaderTest3 Failure #1");

                        // All reads succeeded - check values.
                        Assert.True(reader1.GetInt32(0) == reader2.GetInt32(0) &&
                                    reader2.GetInt32(0) == reader3.GetInt32(0) &&
                                    reader3.GetInt32(0) == reader4.GetInt32(0) &&
                                    reader4.GetInt32(0) == reader5.GetInt32(0),
                                    "MARSSyncExecuteReaderTest3, Failure #2" + "\n" +
                                    "reader1.GetInt32(0): " + reader1.GetInt32(0) + "\n" +
                                    "reader2.GetInt32(0): " + reader2.GetInt32(0) + "\n" +
                                    "reader3.GetInt32(0): " + reader3.GetInt32(0) + "\n" +
                                    "reader4.GetInt32(0): " + reader4.GetInt32(0) + "\n" +
                                    "reader5.GetInt32(0): " + reader5.GetInt32(0));
                    }

                    Assert.False(reader1.Read() || reader2.Read() || reader3.Read() || reader4.Read() || reader5.Read(), "MARSSyncExecuteReaderTest3 Failure #3");
                }
            }
        }

        [CheckConnStrSetupFact]
        public static void MARSSyncExecuteReaderTest4()
        {
            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                conn.Open();

                using (SqlDataReader reader1 = (new SqlCommand("select * from Orders where OrderID = 10248", conn)).ExecuteReader())
                using (SqlDataReader reader2 = (new SqlCommand("select * from Orders where OrderID = 10249", conn)).ExecuteReader())
                using (SqlDataReader reader3 = (new SqlCommand("select * from Orders where OrderID = 10250", conn)).ExecuteReader())
                {
                    Assert.True(reader1.Read() && reader2.Read() && reader3.Read(), "MARSSyncExecuteReaderTest4 failure #1");

                    Assert.True(reader1.GetInt32(0) == 10248 &&
                                reader2.GetInt32(0) == 10249 &&
                                reader3.GetInt32(0) == 10250,
                                "MARSSyncExecuteReaderTest4 failure #2");

                    Assert.False(reader1.Read() || reader2.Read() || reader3.Read(), "MARSSyncExecuteReaderTest4 failure #3");
                }
            }
        }
    }
}


