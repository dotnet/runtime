// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Data
{
    public interface IDataParameter
    {
        DbType DbType { get; set; }
        ParameterDirection Direction { get; set; }
        bool IsNullable { get; }
        [AllowNull]
        string ParameterName { get; set; }
        [AllowNull]
        string SourceColumn { get; set; }
        DataRowVersion SourceVersion { get; set; }
        object? Value { get; set; }
    }
}
