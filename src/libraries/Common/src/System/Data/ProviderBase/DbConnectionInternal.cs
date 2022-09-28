// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;


namespace System.Data.ProviderBase
{
    internal abstract partial class DbConnectionInternal
    {
        internal static readonly StateChangeEventArgs StateChangeClosed = new StateChangeEventArgs(ConnectionState.Open, ConnectionState.Closed);
        internal static readonly StateChangeEventArgs StateChangeOpen = new StateChangeEventArgs(ConnectionState.Closed, ConnectionState.Open);

        private readonly bool _allowSetConnectionString;
        private readonly bool _hidePassword;
        private readonly ConnectionState _state;

        private readonly WeakReference _owningObject = new WeakReference(null, false);  // [usage must be thread safe] the owning object, when not in the pool. (both Pooled and Non-Pooled connections)

        private DbConnectionPool? _connectionPool;           // the pooler that the connection came from (Pooled connections only)
        private DbReferenceCollection? _referenceCollection;      // collection of objects that we need to notify in some way when we're being deactivated
        private int _pooledCount;              // [usage must be thread safe] the number of times this object has been pushed into the pool less the number of times it's been popped (0 != inPool)

        private bool _connectionIsDoomed;       // true when the connection should no longer be used.
        private bool _cannotBePooled;           // true when the connection should no longer be pooled.

        private DateTime _createTime;               // when the connection was created.

#if DEBUG
        private int _activateCount;            // debug only counter to verify activate/deactivates are in sync.
#endif //DEBUG

        protected DbConnectionInternal() : this(ConnectionState.Open, true, false)
        {
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

                // The functionality is as follows:
                //
                //    _pooledCount is incremented when the connection is pushed into the pool
                //    _pooledCount is decremented when the connection is popped from the pool
                //    _pooledCount is set to -1 when the connection is not pooled (just in case...)
                //
                // That means that:
                //
                //    _pooledCount > 1    connection is in the pool multiple times (This should not happen)
                //    _pooledCount == 1   connection is in the pool
                //    _pooledCount == 0   connection is out of the pool
                //    _pooledCount == -1  connection is not a pooled connection; we shouldn't be here for non-pooled connections.
                //    _pooledCount < -1   connection out of the pool multiple times
                //
                // Now, our job is to return TRUE when the connection is out
                // of the pool and it's owning object is no longer around to
                // return it.

                bool value = (_pooledCount < 1) && !_owningObject.IsAlive;
                return value;
            }
        }

        internal bool IsInPool
        {
            get
            {
                Debug.Assert(_pooledCount <= 1 && _pooledCount >= -1, "Pooled count for object is invalid");
                return (_pooledCount == 1);
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

        // this should be abstract but until it is added to all the providers virtual will have to do
        public virtual string ServerVersionNormalized
        {
            get
            {
                throw ADP.NotSupported();
            }
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

        public virtual void ChangeDatabase(string value)
        {
            throw ADP.MethodNotImplemented();
        }

        internal virtual void PrepareForReplaceConnection()
        {
            // By default, there is no preparation required
        }

        protected virtual void PrepareForCloseConnection()
        {
            // By default, there is no preparation required
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
            Debug.Assert(Pool != null);

            // Internal method called from the connection pooler so we don't expose
            // the Deactivate method publicly.

#if DEBUG
            Interlocked.Decrement(ref _activateCount);
#endif // DEBUG


            if (!_connectionIsDoomed && Pool.UseLoadBalancing)
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

        protected internal void DoNotPoolThisConnection()
        {
            _cannotBePooled = true;
        }

        /// <devdoc>Ensure that this connection cannot be put back into the pool.</devdoc>
        protected internal void DoomThisConnection()
        {
            _connectionIsDoomed = true;
        }

        protected internal virtual DataTable GetSchema(DbConnectionFactory factory, DbConnectionPoolGroup poolGroup, DbConnection outerConnection, string collectionName, string?[]? restrictions)
        {
            Debug.Assert(outerConnection != null, "outerConnection may not be null.");

            DbMetaDataFactory metaDataFactory = factory.GetMetaDataFactory(poolGroup, this);
            Debug.Assert(metaDataFactory != null, "metaDataFactory may not be null.");

            return metaDataFactory.GetSchema(outerConnection, collectionName, restrictions);
        }

        internal void MakeNonPooledObject(object owningObject)
        {
            // Used by DbConnectionFactory to indicate that this object IS NOT part of
            // a connection pool.

            _connectionPool = null;
            _owningObject.Target = owningObject;
            _pooledCount = -1;
        }

        internal void MakePooledConnection(DbConnectionPool connectionPool)
        {
            // Used by DbConnectionFactory to indicate that this object IS part of
            // a connection pool.
            _createTime = DateTime.UtcNow;

            _connectionPool = connectionPool;
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

        internal virtual bool TryReplaceConnection(DbConnection outerConnection, DbConnectionFactory connectionFactory, TaskCompletionSource<DbConnectionInternal>? retry, DbConnectionOptions? userOptions)
        {
            throw ADP.MethodNotImplemented();
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
