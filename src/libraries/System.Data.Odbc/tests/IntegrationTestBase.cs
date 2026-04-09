// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.XUnitExtensions;

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
            try
            {
                connection.Open();
            }
            catch (OdbcException e) when (e.ErrorCode == unchecked((int)0x80131937)) // Data source name not found and no default driver specified
            {
                throw new SkipTestException(e.Message);
            }
            catch (DllNotFoundException e)
            {
                throw new SkipTestException(e.Message);
            }

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
