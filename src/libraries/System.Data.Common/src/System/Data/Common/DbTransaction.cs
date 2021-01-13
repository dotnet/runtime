// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Data.Common
{
    public abstract class DbTransaction : MarshalByRefObject, IDbTransaction, IAsyncDisposable
    {
        protected DbTransaction() : base() { }

        public DbConnection? Connection => DbConnection;

        IDbConnection? IDbTransaction.Connection => DbConnection;

        protected abstract DbConnection? DbConnection { get; }

        public abstract IsolationLevel IsolationLevel { get; }

        public abstract void Commit();

        public virtual Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            try
            {
                Commit();
                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                return Task.FromException(e);
            }
        }

        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing) { }

        public virtual ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }

        public abstract void Rollback();

        public virtual Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            try
            {
                Rollback();
                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                return Task.FromException(e);
            }
        }

        #region Savepoints

        /// <summary>
        /// Gets a value that indicates whether this <see cref="DbTransaction" /> instance supports database savepoints.
        /// If <see langword="false" />, the methods <see cref="SaveAsync" />,
        /// <see cref="RollbackAsync(string, System.Threading.CancellationToken)"/> and <see cref="ReleaseAsync" /> as
        /// well as their synchronous counterparts are expected to throw <see cref="NotSupportedException" />.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if this <see cref="DbTransaction"/> instance supports database savepoints; otherwise,
        /// <see langword="false" />.
        /// </returns>
        public virtual bool SupportsSavepoints => false;

        /// <summary>
        /// Creates a savepoint in the transaction. This allows all commands that are executed after the savepoint was
        /// established to be rolled back, restoring the transaction state to what it was at the time of the savepoint.
        /// </summary>
        /// <param name="savepointName">The name of the savepoint to be created.</param>
        /// <param name="cancellationToken">
        /// An optional token to cancel the asynchronous operation. The default value is <see cref="CancellationToken.None" />.
        /// </param>
        /// <returns>A <see cref="Task " /> representing the asynchronous operation.</returns>
        public virtual Task SaveAsync(string savepointName, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            try
            {
                Save(savepointName);
                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                return Task.FromException(e);
            }
        }

        /// <summary>
        /// Rolls back all commands that were executed after the specified savepoint was established.
        /// </summary>
        /// <param name="savepointName">The name of the savepoint to roll back to.</param>
        /// <param name="cancellationToken">
        /// An optional token to cancel the asynchronous operation. The default value is <see cref="CancellationToken.None" />.
        /// </param>
        /// <returns>A <see cref="Task " /> representing the asynchronous operation.</returns>
        public virtual Task RollbackAsync(string savepointName, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            try
            {
                Rollback(savepointName);
                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                return Task.FromException(e);
            }
        }

        /// <summary>
        /// Destroys a savepoint previously defined in the current transaction. This allows the system to
        /// reclaim some resources before the transaction ends.
        /// </summary>
        /// <param name="savepointName">The name of the savepoint to release.</param>
        /// <param name="cancellationToken">
        /// An optional token to cancel the asynchronous operation. The default value is <see cref="CancellationToken.None" />.
        /// </param>
        /// <returns>A <see cref="Task " /> representing the asynchronous operation.</returns>
        public virtual Task ReleaseAsync(string savepointName, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            try
            {
                Release(savepointName);
                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                return Task.FromException(e);
            }
        }

        /// <summary>
        /// Creates a savepoint in the transaction. This allows all commands that are executed after the savepoint was
        /// established to be rolled back, restoring the transaction state to what it was at the time of the savepoint.
        /// </summary>
        /// <param name="savepointName">The name of the savepoint to be created.</param>
        public virtual void Save(string savepointName) => throw new NotSupportedException();

        /// <summary>
        /// Rolls back all commands that were executed after the specified savepoint was established.
        /// </summary>
        /// <param name="savepointName">The name of the savepoint to roll back to.</param>
        public virtual void Rollback(string savepointName) => throw new NotSupportedException();

        /// <summary>
        /// Destroys a savepoint previously defined in the current transaction. This allows the system to
        /// reclaim some resources before the transaction ends.
        /// </summary>
        /// <param name="savepointName">The name of the savepoint to release.</param>
        public virtual void Release(string savepointName) {}

        #endregion
    }
}
