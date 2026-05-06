// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Data
{
    public interface ITableMapping
    {
        IColumnMappingCollection ColumnMappings { get; }
        string DataSetTable { get; set; }
        string SourceTable { get; set; }
    }
}
