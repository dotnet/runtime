// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// TargetCore.cs
//
//
// The core implementation of a standard ITargetBlock<TInput>.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;

namespace System.Threading.Tasks.Dataflow.Internal
{
    // LOCK-LEVELING SCHEME
    // --------------------
    // TargetCore employs a single lock: IncomingLock.  This lock must not be used when calling out to any targets,
    // which TargetCore should not have, anyway.  It also must not be held when calling back to any sources, except
    // during calls to OfferMessage from that same source.

    /// <summary>Options used to configure a target core.</summary>
    [Flags]
    internal enum TargetCoreOptions : byte
    {
        /// <summary>Synchronous completion, both a target and a source, etc.</summary>
        None = 0x0,
        /// <summary>Whether the block relies on the delegate to signal when an async operation has completed.</summary>
        UsesAsyncCompletion = 0x1,
        /// <summary>
        /// Whether the block containing this target core is just a target or also has a source side.
        /// If it's just a target, then this target core's completion represents the entire block's completion.
        /// </summary>
        RepresentsBlockCompletion = 0x2
    }

    /// <summary>
    /// Provides a core implementation of <see cref="ITargetBlock{TInput}"/>.</summary>
    /// <typeparam name="TInput">Specifies the type of data accepted by the <see cref="TargetCore{TInput}"/>.</typeparam>
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    [DebuggerDisplay("{DebuggerDisplayContent,nq}")]
    internal sealed class TargetCore<TInput>
    {
        // *** These fields are readonly and are initialized at AppDomain startup.

        /// <summary>Caching the keep alive predicate.</summary>
        private static readonly Common.KeepAlivePredicate<TargetCore<TInput>, KeyValuePair<TInput, long>> _keepAlivePredicate =
                (TargetCore<TInput> thisTargetCore, out KeyValuePair<TInput, long> messageWithId) =>
                    thisTargetCore.TryGetNextAvailableOrPostponedMessage(out messageWithId);

        // *** These fields are readonly and are initialized to new instances at construction.

        /// <summary>A task representing the completion of the block.</summary>
        private readonly TaskCompletionSource<VoidResult> _completionSource = new TaskCompletionSource<VoidResult>();

        // *** These fields are readonly and are initialized by arguments to the constructor.

        /// <summary>The target block using this helper.</summary>
        private readonly ITargetBlock<TInput> _owningTarget;
        /// <summary>The messages in this target.</summary>
        /// <remarks>This field doubles as the IncomingLock.</remarks>
        private readonly IProducerConsumerQueue<KeyValuePair<TInput, long>> _messages;
        /// <summary>The options associated with this block.</summary>
        private readonly ExecutionDataflowBlockOptions _dataflowBlockOptions;
        /// <summary>An action to invoke for every accepted message.</summary>
        private readonly Action<KeyValuePair<TInput, long>> _callAction;
        /// <summary>Whether the block relies on the delegate to signal when an async operation has completed.</summary>
        private readonly TargetCoreOptions _targetCoreOptions;
        /// <summary>Bounding state for when the block is executing in bounded mode.</summary>
        private readonly BoundingStateWithPostponed<TInput> _boundingState;
        /// <summary>The reordering buffer used by the owner.  May be null.</summary>
        private readonly IReorderingBuffer _reorderingBuffer;

        /// <summary>Gets the object used as the incoming lock.</summary>
        private object IncomingLock { get { return _messages; } }

        // *** These fields are mutated during execution.

        /// <summary>Exceptions that may have occurred and gone unhandled during processing.</summary>
        private List<Exception> _exceptions;
        /// <summary>Whether to stop accepting new messages.</summary>
        private bool _decliningPermanently;
        /// <summary>The number of operations (including service tasks) currently running asynchronously.</summary>
        /// <remarks>Must always be accessed from inside a lock.</remarks>
        private int _numberOfOutstandingOperations;
        /// <summary>The number of service tasks in async mode currently running.</summary>
        /// <remarks>Must always be accessed from inside a lock.</remarks>
        private int _numberOfOutstandingServiceTasks;
        /// <summary>The next available ID we can assign to a message about to be processed.</summary>
        private PaddedInt64 _nextAvailableInputMessageId; // initialized to 0... very important for a reordering buffer
        /// <summary>A task has reserved the right to run the completion routine.</summary>
        private bool _completionReserved;
        /// <summary>This counter is set by the processing loop to prevent itself from trying to keep alive.</summary>
        private int _keepAliveBanCounter;

        /// <summary>Initializes the target core.</summary>
        /// <param name="owningTarget">The target using this helper.</param>
        /// <param name="callAction">An action to invoke for all accepted items.</param>
        /// <param name="reorderingBuffer">The reordering buffer used by the owner; may be null.</param>
        /// <param name="dataflowBlockOptions">The options to use to configure this block. The target core assumes these options are immutable.</param>
        /// <param name="targetCoreOptions">Options for how the target core should behave.</param>
        internal TargetCore(
            ITargetBlock<TInput> owningTarget,
            Action<KeyValuePair<TInput, long>> callAction,
            IReorderingBuffer reorderingBuffer,
            ExecutionDataflowBlockOptions dataflowBlockOptions,
            TargetCoreOptions targetCoreOptions)
        {
            // Validate internal arguments
            Debug.Assert(owningTarget != null, "Core must be associated with a target block.");
            Debug.Assert(dataflowBlockOptions != null, "Options must be provided to configure the core.");
            Debug.Assert(callAction != null, "Action to invoke for each item is required.");

            // Store arguments and do additional initialization
            _owningTarget = owningTarget;
            _callAction = callAction;
            _reorderingBuffer = reorderingBuffer;
            _dataflowBlockOptions = dataflowBlockOptions;
            _targetCoreOptions = targetCoreOptions;
            _messages = (dataflowBlockOptions.MaxDegreeOfParallelism == 1) ?
                (IProducerConsumerQueue<KeyValuePair<TInput, long>>)new SingleProducerSingleConsumerQueue<KeyValuePair<TInput, long>>() :
                (IProducerConsumerQueue<KeyValuePair<TInput, long>>)new MultiProducerMultiConsumerQueue<KeyValuePair<TInput, long>>();
            if (_dataflowBlockOptions.BoundedCapacity != System.Threading.Tasks.Dataflow.DataflowBlockOptions.Unbounded)
            {
                Debug.Assert(_dataflowBlockOptions.BoundedCapacity > 0, "Positive bounding count expected; should have been verified by options ctor");
                _boundingState = new BoundingStateWithPostponed<TInput>(_dataflowBlockOptions.BoundedCapacity);
            }
        }

        /// <summary>Internal Complete entry point with extra parameters for different contexts.</summary>
        /// <param name="exception">If not null, the block will be faulted.</param>
        /// <param name="dropPendingMessages">If true, any unprocessed input messages will be dropped.</param>
        /// <param name="storeExceptionEvenIfAlreadyCompleting">If true, an exception will be stored after _decliningPermanently has been set to true.</param>
        /// <param name="unwrapInnerExceptions">If true, exception will be treated as an AggregateException.</param>
        /// <param name="revertProcessingState">Indicates whether the processing state is dirty and has to be reverted.</param>
        internal void Complete(Exception exception, bool dropPendingMessages, bool storeExceptionEvenIfAlreadyCompleting = false,
            bool unwrapInnerExceptions = false, bool revertProcessingState = false)
        {
            Debug.Assert(storeExceptionEvenIfAlreadyCompleting || !revertProcessingState,
                            "Indicating dirty processing state may only come with storeExceptionEvenIfAlreadyCompleting==true.");
            Contract.EndContractBlock();

            // Ensure that no new messages may be added
            lock (IncomingLock)
            {
                // Faulting from outside is allowed until we start declining permanently.
                // Faulting from inside is allowed at any time.
                if (exception != null && (!_decliningPermanently || storeExceptionEvenIfAlreadyCompleting))
                {
                    Debug.Assert(_numberOfOutstandingOperations > 0 || !storeExceptionEvenIfAlreadyCompleting,
                                "Calls with storeExceptionEvenIfAlreadyCompleting==true may only be coming from processing task.");

#pragma warning disable 0420
                    Common.AddException(ref _exceptions, exception, unwrapInnerExceptions);
                }

                // Clear the messages queue if requested
                if (dropPendingMessages)
                {
                    KeyValuePair<TInput, long> dummy;
                    while (_messages.TryDequeue(out dummy)) ;
                }

                // Revert the dirty processing state if requested
                if (revertProcessingState)
                {
                    Debug.Assert(_numberOfOutstandingOperations > 0 && (!UsesAsyncCompletion || _numberOfOutstandingServiceTasks > 0),
                                    "The processing state must be dirty when revertProcessingState==true.");
                    _numberOfOutstandingOperations--;
                    if (UsesAsyncCompletion) _numberOfOutstandingServiceTasks--;
                }

                // Trigger completion
                _decliningPermanently = true;
                CompleteBlockIfPossible();
            }
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Targets/Member[@name="OfferMessage"]/*' />
        internal DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, TInput messageValue, ISourceBlock<TInput> source, Boolean consumeToAccept)
        {
            // Validate arguments
            if (!messageHeader.IsValid) throw new ArgumentException(SR.Argument_InvalidMessageHeader, nameof(messageHeader));
            if (source == null && consumeToAccept) throw new ArgumentException(SR.Argument_CantConsumeFromANullSource, nameof(consumeToAccept));
            Contract.EndContractBlock();

            lock (IncomingLock)
            {
                // If we shouldn't be accepting more messages, don't.
                if (_decliningPermanently)
                {
                    CompleteBlockIfPossible();
                    return DataflowMessageStatus.DecliningPermanently;
                }

                // We can directly accept the message if:
                //      1) we are not bounding, OR 
                //      2) we are bounding AND there is room available AND there are no postponed messages AND no messages are currently being transfered to the input queue.
                // (If there were any postponed messages, we would need to postpone so that ordering would be maintained.)
                // (Unlike all other blocks, TargetCore can accept messages while processing, because 
                // input message IDs are properly assigned and the correct order is preserved.)
                if (_boundingState == null ||
                    (_boundingState.OutstandingTransfers == 0 && _boundingState.CountIsLessThanBound && _boundingState.PostponedMessages.Count == 0))
                {
                    // Consume the message from the source if necessary
                    if (consumeToAccept)
                    {
                        Debug.Assert(source != null, "We must have thrown if source == null && consumeToAccept == true.");

                        bool consumed;
                        messageValue = source.ConsumeMessage(messageHeader, _owningTarget, out consumed);
                        if (!consumed) return DataflowMessageStatus.NotAvailable;
                    }

                    // Assign a message ID - strictly sequential, no gaps.
                    // Once consumed, enqueue the message with its ID and kick off asynchronous processing.
                    long messageId = _nextAvailableInputMessageId.Value++;
                    Debug.Assert(messageId != Common.INVALID_REORDERING_ID, "The assigned message ID is invalid.");
                    if (_boundingState != null) _boundingState.CurrentCount += 1; // track this new item against our bound
                    _messages.Enqueue(new KeyValuePair<TInput, long>(messageValue, messageId));
                    ProcessAsyncIfNecessary();
                    return DataflowMessageStatus.Accepted;
                }
                // Otherwise, we try to postpone if a source was provided
                else if (source != null)
                {
                    Debug.Assert(_boundingState != null && _boundingState.PostponedMessages != null,
                        "PostponedMessages must have been initialized during construction in non-greedy mode.");

                    // Store the message's info and kick off asynchronous processing
                    _boundingState.PostponedMessages.Push(source, messageHeader);
                    ProcessAsyncIfNecessary();
                    return DataflowMessageStatus.Postponed;
                }
                // We can't do anything else about this message
                return DataflowMessageStatus.Declined;
            }
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Completion"]/*' />
        internal Task Completion { get { return _completionSource.Task; } }

        /// <summary>Gets the number of items waiting to be processed by this target.</summary>
        internal int InputCount { get { return _messages.GetCountSafe(IncomingLock); } }

        /// <summary>Signals to the target core that a previously launched asynchronous operation has now completed.</summary>
        internal void SignalOneAsyncMessageCompleted()
        {
            SignalOneAsyncMessageCompleted(boundingCountChange: 0);
        }

        /// <summary>Signals to the target core that a previously launched asynchronous operation has now completed.</summary>
        /// <param name="boundingCountChange">The number of elements by which to change the bounding count, if bounding is occurring.</param>
        internal void SignalOneAsyncMessageCompleted(int boundingCountChange)
        {
            lock (IncomingLock)
            {
                // We're no longer processing, so decrement the DOP counter
                Debug.Assert(_numberOfOutstandingOperations > 0, "Operations may only be completed if any are outstanding.");
                if (_numberOfOutstandingOperations > 0) _numberOfOutstandingOperations--;

                // Fix up the bounding count if necessary
                if (_boundingState != null && boundingCountChange != 0)
                {
                    Debug.Assert(boundingCountChange <= 0 && _boundingState.CurrentCount + boundingCountChange >= 0,
                        "Expected a negative bounding change and not to drop below zero.");
                    _boundingState.CurrentCount += boundingCountChange;
                }

                // However, we may have given up early because we hit our own configured
                // processing limits rather than because we ran out of work to do.  If that's
                // the case, make sure we spin up another task to keep going.
                ProcessAsyncIfNecessary(repeat: true);

                // If, however, we stopped because we ran out of work to do and we
                // know we'll never get more, then complete.
                CompleteBlockIfPossible();
            }
        }

        /// <summary>Gets whether this instance has been constructed for async processing.</summary>
        private bool UsesAsyncCompletion
        {
            get
            {
                return (_targetCoreOptions & TargetCoreOptions.UsesAsyncCompletion) != 0;
            }
        }

        /// <summary>Gets whether there's room to launch more processing operations.</summary>
        private bool HasRoomForMoreOperations
        {
            get
            {
                Debug.Assert(_numberOfOutstandingOperations >= 0, "Number of outstanding operations should never be negative.");
                Debug.Assert(_numberOfOutstandingServiceTasks >= 0, "Number of outstanding service tasks should never be negative.");
                Debug.Assert(_numberOfOutstandingOperations >= _numberOfOutstandingServiceTasks, "Number of outstanding service tasks should never exceed the number of outstanding operations.");
                Common.ContractAssertMonitorStatus(IncomingLock, held: true);

                // In async mode, we increment _numberOfOutstandingOperations before we start 
                // our own processing loop which should not count towards the MaxDOP.
                return (_numberOfOutstandingOperations - _numberOfOutstandingServiceTasks) < _dataflowBlockOptions.ActualMaxDegreeOfParallelism;
            }
        }

        /// <summary>Gets whether there's room to launch more service tasks for doing/launching processing operations.</summary>
        private bool HasRoomForMoreServiceTasks
        {
            get
            {
                Debug.Assert(_numberOfOutstandingOperations >= 0, "Number of outstanding operations should never be negative.");
                Debug.Assert(_numberOfOutstandingServiceTasks >= 0, "Number of outstanding service tasks should never be negative.");
                Debug.Assert(_numberOfOutstandingOperations >= _numberOfOutstandingServiceTasks, "Number of outstanding service tasks should never exceed the number of outstanding operations.");
                Common.ContractAssertMonitorStatus(IncomingLock, held: true);

                if (!UsesAsyncCompletion)
                {
                    // Sync mode: 
                    // We don't count service tasks, because our tasks are counted as operations.
                    // Therefore, return HasRoomForMoreOperations.
                    return HasRoomForMoreOperations;
                }
                else
                {
                    // Async mode:
                    // We allow up to MaxDOP true service tasks.
                    // Checking whether there is room for more processing operations is not necessary, 
                    // but doing so will help us avoid spinning up a task that will go away without 
                    // launching any processing operation.
                    return HasRoomForMoreOperations &&
                           _numberOfOutstandingServiceTasks < _dataflowBlockOptions.ActualMaxDegreeOfParallelism;
                }
            }
        }

        /// <summary>Called when new messages are available to be processed.</summary>
        /// <param name="repeat">Whether this call is the continuation of a previous message loop.</param>
        private void ProcessAsyncIfNecessary(bool repeat = false)
        {
            Common.ContractAssertMonitorStatus(IncomingLock, held: true);

            if (HasRoomForMoreServiceTasks)
            {
                ProcessAsyncIfNecessary_Slow(repeat);
            }
        }

        /// <summary>
        /// Slow path for ProcessAsyncIfNecessary. 
        /// Separating out the slow path into its own method makes it more likely that the fast path method will get inlined.
        /// </summary>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        private void ProcessAsyncIfNecessary_Slow(bool repeat)
        {
            Debug.Assert(HasRoomForMoreServiceTasks, "There must be room to process asynchronously.");
            Common.ContractAssertMonitorStatus(IncomingLock, held: true);

            // Determine preconditions to launching a processing task
            bool messagesAvailableOrPostponed =
                !_messages.IsEmpty ||
                (!_decliningPermanently && _boundingState != null && _boundingState.CountIsLessThanBound && _boundingState.PostponedMessages.Count > 0);

            // If all conditions are met, launch away
            if (messagesAvailableOrPostponed && !CanceledOrFaulted)
            {
                // Any book keeping related to the processing task like incrementing the 
                // DOP counter or eventually recording the tasks reference must be done
                // before the task starts. That is because the task itself will do the 
                // reverse operation upon its completion.
                _numberOfOutstandingOperations++;
                if (UsesAsyncCompletion) _numberOfOutstandingServiceTasks++;

                var taskForInputProcessing = new Task(thisTargetCore => ((TargetCore<TInput>)thisTargetCore).ProcessMessagesLoopCore(), this,
                                                      Common.GetCreationOptionsForTask(repeat));

#if FEATURE_TRACING
                DataflowEtwProvider etwLog = DataflowEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.TaskLaunchedForMessageHandling(
                        _owningTarget, taskForInputProcessing, DataflowEtwProvider.TaskLaunchedReason.ProcessingInputMessages,
                        _messages.Count + (_boundingState != null ? _boundingState.PostponedMessages.Count : 0));
                }
#endif

                // Start the task handling scheduling exceptions
                Exception exception = Common.StartTaskSafe(taskForInputProcessing, _dataflowBlockOptions.TaskScheduler);
                if (exception != null)
                {
                    // Get out from under currently held locks. Complete re-acquires the locks it needs.
                    Task.Factory.StartNew(exc => Complete(exception: (Exception)exc, dropPendingMessages: true, storeExceptionEvenIfAlreadyCompleting: true,
                                                        unwrapInnerExceptions: false, revertProcessingState: true),
                                        exception, CancellationToken.None, Common.GetCreationOptionsForTask(), TaskScheduler.Default);
                }
            }
        }

        /// <summary>Task body used to process messages.</summary>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void ProcessMessagesLoopCore()
        {
            Common.ContractAssertMonitorStatus(IncomingLock, held: false);

            KeyValuePair<TInput, long> messageWithId = default(KeyValuePair<TInput, long>);
            try
            {
                bool useAsyncCompletion = UsesAsyncCompletion;
                bool shouldAttemptPostponedTransfer = _boundingState != null && _boundingState.BoundedCapacity > 1;
                int numberOfMessagesProcessedByThisTask = 0;
                int numberOfMessagesProcessedSinceTheLastKeepAlive = 0;
                int maxMessagesPerTask = _dataflowBlockOptions.ActualMaxMessagesPerTask;

                while (numberOfMessagesProcessedByThisTask < maxMessagesPerTask && !CanceledOrFaulted)
                {
                    // If we're bounding, try to transfer a message from the postponed queue
                    // to the input queue.  This enables us to more quickly unblock sources
                    // sending data to the block (otherwise, no postponed messages will be consumed
                    // until the input queue is entirely empty).  If the bounded size is 1,
                    // there's no need to transfer, as attempting to get the next message will
                    // just go and consume the postponed message anyway, and we'll save
                    // the extra trip through the _messages queue.
                    KeyValuePair<TInput, long> transferMessageWithId;
                    if (shouldAttemptPostponedTransfer &&
                        TryConsumePostponedMessage(forPostponementTransfer: true, result: out transferMessageWithId))
                    {
                        lock (IncomingLock)
                        {
                            Debug.Assert(
                                _boundingState.OutstandingTransfers > 0
                                && _boundingState.OutstandingTransfers <= _dataflowBlockOptions.ActualMaxDegreeOfParallelism,
                                "Expected TryConsumePostponedMessage to have incremented the count and for the count to not exceed the DOP.");
                            _boundingState.OutstandingTransfers--; // was incremented in TryConsumePostponedMessage
                            _messages.Enqueue(transferMessageWithId);
                            ProcessAsyncIfNecessary();
                        }
                    }

                    if (useAsyncCompletion)
                    {
                        // Get the next message if DOP is available.
                        // If we can't get a message or DOP is not available, bail out.
                        if (!TryGetNextMessageForNewAsyncOperation(out messageWithId)) break;
                    }
                    else
                    {
                        // Try to get a message for sequential execution, i.e. without checking DOP availability 
                        if (!TryGetNextAvailableOrPostponedMessage(out messageWithId))
                        {
                            // Try to keep the task alive only if MaxDOP=1
                            if (_dataflowBlockOptions.MaxDegreeOfParallelism != 1) break;

                            // If this task has processed enough messages without being kept alive, 
                            // it has served its purpose. Don't keep it alive.
                            if (numberOfMessagesProcessedSinceTheLastKeepAlive > Common.KEEP_ALIVE_NUMBER_OF_MESSAGES_THRESHOLD) break;

                            // If keep alive is banned, don't attempt it
                            if (_keepAliveBanCounter > 0)
                            {
                                _keepAliveBanCounter--;
                                break;
                            }

                            // Reset the keep alive counter. (Keep this line together with TryKeepAliveUntil.)
                            numberOfMessagesProcessedSinceTheLastKeepAlive = 0;

                            // Try to keep the task alive briefly until a new message arrives
                            if (!Common.TryKeepAliveUntil(_keepAlivePredicate, this, out messageWithId))
                            {
                                // Keep alive was unsuccessful. 
                                // Therefore ban further attempts temporarily.
                                _keepAliveBanCounter = Common.KEEP_ALIVE_BAN_COUNT;
                                break;
                            }
                        }
                    }

                    // We have popped a message from the queue.
                    // So increment the counter of processed messages.
                    numberOfMessagesProcessedByThisTask++;
                    numberOfMessagesProcessedSinceTheLastKeepAlive++;

                    // Invoke the user action
                    _callAction(messageWithId);
                }
            }
            catch (Exception exc)
            {
                Common.StoreDataflowMessageValueIntoExceptionData(exc, messageWithId.Key);
                Complete(exc, dropPendingMessages: true, storeExceptionEvenIfAlreadyCompleting: true, unwrapInnerExceptions: false);
            }
            finally
            {
                lock (IncomingLock)
                {
                    // We incremented _numberOfOutstandingOperations before we launched this task.
                    // So we must decremented it before exiting.
                    // Note that each async task additionally incremented it before starting and 
                    // is responsible for decrementing it prior to exiting.
                    Debug.Assert(_numberOfOutstandingOperations > 0, "Expected a positive number of outstanding operations, since we're completing one here.");
                    _numberOfOutstandingOperations--;

                    // If we are in async mode, we've also incremented _numberOfOutstandingServiceTasks.
                    // Now it's time to decrement it.
                    if (UsesAsyncCompletion)
                    {
                        Debug.Assert(_numberOfOutstandingServiceTasks > 0, "Expected a positive number of outstanding service tasks, since we're completing one here.");
                        _numberOfOutstandingServiceTasks--;
                    }

                    // However, we may have given up early because we hit our own configured
                    // processing limits rather than because we ran out of work to do.  If that's
                    // the case, make sure we spin up another task to keep going.
                    ProcessAsyncIfNecessary(repeat: true);

                    // If, however, we stopped because we ran out of work to do and we
                    // know we'll never get more, then complete.
                    CompleteBlockIfPossible();
                }
            }
        }

        /// <summary>Retrieves the next message from the input queue for the useAsyncCompletion mode.</summary>
        /// <param name="messageWithId">The next message retrieved.</param>
        /// <returns>true if a message was found and removed; otherwise, false.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private bool TryGetNextMessageForNewAsyncOperation(out KeyValuePair<TInput, long> messageWithId)
        {
            Debug.Assert(UsesAsyncCompletion, "Only valid to use when in async mode.");
            Common.ContractAssertMonitorStatus(IncomingLock, held: false);

            bool parallelismAvailable;

            lock (IncomingLock)
            {
                // If we have room for another asynchronous operation, reserve it.
                // If later it turns out that we had no work to fill the slot, we'll undo the addition.
                parallelismAvailable = HasRoomForMoreOperations;
                if (parallelismAvailable) ++_numberOfOutstandingOperations;
            }

            messageWithId = default(KeyValuePair<TInput, long>);
            if (parallelismAvailable)
            {
                // If a parallelism slot was available, try to get an item.
                // Be careful, because an exception may be thrown from ConsumeMessage
                // and we have already incremented _numberOfOutstandingOperations.
                bool gotMessage = false;
                try
                {
                    gotMessage = TryGetNextAvailableOrPostponedMessage(out messageWithId);
                }
                catch
                {
                    // We have incremented the counter, but we didn't get a message.
                    // So we must undo the increment and eventually complete the block.
                    SignalOneAsyncMessageCompleted();

                    // Re-throw the exception. The processing loop will catch it.
                    throw;
                }

                // There may not be an error, but may have still failed to get a message.
                // So we must undo the increment and eventually complete the block.
                if (!gotMessage) SignalOneAsyncMessageCompleted();

                return gotMessage;
            }

            // If there was no parallelism available, we didn't increment _numberOfOutstandingOperations.
            // So there is nothing to do except to return false.
            return false;
        }

        /// <summary>
        /// Either takes the next available message from the input queue or retrieves a postponed 
        /// message from a source, based on whether we're in greedy or non-greedy mode.
        /// </summary>
        /// <param name="messageWithId">The retrieved item with its Id.</param>
        /// <returns>true if a message could be removed and returned; otherwise, false.</returns>
        private bool TryGetNextAvailableOrPostponedMessage(out KeyValuePair<TInput, long> messageWithId)
        {
            Common.ContractAssertMonitorStatus(IncomingLock, held: false);

            // First try to get a message from our input buffer.
            if (_messages.TryDequeue(out messageWithId))
            {
                return true;
            }
            // If we can't, but if we have any postponed messages due to bounding, then
            // try to consume one of these postponed messages.
            // Since we are not currently holding the lock, it is possible that new messages get queued up
            // by the time we take the lock to manipulate _boundingState. So we have to double-check the
            // input queue once we take the lock before we consider postponed messages.
            else if (_boundingState != null && TryConsumePostponedMessage(forPostponementTransfer: false, result: out messageWithId))
            {
                return true;
            }
            // Otherwise, there's no message available.
            else
            {
                messageWithId = default(KeyValuePair<TInput, long>);
                return false;
            }
        }

        /// <summary>Consumes a single postponed message.</summary>
        /// <param name="forPostponementTransfer">
        /// true if the method is being called to consume a message that'll then be stored into the input queue;
        /// false if the method is being called to consume a message that'll be processed immediately.
        /// If true, the bounding state's ForcePostponement will be updated.
        /// If false, the method will first try (while holding the lock) to consume from the input queue before
        /// consuming a postponed message.
        /// </param>
        /// <param name="result">The consumed message.</param>
        /// <returns>true if a message was consumed; otherwise, false.</returns>
        private bool TryConsumePostponedMessage(
            bool forPostponementTransfer,
            out KeyValuePair<TInput, long> result)
        {
            Debug.Assert(
                _dataflowBlockOptions.BoundedCapacity !=
                System.Threading.Tasks.Dataflow.DataflowBlockOptions.Unbounded, "Only valid to use when in bounded mode.");
            Common.ContractAssertMonitorStatus(IncomingLock, held: false);

            // Iterate until we either consume a message successfully or there are no more postponed messages.
            bool countIncrementedExpectingToGetItem = false;
            long messageId = Common.INVALID_REORDERING_ID;
            while (true)
            {
                KeyValuePair<ISourceBlock<TInput>, DataflowMessageHeader> element;
                lock (IncomingLock)
                {
                    // If we are declining permanently, don't consume postponed messages.
                    if (_decliningPermanently) break;

                    // New messages may have been queued up while we weren't holding the lock.
                    // In particular, the input queue may have been filled up and messages may have
                    // gotten postponed. If we process such a postponed message, we would mess up the
                    // order. Therefore, we have to double-check the input queue first.
                    if (!forPostponementTransfer && _messages.TryDequeue(out result)) return true;

                    // We can consume a message to process if there's one to process and also if
                    // if we have logical room within our bound for the message.
                    if (!_boundingState.CountIsLessThanBound || !_boundingState.PostponedMessages.TryPop(out element))
                    {
                        if (countIncrementedExpectingToGetItem)
                        {
                            countIncrementedExpectingToGetItem = false;
                            _boundingState.CurrentCount -= 1;
                        }
                        break;
                    }
                    if (!countIncrementedExpectingToGetItem)
                    {
                        countIncrementedExpectingToGetItem = true;
                        messageId = _nextAvailableInputMessageId.Value++; // optimistically assign an ID
                        Debug.Assert(messageId != Common.INVALID_REORDERING_ID, "The assigned message ID is invalid.");
                        _boundingState.CurrentCount += 1; // optimistically take bounding space
                        if (forPostponementTransfer)
                        {
                            Debug.Assert(_boundingState.OutstandingTransfers >= 0, "Expected TryConsumePostponedMessage to not be negative.");
                            _boundingState.OutstandingTransfers++; // temporarily force postponement until we've successfully consumed the element
                        }
                    }
                } // Must not call to source while holding lock

                bool consumed;
                TInput consumedValue = element.Key.ConsumeMessage(element.Value, _owningTarget, out consumed);
                if (consumed)
                {
                    result = new KeyValuePair<TInput, long>(consumedValue, messageId);
                    return true;
                }
                else
                {
                    if (forPostponementTransfer)
                    {
                        // We didn't consume message so we need to decrement because we haven't consumed the element.
                        _boundingState.OutstandingTransfers--;
                    }
                }
            }

            // We optimistically acquired a message ID for a message that, in the end, we never got.
            // So, we need to let the reordering buffer (if one exists) know that it should not
            // expect an item with this ID.  Otherwise, it would stall forever.
            if (_reorderingBuffer != null && messageId != Common.INVALID_REORDERING_ID) _reorderingBuffer.IgnoreItem(messageId);

            // Similarly, we optimistically increased the bounding count, expecting to get another message in.
            // Since we didn't, we need to fix the bounding count back to what it should have been.
            if (countIncrementedExpectingToGetItem) ChangeBoundingCount(-1);

            // Inform the caller that no message could be consumed.
            result = default(KeyValuePair<TInput, long>);
            return false;
        }

        /// <summary>Gets whether the target has had cancellation requested or an exception has occurred.</summary>
        private bool CanceledOrFaulted
        {
            get
            {
                return _dataflowBlockOptions.CancellationToken.IsCancellationRequested || Volatile.Read(ref _exceptions) != null;
            }
        }

        /// <summary>Completes the block once all completion conditions are met.</summary>
        private void CompleteBlockIfPossible()
        {
            Common.ContractAssertMonitorStatus(IncomingLock, held: true);

            bool noMoreMessages = _decliningPermanently && _messages.IsEmpty;
            if (noMoreMessages || CanceledOrFaulted)
            {
                CompleteBlockIfPossible_Slow();
            }
        }

        /// <summary>
        /// Slow path for CompleteBlockIfPossible. 
        /// Separating out the slow path into its own method makes it more likely that the fast path method will get inlined.
        /// </summary>
        private void CompleteBlockIfPossible_Slow()
        {
            Debug.Assert((_decliningPermanently && _messages.IsEmpty) || CanceledOrFaulted, "There must be no more messages.");
            Common.ContractAssertMonitorStatus(IncomingLock, held: true);

            bool notCurrentlyProcessing = _numberOfOutstandingOperations == 0;
            if (notCurrentlyProcessing && !_completionReserved)
            {
                // Make sure no one else tries to call CompleteBlockOncePossible
                _completionReserved = true;

                // Make sure the target is declining
                _decliningPermanently = true;

                // Get out from under currently held locks.  This is to avoid
                // invoking synchronous continuations off of _completionSource.Task
                // while holding a lock.
                Task.Factory.StartNew(state => ((TargetCore<TInput>)state).CompleteBlockOncePossible(),
                    this, CancellationToken.None, Common.GetCreationOptionsForTask(), TaskScheduler.Default);
            }
        }

        /// <summary>
        /// Completes the block.  This must only be called once, and only once all of the completion conditions are met.
        /// As such, it must only be called from CompleteBlockIfPossible.
        /// </summary>
        private void CompleteBlockOncePossible()
        {
            // Since the lock is needed only for the Assert, we do this only in DEBUG mode
#if DEBUG
            lock (IncomingLock) Debug.Assert(_numberOfOutstandingOperations == 0, "Everything must be done by now.");
#endif

            // Release any postponed messages
            if (_boundingState != null)
            {
                // Note: No locks should be held at this point.
                Common.ReleaseAllPostponedMessages(_owningTarget, _boundingState.PostponedMessages, ref _exceptions);
            }

            // For good measure and help in preventing leaks, clear out the incoming message queue, 
            // which may still contain orphaned data if we were canceled or faulted.  However,
            // we don't reset the bounding count here, as the block as a whole may still be active.
            KeyValuePair<TInput, long> ignored;
            IProducerConsumerQueue<KeyValuePair<TInput, long>> messages = _messages;
            while (messages.TryDequeue(out ignored)) ;

            // If we completed with any unhandled exception, finish in an error state
            if (Volatile.Read(ref _exceptions) != null)
            {
                // It's ok to read _exceptions' content here, because
                // at this point no more exceptions can be generated and thus no one will
                // be writing to it.
                _completionSource.TrySetException(Volatile.Read(ref _exceptions));
            }
            // If we completed with cancellation, finish in a canceled state
            else if (_dataflowBlockOptions.CancellationToken.IsCancellationRequested)
            {
                _completionSource.TrySetCanceled();
            }
            // Otherwise, finish in a successful state.
            else
            {
                _completionSource.TrySetResult(default(VoidResult));
            }
#if FEATURE_TRACING
            // We only want to do tracing for block completion if this target core represents the whole block.
            // If it only represents a part of the block (i.e. there's a source associated with it as well),
            // then we shouldn't log just for the first half of the block; the source half will handle logging.
            DataflowEtwProvider etwLog;
            if ((_targetCoreOptions & TargetCoreOptions.RepresentsBlockCompletion) != 0 &&
                (etwLog = DataflowEtwProvider.Log).IsEnabled())
            {
                etwLog.DataflowBlockCompleted(_owningTarget);
            }
#endif
        }

        /// <summary>Gets whether the target core is operating in a bounded mode.</summary>
        internal bool IsBounded { get { return _boundingState != null; } }

        /// <summary>Increases or decreases the bounding count.</summary>
        /// <param name="count">The incremental addition (positive to increase, negative to decrease).</param>
        internal void ChangeBoundingCount(int count)
        {
            Debug.Assert(count != 0, "Should only be called when the count is actually changing.");
            Common.ContractAssertMonitorStatus(IncomingLock, held: false);
            if (_boundingState != null)
            {
                lock (IncomingLock)
                {
                    Debug.Assert(count > 0 || (count < 0 && _boundingState.CurrentCount + count >= 0),
                        "If count is negative, it must not take the total count negative.");
                    _boundingState.CurrentCount += count;
                    ProcessAsyncIfNecessary();
                    CompleteBlockIfPossible();
                }
            }
        }

        /// <summary>Gets the object to display in the debugger display attribute.</summary>
        [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider")]
        private object DebuggerDisplayContent
        {
            get
            {
                var displayTarget = _owningTarget as IDebuggerDisplay;
                return string.Format("Block=\"{0}\"",
                    displayTarget != null ? displayTarget.Content : _owningTarget);
            }
        }

        /// <summary>Gets the DataflowBlockOptions used to configure this block.</summary>
        internal ExecutionDataflowBlockOptions DataflowBlockOptions { get { return _dataflowBlockOptions; } }

        /// <summary>Gets information about this helper to be used for display in a debugger.</summary>
        /// <returns>Debugging information about this target.</returns>
        internal DebuggingInformation GetDebuggingInformation() { return new DebuggingInformation(this); }

        /// <summary>Provides a wrapper for commonly needed debugging information.</summary>
        internal sealed class DebuggingInformation
        {
            /// <summary>The target being viewed.</summary>
            private readonly TargetCore<TInput> _target;

            /// <summary>Initializes the debugging helper.</summary>
            /// <param name="target">The target being viewed.</param>
            internal DebuggingInformation(TargetCore<TInput> target) { _target = target; }

            /// <summary>Gets the number of messages waiting to be processed.</summary>
            internal int InputCount { get { return _target._messages.Count; } }
            /// <summary>Gets the messages waiting to be processed.</summary>
            internal IEnumerable<TInput> InputQueue { get { return _target._messages.Select(kvp => kvp.Key).ToList(); } }

            /// <summary>Gets any postponed messages.</summary>
            internal QueuedMap<ISourceBlock<TInput>, DataflowMessageHeader> PostponedMessages
            {
                get { return _target._boundingState != null ? _target._boundingState.PostponedMessages : null; }
            }

            /// <summary>Gets the current number of outstanding input processing operations.</summary>
            internal Int32 CurrentDegreeOfParallelism { get { return _target._numberOfOutstandingOperations - _target._numberOfOutstandingServiceTasks; } }

            /// <summary>Gets the DataflowBlockOptions used to configure this block.</summary>
            internal ExecutionDataflowBlockOptions DataflowBlockOptions { get { return _target._dataflowBlockOptions; } }
            /// <summary>Gets whether the block is declining further messages.</summary>
            internal bool IsDecliningPermanently { get { return _target._decliningPermanently; } }
            /// <summary>Gets whether the block is completed.</summary>
            internal bool IsCompleted { get { return _target.Completion.IsCompleted; } }
        }
    }
}
