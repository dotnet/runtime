// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;            //Component
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;

// todo:
// There may be two ways to improve performance:
// 1. pool statements on the connection object
// 2. Do not create a datareader object for non-datareader returning command execution.
//
// We do not want to do the effort unless we have to squeze performance.

namespace System.Data.Odbc
{
    public sealed class OdbcCommand : DbCommand, ICloneable
    {
        private static int s_objectTypeCount; // Bid counter
        internal readonly int ObjectID = System.Threading.Interlocked.Increment(ref s_objectTypeCount);

        private string _commandText;
        private CommandType _commandType;
        private int _commandTimeout = ADP.DefaultCommandTimeout;
        private UpdateRowSource _updatedRowSource = UpdateRowSource.Both;
        private bool _designTimeInvisible;
        private bool _isPrepared;                        // true if the command is prepared

        private OdbcConnection _connection;
        private OdbcTransaction _transaction;

        private WeakReference _weakDataReaderReference;

        private CMDWrapper _cmdWrapper;

        private OdbcParameterCollection _parameterCollection;   // Parameter collection

        private ConnectionState _cmdState;

        public OdbcCommand() : base()
        {
            GC.SuppressFinalize(this);
        }

        public OdbcCommand(string cmdText) : this()
        {
            // note: arguments are assigned to properties so we do not have to trace them.
            // We still need to include them into the argument list of the definition!
            CommandText = cmdText;
        }

        public OdbcCommand(string cmdText, OdbcConnection connection) : this()
        {
            CommandText = cmdText;
            Connection = connection;
        }

        public OdbcCommand(string cmdText, OdbcConnection connection, OdbcTransaction transaction) : this()
        {
            CommandText = cmdText;
            Connection = connection;
            Transaction = transaction;
        }

        private void DisposeDeadDataReader()
        {
            if (ConnectionState.Fetching == _cmdState)
            {
                if (null != _weakDataReaderReference && !_weakDataReaderReference.IsAlive)
                {
                    if (_cmdWrapper != null)
                    {
                        _cmdWrapper.FreeKeyInfoStatementHandle(ODBC32.STMT.CLOSE);
                        _cmdWrapper.FreeStatementHandle(ODBC32.STMT.CLOSE);
                    }
                    CloseFromDataReader();
                }
            }
        }

        private void DisposeDataReader()
        {
            if (null != _weakDataReaderReference)
            {
                IDisposable reader = (IDisposable)_weakDataReaderReference.Target;
                if ((null != reader) && _weakDataReaderReference.IsAlive)
                {
                    ((IDisposable)reader).Dispose();
                }
                CloseFromDataReader();
            }
        }

        internal void DisconnectFromDataReaderAndConnection()
        {
            // get a reference to the datareader if it is alive
            OdbcDataReader liveReader = null;
            if (_weakDataReaderReference != null)
            {
                OdbcDataReader reader;
                reader = (OdbcDataReader)_weakDataReaderReference.Target;
                if (_weakDataReaderReference.IsAlive)
                {
                    liveReader = reader;
                }
            }

            // remove reference to this from the live datareader
            if (liveReader != null)
            {
                liveReader.Command = null;
            }

            _transaction = null;

            if (null != _connection)
            {
                _connection.RemoveWeakReference(this);
                _connection = null;
            }

            // if the reader is dead we have to dismiss the statement
            if (liveReader == null)
            {
                CloseCommandWrapper();
            }
            // else DataReader now has exclusive ownership
            _cmdWrapper = null;
        }

        protected override void Dispose(bool disposing)
        { // MDAC 65459
            if (disposing)
            {
                // release mananged objects
                // in V1.0, V1.1 the Connection,Parameters,CommandText,Transaction where reset
                this.DisconnectFromDataReaderAndConnection();
                _parameterCollection = null;
                CommandText = null;
            }
            _cmdWrapper = null;                         // let go of the CommandWrapper
            _isPrepared = false;

            base.Dispose(disposing);    // notify base classes
        }

        internal bool Canceling
        {
            get
            {
                return _cmdWrapper.Canceling;
            }
        }

        public override string CommandText
        {
            get
            {
                string value = _commandText;
                return ((null != value) ? value : string.Empty);
            }
            set
            {
                if (_commandText != value)
                {
                    PropertyChanging();
                    _commandText = value;
                }
            }
        }

        public override int CommandTimeout
        { // V1.2.3300, XXXCommand V1.0.5000
            get
            {
                return _commandTimeout;
            }
            set
            {
                if (value < 0)
                {
                    throw ADP.InvalidCommandTimeout(value);
                }
                if (value != _commandTimeout)
                {
                    PropertyChanging();
                    _commandTimeout = value;
                }
            }
        }

        public void ResetCommandTimeout()
        { // V1.2.3300
            if (ADP.DefaultCommandTimeout != _commandTimeout)
            {
                PropertyChanging();
                _commandTimeout = ADP.DefaultCommandTimeout;
            }
        }

        private bool ShouldSerializeCommandTimeout()
        { // V1.2.3300
            return (ADP.DefaultCommandTimeout != _commandTimeout);
        }

        [
        DefaultValue(System.Data.CommandType.Text),
        ]
        public override CommandType CommandType
        {
            get
            {
                CommandType cmdType = _commandType;
                return ((0 != cmdType) ? cmdType : CommandType.Text);
            }
            set
            {
                switch (value)
                { // @perfnote: Enum.IsDefined
                    case CommandType.Text:
                    case CommandType.StoredProcedure:
                        PropertyChanging();
                        _commandType = value;
                        break;
                    case CommandType.TableDirect:
                        throw ODBC.NotSupportedCommandType(value);
                    default:
                        throw ADP.InvalidCommandType(value);
                }
            }
        }

        public new OdbcConnection Connection
        {
            get
            {
                return _connection;
            }
            set
            {
                if (value != _connection)
                {
                    PropertyChanging();
                    this.DisconnectFromDataReaderAndConnection();
                    Debug.Assert(null == _cmdWrapper, "has CMDWrapper when setting connection");
                    _connection = value;
                    //OnSchemaChanged();
                }
            }
        }

        protected override DbConnection DbConnection
        { // V1.2.3300
            get
            {
                return Connection;
            }
            set
            {
                Connection = (OdbcConnection)value;
            }
        }

        protected override DbParameterCollection DbParameterCollection
        { // V1.2.3300
            get
            {
                return Parameters;
            }
        }

        protected override DbTransaction DbTransaction
        { // V1.2.3300
            get
            {
                return Transaction;
            }
            set
            {
                Transaction = (OdbcTransaction)value;
            }
        }

        // @devnote: By default, the cmd object is visible on the design surface (i.e. VS7 Server Tray)
        // to limit the number of components that clutter the design surface,
        // when the DataAdapter design wizard generates the insert/update/delete commands it will
        // set the DesignTimeVisible property to false so that cmds won't appear as individual objects
        [
        DefaultValue(true),
        DesignOnly(true),
        Browsable(false),
        EditorBrowsableAttribute(EditorBrowsableState.Never),
        ]
        public override bool DesignTimeVisible
        { // V1.2.3300, XXXCommand V1.0.5000
            get
            {
                return !_designTimeInvisible;
            }
            set
            {
                _designTimeInvisible = !value;
                TypeDescriptor.Refresh(this); // VS7 208845
            }
        }

        internal bool HasParameters
        {
            get
            {
                return (null != _parameterCollection);
            }
        }

        [
        DesignerSerializationVisibility(DesignerSerializationVisibility.Content),
        ]
        public new OdbcParameterCollection Parameters
        {
            get
            {
                if (null == _parameterCollection)
                {
                    _parameterCollection = new OdbcParameterCollection();
                }
                return _parameterCollection;
            }
        }

        [
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ]
        public new OdbcTransaction Transaction
        {
            get
            {
                if ((null != _transaction) && (null == _transaction.Connection))
                {
                    _transaction = null;       // Dawn of the Dead
                }
                return _transaction;
            }
            set
            {
                if (_transaction != value)
                {
                    PropertyChanging(); // fire event before value is validated
                    _transaction = value;
                }
            }
        }

        [
        DefaultValue(System.Data.UpdateRowSource.Both),
        ]
        public override UpdateRowSource UpdatedRowSource
        { // V1.2.3300, XXXCommand V1.0.5000
            get
            {
                return _updatedRowSource;
            }
            set
            {
                switch (value)
                { // @perfnote: Enum.IsDefined
                    case UpdateRowSource.None:
                    case UpdateRowSource.OutputParameters:
                    case UpdateRowSource.FirstReturnedRecord:
                    case UpdateRowSource.Both:
                        _updatedRowSource = value;
                        break;
                    default:
                        throw ADP.InvalidUpdateRowSource(value);
                }
            }
        }

        internal OdbcDescriptorHandle GetDescriptorHandle(ODBC32.SQL_ATTR attribute)
        {
            return _cmdWrapper.GetDescriptorHandle(attribute);
        }


        // GetStatementHandle
        // ------------------
        // Try to return a cached statement handle.
        //
        // Creates a CmdWrapper object if necessary
        // If no handle is available a handle will be allocated.
        // Bindings will be unbound if a handle is cached and the bindings are invalid.
        //
        internal CMDWrapper GetStatementHandle()
        {
            // update the command wrapper object, allocate buffer
            // create reader object
            //
            if (_cmdWrapper == null)
            {
                _cmdWrapper = new CMDWrapper(_connection);

                Debug.Assert(null != _connection, "GetStatementHandle without connection?");
                _connection.AddWeakReference(this, OdbcReferenceCollection.CommandTag);
            }

            if (_cmdWrapper._dataReaderBuf == null)
            {
                _cmdWrapper._dataReaderBuf = new CNativeBuffer(4096);
            }

            // if there is already a statement handle we need to do some cleanup
            //
            if (null == _cmdWrapper.StatementHandle)
            {
                _isPrepared = false;
                _cmdWrapper.CreateStatementHandle();
            }
            else if ((null != _parameterCollection) && _parameterCollection.RebindCollection)
            {
                _cmdWrapper.FreeStatementHandle(ODBC32.STMT.RESET_PARAMS);
            }
            return _cmdWrapper;
        }

        // OdbcCommand.Cancel()
        //
        // In ODBC3.0 ... a call to SQLCancel when no processing is done has no effect at all
        // (ODBC Programmer's Reference ...)
        //

        public override void Cancel()
        {
            CMDWrapper wrapper = _cmdWrapper;
            if (null != wrapper)
            {
                wrapper.Canceling = true;
                OdbcStatementHandle stmt = wrapper.StatementHandle;
                if (null != stmt)
                {
                    lock (stmt)
                    {
                        // Cancel the statement
                        ODBC32.RetCode retcode = stmt.Cancel();

                        // copy of StatementErrorHandler, because stmt may become null
                        switch (retcode)
                        {
                            case ODBC32.RetCode.SUCCESS:
                            case ODBC32.RetCode.SUCCESS_WITH_INFO:
                                // don't fire info message events on cancel
                                break;
                            default:
                                throw wrapper.Connection.HandleErrorNoThrow(stmt, retcode);
                        }
                    }
                }
            }
        }


        object ICloneable.Clone()
        {
            OdbcCommand clone = new OdbcCommand();
            clone.CommandText = CommandText;
            clone.CommandTimeout = this.CommandTimeout;
            clone.CommandType = CommandType;
            clone.Connection = this.Connection;
            clone.Transaction = this.Transaction;
            clone.UpdatedRowSource = UpdatedRowSource;

            if ((null != _parameterCollection) && (0 < Parameters.Count))
            {
                OdbcParameterCollection parameters = clone.Parameters;
                foreach (ICloneable parameter in Parameters)
                {
                    parameters.Add(parameter.Clone());
                }
            }
            return clone;
        }

        internal bool RecoverFromConnection()
        {
            DisposeDeadDataReader();
            return (ConnectionState.Closed == _cmdState);
        }

        private void CloseCommandWrapper()
        {
            CMDWrapper wrapper = _cmdWrapper;
            if (null != wrapper)
            {
                try
                {
                    wrapper.Dispose();

                    if (null != _connection)
                    {
                        _connection.RemoveWeakReference(this);
                    }
                }
                finally
                {
                    _cmdWrapper = null;
                }
            }
        }

        internal void CloseFromConnection()
        {
            if (null != _parameterCollection)
            {
                _parameterCollection.RebindCollection = true;
            }
            DisposeDataReader();
            CloseCommandWrapper();
            _isPrepared = false;
            _transaction = null;
        }

        internal void CloseFromDataReader()
        {
            _weakDataReaderReference = null;
            _cmdState = ConnectionState.Closed;
        }

        public new OdbcParameter CreateParameter()
        {
            return new OdbcParameter();
        }

        protected override DbParameter CreateDbParameter()
        {
            return CreateParameter();
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            return ExecuteReader(behavior);
        }

        public override int ExecuteNonQuery()
        {
            using (OdbcDataReader reader = ExecuteReaderObject(0, ADP.ExecuteNonQuery, false))
            {
                reader.Close();
                return reader.RecordsAffected;
            }
        }

        public new OdbcDataReader ExecuteReader()
        {
            return ExecuteReader(0/*CommandBehavior*/);
        }


        public new OdbcDataReader ExecuteReader(CommandBehavior behavior)
        {
            return ExecuteReaderObject(behavior, ADP.ExecuteReader, true);
        }

        internal OdbcDataReader ExecuteReaderFromSQLMethod(object[] methodArguments,
                                                            ODBC32.SQL_API method)
        {
            return ExecuteReaderObject(CommandBehavior.Default, method.ToString(), true, methodArguments, method);
        }

        private OdbcDataReader ExecuteReaderObject(CommandBehavior behavior, string method, bool needReader)
        { // MDAC 68324
            if ((CommandText == null) || (CommandText.Length == 0))
            {
                throw (ADP.CommandTextRequired(method));
            }
            // using all functions to tell ExecuteReaderObject that
            return ExecuteReaderObject(behavior, method, needReader, null, ODBC32.SQL_API.SQLEXECDIRECT);
        }

        private OdbcDataReader ExecuteReaderObject(CommandBehavior behavior,
                                                   string method,
                                                   bool needReader,
                                                   object[] methodArguments,
                                                   ODBC32.SQL_API odbcApiMethod)
        { // MDAC 68324
            OdbcDataReader localReader = null;
            try
            {
                DisposeDeadDataReader();    // this is a no-op if cmdState is not Fetching
                ValidateConnectionAndTransaction(method);  // cmdState will change to Executing

                if (0 != (CommandBehavior.SingleRow & behavior))
                {
                    // CommandBehavior.SingleRow implies CommandBehavior.SingleResult
                    behavior |= CommandBehavior.SingleResult;
                }

                ODBC32.RetCode retcode;

                OdbcStatementHandle stmt = GetStatementHandle().StatementHandle;
                _cmdWrapper.Canceling = false;

                if (null != _weakDataReaderReference)
                {
                    if (_weakDataReaderReference.IsAlive)
                    {
                        object target = _weakDataReaderReference.Target;
                        if (null != target && _weakDataReaderReference.IsAlive)
                        {
                            if (!((OdbcDataReader)target).IsClosed)
                            {
                                throw ADP.OpenReaderExists(); // MDAC 66411
                            }
                        }
                    }
                }
                localReader = new OdbcDataReader(this, _cmdWrapper, behavior);

                //Set command properties
                //Not all drivers support timeout. So fail silently if error
                if (!Connection.ProviderInfo.NoQueryTimeout)
                {
                    TrySetStatementAttribute(stmt,
                        ODBC32.SQL_ATTR.QUERY_TIMEOUT,
                        (IntPtr)this.CommandTimeout);
                }

                // todo: If we remember the state we can omit a lot of SQLSetStmtAttrW calls ...
                // if we do not create a reader we do not even need to do that
                if (needReader)
                {
                    if (Connection.IsV3Driver)
                    {
                        if (!Connection.ProviderInfo.NoSqlSoptSSNoBrowseTable && !Connection.ProviderInfo.NoSqlSoptSSHiddenColumns)
                        {
                            // Need to get the metadata information

                            //SQLServer actually requires browse info turned on ahead of time...
                            //Note: We ignore any failures, since this is SQLServer specific
                            //We won't specialcase for SQL Server but at least for non-V3 drivers
                            if (localReader.IsBehavior(CommandBehavior.KeyInfo))
                            {
                                if (!_cmdWrapper._ssKeyInfoModeOn)
                                {
                                    TrySetStatementAttribute(stmt, (ODBC32.SQL_ATTR)ODBC32.SQL_SOPT_SS.NOBROWSETABLE, (IntPtr)ODBC32.SQL_NB.ON);
                                    TrySetStatementAttribute(stmt, (ODBC32.SQL_ATTR)ODBC32.SQL_SOPT_SS.HIDDEN_COLUMNS, (IntPtr)ODBC32.SQL_HC.ON);
                                    _cmdWrapper._ssKeyInfoModeOff = false;
                                    _cmdWrapper._ssKeyInfoModeOn = true;
                                }
                            }
                            else
                            {
                                if (!_cmdWrapper._ssKeyInfoModeOff)
                                {
                                    TrySetStatementAttribute(stmt, (ODBC32.SQL_ATTR)ODBC32.SQL_SOPT_SS.NOBROWSETABLE, (IntPtr)ODBC32.SQL_NB.OFF);
                                    TrySetStatementAttribute(stmt, (ODBC32.SQL_ATTR)ODBC32.SQL_SOPT_SS.HIDDEN_COLUMNS, (IntPtr)ODBC32.SQL_HC.OFF);
                                    _cmdWrapper._ssKeyInfoModeOff = true;
                                    _cmdWrapper._ssKeyInfoModeOn = false;
                                }
                            }
                        }
                    }
                }

                if (localReader.IsBehavior(CommandBehavior.KeyInfo) ||
                    localReader.IsBehavior(CommandBehavior.SchemaOnly))
                {
                    retcode = stmt.Prepare(CommandText);

                    if (ODBC32.RetCode.SUCCESS != retcode)
                    {
                        _connection.HandleError(stmt, retcode);
                    }
                }

                bool mustRelease = false;
                CNativeBuffer parameterBuffer = _cmdWrapper._nativeParameterBuffer;

                try
                {
                    //Handle Parameters
                    //Note: We use the internal variable as to not instante a new object collection,
                    //for the common case of using no parameters.
                    if ((null != _parameterCollection) && (0 < _parameterCollection.Count))
                    {
                        int parameterBufferSize = _parameterCollection.CalcParameterBufferSize(this);

                        if (null == parameterBuffer || parameterBuffer.Length < parameterBufferSize)
                        {
                            if (null != parameterBuffer)
                            {
                                parameterBuffer.Dispose();
                            }
                            parameterBuffer = new CNativeBuffer(parameterBufferSize);
                            _cmdWrapper._nativeParameterBuffer = parameterBuffer;
                        }
                        else
                        {
                            parameterBuffer.ZeroMemory();
                        }

                        parameterBuffer.DangerousAddRef(ref mustRelease);

                        _parameterCollection.Bind(this, _cmdWrapper, parameterBuffer);
                    }

                    if (!localReader.IsBehavior(CommandBehavior.SchemaOnly))
                    {
                        // Can't get the KeyInfo after command execution (SQL Server only since it does not support multiple
                        // results on the same connection). Stored procedures (SP) do not return metadata before actual execution
                        // Need to check the column count since the command type may not be set to SP for a SP.
                        if ((localReader.IsBehavior(CommandBehavior.KeyInfo) || localReader.IsBehavior(CommandBehavior.SchemaOnly))
                            && (CommandType != CommandType.StoredProcedure))
                        {
                            short cColsAffected;
                            retcode = stmt.NumberOfResultColumns(out cColsAffected);
                            if (retcode == ODBC32.RetCode.SUCCESS || retcode == ODBC32.RetCode.SUCCESS_WITH_INFO)
                            {
                                if (cColsAffected > 0)
                                {
                                    localReader.GetSchemaTable();
                                }
                            }
                            else if (retcode == ODBC32.RetCode.NO_DATA)
                            {
                                // do nothing
                            }
                            else
                            {
                                // any other returncode indicates an error
                                _connection.HandleError(stmt, retcode);
                            }
                        }

                        switch (odbcApiMethod)
                        {
                            case ODBC32.SQL_API.SQLEXECDIRECT:
                                if (localReader.IsBehavior(CommandBehavior.KeyInfo) || _isPrepared)
                                {
                                    //Already prepared, so use SQLExecute
                                    retcode = stmt.Execute();
                                    // Build metadata here
                                    // localReader.GetSchemaTable();
                                }
                                else
                                {
#if DEBUG
                                    //if (AdapterSwitches.OleDbTrace.TraceInfo) {
                                    //    ADP.DebugWriteLine("SQLExecDirectW: " + CommandText);
                                    //}
#endif
                                    //SQLExecDirect
                                    retcode = stmt.ExecuteDirect(CommandText);
                                }
                                break;

                            case ODBC32.SQL_API.SQLTABLES:
                                retcode = stmt.Tables((string)methodArguments[0],  //TableCatalog
                                    (string)methodArguments[1],  //TableSchema,
                                    (string)methodArguments[2],  //TableName
                                    (string)methodArguments[3]); //TableType
                                break;

                            case ODBC32.SQL_API.SQLCOLUMNS:
                                retcode = stmt.Columns((string)methodArguments[0],  //TableCatalog
                                    (string)methodArguments[1],  //TableSchema
                                    (string)methodArguments[2],  //TableName
                                    (string)methodArguments[3]); //ColumnName
                                break;

                            case ODBC32.SQL_API.SQLPROCEDURES:
                                retcode = stmt.Procedures((string)methodArguments[0],  //ProcedureCatalog
                                    (string)methodArguments[1],  //ProcedureSchema
                                    (string)methodArguments[2]); //procedureName
                                break;

                            case ODBC32.SQL_API.SQLPROCEDURECOLUMNS:
                                retcode = stmt.ProcedureColumns((string)methodArguments[0],  //ProcedureCatalog
                                    (string)methodArguments[1],  //ProcedureSchema
                                    (string)methodArguments[2],  //procedureName
                                    (string)methodArguments[3]); //columnName
                                break;

                            case ODBC32.SQL_API.SQLSTATISTICS:
                                retcode = stmt.Statistics((string)methodArguments[0],  //TableCatalog
                                    (string)methodArguments[1],  //TableSchema
                                    (string)methodArguments[2],  //TableName
                                    (short)methodArguments[3],   //IndexTrpe
                                    (short)methodArguments[4]);  //Accuracy
                                break;

                            case ODBC32.SQL_API.SQLGETTYPEINFO:
                                retcode = stmt.GetTypeInfo((short)methodArguments[0]);  //SQL Type
                                break;

                            default:
                                // this should NEVER happen
                                Debug.Fail("ExecuteReaderObjectcalled with unsupported ODBC API method.");
                                throw ADP.InvalidOperation(method.ToString());
                        }

                        //Note: Execute will return NO_DATA for Update/Delete non-row returning queries
                        if ((ODBC32.RetCode.SUCCESS != retcode) && (ODBC32.RetCode.NO_DATA != retcode))
                        {
                            _connection.HandleError(stmt, retcode);
                        }
                    } // end SchemaOnly
                }
                finally
                {
                    if (mustRelease)
                    {
                        parameterBuffer.DangerousRelease();
                    }
                }

                _weakDataReaderReference = new WeakReference(localReader);

                // XXXCommand.Execute should position reader on first row returning result
                // any exceptions in the initial non-row returning results should be thrown
                // from ExecuteXXX not the DataReader
                if (!localReader.IsBehavior(CommandBehavior.SchemaOnly))
                {
                    localReader.FirstResult();
                }
                _cmdState = ConnectionState.Fetching;
            }
            finally
            {
                if (ConnectionState.Fetching != _cmdState)
                {
                    if (null != localReader)
                    {
                        // clear bindings so we don't grab output parameters on a failed execute
                        if (null != _parameterCollection)
                        {
                            _parameterCollection.ClearBindings();
                        }
                        ((IDisposable)localReader).Dispose();
                    }
                    if (ConnectionState.Closed != _cmdState)
                    {
                        _cmdState = ConnectionState.Closed;
                    }
                }
            }
            return localReader;
        }

        public override object ExecuteScalar()
        {
            object value = null;
            using (IDataReader reader = ExecuteReaderObject(0, ADP.ExecuteScalar, false))
            {
                if (reader.Read() && (0 < reader.FieldCount))
                {
                    value = reader.GetValue(0);
                }
                reader.Close();
            }
            return value;
        }

        internal string GetDiagSqlState()
        {
            return _cmdWrapper.GetDiagSqlState();
        }

        private void PropertyChanging()
        {
            _isPrepared = false;
        }

        // Prepare
        //
        // if the CommandType property is set to TableDirect Prepare does nothing.
        // if the CommandType property is set to StoredProcedure Prepare should succeed but result
        // in a no-op
        //
        // throw InvalidOperationException
        // if the connection is not set
        // if the connection is not open
        //
        public override void Prepare()
        {
            ODBC32.RetCode retcode;

            ValidateOpenConnection(ADP.Prepare);

            if (0 != (ConnectionState.Fetching & _connection.InternalState))
            {
                throw ADP.OpenReaderExists();
            }

            if (CommandType == CommandType.TableDirect)
            {
                return; // do nothing
            }

            DisposeDeadDataReader();
            GetStatementHandle();

            OdbcStatementHandle stmt = _cmdWrapper.StatementHandle;

            retcode = stmt.Prepare(CommandText);


            if (ODBC32.RetCode.SUCCESS != retcode)
            {
                _connection.HandleError(stmt, retcode);
            }
            _isPrepared = true;
        }



        private void TrySetStatementAttribute(OdbcStatementHandle stmt, ODBC32.SQL_ATTR stmtAttribute, IntPtr value)
        {
            ODBC32.RetCode retcode = stmt.SetStatementAttribute(
                stmtAttribute,
                value,
                ODBC32.SQL_IS.UINTEGER);

            if (retcode == ODBC32.RetCode.ERROR)
            {
                string sqlState;
                stmt.GetDiagnosticField(out sqlState);

                if ((sqlState == "HYC00") || (sqlState == "HY092"))
                {
                    Connection.FlagUnsupportedStmtAttr(stmtAttribute);
                }
                else
                {
                    // now what? Should we throw?
                }
            }
        }

        private void ValidateOpenConnection(string methodName)
        {
            // see if we have a connection
            OdbcConnection connection = Connection;

            if (null == connection)
            {
                throw ADP.ConnectionRequired(methodName);
            }

            // must have an open and available connection
            ConnectionState state = connection.State;

            if (ConnectionState.Open != state)
            {
                throw ADP.OpenConnectionRequired(methodName, state);
            }
        }

        private void ValidateConnectionAndTransaction(string method)
        {
            if (null == _connection)
            {
                throw ADP.ConnectionRequired(method);
            }
            _transaction = _connection.SetStateExecuting(method, Transaction);
            _cmdState = ConnectionState.Executing;
        }
    }
    internal sealed class CMDWrapper
    {
        private OdbcStatementHandle _stmt;                  // hStmt
        private OdbcStatementHandle _keyinfostmt;           // hStmt for keyinfo

        internal OdbcDescriptorHandle _hdesc;              // hDesc

        internal CNativeBuffer _nativeParameterBuffer;      // Native memory for internal memory management
        // (Performance optimization)

        internal CNativeBuffer _dataReaderBuf;         // Reusable DataReader buffer

        private readonly OdbcConnection _connection;        // Connection
        private bool _canceling;             // true if the command is canceling
        internal bool _hasBoundColumns;
        internal bool _ssKeyInfoModeOn;       // tells us if the SqlServer specific options are on
        internal bool _ssKeyInfoModeOff;      // a tri-state value would be much better ...

        internal CMDWrapper(OdbcConnection connection)
        {
            _connection = connection;
        }

        internal bool Canceling
        {
            get
            {
                return _canceling;
            }
            set
            {
                _canceling = value;
            }
        }

        internal OdbcConnection Connection
        {
            get
            {
                return _connection;
            }
        }

        internal bool HasBoundColumns
        {
            //            get {
            //                return _hasBoundColumns;
            //            }
            set
            {
                _hasBoundColumns = value;
            }
        }

        internal OdbcStatementHandle StatementHandle
        {
            get { return _stmt; }
        }

        internal OdbcStatementHandle KeyInfoStatement
        {
            get
            {
                return _keyinfostmt;
            }
        }

        internal void CreateKeyInfoStatementHandle()
        {
            DisposeKeyInfoStatementHandle();
            _keyinfostmt = _connection.CreateStatementHandle();
        }

        internal void CreateStatementHandle()
        {
            DisposeStatementHandle();
            _stmt = _connection.CreateStatementHandle();
        }

        internal void Dispose()
        {
            if (null != _dataReaderBuf)
            {
                _dataReaderBuf.Dispose();
                _dataReaderBuf = null;
            }
            DisposeStatementHandle();

            CNativeBuffer buffer = _nativeParameterBuffer;
            _nativeParameterBuffer = null;
            if (null != buffer)
            {
                buffer.Dispose();
            }
            _ssKeyInfoModeOn = false;
            _ssKeyInfoModeOff = false;
        }

        private void DisposeDescriptorHandle()
        {
            OdbcDescriptorHandle handle = _hdesc;
            if (null != handle)
            {
                _hdesc = null;
                handle.Dispose();
            }
        }
        internal void DisposeStatementHandle()
        {
            DisposeKeyInfoStatementHandle();
            DisposeDescriptorHandle();

            OdbcStatementHandle handle = _stmt;
            if (null != handle)
            {
                _stmt = null;
                handle.Dispose();
            }
        }

        internal void DisposeKeyInfoStatementHandle()
        {
            OdbcStatementHandle handle = _keyinfostmt;
            if (null != handle)
            {
                _keyinfostmt = null;
                handle.Dispose();
            }
        }

        internal void FreeStatementHandle(ODBC32.STMT stmt)
        {
            DisposeDescriptorHandle();

            OdbcStatementHandle handle = _stmt;
            if (null != handle)
            {
                try
                {
                    ODBC32.RetCode retcode;
                    retcode = handle.FreeStatement(stmt);
                    StatementErrorHandler(retcode);
                }
                catch (Exception e)
                {
                    //
                    if (ADP.IsCatchableExceptionType(e))
                    {
                        _stmt = null;
                        handle.Dispose();
                    }

                    throw;
                }
            }
        }

        internal void FreeKeyInfoStatementHandle(ODBC32.STMT stmt)
        {
            OdbcStatementHandle handle = _keyinfostmt;
            if (null != handle)
            {
                try
                {
                    handle.FreeStatement(stmt);
                }
                catch (Exception e)
                {
                    //
                    if (ADP.IsCatchableExceptionType(e))
                    {
                        _keyinfostmt = null;
                        handle.Dispose();
                    }

                    throw;
                }
            }
        }

        // Get the Descriptor Handle for the current statement
        //
        internal OdbcDescriptorHandle GetDescriptorHandle(ODBC32.SQL_ATTR attribute)
        {
            OdbcDescriptorHandle hdesc = _hdesc;
            if (null == _hdesc)
            {
                _hdesc = hdesc = new OdbcDescriptorHandle(_stmt, attribute);
            }
            return hdesc;
        }

        internal string GetDiagSqlState()
        {
            string sqlstate;
            _stmt.GetDiagnosticField(out sqlstate);
            return sqlstate;
        }

        internal void StatementErrorHandler(ODBC32.RetCode retcode)
        {
            switch (retcode)
            {
                case ODBC32.RetCode.SUCCESS:
                case ODBC32.RetCode.SUCCESS_WITH_INFO:
                    _connection.HandleErrorNoThrow(_stmt, retcode);
                    break;
                default:
                    throw _connection.HandleErrorNoThrow(_stmt, retcode);
            }
        }

        internal void UnbindStmtColumns()
        {
            if (_hasBoundColumns)
            {
                FreeStatementHandle(ODBC32.STMT.UNBIND);
                _hasBoundColumns = false;
            }
        }
    }
}
