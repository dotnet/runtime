// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Threading.Tasks;
using System.Threading;

namespace System.Data.Common
{
    public abstract class DbCommand : Component, IDbCommand
    {
        protected DbCommand() : base()
        {
        }

        [DefaultValue("")]
        [RefreshProperties(RefreshProperties.All)]
        public abstract string CommandText { get; set; }

        public abstract int CommandTimeout { get; set; }

        [DefaultValue(System.Data.CommandType.Text)]
        [RefreshProperties(RefreshProperties.All)]
        public abstract CommandType CommandType { get; set; }

        [Browsable(false)]
        [DefaultValue(null)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DbConnection Connection
        {
            get { return DbConnection; }
            set { DbConnection = value; }
        }

        IDbConnection IDbCommand.Connection
        {
            get { return DbConnection; }
            set { DbConnection = (DbConnection)value; }
        }

        protected abstract DbConnection DbConnection { get; set; }

        protected abstract DbParameterCollection DbParameterCollection { get; }

        protected abstract DbTransaction DbTransaction { get; set; }

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
        public DbTransaction Transaction
        {
            get { return DbTransaction; }
            set { DbTransaction = value; }
        }

        IDbTransaction IDbCommand.Transaction
        {
            get { return DbTransaction; }
            set { DbTransaction = (DbTransaction)value; }
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

        public Task<int> ExecuteNonQueryAsync() => ExecuteNonQueryAsync(CancellationToken.None);

        public virtual Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ADP.CreatedTaskWithCancellation<int>();
            }
            else
            {
                CancellationTokenRegistration registration = new CancellationTokenRegistration();
                if (cancellationToken.CanBeCanceled)
                {
                    registration = cancellationToken.Register(s => ((DbCommand)s).CancelIgnoreFailure(), this);
                }

                try
                {
                    return Task.FromResult(ExecuteNonQuery());
                }
                catch (Exception e)
                {
                    return Task.FromException<int>(e);
                }
                finally
                {
                    registration.Dispose();
                }
            }
        }

        public Task<DbDataReader> ExecuteReaderAsync() =>
            ExecuteReaderAsync(CommandBehavior.Default, CancellationToken.None);

        public Task<DbDataReader> ExecuteReaderAsync(CancellationToken cancellationToken) =>
            ExecuteReaderAsync(CommandBehavior.Default, cancellationToken);

        public Task<DbDataReader> ExecuteReaderAsync(CommandBehavior behavior) =>
            ExecuteReaderAsync(behavior, CancellationToken.None);

        public Task<DbDataReader> ExecuteReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken) =>
            ExecuteDbDataReaderAsync(behavior, cancellationToken);

        protected virtual Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ADP.CreatedTaskWithCancellation<DbDataReader>();
            }
            else
            {
                CancellationTokenRegistration registration = new CancellationTokenRegistration();
                if (cancellationToken.CanBeCanceled)
                {
                    registration = cancellationToken.Register(s => ((DbCommand)s).CancelIgnoreFailure(), this);
                }

                try
                {
                    return Task.FromResult<DbDataReader>(ExecuteReader(behavior));
                }
                catch (Exception e)
                {
                    return Task.FromException<DbDataReader>(e);
                }
                finally
                {
                    registration.Dispose();
                }
            }
        }

        public Task<object> ExecuteScalarAsync() =>
            ExecuteScalarAsync(CancellationToken.None);

        public virtual Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ADP.CreatedTaskWithCancellation<object>();
            }
            else
            {
                CancellationTokenRegistration registration = new CancellationTokenRegistration();
                if (cancellationToken.CanBeCanceled)
                {
                    registration = cancellationToken.Register(s => ((DbCommand)s).CancelIgnoreFailure(), this);
                }

                try
                {
                    return Task.FromResult<object>(ExecuteScalar());
                }
                catch (Exception e)
                {
                    return Task.FromException<object>(e);
                }
                finally
                {
                    registration.Dispose();
                }
            }
        }

        public abstract object ExecuteScalar();

        public abstract void Prepare();
    }
}
