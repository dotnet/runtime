// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Transactions.Distributed;

namespace System.Transactions
{
    // InternalTransaction
    //
    // This class holds the state and all data common to a transaction instance
    internal sealed class InternalTransaction : IDisposable
    {
        // This variable manages the state of the transaction it should be one of the
        // static elements of TransactionState derived from TransactionState.
        private TransactionState? _transactionState;

        internal TransactionState? State
        {
            get { return _transactionState; }
            set { _transactionState = value; }
        }

        // This variable holds the state that the transaction will promote to.  By
        // default it uses the straight forward TransactionStatePromoted.  If the
        // transaction has a promotable single phase enlistment however it must use
        // a different state so that it is promoted correctly.
        internal TransactionState _promoteState;

        // The PromoterType for the transaction.
        // This is set when a PSPE enlistment is created via Transaction.EnlistPromotableSinglePhase.
        // It is also set when a transaction promotes without a PSPE enlistment.
        internal Guid _promoterType = Guid.Empty;

        // The promoted token for the transaction.
        // This is set when the transaction is promoted. For an MSDTC transaction, it is the
        // same as the DTC propagation token.
        internal byte[]? promotedToken;

        // This is only used if the promoter type is different than TransactionInterop.PromoterTypeDtc.
        // The promoter is supposed to tell us what the distributed transaction id after promoting it.
        // We store the value here.
        internal Guid _distributedTransactionIdentifierNonMSDTC = Guid.Empty;

#if DEBUG
        // Keep a history of th transaction states
        internal const int MaxStateHist = 20;
        internal readonly TransactionState[] _stateHistory = new TransactionState[MaxStateHist];
        internal int _currentStateHist;
#endif

        // Finalized object see class definition for the use of this object
        internal FinalizedObject? _finalizedObject;

        internal readonly int _transactionHash;
        internal int TransactionHash => _transactionHash;

        internal static int _nextHash;

        // timeout stores a relative timeout for the transaction.  absoluteTimeout stores
        // the actual time in ticks.
        private readonly long _absoluteTimeout;
        internal long AbsoluteTimeout => _absoluteTimeout;

        // record the current number of ticks active when the transaction is created.
        private long _creationTime;
        internal long CreationTime
        {
            get { return _creationTime; }
            set { _creationTime = value; }
        }

        // The goal for the LTM is to only allocate as few heap objects as possible for a given
        // transaction and all of its enlistments.  To accomplish this, enlistment objects are
        // held in system arrays.  The transaction contains one enlistment for the single durable
        // enlistment it can handle and a small array of volatile enlistments.  If the number of
        // enlistments for a given transaction exceeds the capacity of the current array a new
        // larger array will be created and the contents of the old array will be copied into it.
        // Heuristic data based on TransactionType can be created to avoid this sort of copy
        // operation repeatedly for a given type of transaction.  So if a transaction of a specific
        // type continually causes the array size to be increased the LTM could start
        // allocating a larger array initially for transactions of that type.
        internal InternalEnlistment? _durableEnlistment;
        internal VolatileEnlistmentSet _phase0Volatiles;
        internal VolatileEnlistmentSet _phase1Volatiles;

        // This member stores the number of phase 0 volatiles for the last wave
        internal int _phase0VolatileWaveCount;

        // These members are used for promoted waves of dependent blocking clones.  The Ltm
        // does not register individually for each blocking clone created in phase 0.  Instead
        // it multiplexes a single phase 0 blocking clone only created after phase 0 has started.
        internal DistributedDependentTransaction? _phase0WaveDependentClone;
        internal int _phase0WaveDependentCloneCount;

        // These members are used for keeping track of aborting dependent clones if we promote
        // BEFORE we get an aborting dependent clone or a Ph1 volatile enlistment.  If we
        // promote before we get either of these, then we never create a Ph1 volatile enlistment
        // on the distributed TM.  If we promote AFTER an aborting dependent clone or Ph1 volatile
        // enlistment is created, then we create a Ph1 volatile enlistment on the distributed TM
        // as part of promotion, so these won't be used.  In that case, the Ph1 volatile enlistment
        // on the distributed TM takes care of checking to make sure all the aborting dependent
        // clones have completed as part of its Prepare processing.  These are used in conjunction with
        // phase1volatiles.dependentclones.
        internal DistributedDependentTransaction? _abortingDependentClone;
        internal int _abortingDependentCloneCount;

        // When the size of the volatile enlistment array grows increase it by this amount.
        internal const int VolatileArrayIncrement = 8;

        // Data maintained for TransactionTable participation
        internal Bucket? _tableBucket;
        internal int _bucketIndex;

        // Delegate to fire on transaction completion
        internal TransactionCompletedEventHandler? _transactionCompletedDelegate;

        // If this transaction get's promoted keep a reference to the promoted transaction
        private DistributedTransaction? _promotedTransaction;
        internal DistributedTransaction? PromotedTransaction
        {
            get { return _promotedTransaction; }
            set
            {
                Debug.Assert(_promotedTransaction == null, "A transaction can only be promoted once!");
                _promotedTransaction = value;
            }
        }

        // If there was an exception that happened during promotion save that exception so that it
        // can be used as an inner exception to the transaciton aborted exception.
        internal Exception? _innerException;

        // Note the number of Transaction objects supported by this object
        internal int _cloneCount;

        // The number of enlistments on this transaction.
        internal int _enlistmentCount;

        // Double-checked locking pattern requires volatile for read/write synchronization
        // Manual Reset event for IAsyncResult support
        internal volatile ManualResetEvent? _asyncResultEvent;

        // Store the callback and state for the caller of BeginCommit
        internal bool _asyncCommit;
        internal AsyncCallback? _asyncCallback;
        internal object? _asyncState;

        // Flag to indicate if we need to be pulsed for tx completion
        internal bool _needPulse;

        // Store the transaction information object
        internal TransactionInformation? _transactionInformation;

        // Store a reference to the owning Committable Transaction
        internal readonly CommittableTransaction? _committableTransaction;

        // Store a reference to the outcome source
        internal readonly Transaction _outcomeSource;

        // Object for synchronizing access to the entire class( avoiding lock( typeof( ... )) )
        private static object? s_classSyncObject;

        internal Guid DistributedTxId => State!.get_Identifier(this);

        private static string? s_instanceIdentifier;
        internal static string InstanceIdentifier =>
            LazyInitializer.EnsureInitialized(ref s_instanceIdentifier, ref s_classSyncObject, () => $"{Guid.NewGuid()}:");

        // Double-checked locking pattern requires volatile for read/write synchronization
        private volatile bool _traceIdentifierInited;

        // The trace identifier for the internal transaction.
        private TransactionTraceIdentifier _traceIdentifier;
        internal TransactionTraceIdentifier TransactionTraceId
        {
            get
            {
                if (!_traceIdentifierInited)
                {
                    lock (this)
                    {
                        if (!_traceIdentifierInited)
                        {
                            TransactionTraceIdentifier temp = new TransactionTraceIdentifier(
                                string.Create(CultureInfo.InvariantCulture, $"{InstanceIdentifier}{_transactionHash}"),
                                0);
                            _traceIdentifier = temp;
                            _traceIdentifierInited = true;
                        }
                    }
                }
                return _traceIdentifier;
            }
        }

        internal ITransactionPromoter? _promoter;

        // This member is used to allow a PSPE enlistment to call Transaction.PSPEPromoteAndConvertToEnlistDurable when it is
        // asked to promote a transaction. The value is set to true in TransactionStatePSPEOperation.PSPEPromote before the
        // Promote call is made and set back to false after the call returns (or an exception is thrown). The value is
        // checked for true in TransactionStatePSPEOperation.PSPEPromoteAndConvertToEnlistDurable to make sure the transaction
        // is in the process of promoting via a PSPE enlistment.
        internal bool _attemptingPSPEPromote;

        // This is called from TransactionStatePromoted.EnterState. We assume we are promoting to MSDTC.
        internal void SetPromoterTypeToMSDTC()
        {
            // The promoter type should either not yet be set or should already be TransactionInterop.PromoterTypeDtc in this case.
            if ((_promoterType != Guid.Empty) && (_promoterType != TransactionInterop.PromoterTypeDtc))
            {
                throw new InvalidOperationException(SR.PromoterTypeInvalid);
            }
            _promoterType = TransactionInterop.PromoterTypeDtc;
        }

        // Throws a TransactionPromotionException if the promoterType is NOT
        // Guid.Empty AND NOT TransactionInterop.PromoterTypeDtc.
        internal void ThrowIfPromoterTypeIsNotMSDTC()
        {
            if ((_promoterType != Guid.Empty) && (_promoterType != TransactionInterop.PromoterTypeDtc))
            {
                throw new TransactionPromotionException(SR.Format(SR.PromoterTypeUnrecognized, _promoterType.ToString()), _innerException);
            }
        }

        // Construct an internal transaction
        internal InternalTransaction(TimeSpan timeout, CommittableTransaction committableTransaction)
        {
            // Calculate the absolute timeout for this transaction
            _absoluteTimeout = TransactionManager.TransactionTable.TimeoutTicks(timeout);

            // Start the transaction off as active
            TransactionState.TransactionStateActive.EnterState(this);

            // Until otherwise noted this transaction uses normal promotion.
            _promoteState = TransactionState.TransactionStatePromoted;

            // Keep a reference to the commitable transaction
            _committableTransaction = committableTransaction;
            _outcomeSource = committableTransaction;

            // Initialize the hash
            _transactionHash = TransactionManager.TransactionTable.Add(this);
        }

        // Construct an internal transaction
        internal InternalTransaction(Transaction outcomeSource, DistributedTransaction distributedTx)
        {
            _promotedTransaction = distributedTx;

            _absoluteTimeout = long.MaxValue;

            // Store the initial creater as it will be the source of outcome events
            _outcomeSource = outcomeSource;

            // Initialize the hash
            _transactionHash = TransactionManager.TransactionTable.Add(this);

            // Start the transaction off as active
            TransactionState.TransactionStateNonCommittablePromoted.EnterState(this);

            // Until otherwise noted this transaction uses normal promotion.
            _promoteState = TransactionState.TransactionStateNonCommittablePromoted;
        }

        // Construct an internal transaction
        internal InternalTransaction(Transaction outcomeSource, ITransactionPromoter promoter)
        {
            _absoluteTimeout = long.MaxValue;

            // Store the initial creater as it will be the source of outcome events
            _outcomeSource = outcomeSource;

            // Initialize the hash
            _transactionHash = TransactionManager.TransactionTable.Add(this);

            // Save the transaction promoter.
            _promoter = promoter;

            // This transaction starts in a special state.
            TransactionState.TransactionStateSubordinateActive.EnterState(this);

            // This transaction promotes through delegation
            _promoteState = TransactionState.TransactionStateDelegatedSubordinate;
        }

        internal static void DistributedTransactionOutcome(InternalTransaction tx, TransactionStatus status)
        {
            FinalizedObject? fo = null;

            lock (tx)
            {
                if (null == tx._innerException)
                {
                    Debug.Assert(tx.PromotedTransaction != null);
                    tx._innerException = tx.PromotedTransaction.InnerException;
                }

                Debug.Assert(tx.State! != null);
                switch (status)
                {
                    case TransactionStatus.Committed:
                        {
                            tx.State.ChangeStatePromotedCommitted(tx);
                            break;
                        }

                    case TransactionStatus.Aborted:
                        {
                            tx.State.ChangeStatePromotedAborted(tx);
                            break;
                        }

                    case TransactionStatus.InDoubt:
                        {
                            tx.State.InDoubtFromDtc(tx);
                            break;
                        }

                    default:
                        {
                            Debug.Fail("InternalTransaction.DistributedTransactionOutcome - Unexpected TransactionStatus");
                            TransactionException.CreateInvalidOperationException(TraceSourceType.TraceSourceLtm,
                                "",
                                null,
                                tx.DistributedTxId
                                );
                            break;
                        }
                }

                fo = tx._finalizedObject;
            }

            if (null != fo)
            {
                fo.Dispose();
            }
        }

        #region Outcome Events

        // Signal Waiters anyone waiting for transaction outcome.
        internal void SignalAsyncCompletion()
        {
            if (_asyncResultEvent != null)
            {
                _asyncResultEvent.Set();
            }

            if (_asyncCallback != null)
            {
                Monitor.Exit(this); // Don't hold a lock calling user code.
                try
                {
                    Debug.Assert(_committableTransaction != null);
                    _asyncCallback(_committableTransaction);
                }
                finally
                {
                    Monitor.Enter(this);
                }
            }
        }

        // Fire completion to anyone registered for outcome
        internal void FireCompletion()
        {
            TransactionCompletedEventHandler? eventHandlers = _transactionCompletedDelegate;

            if (eventHandlers != null)
            {
                TransactionEventArgs args = new TransactionEventArgs();
                args._transaction = _outcomeSource.InternalClone();

                eventHandlers(args._transaction, args);
            }
        }


        #endregion


        #region IDisposable Members

        public void Dispose()
        {
            if (_promotedTransaction != null)
            {
                // If there is a promoted transaction dispose it.
                _promotedTransaction.Dispose();
            }
        }

        #endregion
    }

    // Finalized Object
    //
    // This object is created if the InternalTransaction needs some kind of finalization.  An
    // InternalTransaction will only need finalization if it is promoted so having a finalizer
    // would only hurt performance for the unpromoted case.  When the Ltm does promote it creates this
    // object which is finalized and will handle the necessary cleanup.
    internal sealed class FinalizedObject : IDisposable
    {
        // Keep the identifier separate.  Since it is a struct it won't be finalized out from under
        // this object.
        private readonly Guid _identifier;

        private readonly InternalTransaction _internalTransaction;

        internal FinalizedObject(InternalTransaction internalTransaction, Guid identifier)
        {
            _internalTransaction = internalTransaction;
            _identifier = identifier;
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                GC.SuppressFinalize(this);
            }

            // We need to remove the entry for the transaction from the static
            // LightweightTransactionManager.PromotedTransactionTable.
            Hashtable promotedTransactionTable = TransactionManager.PromotedTransactionTable;
            lock (promotedTransactionTable)
            {
                WeakReference? weakRef = (WeakReference?)promotedTransactionTable[_identifier];
                if (null != weakRef)
                {
                    if (weakRef.Target != null)
                    {
                        weakRef.Target = null;
                    }
                }

                promotedTransactionTable.Remove(_identifier);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }


        ~FinalizedObject()
        {
            Dispose(false);
        }
    }
}
