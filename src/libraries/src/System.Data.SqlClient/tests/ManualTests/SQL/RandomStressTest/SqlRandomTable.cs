// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    /// <summary>
    /// represents table with random column types and values in it
    /// </summary>
    public class SqlRandomTable
    {
        // "Row-Overflow Data Exceeding 8 KB"
        private const int MaxBytesPerRow = 8060;
        // however, with sparse columns the limit drops to 8018
        private const int MaxBytesPerRowWithSparse = 8018;

        // SQL Server uses 6 bytes per-row to store row info (null-bitmap overhead is calculated based on column size)
        private const int ConstantOverhead = 6;

        // SQL does not allow table creation with more than 1024 non-sparse columns
        private const int MaxNonSparseColumns = 1024;

        // cannot send more than 2100 parameters in one command
        private const int MaxParameterCount = 2100;

        /// <summary>
        /// column types
        /// </summary>
        private readonly SqlRandomTableColumn[] _columns;
        private readonly string[] _columnNames;

        public readonly IList<SqlRandomTableColumn> Columns;
        public readonly IList<string> ColumnNames;
        public readonly int? PrimaryKeyColumnIndex;
        public readonly bool HasSparseColumns;
        public readonly double NonSparseValuesTotalSize;

        /// <summary>
        /// maximum size of the row allowed for this column (depends if it has sparse columns or not)
        /// </summary>
        public int MaxRowSize
        {
            get
            {
                if (HasSparseColumns)
                    return MaxBytesPerRowWithSparse;
                else
                    return MaxBytesPerRow;
            }
        }

        /// <summary>
        /// rows and their values
        /// </summary>
        private readonly List<object[]> _rows;

        public IList<object> this[int row]
        {
            get
            {
                return new List<object>(_rows[row]).AsReadOnly();
            }
        }

        public SqlRandomTable(SqlRandomTableColumn[] columns, int? primaryKeyColumnIndex = null, int estimatedRowCount = 0)
        {
            if (columns == null || columns.Length == 0)
                throw new ArgumentException("non-empty type array is required");
            if (estimatedRowCount < 0)
                throw new ArgumentOutOfRangeException("non-negative row count is required, use 0 for default");
            if (primaryKeyColumnIndex.HasValue && (primaryKeyColumnIndex.Value < 0 || primaryKeyColumnIndex.Value >= columns.Length))
                throw new ArgumentOutOfRangeException("primaryColumnIndex");

            PrimaryKeyColumnIndex = primaryKeyColumnIndex;
            _columns = (SqlRandomTableColumn[])columns.Clone();
            _columnNames = new string[columns.Length];
            if (estimatedRowCount == 0)
                _rows = new List<object[]>();
            else
                _rows = new List<object[]>(estimatedRowCount);

            Columns = new List<SqlRandomTableColumn>(_columns).AsReadOnly();
            ColumnNames = new List<string>(_columnNames).AsReadOnly();
            bool hasSparse = false;
            double totalNonSparse = 0;
            foreach (var c in _columns)
            {
                if (c.IsSparse)
                {
                    hasSparse = true;
                }
                else
                {
                    totalNonSparse += c.GetInRowSize(null); // for non-sparse columns size does not depend on the value
                }
            }
            HasSparseColumns = hasSparse;
            NonSparseValuesTotalSize = totalNonSparse;
        }

        public string GetColumnName(int c)
        {
            if (_columnNames[c] == null)
            {
                AutoGenerateColumnNames();
            }

            return _columnNames[c];
        }

        public string GetColumnTSqlType(int c)
        {
            return _columns[c].GetTSqlTypeDefinition();
        }

        /// <summary>
        /// adds new row with random values, each column has 50% chance to have null value
        /// </summary>
        public void AddRow(SqlRandomizer rand)
        {
            BitArray nullBitmap = rand.NextBitmap(_columns.Length);
            AddRow(rand, nullBitmap);
        }

        private bool IsPrimaryKey(int c)
        {
            return PrimaryKeyColumnIndex.HasValue && PrimaryKeyColumnIndex.Value == c;
        }

        /// <summary>
        /// adds a new row with random values and specified null bitmap
        /// </summary>
        public void AddRow(SqlRandomizer rand, BitArray nullBitmap)
        {
            object[] row = new object[_columns.Length];

            // non-sparse columns will always take fixed size, must add them now
            double rowSize = NonSparseValuesTotalSize;
            double maxRowSize = MaxRowSize;

            if (PrimaryKeyColumnIndex.HasValue)
            {
                // make sure pkey is set first
                // ignore the null bitmap in this case
                object pkeyValue = _rows.Count;
                row[PrimaryKeyColumnIndex.Value] = pkeyValue;
            }

            for (int c = 0; c < row.Length; c++)
            {
                if (IsPrimaryKey(c))
                {
                    // handled above
                    continue;
                }

                if (SkipOnInsert(c))
                {
                    row[c] = null; // this null value should not be used, assert triggered if it is
                }
                else if (rowSize >= maxRowSize)
                {
                    // reached the limit, cannot add more for this row
                    row[c] = DBNull.Value;
                }
                else if (nullBitmap[c])
                {
                    row[c] = DBNull.Value;
                }
                else
                {
                    object value = _columns[c].CreateRandomValue(rand);
                    if (value == null || value == DBNull.Value)
                    {
                        row[c] = DBNull.Value;
                    }
                    else if (IsSparse(c))
                    {
                        // check if the value fits
                        double newRowSize = rowSize + _columns[c].GetInRowSize(value);
                        if (newRowSize > maxRowSize)
                        {
                            // cannot fit it, zero this one and try to fit next column
                            row[c] = DBNull.Value;
                        }
                        else
                        {
                            // the value is OK, keep it
                            row[c] = value;
                            rowSize = newRowSize;
                        }
                    }
                    else
                    {
                        // non-sparse values are already counted in NonSparseValuesTotalSize
                        row[c] = value;
                    }
                }
            }

            _rows.Add(row);
        }

        public void AddRows(SqlRandomizer rand, int rowCount)
        {
            for (int i = 0; i < rowCount; i++)
                AddRow(rand);
        }

        public string GenerateCreateTableTSql(string tableName)
        {
            StringBuilder tsql = new StringBuilder();

            tsql.AppendFormat("CREATE TABLE {0} (", tableName);

            for (int c = 0; c < _columns.Length; c++)
            {
                if (c != 0)
                    tsql.Append(", ");

                tsql.AppendFormat("[{0}] {1}", GetColumnName(c), GetColumnTSqlType(c));
                if (IsPrimaryKey(c))
                {
                    tsql.Append(" PRIMARY KEY");
                }
                else if (IsSparse(c))
                {
                    tsql.Append(" SPARSE NULL");
                }
                else if (IsColumnSet(c))
                {
                    tsql.Append(" COLUMN_SET FOR ALL_SPARSE_COLUMNS NULL");
                }
                else
                {
                    tsql.Append(" NULL");
                }
            }

            tsql.AppendFormat(") ON [PRIMARY]");

            return tsql.ToString();
        }

        public void DumpColumnsInfo(TextWriter output)
        {
            for (int i = 0; i < _columnNames.Length - 1; i++)
            {
                output.Write(_columnNames[i]);
                if (_columns[i].StorageSize.HasValue)
                    output.Write(",  [StorageSize={0}]", _columns[i].StorageSize.Value);
                if (_columns[i].Precision.HasValue)
                    output.Write(",  [Precision={0}]", _columns[i].Precision.Value);
                if (_columns[i].Scale.HasValue)
                    output.Write(",  [Scale={0}]", _columns[i].Scale.Value);
                output.WriteLine();
            }
        }

        public void DumpRow(TextWriter output, object[] row)
        {
            if (row == null || row.Length != _columns.Length)
                throw new ArgumentException("Row length does not match the columns");

            for (int i = 0; i < _columnNames.Length - 1; i++)
            {
                object val = row[i];
                string type;
                if (val == null)
                {
                    val = "<dbnull>";
                    type = "";
                }
                else
                {
                    type = val.GetType().Name;
                    if (val is Array)
                    {
                        val = string.Format("[Length={0}]", ((Array)val).Length);
                    }
                    else if (val is string)
                    {
                        val = string.Format("[Length={0}]", ((string)val).Length);
                    }
                    else
                    {
                        val = string.Format("[{0}]", val);
                    }
                }

                output.WriteLine("[{0}] = {1}", _columnNames[i], val);
            }
        }

        private bool SkipOnInsert(int c)
        {
            if (_columns[c].Type == SqlDbType.Timestamp)
            {
                // cannot insert timestamp
                return true;
            }

            if (IsColumnSet(c))
            {
                // skip column set, using sparse columns themselves
                return true;
            }

            // OK to insert value
            return false;
        }

        public void GenerateTableOnServer(SqlConnection con, string tableName)
        {
            // create table
            SqlCommand cmd = con.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = GenerateCreateTableTSql(tableName);

            cmd.ExecuteNonQuery();

            InsertRows(con, tableName, 0, _rows.Count);
        }

        public void InsertRows(SqlConnection con, string tableName, int rowFrom, int rowToExclusive)
        {
            if (con == null || tableName == null)
            {
                throw new ArgumentNullException("connection and table name must be valid");
            }

            if (rowToExclusive > _rows.Count)
            {
                throw new ArgumentOutOfRangeException("rowToExclusive", rowToExclusive, "cannot be greater than the row count");
            }

            if (rowFrom < 0 || rowFrom > rowToExclusive)
            {
                throw new ArgumentOutOfRangeException("rowFrom", rowFrom, "cannot be less than 0 or greater than rowToExclusive");
            }

            SqlCommand cmd = null;
            SqlParameter[] parameters = null;
            for (int r = rowFrom; r < rowToExclusive; r++)
            {
                InsertRowInternal(con, ref cmd, ref parameters, tableName, r);
            }
        }

        public void InsertRow(SqlConnection con, string tableName, int row)
        {
            if (con == null || tableName == null)
            {
                throw new ArgumentNullException("connection and table name must be valid");
            }

            if (row < 0 || row >= _rows.Count)
            {
                throw new ArgumentOutOfRangeException("row", row, "cannot be less than 0 or greater than or equal to row count");
            }

            SqlCommand cmd = null;
            SqlParameter[] parameters = null;
            InsertRowInternal(con, ref cmd, ref parameters, tableName, row);
        }

        private void InsertRowInternal(SqlConnection con, ref SqlCommand cmd, ref SqlParameter[] parameters, string tableName, int row)
        {
            // cannot use DataTable: it does not handle well char[] values and variant and also does not support sparse/column set ones
            StringBuilder columnsText = new StringBuilder();
            StringBuilder valuesText = new StringBuilder();

            // create the command and parameters on first call, reuse afterwards (to reduces table creation overhead)
            if (cmd == null)
            {
                cmd = con.CreateCommand();
                cmd.CommandType = CommandType.Text;
            }
            else
            {
                // need to unbind existing parameters and re-add the next set of values
                cmd.Parameters.Clear();
            }

            if (parameters == null)
            {
                parameters = new SqlParameter[_columns.Length];
            }

            object[] rowValues = _rows[row];

            // there is a limit of parameters to be sent (2010)
            for (int ci = 0; ci < _columns.Length; ci++)
            {
                if (cmd.Parameters.Count >= MaxParameterCount)
                {
                    // reached the limit of max parameters, cannot continue
                    // theoretically, we could do INSERT + UPDATE. practically, chances for this to happen are almost none since nulls are skipped
                    rowValues[ci] = DBNull.Value;
                    continue;
                }

                if (SkipOnInsert(ci))
                {
                    // cannot insert timestamp
                    // insert of values into columnset columns are also not supported (use sparse columns themselves)
                    continue;
                }

                bool isNull = (rowValues[ci] == DBNull.Value || rowValues[ci] == null);

                if (isNull)
                {
                    // columns such as sparse cannot have DEFAULT constraint, thus it is safe to ignore the value of the column when inserting new row
                    // this also significantly reduces number of columns updated during insert, to prevent "The number of target 
                    // columns that are specified in an INSERT, UPDATE, or MERGE statement exceeds the maximum of 4096."
                    continue;
                }

                SqlParameter p = parameters[ci];

                // construct column list
                if (columnsText.Length > 0)
                {
                    columnsText.Append(", ");
                    valuesText.Append(", ");
                }

                columnsText.AppendFormat("[{0}]", _columnNames[ci]);

                if (p == null)
                {
                    p = cmd.CreateParameter();
                    p.ParameterName = "@p" + ci;
                    p.SqlDbType = _columns[ci].Type;

                    parameters[ci] = p;
                }

                p.Value = rowValues[ci] ?? DBNull.Value;

                cmd.Parameters.Add(p);

                valuesText.Append(p.ParameterName);
            }

            Debug.Assert(columnsText.Length > 0, "Table that have only TIMESTAMP, ColumnSet or Sparse columns are not allowed - use primary key in this case");

            cmd.CommandText = string.Format("INSERT INTO {0} ( {1} ) VALUES ( {2} )", tableName, columnsText, valuesText);

            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// generates SELECT statement; if columnIndices is null the statement will include all the columns
        /// </summary>
        public int GenerateSelectFromTableTSql(string tableName, StringBuilder selectBuilder, int[] columnIndices = null, int indicesOffset = -1, int indicesCount = -1)
        {
            if (tableName == null || selectBuilder == null)
                throw new ArgumentNullException("tableName == null || selectBuilder == null");

            int maxIndicesLength = (columnIndices == null) ? _columns.Length : columnIndices.Length;
            if (indicesOffset == -1)
            {
                indicesOffset = 0;
            }
            else if (indicesOffset < 0 || indicesOffset >= maxIndicesLength)
            {
                throw new ArgumentOutOfRangeException("indicesOffset");
            }

            if (indicesCount == -1)
            {
                indicesCount = maxIndicesLength;
            }
            else if (indicesCount < 1 || (indicesCount + indicesOffset) > maxIndicesLength)
            {
                // at least one index required
                throw new ArgumentOutOfRangeException("indicesCount");
            }

            double totalRowSize = 0;
            int countAdded = 0;

            // append the first
            int columnIndex = (columnIndices == null) ? indicesOffset : columnIndices[indicesOffset];
            selectBuilder.AppendFormat("SELECT [{0}]", _columnNames[columnIndex]);
            totalRowSize += _columns[columnIndex].GetInRowSize(null);
            countAdded++;

            // append the rest, if any
            int end = indicesOffset + indicesCount;
            for (int c = indicesOffset + 1; c < end; c++)
            {
                columnIndex = (columnIndices == null) ? c : columnIndices[c];
                totalRowSize += _columns[columnIndex].GetInRowSize(null);
                if (totalRowSize > MaxRowSize)
                {
                    // overflow - stop now
                    break;
                }

                selectBuilder.AppendFormat(", [{0}]", _columnNames[columnIndex]);
                countAdded++;
            }

            selectBuilder.AppendFormat(" FROM {0}", tableName);

            if (PrimaryKeyColumnIndex.HasValue)
                selectBuilder.AppendFormat(" ORDER BY [{0}]", _columnNames[PrimaryKeyColumnIndex.Value]);

            return countAdded;
        }

        #region static helper methods

        private static int GetRowOverhead(int columnSize)
        {
            int nullBitmapSize = (columnSize + 7) / 8;
            return ConstantOverhead + nullBitmapSize;
        }

        // once we have only this size left on the row, column set column is forced
        // 40 is an XML and variant size
        private static readonly int s_columnSetSafetyRange = SqlRandomTypeInfo.XmlRowUsage * 3;

        /// <summary>
        /// Creates random list of columns from the given source collection. The rules are:
        /// * table cannot contain more than 1024 non-sparse columns
        /// * total row size of non-sparse columns should not exceed 8060 or 8018 (with sparse)
        /// * column set column must be added if number of columns in total exceeds 1024
        /// </summary>
        public static SqlRandomTableColumn[] CreateRandTypes(SqlRandomizer rand, SqlRandomTypeInfoCollection sourceCollection, int maxColumnsCount, bool createIdColumn)
        {
            var retColumns = new List<SqlRandomTableColumn>(maxColumnsCount);
            bool hasTimestamp = false;
            double totalRowSize = 0;
            int totalRegularColumns = 0;

            bool hasColumnSet = false;
            bool hasSparseColumns = false;
            int maxRowSize = MaxBytesPerRow; // set to MaxBytesPerRowWithSparse when sparse column is first added

            int i = 0;
            if (createIdColumn)
            {
                SqlRandomTypeInfo keyType = sourceCollection[SqlDbType.Int];
                SqlRandomTableColumn keyColumn = keyType.CreateDefaultColumn(SqlRandomColumnOptions.None);
                retColumns.Add(keyColumn);
                totalRowSize += keyType.GetInRowSize(keyColumn, null);
                i++;
                totalRegularColumns++;
            }

            for (; i < maxColumnsCount; i++)
            {
                // select column options (sparse/column-set)
                bool isSparse; // must be set in the if/else flow below
                bool isColumnSet = false;

                if (totalRegularColumns >= MaxNonSparseColumns)
                {
                    // reached the limit for regular columns

                    if (!hasColumnSet)
                    {
                        // no column-set yet, stop unconditionally
                        // this can happen if large char/binary value brought the row size total to a limit leaving no space for column-set
                        break;
                    }

                    // there is a column set, enforce sparse from this point
                    isSparse = true;
                }
                else if (i == (MaxNonSparseColumns - 1) && hasSparseColumns && !hasColumnSet)
                {
                    // we almost reached the limit of regular & sparse columns with, but no column set added
                    // to increase chances for >1024 columns, enforce column set now
                    isColumnSet = true;
                    isSparse = false;
                }
                else if (totalRowSize > MaxBytesPerRowWithSparse)
                {
                    Debug.Assert(totalRowSize <= MaxBytesPerRow, "size over the max limit");
                    Debug.Assert(!hasSparseColumns, "should not have sparse columns after MaxBytesPerRowWithSparse (check maxRowSize)");
                    // cannot insert sparse from this point
                    isSparse = false;
                    isColumnSet = false;
                }
                else
                {
                    // check how close we are to the limit of the row size
                    int sparseProbability;
                    if (totalRowSize < 100)
                    {
                        sparseProbability = 2;
                    }
                    else if (totalRowSize < MaxBytesPerRowWithSparse / 2)
                    {
                        sparseProbability = 10;
                    }
                    else if (totalRowSize < (MaxBytesPerRowWithSparse - s_columnSetSafetyRange))
                    {
                        sparseProbability = 50;
                    }
                    else
                    {
                        // close to the row size limit, special case
                        if (!hasColumnSet)
                        {
                            // if we have not added column set column yet
                            // column-set is a regular column and its size counts towards row size, so time to add it
                            isColumnSet = true;
                            sparseProbability = -1; // not used
                        }
                        else
                        {
                            sparseProbability = 90;
                        }
                    }

                    if (!isColumnSet)
                    {
                        isSparse = (rand.Next(100) < sparseProbability);

                        if (!isSparse && !hasColumnSet)
                        {
                            // if decided to add regular column, give it a (low) chance to inject a column set at any position
                            isColumnSet = rand.Next(100) < 1;
                        }
                    }
                    else
                    {
                        isSparse = false;
                    }
                }

                // select the type
                SqlRandomTypeInfo ti;
                SqlRandomColumnOptions options = SqlRandomColumnOptions.None;

                if (isSparse)
                {
                    Debug.Assert(!isColumnSet, "should not have both sparse and column set flags set");
                    ti = sourceCollection.NextSparse(rand);
                    Debug.Assert(ti.CanBeSparseColumn, "NextSparse must return only types that can be sparse");
                    options |= SqlRandomColumnOptions.Sparse;
                }
                else if (isColumnSet)
                {
                    Debug.Assert(!hasColumnSet, "there is already a column set, we should not set isColumnSet again above");
                    ti = sourceCollection[SqlDbType.Xml];
                    options |= SqlRandomColumnOptions.ColumnSet;
                }
                else
                {
                    // regular column
                    ti = sourceCollection.Next(rand);

                    if (ti.Type == SqlDbType.Timestamp)
                    {
                        // while table can contain single timestamp column only, there is no way to insert values into it. 
                        // thus, do not allow this
                        if (hasTimestamp || maxColumnsCount == 1)
                        {
                            ti = sourceCollection[SqlDbType.Int];
                        }
                        else
                        {
                            // table cannot have two timestamp columns
                            hasTimestamp = true;
                        }
                    }
                }

                SqlRandomTableColumn col = ti.CreateRandomColumn(rand, options);

                if (!isSparse)
                {
                    double rowSize = ti.GetInRowSize(col, DBNull.Value);
                    int overhead = GetRowOverhead(retColumns.Count + 1); // +1 for this column

                    if (totalRowSize + rowSize + overhead > maxRowSize)
                    {
                        // cannot use this column
                        // note that if this column is a column set column
                        continue;
                    }

                    totalRowSize += rowSize;
                    totalRegularColumns++;
                }
                // else - sparse columns are not counted towards row size when table is created (they are when inserting new row with non-null value in the sparse column)...

                retColumns.Add(col);

                // after adding the column, update the state
                if (isColumnSet)
                {
                    hasColumnSet = true;
                }

                if (isSparse)
                {
                    hasSparseColumns = true;
                    maxRowSize = MaxBytesPerRowWithSparse; // reduce the max row size
                }
            }

            return retColumns.ToArray();
        }

        public static SqlRandomTable Create(SqlRandomizer rand, SqlRandomTypeInfoCollection sourceCollection, int maxColumnsCount, int rowCount, bool createPrimaryKeyColumn)
        {
            SqlRandomTableColumn[] testTypes = CreateRandTypes(rand, sourceCollection, maxColumnsCount, createPrimaryKeyColumn);
            SqlRandomTable table = new SqlRandomTable(testTypes, primaryKeyColumnIndex: createPrimaryKeyColumn ? (Nullable<int>)0 : null, estimatedRowCount: rowCount);
            table.AddRows(rand, rowCount);
            return table;
        }

        private void AutoGenerateColumnNames()
        {
            Dictionary<string, int> nameMap = new Dictionary<string, int>(_columns.Length);
            for (int c = 0; c < _columns.Length; c++)
            {
                if (_columnNames[c] == null)
                {
                    // pick name that is not in table yet
                    string name = GenerateColumnName(nameMap, c);
                    nameMap[name] = c;
                    _columnNames[c] = name;
                }
                else
                {
                    // check for dups
                    if (nameMap.ContainsKey(_columnNames[c]))
                    {
                        // should not happen now since column names are auto-generated only
                        throw new InvalidOperationException("duplicate column names detected");
                    }
                }
            }
        }

        private string GenerateColumnName(Dictionary<string, int> nameMap, int c)
        {
            string baseName;
            if (IsPrimaryKey(c))
                baseName = "PKEY";
            else
                baseName = string.Format("C{0}_{1}", _columns[c].Type, c);

            string name = baseName;
            int extraSuffix = 1;
            while (nameMap.ContainsKey(name))
            {
                name = string.Format("{0}_{1}", baseName, extraSuffix);
                ++extraSuffix;
            }
            return name;
        }

        private bool IsSparse(int c)
        {
            return _columns[c].IsSparse;
        }

        private bool IsColumnSet(int c)
        {
            return _columns[c].IsColumnSet;
        }

        #endregion static helper methods
    }
}
