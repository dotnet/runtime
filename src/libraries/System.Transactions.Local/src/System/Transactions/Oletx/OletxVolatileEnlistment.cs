// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Threading;
using System.Transactions;
using System.Transactions.Diagnostics;

namespace System.Transactions.Oletx
{
    internal abstract class OletxVolatileEnlistmentContainer
    {
        protected RealOletxTransaction realOletxTransaction;
        protected ArrayList enlistmentList;
        protected int phase;
        protected int outstandingNotifications;
        protected bool collectedVoteYes;
        protected int incompleteDependentClones;
        protected bool alreadyVoted;

        internal abstract void DecrementOutstandingNotifications( bool voteYes );

        internal abstract void AddDependentClone();

        internal abstract void DependentCloneCompleted();

        internal abstract void RollbackFromTransaction();

        internal abstract void OutcomeFromTransaction( TransactionStatus outcome );

        internal abstract void Committed();

        internal abstract void Aborted();

        internal abstract void InDoubt();

        internal Guid TransactionIdentifier
        {
            get
            {
                return this.realOletxTransaction.Identifier;
            }
        }
    }

    internal class OletxPhase0VolatileEnlistmentContainer : OletxVolatileEnlistmentContainer
    {
        IPhase0EnlistmentShim phase0EnlistmentShim;
        bool aborting;
        bool tmWentDown;


        internal OletxPhase0VolatileEnlistmentContainer(
            RealOletxTransaction realOletxTransaction
            )
        {
            Debug.Assert( null != realOletxTransaction, "Argument is null" );

            // This will be set later, after the caller creates the enlistment with the proxy.
            this.phase0EnlistmentShim = null;

            this.realOletxTransaction = realOletxTransaction;
            this.phase = -1;
            this.aborting = false;
            this.tmWentDown = false;
            this.outstandingNotifications = 0;
            this.incompleteDependentClones = 0;
            this.alreadyVoted = false;
            // If anybody votes false, this will get set to false.
            this.collectedVoteYes = true;
            this.enlistmentList = new ArrayList();

            // This is a new undecided enlistment on the transaction.  Do this last since it has side affects.
            realOletxTransaction.IncrementUndecidedEnlistments();
        }

        internal void TMDown()
        {
            if ( DiagnosticTrace.Verbose )
            {
                MethodEnteredTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                    "OletxPhase0VolatileEnlistmentContainer.TMDown"
                    );
            }

            this.tmWentDown = true;

            if ( DiagnosticTrace.Verbose )
            {
                MethodExitedTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                    "OletxPhase0VolatileEnlistmentContainer.TMDown"
                    );
            }
        }

        internal bool NewEnlistmentsAllowed
        // Be sure to lock this object before calling this.
        {
            get
            {
                return ( -1 == phase );
            }
        }

        internal void AddEnlistment(
            OletxVolatileEnlistment enlistment
            )
        {
            Debug.Assert( null != enlistment, "Argument is null" );

            lock ( this )
            {
                if ( -1 != phase )
                {
                    throw TransactionException.Create( SR.GetString( SR.TraceSourceOletx ),
                        SR.GetString( SR.TooLate ), null );
                }

                this.enlistmentList.Add( enlistment );

            }

        }

        internal override void AddDependentClone()
        {
            lock ( this )
            {
                if ( -1 != phase )
                {
                    throw TransactionException.CreateTransactionStateException( SR.GetString( SR.TraceSourceOletx ), null );
                }

                this.incompleteDependentClones++;

            }
        }

        internal override void DependentCloneCompleted()
        {
            bool doDecrement = false;
            lock ( this )
            {
                if ( DiagnosticTrace.Verbose )
                {
                    string description = "OletxPhase0VolatileEnlistmentContainer.DependentCloneCompleted, outstandingNotifications = " +
                        this.outstandingNotifications.ToString( CultureInfo.CurrentCulture ) +
                        ", incompleteDependentClones = " +
                        this.incompleteDependentClones.ToString( CultureInfo.CurrentCulture ) +
                        ", phase = " + this.phase.ToString( CultureInfo.CurrentCulture );
                    MethodEnteredTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                        description
                        );
                }
                this.incompleteDependentClones--;
                Debug.Assert( 0 <= this.incompleteDependentClones, "OletxPhase0VolatileEnlistmentContainer.DependentCloneCompleted - incompleteDependentClones < 0" );

                // If we have not more incomplete dependent clones and we are in Phase 0, we need to "fake out" a notification completion.
                if ( ( 0 == this.incompleteDependentClones ) && ( 0 == this.phase ) )
                {
                    this.outstandingNotifications++;
                    doDecrement = true;
                }
            }
            if ( doDecrement )
            {
                DecrementOutstandingNotifications( true );
            }
            if ( DiagnosticTrace.Verbose )
            {
                string description = "OletxPhase0VolatileEnlistmentContainer.DependentCloneCompleted";
                MethodExitedTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                    description
                    );
            }
        }

        internal override void RollbackFromTransaction()
        {
            lock ( this )
            {
                if ( DiagnosticTrace.Verbose )
                {
                    string description = "OletxPhase0VolatileEnlistmentContainer.RollbackFromTransaction, outstandingNotifications = " +
                        this.outstandingNotifications.ToString( CultureInfo.CurrentCulture ) +
                        ", incompleteDependentClones = " + this.incompleteDependentClones.ToString( CultureInfo.CurrentCulture );
                    MethodEnteredTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                        description
                        );
                }
                if ( ( 0 == phase ) && ( ( 0 < this.outstandingNotifications ) || ( 0 < incompleteDependentClones ) ) )
                {
                    this.alreadyVoted = true;
                    // All we are going to do is release the Phase0Enlistment interface because there
                    // is no negative vote to Phase0Request.
                    if ( null != this.Phase0EnlistmentShim )
                    {
                        this.Phase0EnlistmentShim.Phase0Done( false );
                    }
                }
            }
            if ( DiagnosticTrace.Verbose )
            {
                string description = "OletxPhase0VolatileEnlistmentContainer.RollbackFromTransaction";
                MethodExitedTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                    description
                    );
            }
        }


        internal IPhase0EnlistmentShim Phase0EnlistmentShim
        {
            get
            {
                IPhase0EnlistmentShim returnValue = null;
                lock ( this )
                {
                    returnValue = this.phase0EnlistmentShim;
                }
                return returnValue;
            }
            set
            {
                lock ( this )
                {
                    // If this.aborting is set to true, then we must have already received a
                    // Phase0Request.  This could happen if the transaction aborts after the
                    // enlistment is made, but before we are given the shim.
                    if ( this.aborting || this.tmWentDown )
                    {
                        value.Phase0Done( false );
                    }
                    this.phase0EnlistmentShim = value;
                }
            }
        }

        internal override void DecrementOutstandingNotifications( bool voteYes )
        {
            bool respondToProxy = false;
            IPhase0EnlistmentShim localPhase0Shim = null;

            lock ( this )
            {
                if ( DiagnosticTrace.Verbose )
                {
                    string description = "OletxPhase0VolatileEnlistmentContainer.DecrementOutstandingNotifications, outstandingNotifications = " +
                        this.outstandingNotifications.ToString( CultureInfo.CurrentCulture ) +
                        ", incompleteDependentClones = " +
                        this.incompleteDependentClones.ToString( CultureInfo.CurrentCulture );
                    MethodEnteredTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                        description
                        );
                }
                outstandingNotifications--;
                Debug.Assert( 0 <= outstandingNotifications, "OletxPhase0VolatileEnlistmentContainer.DecrementOutstandingNotifications - outstandingNotifications < 0" );

                this.collectedVoteYes = this.collectedVoteYes && voteYes;
                if ( ( 0 == this.outstandingNotifications ) && ( 0 == this.incompleteDependentClones ) )
                {
                    if ( ( 0 == this.phase ) && ( !this.alreadyVoted ) )
                    {
                        respondToProxy = true;
                        this.alreadyVoted = true;
                        localPhase0Shim = this.phase0EnlistmentShim;
                    }
                    this.realOletxTransaction.DecrementUndecidedEnlistments();
                }
            }

            try
            {
                if ( respondToProxy )
                {
                    if ( null != localPhase0Shim )
                    {
                        localPhase0Shim.Phase0Done( ( this.collectedVoteYes ) && ( !this.realOletxTransaction.Doomed ) );
                    }
                }
            }
            catch ( COMException ex )
            {
                if ( ( NativeMethods.XACT_E_CONNECTION_DOWN == ex.ErrorCode ) ||
                    ( NativeMethods.XACT_E_TMNOTAVAILABLE == ex.ErrorCode )
                    )
                {
                    if ( DiagnosticTrace.Verbose )
                    {
                        ExceptionConsumedTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                            ex );
                    }
                }
                // In the case of Phase0, there is a bug in the proxy that causes an XACT_E_PROTOCOL
                // error if the TM goes down while the enlistment is still active.  The Phase0Request is
                // sent out with abortHint false, but the state of the proxy object is not changed, causing
                // Phase0Done request to fail with XACT_E_PROTOCOL.
                // For Prepared, we want to make sure the proxy aborts the transaction.  We don't need
                // to drive the abort to the application here because the Phase1 enlistment will do that.
                // In other words, treat this as if the proxy said Phase0Request( abortingHint = true ).
                else if ( NativeMethods.XACT_E_PROTOCOL == ex.ErrorCode )
                {
                    this.phase0EnlistmentShim = null;
                    if ( DiagnosticTrace.Verbose )
                    {
                        ExceptionConsumedTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                            ex );
                    }
                }
                else
                {
                    throw;
                }
            }

            if ( DiagnosticTrace.Verbose )
            {
                string description = "OletxPhase0VolatileEnlistmentContainer.DecrementOutstandingNotifications";
                MethodExitedTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                    description
                    );
            }
        }

        internal override void OutcomeFromTransaction( TransactionStatus outcome )
        {

            if ( TransactionStatus.Committed == outcome )
            {
                this.Committed();
            }
            else if ( TransactionStatus.Aborted == outcome )
            {
                this.Aborted();
            }
            else if ( TransactionStatus.InDoubt == outcome )
            {
                this.InDoubt();
            }
            else
            {
                Debug.Assert( false, "OletxPhase0VolatileEnlistmentContainer.OutcomeFromTransaction, outcome is not Commited or Aborted or InDoubt" );
            }
        }

        internal override void Committed()
        {
            OletxVolatileEnlistment enlistment = null;
            int localCount = 0;

            lock ( this )
            {
                Debug.Assert( ( 0 == phase ) && ( 0 == this.outstandingNotifications ) );
                phase = 2;
                localCount = this.enlistmentList.Count;
            }

            for ( int i = 0; i < localCount; i++ )
            {
                enlistment = this.enlistmentList[i] as OletxVolatileEnlistment;
                if ( null == enlistment )
                {
                    if ( DiagnosticTrace.Critical )
                    {
                        InternalErrorTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                            ""
                            );
                    }

                    Debug.Assert( false, "OletxPhase1VolatileEnlistmentContainer.Committed, enlistmentList element is not an OletxVolatileEnlistment." );
                    throw new InvalidOperationException( SR.GetString( SR.InternalError ) );
                }

                enlistment.Commit();
            }
        }

        internal override void Aborted()
        {
            OletxVolatileEnlistment enlistment = null;
            int localCount = 0;

            lock ( this )
            {
                // Tell all the enlistments that the transaction aborted and let the enlistment
                // state determine if the notification should be delivered.
                this.phase = 2;
                localCount = this.enlistmentList.Count;
            }

            for ( int i = 0; i < localCount; i++ )
            {
                enlistment = this.enlistmentList[i] as OletxVolatileEnlistment;
                if ( null == enlistment )
                {
                    if ( DiagnosticTrace.Critical )
                    {
                        InternalErrorTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                            ""
                            );
                    }

                    Debug.Assert( false, "OletxPhase1VolatileEnlistmentContainer.Aborted, enlistmentList element is not an OletxVolatileEnlistment." );
                    throw new InvalidOperationException( SR.GetString( SR.InternalError ) );
                }

                enlistment.Rollback();
            }

        }

        internal override void InDoubt()
        {
            OletxVolatileEnlistment enlistment = null;
            int localCount = 0;

            lock ( this )
            {
                // Tell all the enlistments that the transaction is InDoubt and let the enlistment
                // state determine if the notification should be delivered.
                phase = 2;
                localCount = this.enlistmentList.Count;
            }

            for ( int i = 0; i < localCount; i++ )
            {
                enlistment = this.enlistmentList[i] as OletxVolatileEnlistment;
                if ( null == enlistment )
                {
                    if ( DiagnosticTrace.Critical )
                    {
                        InternalErrorTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                            ""
                            );
                    }

                    Debug.Assert( false, "OletxPhase1VolatileEnlistmentContainer.InDoubt, enlistmentList element is not an OletxVolatileEnlistment." );
                    throw new InvalidOperationException( SR.GetString( SR.InternalError ) );
                }

                enlistment.InDoubt();
            }

        }

        internal void Phase0Request(
            bool abortHint
            )
        {
            OletxVolatileEnlistment enlistment = null;
            int localCount = 0;
            OletxCommittableTransaction committableTx = null;
            bool commitNotYetCalled = false;

            lock ( this )
            {
                if ( DiagnosticTrace.Verbose )
                {
                    string description = "OletxPhase0VolatileEnlistmentContainer.Phase0Request, abortHint = " +
                        abortHint.ToString( CultureInfo.CurrentCulture ) +
                        ", phase = " + this.phase.ToString( CultureInfo.CurrentCulture );
                    MethodEnteredTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                        description
                        );
                }

                this.aborting = abortHint;
                committableTx = this.realOletxTransaction.committableTransaction;
                if ( null != committableTx )
                {
                    // We are dealing with the committable transaction.  If Commit or BeginCommit has NOT been
                    // called, then we are dealing with a situation where the TM went down and we are getting
                    // a bogus Phase0Request with abortHint = false (COMPlus bug 36760/36758).  This is an attempt
                    // to not send the app a Prepare request when we know the transaction is going to abort.
                    if (!committableTx.CommitCalled )
                    {
                        commitNotYetCalled = true;
                        this.aborting = true;
                    }
                }
                // It's possible that we are in phase 2 if we got an Aborted outcome from the transaction before we got the
                // Phase0Request.  In both cases, we just respond to the proxy and don't bother telling the enlistments.
                // They have either already heard about the abort or will soon.
                if ( ( 2 == this.phase ) || ( -1 == this.phase ) )
                {
                    if ( -1 == this.phase )
                    {
                        this.phase = 0;
                    }

                    // If we got an abort hint or we are the committable transaction and Commit has not yet been called or the TM went down,
                    // we don't want to do any more work on the transaction.  The abort notifications will be sent by the phase 1
                    // enlistment
                    if ( ( this.aborting ) || ( this.tmWentDown ) || ( commitNotYetCalled ) || ( 2 == this.phase ) )
                    {
                        // There is a possible race where we could get the Phase0Request before we are given the
                        // shim.  In that case, we will vote "no" when we are given the shim.
                        if ( null != this.phase0EnlistmentShim )
                        {
                            try
                            {
                                this.phase0EnlistmentShim.Phase0Done( false );
                                // CSDMain 138031: There is a potential race between DTC sending Abort notification and OletxDependentTransaction::Complete is called.
                                // We need to set the alreadyVoted flag to true once we successfully voted, so later we don't vote again when OletxDependentTransaction::Complete is called
                                // Otherwise, in OletxPhase0VolatileEnlistmentContainer::DecrementOutstandingNotifications code path, we are going to call Phase0Done( true ) again
                                // and result in an access violation while accessing the pPhase0EnlistmentAsync member variable of the Phase0Shim object.
                                this.alreadyVoted = true;
                            }
                            // I am not going to check for XACT_E_PROTOCOL here because that check is a workaround for a bug
                            // that only shows up if abortingHint is false.
                            catch ( COMException ex )
                            {
                                if ( DiagnosticTrace.Verbose )
                                {
                                    ExceptionConsumedTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                                        ex );
                                }
                            }
                        }
                        return;
                    }
                    outstandingNotifications = enlistmentList.Count;
                    localCount = enlistmentList.Count;
                    // If we don't have any enlistments, then we must have created this container for
                    // delay commit dependent clones only.  So we need to fake a notification.
                    if ( 0 == localCount )
                    {
                        this.outstandingNotifications = 1;
                    }
                }
                else  // any other phase is bad news.
                {
                    if ( DiagnosticTrace.Critical )
                    {
                        InternalErrorTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                            "OletxPhase0VolatileEnlistmentContainer.Phase0Request, phase != -1"
                            );
                    }

                    Debug.Assert( false, "OletxPhase0VolatileEnlistmentContainer.Phase0Request, phase != -1" );
                    throw new InvalidOperationException( SR.GetString( SR.InternalError ) );
                }
            }

            // We may not have any Phase0 volatile enlistments, which means that this container
            // got created solely for delay commit dependent transactions.  We need to fake out a
            // notification completion.
            if ( 0 == localCount )
            {
                DecrementOutstandingNotifications( true );
            }
            else
            {
                for ( int i = 0; i < localCount; i++ )
                {
                    enlistment = enlistmentList[i] as OletxVolatileEnlistment;
                    if ( null == enlistment )
                    {
                        if ( DiagnosticTrace.Critical )
                        {
                            InternalErrorTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                                ""
                                );
                        }

                        Debug.Assert( false, "OletxPhase0VolatileEnlistmentContainer.Phase0Request, enlistmentList element is not an OletxVolatileEnlistment.");
                        throw new InvalidOperationException( SR.GetString( SR.InternalError ) );
                    }

                    // Do the notification outside any locks.
                    Debug.Assert( enlistment.enlistDuringPrepareRequired, "OletxPhase0VolatileEnlistmentContainer.Phase0Request, enlistmentList element not marked as EnlistmentDuringPrepareRequired." );
                    Debug.Assert( !abortHint, "OletxPhase0VolatileEnlistmentContainer.Phase0Request, abortingHint is true just before sending Prepares." );

                    enlistment.Prepare( this );
                }
            }

            if ( DiagnosticTrace.Verbose )
            {
                string description = "OletxPhase0VolatileEnlistmentContainer.Phase0Request, abortHint = " + abortHint.ToString( CultureInfo.CurrentCulture );
                MethodExitedTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                    description
                    );
            }
            return;
        }

    }


    internal class OletxPhase1VolatileEnlistmentContainer : OletxVolatileEnlistmentContainer
    {
        IVoterBallotShim voterBallotShim;

        internal IntPtr voterHandle = IntPtr.Zero;

        internal OletxPhase1VolatileEnlistmentContainer(
            RealOletxTransaction realOletxTransaction
            )
        {
            Debug.Assert( null != realOletxTransaction, "Argument is null" );

            // This will be set later, after the caller creates the enlistment with the proxy.
            voterBallotShim = null;

            this.realOletxTransaction = realOletxTransaction;
            this.phase = -1;
            this.outstandingNotifications = 0;
            this.incompleteDependentClones = 0;
            this.alreadyVoted = false;

            // If anybody votes false, this will get set to false.
            this.collectedVoteYes = true;

            this.enlistmentList = new ArrayList();

            // This is a new undecided enlistment on the transaction.  Do this last since it has side affects.
            realOletxTransaction.IncrementUndecidedEnlistments();
        }

        // Returns true if this container is enlisted for Phase 0.
        internal void AddEnlistment(
            OletxVolatileEnlistment enlistment
            )
        {
            Debug.Assert( null != enlistment, "Argument is null" );

            lock ( this )
            {
                if ( -1 != phase )
                {
                    throw TransactionException.Create( SR.GetString( SR.TraceSourceOletx ),
                        SR.GetString( SR.TooLate ), null );
                }

                enlistmentList.Add( enlistment );


            }

        }

        internal override void AddDependentClone()
        {
            lock ( this )
            {
                if ( -1 != phase )
                {
                    throw TransactionException.CreateTransactionStateException( SR.GetString( SR.TraceSourceOletx ), null );
                }

                // We simply need to block the response to the proxy until all clone is completed.
                this.incompleteDependentClones++;

            }
        }

        internal override void DependentCloneCompleted()
        {
            if ( DiagnosticTrace.Verbose )
            {
                string description = "OletxPhase1VolatileEnlistmentContainer.DependentCloneCompleted, outstandingNotifications = " +
                    this.outstandingNotifications.ToString( CultureInfo.CurrentCulture ) +
                    ", incompleteDependentClones = " +
                    this.incompleteDependentClones.ToString( CultureInfo.CurrentCulture ) +
                    ", phase = " + this.phase.ToString( CultureInfo.CurrentCulture );
                MethodEnteredTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                    description
                    );
            }

            //Fix for stress bug CSDMain 126887. This is to synchronize with the corresponding AddDependentClone
            //which takes the container lock while incrementing the incompleteDependentClone count
            lock (this)
            {
                this.incompleteDependentClones--;
            }

            Debug.Assert( 0 <= this.outstandingNotifications, "OletxPhase1VolatileEnlistmentContainer.DependentCloneCompleted - DependentCloneCompleted < 0" );

            if ( DiagnosticTrace.Verbose )
            {
                string description = "OletxPhase1VolatileEnlistmentContainer.DependentCloneCompleted";
                MethodExitedTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                    description
                    );
            }
        }


        internal override void RollbackFromTransaction()
        {
            bool voteNo = false;
            IVoterBallotShim localVoterShim = null;

            lock ( this )
            {
                if ( DiagnosticTrace.Verbose )
                {
                    string description = "OletxPhase1VolatileEnlistmentContainer.RollbackFromTransaction, outstandingNotifications = " +
                        this.outstandingNotifications.ToString( CultureInfo.CurrentCulture ) +
                        ", incompleteDependentClones = " + this.incompleteDependentClones.ToString( CultureInfo.CurrentCulture );
                    MethodEnteredTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                        description
                        );
                }

                if ( ( 1 == phase ) && ( 0 < this.outstandingNotifications ) )
                {
                    this.alreadyVoted = true;
                    voteNo = true;
                    localVoterShim = this.voterBallotShim;
                }
            }

            if ( voteNo )
            {
                try
                {
                    if ( null != localVoterShim )
                    {
                        localVoterShim.Vote( false );
                    }
                    // We are not going to hear anymore from the proxy if we voted no, so we need to tell the
                    // enlistments to rollback.  The state of the OletxVolatileEnlistment will determine whether or
                    // not the notification actually goes out to the app.
                    Aborted();
                }
                catch ( COMException ex )
                {
                    if ( ( NativeMethods.XACT_E_CONNECTION_DOWN == ex.ErrorCode ) ||
                        ( NativeMethods.XACT_E_TMNOTAVAILABLE == ex.ErrorCode )
                        )
                    {
                        lock ( this )
                        {
                            // If we are in phase 1, we need to tell the enlistments that the transaction is InDoubt.
                            if ( 1 == phase )
                            {
                                InDoubt();
                            }
                        }
                        if ( DiagnosticTrace.Verbose )
                        {
                            ExceptionConsumedTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                                ex );
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
                finally
                {
                    // At this point it is unclear if we will get a notification from DTC or not
                    // it depends on whether or not the transaction was in the process of aborting
                    // already.  The only safe thing to do is to ensure that the Handle for the
                    // voter is released at this point.
                    HandleTable.FreeHandle(this.voterHandle);
                }
            }

            if ( DiagnosticTrace.Verbose )
            {
                string description = "OletxPhase1VolatileEnlistmentContainer.RollbackFromTransaction";
                MethodExitedTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                    description
                    );
            }

        }

        internal IVoterBallotShim VoterBallotShim
        {
            get
            {
                IVoterBallotShim returnValue = null;
                lock ( this )
                {
                    returnValue = this.voterBallotShim;
                }
                return returnValue;
            }
            set
            {
                lock ( this )
                {
                    this.voterBallotShim = value;
                }
            }
        }

        internal override void DecrementOutstandingNotifications( bool voteYes )
        {
            bool respondToProxy = false;
            IVoterBallotShim localVoterShim = null;

            lock ( this )
            {
                if ( DiagnosticTrace.Verbose )
                {
                    string description = "OletxPhase1VolatileEnlistmentContainer.DecrementOutstandingNotifications, outstandingNotifications = " +
                        this.outstandingNotifications.ToString( CultureInfo.CurrentCulture ) +
                        ", incompleteDependentClones = " +
                        this.incompleteDependentClones.ToString( CultureInfo.CurrentCulture );
                    MethodEnteredTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                        description
                        );
                }

                this.outstandingNotifications--;
                Debug.Assert( 0 <= this.outstandingNotifications, "OletxPhase1VolatileEnlistmentContainer.DecrementOutstandingNotifications - outstandingNotifications < 0" );
                collectedVoteYes = collectedVoteYes && voteYes;
                if ( 0 == outstandingNotifications )
                {
                    if ( ( 1 == phase ) && ( ! this.alreadyVoted ) )
                    {
                        respondToProxy = true;
                        this.alreadyVoted = true;
                        localVoterShim = this.VoterBallotShim;
                    }
                    this.realOletxTransaction.DecrementUndecidedEnlistments();
                }
            }

            try
            {
                if ( respondToProxy )
                {
                    if ( ( collectedVoteYes ) && ( ! realOletxTransaction.Doomed ) )
                    {
                        if ( null != localVoterShim )
                        {
                            localVoterShim.Vote( true );
                        }
                    }
                    else  // we need to vote no.
                    {
                        try
                        {
                            if ( null != localVoterShim )
                            {
                                localVoterShim.Vote( false );
                            }
                            // We are not going to hear anymore from the proxy if we voted no, so we need to tell the
                            // enlistments to rollback.  The state of the OletxVolatileEnlistment will determine whether or
                            // not the notification actually goes out to the app.
                            Aborted();
                        }
                        finally
                        {
                            // At this point it is unclear if we will get a notification from DTC or not
                            // it depends on whether or not the transaction was in the process of aborting
                            // already.  The only safe thing to do is to ensure that the Handle for the
                            // voter is released at this point.
                            HandleTable.FreeHandle(this.voterHandle);
                        }
                    }
                }
            }
            catch ( COMException ex )
            {
                if ( ( NativeMethods.XACT_E_CONNECTION_DOWN == ex.ErrorCode ) ||
                    ( NativeMethods.XACT_E_TMNOTAVAILABLE == ex.ErrorCode )
                    )
                {
                    lock ( this )
                    {
                        // If we are in phase 1, we need to tell the enlistments that the transaction is InDoubt.
                        if ( 1 == phase )
                        {
                            InDoubt();
                        }

                        // There is nothing special to do for phase 2.
                    }
                    if ( DiagnosticTrace.Verbose )
                    {
                        ExceptionConsumedTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                            ex );
                    }
                }
                else
                {
                    throw;
                }
            }

            if ( DiagnosticTrace.Verbose )
            {
                string description = "OletxPhase1VolatileEnlistmentContainer.DecrementOutstandingNotifications";
                MethodExitedTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                    description
                    );
            }
        }

        internal override void OutcomeFromTransaction( TransactionStatus outcome )
        {
            bool driveAbort = false;
            bool driveInDoubt = false;

            lock ( this )
            {
                // If we are in Phase 1 and still have outstanding notifications, we need
                // to drive sending of the outcome to the enlistments.  If we are in any
                // other phase, or we don't have outstanding notifications, we will eventually
                // get the outcome notification on our OWN voter enlistment, so we will just
                // wait for that.
                if ( ( 1 == this.phase ) && ( 0 < this.outstandingNotifications ) )
                {
                    if ( TransactionStatus.Aborted == outcome )
                    {
                        driveAbort = true;
                    }
                    else if ( TransactionStatus.InDoubt == outcome )
                    {
                        driveInDoubt = true;
                    }
                    else
                    {
                        Debug.Assert( false, "OletxPhase1VolatileEnlistmentContainer.OutcomeFromTransaction, outcome is not Aborted or InDoubt" );
                    }
                }
            }

            if ( driveAbort )
            {
                Aborted();
            }

            if ( driveInDoubt )
            {
                InDoubt();
            }

        }

        internal override void Committed()
        {
            OletxVolatileEnlistment enlistment = null;
            int localPhase1Count = 0;

            lock ( this )
            {
                phase = 2;
                localPhase1Count = this.enlistmentList.Count;
            }

            for ( int i = 0; i < localPhase1Count; i++ )
            {
                enlistment = this.enlistmentList[i] as OletxVolatileEnlistment;
                if ( null == enlistment )
                {
                    if ( DiagnosticTrace.Critical )
                    {
                        InternalErrorTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                            ""
                            );
                    }

                    Debug.Assert( false, "OletxPhase1VolatileEnlistmentContainer.Committed, enlistmentList element is not an OletxVolatileEnlistment." );
                    throw new InvalidOperationException( SR.GetString( SR.InternalError ) );
                }

                enlistment.Commit();
            }
        }

        internal override void Aborted()
        {
            OletxVolatileEnlistment enlistment = null;
            int localPhase1Count = 0;

            lock ( this )
            {
                phase = 2;
                localPhase1Count = this.enlistmentList.Count;
            }

            for ( int i = 0; i < localPhase1Count; i++ )
            {
                enlistment = this.enlistmentList[i] as OletxVolatileEnlistment;
                if ( null == enlistment )
                {
                    if ( DiagnosticTrace.Critical )
                    {
                        InternalErrorTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                            ""
                            );
                    }

                    Debug.Assert( false, "OletxPhase1VolatileEnlistmentContainer.Aborted, enlistmentList element is not an OletxVolatileEnlistment." );
                    throw new InvalidOperationException( SR.GetString( SR.InternalError ) );
                }

                enlistment.Rollback();
            }

        }

        internal override void InDoubt()
        {
            OletxVolatileEnlistment enlistment = null;
            int localPhase1Count = 0;

            lock ( this )
            {
                phase = 2;
                localPhase1Count = this.enlistmentList.Count;
            }

            for ( int i = 0; i < localPhase1Count; i++ )
            {
                enlistment = this.enlistmentList[i] as OletxVolatileEnlistment;
                if ( null == enlistment )
                {
                    if ( DiagnosticTrace.Critical )
                    {
                        InternalErrorTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                            ""
                            );
                    }

                    Debug.Assert( false, "OletxPhase1VolatileEnlistmentContainer.InDoubt, enlistmentList element is not an OletxVolatileEnlistment." );
                    throw new InvalidOperationException( SR.GetString( SR.InternalError ) );
                }

                enlistment.InDoubt();
            }

        }

        internal void VoteRequest()
        {
            OletxVolatileEnlistment enlistment = null;
            int localPhase1Count = 0;
            bool voteNo = false;

            lock ( this )
            {
                if ( DiagnosticTrace.Verbose )
                {
                    string description = "OletxPhase1VolatileEnlistmentContainer.VoteRequest";
                    MethodEnteredTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                        description
                        );
                }

                phase = 1;

                // If we still have incomplete dependent clones, vote no now.
                if ( 0 < this.incompleteDependentClones )
                {
                    voteNo = true;
                    this.outstandingNotifications = 1;
                }
                else
                {
                    this.outstandingNotifications = this.enlistmentList.Count;
                    localPhase1Count = this.enlistmentList.Count;
                    // We may not have an volatile phase 1 enlistments, which means that this
                    // container was created only for non-delay commit dependent clones.  If that
                    // is the case, fake out a notification and response.
                    if ( 0 == localPhase1Count )
                    {
                        this.outstandingNotifications = 1;
                    }
                }

                this.realOletxTransaction.TooLateForEnlistments = true;
            }

            if ( voteNo )
            {
                DecrementOutstandingNotifications( false );
            }
            else if ( 0 == localPhase1Count )
            {
                DecrementOutstandingNotifications( true );
            }
            else
            {
                for ( int i = 0; i < localPhase1Count; i++ )
                {
                    enlistment = this.enlistmentList[i] as OletxVolatileEnlistment;
                    if ( null == enlistment )
                    {
                        if ( DiagnosticTrace.Critical )
                        {
                            InternalErrorTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                                ""
                                );
                        }

                    Debug.Assert( false, "OletxPhase1VolatileEnlistmentContainer.VoteRequest, enlistmentList element is not an OletxVolatileEnlistment." );
                        throw new InvalidOperationException( SR.GetString( SR.InternalError ) );
                    }

                    enlistment.Prepare( this );
                }
            }

            if ( DiagnosticTrace.Verbose )
            {
                string description = "OletxPhase1VolatileEnlistmentContainer.VoteRequest";
                MethodExitedTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                    description
                    );
            }

        }
    }

    class OletxVolatileEnlistment :
        OletxBaseEnlistment,
        IPromotedEnlistment
    {
        enum OletxVolatileEnlistmentState
        {
            Active,
            Preparing,
            Committing,
            Aborting,
            Prepared,
            Aborted,
            InDoubt,
            Done
        }

        IEnlistmentNotificationInternal iEnlistmentNotification;
        OletxVolatileEnlistmentState state = OletxVolatileEnlistmentState.Active;
        OletxVolatileEnlistmentContainer container;
        internal bool enlistDuringPrepareRequired;

        // This is used if the transaction outcome is received while a prepare request
        // is still outstanding to an app.  Active means no outcome, yet.  Aborted means
        // we should tell the app Aborted.  InDoubt means tell the app InDoubt.  This
        // should never be Committed because we shouldn't receive a Committed notification
        // from the proxy while we have a Prepare outstanding.
        TransactionStatus pendingOutcome;

        internal OletxVolatileEnlistment(
            IEnlistmentNotificationInternal enlistmentNotification,
            EnlistmentOptions enlistmentOptions,
            OletxTransaction oletxTransaction
            ) : base( null, oletxTransaction )
        {
            this.iEnlistmentNotification = enlistmentNotification;
            this.enlistDuringPrepareRequired = (enlistmentOptions & EnlistmentOptions.EnlistDuringPrepareRequired) != 0;

            // We get a container when we are asked to vote.
            this.container = null;

            pendingOutcome = TransactionStatus.Active;

            if ( DiagnosticTrace.Information )
            {
                EnlistmentTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                    this.InternalTraceIdentifier,
                    EnlistmentType.Volatile,
                    enlistmentOptions
                    );
            }
        }


        internal void Prepare( OletxVolatileEnlistmentContainer container )
        {
            OletxVolatileEnlistmentState localState = OletxVolatileEnlistmentState.Active;
            IEnlistmentNotificationInternal localEnlistmentNotification = null;

            lock ( this )
            {
                localEnlistmentNotification = iEnlistmentNotification;

                // The app may have already called EnlistmentDone.  If this occurs, don't bother sending
                // the notification to the app.
                if ( OletxVolatileEnlistmentState.Active == state )
                {
                    localState = state = OletxVolatileEnlistmentState.Preparing;
                }
                else
                {
                    localState = state;
                }
                this.container = container;

            }

            // Tell the application to do the work.
            if ( OletxVolatileEnlistmentState.Preparing == localState )
            {
                if ( null != localEnlistmentNotification )
                {
                    if ( DiagnosticTrace.Verbose )
                    {
                        EnlistmentNotificationCallTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                            this.InternalTraceIdentifier,
                            NotificationCall.Prepare
                            );
                    }

                    localEnlistmentNotification.Prepare( this );
                }
                else
                {
                    if ( DiagnosticTrace.Critical )
                    {
                        InternalErrorTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                            ""
                            );
                    }

                    Debug.Assert( false, "OletxVolatileEnlistment.Prepare, no enlistmentNotification member." );
                    throw new InvalidOperationException( SR.GetString( SR.InternalError ) );
                }
            }
            else if ( OletxVolatileEnlistmentState.Done == localState )
            {
                // Voting yes because it was an early read-only vote.
                container.DecrementOutstandingNotifications( true );

                // We must have had a race between EnlistmentDone and the proxy telling
                // us Phase0Request.  Just return.
                return;
            }
            // It is okay to be in Prepared state if we are edpr=true because we already
            // did our prepare in Phase0.
            else if ( ( OletxVolatileEnlistmentState.Prepared == localState ) &&
                        ( this.enlistDuringPrepareRequired ) )
            {
                container.DecrementOutstandingNotifications( true );
                return;
            }
            else if ( ( OletxVolatileEnlistmentState.Aborting == localState ) ||
                      ( OletxVolatileEnlistmentState.Aborted == localState ) )
            {
                // An abort has raced with this volatile Prepare
                // decrement the outstanding notifications making sure to vote no.
                container.DecrementOutstandingNotifications( false );
                return;
            }
            else
            {
                if ( DiagnosticTrace.Critical )
                {
                    InternalErrorTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                        ""
                        );
                }

                Debug.Assert( false, "OletxVolatileEnlistment.Prepare, invalid state." );
                throw new InvalidOperationException( SR.GetString( SR.InternalError ) );
            }

        }

        internal void Commit()
        {
            OletxVolatileEnlistmentState localState = OletxVolatileEnlistmentState.Active;
            IEnlistmentNotificationInternal localEnlistmentNotification = null;

            lock ( this )
            {
                // The app may have already called EnlistmentDone.  If this occurs, don't bother sending
                // the notification to the app and we don't need to tell the proxy.
                if ( OletxVolatileEnlistmentState.Prepared == state )
                {
                    localState = state = OletxVolatileEnlistmentState.Committing;
                    localEnlistmentNotification = iEnlistmentNotification;
                }
                else
                {
                    localState = state;
                }
            }

            // Tell the application to do the work.
            if ( OletxVolatileEnlistmentState.Committing == localState )
            {
                if ( null != localEnlistmentNotification )
                {
                    if ( DiagnosticTrace.Verbose )
                    {
                        EnlistmentNotificationCallTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                            this.InternalTraceIdentifier,
                            NotificationCall.Commit
                            );
                    }

                    localEnlistmentNotification.Commit( this );
                }
                else
                {
                    if ( DiagnosticTrace.Critical )
                    {
                        InternalErrorTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                            ""
                            );
                    }

                    Debug.Assert( false, "OletxVolatileEnlistment.Commit, no enlistmentNotification member." );
                    throw new InvalidOperationException( SR.GetString( SR.InternalError ) );
                }
            }
            else if ( OletxVolatileEnlistmentState.Done == localState )
            {
                // Early Exit - state was Done
            }
            else
            {
                if ( DiagnosticTrace.Critical )
                {
                    InternalErrorTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                        ""
                        );
                }

                Debug.Assert( false, "OletxVolatileEnlistment.Commit, invalid state." );
                throw new InvalidOperationException( SR.GetString( SR.InternalError ) );
            }

        }

        internal void Rollback()
        {
            OletxVolatileEnlistmentState localState = OletxVolatileEnlistmentState.Active;
            IEnlistmentNotificationInternal localEnlistmentNotification = null;

            lock ( this )
            {
                // The app may have already called EnlistmentDone.  If this occurs, don't bother sending
                // the notification to the app and we don't need to tell the proxy.
                if ( ( OletxVolatileEnlistmentState.Prepared == state ) ||
                    ( OletxVolatileEnlistmentState.Active == state )
                    )
                {
                    localState = state = OletxVolatileEnlistmentState.Aborting;
                    localEnlistmentNotification = iEnlistmentNotification;
                }
                else
                {
                    if ( OletxVolatileEnlistmentState.Preparing == state )
                    {
                        pendingOutcome = TransactionStatus.Aborted;
                    }
                    localState = state;
                }
            }

            // Tell the application to do the work.
            if ( OletxVolatileEnlistmentState.Aborting == localState )
            {
                if ( null != localEnlistmentNotification )
                {
                    if ( DiagnosticTrace.Verbose )
                    {
                        EnlistmentNotificationCallTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                            this.InternalTraceIdentifier,
                            NotificationCall.Rollback
                            );
                    }

                    localEnlistmentNotification.Rollback( this );
                }

                // There is a small race where Rollback could be called when the enlistment is already
                // aborting the transaciton, so just ignore that call.  When the app enlistment
                // finishes responding to its Rollback notification with EnlistmentDone, things will get
                // cleaned up.
            }
            else if ( OletxVolatileEnlistmentState.Preparing == localState )
            {
                // We need to tolerate this state, but we have already marked the
                // enlistment as pendingRollback, so there is nothing else to do here.
            }
            else if ( OletxVolatileEnlistmentState.Done == localState )
            {
                // Early Exit - state was Done
            }
            else
            {
                if ( DiagnosticTrace.Critical )
                {
                    InternalErrorTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                        ""
                        );
                }

                Debug.Assert( false, "OletxVolatileEnlistment.Rollback, invalid state." );
                throw new InvalidOperationException( SR.GetString( SR.InternalError ) );
            }

        }

        internal void InDoubt()
        {
            OletxVolatileEnlistmentState localState = OletxVolatileEnlistmentState.Active;
            IEnlistmentNotificationInternal localEnlistmentNotification = null;

            lock ( this )
            {
                // The app may have already called EnlistmentDone.  If this occurs, don't bother sending
                // the notification to the app and we don't need to tell the proxy.
                if ( OletxVolatileEnlistmentState.Prepared == state )
                {
                    localState = state = OletxVolatileEnlistmentState.InDoubt;
                    localEnlistmentNotification = iEnlistmentNotification;
                }
                else
                {
                    if ( OletxVolatileEnlistmentState.Preparing == state )
                    {
                        pendingOutcome = TransactionStatus.InDoubt;
                    }
                    localState = state;
                }
            }

            // Tell the application to do the work.
            if ( OletxVolatileEnlistmentState.InDoubt == localState )
            {
                if ( null != localEnlistmentNotification )
                {
                    if ( DiagnosticTrace.Verbose )
                    {
                        EnlistmentNotificationCallTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                            this.InternalTraceIdentifier,
                            NotificationCall.InDoubt
                            );
                    }

                    localEnlistmentNotification.InDoubt( this );
                }
                else
                {
                    if ( DiagnosticTrace.Critical )
                    {
                        InternalErrorTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                            ""
                            );
                    }

                    Debug.Assert( false, "OletxVolatileEnlistment.InDoubt, no enlistmentNotification member." );
                    throw new InvalidOperationException( SR.GetString( SR.InternalError ) );
                }
            }
            else if ( OletxVolatileEnlistmentState.Preparing == localState )
            {
                // We have already set pendingOutcome, so there is nothing else to do.
            }
            else if ( OletxVolatileEnlistmentState.Done == localState )
            {
                // Early Exit - state was Done
            }
            else
            {
                if ( DiagnosticTrace.Critical )
                {
                    InternalErrorTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                        ""
                        );
                }

                Debug.Assert( false, "OletxVolatileEnlistment.InDoubt, invalid state." );
                throw new InvalidOperationException( SR.GetString( SR.InternalError ) );
            }

        }

        void IPromotedEnlistment.EnlistmentDone()
        {
            if ( DiagnosticTrace.Verbose )
            {
                MethodEnteredTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                    "OletxEnlistment.EnlistmentDone"
                    );
                EnlistmentCallbackPositiveTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                    this.InternalTraceIdentifier,
                    EnlistmentCallback.Done
                    );
            }

            OletxVolatileEnlistmentState localState = OletxVolatileEnlistmentState.Active;
            OletxVolatileEnlistmentContainer localContainer = null;

            lock ( this )
            {
                localState = state;
                localContainer = container;

                if ( ( OletxVolatileEnlistmentState.Active != state ) &&
                     ( OletxVolatileEnlistmentState.Preparing != state ) &&
                     ( OletxVolatileEnlistmentState.Aborting != state ) &&
                     ( OletxVolatileEnlistmentState.Committing != state ) &&
                     ( OletxVolatileEnlistmentState.InDoubt != state )
                   )
                {
                    throw TransactionException.CreateEnlistmentStateException( SR.GetString( SR.TraceSourceOletx ), null, this.DistributedTxId );
                }

                state = OletxVolatileEnlistmentState.Done;
            }

            // For the Preparing state, we need to decrement the outstanding
            // count with the container.  If the state is Active, it is an early vote so we
            // just stay in the Done state and when we get the Prepare, we will vote appropriately.
            if ( OletxVolatileEnlistmentState.Preparing == localState )
            {
                if ( null != localContainer )
                {
                    // Specify true.  If aborting, it is okay because the transaction is already
                    // aborting.
                    localContainer.DecrementOutstandingNotifications( true );
                }
            }

            if ( DiagnosticTrace.Verbose )
            {
                MethodExitedTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                    "OletxEnlistment.EnlistmentDone"
                    );
            }
        }

        void IPromotedEnlistment.Prepared()
        {
            if ( DiagnosticTrace.Verbose )
            {
                MethodEnteredTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                    "OletxPreparingEnlistment.Prepared"
                    );
                EnlistmentCallbackPositiveTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                    this.InternalTraceIdentifier,
                    EnlistmentCallback.Prepared
                    );
            }

            OletxVolatileEnlistmentContainer localContainer = null;
            TransactionStatus localPendingOutcome = TransactionStatus.Active;

            lock ( this )
            {
                if ( OletxVolatileEnlistmentState.Preparing != state )
                {
                    throw TransactionException.CreateEnlistmentStateException( SR.GetString( SR.TraceSourceOletx ), null, this.DistributedTxId );
                }

                state = OletxVolatileEnlistmentState.Prepared;
                localPendingOutcome = pendingOutcome;

                if ( null == container )
                {
                    if ( DiagnosticTrace.Critical )
                    {
                        InternalErrorTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                            ""
                            );
                    }

                    Debug.Assert( false, "OletxVolatileEnlistment.Prepared, no container member." );
                    throw new InvalidOperationException( SR.GetString( SR.InternalError ) );
                }

                localContainer = container;
            }

            // Vote yes.
            localContainer.DecrementOutstandingNotifications( true );

            switch ( localPendingOutcome )
            {
                case TransactionStatus.Active:
                {
                    // nothing to do.  Everything is proceeding as normal.
                    break;
                }
                case TransactionStatus.Aborted:
                {
                    // The transaction aborted while the Prepare was outstanding.
                    // We need to tell the app to rollback.
                    Rollback();
                    break;
                }
                case TransactionStatus.InDoubt:
                {
                    // The transaction went InDoubt while the Prepare was outstanding.
                    // We need to tell the app.
                    InDoubt();
                    break;
                }
                default:
                {
                    // This shouldn't happen.
                    if ( DiagnosticTrace.Critical )
                    {
                        InternalErrorTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                            ""
                            );
                    }

                    Debug.Assert( false, "OletxVolatileEnlistment.Prepared, invalid pending outcome value." );
                    throw new InvalidOperationException( SR.GetString( SR.InternalError ) );
                }
            }


            if ( DiagnosticTrace.Verbose )
            {
                MethodExitedTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                    "OletxPreparingEnlistment.Prepared"
                    );
            }
        }


        void IPromotedEnlistment.ForceRollback()
        {
            ((IPromotedEnlistment)this).ForceRollback( null );
        }

        void IPromotedEnlistment.ForceRollback(Exception e)
        {
            if ( DiagnosticTrace.Verbose )
            {
                MethodEnteredTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                    "OletxPreparingEnlistment.ForceRollback"
                    );
            }

            if ( DiagnosticTrace.Warning )
            {
                EnlistmentCallbackNegativeTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                    this.InternalTraceIdentifier,
                    EnlistmentCallback.ForceRollback
                    );
            }

            OletxVolatileEnlistmentContainer localContainer = null;

            lock ( this )
            {
                if ( OletxVolatileEnlistmentState.Preparing != state )
                {
                    throw TransactionException.CreateEnlistmentStateException( SR.GetString( SR.TraceSourceOletx ), null, this.DistributedTxId );
                }

                // There are no more notifications that need to happen on this enlistment.
                state = OletxVolatileEnlistmentState.Done;

                if ( null == container )
                {
                    if ( DiagnosticTrace.Critical )
                    {
                        InternalErrorTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                            ""
                            );
                    }

                    Debug.Assert( false, "OletxVolatileEnlistment.ForceRollback, no container member." );
                    throw new InvalidOperationException( SR.GetString( SR.InternalError ) );
                }

                localContainer = container;
            }

            Interlocked.CompareExchange<Exception>( ref this.oletxTransaction.realOletxTransaction.innerException, e, null );

            // Vote no.
            localContainer.DecrementOutstandingNotifications( false );

            if ( DiagnosticTrace.Verbose )
            {
                MethodExitedTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                    "OletxPreparingEnlistment.ForceRollback"
                    );
            }
        }

        void IPromotedEnlistment.Committed()
        {
            throw new InvalidOperationException();
        }

        void IPromotedEnlistment.Aborted()
        {
            throw new InvalidOperationException();
        }

        void IPromotedEnlistment.Aborted(Exception e)
        {
            throw new InvalidOperationException();
        }

        void IPromotedEnlistment.InDoubt()
        {
            throw new InvalidOperationException();
        }

        void IPromotedEnlistment.InDoubt(Exception e)
        {
            throw new InvalidOperationException();
        }

        byte[] IPromotedEnlistment.GetRecoveryInformation()
        {
            throw TransactionException.CreateInvalidOperationException( SR.GetString( SR.TraceSourceOletx ),
                SR.GetString( SR.VolEnlistNoRecoveryInfo), null, this.DistributedTxId );
        }

        InternalEnlistment IPromotedEnlistment.InternalEnlistment
        {
            get
            {
                return this.internalEnlistment;
            }

            set
            {
                this.internalEnlistment = value;
            }
        }

    }
}
