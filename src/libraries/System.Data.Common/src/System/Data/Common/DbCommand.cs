// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data.Common
{
    public abstract class DbCommand : Component, IDbCommand, IAsyncDisposable
    {
        protected DbCommand() : base()
        {
        }

        [DefaultValue("")]
        [RefreshProperties(RefreshProperties.All)]
        [AllowNull]
        public abstract string CommandText { get; set; }

        public abstract int CommandTimeout { get; set; }

        [DefaultValue(System.Data.CommandType.Text)]
        [RefreshProperties(RefreshProperties.All)]
        public abstract CommandType CommandType { get; set; }

        [Browsable(false)]
        [DefaultValue(null)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DbConnection? Connection
        {
            get { return DbConnection; }
            set { DbConnection = value; }
        }

        IDbConnection? IDbCommand.Connection
        {
            get { return DbConnection; }
            set { DbConnection = (DbConnection?)value; }
        }

        protected abstract DbConnection? DbConnection { get; set; }

        protected abstract DbParameterCollection DbParameterCollection { get; }

        protected abstract DbTransaction? DbTransaction { get; set; }

        // By default, the cmd object is visible on the design surface (i.e. VS7 Server Tray)
        // to limit the number of components that clutter the design surface,
        // when the DataAdapter design wizard generates the insert/update/delete commands it will
        // set the DesignTimeVisible property to false so that cmds won't appear as individual objects
        [DefaultValue(true)]
        [DesignOnly(true)]
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public abstract bool DesignTimeVisible { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DbParameterCollection Parameters => DbParameterCollection;

        IDataParameterCollection IDbCommand.Parameters => DbParameterCollection;

        [Browsable(false)]
        [DefaultValue(null)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DbTransaction? Transaction
        {
            get { return DbTransaction; }
            set { DbTransaction = value; }
        }

        IDbTransaction? IDbCommand.Transaction
        {
            get { return DbTransaction; }
            set { DbTransaction = (DbTransaction?)value; }
        }

        [DefaultValue(System.Data.UpdateRowSource.Both)]
        public abstract UpdateRowSource UpdatedRowSource { get; set; }

        internal void CancelIgnoreFailure()
        {
            // This method is used to route CancellationTokens to the Cancel method.
            // Cancellation is a suggestion, and exceptions should be ignored
            // rather than allowed to be unhandled, as the exceptions cannot be
            // routed to the caller. These errors will be observed in the regular
            // method instead.
            try
            {
                Cancel();
            }
            catch (Exception)
            {
            }
        }

        public abstract void Cancel();

        public DbParameter CreateParameter() => CreateDbParameter();

        IDbDataParameter IDbCommand.CreateParameter() => CreateDbParameter();

        protected abstract DbParameter CreateDbParameter();

        protected abstract DbDataReader ExecuteDbDataReader(CommandBehavior behavior);

        public abstract int ExecuteNonQuery();

        public DbDataReader ExecuteReader() => ExecuteDbDataReader(CommandBehavior.Default);

        IDataReader IDbCommand.ExecuteReader() => ExecuteDbDataReader(CommandBehavior.Default);

        public DbDataReader ExecuteReader(CommandBehavior behavior) => ExecuteDbDataReader(behavior);

        IDataReader IDbCommand.ExecuteReader(CommandBehavior behavior) => ExecuteDbDataReader(behavior);

        public async Task<int> ExecuteNonQueryAsync() => await ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);

        public virtual async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return await ADP.CreatedTaskWithCancellation<int>().ConfigureAwait(false);
            }
            else
            {
                CancellationTokenRegistration registration = default;
                if (cancellationToken.CanBeCanceled)
                {
                    registration = cancellationToken.Register(s => ((DbCommand)s!).CancelIgnoreFailure(), this);
                }

                try
                {
                    return ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    return await Task.FromException<int>(e).ConfigureAwait(false);
                }
                finally
                {
                    registration.Dispose();
                }
            }
        }

        public async Task<DbDataReader> ExecuteReaderAsync() =>
            await ExecuteReaderAsync(CommandBehavior.Default, CancellationToken.None).ConfigureAwait(false);

        public async Task<DbDataReader> ExecuteReaderAsync(CancellationToken cancellationToken) =>
            await ExecuteReaderAsync(CommandBehavior.Default, cancellationToken).ConfigureAwait(false);

        public async Task<DbDataReader> ExecuteReaderAsync(CommandBehavior behavior) =>
            await ExecuteReaderAsync(behavior, CancellationToken.None).ConfigureAwait(false);

        public async Task<DbDataReader> ExecuteReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken) =>
            await ExecuteDbDataReaderAsync(behavior, cancellationToken).ConfigureAwait(false);

        protected virtual async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return await ADP.CreatedTaskWithCancellation<DbDataReader>().ConfigureAwait(false);
            }
            else
            {
                CancellationTokenRegistration registration = default;
                if (cancellationToken.CanBeCanceled)
                {
                    registration = cancellationToken.Register(s => ((DbCommand)s!).CancelIgnoreFailure(), this);
                }

                try
                {
                    return ExecuteReader(behavior);
                }
                catch (Exception e)
                {
                    return await Task.FromException<DbDataReader>(e).ConfigureAwait(false);
                }
                finally
                {
                    registration.Dispose();
                }
            }
        }

        public async Task<object?> ExecuteScalarAsync() =>
            await ExecuteScalarAsync(CancellationToken.None).ConfigureAwait(false);

        public virtual async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return await ADP.CreatedTaskWithCancellation<object?>().ConfigureAwait(false);
            }
            else
            {
                CancellationTokenRegistration registration = default;
                if (cancellationToken.CanBeCanceled)
                {
                    registration = cancellationToken.Register(s => ((DbCommand)s!).CancelIgnoreFailure(), this);
                }

                try
                {
                    return ExecuteScalar();
                }
                catch (Exception e)
                {
                    return await Task.FromException<object?>(e).ConfigureAwait(false);
                }
                finally
                {
                    registration.Dispose();
                }
            }
        }

        public abstract object? ExecuteScalar();

        public abstract void Prepare();

        public virtual Task PrepareAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            try
            {
                Prepare();
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
    }
}
