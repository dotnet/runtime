// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Data.Common
{
    internal sealed class DbSchemaRow
    {
        internal const string SchemaMappingUnsortedIndex = "SchemaMapping Unsorted Index";
        private readonly DbSchemaTable _schemaTable;
        private readonly DataRow _dataRow;

        internal static DbSchemaRow[] GetSortedSchemaRows(DataTable dataTable, bool returnProviderSpecificTypes)
        {
            DataColumn? sortindex = dataTable.Columns[SchemaMappingUnsortedIndex];
            if (null == sortindex)
            {
                sortindex = new DataColumn(SchemaMappingUnsortedIndex, typeof(int));
                dataTable.Columns.Add(sortindex);
            }
            int count = dataTable.Rows.Count;
            for (int i = 0; i < count; ++i)
            {
                dataTable.Rows[i][sortindex] = i;
            };
            DbSchemaTable schemaTable = new DbSchemaTable(dataTable, returnProviderSpecificTypes);


            DataRow[] dataRows = SelectRows(dataTable, returnProviderSpecificTypes);
            Debug.Assert(null != dataRows, "GetSchemaRows: unexpected null dataRows");

            DbSchemaRow[] schemaRows = new DbSchemaRow[dataRows.Length];

            for (int i = 0; i < dataRows.Length; ++i)
            {
                schemaRows[i] = new DbSchemaRow(schemaTable, dataRows[i]);
            }
            return schemaRows;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Filter expression is null.")]
        private static DataRow[] SelectRows(DataTable dataTable, bool returnProviderSpecificTypes)
        {
            const DataViewRowState rowStates = DataViewRowState.Unchanged | DataViewRowState.Added | DataViewRowState.ModifiedCurrent;
            return dataTable.Select(null, "ColumnOrdinal ASC", rowStates);
        }

        internal DbSchemaRow(DbSchemaTable schemaTable, DataRow dataRow)
        {
            _schemaTable = schemaTable;
            _dataRow = dataRow;
        }

        internal DataRow DataRow
        {
            get
            {
                return _dataRow;
            }
        }

        internal string ColumnName
        {
            get
            {
                Debug.Assert(null != _schemaTable.ColumnName, "no column ColumnName");
                object value = _dataRow[_schemaTable.ColumnName, DataRowVersion.Default];
                if (!Convert.IsDBNull(value))
                {
                    return Convert.ToString(value, CultureInfo.InvariantCulture)!;
                }
                return string.Empty;
            }
        }

        internal int Size
        {
            get
            {
                Debug.Assert(null != _schemaTable.Size, "no column Size");
                object value = _dataRow[_schemaTable.Size, DataRowVersion.Default];
                if (!Convert.IsDBNull(value))
                {
                    return Convert.ToInt32(value, CultureInfo.InvariantCulture);
                }
                return 0;
            }
        }

        internal string BaseColumnName
        {
            get
            {
                if (null != _schemaTable.BaseColumnName)
                {
                    object value = _dataRow[_schemaTable.BaseColumnName, DataRowVersion.Default];
                    if (!Convert.IsDBNull(value))
                    {
                        return Convert.ToString(value, CultureInfo.InvariantCulture)!;
                    }
                }
                return string.Empty;
            }
        }

        internal string BaseServerName
        {
            get
            {
                if (null != _schemaTable.BaseServerName)
                {
                    object value = _dataRow[_schemaTable.BaseServerName, DataRowVersion.Default];
                    if (!Convert.IsDBNull(value))
                    {
                        return Convert.ToString(value, CultureInfo.InvariantCulture)!;
                    }
                }
                return string.Empty;
            }
        }


        internal string BaseCatalogName
        {
            get
            {
                if (null != _schemaTable.BaseCatalogName)
                {
                    object value = _dataRow[_schemaTable.BaseCatalogName, DataRowVersion.Default];
                    if (!Convert.IsDBNull(value))
                    {
                        return Convert.ToString(value, CultureInfo.InvariantCulture)!;
                    }
                }
                return string.Empty;
            }
        }

        internal string BaseSchemaName
        {
            get
            {
                if (null != _schemaTable.BaseSchemaName)
                {
                    object value = _dataRow[_schemaTable.BaseSchemaName, DataRowVersion.Default];
                    if (!Convert.IsDBNull(value))
                    {
                        return Convert.ToString(value, CultureInfo.InvariantCulture)!;
                    }
                }
                return string.Empty;
            }
        }

        internal string BaseTableName
        {
            get
            {
                if (null != _schemaTable.BaseTableName)
                {
                    object value = _dataRow[_schemaTable.BaseTableName, DataRowVersion.Default];
                    if (!Convert.IsDBNull(value))
                    {
                        return Convert.ToString(value, CultureInfo.InvariantCulture)!;
                    }
                }
                return string.Empty;
            }
        }

        internal bool IsAutoIncrement
        {
            get
            {
                if (null != _schemaTable.IsAutoIncrement)
                {
                    object value = _dataRow[_schemaTable.IsAutoIncrement, DataRowVersion.Default];
                    if (!Convert.IsDBNull(value))
                    {
                        return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                    }
                }
                return false;
            }
        }

        internal bool IsUnique
        {
            get
            {
                if (null != _schemaTable.IsUnique)
                {
                    object value = _dataRow[_schemaTable.IsUnique, DataRowVersion.Default];
                    if (!Convert.IsDBNull(value))
                    {
                        return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                    }
                }
                return false;
            }
        }

        internal bool IsRowVersion
        {
            get
            {
                if (null != _schemaTable.IsRowVersion)
                {
                    object value = _dataRow[_schemaTable.IsRowVersion, DataRowVersion.Default];
                    if (!Convert.IsDBNull(value))
                    {
                        return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                    }
                }
                return false;
            }
        }

        internal bool IsKey
        {
            get
            {
                if (null != _schemaTable.IsKey)
                {
                    object value = _dataRow[_schemaTable.IsKey, DataRowVersion.Default];
                    if (!Convert.IsDBNull(value))
                    {
                        return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                    }
                }
                return false;
            }
        }

        internal bool IsExpression
        {
            get
            {
                if (null != _schemaTable.IsExpression)
                {
                    object value = _dataRow[_schemaTable.IsExpression, DataRowVersion.Default];
                    if (!Convert.IsDBNull(value))
                    {
                        return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                    }
                }
                return false;
            }
        }

        internal bool IsHidden
        {
            get
            {
                if (null != _schemaTable.IsHidden)
                {
                    object value = _dataRow[_schemaTable.IsHidden, DataRowVersion.Default];
                    if (!Convert.IsDBNull(value))
                    {
                        return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                    }
                }
                return false;
            }
        }

        internal bool IsLong
        {
            get
            {
                if (null != _schemaTable.IsLong)
                {
                    object value = _dataRow[_schemaTable.IsLong, DataRowVersion.Default];
                    if (!Convert.IsDBNull(value))
                    {
                        return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                    }
                }
                return false;
            }
        }

        internal bool IsReadOnly
        {
            get
            {
                if (null != _schemaTable.IsReadOnly)
                {
                    object value = _dataRow[_schemaTable.IsReadOnly, DataRowVersion.Default];
                    if (!Convert.IsDBNull(value))
                    {
                        return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                    }
                }
                return false;
            }
        }

        internal Type? DataType
        {
            [RequiresUnreferencedCode("DataRow's DataType cannot be statically analyzed")]
            get
            {
                if (null != _schemaTable.DataType)
                {
                    object value = _dataRow[_schemaTable.DataType, DataRowVersion.Default];
                    if (!Convert.IsDBNull(value))
                    {
                        return (Type)value;
                    }
                }
                return null;
            }
        }

        internal bool AllowDBNull
        {
            get
            {
                if (null != _schemaTable.AllowDBNull)
                {
                    object value = _dataRow[_schemaTable.AllowDBNull, DataRowVersion.Default];
                    if (!Convert.IsDBNull(value))
                    {
                        return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                    }
                }
                return true;
            }
        }

        internal int UnsortedIndex
        {
            get
            {
                return (int)_dataRow[_schemaTable.UnsortedIndex!, DataRowVersion.Default];
            }
        }
    }
}
