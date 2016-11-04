// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Transactions.Diagnostics;
using System.Transactions.Distributed;

namespace System.Transactions
{
    public class TransactionEventArgs : EventArgs
    {
        internal Transaction _transaction;
        public Transaction Transaction => _transaction;
    }

    public delegate void TransactionCompletedEventHandler(object sender, TransactionEventArgs e);

    public enum IsolationLevel
    {
        Serializable = 0,
        RepeatableRead = 1,
        ReadCommitted = 2,
        ReadUncommitted = 3,
        Snapshot = 4,
        Chaos = 5,
        Unspecified = 6,
    }

    public enum TransactionStatus
    {
        Active = 0,
        Committed = 1,
        Aborted = 2,
        InDoubt = 3
    }

    public enum DependentCloneOption
    {
        BlockCommitUntilComplete = 0,
        RollbackIfNotComplete = 1,
    }

    [Flags]
    public enum EnlistmentOptions
    {
        None = 0x0,
        EnlistDuringPrepareRequired = 0x1,
    }

    // When we serialize a Transaction, we specify the type DistributedTransaction, so a Transaction never
    // actually gets deserialized.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2229", Justification = "Serialization not yet supported and will be done using DistributedTransaction")]
    [Serializable]
    public class Transaction : IDisposable, ISerializable
    {
        // UseServiceDomain
        //
        // Property tells parts of system.transactions if it should use a
        // service domain for current.
        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal static bool UseServiceDomainForCurrent() => false;

        // InteropMode
        //
        // This property figures out the current interop mode based on the
        // top of the transaction scope stack as well as the default mode
        // from config.
        internal static EnterpriseServicesInteropOption InteropMode(TransactionScope currentScope)
        {
            if (currentScope != null)
            {
                return currentScope.InteropMode;
            }

            return EnterpriseServicesInteropOption.None;
        }

        internal static Transaction FastGetTransaction(TransactionScope currentScope, ContextData contextData, out Transaction contextTransaction)
        {
            Transaction current = null;
            contextTransaction = null;

            contextTransaction = contextData.CurrentTransaction;

            switch (InteropMode(currentScope))
            {
                case EnterpriseServicesInteropOption.None:

                    current = contextTransaction;

                    // If there is a transaction in the execution context or if there is a current transaction scope
                    // then honer the transaction context.
                    if (current == null && currentScope == null)
                    {
                        // Otherwise check for an external current.
                        if (TransactionManager.s_currentDelegateSet)
                        {
                            current = TransactionManager.s_currentDelegate();
                        }
                        else
                        {
                            current = EnterpriseServices.GetContextTransaction(contextData);
                        }
                    }
                    break;

                case EnterpriseServicesInteropOption.Full:
                    current = EnterpriseServices.GetContextTransaction(contextData);
                    break;

                case EnterpriseServicesInteropOption.Automatic:
                    if (EnterpriseServices.UseServiceDomainForCurrent())
                    {
                        current = EnterpriseServices.GetContextTransaction(contextData);
                    }
                    else
                    {
                        current = contextData.CurrentTransaction;
                    }
                    break;
            }

            return current;
        }


        // GetCurrentTransactionAndScope
        //
        // Returns both the current transaction and scope.  This is implemented for optimizations
        // in TransactionScope because it is required to get both of them in several cases.
        internal static void GetCurrentTransactionAndScope(
            TxLookup defaultLookup,
            out Transaction current,
            out TransactionScope currentScope,
            out Transaction contextTransaction)
        {
            current = null;
            currentScope = null;
            contextTransaction = null;

            ContextData contextData = ContextData.LookupContextData(defaultLookup);
            if (contextData != null)
            {
                currentScope = contextData.CurrentScope;
                current = FastGetTransaction(currentScope, contextData, out contextTransaction);
            }
        }

        public static Transaction Current
        {
            get
            {
                if (DiagnosticTrace.Verbose)
                {
                    MethodEnteredTraceRecord.Trace(SR.TraceSourceBase, "Transaction.get_Current");
                }

                Transaction current = null;
                TransactionScope currentScope = null;
                Transaction contextValue = null;
                GetCurrentTransactionAndScope(TxLookup.Default, out current, out currentScope, out contextValue);

                if (currentScope != null)
                {
                    if (currentScope.ScopeComplete)
                    {
                        throw new InvalidOperationException(SR.TransactionScopeComplete);
                    }
                }

                if (DiagnosticTrace.Verbose)
                {
                    MethodExitedTraceRecord.Trace(SR.TraceSourceBase, "Transaction.get_Current");
                }

                return current;
            }

            set
            {
                if (DiagnosticTrace.Verbose)
                {
                    MethodEnteredTraceRecord.Trace(SR.TraceSourceBase, "Transaction.set_Current");
                }

                // Bring your own Transaction(BYOT) is supported only for legacy scenarios. 
                // This transaction won't be flown across thread continuations.
                if (InteropMode(ContextData.TLSCurrentData.CurrentScope) != EnterpriseServicesInteropOption.None)
                {
                    if (DiagnosticTrace.Error)
                    {
                        InvalidOperationExceptionTraceRecord.Trace(SR.TraceSourceBase, SR.CannotSetCurrent);
                    }

                    throw new InvalidOperationException(SR.CannotSetCurrent);
                }

                // Support only legacy scenarios using TLS. 
                ContextData.TLSCurrentData.CurrentTransaction = value;
                // Clear CallContext data.
                CallContextCurrentData.ClearCurrentData(null, false);

                if (DiagnosticTrace.Verbose)
                {
                    MethodExitedTraceRecord.Trace(SR.TraceSourceBase, "Transaction.set_Current");
                }
            }
        }

        // Storage for the transaction isolation level
        internal IsolationLevel _isoLevel;

        // Storage for the consistent flag
        internal bool _complete = false;

        // Record an identifier for this clone
        internal int _cloneId;

        // Storage for a disposed flag
        internal const int _disposedTrueValue = 1;
        internal int _disposed = 0;
        internal bool Disposed { get { return _disposed == Transaction._disposedTrueValue; } }

        internal Guid DistributedTxId
        {
            get
            {
                Guid returnValue = Guid.Empty;

                if (_internalTransaction != null)
                {
                    returnValue = _internalTransaction.DistributedTxId;
                }
                return returnValue;
            }
        }

        // Internal synchronization object for transactions.  It is not safe to lock on the 
        // transaction object because it is public and users of the object may lock it for 
        // other purposes.
        internal InternalTransaction _internalTransaction;

        // The TransactionTraceIdentifier for the transaction instance.
        internal TransactionTraceIdentifier _traceIdentifier;

        // Not used by anyone
        private Transaction() { }

        // Create a transaction with the given settings
        //
        internal Transaction(IsolationLevel isoLevel, InternalTransaction internalTransaction)
        {
            TransactionManager.ValidateIsolationLevel(isoLevel);

            _isoLevel = isoLevel;

            // Never create a transaction with an IsolationLevel of Unspecified.
            if (IsolationLevel.Unspecified == _isoLevel)
            {
                _isoLevel = TransactionManager.DefaultIsolationLevel;
            }

            if (internalTransaction != null)
            {
                _internalTransaction = internalTransaction;
                _cloneId = Interlocked.Increment(ref _internalTransaction._cloneCount);
            }
            else
            {
                // Null is passed from the constructor of a CommittableTransaction.  That
                // constructor will fill in the traceIdentifier because it has allocated the
                // internal transaction.
            }
        }

        internal Transaction(DistributedTransaction distributedTransaction)
        {
            _isoLevel = distributedTransaction.IsolationLevel;
            _internalTransaction = new InternalTransaction(this, distributedTransaction);
            _cloneId = Interlocked.Increment(ref _internalTransaction._cloneCount);
        }

        internal Transaction(IsolationLevel isoLevel, ISimpleTransactionSuperior superior)
        {
            TransactionManager.ValidateIsolationLevel(isoLevel);

            if (superior == null)
            {
                throw new ArgumentNullException(nameof(superior));
            }

            _isoLevel = isoLevel;

            // Never create a transaction with an IsolationLevel of Unspecified.
            if (IsolationLevel.Unspecified == _isoLevel)
            {
                _isoLevel = TransactionManager.DefaultIsolationLevel;
            }

            _internalTransaction = new InternalTransaction(this, superior);
            // ISimpleTransactionSuperior is defined to also promote to MSDTC.
            _internalTransaction.SetPromoterTypeToMSDTC();
            _cloneId = 1;
        }

        #region System.Object Overrides

        // Don't use the identifier for the hash code.
        //
        public override int GetHashCode()
        {
            return _internalTransaction.TransactionHash;
        }


        // Don't allow equals to get the identifier
        //
        public override bool Equals(object obj)
        {
            Transaction transaction = obj as Transaction;

            // If we can't cast the object as a Transaction, it must not be equal
            // to this, which is a Transaction.
            if (null == transaction)
            {
                return false;
            }

            // Check the internal transaction object for equality.
            return _internalTransaction.TransactionHash == transaction._internalTransaction.TransactionHash;
        }

        public static bool operator ==(Transaction x, Transaction y)
        {
            if (((object)x) != null)
            {
                return x.Equals(y);
            }
            return ((object)y) == null;
        }

        public static bool operator !=(Transaction x, Transaction y)
        {
            if (((object)x) != null)
            {
                return !x.Equals(y);
            }
            return ((object)y) != null;
        }


        #endregion

        public TransactionInformation TransactionInformation
        {
            get
            {
                if (DiagnosticTrace.Verbose)
                {
                    MethodEnteredTraceRecord.Trace(SR.TraceSourceLtm, "Transaction.get_TransactionInformation");
                }

                if (Disposed)
                {
                    throw new ObjectDisposedException(nameof(Transaction));
                }

                TransactionInformation txInfo = _internalTransaction._transactionInformation;
                if (txInfo == null)
                {
                    // A race would only result in an extra allocation
                    txInfo = new TransactionInformation(_internalTransaction);
                    _internalTransaction._transactionInformation = txInfo;
                }

                if (DiagnosticTrace.Verbose)
                {
                    MethodExitedTraceRecord.Trace(SR.TraceSourceLtm, "Transaction.get_TransactionInformation");
                }

                return txInfo;
            }
        }


        // Return the Isolation Level for the given transaction
        //
        public IsolationLevel IsolationLevel
        {
            get
            {
                if (DiagnosticTrace.Verbose)
                {
                    MethodEnteredTraceRecord.Trace(SR.TraceSourceLtm, "Transaction.get_IsolationLevel");
                }
                if (Disposed)
                {
                    throw new ObjectDisposedException(nameof(Transaction));
                }

                if (DiagnosticTrace.Verbose)
                {
                    MethodExitedTraceRecord.Trace(SR.TraceSourceLtm, "Transaction.get_IsolationLevel");
                }
                return _isoLevel;
            }
        }

        /// <summary>
        /// Gets the PromoterType value for the transaction.
        /// </summary>
        /// <value>
        /// If the transaction has not yet been promoted and does not yet have a promotable single phase enlistment,
        /// this property value will be Guid.Empty.
        /// 
        /// If the transaction has been promoted or has a promotable single phase enlistment, this will return the
        /// promoter type specified by the transaction promoter.
        /// 
        /// If the transaction is, or will be, promoted to MSDTC, the value will be TransactionInterop.PromoterTypeDtc.
        /// </value>
        public Guid PromoterType
        {
            get
            {
                if (DiagnosticTrace.Verbose)
                {
                    MethodEnteredTraceRecord.Trace(SR.TraceSourceLtm, "Transaction.get_PromoterType");
                }

                if (Disposed)
                {
                    throw new ObjectDisposedException(nameof(Transaction));
                }

                lock (_internalTransaction)
                {
                    return _internalTransaction._promoterType;
                }
            }
        }

        /// <summary>
        /// Gets the PromotedToken for the transaction.
        /// 
        /// If the transaction has not already been promoted, retrieving this value will cause promotion. Before retrieving the
        /// PromotedToken, the Transaction.PromoterType value should be checked to see if it is a promoter type (Guid) that the
        /// caller understands. If the caller does not recognize the PromoterType value, retreiving the PromotedToken doesn't
        /// have much value because the caller doesn't know how to utilize it. But if the PromoterType is recognized, the
        /// caller should know how to utilize the PromotedToken to communicate with the promoting distributed transaction
        /// coordinator to enlist on the distributed transaction.
        /// 
        /// If the value of a transaction's PromoterType is TransactionInterop.PromoterTypeDtc, then that transaction's
        /// PromotedToken will be an MSDTC-based TransmitterPropagationToken.
        /// </summary>
        /// <returns> 
        /// The byte[] that can be used to enlist with the distributed transaction coordinator used to promote the transaction.
        /// The format of the byte[] depends upon the value of Transaction.PromoterType.
        /// </returns>
        public byte[] GetPromotedToken()
        {
            // We need to ask the current transaction state for the PromotedToken because depending on the state
            // we may need to induce a promotion.
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceLtm, "Transaction.GetPromotedToken");
            }

            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(Transaction));
            }

            // We always make a copy of the promotedToken stored in the internal transaction.
            byte[] internalPromotedToken;
            lock (_internalTransaction)
            {
                internalPromotedToken = _internalTransaction.State.PromotedToken(_internalTransaction);
            }

            byte[] toReturn = new byte[internalPromotedToken.Length];
            Array.Copy(internalPromotedToken, toReturn, toReturn.Length);
            return toReturn;
        }

        public Enlistment EnlistDurable(
            Guid resourceManagerIdentifier,
            IEnlistmentNotification enlistmentNotification,
            EnlistmentOptions enlistmentOptions)
        {
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceLtm, "Transaction.EnlistDurable( IEnlistmentNotification )");
            }

            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(Transaction));
            }

            if (resourceManagerIdentifier == Guid.Empty)
            {
                throw new ArgumentException(SR.BadResourceManagerId, nameof(resourceManagerIdentifier));
            }

            if (enlistmentNotification == null)
            {
                throw new ArgumentNullException(nameof(enlistmentNotification));
            }

            if (enlistmentOptions != EnlistmentOptions.None && enlistmentOptions != EnlistmentOptions.EnlistDuringPrepareRequired)
            {
                throw new ArgumentOutOfRangeException(nameof(enlistmentOptions));
            }

            if (_complete)
            {
                throw TransactionException.CreateTransactionCompletedException(SR.TraceSourceLtm, DistributedTxId);
            }

            lock (_internalTransaction)
            {
                Enlistment enlistment = _internalTransaction.State.EnlistDurable(_internalTransaction,
                    resourceManagerIdentifier, enlistmentNotification, enlistmentOptions, this);

                if (DiagnosticTrace.Verbose)
                {
                    MethodExitedTraceRecord.Trace(SR.TraceSourceLtm, "Transaction.EnlistDurable( IEnlistmentNotification )");
                }
                return enlistment;
            }
        }


        // Forward request to the state machine to take the appropriate action.
        //
        public Enlistment EnlistDurable(
            Guid resourceManagerIdentifier,
            ISinglePhaseNotification singlePhaseNotification,
            EnlistmentOptions enlistmentOptions)
        {
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceLtm, "Transaction.EnlistDurable( ISinglePhaseNotification )");
            }

            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(Transaction));
            }

            if (resourceManagerIdentifier == Guid.Empty)
            {
                throw new ArgumentException(SR.BadResourceManagerId, nameof(resourceManagerIdentifier));
            }

            if (singlePhaseNotification == null)
            {
                throw new ArgumentNullException(nameof(singlePhaseNotification));
            }

            if (enlistmentOptions != EnlistmentOptions.None && enlistmentOptions != EnlistmentOptions.EnlistDuringPrepareRequired)
            {
                throw new ArgumentOutOfRangeException(nameof(enlistmentOptions));
            }

            if (_complete)
            {
                throw TransactionException.CreateTransactionCompletedException(SR.TraceSourceLtm, DistributedTxId);
            }

            lock (_internalTransaction)
            {
                Enlistment enlistment = _internalTransaction.State.EnlistDurable(_internalTransaction,
                    resourceManagerIdentifier, singlePhaseNotification, enlistmentOptions, this);

                if (DiagnosticTrace.Verbose)
                {
                    MethodExitedTraceRecord.Trace(SR.TraceSourceLtm, "Transaction.EnlistDurable( ISinglePhaseNotification )");
                }
                return enlistment;
            }
        }


        public void Rollback()
        {
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceLtm, "Transaction.Rollback");
            }

            if (DiagnosticTrace.Warning)
            {
                TransactionRollbackCalledTraceRecord.Trace(SR.TraceSourceLtm, TransactionTraceId);
            }

            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(Transaction));
            }

            lock (_internalTransaction)
            {
                Debug.Assert(_internalTransaction.State != null);
                _internalTransaction.State.Rollback(_internalTransaction, null);
            }

            if (DiagnosticTrace.Verbose)
            {
                MethodExitedTraceRecord.Trace(SR.TraceSourceLtm, "Transaction.Rollback");
            }
        }


        public void Rollback(Exception e)
        {
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceLtm, "Transaction.Rollback");
            }

            if (DiagnosticTrace.Warning)
            {
                TransactionRollbackCalledTraceRecord.Trace(SR.TraceSourceLtm, TransactionTraceId);
            }

            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(Transaction));
            }

            lock (_internalTransaction)
            {
                Debug.Assert(_internalTransaction.State != null);
                _internalTransaction.State.Rollback(_internalTransaction, e);
            }

            if (DiagnosticTrace.Verbose)
            {
                MethodExitedTraceRecord.Trace(SR.TraceSourceLtm, "Transaction.Rollback");
            }
        }


        // Forward request to the state machine to take the appropriate action.
        //
        public Enlistment EnlistVolatile(IEnlistmentNotification enlistmentNotification, EnlistmentOptions enlistmentOptions)
        {
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceLtm, "Transaction.EnlistVolatile( IEnlistmentNotification )");
            }

            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(Transaction));
            }

            if (enlistmentNotification == null)
            {
                throw new ArgumentNullException(nameof(enlistmentNotification));
            }

            if (enlistmentOptions != EnlistmentOptions.None && enlistmentOptions != EnlistmentOptions.EnlistDuringPrepareRequired)
            {
                throw new ArgumentOutOfRangeException(nameof(enlistmentOptions));
            }

            if (_complete)
            {
                throw TransactionException.CreateTransactionCompletedException(SR.TraceSourceLtm, DistributedTxId);
            }

            lock (_internalTransaction)
            {
                Enlistment enlistment = _internalTransaction.State.EnlistVolatile(_internalTransaction,
                    enlistmentNotification, enlistmentOptions, this);

                if (DiagnosticTrace.Verbose)
                {
                    MethodExitedTraceRecord.Trace(SR.TraceSourceLtm,
                        "Transaction.EnlistVolatile( IEnlistmentNotification )"
                        );
                }
                return enlistment;
            }
        }


        // Forward request to the state machine to take the appropriate action.
        //
        public Enlistment EnlistVolatile(ISinglePhaseNotification singlePhaseNotification, EnlistmentOptions enlistmentOptions)
        {
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceLtm, "Transaction.EnlistVolatile( ISinglePhaseNotification )");
            }

            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(Transaction));
            }

            if (singlePhaseNotification == null)
            {
                throw new ArgumentNullException(nameof(singlePhaseNotification));
            }

            if (enlistmentOptions != EnlistmentOptions.None && enlistmentOptions != EnlistmentOptions.EnlistDuringPrepareRequired)
            {
                throw new ArgumentOutOfRangeException(nameof(enlistmentOptions));
            }

            if (_complete)
            {
                throw TransactionException.CreateTransactionCompletedException(SR.TraceSourceLtm, DistributedTxId);
            }

            lock (_internalTransaction)
            {
                Enlistment enlistment = _internalTransaction.State.EnlistVolatile(_internalTransaction,
                    singlePhaseNotification, enlistmentOptions, this);

                if (DiagnosticTrace.Verbose)
                {
                    MethodExitedTraceRecord.Trace(SR.TraceSourceLtm,
                        "Transaction.EnlistVolatile( ISinglePhaseNotification )"
                        );
                }
                return enlistment;
            }
        }

        // Create a clone of the transaction that forwards requests to this object.
        //
        public Transaction Clone()
        {
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceLtm, "Transaction.Clone");
            }

            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(Transaction));
            }

            if (_complete)
            {
                throw TransactionException.CreateTransactionCompletedException(SR.TraceSourceLtm, DistributedTxId);
            }

            Transaction clone = InternalClone();

            if (DiagnosticTrace.Verbose)
            {
                MethodExitedTraceRecord.Trace(SR.TraceSourceLtm, "Transaction.Clone");
            }
            return clone;
        }

        internal Transaction InternalClone()
        {
            Transaction clone = new Transaction(_isoLevel, _internalTransaction);

            if (DiagnosticTrace.Verbose)
            {
                CloneCreatedTraceRecord.Trace(SR.TraceSourceLtm, clone.TransactionTraceId);
            }

            return clone;
        }


        // Create a dependent clone of the transaction that forwards requests to this object.
        //
        public DependentTransaction DependentClone(
            DependentCloneOption cloneOption
            )
        {
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceLtm, "Transaction.DependentClone");
            }

            if (cloneOption != DependentCloneOption.BlockCommitUntilComplete
                && cloneOption != DependentCloneOption.RollbackIfNotComplete)
            {
                throw new ArgumentOutOfRangeException(nameof(cloneOption));
            }

            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(Transaction));
            }

            if (_complete)
            {
                throw TransactionException.CreateTransactionCompletedException(SR.TraceSourceLtm, DistributedTxId);
            }

            DependentTransaction clone = new DependentTransaction(
                _isoLevel, _internalTransaction, cloneOption == DependentCloneOption.BlockCommitUntilComplete);

            if (DiagnosticTrace.Information)
            {
                DependentCloneCreatedTraceRecord.Trace(SR.TraceSourceLtm, clone.TransactionTraceId, cloneOption);
            }
            if (DiagnosticTrace.Verbose)
            {
                MethodExitedTraceRecord.Trace(SR.TraceSourceLtm, "Transaction.DependentClone");
            }
            return clone;
        }


        internal TransactionTraceIdentifier TransactionTraceId
        {
            get
            {
                if (_traceIdentifier == TransactionTraceIdentifier.Empty)
                {
                    lock (_internalTransaction)
                    {
                        if (_traceIdentifier == TransactionTraceIdentifier.Empty)
                        {
                            TransactionTraceIdentifier temp = new TransactionTraceIdentifier(
                                _internalTransaction.TransactionTraceId.TransactionIdentifier,
                                _cloneId);
                            Interlocked.MemoryBarrier();
                            _traceIdentifier = temp;
                        }
                    }
                }
                return _traceIdentifier;
            }
        }

        // Forward request to the state machine to take the appropriate action.
        //
        public event TransactionCompletedEventHandler TransactionCompleted
        {
            add
            {
                if (Disposed)
                {
                    throw new ObjectDisposedException(nameof(Transaction));
                }

                lock (_internalTransaction)
                {
                    // Register for completion with the inner transaction
                    _internalTransaction.State.AddOutcomeRegistrant(_internalTransaction, value);
                }
            }

            remove
            {
                lock (_internalTransaction)
                {
                    _internalTransaction._transactionCompletedDelegate = (TransactionCompletedEventHandler)
                        System.Delegate.Remove(_internalTransaction._transactionCompletedDelegate, value);
                }
            }
        }

        public void Dispose()
        {
            InternalDispose();
        }

        // Handle Transaction Disposal.
        //
        internal virtual void InternalDispose()
        {
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceLtm, "IDisposable.Dispose");
            }

            if (Interlocked.Exchange(ref _disposed, Transaction._disposedTrueValue) == Transaction._disposedTrueValue)
            {
                return;
            }

            // Attempt to clean up the internal transaction
            long remainingITx = Interlocked.Decrement(ref _internalTransaction._cloneCount);
            if (remainingITx == 0)
            {
                _internalTransaction.Dispose();
            }

            if (DiagnosticTrace.Verbose)
            {
                MethodExitedTraceRecord.Trace(SR.TraceSourceLtm, "IDisposable.Dispose");
            }
        }

        // Ask the state machine for serialization info.
        //
        void ISerializable.GetObjectData(
            SerializationInfo serializationInfo,
            StreamingContext context)
        {
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceLtm, "ISerializable.GetObjectData");
            }

            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(Transaction));
            }

            if (serializationInfo == null)
            {
                throw new ArgumentNullException(nameof(serializationInfo));
            }

            if (_complete)
            {
                throw TransactionException.CreateTransactionCompletedException(SR.TraceSourceLtm, DistributedTxId);
            }

            lock (_internalTransaction)
            {
                _internalTransaction.State.GetObjectData(_internalTransaction, serializationInfo, context);
            }

            if (DiagnosticTrace.Information)
            {
                TransactionSerializedTraceRecord.Trace(SR.TraceSourceLtm, TransactionTraceId);
            }

            if (DiagnosticTrace.Verbose)
            {
                MethodExitedTraceRecord.Trace(SR.TraceSourceLtm, "ISerializable.GetObjectData");
            }
        }

        /// <summary>
        /// Create a promotable single phase enlistment that promotes to MSDTC.
        /// </summary>
        /// <param name="promotableSinglePhaseNotification">The object that implements the IPromotableSinglePhaseNotification interface.</param>
        /// <returns>
        /// True if the enlistment is successful.
        /// 
        /// False if the transaction already has a durable enlistment or promotable single phase enlistment or
        /// if the transaction has already promoted. In this case, the caller will need to enlist in the transaction through other
        /// means, such as Transaction.EnlistDurable or retreive the MSDTC export cookie or propagation token to enlist with MSDTC.
        /// </returns>
        // We apparently didn't spell Promotable like FXCop thinks it should be spelled.
        public bool EnlistPromotableSinglePhase(IPromotableSinglePhaseNotification promotableSinglePhaseNotification)
        {
            return EnlistPromotableSinglePhase(promotableSinglePhaseNotification, TransactionInterop.PromoterTypeDtc);
        }

        /// <summary>
        /// Create a promotable single phase enlistment that promotes to a distributed transaction manager other than MSDTC.
        /// </summary>
        /// <param name="promotableSinglePhaseNotification">The object that implements the IPromotableSinglePhaseNotification interface.</param>
        /// <param name="promoterType">
        /// The promoter type Guid that identifies the format of the byte[] that is returned by the ITransactionPromoter.Promote
        /// call that is implemented by the IPromotableSinglePhaseNotificationObject, and thus the promoter of the transaction.
        /// </param>
        /// <returns>
        /// True if the enlistment is successful.
        /// 
        /// False if the transaction already has a durable enlistment or promotable single phase enlistment or
        /// if the transaction has already promoted. In this case, the caller will need to enlist in the transaction through other
        /// means.
        /// 
        /// If the Transaction.PromoterType matches the promoter type supported by the caller, then the
        /// Transaction.PromotedToken can be retrieved and used to enlist directly with the identified distributed transaction manager.
        ///
        /// How the enlistment is created with the distributed transaction manager identified by the Transaction.PromoterType
        /// is defined by that distributed transaction manager.
        /// </returns>
        public bool EnlistPromotableSinglePhase(IPromotableSinglePhaseNotification promotableSinglePhaseNotification, Guid promoterType)
        {
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceLtm, "Transaction.EnlistPromotableSinglePhase");
            }

            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(Transaction));
            }

            if (promotableSinglePhaseNotification == null)
            {
                throw new ArgumentNullException(nameof(promotableSinglePhaseNotification));
            }

            if (promoterType == Guid.Empty)
            {
                throw new ArgumentException(SR.PromoterTypeInvalid, nameof(promoterType));
            }

            if (_complete)
            {
                throw TransactionException.CreateTransactionCompletedException(SR.TraceSourceLtm, DistributedTxId);
            }

            bool succeeded = false;

            lock (_internalTransaction)
            {
                succeeded = _internalTransaction.State.EnlistPromotableSinglePhase(_internalTransaction, promotableSinglePhaseNotification, this, promoterType);
            }

            if (DiagnosticTrace.Verbose)
            {
                MethodExitedTraceRecord.Trace(SR.TraceSourceLtm, "Transaction.EnlistPromotableSinglePhase");
            }

            return succeeded;
        }

        public Enlistment PromoteAndEnlistDurable(Guid resourceManagerIdentifier,
                                                  IPromotableSinglePhaseNotification promotableNotification,
                                                  ISinglePhaseNotification enlistmentNotification,
                                                  EnlistmentOptions enlistmentOptions)
        {
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceDistributed, "Transaction.PromoteAndEnlistDurable");
            }

            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(Transaction));
            }

            if (resourceManagerIdentifier == Guid.Empty)
            {
                throw new ArgumentException(SR.BadResourceManagerId, nameof(resourceManagerIdentifier));
            }

            if (promotableNotification == null)
            {
                throw new ArgumentNullException(nameof(promotableNotification));
            }

            if (enlistmentNotification == null)
            {
                throw new ArgumentNullException(nameof(enlistmentNotification));
            }

            if (enlistmentOptions != EnlistmentOptions.None && enlistmentOptions != EnlistmentOptions.EnlistDuringPrepareRequired)
            {
                throw new ArgumentOutOfRangeException(nameof(enlistmentOptions));
            }

            if (_complete)
            {
                throw TransactionException.CreateTransactionCompletedException(SR.TraceSourceLtm, DistributedTxId);
            }

            lock (_internalTransaction)
            {
                Enlistment enlistment = _internalTransaction.State.PromoteAndEnlistDurable(_internalTransaction,
                    resourceManagerIdentifier, promotableNotification, enlistmentNotification, enlistmentOptions, this);

                if (DiagnosticTrace.Verbose)
                {
                    MethodExitedTraceRecord.Trace(SR.TraceSourceDistributed, "Transaction.PromoteAndEnlistDurable");
                }
                return enlistment;
            }
        }

        public void SetDistributedTransactionIdentifier(IPromotableSinglePhaseNotification promotableNotification,
                                                        Guid distributedTransactionIdentifier)
        {
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceLtm, "Transaction.SetDistributedTransactionIdentifier");
            }

            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(Transaction));
            }

            if (promotableNotification == null)
            {
                throw new ArgumentNullException(nameof(promotableNotification));
            }

            if (distributedTransactionIdentifier == Guid.Empty)
            {
                throw new ArgumentException(null, nameof(distributedTransactionIdentifier));
            }

            if (_complete)
            {
                throw TransactionException.CreateTransactionCompletedException(SR.TraceSourceLtm, DistributedTxId);
            }

            lock (_internalTransaction)
            {
                _internalTransaction.State.SetDistributedTransactionId(_internalTransaction,
                    promotableNotification,
                    distributedTransactionIdentifier);

                if (DiagnosticTrace.Verbose)
                {
                    MethodExitedTraceRecord.Trace(SR.TraceSourceLtm,
                        "Transaction.SetDistributedTransactionIdentifier"
                        );
                }
                return;
            }
        }

        internal DistributedTransaction Promote()
        {
            lock (_internalTransaction)
            {
                // This method is only called when we expect to be promoting to MSDTC.
                _internalTransaction.ThrowIfPromoterTypeIsNotMSDTC();
                _internalTransaction.State.Promote(_internalTransaction);
                return _internalTransaction.PromotedTransaction;
            }
        }
    }

    //
    // The following code & data is related to management of Transaction.Current
    //

    internal enum DefaultComContextState
    {
        Unknown = 0,
        Unavailable = -1,
        Available = 1
    }

    //
    //  The TxLookup enum is used internally to detect where the ambient context needs to be stored or looked up. 
    //  Default                  - Used internally when looking up Transaction.Current.  
    //  DefaultCallContext - Used when TransactionScope with async flow option is enabled. Internally we will use CallContext to store the ambient transaction.
    //  Default TLS            - Used for legacy/syncronous TransactionScope. Internally we will use TLS to store the ambient transaction.  
    //
    internal enum TxLookup
    {
        Default,
        DefaultCallContext,
        DefaultTLS,
    }

    //
    //  CallContextCurrentData holds the ambient transaction and uses CallContext and ConditionalWeakTable to track the ambient transaction.
    //  For async flow scenarios, we should not allow flowing of transaction across app domains. To prevent transaction from flowing across
    //  AppDomain/Remoting boundaries, we are using ConditionalWeakTable to hold the actual ambient transaction and store only a object reference 
    //  in CallContext. When TransactionScope is used to invoke a call across AppDomain/Remoting boundaries, only the object reference will be sent   
    //  across and not the actaul ambient transaction which is stashed away in the ConditionalWeakTable. 
    //
    internal static class CallContextCurrentData
    {
        private static AsyncLocal<ContextKey> s_currentTransaction = new AsyncLocal<ContextKey>();

        // ConditionalWeakTable is used to automatically remove the entries that are no longer referenced. This will help prevent leaks in async nested TransactionScope
        // usage and when child nested scopes are not syncronized properly. 
        private static readonly ConditionalWeakTable<ContextKey, ContextData> s_contextDataTable = new ConditionalWeakTable<ContextKey, ContextData>();

        // 
        //  Set CallContext data with the given contextKey. 
        //  return the ContextData if already present in contextDataTable, otherwise return the default value. 
        // 
        public static ContextData CreateOrGetCurrentData(ContextKey contextKey)
        {
            s_currentTransaction.Value = contextKey;
            return s_contextDataTable.GetValue(contextKey, (env) => new ContextData(true));
        }

        public static void ClearCurrentData(ContextKey contextKey, bool removeContextData)
        {
            // Get the current ambient CallContext.
            ContextKey key = s_currentTransaction.Value;
            if (contextKey != null || key != null)
            {
                // removeContextData flag is used for perf optimization to avoid removing from the table in certain nested TransactionScope usage. 
                if (removeContextData)
                {
                    // if context key is passed in remove that from the contextDataTable, otherwise remove the default context key. 
                    s_contextDataTable.Remove(contextKey ?? key);
                }

                if (key != null)
                {
                    s_currentTransaction.Value = null;
                }
            }
        }

        public static bool TryGetCurrentData(out ContextData currentData)
        {
            currentData = null;
            ContextKey contextKey = s_currentTransaction.Value;
            if (contextKey == null)
            {
                return false;
            }
            else
            {
                return s_contextDataTable.TryGetValue(contextKey, out currentData);
            }
        }
    }

    //
    // MarshalByRefObject is needed for cross AppDomain scenarios where just using object will end up with a different reference when call is made across serialization boundary.
    //
    internal class ContextKey // : MarshalByRefObject
    {
    }

    internal class ContextData
    {
        internal TransactionScope CurrentScope;
        internal Transaction CurrentTransaction;

        internal DefaultComContextState DefaultComContextState;
        internal WeakReference WeakDefaultComContext;

        internal bool _asyncFlow;

        [ThreadStatic]
        private static ContextData s_staticData;

        internal ContextData(bool asyncFlow)
        {
            _asyncFlow = asyncFlow;
        }

        internal static ContextData TLSCurrentData
        {
            get
            {
                ContextData data = s_staticData;
                if (data == null)
                {
                    data = new ContextData(false);
                    s_staticData = data;
                }

                return data;
            }
            set
            {
                if (value == null && s_staticData != null)
                {
                    // set each property to null to retain one TLS ContextData copy.
                    s_staticData.CurrentScope = null;
                    s_staticData.CurrentTransaction = null;
                    s_staticData.DefaultComContextState = DefaultComContextState.Unknown;
                    s_staticData.WeakDefaultComContext = null;
                }
                else
                {
                    s_staticData = value;
                }
            }
        }

        internal static ContextData LookupContextData(TxLookup defaultLookup)
        {
            ContextData currentData = null;
            if (CallContextCurrentData.TryGetCurrentData(out currentData))
            {
                if (currentData.CurrentScope == null && currentData.CurrentTransaction == null && defaultLookup != TxLookup.DefaultCallContext)
                {
                    // Clear Call Context Data
                    CallContextCurrentData.ClearCurrentData(null, true);
                    return TLSCurrentData;
                }

                return currentData;
            }
            else
            {
                return TLSCurrentData;
            }
        }
    }
}
