// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

using Xunit;

namespace System.Data.SqlClient.Tests
{
    [OuterLoop("Takes minutes on some networks")]
    public class ExceptionTest
    {
        // test connection string
        private string connectionString = "server=tcp:server,1432;database=test;uid=admin;pwd=SQLDB;connect timeout=60;";

        // data value and server consts
        private const string badServer = "NotAServer";
        private const string sqlsvrBadConn = "A network-related or instance-specific error occurred while establishing a connection to SQL Server. The server was not found or was not accessible. Verify that the instance name is correct and that SQL Server is configured to allow remote connections.";
        private const string execReaderFailedMessage = "ExecuteReader requires an open and available Connection. The connection's current state is closed.";
        private const string orderIdQuery = "select orderid from orders where orderid < 10250";

        [Fact]
        public void ExceptionTests()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);

            // tests improper server name thrown from constructor of tdsparser
            SqlConnectionStringBuilder badBuilder = new SqlConnectionStringBuilder(builder.ConnectionString) { DataSource = badServer, ConnectTimeout = 1 };

            VerifyConnectionFailure<SqlException>(() => GenerateConnectionException(badBuilder.ConnectionString), sqlsvrBadConn, VerifyException);
        }

        [Fact]
        public void VariousExceptionTests()
        {
            // Test exceptions - makes sure they are only thrown from upper layers
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);

            SqlConnectionStringBuilder badBuilder = new SqlConnectionStringBuilder(builder.ConnectionString) { DataSource = badServer, ConnectTimeout = 1 };
            using (var sqlConnection = new SqlConnection(badBuilder.ConnectionString))
            {
                using (SqlCommand command = sqlConnection.CreateCommand())
                {
                    command.CommandText = orderIdQuery;
                    VerifyConnectionFailure<InvalidOperationException>(() => command.ExecuteReader(), execReaderFailedMessage);
                }
            }
        }

        [Fact]
        public void IndependentConnectionExceptionTestOpenConnection()
        {
            // Test exceptions for existing connection to ensure proper exception and call stack
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);

            SqlConnectionStringBuilder badBuilder = new SqlConnectionStringBuilder(builder.ConnectionString) { DataSource = badServer, ConnectTimeout = 1 };
            using (var sqlConnection = new SqlConnection(badBuilder.ConnectionString))
            {
                VerifyConnectionFailure<SqlException>(() => sqlConnection.Open(), sqlsvrBadConn, VerifyException);
            }
        }

        [Fact]
        public void IndependentConnectionExceptionTestExecuteReader()
        {
            // Test exceptions for existing connection to ensure proper exception and call stack
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);

            SqlConnectionStringBuilder badBuilder = new SqlConnectionStringBuilder(builder.ConnectionString) { DataSource = badServer, ConnectTimeout = 1 };
            using (var sqlConnection = new SqlConnection(badBuilder.ConnectionString))
            {
                using (SqlCommand command = new SqlCommand(orderIdQuery, sqlConnection))
                {
                    VerifyConnectionFailure<InvalidOperationException>(() => command.ExecuteReader(), execReaderFailedMessage);
                }
            }
        }

        private void GenerateConnectionException(string connectionString)
        {
            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();
                using (SqlCommand command = sqlConnection.CreateCommand())
                {
                    command.CommandText = orderIdQuery;
                    command.ExecuteReader();
                }
            }
        }

        private TException VerifyConnectionFailure<TException>(Action connectAction, string expectedExceptionMessage, Func<TException, bool> exVerifier) where TException : Exception
        {
            TException ex = Assert.Throws<TException>(connectAction);
            Assert.Contains(expectedExceptionMessage, ex.Message);
            Assert.True(exVerifier(ex), "FAILED Exception verifier failed on the exception.");

            return ex;
        }

        private TException VerifyConnectionFailure<TException>(Action connectAction, string expectedExceptionMessage) where TException : Exception
        {
            return VerifyConnectionFailure<TException>(connectAction, expectedExceptionMessage, (ex) => true);
        }

        private bool VerifyException(SqlException exception)
        {
            VerifyException(exception, 1);
            return true;
        }

        private bool VerifyException(SqlException exception, int count, int? errorNumber = null, int? errorState = null, int? severity = null)
        {
            Assert.NotEmpty(exception.Errors);
            Assert.Equal(count, exception.Errors.Count);

            // Ensure that all errors have an error-level severity
            for (int i = 0; i < count; i++)
            {
                Assert.InRange(exception.Errors[i].Class, 10, byte.MaxValue);
            }

            // Check the properties of the exception populated by the server are correct
            if (errorNumber.HasValue)
            {
                Assert.Equal(errorNumber.Value, exception.Number);
            }

            if (errorState.HasValue)
            {
                Assert.Equal(errorState.Value, exception.State);
            }

            if (severity.HasValue)
            {
                Assert.Equal(severity.Value, exception.Class);
            }

            if ((errorNumber.HasValue) && (errorState.HasValue) && (severity.HasValue))
            {
                string expected = $"Error Number:{errorNumber.Value},State:{errorState.Value},Class:{severity.Value}";
                Assert.Contains(expected, exception.ToString());
            }

            return true;
        }
    }
}
