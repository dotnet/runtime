// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace System.Data
{
    public sealed class DataTableReader : DbDataReader
    {
        private readonly DataTable[] _tables;
        private bool _isOpen = true;
        private DataTable? _schemaTable;

        private int _tableCounter = -1;
        private int _rowCounter = -1;
        private DataTable _currentDataTable = default!; // Always initialized in Init
        private DataRow? _currentDataRow;

        private bool _hasRows = true;
        private bool _reachEORows;
        private bool _currentRowRemoved;
        private bool _schemaIsChanged;
        private bool _started;
        private bool _readerIsInvalid;
        private DataTableReaderListener _listener = default!; // Always initialized in Init
        private bool _tableCleared;

        public DataTableReader(DataTable dataTable)
        {
            if (dataTable == null)
            {
                throw ExceptionBuilder.ArgumentNull(nameof(DataTable));
            }
            _tables = new DataTable[1] { dataTable };

            Init();
        }

        public DataTableReader(DataTable[] dataTables)
        {
            if (dataTables == null)
            {
                throw ExceptionBuilder.ArgumentNull(nameof(DataTable));
            }
            if (dataTables.Length == 0)
            {
                throw ExceptionBuilder.DataTableReaderArgumentIsEmpty();
            }

            _tables = new DataTable[dataTables.Length];
            for (int i = 0; i < dataTables.Length; i++)
            {
                if (dataTables[i] == null)
                {
                    throw ExceptionBuilder.ArgumentNull(nameof(DataTable));
                }
                _tables[i] = dataTables[i];
            }

            Init();
        }

        private bool ReaderIsInvalid
        {
            get { return _readerIsInvalid; }
            set
            {
                if (_readerIsInvalid == value)
                {
                    return;
                }

                _readerIsInvalid = value;
                if (_readerIsInvalid && _listener != null)
                {
                    _listener.CleanUp();
                }
            }
        }

        private bool IsSchemaChanged
        {
            get { return _schemaIsChanged; }
            set
            {
                if (!value || _schemaIsChanged == value) //once it is set to false; should not change unless in init() or NextResult()
                {
                    return;
                }

                _schemaIsChanged = value;
                if (_listener != null)
                {
                    _listener.CleanUp();
                }
            }
        }

        internal DataTable CurrentDataTable => _currentDataTable;

        private void Init()
        {
            _tableCounter = 0;
            _reachEORows = false;
            _schemaIsChanged = false;
            _currentDataTable = _tables[_tableCounter];
            _hasRows = (_currentDataTable.Rows.Count > 0);
            ReaderIsInvalid = false;

            // we need to listen to current tables event so create a listener, it will listen to events and call us back.
            _listener = new DataTableReaderListener(this);
        }

        public override void Close()
        {
            if (!_isOpen)
            {
                return;
            }

            // no need to listen to events after close
            if (_listener != null)
            {
                _listener.CleanUp();
            }

            _listener = null!;
            _schemaTable = null;
            _isOpen = false;
        }

        public override DataTable GetSchemaTable()
        {
            ValidateOpen(nameof(GetSchemaTable));
            ValidateReader();

            // each time, we just get schema table of current table for once, no need to recreate each time, if schema is changed, reader is already
            // is invalid
            if (_schemaTable == null)
            {
                _schemaTable = GetSchemaTableFromDataTable(_currentDataTable);
            }

            return _schemaTable;
        }

        public override bool NextResult()
        {
            // next result set; reset everything
            ValidateOpen(nameof(NextResult));

            if ((_tableCounter == _tables.Length - 1))
            {
                return false;
            }

            _currentDataTable = _tables[++_tableCounter];

            if (_listener != null)
            {
                _listener.UpdataTable(_currentDataTable); // it will unsubscribe from preveous tables events and subscribe to new table's events
            }

            _schemaTable = null;
            _rowCounter = -1;
            _currentRowRemoved = false;
            _reachEORows = false;
            _schemaIsChanged = false;
            _started = false;
            ReaderIsInvalid = false;
            _tableCleared = false;

            _hasRows = (_currentDataTable.Rows.Count > 0);

            return true;
        }

        public override bool Read()
        {
            if (!_started)
            {
                _started = true;
            }

            ValidateOpen(nameof(Read));
            ValidateReader();

            if (_reachEORows)
            {
                return false;
            }

            if (_rowCounter >= _currentDataTable.Rows.Count - 1)
            {
                _reachEORows = true;
                if (_listener != null)
                {
                    _listener.CleanUp();
                }
                return false;
            }

            _rowCounter++;
            ValidateRow(_rowCounter);
            _currentDataRow = _currentDataTable.Rows[_rowCounter];

            while (_currentDataRow.RowState == DataRowState.Deleted)
            {
                _rowCounter++;
                if (_rowCounter == _currentDataTable.Rows.Count)
                {
                    _reachEORows = true;
                    if (_listener != null)
                    {
                        _listener.CleanUp();
                    }
                    return false;
                }
                ValidateRow(_rowCounter);
                _currentDataRow = _currentDataTable.Rows[_rowCounter];
            }
            if (_currentRowRemoved)
            {
                _currentRowRemoved = false;
            }

            return true;
        }

        public override int Depth
        {
            get
            {
                ValidateOpen(nameof(Depth));
                ValidateReader();
                return 0;
            }
        }

        public override bool IsClosed => !_isOpen;

        public override int RecordsAffected
        {
            get
            {
                ValidateReader();
                return 0;
            }
        }

        public override bool HasRows
        {
            get
            {
                ValidateOpen(nameof(HasRows));
                ValidateReader();
                return _hasRows;
            }
        }

        public override object this[int ordinal]
        {
            get
            {
                ValidateOpen("Item");
                ValidateReader();
                if ((_currentDataRow == null) || (_currentDataRow.RowState == DataRowState.Deleted))
                {
                    ReaderIsInvalid = true;
                    throw ExceptionBuilder.InvalidDataTableReader(_currentDataTable.TableName);
                }
                try
                {
                    return _currentDataRow[ordinal];
                }
                catch (IndexOutOfRangeException e)
                {
                    // thrown by DataColumnCollection
                    ExceptionBuilder.TraceExceptionWithoutRethrow(e);
                    throw ExceptionBuilder.ArgumentOutOfRange(nameof(ordinal));
                }
            }
        }

        public override object this[string name]
        {
            get
            {
                ValidateOpen("Item");
                ValidateReader();
                if ((_currentDataRow == null) || (_currentDataRow.RowState == DataRowState.Deleted))
                {
                    ReaderIsInvalid = true;
                    throw ExceptionBuilder.InvalidDataTableReader(_currentDataTable.TableName);
                }
                return _currentDataRow[name];
            }
        }

        public override int FieldCount
        {
            get
            {
                ValidateOpen(nameof(FieldCount));
                ValidateReader();
                return _currentDataTable.Columns.Count;
            }
        }

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)]
        public override Type GetProviderSpecificFieldType(int ordinal)
        {
            ValidateOpen(nameof(GetProviderSpecificFieldType));
            ValidateReader();
            return GetFieldType(ordinal);
        }

        public override object GetProviderSpecificValue(int ordinal)
        {
            ValidateOpen(nameof(GetProviderSpecificValue));
            ValidateReader();
            return GetValue(ordinal);
        }

        public override int GetProviderSpecificValues(object[] values)
        {
            ValidateOpen(nameof(GetProviderSpecificValues));
            ValidateReader();
            return GetValues(values);
        }

        public override bool GetBoolean(int ordinal)
        {
            ValidateState(nameof(GetBoolean));
            ValidateReader();
            try
            {
                return (bool)_currentDataRow![ordinal];
            }
            catch (IndexOutOfRangeException e)
            {
                // thrown by DataColumnCollection
                ExceptionBuilder.TraceExceptionWithoutRethrow(e);
                throw ExceptionBuilder.ArgumentOutOfRange(nameof(ordinal));
            }
        }

        public override byte GetByte(int ordinal)
        {
            ValidateState(nameof(GetByte));
            ValidateReader();
            try
            {
                return (byte)_currentDataRow![ordinal];
            }
            catch (IndexOutOfRangeException e)
            {
                // thrown by DataColumnCollection
                ExceptionBuilder.TraceExceptionWithoutRethrow(e);
                throw ExceptionBuilder.ArgumentOutOfRange(nameof(ordinal));
            }
        }

        public override long GetBytes(int ordinal, long dataIndex, byte[]? buffer, int bufferIndex, int length)
        {
            ValidateState(nameof(GetBytes));
            ValidateReader();
            byte[] tempBuffer;
            try
            {
                tempBuffer = (byte[])_currentDataRow![ordinal];
            }
            catch (IndexOutOfRangeException e)
            {
                // thrown by DataColumnCollection
                ExceptionBuilder.TraceExceptionWithoutRethrow(e);
                throw ExceptionBuilder.ArgumentOutOfRange(nameof(ordinal));
            }

            if (buffer == null)
            {
                return tempBuffer.Length;
            }

            int srcIndex = (int)dataIndex;
            int byteCount = Math.Min(tempBuffer.Length - srcIndex, length);
            if (srcIndex < 0)
            {
                throw ADP.InvalidSourceBufferIndex(tempBuffer.Length, srcIndex, nameof(dataIndex));
            }
            else if ((bufferIndex < 0) || (bufferIndex > 0 && bufferIndex >= buffer.Length))
            {
                throw ADP.InvalidDestinationBufferIndex(buffer.Length, bufferIndex, nameof(bufferIndex));
            }

            if (0 < byteCount)
            {
                Array.Copy(tempBuffer, dataIndex, buffer, bufferIndex, byteCount);
            }
            else if (length < 0)
            {
                throw ADP.InvalidDataLength(length);
            }
            else
            {
                byteCount = 0;
            }
            return byteCount;
        }

        public override char GetChar(int ordinal)
        {
            ValidateState(nameof(GetChar));
            ValidateReader();
            try
            {
                return (char)_currentDataRow![ordinal];
            }
            catch (IndexOutOfRangeException e)
            {
                // thrown by DataColumnCollection
                ExceptionBuilder.TraceExceptionWithoutRethrow(e);
                throw ExceptionBuilder.ArgumentOutOfRange(nameof(ordinal));
            }
        }

        public override long GetChars(int ordinal, long dataIndex, char[]? buffer, int bufferIndex, int length)
        {
            ValidateState(nameof(GetChars));
            ValidateReader();
            char[] tempBuffer;
            try
            {
                tempBuffer = (char[])_currentDataRow![ordinal];
            }
            catch (IndexOutOfRangeException e)
            {
                // thrown by DataColumnCollection
                ExceptionBuilder.TraceExceptionWithoutRethrow(e);
                throw ExceptionBuilder.ArgumentOutOfRange(nameof(ordinal));
            }

            if (buffer == null)
            {
                return tempBuffer.Length;
            }

            int srcIndex = (int)dataIndex;
            int charCount = Math.Min(tempBuffer.Length - srcIndex, length);
            if (srcIndex < 0)
            {
                throw ADP.InvalidSourceBufferIndex(tempBuffer.Length, srcIndex, nameof(dataIndex));
            }
            else if ((bufferIndex < 0) || (bufferIndex > 0 && bufferIndex >= buffer.Length))
            {
                throw ADP.InvalidDestinationBufferIndex(buffer.Length, bufferIndex, nameof(bufferIndex));
            }

            if (0 < charCount)
            {
                Array.Copy(tempBuffer, dataIndex, buffer, bufferIndex, charCount);
            }
            else if (length < 0)
            {
                throw ADP.InvalidDataLength(length);
            }
            else
            {
                charCount = 0;
            }
            return charCount;
        }

        public override string GetDataTypeName(int ordinal)
        {
            ValidateOpen(nameof(GetDataTypeName));
            ValidateReader();
            return GetFieldType(ordinal).Name;
        }

        public override DateTime GetDateTime(int ordinal)
        {
            ValidateState(nameof(GetDateTime));
            ValidateReader();
            try
            {
                return (DateTime)_currentDataRow![ordinal];
            }
            catch (IndexOutOfRangeException e)
            {
                // thrown by DataColumnCollection
                ExceptionBuilder.TraceExceptionWithoutRethrow(e);
                throw ExceptionBuilder.ArgumentOutOfRange(nameof(ordinal));
            }
        }

        public override decimal GetDecimal(int ordinal)
        {
            ValidateState(nameof(GetDecimal));
            ValidateReader();
            try
            {
                return (decimal)_currentDataRow![ordinal];
            }
            catch (IndexOutOfRangeException e)
            {
                // thrown by DataColumnCollection
                ExceptionBuilder.TraceExceptionWithoutRethrow(e);
                throw ExceptionBuilder.ArgumentOutOfRange(nameof(ordinal));
            }
        }

        public override double GetDouble(int ordinal)
        {
            ValidateState(nameof(GetDouble));
            ValidateReader();
            try
            {
                return (double)_currentDataRow![ordinal];
            }
            catch (IndexOutOfRangeException e)
            { // thrown by DataColumnCollection
                ExceptionBuilder.TraceExceptionWithoutRethrow(e);
                throw ExceptionBuilder.ArgumentOutOfRange(nameof(ordinal));
            }
        }

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)]
        public override Type GetFieldType(int ordinal)
        {
            ValidateOpen(nameof(GetFieldType));
            ValidateReader();
            try
            {
                return (_currentDataTable.Columns[ordinal].DataType);
            }
            catch (IndexOutOfRangeException e)
            { // thrown by DataColumnCollection
                ExceptionBuilder.TraceExceptionWithoutRethrow(e);
                throw ExceptionBuilder.ArgumentOutOfRange(nameof(ordinal));
            }
        }

        public override float GetFloat(int ordinal)
        {
            ValidateState(nameof(GetFloat));
            ValidateReader();
            try
            {
                return (float)_currentDataRow![ordinal];
            }
            catch (IndexOutOfRangeException e)
            {
                // thrown by DataColumnCollection
                ExceptionBuilder.TraceExceptionWithoutRethrow(e);
                throw ExceptionBuilder.ArgumentOutOfRange(nameof(ordinal));
            }
        }

        public override Guid GetGuid(int ordinal)
        {
            ValidateState(nameof(GetGuid));
            ValidateReader();
            try
            {
                return (Guid)_currentDataRow![ordinal];
            }
            catch (IndexOutOfRangeException e)
            {
                // thrown by DataColumnCollection
                ExceptionBuilder.TraceExceptionWithoutRethrow(e);
                throw ExceptionBuilder.ArgumentOutOfRange(nameof(ordinal));
            }
        }

        public override short GetInt16(int ordinal)
        {
            ValidateState(nameof(GetInt16));
            ValidateReader();
            try
            {
                return (short)_currentDataRow![ordinal];
            }
            catch (IndexOutOfRangeException e)
            {
                // thrown by DataColumnCollection
                ExceptionBuilder.TraceExceptionWithoutRethrow(e);
                throw ExceptionBuilder.ArgumentOutOfRange(nameof(ordinal));
            }
        }

        public override int GetInt32(int ordinal)
        {
            ValidateState(nameof(GetInt32));
            ValidateReader();
            try
            {
                return (int)_currentDataRow![ordinal];
            }
            catch (IndexOutOfRangeException e)
            {
                // thrown by DataColumnCollection
                ExceptionBuilder.TraceExceptionWithoutRethrow(e);
                throw ExceptionBuilder.ArgumentOutOfRange(nameof(ordinal));
            }
        }

        public override long GetInt64(int ordinal)
        {
            ValidateState(nameof(GetInt64));
            ValidateReader();
            try
            {
                return (long)_currentDataRow![ordinal];
            }
            catch (IndexOutOfRangeException e)
            {
                // thrown by DataColumnCollection
                ExceptionBuilder.TraceExceptionWithoutRethrow(e);
                throw ExceptionBuilder.ArgumentOutOfRange(nameof(ordinal));
            }
        }

        public override string GetName(int ordinal)
        {
            ValidateOpen(nameof(GetName));
            ValidateReader();
            try
            {
                return (_currentDataTable.Columns[ordinal].ColumnName);
            }
            catch (IndexOutOfRangeException e)
            {
                // thrown by DataColumnCollection
                ExceptionBuilder.TraceExceptionWithoutRethrow(e);
                throw ExceptionBuilder.ArgumentOutOfRange(nameof(ordinal));
            }
        }

        public override int GetOrdinal(string name)
        {
            ValidateOpen(nameof(GetOrdinal));
            ValidateReader();
            DataColumn? dc = _currentDataTable.Columns[name];

            if (dc != null)
            {
                return dc.Ordinal;
            }
            else
            {
                throw ExceptionBuilder.ColumnNotInTheTable(name, _currentDataTable.TableName);
            }
        }

        public override string GetString(int ordinal)
        {
            ValidateState(nameof(GetString));
            ValidateReader();
            try
            {
                return (string)_currentDataRow![ordinal];
            }
            catch (IndexOutOfRangeException e)
            {
                // thrown by DataColumnCollection
                ExceptionBuilder.TraceExceptionWithoutRethrow(e);
                throw ExceptionBuilder.ArgumentOutOfRange(nameof(ordinal));
            }
        }

        public override object GetValue(int ordinal)
        {
            ValidateState(nameof(GetValue));
            ValidateReader();
            try
            {
                return _currentDataRow![ordinal];
            }
            catch (IndexOutOfRangeException e)
            {
                // thrown by DataColumnCollection
                ExceptionBuilder.TraceExceptionWithoutRethrow(e);
                throw ExceptionBuilder.ArgumentOutOfRange(nameof(ordinal));
            }
        }

        public override int GetValues(object[] values)
        {
            ValidateState(nameof(GetValues));
            ValidateReader();

            if (values == null)
            {
                throw ExceptionBuilder.ArgumentNull(nameof(values));
            }

            Array.Copy(_currentDataRow!.ItemArray, values, _currentDataRow.ItemArray.Length > values.Length ? values.Length : _currentDataRow.ItemArray.Length);
            return (_currentDataRow.ItemArray.Length > values.Length ? values.Length : _currentDataRow.ItemArray.Length);
        }
        public override bool IsDBNull(int ordinal)
        {
            ValidateState(nameof(IsDBNull));
            ValidateReader();
            try
            {
                return (_currentDataRow!.IsNull(ordinal));
            }
            catch (IndexOutOfRangeException e)
            {
                // thrown by DataColumnCollection
                ExceptionBuilder.TraceExceptionWithoutRethrow(e);
                throw ExceptionBuilder.ArgumentOutOfRange(nameof(ordinal));
            }
        }

        public override IEnumerator GetEnumerator()
        {
            ValidateOpen(nameof(GetEnumerator));
            return new DbEnumerator((IDataReader)this);
        }

        internal static DataTable GetSchemaTableFromDataTable(DataTable table)
        {
            if (table == null)
            {
                throw ExceptionBuilder.ArgumentNull(nameof(DataTable));
            }

            DataTable tempSchemaTable = new DataTable("SchemaTable");
            tempSchemaTable.Locale = System.Globalization.CultureInfo.InvariantCulture;

            DataColumn ColumnName = new DataColumn(SchemaTableColumn.ColumnName, typeof(string));
            DataColumn ColumnOrdinal = new DataColumn(SchemaTableColumn.ColumnOrdinal, typeof(int));
            DataColumn ColumnSize = new DataColumn(SchemaTableColumn.ColumnSize, typeof(int));
            DataColumn NumericPrecision = new DataColumn(SchemaTableColumn.NumericPrecision, typeof(short));
            DataColumn NumericScale = new DataColumn(SchemaTableColumn.NumericScale, typeof(short));
            DataColumn DataType = GetSystemTypeDataColumn();

            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2111:ReflectionToDynamicallyAccessedMembers",
                Justification = "The problem is Type.TypeInitializer which requires constructors on the Type instance." +
                    "In this case the TypeInitializer property is not accessed dynamically.")]
            static DataColumn GetSystemTypeDataColumn() =>
                new DataColumn(SchemaTableColumn.DataType, typeof(Type));

            DataColumn ProviderType = new DataColumn(SchemaTableColumn.ProviderType, typeof(int));
            DataColumn IsLong = new DataColumn(SchemaTableColumn.IsLong, typeof(bool));
            DataColumn AllowDBNull = new DataColumn(SchemaTableColumn.AllowDBNull, typeof(bool));
            DataColumn IsReadOnly = new DataColumn(SchemaTableOptionalColumn.IsReadOnly, typeof(bool));
            DataColumn IsRowVersion = new DataColumn(SchemaTableOptionalColumn.IsRowVersion, typeof(bool));
            DataColumn IsUnique = new DataColumn(SchemaTableColumn.IsUnique, typeof(bool));
            DataColumn IsKeyColumn = new DataColumn(SchemaTableColumn.IsKey, typeof(bool));
            DataColumn IsAutoIncrement = new DataColumn(SchemaTableOptionalColumn.IsAutoIncrement, typeof(bool));
            DataColumn BaseSchemaName = new DataColumn(SchemaTableColumn.BaseSchemaName, typeof(string));
            DataColumn BaseCatalogName = new DataColumn(SchemaTableOptionalColumn.BaseCatalogName, typeof(string));
            DataColumn BaseTableName = new DataColumn(SchemaTableColumn.BaseTableName, typeof(string));
            DataColumn BaseColumnName = new DataColumn(SchemaTableColumn.BaseColumnName, typeof(string));
            DataColumn AutoIncrementSeed = new DataColumn(SchemaTableOptionalColumn.AutoIncrementSeed, typeof(long));
            DataColumn AutoIncrementStep = new DataColumn(SchemaTableOptionalColumn.AutoIncrementStep, typeof(long));
            DataColumn DefaultValue = new DataColumn(SchemaTableOptionalColumn.DefaultValue, typeof(object));
            DataColumn Expression = new DataColumn(SchemaTableOptionalColumn.Expression, typeof(string));
            DataColumn ColumnMapping = new DataColumn(SchemaTableOptionalColumn.ColumnMapping, typeof(MappingType));
            DataColumn BaseTableNamespace = new DataColumn(SchemaTableOptionalColumn.BaseTableNamespace, typeof(string));
            DataColumn BaseColumnNamespace = new DataColumn(SchemaTableOptionalColumn.BaseColumnNamespace, typeof(string));

            ColumnSize.DefaultValue = -1;

            if (table.DataSet != null)
            {
                BaseCatalogName.DefaultValue = table.DataSet.DataSetName;
            }

            BaseTableName.DefaultValue = table.TableName;
            BaseTableNamespace.DefaultValue = table.Namespace;
            IsRowVersion.DefaultValue = false;
            IsLong.DefaultValue = false;
            IsReadOnly.DefaultValue = false;
            IsKeyColumn.DefaultValue = false;
            IsAutoIncrement.DefaultValue = false;
            AutoIncrementSeed.DefaultValue = 0;
            AutoIncrementStep.DefaultValue = 1;

            tempSchemaTable.Columns.Add(ColumnName);
            tempSchemaTable.Columns.Add(ColumnOrdinal);
            tempSchemaTable.Columns.Add(ColumnSize);
            tempSchemaTable.Columns.Add(NumericPrecision);
            tempSchemaTable.Columns.Add(NumericScale);
            tempSchemaTable.Columns.Add(DataType);
            tempSchemaTable.Columns.Add(ProviderType);
            tempSchemaTable.Columns.Add(IsLong);
            tempSchemaTable.Columns.Add(AllowDBNull);
            tempSchemaTable.Columns.Add(IsReadOnly);
            tempSchemaTable.Columns.Add(IsRowVersion);
            tempSchemaTable.Columns.Add(IsUnique);
            tempSchemaTable.Columns.Add(IsKeyColumn);
            tempSchemaTable.Columns.Add(IsAutoIncrement);
            tempSchemaTable.Columns.Add(BaseCatalogName);
            tempSchemaTable.Columns.Add(BaseSchemaName);

            // specific to datatablereader
            tempSchemaTable.Columns.Add(BaseTableName);
            tempSchemaTable.Columns.Add(BaseColumnName);
            tempSchemaTable.Columns.Add(AutoIncrementSeed);
            tempSchemaTable.Columns.Add(AutoIncrementStep);
            tempSchemaTable.Columns.Add(DefaultValue);
            tempSchemaTable.Columns.Add(Expression);
            tempSchemaTable.Columns.Add(ColumnMapping);
            tempSchemaTable.Columns.Add(BaseTableNamespace);
            tempSchemaTable.Columns.Add(BaseColumnNamespace);

            foreach (DataColumn dc in table.Columns)
            {
                DataRow dr = tempSchemaTable.NewRow();

                dr[ColumnName] = dc.ColumnName;
                dr[ColumnOrdinal] = dc.Ordinal;
                dr[DataType] = dc.DataType;

                if (dc.DataType == typeof(string))
                {
                    dr[ColumnSize] = dc.MaxLength;
                }

                dr[AllowDBNull] = dc.AllowDBNull;
                dr[IsReadOnly] = dc.ReadOnly;
                dr[IsUnique] = dc.Unique;

                if (dc.AutoIncrement)
                {
                    dr[IsAutoIncrement] = true;
                    dr[AutoIncrementSeed] = dc.AutoIncrementSeed;
                    dr[AutoIncrementStep] = dc.AutoIncrementStep;
                }

                if (dc.DefaultValue != DBNull.Value)
                {
                    dr[DefaultValue] = dc.DefaultValue;
                }

                if (dc.Expression.Length != 0)
                {
                    bool hasExternalDependency = false;
                    DataColumn[] dependency = dc.DataExpression!.GetDependency();
                    for (int j = 0; j < dependency.Length; j++)
                    {
                        if (dependency[j].Table != table)
                        {
                            hasExternalDependency = true;
                            break;
                        }
                    }
                    if (!hasExternalDependency)
                    {
                        dr[Expression] = dc.Expression;
                    }
                }

                dr[ColumnMapping] = dc.ColumnMapping;
                dr[BaseColumnName] = dc.ColumnName;
                dr[BaseColumnNamespace] = dc.Namespace;

                tempSchemaTable.Rows.Add(dr);
            }

            foreach (DataColumn key in table.PrimaryKey)
            {
                tempSchemaTable.Rows[key.Ordinal][IsKeyColumn] = true;
            }

            tempSchemaTable.AcceptChanges();

            return tempSchemaTable;
        }

        private void ValidateOpen(string caller)
        {
            if (!_isOpen)
            {
                throw ADP.DataReaderClosed(caller);
            }
        }

        private void ValidateReader()
        {
            if (ReaderIsInvalid)
            {
                throw ExceptionBuilder.InvalidDataTableReader(_currentDataTable.TableName);
            }

            if (IsSchemaChanged)
            {
                throw ExceptionBuilder.DataTableReaderSchemaIsInvalid(_currentDataTable.TableName); // may be we can use better error message!
            }
        }

        private void ValidateState(string caller)
        {
            ValidateOpen(caller);
            if (_tableCleared)
            {
                throw ExceptionBuilder.EmptyDataTableReader(_currentDataTable.TableName);
            }

            // see if without any event raising, if our current row has some changes!if so reader is invalid.
            if ((_currentDataRow == null) || (_currentDataTable == null))
            {
                ReaderIsInvalid = true;
                // TODO: This will throw an NRE
                throw ExceptionBuilder.InvalidDataTableReader(_currentDataTable!.TableName);
            }

            //See if without any event raing, if our rows are deleted, or removed! Reader is not invalid, user should be able to read and reach goo row
            if ((_currentDataRow.RowState == DataRowState.Deleted) || (_currentDataRow.RowState == DataRowState.Detached) || _currentRowRemoved)
            {
                throw ExceptionBuilder.InvalidCurrentRowInDataTableReader();
            }
            // user may have called clear (which removes the rows without raing event) or deleted part of rows without raising event!if so reader is invalid.
            if (0 > _rowCounter || _currentDataTable.Rows.Count <= _rowCounter)
            {
                ReaderIsInvalid = true;
                throw ExceptionBuilder.InvalidDataTableReader(_currentDataTable.TableName);
            }
        }

        private void ValidateRow(int rowPosition)
        {
            if (ReaderIsInvalid)
            {
                throw ExceptionBuilder.InvalidDataTableReader(_currentDataTable.TableName);
            }

            if (0 > rowPosition || _currentDataTable.Rows.Count <= rowPosition)
            {
                ReaderIsInvalid = true;
                throw ExceptionBuilder.InvalidDataTableReader(_currentDataTable.TableName);
            }
        }

        // Event Call backs from DataTableReaderListener,  will invoke these methods
        internal void SchemaChanged()
        {
            IsSchemaChanged = true;
        }

        internal void DataTableCleared()
        {
            if (!_started)
            {
                return;
            }

            _rowCounter = -1;
            if (!_reachEORows)
            {
                _currentRowRemoved = true;
            }
        }

        internal void DataChanged(DataRowChangeEventArgs args)
        {
            if ((!_started) || (_rowCounter == -1 && !_tableCleared))
            {
                return;
            }

            switch (args.Action)
            {
                case DataRowAction.Add:
                    ValidateRow(_rowCounter + 1);
                    if (_currentDataRow == _currentDataTable.Rows[_rowCounter + 1])
                    {
                        // check if we moved one position up
                        _rowCounter++;  // if so, refresh the datarow and fix the counter
                    }
                    break;
                case DataRowAction.Delete: // delete
                case DataRowAction.Rollback:// rejectchanges
                case DataRowAction.Commit: // acceptchanges
                    if (args.Row.RowState == DataRowState.Detached)
                    {
                        if (args.Row != _currentDataRow)
                        {
                            if (_rowCounter == 0) // if I am at first row and no previous row exist,NOOP
                                break;
                            ValidateRow(_rowCounter - 1);
                            if (_currentDataRow == _currentDataTable.Rows[_rowCounter - 1])
                            {
                                // one of previous rows is detached, collection size is changed!
                                _rowCounter--;
                            }
                        }
                        else
                        { // we are proccessing current datarow
                            _currentRowRemoved = true;
                            if (_rowCounter > 0)
                            {
                                // go back one row, no matter what the state is
                                _rowCounter--;
                                _currentDataRow = _currentDataTable.Rows[_rowCounter];
                            }
                            else
                            {
                                // we are on 0th row, so reset data to initial state!
                                _rowCounter = -1;
                                _currentDataRow = null;
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
