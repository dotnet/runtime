// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using SysTx = System.Transactions;

namespace System.Data.Odbc
{
    public sealed partial class OdbcConnection : DbConnection, ICloneable
    {
        private int _connectionTimeout = ADP.DefaultConnectionTimeout;

        private OdbcInfoMessageEventHandler? _infoMessageEventHandler;
        private WeakReference? _weakTransaction;

        private OdbcConnectionHandle? _connectionHandle;

        public OdbcConnection(string? connectionString) : this()
        {
            ConnectionString = connectionString;
        }

        private OdbcConnection(OdbcConnection connection) : this()
        { // Clone
            CopyFrom(connection);
            _connectionTimeout = connection._connectionTimeout;
        }

        internal OdbcConnectionHandle? ConnectionHandle
        {
            get
            {
                return _connectionHandle;
            }
            set
            {
                Debug.Assert(null == _connectionHandle, "reopening a connection?");
                _connectionHandle = value;
            }
        }

        [AllowNull]
        [Editor("Microsoft.VSDesigner.Data.Odbc.Design.OdbcConnectionStringEditor, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public override string ConnectionString
        {
            get
            {
                return ConnectionString_Get();
            }
            set
            {
                ConnectionString_Set(value);
            }
        }

        [
        DefaultValue(ADP.DefaultConnectionTimeout),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ]
        public new int ConnectionTimeout
        {
            get
            {
                return _connectionTimeout;
            }
            set
            {
                if (value < 0)
                    throw ODBC.NegativeArgument();
                if (IsOpen)
                    throw ODBC.CantSetPropertyOnOpenConnection();
                _connectionTimeout = value;
            }
        }

        [
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ]
        public override string Database
        {
            get
            {
                if (IsOpen && !ProviderInfo.NoCurrentCatalog)
                {
                    //Note: CURRENT_CATALOG may not be supported by the current driver.  In which
                    //case we ignore any error (without throwing), and just return string.empty.
                    //As we really don't want people to have to have try/catch around simple properties
                    return GetConnectAttrString(ODBC32.SQL_ATTR.CURRENT_CATALOG);
                }
                //Database is not available before open, and its not worth parsing the
                //connection string over.
                return string.Empty;
            }
        }

        [
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ]
        public override string DataSource
        {
            get
            {
                if (IsOpen)
                {
                    // note: This will return an empty string if the driver keyword was used to connect
                    // see ODBC3.0 Programmers Reference, SQLGetInfo
                    //
                    return GetInfoStringUnhandled(ODBC32.SQL_INFO.SERVER_NAME, true)!;
                }
                return string.Empty;
            }
        }

        [
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ]
        public override string ServerVersion
        {
            get
            {
                return InnerConnection.ServerVersion;
            }
        }

        [
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ]
        public override ConnectionState State
        {
            get
            {
                return InnerConnection.State;
            }
        }

        internal OdbcConnectionPoolGroupProviderInfo ProviderInfo
        {
            get
            {
                Debug.Assert(null != this.PoolGroup, "PoolGroup must never be null when accessing ProviderInfo");
                return (OdbcConnectionPoolGroupProviderInfo)this.PoolGroup.ProviderInfo!;
            }
        }

        internal ConnectionState InternalState
        {
            get
            {
                return this.State;
            }
        }

        internal bool IsOpen
        {
            get
            {
                return (InnerConnection is OdbcConnectionOpen);
            }
        }

        internal OdbcTransaction? LocalTransaction
        {
            get
            {
                OdbcTransaction? result = null;
                if (null != _weakTransaction)
                {
                    result = ((OdbcTransaction?)_weakTransaction.Target);
                }
                return result;
            }

            set
            {
                _weakTransaction = null;

                if (null != value)
                {
                    _weakTransaction = new WeakReference((OdbcTransaction)value);
                }
            }
        }

        [
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ]
        public string Driver
        {
            get
            {
                if (IsOpen)
                {
                    if (ProviderInfo.DriverName == null)
                    {
                        ProviderInfo.DriverName = GetInfoStringUnhandled(ODBC32.SQL_INFO.DRIVER_NAME)!;
                    }
                    return ProviderInfo.DriverName;
                }
                return string.Empty;
            }
        }

        internal bool IsV3Driver
        {
            get
            {
                if (ProviderInfo.DriverVersion == null)
                {
                    ProviderInfo.DriverVersion = GetInfoStringUnhandled(ODBC32.SQL_INFO.DRIVER_ODBC_VER);
                    // protected against null and index out of range. Number cannot be bigger than 99
                    if (ProviderInfo.DriverVersion != null && ProviderInfo.DriverVersion.Length >= 2)
                    {
                        try
                        {   // mdac 89269: driver may return malformatted string
                            ProviderInfo.IsV3Driver = (int.Parse(ProviderInfo.DriverVersion.Substring(0, 2), CultureInfo.InvariantCulture) >= 3);
                        }
                        catch (System.FormatException e)
                        {
                            ProviderInfo.IsV3Driver = false;
                            ADP.TraceExceptionWithoutRethrow(e);
                        }
                    }
                    else
                    {
                        ProviderInfo.DriverVersion = "";
                    }
                }
                return ProviderInfo.IsV3Driver;
            }
        }

        public event OdbcInfoMessageEventHandler? InfoMessage
        {
            add
            {
                _infoMessageEventHandler += value;
            }
            remove
            {
                _infoMessageEventHandler -= value;
            }
        }

        internal char EscapeChar(string method)
        {
            CheckState(method);
            if (!ProviderInfo.HasEscapeChar)
            {
                string escapeCharString;
                escapeCharString = GetInfoStringUnhandled(ODBC32.SQL_INFO.SEARCH_PATTERN_ESCAPE)!;
                Debug.Assert((escapeCharString.Length <= 1), "Can't handle multichar quotes");
                ProviderInfo.EscapeChar = (escapeCharString.Length == 1) ? escapeCharString[0] : QuoteChar(method)[0];
            }
            return ProviderInfo.EscapeChar;
        }

        internal string QuoteChar(string method)
        {
            CheckState(method);
            if (!ProviderInfo.HasQuoteChar)
            {
                string quoteCharString;
                quoteCharString = GetInfoStringUnhandled(ODBC32.SQL_INFO.IDENTIFIER_QUOTE_CHAR)!;
                Debug.Assert((quoteCharString.Length <= 1), "Can't handle multichar quotes");
                ProviderInfo.QuoteChar = (1 == quoteCharString.Length) ? quoteCharString : "\0";
            }
            return ProviderInfo.QuoteChar;
        }

        public new OdbcTransaction BeginTransaction()
        {
            return BeginTransaction(IsolationLevel.Unspecified);
        }

        public new OdbcTransaction BeginTransaction(IsolationLevel isolevel)
        {
            return (OdbcTransaction)InnerConnection.BeginTransaction(isolevel);
        }

        private void RollbackDeadTransaction()
        {
            WeakReference? weak = _weakTransaction;
            if ((null != weak) && !weak.IsAlive)
            {
                _weakTransaction = null;
                ConnectionHandle!.CompleteTransaction(ODBC32.SQL_ROLLBACK);
            }
        }

        public override void ChangeDatabase(string value)
        {
            InnerConnection.ChangeDatabase(value);
        }

        internal void CheckState(string method)
        {
            ConnectionState state = InternalState;
            if (ConnectionState.Open != state)
            {
                throw ADP.OpenConnectionRequired(method, state); // MDAC 68323
            }
        }

        object ICloneable.Clone()
        {
            OdbcConnection clone = new OdbcConnection(this);
            return clone;
        }

        internal bool ConnectionIsAlive(Exception? innerException)
        {
            if (IsOpen)
            {
                if (!ProviderInfo.NoConnectionDead)
                {
                    int isDead = GetConnectAttr(ODBC32.SQL_ATTR.CONNECTION_DEAD, ODBC32.HANDLER.IGNORE);
                    if (ODBC32.SQL_CD_TRUE == isDead)
                    {
                        Close();
                        throw ADP.ConnectionIsDisabled(innerException);
                    }
                }
                // else connection is still alive or attribute not supported
                return true;
            }
            return false;
        }

        public new OdbcCommand CreateCommand()
        {
            return new OdbcCommand(string.Empty, this);
        }

        internal OdbcStatementHandle CreateStatementHandle()
        {
            return new OdbcStatementHandle(ConnectionHandle);
        }

        public override void Close()
        {
            InnerConnection.CloseConnection(this, ConnectionFactory);

            OdbcConnectionHandle? connectionHandle = _connectionHandle;

            if (null != connectionHandle)
            {
                _connectionHandle = null;

                // If there is a pending transaction, automatically rollback.
                WeakReference? weak = _weakTransaction;
                if (null != weak)
                {
                    _weakTransaction = null;
                    IDisposable? transaction = weak.Target as OdbcTransaction;
                    if ((null != transaction) && weak.IsAlive)
                    {
                        transaction.Dispose();
                    }
                    // else transaction will be rolled back when handle is disposed
                }
                connectionHandle.Dispose();
            }
        }

        internal string GetConnectAttrString(ODBC32.SQL_ATTR attribute)
        {
            string value = "";
            int cbActual;
            byte[] buffer = new byte[100];
            OdbcConnectionHandle? connectionHandle = ConnectionHandle;
            if (null != connectionHandle)
            {
                ODBC32.SQLRETURN retcode = connectionHandle.GetConnectionAttribute(attribute, buffer, out cbActual);
                if (buffer.Length + 2 <= cbActual)
                {
                    // 2 bytes for unicode null-termination character
                    // retry with cbActual because original buffer was too small
                    buffer = new byte[cbActual + 2];
                    retcode = connectionHandle.GetConnectionAttribute(attribute, buffer, out cbActual);
                }
                if ((ODBC32.SQLRETURN.SUCCESS == retcode) || (ODBC32.SQLRETURN.SUCCESS_WITH_INFO == retcode))
                {
                    value = (BitConverter.IsLittleEndian ? Encoding.Unicode : Encoding.BigEndianUnicode).GetString(buffer, 0, Math.Min(cbActual, buffer.Length));
                }
                else if (retcode == ODBC32.SQLRETURN.ERROR)
                {
                    string sqlstate = GetDiagSqlState();
                    if (("HYC00" == sqlstate) || ("HY092" == sqlstate) || ("IM001" == sqlstate))
                    {
                        FlagUnsupportedConnectAttr(attribute);
                    }
                    // not throwing errors if not supported or other failure
                }
            }
            return value;
        }

        internal int GetConnectAttr(ODBC32.SQL_ATTR attribute, ODBC32.HANDLER handler)
        {
            int retval = -1;
            byte[] buffer = new byte[4];
            OdbcConnectionHandle? connectionHandle = ConnectionHandle;
            if (null != connectionHandle)
            {
                ODBC32.SQLRETURN retcode = connectionHandle.GetConnectionAttribute(attribute, buffer, out _);

                if ((ODBC32.SQLRETURN.SUCCESS == retcode) || (ODBC32.SQLRETURN.SUCCESS_WITH_INFO == retcode))
                {
                    retval = BitConverter.ToInt32(buffer, 0);
                }
                else
                {
                    if (retcode == ODBC32.SQLRETURN.ERROR)
                    {
                        string sqlstate = GetDiagSqlState();
                        if (("HYC00" == sqlstate) || ("HY092" == sqlstate) || ("IM001" == sqlstate))
                        {
                            FlagUnsupportedConnectAttr(attribute);
                        }
                    }
                    if (handler == ODBC32.HANDLER.THROW)
                    {
                        this.HandleError(connectionHandle, retcode);
                    }
                }
            }
            return retval;
        }

        private string GetDiagSqlState()
        {
            OdbcConnectionHandle connectionHandle = ConnectionHandle!;
            string sqlstate;
            connectionHandle.GetDiagnosticField(out sqlstate);
            return sqlstate;
        }

        internal ODBC32.SQLRETURN GetInfoInt16Unhandled(ODBC32.SQL_INFO info, out short resultValue)
        {
            byte[] buffer = new byte[2];
            ODBC32.SQLRETURN retcode = ConnectionHandle!.GetInfo1(info, buffer);
            resultValue = BitConverter.ToInt16(buffer, 0);
            return retcode;
        }

        internal ODBC32.SQLRETURN GetInfoInt32Unhandled(ODBC32.SQL_INFO info, out int resultValue)
        {
            byte[] buffer = new byte[4];
            ODBC32.SQLRETURN retcode = ConnectionHandle!.GetInfo1(info, buffer);
            resultValue = BitConverter.ToInt32(buffer, 0);
            return retcode;
        }

        private int GetInfoInt32Unhandled(ODBC32.SQL_INFO infotype)
        {
            byte[] buffer = new byte[4];
            ConnectionHandle!.GetInfo1(infotype, buffer);
            return BitConverter.ToInt32(buffer, 0);
        }

        internal string? GetInfoStringUnhandled(ODBC32.SQL_INFO info)
        {
            return GetInfoStringUnhandled(info, false);
        }

        private string? GetInfoStringUnhandled(ODBC32.SQL_INFO info, bool handleError)
        {
            //SQLGetInfo
            string? value = null;
            short cbActual;
            byte[] buffer = new byte[100];
            OdbcConnectionHandle? connectionHandle = ConnectionHandle;
            if (null != connectionHandle)
            {
                ODBC32.SQLRETURN retcode = connectionHandle.GetInfo2(info, buffer, out cbActual);
                if (buffer.Length < cbActual - 2)
                {
                    // 2 bytes for unicode null-termination character
                    // retry with cbActual because original buffer was too small
                    buffer = new byte[cbActual + 2];
                    retcode = connectionHandle.GetInfo2(info, buffer, out cbActual);
                }
                if (retcode == ODBC32.SQLRETURN.SUCCESS || retcode == ODBC32.SQLRETURN.SUCCESS_WITH_INFO)
                {
                    value = (BitConverter.IsLittleEndian ? Encoding.Unicode : Encoding.BigEndianUnicode).GetString(buffer, 0, Math.Min(cbActual, buffer.Length));
                }
                else if (handleError)
                {
                    this.HandleError(connectionHandle, retcode);
                }
            }
            else if (handleError)
            {
                value = "";
            }
            return value;
        }

        // non-throwing HandleError
        internal Exception? HandleErrorNoThrow(OdbcHandle hrHandle, ODBC32.SQLRETURN retcode)
        {
            Debug.Assert(retcode != ODBC32.SQLRETURN.INVALID_HANDLE, "retcode must never be ODBC32.RetCode.INVALID_HANDLE");

            switch (retcode)
            {
                case ODBC32.SQLRETURN.SUCCESS:
                    break;
                case ODBC32.SQLRETURN.SUCCESS_WITH_INFO:
                    {
                        //Optimize to only create the event objects and obtain error info if
                        //the user is really interested in retrieveing the events...
                        if (_infoMessageEventHandler != null)
                        {
                            OdbcErrorCollection errors = ODBC32.GetDiagErrors(null, hrHandle, retcode);
                            errors.SetSource(this.Driver);
                            OnInfoMessage(new OdbcInfoMessageEventArgs(errors));
                        }
                        break;
                    }
                default:
                    OdbcException e = OdbcException.CreateException(ODBC32.GetDiagErrors(null, hrHandle, retcode), retcode);
                    if (e != null)
                    {
                        e.Errors.SetSource(this.Driver);
                    }
                    ConnectionIsAlive(e);        // this will close and throw if the connection is dead
                    return e;
            }
            return null;
        }

        internal void HandleError(OdbcHandle hrHandle, ODBC32.SQLRETURN retcode)
        {
            Exception? e = HandleErrorNoThrow(hrHandle, retcode);
            switch (retcode)
            {
                case ODBC32.SQLRETURN.SUCCESS:
                case ODBC32.SQLRETURN.SUCCESS_WITH_INFO:
                    Debug.Assert(null == e, "success exception");
                    break;
                default:
                    Debug.Assert(null != e, "failure without exception");
                    throw e;
            }
        }

        public override void Open()
        {
            try
            {
                InnerConnection.OpenConnection(this, ConnectionFactory);
            }
            catch (DllNotFoundException e) when (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new DllNotFoundException(SR.Odbc_UnixOdbcNotFound + Environment.NewLine + e.Message);
            }

            // SQLBUDT #276132 - need to manually enlist in some cases, because
            // native ODBC doesn't know about SysTx transactions.
            if (ADP.NeedManualEnlistment())
            {
                EnlistTransaction(SysTx.Transaction.Current);
            }
        }

        private void OnInfoMessage(OdbcInfoMessageEventArgs args)
        {
            if (null != _infoMessageEventHandler)
            {
                try
                {
                    _infoMessageEventHandler(this, args);
                }
                catch (Exception e)
                {
                    //
                    if (!ADP.IsCatchableOrSecurityExceptionType(e))
                    {
                        throw;
                    }
                    ADP.TraceExceptionWithoutRethrow(e);
                }
            }
        }

        public static void ReleaseObjectPool()
        {
            OdbcEnvironment.ReleaseObjectPool();
        }

        internal OdbcTransaction? SetStateExecuting(string method, OdbcTransaction? transaction)
        { // MDAC 69003
            if (null != _weakTransaction)
            { // transaction may exist
                OdbcTransaction? weak = (_weakTransaction.Target as OdbcTransaction);
                if (transaction != weak)
                { // transaction doesn't exist
                    if (null == transaction)
                    { // transaction exists
                        throw ADP.TransactionRequired(method);
                    }
                    if (this != transaction.Connection)
                    {
                        // transaction can't have come from this connection
                        throw ADP.TransactionConnectionMismatch();
                    }
                    // if transaction is zombied, we don't know the original connection
                    transaction = null; // MDAC 69264
                }
            }
            else if (null != transaction)
            { // no transaction started
                if (null != transaction.Connection)
                {
                    // transaction can't have come from this connection
                    throw ADP.TransactionConnectionMismatch();
                }
                // if transaction is zombied, we don't know the original connection
                transaction = null; // MDAC 69264
            }
            ConnectionState state = InternalState;
            if (ConnectionState.Open != state)
            {
                NotifyWeakReference(OdbcReferenceCollection.Recover); // recover for a potentially finalized reader

                state = InternalState;
                if (ConnectionState.Open != state)
                {
                    if (0 != (ConnectionState.Fetching & state))
                    {
                        throw ADP.OpenReaderExists();
                    }
                    throw ADP.OpenConnectionRequired(method, state);
                }
            }
            return transaction;
        }

        // This adds a type to the list of types that are supported by the driver
        // (don't need to know that for all the types)
        //

        internal void SetSupportedType(ODBC32.SQL_TYPE sqltype)
        {
            ODBC32.SQL_CVT sqlcvt;

            switch (sqltype)
            {
                case ODBC32.SQL_TYPE.NUMERIC:
                    {
                        sqlcvt = ODBC32.SQL_CVT.NUMERIC;
                        break;
                    }
                case ODBC32.SQL_TYPE.WCHAR:
                    {
                        sqlcvt = ODBC32.SQL_CVT.WCHAR;
                        break;
                    }
                case ODBC32.SQL_TYPE.WVARCHAR:
                    {
                        sqlcvt = ODBC32.SQL_CVT.WVARCHAR;
                        break;
                    }
                case ODBC32.SQL_TYPE.WLONGVARCHAR:
                    {
                        sqlcvt = ODBC32.SQL_CVT.WLONGVARCHAR;
                        break;
                    }
                default:
                    // other types are irrelevant at this time
                    return;
            }
            ProviderInfo.TestedSQLTypes |= (int)sqlcvt;
            ProviderInfo.SupportedSQLTypes |= (int)sqlcvt;
        }

        internal void FlagRestrictedSqlBindType(ODBC32.SQL_TYPE sqltype)
        {
            ODBC32.SQL_CVT sqlcvt;

            switch (sqltype)
            {
                case ODBC32.SQL_TYPE.NUMERIC:
                    {
                        sqlcvt = ODBC32.SQL_CVT.NUMERIC;
                        break;
                    }
                case ODBC32.SQL_TYPE.DECIMAL:
                    {
                        sqlcvt = ODBC32.SQL_CVT.DECIMAL;
                        break;
                    }
                default:
                    // other types are irrelevant at this time
                    return;
            }
            ProviderInfo.RestrictedSQLBindTypes |= (int)sqlcvt;
        }

        internal void FlagUnsupportedConnectAttr(ODBC32.SQL_ATTR Attribute)
        {
            switch (Attribute)
            {
                case ODBC32.SQL_ATTR.CURRENT_CATALOG:
                    ProviderInfo.NoCurrentCatalog = true;
                    break;
                case ODBC32.SQL_ATTR.CONNECTION_DEAD:
                    ProviderInfo.NoConnectionDead = true;
                    break;
                default:
                    Debug.Fail("Can't flag unknown Attribute");
                    break;
            }
        }

        internal void FlagUnsupportedStmtAttr(ODBC32.SQL_ATTR Attribute)
        {
            switch (Attribute)
            {
                case ODBC32.SQL_ATTR.QUERY_TIMEOUT:
                    ProviderInfo.NoQueryTimeout = true;
                    break;
                case (ODBC32.SQL_ATTR)ODBC32.SQL_SOPT_SS.NOBROWSETABLE:
                    ProviderInfo.NoSqlSoptSSNoBrowseTable = true;
                    break;
                case (ODBC32.SQL_ATTR)ODBC32.SQL_SOPT_SS.HIDDEN_COLUMNS:
                    ProviderInfo.NoSqlSoptSSHiddenColumns = true;
                    break;
                default:
                    Debug.Fail("Can't flag unknown Attribute");
                    break;
            }
        }

        internal void FlagUnsupportedColAttr(ODBC32.SQL_DESC v3FieldId, ODBC32.SQL_COLUMN v2FieldId)
        {
            if (IsV3Driver)
            {
                switch (v3FieldId)
                {
                    case (ODBC32.SQL_DESC)ODBC32.SQL_CA_SS.COLUMN_KEY:
                        // SSS_WARNINGS_OFF
                        ProviderInfo.NoSqlCASSColumnKey = true;
                        break;
                    // SSS_WARNINGS_ON
                    default:
                        Debug.Fail("Can't flag unknown Attribute");
                        break;
                }
            }
            else
            {
                switch (v2FieldId)
                {
                    default:
                        Debug.Fail("Can't flag unknown Attribute");
                        break;
                }
            }
        }

        internal bool SQLGetFunctions(ODBC32.SQL_API odbcFunction)
        {
            //SQLGetFunctions
            ODBC32.SQLRETURN retcode;
            short fExists;
            Debug.Assert((short)odbcFunction != 0, "SQL_API_ALL_FUNCTIONS is not supported");
            OdbcConnectionHandle? connectionHandle = ConnectionHandle;
            if (null != connectionHandle)
            {
                retcode = connectionHandle.GetFunctions(odbcFunction, out fExists);
            }
            else
            {
                Debug.Fail("GetFunctions called and ConnectionHandle is null (connection is disposed?)");
                throw ODBC.ConnectionClosed();
            }

            if (retcode != ODBC32.SQLRETURN.SUCCESS)
                this.HandleError(connectionHandle, retcode);

            if (fExists == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        internal bool TestTypeSupport(ODBC32.SQL_TYPE sqltype)
        {
            ODBC32.SQL_CONVERT sqlconvert;
            ODBC32.SQL_CVT sqlcvt;

            // we need to convert the sqltype to sqlconvert and sqlcvt first
            //
            switch (sqltype)
            {
                case ODBC32.SQL_TYPE.NUMERIC:
                    {
                        sqlconvert = ODBC32.SQL_CONVERT.NUMERIC;
                        sqlcvt = ODBC32.SQL_CVT.NUMERIC;
                        break;
                    }
                case ODBC32.SQL_TYPE.WCHAR:
                    {
                        sqlconvert = ODBC32.SQL_CONVERT.CHAR;
                        sqlcvt = ODBC32.SQL_CVT.WCHAR;
                        break;
                    }
                case ODBC32.SQL_TYPE.WVARCHAR:
                    {
                        sqlconvert = ODBC32.SQL_CONVERT.VARCHAR;
                        sqlcvt = ODBC32.SQL_CVT.WVARCHAR;
                        break;
                    }
                case ODBC32.SQL_TYPE.WLONGVARCHAR:
                    {
                        sqlconvert = ODBC32.SQL_CONVERT.LONGVARCHAR;
                        sqlcvt = ODBC32.SQL_CVT.WLONGVARCHAR;
                        break;
                    }
                default:
                    Debug.Fail("Testing that sqltype is currently not supported");
                    return false;
            }
            // now we can check if we have already tested that type
            // if not we need to do so
            if (0 == (ProviderInfo.TestedSQLTypes & (int)sqlcvt))
            {
                int flags;

                flags = GetInfoInt32Unhandled((ODBC32.SQL_INFO)sqlconvert);
                flags = flags & (int)sqlcvt;

                ProviderInfo.TestedSQLTypes |= (int)sqlcvt;
                ProviderInfo.SupportedSQLTypes |= flags;
            }

            // now check if the type is supported and return the result
            //
            return (0 != (ProviderInfo.SupportedSQLTypes & (int)sqlcvt));
        }

        internal bool TestRestrictedSqlBindType(ODBC32.SQL_TYPE sqltype)
        {
            ODBC32.SQL_CVT sqlcvt;
            switch (sqltype)
            {
                case ODBC32.SQL_TYPE.NUMERIC:
                    {
                        sqlcvt = ODBC32.SQL_CVT.NUMERIC;
                        break;
                    }
                case ODBC32.SQL_TYPE.DECIMAL:
                    {
                        sqlcvt = ODBC32.SQL_CVT.DECIMAL;
                        break;
                    }
                default:
                    Debug.Fail("Testing that sqltype is currently not supported");
                    return false;
            }
            return (0 != (ProviderInfo.RestrictedSQLBindTypes & (int)sqlcvt));
        }

        // suppress this message - we cannot use SafeHandle here. Also, see notes in the code (VSTFDEVDIV# 560355)
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            DbTransaction transaction = InnerConnection.BeginTransaction(isolationLevel);

            // VSTFDEVDIV# 560355 - InnerConnection doesn't maintain a ref on the outer connection (this) and
            //   subsequently leaves open the possibility that the outer connection could be GC'ed before the DbTransaction
            //   is fully hooked up (leaving a DbTransaction with a null connection property). Ensure that this is reachable
            //   until the completion of BeginTransaction with KeepAlive
            GC.KeepAlive(this);

            return transaction;
        }

        internal OdbcTransaction Open_BeginTransaction(IsolationLevel isolevel)
        {
            CheckState(ADP.BeginTransaction); // MDAC 68323

            RollbackDeadTransaction();

            if ((null != _weakTransaction) && _weakTransaction.IsAlive)
            { // regression from Dispose/Finalize work
                throw ADP.ParallelTransactionsNotSupported(this);
            }

            //Use the default for unspecified.
            switch (isolevel)
            {
                case IsolationLevel.Unspecified:
                case IsolationLevel.ReadUncommitted:
                case IsolationLevel.ReadCommitted:
                case IsolationLevel.RepeatableRead:
                case IsolationLevel.Serializable:
                case IsolationLevel.Snapshot:
                    break;
                case IsolationLevel.Chaos:
                    throw ODBC.NotSupportedIsolationLevel(isolevel);
                default:
                    throw ADP.InvalidIsolationLevel(isolevel);
            };

            //Start the transaction
            OdbcConnectionHandle connectionHandle = ConnectionHandle!;
            ODBC32.SQLRETURN retcode = connectionHandle.BeginTransaction(ref isolevel);
            if (retcode == ODBC32.SQLRETURN.ERROR)
            {
                HandleError(connectionHandle, retcode);
            }
            OdbcTransaction transaction = new OdbcTransaction(this, isolevel, connectionHandle);
            _weakTransaction = new WeakReference(transaction); // MDAC 69188
            return transaction;
        }

        internal void Open_ChangeDatabase(string value)
        {
            CheckState(ADP.ChangeDatabase);

            // Database name must not be null, empty or whitspace
            if (string.IsNullOrWhiteSpace(value))
            { // MDAC 62679
                throw ADP.EmptyDatabaseName();
            }
            if (1024 < value.Length * 2 + 2)
            {
                throw ADP.DatabaseNameTooLong();
            }
            RollbackDeadTransaction();

            //Set the database
            OdbcConnectionHandle connectionHandle = ConnectionHandle!;
            ODBC32.SQLRETURN retcode = connectionHandle.SetConnectionAttribute3(ODBC32.SQL_ATTR.CURRENT_CATALOG, value, checked((int)value.Length * 2));

            if (retcode != ODBC32.SQLRETURN.SUCCESS)
            {
                HandleError(connectionHandle, retcode);
            }
        }

        internal string? Open_GetServerVersion()
        {
            //SQLGetInfo - SQL_DBMS_VER
            return GetInfoStringUnhandled(ODBC32.SQL_INFO.DBMS_VER, true);
        }
    }
}
