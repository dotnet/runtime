// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class MARSSessionPoolingTest
    {
        private const string COMMAND_STATUS = "select count(*) as ConnectionCount from sys.dm_exec_connections where session_id=@@spid and net_transport='Session'; select count(*) as ActiveRequestCount from sys.dm_exec_requests where session_id=@@spid and status='running' or session_id=@@spid and status='suspended'";
        private const string COMMAND_SPID = "select @@spid";
        private const int CONCURRENT_COMMANDS = 5;

        private const string _COMMAND_RPC = "sp_who";
        private const string _COMMAND_SQL = 
            "select * from sys.databases; select * from sys.databases; select * from sys.databases; select * from sys.databases; select * from sys.databases; " +
            "select * from sys.databases; select * from sys.databases; select * from sys.databases; select * from sys.databases; select * from sys.databases; " +
            "select * from sys.databases; select * from sys.databases; select * from sys.databases; select * from sys.databases; select * from sys.databases; " +
            "select * from sys.databases; select * from sys.databases; select * from sys.databases; select * from sys.databases; select * from sys.databases; " +
            "select * from sys.databases; print 'THIS IS THE END!'";

        private static readonly string _testConnString =
            (new SqlConnectionStringBuilder(DataTestUtility.TcpConnStr) 
            {
                PacketSize = 512,
                MaxPoolSize = 1,
                MultipleActiveResultSets = true
            }).ConnectionString;

        [ConditionalFact(typeof(DataTestUtility),nameof(DataTestUtility.AreConnStringsSetup))]
        public static void MarsExecuteScalar_AllFlavors()
        {
            TestMARSSessionPooling("Case: Text, ExecuteScalar", _testConnString, CommandType.Text, ExecuteType.ExecuteScalar, ReaderTestType.ReaderClose, GCType.Wait);
            TestMARSSessionPooling("Case: RPC,  ExecuteScalar", _testConnString, CommandType.StoredProcedure, ExecuteType.ExecuteScalar, ReaderTestType.ReaderClose, GCType.Wait);
        }

        [ConditionalFact(typeof(DataTestUtility),nameof(DataTestUtility.AreConnStringsSetup))]
        public static void MarsExecuteNonQuery_AllFlavors()
        {
            TestMARSSessionPooling("Case: Text, ExecuteNonQuery", _testConnString, CommandType.Text, ExecuteType.ExecuteNonQuery, ReaderTestType.ReaderClose, GCType.Wait);
            TestMARSSessionPooling("Case: RPC,  ExecuteNonQuery", _testConnString, CommandType.StoredProcedure, ExecuteType.ExecuteNonQuery, ReaderTestType.ReaderClose, GCType.Wait);
        }

        [ConditionalFact(typeof(DataTestUtility),nameof(DataTestUtility.AreConnStringsSetup))]
        public static void MarsExecuteReader_Text_NoGC()
        {
            TestMARSSessionPooling("Case: Text, ExecuteReader, ReaderClose", _testConnString, CommandType.Text, ExecuteType.ExecuteReader, ReaderTestType.ReaderClose, GCType.Wait);
            TestMARSSessionPooling("Case: Text, ExecuteReader, ReaderDispose", _testConnString, CommandType.Text, ExecuteType.ExecuteReader, ReaderTestType.ReaderDispose, GCType.Wait);
            TestMARSSessionPooling("Case: Text, ExecuteReader, ConnectionClose", _testConnString, CommandType.Text, ExecuteType.ExecuteReader, ReaderTestType.ConnectionClose, GCType.Wait);
        }

        [ConditionalFact(typeof(DataTestUtility),nameof(DataTestUtility.AreConnStringsSetup))]
        public static void MarsExecuteReader_RPC_NoGC()
        {
            TestMARSSessionPooling("Case: RPC,  ExecuteReader, ReaderClose", _testConnString, CommandType.StoredProcedure, ExecuteType.ExecuteReader, ReaderTestType.ReaderClose, GCType.Wait);
            TestMARSSessionPooling("Case: RPC,  ExecuteReader, ReaderDispose", _testConnString, CommandType.StoredProcedure, ExecuteType.ExecuteReader, ReaderTestType.ReaderDispose, GCType.Wait);
            TestMARSSessionPooling("Case: RPC,  ExecuteReader, ConnectionClose", _testConnString, CommandType.StoredProcedure, ExecuteType.ExecuteReader, ReaderTestType.ConnectionClose, GCType.Wait);
        }

        [ConditionalFact(typeof(DataTestUtility),nameof(DataTestUtility.AreConnStringsSetup))]
        public static void MarsExecuteReader_Text_WithGC()
        {
            TestMARSSessionPooling("Case: Text, ExecuteReader, GC-Wait", _testConnString, CommandType.Text, ExecuteType.ExecuteReader, ReaderTestType.ReaderGC, GCType.Wait);
            TestMARSSessionPooling("Case: Text, ExecuteReader, GC-NoWait", _testConnString, CommandType.Text, ExecuteType.ExecuteReader, ReaderTestType.ReaderGC, GCType.NoWait);
        }

        [ConditionalFact(typeof(DataTestUtility),nameof(DataTestUtility.AreConnStringsSetup))]
        public static void MarsExecuteReader_StoredProcedure_WithGC()
        {
            TestMARSSessionPooling("Case: RPC,  ExecuteReader, GC-Wait", _testConnString, CommandType.StoredProcedure, ExecuteType.ExecuteReader, ReaderTestType.ReaderGC, GCType.Wait);
            TestMARSSessionPooling("Case: RPC,  ExecuteReader, GC-NoWait", _testConnString, CommandType.StoredProcedure, ExecuteType.ExecuteReader, ReaderTestType.ReaderGC, GCType.NoWait);

            TestMARSSessionPooling("Case: Text, ExecuteReader, NoCloses", _testConnString + " ", CommandType.Text, ExecuteType.ExecuteReader, ReaderTestType.NoCloses, GCType.Wait);
            TestMARSSessionPooling("Case: RPC,  ExecuteReader, NoCloses", _testConnString + "  ", CommandType.StoredProcedure, ExecuteType.ExecuteReader, ReaderTestType.NoCloses, GCType.Wait);
        }

        private enum ExecuteType
        {
            ExecuteScalar,
            ExecuteNonQuery,
            ExecuteReader,
        }

        private enum ReaderTestType
        {
            ReaderClose,
            ReaderDispose,
            ReaderGC,
            ConnectionClose,
            NoCloses,
        }

        private enum GCType
        {
            Wait,
            NoWait,
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void TestMARSSessionPooling(string caseName, string connectionString, CommandType commandType,
                                           ExecuteType executeType, ReaderTestType readerTestType, GCType gcType)
        {
            SqlCommand[] cmd = new SqlCommand[CONCURRENT_COMMANDS];
            SqlDataReader[] gch = new SqlDataReader[CONCURRENT_COMMANDS];

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                for (int i = 0; i < CONCURRENT_COMMANDS; i++)
                {
                    // Prepare all commands
                    cmd[i] = con.CreateCommand();
                    switch (commandType)
                    {
                        case CommandType.Text:
                            cmd[i].CommandText = _COMMAND_SQL;
                            cmd[i].CommandTimeout = 120;
                            break;
                        case CommandType.StoredProcedure:
                            cmd[i].CommandText = _COMMAND_RPC;
                            cmd[i].CommandTimeout = 120;
                            cmd[i].CommandType = CommandType.StoredProcedure;
                            break;
                    }
                }

                for (int i = 0; i < CONCURRENT_COMMANDS; i++)
                {
                    switch (executeType)
                    {
                        case ExecuteType.ExecuteScalar:
                            cmd[i].ExecuteScalar();
                            break;
                        case ExecuteType.ExecuteNonQuery:
                            cmd[i].ExecuteNonQuery();
                            break;
                        case ExecuteType.ExecuteReader:
                            if (readerTestType != ReaderTestType.ReaderGC)
                                gch[i] = cmd[i].ExecuteReader();

                            switch (readerTestType)
                            {
                                case ReaderTestType.ReaderClose:
                                    {
                                        gch[i].Dispose();
                                        break;
                                    }
                                case ReaderTestType.ReaderDispose:
                                    gch[i].Dispose();
                                    break;
                                case ReaderTestType.ReaderGC:
                                    gch[i] = null;
                                    WeakReference weak = OpenReaderThenNullify(cmd[i]);
                                    GC.Collect();

                                    if (gcType == GCType.Wait)
                                    {
                                        GC.WaitForPendingFinalizers();
                                        Assert.False(weak.IsAlive, "Error - target still alive!");
                                    }
                                    break;
                                case ReaderTestType.ConnectionClose:
                                    GC.SuppressFinalize(gch[i]);
                                    con.Close();
                                    con.Open();
                                    break;
                                case ReaderTestType.NoCloses:
                                    GC.SuppressFinalize(gch[i]);
                                    break;
                            }
                            break;
                    }

                    if (readerTestType != ReaderTestType.NoCloses)
                    {
                        con.Close();
                        con.Open(); // Close and open, to re-assure collection!
                    }

                    SqlCommand verificationCmd = con.CreateCommand();

                    verificationCmd.CommandText = COMMAND_STATUS;
                    using (SqlDataReader rdr = verificationCmd.ExecuteReader())
                    {
                        rdr.Read();
                        int connections = (int)rdr.GetValue(0);
                        rdr.NextResult();
                        rdr.Read();
                        int requests = (int)rdr.GetValue(0);

                        switch (executeType)
                        {
                            case ExecuteType.ExecuteScalar:
                            case ExecuteType.ExecuteNonQuery:
                                // 1 for connection, 1 for command
                                Assert.True(connections == 2, "Failure - incorrect number of connections for ExecuteScalar! #connections: " + connections);

                                // only 1 executing
                                Assert.True(requests == 1, "Failure - incorrect number of requests for ExecuteScalar! #requests: " + requests);
                                break;
                            case ExecuteType.ExecuteReader:
                                switch (readerTestType)
                                {
                                    case ReaderTestType.ReaderClose:
                                    case ReaderTestType.ReaderDispose:
                                    case ReaderTestType.ConnectionClose:
                                        // 1 for connection, 1 for command
                                        Assert.True(connections == 2, "Failure - Incorrect number of connections for ReaderClose / ReaderDispose / ConnectionClose! #connections: " + connections);

                                        // only 1 executing
                                        Assert.True(requests == 1, "Failure - incorrect number of requests for ReaderClose/ReaderDispose/ConnectionClose! #requests: " + requests);
                                        break;
                                    case ReaderTestType.ReaderGC:
                                        switch (gcType)
                                        {
                                            case GCType.Wait:
                                                // 1 for connection, 1 for open reader
                                                Assert.True(connections == 2, "Failure - incorrect number of connections for ReaderGCWait! #connections: " + connections);
                                                // only 1 executing
                                                Assert.True(requests == 1, "Failure - incorrect number of requests for ReaderGCWait! #requests: " + requests);
                                                break;
                                            case GCType.NoWait:
                                                // 1 for connection, 1 for open reader
                                                Assert.True(connections == 2, "Failure - incorrect number of connections for ReaderGCNoWait! #connections: " + connections);

                                                // only 1 executing
                                                Assert.True(requests == 1, "Failure - incorrect number of requests for ReaderGCNoWait! #requests: " + requests);
                                                break;
                                        }
                                        break;
                                    case ReaderTestType.NoCloses:
                                        // 1 for connection, 1 for current command, 1 for 0 based array offset, plus i for open readers
                                        Assert.True(connections == (3 + i), "Failure - incorrect number of connections for NoCloses: " + connections);

                                        // 1 for current command, 1 for 0 based array offset, plus i open readers
                                        Assert.True(requests == (2 + i), "Failure - incorrect number of requests for NoCloses: " + requests);
                                        break;
                                }
                                break;
                        }
                    }
                }
            }
        }

        private static WeakReference OpenReaderThenNullify(SqlCommand command)
        {
            SqlDataReader reader = command.ExecuteReader();
            WeakReference weak = new WeakReference(reader);
            reader = null;
            return weak;
        }
    }
}
