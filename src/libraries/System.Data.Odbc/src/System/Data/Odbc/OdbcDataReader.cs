// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.ProviderBase;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;              // StringBuilder

namespace System.Data.Odbc
{
    public sealed class OdbcDataReader : DbDataReader
    {
        private OdbcCommand _command;

        private int _recordAffected = -1;
        private FieldNameLookup _fieldNameLookup;

        private DbCache _dataCache;
        private enum HasRowsStatus
        {
            DontKnow = 0,
            HasRows = 1,
            HasNoRows = 2,
        }
        private HasRowsStatus _hasRows = HasRowsStatus.DontKnow;
        private bool _isClosed;
        private bool _isRead;
        private bool _isValidResult;
        private bool _noMoreResults;
        private bool _noMoreRows;
        private bool _skipReadOnce;
        private int _hiddenColumns;                 // number of hidden columns
        private CommandBehavior _commandBehavior;

        // track current row and column, will be set on the first Fetch call
        private int _row = -1;
        private int _column = -1;

        // used to track position in field for sucessive reads in case of Sequential Access
        private long _sequentialBytesRead;

        private static int s_objectTypeCount; // Bid counter
        internal readonly int ObjectID = System.Threading.Interlocked.Increment(ref s_objectTypeCount);

        // the statement handle here is just a copy of the statement handle owned by the command
        // the DataReader must not free the statement handle. this is done by the command
        //

        private MetaData[] _metadata;
        private DataTable _schemaTable; // MDAC 68336
        private string _cmdText;    // get a copy in case the command text on the command is changed ...
        private CMDWrapper _cmdWrapper;

        internal OdbcDataReader(OdbcCommand command, CMDWrapper cmdWrapper, CommandBehavior commandbehavior)
        {
            Debug.Assert(command != null, "Command null on OdbcDataReader ctor");
            _command = command;
            _commandBehavior = commandbehavior;
            _cmdText = command.CommandText;    // get a copy in case the command text on the command is changed ...
            _cmdWrapper = cmdWrapper;
        }

        private CNativeBuffer Buffer
        {
            get
            {
                CNativeBuffer value = _cmdWrapper._dataReaderBuf;
                if (null == value)
                {
                    Debug.Fail("object is disposed");
                    throw new ObjectDisposedException(GetType().Name);
                }
                return value;
            }
        }

        private OdbcConnection Connection
        {
            get
            {
                if (null != _cmdWrapper)
                {
                    return _cmdWrapper.Connection;
                }
                else
                {
                    return null;
                }
            }
        }

        internal OdbcCommand Command
        {
            get
            {
                return _command;
            }
            set
            {
                _command = value;
            }
        }

        private OdbcStatementHandle StatementHandle
        {
            get
            {
                return _cmdWrapper.StatementHandle;
            }
        }

        private OdbcStatementHandle KeyInfoStatementHandle
        {
            get { return _cmdWrapper.KeyInfoStatement; }
        }

        internal bool IsBehavior(CommandBehavior behavior)
        {
            return IsCommandBehavior(behavior);
        }

        internal bool IsCancelingCommand
        {
            get
            {
                if (_command != null)
                {
                    return _command.Canceling;
                }
                return false;
            }
        }

        internal bool IsNonCancelingCommand
        {
            get
            {
                if (_command != null)
                {
                    return !_command.Canceling;
                }
                return false;
            }
        }

        public override int Depth
        {
            get
            {
                if (IsClosed)
                { // MDAC 63669
                    throw ADP.DataReaderClosed("Depth");
                }
                return 0;
            }
        }

        public override int FieldCount
        {
            get
            {
                if (IsClosed)
                { // MDAC 63669
                    throw ADP.DataReaderClosed("FieldCount");
                }
                if (_noMoreResults)
                {   // MDAC 93325
                    return 0;
                }
                if (null == _dataCache)
                {
                    short cColsAffected;
                    ODBC32.RetCode retcode = this.FieldCountNoThrow(out cColsAffected);
                    if (retcode != ODBC32.RetCode.SUCCESS)
                    {
                        Connection.HandleError(StatementHandle, retcode);
                    }
                }
                return ((null != _dataCache) ? _dataCache._count : 0);
            }
        }

        // HasRows
        //
        // Use to detect wheter there are one ore more rows in the result without going through Read
        // May be called at any time
        // Basically it calls Read and sets a flag so that the actual Read call will be skipped once
        //
        public override bool HasRows
        {
            get
            {
                if (IsClosed)
                {
                    throw ADP.DataReaderClosed("HasRows");
                }
                if (_hasRows == HasRowsStatus.DontKnow)
                {
                    Read();                     //
                    _skipReadOnce = true;       // need to skip Read once because we just did it
                }
                return (_hasRows == HasRowsStatus.HasRows);
            }
        }

        internal ODBC32.RetCode FieldCountNoThrow(out short cColsAffected)
        {
            if (IsCancelingCommand)
            {
                cColsAffected = 0;
                return ODBC32.RetCode.ERROR;
            }

            ODBC32.RetCode retcode = StatementHandle.NumberOfResultColumns(out cColsAffected);
            if (retcode == ODBC32.RetCode.SUCCESS)
            {
                _hiddenColumns = 0;
                if (IsCommandBehavior(CommandBehavior.KeyInfo))
                {
                    // we need to search for the first hidden column
                    //
                    if (!Connection.ProviderInfo.NoSqlSoptSSNoBrowseTable && !Connection.ProviderInfo.NoSqlSoptSSHiddenColumns)
                    {
                        for (int i = 0; i < cColsAffected; i++)
                        {
                            SQLLEN isHidden = GetColAttribute(i, (ODBC32.SQL_DESC)ODBC32.SQL_CA_SS.COLUMN_HIDDEN, (ODBC32.SQL_COLUMN)(-1), ODBC32.HANDLER.IGNORE);
                            if (isHidden.ToInt64() == 1)
                            {
                                _hiddenColumns = (int)cColsAffected - i;
                                cColsAffected = (short)i;
                                break;
                            }
                        }
                    }
                }
                _dataCache = new DbCache(this, cColsAffected);
            }
            else
            {
                cColsAffected = 0;
            }
            return retcode;
        }

        public override bool IsClosed
        {
            get
            {
                return _isClosed;
            }
        }

        private SQLLEN GetRowCount()
        {
            if (!IsClosed)
            {
                SQLLEN cRowsAffected;
                ODBC32.RetCode retcode = StatementHandle.RowCount(out cRowsAffected);
                if (ODBC32.RetCode.SUCCESS == retcode || ODBC32.RetCode.SUCCESS_WITH_INFO == retcode)
                {
                    return cRowsAffected;
                }
            }
            return -1;
        }

        internal int CalculateRecordsAffected(int cRowsAffected)
        {
            if (0 <= cRowsAffected)
            {
                if (-1 == _recordAffected)
                {
                    _recordAffected = cRowsAffected;
                }
                else
                {
                    _recordAffected += cRowsAffected;
                }
            }
            return _recordAffected;
        }


        public override int RecordsAffected
        {
            get
            {
                return _recordAffected;
            }
        }

        public override object this[int i]
        {
            get
            {
                return GetValue(i);
            }
        }

        public override object this[string value]
        {
            get
            {
                return GetValue(GetOrdinal(value));
            }
        }

        public override void Close()
        {
            Close(false);
        }

        private void Close(bool disposing)
        {
            Exception error = null;

            CMDWrapper wrapper = _cmdWrapper;
            if (null != wrapper && wrapper.StatementHandle != null)
            {
                // disposing
                // true to release both managed and unmanaged resources; false to release only unmanaged resources.
                //
                if (IsNonCancelingCommand)
                {
                    //Read any remaining results off the wire
                    // some batch statements may not be executed until SQLMoreResults is called.
                    // We want the user to be able to do ExecuteNonQuery or ExecuteReader
                    // and close without having iterate to get params or batch.
                    //
                    NextResult(disposing, !disposing); // false,true or true,false
                    if (null != _command)
                    {
                        if (_command.HasParameters)
                        {
                            // Output Parameters are not guareenteed to be returned until all the data
                            // from any restssets are read, so we do this after the above NextResult call(s)
                            _command.Parameters.GetOutputValues(_cmdWrapper);
                        }
                        wrapper.FreeStatementHandle(ODBC32.STMT.CLOSE);
                        _command.CloseFromDataReader();
                    }
                }
                wrapper.FreeKeyInfoStatementHandle(ODBC32.STMT.CLOSE);
            }

            // if the command is still around we call CloseFromDataReader,
            // otherwise we need to dismiss the statement handle ourselves
            //
            if (null != _command)
            {
                _command.CloseFromDataReader();

                if (IsCommandBehavior(CommandBehavior.CloseConnection))
                {
                    Debug.Assert(null != Connection, "null cmd connection");
                    _command.Parameters.RebindCollection = true;
                    Connection.Close();
                }
            }
            else if (null != wrapper)
            {
                wrapper.Dispose();
            }

            _command = null;
            _isClosed = true;
            _dataCache = null;
            _metadata = null;
            _schemaTable = null;
            _isRead = false;
            _hasRows = HasRowsStatus.DontKnow;
            _isValidResult = false;
            _noMoreResults = true;
            _noMoreRows = true;
            _fieldNameLookup = null;

            SetCurrentRowColumnInfo(-1, 0);

            if ((null != error) && !disposing)
            {
                throw error;
            }
            _cmdWrapper = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Close(true);
            }
            // not delegating to base class because we know it only calls Close
            //base.Dispose(disposing)
        }

        public override string GetDataTypeName(int i)
        {
            if (null != _dataCache)
            {
                DbSchemaInfo info = _dataCache.GetSchema(i);
                if (info._typename == null)
                {
                    info._typename = GetColAttributeStr(i, ODBC32.SQL_DESC.TYPE_NAME, ODBC32.SQL_COLUMN.TYPE_NAME, ODBC32.HANDLER.THROW);
                }
                return info._typename;
            }
            throw ADP.DataReaderNoData();
        }

        public override IEnumerator GetEnumerator()
        {
            return new DbEnumerator((IDataReader)this, IsCommandBehavior(CommandBehavior.CloseConnection));
        }

        public override Type GetFieldType(int i)
        {
            if (null != _dataCache)
            {
                DbSchemaInfo info = _dataCache.GetSchema(i);
                if (info._type == null)
                {
                    info._type = GetSqlType(i)._type;
                }
                return info._type;
            }
            throw ADP.DataReaderNoData();
        }

        public override string GetName(int i)
        {
            if (null != _dataCache)
            {
                DbSchemaInfo info = _dataCache.GetSchema(i);
                if (info._name == null)
                {
                    info._name = GetColAttributeStr(i, ODBC32.SQL_DESC.NAME, ODBC32.SQL_COLUMN.NAME, ODBC32.HANDLER.THROW);
                    if (null == info._name)
                    { // MDAC 66681
                        info._name = "";
                    }
                }
                return info._name;
            }
            throw ADP.DataReaderNoData();
        }

        public override int GetOrdinal(string value)
        {
            if (null == _fieldNameLookup)
            {
                if (null == _dataCache)
                {
                    throw ADP.DataReaderNoData();
                }
                _fieldNameLookup = new FieldNameLookup(this, -1);
            }
            return _fieldNameLookup.GetOrdinal(value); // MDAC 71470
        }

        private int IndexOf(string value)
        {
            if (null == _fieldNameLookup)
            {
                if (null == _dataCache)
                {
                    throw ADP.DataReaderNoData();
                }
                _fieldNameLookup = new FieldNameLookup(this, -1);
            }
            return _fieldNameLookup.IndexOf(value);
        }

        private bool IsCommandBehavior(CommandBehavior condition)
        {
            return (condition == (condition & _commandBehavior));
        }

        internal object GetValue(int i, TypeMap typemap)
        {
            switch (typemap._sql_type)
            {
                case ODBC32.SQL_TYPE.CHAR:
                case ODBC32.SQL_TYPE.VARCHAR:
                case ODBC32.SQL_TYPE.LONGVARCHAR:
                case ODBC32.SQL_TYPE.WCHAR:
                case ODBC32.SQL_TYPE.WVARCHAR:
                case ODBC32.SQL_TYPE.WLONGVARCHAR:
                    return internalGetString(i);

                case ODBC32.SQL_TYPE.DECIMAL:
                case ODBC32.SQL_TYPE.NUMERIC:
                    return internalGetDecimal(i);

                case ODBC32.SQL_TYPE.SMALLINT:
                    return internalGetInt16(i);

                case ODBC32.SQL_TYPE.INTEGER:
                    return internalGetInt32(i);

                case ODBC32.SQL_TYPE.REAL:
                    return internalGetFloat(i);

                case ODBC32.SQL_TYPE.FLOAT:
                case ODBC32.SQL_TYPE.DOUBLE:
                    return internalGetDouble(i);

                case ODBC32.SQL_TYPE.BIT:
                    return internalGetBoolean(i);

                case ODBC32.SQL_TYPE.TINYINT:
                    return internalGetByte(i);

                case ODBC32.SQL_TYPE.BIGINT:
                    return internalGetInt64(i);

                case ODBC32.SQL_TYPE.BINARY:
                case ODBC32.SQL_TYPE.VARBINARY:
                case ODBC32.SQL_TYPE.LONGVARBINARY:
                    return internalGetBytes(i);

                case ODBC32.SQL_TYPE.TYPE_DATE:
                    return internalGetDate(i);

                case ODBC32.SQL_TYPE.TYPE_TIME:
                    return internalGetTime(i);

                //                  case ODBC32.SQL_TYPE.TIMESTAMP:
                case ODBC32.SQL_TYPE.TYPE_TIMESTAMP:
                    return internalGetDateTime(i);

                case ODBC32.SQL_TYPE.GUID:
                    return internalGetGuid(i);

                case ODBC32.SQL_TYPE.SS_VARIANT:
                    //Note: SQL Variant is not an ODBC defined type.
                    //Instead of just binding it as a byte[], which is not very useful,
                    //we will actually code this specific for SQL Server.

                    //To obtain the sub-type, we need to first load the context (obtaining the length
                    //will work), and then query for a speicial SQLServer specific attribute.
                    if (_isRead)
                    {
                        if (_dataCache.AccessIndex(i) == null)
                        {
                            int dummy;
                            bool isNotDbNull = QueryFieldInfo(i, ODBC32.SQL_C.BINARY, out dummy);
                            // if the value is DBNull, QueryFieldInfo will cache it
                            if (isNotDbNull)
                            {
                                //Delegate (for the sub type)
                                ODBC32.SQL_TYPE subtype = (ODBC32.SQL_TYPE)(int)GetColAttribute(i, (ODBC32.SQL_DESC)ODBC32.SQL_CA_SS.VARIANT_SQL_TYPE, (ODBC32.SQL_COLUMN)(-1), ODBC32.HANDLER.THROW);
                                return GetValue(i, TypeMap.FromSqlType(subtype));
                            }
                        }
                        return _dataCache[i];
                    }
                    throw ADP.DataReaderNoData();



                default:
                    //Unknown types are bound strictly as binary
                    return internalGetBytes(i);
            }
        }

        public override object GetValue(int i)
        {
            if (_isRead)
            {
                if (_dataCache.AccessIndex(i) == null)
                {
                    _dataCache[i] = GetValue(i, GetSqlType(i));
                }
                return _dataCache[i];
            }
            throw ADP.DataReaderNoData();
        }

        public override int GetValues(object[] values)
        {
            if (_isRead)
            {
                int nValues = Math.Min(values.Length, FieldCount);
                for (int i = 0; i < nValues; ++i)
                {
                    values[i] = GetValue(i);
                }
                return nValues;
            }
            throw ADP.DataReaderNoData();
        }

        private TypeMap GetSqlType(int i)
        {
            //Note: Types are always returned (advertised) from ODBC as SQL_TYPEs, and
            //are always bound by the user as SQL_C types.
            TypeMap typeMap;
            DbSchemaInfo info = _dataCache.GetSchema(i);
            if (!info._dbtype.HasValue)
            {
                info._dbtype = unchecked((ODBC32.SQL_TYPE)(int)GetColAttribute(i, ODBC32.SQL_DESC.CONCISE_TYPE, ODBC32.SQL_COLUMN.TYPE, ODBC32.HANDLER.THROW));
                typeMap = TypeMap.FromSqlType(info._dbtype.Value);
                if (typeMap._signType == true)
                {
                    bool sign = (GetColAttribute(i, ODBC32.SQL_DESC.UNSIGNED, ODBC32.SQL_COLUMN.UNSIGNED, ODBC32.HANDLER.THROW).ToInt64() != 0);
                    typeMap = TypeMap.UpgradeSignedType(typeMap, sign);
                    info._dbtype = typeMap._sql_type;
                }
            }
            else
            {
                typeMap = TypeMap.FromSqlType(info._dbtype.Value);
            }
            Connection.SetSupportedType(info._dbtype.Value);
            return typeMap;
        }

        public override bool IsDBNull(int i)
        {
            //  Note: ODBC SQLGetData doesn't allow retriving the column value twice.
            //  The reational is that for ForwardOnly access (the default and LCD of drivers)
            //  we cannot obtain the data more than once, and even GetData(0) (to determine is-null)
            //  still obtains data for fixed length types.

            //  So simple code like:
            //      if(!rReader.IsDBNull(i))
            //          rReader.GetInt32(i)
            //
            //  Would fail, unless we cache on the IsDBNull call, and return the cached
            //  item for GetInt32.  This actually improves perf anyway, (even if the driver could
            //  support it), since we are not making a separate interop call...

            // Bug SQLBUVSTS01:110664 - available cases:
            // 1. random access - always cache the value (as before the fix), to minimize regression risk 
            // 2. sequential access, fixed-size value: continue caching the value as before, again to minimize regression risk 
            // 3. sequential access, variable-length value: this scenario did not work properly before the fix. Fix
            //                                              it now by calling GetData(length = 0).
            // 4. sequential access, cache value exists: just check the cache for DbNull (no validations done, again to minimize regressions)

            if (!IsCommandBehavior(CommandBehavior.SequentialAccess))
                return Convert.IsDBNull(GetValue(i)); // case 1, cache the value

            // in 'ideal' Sequential access support, we do not want cache the value in order to check if it is DbNull or not.
            // But, to minimize regressions, we will continue caching the fixed-size values (case 2), even with SequentialAccess
            // only in case of SequentialAccess with variable length data types (case 3), we will use GetData with zero length.

            object cachedObj = _dataCache[i];
            if (cachedObj != null)
            {
                // case 4 - if cached object was created before, use it
                return Convert.IsDBNull(cachedObj);
            }

            // no cache, check for the type (cases 2 and 3)
            TypeMap typeMap = GetSqlType(i);
            if (typeMap._bufferSize > 0)
            {
                // case 2 - fixed-size types have _bufferSize set to positive value
                // call GetValue(i) as before the fix of SQLBUVSTS01:110664
                // note, when SQLGetData is called for a fixed length type, the buffer size is always ignored and
                // the data will always be read off the wire
                return Convert.IsDBNull(GetValue(i));
            }
            else
            {
                // case 3 - the data has variable-length type, read zero-length data to query for null
                // QueryFieldInfo will return false only if the object cached as DbNull
                // QueryFieldInfo will put DbNull in cache only if the SQLGetData returns SQL_NULL_DATA, otherwise it does not change it
                int dummy;
                return !QueryFieldInfo(i, typeMap._sql_c, out dummy);
            }
        }

        public override byte GetByte(int i)
        {
            return (byte)internalGetByte(i);
        }

        private object internalGetByte(int i)
        {
            if (_isRead)
            {
                if (_dataCache.AccessIndex(i) == null)
                {
                    if (GetData(i, ODBC32.SQL_C.UTINYINT))
                    {
                        _dataCache[i] = Buffer.ReadByte(0);
                    }
                }
                return _dataCache[i];
            }
            throw ADP.DataReaderNoData();
        }

        public override char GetChar(int i)
        {
            return (char)internalGetChar(i);
        }
        private object internalGetChar(int i)
        {
            if (_isRead)
            {
                if (_dataCache.AccessIndex(i) == null)
                {
                    if (GetData(i, ODBC32.SQL_C.WCHAR))
                    {
                        _dataCache[i] = Buffer.ReadChar(0);
                    }
                }
                return _dataCache[i];
            }
            throw ADP.DataReaderNoData();
        }

        public override short GetInt16(int i)
        {
            return (short)internalGetInt16(i);
        }
        private object internalGetInt16(int i)
        {
            if (_isRead)
            {
                if (_dataCache.AccessIndex(i) == null)
                {
                    if (GetData(i, ODBC32.SQL_C.SSHORT))
                    {
                        _dataCache[i] = Buffer.ReadInt16(0);
                    }
                }
                return _dataCache[i];
            }
            throw ADP.DataReaderNoData();
        }

        public override int GetInt32(int i)
        {
            return (int)internalGetInt32(i);
        }
        private object internalGetInt32(int i)
        {
            if (_isRead)
            {
                if (_dataCache.AccessIndex(i) == null)
                {
                    if (GetData(i, ODBC32.SQL_C.SLONG))
                    {
                        _dataCache[i] = Buffer.ReadInt32(0);
                    }
                }
                return _dataCache[i];
            }
            throw ADP.DataReaderNoData();
        }

        public override long GetInt64(int i)
        {
            return (long)internalGetInt64(i);
        }
        // ---------------------------------------------------------------------------------------------- //
        // internal internalGetInt64
        // -------------------------
        // Get Value of type SQL_BIGINT
        // Since the driver refused to accept the type SQL_BIGINT we read that
        // as SQL_C_WCHAR and convert it back to the Int64 data type
        //
        private object internalGetInt64(int i)
        {
            if (_isRead)
            {
                if (_dataCache.AccessIndex(i) == null)
                {
                    if (GetData(i, ODBC32.SQL_C.WCHAR))
                    {
                        string value = (string)Buffer.MarshalToManaged(0, ODBC32.SQL_C.WCHAR, ODBC32.SQL_NTS);
                        _dataCache[i] = long.Parse(value, CultureInfo.InvariantCulture);
                    }
                }
                return _dataCache[i];
            }
            throw ADP.DataReaderNoData();
        }

        public override bool GetBoolean(int i)
        {
            return (bool)internalGetBoolean(i);
        }
        private object internalGetBoolean(int i)
        {
            if (_isRead)
            {
                if (_dataCache.AccessIndex(i) == null)
                {
                    if (GetData(i, ODBC32.SQL_C.BIT))
                    {
                        _dataCache[i] = Buffer.MarshalToManaged(0, ODBC32.SQL_C.BIT, -1);
                    }
                }
                return _dataCache[i];
            }
            throw ADP.DataReaderNoData();
        }

        public override float GetFloat(int i)
        {
            return (float)internalGetFloat(i);
        }
        private object internalGetFloat(int i)
        {
            if (_isRead)
            {
                if (_dataCache.AccessIndex(i) == null)
                {
                    if (GetData(i, ODBC32.SQL_C.REAL))
                    {
                        _dataCache[i] = Buffer.ReadSingle(0);
                    }
                }
                return _dataCache[i];
            }
            throw ADP.DataReaderNoData();
        }

        public DateTime GetDate(int i)
        {
            return (DateTime)internalGetDate(i);
        }

        private object internalGetDate(int i)
        {
            if (_isRead)
            {
                if (_dataCache.AccessIndex(i) == null)
                {
                    if (GetData(i, ODBC32.SQL_C.TYPE_DATE))
                    {
                        _dataCache[i] = Buffer.MarshalToManaged(0, ODBC32.SQL_C.TYPE_DATE, -1);
                    }
                }
                return _dataCache[i];
            }
            throw ADP.DataReaderNoData();
        }

        public override DateTime GetDateTime(int i)
        {
            return (DateTime)internalGetDateTime(i);
        }

        private object internalGetDateTime(int i)
        {
            if (_isRead)
            {
                if (_dataCache.AccessIndex(i) == null)
                {
                    if (GetData(i, ODBC32.SQL_C.TYPE_TIMESTAMP))
                    {
                        _dataCache[i] = Buffer.MarshalToManaged(0, ODBC32.SQL_C.TYPE_TIMESTAMP, -1);
                    }
                }
                return _dataCache[i];
            }
            throw ADP.DataReaderNoData();
        }

        public override decimal GetDecimal(int i)
        {
            return (decimal)internalGetDecimal(i);
        }

        // ---------------------------------------------------------------------------------------------- //
        // internal GetDecimal
        // -------------------
        // Get Value of type SQL_DECIMAL or SQL_NUMERIC
        // Due to provider incompatibilities with SQL_DECIMAL or SQL_NUMERIC types we always read the value
        // as SQL_C_WCHAR and convert it back to the Decimal data type
        //
        private object internalGetDecimal(int i)
        {
            if (_isRead)
            {
                if (_dataCache.AccessIndex(i) == null)
                {
                    if (GetData(i, ODBC32.SQL_C.WCHAR))
                    {
                        string s = null;
                        try
                        {
                            s = (string)Buffer.MarshalToManaged(0, ODBC32.SQL_C.WCHAR, ODBC32.SQL_NTS);
                            _dataCache[i] = decimal.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
                        }
                        catch (OverflowException e)
                        {
                            _dataCache[i] = s;
                            throw e;
                        }
                    }
                }
                return _dataCache[i];
            }
            throw ADP.DataReaderNoData();
        }

        public override double GetDouble(int i)
        {
            return (double)internalGetDouble(i);
        }
        private object internalGetDouble(int i)
        {
            if (_isRead)
            {
                if (_dataCache.AccessIndex(i) == null)
                {
                    if (GetData(i, ODBC32.SQL_C.DOUBLE))
                    {
                        _dataCache[i] = Buffer.ReadDouble(0);
                    }
                }
                return _dataCache[i];
            }
            throw ADP.DataReaderNoData();
        }

        public override Guid GetGuid(int i)
        {
            return (Guid)internalGetGuid(i);
        }

        private object internalGetGuid(int i)
        {
            if (_isRead)
            {
                if (_dataCache.AccessIndex(i) == null)
                {
                    if (GetData(i, ODBC32.SQL_C.GUID))
                    {
                        _dataCache[i] = Buffer.ReadGuid(0);
                    }
                }
                return _dataCache[i];
            }
            throw ADP.DataReaderNoData();
        }

        public override string GetString(int i)
        {
            return (string)internalGetString(i);
        }

        private object internalGetString(int i)
        {
            if (_isRead)
            {
                if (_dataCache.AccessIndex(i) == null)
                {
                    // Obtain _ALL_ the characters
                    // Note: We can bind directly as WCHAR in ODBC and the DM will convert to and
                    // from ANSI if not supported by the driver.
                    //

                    // Note: The driver always returns the raw length of the data, minus the
                    // terminator.  This means that our buffer length always includes the terminator
                    // charactor, so when determining which characters to count, and if more data
                    // exists, it should not take the terminator into effect.
                    //
                    CNativeBuffer buffer = Buffer;
                    // that does not make sense unless we expect four byte terminators
                    int cbMaxData = buffer.Length - 4;

                    // The first time GetData returns the true length (so we have to min it).
                    // We also pass in the true length to the marshal function since there could be
                    // embedded nulls
                    //
                    int lengthOrIndicator;
                    if (GetData(i, ODBC32.SQL_C.WCHAR, buffer.Length - 2, out lengthOrIndicator))
                    {
                        // RFC 50002644: we do not expect negative values from GetData call except SQL_NO_TOTAL(== -4)
                        // note that in general you should not trust third-party providers so such asserts should be
                        // followed by exception. I did not add it now to avoid breaking change
                        Debug.Assert(lengthOrIndicator >= 0 || lengthOrIndicator == ODBC32.SQL_NO_TOTAL, "unexpected lengthOrIndicator value");

                        if (lengthOrIndicator <= cbMaxData && (ODBC32.SQL_NO_TOTAL != lengthOrIndicator))
                        {
                            // all data read? good! Directly marshal to a string and we're done
                            //
                            string strdata = buffer.PtrToStringUni(0, Math.Min(lengthOrIndicator, cbMaxData) / 2);
                            _dataCache[i] = strdata;
                            return strdata;
                        }

                        // We need to chunk the data
                        // Char[] buffer for the junks
                        // StringBuilder for the actual string
                        //
                        char[] rgChars = new char[cbMaxData / 2];

                        // RFC 50002644: negative value cannot be used for capacity.
                        // in case of SQL_NO_TOTAL, set the capacity to cbMaxData, StringBuilder will automatically reallocate 
                        // its internal buffer when appending more data
                        int cbBuilderInitialCapacity = (lengthOrIndicator == ODBC32.SQL_NO_TOTAL) ? cbMaxData : lengthOrIndicator;
                        StringBuilder builder = new StringBuilder(cbBuilderInitialCapacity / 2);

                        bool gotData;
                        int cchJunk;
                        int cbActual = cbMaxData;
                        int cbMissing = (ODBC32.SQL_NO_TOTAL == lengthOrIndicator) ? -1 : lengthOrIndicator - cbActual;

                        do
                        {
                            cchJunk = cbActual / 2;
                            buffer.ReadChars(0, rgChars, 0, cchJunk);
                            builder.Append(rgChars, 0, cchJunk);

                            if (0 == cbMissing)
                            {
                                break;  // done
                            }

                            gotData = GetData(i, ODBC32.SQL_C.WCHAR, buffer.Length - 2, out lengthOrIndicator);
                            // RFC 50002644: we do not expect negative values from GetData call except SQL_NO_TOTAL(== -4)
                            // note that in general you should not trust third-party providers so such asserts should be
                            // followed by exception. I did not add it now to avoid breaking change
                            Debug.Assert(lengthOrIndicator >= 0 || lengthOrIndicator == ODBC32.SQL_NO_TOTAL, "unexpected lengthOrIndicator value");

                            if (ODBC32.SQL_NO_TOTAL != lengthOrIndicator)
                            {
                                cbActual = Math.Min(lengthOrIndicator, cbMaxData);
                                if (0 < cbMissing)
                                {
                                    cbMissing -= cbActual;
                                }
                                else
                                {
                                    // it is a last call to SqlGetData that started with SQL_NO_TOTAL
                                    // the last call to SqlGetData must always return the length of the
                                    // data, not zero or SqlNoTotal (see Odbc Programmers Reference)
                                    Debug.Assert(cbMissing == -1 && lengthOrIndicator <= cbMaxData);
                                    cbMissing = 0;
                                }
                            }
                        }
                        while (gotData);

                        _dataCache[i] = builder.ToString();
                    }
                }
                return _dataCache[i];
            }
            throw ADP.DataReaderNoData();
        }

        public TimeSpan GetTime(int i)
        {
            return (TimeSpan)internalGetTime(i);
        }

        private object internalGetTime(int i)
        {
            if (_isRead)
            {
                if (_dataCache.AccessIndex(i) == null)
                {
                    if (GetData(i, ODBC32.SQL_C.TYPE_TIME))
                    {
                        _dataCache[i] = Buffer.MarshalToManaged(0, ODBC32.SQL_C.TYPE_TIME, -1);
                    }
                }
                return _dataCache[i];
            }
            throw ADP.DataReaderNoData();
        }

        private void SetCurrentRowColumnInfo(int row, int column)
        {
            if (_row != row || _column != column)
            {
                _row = row;
                _column = column;

                // reset the blob reader when moved to new column
                _sequentialBytesRead = 0;
            }
        }

        public override long GetBytes(int i, long dataIndex, byte[] buffer, int bufferIndex, int length)
        {
            return GetBytesOrChars(i, dataIndex, buffer, false /* bytes buffer */, bufferIndex, length);
        }
        public override long GetChars(int i, long dataIndex, char[] buffer, int bufferIndex, int length)
        {
            return GetBytesOrChars(i, dataIndex, buffer, true /* chars buffer */, bufferIndex, length);
        }

        // unify the implementation of GetChars and GetBytes to prevent code duplicate
        private long GetBytesOrChars(int i, long dataIndex, Array buffer, bool isCharsBuffer, int bufferIndex, int length)
        {
            if (IsClosed)
            {
                throw ADP.DataReaderNoData();
            }
            if (!_isRead)
            {
                throw ADP.DataReaderNoData();
            }
            if (dataIndex < 0)
            {
                // test only for negative value here, Int32.MaxValue will be validated only in case of random access
                throw ADP.ArgumentOutOfRange(nameof(dataIndex));
            }
            if (bufferIndex < 0)
            {
                throw ADP.ArgumentOutOfRange(nameof(bufferIndex));
            }
            if (length < 0)
            {
                throw ADP.ArgumentOutOfRange(nameof(length));
            }

            string originalMethodName = isCharsBuffer ? "GetChars" : "GetBytes";

            // row/column info will be reset only if changed
            SetCurrentRowColumnInfo(_row, i);

            // Possible cases:
            // 1. random access, user asks for the value first time: bring it and cache the value
            // 2. random access, user already queried the value: use the cache
            // 3. sequential access, cache exists: user already read this value using different method (it becomes cached) 
            //                       use the cache - preserve the original behavior to minimize regression risk
            // 4. sequential access, no cache: (fixed now) user reads the bytes/chars in sequential order (no cache)

            object cachedObj = null;                 // The cached object (if there is one)

            // Get cached object, ensure the correct type using explicit cast, to preserve same behavior as before
            if (isCharsBuffer)
                cachedObj = (string)_dataCache[i];
            else
                cachedObj = (byte[])_dataCache[i];

            bool isRandomAccess = !IsCommandBehavior(CommandBehavior.SequentialAccess);

            if (isRandomAccess || (cachedObj != null))
            {
                // random access (cases 1 or 2) and sequential access with cache (case 3)
                // preserve the original behavior as before the fix

                if (int.MaxValue < dataIndex)
                {
                    // indices greater than allocable size are not supported in random access
                    // (negative value is already tested in the beginning of ths function)
                    throw ADP.ArgumentOutOfRange(nameof(dataIndex));
                }

                if (cachedObj == null)
                {
                    // case 1, get the value and cache it
                    // internalGetString/internalGetBytes will get the entire value and cache it,
                    // since we are not in SequentialAccess (isRandomAccess is true), it is OK

                    if (isCharsBuffer)
                    {
                        cachedObj = (string)internalGetString(i);
                        Debug.Assert((cachedObj != null), "internalGetString should always return non-null or raise exception");
                    }
                    else
                    {
                        cachedObj = (byte[])internalGetBytes(i);
                        Debug.Assert((cachedObj != null), "internalGetBytes should always return non-null or raise exception");
                    }

                    // continue to case 2
                }

                // after this point the value is cached (case 2 or 3)
                // if it is DbNull, cast exception will be raised (same as before the 110664 fix)
                int cachedObjectLength = isCharsBuffer ? ((string)cachedObj).Length : ((byte[])cachedObj).Length;

                // the user can ask for the length of the field by passing in a null pointer for the buffer
                if (buffer == null)
                {
                    // return the length if that's all what user needs
                    return cachedObjectLength;
                }

                // user asks for bytes

                if (length == 0)
                {
                    return 0;   // Nothing to do ...
                }

                if (dataIndex >= cachedObjectLength)
                {
                    // no more bytes to read
                    // see also MDAC bug 73298
                    return 0;
                }

                int lengthFromDataIndex = cachedObjectLength - (int)dataIndex;
                int lengthOfCopy = Math.Min(lengthFromDataIndex, length);

                // silently reduce the length to avoid regression from EVERETT
                lengthOfCopy = Math.Min(lengthOfCopy, buffer.Length - bufferIndex);
                if (lengthOfCopy <= 0) return 0;                    // MDAC Bug 73298

                if (isCharsBuffer)
                    ((string)cachedObj).CopyTo((int)dataIndex, (char[])buffer, bufferIndex, lengthOfCopy);
                else
                    Array.Copy((byte[])cachedObj, (int)dataIndex, (byte[])buffer, bufferIndex, lengthOfCopy);

                return lengthOfCopy;
            }
            else
            {
                // sequential access, case 4

                // SQLBU:532243 -- For SequentialAccess we need to read a chunk of
                // data and not cache it. 
                // Note: If the object was previous cached (see case 3 above), the function will go thru 'if' path, to minimize
                // regressions

                // the user can ask for the length of the field by passing in a null pointer for the buffer
                if (buffer == null)
                {
                    // Get len. of remaining data from provider
                    ODBC32.SQL_C sqlctype;
                    int cbLengthOrIndicator;
                    bool isDbNull;

                    sqlctype = isCharsBuffer ? ODBC32.SQL_C.WCHAR : ODBC32.SQL_C.BINARY;
                    isDbNull = !QueryFieldInfo(i, sqlctype, out cbLengthOrIndicator);

                    if (isDbNull)
                    {
                        // SQLBU 266054:

                        // GetChars:
                        //   in Orcas RTM: GetChars has always raised InvalidCastException.
                        //   in Orcas SP1: GetChars returned 0 if DbNull is not cached yet and InvalidCastException if it is in cache (from case 3).
                        //   Managed Providers team has decided to fix the GetChars behavior and raise InvalidCastException, as it was in RTM
                        //   Reason: returing 0 is wrong behavior since it conflicts with return value in case of empty data

                        // GetBytes:
                        //   In Orcas RTM: GetBytes(null buffer) returned -1 for null value if DbNull is not cached yet. 
                        //   But, after calling IsDBNull, GetBytes(null) raised InvalidCastException.
                        //   In Orcas SP1: GetBytes always raises InvalidCastException for null value.                            
                        //   Managed Providers team has decided to keep the behavior of RTM for this case to fix the RTM's breaking change.
                        //   Reason: while -1 is wrong behavior, people might be already relying on it, so we should not be changing it.
                        //   Note: this will happen only on the first call to GetBytes(with null buffer). 
                        //   If IsDbNull has already been called before or for second call to query for size,
                        //   DBNull is cached and GetBytes raises InvalidCastException in case 3 (see the cases above in this method).

                        if (isCharsBuffer)
                        {
                            throw ADP.InvalidCast();
                        }
                        else
                        {
                            return -1;
                        }
                    }
                    else
                    {
                        // the value is not null

                        // SQLBU 266054:
                        // If cbLengthOrIndicator is SQL_NO_TOTAL (-4), this call returns -4 or -2, depending on the type (GetChars=>-2, GetBytes=>-4).
                        // This is the Orcas RTM and SP1 behavior, changing this would be a breaking change.
                        // SQL_NO_TOTAL means that the driver does not know what is the remained lenght of the data, so we cannot really guess the value here.
                        // Reason: while returning different negative values depending on the type seems inconsistent, 
                        // this is what we did in Orcas RTM and SP1 and user code might rely on this behavior => changing it would be a breaking change.
                        if (isCharsBuffer)
                        {
                            return cbLengthOrIndicator / 2; // return length in wide characters or -2 if driver returns SQL_NO_TOTAL
                        }
                        else
                        {
                            return cbLengthOrIndicator; // return length in bytes or -4 if driver returns SQL_NO_TOTAL
                        }
                    }
                }
                else
                {
                    // buffer != null, read the data

                    // check if user tries to read data that was already received
                    // if yes, this violates 'sequential access'
                    if ((isCharsBuffer && dataIndex < _sequentialBytesRead / 2) ||
                        (!isCharsBuffer && dataIndex < _sequentialBytesRead))
                    {
                        // backward reading is not allowed in sequential access
                        throw ADP.NonSeqByteAccess(
                            dataIndex,
                            _sequentialBytesRead,
                            originalMethodName
                            );
                    }

                    // note that it is actually not possible to read with an offset (dataIndex)
                    // therefore, adjust the data index relative to number of bytes already read
                    if (isCharsBuffer)
                        dataIndex -= _sequentialBytesRead / 2;
                    else
                        dataIndex -= _sequentialBytesRead;

                    if (dataIndex > 0)
                    {
                        // user asked to skip bytes - it is OK, even in case of sequential access
                        // forward the stream by dataIndex bytes/chars
                        int charsOrBytesRead = readBytesOrCharsSequentialAccess(i, null, isCharsBuffer, 0, dataIndex);
                        if (charsOrBytesRead < dataIndex)
                        {
                            // the stream ended before we forwarded to the requested index, stop now
                            return 0;
                        }
                    }

                    // ODBC driver now points to the correct position, start filling the user buffer from now

                    // Make sure we don't overflow the user provided buffer
                    // Note: SqlDataReader will raise exception if there is no enough room for length requested.
                    // In case of ODBC, I decided to keep this consistent with random access after consulting with PM.
                    length = Math.Min(length, buffer.Length - bufferIndex);
                    if (length <= 0)
                    {
                        // SQLBU 266054:
                        // if the data is null, the ideal behavior here is to raise InvalidCastException. But,
                        // * GetBytes returned 0 in Orcas RTM and SP1, continue to do so to avoid breaking change from Orcas RTM and SP1.
                        // * GetChars raised exception in RTM, and returned 0 in SP1: we decided to revert back to the RTM's behavior and raise InvalidCast
                        if (isCharsBuffer)
                        {
                            // for GetChars, ensure data is not null
                            // 2 bytes for '\0' termination, no data is actually read from the driver
                            int cbLengthOrIndicator;
                            bool isDbNull = !QueryFieldInfo(i, ODBC32.SQL_C.WCHAR, out cbLengthOrIndicator);
                            if (isDbNull)
                            {
                                throw ADP.InvalidCast();
                            }
                        }
                        // else - GetBytes - return now
                        return 0;
                    }

                    // fill the user's buffer
                    return readBytesOrCharsSequentialAccess(i, buffer, isCharsBuffer, bufferIndex, length);
                }
            }
        }

        // fill the user's buffer (char[] or byte[], depending on isCharsBuffer)
        // if buffer is null, just skip the bytesOrCharsLength bytes or chars
        private int readBytesOrCharsSequentialAccess(int i, Array buffer, bool isCharsBuffer, int bufferIndex, long bytesOrCharsLength)
        {
            Debug.Assert(bufferIndex >= 0, "Negative buffer index");
            Debug.Assert(bytesOrCharsLength >= 0, "Negative number of bytes or chars to read");

            // validated by the caller
            Debug.Assert(buffer == null || bytesOrCharsLength <= (buffer.Length - bufferIndex), "Not enough space in user's buffer");

            int totalBytesOrCharsRead = 0;
            string originalMethodName = isCharsBuffer ? "GetChars" : "GetBytes";

            // we need length in bytes, b/c that is what SQLGetData expects
            long cbLength = (isCharsBuffer) ? checked(bytesOrCharsLength * 2) : bytesOrCharsLength;

            // continue reading from the driver until we fill the user's buffer or until no more data is available
            // the data is pumped first into the internal native buffer and after that copied into the user's one if buffer is not null
            CNativeBuffer internalNativeBuffer = this.Buffer;

            // read the data in loop up to th user's length
            // if the data size is less than requested or in case of error, the while loop will stop in the middle
            while (cbLength > 0)
            {
                // max data to be read, in bytes, not including null-terminator for WCHARs
                int cbReadMax;

                // read from the driver
                bool isNotDbNull;
                int cbTotal;
                // read either bytes or chars, depending on the method called
                if (isCharsBuffer)
                {
                    // for WCHAR buffers, we need to leave space for null-terminator (2 bytes)
                    // reserve 2 bytes for null-terminator and 2 bytes to prevent assert in GetData
                    // if SQL_NO_TOTAL is returned, this ammount is read from the wire, in bytes
                    cbReadMax = (int)Math.Min(cbLength, internalNativeBuffer.Length - 4);

                    // SQLGetData will always append it - we do not to copy it to user's buffer
                    isNotDbNull = GetData(i, ODBC32.SQL_C.WCHAR, cbReadMax + 2, out cbTotal);
                }
                else
                {
                    // reserve 2 bytes to prevent assert in GetData
                    // when querying bytes, no need to reserve space for null
                    cbReadMax = (int)Math.Min(cbLength, internalNativeBuffer.Length - 2);

                    isNotDbNull = GetData(i, ODBC32.SQL_C.BINARY, cbReadMax, out cbTotal);
                }

                if (!isNotDbNull)
                {
                    // DbNull received, neither GetBytes nor GetChars should be used with DbNull value
                    // two options
                    // 1. be consistent with SqlDataReader, raise SqlNullValueException
                    // 2. be consistent with other Get* methods of OdbcDataReader and raise InvalidCastException
                    // after consulting with Himanshu (PM), decided to go with option 2 (raise cast exception)
                    throw ADP.InvalidCast();
                }

                int cbRead; // will hold number of bytes read in this loop
                bool noDataRemained = false;
                if (cbTotal == 0)
                {
                    // no bytes read, stop
                    break;
                }
                else if (ODBC32.SQL_NO_TOTAL == cbTotal)
                {
                    // the driver has filled the internal buffer, but the length of remained data is still unknown
                    // we will continue looping until SQLGetData indicates the end of data or user buffer is fully filled
                    cbRead = cbReadMax;
                }
                else
                {
                    Debug.Assert((cbTotal > 0), "GetData returned negative value, which is not SQL_NO_TOTAL");
                    // GetData uses SQLGetData, which StrLen_or_IndPtr (cbTotal in our case) to the current buf + remained buf (if any)
                    if (cbTotal > cbReadMax)
                    {
                        // in this case the amount of bytes/chars read will be the max requested (and more bytes can be read)
                        cbRead = cbReadMax;
                    }
                    else
                    {
                        // SQLGetData read all the available data, no more remained
                        // continue processing this chunk and stop
                        cbRead = cbTotal;
                        noDataRemained = true;
                    }
                }

                _sequentialBytesRead += cbRead;

                // update internal state and copy the data to user's buffer
                if (isCharsBuffer)
                {
                    int cchRead = cbRead / 2;
                    if (buffer != null)
                    {
                        internalNativeBuffer.ReadChars(0, (char[])buffer, bufferIndex, cchRead);
                        bufferIndex += cchRead;
                    }
                    totalBytesOrCharsRead += cchRead;
                }
                else
                {
                    if (buffer != null)
                    {
                        internalNativeBuffer.ReadBytes(0, (byte[])buffer, bufferIndex, cbRead);
                        bufferIndex += cbRead;
                    }
                    totalBytesOrCharsRead += cbRead;
                }

                cbLength -= cbRead;

                // stop if no data remained
                if (noDataRemained)
                    break;
            }

            return totalBytesOrCharsRead;
        }

        private object internalGetBytes(int i)
        {
            if (_dataCache.AccessIndex(i) == null)
            {
                // Obtain _ALL_ the bytes...
                // The first time GetData returns the true length (so we have to min it).
                byte[] rgBytes;
                int cbBufferLen = Buffer.Length - 4;
                int cbActual;
                int cbOffset = 0;

                if (GetData(i, ODBC32.SQL_C.BINARY, cbBufferLen, out cbActual))
                {
                    CNativeBuffer buffer = Buffer;

                    if (ODBC32.SQL_NO_TOTAL != cbActual)
                    {
                        rgBytes = new byte[cbActual];
                        Buffer.ReadBytes(0, rgBytes, cbOffset, Math.Min(cbActual, cbBufferLen));

                        // Chunking.  The data may be larger than our native buffer.  In which case
                        // instead of growing the buffer (out of control), we will read in chunks to
                        // reduce memory footprint size.
                        while (cbActual > cbBufferLen)
                        {
                            // The first time GetData returns the true length.  Then successive calls
                            // return the remaining data.
                            bool flag = GetData(i, ODBC32.SQL_C.BINARY, cbBufferLen, out cbActual);
                            Debug.Assert(flag, "internalGetBytes - unexpected invalid result inside if-block");

                            cbOffset += cbBufferLen;
                            buffer.ReadBytes(0, rgBytes, cbOffset, Math.Min(cbActual, cbBufferLen));
                        }
                    }
                    else
                    {
                        List<byte[]> junkArray = new List<byte[]>();
                        int junkSize;
                        int totalSize = 0;
                        do
                        {
                            junkSize = (ODBC32.SQL_NO_TOTAL != cbActual) ? cbActual : cbBufferLen;
                            rgBytes = new byte[junkSize];
                            totalSize += junkSize;
                            buffer.ReadBytes(0, rgBytes, 0, junkSize);
                            junkArray.Add(rgBytes);
                        }
                        while ((ODBC32.SQL_NO_TOTAL == cbActual) && GetData(i, ODBC32.SQL_C.BINARY, cbBufferLen, out cbActual));

                        rgBytes = new byte[totalSize];
                        foreach (byte[] junk in junkArray)
                        {
                            junk.CopyTo(rgBytes, cbOffset);
                            cbOffset += junk.Length;
                        }
                    }

                    // always update the cache
                    _dataCache[i] = rgBytes;
                }
            }
            return _dataCache[i];
        }

        // GetColAttribute
        // ---------------
        // [IN] iColumn   ColumnNumber
        // [IN] v3FieldId FieldIdentifier of the attribute for version3 drivers (>=3.0)
        // [IN] v2FieldId FieldIdentifier of the attribute for version2 drivers (<3.0)
        //
        // returns the value of the FieldIdentifier field of the column
        // or -1 if the FieldIdentifier wasn't supported by the driver
        //
        private SQLLEN GetColAttribute(int iColumn, ODBC32.SQL_DESC v3FieldId, ODBC32.SQL_COLUMN v2FieldId, ODBC32.HANDLER handler)
        {
            short cchNameLength = 0;
            SQLLEN numericAttribute;
            ODBC32.RetCode retcode;

            // protect against dead connection, dead or canceling command.
            if ((Connection == null) || _cmdWrapper.Canceling)
            {
                return -1;
            }

            //Ordinals are 1:base in odbc
            OdbcStatementHandle stmt = StatementHandle;
            if (Connection.IsV3Driver)
            {
                retcode = stmt.ColumnAttribute(iColumn + 1, (short)v3FieldId, Buffer, out cchNameLength, out numericAttribute);
            }
            else if (v2FieldId != (ODBC32.SQL_COLUMN)(-1))
            {
                retcode = stmt.ColumnAttribute(iColumn + 1, (short)v2FieldId, Buffer, out cchNameLength, out numericAttribute);
            }
            else
            {
                return 0;
            }
            if (retcode != ODBC32.RetCode.SUCCESS)
            {
                if (retcode == ODBC32.RetCode.ERROR)
                {
                    if ("HY091" == Command.GetDiagSqlState())
                    {
                        Connection.FlagUnsupportedColAttr(v3FieldId, v2FieldId);
                    }
                }
                if (handler == ODBC32.HANDLER.THROW)
                {
                    Connection.HandleError(stmt, retcode);
                }
                return -1;
            }
            return numericAttribute;
        }

        // GetColAttributeStr
        // ---------------
        // [IN] iColumn   ColumnNumber
        // [IN] v3FieldId FieldIdentifier of the attribute for version3 drivers (>=3.0)
        // [IN] v2FieldId FieldIdentifier of the attribute for version2 drivers (<3.0)
        //
        // returns the stringvalue of the FieldIdentifier field of the column
        // or null if the string returned was empty or if the FieldIdentifier wasn't supported by the driver
        //
        private string GetColAttributeStr(int i, ODBC32.SQL_DESC v3FieldId, ODBC32.SQL_COLUMN v2FieldId, ODBC32.HANDLER handler)
        {
            ODBC32.RetCode retcode;
            short cchNameLength = 0;
            SQLLEN numericAttribute;
            CNativeBuffer buffer = Buffer;
            buffer.WriteInt16(0, 0);

            OdbcStatementHandle stmt = StatementHandle;

            // protect against dead connection
            if (Connection == null || _cmdWrapper.Canceling || stmt == null)
            {
                return "";
            }

            if (Connection.IsV3Driver)
            {
                retcode = stmt.ColumnAttribute(i + 1, (short)v3FieldId, buffer, out cchNameLength, out numericAttribute);
            }
            else if (v2FieldId != (ODBC32.SQL_COLUMN)(-1))
            {
                retcode = stmt.ColumnAttribute(i + 1, (short)v2FieldId, buffer, out cchNameLength, out numericAttribute);
            }
            else
            {
                return null;
            }
            if ((retcode != ODBC32.RetCode.SUCCESS) || (cchNameLength == 0))
            {
                if (retcode == ODBC32.RetCode.ERROR)
                {
                    if ("HY091" == Command.GetDiagSqlState())
                    {
                        Connection.FlagUnsupportedColAttr(v3FieldId, v2FieldId);
                    }
                }
                if (handler == ODBC32.HANDLER.THROW)
                {
                    Connection.HandleError(stmt, retcode);
                }
                return null;
            }
            string retval = buffer.PtrToStringUni(0, cchNameLength / 2 /*cch*/);
            return retval;
        }

        // todo: Another 3.0 only attribute that is guaranteed to fail on V2 driver.
        // need to special case this for V2 drivers.
        //
        private string GetDescFieldStr(int i, ODBC32.SQL_DESC attribute, ODBC32.HANDLER handler)
        {
            int numericAttribute = 0;

            // protect against dead connection, dead or canceling command.
            if ((Connection == null) || _cmdWrapper.Canceling)
            {
                return "";
            }

            // APP_PARAM_DESC is a (ODBCVER >= 0x0300) attribute
            if (!Connection.IsV3Driver)
            {
                Debug.Fail("Non-V3 driver. Must not call GetDescFieldStr");
                return null;
            }

            ODBC32.RetCode retcode;
            CNativeBuffer buffer = Buffer;

            // Need to set the APP_PARAM_DESC values here
            using (OdbcDescriptorHandle hdesc = new OdbcDescriptorHandle(StatementHandle, ODBC32.SQL_ATTR.APP_PARAM_DESC))
            {
                //SQLGetDescField
                retcode = hdesc.GetDescriptionField(i + 1, attribute, buffer, out numericAttribute);

                //Since there are many attributes (column, statement, etc), that may or may not be
                //supported, we don't want to throw (which obtains all errorinfo, marshals strings,
                //builds exceptions, etc), in common cases, unless we absolutely need this info...
                if ((retcode != ODBC32.RetCode.SUCCESS) || (numericAttribute == 0))
                {
                    if (retcode == ODBC32.RetCode.ERROR)
                    {
                        if ("HY091" == Command.GetDiagSqlState())
                        {
                            Connection.FlagUnsupportedColAttr(attribute, (ODBC32.SQL_COLUMN)0);
                        }
                    }
                    if (handler == ODBC32.HANDLER.THROW)
                    {
                        Connection.HandleError(StatementHandle, retcode);
                    }
                    return null;
                }
            }
            string retval = buffer.PtrToStringUni(0, numericAttribute / 2 /*cch*/);
            return retval;
        }

        /// <summary>
        /// This methods queries the following field information: isDbNull and remained size/indicator. No data is read from the driver.
        /// If the value is DbNull, this value will be cached. Refer to GetData for more details.
        /// </summary>
        /// <returns>false if value is DbNull, true otherwise</returns>
        private bool QueryFieldInfo(int i, ODBC32.SQL_C sqlctype, out int cbLengthOrIndicator)
        {
            int cb = 0;
            if (sqlctype == ODBC32.SQL_C.WCHAR)
            {
                // SQLBU 266054 - in case of WCHAR data, we need to provide buffer with a space for null terminator (two bytes)
                cb = 2;
            }
            return GetData(i, sqlctype, cb /* no data should be lost */, out cbLengthOrIndicator);
        }

        private bool GetData(int i, ODBC32.SQL_C sqlctype)
        {
            // Never call GetData with anything larger than _buffer.Length-2.
            // We keep reallocating native buffers and it kills performance!!!
            int dummy;
            return GetData(i, sqlctype, Buffer.Length - 4, out dummy);
        }

        /// <summary>
        /// Note: use only this method to call SQLGetData! It caches the null value so the fact that the value is null is kept and no other calls
        /// are made after it.
        /// 
        /// retrieves the data into this.Buffer. 
        /// * If the data is DbNull, the value be also cached and false is returned.
        /// * if the data is not DbNull, the value is not cached and true is returned
        /// 
        /// Note: cbLengthOrIndicator can be either the length of (remained) data or SQL_NO_TOTAL (-4) when the length is not known.
        /// in case of SQL_NO_TOTAL, driver fills the buffer till the end. 
        /// The indicator will NOT be SQL_NULL_DATA, GetData will replace it with zero and return false.
        /// </summary>
        /// <returns>false if value is DbNull, true otherwise</returns>
        private bool GetData(int i, ODBC32.SQL_C sqlctype, int cb, out int cbLengthOrIndicator)
        {
            IntPtr cbActual = IntPtr.Zero;  // Length or an indicator value

            if (IsCancelingCommand)
            {
                throw ADP.DataReaderNoData();
            }
            Debug.Assert(null != StatementHandle, "Statement handle is null in DateReader");

            // see notes on ODBC32.RetCode.NO_DATA case below.
            Debug.Assert(_dataCache == null || !Convert.IsDBNull(_dataCache[i]), "Cannot call GetData without checking for cache first!");

            // Never call GetData with anything larger than _buffer.Length-2.
            // We keep reallocating native buffers and it kills performance!!!

            Debug.Assert(cb <= Buffer.Length - 2, "GetData needs to Reallocate. Perf bug");

            // SQLGetData
            CNativeBuffer buffer = Buffer;
            ODBC32.RetCode retcode = StatementHandle.GetData(
               (i + 1),    // Column ordinals start at 1 in odbc
               sqlctype,
               buffer,
               cb,
               out cbActual);

            switch (retcode)
            {
                case ODBC32.RetCode.SUCCESS:
                    break;
                case ODBC32.RetCode.SUCCESS_WITH_INFO:
                    if ((int)cbActual == ODBC32.SQL_NO_TOTAL)
                    {
                        break;
                    }
                    // devnote: don't we want to fire an event?
                    break;

                case ODBC32.RetCode.NO_DATA:
                    // SQLBU 266054: System.Data.Odbc: Fails with truncated error when we pass BufferLength  as 0
                    // NO_DATA return value is success value - it means that the driver has fully consumed the current column value
                    // but did not move to the next column yet.
                    // For fixed-size values, we do not expect this to happen because we fully consume the data and store it in cache after the first call.
                    // For variable-length values (any character or binary data), SQLGetData can be called several times on the same column, 
                    // to query for the next chunk of value, even after reaching its end!
                    // Thus, ignore NO_DATA for variable length data, but raise exception for fixed-size types
                    if (sqlctype != ODBC32.SQL_C.WCHAR && sqlctype != ODBC32.SQL_C.BINARY)
                    {
                        Connection.HandleError(StatementHandle, retcode);
                    }

                    if (cbActual == (IntPtr)ODBC32.SQL_NO_TOTAL)
                    {
                        // ensure SQL_NO_TOTAL value gets replaced with zero if the driver has fully consumed the current column
                        cbActual = (IntPtr)0;
                    }
                    break;

                default:
                    Connection.HandleError(StatementHandle, retcode);
                    break;
            }

            // reset the current row and column
            SetCurrentRowColumnInfo(_row, i);

            // test for SQL_NULL_DATA
            if (cbActual == (IntPtr)ODBC32.SQL_NULL_DATA)
            {
                // Store the DBNull value in cache. Note that if we need to do it, because second call into the SQLGetData returns NO_DATA, which means
                // we already consumed the value (see above) and the NULL information is lost. By storing the null in cache, we avoid second call into the driver
                // for the same row/column.
                _dataCache[i] = DBNull.Value;
                // the indicator is never -1 (and it should not actually be used if the data is DBNull)
                cbLengthOrIndicator = 0;
                return false;
            }
            else
            {
                //Return the actual size (for chunking scenarios)
                // note the return value can be SQL_NO_TOTAL (-4)
                cbLengthOrIndicator = (int)cbActual;
                return true;
            }
        }

        public override bool Read()
        {
            if (IsClosed)
            {
                throw ADP.DataReaderClosed("Read");
            }

            if (IsCancelingCommand)
            {
                _isRead = false;
                return false;
            }

            // HasRows needs to call into Read so we don't want to read on the actual Read call
            if (_skipReadOnce)
            {
                _skipReadOnce = false;
                return _isRead;
            }

            if (_noMoreRows || _noMoreResults || IsCommandBehavior(CommandBehavior.SchemaOnly))
                return false;

            if (!_isValidResult)
            {
                return false;
            }

            ODBC32.RetCode retcode;

            //SQLFetch is only valid to call for row returning queries
            //We get: [24000]Invalid cursor state.  So we could either check the count
            //ahead of time (which is cached), or check on error and compare error states.
            //Note: SQLFetch is also invalid to be called on a prepared (schemaonly) statement
            //SqlFetch
            retcode = StatementHandle.Fetch();

            switch (retcode)
            {
                case ODBC32.RetCode.SUCCESS_WITH_INFO:
                    Connection.HandleErrorNoThrow(StatementHandle, retcode);
                    _hasRows = HasRowsStatus.HasRows;
                    _isRead = true;
                    break;
                case ODBC32.RetCode.SUCCESS:
                    _hasRows = HasRowsStatus.HasRows;
                    _isRead = true;
                    break;
                case ODBC32.RetCode.NO_DATA:
                    _isRead = false;
                    if (_hasRows == HasRowsStatus.DontKnow)
                    {
                        _hasRows = HasRowsStatus.HasNoRows;
                    }
                    break;
                default:
                    Connection.HandleError(StatementHandle, retcode);
                    break;
            }
            //Null out previous cached row values.
            _dataCache.FlushValues();

            // if CommandBehavior == SingleRow we set _noMoreResults to true so that following reads will fail
            if (IsCommandBehavior(CommandBehavior.SingleRow))
            {
                _noMoreRows = true;
                // no more rows, set to -1
                SetCurrentRowColumnInfo(-1, 0);
            }
            else
            {
                // move to the next row
                SetCurrentRowColumnInfo(_row + 1, 0);
            }
            return _isRead;
        }

        // Called by odbccommand when executed for the first time
        internal void FirstResult()
        {
            short cCols;
            SQLLEN cRowsAffected;

            cRowsAffected = GetRowCount();              // get rowcount of the current resultset (if any)
            CalculateRecordsAffected(cRowsAffected);    // update recordsaffected

            ODBC32.RetCode retcode = FieldCountNoThrow(out cCols);
            if ((retcode == ODBC32.RetCode.SUCCESS) && (cCols == 0))
            {
                NextResult();
            }
            else
            {
                _isValidResult = true;
            }
        }

        public override bool NextResult()
        {
            return NextResult(false, false);
        }

        private bool NextResult(bool disposing, bool allresults)
        {
            // if disposing, loop through all the remaining results and ignore error messages
            // if allresults, loop through all results and collect all error messages for a single exception
            // callers are via Close(false, true), Dispose(true, false), NextResult(false,false)
            Debug.Assert(!disposing || !allresults, "both disposing & allresults are true");
            const int MaxConsecutiveFailure = 2000; // see WebData 72126 for why more than 1000

            SQLLEN cRowsAffected;
            short cColsAffected;
            ODBC32.RetCode retcode, firstRetCode = ODBC32.RetCode.SUCCESS;
            bool hasMoreResults;
            bool hasColumns = false;
            bool singleResult = IsCommandBehavior(CommandBehavior.SingleResult);

            if (IsClosed)
            {
                throw ADP.DataReaderClosed("NextResult");
            }
            _fieldNameLookup = null;

            if (IsCancelingCommand || _noMoreResults)
            {
                return false;
            }

            //Blow away the previous cache (since the next result set will have a different shape,
            //different schema data, and different data.
            _isRead = false;
            _hasRows = HasRowsStatus.DontKnow;
            _fieldNameLookup = null;
            _metadata = null;
            _schemaTable = null;

            int loop = 0; // infinite loop protection, max out after 2000 consecutive failed results
            OdbcErrorCollection errors = null; // SQLBU 342112
            do
            {
                _isValidResult = false;
                retcode = StatementHandle.MoreResults();
                hasMoreResults = ((retcode == ODBC32.RetCode.SUCCESS)
                                || (retcode == ODBC32.RetCode.SUCCESS_WITH_INFO));

                if (retcode == ODBC32.RetCode.SUCCESS_WITH_INFO)
                {
                    Connection.HandleErrorNoThrow(StatementHandle, retcode);
                }
                else if (!disposing && (retcode != ODBC32.RetCode.NO_DATA) && (ODBC32.RetCode.SUCCESS != retcode))
                {
                    // allow for building comulative error messages.
                    if (null == errors)
                    {
                        firstRetCode = retcode;
                        errors = new OdbcErrorCollection();
                    }
                    ODBC32.GetDiagErrors(errors, null, StatementHandle, retcode);
                    ++loop;
                }

                if (!disposing && hasMoreResults)
                {
                    loop = 0;
                    cRowsAffected = GetRowCount();              // get rowcount of the current resultset (if any)
                    CalculateRecordsAffected(cRowsAffected);    // update recordsaffected
                    if (!singleResult)
                    {
                        // update row- and columncount
                        FieldCountNoThrow(out cColsAffected);
                        hasColumns = (0 != cColsAffected);
                        _isValidResult = hasColumns;
                    }
                }
            } while ((!singleResult && hasMoreResults && !hasColumns)  // repeat for results with no columns
                     || ((ODBC32.RetCode.NO_DATA != retcode) && allresults && (loop < MaxConsecutiveFailure)) // or process all results until done
                     || (singleResult && hasMoreResults));           // or for any result in singelResult mode

            if (retcode == ODBC32.RetCode.NO_DATA)
            {
                _dataCache = null;
                _noMoreResults = true;
            }
            if (null != errors)
            {
                Debug.Assert(!disposing, "errors while disposing");
                errors.SetSource(Connection.Driver);
                OdbcException exception = OdbcException.CreateException(errors, firstRetCode);
                Connection.ConnectionIsAlive(exception);
                throw exception;
            }
            return (hasMoreResults);
        }

        private void BuildMetaDataInfo()
        {
            int count = FieldCount;
            MetaData[] metaInfos = new MetaData[count];
            List<string> qrytables;
            bool needkeyinfo = IsCommandBehavior(CommandBehavior.KeyInfo);
            bool isKeyColumn;
            bool isHidden;
            ODBC32.SQL_NULLABILITY nullable;

            if (needkeyinfo)
                qrytables = new List<string>();
            else
                qrytables = null;

            // Find out all the metadata info, not all of this info will be available in all cases
            //
            for (int i = 0; i < count; i++)
            {
                metaInfos[i] = new MetaData();
                metaInfos[i].ordinal = i;
                TypeMap typeMap;

                // for precision and scale we take the SQL_COLUMN_ attributes.
                // Those attributes are supported by all provider versions.
                // for size we use the octet length. We can't use column length because there is an incompatibility with the jet driver.
                // furthermore size needs to be special cased for wchar types
                //
                typeMap = TypeMap.FromSqlType((ODBC32.SQL_TYPE)unchecked((int)GetColAttribute(i, ODBC32.SQL_DESC.CONCISE_TYPE, ODBC32.SQL_COLUMN.TYPE, ODBC32.HANDLER.THROW)));
                if (typeMap._signType == true)
                {
                    bool sign = (GetColAttribute(i, ODBC32.SQL_DESC.UNSIGNED, ODBC32.SQL_COLUMN.UNSIGNED, ODBC32.HANDLER.THROW).ToInt64() != 0);
                    // sign = true if the column is unsigned
                    typeMap = TypeMap.UpgradeSignedType(typeMap, sign);
                }

                metaInfos[i].typemap = typeMap;
                metaInfos[i].size = GetColAttribute(i, ODBC32.SQL_DESC.OCTET_LENGTH, ODBC32.SQL_COLUMN.LENGTH, ODBC32.HANDLER.IGNORE);

                // special case the 'n' types
                //
                switch (metaInfos[i].typemap._sql_type)
                {
                    case ODBC32.SQL_TYPE.WCHAR:
                    case ODBC32.SQL_TYPE.WLONGVARCHAR:
                    case ODBC32.SQL_TYPE.WVARCHAR:
                        metaInfos[i].size /= 2;
                        break;
                }

                metaInfos[i].precision = (byte)GetColAttribute(i, (ODBC32.SQL_DESC)ODBC32.SQL_COLUMN.PRECISION, ODBC32.SQL_COLUMN.PRECISION, ODBC32.HANDLER.IGNORE);
                metaInfos[i].scale = (byte)GetColAttribute(i, (ODBC32.SQL_DESC)ODBC32.SQL_COLUMN.SCALE, ODBC32.SQL_COLUMN.SCALE, ODBC32.HANDLER.IGNORE);

                metaInfos[i].isAutoIncrement = GetColAttribute(i, ODBC32.SQL_DESC.AUTO_UNIQUE_VALUE, ODBC32.SQL_COLUMN.AUTO_INCREMENT, ODBC32.HANDLER.IGNORE) == 1;
                metaInfos[i].isReadOnly = (GetColAttribute(i, ODBC32.SQL_DESC.UPDATABLE, ODBC32.SQL_COLUMN.UPDATABLE, ODBC32.HANDLER.IGNORE) == (int)ODBC32.SQL_UPDATABLE.READONLY);

                nullable = (ODBC32.SQL_NULLABILITY)(int)GetColAttribute(i, ODBC32.SQL_DESC.NULLABLE, ODBC32.SQL_COLUMN.NULLABLE, ODBC32.HANDLER.IGNORE);
                metaInfos[i].isNullable = (nullable == ODBC32.SQL_NULLABILITY.NULLABLE);

                switch (metaInfos[i].typemap._sql_type)
                {
                    case ODBC32.SQL_TYPE.LONGVARCHAR:
                    case ODBC32.SQL_TYPE.WLONGVARCHAR:
                    case ODBC32.SQL_TYPE.LONGVARBINARY:
                        metaInfos[i].isLong = true;
                        break;
                    default:
                        metaInfos[i].isLong = false;
                        break;
                }

                if (IsCommandBehavior(CommandBehavior.KeyInfo))
                {
                    // Note: Following two attributes are SQL Server specific (hence _SS in the name)

                    // SSS_WARNINGS_OFF
                    if (!Connection.ProviderInfo.NoSqlCASSColumnKey)
                    {
                        isKeyColumn = GetColAttribute(i, (ODBC32.SQL_DESC)ODBC32.SQL_CA_SS.COLUMN_KEY, (ODBC32.SQL_COLUMN)(-1), ODBC32.HANDLER.IGNORE) == 1;
                        if (isKeyColumn)
                        {
                            metaInfos[i].isKeyColumn = isKeyColumn;
                            metaInfos[i].isUnique = true;
                            needkeyinfo = false;
                        }
                    }
                    // SSS_WARNINGS_ON

                    metaInfos[i].baseSchemaName = GetColAttributeStr(i, ODBC32.SQL_DESC.SCHEMA_NAME, ODBC32.SQL_COLUMN.OWNER_NAME, ODBC32.HANDLER.IGNORE);
                    metaInfos[i].baseCatalogName = GetColAttributeStr(i, ODBC32.SQL_DESC.CATALOG_NAME, (ODBC32.SQL_COLUMN)(-1), ODBC32.HANDLER.IGNORE);
                    metaInfos[i].baseTableName = GetColAttributeStr(i, ODBC32.SQL_DESC.BASE_TABLE_NAME, ODBC32.SQL_COLUMN.TABLE_NAME, ODBC32.HANDLER.IGNORE);
                    metaInfos[i].baseColumnName = GetColAttributeStr(i, ODBC32.SQL_DESC.BASE_COLUMN_NAME, ODBC32.SQL_COLUMN.NAME, ODBC32.HANDLER.IGNORE);

                    if (Connection.IsV3Driver)
                    {
                        if ((metaInfos[i].baseTableName == null) || (metaInfos[i].baseTableName.Length == 0))
                        {
                            // Driver didn't return the necessary information from GetColAttributeStr.
                            // Try GetDescField()
                            metaInfos[i].baseTableName = GetDescFieldStr(i, ODBC32.SQL_DESC.BASE_TABLE_NAME, ODBC32.HANDLER.IGNORE);
                        }
                        if ((metaInfos[i].baseColumnName == null) || (metaInfos[i].baseColumnName.Length == 0))
                        {
                            // Driver didn't return the necessary information from GetColAttributeStr.
                            // Try GetDescField()
                            metaInfos[i].baseColumnName = GetDescFieldStr(i, ODBC32.SQL_DESC.BASE_COLUMN_NAME, ODBC32.HANDLER.IGNORE);
                        }
                    }
                    if ((metaInfos[i].baseTableName != null) && !(qrytables.Contains(metaInfos[i].baseTableName)))
                    {
                        qrytables.Add(metaInfos[i].baseTableName);
                    }
                }

                // If primary key or autoincrement, then must also be unique
                if (metaInfos[i].isKeyColumn || metaInfos[i].isAutoIncrement)
                {
                    if (nullable == ODBC32.SQL_NULLABILITY.UNKNOWN)
                        metaInfos[i].isNullable = false;    // We can safely assume these are not nullable
                }
            }

            // now loop over the hidden columns (if any)

            // SSS_WARNINGS_OFF
            if (!Connection.ProviderInfo.NoSqlCASSColumnKey)
            {
                for (int i = count; i < count + _hiddenColumns; i++)
                {
                    isKeyColumn = GetColAttribute(i, (ODBC32.SQL_DESC)ODBC32.SQL_CA_SS.COLUMN_KEY, (ODBC32.SQL_COLUMN)(-1), ODBC32.HANDLER.IGNORE) == 1;
                    if (isKeyColumn)
                    {
                        isHidden = GetColAttribute(i, (ODBC32.SQL_DESC)ODBC32.SQL_CA_SS.COLUMN_HIDDEN, (ODBC32.SQL_COLUMN)(-1), ODBC32.HANDLER.IGNORE) == 1;
                        if (isHidden)
                        {
                            for (int j = 0; j < count; j++)
                            {
                                metaInfos[j].isKeyColumn = false;   // downgrade keycolumn
                                metaInfos[j].isUnique = false;      // downgrade uniquecolumn
                            }
                        }
                    }
                }
            }
            // SSS_WARNINGS_ON

            // Blow away the previous metadata
            _metadata = metaInfos;

            // If key info is requested, then we have to make a few more calls to get the
            //  special columns. This may not succeed for all drivers, so ignore errors and
            // fill in as much as possible.
            if (IsCommandBehavior(CommandBehavior.KeyInfo))
            {
                if ((qrytables != null) && (qrytables.Count > 0))
                {
                    List<string>.Enumerator tablesEnum = qrytables.GetEnumerator();
                    QualifiedTableName qualifiedTableName = new QualifiedTableName(Connection.QuoteChar(ADP.GetSchemaTable));
                    while (tablesEnum.MoveNext())
                    {
                        // Find the primary keys, identity and autincrement columns
                        qualifiedTableName.Table = tablesEnum.Current;
                        if (RetrieveKeyInfo(needkeyinfo, qualifiedTableName, false) <= 0)
                        {
                            RetrieveKeyInfo(needkeyinfo, qualifiedTableName, true);
                        }
                    }
                }
                else
                {
                    // Some drivers ( < 3.x ?) do not provide base table information. In this case try to
                    // find it by parsing the statement

                    QualifiedTableName qualifiedTableName = new QualifiedTableName(Connection.QuoteChar(ADP.GetSchemaTable), GetTableNameFromCommandText());
                    if (!string.IsNullOrEmpty(qualifiedTableName.Table))
                    { // fxcop
                        SetBaseTableNames(qualifiedTableName);
                        if (RetrieveKeyInfo(needkeyinfo, qualifiedTableName, false) <= 0)
                        {
                            RetrieveKeyInfo(needkeyinfo, qualifiedTableName, true);
                        }
                    }
                }
            }
        }

        private DataTable NewSchemaTable()
        {
            DataTable schematable = new DataTable("SchemaTable");
            schematable.Locale = CultureInfo.InvariantCulture;
            schematable.MinimumCapacity = this.FieldCount;

            //Schema Columns
            DataColumnCollection columns = schematable.Columns;
            columns.Add(new DataColumn("ColumnName", typeof(string)));
            columns.Add(new DataColumn("ColumnOrdinal", typeof(int))); // UInt32
            columns.Add(new DataColumn("ColumnSize", typeof(int))); // UInt32
            columns.Add(new DataColumn("NumericPrecision", typeof(short))); // UInt16
            columns.Add(new DataColumn("NumericScale", typeof(short)));
            columns.Add(new DataColumn("DataType", typeof(object)));
            columns.Add(new DataColumn("ProviderType", typeof(int)));
            columns.Add(new DataColumn("IsLong", typeof(bool)));
            columns.Add(new DataColumn("AllowDBNull", typeof(bool)));
            columns.Add(new DataColumn("IsReadOnly", typeof(bool)));
            columns.Add(new DataColumn("IsRowVersion", typeof(bool)));
            columns.Add(new DataColumn("IsUnique", typeof(bool)));
            columns.Add(new DataColumn("IsKey", typeof(bool)));
            columns.Add(new DataColumn("IsAutoIncrement", typeof(bool)));
            columns.Add(new DataColumn("BaseSchemaName", typeof(string)));
            columns.Add(new DataColumn("BaseCatalogName", typeof(string)));
            columns.Add(new DataColumn("BaseTableName", typeof(string)));
            columns.Add(new DataColumn("BaseColumnName", typeof(string)));

            // MDAC Bug 79231
            foreach (DataColumn column in columns)
            {
                column.ReadOnly = true;
            }
            return schematable;
        }

        // The default values are already defined in DbSchemaRows (see DbSchemaRows.cs) so there is no need to set any default value
        //

        public override DataTable GetSchemaTable()
        {
            if (IsClosed)
            { // MDAC 68331
                throw ADP.DataReaderClosed("GetSchemaTable");           // can't use closed connection
            }
            if (_noMoreResults)
            {
                return null;                                            // no more results
            }
            if (null != _schemaTable)
            {
                return _schemaTable;                                // return cached schematable
            }

            //Delegate, to have the base class setup the structure
            DataTable schematable = NewSchemaTable();

            if (FieldCount == 0)
            {
                return schematable;
            }
            if (_metadata == null)
            {
                BuildMetaDataInfo();
            }

            DataColumn columnName = schematable.Columns["ColumnName"];
            DataColumn columnOrdinal = schematable.Columns["ColumnOrdinal"];
            DataColumn columnSize = schematable.Columns["ColumnSize"];
            DataColumn numericPrecision = schematable.Columns["NumericPrecision"];
            DataColumn numericScale = schematable.Columns["NumericScale"];
            DataColumn dataType = schematable.Columns["DataType"];
            DataColumn providerType = schematable.Columns["ProviderType"];
            DataColumn isLong = schematable.Columns["IsLong"];
            DataColumn allowDBNull = schematable.Columns["AllowDBNull"];
            DataColumn isReadOnly = schematable.Columns["IsReadOnly"];
            DataColumn isRowVersion = schematable.Columns["IsRowVersion"];
            DataColumn isUnique = schematable.Columns["IsUnique"];
            DataColumn isKey = schematable.Columns["IsKey"];
            DataColumn isAutoIncrement = schematable.Columns["IsAutoIncrement"];
            DataColumn baseSchemaName = schematable.Columns["BaseSchemaName"];
            DataColumn baseCatalogName = schematable.Columns["BaseCatalogName"];
            DataColumn baseTableName = schematable.Columns["BaseTableName"];
            DataColumn baseColumnName = schematable.Columns["BaseColumnName"];


            //Populate the rows (1 row for each column)
            int count = FieldCount;
            for (int i = 0; i < count; i++)
            {
                DataRow row = schematable.NewRow();

                row[columnName] = GetName(i);        //ColumnName
                row[columnOrdinal] = i;                 //ColumnOrdinal
                row[columnSize] = unchecked((int)Math.Min(Math.Max(int.MinValue, _metadata[i].size.ToInt64()), int.MaxValue));
                row[numericPrecision] = (short)_metadata[i].precision;
                row[numericScale] = (short)_metadata[i].scale;
                row[dataType] = _metadata[i].typemap._type;          //DataType
                row[providerType] = _metadata[i].typemap._odbcType;          // ProviderType
                row[isLong] = _metadata[i].isLong;           // IsLong
                row[allowDBNull] = _metadata[i].isNullable;       //AllowDBNull
                row[isReadOnly] = _metadata[i].isReadOnly;      // IsReadOnly
                row[isRowVersion] = _metadata[i].isRowVersion;    //IsRowVersion
                row[isUnique] = _metadata[i].isUnique;        //IsUnique
                row[isKey] = _metadata[i].isKeyColumn;    // IsKey
                row[isAutoIncrement] = _metadata[i].isAutoIncrement; //IsAutoIncrement

                //BaseSchemaName
                row[baseSchemaName] = _metadata[i].baseSchemaName;
                //BaseCatalogName
                row[baseCatalogName] = _metadata[i].baseCatalogName;
                //BaseTableName
                row[baseTableName] = _metadata[i].baseTableName;
                //BaseColumnName
                row[baseColumnName] = _metadata[i].baseColumnName;

                schematable.Rows.Add(row);
                row.AcceptChanges();
            }
            _schemaTable = schematable;
            return schematable;
        }

        internal int RetrieveKeyInfo(bool needkeyinfo, QualifiedTableName qualifiedTableName, bool quoted)
        {
            ODBC32.RetCode retcode;
            string columnname;
            int ordinal;
            int keyColumns = 0;
            IntPtr cbActual = IntPtr.Zero;

            if (IsClosed || (_cmdWrapper == null))
            {
                return 0;     // Can't do anything without a second handle
            }
            _cmdWrapper.CreateKeyInfoStatementHandle();

            CNativeBuffer buffer = Buffer;
            bool mustRelease = false;
            Debug.Assert(buffer.Length >= 264, "Native buffer to small (_buffer.Length < 264)");

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                buffer.DangerousAddRef(ref mustRelease);

                if (needkeyinfo)
                {
                    if (!Connection.ProviderInfo.NoSqlPrimaryKeys)
                    {
                        // Get the primary keys
                        retcode = KeyInfoStatementHandle.PrimaryKeys(
                                    qualifiedTableName.Catalog,
                                    qualifiedTableName.Schema,
                                    qualifiedTableName.GetTable(quoted));

                        if ((retcode == ODBC32.RetCode.SUCCESS) || (retcode == ODBC32.RetCode.SUCCESS_WITH_INFO))
                        {
                            bool noUniqueKey = false;

                            // We are only interested in column name
                            buffer.WriteInt16(0, 0);
                            retcode = KeyInfoStatementHandle.BindColumn2(
                                           (short)(ODBC32.SQL_PRIMARYKEYS.COLUMNNAME),    // Column Number
                                           ODBC32.SQL_C.WCHAR,
                                           buffer.PtrOffset(0, 256),
                                           (IntPtr)256,
                                           buffer.PtrOffset(256, IntPtr.Size).Handle);
                            while (ODBC32.RetCode.SUCCESS == (retcode = KeyInfoStatementHandle.Fetch()))
                            {
                                cbActual = buffer.ReadIntPtr(256);
                                columnname = buffer.PtrToStringUni(0, (int)cbActual / 2/*cch*/);
                                ordinal = this.GetOrdinalFromBaseColName(columnname);
                                if (ordinal != -1)
                                {
                                    keyColumns++;
                                    _metadata[ordinal].isKeyColumn = true;
                                    _metadata[ordinal].isUnique = true;
                                    _metadata[ordinal].isNullable = false;
                                    _metadata[ordinal].baseTableName = qualifiedTableName.Table;

                                    if (_metadata[ordinal].baseColumnName == null)
                                    {
                                        _metadata[ordinal].baseColumnName = columnname;
                                    }
                                }
                                else
                                {
                                    noUniqueKey = true;
                                    break;  // no need to go over the remaining columns anymore
                                }
                            }
                            // 




                            // if we got keyinfo from the column we dont even get to here!
                            //
                            // reset isUnique flag if the key(s) are not unique
                            //
                            if (noUniqueKey)
                            {
                                foreach (MetaData metadata in _metadata)
                                {
                                    metadata.isKeyColumn = false;
                                }
                            }

                            // Unbind the column
                            retcode = KeyInfoStatementHandle.BindColumn3(
                                (short)(ODBC32.SQL_PRIMARYKEYS.COLUMNNAME),      // SQLUSMALLINT ColumnNumber
                                ODBC32.SQL_C.WCHAR,                     // SQLSMALLINT  TargetType
                                buffer.DangerousGetHandle());                                   // SQLLEN *     StrLen_or_Ind
                        }
                        else
                        {
                            if ("IM001" == Command.GetDiagSqlState())
                            {
                                Connection.ProviderInfo.NoSqlPrimaryKeys = true;
                            }
                        }
                    }

                    if (keyColumns == 0)
                    {
                        // SQLPrimaryKeys did not work. Have to use the slower SQLStatistics to obtain key information
                        KeyInfoStatementHandle.MoreResults();
                        keyColumns += RetrieveKeyInfoFromStatistics(qualifiedTableName, quoted);
                    }
                    KeyInfoStatementHandle.MoreResults();
                }

                // Get the special columns for version
                retcode = KeyInfoStatementHandle.SpecialColumns(qualifiedTableName.GetTable(quoted));

                if ((retcode == ODBC32.RetCode.SUCCESS) || (retcode == ODBC32.RetCode.SUCCESS_WITH_INFO))
                {
                    // We are only interested in column name
                    cbActual = IntPtr.Zero;
                    buffer.WriteInt16(0, 0);
                    retcode = KeyInfoStatementHandle.BindColumn2(
                                   (short)(ODBC32.SQL_SPECIALCOLUMNSET.COLUMN_NAME),
                                   ODBC32.SQL_C.WCHAR,
                                   buffer.PtrOffset(0, 256),
                                   (IntPtr)256,
                                   buffer.PtrOffset(256, IntPtr.Size).Handle);

                    while (ODBC32.RetCode.SUCCESS == (retcode = KeyInfoStatementHandle.Fetch()))
                    {
                        cbActual = buffer.ReadIntPtr(256);
                        columnname = buffer.PtrToStringUni(0, (int)cbActual / 2/*cch*/);
                        ordinal = this.GetOrdinalFromBaseColName(columnname);
                        if (ordinal != -1)
                        {
                            _metadata[ordinal].isRowVersion = true;
                            if (_metadata[ordinal].baseColumnName == null)
                            {
                                _metadata[ordinal].baseColumnName = columnname;
                            }
                        }
                    }
                    // Unbind the column
                    retcode = KeyInfoStatementHandle.BindColumn3(
                                   (short)(ODBC32.SQL_SPECIALCOLUMNSET.COLUMN_NAME),
                                   ODBC32.SQL_C.WCHAR,
                                   buffer.DangerousGetHandle());

                    retcode = KeyInfoStatementHandle.MoreResults();
                }
                else
                {
                    //  i've seen "DIAG [HY000] [Microsoft][ODBC SQL Server Driver]Connection is busy with results for another hstmt (0) "
                    //  how did we get here? SqlServer does not allow a second handle (Keyinfostmt) anyway...
                    //
                    /*
                        string msg = "Unexpected failure of SQLSpecialColumns. Code = " + Command.GetDiagSqlState();
                        Debug.Assert (false, msg);
                    */
                }
            }
            finally
            {
                if (mustRelease)
                {
                    buffer.DangerousRelease();
                }
            }
            return keyColumns;
        }

        // Uses SQLStatistics to retrieve key information for a table
        private int RetrieveKeyInfoFromStatistics(QualifiedTableName qualifiedTableName, bool quoted)
        {
            ODBC32.RetCode retcode;
            string columnname = string.Empty;
            string indexname = string.Empty;
            string currentindexname = string.Empty;
            int[] indexcolumnordinals = new int[16];
            int[] pkcolumnordinals = new int[16];
            int npkcols = 0;
            int ncols = 0;                  // No of cols in the index
            bool partialcolumnset = false;
            int ordinal;
            int indexordinal;
            IntPtr cbIndexLen = IntPtr.Zero;
            IntPtr cbColnameLen = IntPtr.Zero;
            int keyColumns = 0;

            // devnote: this test is already done by calling method ...
            // if (IsClosed) return;   // protect against dead connection

            string tablename1 = qualifiedTableName.GetTable(quoted);

            // Select only unique indexes
            retcode = KeyInfoStatementHandle.Statistics(tablename1);

            if (retcode != ODBC32.RetCode.SUCCESS)
            {
                // We give up at this point
                return 0;
            }

            CNativeBuffer buffer = Buffer;
            bool mustRelease = false;
            Debug.Assert(buffer.Length >= 544, "Native buffer to small (_buffer.Length < 544)");

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                buffer.DangerousAddRef(ref mustRelease);

                const int colnameBufOffset = 0;
                const int indexBufOffset = 256;
                const int ordinalBufOffset = 512;
                const int colnameActualOffset = 520;
                const int indexActualOffset = 528;
                const int ordinalActualOffset = 536;
                HandleRef colnamebuf = buffer.PtrOffset(colnameBufOffset, 256);
                HandleRef indexbuf = buffer.PtrOffset(indexBufOffset, 256);
                HandleRef ordinalbuf = buffer.PtrOffset(ordinalBufOffset, 4);

                IntPtr colnameActual = buffer.PtrOffset(colnameActualOffset, IntPtr.Size).Handle;
                IntPtr indexActual = buffer.PtrOffset(indexActualOffset, IntPtr.Size).Handle;
                IntPtr ordinalActual = buffer.PtrOffset(ordinalActualOffset, IntPtr.Size).Handle;

                //We are interested in index name, column name, and ordinal
                buffer.WriteInt16(indexBufOffset, 0);
                retcode = KeyInfoStatementHandle.BindColumn2(
                            (short)(ODBC32.SQL_STATISTICS.INDEXNAME),
                            ODBC32.SQL_C.WCHAR,
                            indexbuf,
                            (IntPtr)256,
                            indexActual);
                retcode = KeyInfoStatementHandle.BindColumn2(
                            (short)(ODBC32.SQL_STATISTICS.ORDINAL_POSITION),
                            ODBC32.SQL_C.SSHORT,
                            ordinalbuf,
                            (IntPtr)4,
                            ordinalActual);
                buffer.WriteInt16(ordinalBufOffset, 0);
                retcode = KeyInfoStatementHandle.BindColumn2(
                            (short)(ODBC32.SQL_STATISTICS.COLUMN_NAME),
                            ODBC32.SQL_C.WCHAR,
                            colnamebuf,
                            (IntPtr)256,
                            colnameActual);
                // Find the best unique index on the table, use the ones whose columns are
                // completely covered by the query.
                while (ODBC32.RetCode.SUCCESS == (retcode = KeyInfoStatementHandle.Fetch()))
                {
                    cbColnameLen = buffer.ReadIntPtr(colnameActualOffset);
                    cbIndexLen = buffer.ReadIntPtr(indexActualOffset);

                    // If indexname is not returned, skip this row
                    if (0 == buffer.ReadInt16(indexBufOffset))
                        continue;       // Not an index row, get next row.

                    columnname = buffer.PtrToStringUni(colnameBufOffset, (int)cbColnameLen / 2/*cch*/);
                    indexname = buffer.PtrToStringUni(indexBufOffset, (int)cbIndexLen / 2/*cch*/);
                    ordinal = (int)buffer.ReadInt16(ordinalBufOffset);

                    if (SameIndexColumn(currentindexname, indexname, ordinal, ncols))
                    {
                        // We are still working on the same index
                        if (partialcolumnset)
                            continue;       // We don't have all the keys for this index, so we can't use it

                        ordinal = this.GetOrdinalFromBaseColName(columnname, qualifiedTableName.Table);
                        if (ordinal == -1)
                        {
                            partialcolumnset = true;
                        }
                        else
                        {
                            // Add the column to the index column set
                            if (ncols < 16)
                                indexcolumnordinals[ncols++] = ordinal;
                            else    // Can't deal with indexes > 16 columns
                                partialcolumnset = true;
                        }
                    }
                    else
                    {
                        // We got a new index, save the previous index information
                        if (!partialcolumnset && (ncols != 0))
                        {
                            // Choose the unique index with least columns as primary key
                            if ((npkcols == 0) || (npkcols > ncols))
                            {
                                npkcols = ncols;
                                for (int i = 0; i < ncols; i++)
                                    pkcolumnordinals[i] = indexcolumnordinals[i];
                            }
                        }
                        // Reset the parameters for a new index
                        ncols = 0;
                        currentindexname = indexname;
                        partialcolumnset = false;
                        // Add this column to index
                        ordinal = this.GetOrdinalFromBaseColName(columnname, qualifiedTableName.Table);
                        if (ordinal == -1)
                        {
                            partialcolumnset = true;
                        }
                        else
                        {
                            // Add the column to the index column set
                            indexcolumnordinals[ncols++] = ordinal;
                        }
                    }
                    // Go on to the next column
                }
                // Do we have an index?
                if (!partialcolumnset && (ncols != 0))
                {
                    // Choose the unique index with least columns as primary key
                    if ((npkcols == 0) || (npkcols > ncols))
                    {
                        npkcols = ncols;
                        for (int i = 0; i < ncols; i++)
                            pkcolumnordinals[i] = indexcolumnordinals[i];
                    }
                }
                // Mark the chosen index as primary key
                if (npkcols != 0)
                {
                    for (int i = 0; i < npkcols; i++)
                    {
                        indexordinal = pkcolumnordinals[i];
                        keyColumns++;
                        _metadata[indexordinal].isKeyColumn = true;
                        // should we set isNullable = false?
                        // This makes the QuikTest against Jet fail
                        //
                        // test test test - we don't know if this is nulalble or not so why do we want to set it to a value?
                        _metadata[indexordinal].isNullable = false;
                        _metadata[indexordinal].isUnique = true;
                        if (_metadata[indexordinal].baseTableName == null)
                        {
                            _metadata[indexordinal].baseTableName = qualifiedTableName.Table;
                        }
                        if (_metadata[indexordinal].baseColumnName == null)
                        {
                            _metadata[indexordinal].baseColumnName = columnname;
                        }
                    }
                }
                // Unbind the columns
                _cmdWrapper.FreeKeyInfoStatementHandle(ODBC32.STMT.UNBIND);
            }
            finally
            {
                if (mustRelease)
                {
                    buffer.DangerousRelease();
                }
            }
            return keyColumns;
        }

        internal bool SameIndexColumn(string currentindexname, string indexname, int ordinal, int ncols)
        {
            if (string.IsNullOrEmpty(currentindexname))
            {
                return false;
            }
            if ((currentindexname == indexname) &&
                (ordinal == ncols + 1))
                return true;
            return false;
        }

        internal int GetOrdinalFromBaseColName(string columnname)
        {
            return GetOrdinalFromBaseColName(columnname, null);
        }

        internal int GetOrdinalFromBaseColName(string columnname, string tablename)
        {
            if (string.IsNullOrEmpty(columnname))
            {
                return -1;
            }
            if (_metadata != null)
            {
                int count = FieldCount;
                for (int i = 0; i < count; i++)
                {
                    if ((_metadata[i].baseColumnName != null) &&
                        (columnname == _metadata[i].baseColumnName))
                    {
                        if (!string.IsNullOrEmpty(tablename))
                        {
                            if (tablename == _metadata[i].baseTableName)
                            {
                                return i;
                            } // end if
                        } // end if
                        else
                        {
                            return i;
                        } // end else
                    }
                }
            }
            // We can't find it in base column names, try regular colnames
            return this.IndexOf(columnname);
        }

        // We try parsing the SQL statement to get the table name as a last resort when
        // the driver doesn't return this information back to us.
        //
        // we can't handle multiple tablenames (JOIN)
        // only the first tablename will be returned

        internal string GetTableNameFromCommandText()
        {
            if (_command == null)
            {
                return null;
            }
            string localcmdtext = _cmdText;
            if (string.IsNullOrEmpty(localcmdtext))
            { // fxcop
                return null;
            }
            string tablename;
            int idx;
            CStringTokenizer tokenstmt = new CStringTokenizer(localcmdtext, Connection.QuoteChar(ADP.GetSchemaTable)[0], Connection.EscapeChar(ADP.GetSchemaTable));

            if (tokenstmt.StartsWith("select") == true)
            {
                // select command, search for from clause
                idx = tokenstmt.FindTokenIndex("from");
            }
            else
            {
                if ((tokenstmt.StartsWith("insert") == true) ||
                    (tokenstmt.StartsWith("update") == true) ||
                    (tokenstmt.StartsWith("delete") == true))
                {
                    // Get the following word
                    idx = tokenstmt.CurrentPosition;
                }
                else
                    idx = -1;
            }
            if (idx == -1)
                return null;
            // The next token is the table name
            tablename = tokenstmt.NextToken();

            localcmdtext = tokenstmt.NextToken();
            if ((localcmdtext.Length > 0) && (localcmdtext[0] == ','))
            {
                return null;        // can't handle multiple tables
            }
            if ((localcmdtext.Length == 2) &&
                ((localcmdtext[0] == 'a') || (localcmdtext[0] == 'A')) &&
                ((localcmdtext[1] == 's') || (localcmdtext[1] == 'S')))
            {
                // aliased table, skip the alias name
                localcmdtext = tokenstmt.NextToken();
                localcmdtext = tokenstmt.NextToken();
                if ((localcmdtext.Length > 0) && (localcmdtext[0] == ','))
                {
                    return null;        // Multiple tables
                }
            }
            return tablename;
        }

        internal void SetBaseTableNames(QualifiedTableName qualifiedTableName)
        {
            int count = FieldCount;

            for (int i = 0; i < count; i++)
            {
                if (_metadata[i].baseTableName == null)
                {
                    _metadata[i].baseTableName = qualifiedTableName.Table;
                    _metadata[i].baseSchemaName = qualifiedTableName.Schema;
                    _metadata[i].baseCatalogName = qualifiedTableName.Catalog;
                }
            }
            return;
        }

        internal sealed class QualifiedTableName
        {
            private string _catalogName;
            private string _schemaName;
            private string _tableName;
            private string _quotedTableName;
            private string _quoteChar;

            internal string Catalog
            {
                get
                {
                    return _catalogName;
                }
            }

            internal string Schema
            {
                get
                {
                    return _schemaName;
                }
            }

            internal string Table
            {
                get
                {
                    return _tableName;
                }
                set
                {
                    _quotedTableName = value;
                    _tableName = UnQuote(value);
                }
            }
            internal string QuotedTable
            {
                get
                {
                    return _quotedTableName;
                }
            }
            internal string GetTable(bool flag)
            {
                return (flag ? QuotedTable : Table);
            }
            internal QualifiedTableName(string quoteChar)
            {
                _quoteChar = quoteChar;
            }
            internal QualifiedTableName(string quoteChar, string qualifiedname)
            {
                _quoteChar = quoteChar;

                string[] names = ParseProcedureName(qualifiedname, quoteChar, quoteChar);
                _catalogName = UnQuote(names[1]);
                _schemaName = UnQuote(names[2]);
                _quotedTableName = names[3];
                _tableName = UnQuote(names[3]);
            }

            private string UnQuote(string str)
            {
                if ((str != null) && (str.Length > 0))
                {
                    char quoteChar = _quoteChar[0];
                    if (str[0] == quoteChar)
                    {
                        Debug.Assert(str.Length > 1, "Illegal string, only one char that is a quote");
                        Debug.Assert(str[str.Length - 1] == quoteChar, "Missing quote at end of string that begins with quote");
                        if (str.Length > 1 && str[str.Length - 1] == quoteChar)
                        {
                            str = str.Substring(1, str.Length - 2);
                        }
                    }
                }
                return str;
            }

            // Note: copy-and pasted from internal DbCommandBuilder implementation
            // Note: Per definition (ODBC reference) the CatalogSeparator comes before and after the
            // catalog name, the SchemaSeparator is undefined. Does it come between Schema and Table?
            internal static string[] ParseProcedureName(string name, string quotePrefix, string quoteSuffix)
            {
                // Procedure may consist of up to four parts:
                // 0) Server
                // 1) Catalog
                // 2) Schema
                // 3) ProcedureName
                //
                // Parse the string into four parts, allowing the last part to contain '.'s.
                // If less than four period delimited parts, use the parts from procedure backwards.
                //
                const string Separator = ".";

                string[] qualifiers = new string[4];
                if (!string.IsNullOrEmpty(name))
                {
                    bool useQuotes = !string.IsNullOrEmpty(quotePrefix) && !string.IsNullOrEmpty(quoteSuffix);

                    int currentPos = 0, parts;
                    for (parts = 0; (parts < qualifiers.Length) && (currentPos < name.Length); ++parts)
                    {
                        int startPos = currentPos;

                        // does the part begin with a quotePrefix?
                        if (useQuotes && (name.IndexOf(quotePrefix, currentPos, quotePrefix.Length, StringComparison.Ordinal) == currentPos))
                        {
                            currentPos += quotePrefix.Length; // move past the quotePrefix

                            // search for the quoteSuffix (or end of string)
                            while (currentPos < name.Length)
                            {
                                currentPos = name.IndexOf(quoteSuffix, currentPos, StringComparison.Ordinal);
                                if (currentPos < 0)
                                {
                                    // error condition, no quoteSuffix
                                    currentPos = name.Length;
                                    break;
                                }
                                else
                                {
                                    currentPos += quoteSuffix.Length; // move past the quoteSuffix

                                    // is this a double quoteSuffix?
                                    if ((currentPos < name.Length) && (name.IndexOf(quoteSuffix, currentPos, quoteSuffix.Length, StringComparison.Ordinal) == currentPos))
                                    {
                                        // a second quoteSuffix, continue search for terminating quoteSuffix
                                        currentPos += quoteSuffix.Length; // move past the second quoteSuffix
                                    }
                                    else
                                    {
                                        // found the terminating quoteSuffix
                                        break;
                                    }
                                }
                            }
                        }

                        // search for separator (either no quotePrefix or already past quoteSuffix)
                        if (currentPos < name.Length)
                        {
                            currentPos = name.IndexOf(Separator, currentPos, StringComparison.Ordinal);
                            if ((currentPos < 0) || (parts == qualifiers.Length - 1))
                            {
                                // last part that can be found
                                currentPos = name.Length;
                            }
                        }

                        qualifiers[parts] = name.Substring(startPos, currentPos - startPos);
                        currentPos += Separator.Length;
                    }

                    // allign the qualifiers if we had less than MaxQualifiers
                    for (int j = qualifiers.Length - 1; 0 <= j; --j)
                    {
                        qualifiers[j] = ((0 < parts) ? qualifiers[--parts] : null);
                    }
                }
                return qualifiers;
            }
        }

        private sealed class MetaData
        {
            internal int ordinal;
            internal TypeMap typemap;

            internal SQLLEN size;
            internal byte precision;
            internal byte scale;

            internal bool isAutoIncrement;
            internal bool isUnique;
            internal bool isReadOnly;
            internal bool isNullable;
            internal bool isRowVersion;
            internal bool isLong;

            internal bool isKeyColumn;
            internal string baseSchemaName;
            internal string baseCatalogName;
            internal string baseTableName;
            internal string baseColumnName;
        }
    }
}
