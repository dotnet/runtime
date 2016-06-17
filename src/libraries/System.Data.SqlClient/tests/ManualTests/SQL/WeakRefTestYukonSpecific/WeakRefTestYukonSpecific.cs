// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class WeakRefTestYukonSpecific
    {
        private const string COMMAND_TEXT_1 = "SELECT CustomerID, CompanyName, ContactName, ContactTitle, Address, City, Region, PostalCode, Country, Phone, Fax from Customers";
        private const string COMMAND_TEXT_2 = "SELECT CompanyName from Customers";
        private const string COLUMN_NAME_2 = "CompanyName";
        private const string DATABASE_NAME = "master";
        private const int CONCURRENT_COMMANDS = 5;

        [CheckConnStrSetupFact]
        public static void TestReaderMars()
        {
            string connectionString =
                (new SqlConnectionStringBuilder(DataTestUtility.TcpConnStr) 
                {
                    MultipleActiveResultSets = true,
                    MaxPoolSize = 1
                }).ConnectionString;

            TestReaderMarsCase("Case 1: ExecuteReader*5 Close, ExecuteReader.", connectionString, ReaderTestType.ReaderClose, ReaderVerificationType.ExecuteReader);
            TestReaderMarsCase("Case 2: ExecuteReader*5 Dispose, ExecuteReader.", connectionString, ReaderTestType.ReaderDispose, ReaderVerificationType.ExecuteReader);
            TestReaderMarsCase("Case 3: ExecuteReader*5 GC, ExecuteReader.", connectionString, ReaderTestType.ReaderGC, ReaderVerificationType.ExecuteReader);
            TestReaderMarsCase("Case 4: ExecuteReader*5 Connection Close, ExecuteReader.", connectionString, ReaderTestType.ConnectionClose, ReaderVerificationType.ExecuteReader);
            TestReaderMarsCase("Case 5: ExecuteReader*5 GC, Connection Close, ExecuteReader.", connectionString, ReaderTestType.ReaderGCConnectionClose, ReaderVerificationType.ExecuteReader);

            TestReaderMarsCase("Case 6: ExecuteReader*5 Close, ChangeDatabase.", connectionString, ReaderTestType.ReaderClose, ReaderVerificationType.ChangeDatabase);
            TestReaderMarsCase("Case 7: ExecuteReader*5 Dispose, ChangeDatabase.", connectionString, ReaderTestType.ReaderDispose, ReaderVerificationType.ChangeDatabase);
            TestReaderMarsCase("Case 8: ExecuteReader*5 GC, ChangeDatabase.", connectionString, ReaderTestType.ReaderGC, ReaderVerificationType.ChangeDatabase);
            TestReaderMarsCase("Case 9: ExecuteReader*5 Connection Close, ChangeDatabase.", connectionString, ReaderTestType.ConnectionClose, ReaderVerificationType.ChangeDatabase);
            TestReaderMarsCase("Case 10: ExecuteReader*5 GC, Connection Close, ChangeDatabase.", connectionString, ReaderTestType.ReaderGCConnectionClose, ReaderVerificationType.ChangeDatabase);

            TestReaderMarsCase("Case 11: ExecuteReader*5 Close, BeginTransaction.", connectionString, ReaderTestType.ReaderClose, ReaderVerificationType.BeginTransaction);
            TestReaderMarsCase("Case 12: ExecuteReader*5 Dispose, BeginTransaction.", connectionString, ReaderTestType.ReaderDispose, ReaderVerificationType.BeginTransaction);

            TestReaderMarsCase("Case 13: ExecuteReader*5 Connection Close, BeginTransaction.", connectionString, ReaderTestType.ConnectionClose, ReaderVerificationType.BeginTransaction);
            TestReaderMarsCase("Case 14: ExecuteReader*5 GC, Connection Close, BeginTransaction.", connectionString, ReaderTestType.ReaderGCConnectionClose, ReaderVerificationType.BeginTransaction);
        }

        [CheckConnStrSetupFact]
        public static void TestTransactionSingle()
        {
            string connectionString =
                (new SqlConnectionStringBuilder(DataTestUtility.TcpConnStr) 
                {
                    MultipleActiveResultSets = true,
                    MaxPoolSize = 1
                }).ConnectionString;

            TestTransactionSingleCase("Case 1: BeginTransaction, Rollback.", connectionString, TransactionTestType.TransactionRollback);
            TestTransactionSingleCase("Case 2: BeginTransaction, Dispose.", connectionString, TransactionTestType.TransactionDispose);
            TestTransactionSingleCase("Case 3: BeginTransaction, GC.", connectionString, TransactionTestType.TransactionGC);
            TestTransactionSingleCase("Case 4: BeginTransaction, Connection Close.", connectionString, TransactionTestType.ConnectionClose);
            TestTransactionSingleCase("Case 5: BeginTransaction, GC, Connection Close.", connectionString, TransactionTestType.TransactionGCConnectionClose);
        }

        private enum ReaderTestType
        {
            ReaderClose,
            ReaderDispose,
            ReaderGC,
            ConnectionClose,
            ReaderGCConnectionClose,
        }

        private enum ReaderVerificationType
        {
            ExecuteReader,
            ChangeDatabase,
            BeginTransaction,
            EnlistDistributedTransaction,
        }

        private enum TransactionTestType
        {
            TransactionRollback,
            TransactionDispose,
            TransactionGC,
            ConnectionClose,
            TransactionGCConnectionClose,
        }

        public static int GCCount = 0;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void TestReaderMarsCase(string caseName, string connectionString, ReaderTestType testType, ReaderVerificationType verificationType)
        {
            WeakReference weak = null;
            SqlCommand[] cmd = new SqlCommand[CONCURRENT_COMMANDS];
            SqlDataReader[] gch = new SqlDataReader[CONCURRENT_COMMANDS];

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                for (int i = 0; i < CONCURRENT_COMMANDS; i++)
                {
                    cmd[i] = con.CreateCommand();
                    cmd[i].CommandText = COMMAND_TEXT_1;
                    if ((testType != ReaderTestType.ReaderGC) && (testType != ReaderTestType.ReaderGCConnectionClose))
                        gch[i] = cmd[i].ExecuteReader();
                    else
                        gch[i] = null;
                }

                for (int i = 0; i < CONCURRENT_COMMANDS; i++)
                {
                    switch (testType)
                    {
                        case ReaderTestType.ReaderClose:
                            gch[i].Dispose();
                            break;

                        case ReaderTestType.ReaderDispose:
                            gch[i].Dispose();
                            break;

                        case ReaderTestType.ReaderGC:
                            weak = OpenNullifyReader(cmd[i]);
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                            Assert.False(weak.IsAlive, "Transaction is still alive on TestReaderMars: ReaderGC");
                            break;

                        case ReaderTestType.ConnectionClose:
                            GC.SuppressFinalize(gch[i]);
                            con.Close();
                            con.Open();
                            break;

                        case ReaderTestType.ReaderGCConnectionClose:
                            weak = OpenNullifyReader(cmd[i]);
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                            Assert.False(weak.IsAlive, "Transaction is still alive on TestReaderMars: ReaderGCConnectionClose");
                            con.Close();
                            con.Open();
                            break;
                    }

                    cmd[i].Dispose();
                }

                SqlCommand verificationCmd = con.CreateCommand();

                switch (verificationType)
                {
                    case ReaderVerificationType.ExecuteReader:
                        verificationCmd.CommandText = COMMAND_TEXT_2;
                        using (SqlDataReader rdr = verificationCmd.ExecuteReader())
                        {
                            rdr.Read();
                            DataTestUtility.AssertEqualsWithDescription(1, rdr.FieldCount, "Execute Reader should return expected Field count");
                            DataTestUtility.AssertEqualsWithDescription(COLUMN_NAME_2, rdr.GetName(0), "Execute Reader should return expected Field name");
                        }
                        break;

                    case ReaderVerificationType.ChangeDatabase:
                        con.ChangeDatabase(DATABASE_NAME);
                        DataTestUtility.AssertEqualsWithDescription(DATABASE_NAME, con.Database, "Change Database should return expected Database Name");
                        break;

                    case ReaderVerificationType.BeginTransaction:
                        verificationCmd.Transaction = con.BeginTransaction();
                        verificationCmd.CommandText = "select @@trancount";
                        int tranCount = (int)verificationCmd.ExecuteScalar();
                        DataTestUtility.AssertEqualsWithDescription(1, tranCount, "Begin Transaction should return expected Transaction count");
                        break;
                }

                verificationCmd.Dispose();
            }
        }

        private static WeakReference OpenNullifyReader(SqlCommand command)
        {
            SqlDataReader reader = command.ExecuteReader();
            WeakReference weak = new WeakReference(reader);
            reader = null;
            return weak;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void TestTransactionSingleCase(string caseName, string connectionString, TransactionTestType testType)
        {
            WeakReference weak = null;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                SqlTransaction gch = null;
                if ((testType != TransactionTestType.TransactionGC) && (testType != TransactionTestType.TransactionGCConnectionClose))
                    gch = con.BeginTransaction();

                switch (testType)
                {
                    case TransactionTestType.TransactionRollback:
                        gch.Rollback();
                        break;

                    case TransactionTestType.TransactionDispose:
                        gch.Dispose();
                        break;

                    case TransactionTestType.TransactionGC:
                        weak = OpenNullifyTransaction(con);
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        Assert.False(weak.IsAlive, "Transaction is still alive on TestTransactionSingle: TransactionGC");
                        break;

                    case TransactionTestType.ConnectionClose:
                        GC.SuppressFinalize(gch);
                        con.Close();
                        con.Open();
                        break;

                    case TransactionTestType.TransactionGCConnectionClose:
                        weak = OpenNullifyTransaction(con);
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        Assert.False(weak.IsAlive, "Transaction is still alive on TestTransactionSingle: TransactionGCConnectionClose");
                        con.Close();
                        con.Open();
                        break;
                }

                using (SqlCommand cmd = con.CreateCommand())
                {
                    cmd.CommandText = "select @@trancount";
                    int tranCount = (int)cmd.ExecuteScalar();
                    DataTestUtility.AssertEqualsWithDescription(0, tranCount, "TransactionSingle Case " + caseName + " should return expected trans count");
                }
            }
        }

        private static WeakReference OpenNullifyTransaction(SqlConnection connection)
        {
            SqlTransaction transaction = connection.BeginTransaction();
            WeakReference weak = new WeakReference(transaction);
            transaction = null;
            return weak;
        }
    }
}
