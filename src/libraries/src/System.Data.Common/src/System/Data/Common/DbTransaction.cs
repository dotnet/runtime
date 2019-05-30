// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace System.Data.Common
{
    public abstract class DbTransaction : MarshalByRefObject, IDbTransaction
    {
        protected DbTransaction() : base() { }

        public DbConnection Connection => DbConnection;

        IDbConnection IDbTransaction.Connection => DbConnection;

        protected abstract DbConnection DbConnection { get; }

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
    }
}
