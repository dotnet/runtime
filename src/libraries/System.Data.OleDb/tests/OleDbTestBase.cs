// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;
using Xunit;

namespace System.Data.OleDb.Tests
{
    public abstract class OleDbTestBase : FileCleanupTestBase, IDisposable
    {
        protected OleDbConnection connection;
        protected OleDbTransaction transaction;
        protected OleDbCommand command;
        private bool _disposed;
        

        public OleDbTestBase()
        {
            _disposed = false;
            connection = new OleDbConnection(ConnectionString);
            // Make 3 attempts
            string failure = string.Empty;
            for (int i = 0; i <= 2; i++)
            {
                try
                {
                    connection.Open();
                    break;
                }
                catch (Exception ex)
                {
                    failure += ex.ToString() + Environment.NewLine;
                    Thread.Sleep(10); // Give a transient condition like antivirus/indexing a chance to go away
                }
            }
            Assert.True(ConnectionState.Open == connection.State, $"{nameof(OleDbTestBase)} failed to open {nameof(OleDbConnection)}. {failure}");
            transaction = connection.BeginTransaction();
            command = connection.CreateCommand();
            command.Transaction = transaction;
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                if (command != null)
                {
                    command.Dispose();
                    command = null;
                }
                if (transaction != null)
                {
                    transaction.Dispose();
                    transaction = null;
                }
                if (connection != null)
                {
                    connection.Close();
                    connection.Dispose();
                    connection = null;
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        protected string ConnectionString => @"Provider=" + Helpers.ProviderName + @";Data source=" + TestDirectory + @";Extended Properties=""Text;HDR=No;FMT=Delimited""";
    }
}
