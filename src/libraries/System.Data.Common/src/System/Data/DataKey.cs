// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Data
{
    internal readonly struct DataKey
    {
        private const int maxColumns = 32;

        private readonly DataColumn[] _columns;

        internal DataKey(DataColumn[] columns, bool copyColumns)
        {
            if (columns == null)
            {
                throw ExceptionBuilder.ArgumentNull(nameof(columns));
            }

            if (columns.Length == 0)
            {
                throw ExceptionBuilder.KeyNoColumns();
            }

            if (columns.Length > maxColumns)
            {
                throw ExceptionBuilder.KeyTooManyColumns(maxColumns);
            }

            for (int i = 0; i < columns.Length; i++)
            {
                if (columns[i] == null)
                {
                    throw ExceptionBuilder.ArgumentNull("column");
                }
            }

            for (int i = 0; i < columns.Length; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    if (columns[i] == columns[j])
                    {
                        throw ExceptionBuilder.KeyDuplicateColumns(columns[i].ColumnName);
                    }
                }
            }

            if (copyColumns)
            {
                // Need to make a copy of all columns
                _columns = new DataColumn[columns.Length];
                for (int i = 0; i < columns.Length; i++)
                {
                    _columns[i] = columns[i];
                }
            }
            else
            {
                // take ownership of the array passed in
                _columns = columns;
            }
            CheckState();
        }

        internal DataColumn[] ColumnsReference => _columns;
        internal bool HasValue => null != _columns;
        internal DataTable Table => _columns[0].Table;
        internal void CheckState()
        {
            DataTable table = _columns[0].Table;

            if (table == null)
            {
                throw ExceptionBuilder.ColumnNotInAnyTable();
            }

            for (int i = 1; i < _columns.Length; i++)
            {
                if (_columns[i].Table == null)
                {
                    throw ExceptionBuilder.ColumnNotInAnyTable();
                }
                if (_columns[i].Table != table)
                {
                    throw ExceptionBuilder.KeyTableMismatch();
                }
            }
        }

        //check to see if this.columns && key2's columns are equal regardless of order
        internal bool ColumnsEqual(DataKey key) => ColumnsEqual(_columns, key._columns);

        //check to see if columns1 && columns2 are equal regardless of order
        internal static bool ColumnsEqual(DataColumn[] column1, DataColumn[] column2)
        {
            if (column1 == column2)
            {
                return true;
            }
            else if (column1 == null || column2 == null)
            {
                return false;
            }
            else if (column1.Length != column2.Length)
            {
                return false;
            }
            else
            {
                int i, j;
                for (i = 0; i < column1.Length; i++)
                {
                    bool check = false;
                    for (j = 0; j < column2.Length; j++)
                    {
                        if (column1[i].Equals(column2[j]))
                        {
                            check = true;
                            break;
                        }
                    }
                    if (!check)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        internal bool ContainsColumn(DataColumn column)
        {
            for (int i = 0; i < _columns.Length; i++)
            {
                if (column == _columns[i])
                {
                    return true;
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            Debug.Fail("don't put DataKey into a Hashtable");
            return base.GetHashCode();
        }
        
        public override bool Equals(object value)
        {
            Debug.Fail("need to directly call Equals(DataKey)");
            return Equals((DataKey)value);
        }

        internal bool Equals(DataKey value)
        {
            //check to see if this.columns && key2's columns are equal...
            DataColumn[] column1 = _columns;
            DataColumn[] column2 = value._columns;

            if (column1 == column2)
            {
                return true;
            }
            else if (column1 == null || column2 == null)
            {
                return false;
            }
            else if (column1.Length != column2.Length)
            {
                return false;
            }
            else
            {
                for (int i = 0; i < column1.Length; i++)
                {
                    if (!column1[i].Equals(column2[i]))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        internal string[] GetColumnNames()
        {
            string[] values = new string[_columns.Length];
            for (int i = 0; i < _columns.Length; ++i)
            {
                values[i] = _columns[i].ColumnName;
            }
            return values;
        }

        internal IndexField[] GetIndexDesc()
        {
            IndexField[] indexDesc = new IndexField[_columns.Length];
            for (int i = 0; i < _columns.Length; i++)
            {
                indexDesc[i] = new IndexField(_columns[i], false);
            }
            return indexDesc;
        }

        internal object[] GetKeyValues(int record)
        {
            object[] values = new object[_columns.Length];
            for (int i = 0; i < _columns.Length; i++)
            {
                values[i] = _columns[i][record];
            }
            return values;
        }

        internal Index GetSortIndex() => GetSortIndex(DataViewRowState.CurrentRows);

        internal Index GetSortIndex(DataViewRowState recordStates)
        {
            IndexField[] indexDesc = GetIndexDesc();
            return _columns[0].Table.GetIndex(indexDesc, recordStates, null);
        }

        internal bool RecordsEqual(int record1, int record2)
        {
            for (int i = 0; i < _columns.Length; i++)
            {
                if (_columns[i].Compare(record1, record2) != 0)
                {
                    return false;
                }
            }
            return true;
        }

        internal DataColumn[] ToArray()
        {
            DataColumn[] values = new DataColumn[_columns.Length];
            for (int i = 0; i < _columns.Length; ++i)
            {
                values[i] = _columns[i];
            }
            return values;
        }
    }
}
