// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Data.Common;
using System.Data.ProviderBase;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using System.Transactions;
using Microsoft.SqlServer.Server;
using System.Reflection;
using System.IO;
using System.Globalization;

namespace System.Data.SqlClient
{
    public sealed partial class SqlConnection : DbConnection, ICloneable
    {
        private bool _AsyncCommandInProgress;

        // SQLStatistics support
        internal SqlStatistics _statistics;
        private bool _collectstats;

        private bool _fireInfoMessageEventOnUserErrors; // False by default

        // root task associated with current async invocation
        private Tuple<TaskCompletionSource<DbConnectionInternal>, Task> _currentCompletion;

        private string _connectionString;
        private int _connectRetryCount;

        // connection resiliency
        private object _reconnectLock = new object();
        internal Task _currentReconnectionTask;
        private Task _asyncWaitingForReconnection; // current async task waiting for reconnection in non-MARS connections
        private Guid _originalConnectionId = Guid.Empty;
        private CancellationTokenSource _reconnectionCancellationSource;
        internal SessionData _recoverySessionData;
        internal bool _suppressStateChangeForReconnection;
        private int _reconnectCount;

        // diagnostics listener
        private static readonly DiagnosticListener s_diagnosticListener = new DiagnosticListener(SqlClientDiagnosticListenerExtensions.DiagnosticListenerName);

        // Transient Fault handling flag. This is needed to convey to the downstream mechanism of connection establishment, if Transient Fault handling should be used or not
        // The downstream handling of Connection open is the same for idle connection resiliency. Currently we want to apply transient fault handling only to the connections opened
        // using SqlConnection.Open() method. 
        internal bool _applyTransientFaultHandling = false;

        public SqlConnection(string connectionString) : this()
        {
            ConnectionString = connectionString;    // setting connection string first so that ConnectionOption is available
            CacheConnectionStringProperties();
        }

        private SqlConnection(SqlConnection connection)
        {
            GC.SuppressFinalize(this);
            CopyFrom(connection);
            _connectionString = connection._connectionString;

            CacheConnectionStringProperties();
        }

        // This method will be called once connection string is set or changed. 
        private void CacheConnectionStringProperties()
        {
            SqlConnectionString connString = ConnectionOptions as SqlConnectionString;
            if (connString != null)
            {
                _connectRetryCount = connString.ConnectRetryCount;
            }
        }

        //
        // PUBLIC PROPERTIES
        //

        // used to start/stop collection of statistics data and do verify the current state
        //
        // devnote: start/stop should not performed using a property since it requires execution of code
        //
        // start statistics
        //  set the internal flag (_statisticsEnabled) to true.
        //  Create a new SqlStatistics object if not already there.
        //  connect the parser to the object.
        //  if there is no parser at this time we need to connect it after creation.
        //

        public bool StatisticsEnabled
        {
            get
            {
                return (_collectstats);
            }
            set
            {
                {
                    if (value)
                    {
                        // start
                        if (ConnectionState.Open == State)
                        {
                            if (null == _statistics)
                            {
                                _statistics = new SqlStatistics();
                                ADP.TimerCurrent(out _statistics._openTimestamp);
                            }
                            // set statistics on the parser
                            // update timestamp;
                            Debug.Assert(Parser != null, "Where's the parser?");
                            Parser.Statistics = _statistics;
                        }
                    }
                    else
                    {
                        // stop
                        if (null != _statistics)
                        {
                            if (ConnectionState.Open == State)
                            {
                                // remove statistics from parser
                                // update timestamp;
                                TdsParser parser = Parser;
                                Debug.Assert(parser != null, "Where's the parser?");
                                parser.Statistics = null;
                                ADP.TimerCurrent(out _statistics._closeTimestamp);
                            }
                        }
                    }
                    _collectstats = value;
                }
            }
        }

        internal bool AsyncCommandInProgress
        {
            get => _AsyncCommandInProgress;
            set => _AsyncCommandInProgress = value;
        }

        internal SqlConnectionString.TransactionBindingEnum TransactionBinding
        {
            get => ((SqlConnectionString)ConnectionOptions).TransactionBinding;
        }

        internal SqlConnectionString.TypeSystem TypeSystem
        {
            get => ((SqlConnectionString)ConnectionOptions).TypeSystemVersion;
        }

        internal Version TypeSystemAssemblyVersion
        {
            get => ((SqlConnectionString)ConnectionOptions).TypeSystemAssemblyVersion;
        }

        internal int ConnectRetryInterval
        {
            get => ((SqlConnectionString)ConnectionOptions).ConnectRetryInterval;
        }

        public override string ConnectionString
        {
            get
            {
                return ConnectionString_Get();
            }
            set
            {
                ConnectionString_Set(new SqlConnectionPoolKey(value));
                _connectionString = value;  // Change _connectionString value only after value is validated
                CacheConnectionStringProperties();
            }
        }

        public override int ConnectionTimeout
        {
            get
            {
                SqlConnectionString constr = (SqlConnectionString)ConnectionOptions;
                return ((null != constr) ? constr.ConnectTimeout : SqlConnectionString.DEFAULT.Connect_Timeout);
            }
        }

        public override string Database
        {
            // if the connection is open, we need to ask the inner connection what it's
            // current catalog is because it may have gotten changed, otherwise we can
            // just return what the connection string had.
            get
            {
                SqlInternalConnection innerConnection = (InnerConnection as SqlInternalConnection);
                string result;

                if (null != innerConnection)
                {
                    result = innerConnection.CurrentDatabase;
                }
                else
                {
                    SqlConnectionString constr = (SqlConnectionString)ConnectionOptions;
                    result = ((null != constr) ? constr.InitialCatalog : SqlConnectionString.DEFAULT.Initial_Catalog);
                }
                return result;
            }
        }

        public override string DataSource
        {
            get
            {
                SqlInternalConnection innerConnection = (InnerConnection as SqlInternalConnection);
                string result;

                if (null != innerConnection)
                {
                    result = innerConnection.CurrentDataSource;
                }
                else
                {
                    SqlConnectionString constr = (SqlConnectionString)ConnectionOptions;
                    result = ((null != constr) ? constr.DataSource : SqlConnectionString.DEFAULT.Data_Source);
                }
                return result;
            }
        }

        public int PacketSize
        {
            // if the connection is open, we need to ask the inner connection what it's
            // current packet size is because it may have gotten changed, otherwise we
            // can just return what the connection string had.
            get
            {
                SqlInternalConnectionTds innerConnection = (InnerConnection as SqlInternalConnectionTds);
                int result;

                if (null != innerConnection)
                {
                    result = innerConnection.PacketSize;
                }
                else
                {
                    SqlConnectionString constr = (SqlConnectionString)ConnectionOptions;
                    result = ((null != constr) ? constr.PacketSize : SqlConnectionString.DEFAULT.Packet_Size);
                }
                return result;
            }
        }

        public Guid ClientConnectionId
        {
            get
            {
                SqlInternalConnectionTds innerConnection = (InnerConnection as SqlInternalConnectionTds);

                if (null != innerConnection)
                {
                    return innerConnection.ClientConnectionId;
                }
                else
                {
                    Task reconnectTask = _currentReconnectionTask;
                    if (reconnectTask != null && !reconnectTask.IsCompleted)
                    {
                        return _originalConnectionId;
                    }
                    return Guid.Empty;
                }
            }
        }

        public override string ServerVersion
        {
            get => GetOpenTdsConnection().ServerVersion;
        }

        public override ConnectionState State
        {
            get
            {
                Task reconnectTask = _currentReconnectionTask;
                if (reconnectTask != null && !reconnectTask.IsCompleted)
                {
                    return ConnectionState.Open;
                }
                return InnerConnection.State;
            }
        }


        internal SqlStatistics Statistics
        {
            get => _statistics;
        }

        public string WorkstationId
        {
            get
            {
                // If not supplied by the user, the default value is the MachineName
                // Note: In Longhorn you'll be able to rename a machine without
                // rebooting.  Therefore, don't cache this machine name.
                SqlConnectionString constr = (SqlConnectionString)ConnectionOptions;
                string result = ((null != constr) ? constr.WorkstationId : string.Empty);
                return result;
            }
        }

        protected override DbProviderFactory DbProviderFactory
        {
            get => SqlClientFactory.Instance;
        }

        // SqlCredential: Pair User Id and password in SecureString which are to be used for SQL authentication

        //
        // PUBLIC EVENTS
        //

        public event SqlInfoMessageEventHandler InfoMessage;

        public bool FireInfoMessageEventOnUserErrors
        {
            get => _fireInfoMessageEventOnUserErrors;
            set => _fireInfoMessageEventOnUserErrors = value;
        }

        // Approx. number of times that the internal connection has been reconnected
        internal int ReconnectCount
        {
            get => _reconnectCount;
        }

        internal bool ForceNewConnection { get; set; }

        protected override void OnStateChange(StateChangeEventArgs stateChange)
        {
            if (!_suppressStateChangeForReconnection)
            {
                base.OnStateChange(stateChange);
            }
        }

        //
        // PUBLIC METHODS
        //

        new public SqlTransaction BeginTransaction()
        {
            // this is just a delegate. The actual method tracks executiontime
            return BeginTransaction(IsolationLevel.Unspecified, null);
        }

        new public SqlTransaction BeginTransaction(IsolationLevel iso)
        {
            // this is just a delegate. The actual method tracks executiontime
            return BeginTransaction(iso, null);
        }

        public SqlTransaction BeginTransaction(string transactionName)
        {
            // Use transaction names only on the outermost pair of nested
            // BEGIN...COMMIT or BEGIN...ROLLBACK statements.  Transaction names
            // are ignored for nested BEGIN's.  The only way to rollback a nested
            // transaction is to have a save point from a SAVE TRANSACTION call.
            return BeginTransaction(IsolationLevel.Unspecified, transactionName);
        }

        [SuppressMessage("Microsoft.Reliability", "CA2004:RemoveCallsToGCKeepAlive")]
        override protected DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            DbTransaction transaction = BeginTransaction(isolationLevel);

            //   InnerConnection doesn't maintain a ref on the outer connection (this) and 
            //   subsequently leaves open the possibility that the outer connection could be GC'ed before the SqlTransaction
            //   is fully hooked up (leaving a DbTransaction with a null connection property). Ensure that this is reachable
            //   until the completion of BeginTransaction with KeepAlive
            GC.KeepAlive(this);

            return transaction;
        }

        public SqlTransaction BeginTransaction(IsolationLevel iso, string transactionName)
        {
            WaitForPendingReconnection();
            SqlStatistics statistics = null;

            try
            {
                statistics = SqlStatistics.StartTimer(Statistics);

                SqlTransaction transaction;
                bool isFirstAttempt = true;
                do
                {
                    transaction = GetOpenTdsConnection().BeginSqlTransaction(iso, transactionName, isFirstAttempt); // do not reconnect twice
                    Debug.Assert(isFirstAttempt || !transaction.InternalTransaction.ConnectionHasBeenRestored, "Restored connection on non-first attempt");
                    isFirstAttempt = false;
                } while (transaction.InternalTransaction.ConnectionHasBeenRestored);


                //  The GetOpenConnection line above doesn't keep a ref on the outer connection (this),
                //  and it could be collected before the inner connection can hook it to the transaction, resulting in
                //  a transaction with a null connection property.  Use GC.KeepAlive to ensure this doesn't happen.
                GC.KeepAlive(this);

                return transaction;
            }
            finally
            {
                SqlStatistics.StopTimer(statistics);
            }
        }

        public override void ChangeDatabase(string database)
        {
            SqlStatistics statistics = null;
            RepairInnerConnection();
            try
            {
                statistics = SqlStatistics.StartTimer(Statistics);
                InnerConnection.ChangeDatabase(database);
            }
            finally
            {
                SqlStatistics.StopTimer(statistics);
            }
        }

        public static void ClearAllPools()
        {
            SqlConnectionFactory.SingletonInstance.ClearAllPools();
        }

        public static void ClearPool(SqlConnection connection)
        {
            ADP.CheckArgumentNull(connection, nameof(connection));

            DbConnectionOptions connectionOptions = connection.UserConnectionOptions;
            if (null != connectionOptions)
            {
                SqlConnectionFactory.SingletonInstance.ClearPool(connection);
            }
        }


        private void CloseInnerConnection()
        {
            // CloseConnection() now handles the lock

            // The SqlInternalConnectionTds is set to OpenBusy during close, once this happens the cast below will fail and 
            // the command will no longer be cancelable.  It might be desirable to be able to cancel the close operation, but this is
            // outside of the scope of Whidbey RTM.  See (SqlCommand::Cancel) for other lock.
            InnerConnection.CloseConnection(this, ConnectionFactory);
        }

        public override void Close()
        {
            ConnectionState previousState = State;
            Guid operationId = default(Guid);
            Guid clientConnectionId = default(Guid);

            // during the call to Dispose() there is a redundant call to 
            // Close(). because of this, the second time Close() is invoked the 
            // connection is already in a closed state. this doesn't seem to be a 
            // problem except for logging, as we'll get duplicate Before/After/Error
            // log entries
            if (previousState != ConnectionState.Closed)
            { 
                operationId = s_diagnosticListener.WriteConnectionCloseBefore(this);
                // we want to cache the ClientConnectionId for After/Error logging, as when the connection 
                // is closed then we will lose this identifier
                //
                // note: caching this is only for diagnostics logging purposes
                clientConnectionId = ClientConnectionId;
            }

            SqlStatistics statistics = null;

            Exception e = null;
            try
            {
                statistics = SqlStatistics.StartTimer(Statistics);

                Task reconnectTask = _currentReconnectionTask;
                if (reconnectTask != null && !reconnectTask.IsCompleted)
                {
                    CancellationTokenSource cts = _reconnectionCancellationSource;
                    if (cts != null)
                    {
                        cts.Cancel();
                    }
                    AsyncHelper.WaitForCompletion(reconnectTask, 0, null, rethrowExceptions: false); // we do not need to deal with possible exceptions in reconnection
                    if (State != ConnectionState.Open)
                    {// if we cancelled before the connection was opened 
                        OnStateChange(DbConnectionInternal.StateChangeClosed);
                    }
                }
                CancelOpenAndWait();
                CloseInnerConnection();
                GC.SuppressFinalize(this);

                if (null != Statistics)
                {
                    ADP.TimerCurrent(out _statistics._closeTimestamp);
                }
            }
            catch (Exception ex)
            {
                e = ex;
                throw;
            }
            finally
            {
                SqlStatistics.StopTimer(statistics);

                // we only want to log this if the previous state of the 
                // connection is open, as that's the valid use-case
                if (previousState != ConnectionState.Closed)
                { 
                    if (e != null)
                    {
                        s_diagnosticListener.WriteConnectionCloseError(operationId, clientConnectionId, this, e);
                    }
                    else
                    {
                        s_diagnosticListener.WriteConnectionCloseAfter(operationId, clientConnectionId, this);
                    }
                }
            }
        }

        new public SqlCommand CreateCommand()
        {
            return new SqlCommand(null, this);
        }

        private void DisposeMe(bool disposing)
        {
            if (!disposing)
            {
                // For non-pooled connections we need to make sure that if the SqlConnection was not closed, 
                // then we release the GCHandle on the stateObject to allow it to be GCed
                // For pooled connections, we will rely on the pool reclaiming the connection
                var innerConnection = (InnerConnection as SqlInternalConnectionTds);
                if ((innerConnection != null) && (!innerConnection.ConnectionOptions.Pooling))
                {
                    var parser = innerConnection.Parser;
                    if ((parser != null) && (parser._physicalStateObj != null))
                    {
                        parser._physicalStateObj.DecrementPendingCallbacks(release: false);
                    }
                }
            }
        }


        public override void Open()
        {
            Guid operationId = s_diagnosticListener.WriteConnectionOpenBefore(this);

            PrepareStatisticsForNewConnection();

            SqlStatistics statistics = null;

            Exception e = null;
            try
            {
                statistics = SqlStatistics.StartTimer(Statistics);

                if (!TryOpen(null))
                {
                    throw ADP.InternalError(ADP.InternalErrorCode.SynchronousConnectReturnedPending);
                }
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
                    s_diagnosticListener.WriteConnectionOpenError(operationId, this, e);
                }
                else
                { 
                    s_diagnosticListener.WriteConnectionOpenAfter(operationId, this);
                }
            }
        }

        internal void RegisterWaitingForReconnect(Task waitingTask)
        {
            if (((SqlConnectionString)ConnectionOptions).MARS)
            {
                return;
            }
            Interlocked.CompareExchange(ref _asyncWaitingForReconnection, waitingTask, null);
            if (_asyncWaitingForReconnection != waitingTask)
            { // somebody else managed to register 
                throw SQL.MARSUnspportedOnConnection();
            }
        }

        private async Task ReconnectAsync(int timeout)
        {
            try
            {
                long commandTimeoutExpiration = 0;
                if (timeout > 0)
                {
                    commandTimeoutExpiration = ADP.TimerCurrent() + ADP.TimerFromSeconds(timeout);
                }
                CancellationTokenSource cts = new CancellationTokenSource();
                _reconnectionCancellationSource = cts;
                CancellationToken ctoken = cts.Token;
                int retryCount = _connectRetryCount; // take a snapshot: could be changed by modifying the connection string
                for (int attempt = 0; attempt < retryCount; attempt++)
                {
                    if (ctoken.IsCancellationRequested)
                    {
                        return;
                    }
                    try
                    {
                        try
                        {
                            ForceNewConnection = true;
                            await OpenAsync(ctoken).ConfigureAwait(false);
                            // On success, increment the reconnect count - we don't really care if it rolls over since it is approx.
                            _reconnectCount = unchecked(_reconnectCount + 1);
#if DEBUG
                            Debug.Assert(_recoverySessionData._debugReconnectDataApplied, "Reconnect data was not applied !");
#endif
                        }
                        finally
                        {
                            ForceNewConnection = false;
                        }
                        return;
                    }
                    catch (SqlException e)
                    {
                        if (attempt == retryCount - 1)
                        {
                            throw SQL.CR_AllAttemptsFailed(e, _originalConnectionId);
                        }
                        if (timeout > 0 && ADP.TimerRemaining(commandTimeoutExpiration) < ADP.TimerFromSeconds(ConnectRetryInterval))
                        {
                            throw SQL.CR_NextAttemptWillExceedQueryTimeout(e, _originalConnectionId);
                        }
                    }
                    await Task.Delay(1000 * ConnectRetryInterval, ctoken).ConfigureAwait(false);
                }
            }
            finally
            {
                _recoverySessionData = null;
                _suppressStateChangeForReconnection = false;
            }
            Debug.Assert(false, "Should not reach this point");
        }

        internal Task ValidateAndReconnect(Action beforeDisconnect, int timeout)
        {
            Task runningReconnect = _currentReconnectionTask;
            // This loop in the end will return not completed reconnect task or null
            while (runningReconnect != null && runningReconnect.IsCompleted)
            {
                // clean current reconnect task (if it is the same one we checked
                Interlocked.CompareExchange<Task>(ref _currentReconnectionTask, null, runningReconnect);
                // make sure nobody started new task (if which case we did not clean it)
                runningReconnect = _currentReconnectionTask;
            }
            if (runningReconnect == null)
            {
                if (_connectRetryCount > 0)
                {
                    SqlInternalConnectionTds tdsConn = GetOpenTdsConnection();
                    if (tdsConn._sessionRecoveryAcknowledged)
                    {
                        TdsParserStateObject stateObj = tdsConn.Parser._physicalStateObj;
                        if (!stateObj.ValidateSNIConnection())
                        {
                            if (tdsConn.Parser._sessionPool != null)
                            {
                                if (tdsConn.Parser._sessionPool.ActiveSessionsCount > 0)
                                {
                                    // >1 MARS session 
                                    if (beforeDisconnect != null)
                                    {
                                        beforeDisconnect();
                                    }
                                    OnError(SQL.CR_UnrecoverableClient(ClientConnectionId), true, null);
                                }
                            }
                            SessionData cData = tdsConn.CurrentSessionData;
                            cData.AssertUnrecoverableStateCountIsCorrect();
                            if (cData._unrecoverableStatesCount == 0)
                            {
                                bool callDisconnect = false;
                                lock (_reconnectLock)
                                {
                                    tdsConn.CheckEnlistedTransactionBinding();
                                    runningReconnect = _currentReconnectionTask; // double check after obtaining the lock
                                    if (runningReconnect == null)
                                    {
                                        if (cData._unrecoverableStatesCount == 0)
                                        { // could change since the first check, but now is stable since connection is know to be broken
                                            _originalConnectionId = ClientConnectionId;
                                            _recoverySessionData = cData;
                                            if (beforeDisconnect != null)
                                            {
                                                beforeDisconnect();
                                            }
                                            try
                                            {
                                                _suppressStateChangeForReconnection = true;
                                                tdsConn.DoomThisConnection();
                                            }
                                            catch (SqlException)
                                            {
                                            }
                                            runningReconnect = Task.Run(() => ReconnectAsync(timeout));
                                            // if current reconnect is not null, somebody already started reconnection task - some kind of race condition
                                            Debug.Assert(_currentReconnectionTask == null, "Duplicate reconnection tasks detected");
                                            _currentReconnectionTask = runningReconnect;
                                        }
                                    }
                                    else
                                    {
                                        callDisconnect = true;
                                    }
                                }
                                if (callDisconnect && beforeDisconnect != null)
                                {
                                    beforeDisconnect();
                                }
                            }
                            else
                            {
                                if (beforeDisconnect != null)
                                {
                                    beforeDisconnect();
                                }
                                OnError(SQL.CR_UnrecoverableServer(ClientConnectionId), true, null);
                            }
                        } // ValidateSNIConnection
                    } // sessionRecoverySupported                  
                } // connectRetryCount>0
            }
            else
            { // runningReconnect = null
                if (beforeDisconnect != null)
                {
                    beforeDisconnect();
                }
            }
            return runningReconnect;
        }

        // this is straightforward, but expensive method to do connection resiliency - it take locks and all preparations as for TDS request
        partial void RepairInnerConnection()
        {
            WaitForPendingReconnection();
            if (_connectRetryCount == 0)
            {
                return;
            }
            SqlInternalConnectionTds tdsConn = InnerConnection as SqlInternalConnectionTds;
            if (tdsConn != null)
            {
                tdsConn.ValidateConnectionForExecute(null);
                tdsConn.GetSessionAndReconnectIfNeeded((SqlConnection)this);
            }
        }

        private void WaitForPendingReconnection()
        {
            Task reconnectTask = _currentReconnectionTask;
            if (reconnectTask != null && !reconnectTask.IsCompleted)
            {
                AsyncHelper.WaitForCompletion(reconnectTask, 0, null, rethrowExceptions: false);
            }
        }

        private void CancelOpenAndWait()
        {
            // copy from member to avoid changes by background thread
            var completion = _currentCompletion;
            if (completion != null)
            {
                completion.Item1.TrySetCanceled();
                ((IAsyncResult)completion.Item2).AsyncWaitHandle.WaitOne();
            }
            Debug.Assert(_currentCompletion == null, "After waiting for an async call to complete, there should be no completion source");
        }

        public override Task OpenAsync(CancellationToken cancellationToken)
        {
            Guid operationId = s_diagnosticListener.WriteConnectionOpenBefore(this);

            PrepareStatisticsForNewConnection();
            
            SqlStatistics statistics = null;
            try
            {
                statistics = SqlStatistics.StartTimer(Statistics);

                System.Transactions.Transaction transaction = ADP.GetCurrentTransaction();
                TaskCompletionSource<DbConnectionInternal> completion = new TaskCompletionSource<DbConnectionInternal>(transaction);
                TaskCompletionSource<object> result = new TaskCompletionSource<object>();

                if (s_diagnosticListener.IsEnabled(SqlClientDiagnosticListenerExtensions.SqlAfterOpenConnection) ||
                    s_diagnosticListener.IsEnabled(SqlClientDiagnosticListenerExtensions.SqlErrorOpenConnection))
                { 
                    result.Task.ContinueWith((t) =>
                    {
                        if (t.Exception != null)
                        {
                            s_diagnosticListener.WriteConnectionOpenError(operationId, this, t.Exception);
                        }
                        else
                        { 
                            s_diagnosticListener.WriteConnectionOpenAfter(operationId, this);
                        }
                    }, TaskScheduler.Default);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    result.SetCanceled();
                    return result.Task;
                }


                bool completed;

                try
                {
                    completed = TryOpen(completion);
                }
                catch (Exception e)
                {
                    s_diagnosticListener.WriteConnectionOpenError(operationId, this, e);
                    result.SetException(e);
                    return result.Task;
                }

                if (completed)
                {
                    result.SetResult(null);
                }
                else
                {
                    CancellationTokenRegistration registration = new CancellationTokenRegistration();
                    if (cancellationToken.CanBeCanceled)
                    {
                        registration = cancellationToken.Register(s => ((TaskCompletionSource<DbConnectionInternal>)s).TrySetCanceled(), completion);
                    }
                    OpenAsyncRetry retry = new OpenAsyncRetry(this, completion, result, registration);
                    _currentCompletion = new Tuple<TaskCompletionSource<DbConnectionInternal>, Task>(completion, result.Task);
                    completion.Task.ContinueWith(retry.Retry, TaskScheduler.Default);
                    return result.Task;
                }

                return result.Task;
            }
            catch (Exception ex)
            {
                s_diagnosticListener.WriteConnectionOpenError(operationId, this, ex);
                throw;
            }
            finally
            {
                SqlStatistics.StopTimer(statistics);
            }
        }

        public override DataTable GetSchema()
        {
            return GetSchema(DbMetaDataCollectionNames.MetaDataCollections, null);
        }

        public override DataTable GetSchema(string collectionName)
        {
            return GetSchema(collectionName, null);
        }

        public override DataTable GetSchema(string collectionName, string[] restrictionValues)
        {
            return InnerConnection.GetSchema(ConnectionFactory, PoolGroup, this, collectionName, restrictionValues);
        }

        private class OpenAsyncRetry
        {
            private SqlConnection _parent;
            private TaskCompletionSource<DbConnectionInternal> _retry;
            private TaskCompletionSource<object> _result;
            private CancellationTokenRegistration _registration;

            public OpenAsyncRetry(SqlConnection parent, TaskCompletionSource<DbConnectionInternal> retry, TaskCompletionSource<object> result, CancellationTokenRegistration registration)
            {
                _parent = parent;
                _retry = retry;
                _result = result;
                _registration = registration;
            }

            internal void Retry(Task<DbConnectionInternal> retryTask)
            {
                _registration.Dispose();
                try
                {
                    SqlStatistics statistics = null;
                    try
                    {
                        statistics = SqlStatistics.StartTimer(_parent.Statistics);

                        if (retryTask.IsFaulted)
                        {
                            Exception e = retryTask.Exception.InnerException;
                            _parent.CloseInnerConnection();
                            _parent._currentCompletion = null;
                            _result.SetException(retryTask.Exception.InnerException);
                        }
                        else if (retryTask.IsCanceled)
                        {
                            _parent.CloseInnerConnection();
                            _parent._currentCompletion = null;
                            _result.SetCanceled();
                        }
                        else
                        {
                            bool result;
                            // protect continuation from races with close and cancel
                            lock (_parent.InnerConnection)
                            {
                                result = _parent.TryOpen(_retry);
                            }
                            if (result)
                            {
                                _parent._currentCompletion = null;
                                _result.SetResult(null);
                            }
                            else
                            {
                                _parent.CloseInnerConnection();
                                _parent._currentCompletion = null;
                                _result.SetException(ADP.ExceptionWithStackTrace(ADP.InternalError(ADP.InternalErrorCode.CompletedConnectReturnedPending)));
                            }
                        }
                    }
                    finally
                    {
                        SqlStatistics.StopTimer(statistics);
                    }
                }
                catch (Exception e)
                {
                    _parent.CloseInnerConnection();
                    _parent._currentCompletion = null;
                    _result.SetException(e);
                }
            }
        }

        private void PrepareStatisticsForNewConnection()
        {
            if (StatisticsEnabled ||
                s_diagnosticListener.IsEnabled(SqlClientDiagnosticListenerExtensions.SqlAfterExecuteCommand) ||
                s_diagnosticListener.IsEnabled(SqlClientDiagnosticListenerExtensions.SqlAfterOpenConnection))
            {
                if (null == _statistics)
                {
                    _statistics = new SqlStatistics();
                }
                else
                {
                    _statistics.ContinueOnNewConnection();
                }
            }
        }

        private bool TryOpen(TaskCompletionSource<DbConnectionInternal> retry)
        {
            SqlConnectionString connectionOptions = (SqlConnectionString)ConnectionOptions;

            _applyTransientFaultHandling = (retry == null && connectionOptions != null && connectionOptions.ConnectRetryCount > 0);

            if (ForceNewConnection)
            {
                if (!InnerConnection.TryReplaceConnection(this, ConnectionFactory, retry, UserConnectionOptions))
                {
                    return false;
                }
            }
            else
            {
                if (!InnerConnection.TryOpenConnection(this, ConnectionFactory, retry, UserConnectionOptions))
                {
                    return false;
                }
            }
            // does not require GC.KeepAlive(this) because of ReRegisterForFinalize below.

            var tdsInnerConnection = (SqlInternalConnectionTds)InnerConnection;

            Debug.Assert(tdsInnerConnection.Parser != null, "Where's the parser?");

            if (!tdsInnerConnection.ConnectionOptions.Pooling)
            {
                // For non-pooled connections, we need to make sure that the finalizer does actually run to avoid leaking SNI handles
                GC.ReRegisterForFinalize(this);
            }

            // The _statistics can change with StatisticsEnabled. Copying to a local variable before checking for a null value.
            SqlStatistics statistics = _statistics;
            if (StatisticsEnabled ||
                (s_diagnosticListener.IsEnabled(SqlClientDiagnosticListenerExtensions.SqlAfterExecuteCommand) && statistics != null))
            {
                ADP.TimerCurrent(out _statistics._openTimestamp);
                tdsInnerConnection.Parser.Statistics = _statistics;
            }
            else
            {
                tdsInnerConnection.Parser.Statistics = null;
                _statistics = null; // in case of previous Open/Close/reset_CollectStats sequence
            }

            return true;
        }


        //
        // INTERNAL PROPERTIES
        //

        internal bool HasLocalTransaction
        {
            get
            {
                return GetOpenTdsConnection().HasLocalTransaction;
            }
        }

        internal bool HasLocalTransactionFromAPI
        {
            get
            {
                Task reconnectTask = _currentReconnectionTask;
                if (reconnectTask != null && !reconnectTask.IsCompleted)
                {
                    return false; //we will not go into reconnection if we are inside the transaction
                }
                return GetOpenTdsConnection().HasLocalTransactionFromAPI;
            }
        }


        internal bool IsKatmaiOrNewer
        {
            get
            {
                if (_currentReconnectionTask != null)
                { // holds true even if task is completed
                    return true; // if CR is enabled, connection, if established, will be Katmai+
                }
                return GetOpenTdsConnection().IsKatmaiOrNewer;
            }
        }

        internal TdsParser Parser
        {
            get
            {
                SqlInternalConnectionTds tdsConnection = GetOpenTdsConnection();
                return tdsConnection.Parser;
            }
        }


        //
        // INTERNAL METHODS
        //

        internal void ValidateConnectionForExecute(string method, SqlCommand command)
        {
            Task asyncWaitingForReconnection = _asyncWaitingForReconnection;
            if (asyncWaitingForReconnection != null)
            {
                if (!asyncWaitingForReconnection.IsCompleted)
                {
                    throw SQL.MARSUnspportedOnConnection();
                }
                else
                {
                    Interlocked.CompareExchange(ref _asyncWaitingForReconnection, null, asyncWaitingForReconnection);
                }
            }
            if (_currentReconnectionTask != null)
            {
                Task currentReconnectionTask = _currentReconnectionTask;
                if (currentReconnectionTask != null && !currentReconnectionTask.IsCompleted)
                {
                    return; // execution will wait for this task later
                }
            }
            SqlInternalConnectionTds innerConnection = GetOpenTdsConnection(method);
            innerConnection.ValidateConnectionForExecute(command);
        }

        // Surround name in brackets and then escape any end bracket to protect against SQL Injection.
        // NOTE: if the user escapes it themselves it will not work, but this was the case in V1 as well
        // as native OleDb and Odbc.
        internal static string FixupDatabaseTransactionName(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                return SqlServerEscapeHelper.EscapeIdentifier(name);
            }
            else
            {
                return name;
            }
        }

        // If wrapCloseInAction is defined, then the action it defines will be run with the connection close action passed in as a parameter
        // The close action also supports being run asynchronously
        internal void OnError(SqlException exception, bool breakConnection, Action<Action> wrapCloseInAction)
        {
            Debug.Assert(exception != null && exception.Errors.Count != 0, "SqlConnection: OnError called with null or empty exception!");


            if (breakConnection && (ConnectionState.Open == State))
            {
                if (wrapCloseInAction != null)
                {
                    int capturedCloseCount = _closeCount;

                    Action closeAction = () =>
                    {
                        if (capturedCloseCount == _closeCount)
                        {
                            Close();
                        }
                    };

                    wrapCloseInAction(closeAction);
                }
                else
                {
                    Close();
                }
            }

            if (exception.Class >= TdsEnums.MIN_ERROR_CLASS)
            {
                // It is an error, and should be thrown.  Class of TdsEnums.MIN_ERROR_CLASS or above is an error,
                // below TdsEnums.MIN_ERROR_CLASS denotes an info message.
                throw exception;
            }
            else
            {
                // If it is a class < TdsEnums.MIN_ERROR_CLASS, it is a warning collection - so pass to handler
                this.OnInfoMessage(new SqlInfoMessageEventArgs(exception));
            }
        }

        //
        // PRIVATE METHODS
        //


        internal SqlInternalConnectionTds GetOpenTdsConnection()
        {
            SqlInternalConnectionTds innerConnection = (InnerConnection as SqlInternalConnectionTds);
            if (null == innerConnection)
            {
                throw ADP.ClosedConnectionError();
            }
            return innerConnection;
        }

        internal SqlInternalConnectionTds GetOpenTdsConnection(string method)
        {
            SqlInternalConnectionTds innerConnection = (InnerConnection as SqlInternalConnectionTds);
            if (null == innerConnection)
            {
                throw ADP.OpenConnectionRequired(method, InnerConnection.State);
            }
            return innerConnection;
        }

        internal void OnInfoMessage(SqlInfoMessageEventArgs imevent)
        {
            bool notified;
            OnInfoMessage(imevent, out notified);
        }

        internal void OnInfoMessage(SqlInfoMessageEventArgs imevent, out bool notified)
        {
            SqlInfoMessageEventHandler handler = InfoMessage;
            if (null != handler)
            {
                notified = true;
                try
                {
                    handler(this, imevent);
                }
                catch (Exception e)
                {
                    if (!ADP.IsCatchableOrSecurityExceptionType(e))
                    {
                        throw;
                    }
                }
            }
            else
            {
                notified = false;
            }
        }

        //
        // SQL DEBUGGING SUPPORT
        //

        // this only happens once per connection
        // SxS: using named file mapping APIs

        internal void RegisterForConnectionCloseNotification<T>(ref Task<T> outerTask, object value, int tag)
        {
            // Connection exists,  schedule removal, will be added to ref collection after calling ValidateAndReconnect
            outerTask = outerTask.ContinueWith(task =>
            {
                RemoveWeakReference(value);
                return task;
            }, TaskScheduler.Default).Unwrap();
        }


        public void ResetStatistics()
        {
            if (null != Statistics)
            {
                Statistics.Reset();
                if (ConnectionState.Open == State)
                {
                    // update timestamp;
                    ADP.TimerCurrent(out _statistics._openTimestamp);
                }
            }
        }

        public IDictionary RetrieveStatistics()
        {
            if (null != Statistics)
            {
                UpdateStatistics();
                return Statistics.GetDictionary();
            }
            else
            {
                return new SqlStatistics().GetDictionary();
            }
        }

        private void UpdateStatistics()
        {
            if (ConnectionState.Open == State)
            {
                // update timestamp
                ADP.TimerCurrent(out _statistics._closeTimestamp);
            }
            // delegate the rest of the work to the SqlStatistics class
            Statistics.UpdateStatistics();
        }

        object ICloneable.Clone() => new SqlConnection(this);

        private void CopyFrom(SqlConnection connection)
        {
            ADP.CheckArgumentNull(connection, nameof(connection));
            _userConnectionOptions = connection.UserConnectionOptions;
            _poolGroup = connection.PoolGroup;
            
            if (DbConnectionClosedNeverOpened.SingletonInstance == connection._innerConnection)
            {
                _innerConnection = DbConnectionClosedNeverOpened.SingletonInstance;
            }
            else
            {
                _innerConnection = DbConnectionClosedPreviouslyOpened.SingletonInstance;
            }
        }

        // UDT SUPPORT
        private Assembly ResolveTypeAssembly(AssemblyName asmRef, bool throwOnError)
        {
            Debug.Assert(TypeSystemAssemblyVersion != null, "TypeSystemAssembly should be set !");
            if (string.Compare(asmRef.Name, "Microsoft.SqlServer.Types", StringComparison.OrdinalIgnoreCase) == 0)
            {
                asmRef.Version = TypeSystemAssemblyVersion;
            }
            try
            {
                return Assembly.Load(asmRef);
            }
            catch (Exception e)
            {
                if (throwOnError || !ADP.IsCatchableExceptionType(e))
                {
                    throw;
                }
                else
                {
                    return null;
                };
            }
        }

        internal void CheckGetExtendedUDTInfo(SqlMetaDataPriv metaData, bool fThrow)
        {
            if (metaData.udtType == null)
            { // If null, we have not obtained extended info.
                Debug.Assert(!string.IsNullOrEmpty(metaData.udtAssemblyQualifiedName), "Unexpected state on GetUDTInfo");
                // Parameter throwOnError determines whether exception from Assembly.Load is thrown.
                metaData.udtType =
                    Type.GetType(typeName: metaData.udtAssemblyQualifiedName, assemblyResolver: asmRef => ResolveTypeAssembly(asmRef, fThrow), typeResolver: null, throwOnError: fThrow);

                if (fThrow && metaData.udtType == null)
                {
                    throw SQL.UDTUnexpectedResult(metaData.udtAssemblyQualifiedName);
                }
            }
        }

        internal object GetUdtValue(object value, SqlMetaDataPriv metaData, bool returnDBNull)
        {
            if (returnDBNull && ADP.IsNull(value))
            {
                return DBNull.Value;
            }

            object o = null;

            // Since the serializer doesn't handle nulls...
            if (ADP.IsNull(value))
            {
                Type t = metaData.udtType;
                Debug.Assert(t != null, "Unexpected null of udtType on GetUdtValue!");
                o = t.InvokeMember("Null", BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.Static, null, null, new object[] { }, CultureInfo.InvariantCulture);
                Debug.Assert(o != null);
                return o;
            }
            else
            {

                MemoryStream stm = new MemoryStream((byte[])value);

                o = SerializationHelperSql9.Deserialize(stm, metaData.udtType);

                Debug.Assert(o != null, "object could NOT be created");
                return o;
            }
        }

        internal byte[] GetBytes(object o)
        {
            Format format = Format.Native;
            return GetBytes(o, out format, out int maxSize);
        }

        internal byte[] GetBytes(object o, out Format format, out int maxSize)
        {
            SqlUdtInfo attr = GetInfoFromType(o.GetType());
            maxSize = attr.MaxByteSize;
            format = attr.SerializationFormat;

            if (maxSize < -1 || maxSize >= ushort.MaxValue)
            {
                throw new InvalidOperationException(o.GetType() + ": invalid Size");
            }

            byte[] retval;

            using (MemoryStream stm = new MemoryStream(maxSize < 0 ? 0 : maxSize))
            {
                SerializationHelperSql9.Serialize(stm, o);
                retval = stm.ToArray();
            }
            return retval;
        }

        private SqlUdtInfo GetInfoFromType(Type t)
        {
            Debug.Assert(t != null, "Type object cant be NULL");
            Type orig = t;
            do
            {
                SqlUdtInfo attr = SqlUdtInfo.TryGetFromType(t);
                if (attr != null)
                {
                    return attr;
                }
                else
                {
                    t = t.BaseType;
                }
            }
            while (t != null);

            throw SQL.UDTInvalidSqlType(orig.AssemblyQualifiedName);
        }
    }
}


