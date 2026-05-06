// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Data
{
    public interface IDataAdapter
    {
        MissingMappingAction MissingMappingAction { get; set; }
        MissingSchemaAction MissingSchemaAction { get; set; }
        ITableMappingCollection TableMappings { get; }
        [RequiresUnreferencedCode("IDataReader's (built from adapter commands) schema table types cannot be statically analyzed.")]
        DataTable[] FillSchema(DataSet dataSet, SchemaType schemaType);
        int Fill(DataSet dataSet);
        IDataParameter[] GetFillParameters();
        [RequiresUnreferencedCode("IDataReader's (built from adapter commands) schema table types cannot be statically analyzed.")]
        int Update(DataSet dataSet);
    }
}
