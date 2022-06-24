// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
// using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Threading;
using System.Transactions.Diagnostics;

#nullable disable

namespace System.Transactions.Oletx
{
    /// <summary>
    /// A Transaction object represents a single transaction.  It is created by TransactionManager
    /// objects through CreateTransaction or through deserialization.  Alternatively, the static Create
    /// methodis provided, which creates a "default" TransactionManager and requests that it create
    /// a new transaction with default values.  A transaction can only be committed by
    /// the client application that created the transaction.  If a client application wishes to allow
    /// access to the transaction by multiple threads, but wants to prevent those other threads from
    /// committing the transaction, the application can make a "clone" of the transaction.  Transaction
    /// clones have the same capabilities as the original transaction, except for the ability to commit
    /// the transaction.
    /// </summary>
    [Serializable]
    internal class OletxTransaction : ISerializable, IObjectReference
    {
        // We have a strong reference on realOletxTransaction which does the real work
        internal RealOletxTransaction RealOletxTransaction;

        // String that is used as a name for the propagationToken
        // while serializing and deserializing this object
        protected const string PropagationTokenString = "OletxTransactionPropagationToken";

        // When an OletxTransaction is being created via deserialization, this member is
        // filled with the propagation token from the serialization info.  Later, when
        // GetRealObject is called, this array is used to decide whether or not a new
        // transation needs to be created and if so, to create the transaction.
        private byte[] _propagationTokenForDeserialize;

        protected int Disposed;

        // In GetRealObject, we ask LTM if it has a promoted transaction with the same ID.  If it does,
        // we need to remember that transaction because GetRealObject is called twice during
        // deserialization.  In this case, GetRealObject returns the LTM transaction, not this OletxTransaction.
        // The OletxTransaction will get GC'd because there will be no references to it.
        internal Transaction SavedLtmPromotedTransaction;

        private TransactionTraceIdentifier _traceIdentifier = TransactionTraceIdentifier.Empty;

        // Property
        internal RealOletxTransaction RealTransaction
            => RealOletxTransaction;

        internal Guid Identifier
        {
            get
            {
                if (DiagnosticTrace.Verbose)
                {
                    MethodEnteredTraceRecord.Trace(SR.TraceSourceOletx, "OletxTransaction.get_Identifier");
                }
                Guid returnValue = RealOletxTransaction.Identifier;
                if (DiagnosticTrace.Verbose)
                {
                    MethodExitedTraceRecord.Trace(SR.TraceSourceOletx, "OletxTransaction.get_Identifier");
                }
                return returnValue;
            }
        }

        internal Guid DistributedTxId
        {
            get
            {
                Guid returnValue = Guid.Empty;

                if (RealOletxTransaction != null && RealOletxTransaction.InternalTransaction != null)
                {
                    returnValue = RealOletxTransaction.InternalTransaction.DistributedTxId;
                }

                return returnValue;
            }
        }

        internal TransactionStatus Status
        {
            get
            {
                if (DiagnosticTrace.Verbose)
                {
                    MethodEnteredTraceRecord.Trace(SR.TraceSourceOletx, "OletxTransaction.get_Status");
                }
                TransactionStatus returnValue = RealOletxTransaction.Status;
                if (DiagnosticTrace.Verbose)
                {
                    MethodExitedTraceRecord.Trace(SR.TraceSourceOletx, "OletxTransaction.get_Status");
                }
                return returnValue;
            }
        }

        internal Exception InnerException
            => RealOletxTransaction.InnerException;

        internal OletxTransaction(RealOletxTransaction realOletxTransaction)
        {
            RealOletxTransaction = realOletxTransaction;

            // Tell the realOletxTransaction that we are here.
            RealOletxTransaction.OletxTransactionCreated();
        }

        protected OletxTransaction(SerializationInfo serializationInfo, StreamingContext context)
        {
            if (serializationInfo == null)
            {
                throw new ArgumentNullException( "serializationInfo");
            }

            // Simply store the propagation token from the serialization info.  GetRealObject will
            // decide whether or not we will use it.
            _propagationTokenForDeserialize = (byte[])serializationInfo.GetValue(PropagationTokenString, typeof(byte[]));

            if (_propagationTokenForDeserialize.Length < 24)
            {
                throw new ArgumentException(SR.InvalidArgument, "serializationInfo");
            }
        }

        public object GetRealObject(StreamingContext context)
        {
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceOletx, "IObjectReference.GetRealObject");
            }

            if (_propagationTokenForDeserialize == null)
            {
                if (DiagnosticTrace.Critical)
                {
                    InternalErrorTraceRecord.Trace(SR.TraceSourceOletx, SR.UnableToDeserializeTransaction);
                }

                throw TransactionException.Create(SR.UnableToDeserializeTransactionInternalError, null);
            }

            // This may be a second call.  If so, just return.
            if (SavedLtmPromotedTransaction != null)
            {
                if (DiagnosticTrace.Verbose)
                {
                    MethodExitedTraceRecord.Trace(SR.TraceSourceOletx, "IObjectReference.GetRealObject");
                }

                return SavedLtmPromotedTransaction;
            }

            Transaction returnValue = TransactionInterop.GetTransactionFromTransmitterPropagationToken(_propagationTokenForDeserialize);
            Debug.Assert(returnValue != null, "OletxTransaction.GetRealObject - GetTxFromPropToken returned null");

            SavedLtmPromotedTransaction = returnValue;

            if (DiagnosticTrace.Verbose)
            {
                TransactionDeserializedTraceRecord.Trace(
                    SR.TraceSourceOletx,
                    returnValue._internalTransaction.PromotedTransaction.TransactionTraceId);
            }

            if (DiagnosticTrace.Verbose)
            {
                MethodExitedTraceRecord.Trace(SR.TraceSourceOletx, "IObjectReference.GetRealObject");
            }

            return returnValue;
        }

        /// <summary>
        /// Implementation of IDisposable.Dispose. Releases managed, and unmanaged resources
        /// associated with the Transaction object.
        /// </summary>
        internal void Dispose()
        {
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceOletx, "IDisposable.Dispose");
            }

            int localDisposed = Interlocked.CompareExchange(ref Disposed, 1, 0);
            if (localDisposed == 0)
            {
                RealOletxTransaction.OletxTransactionDisposed();
            }
            GC.SuppressFinalize(this);
            if (DiagnosticTrace.Verbose)
            {
                MethodExitedTraceRecord.Trace(SR.TraceSourceOletx, "IDisposable.Dispose");
            }
        }

        // Specific System.Transactions implementation

        /// <summary>
        /// Initiates commit processing of the transaction.  The caller must have created the transaction
        /// as a new transaction through TransactionManager.CreateTransaction or Transaction.Create.
        ///
        /// If the transaction is already aborted due to some other participant making a Rollback call,
        /// the transaction timeout period expiring, or some sort of network failure, an exception will
        /// be raised.
        /// </summary>
        /// <remarks>
        /// Initiates rollback processing of the transaction.  This method can be called on any instance
        /// of a Transaction class, regardless of how the Transaction was obtained.  It is possible for this
        /// method to be called "too late", after the outcome of the transaction has already been determined.
        /// In this case, an exception is raised.
        /// </remarks>
        internal void Rollback()
        {
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceOletx, "OletxTransaction.Rollback");
            }

            if (DiagnosticTrace.Warning)
            {
                TransactionRollbackCalledTraceRecord.Trace(SR.TraceSourceOletx, TransactionTraceId);
            }

            Debug.Assert(Disposed == 0, "OletxTransction object is disposed");

            RealOletxTransaction.Rollback();

            if (DiagnosticTrace.Verbose)
            {
                MethodExitedTraceRecord.Trace(SR.TraceSourceOletx, "OletxTransaction.Rollback");
            }
        }

        internal IPromotedEnlistment EnlistVolatile(
            ISinglePhaseNotificationInternal singlePhaseNotification,
            EnlistmentOptions enlistmentOptions)
        {
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(
                    SR.TraceSourceOletx,
                    "OletxTransaction.EnlistVolatile( ISinglePhaseNotificationInternal )");
            }

            Debug.Assert(singlePhaseNotification != null, "Argument is null");
            Debug.Assert(Disposed == 0, "OletxTransction object is disposed");

            if (RealOletxTransaction == null || RealOletxTransaction.TooLateForEnlistments)
            {
                throw TransactionException.Create(SR.TooLate, null, DistributedTxId);
            }

            IPromotedEnlistment enlistment = RealOletxTransaction.EnlistVolatile(
                singlePhaseNotification,
                enlistmentOptions,
                this);

            if (DiagnosticTrace.Verbose)
            {
                MethodExitedTraceRecord.Trace(
                    SR.TraceSourceOletx,
                    "OletxTransaction.EnlistVolatile( ISinglePhaseNotificationInternal )");
            }

            return enlistment;
        }

        internal IPromotedEnlistment EnlistVolatile(
            IEnlistmentNotificationInternal enlistmentNotification,
            EnlistmentOptions enlistmentOptions)
        {
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceOletx,
                    "OletxTransaction.EnlistVolatile( IEnlistmentNotificationInternal )");
            }

            Debug.Assert(enlistmentNotification != null, "Argument is null");
            Debug.Assert(Disposed == 0, "OletxTransction object is disposed");

            if (RealOletxTransaction == null || RealOletxTransaction.TooLateForEnlistments )
            {
                throw TransactionException.Create(SR.TooLate, null, DistributedTxId);
            }

            IPromotedEnlistment enlistment = RealOletxTransaction.EnlistVolatile(
                enlistmentNotification,
                enlistmentOptions,
                this);

            if (DiagnosticTrace.Verbose)
            {
                MethodExitedTraceRecord.Trace(
                    SR.TraceSourceOletx,
                    "OletxTransaction.EnlistVolatile( IEnlistmentNotificationInternal )");
            }

            return enlistment;
        }

        internal IPromotedEnlistment EnlistDurable(
            Guid resourceManagerIdentifier,
            ISinglePhaseNotificationInternal singlePhaseNotification,
            bool canDoSinglePhase,
            EnlistmentOptions enlistmentOptions)
        {
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(
                    SR.TraceSourceOletx,
                    "OletxTransaction.EnlistDurable( ISinglePhaseNotificationInternal )");
            }

            Debug.Assert(Disposed == 0, "OletxTransction object is disposed");

            if (RealOletxTransaction == null || RealOletxTransaction.TooLateForEnlistments)
            {
                throw TransactionException.Create(SR.TooLate, null, DistributedTxId);
            }

            // get the Oletx TM from the real class
            OletxTransactionManager oletxTM = RealOletxTransaction.OletxTransactionManagerInstance;

            // get the resource manager from the Oletx TM
            OletxResourceManager rm = oletxTM.FindOrRegisterResourceManager(resourceManagerIdentifier);

            // ask the rm to do the durable enlistment
            OletxEnlistment enlistment = rm.EnlistDurable(
                this,
                canDoSinglePhase,
                singlePhaseNotification,
                enlistmentOptions);

            if (DiagnosticTrace.Verbose)
            {
                MethodExitedTraceRecord.Trace(
                    SR.TraceSourceOletx,
                    "OletxTransaction.EnlistDurable( ISinglePhaseNotificationInternal )");
            }

            return enlistment;
        }


        internal OletxDependentTransaction DependentClone(bool delayCommit)
        {
            OletxDependentTransaction dependentClone;

            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceOletx, "OletxTransaction.DependentClone");
            }

            Debug.Assert(Disposed == 0, "OletxTransction object is disposed");

            if (TransactionStatus.Aborted == Status)
            {
                throw TransactionAbortedException.Create(
                    SR.TransactionAborted, RealOletxTransaction.InnerException, DistributedTxId);
            }
            if (TransactionStatus.InDoubt == Status)
            {
                throw TransactionInDoubtException.Create(
                    SR.TransactionIndoubt, RealOletxTransaction.InnerException, DistributedTxId);
            }
            if (TransactionStatus.Active != Status)
            {
                throw TransactionException.Create(SR.TransactionAlreadyOver, null, DistributedTxId);
            }

            dependentClone = new OletxDependentTransaction(RealOletxTransaction, delayCommit);

            if (DiagnosticTrace.Verbose)
            {
                MethodExitedTraceRecord.Trace(SR.TraceSourceOletx, "OletxTransaction.DependentClone");
            }

            return dependentClone;

        }

        internal TransactionTraceIdentifier TransactionTraceId
        {
            get
            {
                if (_traceIdentifier == TransactionTraceIdentifier.Empty)
                {
                    lock (RealOletxTransaction)
                    {
                        if (_traceIdentifier == TransactionTraceIdentifier.Empty)
                        {
                            try
                            {
                                TransactionTraceIdentifier temp = new(RealOletxTransaction.Identifier.ToString(), 0);
                                Thread.MemoryBarrier();
                                _traceIdentifier = temp;
                            }
                            catch (TransactionException ex)
                            {
                                // realOletxTransaction.Identifier throws a TransactionException if it can't determine the guid of the
                                // transaction because the transaction was already committed or aborted before the RealOletxTransaction was
                                // created.  If that happens, we don't want to throw just because we are trying to trace.  So just use
                                // the TransactionTraceIdentifier.Empty.
                                if (DiagnosticTrace.Verbose)
                                {
                                    ExceptionConsumedTraceRecord.Trace(SR.TraceSourceOletx, ex);
                                }
                            }

                        }
                    }
                }
                return _traceIdentifier;
            }
        }

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public void GetObjectData(SerializationInfo serializationInfo, StreamingContext context)
        {
            if (serializationInfo == null)
            {
                throw new ArgumentNullException( "serializationInfo");
            }

            byte[] propagationToken;

            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceOletx, "OletxTransaction.GetObjectData");
            }

            Debug.Assert(Disposed == 0, "OletxTransction object is disposed");

            propagationToken = TransactionInterop.GetTransmitterPropagationToken(this);

            serializationInfo.SetType(typeof(OletxTransaction));
            serializationInfo.AddValue(PropagationTokenString, propagationToken);

            if (DiagnosticTrace.Information)
            {
                TransactionSerializedTraceRecord.Trace(SR.TraceSourceOletx, TransactionTraceId);
            }

            if (DiagnosticTrace.Verbose)
            {
                MethodExitedTraceRecord.Trace(SR.TraceSourceOletx, "OletxTransaction.GetObjectData");
            }
        }

        public virtual IsolationLevel IsolationLevel
            => RealOletxTransaction.TransactionIsolationLevel;
    }

    // Internal class used by OletxTransaction class which is public
    internal class RealOletxTransaction
    {
        // Transaction manager
        internal OletxTransactionManager OletxTransactionManagerInstance { get; }

        private ITransactionShim _transactionShim;

        // guid related to transaction
        internal Guid TxGuid { get; private set; }

        // Isolation level of the transaction
        internal IsolationLevel TransactionIsolationLevel { get; private set; }

        // Record the exception that caused the transaction to abort.
        internal Exception InnerException;

        // Store status
        internal TransactionStatus Status { get; private set; }

        // This is the count of undisposed OletxTransaction objects that reference
        // this RealOletxTransaction.  This is incremented when an OletxTransaction is created
        // and decremented when OletxTransactionDisposed is
        // called.  When it is decremented to zero, the transactionShim
        // field is "released", thus releasing the unmanged proxy interface
        // pointer.
        private int _undisposedOletxTransactionCount;

        // The list of containers for phase0 volatile enlistment multiplexing so we only enlist with the proxy once per wave.
        // The last one on the list is the "current" one.
        internal ArrayList Phase0EnlistVolatilementContainerList;

        // The container for phase1 volatile enlistment multiplexing so we only enlist with the proxy once.
        internal OletxPhase1VolatileEnlistmentContainer Phase1EnlistVolatilementContainer;

        // Used to get outcomes of transactions with a voter.
        private OutcomeEnlistment _outcomeEnlistment;

        // This is a count of volatile and Phase0 durable enlistments on this transaction that have not yet voted.
        // This is incremented when an enlistment is made and decremented when the
        // enlistment votes.  It is checked in Rollback.  If the count is greater than 0,
        // then the doomed field is set to true and the Rollback is allowed.  If the count
        // is zero in Rollback, the rollback is rejected with a "too late" exception.
        // All checking and modification of this field needs to be done under a lock( this ).
        private int _undecidedEnlistmentCount;

        // If true, indicates that the transaction should NOT commit.  This is set to
        // true if Rollback is called when there are outstanding enlistments.  This is
        // checked when enlistments vote Prepared.  If true, then the enlistment's vote
        // is turned into a ForceRollback.  All checking and modification of this field
        // needs to be done under a lock (this).
        internal bool Doomed { get; private set; }

        // This property is used to allocate enlistment identifiers for enlistment trace identifiers.
        // It is only incremented when a new enlistment is created for this instance of RealOletxTransaction.
        // Enlistments on all clones of this Real transaction use this value.
        internal int _enlistmentCount;

        private DateTime _creationTime;
        private DateTime _lastStateChangeTime;
        private TransactionTraceIdentifier _traceIdentifier = TransactionTraceIdentifier.Empty;

        // This field is set directly from the OletxCommittableTransaction constructor.  It will be null
        // for non-root RealOletxTransactions.
        internal OletxCommittableTransaction CommittableTransaction;

        // This is an internal OletxTransaction.  It is created as part of the RealOletxTransaction constructor.
        // It is used by the DependentCloneEnlistments when creating their volatile enlistments.
        internal OletxTransaction InternalClone;

        // This is set initialized to false.  It is set to true when the OletxPhase1VolatileContainer gets a VoteRequest or
        // when any OletxEnlistment attached to this transaction gets a PrepareRequest.  At that point, it is too late for any
        // more enlistments.
        internal bool TooLateForEnlistments { get; set; }

        // This is the InternalTransaction that instigated creation of this RealOletxTransaction.  When we get the outcome
        // of the transaction, we use this to notify the InternalTransaction of the outcome.  We do this to avoid the LTM
        // always creating a volatile enlistment just to get the outcome.
        internal InternalTransaction InternalTransaction { get; set; }

        internal Guid Identifier
        {
            get
            {
                // The txGuid will be empty if the oletx transaction was already committed or aborted when we
                // tried to create the RealOletxTransaction.  We still allow creation of the RealOletxTransaction
                // for COM+ interop purposes, but we can't get the guid or the status of the transaction.
                if (TxGuid.Equals( Guid.Empty))
                {
                    throw TransactionException.Create(SR.GetResourceString(SR.CannotGetTransactionIdentifier), null);
                }

                return TxGuid;
            }
        }

        internal Guid DistributedTxId
        {
            get
            {
                Guid returnValue = Guid.Empty;

                if (InternalTransaction != null)
                {
                    returnValue = InternalTransaction.DistributedTxId;
                }

                return returnValue;
            }
        }

        internal void IncrementUndecidedEnlistments()
        {
            // Avoid taking a lock on the transaction here.  Decrement
            // will be called by a thread owning a lock on enlistment
            // containers.  When creating new enlistments the transaction
            // will attempt to get a lock on the container when it
            // already holds a lock on the transaction.  This can result
            // in a deadlock.
            Interlocked.Increment(ref _undecidedEnlistmentCount);
        }

        internal void DecrementUndecidedEnlistments()
        {
            // Avoid taking a lock on the transaction here.  Decrement
            // will be called by a thread owning a lock on enlistment
            // containers.  When creating new enlistments the transaction
            // will attempt to get a lock on the container when it
            // already holds a lock on the transaction.  This can result
            // in a deadlock.
            Interlocked.Decrement(ref _undecidedEnlistmentCount);
        }

        internal int UndecidedEnlistments
            => _undecidedEnlistmentCount;

        internal ITransactionShim TransactionShim
        {
            get
            {
                ITransactionShim shim = _transactionShim;
                if (shim == null)
                {
                    throw TransactionInDoubtException.Create(SR.TransactionIndoubt, null, DistributedTxId);
                }

                return shim;
            }
        }

        // Common constructor used by all types of constructors
        // Create a clean and fresh transaction.
        internal RealOletxTransaction(OletxTransactionManager transactionManager,
            ITransactionShim transactionShim,
            OutcomeEnlistment outcomeEnlistment,
            Guid identifier,
            OletxTransactionIsolationLevel oletxIsoLevel,
            bool isRoot )
        {
            bool successful = false;

            try
            {
                // initialize the member fields
                OletxTransactionManagerInstance = transactionManager;
                _transactionShim = transactionShim;
                _outcomeEnlistment = outcomeEnlistment;
                TxGuid = identifier;
                TransactionIsolationLevel = OletxTransactionManager.ConvertIsolationLevelFromProxyValue(oletxIsoLevel);
                Status = TransactionStatus.Active;
                _undisposedOletxTransactionCount = 0;
                Phase0EnlistVolatilementContainerList = null;
                Phase1EnlistVolatilementContainer = null;
                TooLateForEnlistments = false;
                InternalTransaction = null;

                _creationTime = DateTime.UtcNow;
                _lastStateChangeTime = _creationTime;

                // Connect this object with the OutcomeEnlistment.
                InternalClone = new OletxTransaction( this );

                // We have have been created without an outcome enlistment if it was too late to create
                // a clone from the ITransactionNative that we were created from.
                if (_outcomeEnlistment != null)
                {
                    _outcomeEnlistment.SetRealTransaction(this);
                }
                else
                {
                    Status = TransactionStatus.InDoubt;
                }

                if (DiagnosticTrace.HaveListeners)
                {
                    DiagnosticTrace.TraceTransfer(TxGuid);
                }

                successful = true;
            }
            finally
            {
                if (!successful)
                {
                    if (_outcomeEnlistment != null)
                    {
                        _outcomeEnlistment.UnregisterOutcomeCallback();
                        _outcomeEnlistment = null;
                    }
                }
            }
        }

        internal OletxVolatileEnlistmentContainer AddDependentClone( bool delayCommit )
        {
            IPhase0EnlistmentShim phase0Shim = null;
            IVoterBallotShim voterShim = null;
            bool needVoterEnlistment = false;
            bool needPhase0Enlistment = false;
            OletxVolatileEnlistmentContainer returnValue = null;
            OletxPhase0VolatileEnlistmentContainer localPhase0VolatileContainer = null;
            OletxPhase1VolatileEnlistmentContainer localPhase1VolatileContainer = null;
            bool enlistmentSucceeded = false;
            bool phase0ContainerLockAcquired = false;

            IntPtr phase0Handle = IntPtr.Zero;

            // Yes, we are talking to the proxy while holding the lock on the RealOletxTransaction.
            // If we don't then things get real sticky with other threads allocating containers.
            // We only do this the first time we get a depenent clone of a given type (delay vs. non-delay).
            // After that, we don't create a new container, except for Phase0 if we need to create one
            // for a second wave.
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                lock (this)
                {
                    if (delayCommit)
                    {
                        if (Phase0EnlistVolatilementContainerList == null)
                        {
                            // Not using a MemoryBarrier because all access to this member variable is under a lock of the
                            // object.
                            Phase0EnlistVolatilementContainerList = new ArrayList(1);
                        }
                        // We may have failed the proxy enlistment for the first container, but we would have
                        // allocated the list.  That is why we have this check here.
                        if (Phase0EnlistVolatilementContainerList.Count == 0)
                        {
                            localPhase0VolatileContainer = new OletxPhase0VolatileEnlistmentContainer(this);
                            needPhase0Enlistment = true;
                        }
                        else
                        {
                            localPhase0VolatileContainer = Phase0EnlistVolatilementContainerList[^1] as OletxPhase0VolatileEnlistmentContainer;

                            if (localPhase0VolatileContainer != null)
                            {
                                //CSDMain 91509 - We now synchronize this call with the shim notification trying to call Phase0Request on this container
                                TakeContainerLock(localPhase0VolatileContainer, ref phase0ContainerLockAcquired);
                            }

                            if (!localPhase0VolatileContainer.NewEnlistmentsAllowed)
                            {
                                //It is OK to release the lock at this time because we are creating a new container that has not yet
                                //been enlisted with DTC. So there is no race to worry about
                                ReleaseContainerLock(localPhase0VolatileContainer, ref phase0ContainerLockAcquired);

                                localPhase0VolatileContainer = new OletxPhase0VolatileEnlistmentContainer( this );
                                needPhase0Enlistment = true;
                            }
                            else
                            {
                                needPhase0Enlistment = false;
                            }
                        }

                        if (needPhase0Enlistment)
                        {
                            // We need to create a VoterNotifyShim if native threads are not allowed to enter managed code.
                            phase0Handle = HandleTable.AllocHandle(localPhase0VolatileContainer);
                        }
                    }

                    else // ! delayCommit
                    {
                        if (Phase1EnlistVolatilementContainer == null)
                        {
                            localPhase1VolatileContainer = new OletxPhase1VolatileEnlistmentContainer(this);
                            needVoterEnlistment = true;

                            // We need to create a VoterNotifyShim.
                            localPhase1VolatileContainer.VoterHandle =
                                HandleTable.AllocHandle(localPhase1VolatileContainer);
                        }
                        else
                        {
                            needVoterEnlistment = false;
                            localPhase1VolatileContainer = Phase1EnlistVolatilementContainer;
                        }
                    }

                    try
                    {
                        //At this point, we definitely need the lock on the phase0 container so that it doesnt race with shim notifications from unmanaged code
                        //corrupting state while we are in the middle of an AddDependentClone processing
                        if (localPhase0VolatileContainer != null)
                        {
                            TakeContainerLock(localPhase0VolatileContainer, ref phase0ContainerLockAcquired);
                        }

                        // If enlistDuringPrepareRequired is true, we need to ask the proxy to create a Phase0 enlistment.
                        if (needPhase0Enlistment)
                        {
                            // We need to use shims if native threads are not allowed to enter managed code.
                            _transactionShim.Phase0Enlist(phase0Handle, out phase0Shim);
                            localPhase0VolatileContainer.Phase0EnlistmentShim = phase0Shim;
                        }

                        if (needVoterEnlistment)
                        {
                            // We need to use shims if native threads are not allowed to enter managed code.
                            OletxTransactionManagerInstance.DtcTransactionManagerLock.AcquireReaderLock(-1);
                            try
                            {
                                _transactionShim.CreateVoter(localPhase1VolatileContainer.VoterHandle, out voterShim);

                                enlistmentSucceeded = true;
                            }
                            finally
                            {
                                OletxTransactionManagerInstance.DtcTransactionManagerLock.ReleaseReaderLock();
                            }

                            localPhase1VolatileContainer.VoterBallotShim = voterShim;
                        }

                        if (delayCommit)
                        {
                            // if we needed a Phase0 enlistment, we need to add the container to the
                            // list.
                            if (needPhase0Enlistment)
                            {
                                Phase0EnlistVolatilementContainerList.Add(localPhase0VolatileContainer);
                            }
                            localPhase0VolatileContainer.AddDependentClone();
                            returnValue = localPhase0VolatileContainer;
                        }
                        else
                        {
                            // If we needed a voter enlistment, we need to save the container as THE
                            // phase1 container for this transaction.
                            if (needVoterEnlistment)
                            {
                                Debug.Assert(Phase1EnlistVolatilementContainer == null,
                                    "RealOletxTransaction.AddDependentClone - phase1VolContainer not null when expected" );
                                Phase1EnlistVolatilementContainer = localPhase1VolatileContainer;
                            }
                            localPhase1VolatileContainer.AddDependentClone();
                            returnValue = localPhase1VolatileContainer;
                        }

                    }
                    catch (COMException comException)
                    {
                        OletxTransactionManager.ProxyException(comException);
                        throw;
                    }
                }
            }
            finally
            {
                //First release the lock on the phase 0 container if it was acquired. Any work on localPhase0VolatileContainer
                //that needs its state to be consistent while processing should do so before this statement is executed.
                if (localPhase0VolatileContainer != null)
                {
                    ReleaseContainerLock(localPhase0VolatileContainer, ref phase0ContainerLockAcquired);
                }

                if (phase0Handle != IntPtr.Zero && localPhase0VolatileContainer.Phase0EnlistmentShim == null)
                {
                    HandleTable.FreeHandle(phase0Handle);
                }

                if (!enlistmentSucceeded &&
                    localPhase1VolatileContainer != null &&
                    localPhase1VolatileContainer.VoterHandle != IntPtr.Zero &&
                    needVoterEnlistment)
                {
                    HandleTable.FreeHandle(localPhase1VolatileContainer.VoterHandle);
                }
            }
            return returnValue;

        }

        void ReleaseContainerLock(OletxPhase0VolatileEnlistmentContainer localPhase0VolatileContainer, ref bool phase0ContainerLockAcquired)
        {
            if (phase0ContainerLockAcquired)
            {
                Monitor.Exit(localPhase0VolatileContainer);
                phase0ContainerLockAcquired = false;
            }
        }

        void TakeContainerLock(OletxPhase0VolatileEnlistmentContainer localPhase0VolatileContainer, ref bool phase0ContainerLockAcquired)
        {
            if (!phase0ContainerLockAcquired)
            {
#pragma warning disable 0618
                //@TODO: This overload of Monitor.Enter is obsolete.  Please change this to use Monitor.Enter(ref bool), and remove the pragmas   -- ericeil
                Monitor.Enter(localPhase0VolatileContainer);
#pragma warning restore 0618
                phase0ContainerLockAcquired = true;
            }
        }

        internal IPromotedEnlistment CommonEnlistVolatile(
            IEnlistmentNotificationInternal enlistmentNotification,
            EnlistmentOptions enlistmentOptions,
            OletxTransaction oletxTransaction)
        {
            OletxVolatileEnlistment enlistment = null;
            bool needVoterEnlistment = false;
            bool needPhase0Enlistment = false;
            OletxPhase0VolatileEnlistmentContainer localPhase0VolatileContainer = null;
            OletxPhase1VolatileEnlistmentContainer localPhase1VolatileContainer = null;
            IntPtr phase0Handle = IntPtr.Zero;
            IVoterBallotShim voterShim = null;
            IPhase0EnlistmentShim phase0Shim = null;
            bool enlistmentSucceeded = false;

            // Yes, we are talking to the proxy while holding the lock on the RealOletxTransaction.
            // If we don't then things get real sticky with other threads allocating containers.
            // We only do this the first time we get a depenent clone of a given type (delay vs. non-delay).
            // After that, we don't create a new container, except for Phase0 if we need to create one
            // for a second wave.
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                lock (this)
                {
                    enlistment = new OletxVolatileEnlistment(
                        enlistmentNotification,
                        enlistmentOptions,
                        oletxTransaction);

                    if ((enlistmentOptions & EnlistmentOptions.EnlistDuringPrepareRequired) != 0)
                    {
                        if (Phase0EnlistVolatilementContainerList == null)
                        {
                            // Not using a MemoryBarrier because all access to this member variable is done when holding
                            // a lock on the object.
                            Phase0EnlistVolatilementContainerList = new ArrayList(1);
                        }
                        // We may have failed the proxy enlistment for the first container, but we would have
                        // allocated the list.  That is why we have this check here.
                        if (Phase0EnlistVolatilementContainerList.Count == 0)
                        {
                            localPhase0VolatileContainer = new OletxPhase0VolatileEnlistmentContainer(this);
                            needPhase0Enlistment = true;
                        }
                        else
                        {
                            localPhase0VolatileContainer = Phase0EnlistVolatilementContainerList[^1] as OletxPhase0VolatileEnlistmentContainer;
                            if (!localPhase0VolatileContainer.NewEnlistmentsAllowed)
                            {
                                localPhase0VolatileContainer = new OletxPhase0VolatileEnlistmentContainer(this);
                                needPhase0Enlistment = true;
                            }
                            else
                            {
                                needPhase0Enlistment = false;
                            }
                        }

                        if (needPhase0Enlistment)
                        {
                            // We need to create a VoterNotifyShim if native threads are not allowed to enter managed code.
                            phase0Handle = HandleTable.AllocHandle(localPhase0VolatileContainer);
                        }
                    }
                    else  // not EDPR = TRUE - may need a voter...
                    {
                        if (Phase1EnlistVolatilementContainer == null)
                        {
                            needVoterEnlistment = true;
                            localPhase1VolatileContainer = new OletxPhase1VolatileEnlistmentContainer(this);

                            // We need to create a VoterNotifyShim.
                            localPhase1VolatileContainer.VoterHandle =
                                HandleTable.AllocHandle(localPhase1VolatileContainer);
                        }
                        else
                        {
                            needVoterEnlistment = false;
                            localPhase1VolatileContainer = Phase1EnlistVolatilementContainer;
                        }
                    }


                    try
                    {
                        // If enlistDuringPrepareRequired is true, we need to ask the proxy to create a Phase0 enlistment.
                        if (needPhase0Enlistment)
                        {
                            lock (localPhase0VolatileContainer)
                            {
                                _transactionShim.Phase0Enlist(phase0Handle, out phase0Shim);

                                localPhase0VolatileContainer.Phase0EnlistmentShim = phase0Shim;
                            }
                        }

                        if (needVoterEnlistment)
                        {
                            _transactionShim.CreateVoter(localPhase1VolatileContainer.VoterHandle, out voterShim);

                            enlistmentSucceeded = true;
                            localPhase1VolatileContainer.VoterBallotShim = voterShim;
                        }

                        if ((enlistmentOptions & EnlistmentOptions.EnlistDuringPrepareRequired) != 0)
                        {
                            localPhase0VolatileContainer.AddEnlistment(enlistment);
                            if (needPhase0Enlistment)
                            {
                                Phase0EnlistVolatilementContainerList.Add( localPhase0VolatileContainer);
                            }
                        }
                        else
                        {
                            localPhase1VolatileContainer.AddEnlistment(enlistment);

                            if (needVoterEnlistment)
                            {
                                Debug.Assert(Phase1EnlistVolatilementContainer == null,
                                    "RealOletxTransaction.CommonEnlistVolatile - phase1VolContainer not null when expected.");
                                Phase1EnlistVolatilementContainer = localPhase1VolatileContainer;
                            }
                        }
                    }
                    catch (COMException comException)
                    {
                        OletxTransactionManager.ProxyException(comException);
                        throw;
                    }
                }
            }
            finally
            {
                if (phase0Handle != IntPtr.Zero && localPhase0VolatileContainer.Phase0EnlistmentShim == null)
                {
                    HandleTable.FreeHandle(phase0Handle);
                }

                if (!enlistmentSucceeded &&
                    localPhase1VolatileContainer != null&&
                    localPhase1VolatileContainer.VoterHandle != IntPtr.Zero &&
                    needVoterEnlistment)
                {
                    HandleTable.FreeHandle(localPhase1VolatileContainer.VoterHandle);
                }
            }

            return enlistment;
        }

        internal IPromotedEnlistment EnlistVolatile(
            ISinglePhaseNotificationInternal enlistmentNotification,
            EnlistmentOptions enlistmentOptions,
            OletxTransaction oletxTransaction)
            => CommonEnlistVolatile(
                enlistmentNotification,
                enlistmentOptions,
                oletxTransaction);

        internal IPromotedEnlistment EnlistVolatile(
            IEnlistmentNotificationInternal enlistmentNotification,
            EnlistmentOptions enlistmentOptions,
            OletxTransaction oletxTransaction)
            => CommonEnlistVolatile(
                enlistmentNotification,
                enlistmentOptions,
                oletxTransaction);

        internal void Commit()
        {
            try
            {
                _transactionShim.Commit();
            }
            catch (COMException comException)
            {
                if (comException.ErrorCode == NativeMethods.XACT_E_ABORTED ||
                    comException.ErrorCode == NativeMethods.XACT_E_INDOUBT)
                {
                    Interlocked.CompareExchange(ref InnerException, comException, null);

                    if (DiagnosticTrace.Verbose)
                    {
                        ExceptionConsumedTraceRecord.Trace(SR.TraceSourceOletx, comException);
                    }
                }
                else if (comException.ErrorCode == NativeMethods.XACT_E_ALREADYINPROGRESS)
                {
                    throw TransactionException.Create(SR.TransactionAlreadyOver, comException);
                }
                else
                {
                    OletxTransactionManager.ProxyException(comException);
                    throw;
                }
            }
        }

        internal void Rollback()
        {
            Guid tempGuid = Guid.Empty;

            lock (this)
            {
                // if status is not active and not aborted, then throw an exception
                if (TransactionStatus.Aborted != Status &&
                    TransactionStatus.Active != Status)
                {
                    throw TransactionException.Create(SR.TransactionAlreadyOver, null, DistributedTxId);
                }

                // If the transaciton is already aborted, we can get out now.  Calling Rollback on an already aborted transaction
                // is legal.
                if (TransactionStatus.Aborted == Status)
                {
                    return;
                }

                // If there are still undecided enlistments, we can doom the transaction.
                // We can safely make this check because we ALWAYS have a Phase1 Volatile enlistment to
                // get the outcome.  If we didn't have that enlistment, we would not be able to do this
                // because not all instances of RealOletxTransaction would have enlistments.
                if (_undecidedEnlistmentCount > 0)
                {
                    Doomed = true;
                }
                else if (TooLateForEnlistments )
                {
                    // It's too late for rollback to be called here.
                    throw TransactionException.Create(SR.TransactionAlreadyOver, null, DistributedTxId);
                }

                // Tell the volatile enlistment containers to vote no now if they have outstanding
                // notifications.
                if (Phase0EnlistVolatilementContainerList != null)
                {
                    foreach (OletxPhase0VolatileEnlistmentContainer phase0VolatileContainer in Phase0EnlistVolatilementContainerList)
                    {
                        phase0VolatileContainer.RollbackFromTransaction();
                    }
                }
                if (Phase1EnlistVolatilementContainer != null)
                {
                    Phase1EnlistVolatilementContainer.RollbackFromTransaction();
                }
            }

            try
            {
                _transactionShim.Abort();
            }
            catch (COMException comException)
            {
                // If the ErrorCode is XACT_E_ALREADYINPROGRESS and the transaciton is already doomed, we must be
                // the root transaction and we have already called Commit - ignore the exception.  The
                // Rollback is allowed and one of the enlistments that hasn't voted yet will make sure it is
                // aborted.
                if (comException.ErrorCode == NativeMethods.XACT_E_ALREADYINPROGRESS)
                {
                    if (Doomed)
                    {
                        if (DiagnosticTrace.Verbose)
                        {
                            ExceptionConsumedTraceRecord.Trace(SR.TraceSourceOletx, comException);
                        }
                    }
                    else
                    {
                        throw TransactionException.Create(SR.TransactionAlreadyOver, comException, DistributedTxId);
                    }
                }
                else
                {
                    // Otherwise, throw the exception out to the app.
                    OletxTransactionManager.ProxyException(comException);

                    throw;
                }
            }
        }

        internal void OletxTransactionCreated()
            => Interlocked.Increment(ref _undisposedOletxTransactionCount);

        internal void OletxTransactionDisposed()
        {
            int localCount = Interlocked.Decrement(ref _undisposedOletxTransactionCount);
            Debug.Assert(localCount >= 0, "RealOletxTransction.undisposedOletxTransationCount < 0");
        }

        internal void FireOutcome(TransactionStatus statusArg)
        {
            lock (this)
            {
                if (statusArg == TransactionStatus.Committed)
                {
                    if (DiagnosticTrace.Verbose)
                    {
                        TransactionCommittedTraceRecord.Trace(SR.TraceSourceOletx, TransactionTraceId);
                    }

                    Status = TransactionStatus.Committed;
                }
                else if (statusArg == TransactionStatus.Aborted)
                {
                    if (DiagnosticTrace.Warning)
                    {
                        TransactionAbortedTraceRecord.Trace(SR.TraceSourceOletx, TransactionTraceId);
                    }

                    Status = TransactionStatus.Aborted;
                }
                else
                {
                    if (DiagnosticTrace.Warning)
                    {
                        TransactionInDoubtTraceRecord.Trace(SR.TraceSourceOletx, TransactionTraceId);
                    }

                    Status = TransactionStatus.InDoubt;
                }
            }

            // Let the InternalTransaciton know about the outcome.
            if (InternalTransaction != null)
            {
                InternalTransaction.DistributedTransactionOutcome(InternalTransaction, Status);
            }

        }

        internal TransactionTraceIdentifier TransactionTraceId
        {
            get
            {
                if (TransactionTraceIdentifier.Empty == _traceIdentifier)
                {
                    lock (this)
                    {
                        if (_traceIdentifier == TransactionTraceIdentifier.Empty)
                        {
                            if (TxGuid != Guid.Empty)
                            {
                                TransactionTraceIdentifier temp = new(TxGuid.ToString(), 0);
                                Thread.MemoryBarrier();
                                _traceIdentifier = temp;
                            }
                            else
                            {
                                // We don't have a txGuid if we couldn't determine the guid of the
                                // transaction because the transaction was already committed or aborted before the RealOletxTransaction was
                                // created.  If that happens, we don't want to throw just because we are trying to trace.  So just use the
                                // TransactionTraceIdentifier.Empty.
                            }
                        }
                    }
                }
                return _traceIdentifier;
            }
        }

        internal void TMDown()
        {
            lock (this)
            {
                // Tell the volatile enlistment containers that the TM went down.
                if (Phase0EnlistVolatilementContainerList != null)
                {
                    foreach (OletxPhase0VolatileEnlistmentContainer phase0VolatileContainer in Phase0EnlistVolatilementContainerList)
                    {
                        phase0VolatileContainer.TMDown();
                    }
                }
            }
            // Tell the outcome enlistment the TM went down.  We are doing this outside the lock
            // because this may end up making calls out to user code through enlistments.
            _outcomeEnlistment.TMDown();
        }
    }

    internal sealed class OutcomeEnlistment
    {
        private WeakReference _weakRealTransaction;

        internal Guid TransactionIdentifier { get; private set; }

        private bool _haveIssuedOutcome;

        private TransactionStatus _savedStatus;

        internal OutcomeEnlistment()
        {
            _haveIssuedOutcome = false;
            _savedStatus = TransactionStatus.InDoubt;
        }

        internal void SetRealTransaction( RealOletxTransaction realTx )
        {
            bool localHaveIssuedOutcome = false;
            TransactionStatus localStatus = TransactionStatus.InDoubt;

            lock (this)
            {
                localHaveIssuedOutcome = _haveIssuedOutcome;
                localStatus = _savedStatus;

                // We want to do this while holding the lock.
                if (!localHaveIssuedOutcome)
                {
                    // We don't use MemoryBarrier here because all access to these member variables is done while holding
                    // a lock on the object.

                    // We are going to use a weak reference so the transaction object can get garbage
                    // collected before we receive the outcome.
                    _weakRealTransaction = new WeakReference(realTx);

                    // Save the transaction guid so that the transaction can be removed from the
                    // TransactionTable
                    TransactionIdentifier = realTx.TxGuid;
                }
            }

            // We want to do this outside the lock because we are potentially calling out to user code.
            if (localHaveIssuedOutcome)
            {
                realTx.FireOutcome(localStatus);

                // We may be getting this notification while there are still volatile prepare notifications outstanding.  Tell the
                // container to drive the aborted notification in that case.
                if ( localStatus is TransactionStatus.Aborted or TransactionStatus.InDoubt &&
                   realTx.Phase1EnlistVolatilementContainer != null)
                {
                    realTx.Phase1EnlistVolatilementContainer.OutcomeFromTransaction(localStatus);
                }
            }
        }

        internal void UnregisterOutcomeCallback()
        {
            _weakRealTransaction = null;
        }

        private void InvokeOutcomeFunction(TransactionStatus status)
        {
            WeakReference localTxWeakRef;

            // In the face of TMDown notifications, we may have already issued
            // the outcome of the transaction.
            lock (this)
            {
                if (_haveIssuedOutcome)
                {
                    return;
                }
                _haveIssuedOutcome = true;
                _savedStatus = status;
                localTxWeakRef = _weakRealTransaction;
            }

            // It is possible for the weakRealTransaction member to be null if some exception was thrown
            // during the RealOletxTransaction constructor after the OutcomeEnlistment object was created.
            // In the finally block of the constructor, it calls UnregisterOutcomeCallback, which will
            // null out weakRealTransaction.  If this is the case, there is nothing to do.
            if (localTxWeakRef != null )
            {
                if (localTxWeakRef.Target is RealOletxTransaction realOletxTransaction)
                {
                    realOletxTransaction.FireOutcome(status);

                    // The container list won't be changing on us now because the transaction status has changed such that
                    // new enlistments will not be created.
                    // Tell the Phase0Volatile containers, if any, about the outcome of the transaction.
                    // I am not protecting the access to phase0EnlistVolatilementContainerList with a lock on "this"
                    // because it is too late for these to be allocated anyway.
                    if (realOletxTransaction.Phase0EnlistVolatilementContainerList != null)
                    {
                        foreach (OletxPhase0VolatileEnlistmentContainer phase0VolatileContainer in realOletxTransaction.Phase0EnlistVolatilementContainerList)
                        {
                            phase0VolatileContainer.OutcomeFromTransaction( status );
                        }
                    }

                    // We may be getting this notification while there are still volatile prepare notifications outstanding.  Tell the
                    // container to drive the aborted notification in that case.
                    if ( status is TransactionStatus.Aborted or TransactionStatus.InDoubt &&
                           realOletxTransaction.Phase1EnlistVolatilementContainer != null)
                    {
                        realOletxTransaction.Phase1EnlistVolatilementContainer.OutcomeFromTransaction(status);
                    }
                }

                localTxWeakRef.Target = null;
            }
        }


        //
        // We need to figure out if the transaction is InDoubt as a result of TMDown.  This
        // can happen for a number of reasons.  For instance we have responded prepared
        // to all of our enlistments or we have no enlistments.
        //
        internal bool TransactionIsInDoubt(RealOletxTransaction realTx)
        {
            if (realTx.CommittableTransaction is { CommitCalled: false } )
            {
                // If this is a committable transaction and commit has not been called
                // then we know the outcome.
                return false;
            }

            return realTx.UndecidedEnlistments == 0;
        }

        internal void TMDown()
        {
            // Assume that we don't know because that is the safest answer.
            bool transactionIsInDoubt = true;
            RealOletxTransaction realOletxTransaction = null;
            lock (this)
            {
                if (_weakRealTransaction != null)
                {
                    realOletxTransaction = _weakRealTransaction.Target as RealOletxTransaction;
                }
            }

            if (realOletxTransaction != null)
            {
                lock (realOletxTransaction)
                {
                    transactionIsInDoubt = TransactionIsInDoubt(realOletxTransaction);
                }
            }

            // If we have already voted, then we can't tell what the outcome
            // is.  We do this outside the lock because it may end up invoking user
            // code when it calls into the enlistments later on the stack.
            if (transactionIsInDoubt)
            {
                InDoubt();
            }
            // We have not yet voted, so just say it aborted.
            else
            {
                Aborted();
            }
        }

        #region ITransactionOutcome Members

        public void Committed()
            => InvokeOutcomeFunction(TransactionStatus.Committed);

        public void Aborted()
            => InvokeOutcomeFunction(TransactionStatus.Aborted);

        public void InDoubt()
            => InvokeOutcomeFunction(TransactionStatus.InDoubt);

        #endregion
    }
}
