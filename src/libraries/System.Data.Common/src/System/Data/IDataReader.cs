// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Data
{
    public interface IDataReader : IDisposable, IDataRecord
    {
        int Depth { get; }
        bool IsClosed { get; }
        int RecordsAffected { get; }
        void Close();
        DataTable? GetSchemaTable();
        bool NextResult();
        bool Read();
    }
}
