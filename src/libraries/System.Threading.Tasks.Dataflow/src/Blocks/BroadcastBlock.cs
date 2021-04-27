// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// BroadcastBlock.cs
//
//
// A propagator that broadcasts incoming messages to all targets, overwriting the current
// message in the process.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security;
using System.Threading.Tasks.Dataflow.Internal;

namespace System.Threading.Tasks.Dataflow
{
    /// <summary>
    /// Provides a buffer for storing at most one element at time, overwriting each message with the next as it arrives.
    /// Messages are broadcast to all linked targets, all of which may consume a clone of the message.
    /// </summary>
    /// <typeparam name="T">Specifies the type of the data buffered by this dataflow block.</typeparam>
    /// <remarks>
    /// <see cref="BroadcastBlock{T}"/> exposes at most one element at a time.  However, unlike
    /// <see cref="WriteOnceBlock{T}"/>, that element will be overwritten as new elements are provided
    /// to the block.  <see cref="BroadcastBlock{T}"/> ensures that the current element is broadcast to any
    /// linked targets before allowing the element to be overwritten.
    /// </remarks>
    [DebuggerDisplay("{DebuggerDisplayContent,nq}")]
    [DebuggerTypeProxy(typeof(BroadcastBlock<>.DebugView))]
    public sealed class BroadcastBlock<T> : IPropagatorBlock<T, T>, IReceivableSourceBlock<T>, IDebuggerDisplay
    {
        /// <summary>The source side.</summary>
        private readonly BroadcastingSourceCore<T> _source;
        /// <summary>Bounding state for when the block is executing in bounded mode.</summary>
        private readonly BoundingStateWithPostponedAndTask<T>? _boundingState;
        /// <summary>Whether all future messages should be declined.</summary>
        private bool _decliningPermanently;
        /// <summary>A task has reserved the right to run the completion routine.</summary>
        private bool _completionReserved;
        /// <summary>Gets the lock used to synchronize incoming requests.</summary>
        private object IncomingLock { get { return _source; } }

        /// <summary>Initializes the <see cref="BroadcastBlock{T}"/> with the specified cloning function.</summary>
        /// <param name="cloningFunction">
        /// The function to use to clone the data when offered to other blocks.
        /// This may be null to indicate that no cloning need be performed.
        /// </param>
        public BroadcastBlock(Func<T, T>? cloningFunction) :
            this(cloningFunction, DataflowBlockOptions.Default)
        { }

        /// <summary>Initializes the <see cref="BroadcastBlock{T}"/>  with the specified cloning function and <see cref="DataflowBlockOptions"/>.</summary>
        /// <param name="cloningFunction">
        /// The function to use to clone the data when offered to other blocks.
        /// This may be null to indicate that no cloning need be performed.
        /// </param>
        /// <param name="dataflowBlockOptions">The options with which to configure this <see cref="BroadcastBlock{T}"/>.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="dataflowBlockOptions"/> is null (Nothing in Visual Basic).</exception>
        public BroadcastBlock(Func<T, T>? cloningFunction, DataflowBlockOptions dataflowBlockOptions)
        {
            // Validate arguments
            if (dataflowBlockOptions == null) throw new ArgumentNullException(nameof(dataflowBlockOptions));

            // Ensure we have options that can't be changed by the caller
            dataflowBlockOptions = dataflowBlockOptions.DefaultOrClone();

            // Initialize bounding state if necessary
            Action<int>? onItemsRemoved = null;
            if (dataflowBlockOptions.BoundedCapacity > 0)
            {
                Debug.Assert(dataflowBlockOptions.BoundedCapacity > 0, "Positive bounding count expected; should have been verified by options ctor");
                onItemsRemoved = OnItemsRemoved;
                _boundingState = new BoundingStateWithPostponedAndTask<T>(dataflowBlockOptions.BoundedCapacity);
            }

            // Initialize the source side
            _source = new BroadcastingSourceCore<T>(this, cloningFunction, dataflowBlockOptions, onItemsRemoved);

            // It is possible that the source half may fault on its own, e.g. due to a task scheduler exception.
            // In those cases we need to fault the target half to drop its buffered messages and to release its
            // reservations. This should not create an infinite loop, because all our implementations are designed
            // to handle multiple completion requests and to carry over only one.
            _source.Completion.ContinueWith((completed, state) =>
            {
                var thisBlock = ((BroadcastBlock<T>)state!) as IDataflowBlock;
                Debug.Assert(completed.IsFaulted, "The source must be faulted in order to trigger a target completion.");
                thisBlock.Fault(completed.Exception!);
            }, this, CancellationToken.None, Common.GetContinuationOptions() | TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);

            // Handle async cancellation requests by declining on the target
            Common.WireCancellationToComplete(
                dataflowBlockOptions.CancellationToken, _source.Completion, state => ((BroadcastBlock<T>)state!).Complete(), this);
#if FEATURE_TRACING
            DataflowEtwProvider etwLog = DataflowEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.DataflowBlockCreated(this, dataflowBlockOptions);
            }
#endif
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Complete"]/*' />
        public void Complete()
        {
            CompleteCore(exception: null, storeExceptionEvenIfAlreadyCompleting: false);
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Fault"]/*' />
        void IDataflowBlock.Fault(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));

            CompleteCore(exception, storeExceptionEvenIfAlreadyCompleting: false);
        }

        internal void CompleteCore(Exception? exception, bool storeExceptionEvenIfAlreadyCompleting, bool revertProcessingState = false)
        {
            Debug.Assert(storeExceptionEvenIfAlreadyCompleting || !revertProcessingState,
                            "Indicating dirty processing state may only come with storeExceptionEvenIfAlreadyCompleting==true.");

            lock (IncomingLock)
            {
                // Faulting from outside is allowed until we start declining permanently.
                // Faulting from inside is allowed at any time.
                if (exception != null && (!_decliningPermanently || storeExceptionEvenIfAlreadyCompleting))
                {
                    _source.AddException(exception);
                }

                // Revert the dirty processing state if requested
                if (revertProcessingState)
                {
                    Debug.Assert(_boundingState != null && _boundingState.TaskForInputProcessing != null,
                                    "The processing state must be dirty when revertProcessingState==true.");
                    _boundingState.TaskForInputProcessing = null;
                }

                // Trigger completion if possible
                _decliningPermanently = true;
                CompleteTargetIfPossible();
            }
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="LinkTo"]/*' />
        public IDisposable LinkTo(ITargetBlock<T> target, DataflowLinkOptions linkOptions) { return _source.LinkTo(target, linkOptions); }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="TryReceive"]/*' />
        public bool TryReceive(Predicate<T>? filter, [MaybeNullWhen(false)] out T item) { return _source.TryReceive(filter, out item); }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="TryReceiveAll"]/*' />
        bool IReceivableSourceBlock<T>.TryReceiveAll([NotNullWhen(true)] out IList<T>? items) { return _source.TryReceiveAll(out items); }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Completion"]/*' />
        public Task Completion { get { return _source.Completion; } }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Targets/Member[@name="OfferMessage"]/*' />
        DataflowMessageStatus ITargetBlock<T>.OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T>? source, bool consumeToAccept)
        {
            // Validate arguments
            if (!messageHeader.IsValid) throw new ArgumentException(SR.Argument_InvalidMessageHeader, nameof(messageHeader));
            if (source == null && consumeToAccept) throw new ArgumentException(SR.Argument_CantConsumeFromANullSource, nameof(consumeToAccept));

            lock (IncomingLock)
            {
                // If we've already stopped accepting messages, decline permanently
                if (_decliningPermanently)
                {
                    CompleteTargetIfPossible();
                    return DataflowMessageStatus.DecliningPermanently;
                }

                // We can directly accept the message if:
                //      1) we are not bounding, OR
                //      2) we are bounding AND there is room available AND there are no postponed messages AND we are not currently processing.
                // (If there were any postponed messages, we would need to postpone so that ordering would be maintained.)
                // (We should also postpone if we are currently processing, because there may be a race between consuming postponed messages and
                // accepting new ones directly into the queue.)
                if (_boundingState == null
                        ||
                    (_boundingState.CountIsLessThanBound && _boundingState.PostponedMessages.Count == 0 && _boundingState.TaskForInputProcessing == null))
                {
                    // Consume the message from the source if necessary
                    if (consumeToAccept)
                    {
                        Debug.Assert(source != null, "We must have thrown if source == null && consumeToAccept == true.");

                        bool consumed;
                        messageValue = source.ConsumeMessage(messageHeader, this, out consumed)!;
                        if (!consumed) return DataflowMessageStatus.NotAvailable;
                    }

                    // Once consumed, pass it to the delegate
                    _source.AddMessage(messageValue!);
                    if (_boundingState != null) _boundingState.CurrentCount += 1; // track this new item against our bound
                    return DataflowMessageStatus.Accepted;
                }
                // Otherwise, we try to postpone if a source was provided
                else if (source != null)
                {
                    Debug.Assert(_boundingState != null && _boundingState.PostponedMessages != null,
                        "PostponedMessages must have been initialized during construction in bounding mode.");

                    _boundingState.PostponedMessages.Push(source, messageHeader);
                    return DataflowMessageStatus.Postponed;
                }
                // We can't do anything else about this message
                return DataflowMessageStatus.Declined;
            }
        }

        /// <summary>Notifies the block that one or more items was removed from the queue.</summary>
        /// <param name="numItemsRemoved">The number of items removed.</param>
        private void OnItemsRemoved(int numItemsRemoved)
        {
            Debug.Assert(numItemsRemoved > 0, "Should only be called for a positive number of items removed.");
            Common.ContractAssertMonitorStatus(IncomingLock, held: false);

            // If we're bounding, we need to know when an item is removed so that we
            // can update the count that's mirroring the actual count in the source's queue,
            // and potentially kick off processing to start consuming postponed messages.
            if (_boundingState != null)
            {
                lock (IncomingLock)
                {
                    // Decrement the count, which mirrors the count in the source half
                    Debug.Assert(_boundingState.CurrentCount - numItemsRemoved >= 0,
                        "It should be impossible to have a negative number of items.");
                    _boundingState.CurrentCount -= numItemsRemoved;

                    ConsumeAsyncIfNecessary();
                    CompleteTargetIfPossible();
                }
            }
        }

        /// <summary>Called when postponed messages may need to be consumed.</summary>
        /// <param name="isReplacementReplica">Whether this call is the continuation of a previous message loop.</param>
        internal void ConsumeAsyncIfNecessary(bool isReplacementReplica = false)
        {
            Common.ContractAssertMonitorStatus(IncomingLock, held: true);
            Debug.Assert(_boundingState != null, "Must be in bounded mode.");

            if (!_decliningPermanently &&
                _boundingState.TaskForInputProcessing == null &&
                _boundingState.PostponedMessages.Count > 0 &&
                _boundingState.CountIsLessThanBound)
            {
                // Create task and store into _taskForInputProcessing prior to scheduling the task
                // so that _taskForInputProcessing will be visibly set in the task loop.
                _boundingState.TaskForInputProcessing =
                    new Task(state => ((BroadcastBlock<T>)state!).ConsumeMessagesLoopCore(), this,
                        Common.GetCreationOptionsForTask(isReplacementReplica));

#if FEATURE_TRACING
                DataflowEtwProvider etwLog = DataflowEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.TaskLaunchedForMessageHandling(
                        this, _boundingState.TaskForInputProcessing, DataflowEtwProvider.TaskLaunchedReason.ProcessingInputMessages,
                        _boundingState.PostponedMessages.Count);
                }
#endif

                // Start the task handling scheduling exceptions
                Exception? exception = Common.StartTaskSafe(_boundingState.TaskForInputProcessing, _source.DataflowBlockOptions.TaskScheduler);
                if (exception != null)
                {
                    // Get out from under currently held locks. Complete re-acquires the locks it needs.
                    Task.Factory.StartNew(exc => CompleteCore(exception: (Exception)exc!, storeExceptionEvenIfAlreadyCompleting: true, revertProcessingState: true),
                                        exception, CancellationToken.None, Common.GetCreationOptionsForTask(), TaskScheduler.Default);
                }
            }
        }

        /// <summary>Task body used to consume postponed messages.</summary>
        private void ConsumeMessagesLoopCore()
        {
            Debug.Assert(_boundingState != null && _boundingState.TaskForInputProcessing != null,
                "May only be called in bounded mode and when a task is in flight.");
            Debug.Assert(_boundingState.TaskForInputProcessing.Id == Task.CurrentId,
                "This must only be called from the in-flight processing task.");
            Common.ContractAssertMonitorStatus(IncomingLock, held: false);

            try
            {
                int maxMessagesPerTask = _source.DataflowBlockOptions.ActualMaxMessagesPerTask;
                for (int i = 0;
                    i < maxMessagesPerTask && ConsumeAndStoreOneMessageIfAvailable();
                    i++)
                    ;
            }
            catch (Exception exception)
            {
                // Prevent the creation of new processing tasks
                CompleteCore(exception, storeExceptionEvenIfAlreadyCompleting: true);
            }
            finally
            {
                lock (IncomingLock)
                {
                    // We're no longer processing, so null out the processing task
                    _boundingState.TaskForInputProcessing = null;

                    // However, we may have given up early because we hit our own configured
                    // processing limits rather than because we ran out of work to do.  If that's
                    // the case, make sure we spin up another task to keep going.
                    ConsumeAsyncIfNecessary(isReplacementReplica: true);

                    // If, however, we stopped because we ran out of work to do and we
                    // know we'll never get more, then complete.
                    CompleteTargetIfPossible();
                }
            }
        }

        /// <summary>
        /// Retrieves one postponed message if there's room and if we can consume a postponed message.
        /// Stores any consumed message into the source half.
        /// </summary>
        /// <returns>true if a message could be consumed and stored; otherwise, false.</returns>
        /// <remarks>This must only be called from the asynchronous processing loop.</remarks>
        private bool ConsumeAndStoreOneMessageIfAvailable()
        {
            Debug.Assert(_boundingState != null && _boundingState.TaskForInputProcessing != null,
                "May only be called in bounded mode and when a task is in flight.");
            Debug.Assert(_boundingState.TaskForInputProcessing.Id == Task.CurrentId,
                "This must only be called from the in-flight processing task.");
            Common.ContractAssertMonitorStatus(IncomingLock, held: false);

            // Loop through the postponed messages until we get one.
            while (true)
            {
                // Get the next item to retrieve.  If there are no more, bail.
                KeyValuePair<ISourceBlock<T>, DataflowMessageHeader> sourceAndMessage;
                lock (IncomingLock)
                {
                    if (!_boundingState.CountIsLessThanBound) return false;
                    if (!_boundingState.PostponedMessages.TryPop(out sourceAndMessage)) return false;

                    // Optimistically assume we're going to get the item. This avoids taking the lock
                    // again if we're right.  If we're wrong, we decrement it later under lock.
                    _boundingState.CurrentCount++;
                }

                // Consume the item
                bool consumed = false;
                try
                {
                    T? consumedValue = sourceAndMessage.Key.ConsumeMessage(sourceAndMessage.Value, this, out consumed);
                    if (consumed)
                    {
                        _source.AddMessage(consumedValue!);
                        return true;
                    }
                }
                finally
                {
                    // We didn't get the item, so decrement the count to counteract our optimistic assumption.
                    if (!consumed)
                    {
                        lock (IncomingLock) _boundingState.CurrentCount--;
                    }
                }
            }
        }

        /// <summary>Completes the target, notifying the source, once all completion conditions are met.</summary>
        private void CompleteTargetIfPossible()
        {
            Common.ContractAssertMonitorStatus(IncomingLock, held: true);
            if (_decliningPermanently &&
                !_completionReserved &&
                (_boundingState == null || _boundingState.TaskForInputProcessing == null))
            {
                _completionReserved = true;

                // If we're in bounding mode and we have any postponed messages, we need to clear them,
                // which means calling back to the source, which means we need to escape the incoming lock.
                if (_boundingState != null && _boundingState.PostponedMessages.Count > 0)
                {
                    Task.Factory.StartNew(state =>
                    {
                        var thisBroadcastBlock = (BroadcastBlock<T>)state!;

                        // Release any postponed messages
                        List<Exception>? exceptions = null;
                        if (thisBroadcastBlock._boundingState != null)
                        {
                            // Note: No locks should be held at this point
                            Common.ReleaseAllPostponedMessages(thisBroadcastBlock,
                                                               thisBroadcastBlock._boundingState.PostponedMessages,
                                                               ref exceptions);
                        }

                        if (exceptions != null)
                        {
                            // It is important to migrate these exceptions to the source part of the owning batch,
                            // because that is the completion task that is publicly exposed.
                            thisBroadcastBlock._source.AddExceptions(exceptions);
                        }

                        thisBroadcastBlock._source.Complete();
                    }, this, CancellationToken.None, Common.GetCreationOptionsForTask(), TaskScheduler.Default);
                }
                // Otherwise, we can just decline the source directly.
                else
                {
                    _source.Complete();
                }
            }
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="ConsumeMessage"]/*' />
        T? ISourceBlock<T>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<T> target, out bool messageConsumed)
        {
            return _source.ConsumeMessage(messageHeader, target, out messageConsumed);
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="ReserveMessage"]/*' />
        bool ISourceBlock<T>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<T> target)
        {
            return _source.ReserveMessage(messageHeader, target);
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="ReleaseReservation"]/*' />
        void ISourceBlock<T>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<T> target)
        {
            _source.ReleaseReservation(messageHeader, target);
        }

        /// <summary>Gets a value to be used for the DebuggerDisplayAttribute.  This must not throw even if HasValue is false.</summary>
        private bool HasValueForDebugger { get { return _source.GetDebuggingInformation().HasValue; } }
        /// <summary>Gets a value to be used for the DebuggerDisplayAttribute.  This must not throw even if HasValue is false.</summary>
        private T ValueForDebugger { get { return _source.GetDebuggingInformation().Value; } }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="ToString"]/*' />
        public override string ToString() { return Common.GetNameForDebugger(this, _source.DataflowBlockOptions); }

        /// <summary>The data to display in the debugger display attribute.</summary>
        private object DebuggerDisplayContent => $"{Common.GetNameForDebugger(this, _source.DataflowBlockOptions)}, HasValue={HasValueForDebugger}, Value={ValueForDebugger}";

        /// <summary>Gets the data to display in the debugger display attribute for this instance.</summary>
        object IDebuggerDisplay.Content { get { return DebuggerDisplayContent; } }

        /// <summary>Provides a debugger type proxy for the BroadcastBlock.</summary>
        private sealed class DebugView
        {
            /// <summary>The BroadcastBlock being debugged.</summary>
            private readonly BroadcastBlock<T> _broadcastBlock;
            /// <summary>Debug info about the source side of the broadcast.</summary>
            private readonly BroadcastingSourceCore<T>.DebuggingInformation _sourceDebuggingInformation;

            /// <summary>Initializes the debug view.</summary>
            /// <param name="broadcastBlock">The BroadcastBlock being debugged.</param>
            public DebugView(BroadcastBlock<T> broadcastBlock)
            {
                Debug.Assert(broadcastBlock != null, "Need a block with which to construct the debug view.");
                _broadcastBlock = broadcastBlock;
                _sourceDebuggingInformation = broadcastBlock._source.GetDebuggingInformation();
            }

            /// <summary>Gets the messages waiting to be processed.</summary>
            public IEnumerable<T> InputQueue { get { return _sourceDebuggingInformation.InputQueue; } }
            /// <summary>Gets whether the broadcast has a current value.</summary>
            public bool HasValue { get { return _broadcastBlock.HasValueForDebugger; } }
            /// <summary>Gets the broadcast's current value.</summary>
            public T Value { get { return _broadcastBlock.ValueForDebugger; } }

            /// <summary>Gets the task being used for output processing.</summary>
            public Task? TaskForOutputProcessing { get { return _sourceDebuggingInformation.TaskForOutputProcessing; } }

            /// <summary>Gets the DataflowBlockOptions used to configure this block.</summary>
            public DataflowBlockOptions DataflowBlockOptions { get { return _sourceDebuggingInformation.DataflowBlockOptions; } }
            /// <summary>Gets whether the block is declining further messages.</summary>
            public bool IsDecliningPermanently { get { return _broadcastBlock._decliningPermanently; } }
            /// <summary>Gets whether the block is completed.</summary>
            public bool IsCompleted { get { return _sourceDebuggingInformation.IsCompleted; } }
            /// <summary>Gets the block's Id.</summary>
            public int Id { get { return Common.GetBlockId(_broadcastBlock); } }

            /// <summary>Gets the set of all targets linked from this block.</summary>
            public TargetRegistry<T> LinkedTargets { get { return _sourceDebuggingInformation.LinkedTargets; } }
            /// <summary>Gets the set of all targets linked from this block.</summary>
            public ITargetBlock<T>? NextMessageReservedFor { get { return _sourceDebuggingInformation.NextMessageReservedFor; } }
        }

        /// <summary>Provides a core implementation for blocks that implement <see cref="ISourceBlock{TOutput}"/>.</summary>
        /// <typeparam name="TOutput">Specifies the type of data supplied by the <see cref="SourceCore{TOutput}"/>.</typeparam>
        [DebuggerDisplay("{DebuggerDisplayContent,nq}")]
        private sealed class BroadcastingSourceCore<TOutput>
        {
            /// <summary>A registry used to store all linked targets and information about them.</summary>
            private readonly TargetRegistry<TOutput> _targetRegistry;
            /// <summary>All of the output messages queued up to be received by consumers/targets.</summary>
            private readonly Queue<TOutput> _messages = new Queue<TOutput>();
            /// <summary>A TaskCompletionSource that represents the completion of this block.</summary>
            private readonly TaskCompletionSource<VoidResult> _completionTask = new TaskCompletionSource<VoidResult>();
            /// <summary>
            /// An action to be invoked on the owner block when an item is removed.
            /// This may be null if the owner block doesn't need to be notified.
            /// </summary>
            private readonly Action<int>? _itemsRemovedAction;

            /// <summary>Gets the object to use as the outgoing lock.</summary>
            private object OutgoingLock { get { return _completionTask; } }
            /// <summary>Gets the object to use as the value lock.</summary>
            private object ValueLock { get { return _targetRegistry; } }

            /// <summary>The source utilize this helper.</summary>
            private readonly BroadcastBlock<TOutput> _owningSource;
            /// <summary>The options used to configure this block's execution.</summary>
            private readonly DataflowBlockOptions _dataflowBlockOptions;
            /// <summary>The cloning function to use.</summary>
            private readonly Func<TOutput, TOutput>? _cloningFunction;

            /// <summary>An indicator whether _currentMessage has a value.</summary>
            private bool _currentMessageIsValid;
            /// <summary>The message currently being broadcast.</summary>
            private TOutput? _currentMessage;
            /// <summary>The target that the next message is reserved for, or null if nothing is reserved.</summary>
            private ITargetBlock<TOutput>? _nextMessageReservedFor;
            /// <summary>Whether this block should again attempt to offer messages to targets.</summary>
            private bool _enableOffering;
            /// <summary>Whether all future messages should be declined.</summary>
            private bool _decliningPermanently;
            /// <summary>The task used to process the output and offer it to targets.</summary>
            private Task? _taskForOutputProcessing;
            /// <summary>Exceptions that may have occurred and gone unhandled during processing.</summary>
            private List<Exception>? _exceptions;
            /// <summary>Counter for message IDs unique within this source block.</summary>
            private long _nextMessageId = 1; // We are going to use this value before incrementing.
            /// <summary>Whether someone has reserved the right to call CompleteBlockOncePossible.</summary>
            private bool _completionReserved;

            /// <summary>Initializes the source core.</summary>
            /// <param name="owningSource">The source utilizing this core.</param>
            /// <param name="cloningFunction">The function to use to clone the data when offered to other blocks.  May be null.</param>
            /// <param name="dataflowBlockOptions">The options to use to configure the block.</param>
            /// <param name="itemsRemovedAction">Action to invoke when an item is removed.</param>
            internal BroadcastingSourceCore(
                BroadcastBlock<TOutput> owningSource,
                Func<TOutput, TOutput>? cloningFunction,
                DataflowBlockOptions dataflowBlockOptions,
                Action<int>? itemsRemovedAction)
            {
                Debug.Assert(owningSource != null, "Must be associated with a broadcast block.");
                Debug.Assert(dataflowBlockOptions != null, "Options are required to configure this block.");

                // Store the arguments
                _owningSource = owningSource;
                _cloningFunction = cloningFunction;
                _dataflowBlockOptions = dataflowBlockOptions;
                _itemsRemovedAction = itemsRemovedAction;

                // Construct members that depend on the arguments
                _targetRegistry = new TargetRegistry<TOutput>(_owningSource);
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="TryReceive"]/*' />
            internal bool TryReceive(Predicate<TOutput>? filter, [MaybeNullWhen(false)] out TOutput item)
            {
                // Take the lock only long enough to get the message,
                // synchronizing with other activities on the block.
                // We don't want to execute the user-provided cloning delegate
                // while holding the lock.
                TOutput? message;
                bool isValid;
                lock (OutgoingLock)
                {
                    lock (ValueLock)
                    {
                        message = _currentMessage;
                        isValid = _currentMessageIsValid;
                    }
                }

                // Clone and hand back a message if we have one and if it passes the filter.
                // (A null filter means all messages pass.)
                if (isValid && (filter == null || filter(message!)))
                {
                    item = CloneItem(message!);
                    return true;
                }
                else
                {
                    item = default(TOutput);
                    return false;
                }
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="TryReceiveAll"]/*' />
            internal bool TryReceiveAll([NotNullWhen(true)] out IList<TOutput>? items)
            {
                // Try to receive the one item this block may have.
                // If we can, give back an array of one item. Otherwise, give back null.
                TOutput? item;
                if (TryReceive(null, out item))
                {
                    items = new TOutput[] { item };
                    return true;
                }
                else
                {
                    items = null;
                    return false;
                }
            }

            /// <summary>Adds a message to the source block for propagation.</summary>
            /// <param name="item">The item to be wrapped in a message to be added.</param>
            internal void AddMessage(TOutput item)
            {
                // This method must not take the outgoing lock, as it will be called in situations
                // where a derived type's incoming lock is held.  The lock leveling structure
                // we're employing is such that outgoing may be held while acquiring incoming, but
                // of course not the other way around.  This is the reason why DataflowSourceBlock
                // needs ValueLock as well.  Otherwise, it would be pure overhead.
                lock (ValueLock)
                {
                    if (_decliningPermanently) return;
                    _messages.Enqueue(item);
                    if (_messages.Count == 1) _enableOffering = true;
                    OfferAsyncIfNecessary();
                }
            }

            /// <summary>Informs the block that it will not be receiving additional messages.</summary>
            internal void Complete()
            {
                lock (ValueLock)
                {
                    _decliningPermanently = true;

                    // Complete may be called in a context where an incoming lock is held.  We need to
                    // call CompleteBlockIfPossible, but we can't do so if the incoming lock is held.
                    // However, now that _decliningPermanently has been set, the timing of
                    // CompleteBlockIfPossible doesn't matter, so we schedule it to run asynchronously
                    // and take the necessary locks in a situation where we're sure it won't cause a problem.
                    Task.Factory.StartNew(state =>
                    {
                        var thisSourceCore = (BroadcastingSourceCore<TOutput>)state!;
                        lock (thisSourceCore.OutgoingLock)
                        {
                            lock (thisSourceCore.ValueLock)
                            {
                                thisSourceCore.CompleteBlockIfPossible();
                            }
                        }
                    }, this, CancellationToken.None, Common.GetCreationOptionsForTask(), TaskScheduler.Default);
                }
            }

            /// <summary>Clones the item.</summary>
            /// <param name="item">The item to clone.</param>
            /// <returns>The cloned item.</returns>
            private TOutput CloneItem(TOutput item)
            {
                return _cloningFunction != null ?
                    _cloningFunction(item) :
                    item;
            }

            /// <summary>Offers the current message to a specific target.</summary>
            /// <param name="target">The target to which to offer the current message.</param>
            private void OfferCurrentMessageToNewTarget(ITargetBlock<TOutput> target)
            {
                Debug.Assert(target != null, "Target required to offer messages to.");
                Common.ContractAssertMonitorStatus(OutgoingLock, held: true);
                Common.ContractAssertMonitorStatus(ValueLock, held: false);

                // Get the current message if there is one
                TOutput? currentMessage;
                bool isValid;
                lock (ValueLock)
                {
                    currentMessage = _currentMessage;
                    isValid = _currentMessageIsValid;
                }

                // If there is no valid message yet, there is nothing to offer
                if (!isValid) return;

                // Offer it to the target.
                // We must not increment the message ID here. We only do that when we populate _currentMessage, i.e. when we dequeue.
                bool useCloning = _cloningFunction != null;
                DataflowMessageStatus result = target.OfferMessage(new DataflowMessageHeader(_nextMessageId), currentMessage!, _owningSource, consumeToAccept: useCloning);

                // If accepted and the target was linked as "unlinkAfterOne", remove it
                if (result == DataflowMessageStatus.Accepted)
                {
                    if (!useCloning)
                    {
                        // If accepted and the target was linked as "once", mark it for removal.
                        // If we were forcing consumption, this removal would have already
                        // happened in ConsumeMessage.
                        _targetRegistry.Remove(target, onlyIfReachedMaxMessages: true);
                    }
                }
                // If declined permanently, remove it
                else if (result == DataflowMessageStatus.DecliningPermanently)
                {
                    _targetRegistry.Remove(target);
                }
                else Debug.Assert(result != DataflowMessageStatus.NotAvailable, "Messages from a Broadcast should never be missed.");
            }

            /// <summary>Offers messages to targets.</summary>
            private bool OfferToTargets()
            {
                Common.ContractAssertMonitorStatus(OutgoingLock, held: true);
                Common.ContractAssertMonitorStatus(ValueLock, held: false);

                DataflowMessageHeader header = default(DataflowMessageHeader);
                TOutput? message = default(TOutput);
                int numDequeuedMessages = 0;
                lock (ValueLock)
                {
                    // If there's a reservation or there aren't any more messages,
                    // there's nothing for us to do.  If there's no reservation
                    // and a message is available, dequeue the next one and store it
                    // as the new current.  If we're now at 0 message, disable further
                    // propagation until more messages arrive.
                    if (_nextMessageReservedFor == null && _messages.Count > 0)
                    {
                        // If there  are no targets registered, we might as well empty out the broadcast,
                        // keeping just the last.  Otherwise, it'll happen anyway, but much more expensively.
                        if (_targetRegistry.FirstTargetNode == null)
                        {
                            while (_messages.Count > 1)
                            {
                                _messages.Dequeue();
                                numDequeuedMessages++;
                            }
                        }

                        // Get the next message to offer
                        Debug.Assert(_messages.Count > 0, "There must be at least one message to dequeue.");
                        _currentMessage = message = _messages.Dequeue();
                        numDequeuedMessages++;
                        _currentMessageIsValid = true;
                        header = new DataflowMessageHeader(++_nextMessageId);
                        if (_messages.Count == 0) _enableOffering = false;
                    }
                    else
                    {
                        _enableOffering = false;
                        return false;
                    }
                } // must not hold ValueLock when calling out to targets

                // Offer the message
                if (header.IsValid)
                {
                    // Notify the owner block that our count has decreased
                    if (_itemsRemovedAction != null) _itemsRemovedAction(numDequeuedMessages);

                    // Offer it to each target, unless a soleTarget was provided, which case just offer it to that one.
                    TargetRegistry<TOutput>.LinkedTargetInfo? cur = _targetRegistry.FirstTargetNode;
                    while (cur != null)
                    {
                        // Note that during OfferMessage, a target may call ConsumeMessage, which may unlink the target
                        // if the target is registered as "once".  Doing so will remove the target from the targets list.
                        // As such, we avoid using an enumerator over _targetRegistry and instead walk from back to front,
                        // so that if an element is removed, it won't affect the rest of our walk.
                        TargetRegistry<TOutput>.LinkedTargetInfo? next = cur.Next;
                        ITargetBlock<TOutput> target = cur.Target;
                        OfferMessageToTarget(header, message, target);
                        cur = next;
                    }
                }
                return true;
            }

            /// <summary>Offers the specified message to the specified target.</summary>
            /// <param name="header">The header of the message to offer.</param>
            /// <param name="message">The message to offer.</param>
            /// <param name="target">The target to which the message should be offered.</param>
            /// <remarks>
            /// This will remove the target from the target registry if the result of the propagation demands it.
            /// </remarks>
            private void OfferMessageToTarget(DataflowMessageHeader header, TOutput message, ITargetBlock<TOutput> target)
            {
                Common.ContractAssertMonitorStatus(OutgoingLock, held: true);
                Common.ContractAssertMonitorStatus(ValueLock, held: false);

                // Offer the message.  If there's a cloning function, we force the target to
                // come back to us to consume the message, allowing us the opportunity to run
                // the cloning function once we know they want the data.  If there is no cloning
                // function, there's no reason for them to call back here.
                bool useCloning = _cloningFunction != null;
                switch (target.OfferMessage(header, message, _owningSource, consumeToAccept: useCloning))
                {
                    case DataflowMessageStatus.Accepted:
                        if (!useCloning)
                        {
                            // If accepted and the target was linked as "once", mark it for removal.
                            // If we were forcing consumption, this removal would have already
                            // happened in ConsumeMessage.
                            _targetRegistry.Remove(target, onlyIfReachedMaxMessages: true);
                        }
                        break;

                    case DataflowMessageStatus.DecliningPermanently:
                        // If declined permanently, mark the target for removal
                        _targetRegistry.Remove(target);
                        break;

                    case DataflowMessageStatus.NotAvailable:
                        Debug.Assert(false, "Messages from a Broadcast should never be missed.");
                        break;
                        // No action required for Postponed or Declined
                }
            }

            /// <summary>Called when we want to enable asynchronously offering message to targets.</summary>
            /// <param name="isReplacementReplica">Whether this call is the continuation of a previous message loop.</param>
            private void OfferAsyncIfNecessary(bool isReplacementReplica = false)
            {
                Common.ContractAssertMonitorStatus(ValueLock, held: true);
                // This method must not take the OutgoingLock.

                bool currentlyProcessing = _taskForOutputProcessing != null;
                bool processingToDo = _enableOffering && _messages.Count > 0;

                // If there's any work to be done...
                if (!currentlyProcessing && processingToDo && !CanceledOrFaulted)
                {
                    // Create task and store into _taskForOutputProcessing prior to scheduling the task
                    // so that _taskForOutputProcessing will be visibly set in the task loop.
                    _taskForOutputProcessing = new Task(thisSourceCore => ((BroadcastingSourceCore<TOutput>)thisSourceCore!).OfferMessagesLoopCore(), this,
                                                        Common.GetCreationOptionsForTask(isReplacementReplica));

#if FEATURE_TRACING
                    DataflowEtwProvider etwLog = DataflowEtwProvider.Log;
                    if (etwLog.IsEnabled())
                    {
                        etwLog.TaskLaunchedForMessageHandling(
                            _owningSource, _taskForOutputProcessing, DataflowEtwProvider.TaskLaunchedReason.OfferingOutputMessages, _messages.Count);
                    }
#endif

                    // Start the task handling scheduling exceptions
                    Exception? exception = Common.StartTaskSafe(_taskForOutputProcessing, _dataflowBlockOptions.TaskScheduler);
                    if (exception != null)
                    {
                        // First, log the exception while the processing state is dirty which is preventing the block from completing.
                        // Then revert the proactive processing state changes.
                        // And last, try to complete the block.
                        AddException(exception);
                        _decliningPermanently = true;
                        _taskForOutputProcessing = null;

                        // Get out from under currently held locks - ValueLock is taken, but OutgoingLock may not be.
                        // Re-take the locks on a separate thread.
                        Task.Factory.StartNew(state =>
                        {
                            var thisSourceCore = (BroadcastingSourceCore<TOutput>)state!;
                            lock (thisSourceCore.OutgoingLock)
                            {
                                lock (thisSourceCore.ValueLock)
                                {
                                    thisSourceCore.CompleteBlockIfPossible();
                                }
                            }
                        }, this, CancellationToken.None, Common.GetCreationOptionsForTask(), TaskScheduler.Default);
                    }
                }
            }

            /// <summary>Task body used to process messages.</summary>
            private void OfferMessagesLoopCore()
            {
                try
                {
                    int maxMessagesPerTask = _dataflowBlockOptions.ActualMaxMessagesPerTask;
                    lock (OutgoingLock)
                    {
                        // Offer as many messages as we can
                        for (int counter = 0;
                            counter < maxMessagesPerTask && !CanceledOrFaulted;
                            counter++)
                        {
                            if (!OfferToTargets()) break;
                        }
                    }
                }
                catch (Exception exception)
                {
                    _owningSource.CompleteCore(exception, storeExceptionEvenIfAlreadyCompleting: true);
                }
                finally
                {
                    lock (OutgoingLock)
                    {
                        lock (ValueLock)
                        {
                            // We're no longer processing, so null out the processing task
                            _taskForOutputProcessing = null;

                            // However, we may have given up early because we hit our own configured
                            // processing limits rather than because we ran out of work to do.  If that's
                            // the case, make sure we spin up another task to keep going.
                            OfferAsyncIfNecessary(isReplacementReplica: true);

                            // If, however, we stopped because we ran out of work to do and we
                            // know we'll never get more, then complete.
                            CompleteBlockIfPossible();
                        }
                    }
                }
            }

            /// <summary>Completes the block's processing if there's nothing left to do and never will be.</summary>
            private void CompleteBlockIfPossible()
            {
                Common.ContractAssertMonitorStatus(OutgoingLock, held: true);
                Common.ContractAssertMonitorStatus(ValueLock, held: true);

                if (!_completionReserved)
                {
                    bool currentlyProcessing = _taskForOutputProcessing != null;
                    bool noMoreMessages = _decliningPermanently && _messages.Count == 0;

                    // Are we done forever?
                    bool complete = !currentlyProcessing && (noMoreMessages || CanceledOrFaulted);
                    if (complete)
                    {
                        CompleteBlockIfPossible_Slow();
                    }
                }
            }

            /// <summary>
            /// Slow path for CompleteBlockIfPossible.
            /// Separating out the slow path into its own method makes it more likely that the fast path method will get inlined.
            /// </summary>
            private void CompleteBlockIfPossible_Slow()
            {
                Debug.Assert(_taskForOutputProcessing == null, "There must be no processing tasks.");
                Debug.Assert(
                    (_decliningPermanently && _messages.Count == 0) || CanceledOrFaulted,
                    "There must be no more messages or the block must be canceled or faulted.");

                _completionReserved = true;

                // Run asynchronously to get out of the currently held locks
                Task.Factory.StartNew(thisSourceCore => ((BroadcastingSourceCore<TOutput>)thisSourceCore!).CompleteBlockOncePossible(),
                    this, CancellationToken.None, Common.GetCreationOptionsForTask(), TaskScheduler.Default);
            }

            /// <summary>
            /// Completes the block.  This must only be called once, and only once all of the completion conditions are met.
            /// As such, it must only be called from CompleteBlockIfPossible.
            /// </summary>
            private void CompleteBlockOncePossible()
            {
                TargetRegistry<TOutput>.LinkedTargetInfo? linkedTargets;
                List<Exception>? exceptions;

                // Clear out the target registry and buffers to help avoid memory leaks.
                // We do not clear _currentMessage, which should remain as that message forever.
                lock (OutgoingLock)
                {
                    // Save the linked list of targets so that it could be traversed later to propagate completion
                    linkedTargets = _targetRegistry.ClearEntryPoints();
                    lock (ValueLock)
                    {
                        _messages.Clear();

                        // Save a local reference to the exceptions list and null out the field,
                        // so that if the target side tries to add an exception this late,
                        // it will go to a separate list (that will be ignored.)
                        exceptions = _exceptions;
                        _exceptions = null;
                    }
                }

                // If it's due to an exception, finish in a faulted state
                if (exceptions != null)
                {
                    _completionTask.TrySetException(exceptions);
                }
                // It's due to cancellation, finish in a canceled state
                else if (_dataflowBlockOptions.CancellationToken.IsCancellationRequested)
                {
                    _completionTask.TrySetCanceled();
                }
                // Otherwise, finish in a successful state.
                else
                {
                    _completionTask.TrySetResult(default(VoidResult));
                }

                // Now that the completion task is completed, we may propagate completion to the linked targets
                _targetRegistry.PropagateCompletion(linkedTargets);
#if FEATURE_TRACING
                DataflowEtwProvider etwLog = DataflowEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.DataflowBlockCompleted(_owningSource);
                }
#endif
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="LinkTo"]/*' />
            internal IDisposable LinkTo(ITargetBlock<TOutput> target, DataflowLinkOptions linkOptions)
            {
                // Validate arguments
                if (target == null) throw new ArgumentNullException(nameof(target));
                if (linkOptions == null) throw new ArgumentNullException(nameof(linkOptions));

                lock (OutgoingLock)
                {
                    // If we've completed or completion has at least started, offer the message to this target,
                    // and propagate completion if that was requested.
                    // Then there's nothing more to be done.
                    if (_completionReserved)
                    {
                        OfferCurrentMessageToNewTarget(target);
                        if (linkOptions.PropagateCompletion) Common.PropagateCompletionOnceCompleted(_completionTask.Task, target);
                        return Disposables.Nop;
                    }

                    // Otherwise, add the target and then offer it the current
                    // message.  We do this in this order because offering may
                    // cause the target to be removed if it's unlinkAfterOne,
                    // and in the reverse order we would end up adding the target
                    // after it was "removed".
                    _targetRegistry.Add(ref target, linkOptions);
                    OfferCurrentMessageToNewTarget(target);
                    return Common.CreateUnlinker(OutgoingLock, _targetRegistry, target);
                }
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="ConsumeMessage"]/*' />
            internal TOutput? ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target, out bool messageConsumed)
            {
                // Validate arguments
                if (!messageHeader.IsValid) throw new ArgumentException(SR.Argument_InvalidMessageHeader, nameof(messageHeader));
                if (target == null) throw new ArgumentNullException(nameof(target));

                TOutput? valueToClone;
                lock (OutgoingLock) // We may currently be calling out under this lock to the target; requires it to be reentrant
                {
                    lock (ValueLock)
                    {
                        // If this isn't the next message to be served up, bail
                        if (messageHeader.Id != _nextMessageId)
                        {
                            messageConsumed = false;
                            return default(TOutput);
                        }

                        // If the caller has the reservation, release the reservation.
                        // We still allow others to take the message if there's a reservation.
                        if (_nextMessageReservedFor == target)
                        {
                            _nextMessageReservedFor = null;
                            _enableOffering = true;
                        }
                        _targetRegistry.Remove(target, onlyIfReachedMaxMessages: true);

                        OfferAsyncIfNecessary();
                        CompleteBlockIfPossible();

                        // Return a clone of the consumed message.
                        valueToClone = _currentMessage;
                    }
                }

                messageConsumed = true;
                return CloneItem(valueToClone!);
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="ReserveMessage"]/*' />
            internal bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
            {
                // Validate arguments
                if (!messageHeader.IsValid) throw new ArgumentException(SR.Argument_InvalidMessageHeader, nameof(messageHeader));
                if (target == null) throw new ArgumentNullException(nameof(target));

                lock (OutgoingLock)
                {
                    // If no one currently holds a reservation...
                    if (_nextMessageReservedFor == null)
                    {
                        lock (ValueLock)
                        {
                            // ...and the requested message is next in line, allow it
                            if (messageHeader.Id == _nextMessageId)
                            {
                                _nextMessageReservedFor = target;
                                _enableOffering = false;
                                return true;
                            }
                        }
                    }
                }
                return false;
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="ReleaseReservation"]/*' />
            internal void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
            {
                // Validate arguments
                if (!messageHeader.IsValid) throw new ArgumentException(SR.Argument_InvalidMessageHeader, nameof(messageHeader));
                if (target == null) throw new ArgumentNullException(nameof(target));

                lock (OutgoingLock)
                {
                    // If someone else holds the reservation, bail.
                    if (_nextMessageReservedFor != target) throw new InvalidOperationException(SR.InvalidOperation_MessageNotReservedByTarget);

                    TOutput? messageToReoffer;
                    lock (ValueLock)
                    {
                        // If this is not the message at the head of the queue, bail
                        if (messageHeader.Id != _nextMessageId) throw new InvalidOperationException(SR.InvalidOperation_MessageNotReservedByTarget);

                        // Otherwise, release the reservation, and reoffer the message to all targets.
                        _nextMessageReservedFor = null;
                        _enableOffering = true;
                        messageToReoffer = _currentMessage;
                        OfferAsyncIfNecessary();
                    }

                    // We need to explicitly reoffer this message to the releaser,
                    // as otherwise if the target has join behavior it could end up waiting for an offer from
                    // this broadcast forever, even though data is in fact available.  We could only
                    // do this if _messages.Count == 0, as if it's > 0 the message will get overwritten
                    // as part of the asynchronous offering, but for consistency we should always reoffer
                    // the current message.
                    OfferMessageToTarget(messageHeader, messageToReoffer!, target);
                }
            }

            /// <summary>Gets whether the source has had cancellation requested or an exception has occurred.</summary>
            private bool CanceledOrFaulted
            {
                get
                {
                    // Cancellation is honored as soon as the CancellationToken has been signaled.
                    // Faulting is honored after an exception has been encountered and the owning block
                    // has invoked Complete on us.
                    return _dataflowBlockOptions.CancellationToken.IsCancellationRequested ||
                        (Volatile.Read(ref _exceptions) != null && _decliningPermanently);
                }
            }

            /// <summary>Adds an individual exception to this source.</summary>
            /// <param name="exception">The exception to add</param>
            internal void AddException(Exception exception)
            {
                Debug.Assert(exception != null, "An exception to add is required.");
                Debug.Assert(!Completion.IsCompleted || Completion.IsFaulted, "The block must either not be completed or be faulted if we're still storing exceptions.");
                lock (ValueLock)
                {
                    Common.AddException(ref _exceptions, exception);
                }
            }

            /// <summary>Adds exceptions to this source.</summary>
            /// <param name="exceptions">The exceptions to add</param>
            internal void AddExceptions(List<Exception> exceptions)
            {
                Debug.Assert(exceptions != null, "A list of exceptions to add is required.");
                Debug.Assert(!Completion.IsCompleted || Completion.IsFaulted, "The block must either not be completed or be faulted if we're still storing exceptions.");
                lock (ValueLock)
                {
                    foreach (Exception exception in exceptions)
                    {
                        Common.AddException(ref _exceptions, exception);
                    }
                }
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Completion"]/*' />
            internal Task Completion { get { return _completionTask.Task; } }

            /// <summary>Gets the DataflowBlockOptions used to configure this block.</summary>
            internal DataflowBlockOptions DataflowBlockOptions { get { return _dataflowBlockOptions; } }

            /// <summary>Gets the object to display in the debugger display attribute.</summary>
            private object DebuggerDisplayContent
            {
                get
                {
                    var displaySource = _owningSource as IDebuggerDisplay;
                    return $"Block=\"{(displaySource != null ? displaySource.Content : _owningSource)}\"";
                }
            }

            /// <summary>Gets information about this helper to be used for display in a debugger.</summary>
            /// <returns>Debugging information about this source core.</returns>
            internal DebuggingInformation GetDebuggingInformation() { return new DebuggingInformation(this); }

            /// <summary>Provides debugging information about the source core.</summary>
            internal sealed class DebuggingInformation
            {
                /// <summary>The source being viewed.</summary>
                private readonly BroadcastingSourceCore<TOutput> _source;

                /// <summary>Initializes the type proxy.</summary>
                /// <param name="source">The source being viewed.</param>
                public DebuggingInformation(BroadcastingSourceCore<TOutput> source) { _source = source; }

                /// <summary>Gets whether the source contains a current message.</summary>
                public bool HasValue { get { return _source._currentMessageIsValid; } }
                /// <summary>Gets the value of the source's current message.</summary>
                public TOutput Value { get { return _source._currentMessage!; } }
                /// <summary>Gets the messages available for receiving.</summary>
                public IEnumerable<TOutput> InputQueue { get { return _source._messages.ToList(); } }
                /// <summary>Gets the task being used for output processing.</summary>
                public Task? TaskForOutputProcessing { get { return _source._taskForOutputProcessing; } }

                /// <summary>Gets the DataflowBlockOptions used to configure this block.</summary>
                public DataflowBlockOptions DataflowBlockOptions { get { return _source._dataflowBlockOptions; } }
                /// <summary>Gets whether the block is completed.</summary>
                public bool IsCompleted { get { return _source.Completion.IsCompleted; } }

                /// <summary>Gets the set of all targets linked from this block.</summary>
                public TargetRegistry<TOutput> LinkedTargets { get { return _source._targetRegistry; } }
                /// <summary>Gets the target that holds a reservation on the next message, if any.</summary>
                public ITargetBlock<TOutput>? NextMessageReservedFor { get { return _source._nextMessageReservedFor; } }
            }
        }
    }
}
