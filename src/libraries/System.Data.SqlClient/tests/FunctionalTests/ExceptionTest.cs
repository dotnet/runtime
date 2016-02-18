// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Collections;
using Xunit;

namespace System.Data.SqlClient.Tests 
{
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
            Assert.True(ex.Message.Contains(expectedExceptionMessage), string.Format("FAILED SqlException did not contain expected error message. Actual message: {0}", ex.Message));
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

            // Verify that there are the correct number of errors in the exception
            Assert.True(exception.Errors.Count == count, string.Format("FAILED Incorrect number of errors. Expected: {0}. Actual: {1}.", count, exception.Errors.Count));

            // Ensure that all errors have an error-level severity
            for (int i = 0; i < count; i++)
            {
                Assert.True(exception.Errors[i].Class >= 10, "FAILED verification of Exception!  Exception contains a warning!");
            }

            // Check the properties of the exception populated by the server are correct
            if (errorNumber.HasValue)
            {
                Assert.True(errorNumber.Value == exception.Number, string.Format("FAILED Error number of exception is incorrect. Expected: {0}. Actual: {1}.", errorNumber.Value, exception.Number));
            }

            if (errorState.HasValue)
            {
                Assert.True(errorState.Value == exception.State, string.Format("FAILED Error state of exception is incorrect. Expected: {0}. Actual: {1}.", errorState.Value, exception.State));
            }

            if (severity.HasValue)
            {
                Assert.True(severity.Value == exception.Class, string.Format("FAILED Severity of exception is incorrect. Expected: {0}. Actual: {1}.", severity.Value, exception.Class));
            }

            if ((errorNumber.HasValue) && (errorState.HasValue) && (severity.HasValue))
            {
                string detailsText = string.Format("Error Number:{0},State:{1},Class:{2}", errorNumber.Value, errorState.Value, severity.Value);
                Assert.True(exception.ToString().Contains(detailsText), string.Format("FAILED SqlException.ToString does not contain the error number, state and severity information"));
            }

            // verify that the this[] function on the collection works, as well as the All function
            SqlError[] errors = new SqlError[exception.Errors.Count];
            exception.Errors.CopyTo(errors, 0);
            Assert.True((errors[0].Message).Equals(exception.Errors[0].Message), string.Format("FAILED verification of Exception! ErrorCollection indexer/CopyTo resulted in incorrect value."));

            return true;
        }

    }
}
