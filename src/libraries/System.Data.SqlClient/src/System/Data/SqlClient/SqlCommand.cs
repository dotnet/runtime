// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Data.Common;
using System.Data.Sql;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

using Microsoft.SqlServer.Server;

namespace System.Data.SqlClient
{
    public sealed partial class SqlCommand : DbCommand, ICloneable
    {
        private string _commandText;
        private CommandType _commandType;
        private int _commandTimeout = ADP.DefaultCommandTimeout;
        private UpdateRowSource _updatedRowSource = UpdateRowSource.Both;
        private bool _designTimeInvisible;

        internal SqlDependency _sqlDep;

        private static readonly DiagnosticListener _diagnosticListener = new DiagnosticListener(SqlClientDiagnosticListenerExtensions.DiagnosticListenerName);
        private bool _parentOperationStarted = false;

        // Prepare
        // Against 7.0 Serve a prepare/unprepare requires an extra roundtrip to the server.
        //
        // From 8.0 and above  the preparation can be done as part of the command execution.

        private enum EXECTYPE
        {
            UNPREPARED,         // execute unprepared commands, all server versions (results in sp_execsql call)
            PREPAREPENDING,     // prepare and execute command, 8.0 and above only  (results in sp_prepexec call)
            PREPARED,           // execute prepared commands, all server versions   (results in sp_exec call)
        }

        // _hiddenPrepare
        // On 8.0 and above the Prepared state cannot be left. Once a command is prepared it will always be prepared.
        // A change in parameters, commandtext etc (IsDirty) automatically causes a hidden prepare
        //
        // _inPrepare will be set immediately before the actual prepare is done.
        // The OnReturnValue function will test this flag to determine whether the returned value is a _prepareHandle or something else.
        //
        // _prepareHandle - the handle of a prepared command. Apparently there can be multiple prepared commands at a time - a feature that we do not support yet.

        private bool _inPrepare = false;
        private int _prepareHandle = -1;
        private bool _hiddenPrepare = false;
        private int _preparedConnectionCloseCount = -1;
        private int _preparedConnectionReconnectCount = -1;

        private SqlParameterCollection _parameters;
        private SqlConnection _activeConnection;
        private bool _dirty = false;               // true if the user changes the commandtext or number of parameters after the command is already prepared
        private EXECTYPE _execType = EXECTYPE.UNPREPARED; // by default, assume the user is not sharing a connection so the command has not been prepared
        private _SqlRPC[] _rpcArrayOf1 = null;                // Used for RPC executes

        // cut down on object creation and cache all these
        // cached metadata
        private _SqlMetaDataSet _cachedMetaData;

        // Last TaskCompletionSource for reconnect task - use for cancellation only
        private TaskCompletionSource<object> _reconnectionCompletionSource = null;

#if DEBUG
        internal static int DebugForceAsyncWriteDelay { get; set; }
#endif
        internal bool InPrepare
        {
            get
            {
                return _inPrepare;
            }
        }

        // Cached info for async executions
        private class CachedAsyncState
        {
            private int _cachedAsyncCloseCount = -1;    // value of the connection's CloseCount property when the asyncResult was set; tracks when connections are closed after an async operation
            private TaskCompletionSource<object> _cachedAsyncResult = null;
            private SqlConnection _cachedAsyncConnection = null;  // Used to validate that the connection hasn't changed when end the connection;
            private SqlDataReader _cachedAsyncReader = null;
            private RunBehavior _cachedRunBehavior = RunBehavior.ReturnImmediately;
            private string _cachedSetOptions = null;
            private string _cachedEndMethod = null;

            internal CachedAsyncState()
            {
            }

            internal SqlDataReader CachedAsyncReader
            {
                get { return _cachedAsyncReader; }
            }
            internal RunBehavior CachedRunBehavior
            {
                get { return _cachedRunBehavior; }
            }
            internal string CachedSetOptions
            {
                get { return _cachedSetOptions; }
            }
            internal bool PendingAsyncOperation
            {
                get { return (null != _cachedAsyncResult); }
            }
            internal string EndMethodName
            {
                get { return _cachedEndMethod; }
            }

            internal bool IsActiveConnectionValid(SqlConnection activeConnection)
            {
                return (_cachedAsyncConnection == activeConnection && _cachedAsyncCloseCount == activeConnection.CloseCount);
            }

            internal void ResetAsyncState()
            {
                _cachedAsyncCloseCount = -1;
                _cachedAsyncResult = null;
                if (_cachedAsyncConnection != null)
                {
                    _cachedAsyncConnection.AsyncCommandInProgress = false;
                    _cachedAsyncConnection = null;
                }
                _cachedAsyncReader = null;
                _cachedRunBehavior = RunBehavior.ReturnImmediately;
                _cachedSetOptions = null;
                _cachedEndMethod = null;
            }

            internal void SetActiveConnectionAndResult(TaskCompletionSource<object> completion, string endMethod, SqlConnection activeConnection)
            {
                Debug.Assert(activeConnection != null, "Unexpected null connection argument on SetActiveConnectionAndResult!");
                TdsParser parser = activeConnection?.Parser;
                if ((parser == null) || (parser.State == TdsParserState.Closed) || (parser.State == TdsParserState.Broken))
                {
                    throw ADP.ClosedConnectionError();
                }

                _cachedAsyncCloseCount = activeConnection.CloseCount;
                _cachedAsyncResult = completion;
                if (!parser.MARSOn)
                {
                    if (activeConnection.AsyncCommandInProgress)
                        throw SQL.MARSUnspportedOnConnection();
                }
                _cachedAsyncConnection = activeConnection;

                // Should only be needed for non-MARS, but set anyways.
                _cachedAsyncConnection.AsyncCommandInProgress = true;
                _cachedEndMethod = endMethod;
            }

            internal void SetAsyncReaderState(SqlDataReader ds, RunBehavior runBehavior, string optionSettings)
            {
                _cachedAsyncReader = ds;
                _cachedRunBehavior = runBehavior;
                _cachedSetOptions = optionSettings;
            }
        }

        private CachedAsyncState _cachedAsyncState = null;

        private CachedAsyncState cachedAsyncState
        {
            get
            {
                if (_cachedAsyncState == null)
                {
                    _cachedAsyncState = new CachedAsyncState();
                }
                return _cachedAsyncState;
            }
        }

        // sql reader will pull this value out for each NextResult call.  It is not cumulative
        // _rowsAffected is cumulative for ExecuteNonQuery across all rpc batches
        internal int _rowsAffected = -1; // rows affected by the command

        private SqlNotificationRequest _notification;

        // transaction support
        private SqlTransaction _transaction;

        private StatementCompletedEventHandler _statementCompletedEventHandler;

        private TdsParserStateObject _stateObj; // this is the TDS session we're using.

        // Volatile bool used to synchronize with cancel thread the state change of an executing
        // command going from pre-processing to obtaining a stateObject.  The cancel synchronization
        // we require in the command is only from entering an Execute* API to obtaining a 
        // stateObj.  Once a stateObj is successfully obtained, cancel synchronization is handled
        // by the stateObject.
        private volatile bool _pendingCancel;

        private bool _batchRPCMode;
        private List<_SqlRPC> _RPCList;
        private _SqlRPC[] _SqlRPCBatchArray;
        private List<SqlParameterCollection> _parameterCollectionList;
        private int _currentlyExecutingBatch;


        public SqlCommand() : base()
        {
            GC.SuppressFinalize(this);
        }

        public SqlCommand(string cmdText) : this()
        {
            CommandText = cmdText;
        }

        public SqlCommand(string cmdText, SqlConnection connection) : this()
        {
            CommandText = cmdText;
            Connection = connection;
        }

        public SqlCommand(string cmdText, SqlConnection connection, SqlTransaction transaction) : this()
        {
            CommandText = cmdText;
            Connection = connection;
            Transaction = transaction;
        }

        private SqlCommand(SqlCommand from) : this()
        {
            CommandText = from.CommandText;
            CommandTimeout = from.CommandTimeout;
            CommandType = from.CommandType;
            Connection = from.Connection;
            DesignTimeVisible = from.DesignTimeVisible;
            Transaction = from.Transaction;
            UpdatedRowSource = from.UpdatedRowSource;

            SqlParameterCollection parameters = Parameters;
            foreach (object parameter in from.Parameters)
            {
                parameters.Add((parameter is ICloneable) ? (parameter as ICloneable).Clone() : parameter);
            }
        }

        new public SqlConnection Connection
        {
            get
            {
                return _activeConnection;
            }
            set
            {
                // Don't allow the connection to be changed while in an async operation.
                if (_activeConnection != value && _activeConnection != null)
                { // If new value...
                    if (cachedAsyncState.PendingAsyncOperation)
                    { // If in pending async state, throw.
                        throw SQL.CannotModifyPropertyAsyncOperationInProgress();
                    }
                }

                // Check to see if the currently set transaction has completed.  If so,
                // null out our local reference.
                if (null != _transaction && _transaction.Connection == null)
                {
                    _transaction = null;
                }


                // Command is no longer prepared on new connection, cleanup prepare status
                if (IsPrepared)
                {
                    if (_activeConnection != value && _activeConnection != null)
                    {
                        try
                        {
                            // cleanup
                            Unprepare();
                        }
                        catch (Exception)
                        {
                            // we do not really care about errors in unprepare (may be the old connection went bad)                                        
                        }
                        finally
                        {
                            // clean prepare status (even successful Unprepare does not do that)
                            _prepareHandle = -1;
                            _execType = EXECTYPE.UNPREPARED;
                        }
                    }
                }

                _activeConnection = value;
            }
        }

        override protected DbConnection DbConnection
        {
            get
            {
                return Connection;
            }
            set
            {
                Connection = (SqlConnection)value;
            }
        }

        private SqlInternalConnectionTds InternalTdsConnection
        {
            get
            {
                return (SqlInternalConnectionTds)_activeConnection.InnerConnection;
            }
        }

        public SqlNotificationRequest Notification
        {
            get
            {
                return _notification;
            }
            set
            {
                _sqlDep = null;
                _notification = value;
            }
        }

        internal SqlStatistics Statistics
        {
            get
            {
                if (null != _activeConnection)
                {
                    if (_activeConnection.StatisticsEnabled ||
                        _diagnosticListener.IsEnabled(SqlClientDiagnosticListenerExtensions.SqlAfterExecuteCommand))
                    {
                        return _activeConnection.Statistics;
                    }
                }
                return null;
            }
        }

        new public SqlTransaction Transaction
        {
            get
            {
                // if the transaction object has been zombied, just return null
                if ((null != _transaction) && (null == _transaction.Connection))
                {
                    _transaction = null;
                }
                return _transaction;
            }
            set
            {
                // Don't allow the transaction to be changed while in an async operation.
                if (_transaction != value && _activeConnection != null)
                { // If new value...
                    if (cachedAsyncState.PendingAsyncOperation)
                    { // If in pending async state, throw
                        throw SQL.CannotModifyPropertyAsyncOperationInProgress();
                    }
                }

                _transaction = value;
            }
        }

        override protected DbTransaction DbTransaction
        {
            get
            {
                return Transaction;
            }
            set
            {
                Transaction = (SqlTransaction)value;
            }
        }

        override public string CommandText
        {
            get
            {
                string value = _commandText;
                return ((null != value) ? value : ADP.StrEmpty);
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

        override public int CommandTimeout
        {
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
        {
            if (ADP.DefaultCommandTimeout != _commandTimeout)
            {
                PropertyChanging();
                _commandTimeout = ADP.DefaultCommandTimeout;
            }
        }

        override public CommandType CommandType
        {
            get
            {
                CommandType cmdType = _commandType;
                return ((0 != cmdType) ? cmdType : CommandType.Text);
            }
            set
            {
                if (_commandType != value)
                {
                    switch (value)
                    {
                        case CommandType.Text:
                        case CommandType.StoredProcedure:
                            PropertyChanging();
                            _commandType = value;
                            break;
                        case System.Data.CommandType.TableDirect:
                            throw SQL.NotSupportedCommandType(value);
                        default:
                            throw ADP.InvalidCommandType(value);
                    }
                }
            }
        }

        // @devnote: By default, the cmd object is visible on the design surface (i.e. VS7 Server Tray)
        // to limit the number of components that clutter the design surface,
        // when the DataAdapter design wizard generates the insert/update/delete commands it will
        // set the DesignTimeVisible property to false so that cmds won't appear as individual objects
        public override bool DesignTimeVisible
        {
            get
            {
                return !_designTimeInvisible;
            }
            set
            {
                _designTimeInvisible = !value;
            }
        }

        new public SqlParameterCollection Parameters
        {
            get
            {
                if (null == _parameters)
                {
                    // delay the creation of the SqlParameterCollection
                    // until user actually uses the Parameters property
                    _parameters = new SqlParameterCollection();
                }
                return _parameters;
            }
        }

        override protected DbParameterCollection DbParameterCollection
        {
            get
            {
                return Parameters;
            }
        }

        override public UpdateRowSource UpdatedRowSource
        {
            get
            {
                return _updatedRowSource;
            }
            set
            {
                switch (value)
                {
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

        public event StatementCompletedEventHandler StatementCompleted
        {
            add
            {
                _statementCompletedEventHandler += value;
            }
            remove
            {
                _statementCompletedEventHandler -= value;
            }
        }

        internal void OnStatementCompleted(int recordCount)
        {
            if (0 <= recordCount)
            {
                StatementCompletedEventHandler handler = _statementCompletedEventHandler;
                if (null != handler)
                {
                    try
                    {
                        handler(this, new StatementCompletedEventArgs(recordCount));
                    }
                    catch (Exception e)
                    {
                        if (!ADP.IsCatchableOrSecurityExceptionType(e))
                        {
                            throw;
                        }
                    }
                }
            }
        }

        private void PropertyChanging()
        { // also called from SqlParameterCollection
            this.IsDirty = true;
        }

        override public void Prepare()
        {
            // Reset _pendingCancel upon entry into any Execute - used to synchronize state
            // between entry into Execute* API and the thread obtaining the stateObject.
            _pendingCancel = false;


            SqlStatistics statistics = null;
            statistics = SqlStatistics.StartTimer(Statistics);

            // only prepare if batch with parameters
            if (
                this.IsPrepared && !this.IsDirty
                || (this.CommandType == CommandType.StoredProcedure)
                || (
                        (System.Data.CommandType.Text == this.CommandType)
                        && (0 == GetParameterCount(_parameters))
                    )

            )
            {
                if (null != Statistics)
                {
                    Statistics.SafeIncrement(ref Statistics._prepares);
                }
                _hiddenPrepare = false;
            }
            else
            {
                // Validate the command outside of the try\catch to avoid putting the _stateObj on error
                ValidateCommand(async: false);

                bool processFinallyBlock = true;
                try
                {
                    // NOTE: The state object isn't actually needed for this, but it is still here for back-compat (since it does a bunch of checks)
                    GetStateObject();

                    // Loop through parameters ensuring that we do not have unspecified types, sizes, scales, or precisions
                    if (null != _parameters)
                    {
                        int count = _parameters.Count;
                        for (int i = 0; i < count; ++i)
                        {
                            _parameters[i].Prepare(this);
                        }
                    }

                    InternalPrepare();
                }
                catch (Exception e)
                {
                    processFinallyBlock = ADP.IsCatchableExceptionType(e);
                    throw;
                }
                finally
                {
                    if (processFinallyBlock)
                    {
                        _hiddenPrepare = false; // The command is now officially prepared

                        ReliablePutStateObject();
                    }
                }
            }

            SqlStatistics.StopTimer(statistics);
        }

        private void InternalPrepare()
        {
            if (this.IsDirty)
            {
                Debug.Assert(_cachedMetaData == null || !_dirty, "dirty query should not have cached metadata!"); // can have cached metadata if dirty because of parameters
                //
                // someone changed the command text or the parameter schema so we must unprepare the command
                //
                this.Unprepare();
                this.IsDirty = false;
            }
            Debug.Assert(_execType != EXECTYPE.PREPARED, "Invalid attempt to Prepare already Prepared command!");
            Debug.Assert(_activeConnection != null, "must have an open connection to Prepare");
            Debug.Assert(null != _stateObj, "TdsParserStateObject should not be null");
            Debug.Assert(null != _stateObj.Parser, "TdsParser class should not be null in Command.Execute!");
            Debug.Assert(_stateObj.Parser == _activeConnection.Parser, "stateobject parser not same as connection parser");
            Debug.Assert(false == _inPrepare, "Already in Prepare cycle, this.inPrepare should be false!");

            // remember that the user wants to do a prepare but don't actually do an rpc
            _execType = EXECTYPE.PREPAREPENDING;
            // Note the current close count of the connection - this will tell us if the connection has been closed between calls to Prepare() and Execute
            _preparedConnectionCloseCount = _activeConnection.CloseCount;
            _preparedConnectionReconnectCount = _activeConnection.ReconnectCount;

            if (null != Statistics)
            {
                Statistics.SafeIncrement(ref Statistics._prepares);
            }
        }

        // SqlInternalConnectionTds needs to be able to unprepare a statement
        internal void Unprepare()
        {
            Debug.Assert(true == IsPrepared, "Invalid attempt to Unprepare a non-prepared command!");
            Debug.Assert(_activeConnection != null, "must have an open connection to UnPrepare");
            Debug.Assert(false == _inPrepare, "_inPrepare should be false!");
            _execType = EXECTYPE.PREPAREPENDING;
            // Don't zero out the handle because we'll pass it in to sp_prepexec on the next prepare
            // Unless the close count isn't the same as when we last prepared
            if ((_activeConnection.CloseCount != _preparedConnectionCloseCount) || (_activeConnection.ReconnectCount != _preparedConnectionReconnectCount))
            {
                // reset our handle
                _prepareHandle = -1;
            }

            _cachedMetaData = null;
        }


        // Cancel is supposed to be multi-thread safe.
        // It doesn't make sense to verify the connection exists or that it is open during cancel
        // because immediately after checkin the connection can be closed or removed via another thread.
        //
        override public void Cancel()
        {
            SqlStatistics statistics = null;
            try
            {
                statistics = SqlStatistics.StartTimer(Statistics);

                // If we are in reconnect phase simply cancel the waiting task
                var reconnectCompletionSource = _reconnectionCompletionSource;
                if (reconnectCompletionSource != null)
                {
                    if (reconnectCompletionSource.TrySetCanceled())
                    {
                        return;
                    }
                }

                // the pending data flag means that we are awaiting a response or are in the middle of processing a response
                // if we have no pending data, then there is nothing to cancel
                // if we have pending data, but it is not a result of this command, then we don't cancel either.  Note that
                // this model is implementable because we only allow one active command at any one time.  This code
                // will have to change we allow multiple outstanding batches
                if (null == _activeConnection)
                {
                    return;
                }
                SqlInternalConnectionTds connection = (_activeConnection.InnerConnection as SqlInternalConnectionTds);
                if (null == connection)
                {  // Fail with out locking
                    return;
                }

                // The lock here is to protect against the command.cancel / connection.close race condition
                // The SqlInternalConnectionTds is set to OpenBusy during close, once this happens the cast below will fail and 
                // the command will no longer be cancelable.  It might be desirable to be able to cancel the close operation, but this is
                // outside of the scope of Whidbey RTM.  See (SqlConnection::Close) for other lock.
                lock (connection)
                {
                    if (connection != (_activeConnection.InnerConnection as SqlInternalConnectionTds))
                    { // make sure the connection held on the active connection is what we have stored in our temp connection variable, if not between getting "connection" and taking the lock, the connection has been closed
                        return;
                    }

                    TdsParser parser = connection.Parser;
                    if (null == parser)
                    {
                        return;
                    }


                    if (!_pendingCancel)
                    { // Do nothing if already pending.
                      // Before attempting actual cancel, set the _pendingCancel flag to false.
                      // This denotes to other thread before obtaining stateObject from the
                      // session pool that there is another thread wishing to cancel.
                      // The period in question is between entering the ExecuteAPI and obtaining 
                      // a stateObject.
                        _pendingCancel = true;

                        TdsParserStateObject stateObj = _stateObj;
                        if (null != stateObj)
                        {
                            stateObj.Cancel(this);
                        }
                        else
                        {
                            SqlDataReader reader = connection.FindLiveReader(this);
                            if (reader != null)
                            {
                                reader.Cancel(this);
                            }
                        }
                    }
                }
            }
            finally
            {
                SqlStatistics.StopTimer(statistics);
            }
        }

        new public SqlParameter CreateParameter()
        {
            return new SqlParameter();
        }

        override protected DbParameter CreateDbParameter()
        {
            return CreateParameter();
        }

        override protected void Dispose(bool disposing)
        {
            if (disposing)
            { // release managed objects
                _cachedMetaData = null;
            }
            // release unmanaged objects
            base.Dispose(disposing);
        }

        override public object ExecuteScalar()
        {
            // Reset _pendingCancel upon entry into any Execute - used to synchronize state
            // between entry into Execute* API and the thread obtaining the stateObject.
            _pendingCancel = false;

            Guid operationId = _diagnosticListener.WriteCommandBefore(this);

            SqlStatistics statistics = null;

            Exception e = null;
            try
            {
                statistics = SqlStatistics.StartTimer(Statistics);
                SqlDataReader ds;
                ds = RunExecuteReader(0, RunBehavior.ReturnImmediately, returnStream: true);
                return CompleteExecuteScalar(ds, false);
            }
            catch (Exception ex)
            {
                e = ex;
                throw;
            }
            finally
            {
                SqlStatistics.StopTimer(statistics);

                if (e != null)
                {
                    _diagnosticListener.WriteCommandError(operationId, this, e);
                }
                else
                {
                    _diagnosticListener.WriteCommandAfter(operationId, this);
                }
            }
        }

        private object CompleteExecuteScalar(SqlDataReader ds, bool returnSqlValue)
        {
            object retResult = null;

            try
            {
                if (ds.Read())
                {
                    if (ds.FieldCount > 0)
                    {
                        if (returnSqlValue)
                        {
                            retResult = ds.GetSqlValue(0);
                        }
                        else
                        {
                            retResult = ds.GetValue(0);
                        }
                    }
                }
            }
            finally
            {
                // clean off the wire
                ds.Close();
            }

            return retResult;
        }

        override public int ExecuteNonQuery()
        {
            // Reset _pendingCancel upon entry into any Execute - used to synchronize state
            // between entry into Execute* API and the thread obtaining the stateObject.
            _pendingCancel = false;

            Guid operationId = _diagnosticListener.WriteCommandBefore(this);

            SqlStatistics statistics = null;

            Exception e = null;
            try
            {
                statistics = SqlStatistics.StartTimer(Statistics);
                InternalExecuteNonQuery(completion: null, sendToPipe: false, timeout: CommandTimeout);
                return _rowsAffected;
            }
            catch (Exception ex)
            {
                e = ex;
                throw;
            }
            finally
            {
                SqlStatistics.StopTimer(statistics);

                if (e != null)
                {
                    _diagnosticListener.WriteCommandError(operationId, this, e);
                }
                else
                {
                    _diagnosticListener.WriteCommandAfter(operationId, this);
                }
            }
        }

        public IAsyncResult BeginExecuteNonQuery()
        {
            // BeginExecuteNonQuery will track ExecutionTime for us
            return BeginExecuteNonQuery(null, null);
        }

        public IAsyncResult BeginExecuteNonQuery(AsyncCallback callback, object stateObject)
        {
            // Reset _pendingCancel upon entry into any Execute - used to synchronize state
            // between entry into Execute* API and the thread obtaining the stateObject.
            _pendingCancel = false;

            ValidateAsyncCommand(); // Special case - done outside of try/catches to prevent putting a stateObj
                                    // back into pool when we should not.

            SqlStatistics statistics = null;
            try
            {
                statistics = SqlStatistics.StartTimer(Statistics);
                TaskCompletionSource<object> completion = new TaskCompletionSource<object>(stateObject);

                try
                { // InternalExecuteNonQuery already has reliability block, but if failure will not put stateObj back into pool.
                    Task execNQ = InternalExecuteNonQuery(completion, false, CommandTimeout, asyncWrite: true);

                    // must finish caching information before ReadSni which can activate the callback before returning
                    cachedAsyncState.SetActiveConnectionAndResult(completion, nameof(EndExecuteNonQuery), _activeConnection);
                    if (execNQ != null)
                    {
                        AsyncHelper.ContinueTask(execNQ, completion, () => BeginExecuteNonQueryInternalReadStage(completion));
                    }
                    else
                    {
                        BeginExecuteNonQueryInternalReadStage(completion);
                    }
                }
                catch (Exception e)
                {
                    if (!ADP.IsCatchableOrSecurityExceptionType(e))
                    {
                        // If not catchable - the connection has already been caught and doomed in RunExecuteReader.
                        throw;
                    }

                    // For async, RunExecuteReader will never put the stateObj back into the pool, so do so now.
                    ReliablePutStateObject();
                    throw;
                }

                // Add callback after work is done to avoid overlapping Begin\End methods
                if (callback != null)
                {
                    completion.Task.ContinueWith((t) => callback(t), TaskScheduler.Default);
                }

                return completion.Task;
            }
            finally
            {
                SqlStatistics.StopTimer(statistics);
            }
        }

        private void BeginExecuteNonQueryInternalReadStage(TaskCompletionSource<object> completion)
        {
            // Read SNI does not have catches for async exceptions, handle here.
            try
            {
                _stateObj.ReadSni(completion);
            }
            catch (Exception)
            {
                // Similarly, if an exception occurs put the stateObj back into the pool.
                // and reset async cache information to allow a second async execute
                if (null != _cachedAsyncState)
                {
                    _cachedAsyncState.ResetAsyncState();
                }
                ReliablePutStateObject();
                throw;
            }
        }

        private void VerifyEndExecuteState(Task completionTask, String endMethod)
        {
            Debug.Assert(completionTask != null);

            if (completionTask.IsCanceled)
            {
                if (_stateObj != null)
                {
                    _stateObj.Parser.State = TdsParserState.Broken; // We failed to respond to attention, we have to quit!
                    _stateObj.Parser.Connection.BreakConnection();
                    _stateObj.Parser.ThrowExceptionAndWarning(_stateObj);
                }
                else
                {
                    Debug.Assert(_reconnectionCompletionSource == null || _reconnectionCompletionSource.Task.IsCanceled, "ReconnectCompletionSource should be null or cancelled");
                    throw SQL.CR_ReconnectionCancelled();
                }
            }
            else if (completionTask.IsFaulted)
            {
                throw completionTask.Exception.InnerException;
            }
            if (cachedAsyncState.EndMethodName == null)
            {
                throw ADP.MethodCalledTwice(endMethod);
            }
            if (endMethod != cachedAsyncState.EndMethodName)
            {
                throw ADP.MismatchedAsyncResult(cachedAsyncState.EndMethodName, endMethod);
            }
            if ((_activeConnection.State != ConnectionState.Open) || (!cachedAsyncState.IsActiveConnectionValid(_activeConnection)))
            {
                // If the connection is not 'valid' then it was closed while we were executing
                throw ADP.ClosedConnectionError();
            }
        }

        private void WaitForAsyncResults(IAsyncResult asyncResult)
        {
            Task completionTask = (Task)asyncResult;
            if (!asyncResult.IsCompleted)
            {
                asyncResult.AsyncWaitHandle.WaitOne();
            }
            _stateObj._networkPacketTaskSource = null;
            _activeConnection.GetOpenTdsConnection().DecrementAsyncCount();
        }

        private void ThrowIfReconnectionHasBeenCanceled()
        {
            if (_stateObj == null)
            {
                var reconnectionCompletionSource = _reconnectionCompletionSource;
                if (reconnectionCompletionSource != null && reconnectionCompletionSource.Task.IsCanceled)
                {
                    throw SQL.CR_ReconnectionCancelled();
                }
            }
        }

        public int EndExecuteNonQuery(IAsyncResult asyncResult)
        {
            Exception asyncException = ((Task)asyncResult).Exception;
            if (asyncException != null)
            {
                // Leftover exception from the Begin...InternalReadStage
                if (cachedAsyncState != null)
                {
                    cachedAsyncState.ResetAsyncState();
                }
                ReliablePutStateObject();
                throw asyncException.InnerException;
            }
            else
            {
                ThrowIfReconnectionHasBeenCanceled();
                // lock on _stateObj prevents races with close/cancel.
                lock (_stateObj)
                {
                    return EndExecuteNonQueryInternal(asyncResult);
                }
            }
        }

        private int EndExecuteNonQueryInternal(IAsyncResult asyncResult)
        {
            SqlStatistics statistics = null;

            try
            {
                statistics = SqlStatistics.StartTimer(Statistics);
                VerifyEndExecuteState((Task)asyncResult, nameof(EndExecuteNonQuery));
                WaitForAsyncResults(asyncResult);

                bool processFinallyBlock = true;
                try
                {
                    CheckThrowSNIException();

                    // only send over SQL Batch command if we are not a stored proc and have no parameters
                    if ((System.Data.CommandType.Text == this.CommandType) && (0 == GetParameterCount(_parameters)))
                    {
                        try
                        {
                            bool dataReady;
                            Debug.Assert(_stateObj._syncOverAsync, "Should not attempt pends in a synchronous call");
                            bool result = _stateObj.Parser.TryRun(RunBehavior.UntilDone, this, null, null, _stateObj, out dataReady);
                            if (!result) { throw SQL.SynchronousCallMayNotPend(); }
                        }
                        finally
                        {
                            cachedAsyncState.ResetAsyncState();
                        }
                    }
                    else
                    { // otherwise, use a full-fledged execute that can handle params and stored procs
                        SqlDataReader reader = CompleteAsyncExecuteReader();
                        if (null != reader)
                        {
                            reader.Close();
                        }
                    }
                }
                catch (Exception e)
                {
                    processFinallyBlock = ADP.IsCatchableExceptionType(e);
                    throw;
                }
                finally
                {
                    if (processFinallyBlock)
                    {
                        PutStateObject();
                    }
                }

                Debug.Assert(null == _stateObj, "non-null state object in EndExecuteNonQuery");
                return _rowsAffected;
            }
            catch (Exception e)
            {
                if (cachedAsyncState != null)
                {
                    cachedAsyncState.ResetAsyncState();
                };
                if (ADP.IsCatchableExceptionType(e))
                {
                    ReliablePutStateObject();
                };
                throw;
            }
            finally
            {
                SqlStatistics.StopTimer(statistics);
            }
        }

        private Task InternalExecuteNonQuery(TaskCompletionSource<object> completion, bool sendToPipe, int timeout, bool asyncWrite = false, [CallerMemberName] string methodName = "")
        {
            bool async = (null != completion);

            SqlStatistics statistics = Statistics;
            _rowsAffected = -1;

            // this function may throw for an invalid connection
            // returns false for empty command text
            ValidateCommand(async, methodName);

            CheckNotificationStateAndAutoEnlist(); // Only call after validate - requires non null connection!

            Task task = null;

            // only send over SQL Batch command if we are not a stored proc and have no parameters and not in batch RPC mode
            if ((System.Data.CommandType.Text == this.CommandType) && (0 == GetParameterCount(_parameters)))
            {
                Debug.Assert(!sendToPipe, "trying to send non-context command to pipe");
                if (null != statistics)
                {
                    if (!this.IsDirty && this.IsPrepared)
                    {
                        statistics.SafeIncrement(ref statistics._preparedExecs);
                    }
                    else
                    {
                        statistics.SafeIncrement(ref statistics._unpreparedExecs);
                    }
                }

                task = RunExecuteNonQueryTds(methodName, async, timeout, asyncWrite);
            }
            else
            { // otherwise, use a full-fledged execute that can handle params and stored procs
                Debug.Assert(!sendToPipe, "trying to send non-context command to pipe");
                SqlDataReader reader = RunExecuteReader(0, RunBehavior.UntilDone, false, completion, timeout, out task, asyncWrite, methodName);
                if (null != reader)
                {
                    if (task != null)
                    {
                        task = AsyncHelper.CreateContinuationTask(task, () => reader.Close());
                    }
                    else
                    {
                        reader.Close();
                    }
                }
            }
            Debug.Assert(async || null == _stateObj, "non-null state object in InternalExecuteNonQuery");
            return task;
        }

        public XmlReader ExecuteXmlReader()
        {
            // Reset _pendingCancel upon entry into any Execute - used to synchronize state
            // between entry into Execute* API and the thread obtaining the stateObject.
            _pendingCancel = false;

            Guid operationId = _diagnosticListener.WriteCommandBefore(this);

            SqlStatistics statistics = null;

            Exception e = null;
            try
            {
                statistics = SqlStatistics.StartTimer(Statistics);

                // use the reader to consume metadata
                SqlDataReader ds;
                ds = RunExecuteReader(CommandBehavior.SequentialAccess, RunBehavior.ReturnImmediately, returnStream: true);
                return CompleteXmlReader(ds);
            }
            catch (Exception ex)
            {
                e = ex;
                throw;
            }
            finally
            {
                SqlStatistics.StopTimer(statistics);

                if (e != null)
                {
                    _diagnosticListener.WriteCommandError(operationId, this, e);
                }
                else
                {
                    _diagnosticListener.WriteCommandAfter(operationId, this);
                }
            }
        }


        private IAsyncResult BeginExecuteXmlReader(AsyncCallback callback, object stateObject)
        {
            // Reset _pendingCancel upon entry into any Execute - used to synchronize state
            // between entry into Execute* API and the thread obtaining the stateObject.
            _pendingCancel = false;

            ValidateAsyncCommand(); // Special case - done outside of try/catches to prevent putting a stateObj
                                    // back into pool when we should not.

            SqlStatistics statistics = null;
            try
            {
                statistics = SqlStatistics.StartTimer(Statistics);
                TaskCompletionSource<object> completion = new TaskCompletionSource<object>(stateObject);

                Task writeTask;
                try
                { // InternalExecuteNonQuery already has reliability block, but if failure will not put stateObj back into pool.
                    RunExecuteReader(CommandBehavior.SequentialAccess, RunBehavior.ReturnImmediately, true, completion, CommandTimeout, out writeTask, asyncWrite: true);
                }
                catch (Exception e)
                {
                    if (!ADP.IsCatchableOrSecurityExceptionType(e))
                    {
                        // If not catchable - the connection has already been caught and doomed in RunExecuteReader.
                        throw;
                    }

                    // For async, RunExecuteReader will never put the stateObj back into the pool, so do so now.
                    ReliablePutStateObject();
                    throw;
                }

                // must finish caching information before ReadSni which can activate the callback before returning
                cachedAsyncState.SetActiveConnectionAndResult(completion, nameof(EndExecuteXmlReader), _activeConnection);
                if (writeTask != null)
                {
                    AsyncHelper.ContinueTask(writeTask, completion, () => BeginExecuteXmlReaderInternalReadStage(completion));
                }
                else
                {
                    BeginExecuteXmlReaderInternalReadStage(completion);
                }

                // Add callback after work is done to avoid overlapping Begin\End methods
                if (callback != null)
                {
                    completion.Task.ContinueWith((t) => callback(t), TaskScheduler.Default);
                }
                return completion.Task;
            }
            finally
            {
                SqlStatistics.StopTimer(statistics);
            }
        }

        private void BeginExecuteXmlReaderInternalReadStage(TaskCompletionSource<object> completion)
        {
            Debug.Assert(completion != null, "Completion source should not be null");
            // Read SNI does not have catches for async exceptions, handle here.
            try
            {
                _stateObj.ReadSni(completion);
            }
            catch (Exception e)
            {
                // Similarly, if an exception occurs put the stateObj back into the pool.
                // and reset async cache information to allow a second async execute
                if (null != _cachedAsyncState)
                {
                    _cachedAsyncState.ResetAsyncState();
                }
                ReliablePutStateObject();
                completion.TrySetException(e);
            }
        }


        private XmlReader EndExecuteXmlReader(IAsyncResult asyncResult)
        {
            Exception asyncException = ((Task)asyncResult).Exception;
            if (asyncException != null)
            {
                // Leftover exception from the Begin...InternalReadStage
                if (cachedAsyncState != null)
                {
                    cachedAsyncState.ResetAsyncState();
                }
                ReliablePutStateObject();
                throw asyncException.InnerException;
            }
            else
            {
                ThrowIfReconnectionHasBeenCanceled();
                // lock on _stateObj prevents races with close/cancel.
                lock (_stateObj)
                {
                    return EndExecuteXmlReaderInternal(asyncResult);
                }
            }
        }

        private XmlReader EndExecuteXmlReaderInternal(IAsyncResult asyncResult)
        {
            try
            {
                return CompleteXmlReader(InternalEndExecuteReader(asyncResult, nameof(EndExecuteXmlReader)), async: true);
            }
            catch (Exception e)
            {
                if (cachedAsyncState != null)
                {
                    cachedAsyncState.ResetAsyncState();
                };
                if (ADP.IsCatchableExceptionType(e))
                {
                    ReliablePutStateObject();
                };
                throw;
            }
        }

        private XmlReader CompleteXmlReader(SqlDataReader ds, bool async = false)
        {
            XmlReader xr = null;

            SmiExtendedMetaData[] md = ds.GetInternalSmiMetaData();
            bool isXmlCapable = (null != md && md.Length == 1 && (md[0].SqlDbType == SqlDbType.NText
                                                         || md[0].SqlDbType == SqlDbType.NVarChar
                                                         || md[0].SqlDbType == SqlDbType.Xml));

            if (isXmlCapable)
            {
                try
                {
                    SqlStream sqlBuf = new SqlStream(ds, true /*addByteOrderMark*/, (md[0].SqlDbType == SqlDbType.Xml) ? false : true /*process all rows*/);
                    xr = sqlBuf.ToXmlReader(async);
                }
                catch (Exception e)
                {
                    if (ADP.IsCatchableExceptionType(e))
                    {
                        ds.Close();
                    }
                    throw;
                }
            }
            if (xr == null)
            {
                ds.Close();
                throw SQL.NonXmlResult();
            }
            return xr;
        }


        override protected DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            return ExecuteReader(behavior);
        }

        new public SqlDataReader ExecuteReader()
        {
            SqlStatistics statistics = null;
            try
            {
                statistics = SqlStatistics.StartTimer(Statistics);
                return ExecuteReader(CommandBehavior.Default);
            }
            finally
            {
                SqlStatistics.StopTimer(statistics);
            }
        }

        new public SqlDataReader ExecuteReader(CommandBehavior behavior)
        {
            // Reset _pendingCancel upon entry into any Execute - used to synchronize state
            // between entry into Execute* API and the thread obtaining the stateObject.
            _pendingCancel = false;

            Guid operationId = _diagnosticListener.WriteCommandBefore(this);

            SqlStatistics statistics = null;

            Exception e = null;
            try
            {
                statistics = SqlStatistics.StartTimer(Statistics);
                return RunExecuteReader(behavior, RunBehavior.ReturnImmediately, returnStream: true);
            }
            catch (Exception ex)
            {
                e = ex;
                throw;
            }
            finally
            {
                SqlStatistics.StopTimer(statistics);

                if (e != null)
                {
                    _diagnosticListener.WriteCommandError(operationId, this, e);
                }
                else
                {
                    _diagnosticListener.WriteCommandAfter(operationId, this);
                }
            }
        }


        internal SqlDataReader EndExecuteReader(IAsyncResult asyncResult)
        {
            Exception asyncException = ((Task)asyncResult).Exception;
            if (asyncException != null)
            {
                // Leftover exception from the Begin...InternalReadStage
                if (cachedAsyncState != null)
                {
                    cachedAsyncState.ResetAsyncState();
                }
                ReliablePutStateObject();
                throw asyncException.InnerException;
            }
            else
            {
                ThrowIfReconnectionHasBeenCanceled();
                // lock on _stateObj prevents races with close/cancel.
                lock (_stateObj)
                {
                    return EndExecuteReaderInternal(asyncResult);
                }
            }
        }

        private SqlDataReader EndExecuteReaderInternal(IAsyncResult asyncResult)
        {
            SqlStatistics statistics = null;
            try
            {
                statistics = SqlStatistics.StartTimer(Statistics);
                return InternalEndExecuteReader(asyncResult, nameof(EndExecuteReader));
            }
            catch (Exception e)
            {
                if (cachedAsyncState != null)
                {
                    cachedAsyncState.ResetAsyncState();
                };
                if (ADP.IsCatchableExceptionType(e))
                {
                    ReliablePutStateObject();
                };
                throw;
            }
            finally
            {
                SqlStatistics.StopTimer(statistics);
            }
        }

        internal IAsyncResult BeginExecuteReader(CommandBehavior behavior, AsyncCallback callback, object stateObject)
        {
            // Reset _pendingCancel upon entry into any Execute - used to synchronize state
            // between entry into Execute* API and the thread obtaining the stateObject.
            _pendingCancel = false;

            SqlStatistics statistics = null;
            try
            {
                statistics = SqlStatistics.StartTimer(Statistics);
                TaskCompletionSource<object> completion = new TaskCompletionSource<object>(stateObject);

                ValidateAsyncCommand(); // Special case - done outside of try/catches to prevent putting a stateObj
                                        // back into pool when we should not.

                Task writeTask = null;
                try
                { // InternalExecuteNonQuery already has reliability block, but if failure will not put stateObj back into pool.
                    RunExecuteReader(behavior, RunBehavior.ReturnImmediately, true, completion, CommandTimeout, out writeTask, asyncWrite: true);
                }
                catch (Exception e)
                {
                    if (!ADP.IsCatchableOrSecurityExceptionType(e))
                    {
                        // If not catchable - the connection has already been caught and doomed in RunExecuteReader.
                        throw;
                    }

                    // For async, RunExecuteReader will never put the stateObj back into the pool, so do so now.
                    ReliablePutStateObject();
                    throw;
                }

                // must finish caching information before ReadSni which can activate the callback before returning
                cachedAsyncState.SetActiveConnectionAndResult(completion, nameof(EndExecuteReader), _activeConnection);
                if (writeTask != null)
                {
                    AsyncHelper.ContinueTask(writeTask, completion, () => BeginExecuteReaderInternalReadStage(completion));
                }
                else
                {
                    BeginExecuteReaderInternalReadStage(completion);
                }

                // Add callback after work is done to avoid overlapping Begin\End methods
                if (callback != null)
                {
                    completion.Task.ContinueWith((t) => callback(t), TaskScheduler.Default);
                }
                return completion.Task;
            }
            finally
            {
                SqlStatistics.StopTimer(statistics);
            }
        }

        private void BeginExecuteReaderInternalReadStage(TaskCompletionSource<object> completion)
        {
            Debug.Assert(completion != null, "CompletionSource should not be null");
            // Read SNI does not have catches for async exceptions, handle here.
            try
            {
                _stateObj.ReadSni(completion);
            }
            catch (Exception e)
            {
                // Similarly, if an exception occurs put the stateObj back into the pool.
                // and reset async cache information to allow a second async execute
                if (null != _cachedAsyncState)
                {
                    _cachedAsyncState.ResetAsyncState();
                }
                ReliablePutStateObject();
                completion.TrySetException(e);
            }
        }

        private SqlDataReader InternalEndExecuteReader(IAsyncResult asyncResult, string endMethod)
        {
            VerifyEndExecuteState((Task)asyncResult, endMethod);
            WaitForAsyncResults(asyncResult);

            CheckThrowSNIException();

            SqlDataReader reader = CompleteAsyncExecuteReader();
            Debug.Assert(null == _stateObj, "non-null state object in InternalEndExecuteReader");
            return reader;
        }

        public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        {
            Guid operationId = _diagnosticListener.WriteCommandBefore(this);

            TaskCompletionSource<int> source = new TaskCompletionSource<int>();

            CancellationTokenRegistration registration = new CancellationTokenRegistration();
            if (cancellationToken.CanBeCanceled)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    source.SetCanceled();
                    return source.Task;
                }
                registration = cancellationToken.Register(s => ((SqlCommand)s).CancelIgnoreFailure(), this);
            }

            Task<int> returnedTask = source.Task;
            try
            {
                RegisterForConnectionCloseNotification(ref returnedTask);

                Task<int>.Factory.FromAsync(BeginExecuteNonQuery, EndExecuteNonQuery, null).ContinueWith((t) =>
                {
                    registration.Dispose();
                    if (t.IsFaulted)
                    {
                        Exception e = t.Exception.InnerException;
                        _diagnosticListener.WriteCommandError(operationId, this, e);
                        source.SetException(e);
                    }
                    else
                    {
                        if (t.IsCanceled)
                        {
                            source.SetCanceled();
                        }
                        else
                        {
                            source.SetResult(t.Result);
                        }
                        _diagnosticListener.WriteCommandAfter(operationId, this);
                    }
                }, TaskScheduler.Default);
            }
            catch (Exception e)
            {
                _diagnosticListener.WriteCommandError(operationId, this, e);
                source.SetException(e);
            }

            return returnedTask;
        }

        protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        {
            return ExecuteReaderAsync(behavior, cancellationToken).ContinueWith<DbDataReader>((result) =>
            {
                if (result.IsFaulted)
                {
                    throw result.Exception.InnerException;
                }
                return result.Result;
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.NotOnCanceled, TaskScheduler.Default);
        }

        new public Task<SqlDataReader> ExecuteReaderAsync()
        {
            return ExecuteReaderAsync(CommandBehavior.Default, CancellationToken.None);
        }

        new public Task<SqlDataReader> ExecuteReaderAsync(CommandBehavior behavior)
        {
            return ExecuteReaderAsync(behavior, CancellationToken.None);
        }

        new public Task<SqlDataReader> ExecuteReaderAsync(CancellationToken cancellationToken)
        {
            return ExecuteReaderAsync(CommandBehavior.Default, cancellationToken);
        }

        new public Task<SqlDataReader> ExecuteReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        {
            Guid operationId = default(Guid);
            if (!_parentOperationStarted)
                operationId = _diagnosticListener.WriteCommandBefore(this);

            TaskCompletionSource<SqlDataReader> source = new TaskCompletionSource<SqlDataReader>();

            CancellationTokenRegistration registration = new CancellationTokenRegistration();
            if (cancellationToken.CanBeCanceled)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    source.SetCanceled();
                    return source.Task;
                }
                registration = cancellationToken.Register(s => ((SqlCommand)s).CancelIgnoreFailure(), this);
            }

            Task<SqlDataReader> returnedTask = source.Task;
            try
            {
                RegisterForConnectionCloseNotification(ref returnedTask);

                Task<SqlDataReader>.Factory.FromAsync(BeginExecuteReader, EndExecuteReader, behavior, null).ContinueWith((t) =>
                {
                    registration.Dispose();
                    if (t.IsFaulted)
                    {
                        Exception e = t.Exception.InnerException;
                        if (!_parentOperationStarted)
                            _diagnosticListener.WriteCommandError(operationId, this, e);
                        source.SetException(e);
                    }
                    else
                    {
                        if (t.IsCanceled)
                        {
                            source.SetCanceled();
                        }
                        else
                        {
                            source.SetResult(t.Result);
                        }
                        if (!_parentOperationStarted)
                            _diagnosticListener.WriteCommandAfter(operationId, this);
                    }
                }, TaskScheduler.Default);
            }
            catch (Exception e)
            {
                if (!_parentOperationStarted)
                    _diagnosticListener.WriteCommandError(operationId, this, e);

                source.SetException(e);
            }

            return returnedTask;
        }

        public override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            _parentOperationStarted = true;
            Guid operationId = _diagnosticListener.WriteCommandBefore(this);

            return ExecuteReaderAsync(cancellationToken).ContinueWith((executeTask) =>
            {
                TaskCompletionSource<object> source = new TaskCompletionSource<object>();
                if (executeTask.IsCanceled)
                {
                    source.SetCanceled();
                }
                else if (executeTask.IsFaulted)
                {
                    _diagnosticListener.WriteCommandError(operationId, this, executeTask.Exception.InnerException);
                    source.SetException(executeTask.Exception.InnerException);
                }
                else
                {
                    SqlDataReader reader = executeTask.Result;
                    reader.ReadAsync(cancellationToken).ContinueWith((readTask) =>
                    {
                        try
                        {
                            if (readTask.IsCanceled)
                            {
                                reader.Dispose();
                                source.SetCanceled();
                            }
                            else if (readTask.IsFaulted)
                            {
                                reader.Dispose();
                                _diagnosticListener.WriteCommandError(operationId, this, readTask.Exception.InnerException);
                                source.SetException(readTask.Exception.InnerException);
                            }
                            else
                            {
                                Exception exception = null;
                                object result = null;
                                try
                                {
                                    bool more = readTask.Result;
                                    if (more && reader.FieldCount > 0)
                                    {
                                        try
                                        {
                                            result = reader.GetValue(0);
                                        }
                                        catch (Exception e)
                                        {
                                            exception = e;
                                        }
                                    }
                                }
                                finally
                                {
                                    reader.Dispose();
                                }
                                if (exception != null)
                                {
                                    _diagnosticListener.WriteCommandError(operationId, this, exception);
                                    source.SetException(exception);
                                }
                                else
                                {
                                    _diagnosticListener.WriteCommandAfter(operationId, this);
                                    source.SetResult(result);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            // exception thrown by Dispose...
                            source.SetException(e);
                        }
                    }, TaskScheduler.Default);
                }
                _parentOperationStarted = false;
                return source.Task;
            }, TaskScheduler.Default).Unwrap();
        }

        public Task<XmlReader> ExecuteXmlReaderAsync()
        {
            return ExecuteXmlReaderAsync(CancellationToken.None);
        }

        public Task<XmlReader> ExecuteXmlReaderAsync(CancellationToken cancellationToken)
        {
            Guid operationId = _diagnosticListener.WriteCommandBefore(this);

            TaskCompletionSource<XmlReader> source = new TaskCompletionSource<XmlReader>();

            CancellationTokenRegistration registration = new CancellationTokenRegistration();
            if (cancellationToken.CanBeCanceled)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    source.SetCanceled();
                    return source.Task;
                }
                registration = cancellationToken.Register(s => ((SqlCommand)s).CancelIgnoreFailure(), this);
            }

            Task<XmlReader> returnedTask = source.Task;
            try
            {
                RegisterForConnectionCloseNotification(ref returnedTask);

                Task<XmlReader>.Factory.FromAsync(BeginExecuteXmlReader, EndExecuteXmlReader, null).ContinueWith((t) =>
                {
                    registration.Dispose();
                    if (t.IsFaulted)
                    {
                        Exception e = t.Exception.InnerException;
                        _diagnosticListener.WriteCommandError(operationId, this, e);
                        source.SetException(e);
                    }
                    else
                    {
                        if (t.IsCanceled)
                        {
                            source.SetCanceled();
                        }
                        else
                        {
                            source.SetResult(t.Result);
                        }
                        _diagnosticListener.WriteCommandAfter(operationId, this);
                    }
                }, TaskScheduler.Default);
            }
            catch (Exception e)
            {
                _diagnosticListener.WriteCommandError(operationId, this, e);
                source.SetException(e);
            }

            return returnedTask;
        }

        // If the user part is quoted, remove first and last brackets and then unquote any right square
        // brackets in the procedure.  This is a very simple parser that performs no validation.  As
        // with the function below, ideally we should have support from the server for this.
        private static string UnquoteProcedurePart(string part)
        {
            if ((null != part) && (2 <= part.Length))
            {
                if ('[' == part[0] && ']' == part[part.Length - 1])
                {
                    part = part.Substring(1, part.Length - 2); // strip outer '[' & ']'
                    part = part.Replace("]]", "]"); // undo quoted "]" from "]]" to "]"
                }
            }
            return part;
        }

        // User value in this format: [server].[database].[schema].[sp_foo];1
        // This function should only be passed "[sp_foo];1".
        // This function uses a pretty simple parser that doesn't do any validation.
        // Ideally, we would have support from the server rather than us having to do this.
        private static string UnquoteProcedureName(string name, out object groupNumber)
        {
            groupNumber = null; // Out param - initialize value to no value.
            string sproc = name;

            if (null != sproc)
            {
                if (char.IsDigit(sproc[sproc.Length - 1]))
                { // If last char is a digit, parse.
                    int semicolon = sproc.LastIndexOf(';');
                    if (semicolon != -1)
                    { // If we found a semicolon, obtain the integer.
                        string part = sproc.Substring(semicolon + 1);
                        int number = 0;
                        if (int.TryParse(part, out number))
                        { // No checking, just fail if this doesn't work.
                            groupNumber = number;
                            sproc = sproc.Substring(0, semicolon);
                        }
                    }
                }
                sproc = UnquoteProcedurePart(sproc);
            }
            return sproc;
        }

        // Index into indirection arrays for columns of interest to DeriveParameters
        private enum ProcParamsColIndex
        {
            ParameterName = 0,
            ParameterType,
            DataType, // obsolete in katmai, use ManagedDataType instead
            ManagedDataType, // new in katmai
            CharacterMaximumLength,
            NumericPrecision,
            NumericScale,
            TypeCatalogName,
            TypeSchemaName,
            TypeName,
            XmlSchemaCollectionCatalogName,
            XmlSchemaCollectionSchemaName,
            XmlSchemaCollectionName,
            UdtTypeName, // obsolete in Katmai.  Holds the actual typename if UDT, since TypeName didn't back then.
            DateTimeScale // new in Katmai
        };

        // Yukon- column ordinals (this array indexed by ProcParamsColIndex
        internal static readonly string[] PreKatmaiProcParamsNames = new string[] {
            "PARAMETER_NAME",           // ParameterName,
            "PARAMETER_TYPE",           // ParameterType,
            "DATA_TYPE",                // DataType
            null,                       // ManagedDataType,     introduced in Katmai
            "CHARACTER_MAXIMUM_LENGTH", // CharacterMaximumLength,
            "NUMERIC_PRECISION",        // NumericPrecision,
            "NUMERIC_SCALE",            // NumericScale,
            "UDT_CATALOG",              // TypeCatalogName,
            "UDT_SCHEMA",               // TypeSchemaName,
            "TYPE_NAME",                // TypeName,
            "XML_CATALOGNAME",          // XmlSchemaCollectionCatalogName,
            "XML_SCHEMANAME",           // XmlSchemaCollectionSchemaName,
            "XML_SCHEMACOLLECTIONNAME", // XmlSchemaCollectionName
            "UDT_NAME",                 // UdtTypeName
            null,                       // Scale for datetime types with scale, introduced in Katmai
        };

        // Katmai+ column ordinals (this array indexed by ProcParamsColIndex
        internal static readonly string[] KatmaiProcParamsNames = new string[] {
            "PARAMETER_NAME",           // ParameterName,
            "PARAMETER_TYPE",           // ParameterType,
            null,                       // DataType, removed from Katmai+
            "MANAGED_DATA_TYPE",        // ManagedDataType,
            "CHARACTER_MAXIMUM_LENGTH", // CharacterMaximumLength,
            "NUMERIC_PRECISION",        // NumericPrecision,
            "NUMERIC_SCALE",            // NumericScale,
            "TYPE_CATALOG_NAME",        // TypeCatalogName,
            "TYPE_SCHEMA_NAME",         // TypeSchemaName,
            "TYPE_NAME",                // TypeName,
            "XML_CATALOGNAME",          // XmlSchemaCollectionCatalogName,
            "XML_SCHEMANAME",           // XmlSchemaCollectionSchemaName,
            "XML_SCHEMACOLLECTIONNAME", // XmlSchemaCollectionName
            null,                       // UdtTypeName, removed from Katmai+
            "SS_DATETIME_PRECISION",    // Scale for datetime types with scale
        };

        internal void DeriveParameters()
        {
            switch (CommandType)
            {
                case CommandType.Text:
                    throw ADP.DeriveParametersNotSupported(this);
                case CommandType.StoredProcedure:
                    break;
                case CommandType.TableDirect:
                    // CommandType.TableDirect - do nothing, parameters are not supported
                    throw ADP.DeriveParametersNotSupported(this);
                default:
                    throw ADP.InvalidCommandType(CommandType);
            }

            // validate that we have a valid connection
            ValidateCommand(false /*not async*/, nameof(DeriveParameters));

            // Use common parser for SqlClient and OleDb - parse into 4 parts - Server, Catalog, Schema, ProcedureName
            string[] parsedSProc = MultipartIdentifier.ParseMultipartIdentifier(CommandText, "[\"", "]\"", SR.SQL_SqlCommandCommandText, false);
            if (null == parsedSProc[3] || string.IsNullOrEmpty(parsedSProc[3]))
            {
                throw ADP.NoStoredProcedureExists(CommandText);
            }

            Debug.Assert(parsedSProc.Length == 4, "Invalid array length result from SqlCommandBuilder.ParseProcedureName");

            SqlCommand paramsCmd = null;
            StringBuilder cmdText = new StringBuilder();

            // Build call for sp_procedure_params_rowset built of unquoted values from user:
            // [user server, if provided].[user catalog, else current database].[sys if Yukon, else blank].[sp_procedure_params_rowset]

            // Server - pass only if user provided.
            if (!string.IsNullOrEmpty(parsedSProc[0]))
            {
                SqlCommandSet.BuildStoredProcedureName(cmdText, parsedSProc[0]);
                cmdText.Append(".");
            }

            // Catalog - pass user provided, otherwise use current database.
            if (string.IsNullOrEmpty(parsedSProc[1]))
            {
                parsedSProc[1] = Connection.Database;
            }
            SqlCommandSet.BuildStoredProcedureName(cmdText, parsedSProc[1]);
            cmdText.Append(".");

            // Schema - only if Yukon, and then only pass sys.  Also - pass managed version of sproc
            // for Yukon, else older sproc.
            string[] colNames;
            bool useManagedDataType;
            if (Connection.IsKatmaiOrNewer)
            {
                // Procedure - [sp_procedure_params_managed]
                cmdText.Append("[sys].[").Append(TdsEnums.SP_PARAMS_MGD10).Append("]");

                colNames = KatmaiProcParamsNames;
                useManagedDataType = true;
            }
            else
            {
                // Procedure - [sp_procedure_params_managed]
                cmdText.Append("[sys].[").Append(TdsEnums.SP_PARAMS_MANAGED).Append("]");

                colNames = PreKatmaiProcParamsNames;
                useManagedDataType = false;
            }


            paramsCmd = new SqlCommand(cmdText.ToString(), Connection, Transaction)
            {
                CommandType = CommandType.StoredProcedure
            };

            object groupNumber;

            // Prepare parameters for sp_procedure_params_rowset:
            // 1) procedure name - unquote user value
            // 2) group number - parsed at the time we unquoted procedure name
            // 3) procedure schema - unquote user value

            paramsCmd.Parameters.Add(new SqlParameter("@procedure_name", SqlDbType.NVarChar, 255));
            paramsCmd.Parameters[0].Value = UnquoteProcedureName(parsedSProc[3], out groupNumber); // ProcedureName is 4rd element in parsed array

            if (null != groupNumber)
            {
                SqlParameter param = paramsCmd.Parameters.Add(new SqlParameter("@group_number", SqlDbType.Int));
                param.Value = groupNumber;
            }

            if (!string.IsNullOrEmpty(parsedSProc[2]))
            { // SchemaName is 3rd element in parsed array
                SqlParameter param = paramsCmd.Parameters.Add(new SqlParameter("@procedure_schema", SqlDbType.NVarChar, 255));
                param.Value = UnquoteProcedurePart(parsedSProc[2]);
            }

            SqlDataReader r = null;

            List<SqlParameter> parameters = new List<SqlParameter>();
            bool processFinallyBlock = true;

            try
            {
                r = paramsCmd.ExecuteReader();

                SqlParameter p = null;

                while (r.Read())
                {
                    // each row corresponds to a parameter of the stored proc.  Fill in all the info
                    p = new SqlParameter()
                    {
                        ParameterName = (string)r[colNames[(int)ProcParamsColIndex.ParameterName]]
                    };

                    // type
                    if (useManagedDataType)
                    {
                        p.SqlDbType = (SqlDbType)(short)r[colNames[(int)ProcParamsColIndex.ManagedDataType]];

                        // Yukon didn't have as accurate of information as we're getting for Katmai, so re-map a couple of
                        //  types for backward compatability.
                        switch (p.SqlDbType)
                        {
                            case SqlDbType.Image:
                            case SqlDbType.Timestamp:
                                p.SqlDbType = SqlDbType.VarBinary;
                                break;

                            case SqlDbType.NText:
                                p.SqlDbType = SqlDbType.NVarChar;
                                break;

                            case SqlDbType.Text:
                                p.SqlDbType = SqlDbType.VarChar;
                                break;

                            default:
                                break;
                        }
                    }
                    else
                    {
                        p.SqlDbType = MetaType.GetSqlDbTypeFromOleDbType((short)r[colNames[(int)ProcParamsColIndex.DataType]],
                            ADP.IsNull(r[colNames[(int)ProcParamsColIndex.TypeName]]) ?
                                ADP.StrEmpty :
                                (string)r[colNames[(int)ProcParamsColIndex.TypeName]]);
                    }

                    // size
                    object a = r[colNames[(int)ProcParamsColIndex.CharacterMaximumLength]];
                    if (a is int)
                    {
                        int size = (int)a;

                        // Map MAX sizes correctly.  The Katmai server-side proc sends 0 for these instead of -1.
                        //  Should be fixed on the Katmai side, but would likely hold up the RI, and is safer to fix here.
                        //  If we can get the server-side fixed before shipping Katmai, we can remove this mapping.
                        if (0 == size &&
                                (p.SqlDbType == SqlDbType.NVarChar ||
                                 p.SqlDbType == SqlDbType.VarBinary ||
                                 p.SqlDbType == SqlDbType.VarChar))
                        {
                            size = -1;
                        }
                        p.Size = size;
                    }

                    // direction
                    p.Direction = ParameterDirectionFromOleDbDirection((short)r[colNames[(int)ProcParamsColIndex.ParameterType]]);

                    if (p.SqlDbType == SqlDbType.Decimal)
                    {
                        p.ScaleInternal = (byte)((short)r[colNames[(int)ProcParamsColIndex.NumericScale]] & 0xff);
                        p.PrecisionInternal = (byte)((short)r[colNames[(int)ProcParamsColIndex.NumericPrecision]] & 0xff);
                    }

                    // type name for Udt
                    if (SqlDbType.Udt == p.SqlDbType)
                    {
                        string udtTypeName;
                        if (useManagedDataType)
                        {
                            udtTypeName = (string)r[colNames[(int)ProcParamsColIndex.TypeName]];
                        }
                        else
                        {
                            udtTypeName = (string)r[colNames[(int)ProcParamsColIndex.UdtTypeName]];
                        }

                        //read the type name
                        p.UdtTypeName = r[colNames[(int)ProcParamsColIndex.TypeCatalogName]] + "." +
                            r[colNames[(int)ProcParamsColIndex.TypeSchemaName]] + "." +
                            udtTypeName;
                    }

                    // type name for Structured types (same as for Udt's except assign p.TypeName instead of p.UdtTypeName
                    if (SqlDbType.Structured == p.SqlDbType)
                    {
                        Debug.Assert(_activeConnection.IsKatmaiOrNewer, "Invalid datatype token received from pre-katmai server");

                        //read the type name
                        p.TypeName = r[colNames[(int)ProcParamsColIndex.TypeCatalogName]] + "." +
                            r[colNames[(int)ProcParamsColIndex.TypeSchemaName]] + "." +
                            r[colNames[(int)ProcParamsColIndex.TypeName]];
                    }

                    // XmlSchema name for Xml types
                    if (SqlDbType.Xml == p.SqlDbType)
                    {
                        object value;

                        value = r[colNames[(int)ProcParamsColIndex.XmlSchemaCollectionCatalogName]];
                        p.XmlSchemaCollectionDatabase = ADP.IsNull(value) ? String.Empty : (string)value;

                        value = r[colNames[(int)ProcParamsColIndex.XmlSchemaCollectionSchemaName]];
                        p.XmlSchemaCollectionOwningSchema = ADP.IsNull(value) ? String.Empty : (string)value;

                        value = r[colNames[(int)ProcParamsColIndex.XmlSchemaCollectionName]];
                        p.XmlSchemaCollectionName = ADP.IsNull(value) ? String.Empty : (string)value;
                    }

                    if (MetaType._IsVarTime(p.SqlDbType))
                    {
                        object value = r[colNames[(int)ProcParamsColIndex.DateTimeScale]];
                        if (value is int)
                        {
                            p.ScaleInternal = (byte)(((int)value) & 0xff);
                        }
                    }

                    parameters.Add(p);
                }
            }
            catch (Exception e)
            {
                processFinallyBlock = ADP.IsCatchableExceptionType(e);
                throw;
            }
            finally
            {
                if (processFinallyBlock)
                {
                    r?.Close();

                    // always unhook the user's connection
                    paramsCmd.Connection = null;
                }
            }

            if (parameters.Count == 0)
            {
                throw ADP.NoStoredProcedureExists(this.CommandText);
            }

            Parameters.Clear();

            foreach (SqlParameter temp in parameters)
            {
                _parameters.Add(temp);
            }
        }

        private ParameterDirection ParameterDirectionFromOleDbDirection(short oledbDirection)
        {
            Debug.Assert(oledbDirection >= 1 && oledbDirection <= 4, "invalid parameter direction from params_rowset!");

            switch (oledbDirection)
            {
                case 2:
                    return ParameterDirection.InputOutput;
                case 3:
                    return ParameterDirection.Output;
                case 4:
                    return ParameterDirection.ReturnValue;
                default:
                    return ParameterDirection.Input;
            }

        }

        // get cached metadata
        internal _SqlMetaDataSet MetaData
        {
            get
            {
                return _cachedMetaData;
            }
        }

        // Check to see if notificactions auto enlistment is turned on. Enlist if so.
        private void CheckNotificationStateAndAutoEnlist()
        {
            // Auto-enlist not supported in Core

            // If we have a notification with a dependency, setup the notification options at this time.

            // If user passes options, then we will always have option data at the time the SqlDependency
            // ctor is called.  But, if we are using default queue, then we do not have this data until
            // Start().  Due to this, we always delay setting options until execute.

            // There is a variance in order between Start(), SqlDependency(), and Execute.  This is the 
            // best way to solve that problem.
            if (null != Notification)
            {
                if (_sqlDep != null)
                {
                    if (null == _sqlDep.Options)
                    {
                        // If null, SqlDependency was not created with options, so we need to obtain default options now.
                        // GetDefaultOptions can and will throw under certain conditions.

                        // In order to match to the appropriate start - we need 3 pieces of info:
                        // 1) server 2) user identity (SQL Auth or Int Sec) 3) database

                        SqlDependency.IdentityUserNamePair identityUserName = null;

                        // Obtain identity from connection.
                        SqlInternalConnectionTds internalConnection = _activeConnection.InnerConnection as SqlInternalConnectionTds;
                        if (internalConnection.Identity != null)
                        {
                            identityUserName = new SqlDependency.IdentityUserNamePair(internalConnection.Identity, null);
                        }
                        else
                        {
                            identityUserName = new SqlDependency.IdentityUserNamePair(null, internalConnection.ConnectionOptions.UserID);
                        }

                        Notification.Options = SqlDependency.GetDefaultComposedOptions(_activeConnection.DataSource,
                                                             InternalTdsConnection.ServerProvidedFailOverPartner,
                                                             identityUserName, _activeConnection.Database);
                    }

                    // Set UserData on notifications, as well as adding to the appdomain dispatcher.  The value is
                    // computed by an algorithm on the dependency - fixed and will always produce the same value
                    // given identical commandtext + parameter values.
                    Notification.UserData = _sqlDep.ComputeHashAndAddToDispatcher(this);
                    // Maintain server list for SqlDependency.
                    _sqlDep.AddToServerList(_activeConnection.DataSource);
                }
            }
        }

        // Tds-specific logic for ExecuteNonQuery run handling
        private Task RunExecuteNonQueryTds(string methodName, bool async, int timeout, bool asyncWrite)
        {
            Debug.Assert(!asyncWrite || async, "AsyncWrite should be always accompanied by Async");
            bool processFinallyBlock = true;
            try
            {
                Task reconnectTask = _activeConnection.ValidateAndReconnect(null, timeout);

                if (reconnectTask != null)
                {
                    long reconnectionStart = ADP.TimerCurrent();
                    if (async)
                    {
                        TaskCompletionSource<object> completion = new TaskCompletionSource<object>();
                        _activeConnection.RegisterWaitingForReconnect(completion.Task);
                        _reconnectionCompletionSource = completion;
                        CancellationTokenSource timeoutCTS = new CancellationTokenSource();
                        AsyncHelper.SetTimeoutException(completion, timeout, SQL.CR_ReconnectTimeout, timeoutCTS.Token);
                        AsyncHelper.ContinueTask(reconnectTask, completion,
                            () =>
                            {
                                if (completion.Task.IsCompleted)
                                {
                                    return;
                                }
                                Interlocked.CompareExchange(ref _reconnectionCompletionSource, null, completion);
                                timeoutCTS.Cancel();
                                Task subTask = RunExecuteNonQueryTds(methodName, async, TdsParserStaticMethods.GetRemainingTimeout(timeout, reconnectionStart), asyncWrite);
                                if (subTask == null)
                                {
                                    completion.SetResult(null);
                                }
                                else
                                {
                                    AsyncHelper.ContinueTask(subTask, completion, () => completion.SetResult(null));
                                }
                            }, connectionToAbort: _activeConnection);
                        return completion.Task;
                    }
                    else
                    {
                        AsyncHelper.WaitForCompletion(reconnectTask, timeout, () => { throw SQL.CR_ReconnectTimeout(); });
                        timeout = TdsParserStaticMethods.GetRemainingTimeout(timeout, reconnectionStart);
                    }
                }

                if (asyncWrite)
                {
                    _activeConnection.AddWeakReference(this, SqlReferenceCollection.CommandTag);
                }

                GetStateObject();

                // we just send over the raw text with no annotation
                // no parameters are sent over
                // no data reader is returned
                // use this overload for "batch SQL" tds token type
                Task executeTask = _stateObj.Parser.TdsExecuteSQLBatch(this.CommandText, timeout, this.Notification, _stateObj, sync: true);
                Debug.Assert(executeTask == null, "Shouldn't get a task when doing sync writes");

                NotifyDependency();
                if (async)
                {
                    _activeConnection.GetOpenTdsConnection(methodName).IncrementAsyncCount();
                }
                else
                {
                    bool dataReady;
                    Debug.Assert(_stateObj._syncOverAsync, "Should not attempt pends in a synchronous call");
                    bool result = _stateObj.Parser.TryRun(RunBehavior.UntilDone, this, null, null, _stateObj, out dataReady);
                    if (!result) { throw SQL.SynchronousCallMayNotPend(); }
                }
            }
            catch (Exception e)
            {
                processFinallyBlock = ADP.IsCatchableExceptionType(e);
                throw;
            }
            finally
            {
                if (processFinallyBlock && !async)
                {
                    // When executing Async, we need to keep the _stateObj alive...
                    PutStateObject();
                }
            }
            return null;
        }


        internal SqlDataReader RunExecuteReader(CommandBehavior cmdBehavior, RunBehavior runBehavior, bool returnStream, [CallerMemberName] string method = "")
        {
            Task unused; // sync execution 
            SqlDataReader reader = RunExecuteReader(cmdBehavior, runBehavior, returnStream, completion: null, timeout: CommandTimeout, task: out unused, method: method);
            Debug.Assert(unused == null, "returned task during synchronous execution");
            return reader;
        }

        // task is created in case of pending asynchronous write, returned SqlDataReader should not be utilized until that task is complete 
        internal SqlDataReader RunExecuteReader(CommandBehavior cmdBehavior, RunBehavior runBehavior, bool returnStream, TaskCompletionSource<object> completion, int timeout, out Task task, bool asyncWrite = false, [CallerMemberName] string method = "")
        {
            bool async = (null != completion);

            task = null;

            _rowsAffected = -1;

            if (0 != (CommandBehavior.SingleRow & cmdBehavior))
            {
                // CommandBehavior.SingleRow implies CommandBehavior.SingleResult
                cmdBehavior |= CommandBehavior.SingleResult;
            }

            // this function may throw for an invalid connection
            // returns false for empty command text
            ValidateCommand(async, method);

            CheckNotificationStateAndAutoEnlist(); // Only call after validate - requires non null connection!

            SqlStatistics statistics = Statistics;
            if (null != statistics)
            {
                if ((!this.IsDirty && this.IsPrepared && !_hiddenPrepare)
                    || (this.IsPrepared && _execType == EXECTYPE.PREPAREPENDING))
                {
                    statistics.SafeIncrement(ref statistics._preparedExecs);
                }
                else
                {
                    statistics.SafeIncrement(ref statistics._unpreparedExecs);
                }
            }


            {
                return RunExecuteReaderTds(cmdBehavior, runBehavior, returnStream, async, timeout, out task, asyncWrite && async);
            }
        }

        private SqlDataReader RunExecuteReaderTds(CommandBehavior cmdBehavior, RunBehavior runBehavior, bool returnStream, bool async, int timeout, out Task task, bool asyncWrite, SqlDataReader ds = null)
        {
            Debug.Assert(!asyncWrite || async, "AsyncWrite should be always accompanied by Async");

            if (ds == null && returnStream)
            {
                ds = new SqlDataReader(this, cmdBehavior);
            }

            Task reconnectTask = _activeConnection.ValidateAndReconnect(null, timeout);

            if (reconnectTask != null)
            {
                long reconnectionStart = ADP.TimerCurrent();
                if (async)
                {
                    TaskCompletionSource<object> completion = new TaskCompletionSource<object>();
                    _activeConnection.RegisterWaitingForReconnect(completion.Task);
                    _reconnectionCompletionSource = completion;
                    CancellationTokenSource timeoutCTS = new CancellationTokenSource();
                    AsyncHelper.SetTimeoutException(completion, timeout, SQL.CR_ReconnectTimeout, timeoutCTS.Token);
                    AsyncHelper.ContinueTask(reconnectTask, completion,
                        () =>
                        {
                            if (completion.Task.IsCompleted)
                            {
                                return;
                            }
                            Interlocked.CompareExchange(ref _reconnectionCompletionSource, null, completion);
                            timeoutCTS.Cancel();
                            Task subTask;
                            RunExecuteReaderTds(cmdBehavior, runBehavior, returnStream, async, TdsParserStaticMethods.GetRemainingTimeout(timeout, reconnectionStart), out subTask, asyncWrite, ds);
                            if (subTask == null)
                            {
                                completion.SetResult(null);
                            }
                            else
                            {
                                AsyncHelper.ContinueTask(subTask, completion, () => completion.SetResult(null));
                            }
                        }, connectionToAbort: _activeConnection);
                    task = completion.Task;
                    return ds;
                }
                else
                {
                    AsyncHelper.WaitForCompletion(reconnectTask, timeout, () => { throw SQL.CR_ReconnectTimeout(); });
                    timeout = TdsParserStaticMethods.GetRemainingTimeout(timeout, reconnectionStart);
                }
            }

            // make sure we have good parameter information
            // prepare the command
            // execute
            Debug.Assert(null != _activeConnection.Parser, "TdsParser class should not be null in Command.Execute!");

            bool inSchema = (0 != (cmdBehavior & CommandBehavior.SchemaOnly));

            // create a new RPC
            _SqlRPC rpc = null;

            task = null;

            string optionSettings = null;
            bool processFinallyBlock = true;
            bool decrementAsyncCountOnFailure = false;

            if (async)
            {
                _activeConnection.GetOpenTdsConnection().IncrementAsyncCount();
                decrementAsyncCountOnFailure = true;
            }

            try
            {
                if (asyncWrite)
                {
                    _activeConnection.AddWeakReference(this, SqlReferenceCollection.CommandTag);
                }

                GetStateObject();
                Task writeTask = null;

                if ((System.Data.CommandType.Text == this.CommandType) && (0 == GetParameterCount(_parameters)))
                {
                    // Send over SQL Batch command if we are not a stored proc and have no parameters
                    Debug.Assert(!IsUserPrepared, "CommandType.Text with no params should not be prepared!");
                    string text = GetCommandText(cmdBehavior) + GetResetOptionsString(cmdBehavior);
                    writeTask = _stateObj.Parser.TdsExecuteSQLBatch(text, timeout, this.Notification, _stateObj, sync: !asyncWrite);
                }
                else if (System.Data.CommandType.Text == this.CommandType)
                {
                    if (this.IsDirty)
                    {
                        Debug.Assert(_cachedMetaData == null || !_dirty, "dirty query should not have cached metadata!"); // can have cached metadata if dirty because of parameters
                        //
                        // someone changed the command text or the parameter schema so we must unprepare the command
                        //
                        // remember that IsDirty includes test for IsPrepared!
                        if (_execType == EXECTYPE.PREPARED)
                        {
                            _hiddenPrepare = true;
                        }
                        Unprepare();
                        IsDirty = false;
                    }

                    if (_execType == EXECTYPE.PREPARED)
                    {
                        Debug.Assert(this.IsPrepared && (_prepareHandle != -1), "invalid attempt to call sp_execute without a handle!");
                        rpc = BuildExecute(inSchema);
                    }
                    else if (_execType == EXECTYPE.PREPAREPENDING)
                    {
                        rpc = BuildPrepExec(cmdBehavior);
                        // next time through, only do an exec
                        _execType = EXECTYPE.PREPARED;
                        _preparedConnectionCloseCount = _activeConnection.CloseCount;
                        _preparedConnectionReconnectCount = _activeConnection.ReconnectCount;
                        // mark ourselves as preparing the command
                        _inPrepare = true;
                    }
                    else
                    {
                        Debug.Assert(_execType == EXECTYPE.UNPREPARED, "Invalid execType!");
                        BuildExecuteSql(cmdBehavior, null, _parameters, ref rpc);
                    }

                    // if shiloh, then set NOMETADATA_UNLESSCHANGED flag
                    rpc.options = TdsEnums.RPC_NOMETADATA;

                    Debug.Assert(_rpcArrayOf1[0] == rpc);
                    writeTask = _stateObj.Parser.TdsExecuteRPC(_rpcArrayOf1, timeout, inSchema, this.Notification, _stateObj, CommandType.StoredProcedure == CommandType, sync: !asyncWrite);
                }
                else
                {
                    Debug.Assert(this.CommandType == System.Data.CommandType.StoredProcedure, "unknown command type!");

                    BuildRPC(inSchema, _parameters, ref rpc);

                    // if we need to augment the command because a user has changed the command behavior (e.g. FillSchema)
                    // then batch sql them over.  This is inefficient (3 round trips) but the only way we can get metadata only from
                    // a stored proc
                    optionSettings = GetSetOptionsString(cmdBehavior);
                    // turn set options ON
                    if (null != optionSettings)
                    {
                        Task executeTask = _stateObj.Parser.TdsExecuteSQLBatch(optionSettings, timeout, this.Notification, _stateObj, sync: true);
                        Debug.Assert(executeTask == null, "Shouldn't get a task when doing sync writes");
                        Debug.Assert(_stateObj._syncOverAsync, "Should not attempt pends in a synchronous call");
                        bool result = _stateObj.Parser.TryRun(RunBehavior.UntilDone, this, null, null, _stateObj, out bool dataReady);
                        if (!result) { throw SQL.SynchronousCallMayNotPend(); }
                        // and turn OFF when the ds exhausts the stream on Close()
                        optionSettings = GetResetOptionsString(cmdBehavior);
                    }

                    // execute sp
                    Debug.Assert(_rpcArrayOf1[0] == rpc);
                    writeTask = _stateObj.Parser.TdsExecuteRPC(_rpcArrayOf1, timeout, inSchema, this.Notification, _stateObj, CommandType.StoredProcedure == CommandType, sync: !asyncWrite);
                }

                Debug.Assert(writeTask == null || async, "Returned task in sync mode");

                if (async)
                {
                    decrementAsyncCountOnFailure = false;
                    if (writeTask != null)
                    {
                        task = AsyncHelper.CreateContinuationTask(writeTask, () =>
                        {
                            _activeConnection.GetOpenTdsConnection(); // it will throw if connection is closed
                            cachedAsyncState.SetAsyncReaderState(ds, runBehavior, optionSettings);
                        },
                                 onFailure: (exc) =>
                                 {
                                     _activeConnection.GetOpenTdsConnection().DecrementAsyncCount();
                                 });
                    }
                    else
                    {
                        cachedAsyncState.SetAsyncReaderState(ds, runBehavior, optionSettings);
                    }
                }
                else
                {
                    // Always execute - even if no reader!
                    FinishExecuteReader(ds, runBehavior, optionSettings);
                }
            }
            catch (Exception e)
            {
                processFinallyBlock = ADP.IsCatchableExceptionType(e);
                if (decrementAsyncCountOnFailure)
                {
                    SqlInternalConnectionTds innerConnectionTds = (_activeConnection.InnerConnection as SqlInternalConnectionTds);
                    if (null != innerConnectionTds)
                    { // it may be closed 
                        innerConnectionTds.DecrementAsyncCount();
                    }
                }
                throw;
            }
            finally
            {
                if (processFinallyBlock && !async)
                {
                    // When executing async, we need to keep the _stateObj alive...
                    PutStateObject();
                }
            }

            Debug.Assert(async || null == _stateObj, "non-null state object in RunExecuteReader");
            return ds;
        }


        private SqlDataReader CompleteAsyncExecuteReader()
        {
            SqlDataReader ds = cachedAsyncState.CachedAsyncReader; // should not be null
            bool processFinallyBlock = true;
            try
            {
                FinishExecuteReader(ds, cachedAsyncState.CachedRunBehavior, cachedAsyncState.CachedSetOptions);
            }
            catch (Exception e)
            {
                processFinallyBlock = ADP.IsCatchableExceptionType(e);
                throw;
            }
            finally
            {
                if (processFinallyBlock)
                {
                    cachedAsyncState.ResetAsyncState();
                    PutStateObject();
                }
            }

            return ds;
        }

        private void FinishExecuteReader(SqlDataReader ds, RunBehavior runBehavior, string resetOptionsString)
        {
            // always wrap with a try { FinishExecuteReader(...) } finally { PutStateObject(); }

            NotifyDependency();

            if (runBehavior == RunBehavior.UntilDone)
            {
                try
                {
                    bool dataReady;
                    Debug.Assert(_stateObj._syncOverAsync, "Should not attempt pends in a synchronous call");
                    bool result = _stateObj.Parser.TryRun(RunBehavior.UntilDone, this, ds, null, _stateObj, out dataReady);
                    if (!result) { throw SQL.SynchronousCallMayNotPend(); }
                }
                catch (Exception e)
                {
                    if (ADP.IsCatchableExceptionType(e))
                    {
                        if (_inPrepare)
                        {
                            // The flag is expected to be reset by OnReturnValue.  We should receive
                            // the handle unless command execution failed.  If fail, move back to pending
                            // state.
                            _inPrepare = false;                  // reset the flag
                            IsDirty = true;                      // mark command as dirty so it will be prepared next time we're coming through
                            _execType = EXECTYPE.PREPAREPENDING; // reset execution type to pending
                        }

                        if (null != ds)
                        {
                            try
                            {
                                ds.Close();
                            }
                            catch (Exception exClose)
                            {
                                Debug.WriteLine("Received this exception from SqlDataReader.Close() while in another catch block: " + exClose.ToString());
                            }
                        }
                    }
                    throw;
                }
            }

            // bind the parser to the reader if we get this far
            if (ds != null)
            {
                ds.Bind(_stateObj);
                _stateObj = null;   // the reader now owns this...
                ds.ResetOptionsString = resetOptionsString;
                // bind this reader to this connection now
                _activeConnection.AddWeakReference(ds, SqlReferenceCollection.DataReaderTag);

                // force this command to start reading data off the wire.
                // this will cause an error to be reported at Execute() time instead of Read() time
                // if the command is not set.
                try
                {
                    _cachedMetaData = ds.MetaData;
                    ds.IsInitialized = true;
                }
                catch (Exception e)
                {
                    if (ADP.IsCatchableExceptionType(e))
                    {
                        if (_inPrepare)
                        {
                            // The flag is expected to be reset by OnReturnValue.  We should receive
                            // the handle unless command execution failed.  If fail, move back to pending
                            // state.
                            _inPrepare = false;                  // reset the flag
                            IsDirty = true;                      // mark command as dirty so it will be prepared next time we're coming through
                            _execType = EXECTYPE.PREPAREPENDING; // reset execution type to pending
                        }

                        try
                        {
                            ds.Close();
                        }
                        catch (Exception exClose)
                        {
                            Debug.WriteLine("Received this exception from SqlDataReader.Close() while in another catch block: " + exClose.ToString());
                        }
                    }

                    throw;
                }
            }
        }


        private void RegisterForConnectionCloseNotification<T>(ref Task<T> outerTask)
        {
            SqlConnection connection = _activeConnection;
            if (connection == null)
            {
                // No connection
                throw ADP.ClosedConnectionError();
            }

            connection.RegisterForConnectionCloseNotification<T>(ref outerTask, this, SqlReferenceCollection.CommandTag);
        }

        // validates that a command has commandText and a non-busy open connection
        // throws exception for error case, returns false if the commandText is empty
        private void ValidateCommand(bool async, [CallerMemberName] string method = "")
        {
            if (null == _activeConnection)
            {
                throw ADP.ConnectionRequired(method);
            }

            // Ensure that the connection is open and that the Parser is in the correct state
            SqlInternalConnectionTds tdsConnection = _activeConnection.InnerConnection as SqlInternalConnectionTds;
            if (tdsConnection != null)
            {
                var parser = tdsConnection.Parser;
                if ((parser == null) || (parser.State == TdsParserState.Closed))
                {
                    throw ADP.OpenConnectionRequired(method, ConnectionState.Closed);
                }
                else if (parser.State != TdsParserState.OpenLoggedIn)
                {
                    throw ADP.OpenConnectionRequired(method, ConnectionState.Broken);
                }
            }
            else if (_activeConnection.State == ConnectionState.Closed)
            {
                throw ADP.OpenConnectionRequired(method, ConnectionState.Closed);
            }
            else if (_activeConnection.State == ConnectionState.Broken)
            {
                throw ADP.OpenConnectionRequired(method, ConnectionState.Broken);
            }

            ValidateAsyncCommand();

            // close any non MARS dead readers, if applicable, and then throw if still busy.
            // Throw if we have a live reader on this command
            _activeConnection.ValidateConnectionForExecute(method, this);
            // Check to see if the currently set transaction has completed.  If so,
            // null out our local reference.
            if (null != _transaction && _transaction.Connection == null)
                _transaction = null;

            // throw if the connection is in a transaction but there is no
            // locally assigned transaction object
            if (_activeConnection.HasLocalTransactionFromAPI && (null == _transaction))
                throw ADP.TransactionRequired(method);

            // if we have a transaction, check to ensure that the active
            // connection property matches the connection associated with
            // the transaction
            if (null != _transaction && _activeConnection != _transaction.Connection)
                throw ADP.TransactionConnectionMismatch();

            if (string.IsNullOrEmpty(this.CommandText))
                throw ADP.CommandTextRequired(method);
        }

        private void ValidateAsyncCommand()
        {
            if (cachedAsyncState.PendingAsyncOperation)
            { // Enforce only one pending async execute at a time.
                if (cachedAsyncState.IsActiveConnectionValid(_activeConnection))
                {
                    throw SQL.PendingBeginXXXExists();
                }
                else
                {
                    _stateObj = null; // Session was re-claimed by session pool upon connection close.
                    cachedAsyncState.ResetAsyncState();
                }
            }
        }

        private void GetStateObject(TdsParser parser = null)
        {
            Debug.Assert(null == _stateObj, "StateObject not null on GetStateObject");
            Debug.Assert(null != _activeConnection, "no active connection?");

            if (_pendingCancel)
            {
                _pendingCancel = false; // Not really needed, but we'll reset anyways.

                // If a pendingCancel exists on the object, we must have had a Cancel() call
                // between the point that we entered an Execute* API and the point in Execute* that
                // we proceeded to call this function and obtain a stateObject.  In that case,
                // we now throw a cancelled error.
                throw SQL.OperationCancelled();
            }

            if (parser == null)
            {
                parser = _activeConnection.Parser;
                if ((parser == null) || (parser.State == TdsParserState.Broken) || (parser.State == TdsParserState.Closed))
                {
                    // Connection's parser is null as well, therefore we must be closed
                    throw ADP.ClosedConnectionError();
                }
            }

            TdsParserStateObject stateObj = parser.GetSession(this);
            stateObj.StartSession(this);

            _stateObj = stateObj;

            if (_pendingCancel)
            {
                _pendingCancel = false; // Not really needed, but we'll reset anyways.

                // If a pendingCancel exists on the object, we must have had a Cancel() call
                // between the point that we entered this function and the point where we obtained
                // and actually assigned the stateObject to the local member.  It is possible
                // that the flag is set as well as a call to stateObj.Cancel - though that would
                // be a no-op.  So - throw.
                throw SQL.OperationCancelled();
            }
        }

        private void ReliablePutStateObject()
        {
            PutStateObject();
        }

        private void PutStateObject()
        {
            TdsParserStateObject stateObj = _stateObj;
            _stateObj = null;

            if (null != stateObj)
            {
                stateObj.CloseSession();
            }
        }
        internal void OnDoneProc()
        { // called per rpc batch complete
            if (BatchRPCMode)
            {
                // track the records affected for the just completed rpc batch
                // _rowsAffected is cumulative for ExecuteNonQuery across all rpc batches
                _SqlRPCBatchArray[_currentlyExecutingBatch].cumulativeRecordsAffected = _rowsAffected;

                _SqlRPCBatchArray[_currentlyExecutingBatch].recordsAffected =
                    (((0 < _currentlyExecutingBatch) && (0 <= _rowsAffected))
                        ? (_rowsAffected - Math.Max(_SqlRPCBatchArray[_currentlyExecutingBatch - 1].cumulativeRecordsAffected, 0))
                        : _rowsAffected);

                // track the error collection (not available from TdsParser after ExecuteNonQuery)
                // and the which errors are associated with the just completed rpc batch
                _SqlRPCBatchArray[_currentlyExecutingBatch].errorsIndexStart =
                    ((0 < _currentlyExecutingBatch)
                        ? _SqlRPCBatchArray[_currentlyExecutingBatch - 1].errorsIndexEnd
                        : 0);
                _SqlRPCBatchArray[_currentlyExecutingBatch].errorsIndexEnd = _stateObj.ErrorCount;
                _SqlRPCBatchArray[_currentlyExecutingBatch].errors = _stateObj._errors;

                // track the warning collection (not available from TdsParser after ExecuteNonQuery)
                // and the which warnings are associated with the just completed rpc batch
                _SqlRPCBatchArray[_currentlyExecutingBatch].warningsIndexStart =
                    ((0 < _currentlyExecutingBatch)
                        ? _SqlRPCBatchArray[_currentlyExecutingBatch - 1].warningsIndexEnd
                        : 0);
                _SqlRPCBatchArray[_currentlyExecutingBatch].warningsIndexEnd = _stateObj.WarningCount;
                _SqlRPCBatchArray[_currentlyExecutingBatch].warnings = _stateObj._warnings;

                _currentlyExecutingBatch++;
                Debug.Assert(_parameterCollectionList.Count >= _currentlyExecutingBatch, "OnDoneProc: Too many DONEPROC events");
            }
        }
        internal void OnReturnStatus(int status)
        {
            if (_inPrepare)
                return;

            SqlParameterCollection parameters = _parameters;
            if (BatchRPCMode)
            {
                if (_parameterCollectionList.Count > _currentlyExecutingBatch)
                {
                    parameters = _parameterCollectionList[_currentlyExecutingBatch];
                }
                else
                {
                    Debug.Assert(false, "OnReturnStatus: SqlCommand got too many DONEPROC events");
                    parameters = null;
                }
            }
            // see if a return value is bound
            int count = GetParameterCount(parameters);
            for (int i = 0; i < count; i++)
            {
                SqlParameter parameter = parameters[i];
                if (parameter.Direction == ParameterDirection.ReturnValue)
                {
                    object v = parameter.Value;

                    // if the user bound a sqlint32 (the only valid one for status, use it)
                    if ((null != v) && (v.GetType() == typeof(SqlInt32)))
                    {
                        parameter.Value = new SqlInt32(status); // value type
                    }
                    else
                    {
                        parameter.Value = status;

                    }

                    break;
                }
            }
        }

        //
        // Move the return value to the corresponding output parameter.
        // Return parameters are sent in the order in which they were defined in the procedure.
        // If named, match the parameter name, otherwise fill in based on ordinal position.
        // If the parameter is not bound, then ignore the return value.
        //
        internal void OnReturnValue(SqlReturnValue rec, TdsParserStateObject stateObj)
        {
            if (_inPrepare)
            {
                if (!rec.value.IsNull)
                {
                    _prepareHandle = rec.value.Int32;
                }
                _inPrepare = false;
                return;
            }

            SqlParameterCollection parameters = GetCurrentParameterCollection();
            int count = GetParameterCount(parameters);

            SqlParameter thisParam = GetParameterForOutputValueExtraction(parameters, rec.parameter, count);

            if (null != thisParam)
            {
                // copy over data

                // if the value user has supplied a SqlType class, then just copy over the SqlType, otherwise convert
                // to the com type
                object val = thisParam.Value;

                //set the UDT value as typed object rather than bytes
                if (SqlDbType.Udt == thisParam.SqlDbType)
                {
                    object data = null;
                    try
                    {
                        Connection.CheckGetExtendedUDTInfo(rec, true);

                        //extract the byte array from the param value
                        if (rec.value.IsNull)
                        {
                            data = DBNull.Value;
                        }
                        else
                        {
                            data = rec.value.ByteArray; //should work for both sql and non-sql values
                        }

                        //call the connection to instantiate the UDT object
                        thisParam.Value = Connection.GetUdtValue(data, rec, false);
                    }
                    catch (FileNotFoundException e)
                    {
                        // Assign Assembly.Load failure in case where assembly not on client.
                        // This allows execution to complete and failure on SqlParameter.Value.
                        thisParam.SetUdtLoadError(e);
                    }
                    catch (FileLoadException e)
                    {
                        // Assign Assembly.Load failure in case where assembly cannot be loaded on client.
                        // This allows execution to complete and failure on SqlParameter.Value.
                        thisParam.SetUdtLoadError(e);
                    }

                    return;
                }
                else
                {
                    thisParam.SetSqlBuffer(rec.value);
                }

                MetaType mt = MetaType.GetMetaTypeFromSqlDbType(rec.type, false);

                if (rec.type == SqlDbType.Decimal)
                {
                    thisParam.ScaleInternal = rec.scale;
                    thisParam.PrecisionInternal = rec.precision;
                }
                else if (mt.IsVarTime)
                {
                    thisParam.ScaleInternal = rec.scale;
                }
                else if (rec.type == SqlDbType.Xml)
                {
                    SqlCachedBuffer cachedBuffer = (thisParam.Value as SqlCachedBuffer);
                    if (null != cachedBuffer)
                    {
                        thisParam.Value = cachedBuffer.ToString();
                    }
                }

                if (rec.collation != null)
                {
                    Debug.Assert(mt.IsCharType, "Invalid collation structure for non-char type");
                    thisParam.Collation = rec.collation;
                }
            }

            return;
        }

        private SqlParameterCollection GetCurrentParameterCollection()
        {
            if (BatchRPCMode)
            {
                if (_parameterCollectionList.Count > _currentlyExecutingBatch)
                {
                    return _parameterCollectionList[_currentlyExecutingBatch];
                }
                else
                {
                    Debug.Assert(false, "OnReturnValue: SqlCommand got too many DONEPROC events");
                    return null;
                }
            }
            else
            {
                return _parameters;
            }
        }

        private SqlParameter GetParameterForOutputValueExtraction(SqlParameterCollection parameters,
                        string paramName, int paramCount)
        {
            SqlParameter thisParam = null;
            bool foundParam = false;

            if (null == paramName)
            {
                // rec.parameter should only be null for a return value from a function
                for (int i = 0; i < paramCount; i++)
                {
                    thisParam = parameters[i];
                    // searching for ReturnValue
                    if (thisParam.Direction == ParameterDirection.ReturnValue)
                    {
                        foundParam = true;
                        break; // found it
                    }
                }
            }
            else
            {
                for (int i = 0; i < paramCount; i++)
                {
                    thisParam = parameters[i];
                    // searching for Output or InputOutput or ReturnValue with matching name
                    if (thisParam.Direction != ParameterDirection.Input && thisParam.Direction != ParameterDirection.ReturnValue && paramName == thisParam.ParameterNameFixed)
                    {
                        foundParam = true;
                        break; // found it
                    }
                }
            }
            if (foundParam)
                return thisParam;
            else
                return null;
        }

        private void GetRPCObject(int paramCount, ref _SqlRPC rpc)
        {
            // Designed to minimize necessary allocations
            int ii;
            if (rpc == null)
            {
                if (_rpcArrayOf1 == null)
                {
                    _rpcArrayOf1 = new _SqlRPC[1];
                    _rpcArrayOf1[0] = new _SqlRPC();
                }
                rpc = _rpcArrayOf1[0];
            }

            rpc.ProcID = 0;
            rpc.rpcName = null;
            rpc.options = 0;


            // Make sure there is enough space in the parameters and paramoptions arrays
            if (rpc.parameters == null || rpc.parameters.Length < paramCount)
            {
                rpc.parameters = new SqlParameter[paramCount];
            }
            else if (rpc.parameters.Length > paramCount)
            {
                rpc.parameters[paramCount] = null;    // Terminator
            }
            if (rpc.paramoptions == null || (rpc.paramoptions.Length < paramCount))
            {
                rpc.paramoptions = new byte[paramCount];
            }
            else
            {
                for (ii = 0; ii < paramCount; ii++)
                    rpc.paramoptions[ii] = 0;
            }
        }

        private void SetUpRPCParameters(_SqlRPC rpc, int startCount, bool inSchema, SqlParameterCollection parameters)
        {
            int ii;
            int paramCount = GetParameterCount(parameters);
            int j = startCount;
            TdsParser parser = _activeConnection.Parser;

            for (ii = 0; ii < paramCount; ii++)
            {
                SqlParameter parameter = parameters[ii];
                parameter.Validate(ii, CommandType.StoredProcedure == CommandType);

                // func will change type to that with a 4 byte length if the type has a two
                // byte length and a parameter length > than that expressible in 2 bytes
                if ((!parameter.ValidateTypeLengths().IsPlp) && (parameter.Direction != ParameterDirection.Output))
                {
                    parameter.FixStreamDataForNonPLP();
                }

                if (ShouldSendParameter(parameter))
                {
                    rpc.parameters[j] = parameter;

                    // set output bit
                    if (parameter.Direction == ParameterDirection.InputOutput ||
                        parameter.Direction == ParameterDirection.Output)
                        rpc.paramoptions[j] = TdsEnums.RPC_PARAM_BYREF;

                    // set default value bit
                    if (parameter.Direction != ParameterDirection.Output)
                    {
                        // remember that null == Convert.IsEmpty, DBNull.Value is a database null!

                        // Don't assume a default value exists for parameters in the case when
                        // the user is simply requesting schema.
                        // TVPs use DEFAULT and do not allow NULL, even for schema only.
                        if (null == parameter.Value && (!inSchema || SqlDbType.Structured == parameter.SqlDbType))
                        {
                            rpc.paramoptions[j] |= TdsEnums.RPC_PARAM_DEFAULT;
                        }
                    }

                    // Must set parameter option bit for LOB_COOKIE if unfilled LazyMat blob
                    j++;
                }
            }
        }

        private _SqlRPC BuildPrepExec(CommandBehavior behavior)
        {
            Debug.Assert(System.Data.CommandType.Text == this.CommandType, "invalid use of sp_prepexec for stored proc invocation!");
            SqlParameter sqlParam;
            int j = 3;

            int count = CountSendableParameters(_parameters);

            _SqlRPC rpc = null;
            GetRPCObject(count + j, ref rpc);

            rpc.ProcID = TdsEnums.RPC_PROCID_PREPEXEC;
            rpc.rpcName = TdsEnums.SP_PREPEXEC;

            //@handle
            sqlParam = new SqlParameter(null, SqlDbType.Int);
            sqlParam.Direction = ParameterDirection.InputOutput;
            sqlParam.Value = _prepareHandle;
            rpc.parameters[0] = sqlParam;
            rpc.paramoptions[0] = TdsEnums.RPC_PARAM_BYREF;

            //@batch_params
            string paramList = BuildParamList(_stateObj.Parser, _parameters);
            sqlParam = new SqlParameter(null, ((paramList.Length << 1) <= TdsEnums.TYPE_SIZE_LIMIT) ? SqlDbType.NVarChar : SqlDbType.NText, paramList.Length);
            sqlParam.Value = paramList;
            rpc.parameters[1] = sqlParam;

            //@batch_text
            string text = GetCommandText(behavior);
            sqlParam = new SqlParameter(null, ((text.Length << 1) <= TdsEnums.TYPE_SIZE_LIMIT) ? SqlDbType.NVarChar : SqlDbType.NText, text.Length);
            sqlParam.Value = text;
            rpc.parameters[2] = sqlParam;

            SetUpRPCParameters(rpc, j, false, _parameters);
            return rpc;
        }


        //
        // returns true if the parameter is not a return value
        // and it's value is not DBNull (for a nullable parameter)
        //
        private static bool ShouldSendParameter(SqlParameter p)
        {
            switch (p.Direction)
            {
                case ParameterDirection.ReturnValue:
                    // return value parameters are never sent
                    return false;
                case ParameterDirection.Output:
                case ParameterDirection.InputOutput:
                case ParameterDirection.Input:
                    // InputOutput/Output parameters are aways sent
                    return true;
                default:
                    Debug.Assert(false, "Invalid ParameterDirection!");
                    return false;
            }
        }

        private int CountSendableParameters(SqlParameterCollection parameters)
        {
            int cParams = 0;

            if (parameters != null)
            {
                int count = parameters.Count;
                for (int i = 0; i < count; i++)
                {
                    if (ShouldSendParameter(parameters[i]))
                        cParams++;
                }
            }
            return cParams;
        }

        // Returns total number of parameters
        private int GetParameterCount(SqlParameterCollection parameters)
        {
            return ((null != parameters) ? parameters.Count : 0);
        }

        //
        // build the RPC record header for this stored proc and add parameters
        //
        private void BuildRPC(bool inSchema, SqlParameterCollection parameters, ref _SqlRPC rpc)
        {
            Debug.Assert(this.CommandType == System.Data.CommandType.StoredProcedure, "Command must be a stored proc to execute an RPC");
            int count = CountSendableParameters(parameters);
            GetRPCObject(count, ref rpc);

            rpc.rpcName = this.CommandText; // just get the raw command text

            SetUpRPCParameters(rpc, 0, inSchema, parameters);
        }

        //
        // build the RPC record header for sp_unprepare
        //
        // prototype for sp_unprepare is:
        // sp_unprepare(@handle)

        // build the RPC record header for sp_execute
        //
        // prototype for sp_execute is:
        // sp_execute(@handle int,param1value,param2value...)

        private _SqlRPC BuildExecute(bool inSchema)
        {
            Debug.Assert(_prepareHandle != -1, "Invalid call to sp_execute without a valid handle!");
            int j = 1;

            int count = CountSendableParameters(_parameters);

            _SqlRPC rpc = null;
            GetRPCObject(count + j, ref rpc);

            SqlParameter sqlParam;

            rpc.ProcID = TdsEnums.RPC_PROCID_EXECUTE;
            rpc.rpcName = TdsEnums.SP_EXECUTE;

            //@handle
            sqlParam = new SqlParameter(null, SqlDbType.Int);
            sqlParam.Value = _prepareHandle;
            rpc.parameters[0] = sqlParam;

            SetUpRPCParameters(rpc, j, inSchema, _parameters);
            return rpc;
        }

        //
        // build the RPC record header for sp_executesql and add the parameters
        //
        // prototype for sp_executesql is:
        // sp_executesql(@batch_text nvarchar(4000),@batch_params nvarchar(4000), param1,.. paramN)
        private void BuildExecuteSql(CommandBehavior behavior, string commandText, SqlParameterCollection parameters, ref _SqlRPC rpc)
        {

            Debug.Assert(_prepareHandle == -1, "This command has an existing handle, use sp_execute!");
            Debug.Assert(CommandType.Text == this.CommandType, "invalid use of sp_executesql for stored proc invocation!");
            int j;
            SqlParameter sqlParam;

            int cParams = CountSendableParameters(parameters);
            if (cParams > 0)
            {
                j = 2;
            }
            else
            {
                j = 1;
            }

            GetRPCObject(cParams + j, ref rpc);
            rpc.ProcID = TdsEnums.RPC_PROCID_EXECUTESQL;
            rpc.rpcName = TdsEnums.SP_EXECUTESQL;

            // @sql
            if (commandText == null)
            {
                commandText = GetCommandText(behavior);
            }
            sqlParam = new SqlParameter(null, ((commandText.Length << 1) <= TdsEnums.TYPE_SIZE_LIMIT) ? SqlDbType.NVarChar : SqlDbType.NText, commandText.Length);
            sqlParam.Value = commandText;
            rpc.parameters[0] = sqlParam;

            if (cParams > 0)
            {
                string paramList = BuildParamList(_stateObj.Parser, BatchRPCMode ? parameters : _parameters);
                sqlParam = new SqlParameter(null, ((paramList.Length << 1) <= TdsEnums.TYPE_SIZE_LIMIT) ? SqlDbType.NVarChar : SqlDbType.NText, paramList.Length);
                sqlParam.Value = paramList;
                rpc.parameters[1] = sqlParam;

                bool inSchema = (0 != (behavior & CommandBehavior.SchemaOnly));
                SetUpRPCParameters(rpc, j, inSchema, parameters);
            }
        }

        // paramList parameter for sp_executesql, sp_prepare, and sp_prepexec
        internal string BuildParamList(TdsParser parser, SqlParameterCollection parameters)
        {
            StringBuilder paramList = new StringBuilder();
            bool fAddSeperator = false;

            int count = 0;

            count = parameters.Count;
            for (int i = 0; i < count; i++)
            {
                SqlParameter sqlParam = parameters[i];
                sqlParam.Validate(i, CommandType.StoredProcedure == CommandType);
                // skip ReturnValue parameters; we never send them to the server
                if (!ShouldSendParameter(sqlParam))
                    continue;

                // add our separator for the ith parameter
                if (fAddSeperator)
                    paramList.Append(',');

                paramList.Append(sqlParam.ParameterNameFixed);

                MetaType mt = sqlParam.InternalMetaType;

                //for UDTs, get the actual type name. Get only the typename, omit catalog and schema names.
                //in TSQL you should only specify the unqualified type name

                // paragraph above doesn't seem to be correct. Server won't find the type
                // if we don't provide a fully qualified name
                paramList.Append(" ");
                if (mt.SqlDbType == SqlDbType.Udt)
                {
                    string fullTypeName = sqlParam.UdtTypeName;
                    if (string.IsNullOrEmpty(fullTypeName))
                        throw SQL.MustSetUdtTypeNameForUdtParams();

                    paramList.Append(ParseAndQuoteIdentifier(fullTypeName, true /* is UdtTypeName */));
                }
                else if (mt.SqlDbType == SqlDbType.Structured)
                {
                    string typeName = sqlParam.TypeName;
                    if (string.IsNullOrEmpty(typeName))
                    {
                        throw SQL.MustSetTypeNameForParam(mt.TypeName, sqlParam.ParameterNameFixed);
                    }
                    paramList.Append(ParseAndQuoteIdentifier(typeName, false /* is not UdtTypeName*/));

                    // TVPs currently are the only Structured type and must be read only, so add that keyword
                    paramList.Append(" READONLY");
                }
                else
                {
                    // func will change type to that with a 4 byte length if the type has a two
                    // byte length and a parameter length > than that expressible in 2 bytes
                    mt = sqlParam.ValidateTypeLengths();
                    if ((!mt.IsPlp) && (sqlParam.Direction != ParameterDirection.Output))
                    {
                        sqlParam.FixStreamDataForNonPLP();
                    }
                    paramList.Append(mt.TypeName);
                }

                fAddSeperator = true;

                if (mt.SqlDbType == SqlDbType.Decimal)
                {
                    byte precision = sqlParam.GetActualPrecision();
                    byte scale = sqlParam.GetActualScale();

                    paramList.Append('(');

                    if (0 == precision)
                    {
                        precision = TdsEnums.DEFAULT_NUMERIC_PRECISION;
                    }

                    paramList.Append(precision);
                    paramList.Append(',');
                    paramList.Append(scale);
                    paramList.Append(')');
                }
                else if (mt.IsVarTime)
                {
                    byte scale = sqlParam.GetActualScale();

                    paramList.Append('(');
                    paramList.Append(scale);
                    paramList.Append(')');
                }
                else if (!mt.IsFixed && !mt.IsLong && mt.SqlDbType != SqlDbType.Timestamp && mt.SqlDbType != SqlDbType.Udt && SqlDbType.Structured != mt.SqlDbType)
                {
                    int size = sqlParam.Size;

                    paramList.Append('(');

                    // if using non unicode types, obtain the actual byte length from the parser, with it's associated code page
                    if (mt.IsAnsiType)
                    {
                        object val = sqlParam.GetCoercedValue();
                        string s = null;

                        // deal with the sql types
                        if ((null != val) && (DBNull.Value != val))
                        {
                            s = (val as string);
                            if (null == s)
                            {
                                SqlString sval = val is SqlString ? (SqlString)val : SqlString.Null;
                                if (!sval.IsNull)
                                {
                                    s = sval.Value;
                                }
                            }
                        }

                        if (null != s)
                        {
                            int actualBytes = parser.GetEncodingCharLength(s, sqlParam.GetActualSize(), sqlParam.Offset, null);
                            // if actual number of bytes is greater than the user given number of chars, use actual bytes
                            if (actualBytes > size)
                                size = actualBytes;
                        }
                    }

                    // If the user specifies a 0-sized parameter for a variable len field
                    // pass over max size (8000 bytes or 4000 characters for wide types)
                    if (0 == size)
                        size = mt.IsSizeInCharacters ? (TdsEnums.MAXSIZE >> 1) : TdsEnums.MAXSIZE;

                    paramList.Append(size);
                    paramList.Append(')');
                }
                else if (mt.IsPlp && (mt.SqlDbType != SqlDbType.Xml) && (mt.SqlDbType != SqlDbType.Udt))
                {
                    paramList.Append("(max) ");
                }

                // set the output bit for Output or InputOutput parameters
                if (sqlParam.Direction != ParameterDirection.Input)
                    paramList.Append(" " + TdsEnums.PARAM_OUTPUT);
            }

            return paramList.ToString();
        }

        // Adds quotes to each part of a SQL identifier that may be multi-part, while leaving
        //  the result as a single composite name.
        private string ParseAndQuoteIdentifier(string identifier, bool isUdtTypeName)
        {
            string[] strings = SqlParameter.ParseTypeName(identifier, isUdtTypeName);
            StringBuilder bld = new StringBuilder();

            // Stitching back together is a little tricky. Assume we want to build a full multi-part name
            //  with all parts except trimming separators for leading empty names (null or empty strings,
            //  but not whitespace). Separators in the middle should be added, even if the name part is 
            //  null/empty, to maintain proper location of the parts.
            for (int i = 0; i < strings.Length; i++)
            {
                if (0 < bld.Length)
                {
                    bld.Append('.');
                }
                if (null != strings[i] && 0 != strings[i].Length)
                {
                    bld.Append(ADP.BuildQuotedString("[", "]", strings[i]));
                }
            }

            return bld.ToString();
        }

        // returns set option text to turn on format only and key info on and off
        //  When we are executing as a text command, then we never need
        // to turn off the options since they command text is executed in the scope of sp_executesql.
        // For a stored proc command, however, we must send over batch sql and then turn off
        // the set options after we read the data.  See the code in Command.Execute()
        private string GetSetOptionsString(CommandBehavior behavior)
        {
            string s = null;

            if ((System.Data.CommandBehavior.SchemaOnly == (behavior & CommandBehavior.SchemaOnly)) ||
               (System.Data.CommandBehavior.KeyInfo == (behavior & CommandBehavior.KeyInfo)))
            {
                // SET FMTONLY ON will cause the server to ignore other SET OPTIONS, so turn
                // it off before we ask for browse mode metadata
                s = TdsEnums.FMTONLY_OFF;

                if (System.Data.CommandBehavior.KeyInfo == (behavior & CommandBehavior.KeyInfo))
                {
                    s = s + TdsEnums.BROWSE_ON;
                }

                if (System.Data.CommandBehavior.SchemaOnly == (behavior & CommandBehavior.SchemaOnly))
                {
                    s = s + TdsEnums.FMTONLY_ON;
                }
            }

            return s;
        }

        private string GetResetOptionsString(CommandBehavior behavior)
        {
            string s = null;

            // SET FMTONLY ON OFF
            if (System.Data.CommandBehavior.SchemaOnly == (behavior & CommandBehavior.SchemaOnly))
            {
                s = s + TdsEnums.FMTONLY_OFF;
            }

            // SET NO_BROWSETABLE OFF
            if (System.Data.CommandBehavior.KeyInfo == (behavior & CommandBehavior.KeyInfo))
            {
                s = s + TdsEnums.BROWSE_OFF;
            }

            return s;
        }

        private String GetCommandText(CommandBehavior behavior)
        {
            // build the batch string we send over, since we execute within a stored proc (sp_executesql), the SET options never need to be
            // turned off since they are scoped to the sproc
            Debug.Assert(System.Data.CommandType.Text == this.CommandType, "invalid call to GetCommandText for stored proc!");
            return GetSetOptionsString(behavior) + this.CommandText;
        }

        internal void CheckThrowSNIException()
        {
            var stateObj = _stateObj;
            if (stateObj != null)
            {
                stateObj.CheckThrowSNIException();
            }
        }

        // We're being notified that the underlying connection has closed
        internal void OnConnectionClosed()
        {
            var stateObj = _stateObj;
            if (stateObj != null)
            {
                stateObj.OnConnectionClosed();
            }
        }

        internal TdsParserStateObject StateObject
        {
            get
            {
                return _stateObj;
            }
        }

        private bool IsPrepared
        {
            get { return (_execType != EXECTYPE.UNPREPARED); }
        }

        private bool IsUserPrepared
        {
            get { return IsPrepared && !_hiddenPrepare && !IsDirty; }
        }

        internal bool IsDirty
        {
            get
            {
                // only dirty if prepared
                var activeConnection = _activeConnection;
                return (IsPrepared &&
                    (_dirty ||
                    ((_parameters != null) && (_parameters.IsDirty)) ||
                    ((activeConnection != null) && ((activeConnection.CloseCount != _preparedConnectionCloseCount) || (activeConnection.ReconnectCount != _preparedConnectionReconnectCount)))));
            }
            set
            {
                // only mark the command as dirty if it is already prepared
                // but always clear the value if it we are clearing the dirty flag
                _dirty = value ? IsPrepared : false;
                if (null != _parameters)
                {
                    _parameters.IsDirty = _dirty;
                }
                _cachedMetaData = null;
            }
        }

        internal int InternalRecordsAffected
        {
            get
            {
                return _rowsAffected;
            }
            set
            {
                if (-1 == _rowsAffected)
                {
                    _rowsAffected = value;
                }
                else if (0 < value)
                {
                    _rowsAffected += value;
                }
            }
        }

        internal void ClearBatchCommand()
        {
            List<_SqlRPC> rpcList = _RPCList;
            if (null != rpcList)
            {
                rpcList.Clear();
            }
            if (null != _parameterCollectionList)
            {
                _parameterCollectionList.Clear();
            }

            _SqlRPCBatchArray = null;
            _currentlyExecutingBatch = 0;
        }

        internal bool BatchRPCMode
        {
            get
            {
                return _batchRPCMode;
            }
            set
            {
                _batchRPCMode = value;

                if (_batchRPCMode == false)
                {
                    ClearBatchCommand();
                }
                else
                {
                    if (_RPCList == null)
                    {
                        _RPCList = new List<_SqlRPC>();
                    }
                    if (_parameterCollectionList == null)
                    {
                        _parameterCollectionList = new List<SqlParameterCollection>();
                    }
                }
            }
        }

        internal void AddBatchCommand(string commandText, SqlParameterCollection parameters, CommandType cmdType)
        {
            Debug.Assert(BatchRPCMode, "Command is not in batch RPC Mode");
            Debug.Assert(_RPCList != null);
            Debug.Assert(_parameterCollectionList != null);

            _SqlRPC rpc = new _SqlRPC();

            CommandText = commandText;
            CommandType = cmdType;

            GetStateObject();
            if (cmdType == CommandType.StoredProcedure)
            {
                BuildRPC(false, parameters, ref rpc);
            }
            else
            {
                // All batch sql statements must be executed inside sp_executesql, including those without parameters
                BuildExecuteSql(CommandBehavior.Default, commandText, parameters, ref rpc);
            }

            _RPCList.Add(rpc);
            // Always add a parameters collection per RPC, even if there are no parameters.
            _parameterCollectionList.Add(parameters);

            ReliablePutStateObject();
        }

        internal int ExecuteBatchRPCCommand()
        {

            Debug.Assert(BatchRPCMode, "Command is not in batch RPC Mode");
            Debug.Assert(_RPCList != null, "No batch commands specified");

            _SqlRPCBatchArray = _RPCList.ToArray();
            _currentlyExecutingBatch = 0;
            return ExecuteNonQuery();       // Check permissions, execute, return output params
        }

        internal int? GetRecordsAffected(int commandIndex)
        {
            Debug.Assert(BatchRPCMode, "Command is not in batch RPC Mode");
            Debug.Assert(_SqlRPCBatchArray != null, "batch command have been cleared");
            return _SqlRPCBatchArray[commandIndex].recordsAffected;
        }

        internal SqlException GetErrors(int commandIndex)
        {
            SqlException result = null;
            int length = (_SqlRPCBatchArray[commandIndex].errorsIndexEnd - _SqlRPCBatchArray[commandIndex].errorsIndexStart);
            if (0 < length)
            {
                SqlErrorCollection errors = new SqlErrorCollection();
                for (int i = _SqlRPCBatchArray[commandIndex].errorsIndexStart; i < _SqlRPCBatchArray[commandIndex].errorsIndexEnd; ++i)
                {
                    errors.Add(_SqlRPCBatchArray[commandIndex].errors[i]);
                }
                for (int i = _SqlRPCBatchArray[commandIndex].warningsIndexStart; i < _SqlRPCBatchArray[commandIndex].warningsIndexEnd; ++i)
                {
                    errors.Add(_SqlRPCBatchArray[commandIndex].warnings[i]);
                }
                result = SqlException.CreateException(errors, Connection.ServerVersion, Connection.ClientConnectionId);
            }
            return result;
        }


#if DEBUG
        internal void CompletePendingReadWithSuccess(bool resetForcePendingReadsToWait)
        {
            var stateObj = _stateObj;
            if (stateObj != null)
            {
                stateObj.CompletePendingReadWithSuccess(resetForcePendingReadsToWait);
            }
            else
            {
                var tempCachedAsyncState = cachedAsyncState;
                if (tempCachedAsyncState != null)
                {
                    var reader = tempCachedAsyncState.CachedAsyncReader;
                    if (reader != null)
                    {
                        reader.CompletePendingReadWithSuccess(resetForcePendingReadsToWait);
                    }
                }
            }
        }

        internal void CompletePendingReadWithFailure(int errorCode, bool resetForcePendingReadsToWait)
        {
            var stateObj = _stateObj;
            if (stateObj != null)
            {
                stateObj.CompletePendingReadWithFailure(errorCode, resetForcePendingReadsToWait);
            }
            else
            {
                var tempCachedAsyncState = _cachedAsyncState;
                if (tempCachedAsyncState != null)
                {
                    var reader = tempCachedAsyncState.CachedAsyncReader;
                    if (reader != null)
                    {
                        reader.CompletePendingReadWithFailure(errorCode, resetForcePendingReadsToWait);
                    }
                }
            }
        }
#endif

        internal void CancelIgnoreFailure()
        {
            // This method is used to route CancellationTokens to the Cancel method.
            // Cancellation is a suggestion, and exceptions should be ignored
            // rather than allowed to be unhandled, as there is no way to route
            // them to the caller.  It would be expected that the error will be
            // observed anyway from the regular method.  An example is canceling
            // an operation on a closed connection.
            try
            {
                Cancel();
            }
            catch (Exception)
            {
            }
        }

        private void NotifyDependency()
        {
            if (_sqlDep != null)
            {
                _sqlDep.StartTimer(Notification);
            }
        }

        object ICloneable.Clone() => Clone();

        public SqlCommand Clone() => new SqlCommand(this);
    }
}


