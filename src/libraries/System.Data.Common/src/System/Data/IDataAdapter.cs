// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data
{
    public interface IDataAdapter
    {
        MissingMappingAction MissingMappingAction { get; set; }
        MissingSchemaAction MissingSchemaAction { get; set; }
        ITableMappingCollection TableMappings { get; }
        DataTable[] FillSchema(DataSet dataSet, SchemaType schemaType);
        int Fill(DataSet dataSet);
        IDataParameter[] GetFillParameters();
        int Update(DataSet dataSet);
    }
}
