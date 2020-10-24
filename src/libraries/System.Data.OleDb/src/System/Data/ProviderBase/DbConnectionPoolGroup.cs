// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;

namespace System.Data.ProviderBase
{
    // set_ConnectionString calls DbConnectionFactory.GetConnectionPoolGroup
    // when not found a new pool entry is created and potentially added
    // DbConnectionPoolGroup starts in the Active state

    // Open calls DbConnectionFactory.GetConnectionPool
    // if the existing pool entry is Disabled, GetConnectionPoolGroup is called for a new entry
    // DbConnectionFactory.GetConnectionPool calls DbConnectionPoolGroup.GetConnectionPool

    // DbConnectionPoolGroup.GetConnectionPool will return pool for the current identity
    // or null if identity is restricted or pooling is disabled or state is disabled at time of add
    // state changes are Active->Active, Idle->Active

    // DbConnectionFactory.PruneConnectionPoolGroups calls Prune
    // which will QueuePoolForRelease on all empty pools
    // and once no pools remain, change state from Active->Idle->Disabled
    // Once Disabled, factory can remove its reference to the pool entry

    internal sealed class DbConnectionPoolGroup
    {
        private readonly DbConnectionOptions _connectionOptions;
        private readonly DbConnectionPoolKey _poolKey;
        private readonly DbConnectionPoolGroupOptions _poolGroupOptions;
        private ConcurrentDictionary<DbConnectionPoolIdentity, DbConnectionPool> _poolCollection;

        private int _state;          // see PoolGroupState* below

        private DbConnectionPoolGroupProviderInfo? _providerInfo;
        private DbMetaDataFactory? _metaDataFactory;

        // always lock this before changing _state, we don't want to move out of the 'Disabled' state
        // PoolGroupStateUninitialized = 0;
        private const int PoolGroupStateActive = 1; // initial state, GetPoolGroup from cache, connection Open
        private const int PoolGroupStateIdle = 2; // all pools are pruned via Clear
        private const int PoolGroupStateDisabled = 4; // factory pool entry prunning method

        internal DbConnectionPoolGroup(DbConnectionOptions connectionOptions, DbConnectionPoolKey key, DbConnectionPoolGroupOptions poolGroupOptions)
        {
            Debug.Assert(null != connectionOptions, "null connection options");
            Debug.Assert(null == poolGroupOptions || ADP.IsWindowsNT, "should not have pooling options on Win9x");

            _connectionOptions = connectionOptions;
            _poolKey = key;
            _poolGroupOptions = poolGroupOptions!;

            // always lock this object before changing state
            // HybridDictionary does not create any sub-objects until add
            // so it is safe to use for non-pooled connection as long as
            // we check _poolGroupOptions first
            _poolCollection = new ConcurrentDictionary<DbConnectionPoolIdentity, DbConnectionPool>();
            _state = PoolGroupStateActive;
        }

        internal DbConnectionOptions ConnectionOptions
        {
            get
            {
                return _connectionOptions;
            }
        }

        internal DbConnectionPoolKey PoolKey
        {
            get
            {
                return _poolKey;
            }
        }

        internal DbConnectionPoolGroupProviderInfo? ProviderInfo
        {
            get
            {
                return _providerInfo;
            }
            set
            {
                _providerInfo = value;
                if (null != value)
                {
                    _providerInfo!.PoolGroup = this;
                }
            }
        }

        internal bool IsDisabled
        {
            get
            {
                return (PoolGroupStateDisabled == _state);
            }
        }

        internal DbConnectionPoolGroupOptions PoolGroupOptions
        {
            get
            {
                return _poolGroupOptions;
            }
        }

        internal DbMetaDataFactory? MetaDataFactory
        {
            get
            {
                return _metaDataFactory;
            }

            set
            {
                _metaDataFactory = value;
            }
        }

        internal int Clear()
        {
            // must be multi-thread safe with competing calls by Clear and Prune via background thread
            // will return the number of connections in the group after clearing has finished

            // First, note the old collection and create a new collection to be used
            ConcurrentDictionary<DbConnectionPoolIdentity, DbConnectionPool>? oldPoolCollection = null;
            lock (this)
            {
                if (!_poolCollection.IsEmpty)
                {
                    oldPoolCollection = _poolCollection;
                    _poolCollection = new ConcurrentDictionary<DbConnectionPoolIdentity, DbConnectionPool>();
                }
            }

            // Then, if a new collection was created, release the pools from the old collection
            if (oldPoolCollection != null)
            {
                foreach (var entry in oldPoolCollection)
                {
                    DbConnectionPool pool = entry.Value;
                    if (pool != null)
                    {
                        //  Pruning a pool while a connection is currently attempting to connect
                        //  will cause the pool to be prematurely abandoned. The only known effect so
                        //  far is that the errorWait throttling will be reset when this occurs.
                        //  We should be able to avoid this situation by not pruning the pool if
                        //  it's _waitCount is non-zero (i.e. no connections *in* the pool, but also
                        //  no connections attempting to be created for the pool).

                        DbConnectionFactory connectionFactory = pool.ConnectionFactory;
                        connectionFactory.PerformanceCounters.NumberOfActiveConnectionPools.Decrement();
                        connectionFactory.QueuePoolForRelease(pool, true);
                    }
                }
            }

            // Finally, return the pool collection count - this may be non-zero if something was added while we were clearing
            return _poolCollection.Count;
        }

        internal DbConnectionPool? GetConnectionPool(DbConnectionFactory connectionFactory)
        {
            // When this method returns null it indicates that the connection
            // factory should not use pooling.

            // We don't support connection pooling on Win9x; it lacks too
            // many of the APIs we require.
            // PoolGroupOptions will only be null when we're not supposed to pool
            // connections.
            DbConnectionPool? pool = null;
            if (null != _poolGroupOptions)
            {
                Debug.Assert(ADP.IsWindowsNT, "should not be pooling on Win9x");

                DbConnectionPoolIdentity? currentIdentity = DbConnectionPoolIdentity.NoIdentity;
                if (_poolGroupOptions.PoolByIdentity)
                {
                    // if we're pooling by identity (because integrated security is
                    // being used for these connections) then we need to go out and
                    // search for the connectionPool that matches the current identity.

                    currentIdentity = DbConnectionPoolIdentity.GetCurrent();

                    // If the current token is restricted in some way, then we must
                    // not attempt to pool these connections.
                    if (currentIdentity.IsRestricted)
                    {
                        currentIdentity = null;
                    }
                }

                if (null != currentIdentity)
                {
                    if (!_poolCollection.TryGetValue(currentIdentity, out pool))
                    { // find the pool
                        lock (this)
                        {
                            // Did someone already add it to the list?
                            if (!_poolCollection.TryGetValue(currentIdentity, out pool))
                            {
                                DbConnectionPool newPool = new DbConnectionPool(connectionFactory, this, currentIdentity);

                                if (MarkPoolGroupAsActive())
                                {
                                    // If we get here, we know for certain that we there isn't
                                    // a pool that matches the current identity, so we have to
                                    // add the optimistically created one
                                    newPool.Startup(); // must start pool before usage
                                    bool addResult = _poolCollection.TryAdd(currentIdentity, newPool);
                                    Debug.Assert(addResult, "No other pool with current identity should exist at this point");
                                    connectionFactory.PerformanceCounters.NumberOfActiveConnectionPools.Increment();
                                    pool = newPool;
                                }
                                else
                                {
                                    // else pool entry has been disabled so don't create new pools
                                    Debug.Assert(PoolGroupStateDisabled == _state, "state should be disabled");

                                    // don't need to call connectionFactory.QueuePoolForRelease(newPool) because
                                    // pool callbacks were delayed and no risk of connections being created
                                    newPool.Shutdown();
                                }
                            }
                            else
                            {
                                // else found an existing pool to use instead
                                Debug.Assert(PoolGroupStateActive == _state, "state should be active since a pool exists and lock holds");
                            }
                        }
                    }
                    // the found pool could be in any state
                }
            }

            if (null == pool)
            {
                lock (this)
                {
                    // keep the pool entry state active when not pooling
                    MarkPoolGroupAsActive();
                }
            }
            return pool;
        }

        private bool MarkPoolGroupAsActive()
        {
            // when getting a connection, make the entry active if it was idle (but not disabled)
            // must always lock this before calling

            if (PoolGroupStateIdle == _state)
            {
                _state = PoolGroupStateActive;
            }
            return (PoolGroupStateActive == _state);
        }

        internal bool Prune()
        {
            // must only call from DbConnectionFactory.PruneConnectionPoolGroups on background timer thread
            // must lock(DbConnectionFactory._connectionPoolGroups.SyncRoot) before calling ReadyToRemove
            //     to avoid conflict with DbConnectionFactory.CreateConnectionPoolGroup replacing pool entry
            lock (this)
            {
                if (!_poolCollection.IsEmpty)
                {
                    var newPoolCollection = new ConcurrentDictionary<DbConnectionPoolIdentity, DbConnectionPool>();

                    foreach (var entry in _poolCollection)
                    {
                        DbConnectionPool pool = entry.Value;
                        if (pool != null)
                        {
                            //  Pruning a pool while a connection is currently attempting to connect
                            //  will cause the pool to be prematurely abandoned. The only known effect so
                            //  far is that the errorWait throttling will be reset when this occurs.
                            //  We should be able to avoid this situation by not pruning the pool if
                            //  it's _waitCount is non-zero (i.e. no connections *in* the pool, but also
                            //  no connections attempting to be created for the pool).

                            // Actually prune the pool if there are no connections in the pool and no errors occurred.
                            // Empty pool during pruning indicates zero or low activity, but
                            //  an error state indicates the pool needs to stay around to
                            //  throttle new connection attempts.
                            if ((!pool.ErrorOccurred) && (0 == pool.Count))
                            {
                                // Order is important here.  First we remove the pool
                                // from the collection of pools so no one will try
                                // to use it while we're processing and finally we put the
                                // pool into a list of pools to be released when they
                                // are completely empty.
                                DbConnectionFactory connectionFactory = pool.ConnectionFactory;

                                connectionFactory.PerformanceCounters.NumberOfActiveConnectionPools.Decrement();
                                connectionFactory.QueuePoolForRelease(pool, false);
                            }
                            else
                            {
                                newPoolCollection.TryAdd(entry.Key, entry.Value);
                            }
                        }
                    }
                    _poolCollection = newPoolCollection;
                }

                // must be pruning thread to change state and no connections
                // otherwise pruning thread risks making entry disabled soon after user calls ClearPool
                if (_poolCollection.IsEmpty)
                {
                    if (PoolGroupStateActive == _state)
                    {
                        _state = PoolGroupStateIdle;
                    }
                    else if (PoolGroupStateIdle == _state)
                    {
                        _state = PoolGroupStateDisabled;
                    }
                }
                return (PoolGroupStateDisabled == _state);
            }
        }
    }
}
