// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class RandomStressTest
    {
        private static readonly TimeSpan TimeLimitDefault = new TimeSpan(0, 0, 10);
        private const int ThreadCountDefault = 4;
        private const int IterationsPerTableDefault = 50;

        private const int MaxColumns = 5000;
        private const int MaxRows = 100;
        private const int MaxTotal = MaxColumns * 10;

        private string[] _connectionStrings;
        private string _operationCanceledErrorMessage;
        private string _severeErrorMessage;

        private SqlRandomTypeInfoCollection _katmaiTypes;
        private ManualResetEvent _endEvent;
        private int _runningThreads;

        private long _totalValues;
        private long _totalTables;
        private long _totalIterations;
        private long _totalTicks;
        private RandomizerPool _randPool;

        [CheckConnStrSetupFact]
        public void TestMain()
        {
            _operationCanceledErrorMessage = SystemDataResourceManager.Instance.SQL_OperationCancelled;
            _severeErrorMessage = SystemDataResourceManager.Instance.SQL_SevereError;

            // pure random
            _randPool = new RandomizerPool();

            SqlConnectionStringBuilder regularConnectionString = new SqlConnectionStringBuilder(DataTestUtility.TcpConnStr);

            regularConnectionString.MultipleActiveResultSets = false;

            List<string> connStrings = new List<string>();
            connStrings.Add(regularConnectionString.ToString());

            connStrings.Add(regularConnectionString.ToString());

            regularConnectionString.MultipleActiveResultSets = true;
            connStrings.Add(regularConnectionString.ToString());

            _connectionStrings = connStrings.ToArray();

            _katmaiTypes = SqlRandomTypeInfoCollection.CreateSql2008Collection();
            _endEvent = new ManualResetEvent(false);

            if (_randPool.ReproMode)
            {
                _runningThreads = 1;
                TestThread();
            }
            else
            {
                for (int tcount = 0; tcount < ThreadCountDefault; tcount++)
                {
                    Thread t = new Thread(TestThread);
                    t.Start();
                }
            }
        }

        private void NextConnection(ref SqlConnection con, Randomizer rand)
        {
            if (con != null)
            {
                con.Close();
            }

            string connString = _connectionStrings[rand.Next(_connectionStrings.Length)];

            con = new SqlConnection(connString);
            con.Open();
        }

        private void TestThread()
        {
            try
            {
                using (var rootScope = _randPool.RootScope<SqlRandomizer>())
                {
                    Stopwatch watch = new Stopwatch();
                    SqlConnection con = null;
                    try
                    {
                        NextConnection(ref con, rootScope.Current);

                        if (_randPool.ReproMode)
                        {
                            using (var testScope = rootScope.NewScope<SqlRandomizer>())
                            {
                                // run only once if repro file is provided
                                RunTest(con, testScope, _katmaiTypes, watch);
                            }
                        }
                        else
                        {
                            while (watch.Elapsed < TimeLimitDefault)
                            {
                                using (var testScope = rootScope.NewScope<SqlRandomizer>())
                                {
                                    RunTest(con, testScope, _katmaiTypes, watch);
                                }

                                if (rootScope.Current.Next(100) == 0)
                                {
                                    // replace the connection
                                    NextConnection(ref con, rootScope.Current);
                                }
                            }
                        }
                    }
                    finally
                    {
                        if (con != null)
                        {
                            con.Close();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                if (Interlocked.Decrement(ref _runningThreads) == 0)
                    _endEvent.Set();
            }
        }

        private void RunTest(SqlConnection con, RandomizerPool.Scope<SqlRandomizer> testScope, SqlRandomTypeInfoCollection types, Stopwatch watch)
        {
            Exception pendingException = null;
            string tempTableName = null;

            try
            {
                // select number of columns to use and null bitmap to test
                int columnsCount, rowsCount;
                testScope.Current.NextTableDimentions(MaxRows, MaxColumns, MaxTotal, out rowsCount, out columnsCount);
                SqlRandomTable table = SqlRandomTable.Create(testScope.Current, types, columnsCount, rowsCount, createPrimaryKeyColumn: true);

                long total = (long)rowsCount * columnsCount;
                Interlocked.Add(ref _totalValues, total);
                Interlocked.Increment(ref _totalTables);

                tempTableName = SqlRandomizer.GenerateUniqueTempTableNameForSqlServer();
                table.GenerateTableOnServer(con, tempTableName);

                long prevTicks = watch.ElapsedTicks;
                watch.Start();

                if (_randPool.ReproMode)
                {
                    // perform one iteration only
                    using (var iterationScope = testScope.NewScope<SqlRandomizer>())
                    {
                        RunTestIteration(con, iterationScope.Current, table, tempTableName);
                        Interlocked.Increment(ref _totalIterations);
                    }
                }
                else
                {
                    // continue with normal loop
                    for (int i = 0; i < IterationsPerTableDefault && watch.Elapsed < TimeLimitDefault; i++)
                    {
                        using (var iterationScope = testScope.NewScope<SqlRandomizer>())
                        {
                            RunTestIteration(con, iterationScope.Current, table, tempTableName);
                            Interlocked.Increment(ref _totalIterations);
                        }
                    }
                }

                watch.Stop();
                Interlocked.Add(ref _totalTicks, watch.ElapsedTicks - prevTicks);
            }
            catch (Exception e)
            {
                pendingException = e;
                throw;
            }
            finally
            {
                // keep the temp table for troubleshooting if debugger is attached
                // the thread is going down anyway and connection will be closed
                if (pendingException == null && tempTableName != null)
                {
                    // destroy the temp table to free resources on the server
                    SqlCommand cmd = con.CreateCommand();
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "DROP TABLE " + tempTableName;
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch
                    {
                    }
                }
            }
        }

        private void RunTestIteration(SqlConnection con, SqlRandomizer rand, SqlRandomTable table, string tableName)
        {
            // random list of columns
            int columnCount = table.Columns.Count;
            int[] columnIndices = rand.NextIndices(columnCount);
            int selectedCount = rand.NextIntInclusive(1, maxValueInclusive: columnCount);

            StringBuilder selectBuilder = new StringBuilder();
            table.GenerateSelectFromTableTSql(tableName, selectBuilder, columnIndices, 0, selectedCount);
            SqlCommand cmd = con.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = selectBuilder.ToString();

            bool cancel = rand.Next(100) == 0; // in 1% of the cases, call Cancel

            if (cancel)
            {
                int cancelAfterMilliseconds = rand.Next(5);
                int cancelAfterSpinCount = rand.Next(1000);

                ThreadPool.QueueUserWorkItem((object state) =>
                    {
                        for (int i = 0; cancel && i < cancelAfterMilliseconds; i++)
                        {
                            Thread.Sleep(1);
                        }
                        if (cancel && cancelAfterSpinCount > 0)
                        {
                            SpinWait.SpinUntil(() => false, new TimeSpan(cancelAfterSpinCount));
                        }
                        if (cancel)
                        {
                            cmd.Cancel();
                        }
                    });
            }

            int readerRand = rand.NextIntInclusive(0, maxValueInclusive: 256);
            CommandBehavior readerBehavior = CommandBehavior.Default;
            if (readerRand % 10 == 0)
                readerBehavior = CommandBehavior.SequentialAccess;
            try
            {
                using (SqlDataReader reader = cmd.ExecuteReader(readerBehavior))
                {
                    int row = 0;
                    while (reader.Read())
                    {
                        int rowRand = rand.NextIntInclusive();
                        if (rowRand % 1000 == 0)
                        {
                            // abandon this reader
                            break;
                        }
                        else if (rowRand % 25 == 0)
                        {
                            // skip the row
                            row++;
                            continue;
                        }

                        IList<object> expectedRow = table[row];
                        for (int c = 0; c < reader.FieldCount; c++)
                        {
                            if (rand.NextIntInclusive(0, maxValueInclusive: 10) == 0)
                            {
                                // skip the column
                                continue;
                            }

                            int expectedTableColumn = columnIndices[c];
                            object expectedValue = expectedRow[expectedTableColumn];
                            if (table.Columns[expectedTableColumn].CanCompareValues)
                            {
                                Assert.True(expectedValue != null, "FAILED: Null is expected with CanCompareValues");

                                // read the value same way it was written
                                object actualValue = table.Columns[expectedTableColumn].Read(reader, c, expectedValue.GetType());
                                Assert.True(table.Columns[expectedTableColumn].CompareValues(expectedValue, actualValue),
                                    string.Format("FAILED: Data Comparison Failure:\n{0}", table.Columns[expectedTableColumn].BuildErrorMessage(expectedValue, actualValue)));
                            }
                        }

                        row++;
                    }
                }

                // keep last - this will stop the cancel task, if it is still active
                cancel = false;
            }
            catch (SqlException e)
            {
                if (!cancel)
                    throw;

                bool expected = false;

                foreach (SqlError error in e.Errors)
                {
                    if (error.Message == _operationCanceledErrorMessage)
                    {
                        // ignore this one - expected if canceled
                        expected = true;
                        break;
                    }
                    else if (error.Message == _severeErrorMessage)
                    {
                        // A severe error occurred on the current command.  The results, if any, should be discarded.
                        expected = true;
                        break;
                    }
                }

                if (!expected)
                {
                    // rethrow to the user
                    foreach (SqlError error in e.Errors)
                    {
                        Console.WriteLine("{0} {1}", error.Number, error.Message);
                    }
                    throw;
                }
            }
            catch (InvalidOperationException e)
            {
                bool expected = false;

                if (e.Message == _operationCanceledErrorMessage)
                {
                    // "Operation canceled" exception is raised as a SqlException (as one of SqlError objects) and as InvalidOperationException
                    expected = true;
                }

                if (!expected)
                {
                    throw;
                }
            }
        }
    }
}

