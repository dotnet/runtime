// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Threading;
using System.Transactions;

namespace System.Data.SqlClient
{
    sealed internal partial class SqlDelegatedTransaction : IPromotableSinglePhaseNotification
    {
        private static int _objectTypeCount;
        private readonly int _objectID = Interlocked.Increment(ref _objectTypeCount);
        private const int _globalTransactionsTokenVersionSizeInBytes = 4; // the size of the version in the PromotedDTCToken for Global Transactions
        internal int ObjectID
        {
            get
            {
                return _objectID;
            }
        }

        // WARNING!!! Multithreaded object!
        // Locking strategy: Any potentailly-multithreaded operation must first lock the associated connection, then
        //  validate this object's active state.  Locked activities should ONLY include Sql-transaction state altering activities
        //  or notifications of same. Updates to the connection's association with the transaction or to the connection pool
        //  may be initiated here AFTER the connection lock is released, but should NOT fall under this class's locking strategy.

        private SqlInternalConnection _connection;            // the internal connection that is the root of the transaction
        private IsolationLevel _isolationLevel;        // the IsolationLevel of the transaction we delegated to the server
        private SqlInternalTransaction _internalTransaction;   // the SQL Server transaction we're delegating to

        private Transaction _atomicTransaction;

        private bool _active;                // Is the transaction active?

        internal SqlDelegatedTransaction(SqlInternalConnection connection, Transaction tx)
        {
            Debug.Assert(null != connection, "null connection?");
            _connection = connection;
            _atomicTransaction = tx;
            _active = false;
            Transactions.IsolationLevel systxIsolationLevel = (Transactions.IsolationLevel)tx.IsolationLevel;

            // We need to map the System.Transactions IsolationLevel to the one
            // that System.Data uses and communicates to SqlServer.  We could
            // arguably do that in Initialize when the transaction is delegated,
            // however it is better to do this before we actually begin the process
            // of delegation, in case System.Transactions adds another isolation
            // level we don't know about -- we can throw the exception at a better
            // place.
            switch (systxIsolationLevel)
            {
                case Transactions.IsolationLevel.ReadCommitted:
                    _isolationLevel = IsolationLevel.ReadCommitted;
                    break;
                case Transactions.IsolationLevel.ReadUncommitted:
                    _isolationLevel = IsolationLevel.ReadUncommitted;
                    break;
                case Transactions.IsolationLevel.RepeatableRead:
                    _isolationLevel = IsolationLevel.RepeatableRead;
                    break;
                case Transactions.IsolationLevel.Serializable:
                    _isolationLevel = IsolationLevel.Serializable;
                    break;
                case Transactions.IsolationLevel.Snapshot:
                    _isolationLevel = IsolationLevel.Snapshot;
                    break;
                default:
                    throw SQL.UnknownSysTxIsolationLevel(systxIsolationLevel);
            }
        }

        internal Transaction Transaction
        {
            get { return _atomicTransaction; }
        }

        public void Initialize()
        {
            // if we get here, then we know for certain that we're the delegated
            // transaction.
            SqlInternalConnection connection = _connection;
            SqlConnection usersConnection = connection.Connection;

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                if (connection.IsEnlistedInTransaction)
                { // defect first
                    connection.EnlistNull();
                }

                _internalTransaction = new SqlInternalTransaction(connection, TransactionType.Delegated, null);

                connection.ExecuteTransaction(SqlInternalConnection.TransactionRequest.Begin, null, _isolationLevel, _internalTransaction, true);

                // Handle case where ExecuteTran didn't produce a new transaction, but also didn't throw.
                if (null == connection.CurrentTransaction)
                {
                    connection.DoomThisConnection();
                    throw ADP.InternalError(ADP.InternalErrorCode.UnknownTransactionFailure);
                }

                _active = true;
            }
            catch (System.OutOfMemoryException e)
            {
                usersConnection.Abort(e);
                throw;
            }
            catch (System.StackOverflowException e)
            {
                usersConnection.Abort(e);
                throw;
            }
            catch (System.Threading.ThreadAbortException e)
            {
                usersConnection.Abort(e);
                throw;
            }
        }

        internal bool IsActive
        {
            get
            {
                return _active;
            }
        }

        public byte[] Promote()
        {
            // Operations that might be affected by multi-threaded use MUST be done inside the lock.
            //  Don't read values off of the connection outside the lock unless it doesn't really matter
            //  from an operational standpoint (i.e. logging connection's ObjectID should be fine,
            //  but the PromotedDTCToken can change over calls. so that must be protected).
            SqlInternalConnection connection = GetValidConnection();

            Exception promoteException;
            byte[] returnValue = null;
            SqlConnection usersConnection = connection.Connection;

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                lock (connection)
                {
                    try
                    {
                        // Now that we've acquired the lock, make sure we still have valid state for this operation.
                        ValidateActiveOnConnection(connection);

                        connection.ExecuteTransaction(SqlInternalConnection.TransactionRequest.Promote, null, IsolationLevel.Unspecified, _internalTransaction, true);
                        returnValue = _connection.PromotedDTCToken;

                        // For Global Transactions, we need to set the Transaction Id since we use a Non-MSDTC Promoter type.
                        if (_connection.IsGlobalTransaction)
                        {
                            if (SysTxForGlobalTransactions.SetDistributedTransactionIdentifier == null)
                            {
                                throw SQL.UnsupportedSysTxForGlobalTransactions();
                            }

                            if (!_connection.IsGlobalTransactionsEnabledForServer)
                            {
                                throw SQL.GlobalTransactionsNotEnabled();
                            }

                            SysTxForGlobalTransactions.SetDistributedTransactionIdentifier.Invoke(_atomicTransaction, new object[] { this, GetGlobalTxnIdentifierFromToken() });
                        }

                        promoteException = null;
                    }
                    catch (SqlException e)
                    {
                        promoteException = e;

                        // Doom the connection, to make sure that the transaction is
                        // eventually rolled back.
                        // VSTS 144562: doom the connection while having the lock on it to prevent race condition with "Transaction Ended" Event
                        connection.DoomThisConnection();
                    }
                    catch (InvalidOperationException e)
                    {
                        promoteException = e;
                        connection.DoomThisConnection();
                    }
                }
            }
            catch (System.OutOfMemoryException e)
            {
                usersConnection.Abort(e);
                throw;
            }
            catch (System.StackOverflowException e)
            {
                usersConnection.Abort(e);
                throw;
            }
            catch (System.Threading.ThreadAbortException e)
            {
                usersConnection.Abort(e);
                throw;
            }

            if (promoteException != null)
            {
                throw SQL.PromotionFailed(promoteException);
            }

            return returnValue;
        }

        // Called by transaction to initiate abort sequence
        public void Rollback(SinglePhaseEnlistment enlistment)
        {
            Debug.Assert(null != enlistment, "null enlistment?");

            SqlInternalConnection connection = GetValidConnection();
            SqlConnection usersConnection = connection.Connection;

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                lock (connection)
                {
                    try
                    {
                        // Now that we've acquired the lock, make sure we still have valid state for this operation.
                        ValidateActiveOnConnection(connection);
                        _active = false; // set to inactive first, doesn't matter how the execute completes, this transaction is done.
                        _connection = null;  // Set prior to ExecuteTransaction call in case this initiates a TransactionEnd event

                        // If we haven't already rolled back (or aborted) then tell the SQL Server to roll back
                        if (!_internalTransaction.IsAborted)
                        {
                            connection.ExecuteTransaction(SqlInternalConnection.TransactionRequest.Rollback, null, IsolationLevel.Unspecified, _internalTransaction, true);
                        }
                    }
                    catch (SqlException)
                    {
                        // Doom the connection, to make sure that the transaction is
                        // eventually rolled back.
                        // VSTS 144562: doom the connection while having the lock on it to prevent race condition with "Transaction Ended" Event
                        connection.DoomThisConnection();

                        // Unlike SinglePhaseCommit, a rollback is a rollback, regardless 
                        // of how it happens, so SysTx won't throw an exception, and we
                        // don't want to throw an exception either, because SysTx isn't 
                        // handling it and it may create a fail fast scenario. In the end,
                        // there is no way for us to communicate to the consumer that this
                        // failed for more serious reasons than usual.
                        // 
                        // This is a bit like "should you throw if Close fails", however,
                        // it only matters when you really need to know.  In that case, 
                        // we have the tracing that we're doing to fallback on for the
                        // investigation.
                    }
                    catch (InvalidOperationException)
                    {
                        connection.DoomThisConnection();
                    }
                }

                // it doesn't matter whether the rollback succeeded or not, we presume
                // that the transaction is aborted, because it will be eventually.
                connection.CleanupConnectionOnTransactionCompletion(_atomicTransaction);
                enlistment.Aborted();
            }
            catch (System.OutOfMemoryException e)
            {
                usersConnection.Abort(e);
                throw;
            }
            catch (System.StackOverflowException e)
            {
                usersConnection.Abort(e);
                throw;
            }
            catch (System.Threading.ThreadAbortException e)
            {
                usersConnection.Abort(e);
                throw;
            }
        }

        // Called by the transaction to initiate commit sequence
        public void SinglePhaseCommit(SinglePhaseEnlistment enlistment)
        {
            Debug.Assert(null != enlistment, "null enlistment?");

            SqlInternalConnection connection = GetValidConnection();
            SqlConnection usersConnection = connection.Connection;

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                // If the connection is dooomed, we can be certain that the
                // transaction will eventually be rolled back, and we shouldn't
                // attempt to commit it.
                if (connection.IsConnectionDoomed)
                {
                    lock (connection)
                    {
                        _active = false; // set to inactive first, doesn't matter how the rest completes, this transaction is done.
                        _connection = null;
                    }

                    enlistment.Aborted(SQL.ConnectionDoomed());
                }
                else
                {
                    Exception commitException;
                    lock (connection)
                    {
                        try
                        {
                            // Now that we've acquired the lock, make sure we still have valid state for this operation.
                            ValidateActiveOnConnection(connection);

                            _active = false; // set to inactive first, doesn't matter how the rest completes, this transaction is done.
                            _connection = null;   // Set prior to ExecuteTransaction call in case this initiates a TransactionEnd event

                            connection.ExecuteTransaction(SqlInternalConnection.TransactionRequest.Commit, null, IsolationLevel.Unspecified, _internalTransaction, true);
                            commitException = null;
                        }
                        catch (SqlException e)
                        {
                            commitException = e;

                            // Doom the connection, to make sure that the transaction is
                            // eventually rolled back.
                            // VSTS 144562: doom the connection while having the lock on it to prevent race condition with "Transaction Ended" Event
                            connection.DoomThisConnection();
                        }
                        catch (InvalidOperationException e)
                        {
                            commitException = e;
                            connection.DoomThisConnection();
                        }
                    }
                    if (commitException != null)
                    {
                        // connection.ExecuteTransaction failed with exception
                        if (_internalTransaction.IsCommitted)
                        {
                            // Even though we got an exception, the transaction
                            // was committed by the server.
                            enlistment.Committed();
                        }
                        else if (_internalTransaction.IsAborted)
                        {
                            // The transaction was aborted, report that to
                            // SysTx.
                            enlistment.Aborted(commitException);
                        }
                        else
                        {
                            // The transaction is still active, we cannot
                            // know the state of the transaction.
                            enlistment.InDoubt(commitException);
                        }

                        // We eat the exception.  This is called on the SysTx
                        // thread, not the applications thread.  If we don't 
                        // eat the exception an UnhandledException will occur,
                        // causing the process to FailFast.
                    }

                    connection.CleanupConnectionOnTransactionCompletion(_atomicTransaction);
                    if (commitException == null)
                    {
                        // connection.ExecuteTransaction succeeded
                        enlistment.Committed();
                    }
                }
            }
            catch (System.OutOfMemoryException e)
            {
                usersConnection.Abort(e);
                throw;
            }
            catch (System.StackOverflowException e)
            {
                usersConnection.Abort(e);
                throw;
            }
            catch (System.Threading.ThreadAbortException e)
            {
                usersConnection.Abort(e);
                throw;
            }
        }

        // Event notification that transaction ended. This comes from the subscription to the Transaction's
        //  ended event via the internal connection. If it occurs without a prior Rollback or SinglePhaseCommit call,
        //  it indicates the transaction was ended externally (generally that one of the DTC participants aborted
        //  the transaction).
        internal void TransactionEnded(Transaction transaction)
        {
            SqlInternalConnection connection = _connection;

            if (connection != null)
            {
                lock (connection)
                {
                    if (_atomicTransaction.Equals(transaction))
                    {
                        // No need to validate active on connection, this operation can be called on completed transactions
                        _active = false;
                        _connection = null;
                    }
                }
            }
        }

        // Check for connection validity
        private SqlInternalConnection GetValidConnection()
        {
            SqlInternalConnection connection = _connection;
            if (null == connection)
            {
                throw ADP.ObjectDisposed(this);
            }

            return connection;
        }

        // Dooms connection and throws and error if not a valid, active, delegated transaction for the given
        //  connection. Designed to be called AFTER a lock is placed on the connection, otherwise a normal return
        //  may not be trusted.
        private void ValidateActiveOnConnection(SqlInternalConnection connection)
        {
            bool valid = _active && (connection == _connection) && (connection.DelegatedTransaction == this);

            if (!valid)
            {
                // Invalid indicates something BAAAD happened (Commit after TransactionEnded, for instance)
                //  Doom anything remotely involved.
                if (null != connection)
                {
                    connection.DoomThisConnection();
                }
                if (connection != _connection && null != _connection)
                {
                    _connection.DoomThisConnection();
                }

                throw ADP.InternalError(ADP.InternalErrorCode.UnpooledObjectHasWrongOwner);  //TODO: Create a new code
            }
        }
    }
}
