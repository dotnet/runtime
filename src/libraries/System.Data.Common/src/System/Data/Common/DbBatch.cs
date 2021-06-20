// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Data.Common
{
    public abstract class DbBatch : IDisposable, IAsyncDisposable
    {
        public DbBatchCommandCollection BatchCommands => DbBatchCommands;

        protected abstract DbBatchCommandCollection DbBatchCommands { get; }

        public abstract int Timeout { get; set; }

        public DbConnection? Connection { get; set; }

        protected abstract DbConnection? DbConnection { get; set; }

        public DbTransaction? Transaction { get; set; }

        protected abstract DbTransaction? DbTransaction { get; set; }

        public DbDataReader ExecuteReader()
            => ExecuteDbDataReader();

        protected abstract DbDataReader ExecuteDbDataReader();

        public Task<DbDataReader> ExecuteReaderAsync(CancellationToken cancellationToken = default)
            => ExecuteDbDataReaderAsync(cancellationToken);

        protected abstract Task<DbDataReader> ExecuteDbDataReaderAsync(CancellationToken cancellationToken);

        public abstract int ExecuteNonQuery();

        public abstract Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken = default);

        public abstract object? ExecuteScalar();

        public abstract Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken = default);

        public abstract void Prepare();

        public abstract Task PrepareAsync(CancellationToken cancellationToken = default);

        public abstract void Cancel();

        public DbBatchCommand CreateBatchCommand() => CreateDbBatchCommand();

        protected virtual DbBatchCommand CreateDbBatchCommand() => throw new NotSupportedException();

        public virtual void Dispose() {}

        public virtual ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }
    }
}
