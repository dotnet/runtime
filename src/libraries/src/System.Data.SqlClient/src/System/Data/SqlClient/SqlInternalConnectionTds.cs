// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Data.Common;
using System.Data.ProviderBase;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Transactions;
using System.Security;

namespace System.Data.SqlClient
{
    internal class SessionStateRecord
    {
        internal bool _recoverable;
        internal uint _version;
        internal int _dataLength;
        internal byte[] _data;
    }

    internal class SessionData
    {
        internal const int _maxNumberOfSessionStates = 256;
        internal uint _tdsVersion;
        internal bool _encrypted;

        internal string _database;
        internal SqlCollation _collation;
        internal string _language;

        internal string _initialDatabase;
        internal SqlCollation _initialCollation;
        internal string _initialLanguage;

        internal byte _unrecoverableStatesCount = 0;
        internal Dictionary<string, Tuple<string, string>> _resolvedAliases;

#if DEBUG
        internal bool _debugReconnectDataApplied;
#endif

        internal SessionStateRecord[] _delta = new SessionStateRecord[_maxNumberOfSessionStates];
        internal bool _deltaDirty = false;
        internal byte[][] _initialState = new byte[_maxNumberOfSessionStates][];

        public SessionData(SessionData recoveryData)
        {
            _initialDatabase = recoveryData._initialDatabase;
            _initialCollation = recoveryData._initialCollation;
            _initialLanguage = recoveryData._initialLanguage;
            _resolvedAliases = recoveryData._resolvedAliases;

            for (int i = 0; i < _maxNumberOfSessionStates; i++)
            {
                if (recoveryData._initialState[i] != null)
                {
                    _initialState[i] = (byte[])recoveryData._initialState[i].Clone();
                }
            }
        }

        public SessionData()
        {
            _resolvedAliases = new Dictionary<string, Tuple<string, string>>(2);
        }

        public void Reset()
        {
            _database = null;
            _collation = null;
            _language = null;
            if (_deltaDirty)
            {
                _delta = new SessionStateRecord[_maxNumberOfSessionStates];
                _deltaDirty = false;
            }
            _unrecoverableStatesCount = 0;
        }

        [Conditional("DEBUG")]
        public void AssertUnrecoverableStateCountIsCorrect()
        {
            byte unrecoverableCount = 0;
            foreach (var state in _delta)
            {
                if (state != null && !state._recoverable)
                    unrecoverableCount++;
            }
            Debug.Assert(unrecoverableCount == _unrecoverableStatesCount, "Unrecoverable count does not match");
        }
    }

    sealed internal class SqlInternalConnectionTds : SqlInternalConnection, IDisposable
    {
        // CONNECTION AND STATE VARIABLES
        private readonly SqlConnectionPoolGroupProviderInfo _poolGroupProviderInfo; // will only be null when called for ChangePassword, or creating SSE User Instance
        private TdsParser _parser;
        private SqlLoginAck _loginAck;
        private SqlCredential _credential;
        private FederatedAuthenticationFeatureExtensionData? _fedAuthFeatureExtensionData;

        // Connection Resiliency
        private bool _sessionRecoveryRequested;
        internal bool _sessionRecoveryAcknowledged;
        internal SessionData _currentSessionData; // internal for use from TdsParser only, other should use CurrentSessionData property that will fix database and language
        private SessionData _recoverySessionData;

        // Federated Authentication
        // Response obtained from the server for FEDAUTHREQUIRED prelogin option.
        internal bool _fedAuthRequired;
        internal bool _federatedAuthenticationRequested;
        internal bool _federatedAuthenticationAcknowledged;
        internal byte[] _accessTokenInBytes;

        // The errors in the transient error set are contained in
        // https://azure.microsoft.com/en-us/documentation/articles/sql-database-develop-error-messages/#transient-faults-connection-loss-and-other-temporary-errors
        private static readonly HashSet<int> s_transientErrors = new HashSet<int>
        {
            // SQL Error Code: 4060
            // Cannot open database "%.*ls" requested by the login. The login failed.
            4060,

            // SQL Error Code: 10928
            // Resource ID: %d. The %s limit for the database is %d and has been reached.
            10928,

            // SQL Error Code: 10929
            // Resource ID: %d. The %s minimum guarantee is %d, maximum limit is %d and the current usage for the database is %d. 
            // However, the server is currently too busy to support requests greater than %d for this database.
            10929,

            // SQL Error Code: 40197
            // You will receive this error, when the service is down due to software or hardware upgrades, hardware failures, 
            // or any other failover problems. The error code (%d) embedded within the message of error 40197 provides 
            // additional information about the kind of failure or failover that occurred. Some examples of the error codes are 
            // embedded within the message of error 40197 are 40020, 40143, 40166, and 40540.
            40197,

            // The service is currently busy. Retry the request after 10 seconds. Incident ID: %ls. Code: %d.
            40501,

            // Database '%.*ls' on server '%.*ls' is not currently available. Please retry the connection later. 
            // If the problem persists, contact customer support, and provide them the session tracing ID of '%.*ls'.
            40613
        };

        internal SessionData CurrentSessionData
        {
            get
            {
                if (_currentSessionData != null)
                {
                    _currentSessionData._database = CurrentDatabase;
                    _currentSessionData._language = _currentLanguage;
                }
                return _currentSessionData;
            }
        }

        // FOR POOLING
        private bool _fConnectionOpen = false;

        // FOR CONNECTION RESET MANAGEMENT
        private bool _fResetConnection;
        private string _originalDatabase;
        private string _currentFailoverPartner;                     // only set by ENV change from server
        private string _originalLanguage;
        private string _currentLanguage;
        private int _currentPacketSize;
        private int _asyncCommandCount; // number of async Begins minus number of async Ends.

        // FOR SSE
        private string _instanceName = string.Empty;

        // FOR NOTIFICATIONS
        private DbConnectionPoolIdentity _identity; // Used to lookup info for notification matching Start().

        // FOR SYNCHRONIZATION IN TdsParser
        // How to use these locks:
        // 1. Whenever writing to the connection (with the exception of Cancellation) the _parserLock MUST be taken
        // 2. _parserLock will also be taken during close (to prevent closing in the middle of a write)
        // 3. Whenever you have the _parserLock and are calling a method that would cause the connection to close if it failed (with the exception of any writing method), you MUST set ThreadHasParserLockForClose to true
        //      * This is to prevent the connection deadlocking with itself (since you already have the _parserLock, and Closing the connection will attempt to re-take that lock)
        //      * It is safe to set ThreadHasParserLockForClose to true when writing as well, but it is unnecessary
        //      * If you have a method that takes _parserLock, it is a good idea check ThreadHasParserLockForClose first (if you don't expect _parserLock to be taken by something higher on the stack, then you should at least assert that it is false)
        // 4. ThreadHasParserLockForClose is thread-specific - this means that you must set it to false before returning a Task, and set it back to true in the continuation
        // 5. ThreadHasParserLockForClose should only be modified if you currently own the _parserLock
        // 6. Reading ThreadHasParserLockForClose is thread-safe
        internal class SyncAsyncLock
        {
            private SemaphoreSlim _semaphore = new SemaphoreSlim(1);

            internal void Wait(bool canReleaseFromAnyThread)
            {
                Monitor.Enter(_semaphore); // semaphore is used as lock object, no relation to SemaphoreSlim.Wait/Release methods
                if (canReleaseFromAnyThread || _semaphore.CurrentCount == 0)
                {
                    _semaphore.Wait();
                    if (canReleaseFromAnyThread)
                    {
                        Monitor.Exit(_semaphore);
                    }
                    else
                    {
                        _semaphore.Release();
                    }
                }
            }

            internal void Wait(bool canReleaseFromAnyThread, int timeout, ref bool lockTaken)
            {
                lockTaken = false;
                bool hasMonitor = false;
                try
                {
                    Monitor.TryEnter(_semaphore, timeout, ref hasMonitor); // semaphore is used as lock object, no relation to SemaphoreSlim.Wait/Release methods
                    if (hasMonitor)
                    {
                        if ((canReleaseFromAnyThread) || (_semaphore.CurrentCount == 0))
                        {
                            if (_semaphore.Wait(timeout))
                            {
                                if (canReleaseFromAnyThread)
                                {
                                    Monitor.Exit(_semaphore);
                                    hasMonitor = false;
                                }
                                else
                                {
                                    _semaphore.Release();
                                }
                                lockTaken = true;
                            }
                        }
                        else
                        {
                            lockTaken = true;
                        }
                    }
                }
                finally
                {
                    if ((!lockTaken) && (hasMonitor))
                    {
                        Monitor.Exit(_semaphore);
                    }
                }
            }

            internal void Release()
            {
                if (_semaphore.CurrentCount == 0)
                {  //  semaphore methods were used for locking                   
                    _semaphore.Release();
                }
                else
                {
                    Monitor.Exit(_semaphore);
                }
            }


            internal bool CanBeReleasedFromAnyThread
            {
                get
                {
                    return _semaphore.CurrentCount == 0;
                }
            }

            // Necessary but not sufficient condition for thread to have lock (since semaphore may be obtained by any thread)            
            internal bool ThreadMayHaveLock()
            {
                return Monitor.IsEntered(_semaphore) || _semaphore.CurrentCount == 0;
            }
        }


        internal SyncAsyncLock _parserLock = new SyncAsyncLock();
        private int _threadIdOwningParserLock = -1;

        private SqlConnectionTimeoutErrorInternal _timeoutErrorInternal;

        internal SqlConnectionTimeoutErrorInternal TimeoutErrorInternal
        {
            get { return _timeoutErrorInternal; }
        }

        // OTHER STATE VARIABLES AND REFERENCES

        internal Guid _clientConnectionId = Guid.Empty;

        // Routing information (ROR)
        private RoutingInfo _routingInfo = null;
        private Guid _originalClientConnectionId = Guid.Empty;
        private string _routingDestination = null;
        private readonly TimeoutTimer _timeout;

        // although the new password is generally not used it must be passed to the ctor
        // the new Login7 packet will always write out the new password (or a length of zero and no bytes if not present)
        //
        internal SqlInternalConnectionTds(
                DbConnectionPoolIdentity identity,
                SqlConnectionString connectionOptions,
                SqlCredential credential,
                object providerInfo,
                string newPassword,
                SecureString newSecurePassword,
                bool redirectedUserInstance,
                SqlConnectionString userConnectionOptions = null, // NOTE: userConnectionOptions may be different to connectionOptions if the connection string has been expanded (see SqlConnectionString.Expand)
                SessionData reconnectSessionData = null,
                bool applyTransientFaultHandling = false,
                string accessToken = null) : base(connectionOptions)

        {
#if DEBUG
            if (reconnectSessionData != null)
            {
                reconnectSessionData._debugReconnectDataApplied = true;
            }
#endif
            Debug.Assert(reconnectSessionData == null || connectionOptions.ConnectRetryCount > 0, "Reconnect data supplied with CR turned off");

            if (connectionOptions.ConnectRetryCount > 0)
            {
                _recoverySessionData = reconnectSessionData;
                if (reconnectSessionData == null)
                {
                    _currentSessionData = new SessionData();
                }
                else
                {
                    _currentSessionData = new SessionData(_recoverySessionData);
                    _originalDatabase = _recoverySessionData._initialDatabase;
                    _originalLanguage = _recoverySessionData._initialLanguage;
                }
            }

            if (accessToken != null)
            {
                _accessTokenInBytes = System.Text.Encoding.Unicode.GetBytes(accessToken);
            }

            _identity = identity;
            Debug.Assert(newSecurePassword != null || newPassword != null, "cannot have both new secure change password and string based change password to be null");
            Debug.Assert(credential == null || (string.IsNullOrEmpty(connectionOptions.UserID) && string.IsNullOrEmpty(connectionOptions.Password)), "cannot mix the new secure password system and the connection string based password");

            Debug.Assert(credential == null || !connectionOptions.IntegratedSecurity, "Cannot use SqlCredential and Integrated Security");

            _poolGroupProviderInfo = (SqlConnectionPoolGroupProviderInfo)providerInfo;
            _fResetConnection = connectionOptions.ConnectionReset;
            if (_fResetConnection && _recoverySessionData == null)
            {
                _originalDatabase = connectionOptions.InitialCatalog;
                _originalLanguage = connectionOptions.CurrentLanguage;
            }

            _timeoutErrorInternal = new SqlConnectionTimeoutErrorInternal();
            _credential = credential;

            _parserLock.Wait(canReleaseFromAnyThread: false);
            ThreadHasParserLockForClose = true;   // In case of error, let ourselves know that we already own the parser lock

            try
            {
                _timeout = TimeoutTimer.StartSecondsTimeout(connectionOptions.ConnectTimeout);

                // If transient fault handling is enabled then we can retry the login up to the ConnectRetryCount.
                int connectionEstablishCount = applyTransientFaultHandling ? connectionOptions.ConnectRetryCount + 1 : 1;
                int transientRetryIntervalInMilliSeconds = connectionOptions.ConnectRetryInterval * 1000; // Max value of transientRetryInterval is 60*1000 ms. The max value allowed for ConnectRetryInterval is 60
                for (int i = 0; i < connectionEstablishCount; i++)
                {
                    try
                    {
                        OpenLoginEnlist(_timeout, connectionOptions, credential, newPassword, newSecurePassword, redirectedUserInstance);

                        break;
                    }
                    catch (SqlException sqlex)
                    {
                        if (i + 1 == connectionEstablishCount
                            || !applyTransientFaultHandling
                            || _timeout.IsExpired
                            || _timeout.MillisecondsRemaining < transientRetryIntervalInMilliSeconds
                            || !IsTransientError(sqlex))
                        {
                            throw sqlex;
                        }
                        else
                        {
                            Thread.Sleep(transientRetryIntervalInMilliSeconds);
                        }
                    }
                }
            }
            finally
            {
                ThreadHasParserLockForClose = false;
                _parserLock.Release();
            }
        }

        
        // Returns true if the Sql error is a transient.
        private bool IsTransientError(SqlException exc)
        {
            if (exc == null)
            {
                return false;
            }
            foreach (SqlError error in exc.Errors)
            {
                if (s_transientErrors.Contains(error.Number))
                {
                    return true;
                }
            }
            return false;
        }

        internal Guid ClientConnectionId
        {
            get
            {
                return _clientConnectionId;
            }
        }

        internal Guid OriginalClientConnectionId
        {
            get
            {
                return _originalClientConnectionId;
            }
        }

        internal string RoutingDestination
        {
            get
            {
                return _routingDestination;
            }
        }

        internal override SqlInternalTransaction CurrentTransaction
        {
            get
            {
                return _parser.CurrentTransaction;
            }
        }

        internal override SqlInternalTransaction AvailableInternalTransaction
        {
            get
            {
                return _parser._fResetConnection ? null : CurrentTransaction;
            }
        }

        internal override SqlInternalTransaction PendingTransaction
        {
            get
            {
                return _parser.PendingTransaction;
            }
        }

        internal DbConnectionPoolIdentity Identity
        {
            get
            {
                return _identity;
            }
        }

        internal string InstanceName
        {
            get
            {
                return _instanceName;
            }
        }

        internal override bool IsLockedForBulkCopy
        {
            get
            {
                return (!Parser.MARSOn && Parser._physicalStateObj.BcpLock);
            }
        }

        internal protected override bool IsNonPoolableTransactionRoot
        {
            get
            {
                return IsTransactionRoot && (!IsKatmaiOrNewer || null == Pool);
            }
        }

        internal override bool IsKatmaiOrNewer
        {
            get
            {
                return _parser.IsKatmaiOrNewer;
            }
        }

        internal int PacketSize
        {
            get
            {
                return _currentPacketSize;
            }
        }

        internal TdsParser Parser
        {
            get
            {
                return _parser;
            }
        }

        internal string ServerProvidedFailOverPartner
        {
            get
            {
                return _currentFailoverPartner;
            }
        }

        internal SqlConnectionPoolGroupProviderInfo PoolGroupProviderInfo
        {
            get
            {
                return _poolGroupProviderInfo;
            }
        }

        protected override bool ReadyToPrepareTransaction
        {
            get
            {
                bool result = (null == FindLiveReader(null)); // can't prepare with a live data reader...
                return result;
            }
        }

        public override string ServerVersion
        {
            get
            {
                return (string.Format("{0:00}.{1:00}.{2:0000}", _loginAck.majorVersion,
                       (short)_loginAck.minorVersion, _loginAck.buildNum));
            }
        }

        protected override bool UnbindOnTransactionCompletion
        {
            get
            {
                return false;
            }
        }


        ////////////////////////////////////////////////////////////////////////////////////////
        // GENERAL METHODS
        ////////////////////////////////////////////////////////////////////////////////////////
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters")] // copied from Triaged.cs
        protected override void ChangeDatabaseInternal(string database)
        {
            // Add brackets around database
            database = SqlConnection.FixupDatabaseTransactionName(database);
            Task executeTask = _parser.TdsExecuteSQLBatch("use " + database, ConnectionOptions.ConnectTimeout, null, _parser._physicalStateObj, sync: true);
            Debug.Assert(executeTask == null, "Shouldn't get a task when doing sync writes");
            _parser.Run(RunBehavior.UntilDone, null, null, null, _parser._physicalStateObj);
        }

        public override void Dispose()
        {
            try
            {
                TdsParser parser = Interlocked.Exchange(ref _parser, null);  // guard against multiple concurrent dispose calls -- Delegated Transactions might cause this.

                Debug.Assert(parser != null && _fConnectionOpen || parser == null && !_fConnectionOpen, "Unexpected state on dispose");
                if (null != parser)
                {
                    parser.Disconnect();
                }
            }
            finally
            {
                // close will always close, even if exception is thrown
                // remember to null out any object references
                _loginAck = null;
                _fConnectionOpen = false; // mark internal connection as closed
            }
            base.Dispose();
        }

        internal override void ValidateConnectionForExecute(SqlCommand command)
        {
            TdsParser parser = _parser;
            if ((parser == null) || (parser.State == TdsParserState.Broken) || (parser.State == TdsParserState.Closed))
            {
                throw ADP.ClosedConnectionError();
            }
            else
            {
                SqlDataReader reader = null;
                if (parser.MARSOn)
                {
                    if (null != command)
                    { // command can't have datareader already associated with it
                        reader = FindLiveReader(command);
                    }
                }
                else
                { // single execution/datareader per connection
                    if (_asyncCommandCount > 0)
                    {
                        throw SQL.MARSUnspportedOnConnection();
                    }

                    reader = FindLiveReader(null);
                }
                if (null != reader)
                {
                    // if MARS is on, then a datareader associated with the command exists
                    // or if MARS is off, then a datareader exists
                    throw ADP.OpenReaderExists();
                }
                else if (!parser.MARSOn && parser._physicalStateObj._pendingData)
                {
                    parser.DrainData(parser._physicalStateObj);
                }
                Debug.Assert(!parser._physicalStateObj._pendingData, "Should not have a busy physicalStateObject at this point!");

                parser.RollbackOrphanedAPITransactions();
            }
        }

        /// <summary>
        /// Validate the enlisted transaction state, taking into consideration the ambient transaction and transaction unbinding mode.
        /// If there is no enlisted transaction, this method is a nop.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method must be called while holding a lock on the SqlInternalConnection instance,
        /// to ensure we don't accidentally execute after the transaction has completed on a different thread, 
        /// causing us to unwittingly execute in auto-commit mode.
        /// </para>
        /// 
        /// <para>
        /// When using Explicit transaction unbinding, 
        /// verify that the enlisted transaction is active and equal to the current ambient transaction.
        /// </para>
        /// 
        /// <para>
        /// When using Implicit transaction unbinding,
        /// verify that the enlisted transaction is active.
        /// If it is not active, and the transaction object has been disposed, unbind from the transaction.
        /// If it is not active and not disposed, throw an exception.
        /// </para>
        /// </remarks>
        internal void CheckEnlistedTransactionBinding()
        {
            // If we are enlisted in a transaction, check that transaction is active.
            // When using explicit transaction unbinding, also verify that the enlisted transaction is the current transaction.
            Transaction enlistedTransaction = EnlistedTransaction;

            if (enlistedTransaction != null)
            {
                bool requireExplicitTransactionUnbind = ConnectionOptions.TransactionBinding == SqlConnectionString.TransactionBindingEnum.ExplicitUnbind;

                if (requireExplicitTransactionUnbind)
                {
                    Transaction currentTransaction = Transaction.Current;

                    if (TransactionStatus.Active != enlistedTransaction.TransactionInformation.Status || !enlistedTransaction.Equals(currentTransaction))
                    {
                        throw ADP.TransactionConnectionMismatch();
                    }
                }
                else // implicit transaction unbind
                {
                    if (TransactionStatus.Active != enlistedTransaction.TransactionInformation.Status)
                    {
                        if (EnlistedTransactionDisposed)
                        {
                            DetachTransaction(enlistedTransaction, true);
                        }
                        else
                        {
                            throw ADP.TransactionCompletedButNotDisposed();
                        }
                    }
                }
            }
        }

        internal override bool IsConnectionAlive(bool throwOnException)
        {
            bool isAlive = false;
            isAlive = _parser._physicalStateObj.IsConnectionAlive(throwOnException);
            return isAlive;
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        // POOLING METHODS
        ////////////////////////////////////////////////////////////////////////////////////////

        protected override void Activate(Transaction transaction)
        {
            // When we're required to automatically enlist in transactions and
            // there is one we enlist in it. On the other hand, if there isn't a
            // transaction and we are currently enlisted in one, then we
            // unenlist from it.
            //
            // Regardless of whether we're required to automatically enlist,
            // when there is not a current transaction, we cannot leave the
            // connection enlisted in a transaction.
            if (null != transaction)
            {
                if (ConnectionOptions.Enlist)
                {
                    Enlist(transaction);
                }
            }
            else
            {
                Enlist(null);
            }
        }

        protected override void InternalDeactivate()
        {
            // When we're deactivated, the user must have called End on all
            // the async commands, or we don't know that we're in a state that
            // we can recover from.  We doom the connection in this case, to
            // prevent odd cases when we go to the wire.
            if (0 != _asyncCommandCount)
            {
                DoomThisConnection();
            }

            // If we're deactivating with a delegated transaction, we 
            // should not be cleaning up the parser just yet, that will
            // cause our transaction to be rolled back and the connection
            // to be reset.  We'll get called again once the delegated
            // transaction is completed and we can do it all then.
            if (!IsNonPoolableTransactionRoot)
            {
                Debug.Assert(null != _parser || IsConnectionDoomed, "Deactivating a disposed connection?");
                if (_parser != null)
                {
                    _parser.Deactivate(IsConnectionDoomed);

                    if (!IsConnectionDoomed)
                    {
                        ResetConnection();
                    }
                }
            }
        }

        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters")] // copied from Triaged.cs
        private void ResetConnection()
        {
            // For implicit pooled connections, if connection reset behavior is specified,
            // reset the database and language properties back to default.  It is important
            // to do this on activate so that the dictionary is correct before SqlConnection
            // obtains a clone.

            Debug.Assert(!HasLocalTransactionFromAPI, "Upon ResetConnection SqlInternalConnectionTds has a currently ongoing local transaction.");
            Debug.Assert(!_parser._physicalStateObj._pendingData, "Upon ResetConnection SqlInternalConnectionTds has pending data.");

            if (_fResetConnection)
            {
                // Ensure we are either going against shiloh, or we are not enlisted in a
                // distributed transaction - otherwise don't reset!
                // Prepare the parser for the connection reset - the next time a trip
                // to the server is made.
                _parser.PrepareResetConnection(IsTransactionRoot && !IsNonPoolableTransactionRoot);

                // Reset dictionary values, since calling reset will not send us env_changes.
                CurrentDatabase = _originalDatabase;
                _currentLanguage = _originalLanguage;
            }
        }

        internal void DecrementAsyncCount()
        {
            Interlocked.Decrement(ref _asyncCommandCount);
        }

        internal void IncrementAsyncCount()
        {
            Interlocked.Increment(ref _asyncCommandCount);
        }


        ////////////////////////////////////////////////////////////////////////////////////////
        // LOCAL TRANSACTION METHODS
        ////////////////////////////////////////////////////////////////////////////////////////

        internal override void DisconnectTransaction(SqlInternalTransaction internalTransaction)
        {
            TdsParser parser = Parser;

            if (null != parser)
            {
                parser.DisconnectTransaction(internalTransaction);
            }
        }

        internal void ExecuteTransaction(TransactionRequest transactionRequest, string name, IsolationLevel iso)
        {
            ExecuteTransaction(transactionRequest, name, iso, null, false);
        }

        internal override void ExecuteTransaction(TransactionRequest transactionRequest, string name, IsolationLevel iso, SqlInternalTransaction internalTransaction, bool isDelegateControlRequest)
        {
            if (IsConnectionDoomed)
            {  // doomed means we can't do anything else...
                if (transactionRequest == TransactionRequest.Rollback
                 || transactionRequest == TransactionRequest.IfRollback)
                {
                    return;
                }
                throw SQL.ConnectionDoomed();
            }

            if (transactionRequest == TransactionRequest.Commit
             || transactionRequest == TransactionRequest.Rollback
             || transactionRequest == TransactionRequest.IfRollback)
            {
                if (!Parser.MARSOn && Parser._physicalStateObj.BcpLock)
                {
                    throw SQL.ConnectionLockedForBcpEvent();
                }
            }

            string transactionName = (null == name) ? string.Empty : name;

            ExecuteTransactionYukon(transactionRequest, transactionName, iso, internalTransaction, isDelegateControlRequest);
        }


        internal void ExecuteTransactionYukon(
                    TransactionRequest transactionRequest,
                    string transactionName,
                    IsolationLevel iso,
                    SqlInternalTransaction internalTransaction,
                    bool isDelegateControlRequest
        )
        {
            TdsEnums.TransactionManagerRequestType requestType = TdsEnums.TransactionManagerRequestType.Begin;
            TdsEnums.TransactionManagerIsolationLevel isoLevel = TdsEnums.TransactionManagerIsolationLevel.ReadCommitted;

            switch (iso)
            {
                case IsolationLevel.Unspecified:
                    isoLevel = TdsEnums.TransactionManagerIsolationLevel.Unspecified;
                    break;
                case IsolationLevel.ReadCommitted:
                    isoLevel = TdsEnums.TransactionManagerIsolationLevel.ReadCommitted;
                    break;
                case IsolationLevel.ReadUncommitted:
                    isoLevel = TdsEnums.TransactionManagerIsolationLevel.ReadUncommitted;
                    break;
                case IsolationLevel.RepeatableRead:
                    isoLevel = TdsEnums.TransactionManagerIsolationLevel.RepeatableRead;
                    break;
                case IsolationLevel.Serializable:
                    isoLevel = TdsEnums.TransactionManagerIsolationLevel.Serializable;
                    break;
                case IsolationLevel.Snapshot:
                    isoLevel = TdsEnums.TransactionManagerIsolationLevel.Snapshot;
                    break;
                case IsolationLevel.Chaos:
                    throw SQL.NotSupportedIsolationLevel(iso);
                default:
                    throw ADP.InvalidIsolationLevel(iso);
            }

            TdsParserStateObject stateObj = _parser._physicalStateObj;
            TdsParser parser = _parser;
            bool mustPutSession = false;
            bool releaseConnectionLock = false;

            Debug.Assert(!ThreadHasParserLockForClose || _parserLock.ThreadMayHaveLock(), "Thread claims to have parser lock, but lock is not taken");
            if (!ThreadHasParserLockForClose)
            {
                _parserLock.Wait(canReleaseFromAnyThread: false);
                ThreadHasParserLockForClose = true;   // In case of error, let the connection know that we already own the parser lock
                releaseConnectionLock = true;
            }
            try
            {
                switch (transactionRequest)
                {
                    case TransactionRequest.Begin:
                        requestType = TdsEnums.TransactionManagerRequestType.Begin;
                        break;
                    case TransactionRequest.Promote:
                        requestType = TdsEnums.TransactionManagerRequestType.Promote;
                        break;
                    case TransactionRequest.Commit:
                        requestType = TdsEnums.TransactionManagerRequestType.Commit;
                        break;
                    case TransactionRequest.IfRollback:
                    // Map IfRollback to Rollback since with Yukon and beyond we should never need
                    // the if since the server will inform us when transactions have completed
                    // as a result of an error on the server.
                    case TransactionRequest.Rollback:
                        requestType = TdsEnums.TransactionManagerRequestType.Rollback;
                        break;
                    case TransactionRequest.Save:
                        requestType = TdsEnums.TransactionManagerRequestType.Save;
                        break;
                    default:
                        Debug.Fail("Unknown transaction type");
                        break;
                }

                // only restore if connection lock has been taken within the function
                if (internalTransaction != null && internalTransaction.RestoreBrokenConnection && releaseConnectionLock)
                {
                    Task reconnectTask = internalTransaction.Parent.Connection.ValidateAndReconnect(() =>
                    {
                        ThreadHasParserLockForClose = false;
                        _parserLock.Release();
                        releaseConnectionLock = false;
                    }, 0);
                    if (reconnectTask != null)
                    {
                        AsyncHelper.WaitForCompletion(reconnectTask, 0); // there is no specific timeout for BeginTransaction, uses ConnectTimeout
                        internalTransaction.ConnectionHasBeenRestored = true;
                        return;
                    }
                }

                // SQLBUDT #20010853 - Promote, Commit and Rollback requests for
                // delegated transactions often happen while there is an open result
                // set, so we need to handle them by using a different MARS session, 
                // otherwise we'll write on the physical state objects while someone
                // else is using it.  When we don't have MARS enabled, we need to 
                // lock the physical state object to syncronize it's use at least 
                // until we increment the open results count.  Once it's been 
                // incremented the delegated transaction requests will fail, so they
                // won't stomp on anything.
                // 
                // We need to keep this lock through the duration of the TM reqeuest
                // so that we won't hijack a different request's data stream and a
                // different request won't hijack ours, so we have a lock here on 
                // an object that the ExecTMReq will also lock, but since we're on
                // the same thread, the lock is a no-op.

                if (null != internalTransaction && internalTransaction.IsDelegated)
                {
                    if (_parser.MARSOn)
                    {
                        stateObj = _parser.GetSession(this);
                        mustPutSession = true;
                    }
                    else if (internalTransaction.OpenResultsCount != 0)
                    {
                        //throw SQL.CannotCompleteDelegatedTransactionWithOpenResults(this);
                    }
                }

                //  _parser may be nulled out during TdsExecuteTrannsactionManagerRequest.
                //  Only use local variable after this call.
                _parser.TdsExecuteTransactionManagerRequest(null, requestType, transactionName, isoLevel,
                    ConnectionOptions.ConnectTimeout, internalTransaction, stateObj, isDelegateControlRequest);
            }
            finally
            {
                if (mustPutSession)
                {
                    parser.PutSession(stateObj);
                }

                if (releaseConnectionLock)
                {
                    ThreadHasParserLockForClose = false;
                    _parserLock.Release();
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        // DISTRIBUTED TRANSACTION METHODS
        ////////////////////////////////////////////////////////////////////////////////////////

        internal override void DelegatedTransactionEnded()
        {
            base.DelegatedTransactionEnded();
        }

        protected override byte[] GetDTCAddress()
        {
            byte[] dtcAddress = _parser.GetDTCAddress(ConnectionOptions.ConnectTimeout, _parser.GetSession(this));
            Debug.Assert(null != dtcAddress, "null dtcAddress?");
            return dtcAddress;
        }

        protected override void PropagateTransactionCookie(byte[] cookie)
        {
            _parser.PropagateDistributedTransaction(cookie, ConnectionOptions.ConnectTimeout, _parser._physicalStateObj);
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        // LOGIN-RELATED METHODS
        ////////////////////////////////////////////////////////////////////////////////////////

        private void CompleteLogin(bool enlistOK)
        {
            _parser.Run(RunBehavior.UntilDone, null, null, null, _parser._physicalStateObj);

            if (_routingInfo == null)
            { // ROR should not affect state of connection recovery
                if (_federatedAuthenticationRequested && !_federatedAuthenticationAcknowledged)
                {
                    throw SQL.ParsingError(ParsingErrorState.FedAuthNotAcknowledged);
                }
                if (!_sessionRecoveryAcknowledged)
                {
                    _currentSessionData = null;
                    if (_recoverySessionData != null)
                    {
                        throw SQL.CR_NoCRAckAtReconnection(this);
                    }
                }
                if (_currentSessionData != null && _recoverySessionData == null)
                {
                    _currentSessionData._initialDatabase = CurrentDatabase;
                    _currentSessionData._initialCollation = _currentSessionData._collation;
                    _currentSessionData._initialLanguage = _currentLanguage;
                }
                bool isEncrypted = _parser.EncryptionOptions == EncryptionOptions.ON;
                if (_recoverySessionData != null)
                {
                    if (_recoverySessionData._encrypted != isEncrypted)
                    {
                        throw SQL.CR_EncryptionChanged(this);
                    }
                }
                if (_currentSessionData != null)
                {
                    _currentSessionData._encrypted = isEncrypted;
                }
                _recoverySessionData = null;
            }

            Debug.Assert(SniContext.Snix_Login == Parser._physicalStateObj.SniContext, $"SniContext should be Snix_Login; actual Value: {Parser._physicalStateObj.SniContext}");
            _parser._physicalStateObj.SniContext = SniContext.Snix_EnableMars;
            _parser.EnableMars();

            _fConnectionOpen = true; // mark connection as open

            // for non-pooled connections, enlist in a distributed transaction
            // if present - and user specified to enlist
            if (enlistOK && ConnectionOptions.Enlist)
            {
                _parser._physicalStateObj.SniContext = SniContext.Snix_AutoEnlist;
                Transaction tx = ADP.GetCurrentTransaction();
                Enlist(tx);
            }

            _parser._physicalStateObj.SniContext = SniContext.Snix_Login;
        }

        private void Login(ServerInfo server, TimeoutTimer timeout, string newPassword, SecureString newSecurePassword)
        {
            // create a new login record
            SqlLogin login = new SqlLogin();

            // gather all the settings the user set in the connection string or
            // properties and do the login
            CurrentDatabase = server.ResolvedDatabaseName;
            _currentPacketSize = ConnectionOptions.PacketSize;
            _currentLanguage = ConnectionOptions.CurrentLanguage;

            int timeoutInSeconds = 0;

            // If a timeout tick value is specified, compute the timeout based
            // upon the amount of time left in seconds.
            if (!timeout.IsInfinite)
            {
                long t = timeout.MillisecondsRemaining / 1000;
                if ((long)int.MaxValue > t)
                {
                    timeoutInSeconds = (int)t;
                }
            }

            login.timeout = timeoutInSeconds;
            login.userInstance = ConnectionOptions.UserInstance;
            login.hostName = ConnectionOptions.ObtainWorkstationId();
            login.userName = ConnectionOptions.UserID;
            login.password = ConnectionOptions.Password;
            login.applicationName = ConnectionOptions.ApplicationName;

            login.language = _currentLanguage;
            if (!login.userInstance)
            { // Do not send attachdbfilename or database to SSE primary instance
                login.database = CurrentDatabase; ;
                login.attachDBFilename = ConnectionOptions.AttachDBFilename;
            }

            // VSTS#795621 - Ensure ServerName is Sent During TdsLogin To Enable Sql Azure Connectivity.
            // Using server.UserServerName (versus ConnectionOptions.DataSource) since TdsLogin requires 
            // serverName to always be non-null.
            login.serverName = server.UserServerName;

            login.useReplication = ConnectionOptions.Replication;
            login.useSSPI = ConnectionOptions.IntegratedSecurity;
            login.packetSize = _currentPacketSize;
            login.newPassword = newPassword;
            login.readOnlyIntent = ConnectionOptions.ApplicationIntent == ApplicationIntent.ReadOnly;
            login.credential = _credential;
            if (newSecurePassword != null)
            {
                login.newSecurePassword = newSecurePassword;
            }

            TdsEnums.FeatureExtension requestedFeatures = TdsEnums.FeatureExtension.None;
            if (ConnectionOptions.ConnectRetryCount > 0)
            {
                requestedFeatures |= TdsEnums.FeatureExtension.SessionRecovery;
                _sessionRecoveryRequested = true;
            }

            if (_accessTokenInBytes != null)
            {
                requestedFeatures |= TdsEnums.FeatureExtension.FedAuth;
                _fedAuthFeatureExtensionData = new FederatedAuthenticationFeatureExtensionData
                {
                    libraryType = TdsEnums.FedAuthLibrary.SecurityToken,
                    fedAuthRequiredPreLoginResponse = _fedAuthRequired,
                    accessToken = _accessTokenInBytes
                };
                // No need any further info from the server for token based authentication. So set _federatedAuthenticationRequested to true
                _federatedAuthenticationRequested = true;
            }

            // The GLOBALTRANSACTIONS and UTF8 support features are implicitly requested
            requestedFeatures |= TdsEnums.FeatureExtension.GlobalTransactions | TdsEnums.FeatureExtension.UTF8Support;
            _parser.TdsLogin(login, requestedFeatures, _recoverySessionData, _fedAuthFeatureExtensionData);
        }

        private void LoginFailure()
        {
            // If the parser was allocated and we failed, then we must have failed on
            // either the Connect or Login, either way we should call Disconnect.
            // Disconnect can be called if the connection is already closed - becomes
            // no-op, so no issues there.
            if (_parser != null)
            {
                _parser.Disconnect();
            }
        }

        private void OpenLoginEnlist(TimeoutTimer timeout,
                                    SqlConnectionString connectionOptions,
                                    SqlCredential credential,
                                    string newPassword,
                                    SecureString newSecurePassword,
                                    bool redirectedUserInstance)
        {
            bool useFailoverPartner; // should we use primary or secondary first
            ServerInfo dataSource = new ServerInfo(connectionOptions);
            string failoverPartner;

            if (null != PoolGroupProviderInfo)
            {
                useFailoverPartner = PoolGroupProviderInfo.UseFailoverPartner;
                failoverPartner = PoolGroupProviderInfo.FailoverPartner;
            }
            else
            {
                // Only ChangePassword or SSE User Instance comes through this code path.
                useFailoverPartner = false;
                failoverPartner = ConnectionOptions.FailoverPartner;
            }

            _timeoutErrorInternal.SetInternalSourceType(useFailoverPartner ? SqlConnectionInternalSourceType.Failover : SqlConnectionInternalSourceType.Principle);

            bool hasFailoverPartner = !string.IsNullOrEmpty(failoverPartner);

            // Open the connection and Login
            try
            {
                _timeoutErrorInternal.SetAndBeginPhase(SqlConnectionTimeoutErrorPhase.PreLoginBegin);
                if (hasFailoverPartner)
                {
                    _timeoutErrorInternal.SetFailoverScenario(true); // this is a failover scenario
                    LoginWithFailover(
                                useFailoverPartner,
                                dataSource,
                                failoverPartner,
                                newPassword,
                                newSecurePassword,
                                redirectedUserInstance,
                                connectionOptions,
                                credential,
                                timeout);
                }
                else
                {
                    _timeoutErrorInternal.SetFailoverScenario(false); // not a failover scenario
                    LoginNoFailover(
                            dataSource,
                            newPassword,
                            newSecurePassword,
                            redirectedUserInstance,
                            connectionOptions,
                            credential,
                            timeout);
                }
                _timeoutErrorInternal.EndPhase(SqlConnectionTimeoutErrorPhase.PostLogin);
            }
            catch (Exception e)
            {
                if (ADP.IsCatchableExceptionType(e))
                {
                    LoginFailure();
                }
                throw;
            }
            _timeoutErrorInternal.SetAllCompleteMarker();

#if DEBUG
            _parser._physicalStateObj.InvalidateDebugOnlyCopyOfSniContext();
#endif
        }

        // Is the given Sql error one that should prevent retrying
        //   to connect.
        private bool IsDoNotRetryConnectError(SqlException exc)
        {
            return (TdsEnums.LOGON_FAILED == exc.Number) // actual logon failed, i.e. bad password
                || (TdsEnums.PASSWORD_EXPIRED == exc.Number) // actual logon failed, i.e. password isExpired
                || (TdsEnums.IMPERSONATION_FAILED == exc.Number)  // Insufficient privilege for named pipe, among others
                || exc._doNotReconnect; // Exception explicitly suppressed reconnection attempts
        }

        // Attempt to login to a host that does not have a failover partner
        //
        //  Will repeatedly attempt to connect, but back off between each attempt so as not to clog the network.
        //  Back off period increases for first few failures: 100ms, 200ms, 400ms, 800ms, then 1000ms for subsequent attempts
        //
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        //  DEVNOTE: The logic in this method is paralleled by the logic in LoginWithFailover.
        //           Changes to either one should be examined to see if they need to be reflected in the other
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        private void LoginNoFailover(ServerInfo serverInfo,
                                     string newPassword,
                                     SecureString newSecurePassword,
                                     bool redirectedUserInstance,
                                     SqlConnectionString connectionOptions,
                                     SqlCredential credential,
                                     TimeoutTimer timeout)
        {
            Debug.Assert(object.ReferenceEquals(connectionOptions, this.ConnectionOptions), "ConnectionOptions argument and property must be the same"); // consider removing the argument
            int routingAttempts = 0;
            ServerInfo originalServerInfo = serverInfo; // serverInfo may end up pointing to new object due to routing, original object is used to set CurrentDatasource

            int sleepInterval = 100;  //milliseconds to sleep (back off) between attempts.

            ResolveExtendedServerName(serverInfo, !redirectedUserInstance, connectionOptions);

            long timeoutUnitInterval = 0;

            if (connectionOptions.MultiSubnetFailover)
            {
                // Determine unit interval
                if (timeout.IsInfinite)
                {
                    timeoutUnitInterval = checked((long)(ADP.FailoverTimeoutStep * (1000L * ADP.DefaultConnectionTimeout)));
                }
                else
                {
                    timeoutUnitInterval = checked((long)(ADP.FailoverTimeoutStep * timeout.MillisecondsRemaining));
                }
            }
            // Only three ways out of this loop:
            //  1) Successfully connected
            //  2) Parser threw exception while main timer was expired
            //  3) Parser threw logon failure-related exception 
            //  4) Parser threw exception in post-initial connect code,
            //      such as pre-login handshake or during actual logon. (parser state != Closed)
            //
            //  Of these methods, only #1 exits normally. This preserves the call stack on the exception 
            //  back into the parser for the error cases.
            int attemptNumber = 0;
            TimeoutTimer intervalTimer = null;
            while (true)
            {
                if (connectionOptions.MultiSubnetFailover)
                {
                    attemptNumber++;
                    // Set timeout for this attempt, but don't exceed original timer                
                    long nextTimeoutInterval = checked(timeoutUnitInterval * attemptNumber);
                    long milliseconds = timeout.MillisecondsRemaining;
                    if (nextTimeoutInterval > milliseconds)
                    {
                        nextTimeoutInterval = milliseconds;
                    }
                    intervalTimer = TimeoutTimer.StartMillisecondsTimeout(nextTimeoutInterval);
                }

                // Re-allocate parser each time to make sure state is known
                // RFC 50002652 - if parser was created by previous attempt, dispose it to properly close the socket, if created
                if (_parser != null)
                    _parser.Disconnect();

                _parser = new TdsParser(ConnectionOptions.MARS, ConnectionOptions.Asynchronous);
                Debug.Assert(SniContext.Undefined == Parser._physicalStateObj.SniContext, $"SniContext should be Undefined; actual Value: {Parser._physicalStateObj.SniContext}");

                try
                {
                    AttemptOneLogin(serverInfo,
                                    newPassword,
                                    newSecurePassword,
                                    !connectionOptions.MultiSubnetFailover,    // ignore timeout for SniOpen call unless MSF 
                                    connectionOptions.MultiSubnetFailover ? intervalTimer : timeout);

                    if (connectionOptions.MultiSubnetFailover && null != ServerProvidedFailOverPartner)
                    {
                        // connection succeeded: trigger exception if server sends failover partner and MultiSubnetFailover is used
                        throw SQL.MultiSubnetFailoverWithFailoverPartner(serverProvidedFailoverPartner: true, internalConnection: this);
                    }

                    if (_routingInfo != null)
                    {
                        if (routingAttempts > 0)
                        {
                            throw SQL.ROR_RecursiveRoutingNotSupported(this);
                        }

                        if (timeout.IsExpired)
                        {
                            throw SQL.ROR_TimeoutAfterRoutingInfo(this);
                        }

                        serverInfo = new ServerInfo(ConnectionOptions, _routingInfo, serverInfo.ResolvedServerName);
                        _timeoutErrorInternal.SetInternalSourceType(SqlConnectionInternalSourceType.RoutingDestination);
                        _originalClientConnectionId = _clientConnectionId;
                        _routingDestination = serverInfo.UserServerName;

                        // restore properties that could be changed by the environment tokens
                        _currentPacketSize = ConnectionOptions.PacketSize;
                        _currentLanguage = _originalLanguage = ConnectionOptions.CurrentLanguage;
                        CurrentDatabase = _originalDatabase = ConnectionOptions.InitialCatalog;
                        _currentFailoverPartner = null;
                        _instanceName = string.Empty;

                        routingAttempts++;

                        continue; // repeat the loop, but skip code reserved for failed connections (after the catch)
                    }
                    else
                    {
                        break; // leave the while loop -- we've successfully connected
                    }
                }
                catch (SqlException sqlex)
                {
                    if (null == _parser
                            || TdsParserState.Closed != _parser.State
                            || IsDoNotRetryConnectError(sqlex)
                            || timeout.IsExpired)
                    {       // no more time to try again
                        throw;  // Caller will call LoginFailure()
                    }

                    // Check sleep interval to make sure we won't exceed the timeout
                    //  Do this in the catch block so we can re-throw the current exception
                    if (timeout.MillisecondsRemaining <= sleepInterval)
                    {
                        throw;
                    }
                }

                // We only get here when we failed to connect, but are going to re-try

                // Switch to failover logic if the server provided a partner
                if (null != ServerProvidedFailOverPartner)
                {
                    if (connectionOptions.MultiSubnetFailover)
                    {
                        // connection failed: do not allow failover to server-provided failover partner if MultiSubnetFailover is set
                        throw SQL.MultiSubnetFailoverWithFailoverPartner(serverProvidedFailoverPartner: true, internalConnection: this);
                    }
                    Debug.Assert(ConnectionOptions.ApplicationIntent != ApplicationIntent.ReadOnly, "FAILOVER+AppIntent=RO: Should already fail (at LOGSHIPNODE in OnEnvChange)");

                    _timeoutErrorInternal.ResetAndRestartPhase();
                    _timeoutErrorInternal.SetAndBeginPhase(SqlConnectionTimeoutErrorPhase.PreLoginBegin);
                    _timeoutErrorInternal.SetInternalSourceType(SqlConnectionInternalSourceType.Failover);
                    _timeoutErrorInternal.SetFailoverScenario(true); // this is a failover scenario
                    LoginWithFailover(
                                true,   // start by using failover partner, since we already failed to connect to the primary
                                serverInfo,
                                ServerProvidedFailOverPartner,
                                newPassword,
                                newSecurePassword,
                                redirectedUserInstance,
                                connectionOptions,
                                credential,
                                timeout);
                    return; // LoginWithFailover successfully connected and handled entire connection setup
                }

                // Sleep for a bit to prevent clogging the network with requests, 
                //  then update sleep interval for next iteration (max 1 second interval)
                Thread.Sleep(sleepInterval);
                sleepInterval = (sleepInterval < 500) ? sleepInterval * 2 : 1000;
            }

            if (null != PoolGroupProviderInfo)
            {
                // We must wait for CompleteLogin to finish for to have the
                // env change from the server to know its designated failover 
                // partner; save this information in _currentFailoverPartner.
                PoolGroupProviderInfo.FailoverCheck(this, false, connectionOptions, ServerProvidedFailOverPartner);
            }
            CurrentDataSource = originalServerInfo.UserServerName;
        }

        // Attempt to login to a host that has a failover partner
        //
        // Connection & timeout sequence is
        //      First target, timeout = interval * 1
        //      second target, timeout = interval * 1
        //      sleep for 100ms
        //      First target, timeout = interval * 2
        //      Second target, timeout = interval * 2
        //      sleep for 200ms
        //      First Target, timeout = interval * 3
        //      etc.
        //
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        //  DEVNOTE: The logic in this method is paralleled by the logic in LoginNoFailover.
        //           Changes to either one should be examined to see if they need to be reflected in the other
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        private void LoginWithFailover(
                bool useFailoverHost,
                ServerInfo primaryServerInfo,
                string failoverHost,
                string newPassword,
                SecureString newSecurePassword,
                bool redirectedUserInstance,
                SqlConnectionString connectionOptions,
                SqlCredential credential,
                TimeoutTimer timeout
            )
        {
            Debug.Assert(!connectionOptions.MultiSubnetFailover, "MultiSubnetFailover should not be set if failover partner is used");

            int sleepInterval = 100;  //milliseconds to sleep (back off) between attempts.
            long timeoutUnitInterval;

            ServerInfo failoverServerInfo = new ServerInfo(connectionOptions, failoverHost);

            ResolveExtendedServerName(primaryServerInfo, !redirectedUserInstance, connectionOptions);
            if (null == ServerProvidedFailOverPartner)
            {
                ResolveExtendedServerName(failoverServerInfo, !redirectedUserInstance && failoverHost != primaryServerInfo.UserServerName, connectionOptions);
            }

            // Determine unit interval
            if (timeout.IsInfinite)
            {
                timeoutUnitInterval = checked((long)(ADP.FailoverTimeoutStep * ADP.TimerFromSeconds(ADP.DefaultConnectionTimeout)));
            }
            else
            {
                timeoutUnitInterval = checked((long)(ADP.FailoverTimeoutStep * timeout.MillisecondsRemaining));
            }

            // Initialize loop variables
            int attemptNumber = 0;

            // Only three ways out of this loop:
            //  1) Successfully connected
            //  2) Parser threw exception while main timer was expired
            //  3) Parser threw logon failure-related exception (LOGON_FAILED, PASSWORD_EXPIRED, etc)
            //
            //  Of these methods, only #1 exits normally. This preserves the call stack on the exception 
            //  back into the parser for the error cases.
            while (true)
            {
                // Set timeout for this attempt, but don't exceed original timer
                long nextTimeoutInterval = checked(timeoutUnitInterval * ((attemptNumber / 2) + 1));
                long milliseconds = timeout.MillisecondsRemaining;
                if (nextTimeoutInterval > milliseconds)
                {
                    nextTimeoutInterval = milliseconds;
                }

                TimeoutTimer intervalTimer = TimeoutTimer.StartMillisecondsTimeout(nextTimeoutInterval);

                // Re-allocate parser each time to make sure state is known
                // RFC 50002652 - if parser was created by previous attempt, dispose it to properly close the socket, if created
                if (_parser != null)
                    _parser.Disconnect();

                _parser = new TdsParser(ConnectionOptions.MARS, ConnectionOptions.Asynchronous);
                Debug.Assert(SniContext.Undefined == Parser._physicalStateObj.SniContext, $"SniContext should be Undefined; actual Value: {Parser._physicalStateObj.SniContext}");

                ServerInfo currentServerInfo;
                if (useFailoverHost)
                {
                    // Primary server may give us a different failover partner than the connection string indicates.  Update it
                    if (null != ServerProvidedFailOverPartner && failoverServerInfo.ResolvedServerName != ServerProvidedFailOverPartner)
                    {
                        failoverServerInfo.SetDerivedNames(string.Empty, ServerProvidedFailOverPartner);
                    }
                    currentServerInfo = failoverServerInfo;
                    _timeoutErrorInternal.SetInternalSourceType(SqlConnectionInternalSourceType.Failover);
                }
                else
                {
                    currentServerInfo = primaryServerInfo;
                    _timeoutErrorInternal.SetInternalSourceType(SqlConnectionInternalSourceType.Principle);
                }

                try
                {
                    // Attempt login.  Use timerInterval for attempt timeout unless infinite timeout was requested.
                    AttemptOneLogin(
                            currentServerInfo,
                            newPassword,
                            newSecurePassword,
                            false,          // Use timeout in SniOpen
                            intervalTimer,
                            withFailover: true
                            );

                    if (_routingInfo != null)
                    {
                        // We are in login with failover scenation and server sent routing information
                        // If it is read-only routing - we did not supply AppIntent=RO (it should be checked before)
                        // If it is something else, not known yet (future server) - this client is not designed to support this.                    
                        // In any case, server should not have sent the routing info.
                        throw SQL.ROR_UnexpectedRoutingInfo(this);
                    }

                    break; // leave the while loop -- we've successfully connected
                }
                catch (SqlException sqlex)
                {
                    if (IsDoNotRetryConnectError(sqlex)
                            || timeout.IsExpired)
                    {       // no more time to try again
                        throw;  // Caller will call LoginFailure()
                    }

                    if (IsConnectionDoomed)
                    {
                        throw;
                    }

                    if (1 == attemptNumber % 2)
                    {
                        // Check sleep interval to make sure we won't exceed the original timeout
                        //  Do this in the catch block so we can re-throw the current exception
                        if (timeout.MillisecondsRemaining <= sleepInterval)
                        {
                            throw;
                        }
                    }
                }

                // We only get here when we failed to connect, but are going to re-try

                // After trying to connect to both servers fails, sleep for a bit to prevent clogging 
                //  the network with requests, then update sleep interval for next iteration (max 1 second interval)
                if (1 == attemptNumber % 2)
                {
                    Thread.Sleep(sleepInterval);
                    sleepInterval = (sleepInterval < 500) ? sleepInterval * 2 : 1000;
                }

                // Update attempt number and target host
                attemptNumber++;
                useFailoverHost = !useFailoverHost;
            }

            // If we get here, connection/login succeeded!  Just a few more checks & record-keeping

            // if connected to failover host, but said host doesn't have DbMirroring set up, throw an error
            if (useFailoverHost && null == ServerProvidedFailOverPartner)
            {
                throw SQL.InvalidPartnerConfiguration(failoverHost, CurrentDatabase);
            }

            if (null != PoolGroupProviderInfo)
            {
                // We must wait for CompleteLogin to finish for to have the
                // env change from the server to know its designated failover 
                // partner; save this information in _currentFailoverPartner.
                PoolGroupProviderInfo.FailoverCheck(this, useFailoverHost, connectionOptions, ServerProvidedFailOverPartner);
            }
            CurrentDataSource = (useFailoverHost ? failoverHost : primaryServerInfo.UserServerName);
        }

        private void ResolveExtendedServerName(ServerInfo serverInfo, bool aliasLookup, SqlConnectionString options)
        {
            if (serverInfo.ExtendedServerName == null)
            {
                string host = serverInfo.UserServerName;
                string protocol = serverInfo.UserProtocol;


                serverInfo.SetDerivedNames(protocol, host);
            }
        }

        // Common code path for making one attempt to establish a connection and log in to server.
        private void AttemptOneLogin(
                                ServerInfo serverInfo,
                                string newPassword,
                                SecureString newSecurePassword,
                                bool ignoreSniOpenTimeout,
                                TimeoutTimer timeout,
                                bool withFailover = false)
        {
            _routingInfo = null; // forget routing information 

            _parser._physicalStateObj.SniContext = SniContext.Snix_Connect;

            _parser.Connect(serverInfo,
                            this,
                            ignoreSniOpenTimeout,
                            timeout.LegacyTimerExpire,
                            ConnectionOptions.Encrypt,
                            ConnectionOptions.TrustServerCertificate,
                            ConnectionOptions.IntegratedSecurity,
                            withFailover);

            _timeoutErrorInternal.EndPhase(SqlConnectionTimeoutErrorPhase.ConsumePreLoginHandshake);
            _timeoutErrorInternal.SetAndBeginPhase(SqlConnectionTimeoutErrorPhase.LoginBegin);

            _parser._physicalStateObj.SniContext = SniContext.Snix_Login;
            this.Login(serverInfo, timeout, newPassword, newSecurePassword);

            _timeoutErrorInternal.EndPhase(SqlConnectionTimeoutErrorPhase.ProcessConnectionAuth);
            _timeoutErrorInternal.SetAndBeginPhase(SqlConnectionTimeoutErrorPhase.PostLogin);

            CompleteLogin(!ConnectionOptions.Pooling);

            _timeoutErrorInternal.EndPhase(SqlConnectionTimeoutErrorPhase.PostLogin);
        }



        ////////////////////////////////////////////////////////////////////////////////////////
        // PREPARED COMMAND METHODS
        ////////////////////////////////////////////////////////////////////////////////////////

        protected override object ObtainAdditionalLocksForClose()
        {
            bool obtainParserLock = !ThreadHasParserLockForClose;
            Debug.Assert(obtainParserLock || _parserLock.ThreadMayHaveLock(), "Thread claims to have lock, but lock is not taken");
            if (obtainParserLock)
            {
                _parserLock.Wait(canReleaseFromAnyThread: false);
                ThreadHasParserLockForClose = true;
            }
            return obtainParserLock;
        }

        protected override void ReleaseAdditionalLocksForClose(object lockToken)
        {
            Debug.Assert(lockToken is bool, "Lock token should be boolean");
            if ((bool)lockToken)
            {
                ThreadHasParserLockForClose = false;
                _parserLock.Release();
            }
        }

        // called by SqlConnection.RepairConnection which is a relatively expensive way of repair inner connection
        // prior to execution of request, used from EnlistTransaction, EnlistDistributedTransaction and ChangeDatabase
        internal bool GetSessionAndReconnectIfNeeded(SqlConnection parent, int timeout = 0)
        {
            Debug.Assert(!ThreadHasParserLockForClose, "Cannot call this method if caller has parser lock");
            if (ThreadHasParserLockForClose)
            {
                return false; // we cannot restore if we cannot release lock
            }

            _parserLock.Wait(canReleaseFromAnyThread: false);
            ThreadHasParserLockForClose = true;   // In case of error, let the connection know that we already own the parser lock
            bool releaseConnectionLock = true;

            try
            {
                Task reconnectTask = parent.ValidateAndReconnect(() =>
                {
                    ThreadHasParserLockForClose = false;
                    _parserLock.Release();
                    releaseConnectionLock = false;
                }, timeout);
                if (reconnectTask != null)
                {
                    AsyncHelper.WaitForCompletion(reconnectTask, timeout);
                    return true;
                }
                return false;
            }
            finally
            {
                if (releaseConnectionLock)
                {
                    ThreadHasParserLockForClose = false;
                    _parserLock.Release();
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        // PARSER CALLBACKS
        ////////////////////////////////////////////////////////////////////////////////////////

        internal void BreakConnection()
        {
            var connection = Connection;
            DoomThisConnection();   // Mark connection as unusable, so it will be destroyed
            if (null != connection)
            {
                connection.Close();
            }
        }

        internal bool IgnoreEnvChange
        { // true if we are only draining environment change tokens, used by TdsParser
            get
            {
                return _routingInfo != null; // connection was routed, ignore rest of env change
            }
        }

        internal void OnEnvChange(SqlEnvChange rec)
        {
            Debug.Assert(!IgnoreEnvChange, "This function should not be called if IgnoreEnvChange is set!");
            switch (rec.type)
            {
                case TdsEnums.ENV_DATABASE:
                    // If connection is not open and recovery is not in progress, store the server value as the original.
                    if (!_fConnectionOpen && _recoverySessionData == null)
                    {
                        _originalDatabase = rec.newValue;
                    }

                    CurrentDatabase = rec.newValue;
                    break;

                case TdsEnums.ENV_LANG:
                    // If connection is not open and recovery is not in progress, store the server value as the original.
                    if (!_fConnectionOpen && _recoverySessionData == null)
                    {
                        _originalLanguage = rec.newValue;
                    }

                    _currentLanguage = rec.newValue;
                    break;

                case TdsEnums.ENV_PACKETSIZE:
                    _currentPacketSize = int.Parse(rec.newValue, CultureInfo.InvariantCulture);
                    break;

                case TdsEnums.ENV_COLLATION:
                    if (_currentSessionData != null)
                    {
                        _currentSessionData._collation = rec.newCollation;
                    }
                    break;

                case TdsEnums.ENV_CHARSET:
                case TdsEnums.ENV_LOCALEID:
                case TdsEnums.ENV_COMPFLAGS:
                case TdsEnums.ENV_BEGINTRAN:
                case TdsEnums.ENV_COMMITTRAN:
                case TdsEnums.ENV_ROLLBACKTRAN:
                case TdsEnums.ENV_ENLISTDTC:
                case TdsEnums.ENV_DEFECTDTC:
                    // only used on parser
                    break;

                case TdsEnums.ENV_LOGSHIPNODE:
                    if (ConnectionOptions.ApplicationIntent == ApplicationIntent.ReadOnly)
                    {
                        throw SQL.ROR_FailoverNotSupportedServer(this);
                    }
                    _currentFailoverPartner = rec.newValue;
                    break;

                case TdsEnums.ENV_PROMOTETRANSACTION:
                    byte[] dtcToken = null; 
                    if (rec.newBinRented)
                    {
                        dtcToken = new byte[rec.newLength];
                        Buffer.BlockCopy(rec.newBinValue, 0, dtcToken, 0, dtcToken.Length);
                    }
                    else
                    {
                        dtcToken = rec.newBinValue;
                        rec.newBinValue = null;
                    }
                    PromotedDTCToken = dtcToken;
                    break;

                case TdsEnums.ENV_TRANSACTIONENDED:
                    break;

                case TdsEnums.ENV_TRANSACTIONMANAGERADDRESS:
                    // For now we skip these Yukon only env change notifications
                    break;

                case TdsEnums.ENV_SPRESETCONNECTIONACK:
                    // connection is being reset 
                    if (_currentSessionData != null)
                    {
                        _currentSessionData.Reset();
                    }
                    break;

                case TdsEnums.ENV_USERINSTANCE:
                    _instanceName = rec.newValue;
                    break;

                case TdsEnums.ENV_ROUTING:
                    if (string.IsNullOrEmpty(rec.newRoutingInfo.ServerName) || rec.newRoutingInfo.Protocol != 0 || rec.newRoutingInfo.Port == 0)
                    {
                        throw SQL.ROR_InvalidRoutingInfo(this);
                    }
                    _routingInfo = rec.newRoutingInfo;
                    break;

                default:
                    Debug.Fail("Missed token in EnvChange!");
                    break;
            }
        }

        internal void OnLoginAck(SqlLoginAck rec)
        {
            _loginAck = rec;
            if (_recoverySessionData != null)
            {
                if (_recoverySessionData._tdsVersion != rec.tdsVersion)
                {
                    throw SQL.CR_TDSVersionNotPreserved(this);
                }
            }
            if (_currentSessionData != null)
            {
                _currentSessionData._tdsVersion = rec.tdsVersion;
            }
        }

        internal void OnFeatureExtAck(int featureId, byte[] data)
        {
            if (_routingInfo != null)
            {
                return;
            }
            switch (featureId)
            {
                case TdsEnums.FEATUREEXT_SRECOVERY:
                    {
                        // Session recovery not requested
                        if (!_sessionRecoveryRequested)
                        {
                            throw SQL.ParsingError();
                        }
                        _sessionRecoveryAcknowledged = true;

#if DEBUG
                        foreach (var s in _currentSessionData._delta)
                        {
                            Debug.Assert(s == null, "Delta should be null at this point");
                        }
#endif
                        Debug.Assert(_currentSessionData._unrecoverableStatesCount == 0, "Unrecoverable states count should be 0");

                        int i = 0;
                        while (i < data.Length)
                        {
                            byte stateId = data[i]; i++;
                            int len;
                            byte bLen = data[i]; i++;
                            if (bLen == 0xFF)
                            {
                                len = BitConverter.ToInt32(data, i); i += 4;
                            }
                            else
                            {
                                len = bLen;
                            }
                            byte[] stateData = new byte[len];
                            Buffer.BlockCopy(data, i, stateData, 0, len); i += len;
                            if (_recoverySessionData == null)
                            {
                                _currentSessionData._initialState[stateId] = stateData;
                            }
                            else
                            {
                                _currentSessionData._delta[stateId] = new SessionStateRecord { _data = stateData, _dataLength = len, _recoverable = true, _version = 0 };
                                _currentSessionData._deltaDirty = true;
                            }
                        }
                        break;
                    }

                case TdsEnums.FEATUREEXT_GLOBALTRANSACTIONS:
                    {
                        if (data.Length < 1)
                        {
                            throw SQL.ParsingError();
                        }

                        IsGlobalTransaction = true;
                        if (1 == data[0])
                        {
                            IsGlobalTransactionsEnabledForServer = true;
                        }
                        break;
                    }
                case TdsEnums.FEATUREEXT_FEDAUTH:
                    {
                        if (!_federatedAuthenticationRequested)
                        {
                            throw SQL.ParsingErrorFeatureId(ParsingErrorState.UnrequestedFeatureAckReceived, featureId);
                        }

                        Debug.Assert(_fedAuthFeatureExtensionData != null, "_fedAuthFeatureExtensionData must not be null when _federatedAuthenticatonRequested == true");

                        switch (_fedAuthFeatureExtensionData.Value.libraryType)
                        {
                            case TdsEnums.FedAuthLibrary.SecurityToken:
                                // The server shouldn't have sent any additional data with the ack (like a nonce)
                                if (data.Length != 0)
                                {
                                    throw SQL.ParsingError(ParsingErrorState.FedAuthFeatureAckContainsExtraData);
                                }
                                break;

                            default:
                                Debug.Fail("Unknown _fedAuthLibrary type");
                                throw SQL.ParsingErrorLibraryType(ParsingErrorState.FedAuthFeatureAckUnknownLibraryType, (int)_fedAuthFeatureExtensionData.Value.libraryType);
                        }
                        _federatedAuthenticationAcknowledged = true;
                        break;
                    }

                case TdsEnums.FEATUREEXT_UTF8SUPPORT:
                    {
                        if (data.Length < 1)
                        {
                            throw SQL.ParsingError();
                        }
                        break;
                    }
                default:
                    {
                        // Unknown feature ack 
                        throw SQL.ParsingError();
                    }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        // Helper methods for Locks
        ////////////////////////////////////////////////////////////////////////////////////////

        // Indicates if the current thread claims to hold the parser lock
        internal bool ThreadHasParserLockForClose
        {
            get
            {
                return _threadIdOwningParserLock == Thread.CurrentThread.ManagedThreadId;
            }
            set
            {
                Debug.Assert(_parserLock.ThreadMayHaveLock(), "Should not modify ThreadHasParserLockForClose without taking the lock first");
                Debug.Assert(_threadIdOwningParserLock == -1 || _threadIdOwningParserLock == Thread.CurrentThread.ManagedThreadId, "Another thread already claims to own the parser lock");

                if (value)
                {
                    // If setting to true, then the thread owning the lock is the current thread
                    _threadIdOwningParserLock = Thread.CurrentThread.ManagedThreadId;
                }
                else if (_threadIdOwningParserLock == Thread.CurrentThread.ManagedThreadId)
                {
                    // If setting to false and currently owns the lock, then no-one owns the lock
                    _threadIdOwningParserLock = -1;
                }
                // else This thread didn't own the parser lock and doesn't claim to own it, so do nothing
            }
        }

        internal override bool TryReplaceConnection(DbConnection outerConnection, DbConnectionFactory connectionFactory, TaskCompletionSource<DbConnectionInternal> retry, DbConnectionOptions userOptions)
        {
            return base.TryOpenConnectionInternal(outerConnection, connectionFactory, retry, userOptions);
        }
    }

    internal sealed class ServerInfo
    {
        internal string ExtendedServerName { get; private set; } // the resolved servername with protocol
        internal string ResolvedServerName { get; private set; } // the resolved servername only
        internal string ResolvedDatabaseName { get; private set; } // name of target database after resolution
        internal string UserProtocol { get; private set; } // the user specified protocol        

        // The original user-supplied server name from the connection string.
        // If connection string has no Data Source, the value is set to string.Empty.
        // In case of routing, will be changed to routing destination
        internal string UserServerName
        {
            get
            {
                return _userServerName;
            }
            private set
            {
                _userServerName = value;
            }
        }
        private string _userServerName;

        internal readonly string PreRoutingServerName;

        // Initialize server info from connection options, 
        internal ServerInfo(SqlConnectionString userOptions) : this(userOptions, userOptions.DataSource) { }

        // Initialize server info from connection options, but override DataSource with given server name
        internal ServerInfo(SqlConnectionString userOptions, string serverName)
        {
            //-----------------
            // Preconditions
            Debug.Assert(null != userOptions);

            //-----------------
            //Method body

            Debug.Assert(serverName != null, "server name should never be null");
            UserServerName = (serverName ?? string.Empty); // ensure user server name is not null

            UserProtocol = string.Empty;
            ResolvedDatabaseName = userOptions.InitialCatalog;
            PreRoutingServerName = null;
        }


        // Initialize server info from connection options, but override DataSource with given server name
        internal ServerInfo(SqlConnectionString userOptions, RoutingInfo routing, string preRoutingServerName)
        {
            //-----------------
            // Preconditions
            Debug.Assert(null != userOptions && null != routing);

            //-----------------
            //Method body
            Debug.Assert(routing.ServerName != null, "server name should never be null");
            if (routing == null || routing.ServerName == null)
            {
                UserServerName = string.Empty; // ensure user server name is not null
            }
            else
            {
                UserServerName = string.Format(CultureInfo.InvariantCulture, "{0},{1}", routing.ServerName, routing.Port);
            }
            PreRoutingServerName = preRoutingServerName;
            UserProtocol = TdsEnums.TCP;
            SetDerivedNames(UserProtocol, UserServerName);
            ResolvedDatabaseName = userOptions.InitialCatalog;
        }

        internal void SetDerivedNames(string protocol, string serverName)
        {
            // The following concatenates the specified netlib network protocol to the host string, if netlib is not null
            // and the flag is on.  This allows the user to specify the network protocol for the connection - but only
            // when using the Dbnetlib dll.  If the protocol is not specified, the netlib will
            // try all protocols in the order listed in the Client Network Utility.  Connect will
            // then fail if all protocols fail.
            if (!string.IsNullOrEmpty(protocol))
            {
                ExtendedServerName = protocol + ":" + serverName;
            }
            else
            {
                ExtendedServerName = serverName;
            }
            ResolvedServerName = serverName;
        }
    }
}

