using Xunit;

namespace System.Data.Odbc.Tests
{
    public abstract class IntegrationTestBase : IDisposable
    {
        protected readonly OdbcConnection connection;
        protected readonly OdbcTransaction transaction;
        protected readonly OdbcCommand command;

        public IntegrationTestBase()
        {
            connection = new OdbcConnection(ConnectionStrings.WorkingConnection);
            connection.Open();
            transaction = connection.BeginTransaction();
            command = connection.CreateCommand();
            command.Transaction = transaction;
        }

        public void Dispose()
        {
            command.Dispose();
            transaction.Dispose();
            connection.Dispose();
        }
    }
}
