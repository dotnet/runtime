using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;

namespace System.Transactions
{
    internal enum EnlistmentType
    {
        Volatile = 0,
        Durable = 1,
        PromotableSinglePhase = 2
    }

    internal enum NotificationCall
    {
        // IEnlistmentNotification
        Prepare = 0,
        Commit = 1,
        Rollback = 2,
        InDoubt = 3,
        // ISinglePhaseNotification
        SinglePhaseCommit = 4,
        // IPromotableSinglePhaseNotification
        Promote = 5
    }

    internal enum EnlistmentCallback
    {
        Done = 0,
        Prepared = 1,
        ForceRollback = 2,
        Committed = 3,
        Aborted = 4,
        InDoubt = 5
    }
    internal enum TransactionScopeResult
    {
        CreatedTransaction = 0,
        UsingExistingCurrent = 1,
        TransactionPassed = 2,
        DependentTransactionPassed = 3,
        NoTransaction = 4
    }

    internal enum TransactionExceptionType
    {
        InvalidOperationException = 0,
        TransactionAbortedException = 1,
        TransactionException = 2,
        TransactionInDoubtException = 3,
        TransactionManagerCommunicationException = 4,
        UnrecognizedRecoveryInformation = 5
    }

    internal enum TraceSourceType
    {
        TraceSourceBase = 0,
        TraceSourceLtm = 1,
        TraceSourceDistributed = 2
    }
    /// <summary>Provides an event source for tracing Transactions information.</summary>
    [EventSource(
        Name = "System.Transactions.TransactionsEventSource",
        Guid = "8ac2d80a-1f1a-431b-ace4-bff8824aef0b",
        LocalizationResources = "FxResources.System.Transactions.SR")]
    internal sealed class TransactionsEtwProvider : EventSource
    {
        /// <summary>
        /// Defines the singleton instance for the Transactions ETW provider.
        /// The Transactions provider GUID is {8ac2d80a-1f1a-431b-ace4-bff8824aef0b}.
        /// </summary>
        /// 


        internal readonly static TransactionsEtwProvider Log = new TransactionsEtwProvider();
        /// <summary>Prevent external instantiation.  All logging should go through the Log instance.</summary>
        private TransactionsEtwProvider() { }

        /// <summary>Enabled for all keywords.</summary>
        private const EventKeywords ALL_KEYWORDS = (EventKeywords)(-1);

        //-----------------------------------------------------------------------------------
        //        
        // Transactions Event IDs (must be unique)
        //

        /// <summary>The event ID for configured default timeout adjusted event.</summary>
        private const int CONFIGURED_DEFAULT_TIMEOUT_ADJUSTED_EVENTID = 1;
        /// <summary>The event ID for the enlistment abort event.</summary>
        private const int ENLISTMENT_ABORTED_EVENTID = 2;
        /// <summary>The event ID for the enlistment commit event.</summary>
        private const int ENLISTMENT_COMMITTED_EVENTID = 3;
        /// <summary>The event ID for the enlistment done event.</summary>
        private const int ENLISTMENT_DONE_EVENTID = 4;
        /// <summary>The event ID for the enlistment status.</summary>
        private const int ENLISTMENT_EVENTID = 5;
        /// <summary>The event ID for the enlistment forcerollback event.</summary>
        private const int ENLISTMENT_FORCEROLLBACK_EVENTID = 6;
        /// <summary>The event ID for the enlistment indoubt event.</summary>
        private const int ENLISTMENT_INDOUBT_EVENTID = 7;
        /// <summary>The event ID for the enlistment prepared event.</summary>
        private const int ENLISTMENT_PREPARED_EVENTID = 8;
        /// <summary>The event ID for exception consumed event.</summary>
        private const int EXCEPTION_CONSUMED_BASE_EVENTID = 9;
        /// <summary>The event ID for exception consumed event.</summary>
        private const int EXCEPTION_CONSUMED_LTM_EVENTID = 10;
        /// <summary>The event ID for method enter event.</summary>
        private const int METHOD_ENTER_LTM_EVENTID = 11;
        /// <summary>The event ID for method exit event.</summary>
        private const int METHOD_EXIT_LTM_EVENTID = 12;
        /// <summary>The event ID for method enter event.</summary>
        private const int METHOD_ENTER_BASE_EVENTID = 13;
        /// <summary>The event ID for method exit event.</summary>
        private const int METHOD_EXIT_BASE_EVENTID = 14;
        /// <summary>The event ID for method enter event.</summary>
        private const int METHOD_ENTER_DISTRIBUTED_EVENTID = 15;
        /// <summary>The event ID for method exit event.</summary>
        private const int METHOD_EXIT_DISTRIBUTED_EVENTID = 16;
        /// <summary>The event ID for transaction aborted event.</summary>
        private const int TRANSACTION_ABORTED_EVENTID = 17;
        /// <summary>The event ID for the transaction clone create event.</summary>
        private const int TRANSACTION_CLONECREATE_EVENTID = 18;
        /// <summary>The event ID for the transaction commit event.</summary>
        private const int TRANSACTION_COMMIT_EVENTID = 19;
        /// <summary>The event ID for transaction committed event.</summary>
        private const int TRANSACTION_COMMITTED_EVENTID = 20;
        /// <summary>The event ID for when we encounter a new Transactions object that hasn't had its name traced to the trace file.</summary>
        private const int TRANSACTION_CREATED_EVENTID = 21;
        /// <summary>The event ID for the transaction dependent clone complete event.</summary>
        private const int TRANSACTION_DEPENDENT_CLONE_COMPLETE_EVENTID = 22;
        /// <summary>The event ID for the transaction exception event.</summary>
        private const int TRANSACTION_EXCEPTION_LTM_EVENTID = 23;
        /// <summary>The event ID for the transaction exception event.</summary>
        private const int TRANSACTION_EXCEPTION_BASE_EVENTID = 24;
        /// <summary>The event ID for transaction indoubt event.</summary>
        private const int TRANSACTION_INDOUBT_EVENTID = 25;
        /// <summary>The event ID for the transaction invalid operation event.</summary>
        private const int TRANSACTION_INVALID_OPERATION_EVENTID = 26;
        /// <summary>The event ID for transaction promoted event.</summary>
        private const int TRANSACTION_PROMOTED_EVENTID = 27;
        /// <summary>The event ID for the transaction rollback event.</summary>
        private const int TRANSACTION_ROLLBACK_EVENTID = 28;
        /// <summary>The event ID for the transaction serialized event.</summary>
        private const int TRANSACTION_SERIALIZED_EVENTID = 29;
        /// <summary>The event ID for transaction timeout event.</summary>
        private const int TRANSACTION_TIMEOUT_EVENTID = 30;
        /// <summary>The event ID for transactionmanager recovery complete event.</summary>
        private const int TRANSACTIONMANAGER_RECOVERY_COMPLETE_EVENTID = 31;
        /// <summary>The event ID for transactionmanager reenlist event.</summary>
        private const int TRANSACTIONMANAGER_REENLIST_EVENTID = 32;
        /// <summary>The event ID for transactionscope created event.</summary>
        private const int TRANSACTIONSCOPE_CREATED_EVENTID = 33;
        /// <summary>The event ID for transactionscope current changed event.</summary>
        private const int TRANSACTIONSCOPE_CURRENT_CHANGED_EVENTID = 34;
        /// <summary>The event ID for transactionscope nested incorrectly event.</summary>
        private const int TRANSACTIONSCOPE_DISPOSED_EVENTID = 35;
        /// <summary>The event ID for transactionscope incomplete event.</summary>
        private const int TRANSACTIONSCOPE_INCOMPLETE_EVENTID = 36;
        /// <summary>The event ID for transactionscope internal error event.</summary>
        private const int TRANSACTIONSCOPE_INTERNAL_ERROR_EVENTID = 37;
        /// <summary>The event ID for transactionscope nested incorrectly event.</summary>
        private const int TRANSACTIONSCOPE_NESTED_INCORRECTLY_EVENTID = 38;
        /// <summary>The event ID for transactionscope timeout event.</summary>
        private const int TRANSACTIONSCOPE_TIMEOUT_EVENTID = 39;
        /// <summary>The event ID for enlistment event.</summary>
        private const int TRANSACTIONSTATE_ENLIST_EVENTID = 40;

        //-----------------------------------------------------------------------------------
        //        
        // Transactions Events
        //

        private const string NullInstance = "(null)";
        //-----------------------------------------------------------------------------------
        //        
        // Transactions Events
        //
        [NonEvent]
        public static string IdOf(object value) => value != null ? value.GetType().Name + "#" + GetHashCode(value) : NullInstance;

        [NonEvent]
        public static int GetHashCode(object value) => value?.GetHashCode() ?? 0;

        #region Transaction Creation
        /// <summary>Trace an event when a new transaction is created.</summary>
        /// <param name="transaction">The transaction that was created.</param>
        /// <param name="type">The type of transaction.</param>Method
        [NonEvent]
        internal void TransactionCreated(Transaction transaction, string type)
        {
            Debug.Assert(transaction != null, "Transaction needed for the ETW event.");

            if (IsEnabled(EventLevel.Informational, ALL_KEYWORDS))
            {
                TransactionCreated(
                    transaction.TransactionTraceId.TransactionIdentifier, type);
            }
        }

        [Event(TRANSACTION_CREATED_EVENTID, Keywords = Keywords.TraceLtm, Level = EventLevel.Informational, Task = Tasks.Transaction, Opcode = Opcodes.Create, Message = "Transaction Created. ID is {0}, type is {1}")]
        private void TransactionCreated(string transactionIdentifier, string type)
        {
            WriteEvent(TRANSACTION_CREATED_EVENTID, transactionIdentifier, type);
        }
        #endregion

        #region Transaction Clone Create
        /// <summary>Trace an event when a new transaction is clone created.</summary>
        /// <param name="transaction">The transaction that was clone created.</param>
        /// <param name="type">The type of transaction.</param>
        [NonEvent]
        internal void TransactionCloneCreate(Transaction transaction, string type)
        {
            Debug.Assert(transaction != null, "Transaction needed for the ETW event.");

            if (IsEnabled(EventLevel.Informational, ALL_KEYWORDS))
            {
                TransactionCloneCreate(
                    transaction.TransactionTraceId.TransactionIdentifier, type);
            }
        }

        [Event(TRANSACTION_CLONECREATE_EVENTID, Keywords = Keywords.TraceLtm, Level = EventLevel.Informational, Task = Tasks.Transaction, Opcode = Opcodes.CloneCreate, Message = "Transaction Clone Created. ID is {0}, type is {1}")]
        private void TransactionCloneCreate(string transactionIdentifier, string type)
        {
            WriteEvent(TRANSACTION_CLONECREATE_EVENTID, transactionIdentifier, type);
        }
        #endregion

        #region Transaction Serialized
        /// <summary>Trace an event when a transaction is serialized.</summary>
        /// <param name="transaction">The transaction that was serialized.</param>
        /// <param name="type">The type of transaction.</param>
        [NonEvent]
        internal void TransactionSerialized(Transaction transaction, string type)
        {
            Debug.Assert(transaction != null, "Transaction needed for the ETW event.");

            if (IsEnabled(EventLevel.Informational, ALL_KEYWORDS))
            {
                TransactionSerialized(
                    transaction.TransactionTraceId.TransactionIdentifier, type);
            }
        }

        [Event(TRANSACTION_SERIALIZED_EVENTID, Keywords = Keywords.TraceLtm, Level = EventLevel.Informational, Task = Tasks.Transaction, Opcode = Opcodes.Serialized, Message = "Transaction Serialized. ID is {0}, type is {1}")]
        private void TransactionSerialized(string transactionIdentifier, string type)
        {
            WriteEvent(TRANSACTION_SERIALIZED_EVENTID, transactionIdentifier, type);
        }
        #endregion

        #region Transaction Exception
        /// <summary>Trace an event when an exception happens.</summary>
        /// <param name="traceSource">trace source</param>
        /// <param name="type">The type of transaction.</param>
        /// <param name="message">The message for the exception.</param>
        /// <param name="innerExceptionStr">The inner exception.</param>
        [NonEvent]
        internal void TransactionExceptionTrace(TraceSourceType traceSource, TransactionExceptionType type, string message, string innerExceptionStr)
        {
            if (IsEnabled(EventLevel.Error, ALL_KEYWORDS))
            {
                if (traceSource == TraceSourceType.TraceSourceBase)
                {
                    TransactionExceptionBase(type.ToString(), message, innerExceptionStr);
                }
                else
                {
                    TransactionExceptionLtm(type.ToString(), message, innerExceptionStr);
                }
            }
        }

        /// <summary>Trace an event when an exception happens.</summary>
        /// <param name="type">The type of transaction.</param>
        /// <param name="message">The message for the exception.</param>
        /// <param name="innerExceptionStr">The inner exception.</param>
        [NonEvent]
        internal void TransactionExceptionTrace(TransactionExceptionType type, string message, string innerExceptionStr)
        {
            if (IsEnabled(EventLevel.Error, ALL_KEYWORDS))
            {
                TransactionExceptionLtm(type.ToString(), message, innerExceptionStr);
            }
        }

        [Event(TRANSACTION_EXCEPTION_BASE_EVENTID, Keywords = Keywords.TraceBase, Level = EventLevel.Error, Task = Tasks.TransactionException, Message = "Transaction Exception. Type is {0}, message is {1}, InnerException is {2}")]
        private void TransactionExceptionBase(string type, string message, string innerExceptionStr)
        {
            WriteEvent(TRANSACTION_EXCEPTION_BASE_EVENTID, type, message, innerExceptionStr);
        }

        [Event(TRANSACTION_EXCEPTION_LTM_EVENTID, Keywords = Keywords.TraceLtm, Level = EventLevel.Error, Task = Tasks.TransactionException, Message = "Transaction Exception. Type is {0}, message is {1}, InnerException is {2}")]
        private void TransactionExceptionLtm(string type, string message, string innerExceptionStr)
        {
            WriteEvent(TRANSACTION_EXCEPTION_LTM_EVENTID, type, message, innerExceptionStr);
        }
        #endregion

        #region Transaction Invalid Operation
        /// <summary>Trace an event when an invalid operation happened on a transaction.</summary>
        /// <param name="type">The type of transaction.</param>
        /// <param name="operation">The operationont the transaction.</param>
        [NonEvent]
        internal void InvalidOperation(string type, string operation)
        {
            if (IsEnabled(EventLevel.Error, ALL_KEYWORDS))
            {
                TransactionInvalidOperation(
                    string.Empty, type, operation);
            }
        }
        [Event(TRANSACTION_INVALID_OPERATION_EVENTID, Keywords = Keywords.TraceBase, Level = EventLevel.Error, Task = Tasks.Transaction, Opcode = Opcodes.InvalidOperation, Message = "Transaction Invalid Operation. ID is {0}, type is {1} and operation is {2}")]
        private void TransactionInvalidOperation(string transactionIdentifier, string type, string operation)
        {
            WriteEvent(TRANSACTION_INVALID_OPERATION_EVENTID, transactionIdentifier, type, operation);
        }
        #endregion

        #region Transaction Rollback
        /// <summary>Trace an event when rollback on a transaction.</summary>
        /// <param name="transaction">The transaction to rollback.</param>
        /// <param name="type">The type of transaction.</param>
        [NonEvent]
        internal void TransactionRollback(Transaction transaction, string type)
        {
            Debug.Assert(transaction != null, "Transaction needed for the ETW event.");

            if (IsEnabled(EventLevel.Warning, ALL_KEYWORDS))
            {
                TransactionRollback(
                    transaction.TransactionTraceId.TransactionIdentifier, type);
            }
        }

        [Event(TRANSACTION_ROLLBACK_EVENTID, Keywords = Keywords.TraceLtm, Level = EventLevel.Warning, Task = Tasks.Transaction, Opcode = Opcodes.Rollback, Message = "Transaction Rollback. ID is {0}, type is {1}")]
        private void TransactionRollback(string transactionIdentifier, string type)
        {
            WriteEvent(TRANSACTION_ROLLBACK_EVENTID, transactionIdentifier, type);
        }
        #endregion

        #region Transaction Dependent Clone Complete
        /// <summary>Trace an event when transaction dependent clone complete.</summary>
        /// <param name="transaction">The transaction that do dependent clone.</param>
        /// <param name="type">The type of transaction.</param>
        [NonEvent]
        internal void TransactionDependentCloneComplete(Transaction transaction, string type)
        {
            Debug.Assert(transaction != null, "Transaction needed for the ETW event.");

            if (IsEnabled(EventLevel.Informational, ALL_KEYWORDS))
            {
                TransactionDependentCloneComplete(
                    transaction.TransactionTraceId.TransactionIdentifier, type);
            }
        }

        [Event(TRANSACTION_DEPENDENT_CLONE_COMPLETE_EVENTID, Keywords = Keywords.TraceLtm, Level = EventLevel.Informational, Task = Tasks.Transaction, Opcode = Opcodes.DependentCloneComplete, Message = "Transaction Dependent Clone Completed. ID is {0}, type is {1}")]
        private void TransactionDependentCloneComplete(string transactionIdentifier, string type)
        {
            WriteEvent(TRANSACTION_DEPENDENT_CLONE_COMPLETE_EVENTID, transactionIdentifier, type);
        }
        #endregion
        
        #region Transaction Commit
        /// <summary>Trace an event when there is commit on that transaction.</summary>
        /// <param name="transaction">The transaction to commit.</param>
        /// <param name="type">The type of transaction.</param>
        [NonEvent]
        internal void TransactionCommit(Transaction transaction, string type)
        {
            Debug.Assert(transaction != null, "Transaction needed for the ETW event.");

            if (IsEnabled(EventLevel.Verbose, ALL_KEYWORDS))
            {
                TransactionCommit(
                    transaction.TransactionTraceId.TransactionIdentifier, type);
            }
        }

        [Event(TRANSACTION_COMMIT_EVENTID, Keywords = Keywords.TraceLtm, Level = EventLevel.Verbose, Task = Tasks.Transaction, Opcode = Opcodes.Commit, Message = "Transaction Commit: ID is {0}, type is {1}")]
        private void TransactionCommit(string transactionIdentifier, string type)
        {
            WriteEvent(TRANSACTION_COMMIT_EVENTID, transactionIdentifier, type);
        }
        #endregion

        #region Enlistment
        /// <summary>Trace an event for enlistment status.</summary>
        /// <param name="enlisment">The enlistment to report status.</param>
        /// <param name="notificationCall">The notification call on the enlistment.</param>
        [NonEvent]
        internal void EnlistmentStatus(InternalEnlistment enlistment, NotificationCall notificationCall)
        {
            Debug.Assert(enlistment != null, "Enlistment needed for the ETW event.");

            if (IsEnabled(EventLevel.Verbose, ALL_KEYWORDS))
            {
                EnlistmentStatus(enlistment.EnlistmentTraceId.EnlistmentIdentifier, notificationCall.ToString());
            }
        }

        [Event(ENLISTMENT_EVENTID, Keywords = Keywords.TraceLtm, Level = EventLevel.Verbose, Task = Tasks.Enlistment, Message = "Enlistment status: ID is {0}, notificationcall is {1}")]
        private void EnlistmentStatus(int enlistmentIdentifier, string notificationCall)
        {
            WriteEvent(ENLISTMENT_EVENTID, enlistmentIdentifier, notificationCall);
        }
        #endregion

        #region Enlistment Done
        /// <summary>Trace an event for enlistment done.</summary>
        /// <param name="enlisment">The enlistment done.</param>
        [NonEvent]
        internal void EnlistmentDone(InternalEnlistment enlistment)
        {
            Debug.Assert(enlistment != null, "Enlistment needed for the ETW event.");

            if (IsEnabled(EventLevel.Verbose, ALL_KEYWORDS))
            {
                EnlistmentDone(
                    enlistment.EnlistmentTraceId.EnlistmentIdentifier);
            }
        }

        [Event(ENLISTMENT_DONE_EVENTID, Keywords = Keywords.TraceLtm, Level = EventLevel.Verbose, Task = Tasks.Enlistment, Opcode = Opcodes.Done, Message = "Enlistment.Done: ID is {0}")]
        private void EnlistmentDone(int enlistmentIdentifier)
        {
            WriteEvent(ENLISTMENT_DONE_EVENTID, enlistmentIdentifier);
        }
        #endregion

        #region Enlistment Prepared
        /// <summary>Trace an event for enlistment prepared.</summary>
        /// <param name="enlisment">The enlistment prepared.</param>
        [NonEvent]
        internal void EnlistmentPrepared(InternalEnlistment enlistment)
        {
            Debug.Assert(enlistment != null, "Enlistment needed for the ETW event.");

            if (IsEnabled(EventLevel.Verbose, ALL_KEYWORDS))
            {
                EnlistmentPrepared(
                    enlistment.EnlistmentTraceId.EnlistmentIdentifier);
            }
        }

        [Event(ENLISTMENT_PREPARED_EVENTID, Keywords = Keywords.TraceLtm, Level = EventLevel.Verbose, Task = Tasks.Enlistment, Opcode = Opcodes.Prepared, Message = "PreparingEnlistment.Prepared: ID is {0}")]
        private void EnlistmentPrepared(int enlistmentIdentifier)
        {
            WriteEvent(ENLISTMENT_PREPARED_EVENTID, enlistmentIdentifier);
        }
        #endregion

        #region Enlistment ForceRollback
        /// <summary>Trace an enlistment that will forcerollback.</summary>
        /// <param name="enlistment">The enlistment to forcerollback.</param>
        [NonEvent]
        internal void EnlistmentForceRollback(InternalEnlistment enlistment)
        {
            Debug.Assert(enlistment != null, "Enlistment needed for the ETW event.");

            if (IsEnabled(EventLevel.Warning, ALL_KEYWORDS))
            {
                EnlistmentForceRollback(
                    enlistment.EnlistmentTraceId.EnlistmentIdentifier);
            }
        }

        [Event(ENLISTMENT_FORCEROLLBACK_EVENTID, Keywords = Keywords.TraceLtm, Level = EventLevel.Warning, Task = Tasks.Enlistment, Opcode = Opcodes.ForceRollback, Message = "Enlistment forceRollback: ID is {0}")]
        private void EnlistmentForceRollback(int enlistmentIdentifier)
        {
            WriteEvent(ENLISTMENT_FORCEROLLBACK_EVENTID, enlistmentIdentifier);
        }
        #endregion

        #region Enlistment Aborted
        /// <summary>Trace an enlistment that aborted.</summary>
        /// <param name="enlistment">The enlistment aborted.</param>
        [NonEvent]
        internal void EnlistmentAborted(InternalEnlistment enlistment)
        {
            Debug.Assert(enlistment != null, "Enlistment needed for the ETW event.");

            if (IsEnabled(EventLevel.Warning, ALL_KEYWORDS))
            {
                EnlistmentAborted(
                    enlistment.EnlistmentTraceId.EnlistmentIdentifier);
            }
        }

        [Event(ENLISTMENT_ABORTED_EVENTID, Keywords = Keywords.TraceLtm, Level = EventLevel.Warning, Task = Tasks.Enlistment, Opcode = Opcodes.Aborted, Message = "Enlistment SinglePhase Aborted: ID is {0}")]
        private void EnlistmentAborted(int enlistmentIdentifier)
        {
            WriteEvent(ENLISTMENT_ABORTED_EVENTID, enlistmentIdentifier);
        }
        #endregion

        #region Enlistment Committed
        /// <summary>Trace an enlistment that committed.</summary>
        /// <param name="enlistment">The enlistment committed.</param>
        [NonEvent]
        internal void EnlistmentCommitted(InternalEnlistment enlistment)
        {
            Debug.Assert(enlistment != null, "Enlistment needed for the ETW event.");

            if (IsEnabled(EventLevel.Verbose, ALL_KEYWORDS))
            {
                EnlistmentCommitted(
                    enlistment.EnlistmentTraceId.EnlistmentIdentifier);
            }
        }

        [Event(ENLISTMENT_COMMITTED_EVENTID, Keywords = Keywords.TraceLtm, Level = EventLevel.Verbose, Task = Tasks.Enlistment, Opcode = Opcodes.Committed, Message = "Enlistment Committed: ID is {0}")]
        private void EnlistmentCommitted(int enlistmentIdentifier)
        {
            WriteEvent(ENLISTMENT_COMMITTED_EVENTID, enlistmentIdentifier);
        }
        #endregion

        #region Enlistment InDoubt
        /// <summary>Trace an enlistment that InDoubt.</summary>
        /// <param name="enlistment">The enlistment Indoubt.</param>
        [NonEvent]
        internal void EnlistmentInDoubt(InternalEnlistment enlistment)
        {
            Debug.Assert(enlistment != null, "Enlistment needed for the ETW event.");

            if (IsEnabled(EventLevel.Warning, ALL_KEYWORDS))
            {
                EnlistmentInDoubt(
                    enlistment.EnlistmentTraceId.EnlistmentIdentifier);
            }
        }

        [Event(ENLISTMENT_INDOUBT_EVENTID, Keywords = Keywords.TraceLtm, Level = EventLevel.Warning, Task = Tasks.Enlistment, Opcode = Opcodes.InDoubt, Message = "Enlistment SinglePhase InDoubt: ID is {0}")]
        private void EnlistmentInDoubt(int enlistmentIdentifier)
        {
            WriteEvent(ENLISTMENT_INDOUBT_EVENTID, enlistmentIdentifier);
        }
        #endregion

        #region Method Enter
        /// <summary>Trace an event when enter a method.</summary>
        /// <param name="traceSource"> trace source</param>
        /// <param name="thisOrContextObject">'this', or another object that serves to provide context for the operation.</param>
        /// <param name="methodname">The name of method.</param>
        [NonEvent]
        internal void MethodEnter(TraceSourceType traceSource, object thisOrContextObject, [CallerMemberName] string methodname = null)
        {
            if (IsEnabled(EventLevel.Verbose, ALL_KEYWORDS))
            {
                if (traceSource == TraceSourceType.TraceSourceLtm)
                {
                    MethodEnterTraceLtm(IdOf(thisOrContextObject), methodname);
                }
                else if (traceSource == TraceSourceType.TraceSourceBase)
                {
                    MethodEnterTraceBase(IdOf(thisOrContextObject), methodname);
                }
                else if (traceSource == TraceSourceType.TraceSourceDistributed)
                {
                    MethodEnterTraceDistributed(IdOf(thisOrContextObject), methodname);
                }
            }
        }

        /// <summary>Trace an event when enter a method.</summary>
        /// <param name="traceSource"> trace source</param>
        /// <param name="methodname">The name of method.</param>
        [NonEvent]
        internal void MethodEnter(TraceSourceType traceSource, [CallerMemberName] string methodname = null)
        {
            if (IsEnabled(EventLevel.Verbose, ALL_KEYWORDS))
            {
                if (traceSource == TraceSourceType.TraceSourceLtm)
                {
                    MethodEnterTraceLtm(string.Empty, methodname);
                }
                else if (traceSource == TraceSourceType.TraceSourceBase)
                {
                    MethodEnterTraceBase(string.Empty, methodname);
                }
                else if (traceSource == TraceSourceType.TraceSourceDistributed)
                {
                    MethodEnterTraceDistributed(string.Empty, methodname);
                }
            }
        }

        [Event(METHOD_ENTER_LTM_EVENTID, Keywords = Keywords.TraceLtm, Level = EventLevel.Verbose, Task = Tasks.Method, Opcode = Opcodes.Enter, Message = "Enter method : {0}.{1}")]
        private void MethodEnterTraceLtm(string thisOrContextObject, string methodname)
        {
            WriteEvent(METHOD_ENTER_LTM_EVENTID, thisOrContextObject, methodname);
        }
        [Event(METHOD_ENTER_BASE_EVENTID, Keywords = Keywords.TraceBase, Level = EventLevel.Verbose, Task = Tasks.Method, Opcode = Opcodes.Enter, Message = "Enter method : {0}.{1}")]
        private void MethodEnterTraceBase(string thisOrContextObject, string methodname)
        {
            WriteEvent(METHOD_ENTER_BASE_EVENTID, thisOrContextObject, methodname);
        }
        [Event(METHOD_ENTER_DISTRIBUTED_EVENTID, Keywords = Keywords.TraceDistributed, Level = EventLevel.Verbose, Task = Tasks.Method, Opcode = Opcodes.Enter, Message = "Enter method : {0}.{1}")]
        private void MethodEnterTraceDistributed(string thisOrContextObject, string methodname)
        {
            WriteEvent(METHOD_ENTER_DISTRIBUTED_EVENTID, thisOrContextObject, methodname);
        }
        #endregion

        #region Method Exit
        /// <summary>Trace an event when enter a method.</summary>
        /// <param name="traceSource"> trace source</param>
        /// <param name="thisOrContextObject">'this', or another object that serves to provide context for the operation.</param>
        /// <param name="methodname">The name of method.</param>
        [NonEvent]
        internal void MethodExit(TraceSourceType traceSource, object thisOrContextObject, [CallerMemberName] string methodname = null)
        {
            if (IsEnabled(EventLevel.Verbose, ALL_KEYWORDS))
            {
                if (traceSource == TraceSourceType.TraceSourceLtm)
                {
                    MethodExitTraceLtm(IdOf(thisOrContextObject), methodname);
                }
                else if (traceSource == TraceSourceType.TraceSourceBase)
                {
                    MethodExitTraceBase(IdOf(thisOrContextObject), methodname);
                }
                else if (traceSource == TraceSourceType.TraceSourceDistributed)
                {
                    MethodExitTraceDistributed(IdOf(thisOrContextObject), methodname);
                }
            }
        }

        /// <summary>Trace an event when enter a method.</summary>
        /// <param name="traceSource"> trace source</param>
        /// <param name="methodname">The name of method.</param>
        [NonEvent]
        internal void MethodExit(TraceSourceType traceSource, [CallerMemberName] string methodname = null)
        {
            if (IsEnabled(EventLevel.Verbose, ALL_KEYWORDS))
            {
                if (traceSource == TraceSourceType.TraceSourceLtm)
                {
                    MethodExitTraceLtm(string.Empty, methodname);
                }
                else if (traceSource == TraceSourceType.TraceSourceBase)
                {
                    MethodExitTraceBase(string.Empty, methodname);
                }
                else if (traceSource == TraceSourceType.TraceSourceDistributed)
                {
                    MethodExitTraceDistributed(string.Empty, methodname);
                }
            }
        }

        [Event(METHOD_EXIT_LTM_EVENTID, Keywords = Keywords.TraceLtm, Level = EventLevel.Verbose, Task = Tasks.Method, Opcode = Opcodes.Exit, Message = "Exit method: {0}.{1}")]
        private void MethodExitTraceLtm(string thisOrContextObject, string methodname)
        {
            WriteEvent(METHOD_EXIT_LTM_EVENTID, thisOrContextObject, methodname);
        }
        [Event(METHOD_EXIT_BASE_EVENTID, Keywords = Keywords.TraceBase, Level = EventLevel.Verbose, Task = Tasks.Method, Opcode = Opcodes.Exit, Message = "Exit method: {0}.{1}")]
        private void MethodExitTraceBase(string thisOrContextObject, string methodname)
        {
            WriteEvent(METHOD_EXIT_BASE_EVENTID, thisOrContextObject, methodname);
        }
        [Event(METHOD_EXIT_DISTRIBUTED_EVENTID, Keywords = Keywords.TraceDistributed, Level = EventLevel.Verbose, Task = Tasks.Method, Opcode = Opcodes.Exit, Message = "Exit method: {0}.{1}")]
        private void MethodExitTraceDistributed(string thisOrContextObject, string methodname)
        {
            WriteEvent(METHOD_EXIT_DISTRIBUTED_EVENTID, thisOrContextObject, methodname);
        }

        #endregion

        #region Exception Consumed
        /// <summary>Trace an event when exception consumed.</summary>
        /// <param name="traceSource">trace source</param>
        /// <param name="exception">The exception.</param>
        [NonEvent]
        internal void ExceptionConsumed(TraceSourceType traceSource, Exception exception)
        {
            if (IsEnabled(EventLevel.Verbose, ALL_KEYWORDS))
            {
                if (traceSource == TraceSourceType.TraceSourceBase)
                {
                    ExceptionConsumedBase(exception.ToString());
                }
                else
                {
                    ExceptionConsumedLtm(exception.ToString());
                }
            }
        }
        /// <summary>Trace an event when exception consumed.</summary>
        /// <param name="exception">The exception.</param>
        [NonEvent]
        internal void ExceptionConsumed(Exception exception)
        {
            if (IsEnabled(EventLevel.Verbose, ALL_KEYWORDS))
            {
                ExceptionConsumedLtm(exception.ToString());
            }
        }

        [Event(EXCEPTION_CONSUMED_BASE_EVENTID, Keywords = Keywords.TraceBase, Level = EventLevel.Verbose, Opcode = Opcodes.ExceptionConsumed, Message = "Exception consumed: {0}")]
        private void ExceptionConsumedBase(string exceptionStr)
        {
            WriteEvent(EXCEPTION_CONSUMED_BASE_EVENTID, exceptionStr);
        }
        [Event(EXCEPTION_CONSUMED_LTM_EVENTID, Keywords = Keywords.TraceLtm, Level = EventLevel.Verbose, Opcode = Opcodes.ExceptionConsumed, Message = "Exception consumed: {0}")]
        private void ExceptionConsumedLtm(string exceptionStr)
        {
            WriteEvent(EXCEPTION_CONSUMED_LTM_EVENTID, exceptionStr);
        }
        #endregion

        #region TransactionManager Reenlist
        /// <summary>Trace an event when reenlist transactionmanager.</summary>
        /// <param name="resourceMangerID">The resource manger ID.</param>
        [NonEvent]
        internal void TransactionManagerReenlist(Guid resourceManagerID)
        {
            if (IsEnabled(EventLevel.Informational, ALL_KEYWORDS))
            {
                TransactionManagerReenlistTrace(resourceManagerID.ToString());
            }
        }

        [Event(TRANSACTIONMANAGER_REENLIST_EVENTID, Keywords = Keywords.TraceBase, Level = EventLevel.Informational, Task = Tasks.TransactionManager, Opcode = Opcodes.Reenlist, Message = "Reenlist in: {0}")]
        private void TransactionManagerReenlistTrace(string rmID)
        {
            WriteEvent(TRANSACTIONMANAGER_REENLIST_EVENTID, rmID);
        }
        #endregion

        #region TransactionManager Recovery Complete
        /// <summary>Trace an event when transactionmanager recovery complete.</summary>
        /// <param name="resourceMangerID">The resource manger ID.</param>
        [NonEvent]
        internal void TransactionManagerRecoveryComplete(Guid resourceManagerID)
        {
            if (IsEnabled(EventLevel.Informational, ALL_KEYWORDS))
            {
                TransactionManagerRecoveryComplete(resourceManagerID.ToString());
            }
        }

        [Event(TRANSACTIONMANAGER_RECOVERY_COMPLETE_EVENTID, Keywords = Keywords.TraceBase, Level = EventLevel.Informational, Task = Tasks.TransactionManager, Opcode = Opcodes.RecoveryComplete, Message = "Recovery complete: {0}")]
        private void TransactionManagerRecoveryComplete(string rmID)
        {
            WriteEvent(TRANSACTIONMANAGER_RECOVERY_COMPLETE_EVENTID, rmID);
        }
        #endregion

        #region Configured Default Timeout Adjusted
        /// <summary>Trace an event when configured default timeout adjusted.</summary>
        [NonEvent]
        internal void ConfiguredDefaultTimeoutAdjusted()
        {
            if (IsEnabled(EventLevel.Warning, ALL_KEYWORDS))
            {
                ConfiguredDefaultTimeoutAdjustedTrace();
            }
        }

        [Event(CONFIGURED_DEFAULT_TIMEOUT_ADJUSTED_EVENTID, Keywords = Keywords.TraceBase, Level = EventLevel.Warning, Task = Tasks.ConfiguredDefaultTimeout, Opcode = Opcodes.Adjusted, Message = "Configured Default Timeout Adjusted")]
        private void ConfiguredDefaultTimeoutAdjustedTrace()
        {
            WriteEvent(CONFIGURED_DEFAULT_TIMEOUT_ADJUSTED_EVENTID);
        }
        #endregion

        #region Transactionscope Created
        /// <summary>Trace an event when a transactionscope is created.</summary>
        /// <param name="transactionID">The transaction ID.</param>
        /// <param name="transactionScopeResult">The transaction scope result.</param>
        [NonEvent]
        internal void TransactionScopeCreated(TransactionTraceIdentifier transactionID, TransactionScopeResult transactionScopeResult )
        {
            if (IsEnabled(EventLevel.Informational, ALL_KEYWORDS))
            {
                TransactionScopeCreated(transactionID.TransactionIdentifier.ToString(), transactionScopeResult);
            }
        }

        [Event(TRANSACTIONSCOPE_CREATED_EVENTID, Keywords = Keywords.TraceBase, Level = EventLevel.Informational, Task = Tasks.TransactionScope, Opcode = Opcodes.Created, Message = "Transactionscope was created: Transaction ID is {0}, TransactionScope Result is {1}")]
        private void TransactionScopeCreated(string transactionID, TransactionScopeResult transactionScopeResult)
        {
            WriteEvent(TRANSACTIONSCOPE_CREATED_EVENTID, transactionID, transactionScopeResult);
        }
        #endregion

        #region Transactionscope Current Changed
        /// <summary>Trace an event when a transactionscope current transaction changed.</summary>
        /// <param name="currenttransactionID">The transaction ID.</param>
        /// <param name="newtransactionID">The new transaction ID.</param>
        [NonEvent]
        internal void TransactionScopeCurrentChanged(TransactionTraceIdentifier currenttransactionID, TransactionTraceIdentifier newtransactionID)
        {
            if (IsEnabled(EventLevel.Warning, ALL_KEYWORDS))
            {
                TransactionScopeCurrentChanged(currenttransactionID.TransactionIdentifier.ToString(), newtransactionID.TransactionIdentifier.ToString());
            }
        }

        [Event(TRANSACTIONSCOPE_CURRENT_CHANGED_EVENTID, Keywords = Keywords.TraceBase, Level = EventLevel.Warning, Task = Tasks.TransactionScope, Opcode = Opcodes.CurrentChanged, Message = "Transactionscope current transaction ID changed from {0} to {1}")]
        private void TransactionScopeCurrentChanged(string currenttransactionID, string newtransactionID)
        {
            WriteEvent(TRANSACTIONSCOPE_CURRENT_CHANGED_EVENTID, currenttransactionID, newtransactionID);
        }
        #endregion

        #region Transactionscope Nested Incorrectly
        /// <summary>Trace an event when a transactionscope is nested incorrectly.</summary>
        /// <param name="transactionID">The transaction ID.</param>
        [NonEvent]
        internal void TransactionScopeNestedIncorrectly(TransactionTraceIdentifier transactionID)
        {
            if (IsEnabled(EventLevel.Warning, ALL_KEYWORDS))
            {
                TransactionScopeNestedIncorrectly(transactionID.TransactionIdentifier.ToString());
            }
        }

        [Event(TRANSACTIONSCOPE_NESTED_INCORRECTLY_EVENTID, Keywords = Keywords.TraceBase, Level = EventLevel.Warning, Task = Tasks.TransactionScope, Opcode = Opcodes.NestedIncorrectly, Message = "Transactionscope nested incorrectly: transaction ID is {0}")]
        private void TransactionScopeNestedIncorrectly(string transactionID)
        {
            WriteEvent(TRANSACTIONSCOPE_NESTED_INCORRECTLY_EVENTID, transactionID);
        }
        #endregion

        #region Transactionscope Disposed
        /// <summary>Trace an event when a transactionscope is disposed.</summary>
        /// <param name="transactionID">The transaction ID.</param>
        [NonEvent]
        internal void TransactionScopeDisposed(TransactionTraceIdentifier transactionID)
        {
            if (IsEnabled(EventLevel.Informational, ALL_KEYWORDS))
            {
                TransactionScopeDisposed(transactionID.TransactionIdentifier.ToString());
            }
        }

        [Event(TRANSACTIONSCOPE_DISPOSED_EVENTID, Keywords = Keywords.TraceBase, Level = EventLevel.Informational, Task = Tasks.TransactionScope, Opcode = Opcodes.Disposed, Message = "Transactionscope disposed: transaction ID is {0}")]
        private void TransactionScopeDisposed(string transactionID)
        {
            WriteEvent(TRANSACTIONSCOPE_DISPOSED_EVENTID, transactionID);
        }
        #endregion

        #region Transactionscope Incomplete
        /// <summary>Trace an event when a transactionscope incomplete.</summary>
        /// <param name="transactionID">The transaction ID.</param>
        [NonEvent]
        internal void TransactionScopeIncomplete(TransactionTraceIdentifier transactionID)
        {
            if (IsEnabled(EventLevel.Warning, ALL_KEYWORDS))
            {
                TransactionScopeIncomplete(transactionID.TransactionIdentifier.ToString());
            }
        }

        [Event(TRANSACTIONSCOPE_INCOMPLETE_EVENTID, Keywords = Keywords.TraceBase, Level = EventLevel.Warning, Task = Tasks.TransactionScope, Opcode = Opcodes.Incomplete, Message = "Transactionscope incomplete: transaction ID is {0}")]
        private void TransactionScopeIncomplete(string transactionID)
        {
            WriteEvent(TRANSACTIONSCOPE_INCOMPLETE_EVENTID, transactionID);
        }
        #endregion

        #region Transactionscope Internal Error
        /// <summary>Trace an event when there is an internal error on transactionscope.</summary>
        /// <param name="error">The error information.</param>
        [NonEvent]
        internal void TransactionScopeInternalError(string error)
        {
            if (IsEnabled(EventLevel.Critical, ALL_KEYWORDS))
            {
                TransactionScopeInternalErrorTrace(error);
            }
        }

        [Event(TRANSACTIONSCOPE_INTERNAL_ERROR_EVENTID, Keywords = Keywords.TraceBase, Level = EventLevel.Critical, Task = Tasks.TransactionScope, Opcode = Opcodes.InternalError, Message = "Transactionscope internal error: {0}")]
        private void TransactionScopeInternalErrorTrace(string error)
        {
            WriteEvent(TRANSACTIONSCOPE_INTERNAL_ERROR_EVENTID, error);
        }
        #endregion

        #region Transactionscope Timeout
        /// <summary>Trace an event when there is timeout on transactionscope.</summary>
        /// <param name="transactionID">The transaction ID.</param>
        [NonEvent]
        internal void TransactionScopeTimeout(TransactionTraceIdentifier transactionID)
        {
            if (IsEnabled(EventLevel.Warning, ALL_KEYWORDS))
            {
                TransactionScopeTimeout(transactionID.TransactionIdentifier.ToString());
            }
        }

        [Event(TRANSACTIONSCOPE_TIMEOUT_EVENTID, Keywords = Keywords.TraceBase, Level = EventLevel.Warning, Task = Tasks.TransactionScope, Opcode = Opcodes.Timeout, Message = "Transactionscope timeout: transaction ID is {0}")]
        private void TransactionScopeTimeout(string transactionID)
        {
            WriteEvent(TRANSACTIONSCOPE_TIMEOUT_EVENTID, transactionID);
        }
        #endregion

        #region Transaction Timeout
        /// <summary>Trace an event when there is timeout on transaction.</summary>
        /// <param name="transactionID">The transaction ID.</param>
        [NonEvent]
        internal void TransactionTimeout(TransactionTraceIdentifier transactionID)
        {
            if (IsEnabled(EventLevel.Warning, ALL_KEYWORDS))
            {
                TransactionTimeout(transactionID.TransactionIdentifier.ToString());
            }
        }

        [Event(TRANSACTION_TIMEOUT_EVENTID, Keywords = Keywords.TraceLtm, Level = EventLevel.Warning, Task = Tasks.Transaction, Opcode = Opcodes.Timeout, Message = "Transaction timeout: transaction ID is {0}")]
        private void TransactionTimeout(string transactionID)
        {
            WriteEvent(TRANSACTION_TIMEOUT_EVENTID, transactionID);
        }
        #endregion

        #region Transactionstate Enlist
        /// <summary>Trace an event when there is enlist.</summary>
        /// <param name="enlistmentID">The enlistment ID.</param>
        /// <param name="enlistmentType">The enlistment type.</param>
        /// <param name="enlistmentOption">The enlistment option.</param>
        [NonEvent]
        internal void TransactionstateEnlist(EnlistmentTraceIdentifier enlistmentID, EnlistmentType enlistmentType, EnlistmentOptions enlistmentOption)
        {
            if (IsEnabled(EventLevel.Informational, ALL_KEYWORDS))
            {
                TransactionstateEnlist(enlistmentID.EnlistmentIdentifier.ToString(), enlistmentType.ToString(), enlistmentOption.ToString());
            }
        }

        [Event(TRANSACTIONSTATE_ENLIST_EVENTID, Keywords = Keywords.TraceLtm, Level = EventLevel.Informational, Task = Tasks.TransactionState, Opcode = Opcodes.Enlist, Message = "Transactionstate enlist: Enlistment ID is {0}, type is {1} and options is {2}")]
        private void TransactionstateEnlist(string enlistmentID, string type, string option)
        {
            WriteEvent(TRANSACTIONSTATE_ENLIST_EVENTID, enlistmentID, type, option);
        }
        #endregion

        #region Transactionstate committed
        /// <summary>Trace an event when transaction is committed.</summary>
        /// <param name="transactionID">The transaction ID.</param>
        [NonEvent]
        internal void TransactionCommitted(TransactionTraceIdentifier transactionID)
        {
            if (IsEnabled(EventLevel.Verbose, ALL_KEYWORDS))
            {
                TransactionCommitted(transactionID.TransactionIdentifier.ToString());
            }
        }

        [Event(TRANSACTION_COMMITTED_EVENTID, Keywords = Keywords.TraceLtm, Level = EventLevel.Verbose, Task = Tasks.Transaction, Opcode = Opcodes.Committed, Message = "Transaction committed: transaction ID is {0}")]
        private void TransactionCommitted(string transactionID)
        {
            WriteEvent(TRANSACTION_COMMITTED_EVENTID, transactionID);
        }
        #endregion

        #region Transactionstate indoubt
        /// <summary>Trace an event when transaction is indoubt.</summary>
        /// <param name="transactionID">The transaction ID.</param>
        [NonEvent]
        internal void TransactionInDoubt(TransactionTraceIdentifier transactionID)
        {
            if (IsEnabled(EventLevel.Warning, ALL_KEYWORDS))
            {
                TransactionInDoubt(transactionID.TransactionIdentifier.ToString());
            }
        }

        [Event(TRANSACTION_INDOUBT_EVENTID, Keywords = Keywords.TraceLtm, Level = EventLevel.Warning, Task = Tasks.Transaction, Opcode = Opcodes.InDoubt, Message = "Transaction indoubt: transaction ID is {0}")]
        private void TransactionInDoubt(string transactionID)
        {
            WriteEvent(TRANSACTION_INDOUBT_EVENTID, transactionID);
        }
        #endregion

        #region Transactionstate promoted
        /// <summary>Trace an event when transaction is promoted.</summary>
        /// <param name="txID">The transaction ID.</param>
        /// <param name="distributedTxID">The distributed transaction ID.</param>
        [NonEvent]
        internal void TransactionPromoted(TransactionTraceIdentifier txID, TransactionTraceIdentifier distributedTxID)
        {
            if (IsEnabled(EventLevel.Informational, ALL_KEYWORDS))
            {
                TransactionPromoted(txID.TransactionIdentifier.ToString(), distributedTxID.TransactionIdentifier.ToString());
            }
        }

        [Event(TRANSACTION_PROMOTED_EVENTID, Keywords = Keywords.TraceLtm, Level = EventLevel.Informational, Task = Tasks.Transaction, Opcode = Opcodes.Promoted, Message = "Transaction promoted: transaction ID is {0} and distributed transaction ID is {1}")]
        private void TransactionPromoted(string txID, string distributedTxID)
        {
            WriteEvent(TRANSACTION_PROMOTED_EVENTID, txID, distributedTxID);
        }
        #endregion

        #region Transactionstate aborted
        /// <summary>Trace an event when transaction is aborted.</summary>
        /// <param name="txID">The transaction ID.</param>
        [NonEvent]
        internal void TransactionAborted(TransactionTraceIdentifier txID)
        {
            if (IsEnabled(EventLevel.Warning, ALL_KEYWORDS))
            {
                TransactionAborted(txID.TransactionIdentifier.ToString());
            }
        }

        [Event(TRANSACTION_ABORTED_EVENTID, Keywords = Keywords.TraceLtm, Level = EventLevel.Warning, Task = Tasks.Transaction, Opcode = Opcodes.Aborted, Message = "Transaction aborted: transaction ID is {0}")]
        private void TransactionAborted(string txID)
        {
            WriteEvent(TRANSACTION_ABORTED_EVENTID, txID);
        }
        #endregion
        public class Opcodes
        {
            public const EventOpcode Aborted = (EventOpcode)100;
            public const EventOpcode Activity = (EventOpcode)101;
            public const EventOpcode Adjusted = (EventOpcode)102;
            public const EventOpcode CloneCreate = (EventOpcode)103;
            public const EventOpcode Commit = (EventOpcode)104;
            public const EventOpcode Committed = (EventOpcode)105;
            public const EventOpcode Create = (EventOpcode)106;
            public const EventOpcode Created = (EventOpcode)107;
            public const EventOpcode CurrentChanged = (EventOpcode)108;
            public const EventOpcode DependentCloneComplete = (EventOpcode)109;
            public const EventOpcode Disposed = (EventOpcode)110;
            public const EventOpcode Done = (EventOpcode)111;
            public const EventOpcode Enlist = (EventOpcode)112;
            public const EventOpcode Enter = (EventOpcode)113;
            public const EventOpcode ExceptionConsumed = (EventOpcode)114;
            public const EventOpcode Exit = (EventOpcode)115;
            public const EventOpcode ForceRollback = (EventOpcode)116;
            public const EventOpcode Incomplete = (EventOpcode)117;
            public const EventOpcode InDoubt = (EventOpcode)118;
            public const EventOpcode InternalError = (EventOpcode)119;
            public const EventOpcode InvalidOperation = (EventOpcode)120;
            public const EventOpcode NestedIncorrectly = (EventOpcode)121;
            public const EventOpcode Prepared = (EventOpcode)122;
            public const EventOpcode Promoted = (EventOpcode)123;
            public const EventOpcode RecoveryComplete = (EventOpcode)124;
            public const EventOpcode Reenlist = (EventOpcode)125;
            public const EventOpcode Rollback = (EventOpcode)126;
            public const EventOpcode Serialized = (EventOpcode)127;
            public const EventOpcode Timeout = (EventOpcode)128;
        }

        public class Tasks
        {
            public const EventTask ConfiguredDefaultTimeout = (EventTask)1;
            public const EventTask Enlistment = (EventTask)2;
            public const EventTask ResourceManager = (EventTask)3;
            public const EventTask Method = (EventTask)4;
            public const EventTask Transaction = (EventTask)5;
            public const EventTask TransactionException = (EventTask)6;
            public const EventTask TransactionManager = (EventTask)7;
            public const EventTask TransactionScope = (EventTask)8;
            public const EventTask TransactionState = (EventTask)9;
        }

        public class Keywords
        {
            public const EventKeywords TraceBase = (EventKeywords)0x0001;
            public const EventKeywords TraceLtm = (EventKeywords)0x0002;
            public const EventKeywords TraceDistributed = (EventKeywords)0x0004;
        }
    }
}
