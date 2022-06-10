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
        internal RealOletxTransaction realOletxTransaction = null;

        // String that is used as a name for the propagationToken
        // while serializing and deserializing this object
        protected const string propagationTokenString = "OletxTransactionPropagationToken";

        // When an OletxTransaction is being created via deserialization, this member is
        // filled with the propagation token from the serialization info.  Later, when
        // GetRealObject is called, this array is used to decide whether or not a new
        // transation needs to be created and if so, to create the transaction.
        private byte[] propagationTokenForDeserialize = null;

        protected int disposed = 0;

        // In GetRealObject, we ask LTM if it has a promoted transaction with the same ID.  If it does,
        // we need to remember that transaction because GetRealObject is called twice during
        // deserialization.  In this case, GetRealObject returns the LTM transaction, not this OletxTransaction.
        // The OletxTransaction will get GC'd because there will be no references to it.
        internal Transaction savedLtmPromotedTransaction = null;

        private TransactionTraceIdentifier traceIdentifier = TransactionTraceIdentifier.Empty;

        // Property
        internal RealOletxTransaction RealTransaction
        {
            get
            {
                return this.realOletxTransaction;
            }
        }

        internal Guid Identifier
        {
            get
            {
                if ( DiagnosticTrace.Verbose )
                {
                    MethodEnteredTraceRecord.Trace( SR.TraceSourceOletx,
                        "OletxTransaction.get_Identifier"
                        );
                }
                Guid returnValue = this.realOletxTransaction.Identifier;
                if ( DiagnosticTrace.Verbose )
                {
                    MethodExitedTraceRecord.Trace( SR.TraceSourceOletx,
                        "OletxTransaction.get_Identifier"
                        );
                }
                return returnValue;
            }
        }

        internal Guid DistributedTxId
        {
            get
            {
                Guid returnValue = Guid.Empty;

                if (this.realOletxTransaction != null && this.realOletxTransaction.InternalTransaction != null)
                {
                    returnValue = this.realOletxTransaction.InternalTransaction.DistributedTxId;
                }
                return returnValue;
            }
        }

        internal System.Transactions.TransactionStatus Status
        {
            get
            {
                if ( DiagnosticTrace.Verbose )
                {
                    MethodEnteredTraceRecord.Trace( SR.TraceSourceOletx,
                        "OletxTransaction.get_Status"
                        );
                }
                TransactionStatus returnValue = this.realOletxTransaction.Status;
                if ( DiagnosticTrace.Verbose )
                {
                    MethodExitedTraceRecord.Trace( SR.TraceSourceOletx,
                        "OletxTransaction.get_Status"
                        );
                }
                return returnValue;
            }
        }

        internal Exception InnerException
        {
            get
            {
                return this.realOletxTransaction.innerException;
            }
        }

        internal OletxTransaction(RealOletxTransaction realOletxTransaction)
        {
            this.realOletxTransaction = realOletxTransaction;

            // Tell the realOletxTransaction that we are here.
            this.realOletxTransaction.OletxTransactionCreated();
        }

        protected OletxTransaction(SerializationInfo serializationInfo, StreamingContext context)
        {
            if (serializationInfo == null)
            {
                throw new ArgumentNullException( "serializationInfo");
            }

            // Simply store the propagation token from the serialization info.  GetRealObject will
            // decide whether or not we will use it.
            propagationTokenForDeserialize = (byte[])serializationInfo.GetValue(propagationTokenString, typeof(byte[]));

            if ( propagationTokenForDeserialize.Length < 24 )
            {
                throw new ArgumentException( SR.InvalidArgument, "serializationInfo" );
            }

        }

        public object GetRealObject(
            StreamingContext context
            )
        {
            if ( DiagnosticTrace.Verbose )
            {
                MethodEnteredTraceRecord.Trace( SR.TraceSourceOletx,
                    "IObjectReference.GetRealObject"
                    );
            }

            if ( null == propagationTokenForDeserialize )
            {
                if ( DiagnosticTrace.Critical )
                {
                    InternalErrorTraceRecord.Trace( SR.TraceSourceOletx,
                        SR.UnableToDeserializeTransaction);
                }

                throw TransactionException.Create( SR.UnableToDeserializeTransactionInternalError, null );
            }

            // This may be a second call.  If so, just return.
            if ( null != this.savedLtmPromotedTransaction )
            {
                if ( DiagnosticTrace.Verbose )
                {
                    MethodExitedTraceRecord.Trace( SR.TraceSourceOletx,
                        "IObjectReference.GetRealObject"
                        );
                }
                return this.savedLtmPromotedTransaction;
            }

            Transaction returnValue = TransactionInterop.GetTransactionFromTransmitterPropagationToken( propagationTokenForDeserialize );
            Debug.Assert( null != returnValue, "OletxTransaction.GetRealObject - GetTxFromPropToken returned null" );

            this.savedLtmPromotedTransaction = returnValue;

            if ( DiagnosticTrace.Verbose )
            {
                TransactionDeserializedTraceRecord.Trace( SR.TraceSourceOletx,
                    returnValue._internalTransaction.PromotedTransaction.TransactionTraceId
                    );
            }

            if ( DiagnosticTrace.Verbose )
            {
                MethodExitedTraceRecord.Trace( SR.TraceSourceOletx,
                    "IObjectReference.GetRealObject"
                    );
            }

            return returnValue;
        }

        /// <summary>
        /// Implementation of IDisposable.Dispose. Releases managed, and unmanaged resources
        /// associated with the Transaction object.
        /// </summary>
        internal void Dispose()
        {
            if ( DiagnosticTrace.Verbose )
            {
                MethodEnteredTraceRecord.Trace( SR.TraceSourceOletx,
                    "IDisposable.Dispose"
                    );
            }

            int localDisposed = Interlocked.CompareExchange( ref this.disposed, 1, 0 );
            if ( 0 == localDisposed )
            {
                this.realOletxTransaction.OletxTransactionDisposed();
            }
            GC.SuppressFinalize (this);
            if ( DiagnosticTrace.Verbose )
            {
                MethodExitedTraceRecord.Trace( SR.TraceSourceOletx,
                    "IDisposable.Dispose"
                    );
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
            if ( DiagnosticTrace.Verbose )
            {
                MethodEnteredTraceRecord.Trace( SR.TraceSourceOletx,
                    "OletxTransaction.Rollback"
                    );
            }

            if ( DiagnosticTrace.Warning )
            {
                TransactionRollbackCalledTraceRecord.Trace( SR.TraceSourceOletx,
                    this.TransactionTraceId
                    );
            }

            Debug.Assert( ( 0 == this.disposed ), "OletxTransction object is disposed" );

            this.realOletxTransaction.Rollback();

            if ( DiagnosticTrace.Verbose )
            {
                MethodExitedTraceRecord.Trace( SR.TraceSourceOletx,
                    "OletxTransaction.Rollback"
                    );
            }
        }

        internal IPromotedEnlistment EnlistVolatile(
            ISinglePhaseNotificationInternal singlePhaseNotification,
            EnlistmentOptions enlistmentOptions
            )
        {
            if ( DiagnosticTrace.Verbose )
            {
                MethodEnteredTraceRecord.Trace( SR.TraceSourceOletx,
                    "OletxTransaction.EnlistVolatile( ISinglePhaseNotificationInternal )"
                    );
            }

            Debug.Assert( null != singlePhaseNotification, "Argument is null" );
            Debug.Assert( ( 0 == this.disposed ), "OletxTransction object is disposed" );

            if ( this.realOletxTransaction == null || this.realOletxTransaction.TooLateForEnlistments )
            {
                throw TransactionException.Create(SR.TooLate, null, this.DistributedTxId);
            }

            IPromotedEnlistment enlistment = realOletxTransaction.EnlistVolatile(
                singlePhaseNotification,
                enlistmentOptions,
                this
                );

            if ( DiagnosticTrace.Verbose )
            {
                MethodExitedTraceRecord.Trace( SR.TraceSourceOletx,
                    "OletxTransaction.EnlistVolatile( ISinglePhaseNotificationInternal )"
                    );
            }
            return enlistment;
        }

        internal IPromotedEnlistment EnlistVolatile(
            IEnlistmentNotificationInternal enlistmentNotification,
            EnlistmentOptions enlistmentOptions
            )
        {
            if ( DiagnosticTrace.Verbose )
            {
                MethodEnteredTraceRecord.Trace( SR.TraceSourceOletx,
                    "OletxTransaction.EnlistVolatile( IEnlistmentNotificationInternal )"
                    );
            }

            Debug.Assert( null != enlistmentNotification, "Argument is null" );
            Debug.Assert( ( 0 == this.disposed ), "OletxTransction object is disposed" );

            if ( this.realOletxTransaction == null || this.realOletxTransaction.TooLateForEnlistments )
            {
                throw TransactionException.Create(SR.TooLate, null, this.DistributedTxId);
            }

            IPromotedEnlistment enlistment = realOletxTransaction.EnlistVolatile(
                enlistmentNotification,
                enlistmentOptions,
                this
                );

            if ( DiagnosticTrace.Verbose )
            {
                MethodExitedTraceRecord.Trace( SR.TraceSourceOletx,
                    "OletxTransaction.EnlistVolatile( IEnlistmentNotificationInternal )"
                    );
            }
            return enlistment;
        }

        internal IPromotedEnlistment EnlistDurable(
            Guid resourceManagerIdentifier,
            ISinglePhaseNotificationInternal singlePhaseNotification,
            bool canDoSinglePhase,
            EnlistmentOptions enlistmentOptions
            )
        {
            if ( DiagnosticTrace.Verbose )
            {
                MethodEnteredTraceRecord.Trace( SR.TraceSourceOletx,
                    "OletxTransaction.EnlistDurable( ISinglePhaseNotificationInternal )"
                    );
            }

            Debug.Assert( ( 0 == this.disposed ), "OletxTransction object is disposed" );

            if ( this.realOletxTransaction == null || this.realOletxTransaction.TooLateForEnlistments )
            {
                throw TransactionException.Create(SR.TooLate, null, this.DistributedTxId);
            }

            // get the Oletx TM from the real class
            OletxTransactionManager oletxTM = realOletxTransaction.OletxTransactionManagerInstance;

            // get the resource manager from the Oletx TM
            OletxResourceManager rm = oletxTM.FindOrRegisterResourceManager(resourceManagerIdentifier);

            // ask the rm to do the durable enlistment
            OletxEnlistment enlistment = rm.EnlistDurable(
                this,
                canDoSinglePhase,
                singlePhaseNotification,
                enlistmentOptions
                );

            if ( DiagnosticTrace.Verbose )
            {
                MethodExitedTraceRecord.Trace( SR.TraceSourceOletx,
                    "OletxTransaction.EnlistDurable( ISinglePhaseNotificationInternal )"
                    );
            }
            return enlistment;
        }


        internal OletxDependentTransaction DependentClone(
            bool delayCommit
            )
        {
            OletxDependentTransaction dependentClone = null;

            if ( DiagnosticTrace.Verbose )
            {
                MethodEnteredTraceRecord.Trace( SR.TraceSourceOletx,
                    "OletxTransaction.DependentClone"
                    );
            }

            Debug.Assert( ( 0 == this.disposed ), "OletxTransction object is disposed" );

            if (TransactionStatus.Aborted == Status)
            {
                throw TransactionAbortedException.Create(SR.TransactionAborted, realOletxTransaction.innerException, this.DistributedTxId);
            }
            if (TransactionStatus.InDoubt == Status)
            {
                throw TransactionInDoubtException.Create(SR.TransactionIndoubt, realOletxTransaction.innerException, this.DistributedTxId);
            }
            if (TransactionStatus.Active != Status)
            {
                throw TransactionException.Create(SR.TransactionAlreadyOver, null, this.DistributedTxId);
            }

            dependentClone = new OletxDependentTransaction( realOletxTransaction, delayCommit );

            if ( DiagnosticTrace.Verbose )
            {
                MethodExitedTraceRecord.Trace( SR.TraceSourceOletx,
                    "OletxTransaction.DependentClone"
                    );
            }

            return dependentClone;

        }

        internal TransactionTraceIdentifier TransactionTraceId
        {
            get
            {
                if ( TransactionTraceIdentifier.Empty == this.traceIdentifier )
                {
                    lock ( this.realOletxTransaction )
                    {
                        if ( TransactionTraceIdentifier.Empty == this.traceIdentifier )
                        {
                            try
                            {
                                TransactionTraceIdentifier temp = new TransactionTraceIdentifier( this.realOletxTransaction.Identifier.ToString(), 0 );
                                Thread.MemoryBarrier();
                                this.traceIdentifier = temp;
                            }
                            catch ( TransactionException ex )
                            {
                                // realOletxTransaction.Identifier throws a TransactionException if it can't determine the guid of the
                                // transaction because the transaction was already committed or aborted before the RealOletxTransaction was
                                // created.  If that happens, we don't want to throw just because we are trying to trace.  So just use
                                // the TransactionTraceIdentifier.Empty.
                                if ( DiagnosticTrace.Verbose )
                                {
                                    ExceptionConsumedTraceRecord.Trace( SR.TraceSourceOletx,
                                        ex );
                                }
                            }

                        }
                    }
                }
                return this.traceIdentifier;
            }
        }

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public void GetObjectData(SerializationInfo serializationInfo, StreamingContext context)
        {
            if (serializationInfo == null)
            {
                throw new ArgumentNullException( "serializationInfo");
            }

            byte[] propagationToken = null;

            if ( DiagnosticTrace.Verbose )
            {
                MethodEnteredTraceRecord.Trace( SR.TraceSourceOletx,
                    "OletxTransaction.GetObjectData"
                    );
            }

            Debug.Assert( ( 0 == this.disposed ), "OletxTransction object is disposed" );

            propagationToken = TransactionInterop.GetTransmitterPropagationToken( this );

            serializationInfo.SetType( typeof( OletxTransaction ) );
            serializationInfo.AddValue(propagationTokenString, propagationToken);

            if ( DiagnosticTrace.Information )
            {
                TransactionSerializedTraceRecord.Trace( SR.TraceSourceOletx,
                    this.TransactionTraceId
                    );
            }

            if ( DiagnosticTrace.Verbose )
            {
                MethodExitedTraceRecord.Trace( SR.TraceSourceOletx,
                    "OletxTransaction.GetObjectData"
                    );
            }
        }

        virtual public System.Transactions.IsolationLevel IsolationLevel
        {
            get { return ( this.realOletxTransaction.TransactionIsolationLevel ); }
        }

    }



    // Internal class used by OletxTransaction class which is public
    internal class RealOletxTransaction
    {
        // Transaction manager
        private OletxTransactionManager oletxTransactionManager;

        private ITransactionShim transactionShim;

        // guid related to transaction
        private System.Guid txGuid;

        // Isolation level of the transaction
        private IsolationLevel isolationLevel;

        // Record the exception that caused the transaction to abort.
        internal Exception innerException;

        // Store status
        private TransactionStatus status;

        // This is the count of undisposed OletxTransaction objects that reference
        // this RealOletxTransaction.  This is incremented when an OletxTransaction is created
        // and decremented when OletxTransactionDisposed is
        // called.  When it is decremented to zero, the transactionShim
        // field is "released", thus releasing the unmanged proxy interface
        // pointer.
        private int undisposedOletxTransactionCount;

        // The list of containers for phase0 volatile enlistment multiplexing so we only enlist with the proxy once per wave.
        // The last one on the list is the "current" one.
        internal ArrayList phase0EnlistVolatilementContainerList;

        // The container for phase1 volatile enlistment multiplexing so we only enlist with the proxy once.
        internal OletxPhase1VolatileEnlistmentContainer phase1EnlistVolatilementContainer;

        // Used to get outcomes of transactions with a voter.
        private OutcomeEnlistment outcomeEnlistment = null;

        // This is a count of volatile and Phase0 durable enlistments on this transaction that have not yet voted.
        // This is incremented when an enlistment is made and decremented when the
        // enlistment votes.  It is checked in Rollback.  If the count is greater than 0,
        // then the doomed field is set to true and the Rollback is allowed.  If the count
        // is zero in Rollback, the rollback is rejected with a "too late" exception.
        // All checking and modification of this field needs to be done under a lock( this ).
        private int undecidedEnlistmentCount = 0;

        // If true, indicates that the transaction should NOT commit.  This is set to
        // true if Rollback is called when there are outstanding enlistments.  This is
        // checked when enlistments vote Prepared.  If true, then the enlistment's vote
        // is turned into a ForceRollback.  All checking and modification of this field
        // needs to be done under a lock (this).
        private bool doomed = false;

        // This property is used to allocate enlistment identifiers for enlistment trace identifiers.
        // It is only incremented when a new enlistment is created for this instance of RealOletxTransaction.
        // Enlistments on all clones of this Real transaction use this value.
        internal int enlistmentCount = 0;

        private DateTime creationTime;
        private DateTime lastStateChangeTime;
        private TransactionTraceIdentifier traceIdentifier = TransactionTraceIdentifier.Empty;

        // This field is set directly from the OletxCommittableTransaction constructor.  It will be null
        // for non-root RealOletxTransactions.
        internal OletxCommittableTransaction committableTransaction;

        // This is an internal OletxTransaction.  It is created as part of the RealOletxTransaction constructor.
        // It is used by the DependentCloneEnlistments when creating their volatile enlistments.
        internal OletxTransaction internalClone;

        // This is set initialized to false.  It is set to true when the OletxPhase1VolatileContainer gets a VoteRequest or
        // when any OletxEnlistment attached to this transaction gets a PrepareRequest.  At that point, it is too late for any
        // more enlistments.
        private bool tooLateForEnlistments;

        // This is the InternalTransaction that instigated creation of this RealOletxTransaction.  When we get the outcome
        // of the transaction, we use this to notify the InternalTransaction of the outcome.  We do this to avoid the LTM
        // always creating a volatile enlistment just to get the outcome.
        private InternalTransaction internalTransaction;
        internal InternalTransaction InternalTransaction
        {
            get
            {
                return this.internalTransaction;
            }

            set
            {
                this.internalTransaction = value;
            }

        }

        internal OletxTransactionManager OletxTransactionManagerInstance
        {
            get
            {
                return oletxTransactionManager;
            }
        }

        internal Guid Identifier
        {
            get
            {
                // The txGuid will be empty if the oletx transaction was already committed or aborted when we
                // tried to create the RealOletxTransaction.  We still allow creation of the RealOletxTransaction
                // for COM+ interop purposes, but we can't get the guid or the status of the transaction.
                if ( txGuid.Equals( Guid.Empty ) )
                {
                    throw TransactionException.Create(SR.GetResourceString ( SR.CannotGetTransactionIdentifier ), null );
                }
                return this.txGuid;
            }
        }

        internal Guid DistributedTxId
        {
            get
            {
                Guid returnValue = Guid.Empty;

                if (this.InternalTransaction != null)
                {
                    returnValue = this.InternalTransaction.DistributedTxId;
                }
                return returnValue;
            }
        }

        internal IsolationLevel TransactionIsolationLevel
        {
            get
            {
                return this.isolationLevel;
            }
        }


        internal TransactionStatus Status
        {
            get
            {
                return this.status;
            }
        }

        internal System.Guid TxGuid
        {
            get
            {
                return this.txGuid;
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
            Interlocked.Increment(ref this.undecidedEnlistmentCount);
        }


        internal void DecrementUndecidedEnlistments()
        {
            // Avoid taking a lock on the transaction here.  Decrement
            // will be called by a thread owning a lock on enlistment
            // containers.  When creating new enlistments the transaction
            // will attempt to get a lock on the container when it
            // already holds a lock on the transaction.  This can result
            // in a deadlock.
            Interlocked.Decrement(ref this.undecidedEnlistmentCount);
        }


        internal int UndecidedEnlistments
        {
            get
            {
                return this.undecidedEnlistmentCount;
            }
        }


        internal bool Doomed
        {
            get
            {
                return this.doomed;
            }
        }


        internal ITransactionShim TransactionShim
        {
            get
            {
                ITransactionShim shim = this.transactionShim;
                if ( null == shim )
                {
                    throw TransactionInDoubtException.Create( SR.TransactionIndoubt, null, this.DistributedTxId );
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
                this.oletxTransactionManager = transactionManager;
                this.transactionShim = transactionShim;
                this.outcomeEnlistment = outcomeEnlistment;
                this.txGuid = identifier;
                this.isolationLevel = OletxTransactionManager.ConvertIsolationLevelFromProxyValue( oletxIsoLevel );
                this.status = TransactionStatus.Active;
                this.undisposedOletxTransactionCount = 0;
                this.phase0EnlistVolatilementContainerList = null;
                this.phase1EnlistVolatilementContainer = null;
                this.tooLateForEnlistments = false;
                this.internalTransaction = null;

                this.creationTime = DateTime.UtcNow;
                this.lastStateChangeTime = this.creationTime;

                // Connect this object with the OutcomeEnlistment.
                this.internalClone = new OletxTransaction( this );

                // We have have been created without an outcome enlistment if it was too late to create
                // a clone from the ITransactionNative that we were created from.
                if ( null != this.outcomeEnlistment )
                {
                    this.outcomeEnlistment.SetRealTransaction( this );
                }
                else
                {
                    this.status = TransactionStatus.InDoubt;
                }

                if ( DiagnosticTrace.HaveListeners )
                {
                    DiagnosticTrace.TraceTransfer(this.txGuid);
                }

                successful = true;
            }
            finally
            {
                if (!successful)
                {
                    if (this.outcomeEnlistment != null)
                    {
                        this.outcomeEnlistment.UnregisterOutcomeCallback();
                        this.outcomeEnlistment = null;
                    }
                }
            }

        }

        internal bool TooLateForEnlistments
        {
            get
            {
                return this.tooLateForEnlistments;
            }

            set
            {
                this.tooLateForEnlistments = value;
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
                lock ( this )
                {
                    if ( delayCommit )
                    {
                        if ( null == this.phase0EnlistVolatilementContainerList )
                        {
                            // Not using a MemoryBarrier because all access to this member variable is under a lock of the
                            // object.
                            this.phase0EnlistVolatilementContainerList = new ArrayList(1);
                        }
                        // We may have failed the proxy enlistment for the first container, but we would have
                        // allocated the list.  That is why we have this check here.
                        if ( 0 == this.phase0EnlistVolatilementContainerList.Count )
                        {
                            localPhase0VolatileContainer = new OletxPhase0VolatileEnlistmentContainer( this );
                            needPhase0Enlistment = true;
                        }
                        else
                        {
                            localPhase0VolatileContainer = this.phase0EnlistVolatilementContainerList[this.phase0EnlistVolatilementContainerList.Count - 1] as OletxPhase0VolatileEnlistmentContainer;

                            if (localPhase0VolatileContainer != null)
                            {
                                //CSDMain 91509 - We now synchronize this call with the shim notification trying to call Phase0Request on this container
                                TakeContainerLock(localPhase0VolatileContainer, ref phase0ContainerLockAcquired);
                            }

                            if ( ! localPhase0VolatileContainer.NewEnlistmentsAllowed )
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

                        if ( needPhase0Enlistment )
                        {
                            // We need to create a VoterNotifyShim if native threads are not allowed to enter managed code.
                            phase0Handle = HandleTable.AllocHandle( localPhase0VolatileContainer );
                        }
                    }

                    else // ! delayCommit
                    {
                        if ( null == this.phase1EnlistVolatilementContainer )
                        {
                            localPhase1VolatileContainer = new OletxPhase1VolatileEnlistmentContainer( this );
                            needVoterEnlistment = true;

                            // We need to create a VoterNotifyShim.
                            localPhase1VolatileContainer.voterHandle =
                                HandleTable.AllocHandle( localPhase1VolatileContainer );
                        }
                        else
                        {
                            needVoterEnlistment = false;
                            localPhase1VolatileContainer = this.phase1EnlistVolatilementContainer;
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
                        if ( needPhase0Enlistment )
                        {
                            // We need to use shims if native threads are not allowed to enter managed code.
                            this.transactionShim.Phase0Enlist(
                                phase0Handle,
                                out phase0Shim );
                            localPhase0VolatileContainer.Phase0EnlistmentShim = phase0Shim;
                        }

                        if ( needVoterEnlistment )
                        {
                            // We need to use shims if native threads are not allowed to enter managed code.
                            OletxTransactionManagerInstance.dtcTransactionManagerLock.AcquireReaderLock( -1 );
                            try
                            {
                                this.transactionShim.CreateVoter(
                                    localPhase1VolatileContainer.voterHandle,
                                    out voterShim );

                                enlistmentSucceeded = true;
                            }
                            finally
                            {
                                OletxTransactionManagerInstance.dtcTransactionManagerLock.ReleaseReaderLock();
                            }

                            localPhase1VolatileContainer.VoterBallotShim = voterShim;
                        }

                        if ( delayCommit )
                        {
                            // if we needed a Phase0 enlistment, we need to add the container to the
                            // list.
                            if ( needPhase0Enlistment )
                            {
                                this.phase0EnlistVolatilementContainerList.Add( localPhase0VolatileContainer );
                            }
                            localPhase0VolatileContainer.AddDependentClone();
                            returnValue = localPhase0VolatileContainer;
                        }
                        else
                        {
                            // If we needed a voter enlistment, we need to save the container as THE
                            // phase1 container for this transaction.
                            if ( needVoterEnlistment )
                            {
                                Debug.Assert( ( null == this.phase1EnlistVolatilementContainer ),
                                    "RealOletxTransaction.AddDependentClone - phase1VolContainer not null when expected" );
                                this.phase1EnlistVolatilementContainer = localPhase1VolatileContainer;
                            }
                            localPhase1VolatileContainer.AddDependentClone();
                            returnValue = localPhase1VolatileContainer;
                        }

                    }
                    catch (COMException comException)
                    {
                        OletxTransactionManager.ProxyException( comException );
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

                if ( phase0Handle != IntPtr.Zero && localPhase0VolatileContainer.Phase0EnlistmentShim == null )
                {
                    HandleTable.FreeHandle( phase0Handle );
                }

                if ( !enlistmentSucceeded &&
                    null != localPhase1VolatileContainer &&
                    localPhase1VolatileContainer.voterHandle != IntPtr.Zero &&
                    needVoterEnlistment )
                {
                    HandleTable.FreeHandle( localPhase1VolatileContainer.voterHandle );
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
            OletxTransaction oletxTransaction
            )
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
                lock ( this )
                {
                    enlistment = new OletxVolatileEnlistment(
                        enlistmentNotification,
                        enlistmentOptions,
                        oletxTransaction
                        );

                    if ( (enlistmentOptions & EnlistmentOptions.EnlistDuringPrepareRequired) != 0 )
                    {
                        if ( null == this.phase0EnlistVolatilementContainerList )
                        {
                            // Not using a MemoryBarrier because all access to this member variable is done when holding
                            // a lock on the object.
                            this.phase0EnlistVolatilementContainerList = new ArrayList(1);
                        }
                        // We may have failed the proxy enlistment for the first container, but we would have
                        // allocated the list.  That is why we have this check here.
                        if ( 0 == this.phase0EnlistVolatilementContainerList.Count )
                        {
                            localPhase0VolatileContainer = new OletxPhase0VolatileEnlistmentContainer( this );
                            needPhase0Enlistment = true;
                        }
                        else
                        {
                            localPhase0VolatileContainer = this.phase0EnlistVolatilementContainerList[this.phase0EnlistVolatilementContainerList.Count - 1] as OletxPhase0VolatileEnlistmentContainer;
                            if ( ! localPhase0VolatileContainer.NewEnlistmentsAllowed )
                            {
                                localPhase0VolatileContainer = new OletxPhase0VolatileEnlistmentContainer( this );
                                needPhase0Enlistment = true;
                            }
                            else
                            {
                                needPhase0Enlistment = false;
                            }
                        }

                        if ( needPhase0Enlistment )
                        {
                            // We need to create a VoterNotifyShim if native threads are not allowed to enter managed code.
                            phase0Handle = HandleTable.AllocHandle( localPhase0VolatileContainer );
                        }
                    }
                    else  // not EDPR = TRUE - may need a voter...
                    {
                        if ( null == this.phase1EnlistVolatilementContainer )
                        {
                            needVoterEnlistment = true;
                            localPhase1VolatileContainer = new OletxPhase1VolatileEnlistmentContainer( this );

                            // We need to create a VoterNotifyShim.
                            localPhase1VolatileContainer.voterHandle =
                                HandleTable.AllocHandle( localPhase1VolatileContainer );
                        }
                        else
                        {
                            needVoterEnlistment = false;
                            localPhase1VolatileContainer = this.phase1EnlistVolatilementContainer;
                        }
                    }


                    try
                    {
                        // If enlistDuringPrepareRequired is true, we need to ask the proxy to create a Phase0 enlistment.
                        if ( needPhase0Enlistment )
                        {
                            lock ( localPhase0VolatileContainer )
                            {
                                transactionShim.Phase0Enlist(
                                    phase0Handle,
                                    out phase0Shim );

                                localPhase0VolatileContainer.Phase0EnlistmentShim = phase0Shim;
                            }

                        }

                        if ( needVoterEnlistment )
                        {
                            this.transactionShim.CreateVoter(
                                localPhase1VolatileContainer.voterHandle,
                                out voterShim );

                            enlistmentSucceeded = true;
                            localPhase1VolatileContainer.VoterBallotShim = voterShim;
                        }

                        if ( (enlistmentOptions & EnlistmentOptions.EnlistDuringPrepareRequired) != 0 )
                        {
                            localPhase0VolatileContainer.AddEnlistment(
                                enlistment
                                );
                            if ( needPhase0Enlistment )
                            {
                                this.phase0EnlistVolatilementContainerList.Add( localPhase0VolatileContainer );
                            }
                        }
                        else
                        {
                            localPhase1VolatileContainer.AddEnlistment(
                                enlistment
                                );

                            if ( needVoterEnlistment )
                            {
                                Debug.Assert( ( null == this.phase1EnlistVolatilementContainer ),
                                    "RealOletxTransaction.CommonEnlistVolatile - phase1VolContainer not null when expected." );
                                this.phase1EnlistVolatilementContainer = localPhase1VolatileContainer;
                            }
                        }
                    }
                    catch (COMException comException)
                    {
                        OletxTransactionManager.ProxyException( comException );
                        throw;
                    }
                }
            }
            finally
            {
                if ( phase0Handle != IntPtr.Zero && localPhase0VolatileContainer.Phase0EnlistmentShim == null )
                {
                    HandleTable.FreeHandle( phase0Handle );
                }

                if ( !enlistmentSucceeded &&
                    null != localPhase1VolatileContainer &&
                    localPhase1VolatileContainer.voterHandle != IntPtr.Zero &&
                    needVoterEnlistment )
                {
                    HandleTable.FreeHandle( localPhase1VolatileContainer.voterHandle );
                }
            }

            return enlistment;
        }


        internal IPromotedEnlistment EnlistVolatile(
            ISinglePhaseNotificationInternal enlistmentNotification,
            EnlistmentOptions enlistmentOptions,
            OletxTransaction oletxTransaction
            )
        {
            IPromotedEnlistment enlistment = CommonEnlistVolatile(
                enlistmentNotification,
                enlistmentOptions,
                oletxTransaction
                );
            return enlistment;
        }

        internal IPromotedEnlistment EnlistVolatile(
            IEnlistmentNotificationInternal enlistmentNotification,
            EnlistmentOptions enlistmentOptions,
            OletxTransaction oletxTransaction
            )
        {
            IPromotedEnlistment enlistment = CommonEnlistVolatile(
                enlistmentNotification,
                enlistmentOptions,
                oletxTransaction
                );
            return enlistment;
        }

        internal void Commit()
        {
            try
            {
                this.transactionShim.Commit();
            }
            catch (COMException comException)
            {
                if ( ( NativeMethods.XACT_E_ABORTED == comException.ErrorCode ) ||
                     ( NativeMethods.XACT_E_INDOUBT == comException.ErrorCode )
                   )
                {
                    Interlocked.CompareExchange<Exception>( ref this.innerException, comException, null );

                    if ( DiagnosticTrace.Verbose )
                    {
                        ExceptionConsumedTraceRecord.Trace( SR.TraceSourceOletx,
                            comException );
                    }
                }
                else if ( NativeMethods.XACT_E_ALREADYINPROGRESS == comException.ErrorCode )
                {
                    throw TransactionException.Create(SR.TransactionAlreadyOver, comException );
                }
                else
                {
                    OletxTransactionManager.ProxyException( comException );
                    throw;
                }
            }
        }

        internal void Rollback()
        {
            System.Guid tempGuid = Guid.Empty;

            lock (this)
            {
                // if status is not active and not aborted, then throw an exception
                if (TransactionStatus.Aborted != status &&
                    TransactionStatus.Active != status)
                {
                    throw TransactionException.Create(
                        SR.TransactionAlreadyOver,
                        null,
                        this.DistributedTxId
                        );
                }

                // If the transaciton is already aborted, we can get out now.  Calling Rollback on an already aborted transaction
                // is legal.
                if ( TransactionStatus.Aborted == status )
                {
                    return;
                }

                // If there are still undecided enlistments, we can doom the transaction.
                // We can safely make this check because we ALWAYS have a Phase1 Volatile enlistment to
                // get the outcome.  If we didn't have that enlistment, we would not be able to do this
                // because not all instances of RealOletxTransaction would have enlistments.
                if ( 0 < this.undecidedEnlistmentCount )
                {
                    this.doomed = true;
                }
                else if ( this.tooLateForEnlistments )
                {
                    // It's too late for rollback to be called here.
                    throw TransactionException.Create(
                        SR.TransactionAlreadyOver,
                        null,
                        this.DistributedTxId
                        );
                }

                // Tell the volatile enlistment containers to vote no now if they have outstanding
                // notifications.
                if ( null != this.phase0EnlistVolatilementContainerList )
                {
                    foreach ( OletxPhase0VolatileEnlistmentContainer phase0VolatileContainer in this.phase0EnlistVolatilementContainerList )
                    {
                        phase0VolatileContainer.RollbackFromTransaction();
                    }
                }
                if ( null != this.phase1EnlistVolatilementContainer )
                {
                    this.phase1EnlistVolatilementContainer.RollbackFromTransaction();
                }
            }

            try
            {
                this.transactionShim.Abort();
            }
            catch (COMException comException)
            {
                // If the ErrorCode is XACT_E_ALREADYINPROGRESS and the transaciton is already doomed, we must be
                // the root transaction and we have already called Commit - ignore the exception.  The
                // Rollback is allowed and one of the enlistments that hasn't voted yet will make sure it is
                // aborted.
                if ( NativeMethods.XACT_E_ALREADYINPROGRESS == comException.ErrorCode )
                {
                    if ( this.doomed )
                    {
                        if ( DiagnosticTrace.Verbose )
                        {
                            ExceptionConsumedTraceRecord.Trace( SR.TraceSourceOletx,
                                comException );
                        }
                    }
                    else
                    {
                        throw TransactionException.Create(
                            SR.TransactionAlreadyOver,
                            comException,
                            this.DistributedTxId
                            );
                    }
                }
                else
                {
                    // Otherwise, throw the exception out to the app.
                    OletxTransactionManager.ProxyException( comException );

                    throw;
                }
            }
        }

        internal void OletxTransactionCreated()
        {
            Interlocked.Increment( ref this.undisposedOletxTransactionCount );
        }

        internal void OletxTransactionDisposed()
        {
            int localCount = Interlocked.Decrement( ref this.undisposedOletxTransactionCount );
            Debug.Assert( 0 <= localCount, "RealOletxTransction.undisposedOletxTransationCount < 0" );
        }

        internal void FireOutcome(TransactionStatus statusArg)
        {
            lock ( this )
            {
                if (statusArg == TransactionStatus.Committed)
                {
                    if ( DiagnosticTrace.Verbose )
                    {
                        TransactionCommittedTraceRecord.Trace( SR.TraceSourceOletx,
                            this.TransactionTraceId
                            );
                    }

                    status = TransactionStatus.Committed;
                }
                else if (statusArg == TransactionStatus.Aborted)
                {
                    if ( DiagnosticTrace.Warning )
                    {
                        TransactionAbortedTraceRecord.Trace( SR.TraceSourceOletx,
                            this.TransactionTraceId
                            );
                    }

                    status = TransactionStatus.Aborted;
                }
                else
                {
                    if ( DiagnosticTrace.Warning )
                    {
                        TransactionInDoubtTraceRecord.Trace( SR.TraceSourceOletx,
                            this.TransactionTraceId
                            );
                    }

                    status = TransactionStatus.InDoubt;
                }
            }

            // Let the InternalTransaciton know about the outcome.
            if ( null != this.InternalTransaction )
            {
                InternalTransaction.DistributedTransactionOutcome( this.InternalTransaction, status );
            }

        }

        internal TransactionTraceIdentifier TransactionTraceId
        {
            get
            {
                if ( TransactionTraceIdentifier.Empty == this.traceIdentifier )
                {
                    lock ( this )
                    {
                        if ( TransactionTraceIdentifier.Empty == this.traceIdentifier )
                        {
                            if ( Guid.Empty != this.txGuid )
                            {
                                TransactionTraceIdentifier temp = new TransactionTraceIdentifier( this.txGuid.ToString(), 0 );
                                Thread.MemoryBarrier();
                                this.traceIdentifier = temp;
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
                return this.traceIdentifier;
            }
        }

        internal void TMDown()
        {
            lock ( this )
            {
                // Tell the volatile enlistment containers that the TM went down.
                if ( null != this.phase0EnlistVolatilementContainerList )
                {
                    foreach ( OletxPhase0VolatileEnlistmentContainer phase0VolatileContainer in this.phase0EnlistVolatilementContainerList )
                    {
                        phase0VolatileContainer.TMDown();
                    }
                }

            }
            // Tell the outcome enlistment the TM went down.  We are doing this outside the lock
            // because this may end up making calls out to user code through enlistments.
            outcomeEnlistment.TMDown();
        }
    }

    internal sealed class OutcomeEnlistment
    {
        private WeakReference weakRealTransaction;

        private System.Guid txGuid;

        private bool haveIssuedOutcome;

        private TransactionStatus savedStatus;

        internal Guid TransactionIdentifier
        {
            get
            {
                return txGuid;
            }
        }

        internal OutcomeEnlistment()
        {
            this.haveIssuedOutcome = false;
            this.savedStatus = TransactionStatus.InDoubt;
        }

        internal void SetRealTransaction( RealOletxTransaction realTx )
        {
            bool localHaveIssuedOutcome = false;
            TransactionStatus localStatus = TransactionStatus.InDoubt;

            lock ( this )
            {
                localHaveIssuedOutcome = this.haveIssuedOutcome;
                localStatus = this.savedStatus;

                // We want to do this while holding the lock.
                if ( ! localHaveIssuedOutcome )
                {
                    // We don't use MemoryBarrier here because all access to these member variables is done while holding
                    // a lock on the object.

                    // We are going to use a weak reference so the transaction object can get garbage
                    // collected before we receive the outcome.
                    this.weakRealTransaction = new WeakReference(realTx);

                    // Save the transaction guid so that the transaction can be removed from the
                    // TransactionTable
                    this.txGuid = realTx.TxGuid;
                }
            }

            // We want to do this outside the lock because we are potentially calling out to user code.
            if ( localHaveIssuedOutcome )
            {
                realTx.FireOutcome( localStatus );

                // We may be getting this notification while there are still volatile prepare notifications outstanding.  Tell the
                // container to drive the aborted notification in that case.
                if ( ( ( TransactionStatus.Aborted == localStatus ) || ( TransactionStatus.InDoubt == localStatus ) ) &&
                   ( null != realTx.phase1EnlistVolatilementContainer ) )
                {
                    realTx.phase1EnlistVolatilementContainer.OutcomeFromTransaction( localStatus );
                }
            }
        }

        internal void UnregisterOutcomeCallback()
        {
            this.weakRealTransaction = null;
        }

        private void InvokeOutcomeFunction(TransactionStatus status)
        {
            WeakReference localTxWeakRef = null;

            // In the face of TMDown notifications, we may have already issued
            // the outcome of the transaction.
            lock ( this )
            {
                if ( this.haveIssuedOutcome )
                {
                    return;
                }
                this.haveIssuedOutcome = true;
                this.savedStatus = status;
                localTxWeakRef = this.weakRealTransaction;
            }

            // It is possible for the weakRealTransaction member to be null if some exception was thrown
            // during the RealOletxTransaction constructor after the OutcomeEnlistment object was created.
            // In the finally block of the constructor, it calls UnregisterOutcomeCallback, which will
            // null out weakRealTransaction.  If this is the case, there is nothing to do.
            if ( null != localTxWeakRef )
            {
                RealOletxTransaction realOletxTransaction = localTxWeakRef.Target as RealOletxTransaction;
                if (null != realOletxTransaction)
                {
                    realOletxTransaction.FireOutcome(status);

                    // The container list won't be changing on us now because the transaction status has changed such that
                    // new enlistments will not be created.
                    // Tell the Phase0Volatile containers, if any, about the outcome of the transaction.
                    // I am not protecting the access to phase0EnlistVolatilementContainerList with a lock on "this"
                    // because it is too late for these to be allocated anyway.
                    if ( null != realOletxTransaction.phase0EnlistVolatilementContainerList )
                    {
                        foreach ( OletxPhase0VolatileEnlistmentContainer phase0VolatileContainer in realOletxTransaction.phase0EnlistVolatilementContainerList )
                        {
                            phase0VolatileContainer.OutcomeFromTransaction( status );
                        }
                    }

                    // We may be getting this notification while there are still volatile prepare notifications outstanding.  Tell the
                    // container to drive the aborted notification in that case.
                    if ( ( ( TransactionStatus.Aborted == status ) || ( TransactionStatus.InDoubt == status ) ) &&
                          ( null != realOletxTransaction.phase1EnlistVolatilementContainer ) )
                    {
                        realOletxTransaction.phase1EnlistVolatilementContainer.OutcomeFromTransaction( status );
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
            if ( null != realTx.committableTransaction &&
                 !realTx.committableTransaction.CommitCalled )
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
            lock ( this )
            {
                if ( null != this.weakRealTransaction )
                {
                    realOletxTransaction = this.weakRealTransaction.Target as RealOletxTransaction;
                }
            }

            if (null != realOletxTransaction)
            {
                lock ( realOletxTransaction )
                {
                    transactionIsInDoubt = TransactionIsInDoubt(realOletxTransaction);
                }
            }


            // If we have already voted, then we can't tell what the outcome
            // is.  We do this outside the lock because it may end up invoking user
            // code when it calls into the enlistments later on the stack.
            if ( transactionIsInDoubt )
            {
                this.InDoubt();
            }
            // We have not yet voted, so just say it aborted.
            else
            {
                this.Aborted();
            }
        }


        #region ITransactionOutcome Members

        public void Committed()
        {
            InvokeOutcomeFunction(TransactionStatus.Committed);
            return;
        }

        public void Aborted()
        {
            InvokeOutcomeFunction(TransactionStatus.Aborted);

            return;
        }

        public void InDoubt()
        {
            InvokeOutcomeFunction(TransactionStatus.InDoubt);
            return;
        }

        #endregion
    }
}
