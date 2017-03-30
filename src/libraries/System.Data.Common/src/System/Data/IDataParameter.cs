// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data
{
    public interface IDataParameter
    {
        DbType DbType { get; set; }
        ParameterDirection Direction { get; set; }
        bool IsNullable { get; }
        string ParameterName { get; set; }
        string SourceColumn { get; set; }
        DataRowVersion SourceVersion { get; set; }
        object Value { get; set; }
    }
}
