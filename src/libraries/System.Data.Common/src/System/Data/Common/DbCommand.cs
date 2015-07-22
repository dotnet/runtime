// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



//------------------------------------------------------------------------------

using System;
using System.Data;
using System.Threading.Tasks;
using System.Threading;

namespace System.Data.Common
{
    public abstract class DbCommand :
        IDisposable
    {
        protected DbCommand() : base()
        {
        }

        abstract public string CommandText
        {
            get;
            set;
        }

        abstract public int CommandTimeout
        {
            get;
            set;
        }

        abstract public CommandType CommandType
        {
            get;
            set;
        }

        public DbConnection Connection
        {
            get
            {
                return DbConnection;
            }
            set
            {
                DbConnection = value;
            }
        }


        abstract protected DbConnection DbConnection
        {
            get;
            set;
        }

        abstract protected DbParameterCollection DbParameterCollection
        {
            get;
        }

        abstract protected DbTransaction DbTransaction
        {
            get;
            set;
        }

        public abstract bool DesignTimeVisible
        {
            get;
            set;
        }

        public DbParameterCollection Parameters
        {
            get
            {
                return DbParameterCollection;
            }
        }

        public DbTransaction Transaction
        {
            get
            {
                return DbTransaction;
            }
            set
            {
                DbTransaction = value;
            }
        }

        abstract public UpdateRowSource UpdatedRowSource
        {
            get;
            set;
        }

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

        abstract public void Cancel();

        public DbParameter CreateParameter()
        {
            return CreateDbParameter();
        }


        abstract protected DbParameter CreateDbParameter();

        abstract protected DbDataReader ExecuteDbDataReader(CommandBehavior behavior);

        abstract public int ExecuteNonQuery();

        public DbDataReader ExecuteReader()
        {
            return (DbDataReader)ExecuteDbDataReader(CommandBehavior.Default);
        }


        public DbDataReader ExecuteReader(CommandBehavior behavior)
        {
            return (DbDataReader)ExecuteDbDataReader(behavior);
        }


        public Task<int> ExecuteNonQueryAsync()
        {
            return ExecuteNonQueryAsync(CancellationToken.None);
        }

        public virtual Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
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
                    return Task.FromResult<int>(ExecuteNonQuery());
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

        public Task<DbDataReader> ExecuteReaderAsync()
        {
            return ExecuteReaderAsync(CommandBehavior.Default, CancellationToken.None);
        }

        public Task<DbDataReader> ExecuteReaderAsync(CancellationToken cancellationToken)
        {
            return ExecuteReaderAsync(CommandBehavior.Default, cancellationToken);
        }

        public Task<DbDataReader> ExecuteReaderAsync(CommandBehavior behavior)
        {
            return ExecuteReaderAsync(behavior, CancellationToken.None);
        }

        public Task<DbDataReader> ExecuteReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        {
            return ExecuteDbDataReaderAsync(behavior, cancellationToken);
        }

        protected virtual Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<DbDataReader>(cancellationToken);
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

        public Task<object> ExecuteScalarAsync()
        {
            return ExecuteScalarAsync(CancellationToken.None);
        }

        public virtual Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<object>(cancellationToken);
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

        abstract public object ExecuteScalar();

        abstract public void Prepare();
        public void Dispose()
        {
            Dispose(disposing: true);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
