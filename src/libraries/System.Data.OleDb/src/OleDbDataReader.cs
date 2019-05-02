// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Data.ProviderBase;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Data.OleDb
{
    public sealed class OleDbDataReader : DbDataReader
    {
        private CommandBehavior _commandBehavior;

        // object model interaction
        private OleDbConnection _connection;
        private OleDbCommand _command;

        // DataReader owns the parameter bindings until CloseDataReader
        // this allows OleDbCommand.Dispose to not require OleDbDataReader.Dispose
        private Bindings _parameterBindings;

        // OLEDB interfaces
        private UnsafeNativeMethods.IMultipleResults _imultipleResults;
        private UnsafeNativeMethods.IRowset _irowset;
        private UnsafeNativeMethods.IRow _irow;

        private ChapterHandle _chapterHandle = ChapterHandle.DB_NULL_HCHAPTER;

        private int _depth;
        private bool _isClosed, _isRead, _hasRows, _hasRowsReadCheck;

        long _sequentialBytesRead;
        int _sequentialOrdinal;

        private Bindings[] _bindings; // _metdata contains the ColumnBinding

        // do we need to jump to the next accessor
        private int _nextAccessorForRetrieval;

        // must increment the counter before retrieving value so that
        // if an exception is thrown, user can continue without erroring again
        private int _nextValueForRetrieval;

        // record affected for the current dataset
        private IntPtr _recordsAffected = ADP.RecordsUnaffected;
        private bool _useIColumnsRowset;
        private bool _sequentialAccess;
        private bool _singleRow;

        // cached information for Reading (rowhandles/status)
        private IntPtr _rowHandleFetchCount; // (>1 fails against jet)
        private RowHandleBuffer _rowHandleNativeBuffer;

        private IntPtr _rowFetchedCount;
        private int _currentRow;

        private DataTable _dbSchemaTable;

        private int _visibleFieldCount;
        private MetaData[] _metadata;
        private FieldNameLookup _fieldNameLookup;

        // ctor for an ICommandText, IMultipleResults, IRowset, IRow
        // ctor for an ADODB.Recordset, ADODB.Record or Hierarchial resultset
        internal OleDbDataReader(OleDbConnection connection, OleDbCommand command, int depth, CommandBehavior commandBehavior)
        {
            _connection = connection;
            _command = command;
            _commandBehavior = commandBehavior;

            if ((null != command) && (0 == _depth))
            {
                _parameterBindings = command.TakeBindingOwnerShip();
            }
            _depth = depth;
        }

        private void Initialize()
        {
            CommandBehavior behavior = _commandBehavior;
            _useIColumnsRowset = (0 != (CommandBehavior.KeyInfo & behavior));
            _sequentialAccess = (0 != (CommandBehavior.SequentialAccess & behavior));
            if (0 == _depth)
            {
                _singleRow = (0 != (CommandBehavior.SingleRow & behavior));
            }
        }

        internal void InitializeIMultipleResults(object result)
        {
            Initialize();
            _imultipleResults = (UnsafeNativeMethods.IMultipleResults)result; // maybe null if no results
        }
        internal void InitializeIRowset(object result, ChapterHandle chapterHandle, IntPtr recordsAffected)
        {
            // if from ADODB, connection will be null
            if ((null == _connection) || (ChapterHandle.DB_NULL_HCHAPTER != chapterHandle))
            {
                _rowHandleFetchCount = new IntPtr(1);
            }

            Initialize();
            _recordsAffected = recordsAffected;
            _irowset = (UnsafeNativeMethods.IRowset)result; // maybe null if no results
            _chapterHandle = chapterHandle;
        }

        internal void InitializeIRow(object result, IntPtr recordsAffected)
        {
            Initialize();
            Debug.Assert(_singleRow, "SingleRow not already set");
            _singleRow = true;
            _recordsAffected = recordsAffected;
            _irow = (UnsafeNativeMethods.IRow)result; // maybe null if no results
            _hasRows = (null != _irow);
        }

        internal OleDbCommand Command
        {
            get
            {
                return _command;
            }
        }

        override public int Depth
        {
            get
            {
                if (IsClosed)
                {
                    throw ADP.DataReaderClosed("Depth");
                }
                return _depth;
            }
        }

        override public Int32 FieldCount
        {
            get
            {
                if (IsClosed)
                {
                    throw ADP.DataReaderClosed("FieldCount");
                }
                MetaData[] metadata = MetaData;
                return ((null != metadata) ? metadata.Length : 0);
            }
        }

        override public bool HasRows
        {
            get
            {
                if (IsClosed)
                {
                    throw ADP.DataReaderClosed("HasRows");
                }
                return _hasRows;
            }
        }

        override public Boolean IsClosed
        {
            get
            { // if we have a rowset or multipleresults, we may have more to read
                Debug.Assert((_singleRow && !_isClosed && !_isRead && (null == _irow) && (null == _irowset)) ||
                             _isClosed == ((null == _irow) && (null == _irowset) && (null == _imultipleResults)
                                           && (null == _dbSchemaTable) && (null == _connection) && (null == _command)),
                                           "IsClosed mismatch");
                return _isClosed;
            }
        }

        private MetaData[] MetaData
        {
            get { return _metadata; }
        }

        override public int RecordsAffected
        {
            get
            {
                return ADP.IntPtrToInt32(_recordsAffected);
            }
        }

        /*
        internal long RecordsAffectedLong {
            get {
                return (long)_recordsAffected;
            }
        }*/

        override public object this[Int32 index]
        {
            get
            {
                return GetValue(index);
            }
        }

        override public object this[String name]
        {
            get
            {
                int ordinal = GetOrdinal(name);
                return GetValue(ordinal);
            }
        }

        // grouping the native OLE DB casts togther by required interfaces and optional interfaces
        // want these to be methods, not properties otherwise they appear in VS7 managed debugger which attempts to evaluate them

        // required interface, safe cast
        private UnsafeNativeMethods.IAccessor IAccessor()
        {
            return (UnsafeNativeMethods.IAccessor)IRowset();
        }

        // required interface, safe cast
        private UnsafeNativeMethods.IRowsetInfo IRowsetInfo()
        {
            return (UnsafeNativeMethods.IRowsetInfo)IRowset();
        }

        private UnsafeNativeMethods.IRowset IRowset()
        {
            UnsafeNativeMethods.IRowset irowset = _irowset;
            if (null == irowset)
            {
                Debug.Assert(false, "object is disposed");
                throw new ObjectDisposedException(GetType().Name);
            }
            return irowset;
        }

        private UnsafeNativeMethods.IRow IRow()
        {
            UnsafeNativeMethods.IRow irow = _irow;
            if (null == irow)
            {
                Debug.Assert(false, "object is disposed");
                throw new ObjectDisposedException(GetType().Name);
            }
            return irow;
        }

        override public DataTable GetSchemaTable()
        {
            DataTable schemaTable = _dbSchemaTable;
            if (null == schemaTable)
            {
                MetaData[] metadata = MetaData;
                if ((null != metadata) && (0 < metadata.Length))
                {
                    if ((0 < metadata.Length) && _useIColumnsRowset && (null != _connection))
                    {
                        AppendSchemaInfo();
                    }
                    schemaTable = BuildSchemaTable(metadata);
                }
                else if (IsClosed)
                {
                    throw ADP.DataReaderClosed("GetSchemaTable");
                }
                //GetSchemaTable() is defined to return null after NextResult returns false
                //throw ADP.DataReaderNoData();
            }
            return schemaTable;
        }

        internal void BuildMetaInfo()
        {
            Debug.Assert(null == _metadata, "BuildMetaInfo: already built, by _metadata");

            if (null != _irowset)
            {
                if (_useIColumnsRowset)
                {
                    BuildSchemaTableRowset(_irowset);
                }
                else
                {
                    BuildSchemaTableInfo(_irowset, false, false);
                }
                if (null != _metadata && 0 < _metadata.Length)
                {
                    // @devnote: because we want to use the DBACCESSOR_OPTIMIZED bit,
                    // we are required to create the accessor before fetching any rows
                    CreateAccessors(true);
                    Debug.Assert(null != _bindings, "unexpected dbBindings");
                }
            }
            else if (null != _irow)
            {
                BuildSchemaTableInfo(_irow, false, false);
                if (null != _metadata && 0 < _metadata.Length)
                {
                    CreateBindingsFromMetaData(true);
                }
            }
            if (null == _metadata)
            {
                _hasRows = false;
                _visibleFieldCount = 0;
                _metadata = new MetaData[0];
            }
        }

        private DataTable BuildSchemaTable(MetaData[] metadata)
        {
            Debug.Assert(null == _dbSchemaTable, "BuildSchemaTable: schema table already exists");
            Debug.Assert(null != metadata, "BuildSchemaTable: no _metadata");

            DataTable schemaTable = new DataTable("SchemaTable");
            schemaTable.Locale = CultureInfo.InvariantCulture;
            schemaTable.MinimumCapacity = metadata.Length;

            DataColumn name = new DataColumn("ColumnName", typeof(System.String));
            DataColumn ordinal = new DataColumn("ColumnOrdinal", typeof(System.Int32));
            DataColumn size = new DataColumn("ColumnSize", typeof(System.Int32));
            DataColumn precision = new DataColumn("NumericPrecision", typeof(System.Int16));
            DataColumn scale = new DataColumn("NumericScale", typeof(System.Int16));

            DataColumn dataType = new DataColumn("DataType", typeof(System.Type));
            DataColumn providerType = new DataColumn("ProviderType", typeof(System.Int32));

            DataColumn isLong = new DataColumn("IsLong", typeof(System.Boolean));
            DataColumn allowDBNull = new DataColumn("AllowDBNull", typeof(System.Boolean));
            DataColumn isReadOnly = new DataColumn("IsReadOnly", typeof(System.Boolean));
            DataColumn isRowVersion = new DataColumn("IsRowVersion", typeof(System.Boolean));

            DataColumn isUnique = new DataColumn("IsUnique", typeof(System.Boolean));
            DataColumn isKey = new DataColumn("IsKey", typeof(System.Boolean));
            DataColumn isAutoIncrement = new DataColumn("IsAutoIncrement", typeof(System.Boolean));
            DataColumn isHidden = new DataColumn("IsHidden", typeof(System.Boolean));

            DataColumn baseSchemaName = new DataColumn("BaseSchemaName", typeof(System.String));
            DataColumn baseCatalogName = new DataColumn("BaseCatalogName", typeof(System.String));
            DataColumn baseTableName = new DataColumn("BaseTableName", typeof(System.String));
            DataColumn baseColumnName = new DataColumn("BaseColumnName", typeof(System.String));

            ordinal.DefaultValue = 0;
            isLong.DefaultValue = false;

            DataColumnCollection columns = schemaTable.Columns;

            columns.Add(name);
            columns.Add(ordinal);
            columns.Add(size);
            columns.Add(precision);
            columns.Add(scale);

            columns.Add(dataType);
            columns.Add(providerType);

            columns.Add(isLong);
            columns.Add(allowDBNull);
            columns.Add(isReadOnly);
            columns.Add(isRowVersion);

            columns.Add(isUnique);
            columns.Add(isKey);
            columns.Add(isAutoIncrement);
            if (_visibleFieldCount < metadata.Length)
            {
                columns.Add(isHidden);
            }

            columns.Add(baseSchemaName);
            columns.Add(baseCatalogName);
            columns.Add(baseTableName);
            columns.Add(baseColumnName);

            for (int i = 0; i < metadata.Length; ++i)
            {
                MetaData info = metadata[i];

                DataRow newRow = schemaTable.NewRow();
                newRow[name] = info.columnName;
                newRow[ordinal] = i;
                // @devnote: size is count of characters for WSTR or STR, bytes otherwise
                // @devnote: see OLEDB spec under IColumnsInfo::GetColumnInfo
                newRow[size] = ((info.type.enumOleDbType != OleDbType.BSTR) ? info.size : -1);
                newRow[precision] = info.precision;
                newRow[scale] = info.scale;

                newRow[dataType] = info.type.dataType;
                newRow[providerType] = info.type.enumOleDbType;
                newRow[isLong] = OleDbDataReader.IsLong(info.flags);
                if (info.isKeyColumn)
                {
                    newRow[allowDBNull] = OleDbDataReader.AllowDBNull(info.flags);
                }
                else
                {
                    newRow[allowDBNull] = OleDbDataReader.AllowDBNullMaybeNull(info.flags);
                }
                newRow[isReadOnly] = OleDbDataReader.IsReadOnly(info.flags);
                newRow[isRowVersion] = OleDbDataReader.IsRowVersion(info.flags);

                newRow[isUnique] = info.isUnique;
                newRow[isKey] = info.isKeyColumn;
                newRow[isAutoIncrement] = info.isAutoIncrement;
                if (_visibleFieldCount < metadata.Length)
                {
                    newRow[isHidden] = info.isHidden;
                }

                if (null != info.baseSchemaName)
                {
                    newRow[baseSchemaName] = info.baseSchemaName;
                }
                if (null != info.baseCatalogName)
                {
                    newRow[baseCatalogName] = info.baseCatalogName;
                }
                if (null != info.baseTableName)
                {
                    newRow[baseTableName] = info.baseTableName;
                }
                if (null != info.baseColumnName)
                {
                    newRow[baseColumnName] = info.baseColumnName;
                }

                schemaTable.Rows.Add(newRow);
                newRow.AcceptChanges();
            }

            // mark all columns as readonly
            int count = columns.Count;
            for (int i = 0; i < count; i++)
            {
                columns[i].ReadOnly = true;
            }

            _dbSchemaTable = schemaTable;
            return schemaTable;
        }

        private void BuildSchemaTableInfo(object handle, bool filterITypeInfo, bool filterChapters)
        {
            Debug.Assert(null == _dbSchemaTable, "non-null SchemaTable");
            Debug.Assert(null == _metadata, "non-null metadata");
            Debug.Assert(null != handle, "unexpected null rowset");
            UnsafeNativeMethods.IColumnsInfo icolumnsInfo = (handle as UnsafeNativeMethods.IColumnsInfo);
            if (null == icolumnsInfo)
            {
                _dbSchemaTable = null;
#if DEBUG
                if (handle is UnsafeNativeMethods.IRow)
                {
                    Debug.Assert(false, "bad IRow - IColumnsInfo not available");
                }
                else
                {
                    Debug.Assert(handle is UnsafeNativeMethods.IRowset, "bad IRowset - IColumnsInfo not available");
                }
#endif
                return;
            }

            OleDbHResult hr;
            IntPtr columnCount = ADP.PtrZero; // column count
            IntPtr columnInfos = ADP.PtrZero; // ptr to byvalue tagDBCOLUMNINFO[]

            using (DualCoTaskMem safehandle = new DualCoTaskMem(icolumnsInfo, out columnCount, out columnInfos, out hr))
            {
                if (hr < 0)
                {
                    ProcessResults(hr);
                }
                if (0 < (int)columnCount)
                {
                    BuildSchemaTableInfoTable(columnCount.ToInt32(), columnInfos, filterITypeInfo, filterChapters);
                }
            }
        }

        // create DataColumns
        // add DataColumns to DataTable
        // add schema information to DataTable
        // generate unique column names
        private void BuildSchemaTableInfoTable(int columnCount, IntPtr columnInfos, bool filterITypeInfo, bool filterChapters)
        {
            Debug.Assert(0 < columnCount, "BuildSchemaTableInfoTable - no column");

            int rowCount = 0;
            MetaData[] metainfo = new MetaData[columnCount];

            // for every column, build an equivalent to tagDBCOLUMNINFO
            tagDBCOLUMNINFO dbColumnInfo = new tagDBCOLUMNINFO();
            for (int i = 0, offset = 0; i < columnCount; ++i, offset += ODB.SizeOf_tagDBCOLUMNINFO)
            {
                Marshal.PtrToStructure(ADP.IntPtrOffset(columnInfos, offset), dbColumnInfo);
#if WIN32
                if (0 >= (int) dbColumnInfo.iOrdinal) {
#else
                if (0 >= (long)dbColumnInfo.iOrdinal)
                {
#endif
                    continue;
                }
                if (OleDbDataReader.DoColumnDropFilter(dbColumnInfo.dwFlags))
                {
                    continue;
                }

                if (null == dbColumnInfo.pwszName)
                {
                    dbColumnInfo.pwszName = "";
                }
                if (filterITypeInfo && (ODB.DBCOLUMN_TYPEINFO == dbColumnInfo.pwszName))
                {
                    continue;
                }
                if (filterChapters && (NativeDBType.HCHAPTER == dbColumnInfo.wType))
                {
                    continue;  // filter chapters in IRowset from IDBSchemaRowset for DumpToTable
                }

                bool islong = OleDbDataReader.IsLong(dbColumnInfo.dwFlags);
                bool isfixed = OleDbDataReader.IsFixed(dbColumnInfo.dwFlags);
                NativeDBType dbType = NativeDBType.FromDBType(dbColumnInfo.wType, islong, isfixed);

                MetaData info = new MetaData();
                info.columnName = dbColumnInfo.pwszName;
                info.type = dbType;
                info.ordinal = dbColumnInfo.iOrdinal;
#if WIN32
                    info.size = (int)dbColumnInfo.ulColumnSize;
#else
                long maxsize = (long)dbColumnInfo.ulColumnSize;
                info.size = (((maxsize < 0) || (Int32.MaxValue < maxsize)) ? Int32.MaxValue : (int)maxsize);
#endif
                info.flags = dbColumnInfo.dwFlags;
                info.precision = dbColumnInfo.bPrecision;
                info.scale = dbColumnInfo.bScale;

                info.kind = dbColumnInfo.columnid.eKind;
                switch (dbColumnInfo.columnid.eKind)
                {
                    case ODB.DBKIND_GUID_NAME:
                    case ODB.DBKIND_GUID_PROPID:
                    case ODB.DBKIND_GUID:
                        info.guid = dbColumnInfo.columnid.uGuid;
                        break;
                    default:
                        Debug.Assert(ODB.DBKIND_PGUID_NAME != dbColumnInfo.columnid.eKind, "OLE DB providers never return pGuid-style bindings.");
                        Debug.Assert(ODB.DBKIND_PGUID_PROPID != dbColumnInfo.columnid.eKind, "OLE DB providers never return pGuid-style bindings.");
                        info.guid = Guid.Empty;
                        break;
                }
                switch (dbColumnInfo.columnid.eKind)
                {
                    case ODB.DBKIND_GUID_PROPID:
                    case ODB.DBKIND_PROPID:
                        info.propid = dbColumnInfo.columnid.ulPropid;
                        break;
                    case ODB.DBKIND_GUID_NAME:
                    case ODB.DBKIND_NAME:
                        if (ADP.PtrZero != dbColumnInfo.columnid.ulPropid)
                        {
                            info.idname = Marshal.PtrToStringUni(dbColumnInfo.columnid.ulPropid);
                        }
                        else
                        {
                            info.idname = null;
                        }
                        break;
                    default:
                        info.propid = ADP.PtrZero;
                        break;
                }
                metainfo[rowCount] = info;

#if DEBUG
                if (AdapterSwitches.DataSchema.TraceVerbose)
                {
                    Debug.WriteLine("OleDbDataReader[" + info.ordinal.ToInt64().ToString(CultureInfo.InvariantCulture) + ", " + dbColumnInfo.pwszName + "]=" + dbType.enumOleDbType.ToString() + "," + dbType.dataSourceType + ", " + dbType.wType);
                }
#endif
                rowCount++;
            }
            if (rowCount < columnCount)
            { // shorten names array appropriately
                MetaData[] tmpinfo = new MetaData[rowCount];
                for (int i = 0; i < rowCount; ++i)
                {
                    tmpinfo[i] = metainfo[i];
                }
                metainfo = tmpinfo;
            }
            _visibleFieldCount = rowCount;
            _metadata = metainfo;
        }

        private void BuildSchemaTableRowset(object handle)
        {
            Debug.Assert(null == _dbSchemaTable, "BuildSchemaTableRowset - non-null SchemaTable");
            Debug.Assert(null != handle, "BuildSchemaTableRowset(object) - unexpected null handle");
            UnsafeNativeMethods.IColumnsRowset icolumnsRowset = (handle as UnsafeNativeMethods.IColumnsRowset);

            if (null != icolumnsRowset)
            {
                UnsafeNativeMethods.IRowset rowset = null;
                IntPtr cOptColumns;
                OleDbHResult hr;

                using (DualCoTaskMem prgOptColumns = new DualCoTaskMem(icolumnsRowset, out cOptColumns, out hr))
                {
                    Debug.Assert((0 == hr) || prgOptColumns.IsInvalid, "GetAvailableCOlumns: unexpected return");
                    hr = icolumnsRowset.GetColumnsRowset(ADP.PtrZero, cOptColumns, prgOptColumns, ref ODB.IID_IRowset, 0, ADP.PtrZero, out rowset);
                }

                Debug.Assert((0 <= hr) || (null == rowset), "if GetColumnsRowset failed, rowset should be null");
                if (hr < 0)
                {
                    ProcessResults(hr);
                }
                DumpToSchemaTable(rowset);

                // release the rowset to avoid race condition between the GC and the user code causing
                // "Connection is busy with results for another command" exception
                if (null != rowset)
                {
                    Marshal.ReleaseComObject(rowset);
                }
            }
            else
            {
                _useIColumnsRowset = false;
                BuildSchemaTableInfo(handle, false, false);
            }
        }

        override public void Close()
        {
            OleDbConnection con = _connection;
            OleDbCommand cmd = _command;
            Bindings bindings = _parameterBindings;
            _connection = null;
            _command = null;
            _parameterBindings = null;

            _isClosed = true;

            DisposeOpenResults();
            _hasRows = false;

            if ((null != cmd) && cmd.canceling)
            {
                DisposeNativeMultipleResults();

                if (null != bindings)
                {
                    bindings.CloseFromConnection();
                    bindings = null;
                }
            }
            else
            {
                UnsafeNativeMethods.IMultipleResults multipleResults = _imultipleResults;
                _imultipleResults = null;

                if (null != multipleResults)
                {
                    // if we don't have a cmd, same as a cancel (don't call NextResults) which is ADODB behavior

                    try
                    {
                        // tricky code path is an exception is thrown
                        // causing connection to do a ResetState and connection.Close
                        // resulting in OleDbCommand.CloseFromConnection
                        if ((null != cmd) && !cmd.canceling)
                        {
                            IntPtr affected = IntPtr.Zero;
                            OleDbException nextResultsFailure = NextResults(multipleResults, null, cmd, out affected);
                            _recordsAffected = AddRecordsAffected(_recordsAffected, affected);
                            if (null != nextResultsFailure)
                            {
                                throw nextResultsFailure;
                            }
                        }
                    }
                    finally
                    {
                        if (null != multipleResults)
                        {
                            Marshal.ReleaseComObject(multipleResults);
                        }
                    }
                }
            }

            if ((null != cmd) && (0 == _depth))
            {
                // return bindings back to the cmd after closure of root DataReader
                cmd.CloseFromDataReader(bindings);
            }

            if (null != con)
            {
                con.RemoveWeakReference(this);

                // if the DataReader is Finalized it will not close the connection
                if (IsCommandBehavior(CommandBehavior.CloseConnection))
                {
                    con.Close();
                }
            }

            // release unmanaged objects
            RowHandleBuffer rowHandleNativeBuffer = _rowHandleNativeBuffer;
            _rowHandleNativeBuffer = null;
            if (null != rowHandleNativeBuffer)
            {
                rowHandleNativeBuffer.Dispose();
            }
        }

        internal void CloseReaderFromConnection(bool canceling)
        {
            // being called from the connection, we will have a command. that command
            // may be Disposed, but it doesn't matter since another command can't execute
            // until all DataReader are closed
            if (null != _command)
            { // UNDONE: understand why this could be null, it shouldn't be but was
                _command.canceling = canceling;
            }

            // called from the connection which will remove this from its ReferenceCollection
            // we want the NextResult behavior, but no errors
            _connection = null;

            Close();
        }

        private void DisposeManagedRowset()
        {
            //not cleared after last rowset
            //_hasRows = false;

            _isRead = false;
            _hasRowsReadCheck = false;

            _nextAccessorForRetrieval = 0;
            _nextValueForRetrieval = 0;

            Bindings[] bindings = _bindings;
            _bindings = null;

            if (null != bindings)
            {
                for (int i = 0; i < bindings.Length; ++i)
                {
                    if (null != bindings[i])
                    {
                        bindings[i].Dispose();
                    }
                }
            }

            _currentRow = 0;
            _rowFetchedCount = IntPtr.Zero;

            _dbSchemaTable = null;
            _visibleFieldCount = 0;
            _metadata = null;
            _fieldNameLookup = null;
        }

        private void DisposeNativeMultipleResults()
        {
            UnsafeNativeMethods.IMultipleResults imultipleResults = _imultipleResults;
            _imultipleResults = null;

            if (null != imultipleResults)
            {
                Marshal.ReleaseComObject(imultipleResults);
            }
        }

        private void DisposeNativeRowset()
        {
            UnsafeNativeMethods.IRowset irowset = _irowset;
            _irowset = null;

            ChapterHandle chapter = _chapterHandle;
            _chapterHandle = ChapterHandle.DB_NULL_HCHAPTER;

            if (ChapterHandle.DB_NULL_HCHAPTER != chapter)
            {
                chapter.Dispose();
            }

            if (null != irowset)
            {
                Marshal.ReleaseComObject(irowset);
            }
        }

        private void DisposeNativeRow()
        {
            UnsafeNativeMethods.IRow irow = _irow;
            _irow = null;

            if (null != irow)
            {
                Marshal.ReleaseComObject(irow);
            }
        }

        private void DisposeOpenResults()
        {
            DisposeManagedRowset();

            DisposeNativeRow();
            DisposeNativeRowset();
        }

        override public Boolean GetBoolean(int ordinal)
        {
            ColumnBinding binding = GetColumnBinding(ordinal);
            return binding.ValueBoolean();
        }

        override public Byte GetByte(int ordinal)
        {
            ColumnBinding binding = GetColumnBinding(ordinal);
            return binding.ValueByte();
        }

        private ColumnBinding DoSequentialCheck(int ordinal, Int64 dataIndex, string method)
        {
            ColumnBinding binding = GetColumnBinding(ordinal);

            if (dataIndex > Int32.MaxValue)
            {
                throw ADP.InvalidSourceBufferIndex(0, dataIndex, "dataIndex");
            }
            if (_sequentialOrdinal != ordinal)
            {
                _sequentialOrdinal = ordinal;
                _sequentialBytesRead = 0;
            }
            else if (_sequentialAccess && (_sequentialBytesRead < dataIndex))
            {
                throw ADP.NonSeqByteAccess(dataIndex, _sequentialBytesRead, method);
            }
            // getting the value doesn't really belong, but it's common to both
            // callers GetBytes and GetChars so we might as well have the code here
            return binding;
        }

        override public Int64 GetBytes(int ordinal, Int64 dataIndex, byte[] buffer, Int32 bufferIndex, Int32 length)
        {
            ColumnBinding binding = DoSequentialCheck(ordinal, dataIndex, ADP.GetBytes);
            byte[] value = binding.ValueByteArray();

            if (null == buffer)
            {
                return value.Length;
            }
            int srcIndex = (int)dataIndex;
            int byteCount = Math.Min(value.Length - srcIndex, length);
            if (srcIndex < 0)
            {
                throw ADP.InvalidSourceBufferIndex(value.Length, srcIndex, "dataIndex");
            }
            else if ((bufferIndex < 0) || (bufferIndex >= buffer.Length))
            {
                throw ADP.InvalidDestinationBufferIndex(buffer.Length, bufferIndex, "bufferIndex");
            }
            if (0 < byteCount)
            {
                // @usernote: user may encounter ArgumentException from Buffer.BlockCopy
                Buffer.BlockCopy(value, srcIndex, buffer, bufferIndex, byteCount);
                _sequentialBytesRead = srcIndex + byteCount;
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

        override public Int64 GetChars(int ordinal, Int64 dataIndex, char[] buffer, Int32 bufferIndex, Int32 length)
        {
            ColumnBinding binding = DoSequentialCheck(ordinal, dataIndex, ADP.GetChars);
            string value = binding.ValueString();

            if (null == buffer)
            {
                return value.Length;
            }

            int srcIndex = (int)dataIndex;
            int charCount = Math.Min(value.Length - srcIndex, length);
            if (srcIndex < 0)
            {
                throw ADP.InvalidSourceBufferIndex(value.Length, srcIndex, "dataIndex");
            }
            else if ((bufferIndex < 0) || (bufferIndex >= buffer.Length))
            {
                throw ADP.InvalidDestinationBufferIndex(buffer.Length, bufferIndex, "bufferIndex");
            }
            if (0 < charCount)
            {
                // @usernote: user may encounter ArgumentException from String.CopyTo
                value.CopyTo(srcIndex, buffer, bufferIndex, charCount);
                _sequentialBytesRead = srcIndex + charCount;
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

        [EditorBrowsable(EditorBrowsableState.Never)]
        override public Char GetChar(int ordinal)
        {
            throw ADP.NotSupported();
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        new public OleDbDataReader GetData(int ordinal)
        {
            ColumnBinding binding = GetColumnBinding(ordinal);
            return binding.ValueChapter();
        }

        override protected DbDataReader GetDbDataReader(int ordinal)
        {
            return GetData(ordinal);
        }

        internal OleDbDataReader ResetChapter(int bindingIndex, int index, RowBinding rowbinding, int valueOffset)
        {
            return GetDataForReader(_metadata[bindingIndex + index].ordinal, rowbinding, valueOffset);
        }

        private OleDbDataReader GetDataForReader(IntPtr ordinal, RowBinding rowbinding, int valueOffset)
        {
            UnsafeNativeMethods.IRowsetInfo rowsetInfo = IRowsetInfo();
            UnsafeNativeMethods.IRowset result;
            OleDbHResult hr;
            hr = rowsetInfo.GetReferencedRowset((IntPtr)ordinal, ref ODB.IID_IRowset, out result);

            ProcessResults(hr);

            OleDbDataReader reader = null;
            if (null != result)
            {
                // only when the first datareader is closed will the connection close
                ChapterHandle chapterHandle = ChapterHandle.CreateChapterHandle(result, rowbinding, valueOffset);
                reader = new OleDbDataReader(_connection, _command, 1 + Depth, _commandBehavior & ~CommandBehavior.CloseConnection);
                reader.InitializeIRowset(result, chapterHandle, ADP.RecordsUnaffected);
                reader.BuildMetaInfo();
                reader.HasRowsRead();

                if (null != _connection)
                {
                    // connection tracks all readers to prevent cmd from executing
                    // until all readers (including nested) are closed
                    _connection.AddWeakReference(reader, OleDbReferenceCollection.DataReaderTag);
                }
            }
            return reader;
        }

        override public String GetDataTypeName(int index)
        {
            if (null != _metadata)
            {
                return _metadata[index].type.dataSourceType;
            }
            throw ADP.DataReaderNoData();
        }

        override public DateTime GetDateTime(int ordinal)
        {
            ColumnBinding binding = GetColumnBinding(ordinal);
            return binding.ValueDateTime();
        }

        override public Decimal GetDecimal(int ordinal)
        {
            ColumnBinding binding = GetColumnBinding(ordinal);
            return binding.ValueDecimal();
        }

        override public Double GetDouble(int ordinal)
        {
            ColumnBinding binding = GetColumnBinding(ordinal);
            return binding.ValueDouble();
        }

        override public IEnumerator GetEnumerator()
        {
            return new DbEnumerator((IDataReader)this, IsCommandBehavior(CommandBehavior.CloseConnection));
        }

        override public Type GetFieldType(int index)
        {
            if (null != _metadata)
            {
                return _metadata[index].type.dataType;
            }
            throw ADP.DataReaderNoData();
        }

        override public Single GetFloat(int ordinal)
        {
            ColumnBinding binding = GetColumnBinding(ordinal);
            return binding.ValueSingle();
        }

        override public Guid GetGuid(int ordinal)
        {
            ColumnBinding binding = GetColumnBinding(ordinal);
            return binding.ValueGuid();
        }

        override public Int16 GetInt16(int ordinal)
        {
            ColumnBinding binding = GetColumnBinding(ordinal);
            return binding.ValueInt16();
        }

        override public Int32 GetInt32(int ordinal)
        {
            ColumnBinding binding = GetColumnBinding(ordinal);
            return binding.ValueInt32();
        }

        override public Int64 GetInt64(int ordinal)
        {
            ColumnBinding binding = GetColumnBinding(ordinal);
            return binding.ValueInt64();
        }

        override public String GetName(int index)
        {
            if (null != _metadata)
            {
                Debug.Assert(null != _metadata[index].columnName);
                return _metadata[index].columnName;
            }
            throw ADP.DataReaderNoData();
        }

        override public Int32 GetOrdinal(String name)
        {
            if (null == _fieldNameLookup)
            {
                if (null == _metadata)
                {
                    throw ADP.DataReaderNoData();
                }
                _fieldNameLookup = new FieldNameLookup(this, -1);
            }
            return _fieldNameLookup.GetOrdinal(name);
        }

        override public String GetString(int ordinal)
        {
            ColumnBinding binding = GetColumnBinding(ordinal);
            return binding.ValueString();
        }

        public TimeSpan GetTimeSpan(int ordinal)
        {
            return (TimeSpan)GetValue(ordinal);
        }

        private MetaData DoValueCheck(int ordinal)
        {
            if (!_isRead)
            {
                // Read hasn't been called yet or no more data
                throw ADP.DataReaderNoData();
            }
            else if (_sequentialAccess && (ordinal < _nextValueForRetrieval))
            {
                throw ADP.NonSequentialColumnAccess(ordinal, _nextValueForRetrieval);
            }
            // @usernote: user may encounter the IndexOutOfRangeException
            MetaData info = _metadata[ordinal];
            return info;
        }

        private ColumnBinding GetColumnBinding(int ordinal)
        {
            MetaData info = DoValueCheck(ordinal);
            return GetValueBinding(info);
        }

        private ColumnBinding GetValueBinding(MetaData info)
        {
            ColumnBinding binding = info.columnBinding;
            Debug.Assert(null != binding, "null binding");

            // do we need to jump to the next accessor
            for (int i = _nextAccessorForRetrieval; i <= binding.IndexForAccessor; ++i)
            {
                Debug.Assert(_nextAccessorForRetrieval <= binding.IndexForAccessor, "backwards index for accessor");
                Debug.Assert(_nextAccessorForRetrieval == i, "failed to increment");

                if (_sequentialAccess)
                {
                    if (_nextValueForRetrieval != binding.Index)
                    { // release old value
                        _metadata[_nextValueForRetrieval].columnBinding.ResetValue();
                    }
                    _nextAccessorForRetrieval = binding.IndexForAccessor;
                }

                if (null != _irowset)
                {
                    GetRowDataFromHandle(); // will increment _nextAccessorForRetrieval
                }
                else if (null != _irow)
                {
                    GetRowValue(); // will increment _nextAccessorForRetrieval
                }
                else
                {
                    throw ADP.DataReaderNoData();
                }
            }

            // to enforce sequential access
            _nextValueForRetrieval = binding.Index;
            return binding;
        }

        override public object GetValue(int ordinal)
        {
            ColumnBinding binding = GetColumnBinding(ordinal);
            object value = binding.Value();
            return value;
        }

        override public Int32 GetValues(object[] values)
        {
            if (null == values)
            {
                throw ADP.ArgumentNull("values");
            }
            MetaData info = DoValueCheck(0);
            int count = Math.Min(values.Length, _visibleFieldCount);
            for (int i = 0; (i < _metadata.Length) && (i < count); ++i)
            {
                ColumnBinding binding = GetValueBinding(_metadata[i]);
                values[i] = binding.Value();
            }
            return count;
        }

        private bool IsCommandBehavior(CommandBehavior condition)
        {
            return (condition == (condition & _commandBehavior));
        }

        override public Boolean IsDBNull(int ordinal)
        {
            ColumnBinding binding = GetColumnBinding(ordinal);
            return binding.IsValueNull();
        }

        private void ProcessResults(OleDbHResult hr)
        {
            Exception e;
            if (null != _command)
            {
                e = OleDbConnection.ProcessResults(hr, _connection, _command);
            }
            else
            {
                e = OleDbConnection.ProcessResults(hr, _connection, _connection);
            }
            if (null != e)
            { throw e; }
        }

        static private IntPtr AddRecordsAffected(IntPtr recordsAffected, IntPtr affected)
        {
#if WIN32
            if (0 <= (int)affected) {
                if (0 <= (int)recordsAffected) {
                    return (IntPtr)((int)recordsAffected + (int)affected);
#else
            if (0 <= (long)affected)
            {
                if (0 <= (long)recordsAffected)
                {
                    return (IntPtr)((long)recordsAffected + (long)affected);
#endif
                }
                return affected;
            }
            return recordsAffected;
        }

        override public int VisibleFieldCount
        {
            get
            {
                if (IsClosed)
                {
                    throw ADP.DataReaderClosed("VisibleFieldCount");
                }
                return _visibleFieldCount;
            }
        }

        internal void HasRowsRead()
        {
            Debug.Assert(!_hasRowsReadCheck, "_hasRowsReadCheck not reset");
            bool flag = Read();
            _hasRows = flag;
            _hasRowsReadCheck = true;
            _isRead = false;
        }

        static internal OleDbException NextResults(UnsafeNativeMethods.IMultipleResults imultipleResults, OleDbConnection connection, OleDbCommand command, out IntPtr recordsAffected)
        {
            recordsAffected = ADP.RecordsUnaffected;
            List<OleDbException> exceptions = null;
            if (null != imultipleResults)
            {
                object result;
                IntPtr affected;
                OleDbHResult hr;

                // MSOLAP provider doesn't move onto the next result when calling GetResult with IID_NULL, but does return S_OK with 0 affected records.
                // we want to break out of that infinite loop for ExecuteNonQuery and the multiple result Close scenarios
                for (int loop = 0; ; ++loop)
                {
                    if ((null != command) && command.canceling)
                    {
                        break;
                    }
                    hr = imultipleResults.GetResult(ADP.PtrZero, ODB.DBRESULTFLAG_DEFAULT, ref ODB.IID_NULL, out affected, out result);

                    // If a provider doesn't support IID_NULL and returns E_NOINTERFACE we want to break out
                    // of the loop without throwing an exception.  Our behavior will match ADODB in that scenario
                    // where Recordset.Close just releases the interfaces without proccessing remaining results
                    if ((OleDbHResult.DB_S_NORESULT == hr) || (OleDbHResult.E_NOINTERFACE == hr))
                    {
                        break;
                    }
                    if (null != connection)
                    {
                        Exception e = OleDbConnection.ProcessResults(hr, connection, command);
                        if (null != e)
                        {
                            OleDbException excep = (e as OleDbException);
                            if (null != excep)
                            {
                                if (null == exceptions)
                                {
                                    exceptions = new List<OleDbException>();
                                }
                                exceptions.Add(excep);
                            }
                            else
                            {
                                Debug.Assert(OleDbHResult.DB_E_OBJECTOPEN == hr, "unexpected");
                                throw e; // we don't expect to be here, but it could happen
                            }
                        }
                    }
                    else if (hr < 0)
                    {
                        SafeNativeMethods.Wrapper.ClearErrorInfo();
                        break;
                    }
                    recordsAffected = AddRecordsAffected(recordsAffected, affected);

                    if (0 != (int)affected)
                    {
                        loop = 0;
                    }
                    else if (2000 <= loop)
                    { // (reason for more than 1000 iterations)
                        NextResultsInfinite();
                        break;
                    }
                }
            }
            if (null != exceptions)
            {
                return OleDbException.CombineExceptions(exceptions);
            }
            return null;
        }

        static private void NextResultsInfinite()
        {
            // edtriou's suggestion is that we debug assert so that users will learn of MSOLAP's misbehavior and not call ExecuteNonQuery
            Debug.Assert(false, "<oledb.OleDbDataReader.NextResultsInfinite|INFO> System.Data.OleDb.OleDbDataReader: 2000 IMultipleResult.GetResult(NULL, DBRESULTFLAG_DEFAULT, IID_NULL, NULL, NULL) iterations with 0 records affected. Stopping suspect infinite loop. To work-around try using ExecuteReader() and iterating through results with NextResult().\n");
        }

        override public bool NextResult()
        {
            bool retflag = false;
            if (IsClosed)
            {
                throw ADP.DataReaderClosed("NextResult");
            }
            _fieldNameLookup = null;

            OleDbCommand command = _command;
            UnsafeNativeMethods.IMultipleResults imultipleResults = _imultipleResults;
            if (null != imultipleResults)
            {
                DisposeOpenResults();
                _hasRows = false;

                for (; ; )
                {
                    Debug.Assert(null == _irow, "NextResult: row loop check");
                    Debug.Assert(null == _irowset, "NextResult: rowset loop check");

                    object result = null;
                    OleDbHResult hr;
                    IntPtr affected;

                    if ((null != command) && command.canceling)
                    {
                        Close();
                        break;
                    }
                    hr = imultipleResults.GetResult(ADP.PtrZero, ODB.DBRESULTFLAG_DEFAULT, ref ODB.IID_IRowset, out affected, out result);

                    if ((0 <= hr) && (null != result))
                    {
                        _irowset = (UnsafeNativeMethods.IRowset)result;
                    }
                    _recordsAffected = AddRecordsAffected(_recordsAffected, affected);

                    if (OleDbHResult.DB_S_NORESULT == hr)
                    {
                        DisposeNativeMultipleResults();
                        break;
                    }
                    // @devnote: infomessage events may be fired from here
                    ProcessResults(hr);

                    if (null != _irowset)
                    {
                        BuildMetaInfo();
                        HasRowsRead();
                        retflag = true;
                        break;
                    }
                }
            }
            else
            {
                DisposeOpenResults();
                _hasRows = false;
            }
            return retflag;
        }

        override public bool Read()
        {
            bool retflag = false;
            OleDbCommand command = _command;
            if ((null != command) && command.canceling)
            {
                DisposeOpenResults();
            }
            else if (null != _irowset)
            {
                if (_hasRowsReadCheck)
                {
                    _isRead = retflag = _hasRows;
                    _hasRowsReadCheck = false;
                }
                else if (_singleRow && _isRead)
                {
                    DisposeOpenResults();
                }
                else
                {
                    retflag = ReadRowset();
                }
            }
            else if (null != _irow)
            {
                retflag = ReadRow();
            }
            else if (IsClosed)
            {
                throw ADP.DataReaderClosed("Read");
            }
            return retflag;
        }

        private bool ReadRow()
        {
            if (_isRead)
            {
                _isRead = false; // for DoValueCheck

                DisposeNativeRow();

                _sequentialOrdinal = -1; // sequentialBytesRead will reset when used
            }
            else
            {
                _isRead = true;
                return (0 < _metadata.Length);
            }
            return false;
        }

        private bool ReadRowset()
        {
            Debug.Assert(null != _irowset, "ReadRow: null IRowset");
            Debug.Assert(0 <= _metadata.Length, "incorrect state for fieldCount");

            // releases bindings as necessary
            // bumps current row, else resets it back to initial state
            ReleaseCurrentRow();

            _sequentialOrdinal = -1; // sequentialBytesRead will reset when used

            // making the check if (null != irowset) unnecessary
            // if necessary, get next group of row handles
            if (IntPtr.Zero == _rowFetchedCount)
            { // starts at (-1 <= 0)
                Debug.Assert(0 == _currentRow, "incorrect state for _currentRow");
                Debug.Assert(0 <= _metadata.Length, "incorrect state for fieldCount");
                Debug.Assert(0 == _nextAccessorForRetrieval, "incorrect state for nextAccessorForRetrieval");
                Debug.Assert(0 == _nextValueForRetrieval, "incorrect state for nextValueForRetrieval");

                // @devnote: releasing row handles occurs next time user calls read, skip, or close
                GetRowHandles(/*skipCount*/);
            }
            return ((_currentRow <= (int)_rowFetchedCount) && _isRead);
        }

        private void ReleaseCurrentRow()
        {
            Debug.Assert(null != _irowset, "ReleaseCurrentRow: no rowset");

            if (0 < (int)_rowFetchedCount)
            {
                // release the data in the current row
                Bindings[] bindings = _bindings;
                Debug.Assert(null != bindings, "ReleaseCurrentRow: null dbBindings");
                for (int i = 0; (i < bindings.Length) && (i < _nextAccessorForRetrieval); ++i)
                {
                    bindings[i].CleanupBindings();
                }
                _nextAccessorForRetrieval = 0;
                _nextValueForRetrieval = 0;

                _currentRow++;
                if (_currentRow == (int)_rowFetchedCount)
                {
                    ReleaseRowHandles();
                }
            }
        }

        private void CreateAccessors(bool allowMultipleAccessor)
        {
            Debug.Assert(null == _bindings, "CreateAccessor: dbBindings already exists");
            Debug.Assert(null != _irowset, "CreateAccessor: no IRowset available");
            Debug.Assert(null != _metadata && 0 < _metadata.Length, "no columns");

            Bindings[] dbBindings = CreateBindingsFromMetaData(allowMultipleAccessor);

            UnsafeNativeMethods.IAccessor iaccessor = IAccessor();
            for (int i = 0; i < dbBindings.Length; ++i)
            {
                OleDbHResult hr = dbBindings[i].CreateAccessor(iaccessor, ODB.DBACCESSOR_ROWDATA);
                if (hr < 0)
                {
                    ProcessResults(hr);
                }
            }

            if (IntPtr.Zero == _rowHandleFetchCount)
            {
                _rowHandleFetchCount = new IntPtr(1);

                object maxRows = GetPropertyValue(ODB.DBPROP_MAXROWS);
                if (maxRows is Int32)
                {
                    _rowHandleFetchCount = new IntPtr((int)maxRows);
                    if ((ADP.PtrZero == _rowHandleFetchCount) || (20 <= (int)_rowHandleFetchCount))
                    {
                        _rowHandleFetchCount = new IntPtr(20);
                    }
                }
                else if (maxRows is Int64)
                {
                    _rowHandleFetchCount = new IntPtr((long)maxRows);
                    if ((ADP.PtrZero == _rowHandleFetchCount) || (20 <= (long)_rowHandleFetchCount))
                    {
                        _rowHandleFetchCount = new IntPtr(20);
                    }
                }
            }
            if (null == _rowHandleNativeBuffer)
            {
                _rowHandleNativeBuffer = new RowHandleBuffer(_rowHandleFetchCount);
            }
        }

        private Bindings[] CreateBindingsFromMetaData(bool allowMultipleAccessor)
        {
            int bindingCount = 0;
            int currentBindingIndex = 0;

            MetaData[] metadata = _metadata;

            int[] indexToBinding = new int[metadata.Length];
            int[] indexWithinBinding = new int[metadata.Length];

            // walk through the schemaRows to determine the number of binding groups
            if (allowMultipleAccessor)
            {
                if (null != _irowset)
                {
                    for (int i = 0; i < indexToBinding.Length; ++i)
                    {
                        indexToBinding[i] = bindingCount;
                        indexWithinBinding[i] = currentBindingIndex;
#if false
                        // @denote: single/multiple Accessors
                        if ((bindingCount < 2) && IsLong(metadata[i].flags)) {
                            bindingCount++;
                            currentBindingIndex = 0;
                        }
                        else {
                            currentBindingIndex++;
                        }
#elif false
                        // @devnote: one accessor per column option
                        bindingCount++;
                        currentBindingIndex = 0;
#else
                        // @devnote: one accessor only for IRowset
                        currentBindingIndex++;
#endif
                    }
                    if (0 < currentBindingIndex)
                    { // when blob is not the last column
                        bindingCount++;
                    }
                }
                else if (null != _irow)
                {
                    for (int i = 0; i < indexToBinding.Length; ++i)
                    {
                        indexToBinding[i] = i;
                        indexWithinBinding[i] = 0;
                    }
                    bindingCount = metadata.Length;
                }
            }
            else
            {
                for (int i = 0; i < indexToBinding.Length; ++i)
                {
                    indexToBinding[i] = 0;
                    indexWithinBinding[i] = i;
                }
                bindingCount = 1;
            }

            Bindings bindings;
            Bindings[] dbbindings = new Bindings[bindingCount];
            bindingCount = 0;

            // for every column, build tagDBBinding info
            for (int index = 0; index < metadata.Length; ++index)
            {
                Debug.Assert(indexToBinding[index] < dbbindings.Length, "bad indexToAccessor");
                bindings = dbbindings[indexToBinding[index]];
                if (null == bindings)
                {
                    bindingCount = 0;
                    for (int i = index; (i < metadata.Length) && (bindingCount == indexWithinBinding[i]); ++i)
                    {
                        bindingCount++;
                    }
                    dbbindings[indexToBinding[index]] = bindings = new Bindings((OleDbDataReader)this, (null != _irowset), bindingCount);

                    // runningTotal is buffered to start values on 16-byte boundary
                    // the first columnCount * 8 bytes are for the length and status fields
                    //bindings.DataBufferSize = (bindingCount + (bindingCount % 2)) * sizeof_int64;
                }
                MetaData info = metadata[index];

                int maxLen = info.type.fixlen;
                short getType = info.type.wType;

                Debug.Assert(NativeDBType.STR != getType, "Should have bound as WSTR");
                Debug.Assert(!NativeDBType.HasHighBit(getType), "CreateAccessor - unexpected high bits on datatype");

                if (-1 != info.size)
                {
                    if (info.type.islong)
                    {
                        maxLen = ADP.PtrSize;
                        getType = (short)((ushort)getType | (ushort)NativeDBType.BYREF);
                    }
                    else if (-1 == maxLen)
                    {
                        // @devnote: not using provider owned memory for PDC, no one really supports it anyway.
                        /*if (((null != connection) && connection.PropertyGetProviderOwnedMemory())
                            || ((null != command) && command.Connection.PropertyGetProviderOwnedMemory())) {
                            bindings.MemOwner = DBMemOwner.ProviderOwned;

                            bindings.MaxLen = ADP.PtrSize;
                            bindings.DbType = (short) (getType | DbType.BYREF);
                        }
                        else*/

                        if (ODB.LargeDataSize < info.size)
                        {
                            maxLen = ADP.PtrSize;
                            getType = (short)((ushort)getType | (ushort)NativeDBType.BYREF);
                        }
                        else if ((NativeDBType.WSTR == getType) && (-1 != info.size))
                        {
                            maxLen = info.size * 2 + 2;
                        }
                        else
                        {
                            maxLen = info.size;
                        }
                    }
                }
                else if (maxLen < 0)
                {
                    // if variable length and no defined size we require this to be byref at this time
                    /*if (((null != connection) && connection.PropertyGetProviderOwnedMemory())
                        || ((null != command) && command.Connection.PropertyGetProviderOwnedMemory())) {
                        bindings.MemOwner = DBMemOwner.ProviderOwned;
                    }*/
                    maxLen = ADP.PtrSize;
                    getType = (short)((ushort)getType | (ushort)NativeDBType.BYREF);
                }

                currentBindingIndex = indexWithinBinding[index];
                bindings.CurrentIndex = currentBindingIndex;

                bindings.Ordinal = info.ordinal;
                bindings.Part = info.type.dbPart;
                bindings.Precision = (byte)info.precision;
                bindings.Scale = (byte)info.scale;
                bindings.DbType = (short)getType;
                bindings.MaxLen = maxLen; // also increments databuffer size (uses DbType)

                //bindings.ValueOffset  = // set via MaxLen
                //bindings.LengthOffset = // set via MaxLen
                //bindings.StatusOffset = // set via MaxLen
                //bindings.TypeInfoPtr  = 0;
                //bindings.ObjectPtr    = 0;
                //bindings.BindExtPtr   = 0;
                //bindings.MemOwner     = /*DBMEMOWNER_CLIENTOWNED*/0;
                //bindings.ParamIO      = ODB.DBPARAMIO_NOTPARAM;
                //bindings.Flags        = 0;
            }

            int count = 0, indexStart = 0;
            for (int i = 0; i < dbbindings.Length; ++i)
            {
                indexStart = dbbindings[i].AllocateForAccessor(this, indexStart, i);

                ColumnBinding[] columnBindings = dbbindings[i].ColumnBindings();
                for (int k = 0; k < columnBindings.Length; ++k)
                {
                    Debug.Assert(count == columnBindings[k].Index, "column binding mismatch");
                    metadata[count].columnBinding = columnBindings[k];
                    metadata[count].bindings = dbbindings[i];
                    count++;
                }
            }

            _bindings = dbbindings;
            return dbbindings;
        }

        private void GetRowHandles(/*int skipCount*/)
        {
            Debug.Assert(0 < (int)_rowHandleFetchCount, "GetRowHandles: bad _rowHandleFetchCount");
            Debug.Assert(!_isRead, "GetRowHandles: _isRead");

            OleDbHResult hr = 0;

            RowHandleBuffer rowHandleBuffer = _rowHandleNativeBuffer;
            bool mustRelease = false;

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                rowHandleBuffer.DangerousAddRef(ref mustRelease);

                IntPtr rowHandlesPtr = rowHandleBuffer.DangerousGetHandle();
                UnsafeNativeMethods.IRowset irowset = IRowset();
                try
                {
                    hr = irowset.GetNextRows(_chapterHandle.HChapter, /*skipCount*/IntPtr.Zero, _rowHandleFetchCount, out _rowFetchedCount, ref rowHandlesPtr);
                    Debug.Assert(rowHandleBuffer.DangerousGetHandle() == rowHandlesPtr, "rowhandlebuffer changed");
                }
                catch (System.InvalidCastException e)
                {
                    throw ODB.ThreadApartmentState(e);
                }
            }
            finally
            {
                if (mustRelease)
                {
                    rowHandleBuffer.DangerousRelease();
                }
            }

            //if (/*DB_S_ROWLIMITEXCEEDED*/0x00040EC0 == hr) {
            //    _rowHandleFetchCount = 1;
            //    _isRead = true;
            //} else
            if (hr < 0)
            {
                // filter out the BadStartPosition due to the skipCount which
                // maybe greater than the number of rows in the return rowset
                //const int /*OLEDB_Error.*/DB_E_BADSTARTPOSITION = unchecked((int)0x80040E1E);
                //if (DB_E_BADSTARTPOSITION != hr)
                ProcessResults(hr);
            }
            _isRead = ((OleDbHResult.DB_S_ENDOFROWSET != hr) || (0 < (int)_rowFetchedCount));
            _rowFetchedCount = (IntPtr)Math.Max((int)_rowFetchedCount, 0);
        }

        private void GetRowDataFromHandle()
        {
            Debug.Assert(null != _bindings, "GetRowDataFromHandle: null dbBindings");
            Debug.Assert(null != _rowHandleNativeBuffer, "GetRowDataFromHandle: null dbBindings");

            OleDbHResult hr = 0;
            UnsafeNativeMethods.IRowset irowset = IRowset();

            IntPtr rowHandle = _rowHandleNativeBuffer.GetRowHandle(_currentRow);

            RowBinding rowBinding = _bindings[_nextAccessorForRetrieval].RowBinding();
            IntPtr accessorHandle = rowBinding.DangerousGetAccessorHandle();

            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                rowBinding.DangerousAddRef(ref mustRelease);
                rowBinding.StartDataBlock();

                IntPtr dataPtr = rowBinding.DangerousGetDataPtr();
                hr = irowset.GetData(rowHandle, accessorHandle, dataPtr);
            }
            finally
            {
                if (mustRelease)
                {
                    rowBinding.DangerousRelease();
                }
            }

            _nextAccessorForRetrieval++;
            if (hr < 0)
            {
                ProcessResults(hr);
            }
        }

        private void ReleaseRowHandles()
        {
            Debug.Assert(0 < (int)_rowFetchedCount, "invalid _rowFetchedCount");

            OleDbHResult hr;
            UnsafeNativeMethods.IRowset irowset = IRowset();
            hr = irowset.ReleaseRows(_rowFetchedCount, _rowHandleNativeBuffer, ADP.PtrZero, ADP.PtrZero, ADP.PtrZero);

            if (hr < 0)
            {
                //ProcessFailure(hr);
                //ProcessFailure(hr);
                SafeNativeMethods.Wrapper.ClearErrorInfo();
            }
            _rowFetchedCount = IntPtr.Zero;
            _currentRow = 0;
            _isRead = false;
        }

        private object GetPropertyValue(int propertyId)
        {
            if (null != _irowset)
            {
                return GetPropertyOnRowset(OleDbPropertySetGuid.Rowset, propertyId);
            }
            else if (null != _command)
            {
                return _command.GetPropertyValue(OleDbPropertySetGuid.Rowset, propertyId);
            }
            return OleDbPropertyStatus.NotSupported;
        }

        private object GetPropertyOnRowset(Guid propertySet, int propertyID)
        {
            OleDbHResult hr;
            tagDBPROP[] dbprops;
            UnsafeNativeMethods.IRowsetInfo irowsetinfo = IRowsetInfo();

            using (PropertyIDSet propidset = new PropertyIDSet(propertySet, propertyID))
            {
                using (DBPropSet propset = new DBPropSet(irowsetinfo, propidset, out hr))
                {
                    if (hr < 0)
                    {
                        // OLEDB Data Reader masks provider specific errors by raising "Internal Data Provider error 30."
                        // DBPropSet c-tor will register the exception and it will be raised at GetPropertySet call in case of failure
                        SafeNativeMethods.Wrapper.ClearErrorInfo();
                    }
                    dbprops = propset.GetPropertySet(0, out propertySet);
                }
            }
            if (OleDbPropertyStatus.Ok == dbprops[0].dwStatus)
            {
                return dbprops[0].vValue;
            }
            return dbprops[0].dwStatus;
        }

        private void GetRowValue()
        {
            Debug.Assert(null != _irow, "GetRowValue: null IRow");
            Debug.Assert(null != _metadata, "GetRowValue: null MetaData");

            Bindings bindings = _bindings[_nextAccessorForRetrieval];
            ColumnBinding[] columnBindings = bindings.ColumnBindings();
            RowBinding rowBinding = bindings.RowBinding();
            Debug.Assert(_nextValueForRetrieval <= columnBindings[0].Index, "backwards retrieval");

            bool mustReleaseBinding = false;
            bool[] mustRelease = new bool[columnBindings.Length];
            StringMemHandle[] sptr = new StringMemHandle[columnBindings.Length];

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                for (int i = 0; i < columnBindings.Length; ++i)
                {
                    bindings.CurrentIndex = i;

                    sptr[i] = null;
                    MetaData info = _metadata[columnBindings[i].Index];
                    if ((ODB.DBKIND_GUID_NAME == info.kind) || (ODB.DBKIND_NAME == info.kind))
                    {
                        sptr[i] = new StringMemHandle(info.idname);
                        columnBindings[i]._sptr = sptr[i];
                    }

                    sptr[i].DangerousAddRef(ref mustRelease[i]);

                    IntPtr ulPropid = ((null != sptr[i]) ? sptr[i].DangerousGetHandle() : info.propid);
                    bindings.GuidKindName(info.guid, info.kind, ulPropid);
                }

                OleDbHResult hr;
                tagDBCOLUMNACCESS[] access = bindings.DBColumnAccess;

                rowBinding.DangerousAddRef(ref mustReleaseBinding);
                rowBinding.StartDataBlock();

                UnsafeNativeMethods.IRow irow = IRow();
                hr = irow.GetColumns((IntPtr)access.Length, access);
            }
            finally
            {
                if (mustReleaseBinding)
                {
                    rowBinding.DangerousRelease();
                }
                for (int i = 0; i < mustRelease.Length; i++)
                {
                    if (mustRelease[i])
                    {
                        sptr[i].DangerousRelease();
                    }
                }
            }
            _nextAccessorForRetrieval++;
        }

        private Int32 IndexOf(Hashtable hash, string name)
        {
            // via case sensitive search, first match with lowest ordinal matches
            object index = hash[name];
            if (null != index)
            {
                return (int)index; // match via case-insensitive or by chance lowercase
            }

            // via case insensitive search, first match with lowest ordinal matches
            string tmp = name.ToLower(CultureInfo.InvariantCulture);
            index = hash[tmp]; // match via lowercase
            return ((null != index) ? (int)index : -1);
        }

        private void AppendSchemaInfo()
        {
            Debug.Assert(null != _connection, "null connection");
            Debug.Assert(null != _metadata, "no _metadata");

            if (_metadata.Length <= 0)
            {
                return;
            }

            int keyCount = 0;
            for (int i = 0; i < _metadata.Length; ++i)
            {
                if (_metadata[i].isKeyColumn && !_metadata[i].isHidden)
                {
                    keyCount++;
                }
            }
            if (0 != keyCount) /*|| _connection.IsServer_msdaora || _connection.IsServer_Microsoft_SQL)*/
            {
                return;
            }

            string schemaName, catalogName; // enforce single table
            string baseSchemaName = null, baseCatalogName = null, baseTableName = null;
            for (int i = 0; i < _metadata.Length; ++i)
            {
                MetaData info = _metadata[i];
                if ((null != info.baseTableName) && (0 < info.baseTableName.Length))
                {
                    catalogName = ((null != info.baseCatalogName) ? info.baseCatalogName : "");
                    schemaName = ((null != info.baseSchemaName) ? info.baseSchemaName : "");
                    if (null == baseTableName)
                    {
                        baseSchemaName = schemaName;
                        baseCatalogName = catalogName;
                        baseTableName = info.baseTableName;
                    }
                    else if ((0 != ADP.SrcCompare(baseTableName, info.baseTableName))
                            || (0 != ADP.SrcCompare(baseCatalogName, catalogName))
                            || (0 != ADP.SrcCompare(baseSchemaName, schemaName)))
                    {
#if DEBUG
                        if (AdapterSwitches.DataSchema.TraceVerbose)
                        {
                            Debug.WriteLine("Multiple BaseTableName detected:"
                                + " <" + baseCatalogName + "." + baseCatalogName + "." + baseTableName + ">"
                                + " <" + info.baseCatalogName + "." + info.baseCatalogName + "." + info.baseTableName + ">");
                        }
#endif
                        baseTableName = null;
                        break;
                    }
                }
            }
            if (null == baseTableName)
            {
                return;
            }
            baseCatalogName = ADP.IsEmpty(baseCatalogName) ? null : baseCatalogName;
            baseSchemaName = ADP.IsEmpty(baseSchemaName) ? null : baseSchemaName;

            if (null != _connection)
            {
                if (ODB.DBPROPVAL_IC_SENSITIVE == _connection.QuotedIdentifierCase())
                {
                    string p = null, s = null;
                    _connection.GetLiteralQuotes(ADP.GetSchemaTable, out s, out p);
                    if (null == s)
                    {
                        s = "";
                    }
                    if (null == p)
                    {
                        p = "";
                    }
                    baseTableName = s + baseTableName + p;
                }
            }

            Hashtable baseColumnNames = new Hashtable(_metadata.Length * 2);

            for (int i = _metadata.Length - 1; 0 <= i; --i)
            {
                string basecolumname = _metadata[i].baseColumnName;
                if (!ADP.IsEmpty(basecolumname))
                {
                    baseColumnNames[basecolumname] = i;
                }
            }
            for (int i = 0; i < _metadata.Length; ++i)
            {
                string basecolumname = _metadata[i].baseColumnName;
                if (!ADP.IsEmpty(basecolumname))
                {
                    basecolumname = basecolumname.ToLower(CultureInfo.InvariantCulture);
                    if (!baseColumnNames.Contains(basecolumname))
                    {
                        baseColumnNames[basecolumname] = i;
                    }
                }
            }

            // look for primary keys in the table
            if (_connection.SupportSchemaRowset(OleDbSchemaGuid.Primary_Keys))
            {
                Object[] restrictions = new Object[] { baseCatalogName, baseSchemaName, baseTableName };
                keyCount = AppendSchemaPrimaryKey(baseColumnNames, restrictions);
            }
            if (0 != keyCount)
            {
                return;
            }

            // look for a single unique contraint that can be upgraded
            if (_connection.SupportSchemaRowset(OleDbSchemaGuid.Indexes))
            {
                Object[] restrictions = new Object[] { baseCatalogName, baseSchemaName, null, null, baseTableName };
                AppendSchemaUniqueIndexAsKey(baseColumnNames, restrictions);
            }
        }

        private int AppendSchemaPrimaryKey(Hashtable baseColumnNames, object[] restrictions)
        {
            int keyCount = 0;
            bool partialPrimaryKey = false;
            DataTable table = null;
            try
            {
                table = _connection.GetSchemaRowset(OleDbSchemaGuid.Primary_Keys, restrictions);
            }
            catch (Exception e)
            {
                // UNDONE - should not be catching all exceptions!!!
                if (!ADP.IsCatchableExceptionType(e))
                {
                    throw;
                }

                ADP.TraceExceptionWithoutRethrow(e);
            }
            if (null != table)
            {
                DataColumnCollection dataColumns = table.Columns;
                int nameColumnIndex = dataColumns.IndexOf(ODB.COLUMN_NAME);

                if (-1 != nameColumnIndex)
                {
                    DataColumn nameColumn = dataColumns[nameColumnIndex];
                    foreach (DataRow dataRow in table.Rows)
                    {
                        string name = (string)dataRow[nameColumn, DataRowVersion.Default];

                        int metaindex = IndexOf(baseColumnNames, name);
                        if (0 <= metaindex)
                        {
                            MetaData info = _metadata[metaindex];
                            info.isKeyColumn = true;
                            info.flags &= ~ODB.DBCOLUMNFLAGS_ISNULLABLE;
                            keyCount++;
                        }
                        else
                        {
#if DEBUG
                            if (AdapterSwitches.DataSchema.TraceVerbose)
                            {
                                Debug.WriteLine("PartialKeyColumn detected: <" + name + "> metaindex=" + metaindex);
                            }
#endif
                            partialPrimaryKey = true;
                            break;
                        }
                    }
                }
            }
            if (partialPrimaryKey)
            { // partial primary key detected
                for (int i = 0; i < _metadata.Length; ++i)
                {
                    _metadata[i].isKeyColumn = false;
                }
                return -1;
            }
            return keyCount;
        }

        private void AppendSchemaUniqueIndexAsKey(Hashtable baseColumnNames, object[] restrictions)
        {
            bool partialPrimaryKey = false;
            DataTable table = null;
            try
            {
                table = _connection.GetSchemaRowset(OleDbSchemaGuid.Indexes, restrictions);
            }
            catch (Exception e)
            {
                // UNDONE - should not be catching all exceptions!!!
                if (!ADP.IsCatchableExceptionType(e))
                {
                    throw;
                }

                ADP.TraceExceptionWithoutRethrow(e);
            }
            if (null != table)
            {
                DataColumnCollection dataColumns = table.Columns;

                int indxIndex = dataColumns.IndexOf(ODB.INDEX_NAME);
                int pkeyIndex = dataColumns.IndexOf(ODB.PRIMARY_KEY);
                int uniqIndex = dataColumns.IndexOf(ODB.UNIQUE);
                int nameIndex = dataColumns.IndexOf(ODB.COLUMN_NAME);
                int nullIndex = dataColumns.IndexOf(ODB.NULLS);

                if ((-1 != indxIndex) && (-1 != pkeyIndex) && (-1 != uniqIndex) && (-1 != nameIndex))
                {
                    DataColumn indxColumn = dataColumns[indxIndex];
                    DataColumn pkeyColumn = dataColumns[pkeyIndex];
                    DataColumn uniqCOlumn = dataColumns[uniqIndex];
                    DataColumn nameColumn = dataColumns[nameIndex];
                    DataColumn nulls = ((-1 != nullIndex) ? dataColumns[nullIndex] : null);

                    bool[] keys = new bool[_metadata.Length];
                    bool[] uniq = new bool[_metadata.Length];
                    string uniqueIndexName = null;

                    // match pkey name BaseColumnName
                    foreach (DataRow dataRow in table.Rows)
                    {
                        bool isPKey = (!dataRow.IsNull(pkeyColumn, DataRowVersion.Default) && (bool)dataRow[pkeyColumn, DataRowVersion.Default]);
                        bool isUniq = (!dataRow.IsNull(uniqCOlumn, DataRowVersion.Default) && (bool)dataRow[uniqCOlumn, DataRowVersion.Default]);
                        bool nullsVal = (null != nulls) && (dataRow.IsNull(nulls, DataRowVersion.Default) || (ODB.DBPROPVAL_IN_ALLOWNULL == Convert.ToInt32(dataRow[nulls, DataRowVersion.Default], CultureInfo.InvariantCulture)));

                        if (isPKey || isUniq)
                        {
                            string name = (string)dataRow[nameColumn, DataRowVersion.Default];

                            int metaindex = IndexOf(baseColumnNames, name);
                            if (0 <= metaindex)
                            {
                                if (isPKey)
                                {
                                    keys[metaindex] = true;
                                }
                                if (isUniq && (null != uniq))
                                {
                                    uniq[metaindex] = true;

                                    string indexname = (string)dataRow[indxColumn, DataRowVersion.Default];
                                    if (null == uniqueIndexName)
                                    {
                                        uniqueIndexName = indexname;
                                    }
                                    else if (indexname != uniqueIndexName)
                                    {
#if DEBUG
                                        if (AdapterSwitches.DataSchema.TraceVerbose)
                                        {
                                            Debug.WriteLine("MultipleUniqueIndexes detected: <" + uniqueIndexName + "> <" + indexname + ">");
                                        }
#endif
                                        uniq = null;
                                    }
                                }
                            }
                            else if (isPKey)
                            {
#if DEBUG
                                if (AdapterSwitches.DataSchema.TraceVerbose)
                                {
                                    Debug.WriteLine("PartialKeyColumn detected: " + name);
                                }
#endif
                                partialPrimaryKey = true;
                                break;
                            }
                            else if (null != uniqueIndexName)
                            {
                                string indexname = (string)dataRow[indxColumn, DataRowVersion.Default];

                                if (indexname != uniqueIndexName)
                                {
#if DEBUG
                                    if (AdapterSwitches.DataSchema.TraceVerbose)
                                    {
                                        Debug.WriteLine("PartialUniqueIndexes detected: <" + uniqueIndexName + "> <" + indexname + ">");
                                    }
#endif
                                    uniq = null;
                                }
                            }
                        }
                    }
                    if (partialPrimaryKey)
                    {
                        for (int i = 0; i < _metadata.Length; ++i)
                        {
                            _metadata[i].isKeyColumn = false;
                        }
                        return;
                    }
                    else if (null != uniq)
                    {
#if DEBUG
                        if (AdapterSwitches.DataSchema.TraceVerbose)
                        {
                            Debug.WriteLine("upgrade single unique index to be a key: <" + uniqueIndexName + ">");
                        }
#endif
                        // upgrade single unique index to be a key
                        for (int i = 0; i < _metadata.Length; ++i)
                        {
                            _metadata[i].isKeyColumn = uniq[i];
                        }
                    }
                }
            }
        }

        private MetaData FindMetaData(string name)
        {
            int index = _fieldNameLookup.IndexOfName(name);
            return ((-1 != index) ? _metadata[index] : null);
        }

        internal void DumpToSchemaTable(UnsafeNativeMethods.IRowset rowset)
        {
            List<MetaData> metainfo = new List<MetaData>();

            object hiddenColumns = null;
            using (OleDbDataReader dataReader = new OleDbDataReader(_connection, _command, Int32.MinValue, 0))
            {
                dataReader.InitializeIRowset(rowset, ChapterHandle.DB_NULL_HCHAPTER, IntPtr.Zero);
                dataReader.BuildSchemaTableInfo(rowset, true, false);

                hiddenColumns = GetPropertyValue(ODB.DBPROP_HIDDENCOLUMNS);
                if (0 == dataReader.FieldCount)
                {
                    return;
                }

                Debug.Assert(null == dataReader._fieldNameLookup, "lookup already exists");
                FieldNameLookup lookup = new FieldNameLookup(dataReader, -1);
                dataReader._fieldNameLookup = lookup;

                // This column, together with the DBCOLUMN_GUID and DBCOLUMN_PROPID
                // columns, forms the ID of the column. One or more (but not all) of these columns
                // will be NULL, depending on which elements of the DBID structure the provider uses.
                MetaData columnidname = dataReader.FindMetaData(ODB.DBCOLUMN_IDNAME);
                MetaData columnguid = dataReader.FindMetaData(ODB.DBCOLUMN_GUID);
                MetaData columnpropid = dataReader.FindMetaData(ODB.DBCOLUMN_PROPID);

                MetaData columnname = dataReader.FindMetaData(ODB.DBCOLUMN_NAME);
                MetaData columnordinal = dataReader.FindMetaData(ODB.DBCOLUMN_NUMBER);
                MetaData dbtype = dataReader.FindMetaData(ODB.DBCOLUMN_TYPE);
                MetaData columnsize = dataReader.FindMetaData(ODB.DBCOLUMN_COLUMNSIZE);
                MetaData numericprecision = dataReader.FindMetaData(ODB.DBCOLUMN_PRECISION);
                MetaData numericscale = dataReader.FindMetaData(ODB.DBCOLUMN_SCALE);
                MetaData columnflags = dataReader.FindMetaData(ODB.DBCOLUMN_FLAGS);
                MetaData baseschemaname = dataReader.FindMetaData(ODB.DBCOLUMN_BASESCHEMANAME);
                MetaData basecatalogname = dataReader.FindMetaData(ODB.DBCOLUMN_BASECATALOGNAME);
                MetaData basetablename = dataReader.FindMetaData(ODB.DBCOLUMN_BASETABLENAME);
                MetaData basecolumnname = dataReader.FindMetaData(ODB.DBCOLUMN_BASECOLUMNNAME);
                MetaData isautoincrement = dataReader.FindMetaData(ODB.DBCOLUMN_ISAUTOINCREMENT);
                MetaData isunique = dataReader.FindMetaData(ODB.DBCOLUMN_ISUNIQUE);
                MetaData iskeycolumn = dataReader.FindMetaData(ODB.DBCOLUMN_KEYCOLUMN);

                // @devnote: because we want to use the DBACCESSOR_OPTIMIZED bit,
                // we are required to create the accessor before fetching any rows
                dataReader.CreateAccessors(false);

                ColumnBinding binding;
                while (dataReader.ReadRowset())
                {
                    dataReader.GetRowDataFromHandle();

                    MetaData info = new MetaData();

                    binding = columnidname.columnBinding;
                    if (!binding.IsValueNull())
                    {
                        info.idname = (string)binding.Value();
                        info.kind = ODB.DBKIND_NAME;
                    }

                    binding = columnguid.columnBinding;
                    if (!binding.IsValueNull())
                    {
                        info.guid = binding.Value_GUID();
                        info.kind = ((ODB.DBKIND_NAME == info.kind) ? ODB.DBKIND_GUID_NAME : ODB.DBKIND_GUID);
                    }

                    binding = columnpropid.columnBinding;
                    if (!binding.IsValueNull())
                    {
                        info.propid = new IntPtr(binding.Value_UI4());
                        info.kind = ((ODB.DBKIND_GUID == info.kind) ? ODB.DBKIND_GUID_PROPID : ODB.DBKIND_PROPID);
                    }

                    binding = columnname.columnBinding;
                    if (!binding.IsValueNull())
                    {
                        info.columnName = (string)binding.Value();
                    }
                    else
                    {
                        info.columnName = "";
                    }

                    if (4 == ADP.PtrSize)
                    {
                        info.ordinal = (IntPtr)columnordinal.columnBinding.Value_UI4();
                    }
                    else
                    {
                        info.ordinal = (IntPtr)columnordinal.columnBinding.Value_UI8();
                    }
                    short wType = unchecked((short)dbtype.columnBinding.Value_UI2());

                    if (4 == ADP.PtrSize)
                    {
                        info.size = unchecked((int)columnsize.columnBinding.Value_UI4());
                    }
                    else
                    {
                        info.size = ADP.IntPtrToInt32((IntPtr)unchecked((long)columnsize.columnBinding.Value_UI8()));
                    }

                    binding = numericprecision.columnBinding;
                    if (!binding.IsValueNull())
                    {
                        info.precision = (byte)binding.Value_UI2();
                    }

                    binding = numericscale.columnBinding;
                    if (!binding.IsValueNull())
                    {
                        info.scale = (byte)binding.Value_I2();
                    }

                    info.flags = unchecked((int)columnflags.columnBinding.Value_UI4());

                    bool islong = OleDbDataReader.IsLong(info.flags);
                    bool isfixed = OleDbDataReader.IsFixed(info.flags);
                    NativeDBType dbType = NativeDBType.FromDBType(wType, islong, isfixed);

                    info.type = dbType;

                    if (null != isautoincrement)
                    {
                        binding = isautoincrement.columnBinding;
                        if (!binding.IsValueNull())
                        {
                            info.isAutoIncrement = binding.Value_BOOL();
                        }
                    }
                    if (null != isunique)
                    {
                        binding = isunique.columnBinding;
                        if (!binding.IsValueNull())
                        {
                            info.isUnique = binding.Value_BOOL();
                        }
                    }
                    if (null != iskeycolumn)
                    {
                        binding = iskeycolumn.columnBinding;
                        if (!binding.IsValueNull())
                        {
                            info.isKeyColumn = binding.Value_BOOL();
                        }
                    }
                    if (null != baseschemaname)
                    {
                        binding = baseschemaname.columnBinding;
                        if (!binding.IsValueNull())
                        {
                            info.baseSchemaName = binding.ValueString();
                        }
                    }
                    if (null != basecatalogname)
                    {
                        binding = basecatalogname.columnBinding;
                        if (!binding.IsValueNull())
                        {
                            info.baseCatalogName = binding.ValueString();
                        }
                    }
                    if (null != basetablename)
                    {
                        binding = basetablename.columnBinding;
                        if (!binding.IsValueNull())
                        {
                            info.baseTableName = binding.ValueString();
                        }
                    }
                    if (null != basecolumnname)
                    {
                        binding = basecolumnname.columnBinding;
                        if (!binding.IsValueNull())
                        {
                            info.baseColumnName = binding.ValueString();
                        }
                    }
                    metainfo.Add(info);
                }
            }

            int visibleCount = metainfo.Count;
            if (hiddenColumns is Int32)
            {
                visibleCount -= (int)hiddenColumns;
            }

            //  if one key column is invalidated, they all need to be invalidated. The SET is the key,
            //  and subsets likely are not accurate keys. Note the assumption that the two loops below
            //  will traverse the entire set of columns.
            bool disallowKeyColumns = false;

            for (int index = metainfo.Count - 1; visibleCount <= index; --index)
            {
                MetaData info = metainfo[index];

                info.isHidden = true;

                if (disallowKeyColumns)
                {
                    info.isKeyColumn = false;
                }
                else if (info.guid.Equals(ODB.DBCOL_SPECIALCOL))
                {
                    info.isKeyColumn = false;

                    // This is the first key column to be invalidated, scan back through the 
                    //  columns we already processed to make sure none of those are marked as keys.
                    disallowKeyColumns = true;
                    for (int index2 = metainfo.Count - 1; index < index2; --index2)
                    {
                        metainfo[index2].isKeyColumn = false;
                    }
                }
            }

            for (int index = visibleCount - 1; 0 <= index; --index)
            {
                MetaData info = metainfo[index];

                if (disallowKeyColumns)
                {
                    info.isKeyColumn = false;
                }

                if (info.guid.Equals(ODB.DBCOL_SPECIALCOL))
                {
#if DEBUG
                    if (AdapterSwitches.DataSchema.TraceVerbose)
                    {
                        Debug.WriteLine("Filtered Column: DBCOLUMN_GUID=DBCOL_SPECIALCOL DBCOLUMN_NAME=" + info.columnName + " DBCOLUMN_KEYCOLUMN=" + info.isKeyColumn);
                    }
#endif
                    info.isHidden = true;
                    visibleCount--;
                }
#if WIN32
                else if (0 >= (int)info.ordinal) {
#else
                else if (0 >= (long)info.ordinal)
                {
#endif
#if DEBUG
                    if (AdapterSwitches.DataSchema.TraceVerbose)
                    {
                        Debug.WriteLine("Filtered Column: DBCOLUMN_NUMBER=" + info.ordinal.ToInt64().ToString(CultureInfo.InvariantCulture) + " DBCOLUMN_NAME=" + info.columnName);
                    }
#endif
                    info.isHidden = true;
                    visibleCount--;
                }
                else if (OleDbDataReader.DoColumnDropFilter(info.flags))
                {
#if DEBUG
                    if (AdapterSwitches.DataSchema.TraceVerbose)
                    {
                        Debug.WriteLine("Filtered Column: DBCOLUMN_FLAGS=" + info.flags.ToString("X8", (System.IFormatProvider)null) + " DBCOLUMN_NAME=" + info.columnName);
                    }
#endif
                    info.isHidden = true;
                    visibleCount--;
                }
            }

            // CONSIDER: perf tracking to see if we need to sort or not
            metainfo.Sort();
            _visibleFieldCount = visibleCount;
            _metadata = metainfo.ToArray();
        }

        static internal void GenerateSchemaTable(OleDbDataReader dataReader, object handle, CommandBehavior behavior)
        {
            if (0 != (CommandBehavior.KeyInfo & behavior))
            {
                dataReader.BuildSchemaTableRowset(handle); // tries IColumnsRowset first then IColumnsInfo
                dataReader.AppendSchemaInfo();
            }
            else
            {
                dataReader.BuildSchemaTableInfo(handle, false, false); // only tries IColumnsInfo
            }
            MetaData[] metadata = dataReader.MetaData;
            if ((null != metadata) && (0 < metadata.Length))
            {
                dataReader.BuildSchemaTable(metadata);
            }
        }

        static private bool DoColumnDropFilter(int flags)
        {
            return (0 != (ODB.DBCOLUMNFLAGS_ISBOOKMARK & flags));
        }
        static private bool IsLong(int flags)
        {
            return (0 != (ODB.DBCOLUMNFLAGS_ISLONG & flags));
        }
        static private bool IsFixed(int flags)
        {
            return (0 != (ODB.DBCOLUMNFLAGS_ISFIXEDLENGTH & flags));
        }
        static private bool IsRowVersion(int flags)
        {
            return (0 != (ODB.DBCOLUMNFLAGS_ISROWID_DBCOLUMNFLAGS_ISROWVER & flags));
        }
        static private bool AllowDBNull(int flags)
        {
            return (0 != (ODB.DBCOLUMNFLAGS_ISNULLABLE & flags));
        }
        static private bool AllowDBNullMaybeNull(int flags)
        {
            return (0 != (ODB.DBCOLUMNFLAGS_ISNULLABLE_DBCOLUMNFLAGS_MAYBENULL & flags));
        }
        static private bool IsReadOnly(int flags)
        {
            return (0 == (ODB.DBCOLUMNFLAGS_WRITE_DBCOLUMNFLAGS_WRITEUNKNOWN & flags));
        }
    }

    sealed internal class MetaData : IComparable
    {
        internal Bindings bindings;
        internal ColumnBinding columnBinding;

        internal string columnName;

        internal Guid guid;
        internal int kind;
        internal IntPtr propid;
        internal string idname;

        internal NativeDBType type;

        internal IntPtr ordinal;
        internal int size;

        internal int flags;

        internal byte precision;
        internal byte scale;

        internal bool isAutoIncrement;
        internal bool isUnique;
        internal bool isKeyColumn;
        internal bool isHidden;

        internal string baseSchemaName;
        internal string baseCatalogName;
        internal string baseTableName;
        internal string baseColumnName;

        int IComparable.CompareTo(object obj)
        {
            if (isHidden == (obj as MetaData).isHidden)
            {
#if WIN32
                return ((int)ordinal - (int)(obj as MetaData).ordinal);
#else
                long v = ((long)ordinal - (long)(obj as MetaData).ordinal);
                return ((0 < v) ? 1 : ((v < 0) ? -1 : 0));
#endif

            }
            return (isHidden) ? 1 : -1; // ensure that all hidden columns come after non-hidden columns
        }

        internal MetaData()
        {
        }
    }
}
