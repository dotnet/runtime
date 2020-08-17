// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Data.ProviderBase;
using System.Diagnostics;
using System.Threading;

namespace System.Data.Odbc
{
    public sealed partial class OdbcConnection : DbConnection
    {
        private static readonly DbConnectionFactory s_connectionFactory = OdbcConnectionFactory.SingletonInstance;

        private DbConnectionOptions? _userConnectionOptions;
        private DbConnectionPoolGroup? _poolGroup;
        private DbConnectionInternal _innerConnection;
        private int _closeCount;


        public OdbcConnection() : base()
        {
            GC.SuppressFinalize(this);
            _innerConnection = DbConnectionClosedNeverOpened.SingletonInstance;
        }

        // Copy Constructor
        private void CopyFrom(OdbcConnection connection)
        { // V1.2.3300
            ADP.CheckArgumentNull(connection, nameof(connection));
            _userConnectionOptions = connection.UserConnectionOptions;
            _poolGroup = connection.PoolGroup;

            // SQLBU 432115
            //  Match the original connection's behavior for whether the connection was never opened,
            //  but ensure Clone is in the closed state.
            if (DbConnectionClosedNeverOpened.SingletonInstance == connection._innerConnection)
            {
                _innerConnection = DbConnectionClosedNeverOpened.SingletonInstance;
            }
            else
            {
                _innerConnection = DbConnectionClosedPreviouslyOpened.SingletonInstance;
            }
        }

        internal int CloseCount
        {
            get
            {
                return _closeCount;
            }
        }

        internal DbConnectionFactory ConnectionFactory
        {
            get
            {
                return s_connectionFactory;
            }
        }

        internal DbConnectionOptions? ConnectionOptions
        {
            get
            {
                System.Data.ProviderBase.DbConnectionPoolGroup? poolGroup = PoolGroup;
                return ((null != poolGroup) ? poolGroup.ConnectionOptions : null);
            }
        }

        private string ConnectionString_Get()
        {
            bool hidePassword = InnerConnection.ShouldHidePassword;
            DbConnectionOptions? connectionOptions = UserConnectionOptions;
            return ((null != connectionOptions) ? connectionOptions.UsersConnectionString(hidePassword) : "");
        }

        private void ConnectionString_Set(string? value)
        {
            DbConnectionPoolKey key = new DbConnectionPoolKey(value);

            ConnectionString_Set(key);
        }

        private void ConnectionString_Set(DbConnectionPoolKey key)
        {
            DbConnectionOptions? connectionOptions = null;
            System.Data.ProviderBase.DbConnectionPoolGroup? poolGroup = ConnectionFactory.GetConnectionPoolGroup(key, null, ref connectionOptions);
            DbConnectionInternal connectionInternal = InnerConnection;
            bool flag = connectionInternal.AllowSetConnectionString;
            if (flag)
            {
                flag = SetInnerConnectionFrom(DbConnectionClosedBusy.SingletonInstance, connectionInternal);
                if (flag)
                {
                    _userConnectionOptions = connectionOptions;
                    _poolGroup = poolGroup;
                    _innerConnection = DbConnectionClosedNeverOpened.SingletonInstance;
                }
            }
            if (!flag)
            {
                throw ADP.OpenConnectionPropertySet(nameof(ConnectionString), connectionInternal.State);
            }
        }

        internal DbConnectionInternal InnerConnection
        {
            get
            {
                return _innerConnection;
            }
        }

        internal System.Data.ProviderBase.DbConnectionPoolGroup? PoolGroup
        {
            get
            {
                return _poolGroup;
            }
            set
            {
                Debug.Assert(null != value, "null poolGroup");
                _poolGroup = value;
            }
        }


        internal DbConnectionOptions? UserConnectionOptions
        {
            get
            {
                return _userConnectionOptions;
            }
        }

        internal void Abort(Exception e)
        {
            DbConnectionInternal innerConnection = _innerConnection;
            if (ConnectionState.Open == innerConnection.State)
            {
                Interlocked.CompareExchange(ref _innerConnection, DbConnectionClosedPreviouslyOpened.SingletonInstance, innerConnection);
                innerConnection.DoomThisConnection();
            }
        }

        internal void AddWeakReference(object value, int tag)
        {
            InnerConnection.AddWeakReference(value, tag);
        }

        protected override DbCommand CreateDbCommand()
        {
            DbProviderFactory providerFactory = ConnectionFactory.ProviderFactory;
            DbCommand command = providerFactory.CreateCommand()!;
            command.Connection = this;
            return command;
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _userConnectionOptions = null;
                _poolGroup = null;
                Close();
            }
            DisposeMe(disposing);
            base.Dispose(disposing);
        }

        partial void RepairInnerConnection();

        public override DataTable GetSchema()
        {
            return this.GetSchema(DbMetaDataCollectionNames.MetaDataCollections, null);
        }

        public override DataTable GetSchema(string collectionName)
        {
            return this.GetSchema(collectionName, null);
        }

        public override DataTable GetSchema(string collectionName, string?[]? restrictionValues)
        {
            // NOTE: This is virtual because not all providers may choose to support
            //       returning schema data
            return InnerConnection.GetSchema(ConnectionFactory, PoolGroup!, this, collectionName, restrictionValues);
        }

        internal void NotifyWeakReference(int message)
        {
            InnerConnection.NotifyWeakReference(message);
        }

        internal void PermissionDemand()
        {
            Debug.Assert(DbConnectionClosedConnecting.SingletonInstance == _innerConnection, "not connecting");
            System.Data.ProviderBase.DbConnectionPoolGroup? poolGroup = PoolGroup;
            DbConnectionOptions? connectionOptions = ((null != poolGroup) ? poolGroup.ConnectionOptions : null);
            if ((null == connectionOptions) || connectionOptions.IsEmpty)
            {
                throw ADP.NoConnectionString();
            }
            DbConnectionOptions? userConnectionOptions = UserConnectionOptions;
            Debug.Assert(null != userConnectionOptions, "null UserConnectionOptions");
        }

        internal void RemoveWeakReference(object value)
        {
            InnerConnection.RemoveWeakReference(value);
        }

        internal void SetInnerConnectionEvent(DbConnectionInternal to)
        {
            Debug.Assert(null != _innerConnection, "null InnerConnection");
            Debug.Assert(null != to, "to null InnerConnection");

            ConnectionState originalState = _innerConnection.State & ConnectionState.Open;
            ConnectionState currentState = to.State & ConnectionState.Open;
            if ((originalState != currentState) && (ConnectionState.Closed == currentState))
            {
                unchecked { _closeCount++; }
            }

            _innerConnection = to;
            if (ConnectionState.Closed == originalState && ConnectionState.Open == currentState)
            {
                OnStateChange(DbConnectionInternal.StateChangeOpen);
            }
            else if (ConnectionState.Open == originalState && ConnectionState.Closed == currentState)
            {
                OnStateChange(DbConnectionInternal.StateChangeClosed);
            }
            else
            {
                Debug.Fail("unexpected state switch");
                if (originalState != currentState)
                {
                    OnStateChange(new StateChangeEventArgs(originalState, currentState));
                }
            }
        }

        internal bool SetInnerConnectionFrom(DbConnectionInternal to, DbConnectionInternal from)
        {
            Debug.Assert(null != _innerConnection, "null InnerConnection");
            Debug.Assert(null != from, "from null InnerConnection");
            Debug.Assert(null != to, "to null InnerConnection");
            bool result = (from == Interlocked.CompareExchange<DbConnectionInternal>(ref _innerConnection, to, from));
            return result;
        }

        internal void SetInnerConnectionTo(DbConnectionInternal to)
        {
            Debug.Assert(null != _innerConnection, "null InnerConnection");
            Debug.Assert(null != to, "to null InnerConnection");
            _innerConnection = to;
        }
    }
}
