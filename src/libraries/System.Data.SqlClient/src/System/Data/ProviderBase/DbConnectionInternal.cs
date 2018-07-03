// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using System.Transactions;


namespace System.Data.ProviderBase
{
    internal abstract partial class DbConnectionInternal
    {
        private bool _isInStasis;

        private Transaction _enlistedTransaction;      // [usage must be thread-safe] the transaction that we're enlisted in, either manually or automatically

        // _enlistedTransaction is a clone, so that transaction information can be queried even if the original transaction object is disposed.
        // However, there are times when we need to know if the original transaction object was disposed, so we keep a reference to it here.
        // This field should only be assigned a value at the same time _enlistedTransaction is updated.
        // Also, this reference should not be disposed, since we aren't taking ownership of it.
        private Transaction _enlistedTransactionOriginal;

        protected internal Transaction EnlistedTransaction
        {
            get
            {
                return _enlistedTransaction;
            }
            set
            {
                Transaction currentEnlistedTransaction = _enlistedTransaction;
                if (((null == currentEnlistedTransaction) && (null != value))
                    || ((null != currentEnlistedTransaction) && !currentEnlistedTransaction.Equals(value)))
                {  // WebData 20000024

                    // Pay attention to the order here:
                    // 1) defect from any notifications
                    // 2) replace the transaction
                    // 3) re-enlist in notifications for the new transaction

                    // SQLBUDT #230558 we need to use a clone of the transaction
                    // when we store it, or we'll end up keeping it past the
                    // duration of the using block of the TransactionScope
                    Transaction valueClone = null;
                    Transaction previousTransactionClone = null;
                    try
                    {
                        if (null != value)
                        {
                            valueClone = value.Clone();
                        }

                        // NOTE: rather than take locks around several potential round-
                        // trips to the server, and/or virtual function calls, we simply
                        // presume that you aren't doing something illegal from multiple
                        // threads, and check once we get around to finalizing things
                        // inside a lock.

                        lock (this)
                        {
                            // NOTE: There is still a race condition here, when we are
                            // called from EnlistTransaction (which cannot re-enlist)
                            // instead of EnlistDistributedTransaction (which can),
                            // however this should have been handled by the outer
                            // connection which checks to ensure that it's OK.  The
                            // only case where we have the race condition is multiple
                            // concurrent enlist requests to the same connection, which
                            // is a bit out of line with something we should have to
                            // support.

                            // enlisted transaction can be nullified in Dispose call without lock
                            previousTransactionClone = Interlocked.Exchange(ref _enlistedTransaction, valueClone);
                            _enlistedTransactionOriginal = value;
                            value = valueClone;
                            valueClone = null; // we've stored it, don't dispose it.
                        }
                    }
                    finally
                    {
                        // we really need to dispose our clones; they may have
                        // native resources and GC may not happen soon enough.
                        // VSDevDiv 479564: don't dispose if still holding reference in _enlistedTransaction
                        if (null != previousTransactionClone &&
                                !object.ReferenceEquals(previousTransactionClone, _enlistedTransaction))
                        {
                            previousTransactionClone.Dispose();
                        }
                        if (null != valueClone && !object.ReferenceEquals(valueClone, _enlistedTransaction))
                        {
                            valueClone.Dispose();
                        }
                    }

                    // I don't believe that we need to lock to protect the actual
                    // enlistment in the transaction; it would only protect us
                    // against multiple concurrent calls to enlist, which really
                    // isn't supported anyway.

                    if (null != value)
                    {
                        TransactionOutcomeEnlist(value);
                    }
                }
            }
        }

        /// <summary>
        /// Get boolean value that indicates whether the enlisted transaction has been disposed.
        /// </summary>
        /// <value>
        /// True if there is an enlisted transaction, and it has been disposed.
        /// False if there is an enlisted transaction that has not been disposed, or if the transaction reference is null.
        /// </value>
        /// <remarks>
        /// This method must be called while holding a lock on the DbConnectionInternal instance.
        /// </remarks>
        protected bool EnlistedTransactionDisposed
        {
            get
            {
                // Until the Transaction.Disposed property is public it is necessary to access a member
                // that throws if the object is disposed to determine if in fact the transaction is disposed.
                try
                {
                    bool disposed;

                    Transaction currentEnlistedTransactionOriginal = _enlistedTransactionOriginal;
                    if (currentEnlistedTransactionOriginal != null)
                    {
                        disposed = currentEnlistedTransactionOriginal.TransactionInformation == null;
                    }
                    else
                    {
                        // Don't expect to get here in the general case,
                        // Since this getter is called by CheckEnlistedTransactionBinding
                        // after checking for a non-null enlisted transaction (and it does so under lock).
                        disposed = false;
                    }

                    return disposed;
                }
                catch (ObjectDisposedException)
                {
                    return true;
                }
            }
        }

        internal bool IsTxRootWaitingForTxEnd
        {
            get
            {
                return _isInStasis;
            }
        }

        virtual protected bool UnbindOnTransactionCompletion
        {
            get
            {
                return true;
            }
        }

        // Is this a connection that must be put in stasis (or is already in stasis) pending the end of it's transaction?
        virtual protected internal bool IsNonPoolableTransactionRoot
        {
            get
            {
                return false; // if you want to have delegated transactions that are non-poolable, you better override this...
            }
        }

        virtual internal bool IsTransactionRoot
        {
            get
            {
                return false; // if you want to have delegated transactions, you better override this...
            }
        }

        virtual protected bool ReadyToPrepareTransaction
        {
            get
            {
                return true;
            }
        }

        abstract protected void Activate(Transaction transaction);

        internal void ActivateConnection(Transaction transaction)
        {
            // Internal method called from the connection pooler so we don't expose
            // the Activate method publicly.

            Activate(transaction);
        }

        internal virtual void CloseConnection(DbConnection owningObject, DbConnectionFactory connectionFactory)
        {
            // The implementation here is the implementation required for the
            // "open" internal connections, since our own private "closed"
            // singleton internal connection objects override this method to
            // prevent anything funny from happening (like disposing themselves
            // or putting them into a connection pool)
            //
            // Derived class should override DbConnectionInternal.Deactivate and DbConnectionInternal.Dispose
            // for cleaning up after DbConnection.Close
            //     protected override void Deactivate() { // override DbConnectionInternal.Close
            //         // do derived class connection deactivation for both pooled & non-pooled connections
            //     }
            //     public override void Dispose() { // override DbConnectionInternal.Close
            //         // do derived class cleanup
            //         base.Dispose();
            //     }
            //
            // overriding DbConnection.Close is also possible, but must provider for their own synchronization
            //     public override void Close() { // override DbConnection.Close
            //         base.Close();
            //         // do derived class outer connection for both pooled & non-pooled connections
            //         // user must do their own synchronization here
            //     }
            //
            //     if the DbConnectionInternal derived class needs to close the connection it should
            //     delegate to the DbConnection if one exists or directly call dispose
            //         DbConnection owningObject = (DbConnection)Owner;
            //         if (null != owningObject) {
            //             owningObject.Close(); // force the closed state on the outer object.
            //         }
            //         else {
            //             Dispose();
            //         }
            //
            ////////////////////////////////////////////////////////////////
            // DON'T MESS WITH THIS CODE UNLESS YOU KNOW WHAT YOU'RE DOING!
            ////////////////////////////////////////////////////////////////
            Debug.Assert(null != owningObject, "null owningObject");
            Debug.Assert(null != connectionFactory, "null connectionFactory");


            // if an exception occurs after the state change but before the try block
            // the connection will be stuck in OpenBusy state.  The commented out try-catch
            // block doesn't really help because a ThreadAbort during the finally block
            // would just revert the connection to a bad state.
            // Open->Closed: guarantee internal connection is returned to correct pool
            if (connectionFactory.SetInnerConnectionFrom(owningObject, DbConnectionOpenBusy.SingletonInstance, this))
            {
                // Lock to prevent race condition with cancellation
                lock (this)
                {
                    object lockToken = ObtainAdditionalLocksForClose();
                    try
                    {
                        PrepareForCloseConnection();

                        DbConnectionPool connectionPool = Pool;

                        // Detach from enlisted transactions that are no longer active on close
                        DetachCurrentTransactionIfEnded();

                        // The singleton closed classes won't have owners and
                        // connection pools, and we won't want to put them back
                        // into the pool.
                        if (null != connectionPool)
                        {
                            connectionPool.PutObject(this, owningObject);   // PutObject calls Deactivate for us...
                                                                            // NOTE: Before we leave the PutObject call, another
                                                                            // thread may have already popped the connection from
                                                                            // the pool, so don't expect to be able to verify it.
                        }
                        else
                        {
                            Deactivate();   // ensure we de-activate non-pooled connections, or the data readers and transactions may not get cleaned up...

                            // To prevent an endless recursion, we need to clear
                            // the owning object before we call dispose so that
                            // we can't get here a second time... Ordinarily, I
                            // would call setting the owner to null a hack, but
                            // this is safe since we're about to dispose the
                            // object and it won't have an owner after that for
                            // certain.
                            _owningObject.Target = null;

                            if (IsTransactionRoot)
                            {
                                SetInStasis();
                            }
                            else
                            {
                                Dispose();
                            }
                        }
                    }
                    finally
                    {
                        ReleaseAdditionalLocksForClose(lockToken);
                        // if a ThreadAbort puts us here then its possible the outer connection will not reference
                        // this and this will be orphaned, not reclaimed by object pool until outer connection goes out of scope.
                        connectionFactory.SetInnerConnectionEvent(owningObject, DbConnectionClosedPreviouslyOpened.SingletonInstance);
                    }
                }
            }
        }

        virtual internal void DelegatedTransactionEnded()
        {
            // Called by System.Transactions when the delegated transaction has
            // completed.  We need to make closed connections that are in stasis
            // available again, or disposed closed/leaked non-pooled connections.

            // IMPORTANT NOTE: You must have taken a lock on the object before
            // you call this method to prevent race conditions with Clear and
            // ReclaimEmancipatedObjects.


            if (1 == _pooledCount)
            {
                // When _pooledCount is 1, it indicates a closed, pooled,
                // connection so it is ready to put back into the pool for
                // general use.

                TerminateStasis(true);

                Deactivate(); // call it one more time just in case

                DbConnectionPool pool = Pool;

                if (null == pool)
                {
                    throw ADP.InternalError(ADP.InternalErrorCode.PooledObjectWithoutPool);      // pooled connection does not have a pool
                }
                pool.PutObjectFromTransactedPool(this);
            }
            else if (-1 == _pooledCount && !_owningObject.IsAlive)
            {
                // When _pooledCount is -1 and the owning object no longer exists,
                // it indicates a closed (or leaked), non-pooled connection so 
                // it is safe to dispose.

                TerminateStasis(false);

                Deactivate(); // call it one more time just in case

                // it's a non-pooled connection, we need to dispose of it
                // once and for all, or the server will have fits about us
                // leaving connections open until the client-side GC kicks 
                // in.
                Dispose();
            }
            // When _pooledCount is 0, the connection is a pooled connection
            // that is either open (if the owning object is alive) or leaked (if
            // the owning object is not alive)  In either case, we can't muck 
            // with the connection here.
        }

        public virtual void Dispose()
        {
            _connectionPool = null;
            _connectionIsDoomed = true;
            _enlistedTransactionOriginal = null; // should not be disposed

            // Dispose of the _enlistedTransaction since it is a clone
            // of the original reference.
            // VSDD 780271 - _enlistedTransaction can be changed by another thread (TX end event)
            Transaction enlistedTransaction = Interlocked.Exchange(ref _enlistedTransaction, null);
            if (enlistedTransaction != null)
            {
                enlistedTransaction.Dispose();
            }
        }
        
        abstract public void EnlistTransaction(Transaction transaction);

        // Cleanup connection's transaction-specific structures (currently used by Delegated transaction).
        //  This is a separate method because cleanup can be triggered in multiple ways for a delegated
        //  transaction.
        virtual protected void CleanupTransactionOnCompletion(Transaction transaction)
        {
        }

        internal void DetachCurrentTransactionIfEnded()
        {
            Transaction enlistedTransaction = EnlistedTransaction;
            if (enlistedTransaction != null)
            {
                bool transactionIsDead;
                try
                {
                    transactionIsDead = (TransactionStatus.Active != enlistedTransaction.TransactionInformation.Status);
                }
                catch (TransactionException)
                {
                    // If the transaction is being processed (i.e. is part way through a rollback\commit\etc then TransactionInformation.Status will throw an exception)
                    transactionIsDead = true;
                }
                if (transactionIsDead)
                {
                    DetachTransaction(enlistedTransaction, true);
                }
            }
        }

        // Detach transaction from connection.
        internal void DetachTransaction(Transaction transaction, bool isExplicitlyReleasing)
        {
            // potentially a multi-threaded event, so lock the connection to make sure we don't enlist in a new
            // transaction between compare and assignment. No need to short circuit outside of lock, since failed comparisons should
            // be the exception, not the rule.
            lock (this)
            {
                // Detach if detach-on-end behavior, or if outer connection was closed
                DbConnection owner = (DbConnection)Owner;
                if (isExplicitlyReleasing || UnbindOnTransactionCompletion || null == owner)
                {
                    Transaction currentEnlistedTransaction = _enlistedTransaction;
                    if (currentEnlistedTransaction != null && transaction.Equals(currentEnlistedTransaction))
                    {
                        EnlistedTransaction = null;

                        if (IsTxRootWaitingForTxEnd)
                        {
                            DelegatedTransactionEnded();
                        }
                    }
                }
            }
        }

        // Handle transaction detach, pool cleanup and other post-transaction cleanup tasks associated with
        internal void CleanupConnectionOnTransactionCompletion(Transaction transaction)
        {
            DetachTransaction(transaction, false);

            DbConnectionPool pool = Pool;
            if (null != pool)
            {
                pool.TransactionEnded(transaction, this);
            }
        }

        void TransactionCompletedEvent(object sender, TransactionEventArgs e)
        {
            Transaction transaction = e.Transaction;

            CleanupTransactionOnCompletion(transaction);

            CleanupConnectionOnTransactionCompletion(transaction);
        }

        private void TransactionOutcomeEnlist(Transaction transaction)
        {
            transaction.TransactionCompleted += new TransactionCompletedEventHandler(TransactionCompletedEvent);
        }

        internal void SetInStasis()
        {
            _isInStasis = true;
        }

        private void TerminateStasis(bool returningToPool)
        {
            _isInStasis = false;
        }
    }
}
