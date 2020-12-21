// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System.Data
{
    public interface IColumnMappingCollection : IList
    {
        object this[string index] { get; set; }
        IColumnMapping Add(string sourceColumnName, string dataSetColumnName);
        bool Contains(string? sourceColumnName);
        IColumnMapping GetByDataSetColumn(string dataSetColumnName);
        int IndexOf(string? sourceColumnName);
        void RemoveAt(string sourceColumnName);
    }
}
