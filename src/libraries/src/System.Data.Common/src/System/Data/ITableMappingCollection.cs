// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.Data
{
    public interface ITableMappingCollection : IList
    {
        object this[string index] { get; set; }
        ITableMapping Add(string sourceTableName, string dataSetTableName);
        bool Contains(string sourceTableName);
        ITableMapping GetByDataSetTable(string dataSetTableName);
        int IndexOf(string sourceTableName);
        void RemoveAt(string sourceTableName);
    }
}
