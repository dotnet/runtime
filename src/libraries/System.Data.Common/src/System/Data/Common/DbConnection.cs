// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data.Common
{
    public abstract class DbConnection : Component, IDbConnection
    {
#pragma warning disable 649 // ignore unassigned field warning
        internal bool _suppressStateChangeForReconnection;
#pragma warning restore 649

        protected DbConnection() : base()
        {
        }

        [DefaultValue("")]
        [SettingsBindableAttribute(true)]
        [RefreshProperties(RefreshProperties.All)]
#pragma warning disable 618 // ignore obsolete warning about RecommendedAsConfigurable to use SettingsBindableAttribute
        [RecommendedAsConfigurable(true)]
#pragma warning restore 618
        public abstract string ConnectionString { get; set; }

        public virtual int ConnectionTimeout => ADP.DefaultConnectionTimeout;

        public abstract string Database { get; }

        public abstract string DataSource { get; }

        /// <summary>
        /// The associated provider factory for derived class.
        /// </summary>
        protected virtual DbProviderFactory DbProviderFactory => null;

        internal DbProviderFactory ProviderFactory => DbProviderFactory; 

        [Browsable(false)]
        public abstract string ServerVersion { get; }

        [Browsable(false)]
        public abstract ConnectionState State { get; }

        public virtual event StateChangeEventHandler StateChange;

        protected abstract DbTransaction BeginDbTransaction(IsolationLevel isolationLevel);

        public DbTransaction BeginTransaction() =>
            BeginDbTransaction(IsolationLevel.Unspecified);

        public DbTransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            return BeginDbTransaction(isolationLevel);
        }

        IDbTransaction IDbConnection.BeginTransaction() =>
            BeginDbTransaction(IsolationLevel.Unspecified);

        IDbTransaction IDbConnection.BeginTransaction(IsolationLevel isolationLevel) =>
            BeginDbTransaction(isolationLevel);

        protected virtual ValueTask<DbTransaction> BeginDbTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new ValueTask<DbTransaction>(Task.FromCanceled<DbTransaction>(cancellationToken));
            }

            try
            {
                return new ValueTask<DbTransaction>(BeginDbTransaction(isolationLevel));
            }
            catch (Exception e)
            {
                return new ValueTask<DbTransaction>(Task.FromException<DbTransaction>(e));
            }
        }

        public ValueTask<DbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => BeginDbTransactionAsync(IsolationLevel.Unspecified, cancellationToken);

        public ValueTask<DbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
            => BeginDbTransactionAsync(isolationLevel, cancellationToken);

        public abstract void Close();

        public virtual Task CloseAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            try
            {
                Close();
                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                return Task.FromException(e);
            }
        }

        public virtual ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }

        public abstract void ChangeDatabase(string databaseName);

        public virtual Task ChangeDatabaseAsync(string databaseName, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            try
            {
                ChangeDatabase(databaseName);
                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                return Task.FromException(e);
            }
        }

        public DbCommand CreateCommand() => CreateDbCommand();

        IDbCommand IDbConnection.CreateCommand() => CreateDbCommand();

        protected abstract DbCommand CreateDbCommand();

        public virtual void EnlistTransaction(System.Transactions.Transaction transaction)
        {
            throw ADP.NotSupported();
        }

        // these need to be here so that GetSchema is visible when programming to a dbConnection object.
        // they are overridden by the real implementations in DbConnectionBase
        public virtual DataTable GetSchema()
        {
            throw ADP.NotSupported();
        }

        public virtual DataTable GetSchema(string collectionName)
        {
            throw ADP.NotSupported();
        }

        public virtual DataTable GetSchema(string collectionName, string[] restrictionValues)
        {
            throw ADP.NotSupported();
        }
        
        protected virtual void OnStateChange(StateChangeEventArgs stateChange)
        {
            if (_suppressStateChangeForReconnection)
            {
                return;
            }

            StateChange?.Invoke(this, stateChange);
        }

        public abstract void Open();

        public Task OpenAsync() => OpenAsync(CancellationToken.None);

        public virtual Task OpenAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }
            else
            {
                try
                {
                    Open();
                    return Task.CompletedTask;
                }
                catch (Exception e)
                {
                    return Task.FromException(e);
                }
            }
        }
    }
}
