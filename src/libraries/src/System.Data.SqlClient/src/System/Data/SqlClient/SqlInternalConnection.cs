// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System.Data.Common;
using System.Data.ProviderBase;
using System.Diagnostics;


namespace System.Data.SqlClient
{
    abstract internal class SqlInternalConnection : DbConnectionInternal
    {
        private readonly SqlConnectionString _connectionOptions;

        // if connection is not open: null
        // if connection is open: currently active database
        internal string CurrentDatabase { get; set; }

        // if connection is not open yet, CurrentDataSource is null
        // if connection is open:
        // * for regular connections, it is set to Data Source value from connection string
        // * for connections with FailoverPartner, it is set to the FailoverPartner value from connection string if the connection was opened to it.
        internal string CurrentDataSource { get; set; }


        internal enum TransactionRequest
        {
            Begin,
            Commit,
            Rollback,
            IfRollback,
            Save
        };

        internal SqlInternalConnection(SqlConnectionString connectionOptions) : base()
        {
            Debug.Assert(null != connectionOptions, "null connectionOptions?");
            _connectionOptions = connectionOptions;
        }

        internal SqlConnection Connection
        {
            get
            {
                return (SqlConnection)Owner;
            }
        }

        internal SqlConnectionString ConnectionOptions
        {
            get
            {
                return _connectionOptions;
            }
        }

        abstract internal SqlInternalTransaction CurrentTransaction
        {
            get;
        }

        //  Get the internal transaction that should be hooked to a new outer transaction
        //  during a BeginTransaction API call.  In some cases (i.e. connection is going to 
        //  be reset), CurrentTransaction should not be hooked up this way.
        virtual internal SqlInternalTransaction AvailableInternalTransaction
        {
            get
            {
                return CurrentTransaction;
            }
        }


        internal bool HasLocalTransaction
        {
            get
            {
                SqlInternalTransaction currentTransaction = CurrentTransaction;
                bool result = (null != currentTransaction && currentTransaction.IsLocal);
                return result;
            }
        }

        internal bool HasLocalTransactionFromAPI
        {
            get
            {
                SqlInternalTransaction currentTransaction = CurrentTransaction;
                bool result = (null != currentTransaction && currentTransaction.HasParentTransaction);
                return result;
            }
        }


        abstract internal bool IsLockedForBulkCopy
        {
            get;
        }


        abstract internal bool IsKatmaiOrNewer
        {
            get;
        }


        override public DbTransaction BeginTransaction(IsolationLevel iso)
        {
            return BeginSqlTransaction(iso, null, false);
        }

        virtual internal SqlTransaction BeginSqlTransaction(IsolationLevel iso, string transactionName, bool shouldReconnect)
        {
            SqlStatistics statistics = null;
            try
            {
                statistics = SqlStatistics.StartTimer(Connection.Statistics);


                ValidateConnectionForExecute(null);

                if (HasLocalTransactionFromAPI)
                    throw ADP.ParallelTransactionsNotSupported(Connection);

                if (iso == IsolationLevel.Unspecified)
                {
                    iso = IsolationLevel.ReadCommitted; // Default to ReadCommitted if unspecified.
                }

                SqlTransaction transaction = new SqlTransaction(this, Connection, iso, AvailableInternalTransaction);
                transaction.InternalTransaction.RestoreBrokenConnection = shouldReconnect;
                ExecuteTransaction(TransactionRequest.Begin, transactionName, iso, transaction.InternalTransaction);
                transaction.InternalTransaction.RestoreBrokenConnection = false;
                return transaction;
            }
            finally
            {
                SqlStatistics.StopTimer(statistics);
            }
        }

        override public void ChangeDatabase(string database)
        {
            if (string.IsNullOrEmpty(database))
            {
                throw ADP.EmptyDatabaseName();
            }

            ValidateConnectionForExecute(null);

            ChangeDatabaseInternal(database);  // do the real work...
        }

        abstract protected void ChangeDatabaseInternal(string database);


        override protected DbReferenceCollection CreateReferenceCollection()
        {
            return new SqlReferenceCollection();
        }

        override protected void Deactivate()
        {
            try
            {
                SqlReferenceCollection referenceCollection = (SqlReferenceCollection)ReferenceCollection;
                if (null != referenceCollection)
                {
                    referenceCollection.Deactivate();
                }

                // Invoke subclass-specific deactivation logic
                InternalDeactivate();
            }
            catch (Exception e)
            {
                if (!ADP.IsCatchableExceptionType(e))
                {
                    throw;
                }

                // if an exception occurred, the inner connection will be
                // marked as unusable and destroyed upon returning to the
                // pool
                DoomThisConnection();
            }
        }

        abstract internal void DisconnectTransaction(SqlInternalTransaction internalTransaction);

        override public void Dispose()
        {
            base.Dispose();
        }

        abstract internal void ExecuteTransaction(TransactionRequest transactionRequest, string name, IsolationLevel iso, SqlInternalTransaction internalTransaction);

        internal SqlDataReader FindLiveReader(SqlCommand command)
        {
            SqlDataReader reader = null;
            SqlReferenceCollection referenceCollection = (SqlReferenceCollection)ReferenceCollection;
            if (null != referenceCollection)
            {
                reader = referenceCollection.FindLiveReader(command);
            }
            return reader;
        }

        internal SqlCommand FindLiveCommand(TdsParserStateObject stateObj)
        {
            SqlCommand command = null;
            SqlReferenceCollection referenceCollection = (SqlReferenceCollection)ReferenceCollection;
            if (null != referenceCollection)
            {
                command = referenceCollection.FindLiveCommand(stateObj);
            }
            return command;
        }


        virtual protected void InternalDeactivate()
        {
        }

        // If wrapCloseInAction is defined, then the action it defines will be run with the connection close action passed in as a parameter
        // The close action also supports being run asynchronously
        internal void OnError(SqlException exception, bool breakConnection, Action<Action> wrapCloseInAction = null)
        {
            if (breakConnection)
            {
                DoomThisConnection();
            }

            var connection = Connection;
            if (null != connection)
            {
                connection.OnError(exception, breakConnection, wrapCloseInAction);
            }
            else if (exception.Class >= TdsEnums.MIN_ERROR_CLASS)
            {
                // It is an error, and should be thrown.  Class of TdsEnums.MIN_ERROR_CLASS
                // or above is an error, below TdsEnums.MIN_ERROR_CLASS denotes an info message.
                throw exception;
            }
        }


        abstract internal void ValidateConnectionForExecute(SqlCommand command);
    }
}
