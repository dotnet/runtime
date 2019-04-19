// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class ReaderTest
    {
        [ConditionalFact(typeof(DataTestUtility),nameof(DataTestUtility.AreConnStringsSetup))]
        public static void TestMain()
        {
            string connectionString = DataTestUtility.TcpConnStr;

            string tempTable = DataTestUtility.GetUniqueName("T", "[", "]");
            string tempKey = DataTestUtility.GetUniqueName("K", "[", "]");

            DbProviderFactory provider = SqlClientFactory.Instance;
            try
            {
                using (DbConnection con = provider.CreateConnection())
                {
                    con.ConnectionString = connectionString;
                    con.Open();

                    using (DbCommand cmd = provider.CreateCommand())
                    {
                        cmd.Connection = con;
                        DbTransaction tx;

                        #region <<Create temp table>>
                        cmd.CommandText = "SELECT LastName, FirstName, Title, Address, City, Region, PostalCode, Country into " + tempTable + " from Employees where EmployeeID=0";
                        cmd.ExecuteNonQuery();

                        #endregion

                        tx = con.BeginTransaction();
                        cmd.Transaction = tx;

                        cmd.CommandText = "insert into " + tempTable + "(LastName, FirstName, Title, Address, City, Region, PostalCode, Country) values ('Doe', 'Jane' , 'Ms.', 'One Microsoft Way', 'Redmond', 'WA', '98052', 'USA')";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "insert into " + tempTable + "(LastName, FirstName, Title, Address, City, Region, PostalCode, Country) values ('Doe', 'John' , 'Mr.', NULL, NULL, NULL, NULL, NULL)";
                        cmd.ExecuteNonQuery();

                        tx.Commit();

                        cmd.Transaction = null;
                        string parameterName = "@p1";
                        DbParameter p1 = cmd.CreateParameter();
                        p1.ParameterName = parameterName;
                        p1.Value = "Doe";
                        cmd.Parameters.Add(p1);

                        cmd.CommandText = "select * from " + tempTable + " where LastName = " + parameterName;

                        // Test GetValue + IsDBNull
                        using (DbDataReader rdr = cmd.ExecuteReader())
                        {
                            StringBuilder actualResult = new StringBuilder();
                            int currentValue = 0;
                            string[] expectedValues =
                            {
                                "Doe,Jane,Ms.,One Microsoft Way,Redmond,WA,98052,USA",
                                "Doe,John,Mr.,(NULL),(NULL),(NULL),(NULL),(NULL)"
                            };

                            while (rdr.Read())
                            {
                                Assert.True(currentValue < expectedValues.Length, "ERROR: Received more values than expected");

                                for (int i = 0; i < rdr.FieldCount; i++)
                                {
                                    if (i > 0)
                                    {
                                        actualResult.Append(",");
                                    }
                                    if (rdr.IsDBNull(i))
                                    {
                                        actualResult.Append("(NULL)");
                                    }
                                    else
                                    {
                                        actualResult.Append(rdr.GetValue(i));
                                    }
                                }

                                DataTestUtility.AssertEqualsWithDescription(expectedValues[currentValue++], actualResult.ToString(), "FAILED: Did not receive expected data");
                                actualResult.Clear();
                            }
                        }

                        // Test GetFieldValue<T> + IsDBNull
                        using (DbDataReader rdr = cmd.ExecuteReader())
                        {
                            StringBuilder actualResult = new StringBuilder();
                            int currentValue = 0;
                            string[] expectedValues =
                            {
                                "Doe,Jane,Ms.,One Microsoft Way,Redmond,WA,98052,USA",
                                "Doe,John,Mr.,(NULL),(NULL),(NULL),(NULL),(NULL)"
                            };

                            while (rdr.Read())
                            {
                                Assert.True(currentValue < expectedValues.Length, "ERROR: Received more values than expected");

                                for (int i = 0; i < rdr.FieldCount; i++)
                                {
                                    if (i > 0)
                                    {
                                        actualResult.Append(",");
                                    }
                                    if (rdr.IsDBNull(i))
                                    {
                                        actualResult.Append("(NULL)");
                                    }
                                    else
                                    {
                                        if (rdr.GetFieldType(i) == typeof(bool))
                                        {
                                            actualResult.Append(rdr.GetFieldValue<bool>(i));
                                        }
                                        else if (rdr.GetFieldType(i) == typeof(decimal))
                                        {
                                            actualResult.Append(rdr.GetFieldValue<decimal>(i));
                                        }
                                        else if (rdr.GetFieldType(i) == typeof(int))
                                        {
                                            actualResult.Append(rdr.GetFieldValue<int>(i));
                                        }
                                        else
                                        {
                                            actualResult.Append(rdr.GetFieldValue<string>(i));
                                        }
                                    }
                                }

                                DataTestUtility.AssertEqualsWithDescription(expectedValues[currentValue++], actualResult.ToString(), "FAILED: Did not receive expected data");
                                actualResult.Clear();
                            }
                        }

                        // Test GetFieldValueAsync<T> + IsDBNullAsync
                        using (DbDataReader rdr = cmd.ExecuteReaderAsync().Result)
                        {
                            StringBuilder actualResult = new StringBuilder();
                            int currentValue = 0;
                            string[] expectedValues =
                            {
                                "Doe,Jane,Ms.,One Microsoft Way,Redmond,WA,98052,USA",
                                "Doe,John,Mr.,(NULL),(NULL),(NULL),(NULL),(NULL)"
                            };

                            while (rdr.ReadAsync().Result)
                            {
                                Assert.True(currentValue < expectedValues.Length, "ERROR: Received more values than expected");

                                for (int i = 0; i < rdr.FieldCount; i++)
                                {
                                    if (i > 0)
                                    {
                                        actualResult.Append(",");
                                    }
                                    if (rdr.IsDBNullAsync(i).Result)
                                    {
                                        actualResult.Append("(NULL)");
                                    }
                                    else
                                    {
                                        if (rdr.GetFieldType(i) == typeof(bool))
                                        {
                                            actualResult.Append(rdr.GetFieldValueAsync<bool>(i).Result);
                                        }
                                        else if (rdr.GetFieldType(i) == typeof(decimal))
                                        {
                                            actualResult.Append(rdr.GetFieldValueAsync<decimal>(i).Result);
                                        }
                                        else if (rdr.GetFieldType(i) == typeof(int))
                                        {
                                            actualResult.Append(rdr.GetFieldValue<int>(i));
                                        }
                                        else
                                        {
                                            actualResult.Append(rdr.GetFieldValueAsync<string>(i).Result);
                                        }
                                    }
                                }

                                DataTestUtility.AssertEqualsWithDescription(expectedValues[currentValue++], actualResult.ToString(), "FAILED: Did not receive expected data");
                                actualResult.Clear();
                            }
                        }
                    }

                    // GetStream
                    byte[] correctBytes = { 0x12, 0x34, 0x56, 0x78 };
                    string queryString;
                    string correctBytesAsString = "0x12345678";
                    queryString = string.Format("SELECT CAST({0} AS BINARY(20)), CAST({0} AS IMAGE), CAST({0} AS VARBINARY(20))", correctBytesAsString);
                    using (var command = provider.CreateCommand())
                    {
                        command.CommandText = queryString;
                        command.Connection = con;
                        using (var reader = command.ExecuteReader())
                        {
                            reader.Read();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                byte[] buffer = new byte[256];
                                Stream stream = reader.GetStream(i);
                                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                                for (int j = 0; j < correctBytes.Length; j++)
                                {
                                    Assert.True(correctBytes[j] == buffer[j], "ERROR: Bytes do not match");
                                }
                            }
                        }
                    }

                    // GetTextReader
                    string[] correctStrings = { "Hello World", "\uFF8A\uFF9B\uFF70\uFF9C\uFF70\uFF99\uFF84\uFF9E" };
                    string[] collations = { "Latin1_General_CI_AS", "Japanese_CI_AS" };

                    for (int j = 0; j < collations.Length; j++)
                    {
                        string substring = string.Format("(N'{0}' COLLATE {1})", correctStrings[j], collations[j]);
                        queryString = string.Format("SELECT CAST({0} AS CHAR(20)), CAST({0} AS NCHAR(20)), CAST({0} AS NTEXT), CAST({0} AS NVARCHAR(20)), CAST({0} AS TEXT), CAST({0} AS VARCHAR(20))", substring);
                        using (var command = provider.CreateCommand())
                        {
                            command.CommandText = queryString;
                            command.Connection = con;
                            using (var reader = command.ExecuteReader())
                            {
                                reader.Read();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    char[] buffer = new char[256];
                                    TextReader textReader = reader.GetTextReader(i);
                                    int charsRead = textReader.Read(buffer, 0, buffer.Length);
                                    string stringRead = new string(buffer, 0, charsRead);

                                    Assert.True(stringRead == (string)reader.GetValue(i), "ERROR: Strings to not match");
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                using (DbConnection con = provider.CreateConnection())
                {
                    con.ConnectionString = connectionString;
                    con.Open();

                    using (DbCommand cmd = provider.CreateCommand())
                    {
                        cmd.Connection = con;
                        cmd.CommandText = "drop table " + tempTable;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        [ConditionalFact(typeof(DataTestUtility), nameof(DataTestUtility.AreConnStringsSetup))]
        public static async Task TestConcurrentLoadSync()
        {
            ConcurrentLoadContext context = new ConcurrentLoadContext(
                providerFactory: SqlClientFactory.Instance,
                connectionString: DataTestUtility.TcpConnStr,
                mode: ConcurrentLoadContext.Mode.Async,
                warmupSeconds: 1,
                executionSeconds: 60,
                threadCount: Environment.ProcessorCount * 4
            );
            var (transactionPerSecond, average, stdDeviation) = await context.Run();
            Assert.InRange(transactionPerSecond, 1, int.MaxValue);
            Assert.True(average > 0);
            Assert.True(stdDeviation != 0);
        }

        [ConditionalFact(typeof(DataTestUtility), nameof(DataTestUtility.AreConnStringsSetup))]
        public static async Task TestConcurrentLoadAsync()
        {
            ConcurrentLoadContext context = new ConcurrentLoadContext(
                providerFactory: SqlClientFactory.Instance,
                connectionString: DataTestUtility.TcpConnStr,
                mode: ConcurrentLoadContext.Mode.Async,
                warmupSeconds: 1,
                executionSeconds: 60,
                threadCount: Environment.ProcessorCount * 4
            );
            var (transactionPerSecond, average, stdDeviation) = await context.Run();
            Assert.InRange(transactionPerSecond, 1, int.MaxValue);
            Assert.True(average > 0);
            Assert.True(stdDeviation != 0);
        }

        public class ConcurrentLoadContext
        {
            public enum Mode
            {
                Sync,
                Async,
            }

            public sealed class Category
            {
                public int Id;
                public string Description;
            }

            private readonly DbProviderFactory _providerFactory;
            private readonly string _connectionString;
            private readonly string _query;
            private readonly int _warmupSeconds;
            private readonly int _executionSeconds;
            private readonly Mode _mode;

            private Func<Task> _start;
            private Func<Task> _stop;
            private Func<Task> _work;

            private int _counter;
            private int _totalTransactions;
            private int _threadCount;
            private int _running;
            private List<int> _results;
            private DateTime _startTime;
            private DateTime _stopTime;

            public ConcurrentLoadContext(DbProviderFactory providerFactory, string connectionString, Mode mode, int warmupSeconds, int executionSeconds, int threadCount)
            {
                _providerFactory = providerFactory;
                _connectionString = connectionString;
                _threadCount = threadCount;
                _warmupSeconds = warmupSeconds;
                _executionSeconds = executionSeconds;
                _query = "SELECT CategoryID,Description FROM Categories";
                _results = new List<int>(_executionSeconds);
                _mode = mode;
                _start = Start;
                _stop = Stop;
                switch (_mode)
                {
                    case Mode.Sync:
                        _work = DoWorkSync;
                        break;
                    case Mode.Async:
                        _work = DoWorkSync;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(mode));
                }
            }

            public void IncrementCounter() => Interlocked.Increment(ref _counter);

            public bool IsRunning
            {
                get => _running == 1;
                set => Interlocked.Exchange(ref _running, value ? 1 : 0);
            }

            public Task DoWorkSync()
            {
                while (IsRunning)
                {
                    var results = new List<Category>();

                    using (var connection = _providerFactory.CreateConnection())
                    {
                        connection.ConnectionString = _connectionString;
                        connection.Open();

                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = _query;
                            command.Prepare();

                            using (var reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    results.Add(
                                        new Category
                                        {
                                            Id = reader.GetInt32(0),
                                            Description = reader.GetString(1)
                                        });
                                }
                            }
                        }
                    }

                    CheckResults(results);

                    IncrementCounter();
                }

                return Task.CompletedTask;
            }

            public Task DoWorkSyncCaching()
            {
                using (var connection = _providerFactory.CreateConnection())
                {
                    connection.ConnectionString = _connectionString;
                    connection.Open();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = _query;
                        command.Prepare();

                        while (IsRunning)
                        {
                            var results = new List<Category>();

                            using (var reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    results.Add(
                                        new Category
                                        {
                                            Id = reader.GetInt32(0),
                                            Description = reader.GetString(1)
                                        });
                                }
                            }

                            CheckResults(results);

                            IncrementCounter();
                        }
                    }
                }

                return Task.CompletedTask;
            }

            public async Task DoWorkAsync()
            {
                while (IsRunning)
                {
                    var results = new List<Category>();

                    using (var connection = _providerFactory.CreateConnection())
                    {
                        connection.ConnectionString = _connectionString;

                        await connection.OpenAsync();

                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = _query;
                            command.Prepare();

                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    results.Add(
                                        new Category
                                        {
                                            Id = reader.GetInt32(0),
                                            Description = reader.GetString(1)
                                        });
                                }
                            }
                        }
                    }

                    CheckResults(results);

                    IncrementCounter();
                }
            }

            public async Task DoWorkAsyncCaching()
            {
                using (var connection = _providerFactory.CreateConnection())
                {
                    connection.ConnectionString = _connectionString;

                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = _query;
                        command.Prepare();

                        while (IsRunning)
                        {
                            var results = new List<Category>();

                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    results.Add(
                                        new Category
                                        {
                                            Id = reader.GetInt32(0),
                                            Description = reader.GetString(1)
                                        });
                                }
                            }

                            CheckResults(results);

                            IncrementCounter();
                        }
                    }
                }
            }

            public async Task<(int transactionPerSecond,double average, double stdDeviation)> Run()
            {
                IsRunning = true;

                await Task.WhenAll(CreateTasks());

                _results.Sort();
                _results.RemoveAt(0);
                _results.RemoveAt(_results.Count - 1);

                (double avg, double stdDev) = CalculateStdDev(_results);

                int totalTps = (int)(_totalTransactions / (_stopTime - _startTime).TotalSeconds);

                return (totalTps, avg, stdDev);
            }

            private async Task Start()
            {
                await Task.Delay(TimeSpan.FromSeconds(_warmupSeconds));

                Interlocked.Exchange(ref _counter, 0);

                _startTime = DateTime.UtcNow;
                var lastDisplay = _startTime;

                while (IsRunning)
                {
                    await Task.Delay(200);

                    DateTime now = DateTime.UtcNow;
                    int tps = (int)(_counter / (now - lastDisplay).TotalSeconds);

                    _results.Add(tps);

                    lastDisplay = now;
                    _totalTransactions += Interlocked.Exchange(ref _counter, 0);
                }
            }

            private async Task Stop()
            {
                await Task.Delay(TimeSpan.FromSeconds(_warmupSeconds + _executionSeconds));
                Interlocked.Exchange(ref _running, 0);
                _stopTime = DateTime.UtcNow;
            }

            private IEnumerable<Task> CreateTasks()
            {
                //yield return Task.Run(
                //    async () =>
                //    {

                //        await Task.Delay(TimeSpan.FromSeconds(_warmupSeconds));

                //        Interlocked.Exchange(ref _counter, 0);

                //        _startTime = DateTime.UtcNow;
                //        var lastDisplay = _startTime;

                //        while (IsRunning)
                //        {
                //            await Task.Delay(200);

                //            var now = DateTime.UtcNow;
                //            var tps = (int)(_counter / (now - lastDisplay).TotalSeconds);
                //            var remaining = (int)(_executionSeconds - (now - _startTime).TotalSeconds);

                //            _results.Add(tps);

                //            lastDisplay = now;
                //            _totalTransactions += Interlocked.Exchange(ref _counter, 0);
                //        }
                //    }
                //    );

                yield return Task.Run(_start);

                //yield return Task.Run(
                //    async () =>
                //    {
                //        await Task.Delay(TimeSpan.FromSeconds(_warmupSeconds + _executionSeconds));

                //        Interlocked.Exchange(ref _running, 0);

                //        _stopTime = DateTime.UtcNow;
                //    });

                yield return Task.Run(_stop);

                foreach (var task in Enumerable.Range(0, _threadCount)
                    .Select(_ => Task.Factory.StartNew(_work, TaskCreationOptions.LongRunning).Unwrap()))
                {
                    yield return task;
                }
            }

            private static void CheckResults(ICollection<Category> results)
            {
                Assert.NotNull(results);
                Assert.Equal(8, results.Count);
            }

            private static (double, double) CalculateStdDev(ICollection<int> values)
            {
                double avg = values.Average();
                double sum = values.Sum(d => Math.Pow(d - avg, 2));

                return (avg, Math.Sqrt(sum / values.Count));
            }
        }
    }
}
