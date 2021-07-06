// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data.Common
{
    public abstract class DbConnection : Component, IDbConnection, IAsyncDisposable
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
        [AllowNull]
        public abstract string ConnectionString { get; set; }

        public virtual int ConnectionTimeout => ADP.DefaultConnectionTimeout;

        public abstract string Database { get; }

        public abstract string DataSource { get; }

        /// <summary>
        /// The associated provider factory for derived class.
        /// </summary>
        protected virtual DbProviderFactory? DbProviderFactory => null;

        internal DbProviderFactory? ProviderFactory => DbProviderFactory;

        [Browsable(false)]
        public abstract string ServerVersion { get; }

        [Browsable(false)]
        public abstract ConnectionState State { get; }

        public virtual event StateChangeEventHandler? StateChange;

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
                return ValueTask.FromCanceled<DbTransaction>(cancellationToken);
            }

            try
            {
                return new ValueTask<DbTransaction>(BeginDbTransaction(isolationLevel));
            }
            catch (Exception e)
            {
                return ValueTask.FromException<DbTransaction>(e);
            }
        }

        public ValueTask<DbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => BeginDbTransactionAsync(IsolationLevel.Unspecified, cancellationToken);

        public ValueTask<DbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
            => BeginDbTransactionAsync(isolationLevel, cancellationToken);

        public abstract void Close();

        public virtual Task CloseAsync()
        {
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

        public virtual bool CanCreateBatch => false;

        public DbBatch CreateBatch() => CreateDbBatch();

        protected virtual DbBatch CreateDbBatch() => throw new NotSupportedException();

        public DbCommand CreateCommand() => CreateDbCommand();

        IDbCommand IDbConnection.CreateCommand() => CreateDbCommand();

        protected abstract DbCommand CreateDbCommand();

        public virtual void EnlistTransaction(System.Transactions.Transaction? transaction)
        {
            throw ADP.NotSupported();
        }

        // these need to be here so that GetSchema is visible when programming to a dbConnection object.
        // they are overridden by the real implementations in DbConnectionBase

        /// <summary>
        /// Returns schema information for the data source of this <see cref="DbConnection" />.
        /// </summary>
        /// <returns>A <see cref="DataTable" /> that contains schema information.</returns>
        /// <remarks>
        /// If the connection is associated with a transaction, executing <see cref="GetSchema()" /> calls may cause
        /// some providers to throw an exception.
        /// </remarks>
        public virtual DataTable GetSchema()
        {
            throw ADP.NotSupported();
        }

        /// <summary>
        /// Returns schema information for the data source of this <see cref="DbConnection" /> using the specified
        /// string for the schema name.
        /// </summary>
        /// <param name="collectionName">Specifies the name of the schema to return.</param>
        /// <returns>A <see cref="DataTable" /> that contains schema information.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="collectionName" /> is specified as <see langword="null" />.
        /// </exception>
        /// <remarks>
        /// If the connection is associated with a transaction, executing <see cref="GetSchema(string)" /> calls may cause
        /// some providers to throw an exception.
        /// </remarks>
        public virtual DataTable GetSchema(string collectionName)
        {
            throw ADP.NotSupported();
        }

        /// <summary>
        /// Returns schema information for the data source of this <see cref="DbConnection" /> using the specified
        /// string for the schema name and the specified string array for the restriction values.
        /// </summary>
        /// <param name="collectionName">Specifies the name of the schema to return.</param>
        /// <param name="restrictionValues">Specifies a set of restriction values for the requested schema.</param>
        /// <returns>A <see cref="DataTable" /> that contains schema information.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="collectionName" /> is specified as <see langword="null" />.
        /// </exception>
        /// <remarks>
        /// <para>
        /// The <paramref name="restrictionValues" /> parameter can supply n depth of values, which are specified by the
        /// restrictions collection for a specific collection. In order to set values on a given restriction, and not
        /// set the values of other restrictions, you need to set the preceding restrictions to null and then put the
        /// appropriate value in for the restriction that you would like to specify a value for.
        /// </para>
        /// <para>
        /// An example of this is the "Tables" collection. If the "Tables" collection has three restrictions (database,
        /// owner, and table name) and you want to get back only the tables associated with the owner "Carl", you must
        /// pass in the following values at least: null, "Carl". If a restriction value is not passed in, the default
        /// values are used for that restriction. This is the same mapping as passing in null, which is different from
        /// passing in an empty string for the parameter value. In that case, the empty string ("") is considered to be
        /// the value for the specified parameter.
        /// </para>
        /// <para>
        /// If the connection is associated with a transaction, executing <see cref="GetSchema(string, string[])" />
        /// calls may cause some providers to throw an exception.
        /// </para>
        /// </remarks>
        public virtual DataTable GetSchema(string collectionName, string?[] restrictionValues)
        {
            throw ADP.NotSupported();
        }

        /// <summary>
        /// This is the asynchronous version of <see cref="GetSchema()" />.
        /// Providers should override with an appropriate implementation.
        /// The cancellation token can optionally be honored.
        /// The default implementation invokes the synchronous <see cref="GetSchema()" /> call and returns a completed
        /// task.
        /// The default implementation will return a cancelled task if passed an already cancelled cancellationToken.
        /// Exceptions thrown by <see cref="GetSchema()" /> will be communicated via the returned Task Exception
        /// property.
        /// </summary>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public virtual Task<DataTable> GetSchemaAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<DataTable>(cancellationToken);
            }

            try
            {
                return Task.FromResult(GetSchema());
            }
            catch (Exception e)
            {
                return Task.FromException<DataTable>(e);
            }
        }

        /// <summary>
        /// This is the asynchronous version of <see cref="GetSchema(string)" />.
        /// Providers should override with an appropriate implementation.
        /// The cancellation token can optionally be honored.
        /// The default implementation invokes the synchronous <see cref="GetSchema(string)" /> call and returns a
        /// completed task.
        /// The default implementation will return a cancelled task if passed an already cancelled cancellationToken.
        /// Exceptions thrown by <see cref="GetSchema(string)" /> will be communicated via the returned Task Exception
        /// property.
        /// </summary>
        /// <param name="collectionName">Specifies the name of the schema to return.</param>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public virtual Task<DataTable> GetSchemaAsync(
            string collectionName,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<DataTable>(cancellationToken);
            }

            try
            {
                return Task.FromResult(GetSchema(collectionName));
            }
            catch (Exception e)
            {
                return Task.FromException<DataTable>(e);
            }
        }

        /// <summary>
        /// This is the asynchronous version of <see cref="GetSchema(string, string[])" />.
        /// Providers should override with an appropriate implementation.
        /// The cancellation token can optionally be honored.
        /// The default implementation invokes the synchronous <see cref="GetSchema(string, string[])" /> call and
        /// returns a completed task.
        /// The default implementation will return a cancelled task if passed an already cancelled cancellationToken.
        /// Exceptions thrown by <see cref="GetSchema(string, string[])" /> will be communicated via the returned Task
        /// Exception property.
        /// </summary>
        /// <param name="collectionName">Specifies the name of the schema to return.</param>
        /// <param name="restrictionValues">Specifies a set of restriction values for the requested schema.</param>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public virtual Task<DataTable> GetSchemaAsync(string collectionName, string?[] restrictionValues,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<DataTable>(cancellationToken);
            }

            try
            {
                return Task.FromResult(GetSchema(collectionName, restrictionValues));
            }
            catch (Exception e)
            {
                return Task.FromException<DataTable>(e);
            }
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
