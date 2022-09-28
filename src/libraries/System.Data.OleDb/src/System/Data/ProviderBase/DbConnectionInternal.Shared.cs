// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SysTx = System.Transactions;

namespace System.Data.ProviderBase
{
    internal abstract partial class DbConnectionInternal // V1.1.3300
    {
        internal static readonly StateChangeEventArgs StateChangeClosed = new StateChangeEventArgs(ConnectionState.Open, ConnectionState.Closed);
        internal static readonly StateChangeEventArgs StateChangeOpen = new StateChangeEventArgs(ConnectionState.Closed, ConnectionState.Open);

        private readonly bool _allowSetConnectionString;
        private readonly bool _hidePassword;
        private readonly ConnectionState _state;

        private readonly WeakReference _owningObject = new WeakReference(null, false);  // [usage must be thread safe] the owning object, when not in the pool. (both Pooled and Non-Pooled connections)

        private DbConnectionPool? _connectionPool;           // the pooler that the connection came from (Pooled connections only)
        private DbConnectionPoolCounters? _performanceCounters;      // the performance counters we're supposed to update
        private DbReferenceCollection? _referenceCollection;      // collection of objects that we need to notify in some way when we're being deactivated
        private int _pooledCount;              // [usage must be thread safe] the number of times this object has been pushed into the pool less the number of times it's been popped (0 != inPool)

        private bool _connectionIsDoomed;       // true when the connection should no longer be used.
        private bool _cannotBePooled;           // true when the connection should no longer be pooled.
        private bool _isInStasis;

        private DateTime _createTime;               // when the connection was created.

        private SysTx.Transaction? _enlistedTransaction;      // [usage must be thread-safe] the transaction that we're enlisted in, either manually or automatically

        // _enlistedTransaction is a clone, so that transaction information can be queried even if the original transaction object is disposed.
        // However, there are times when we need to know if the original transaction object was disposed, so we keep a reference to it here.
        // This field should only be assigned a value at the same time _enlistedTransaction is updated.
        // Also, this reference should not be disposed, since we aren't taking ownership of it.
        private SysTx.Transaction? _enlistedTransactionOriginal;

#if DEBUG
        private int _activateCount;            // debug only counter to verify activate/deactivates are in sync.
#endif //DEBUG

        protected DbConnectionInternal() : this(ConnectionState.Open, true, false)
        { // V1.1.3300
        }

        // Constructor for internal connections
        internal DbConnectionInternal(ConnectionState state, bool hidePassword, bool allowSetConnectionString)
        {
            _allowSetConnectionString = allowSetConnectionString;
            _hidePassword = hidePassword;
            _state = state;
        }

        internal bool AllowSetConnectionString
        {
            get
            {
                return _allowSetConnectionString;
            }
        }

        internal bool CanBePooled
        {
            get
            {
                bool flag = (!_connectionIsDoomed && !_cannotBePooled && !_owningObject.IsAlive);
                return flag;
            }
        }

        protected internal SysTx.Transaction? EnlistedTransaction
        {
            get
            {
                return _enlistedTransaction;
            }
            set
            {
                SysTx.Transaction? currentEnlistedTransaction = _enlistedTransaction;
                if (((null == currentEnlistedTransaction) && (null != value))
                    || ((null != currentEnlistedTransaction) && !currentEnlistedTransaction.Equals(value)))
                {
                    // Pay attention to the order here:
                    // 1) defect from any notifications
                    // 2) replace the transaction
                    // 3) re-enlist in notifications for the new transaction

                    // we need to use a clone of the transaction
                    // when we store it, or we'll end up keeping it past the
                    // duration of the using block of the TransactionScope
                    SysTx.Transaction? valueClone = null;
                    SysTx.Transaction? previousTransactionClone = null;
                    try
                    {
                        if (null != value)
                        {
                            valueClone = value.Clone();
                        }

                        // NOTE: rather than take locks around several potential round-
                        // trips to the server, and/or virtual function calls, we simply
                        // presume that you aren't doing something illegal from multiple
                        // threads, and check once we get around to finalizing things
                        // inside a lock.

                        lock (this)
                        {
                            // NOTE: There is still a race condition here, when we are
                            // called from EnlistTransaction (which cannot re-enlist)
                            // instead of EnlistDistributedTransaction (which can),
                            // however this should have been handled by the outer
                            // connection which checks to ensure that it's OK.  The
                            // only case where we have the race condition is multiple
                            // concurrent enlist requests to the same connection, which
                            // is a bit out of line with something we should have to
                            // support.

                            // enlisted transaction can be nullified in Dispose call without lock
                            previousTransactionClone = Interlocked.Exchange(ref _enlistedTransaction, valueClone);
                            _enlistedTransactionOriginal = value;
                            value = valueClone;
                            valueClone = null; // we've stored it, don't dispose it.
                        }
                    }
                    finally
                    {
                        // we really need to dispose our clones; they may have
                        // native resources and GC may not happen soon enough.
                        // don't dispose if still holding reference in _enlistedTransaction
                        if (null != previousTransactionClone &&
                                !object.ReferenceEquals(previousTransactionClone, _enlistedTransaction))
                        {
                            previousTransactionClone.Dispose();
                        }
                        if (null != valueClone && !object.ReferenceEquals(valueClone, _enlistedTransaction))
                        {
                            valueClone.Dispose();
                        }
                    }

                    // I don't believe that we need to lock to protect the actual
                    // enlistment in the transaction; it would only protect us
                    // against multiple concurrent calls to enlist, which really
                    // isn't supported anyway.
                }
            }
        }

        // Is this connection in stasis, waiting for transaction to end before returning to pool?
        internal bool IsTxRootWaitingForTxEnd
        {
            get
            {
                return _isInStasis;
            }
        }

        /// <summary>
        /// Get boolean that specifies whether an enlisted transaction can be unbound from
        /// the connection when that transaction completes.
        /// </summary>
        /// <value>
        /// True if the enlisted transaction can be unbound on transaction completion; otherwise false.
        /// </value>
        protected virtual bool UnbindOnTransactionCompletion
        {
            get
            {
                return true;
            }
        }

        // Is this a connection that must be put in stasis (or is already in stasis) pending the end of it's transaction?
        protected internal virtual bool IsNonPoolableTransactionRoot
        {
            get
            {
                return false; // if you want to have delegated transactions that are non-poolable, you better override this...
            }
        }

        internal virtual bool IsTransactionRoot
        {
            get
            {
                return false; // if you want to have delegated transactions, you better override this...
            }
        }

        protected internal bool IsConnectionDoomed
        {
            get
            {
                return _connectionIsDoomed;
            }
        }

        internal bool IsEmancipated
        {
            get
            {
                // NOTE: There are race conditions between PrePush, PostPop and this
                //       property getter -- only use this while this object is locked;
                //       (DbConnectionPool.Clear and ReclaimEmancipatedObjects
                //       do this for us)

                // Remember how this works (I keep getting confused...)
                //
                //    _pooledCount is incremented when the connection is pushed into the pool
                //    _pooledCount is decremented when the connection is popped from the pool
                //    _pooledCount is set to -1 when the connection is not pooled (just in case...)
                //
                // That means that:
                //
                //    _pooledCount > 1    connection is in the pool multiple times (this is a serious bug...)
                //    _pooledCount == 1   connection is in the pool
                //    _pooledCount == 0   connection is out of the pool
                //    _pooledCount == -1  connection is not a pooled connection; we shouldn't be here for non-pooled connections.
                //    _pooledCount < -1   connection out of the pool multiple times (not sure how this could happen...)
                //
                // Now, our job is to return TRUE when the connection is out
                // of the pool and it's owning object is no longer around to
                // return it.

                bool value = !IsTxRootWaitingForTxEnd && (_pooledCount < 1) && !_owningObject.IsAlive;
                return value;
            }
        }

        protected internal object? Owner
        {
            // We use a weak reference to the owning object so we can identify when
            // it has been garbage collected without throwing exceptions.
            get
            {
                return _owningObject.Target;
            }
        }

        internal DbConnectionPool? Pool
        {
            get
            {
                return _connectionPool;
            }
        }

        protected DbConnectionPoolCounters? PerformanceCounters
        {
            get
            {
                return _performanceCounters;
            }
        }
        protected internal DbReferenceCollection? ReferenceCollection
        {
            get
            {
                return _referenceCollection;
            }
        }

        public abstract string ServerVersion
        {
            get;
        }

        public bool ShouldHidePassword
        {
            get
            {
                return _hidePassword;
            }
        }

        public ConnectionState State
        {
            get
            {
                return _state;
            }
        }

        protected abstract void Activate(SysTx.Transaction? transaction);

        internal void AddWeakReference(object value, int tag)
        {
            if (null == _referenceCollection)
            {
                _referenceCollection = CreateReferenceCollection();
                if (null == _referenceCollection)
                {
                    throw ADP.InternalError(ADP.InternalErrorCode.CreateReferenceCollectionReturnedNull);
                }
            }
            _referenceCollection.Add(value, tag);
        }

        public abstract DbTransaction BeginTransaction(IsolationLevel il);

        internal virtual void PrepareForReplaceConnection()
        {
            // By default, there is no preperation required
        }

        protected virtual void PrepareForCloseConnection()
        {
            // By default, there is no preperation required
        }

        protected virtual object? ObtainAdditionalLocksForClose()
        {
            return null; // no additional locks in default implementation
        }

        protected virtual void ReleaseAdditionalLocksForClose(object? lockToken)
        {
            // no additional locks in default implementation
        }

        protected virtual DbReferenceCollection CreateReferenceCollection()
        {
            throw ADP.InternalError(ADP.InternalErrorCode.AttemptingToConstructReferenceCollectionOnStaticObject);
        }

        protected abstract void Deactivate();

        internal void DeactivateConnection()
        {
            // Internal method called from the connection pooler so we don't expose
            // the Deactivate method publicly.
#if DEBUG
            int activateCount = Interlocked.Decrement(ref _activateCount);
            Debug.Assert(0 == activateCount, "activated multiple times?");
#endif // DEBUG

            // Pool.Clear will DestroyObject that will clean performanceCounters before going here
            PerformanceCounters?.NumberOfActiveConnections.Decrement();

            if (!_connectionIsDoomed && Pool!.UseLoadBalancing)
            {
                // If we're not already doomed, check the connection's lifetime and
                // doom it if it's lifetime has elapsed.

                DateTime now = DateTime.UtcNow;
                if ((now.Ticks - _createTime.Ticks) > Pool.LoadBalanceTimeout.Ticks)
                {
                    DoNotPoolThisConnection();
                }
            }
            Deactivate();
        }

        internal virtual void DelegatedTransactionEnded()
        {
            // Called by System.Transactions when the delegated transaction has
            // completed.  We need to make closed connections that are in stasis
            // available again, or disposed closed/leaked non-pooled connections.

            // IMPORTANT NOTE: You must have taken a lock on the object before
            // you call this method to prevent race conditions with Clear and
            // ReclaimEmancipatedObjects.

            if (1 == _pooledCount)
            {
                // When _pooledCount is 1, it indicates a closed, pooled,
                // connection so it is ready to put back into the pool for
                // general use.

                TerminateStasis(true);

                Deactivate(); // call it one more time just in case

                DbConnectionPool? pool = Pool;

                if (null == pool)
                {
                    throw ADP.InternalError(ADP.InternalErrorCode.PooledObjectWithoutPool);      // pooled connection does not have a pool
                }
                pool.PutObjectFromTransactedPool(this);
            }
            else if (-1 == _pooledCount && !_owningObject.IsAlive)
            {
                // When _pooledCount is -1 and the owning object no longer exists,
                // it indicates a closed (or leaked), non-pooled connection so
                // it is safe to dispose.

                TerminateStasis(false);

                Deactivate(); // call it one more time just in case

                // it's a non-pooled connection, we need to dispose of it
                // once and for all, or the server will have fits about us
                // leaving connections open until the client-side GC kicks
                // in.
                PerformanceCounters!.NumberOfNonPooledConnections.Decrement();
                Dispose();
            }
            // When _pooledCount is 0, the connection is a pooled connection
            // that is either open (if the owning object is alive) or leaked (if
            // the owning object is not alive)  In either case, we can't muck
            // with the connection here.
        }

        protected internal void DoNotPoolThisConnection()
        {
            _cannotBePooled = true;
        }

        /// <devdoc>Ensure that this connection cannot be put back into the pool.</devdoc>
        protected internal void DoomThisConnection()
        {
            _connectionIsDoomed = true;
        }

        public abstract void EnlistTransaction(SysTx.Transaction? transaction);

        protected internal virtual DataTable GetSchema(DbConnectionFactory factory, DbConnectionPoolGroup poolGroup, DbConnection outerConnection, string collectionName, string?[]? restrictions)
        {
            Debug.Assert(outerConnection != null, "outerConnection may not be null.");

            DbMetaDataFactory metaDataFactory = factory.GetMetaDataFactory(poolGroup, this);
            Debug.Assert(metaDataFactory != null, "metaDataFactory may not be null.");

            return metaDataFactory.GetSchema(outerConnection, collectionName, restrictions);
        }

        internal void MakeNonPooledObject(object owningObject, DbConnectionPoolCounters performanceCounters)
        {
            // Used by DbConnectionFactory to indicate that this object IS NOT part of
            // a connection pool.

            _connectionPool = null;
            _performanceCounters = performanceCounters;
            _owningObject.Target = owningObject;
            _pooledCount = -1;
        }

        internal void MakePooledConnection(DbConnectionPool connectionPool)
        {
            // Used by DbConnectionFactory to indicate that this object IS part of
            // a connection pool.

            // TODO: consider using ADP.TimerCurrent() for this.
            _createTime = DateTime.UtcNow;

            _connectionPool = connectionPool;
            _performanceCounters = connectionPool.PerformanceCounters;
        }

        internal void NotifyWeakReference(int message)
        {
            ReferenceCollection?.Notify(message);
        }

        internal virtual void OpenConnection(DbConnection outerConnection, DbConnectionFactory connectionFactory)
        {
            if (!TryOpenConnection(outerConnection, connectionFactory, null, null))
            {
                throw ADP.InternalError(ADP.InternalErrorCode.SynchronousConnectReturnedPending);
            }
        }

        /// <devdoc>The default implementation is for the open connection objects, and
        /// it simply throws.  Our private closed-state connection objects
        /// override this and do the correct thing.</devdoc>
        // User code should either override DbConnectionInternal.Activate when it comes out of the pool
        // or override DbConnectionFactory.CreateConnection when the connection is created for non-pooled connections
        internal virtual bool TryOpenConnection(DbConnection outerConnection, DbConnectionFactory connectionFactory, TaskCompletionSource<DbConnectionInternal>? retry, DbConnectionOptions? userOptions)
        {
            throw ADP.ConnectionAlreadyOpen(State);
        }

        protected bool TryOpenConnectionInternal(DbConnection outerConnection, DbConnectionFactory connectionFactory, TaskCompletionSource<DbConnectionInternal>? retry, DbConnectionOptions? userOptions)
        {
            // ?->Connecting: prevent set_ConnectionString during Open
            if (connectionFactory.SetInnerConnectionFrom(outerConnection, DbConnectionClosedConnecting.SingletonInstance, this))
            {
                DbConnectionInternal? openConnection;
                try
                {
                    connectionFactory.PermissionDemand(outerConnection);
                    if (!connectionFactory.TryGetConnection(outerConnection, retry, userOptions, this, out openConnection))
                    {
                        return false;
                    }
                }
                catch
                {
                    // This should occur for all exceptions, even ADP.UnCatchableExceptions.
                    connectionFactory.SetInnerConnectionTo(outerConnection, this);
                    throw;
                }
                if (null == openConnection)
                {
                    connectionFactory.SetInnerConnectionTo(outerConnection, this);
                    throw ADP.InternalConnectionError(ADP.ConnectionError.GetConnectionReturnsNull);
                }
                connectionFactory.SetInnerConnectionEvent(outerConnection, openConnection);
            }

            return true;
        }

        internal void PrePush(object? expectedOwner)
        {
            // Called by DbConnectionPool when we're about to be put into it's pool, we
            // take this opportunity to ensure ownership and pool counts are legit.

            // IMPORTANT NOTE: You must have taken a lock on the object before
            // you call this method to prevent race conditions with Clear and
            // ReclaimEmancipatedObjects.

            //3 // The following tests are retail assertions of things we can't allow to happen.
            if (null == expectedOwner)
            {
                if (null != _owningObject.Target)
                {
                    throw ADP.InternalError(ADP.InternalErrorCode.UnpooledObjectHasOwner);      // new unpooled object has an owner
                }
            }
            else if (_owningObject.Target != expectedOwner)
            {
                throw ADP.InternalError(ADP.InternalErrorCode.UnpooledObjectHasWrongOwner); // unpooled object has incorrect owner
            }
            if (0 != _pooledCount)
            {
                throw ADP.InternalError(ADP.InternalErrorCode.PushingObjectSecondTime);         // pushing object onto stack a second time
            }
            _pooledCount++;
            _owningObject.Target = null; // NOTE: doing this and checking for InternalError.PooledObjectHasOwner degrades the close by 2%
        }

        internal void PostPop(object newOwner)
        {
            // Called by DbConnectionPool right after it pulls this from it's pool, we
            // take this opportunity to ensure ownership and pool counts are legit.

            Debug.Assert(!IsEmancipated, "pooled object not in pool");

            // When another thread is clearing this pool, it
            // will doom all connections in this pool without prejudice which
            // causes the following assert to fire, which really mucks up stress
            // against checked bits.  The assert is benign, so we're commenting
            // it out.
            //Debug.Assert(CanBePooled,   "pooled object is not poolable");

            // IMPORTANT NOTE: You must have taken a lock on the object before
            // you call this method to prevent race conditions with Clear and
            // ReclaimEmancipatedObjects.

            if (null != _owningObject.Target)
            {
                throw ADP.InternalError(ADP.InternalErrorCode.PooledObjectHasOwner);        // pooled connection already has an owner!
            }
            _owningObject.Target = newOwner;
            _pooledCount--;
            //3 // The following tests are retail assertions of things we can't allow to happen.
            if (null != Pool)
            {
                if (0 != _pooledCount)
                {
                    throw ADP.InternalError(ADP.InternalErrorCode.PooledObjectInPoolMoreThanOnce);  // popping object off stack with multiple pooledCount
                }
            }
            else if (-1 != _pooledCount)
            {
                throw ADP.InternalError(ADP.InternalErrorCode.NonPooledObjectUsedMoreThanOnce); // popping object off stack with multiple pooledCount
            }
        }

        internal void RemoveWeakReference(object value)
        {
            ReferenceCollection?.Remove(value);
        }

        internal void DetachCurrentTransactionIfEnded()
        {
            SysTx.Transaction? enlistedTransaction = EnlistedTransaction;
            if (enlistedTransaction != null)
            {
                bool transactionIsDead;
                try
                {
                    transactionIsDead = (SysTx.TransactionStatus.Active != enlistedTransaction.TransactionInformation.Status);
                }
                catch (SysTx.TransactionException)
                {
                    // If the transaction is being processed (i.e. is part way through a rollback\commit\etc then TransactionInformation.Status will throw an exception)
                    transactionIsDead = true;
                }
                if (transactionIsDead)
                {
                    DetachTransaction(enlistedTransaction, true);
                }
            }
        }

        // Detach transaction from connection.
        internal void DetachTransaction(SysTx.Transaction transaction, bool isExplicitlyReleasing)
        {
            // potentially a multi-threaded event, so lock the connection to make sure we don't enlist in a new
            // transaction between compare and assignment. No need to short circuit outside of lock, since failed comparisons should
            // be the exception, not the rule.
            lock (this)
            {
                // Detach if detach-on-end behavior, or if outer connection was closed
                DbConnection? owner = (DbConnection?)Owner;
                if (isExplicitlyReleasing || UnbindOnTransactionCompletion || null == owner)
                {
                    SysTx.Transaction? currentEnlistedTransaction = _enlistedTransaction;
                    if (currentEnlistedTransaction != null && transaction.Equals(currentEnlistedTransaction))
                    {
                        EnlistedTransaction = null;

                        if (IsTxRootWaitingForTxEnd)
                        {
                            DelegatedTransactionEnded();
                        }
                    }
                }
            }
        }

        internal void SetInStasis()
        {
            _isInStasis = true;
            PerformanceCounters!.NumberOfStasisConnections.Increment();
        }

        private void TerminateStasis(bool returningToPool)
        {
            PerformanceCounters!.NumberOfStasisConnections.Decrement();
            _isInStasis = false;
        }

        /// <summary>
        /// When overridden in a derived class, will check if the underlying connection is still actually alive
        /// </summary>
        /// <param name="throwOnException">If true an exception will be thrown if the connection is dead instead of returning true\false
        /// (this allows the caller to have the real reason that the connection is not alive (e.g. network error, etc))</param>
        /// <returns>True if the connection is still alive, otherwise false (If not overridden, then always true)</returns>
        internal virtual bool IsConnectionAlive(bool throwOnException = false)
        {
            return true;
        }
    }
}
