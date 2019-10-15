// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data
{
    public interface IDbConnection : IDisposable
    {
        string ConnectionString { get; set; }
        int ConnectionTimeout { get; }
        string Database { get; }
        ConnectionState State { get; }
        IDbTransaction BeginTransaction();
        IDbTransaction BeginTransaction(IsolationLevel il);
        void Close();
        void ChangeDatabase(string databaseName);
        IDbCommand CreateCommand();
        void Open();
    }
}
