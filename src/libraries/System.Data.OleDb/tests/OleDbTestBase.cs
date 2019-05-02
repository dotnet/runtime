// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Data.OleDb;
using System.IO;

namespace System.Data.OleDb.Tests
{
    public abstract class OleDbTestBase : FileCleanupTestBase, IDisposable
    {
        protected readonly OleDbConnection connection;
        protected readonly OleDbTransaction transaction;
        protected readonly OleDbCommand command;
        private bool _disposed;
        

        public OleDbTestBase()
        {
            _disposed = false;
            connection = new OleDbConnection(ConnectionString);
            connection.Open();
            transaction = connection.BeginTransaction();
            command = connection.CreateCommand();
            command.Transaction = transaction;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    command.Dispose();
                    transaction.Dispose();
                    connection.Dispose();
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        protected string ConnectionString => @"Provider=" + Helpers.ProviderName + @";Data source=" + TestDirectory + @";Extended Properties=""Text;HDR=No;FMT=Delimited""";
    }
}
