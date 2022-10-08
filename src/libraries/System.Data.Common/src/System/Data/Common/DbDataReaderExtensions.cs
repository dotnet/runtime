// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Data.Common
{
    public static class DbDataReaderExtensions
    {
        private sealed class DataRowDbColumn : DbColumn
        {
            private readonly DataColumnCollection _schemaColumns;
            private readonly DataRow _schemaRow;

            public DataRowDbColumn(DataRow readerSchemaRow, DataColumnCollection readerSchemaColumns)
            {
                _schemaRow = readerSchemaRow;
                _schemaColumns = readerSchemaColumns;
                PopulateFields();
            }

            private void PopulateFields()
            {
                AllowDBNull = GetDbColumnValue<bool?>(SchemaTableColumn.AllowDBNull);
                BaseCatalogName = GetDbColumnValue<string?>(SchemaTableOptionalColumn.BaseCatalogName);
                BaseColumnName = GetDbColumnValue<string?>(SchemaTableColumn.BaseColumnName);
                BaseSchemaName = GetDbColumnValue<string?>(SchemaTableColumn.BaseSchemaName);
                BaseServerName = GetDbColumnValue<string?>(SchemaTableOptionalColumn.BaseServerName);
                BaseTableName = GetDbColumnValue<string?>(SchemaTableColumn.BaseTableName);
                ColumnName = GetDbColumnValue<string>(SchemaTableColumn.ColumnName)!;
                ColumnOrdinal = GetDbColumnValue<int?>(SchemaTableColumn.ColumnOrdinal);
                ColumnSize = GetDbColumnValue<int?>(SchemaTableColumn.ColumnSize);
                IsAliased = GetDbColumnValue<bool?>(SchemaTableColumn.IsAliased);
                IsAutoIncrement = GetDbColumnValue<bool?>(SchemaTableOptionalColumn.IsAutoIncrement);
                IsExpression = GetDbColumnValue<bool>(SchemaTableColumn.IsExpression);
                IsHidden = GetDbColumnValue<bool?>(SchemaTableOptionalColumn.IsHidden);
                IsIdentity = GetDbColumnValue<bool?>("IsIdentity");
                IsKey = GetDbColumnValue<bool?>(SchemaTableColumn.IsKey);
                IsLong = GetDbColumnValue<bool?>(SchemaTableColumn.IsLong);
                IsReadOnly = GetDbColumnValue<bool?>(SchemaTableOptionalColumn.IsReadOnly);
                IsUnique = GetDbColumnValue<bool?>(SchemaTableColumn.IsUnique);
                NumericPrecision = GetDbColumnValue<int?>(SchemaTableColumn.NumericPrecision);
                NumericScale = GetDbColumnValue<int?>(SchemaTableColumn.NumericScale);
                UdtAssemblyQualifiedName = GetDbColumnValue<string?>("UdtAssemblyQualifiedName");
                DataType = GetDbColumnValue<Type?>(SchemaTableColumn.DataType);
                DataTypeName = GetDbColumnValue<string?>("DataTypeName");
            }

            private T? GetDbColumnValue<T>(string columnName) => _schemaColumns.Contains(columnName) && _schemaRow[columnName] is T value ? value : default;
        }

        public static ReadOnlyCollection<DbColumn> GetColumnSchema(this DbDataReader reader)
        {
            ArgumentNullException.ThrowIfNull(reader);

            if (reader is IDbColumnSchemaGenerator schemaGenerator)
            {
                return schemaGenerator.GetColumnSchema();
            }
            return GetColumnSchemaCompatibility(reader);
        }

        public static bool CanGetColumnSchema(this DbDataReader reader)
        {
            return true;
        }

        private static ReadOnlyCollection<DbColumn> GetColumnSchemaCompatibility(DbDataReader reader)
        {
            var columnSchema = new List<DbColumn>();
            DataTable? schemaTable = reader.GetSchemaTable();
            if (schemaTable != null)
            {
                DataColumnCollection schemaTableColumns = schemaTable.Columns;
                foreach (DataRow row in schemaTable.Rows)
                {
                    Debug.Assert(row != null);
                    columnSchema.Add(new DataRowDbColumn(row, schemaTableColumns));
                }
            }
            return new ReadOnlyCollection<DbColumn>(columnSchema);
        }
    }
}
