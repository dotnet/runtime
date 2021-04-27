// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data.ProviderBase
{
    internal abstract partial class DbConnectionFactory
    {
        private Dictionary<DbConnectionPoolKey, DbConnectionPoolGroup> _connectionPoolGroups;
        private readonly List<DbConnectionPool> _poolsToRelease;
        private readonly List<DbConnectionPoolGroup> _poolGroupsToRelease;
        private readonly Timer _pruningTimer;
        private const int PruningDueTime = 4 * 60 * 1000;           // 4 minutes
        private const int PruningPeriod = 30 * 1000;           // thirty seconds


        // s_pendingOpenNonPooled is an array of tasks used to throttle creation of non-pooled connections to
        // a maximum of Environment.ProcessorCount at a time.
        private static uint s_pendingOpenNonPooledNext;
        private static readonly Task<DbConnectionInternal?>[] s_pendingOpenNonPooled = new Task<DbConnectionInternal?>[Environment.ProcessorCount];
        private static Task<DbConnectionInternal?>? s_completedTask;

        protected DbConnectionFactory()
        {
            _connectionPoolGroups = new Dictionary<DbConnectionPoolKey, DbConnectionPoolGroup>();
            _poolsToRelease = new List<DbConnectionPool>();
            _poolGroupsToRelease = new List<DbConnectionPoolGroup>();
            _pruningTimer = CreatePruningTimer();
        }


        public abstract DbProviderFactory ProviderFactory
        {
            get;
        }


        public void ClearAllPools()
        {
            Dictionary<DbConnectionPoolKey, DbConnectionPoolGroup> connectionPoolGroups = _connectionPoolGroups;
            foreach (KeyValuePair<DbConnectionPoolKey, DbConnectionPoolGroup> entry in connectionPoolGroups)
            {
                DbConnectionPoolGroup poolGroup = entry.Value;
                if (null != poolGroup)
                {
                    poolGroup.Clear();
                }
            }
        }

        public void ClearPool(DbConnection connection)
        {
            ADP.CheckArgumentNull(connection, nameof(connection));

            DbConnectionPoolGroup? poolGroup = GetConnectionPoolGroup(connection);
            if (null != poolGroup)
            {
                poolGroup.Clear();
            }
        }

        public void ClearPool(DbConnectionPoolKey key)
        {
            Debug.Assert(key != null, "key cannot be null");
            ADP.CheckArgumentNull(key.ConnectionString, nameof(key) + "." + nameof(key.ConnectionString));

            DbConnectionPoolGroup? poolGroup;
            Dictionary<DbConnectionPoolKey, DbConnectionPoolGroup> connectionPoolGroups = _connectionPoolGroups;
            if (connectionPoolGroups.TryGetValue(key, out poolGroup))
            {
                poolGroup.Clear();
            }
        }

        internal virtual DbConnectionPoolProviderInfo? CreateConnectionPoolProviderInfo(DbConnectionOptions connectionOptions)
        {
            return null;
        }


        internal DbConnectionInternal? CreateNonPooledConnection(DbConnection owningConnection, DbConnectionPoolGroup poolGroup, DbConnectionOptions? userOptions)
        {
            Debug.Assert(null != owningConnection, "null owningConnection?");
            Debug.Assert(null != poolGroup, "null poolGroup?");

            DbConnectionOptions connectionOptions = poolGroup.ConnectionOptions;
            DbConnectionPoolGroupProviderInfo poolGroupProviderInfo = poolGroup.ProviderInfo!;
            DbConnectionPoolKey poolKey = poolGroup.PoolKey;

            DbConnectionInternal? newConnection = CreateConnection(connectionOptions, poolKey, poolGroupProviderInfo, null, owningConnection, userOptions);
            if (null != newConnection)
            {
                newConnection.MakeNonPooledObject(owningConnection);
            }
            return newConnection;
        }

        internal DbConnectionInternal? CreatePooledConnection(DbConnectionPool pool, DbConnection? owningObject, DbConnectionOptions options, DbConnectionPoolKey poolKey, DbConnectionOptions? userOptions)
        {
            Debug.Assert(null != pool, "null pool?");
            DbConnectionPoolGroupProviderInfo poolGroupProviderInfo = pool.PoolGroup.ProviderInfo!;

            DbConnectionInternal? newConnection = CreateConnection(options, poolKey, poolGroupProviderInfo, pool, owningObject, userOptions);
            if (null != newConnection)
            {
                newConnection.MakePooledConnection(pool);
            }
            return newConnection;
        }

        internal virtual DbConnectionPoolGroupProviderInfo? CreateConnectionPoolGroupProviderInfo(DbConnectionOptions connectionOptions)
        {
            return null;
        }

        private Timer CreatePruningTimer() =>
            ADP.UnsafeCreateTimer(
                new TimerCallback(PruneConnectionPoolGroups),
                null,
                PruningDueTime,
                PruningPeriod);

        protected DbConnectionOptions? FindConnectionOptions(DbConnectionPoolKey key)
        {
            Debug.Assert(key != null, "key cannot be null");
            if (!string.IsNullOrEmpty(key.ConnectionString))
            {
                DbConnectionPoolGroup? connectionPoolGroup;
                Dictionary<DbConnectionPoolKey, DbConnectionPoolGroup> connectionPoolGroups = _connectionPoolGroups;
                if (connectionPoolGroups.TryGetValue(key, out connectionPoolGroup))
                {
                    return connectionPoolGroup.ConnectionOptions;
                }
            }
            return null;
        }

        private static Task<DbConnectionInternal?> GetCompletedTask()
        {
            Debug.Assert(Monitor.IsEntered(s_pendingOpenNonPooled), $"Expected {nameof(s_pendingOpenNonPooled)} lock to be held.");
            return s_completedTask ?? (s_completedTask = Task.FromResult<DbConnectionInternal?>(null));
        }

        private DbConnectionPool? GetConnectionPool(DbConnection owningObject, DbConnectionPoolGroup connectionPoolGroup)
        {
            // if poolgroup is disabled, it will be replaced with a new entry

            Debug.Assert(null != owningObject, "null owningObject?");
            Debug.Assert(null != connectionPoolGroup, "null connectionPoolGroup?");

            // It is possible that while the outer connection object has
            // been sitting around in a closed and unused state in some long
            // running app, the pruner may have come along and remove this
            // the pool entry from the master list.  If we were to use a
            // pool entry in this state, we would create "unmanaged" pools,
            // which would be bad.  To avoid this problem, we automagically
            // re-create the pool entry whenever it's disabled.

            // however, don't rebuild connectionOptions if no pooling is involved - let new connections do that work
            if (connectionPoolGroup.IsDisabled && (null != connectionPoolGroup.PoolGroupOptions))
            {
                // reusing existing pool option in case user originally used SetConnectionPoolOptions
                DbConnectionPoolGroupOptions? poolOptions = connectionPoolGroup.PoolGroupOptions;

                // get the string to hash on again
                DbConnectionOptions? connectionOptions = connectionPoolGroup.ConnectionOptions;
                Debug.Assert(null != connectionOptions, "prevent expansion of connectionString");

                connectionPoolGroup = GetConnectionPoolGroup(connectionPoolGroup.PoolKey, poolOptions, ref connectionOptions)!;
                Debug.Assert(null != connectionPoolGroup, "null connectionPoolGroup?");
                SetConnectionPoolGroup(owningObject, connectionPoolGroup);
            }
            DbConnectionPool? connectionPool = connectionPoolGroup.GetConnectionPool(this);
            return connectionPool;
        }

        internal DbConnectionPoolGroup? GetConnectionPoolGroup(DbConnectionPoolKey key, DbConnectionPoolGroupOptions? poolOptions, ref DbConnectionOptions? userConnectionOptions)
        {
            if (string.IsNullOrEmpty(key.ConnectionString))
            {
                return null;
            }

            DbConnectionPoolGroup? connectionPoolGroup;
            Dictionary<DbConnectionPoolKey, DbConnectionPoolGroup> connectionPoolGroups = _connectionPoolGroups;
            if (!connectionPoolGroups.TryGetValue(key, out connectionPoolGroup) || (connectionPoolGroup.IsDisabled && (null != connectionPoolGroup.PoolGroupOptions)))
            {
                // If we can't find an entry for the connection string in
                // our collection of pool entries, then we need to create a
                // new pool entry and add it to our collection.

                DbConnectionOptions? connectionOptions = CreateConnectionOptions(key.ConnectionString, userConnectionOptions);
                if (null == connectionOptions)
                {
                    throw ADP.InternalConnectionError(ADP.ConnectionError.ConnectionOptionsMissing);
                }

                if (null == userConnectionOptions)
                { // we only allow one expansion on the connection string
                    userConnectionOptions = connectionOptions;
                }

                // We don't support connection pooling on Win9x
                if (null == poolOptions)
                {
                    if (null != connectionPoolGroup)
                    {
                        // reusing existing pool option in case user originally used SetConnectionPoolOptions
                        poolOptions = connectionPoolGroup.PoolGroupOptions;
                    }
                    else
                    {
                        // Note: may return null for non-pooled connections
                        poolOptions = CreateConnectionPoolGroupOptions(connectionOptions);
                    }
                }

                lock (this)
                {
                    connectionPoolGroups = _connectionPoolGroups;
                    if (!connectionPoolGroups.TryGetValue(key, out connectionPoolGroup))
                    {
                        DbConnectionPoolGroup newConnectionPoolGroup = new DbConnectionPoolGroup(connectionOptions, key, poolOptions!);
                        newConnectionPoolGroup.ProviderInfo = CreateConnectionPoolGroupProviderInfo(connectionOptions);

                        // build new dictionary with space for new connection string
                        Dictionary<DbConnectionPoolKey, DbConnectionPoolGroup> newConnectionPoolGroups = new Dictionary<DbConnectionPoolKey, DbConnectionPoolGroup>(1 + connectionPoolGroups.Count);
                        foreach (KeyValuePair<DbConnectionPoolKey, DbConnectionPoolGroup> entry in connectionPoolGroups)
                        {
                            newConnectionPoolGroups.Add(entry.Key, entry.Value);
                        }

                        // lock prevents race condition with PruneConnectionPoolGroups
                        newConnectionPoolGroups.Add(key, newConnectionPoolGroup);
                        connectionPoolGroup = newConnectionPoolGroup;
                        _connectionPoolGroups = newConnectionPoolGroups;
                    }
                    else
                    {
                        Debug.Assert(!connectionPoolGroup.IsDisabled, "Disabled pool entry discovered");
                    }
                }
                Debug.Assert(null != connectionPoolGroup, "how did we not create a pool entry?");
                Debug.Assert(null != userConnectionOptions, "how did we not have user connection options?");
            }
            else if (null == userConnectionOptions)
            {
                userConnectionOptions = connectionPoolGroup.ConnectionOptions;
            }
            return connectionPoolGroup;
        }


        private void PruneConnectionPoolGroups(object? state)
        {
            // First, walk the pool release list and attempt to clear each
            // pool, when the pool is finally empty, we dispose of it.  If the
            // pool isn't empty, it's because there are active connections or
            // distributed transactions that need it.
            lock (_poolsToRelease)
            {
                if (0 != _poolsToRelease.Count)
                {
                    DbConnectionPool[] poolsToRelease = _poolsToRelease.ToArray();
                    foreach (DbConnectionPool pool in poolsToRelease)
                    {
                        if (null != pool)
                        {
                            pool.Clear();

                            if (0 == pool.Count)
                            {
                                _poolsToRelease.Remove(pool);
                            }
                        }
                    }
                }
            }

            // Next, walk the pool entry release list and dispose of each
            // pool entry when it is finally empty.  If the pool entry isn't
            // empty, it's because there are active pools that need it.
            lock (_poolGroupsToRelease)
            {
                if (0 != _poolGroupsToRelease.Count)
                {
                    DbConnectionPoolGroup[] poolGroupsToRelease = _poolGroupsToRelease.ToArray();
                    foreach (DbConnectionPoolGroup poolGroup in poolGroupsToRelease)
                    {
                        if (null != poolGroup)
                        {
                            int poolsLeft = poolGroup.Clear(); // may add entries to _poolsToRelease

                            if (0 == poolsLeft)
                            {
                                _poolGroupsToRelease.Remove(poolGroup);
                            }
                        }
                    }
                }
            }

            // Finally, we walk through the collection of connection pool entries
            // and prune each one.  This will cause any empty pools to be put
            // into the release list.
            lock (this)
            {
                Dictionary<DbConnectionPoolKey, DbConnectionPoolGroup> connectionPoolGroups = _connectionPoolGroups;
                Dictionary<DbConnectionPoolKey, DbConnectionPoolGroup> newConnectionPoolGroups = new Dictionary<DbConnectionPoolKey, DbConnectionPoolGroup>(connectionPoolGroups.Count);

                foreach (KeyValuePair<DbConnectionPoolKey, DbConnectionPoolGroup> entry in connectionPoolGroups)
                {
                    if (null != entry.Value)
                    {
                        Debug.Assert(!entry.Value.IsDisabled, "Disabled pool entry discovered");

                        // entries start active and go idle during prune if all pools are gone
                        // move idle entries from last prune pass to a queue for pending release
                        // otherwise process entry which may move it from active to idle
                        if (entry.Value.Prune())
                        { // may add entries to _poolsToRelease
                            QueuePoolGroupForRelease(entry.Value);
                        }
                        else
                        {
                            newConnectionPoolGroups.Add(entry.Key, entry.Value);
                        }
                    }
                }
                _connectionPoolGroups = newConnectionPoolGroups;
            }
        }

        internal void QueuePoolForRelease(DbConnectionPool pool, bool clearing)
        {
            // Queue the pool up for release -- we'll clear it out and dispose
            // of it as the last part of the pruning timer callback so we don't
            // do it with the pool entry or the pool collection locked.
            Debug.Assert(null != pool, "null pool?");

            // set the pool to the shutdown state to force all active
            // connections to be automatically disposed when they
            // are returned to the pool
            pool.Shutdown();

            lock (_poolsToRelease)
            {
                if (clearing)
                {
                    pool.Clear();
                }
                _poolsToRelease.Add(pool);
            }
        }

        internal void QueuePoolGroupForRelease(DbConnectionPoolGroup poolGroup)
        {
            Debug.Assert(null != poolGroup, "null poolGroup?");

            lock (_poolGroupsToRelease)
            {
                _poolGroupsToRelease.Add(poolGroup);
            }
        }

        protected virtual DbConnectionInternal? CreateConnection(DbConnectionOptions options, DbConnectionPoolKey poolKey, object poolGroupProviderInfo, DbConnectionPool? pool, DbConnection? owningConnection, DbConnectionOptions? userOptions)
        {
            return CreateConnection(options, poolKey, poolGroupProviderInfo, pool, owningConnection);
        }

        internal DbMetaDataFactory GetMetaDataFactory(DbConnectionPoolGroup connectionPoolGroup, DbConnectionInternal internalConnection)
        {
            Debug.Assert(connectionPoolGroup != null, "connectionPoolGroup may not be null.");

            // get the matadatafactory from the pool entry. If it does not already have one
            // create one and save it on the pool entry
            DbMetaDataFactory? metaDataFactory = connectionPoolGroup.MetaDataFactory;

            // consider serializing this so we don't construct multiple metadata factories
            // if two threads happen to hit this at the same time.  One will be GC'd
            if (metaDataFactory == null)
            {
                bool allowCache = false;
                metaDataFactory = CreateMetaDataFactory(internalConnection, out allowCache);
                if (allowCache)
                {
                    connectionPoolGroup.MetaDataFactory = metaDataFactory;
                }
            }
            return metaDataFactory;
        }

        protected virtual DbMetaDataFactory CreateMetaDataFactory(DbConnectionInternal internalConnection, out bool cacheMetaDataFactory)
        {
            // providers that support GetSchema must override this with a method that creates a meta data
            // factory appropriate for them.
            cacheMetaDataFactory = false;
            throw ADP.NotSupported();
        }

        protected abstract DbConnectionInternal CreateConnection(DbConnectionOptions options, DbConnectionPoolKey poolKey, object poolGroupProviderInfo, DbConnectionPool? pool, DbConnection? owningConnection);

        protected abstract DbConnectionOptions CreateConnectionOptions(string connectionString, DbConnectionOptions? previous);

        protected abstract DbConnectionPoolGroupOptions? CreateConnectionPoolGroupOptions(DbConnectionOptions options);

        internal abstract DbConnectionPoolGroup? GetConnectionPoolGroup(DbConnection connection);

        internal abstract DbConnectionInternal? GetInnerConnection(DbConnection connection);

        internal abstract void PermissionDemand(DbConnection outerConnection);

        internal abstract void SetConnectionPoolGroup(DbConnection outerConnection, DbConnectionPoolGroup poolGroup);

        internal abstract void SetInnerConnectionEvent(DbConnection owningObject, DbConnectionInternal to);

        internal abstract bool SetInnerConnectionFrom(DbConnection owningObject, DbConnectionInternal to, DbConnectionInternal from);

        internal abstract void SetInnerConnectionTo(DbConnection owningObject, DbConnectionInternal to);
    }
}
