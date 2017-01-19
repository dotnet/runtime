// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data.Common
{
    internal sealed class DbSchemaTable
    {
        private enum ColumnEnum
        {
            ColumnName,
            ColumnOrdinal,
            ColumnSize,
            BaseServerName,
            BaseCatalogName,
            BaseColumnName,
            BaseSchemaName,
            BaseTableName,
            IsAutoIncrement,
            IsUnique,
            IsKey,
            IsRowVersion,
            DataType,
            ProviderSpecificDataType,
            AllowDBNull,
            ProviderType,
            IsExpression,
            IsHidden,
            IsLong,
            IsReadOnly,
            SchemaMappingUnsortedIndex,
        }

        private static readonly string[] s_DBCOLUMN_NAME = new string[] {
            SchemaTableColumn.ColumnName,
            SchemaTableColumn.ColumnOrdinal,
            SchemaTableColumn.ColumnSize,
            SchemaTableOptionalColumn.BaseServerName,
            SchemaTableOptionalColumn.BaseCatalogName,
            SchemaTableColumn.BaseColumnName,
            SchemaTableColumn.BaseSchemaName,
            SchemaTableColumn.BaseTableName,
            SchemaTableOptionalColumn.IsAutoIncrement,
            SchemaTableColumn.IsUnique,
            SchemaTableColumn.IsKey,
            SchemaTableOptionalColumn.IsRowVersion,
            SchemaTableColumn.DataType,
            SchemaTableOptionalColumn.ProviderSpecificDataType,
            SchemaTableColumn.AllowDBNull,
            SchemaTableColumn.ProviderType,
            SchemaTableColumn.IsExpression,
            SchemaTableOptionalColumn.IsHidden,
            SchemaTableColumn.IsLong,
            SchemaTableOptionalColumn.IsReadOnly,
            DbSchemaRow.SchemaMappingUnsortedIndex,
        };

        internal DataTable _dataTable;
        private DataColumnCollection _columns;
        private DataColumn[] _columnCache = new DataColumn[s_DBCOLUMN_NAME.Length];
        private bool _returnProviderSpecificTypes;

        internal DbSchemaTable(DataTable dataTable, bool returnProviderSpecificTypes)
        {
            _dataTable = dataTable;
            _columns = dataTable.Columns;
            _returnProviderSpecificTypes = returnProviderSpecificTypes;
        }

        internal DataColumn ColumnName { get { return CachedDataColumn(ColumnEnum.ColumnName); } }
        internal DataColumn Size { get { return CachedDataColumn(ColumnEnum.ColumnSize); } }
        internal DataColumn BaseServerName { get { return CachedDataColumn(ColumnEnum.BaseServerName); } }
        internal DataColumn BaseColumnName { get { return CachedDataColumn(ColumnEnum.BaseColumnName); } }
        internal DataColumn BaseTableName { get { return CachedDataColumn(ColumnEnum.BaseTableName); } }
        internal DataColumn BaseCatalogName { get { return CachedDataColumn(ColumnEnum.BaseCatalogName); } }
        internal DataColumn BaseSchemaName { get { return CachedDataColumn(ColumnEnum.BaseSchemaName); } }
        internal DataColumn IsAutoIncrement { get { return CachedDataColumn(ColumnEnum.IsAutoIncrement); } }
        internal DataColumn IsUnique { get { return CachedDataColumn(ColumnEnum.IsUnique); } }
        internal DataColumn IsKey { get { return CachedDataColumn(ColumnEnum.IsKey); } }
        internal DataColumn IsRowVersion { get { return CachedDataColumn(ColumnEnum.IsRowVersion); } }

        internal DataColumn AllowDBNull { get { return CachedDataColumn(ColumnEnum.AllowDBNull); } }
        internal DataColumn IsExpression { get { return CachedDataColumn(ColumnEnum.IsExpression); } }
        internal DataColumn IsHidden { get { return CachedDataColumn(ColumnEnum.IsHidden); } }
        internal DataColumn IsLong { get { return CachedDataColumn(ColumnEnum.IsLong); } }
        internal DataColumn IsReadOnly { get { return CachedDataColumn(ColumnEnum.IsReadOnly); } }

        internal DataColumn UnsortedIndex { get { return CachedDataColumn(ColumnEnum.SchemaMappingUnsortedIndex); } }

        internal DataColumn DataType
        {
            get
            {
                if (_returnProviderSpecificTypes)
                {
                    return CachedDataColumn(ColumnEnum.ProviderSpecificDataType, ColumnEnum.DataType);
                }
                return CachedDataColumn(ColumnEnum.DataType);
            }
        }

        private DataColumn CachedDataColumn(ColumnEnum column)
        {
            return CachedDataColumn(column, column);
        }

        private DataColumn CachedDataColumn(ColumnEnum column, ColumnEnum column2)
        {
            DataColumn dataColumn = _columnCache[(int)column];
            if (null == dataColumn)
            {
                int index = _columns.IndexOf(s_DBCOLUMN_NAME[(int)column]);
                if ((-1 == index) && (column != column2))
                {
                    index = _columns.IndexOf(s_DBCOLUMN_NAME[(int)column2]);
                }
                if (-1 != index)
                {
                    dataColumn = _columns[index];
                    _columnCache[(int)column] = dataColumn;
                }
            }
            return dataColumn;
        }
    }
}
