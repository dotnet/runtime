// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

using DPStressHarness;

namespace Stress.Data
{
    /// <summary>
    ///  basic set of tests to run on each managed provider
    /// </summary>
    public abstract class DataTestGroup
    {
        // random is not thread-safe, create one per thread - use RandomInstance to access it.
        // note that each thread and each test method has a different instance of this object, so it
        // doesn't need to be synchronised or have [ThreadStatic], etc
        private TrackedRandom _randomInstance = new TrackedRandom();
        protected Random RandomInstance
        {
            get
            {
                _randomInstance.Mark();
                return _randomInstance;
            }
        }

        /// <summary>
        /// Test factory to use for generation of connection strings and other test objects. Factory is initialized during setup.
        /// null is not returned - if setup was not called yet, exception is raised
        /// This is static so that is shared across all threads (since stresstest will create a new DataTestGroup object for each thread)
        /// </summary>
        private static DataStressFactory s_factory;
        public static DataStressFactory Factory
        {
            get
            {
                DataStressErrors.Assert(s_factory != null, "Tried to access DataTestGroup.Factory before Setup has been called");
                return s_factory;
            }
        }

        /// <summary>
        /// This method is called to create the stress factory used to create connections, commands, etc...
        /// Implementation should set the source and the scenario to valid values if inputs are null/empty.
        /// </summary>
        /// <param name="scenario">Scenario string specified by the user or empty to set default</param>
        /// <param name="dataSource">DataSource string specified by the user or empty to use connection string as is, useful when developing new tests</param>
        protected abstract DataStressFactory CreateFactory(ref string scenario, ref DataSource source);

        /// <summary>
        /// scenario to run, initialized in setup
        /// null is not returned - if setup was not called yet, exception is raised
        /// This is static so that is shared across all threads (since stresstest will create a new DataTestGroup object for each thread)
        /// </summary>
        private static string s_scenario;
        protected static string Scenario
        {
            get
            {
                DataStressErrors.Assert(s_scenario != null, "Tried to access DataTestGroup.Scenario before Setup has been called");
                return s_scenario;
            }
        }

        /// <summary>
        /// data source information used by stress, initialized in Setup
        /// null is not returned - if setup was not called yet, exception is raised
        /// This is static so that is shared across all threads (since stresstest will create a new DataTestGroup object for each thread)
        /// </summary>
        private static DataSource s_source;
        protected static DataSource Source
        {
            get
            {
                DataStressErrors.Assert(s_source != null, "Tried to access DataTestGroup.Source before Setup has been called");
                return s_source;
            }
        }


        /// <summary>
        /// Does test setup that is shared across all threads. This method will be called only once, before
        /// any [TestSetup] methods are called.
        /// If you override this method you must call base.GlobalTestSetup() at the beginning.
        /// </summary>
        [GlobalTestSetup]
        public virtual void GlobalTestSetup()
        {
            // Preconditions - ensure this setup is only called once
            DataStressErrors.Assert(string.IsNullOrEmpty(s_scenario), "Scenario was already set");
            DataStressErrors.Assert(s_source == null, "Source was already set");
            DataStressErrors.Assert(s_factory == null, "Factory was already set");

            // Set m_scenario
            string userProvidedScenario;
            TestMetrics.Overrides.TryGetValue("scenario", out userProvidedScenario);
            // Empty means default scenario for the test group
            s_scenario = (userProvidedScenario ?? string.Empty);
            s_scenario = s_scenario.ToUpperInvariant();

            // Set m_source
            // Empty means that test group will peek the default data source from the config file based on the scenario
            string userProvidedSourceName;
            if (TestMetrics.Overrides.TryGetValue("source", out userProvidedSourceName))
            {
                s_source = DataStressSettings.Instance.GetSourceByName(userProvidedSourceName);
            }

            // Set m_factory
            s_factory = CreateFactory(ref s_scenario, ref s_source);
            s_factory.InitializeSharedData(s_source);

            // Postconditions
            DataStressErrors.Assert(!string.IsNullOrEmpty(s_scenario), "Scenario was not set");
            DataStressErrors.Assert(s_source != null, "Source was not set");
            DataStressErrors.Assert(s_factory != null, "Factory was not set");
        }

        /// <summary>
        /// Does test cleanup that is shared across all threads. This method will not be called until all
        /// threads have finished executing [StressTest] methods. This method will be called only once.
        /// If you override this method you must call base.GlobalTestSetup() at the beginning.
        /// </summary>
        [GlobalTestCleanup]
        public virtual void GlobalTestCleanup()
        {
            s_factory.CleanupSharedData();
            s_source = null;
            s_scenario = null;
            s_factory.Dispose();
            s_factory = null;
        }


        protected bool OpenConnection(DataStressConnection conn)
        {
            try
            {
                conn.Open();
                return true;
            }
            catch (Exception e)
            {
                if (IsServerNotAccessibleException(e, conn.DbConnection.ConnectionString, conn.DbConnection.DataSource))
                {
                    // Ignore this exception.
                    // This exception will fire when using named pipes with MultiSubnetFailover option set to true.
                    // MultiSubnetFailover=true only works with TCP/IP protocol and will result in exception when using with named pipes.
                    return false;
                }
                else
                {
                    throw e;
                }
            }
        }


        [GlobalExceptionHandler]
        public virtual void GlobalExceptionHandler(Exception e)
        {
        }

        /// <summary>
        /// Returns whether or not the datareader should be closed
        /// </summary>
        protected virtual bool ShouldCloseDataReader()
        {
            // Ignore commandCancelled, instead randomly close it 9/10 of the time
            return RandomInstance.Next(10) != 0;
        }


        #region	CommandExecute and Consume methods

        /// <summary>
        /// Utility function used by command tests
        /// </summary>
        protected virtual void CommandExecute(Random rnd, DbCommand com, bool query)
        {
            AsyncUtils.WaitAndUnwrapException(CommandExecuteAsync(rnd, com, query));
        }

        protected async virtual Task CommandExecuteAsync(Random rnd, DbCommand com, bool query)
        {
            CancellationTokenSource cts = null;

            // Cancel 1/10 commands
            Task cancelTask = null;
            bool cancelCommand = rnd.NextBool(0.1);
            if (cancelCommand)
            {
                if (rnd.NextBool())
                {
                    // Use DbCommand.Cancel
                    cancelTask = Task.Run(() => CommandCancel(com));
                }
                else
                {
                    // Use CancellationTokenSource
                    if (cts == null) cts = new CancellationTokenSource();
                    cancelTask = Task.Run(() => cts.Cancel());
                }
            }

            // Get the CancellationToken
            CancellationToken token = (cts != null) ? cts.Token : CancellationToken.None;

            DataStressReader reader = null;
            try
            {
                if (query)
                {
                    CommandBehavior commandBehavior = CommandBehavior.Default;
                    if (rnd.NextBool(0.5)) commandBehavior |= CommandBehavior.SequentialAccess;
                    if (rnd.NextBool(0.25)) commandBehavior |= CommandBehavior.KeyInfo;
                    if (rnd.NextBool(0.1)) commandBehavior |= CommandBehavior.SchemaOnly;

                    // Get the reader
                    reader = new DataStressReader(await com.ExecuteReaderSyncOrAsync(commandBehavior, token, rnd));

                    // Consume the reader's data
                    await ConsumeReaderAsync(reader, commandBehavior.HasFlag(CommandBehavior.SequentialAccess), token, rnd);
                }
                else
                {
                    await com.ExecuteNonQuerySyncOrAsync(token, rnd);
                }
            }
            catch (Exception e)
            {
                if (cancelCommand && IsCommandCancelledException(e))
                {
                    // Catch command canceled exception
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                if (cancelTask != null) AsyncUtils.WaitAndUnwrapException(cancelTask);
                if (reader != null && ShouldCloseDataReader()) reader.Close();
            }
        }

        /// <summary>
        /// Utility function to consume a reader in a random fashion
        /// </summary>
        protected virtual async Task ConsumeReaderAsync(DataStressReader reader, bool sequentialAccess, CancellationToken token, Random rnd)
        {
            // Close 1/10 of readers while they are reading
            Task closeTask = null;
            if (AllowReaderCloseDuringReadAsync() && rnd.NextBool(0.1))
            {
                // Begin closing now on another thread
                closeTask = reader.CloseAsync();
            }

            try
            {
                do
                {
                    while (await reader.ReadSyncOrAsync(token, rnd))
                    {
                        // Optionally stop reading the current result set
                        if (rnd.NextBool(0.1)) break;

                        // Read the current row
                        await ConsumeRowAsync(reader, sequentialAccess, token, rnd);
                    }

                    // Executing NextResult only 50% of the time
                    if (rnd.NextBool())
                        break;
                } while (await reader.NextResultSyncOrAsync(token, rnd));
            }
            catch (Exception e)
            {
                if (closeTask != null && IsReaderClosedException(e))
                {
                    // Catch reader closed exception
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                if (closeTask != null) AsyncUtils.WaitAndUnwrapException(closeTask);
            }
        }

        /// <summary>
        /// Utility function to consume a single row of a reader in a random fashion after Read/ReadAsync has been invoked.
        /// </summary>
        protected virtual async Task ConsumeRowAsync(DataStressReader reader, bool sequentialAccess, CancellationToken token, Random rnd)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (rnd.Next(10) == 0) break; // stop reading from this row
                if (rnd.Next(2) == 0) continue; // skip this field
                bool hasBeenRead = false;

                // If the field is not null, we can optionally use streaming API
                if ((!await reader.IsDBNullSyncOrAsync(i, token, rnd)) && (rnd.NextBool()))
                {
                    Type t = reader.GetFieldType(i);
                    if (t == typeof(byte[]))
                    {
                        await ConsumeBytesAsync(reader, i, token, rnd);
                        hasBeenRead = true;
                    }
                    else if (t == typeof(string))
                    {
                        await ConsumeCharsAsync(reader, i, token, rnd);
                        hasBeenRead = true;
                    }
                }

                // If the field has not yet been read, or if it is non-sequential then we can re-read it
                if ((!hasBeenRead) || (!sequentialAccess))
                {
                    if (!await reader.IsDBNullSyncOrAsync(i, token, rnd))
                    {
                        // Field value is not null, we can use new GetFieldValue<T> methods
                        await reader.GetValueSyncOrAsync(i, token, rnd);
                    }
                    else
                    {
                        // Field value is null, we have to use old GetValue method
                        reader.GetValue(i);
                    }
                }

                // Do IsDBNull check again with 50% probability
                if (rnd.NextBool()) await reader.IsDBNullSyncOrAsync(i, token, rnd);
            }
        }

        protected virtual async Task ConsumeBytesAsync(DataStressReader reader, int i, CancellationToken token, Random rnd)
        {
            byte[] buffer = new byte[255];

            if (rnd.NextBool())
            {
                // We can optionally use GetBytes
                reader.GetBytes(i, rnd.Next(20), buffer, rnd.Next(20), rnd.Next(200));
            }
            else if (reader.GetName(i) != "timestamp_FLD")
            {
                // Timestamp appears to be binary, but cannot be read by Stream
                DataStressStream stream = reader.GetStream(i);
                await stream.ReadSyncOrAsync(buffer, rnd.Next(20), rnd.Next(200), token, rnd);
            }
            else
            {
                // It is timestamp column, so read it later with GetValueSyncOrAsync
                await reader.GetValueSyncOrAsync(i, token, rnd);
            }
        }

        protected virtual async Task ConsumeCharsAsync(DataStressReader reader, int i, CancellationToken token, Random rnd)
        {
            char[] buffer = new char[255];

            if (rnd.NextBool())
            {
                // Read with GetChars
                reader.GetChars(i, rnd.Next(20), buffer, rnd.Next(20), rnd.Next(200));
            }
            else if (reader.GetProviderSpecificFieldType(i) == typeof(SqlXml))
            {
                // SqlClient only: Xml is read by XmlReader
                DataStressXmlReader xmlReader = reader.GetXmlReader(i);
                xmlReader.Read();
            }
            else
            {
                // Read with TextReader
                DataStressTextReader textReader = reader.GetTextReader(i);
                if (rnd.NextBool())
                {
                    textReader.Peek();
                }
                await textReader.ReadSyncOrAsync(buffer, rnd.Next(20), rnd.Next(200), rnd);
                if (rnd.NextBool())
                {
                    textReader.Peek();
                }
            }
        }

        /// <summary>
        /// Returns true if the given exception is expected for the current provider when a command is cancelled by another thread.
        /// </summary>
        /// <param name="e"></param>
        protected virtual bool IsCommandCancelledException(Exception e)
        {
            return e is TaskCanceledException;
        }

        /// <summary>
        /// Returns true if the given exception is expected for the current provider when trying to read from a reader that has been closed
        /// </summary>
        /// <param name="e"></param>
        protected virtual bool IsReaderClosedException(Exception e)
        {
            return false;
        }

        /// <summary>
        /// Returns true if the given exception is expected for the current provider when trying to connect to unavailable/non-existent server
        /// </summary>
        /// <param name="e"></param>
        protected bool IsServerNotAccessibleException(Exception e, string connString, string dataSource)
        {
            return
                e is ArgumentException &&
                connString.Contains("MultiSubnetFailover=True") &&
                dataSource.Contains("np:") &&
                e.Message.Contains("Connecting to a SQL Server instance using the MultiSubnetFailover connection option is only supported when using the TCP protocol.");
        }

        /// <summary>
        /// Returns true if the backend provider supports closing a datareader while asynchronously reading from it
        /// </summary>
        /// <returns></returns>
        protected virtual bool AllowReaderCloseDuringReadAsync()
        {
            return false;
        }

        /// <summary>
        /// Thread Callback function which cancels queries using DbCommand.Cancel()
        /// </summary>
        /// <param name="cmd"></param>
        protected void CommandCancel(object o)
        {
            try
            {
                DbCommand cmd = (DbCommand)o;
                cmd.Cancel();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString(), this.ToString());
            }
        }

        #endregion

        #region	Command and Parameter Tests

        /// <summary>
        /// Command Reader Test: Executes a simple SELECT statement without parameters
        /// </summary>
        [StressTest("TestCommandReader", Weight = 10)]
        public void TestCommandReader()
        {
            Random rnd = RandomInstance;

            using (DataStressConnection conn = Factory.CreateConnection(rnd))
            {
                if (!OpenConnection(conn)) return;
                DataStressFactory.TableMetadata table = Factory.GetRandomTable(rnd);
                DbCommand com = Factory.GetCommand(rnd, table, conn, true);
                CommandExecute(rnd, com, true);
            }
        }

        /// <summary>
        /// Command Select Test: Executes a single SELECT statement with parameters
        /// </summary>
        [StressTest("TestCommandSelect", Weight = 10)]
        public void TestCommandSelect()
        {
            Random rnd = RandomInstance;

            using (DataStressConnection conn = Factory.CreateConnection(rnd))
            {
                if (!OpenConnection(conn)) return;
                DataStressFactory.TableMetadata table = Factory.GetRandomTable(rnd);
                DbCommand com = Factory.GetSelectCommand(rnd, table, conn);
                CommandExecute(rnd, com, true);
            }
        }

        /// <summary>
        /// Command Insert Test: Executes a single INSERT statement with parameters
        /// </summary>
        [StressTest("TestCommandInsert", Weight = 10)]
        public void TestCommandInsert()
        {
            Random rnd = RandomInstance;

            using (DataStressConnection conn = Factory.CreateConnection(rnd))
            {
                if (!OpenConnection(conn)) return;
                DataStressFactory.TableMetadata table = Factory.GetRandomTable(rnd);
                DbCommand com = Factory.GetInsertCommand(rnd, table, conn);
                CommandExecute(rnd, com, false);
            }
        }

        /// <summary>
        /// Command Update Test: Executes a single UPDATE statement with parameters
        /// </summary>
        [StressTest("TestCommandUpdate", Weight = 10)]
        public void TestCommandUpdate()
        {
            Random rnd = RandomInstance;

            using (DataStressConnection conn = Factory.CreateConnection(rnd))
            {
                if (!OpenConnection(conn)) return;
                DataStressFactory.TableMetadata table = Factory.GetRandomTable(rnd);
                DbCommand com = Factory.GetUpdateCommand(rnd, table, conn);
                CommandExecute(rnd, com, false);
            }
        }

        /// <summary>
        /// Command Update Test: Executes a single DELETE statement with parameters
        /// </summary>
        [StressTest("TestCommandDelete", Weight = 10)]
        public void TestCommandDelete()
        {
            Random rnd = RandomInstance;

            using (DataStressConnection conn = Factory.CreateConnection(rnd))
            {
                if (!OpenConnection(conn)) return;
                DataStressFactory.TableMetadata table = Factory.GetRandomTable(rnd);
                DbCommand com = Factory.GetDeleteCommand(rnd, table, conn);
                CommandExecute(rnd, com, false);
            }
        }

        [StressTest("TestCommandTimeout", Weight = 10)]
        public void TestCommandTimeout()
        {
            Random rnd = RandomInstance;
            DataStressConnection conn = null;
            try
            {
                // Use a transaction 50% of the time
                if (rnd.NextBool())
                {
                }

                // Create a select command
                conn = Factory.CreateConnection(rnd);
                if (!OpenConnection(conn)) return;
                DataStressFactory.TableMetadata table = Factory.GetRandomTable(rnd);
                DbCommand com = Factory.GetSelectCommand(rnd, table, conn);

                // Setup timeout. We want to see various possibilities of timeout happening before, after, or at the same time as when the result comes in.
                int delay = rnd.Next(0, 10); // delay is from 0 to 9 seconds inclusive
                int timeout = rnd.Next(1, 10); // timeout is from 1 to 9 seconds inclusive
                com.CommandText += string.Format("; WAITFOR DELAY '00:00:0{0}'", delay);
                com.CommandTimeout = timeout;

                // Execute command and catch timeout exception
                try
                {
                    CommandExecute(rnd, com, true);
                }
                catch (DbException e)
                {
                    if (e is SqlException && ((SqlException)e).Number == 3989)
                    {
                        throw DataStressErrors.ProductError("Timing issue between OnTimeout and ReadAsyncCallback results in SqlClient's packet parsing going out of sync", e);
                    }
                    else if (!e.Message.ToLower().Contains("timeout"))
                    {
                        throw;
                    }
                }
            }
            finally
            {
                if (conn != null) conn.Dispose();
            }
        }

        [StressTest("TestCommandAndReaderAsync", Weight = 10)]
        public void TestCommandAndReaderAsync()
        {
            // Since we're calling an "async" method, we need to do a Wait() here.
            AsyncUtils.WaitAndUnwrapException(TestCommandAndReaderAsyncInternal());
        }

        /// <summary>
        /// Utility method to test Async scenario using await keyword
        /// </summary>
        /// <returns></returns>
        protected virtual async Task TestCommandAndReaderAsyncInternal()
        {
            Random rnd = RandomInstance;
            using (DataStressConnection conn = Factory.CreateConnection(rnd))
            {
                if (!OpenConnection(conn)) return;
                DataStressFactory.TableMetadata table = Factory.GetRandomTable(rnd);
                DbCommand com;

                com = Factory.GetInsertCommand(rnd, table, conn);
                await CommandExecuteAsync(rnd, com, false);

                com = Factory.GetDeleteCommand(rnd, table, conn);
                await CommandExecuteAsync(rnd, com, false);

                com = Factory.GetSelectCommand(rnd, table, conn);
                await com.ExecuteScalarAsync();

                com = Factory.GetSelectCommand(rnd, table, conn);
                await CommandExecuteAsync(rnd, com, true);
            }
        }

        /// <summary>
        /// Utility function used by MARS tests
        /// </summary>
        private void TestCommandMARS(Random rnd, bool query)
        {
            if (Source.Type != DataSourceType.SqlServer)
                return; // skip for non-SQL Server databases

            using (DataStressConnection conn = Factory.CreateConnection(rnd, DataStressFactory.ConnectionStringOptions.EnableMars))
            {
                if (!OpenConnection(conn)) return;
                DbCommand[] commands = new DbCommand[rnd.Next(5, 10)];
                List<Task> tasks = new List<Task>();
                // Create commands
                for (int i = 0; i < commands.Length; i++)
                {
                    DataStressFactory.TableMetadata table = Factory.GetRandomTable(rnd);
                    commands[i] = Factory.GetCommand(rnd, table, conn, query);
                }

                try
                {
                    // Execute commands
                    for (int i = 0; i < commands.Length; i++)
                    {
                        if (rnd.NextBool(0.7))
                            tasks.Add(CommandExecuteAsync(rnd, commands[i], query));
                        else
                            CommandExecute(rnd, commands[i], query);
                    }
                }
                finally
                {
                    // All commands must be complete before closing the connection
                    AsyncUtils.WaitAll(tasks.ToArray());
                }
            }
        }

        /// <summary>
        /// Command MARS Test: Tests MARS by executing multiple readers on same connection
        /// </summary>
        [StressTest("TestCommandMARSRead", Weight = 10)]
        public void TestCommandMARSRead()
        {
            Random rnd = RandomInstance;
            TestCommandMARS(rnd, true);
        }

        /// <summary>
        /// Command MARS Test: Tests MARS by getting multiple connection objects from same connection
        /// </summary>
        [StressTest("TestCommandMARSWrite", Weight = 10)]
        public void TestCommandMARSWrite()
        {
            Random rnd = RandomInstance;
            TestCommandMARS(rnd, false);
        }

        #endregion
    }
}
