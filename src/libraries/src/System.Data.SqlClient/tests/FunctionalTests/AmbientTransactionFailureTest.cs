// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using System.Transactions;
using Xunit;

namespace System.Data.SqlClient.Tests
{
    public class AmbientTransactionFailureTest
    {

        private static readonly string s_servername = Guid.NewGuid().ToString();
        private static readonly string s_connectionStringWithEnlistAsDefault = $"Data Source={s_servername}; Integrated Security=true; Connect Timeout=1;";
        private static readonly string s_connectionStringWithEnlistOff = $"Data Source={s_servername}; Integrated Security=true; Connect Timeout=1;Enlist=False";

        private static Action<string> ConnectToServer = (connectionString) =>
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
            }
        };

        private static Action<string> ConnectToServerTask = (connectionString) =>
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.OpenAsync();
            }
        };

        private static Func<string, Task> ConnectToServerInTransactionScopeTask = (connectionString) =>
        {
            return Task.Run(() =>
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    ConnectToServerTask(connectionString);
                }
            });
        };

        private static Action<string> ConnectToServerInTransactionScope = (connectionString) =>
        {
            using (TransactionScope scope = new TransactionScope())
            {
                ConnectToServer(connectionString);
            }
        };

        private static Action<string> EnlistConnectionInTransaction = (connectionString) =>
        {
            using (TransactionScope scope = new TransactionScope())
            {
                SqlConnection connection = new SqlConnection(connectionString);
                connection.EnlistTransaction(Transaction.Current);
            }
        };

        public static readonly object[][] ExceptionTestDataForSqlException =
        {
            new object[] { ConnectToServerInTransactionScope, s_connectionStringWithEnlistOff },
            new object[] { ConnectToServer, s_connectionStringWithEnlistAsDefault }
        };

        public static readonly object[][] ExceptionTestDataForNotSupportedException =
        {
            new object[] { ConnectToServerInTransactionScope, s_connectionStringWithEnlistAsDefault },
            new object[] { EnlistConnectionInTransaction, s_connectionStringWithEnlistAsDefault },
            new object[] { EnlistConnectionInTransaction, s_connectionStringWithEnlistOff }
        };

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotArmProcess))] // https://github.com/dotnet/corefx/issues/21598
        [MemberData(nameof(ExceptionTestDataForSqlException))]
        public void TestSqlException(Action<string> connectAction, string connectionString)
        {
            Assert.Throws<SqlException>(() =>
            {
                connectAction(connectionString);
            });
        }
    }
}
