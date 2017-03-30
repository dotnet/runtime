// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data
{
    public interface IDbCommand : IDisposable
    {
        IDbConnection Connection { get; set; }
        IDbTransaction Transaction { get; set; }
        string CommandText { get; set; }
        int CommandTimeout { get; set; }
        CommandType CommandType { get; set; }
        IDataParameterCollection Parameters { get; }
        void Prepare();
        UpdateRowSource UpdatedRowSource { get; set; }
        void Cancel();
        IDbDataParameter CreateParameter();
        int ExecuteNonQuery();
        IDataReader ExecuteReader();
        IDataReader ExecuteReader(CommandBehavior behavior);
        object ExecuteScalar();
    }
}
