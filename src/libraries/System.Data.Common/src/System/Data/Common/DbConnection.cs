// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.




//------------------------------------------------------------------------------

using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data.Common
{
    public abstract class DbConnection : 
        IDbConnection,
        IDisposable
    {
        private StateChangeEventHandler _stateChangeEventHandler;

        protected DbConnection() : base()
        {
        }

        ~DbConnection()
        {
            Dispose(disposing: false);
        }

        abstract public string ConnectionString
        {
            get;
            set;
        }

        virtual public int ConnectionTimeout
        {
            get
            {
                return ADP.DefaultConnectionTimeout;
            }
        }

        abstract public string Database
        {
            get;
        }

        abstract public string DataSource
        {
            // Implementation Note: A ChangeDataSource method should be implemented, 
            // if a plan is to allow the data source to be changed.
            get;
        }

        abstract public string ServerVersion
        {
            get;
        }

        abstract public ConnectionState State
        {
            get;
        }

        virtual public event StateChangeEventHandler StateChange
        {
            add
            {
                _stateChangeEventHandler += value;
            }
            remove
            {
                _stateChangeEventHandler -= value;
            }
        }

        abstract protected DbTransaction BeginDbTransaction(IsolationLevel isolationLevel);

        public DbTransaction BeginTransaction()
        {
            return BeginDbTransaction(IsolationLevel.Unspecified);
        }

        public DbTransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            return BeginDbTransaction(isolationLevel);
        }


        abstract public void Close();

        abstract public void ChangeDatabase(string databaseName);

        public DbCommand CreateCommand()
        {
            return CreateDbCommand();
        }


        abstract protected DbCommand CreateDbCommand();


        protected virtual void OnStateChange(StateChangeEventArgs stateChange)
        {
            StateChangeEventHandler handler = _stateChangeEventHandler;
            if (null != handler)
            {
                handler(this, stateChange);
            }
        }


        abstract public void Open();

        public Task OpenAsync()
        {
            return OpenAsync(CancellationToken.None);
        }

        public virtual Task OpenAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return TaskHelpers.FromCancellation(cancellationToken);
            }
            else
            {
                try
                {
                    Open();
                    return TaskHelpers.CompletedTask();
                }
                catch (Exception e)
                {
                    return TaskHelpers.FromException(e);
                }
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Close();
            }
        }

        IDbTransaction IDbConnection.BeginTransaction()
        {
            return BeginDbTransaction(IsolationLevel.Unspecified);
        }

        IDbTransaction IDbConnection.BeginTransaction(IsolationLevel isolationLevel)
        {
            return BeginDbTransaction(isolationLevel);
        }

        IDbCommand IDbConnection.CreateCommand()
        {
            return CreateDbCommand();
        }
    }
}
