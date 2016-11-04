// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Data.Common
{
    internal class DataRowDbColumn : DbColumn
    {
        private DataColumnCollection _schemaColumns;
        private DataRow _schemaRow;

        public DataRowDbColumn(DataRow readerSchemaRow, DataColumnCollection readerSchemaColumns)
        {
            _schemaRow = readerSchemaRow;
            _schemaColumns = readerSchemaColumns;
            populateFields();
        }

        private void populateFields()
        {
            AllowDBNull = GetDbColumnValue<bool?>(SchemaTableColumn.AllowDBNull);
            BaseCatalogName = GetDbColumnValue<string>(SchemaTableOptionalColumn.BaseCatalogName);
            BaseColumnName = GetDbColumnValue<string>(SchemaTableColumn.BaseColumnName);
            BaseSchemaName = GetDbColumnValue<string>(SchemaTableColumn.BaseSchemaName);
            BaseServerName = GetDbColumnValue<string>(SchemaTableOptionalColumn.BaseServerName);
            BaseTableName = GetDbColumnValue<string>(SchemaTableColumn.BaseTableName);
            ColumnName = GetDbColumnValue<string>(SchemaTableColumn.ColumnName);
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
            UdtAssemblyQualifiedName = GetDbColumnValue<string>("UdtAssemblyQualifiedName");
            DataType = GetDbColumnValue<Type>(SchemaTableColumn.DataType);
            DataTypeName = GetDbColumnValue<string>("DataTypeName");
        }

        private T GetDbColumnValue<T>(string columnName)
        {
            if (!_schemaColumns.Contains(columnName))
            {
                return default(T);
            }

            object schemaObject = _schemaRow[columnName];
            if (schemaObject is T)
            {
                return (T)schemaObject;
            }
            return default(T);
        }
    }

    public static class DbDataReaderExtensions
    {
        public static System.Collections.ObjectModel.ReadOnlyCollection<DbColumn> GetColumnSchema(this DbDataReader reader)
        {
            IList<DbColumn> columnSchema = new List<DbColumn>();
            DataTable schemaTable = reader.GetSchemaTable();
            DataColumnCollection schemaTableColumns = schemaTable.Columns;
            foreach (DataRow row in schemaTable.Rows)
            {
                DbColumn dbColumn = new DataRowDbColumn(row, schemaTableColumns);
                columnSchema.Add(dbColumn);
            }
            System.Collections.ObjectModel.ReadOnlyCollection<DbColumn> readOnlyColumnSchema = new System.Collections.ObjectModel.ReadOnlyCollection<DbColumn>(columnSchema);
            return readOnlyColumnSchema;
        }

        public static bool CanGetColumnSchema(this DbDataReader reader)
        {
            return true;
        }
    }
}
