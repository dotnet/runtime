// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System.Data.Common;
using System.Diagnostics;
using System.Threading;

namespace System.Data.SqlClient
{
    internal enum TransactionState
    {
        Pending = 0,
        Active = 1,
        Aborted = 2,
        Committed = 3,
        Unknown = 4,
    }

    internal enum TransactionType
    {
        LocalFromTSQL = 1,
        LocalFromAPI = 2,
        Delegated = 3,
        Distributed = 4,
        Context = 5,     // only valid in proc.
    };

    sealed internal class SqlInternalTransaction
    {
        internal const long NullTransactionId = 0;

        private TransactionState _transactionState;
        private TransactionType _transactionType;
        private long _transactionId;             // passed in the MARS headers
        private int _openResultCount;           // passed in the MARS headers
        private SqlInternalConnection _innerConnection;
        private bool _disposing;                 // used to prevent us from throwing exceptions while we're disposing
        private WeakReference _parent;                    // weak ref to the outer transaction object; needs to be weak to allow GC to occur.


        internal bool RestoreBrokenConnection { get; set; }
        internal bool ConnectionHasBeenRestored { get; set; }

        internal SqlInternalTransaction(SqlInternalConnection innerConnection, TransactionType type, SqlTransaction outerTransaction) : this(innerConnection, type, outerTransaction, NullTransactionId)
        {
        }

        internal SqlInternalTransaction(SqlInternalConnection innerConnection, TransactionType type, SqlTransaction outerTransaction, long transactionId)
        {
            _innerConnection = innerConnection;
            _transactionType = type;

            if (null != outerTransaction)
            {
                _parent = new WeakReference(outerTransaction);
            }

            _transactionId = transactionId;
            RestoreBrokenConnection = false;
            ConnectionHasBeenRestored = false;
        }

        internal bool HasParentTransaction
        {
            get
            {
                // Return true if we are an API started local transaction, or if we were a TSQL
                // started local transaction and were then wrapped with a parent transaction as
                // a result of a later API begin transaction.
                bool result = ((TransactionType.LocalFromAPI == _transactionType) ||
                                (TransactionType.LocalFromTSQL == _transactionType && _parent != null));
                return result;
            }
        }

        internal bool IsAborted
        {
            get
            {
                return (TransactionState.Aborted == _transactionState);
            }
        }

        internal bool IsActive
        {
            get
            {
                return (TransactionState.Active == _transactionState);
            }
        }

        internal bool IsCommitted
        {
            get
            {
                return (TransactionState.Committed == _transactionState);
            }
        }

        internal bool IsCompleted
        {
            get
            {
                return (TransactionState.Aborted == _transactionState
                     || TransactionState.Committed == _transactionState
                     || TransactionState.Unknown == _transactionState);
            }
        }

        internal bool IsDelegated
        {
            get
            {
                bool result = (TransactionType.Delegated == _transactionType);
                return result;
            }
        }

        internal bool IsDistributed
        {
            get
            {
                bool result = (TransactionType.Distributed == _transactionType);
                return result;
            }
        }

        internal bool IsLocal
        {
            get
            {
                bool result = (TransactionType.LocalFromTSQL == _transactionType
                            || TransactionType.LocalFromAPI == _transactionType
                            );
                return result;
            }
        }

        internal bool IsOrphaned
        {
            get
            {
                // An internal transaction is orphaned when its parent has been
                // reclaimed by GC.
                bool result;
                if (null == _parent)
                {
                    // No parent, so we better be LocalFromTSQL.  Should we even return in this case -
                    // since it could be argued this is invalid?
                    Debug.Fail("Why are we calling IsOrphaned with no parent?");
                    Debug.Assert(_transactionType == TransactionType.LocalFromTSQL, "invalid state");
                    result = false;
                }
                else if (null == _parent.Target)
                {
                    // We have an parent, but parent was GC'ed.
                    result = true;
                }
                else
                {
                    // We have an parent, and parent is alive.
                    result = false;
                }

                return result;
            }
        }

        internal bool IsZombied
        {
            get
            {
                return (null == _innerConnection);
            }
        }


        internal int OpenResultsCount
        {
            get
            {
                return _openResultCount;
            }
        }

        internal SqlTransaction Parent
        {
            get
            {
                SqlTransaction result = null;
                // Should we protect against this, since this probably is an invalid state?
                Debug.Assert(null != _parent, "Why are we calling Parent with no parent?");
                if (null != _parent)
                {
                    result = (SqlTransaction)_parent.Target;
                }
                return result;
            }
        }

        internal long TransactionId
        {
            get
            {
                return _transactionId;
            }
            set
            {
                Debug.Assert(NullTransactionId == _transactionId, "setting transaction cookie while one is active?");
                _transactionId = value;
            }
        }

        internal void Activate()
        {
            _transactionState = TransactionState.Active;
        }

        private void CheckTransactionLevelAndZombie()
        {
            try
            {
                if (!IsZombied && GetServerTransactionLevel() == 0)
                {
                    // If not zombied, not closed, and not in transaction, zombie.
                    Zombie();
                }
            }
            catch (Exception e)
            {
                if (!ADP.IsCatchableExceptionType(e))
                {
                    throw;
                }
                Zombie(); // If exception caught when trying to check level, zombie.
            }
        }

        internal void CloseFromConnection()
        {
            SqlInternalConnection innerConnection = _innerConnection;

            Debug.Assert(innerConnection != null, "How can we be here if the connection is null?");

            bool processFinallyBlock = true;
            try
            {
                innerConnection.ExecuteTransaction(SqlInternalConnection.TransactionRequest.IfRollback, null, IsolationLevel.Unspecified, null, false);
            }
            catch (Exception e)
            {
                processFinallyBlock = ADP.IsCatchableExceptionType(e);
                throw;
            }
            finally
            {
                if (processFinallyBlock)
                {
                    // Always ensure we're zombied; Yukon will send an EnvChange that
                    // will cause the zombie, but only if we actually go to the wire;
                    // Sphinx and Shiloh won't send the env change, so we have to handle
                    // them ourselves.
                    Zombie();
                }
            }
        }

        internal void Commit()
        {
            if (_innerConnection.IsLockedForBulkCopy)
            {
                throw SQL.ConnectionLockedForBcpEvent();
            }

            _innerConnection.ValidateConnectionForExecute(null);

            // If this transaction has been completed, throw exception since it is unusable.
            try
            {
                // COMMIT ignores transaction names, and so there is no reason to pass it anything.  COMMIT
                // simply commits the transaction from the most recent BEGIN, nested or otherwise.
                _innerConnection.ExecuteTransaction(SqlInternalConnection.TransactionRequest.Commit, null, IsolationLevel.Unspecified, null, false);
                {
                    ZombieParent();
                }
            }
            catch (Exception e)
            {
                if (ADP.IsCatchableExceptionType(e))
                {
                    CheckTransactionLevelAndZombie();
                }

                throw;
            }
        }

        internal void Completed(TransactionState transactionState)
        {
            Debug.Assert(TransactionState.Active < transactionState, "invalid transaction completion state?");
            _transactionState = transactionState;
            Zombie();
        }

        internal int DecrementAndObtainOpenResultCount()
        {
            int openResultCount = Interlocked.Decrement(ref _openResultCount);
            if (openResultCount < 0)
            {
                throw SQL.OpenResultCountExceeded();
            }
            return openResultCount;
        }

        internal void Dispose()
        {
            this.Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        private /*protected override*/ void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (null != _innerConnection)
                {
                    // implicitly rollback if transaction still valid
                    _disposing = true;
                    this.Rollback();
                }
            }
        }

        private int GetServerTransactionLevel()
        {
            // This function is needed for those times when it is impossible to determine the server's
            // transaction level, unless the user's arguments were parsed - which is something we don't want
            // to do.  An example when it is impossible to determine the level is after a rollback.

            using (SqlCommand transactionLevelCommand = new SqlCommand("set @out = @@trancount", (SqlConnection)(_innerConnection.Owner)))
            {
                transactionLevelCommand.Transaction = Parent;

                SqlParameter parameter = new SqlParameter("@out", SqlDbType.Int);
                parameter.Direction = ParameterDirection.Output;
                transactionLevelCommand.Parameters.Add(parameter);

                transactionLevelCommand.RunExecuteReader(CommandBehavior.Default, RunBehavior.UntilDone, returnStream: false);

                return (int)parameter.Value;
            }
        }

        internal int IncrementAndObtainOpenResultCount()
        {
            int openResultCount = Interlocked.Increment(ref _openResultCount);

            if (openResultCount < 0)
            {
                throw SQL.OpenResultCountExceeded();
            }
            return openResultCount;
        }

        internal void InitParent(SqlTransaction transaction)
        {
            Debug.Assert(_parent == null, "Why do we have a parent on InitParent?");
            _parent = new WeakReference(transaction);
        }

        internal void Rollback()
        {
            if (_innerConnection.IsLockedForBulkCopy)
            {
                throw SQL.ConnectionLockedForBcpEvent();
            }

            _innerConnection.ValidateConnectionForExecute(null);

            try
            {
                // If no arg is given to ROLLBACK it will rollback to the outermost begin - rolling back
                // all nested transactions as well as the outermost transaction.
                _innerConnection.ExecuteTransaction(SqlInternalConnection.TransactionRequest.IfRollback, null, IsolationLevel.Unspecified, null, false);

                // Since Rollback will rollback to outermost begin, no need to check
                // server transaction level.  This transaction has been completed.
                Zombie();
            }
            catch (Exception e)
            {
                if (ADP.IsCatchableExceptionType(e))
                {
                    CheckTransactionLevelAndZombie();

                    if (!_disposing)
                    {
                        throw;
                    }
                }
                else
                {
                    throw;
                }
            }
        }

        internal void Rollback(string transactionName)
        {
            if (_innerConnection.IsLockedForBulkCopy)
            {
                throw SQL.ConnectionLockedForBcpEvent();
            }

            _innerConnection.ValidateConnectionForExecute(null);

            // ROLLBACK takes either a save point name or a transaction name.  It will rollback the
            // transaction to either the save point with the save point name or begin with the
            // transaction name.  NOTE: for simplicity it is possible to give all save point names
            // the same name, and ROLLBACK will simply rollback to the most recent save point with the
            // save point name.
            if (string.IsNullOrEmpty(transactionName))
                throw SQL.NullEmptyTransactionName();

            try
            {
                _innerConnection.ExecuteTransaction(SqlInternalConnection.TransactionRequest.Rollback, transactionName, IsolationLevel.Unspecified, null, false);
            }
            catch (Exception e)
            {
                if (ADP.IsCatchableExceptionType(e))
                {
                    CheckTransactionLevelAndZombie();
                }
                throw;
            }
        }

        internal void Save(string savePointName)
        {
            _innerConnection.ValidateConnectionForExecute(null);

            // ROLLBACK takes either a save point name or a transaction name.  It will rollback the
            // transaction to either the save point with the save point name or begin with the
            // transaction name.  So, to rollback a nested transaction you must have a save point.
            // SAVE TRANSACTION MUST HAVE AN ARGUMENT!!!  Save Transaction without an arg throws an
            // exception from the server.  So, an overload for SaveTransaction without an arg doesn't make
            // sense to have.  Save Transaction does not affect the transaction level.
            if (string.IsNullOrEmpty(savePointName))
                throw SQL.NullEmptyTransactionName();

            try
            {
                _innerConnection.ExecuteTransaction(SqlInternalConnection.TransactionRequest.Save, savePointName, IsolationLevel.Unspecified, null, false);
            }
            catch (Exception e)
            {
                if (ADP.IsCatchableExceptionType(e))
                {
                    CheckTransactionLevelAndZombie();
                }

                throw;
            }
        }

        internal void Zombie()
        {
            // Called by several places in the code to ensure that the outer
            // transaction object has been zombied and the parser has broken
            // it's reference to us.

            // NOTE: we'll be called from the TdsParser when it gets appropriate
            // ENVCHANGE events that indicate the transaction has completed, however
            // we cannot rely upon those events occurring in the case of pre-Yukon
            // servers (and when we don't go to the wire because the connection
            // is broken) so we can also be called from the Commit/Rollback/Save
            // methods to handle that case as well.

            // There are two parts to a full zombie:
            // 1) Zombie parent and disconnect outer transaction from internal transaction
            // 2) Disconnect internal transaction from connection and parser
            // Number 1 needs to be done whenever a SqlTransaction object is completed.  Number
            // 2 is only done when a transaction is actually completed.  Since users can begin
            // transactions both in and outside of the API, and since nested begins are not actual
            // transactions we need to distinguish between #1 and #2.  

            ZombieParent();

            SqlInternalConnection innerConnection = _innerConnection;
            _innerConnection = null;

            if (null != innerConnection)
            {
                innerConnection.DisconnectTransaction(this);
            }
        }

        private void ZombieParent()
        {
            if (null != _parent)
            {
                SqlTransaction parent = (SqlTransaction)_parent.Target;
                if (null != parent)
                {
                    parent.Zombie();
                }
                _parent = null;
            }
        }
    }
}
