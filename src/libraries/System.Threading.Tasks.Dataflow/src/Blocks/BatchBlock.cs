// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// BatchBlock.cs
//
//
// A propagator block that groups individual messages into arrays of messages.
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
    /// <summary>Provides a dataflow block that batches inputs into arrays.</summary>
    /// <typeparam name="T">Specifies the type of data put into batches.</typeparam>
    [DebuggerDisplay("{DebuggerDisplayContent,nq}")]
    [DebuggerTypeProxy(typeof(BatchBlock<>.DebugView))]
    public sealed class BatchBlock<T> : IPropagatorBlock<T, T[]>, IReceivableSourceBlock<T[]>, IDebuggerDisplay
    {
        /// <summary>The target half of this batch.</summary>
        private readonly BatchBlockTargetCore _target;
        /// <summary>The source half of this batch.</summary>
        private readonly SourceCore<T[]> _source;

        /// <summary>Initializes this <see cref="BatchBlock{T}"/> with the specified batch size.</summary>
        /// <param name="batchSize">The number of items to group into a batch.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">The <paramref name="batchSize"/> must be positive.</exception>
        public BatchBlock(int batchSize) :
            this(batchSize, GroupingDataflowBlockOptions.Default)
        { }

        /// <summary>Initializes this <see cref="BatchBlock{T}"/> with the  specified batch size, declining option, and block options.</summary>
        /// <param name="batchSize">The number of items to group into a batch.</param>
        /// <param name="dataflowBlockOptions">The options with which to configure this <see cref="BatchBlock{T}"/>.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">The <paramref name="batchSize"/> must be positive.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">The <paramref name="batchSize"/> must be no greater than the value of the BoundedCapacity option if a non-default value has been set.</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="dataflowBlockOptions"/> is null (Nothing in Visual Basic).</exception>
        public BatchBlock(int batchSize, GroupingDataflowBlockOptions dataflowBlockOptions)
        {
            // Validate arguments
            if (batchSize < 1) throw new ArgumentOutOfRangeException(nameof(batchSize), SR.ArgumentOutOfRange_GenericPositive);
            if (dataflowBlockOptions == null) throw new ArgumentNullException(nameof(dataflowBlockOptions));
            if (dataflowBlockOptions.BoundedCapacity > 0 && dataflowBlockOptions.BoundedCapacity < batchSize) throw new ArgumentOutOfRangeException(nameof(batchSize), SR.ArgumentOutOfRange_BatchSizeMustBeNoGreaterThanBoundedCapacity);

            // Ensure we have options that can't be changed by the caller
            dataflowBlockOptions = dataflowBlockOptions.DefaultOrClone();

            // Initialize bounding actions
            Action<ISourceBlock<T[]>, int>? onItemsRemoved = null;
            Func<ISourceBlock<T[]>, T[], IList<T[]>?, int>? itemCountingFunc = null;
            if (dataflowBlockOptions.BoundedCapacity > 0)
            {
                onItemsRemoved = (owningSource, count) => ((BatchBlock<T>)owningSource)._target.OnItemsRemoved(count);
                itemCountingFunc = (owningSource, singleOutputItem, multipleOutputItems) => BatchBlockTargetCore.CountItems(singleOutputItem, multipleOutputItems);
            }

            // Initialize source
            _source = new SourceCore<T[]>(this, dataflowBlockOptions,
                owningSource => ((BatchBlock<T>)owningSource)._target.Complete(exception: null, dropPendingMessages: true, releaseReservedMessages: false),
                onItemsRemoved, itemCountingFunc);

            // Initialize target
            _target = new BatchBlockTargetCore(this, batchSize, batch => _source.AddMessage(batch), dataflowBlockOptions);

            // When the target is done, let the source know it won't be getting any more data
            _target.Completion.ContinueWith(delegate { _source.Complete(); },
                CancellationToken.None, Common.GetContinuationOptions(), TaskScheduler.Default);

            // It is possible that the source half may fault on its own, e.g. due to a task scheduler exception.
            // In those cases we need to fault the target half to drop its buffered messages and to release its
            // reservations. This should not create an infinite loop, because all our implementations are designed
            // to handle multiple completion requests and to carry over only one.
            _source.Completion.ContinueWith((completed, state) =>
            {
                var thisBlock = ((BatchBlock<T>)state!) as IDataflowBlock;
                Debug.Assert(completed.IsFaulted, "The source must be faulted in order to trigger a target completion.");
                thisBlock.Fault(completed.Exception!);
            }, this, CancellationToken.None, Common.GetContinuationOptions() | TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);

            // Handle async cancellation requests by declining on the target
            Common.WireCancellationToComplete(
                dataflowBlockOptions.CancellationToken, _source.Completion, state => ((BatchBlockTargetCore)state!).Complete(exception: null, dropPendingMessages: true, releaseReservedMessages: false), _target);
            DataflowEtwProvider etwLog = DataflowEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.DataflowBlockCreated(this, dataflowBlockOptions);
            }
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Complete"]/*' />
        public void Complete() { _target.Complete(exception: null, dropPendingMessages: false, releaseReservedMessages: false); }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Fault"]/*' />
        void IDataflowBlock.Fault(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));

            _target.Complete(exception, dropPendingMessages: true, releaseReservedMessages: false);
        }

        /// <summary>
        /// Triggers the <see cref="BatchBlock{T}"/> to initiate a batching operation even if the number
        /// of currently queued or postponed items is less than the <see cref="BatchSize"/>.
        /// </summary>
        /// <remarks>
        /// In greedy mode, a batch will be generated from queued items even if fewer exist than the batch size.
        /// In non-greedy mode, a batch will be generated asynchronously from postponed items even if
        /// fewer than the batch size can be consumed.
        /// </remarks>
        public void TriggerBatch() { _target.TriggerBatch(); }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="LinkTo"]/*' />
        public IDisposable LinkTo(ITargetBlock<T[]> target, DataflowLinkOptions linkOptions)
        {
            return _source.LinkTo(target, linkOptions);
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="TryReceive"]/*' />
        public bool TryReceive(Predicate<T[]>? filter, [NotNullWhen(true)] out T[]? item)
        {
            return _source.TryReceive(filter, out item);
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="TryReceiveAll"]/*' />
        public bool TryReceiveAll([NotNullWhen(true)] out IList<T[]>? items) { return _source.TryReceiveAll(out items); }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="OutputCount"]/*' />
        public int OutputCount { get { return _source.OutputCount; } }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Completion"]/*' />
        public Task Completion { get { return _source.Completion; } }

        /// <summary>Gets the size of the batches generated by this <see cref="BatchBlock{T}"/>.</summary>
        /// <remarks>
        /// If the number of items provided to the block is not evenly divisible by the batch size provided
        /// to the block's constructor, the block's final batch may contain fewer than the requested number of items.
        /// </remarks>
        public int BatchSize { get { return _target.BatchSize; } }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Targets/Member[@name="OfferMessage"]/*' />
        DataflowMessageStatus ITargetBlock<T>.OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T>? source, bool consumeToAccept)
        {
            return _target.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="ConsumeMessage"]/*' />
        T[]? ISourceBlock<T[]>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<T[]> target, out bool messageConsumed)
        {
            return _source.ConsumeMessage(messageHeader, target, out messageConsumed);
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="ReserveMessage"]/*' />
        bool ISourceBlock<T[]>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<T[]> target)
        {
            return _source.ReserveMessage(messageHeader, target);
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="ReleaseReservation"]/*' />
        void ISourceBlock<T[]>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<T[]> target)
        {
            _source.ReleaseReservation(messageHeader, target);
        }

        /// <summary>Gets the number of messages waiting to be offered.  This must only be used from the debugger as it avoids taking necessary locks.</summary>
        private int OutputCountForDebugger { get { return _source.GetDebuggingInformation().OutputCount; } }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="ToString"]/*' />
        public override string ToString() { return Common.GetNameForDebugger(this, _source.DataflowBlockOptions); }

        /// <summary>The data to display in the debugger display attribute.</summary>
        private object DebuggerDisplayContent =>
            $"{Common.GetNameForDebugger(this, _source.DataflowBlockOptions)}, BatchSize={BatchSize}, OutputCount={OutputCountForDebugger}";

        /// <summary>Gets the data to display in the debugger display attribute for this instance.</summary>
        object IDebuggerDisplay.Content { get { return DebuggerDisplayContent; } }

        /// <summary>Provides a debugger type proxy for the Batch.</summary>
        private sealed class DebugView
        {
            /// <summary>The batch block being viewed.</summary>
            private readonly BatchBlock<T> _batchBlock;
            /// <summary>The target half being viewed.</summary>
            private readonly BatchBlockTargetCore.DebuggingInformation _targetDebuggingInformation;
            /// <summary>The source half of the block being viewed.</summary>
            private readonly SourceCore<T[]>.DebuggingInformation _sourceDebuggingInformation;

            /// <summary>Initializes the debug view.</summary>
            /// <param name="batchBlock">The batch being viewed.</param>
            public DebugView(BatchBlock<T> batchBlock)
            {
                Debug.Assert(batchBlock != null, "Need a block with which to construct the debug view");
                _batchBlock = batchBlock;
                _targetDebuggingInformation = batchBlock._target.GetDebuggingInformation();
                _sourceDebuggingInformation = batchBlock._source.GetDebuggingInformation();
            }

            /// <summary>Gets the messages waiting to be processed.</summary>
            public IEnumerable<T> InputQueue { get { return _targetDebuggingInformation.InputQueue; } }
            /// <summary>Gets the messages waiting to be received.</summary>
            public IEnumerable<T[]> OutputQueue { get { return _sourceDebuggingInformation.OutputQueue; } }
            /// <summary>Gets the number of batches that have been completed.</summary>
            public long BatchesCompleted { get { return _targetDebuggingInformation.NumberOfBatchesCompleted; } }

            /// <summary>Gets the task being used for input processing.</summary>
            public Task? TaskForInputProcessing { get { return _targetDebuggingInformation.TaskForInputProcessing; } }
            /// <summary>Gets the task being used for output processing.</summary>
            public Task? TaskForOutputProcessing { get { return _sourceDebuggingInformation.TaskForOutputProcessing; } }

            /// <summary>Gets the DataflowBlockOptions used to configure this block.</summary>
            public GroupingDataflowBlockOptions DataflowBlockOptions { get { return _targetDebuggingInformation.DataflowBlockOptions; } }
            /// <summary>Gets the size of batches generated by the block.</summary>
            public int BatchSize { get { return _batchBlock.BatchSize; } }
            /// <summary>Gets whether the block is declining further messages.</summary>
            public bool IsDecliningPermanently { get { return _targetDebuggingInformation.IsDecliningPermanently; } }
            /// <summary>Gets whether the block is completed.</summary>
            public bool IsCompleted { get { return _sourceDebuggingInformation.IsCompleted; } }
            /// <summary>Gets the block's Id.</summary>
            public int Id { get { return Common.GetBlockId(_batchBlock); } }

            /// <summary>Gets the messages postponed by this batch.</summary>
            public QueuedMap<ISourceBlock<T>, DataflowMessageHeader>? PostponedMessages { get { return _targetDebuggingInformation.PostponedMessages; } }
            /// <summary>Gets the set of all targets linked from this block.</summary>
            public TargetRegistry<T[]> LinkedTargets { get { return _sourceDebuggingInformation.LinkedTargets; } }
            /// <summary>Gets the set of all targets linked from this block.</summary>
            public ITargetBlock<T[]>? NextMessageReservedFor { get { return _sourceDebuggingInformation.NextMessageReservedFor; } }
        }

        /// <summary>Provides the core target implementation for a Batch.</summary>
        [DebuggerDisplay("{DebuggerDisplayContent,nq}")]
        private sealed class BatchBlockTargetCore
        {
            /// <summary>The messages in this target.</summary>
            private readonly Queue<T> _messages = new Queue<T>();
            /// <summary>A task representing the completion of the block.</summary>
            private readonly TaskCompletionSource<VoidResult> _completionTask = new TaskCompletionSource<VoidResult>();

            /// <summary>Gets the object used as the incoming lock.</summary>
            private object IncomingLock { get { return _completionTask; } }

            /// <summary>The target that owns this target core.</summary>
            private readonly BatchBlock<T> _owningBatch;
            /// <summary>The batch size.</summary>
            private readonly int _batchSize;
            /// <summary>State used when in non-greedy mode.</summary>
            private readonly NonGreedyState? _nonGreedyState;
            /// <summary>Bounding state for when the block is executing in bounded mode.</summary>
            private readonly BoundingState? _boundingState;
            /// <summary>The options associated with this block.</summary>
            private readonly GroupingDataflowBlockOptions _dataflowBlockOptions;
            /// <summary>The action invoked with a completed batch.</summary>
            private readonly Action<T[]> _batchCompletedAction;

            /// <summary>Whether to stop accepting new messages.</summary>
            private bool _decliningPermanently;
            /// <summary>Whether we've completed at least one batch.</summary>
            private long _batchesCompleted;
            /// <summary>Whether someone has reserved the right to call CompleteBlockOncePossible.</summary>
            private bool _completionReserved;

            /// <summary>State used only when in non-greedy mode.</summary>
            private sealed class NonGreedyState
            {
                /// <summary>Collection of postponed messages.</summary>
                internal readonly QueuedMap<ISourceBlock<T>, DataflowMessageHeader> PostponedMessages;
                /// <summary>A temporary array used to store data retrieved from PostponedMessages.</summary>
                internal readonly KeyValuePair<ISourceBlock<T>, DataflowMessageHeader>[] PostponedMessagesTemp;
                /// <summary>A temporary list used in non-greedy mode when consuming postponed messages to store successfully reserved messages.</summary>
                internal readonly List<KeyValuePair<ISourceBlock<T>, KeyValuePair<DataflowMessageHeader, T>>> ReservedSourcesTemp;
                /// <summary>Whether the next batching operation should accept fewer than BatchSize items.</summary>
                /// <remarks>This value may be read not under a lock, but it must only be written to protected by the IncomingLock.</remarks>
                internal bool AcceptFewerThanBatchSize;
                /// <summary>The task used to process messages.</summary>
                internal Task? TaskForInputProcessing;

                /// <summary>Initializes the NonGreedyState.</summary>
                /// <param name="batchSize">The batch size used by the BatchBlock.</param>
                internal NonGreedyState(int batchSize)
                {
                    // A non-greedy batch requires at least batchSize sources to be successful.
                    // Thus, we initialize our collections to be able to store at least that many elements
                    // in order to avoid unnecessary allocations below that point.
                    Debug.Assert(batchSize > 0, "A positive batch size is required");
                    PostponedMessages = new QueuedMap<ISourceBlock<T>, DataflowMessageHeader>(batchSize);
                    PostponedMessagesTemp = new KeyValuePair<ISourceBlock<T>, DataflowMessageHeader>[batchSize];
                    ReservedSourcesTemp = new List<KeyValuePair<ISourceBlock<T>, KeyValuePair<DataflowMessageHeader, T>>>(batchSize);
                }
            }

            /// <summary>Initializes this target core with the specified configuration.</summary>
            /// <param name="owningBatch">The owning batch target.</param>
            /// <param name="batchSize">The number of items to group into a batch.</param>
            /// <param name="batchCompletedAction">The delegate to invoke when a batch is completed.</param>
            /// <param name="dataflowBlockOptions">The options with which to configure this <see cref="BatchBlock{T}"/>.  Assumed to be immutable.</param>
            /// <exception cref="System.ArgumentOutOfRangeException">The <paramref name="batchSize"/> must be positive.</exception>
            /// <exception cref="System.ArgumentNullException">The <paramref name="dataflowBlockOptions"/> is null (Nothing in Visual Basic).</exception>
            internal BatchBlockTargetCore(BatchBlock<T> owningBatch, int batchSize, Action<T[]> batchCompletedAction, GroupingDataflowBlockOptions dataflowBlockOptions)
            {
                Debug.Assert(owningBatch != null, "This batch target core must be associated with a batch block.");
                Debug.Assert(batchSize >= 1, "Batch sizes must be positive.");
                Debug.Assert(batchCompletedAction != null, "Completion action must be specified.");
                Debug.Assert(dataflowBlockOptions != null, "Options required to configure the block.");

                // Store arguments
                _owningBatch = owningBatch;
                _batchSize = batchSize;
                _batchCompletedAction = batchCompletedAction;
                _dataflowBlockOptions = dataflowBlockOptions;

                // We'll be using _nonGreedyState even if we are greedy with bounding
                bool boundingEnabled = dataflowBlockOptions.BoundedCapacity > 0;
                if (!_dataflowBlockOptions.Greedy || boundingEnabled) _nonGreedyState = new NonGreedyState(batchSize);
                if (boundingEnabled) _boundingState = new BoundingState(dataflowBlockOptions.BoundedCapacity);
            }

            /// <summary>
            /// Triggers a batching operation even if the number of currently queued or postponed items is less than the <see cref="BatchSize"/>.
            /// </summary>
            internal void TriggerBatch()
            {
                lock (IncomingLock)
                {
                    // If we shouldn't be doing any more work, bail.  Otherwise, note that we're willing to
                    // accept fewer items in the next batching operation, and ensure processing is kicked off.
                    if (!_decliningPermanently && !_dataflowBlockOptions.CancellationToken.IsCancellationRequested)
                    {
                        if (_nonGreedyState == null)
                        {
                            MakeBatchIfPossible(evenIfFewerThanBatchSize: true);
                        }
                        else
                        {
                            _nonGreedyState.AcceptFewerThanBatchSize = true;
                            ProcessAsyncIfNecessary();
                        }
                    }
                    CompleteBlockIfPossible();
                }
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Targets/Member[@name="OfferMessage"]/*' />
            internal DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T>? source, bool consumeToAccept)
            {
                // Validate arguments
                if (!messageHeader.IsValid) throw new ArgumentException(SR.Argument_InvalidMessageHeader, nameof(messageHeader));
                if (source == null && consumeToAccept) throw new ArgumentException(SR.Argument_CantConsumeFromANullSource, nameof(consumeToAccept));

                lock (IncomingLock)
                {
                    // If we shouldn't be accepting more messages, don't.
                    if (_decliningPermanently)
                    {
                        CompleteBlockIfPossible();
                        return DataflowMessageStatus.DecliningPermanently;
                    }

                    // We can directly accept the message if:
                    //      1) we are being greedy AND we are not bounding, OR
                    //      2) we are being greedy AND we are bounding AND there is room available AND there are no postponed messages AND we are not currently processing.
                    // (If there were any postponed messages, we would need to postpone so that ordering would be maintained.)
                    // (We should also postpone if we are currently processing, because there may be a race between consuming postponed messages and
                    // accepting new ones directly into the queue.)
                    if (_dataflowBlockOptions.Greedy &&
                            (_boundingState == null
                                ||
                             (_boundingState.CountIsLessThanBound && _nonGreedyState!.PostponedMessages.Count == 0 && _nonGreedyState.TaskForInputProcessing == null)))
                    {
                        // Consume the message from the source if necessary
                        if (consumeToAccept)
                        {
                            Debug.Assert(source != null, "We must have thrown if source == null && consumeToAccept == true.");

                            bool consumed;
                            messageValue = source.ConsumeMessage(messageHeader, _owningBatch, out consumed)!;
                            if (!consumed) return DataflowMessageStatus.NotAvailable;
                        }

                        // Once consumed, enqueue it.
                        _messages.Enqueue(messageValue!);
                        if (_boundingState != null) _boundingState.CurrentCount += 1; // track this new item against our bound

                        // Now start declining if the number of batches we've already made plus
                        // the number we can make from data already enqueued meets our quota.
                        if (!_decliningPermanently &&
                            (_batchesCompleted + (_messages.Count / _batchSize)) >= _dataflowBlockOptions.ActualMaxNumberOfGroups)
                        {
                            _decliningPermanently = true;
                        }

                        // Now that we have a message, see if we can make forward progress.
                        MakeBatchIfPossible(evenIfFewerThanBatchSize: false);

                        CompleteBlockIfPossible();
                        return DataflowMessageStatus.Accepted;
                    }
                    // Otherwise, we try to postpone if a source was provided
                    else if (source != null)
                    {
                        Debug.Assert(_nonGreedyState != null, "_nonGreedyState must have been initialized during construction in non-greedy mode.");

                        // We always postpone using _nonGreedyState even if we are being greedy with bounding
                        _nonGreedyState.PostponedMessages.Push(source, messageHeader);

                        // In non-greedy mode, we need to see if batch could be completed
                        if (!_dataflowBlockOptions.Greedy) ProcessAsyncIfNecessary();

                        return DataflowMessageStatus.Postponed;
                    }
                    // We can't do anything else about this message
                    return DataflowMessageStatus.Declined;
                }
            }

            /// <summary>Completes/faults the block.
            /// In general, it is not safe to pass releaseReservedMessages:true, because releasing of reserved messages
            /// is done without taking a lock. We pass releaseReservedMessages:true only when an exception has been
            /// caught inside the message processing loop which is a single instance at any given moment.</summary>
            internal void Complete(Exception? exception, bool dropPendingMessages, bool releaseReservedMessages, bool revertProcessingState = false)
            {
                // Ensure that no new messages may be added
                lock (IncomingLock)
                {
                    // Faulting from outside is allowed until we start declining permanently.
                    // Faulting from inside is allowed at any time.
                    if (exception != null && (!_decliningPermanently || releaseReservedMessages))
                    {
                        // Record the exception in the source.
                        // The source, which exposes its Completion to the public will take this
                        // into account and will complete in Faulted state.
                        _owningBatch._source.AddException(exception);
                    }

                    // Drop pending messages if requested
                    if (dropPendingMessages) _messages.Clear();
                }

                // Release reserved messages if requested.
                // This must be done from outside the lock.
                if (releaseReservedMessages)
                {
                    try { ReleaseReservedMessages(throwOnFirstException: false); }
                    catch (Exception e) { _owningBatch._source.AddException(e); }
                }

                // Triggering completion requires the lock
                lock (IncomingLock)
                {
                    // Revert the dirty processing state if requested
                    if (revertProcessingState)
                    {
                        Debug.Assert(_nonGreedyState != null && _nonGreedyState.TaskForInputProcessing != null,
                                        "The processing state must be dirty when revertProcessingState==true.");
                        _nonGreedyState.TaskForInputProcessing = null;
                    }

                    // Trigger completion
                    _decliningPermanently = true;
                    CompleteBlockIfPossible();
                }
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Completion"]/*' />
            internal Task Completion { get { return _completionTask.Task; } }

            /// <summary>Gets the size of the batches generated by this <see cref="BatchBlock{T}"/>.</summary>
            internal int BatchSize { get { return _batchSize; } }

            /// <summary>Gets whether the target has had cancellation requested or an exception has occurred.</summary>
            private bool CanceledOrFaulted
            {
                get
                {
                    return _dataflowBlockOptions.CancellationToken.IsCancellationRequested || _owningBatch._source.HasExceptions;
                }
            }

            /// <summary>Returns the available capacity to bring in postponed items. The exact values above _batchSize don't matter.</summary>
            private int BoundedCapacityAvailable
            {
                get
                {
                    Common.ContractAssertMonitorStatus(IncomingLock, held: true);

                    return _boundingState != null ?
                                _dataflowBlockOptions.BoundedCapacity - _boundingState.CurrentCount :
                                _batchSize;
                }
            }

            /// <summary>Completes the block once all completion conditions are met.</summary>
            private void CompleteBlockIfPossible()
            {
                Common.ContractAssertMonitorStatus(IncomingLock, held: true);

                if (!_completionReserved)
                {
                    bool currentlyProcessing = _nonGreedyState != null && _nonGreedyState.TaskForInputProcessing != null;
                    bool completedAllDesiredBatches = _batchesCompleted >= _dataflowBlockOptions.ActualMaxNumberOfGroups;
                    bool noMoreMessages = _decliningPermanently && _messages.Count < _batchSize;

                    bool complete = !currentlyProcessing && (completedAllDesiredBatches || noMoreMessages || CanceledOrFaulted);
                    if (complete)
                    {
                        _completionReserved = true;

                        // Make sure the target is declining
                        _decliningPermanently = true;

                        // If we still have straggling items remaining, make them into their own batch even though there are fewer than batchSize
                        if (_messages.Count > 0) MakeBatchIfPossible(evenIfFewerThanBatchSize: true);

                        // We need to complete the block, but we may have arrived here from an external
                        // call to the block.  To avoid running arbitrary code in the form of
                        // completion task continuations in that case, do it in a separate task.
                        Task.Factory.StartNew(thisTargetCore =>
                        {
                            var targetCore = (BatchBlockTargetCore)thisTargetCore!;

                            // Release any postponed messages
                            List<Exception>? exceptions = null;
                            if (targetCore._nonGreedyState != null)
                            {
                                // Note: No locks should be held at this point
                                Common.ReleaseAllPostponedMessages(targetCore._owningBatch,
                                                                   targetCore._nonGreedyState.PostponedMessages,
                                                                   ref exceptions);
                            }

                            if (exceptions != null)
                            {
                                // It is important to migrate these exceptions to the source part of the owning batch,
                                // because that is the completion task that is publicly exposed.
                                targetCore._owningBatch._source.AddExceptions(exceptions);
                            }

                            // Target's completion task is only available internally with the sole purpose
                            // of releasing the task that completes the parent. Hence the actual reason
                            // for completing this task doesn't matter.
                            targetCore._completionTask.TrySetResult(default(VoidResult));
                        }, this, CancellationToken.None, Common.GetCreationOptionsForTask(), TaskScheduler.Default);
                    }
                }
            }

            /// <summary>
            /// Gets whether we should launch further synchronous or asynchronous processing
            /// to create batches.
            /// </summary>
            private bool BatchesNeedProcessing
            {
                get
                {
                    Common.ContractAssertMonitorStatus(IncomingLock, held: true);

                    // If we're currently processing asynchronously, let that async task
                    // handle all work; nothing more to do here.  If we're not currently processing
                    // but cancellation has been requested, don't do more work either.
                    bool completedAllDesiredBatches = _batchesCompleted >= _dataflowBlockOptions.ActualMaxNumberOfGroups;
                    bool currentlyProcessing = _nonGreedyState != null && _nonGreedyState.TaskForInputProcessing != null;
                    if (completedAllDesiredBatches || currentlyProcessing || CanceledOrFaulted) return false;

                    // Now, if it's possible to create a batch from queued items or if there are enough
                    // postponed items to attempt a batch, batches need processing.
                    int neededMessageCountToCompleteBatch = _batchSize - _messages.Count;
                    int boundedCapacityAvailable = BoundedCapacityAvailable;

                    // We have items queued up sufficient to make up a batch
                    if (neededMessageCountToCompleteBatch <= 0) return true;

                    if (_nonGreedyState != null)
                    {
                        // If a batch was triggered and we have any messages, we can create a batch from what we already have.
                        if (_nonGreedyState.AcceptFewerThanBatchSize && _messages.Count > 0)
                            return true;

                        // At this point, to make a batch we'll need to consume postponed messages, but we can't do
                        // that if we're declining all future messages.
                        if (_decliningPermanently)
                            return false;

                        // If a batch was triggered and there are any postponed messages to retrieve and there's room available, try.
                        if (_nonGreedyState.AcceptFewerThanBatchSize && _nonGreedyState.PostponedMessages.Count > 0 && boundedCapacityAvailable > 0)
                            return true;

                        if (_dataflowBlockOptions.Greedy)
                        {
                            // We are in greedy mode and we have postponed messages.
                            // (In greedy mode we only postpone due to lack of bounding capacity.)
                            // And now we have capacity to consume some postponed messages.
                            // (In greedy mode we can/should consume as many postponed messages as we can even
                            // if those messages are insufficient to make up a batch.)
                            if (_nonGreedyState.PostponedMessages.Count > 0 && boundedCapacityAvailable > 0) return true;
                        }
                        else
                        {
                            // We are in non-greedy mode and we have enough postponed messages and bounding capacity to make a full batch
                            if (_nonGreedyState.PostponedMessages.Count >= neededMessageCountToCompleteBatch &&
                                boundedCapacityAvailable >= neededMessageCountToCompleteBatch)
                                return true;
                        }
                    }

                    // There is no other reason to kick off a processing task
                    return false;
                }
            }

            /// <summary>Called when new messages are available to be processed.</summary>
            /// <param name="isReplacementReplica">Whether this call is the continuation of a previous message loop.</param>
            private void ProcessAsyncIfNecessary(bool isReplacementReplica = false)
            {
                Debug.Assert(_nonGreedyState != null, "Non-greedy state is required for non-greedy mode.");
                Common.ContractAssertMonitorStatus(IncomingLock, held: true);

                if (BatchesNeedProcessing)
                {
                    ProcessAsyncIfNecessary_Slow(isReplacementReplica);
                }
            }

            /// <summary>
            /// Slow path for ProcessAsyncIfNecessary.
            /// Separating out the slow path into its own method makes it more likely that the fast path method will get inlined.
            /// </summary>
            private void ProcessAsyncIfNecessary_Slow(bool isReplacementReplica)
            {
                Debug.Assert(BatchesNeedProcessing, "There must be a batch that needs processing.");

                // Create task and store into _taskForInputProcessing prior to scheduling the task
                // so that _taskForInputProcessing will be visibly set in the task loop.
                _nonGreedyState!.TaskForInputProcessing = new Task(thisBatchTarget => ((BatchBlockTargetCore)thisBatchTarget!).ProcessMessagesLoopCore(), this,
                                                    Common.GetCreationOptionsForTask(isReplacementReplica));

                DataflowEtwProvider etwLog = DataflowEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.TaskLaunchedForMessageHandling(
                        _owningBatch, _nonGreedyState.TaskForInputProcessing, DataflowEtwProvider.TaskLaunchedReason.ProcessingInputMessages,
                        _messages.Count + _nonGreedyState.PostponedMessages.Count);
                }

                // Start the task handling scheduling exceptions
                Exception? exception = Common.StartTaskSafe(_nonGreedyState.TaskForInputProcessing, _dataflowBlockOptions.TaskScheduler);
                if (exception != null)
                {
                    // Get out from under currently held locks. Complete re-acquires the locks it needs.
                    Task.Factory.StartNew(exc => Complete(exception: (Exception)exc!, dropPendingMessages: true, releaseReservedMessages: true, revertProcessingState: true),
                                        exception, CancellationToken.None, Common.GetCreationOptionsForTask(), TaskScheduler.Default);
                }
            }


            /// <summary>Task body used to process messages.</summary>
            private void ProcessMessagesLoopCore()
            {
                Debug.Assert(_nonGreedyState != null, "Non-greedy state is required for non-greedy mode.");
                Common.ContractAssertMonitorStatus(IncomingLock, held: false);
                try
                {
                    int maxMessagesPerTask = _dataflowBlockOptions.ActualMaxMessagesPerTask;
                    int timesThroughLoop = 0;
                    bool madeProgress;
                    do
                    {
                        // Determine whether a batch has been forced/triggered.
                        // (If the value is read as false and is set to true immediately afterwards,
                        // we'll simply force the next time around.  The only code that can
                        // set the value to false is this function, after reading a true value.)
                        bool triggered = Volatile.Read(ref _nonGreedyState.AcceptFewerThanBatchSize);

                        // Retrieve postponed items:
                        //      In non-greedy mode: Reserve + Consume
                        //      In greedy bounded mode: Consume (without a prior reservation)
                        if (!_dataflowBlockOptions.Greedy) RetrievePostponedItemsNonGreedy(allowFewerThanBatchSize: triggered);
                        else RetrievePostponedItemsGreedyBounded(allowFewerThanBatchSize: triggered);

                        // Try to make a batch if there are enough buffered messages
                        lock (IncomingLock)
                        {
                            madeProgress = MakeBatchIfPossible(evenIfFewerThanBatchSize: triggered);

                            // Reset the trigger flag if:
                            // - We made a batch, regardless of whether it came due to a trigger or not.
                            // - We tried to make a batch due to a trigger, but were unable to, which
                            //   could happen if we're unable to consume any of the postponed messages.
                            if (madeProgress || triggered) _nonGreedyState.AcceptFewerThanBatchSize = false;
                        }

                        timesThroughLoop++;
                    } while (madeProgress && timesThroughLoop < maxMessagesPerTask);
                }
                catch (Exception exc)
                {
                    Complete(exc, dropPendingMessages: false, releaseReservedMessages: true);
                }
                finally
                {
                    lock (IncomingLock)
                    {
                        // We're no longer processing, so null out the processing task
                        _nonGreedyState.TaskForInputProcessing = null;

                        // However, we may have given up early because we hit our own configured
                        // processing limits rather than because we ran out of work to do.  If that's
                        // the case, make sure we spin up another task to keep going.
                        ProcessAsyncIfNecessary(isReplacementReplica: true);

                        // If, however, we stopped because we ran out of work to do and we
                        // know we'll never get more, then complete.
                        CompleteBlockIfPossible();
                    }
                }
            }

            /// <summary>Create a batch from the available items.</summary>
            /// <param name="evenIfFewerThanBatchSize">
            /// Whether to make a batch even if there are fewer than BatchSize items available.
            /// </param>
            /// <returns>true if a batch was created and published; otherwise, false.</returns>
            private bool MakeBatchIfPossible(bool evenIfFewerThanBatchSize)
            {
                Common.ContractAssertMonitorStatus(IncomingLock, held: true);

                // Is a full batch available?
                bool fullBatch = _messages.Count >= _batchSize;

                // If so, or if it's ok to make a batch with fewer than batchSize, make one.
                if (fullBatch || (evenIfFewerThanBatchSize && _messages.Count > 0))
                {
                    var newBatch = new T[fullBatch ? _batchSize : _messages.Count];
                    for (int i = 0; i < newBatch.Length; i++) newBatch[i] = _messages.Dequeue();
                    _batchCompletedAction(newBatch);
                    _batchesCompleted++;
                    if (_batchesCompleted >= _dataflowBlockOptions.ActualMaxNumberOfGroups) _decliningPermanently = true;
                    return true;
                }
                // No batch could be created
                else return false;
            }

            /// <summary>Retrieves postponed items in non-greedy mode if we have enough to make a batch.</summary>
            /// <remarks>Whether we'll accept consuming fewer elements than the defined batch size.</remarks>
            private void RetrievePostponedItemsNonGreedy(bool allowFewerThanBatchSize)
            {
                Debug.Assert(!_dataflowBlockOptions.Greedy, "This method may only be used in non-greedy mode.");
                Debug.Assert(_nonGreedyState != null, "Non-greedy state is required for non-greedy mode.");
                Common.ContractAssertMonitorStatus(IncomingLock, held: false);

                // Shortcuts just to keep the code cleaner
                QueuedMap<ISourceBlock<T>, DataflowMessageHeader> postponed = _nonGreedyState.PostponedMessages;
                KeyValuePair<ISourceBlock<T>, DataflowMessageHeader>[] postponedTemp = _nonGreedyState.PostponedMessagesTemp;
                List<KeyValuePair<ISourceBlock<T>, KeyValuePair<DataflowMessageHeader, T>>> reserved = _nonGreedyState.ReservedSourcesTemp;

                // Clear the temporary buffer.  This is safe to do without a lock because
                // it is only accessed by the serial message loop.
                reserved.Clear();

                int poppedInitially;
                int boundedCapacityAvailable;
                lock (IncomingLock)
                {
                    // The queue must be empty between batches in non-greedy mode
                    Debug.Assert(_messages.Count == 0, "The queue must be empty between batches in non-greedy mode");

                    // If there are not enough postponed items (or if we're not allowing consumption), there's nothing more to be done
                    boundedCapacityAvailable = BoundedCapacityAvailable;
                    if (_decliningPermanently ||
                        postponed.Count == 0 ||
                        boundedCapacityAvailable <= 0 ||
                        (!allowFewerThanBatchSize && (postponed.Count < _batchSize || boundedCapacityAvailable < _batchSize)))
                        return;

                    // Grab an initial batch of postponed messages.
                    poppedInitially = postponed.PopRange(postponedTemp, 0, _batchSize);
                    Debug.Assert(allowFewerThanBatchSize ? poppedInitially > 0 : poppedInitially == _batchSize,
                                    "We received fewer than we expected based on the previous check.");
                } // Release the lock.  We must not hold it while calling Reserve/Consume/Release.

                // Try to reserve the initial batch of messages.
                for (int i = 0; i < poppedInitially; i++)
                {
                    KeyValuePair<ISourceBlock<T>, DataflowMessageHeader> sourceAndMessage = postponedTemp[i];
                    if (sourceAndMessage.Key.ReserveMessage(sourceAndMessage.Value, _owningBatch))
                    {
                        var reservedMessage = new KeyValuePair<DataflowMessageHeader, T>(sourceAndMessage.Value, default(T)!);
                        var reservedSourceAndMessage = new KeyValuePair<ISourceBlock<T>, KeyValuePair<DataflowMessageHeader, T>>(sourceAndMessage.Key, reservedMessage);
                        reserved.Add(reservedSourceAndMessage);
                    }
                }
                Array.Clear(postponedTemp, 0, postponedTemp.Length); // clear out the temp array so as not to hold onto messages too long

                // If we didn't reserve enough to make a batch, start picking off postponed messages
                // one by one until we either have enough reserved or we run out of messages
                while (reserved.Count < _batchSize)
                {
                    KeyValuePair<ISourceBlock<T>, DataflowMessageHeader> sourceAndMessage;
                    lock (IncomingLock)
                    {
                        if (!postponed.TryPop(out sourceAndMessage)) break;
                    } // Release the lock.  We must not hold it while calling Reserve/Consume/Release.
                    if (sourceAndMessage.Key.ReserveMessage(sourceAndMessage.Value, _owningBatch))
                    {
                        var reservedMessage = new KeyValuePair<DataflowMessageHeader, T>(sourceAndMessage.Value, default(T)!);
                        var reservedSourceAndMessage = new KeyValuePair<ISourceBlock<T>, KeyValuePair<DataflowMessageHeader, T>>(sourceAndMessage.Key, reservedMessage);
                        reserved.Add(reservedSourceAndMessage);
                    }
                }

                Debug.Assert(reserved.Count <= _batchSize, "Expected the number of reserved sources to be <= the number needed for a batch.");

                // We've now reserved what we can.  Either consume them all or release them all.
                if (reserved.Count > 0)
                {
                    // TriggerBatch adds a complication here.  It's possible that while we've been reserving
                    // messages, Post has been used to queue up a bunch of messages to the batch,
                    // and that if the batch has a max group count and enough messages were posted,
                    // we could now be declining.  In that case, if we don't specially handle the situation,
                    // we could consume messages that we won't be able to turn into a batch, since MaxNumberOfGroups
                    // implies the block will only ever output a maximum number of batches.  To handle this,
                    // we start declining before consuming, now that we know we'll have enough to form a batch.
                    // (If an exception occurs after we do this, we'll be shutting down the block anyway.)
                    // This is also why we still reserve/consume rather than just consume in forced mode,
                    // so that we only consume if we're able to turn what we consume into a batch.
                    bool shouldProceedToConsume = true;
                    if (allowFewerThanBatchSize)
                    {
                        lock (IncomingLock)
                        {
                            if (!_decliningPermanently &&
                                (_batchesCompleted + 1) >= _dataflowBlockOptions.ActualMaxNumberOfGroups)
                            // Note that this logic differs from the other location where we do a similar check.
                            // Here we want to know whether we're one shy of meeting our quota, because we'll accept
                            // any size batch.  Elsewhere, we need to know whether we have the right number of messages
                            // queued up.
                            {
                                shouldProceedToConsume = !_decliningPermanently;
                                _decliningPermanently = true;
                            }
                        }
                    }

                    if (shouldProceedToConsume && (allowFewerThanBatchSize || reserved.Count == _batchSize))
                    {
                        ConsumeReservedMessagesNonGreedy();
                    }
                    else
                    {
                        ReleaseReservedMessages(throwOnFirstException: true);
                    }
                }

                // Clear out the reserved list, so as not to hold onto values longer than necessary.
                // We don't do this in case of failure, because the higher-level exception handler
                // accesses the list to try to release reservations.
                reserved.Clear();
            }

            /// <summary>Retrieves postponed items in greedy bounded mode.</summary>
            /// <remarks>Whether we'll accept consuming fewer elements than the defined batch size.</remarks>
            private void RetrievePostponedItemsGreedyBounded(bool allowFewerThanBatchSize)
            {
                Debug.Assert(_dataflowBlockOptions.Greedy, "This method may only be used in greedy mode.");
                Debug.Assert(_nonGreedyState != null, "Non-greedy state is required for non-greedy mode.");
                Debug.Assert(_boundingState != null, "Bounding state is required when in bounded mode.");
                Common.ContractAssertMonitorStatus(IncomingLock, held: false);

                // Shortcuts just to keep the code cleaner
                QueuedMap<ISourceBlock<T>, DataflowMessageHeader> postponed = _nonGreedyState.PostponedMessages;
                KeyValuePair<ISourceBlock<T>, DataflowMessageHeader>[] postponedTemp = _nonGreedyState.PostponedMessagesTemp;
                List<KeyValuePair<ISourceBlock<T>, KeyValuePair<DataflowMessageHeader, T>>> reserved = _nonGreedyState.ReservedSourcesTemp;

                // Clear the temporary buffer.  This is safe to do without a lock because
                // it is only accessed by the serial message loop.
                reserved.Clear();

                int poppedInitially;
                int boundedCapacityAvailable;
                int itemCountNeededToCompleteBatch;
                lock (IncomingLock)
                {
                    // If there are not enough postponed items (or if we're not allowing consumption), there's nothing more to be done
                    boundedCapacityAvailable = BoundedCapacityAvailable;
                    itemCountNeededToCompleteBatch = _batchSize - _messages.Count;
                    if (_decliningPermanently ||
                        postponed.Count == 0 ||
                        boundedCapacityAvailable <= 0)
                        return;

                    // Grab an initial batch of postponed messages.
                    if (boundedCapacityAvailable < itemCountNeededToCompleteBatch) itemCountNeededToCompleteBatch = boundedCapacityAvailable;
                    poppedInitially = postponed.PopRange(postponedTemp, 0, itemCountNeededToCompleteBatch);
                    Debug.Assert(poppedInitially > 0, "We received fewer than we expected based on the previous check.");
                } // Release the lock.  We must not hold it while calling Reserve/Consume/Release.

                // Treat popped messages as reserved.
                // We don't have to formally reserve because we are in greedy mode.
                for (int i = 0; i < poppedInitially; i++)
                {
                    KeyValuePair<ISourceBlock<T>, DataflowMessageHeader> sourceAndMessage = postponedTemp[i];
                    var reservedMessage = new KeyValuePair<DataflowMessageHeader, T>(sourceAndMessage.Value, default(T)!);
                    var reservedSourceAndMessage = new KeyValuePair<ISourceBlock<T>, KeyValuePair<DataflowMessageHeader, T>>(sourceAndMessage.Key, reservedMessage);
                    reserved.Add(reservedSourceAndMessage);
                }
                Array.Clear(postponedTemp, 0, postponedTemp.Length); // clear out the temp array so as not to hold onto messages too long

                // If we didn't reserve enough to make a batch, start picking off postponed messages
                // one by one until we either have enough reserved or we run out of messages
                while (reserved.Count < itemCountNeededToCompleteBatch)
                {
                    KeyValuePair<ISourceBlock<T>, DataflowMessageHeader> sourceAndMessage;
                    lock (IncomingLock)
                    {
                        if (!postponed.TryPop(out sourceAndMessage)) break;
                    } // Release the lock.  We must not hold it while calling Reserve/Consume/Release.

                    var reservedMessage = new KeyValuePair<DataflowMessageHeader, T>(sourceAndMessage.Value, default(T)!);
                    var reservedSourceAndMessage = new KeyValuePair<ISourceBlock<T>, KeyValuePair<DataflowMessageHeader, T>>(sourceAndMessage.Key, reservedMessage);
                    reserved.Add(reservedSourceAndMessage);
                }

                Debug.Assert(reserved.Count <= itemCountNeededToCompleteBatch, "Expected the number of reserved sources to be <= the number needed for a batch.");

                // We've gotten as many postponed messages as we can. Try to consume them.
                if (reserved.Count > 0)
                {
                    // TriggerBatch adds a complication here.  It's possible that while we've been reserving
                    // messages, Post has been used to queue up a bunch of messages to the batch,
                    // and that if the batch has a max group count and enough messages were posted,
                    // we could now be declining.  In that case, if we don't specially handle the situation,
                    // we could consume messages that we won't be able to turn into a batch, since MaxNumberOfGroups
                    // implies the block will only ever output a maximum number of batches.  To handle this,
                    // we start declining before consuming, now that we know we'll have enough to form a batch.
                    // (If an exception occurs after we do this, we'll be shutting down the block anyway.)
                    // This is also why we still reserve/consume rather than just consume in forced mode,
                    // so that we only consume if we're able to turn what we consume into a batch.
                    bool shouldProceedToConsume = true;
                    if (allowFewerThanBatchSize)
                    {
                        lock (IncomingLock)
                        {
                            if (!_decliningPermanently &&
                                (_batchesCompleted + 1) >= _dataflowBlockOptions.ActualMaxNumberOfGroups)
                            // Note that this logic differs from the other location where we do a similar check.
                            // Here we want to know whether we're one shy of meeting our quota, because we'll accept
                            // any size batch.  Elsewhere, we need to know whether we have the right number of messages
                            // queued up.
                            {
                                shouldProceedToConsume = !_decliningPermanently;
                                _decliningPermanently = true;
                            }
                        }
                    }

                    if (shouldProceedToConsume)
                    {
                        ConsumeReservedMessagesGreedyBounded();
                    }
                }

                // Clear out the reserved list, so as not to hold onto values longer than necessary.
                // We don't do this in case of failure, because the higher-level exception handler
                // accesses the list to try to release reservations.
                reserved.Clear();
            }

            /// <summary>
            /// Consumes all of the reserved messages stored in the non-greedy state's temporary reserved source list.
            /// </summary>
            private void ConsumeReservedMessagesNonGreedy()
            {
                Debug.Assert(!_dataflowBlockOptions.Greedy, "This method may only be used in non-greedy mode.");
                Debug.Assert(_nonGreedyState != null, "Non-greedy state is required for non-greedy mode.");
                Debug.Assert(_nonGreedyState.ReservedSourcesTemp != null, "ReservedSourcesTemp should have been initialized.");
                Common.ContractAssertMonitorStatus(IncomingLock, held: false);

                // Consume the reserved items and store the data.
                List<KeyValuePair<ISourceBlock<T>, KeyValuePair<DataflowMessageHeader, T>>> reserved = _nonGreedyState.ReservedSourcesTemp;
                for (int i = 0; i < reserved.Count; i++)
                {
                    // We can only store the data into _messages while holding the IncomingLock, we
                    // don't want to allocate extra objects for each batch, and we don't want to
                    // take and release the lock for each individual item... but we do need to use
                    // the consumed message rather than the initial one.  To handle this, because KeyValuePair is immutable,
                    // we store a new KVP with the newly consumed message back into the temp list, so that we can
                    // then enumerate the temp list en mass while taking the lock once afterwards.
                    KeyValuePair<ISourceBlock<T>, KeyValuePair<DataflowMessageHeader, T>> sourceAndMessage = reserved[i];
                    reserved[i] = default(KeyValuePair<ISourceBlock<T>, KeyValuePair<DataflowMessageHeader, T>>); // in case of exception from ConsumeMessage
                    bool consumed;
                    T? consumedValue = sourceAndMessage.Key.ConsumeMessage(sourceAndMessage.Value.Key, _owningBatch, out consumed);
                    if (!consumed)
                    {
                        // The protocol broke down, so throw an exception, as this is fatal.  Before doing so, though,
                        // null out all of the messages we've already consumed, as a higher-level event handler
                        // should try to release everything in the reserved list.
                        for (int prev = 0; prev < i; prev++) reserved[prev] = default(KeyValuePair<ISourceBlock<T>, KeyValuePair<DataflowMessageHeader, T>>);
                        throw new InvalidOperationException(SR.InvalidOperation_FailedToConsumeReservedMessage);
                    }

                    var consumedMessage = new KeyValuePair<DataflowMessageHeader, T>(sourceAndMessage.Value.Key, consumedValue!);
                    var consumedSourceAndMessage = new KeyValuePair<ISourceBlock<T>, KeyValuePair<DataflowMessageHeader, T>>(sourceAndMessage.Key, consumedMessage);
                    reserved[i] = consumedSourceAndMessage;
                }
                lock (IncomingLock)
                {
                    // Increment the bounding count with the number of consumed messages
                    if (_boundingState != null) _boundingState.CurrentCount += reserved.Count;

                    // Enqueue the consumed messages
                    foreach (KeyValuePair<ISourceBlock<T>, KeyValuePair<DataflowMessageHeader, T>> sourceAndMessage in reserved)
                    {
                        _messages.Enqueue(sourceAndMessage.Value.Value);
                    }
                }
            }

            /// <summary>
            /// Consumes all of the reserved messages stored in the non-greedy state's temporary reserved source list.
            /// </summary>
            private void ConsumeReservedMessagesGreedyBounded()
            {
                Debug.Assert(_dataflowBlockOptions.Greedy, "This method may only be used in greedy mode.");
                Debug.Assert(_nonGreedyState != null, "Non-greedy state is required for non-greedy mode.");
                Debug.Assert(_nonGreedyState.ReservedSourcesTemp != null, "ReservedSourcesTemp should have been initialized.");
                Debug.Assert(_boundingState != null, "Bounded state is required for bounded mode.");
                Common.ContractAssertMonitorStatus(IncomingLock, held: false);

                // Consume the reserved items and store the data.
                int consumedCount = 0;
                List<KeyValuePair<ISourceBlock<T>, KeyValuePair<DataflowMessageHeader, T>>> reserved = _nonGreedyState.ReservedSourcesTemp;
                for (int i = 0; i < reserved.Count; i++)
                {
                    // We can only store the data into _messages while holding the IncomingLock, we
                    // don't want to allocate extra objects for each batch, and we don't want to
                    // take and release the lock for each individual item... but we do need to use
                    // the consumed message rather than the initial one.  To handle this, because KeyValuePair is immutable,
                    // we store a new KVP with the newly consumed message back into the temp list, so that we can
                    // then enumerate the temp list en mass while taking the lock once afterwards.
                    KeyValuePair<ISourceBlock<T>, KeyValuePair<DataflowMessageHeader, T>> sourceAndMessage = reserved[i];
                    reserved[i] = default(KeyValuePair<ISourceBlock<T>, KeyValuePair<DataflowMessageHeader, T>>); // in case of exception from ConsumeMessage
                    bool consumed;
                    T? consumedValue = sourceAndMessage.Key.ConsumeMessage(sourceAndMessage.Value.Key, _owningBatch, out consumed);
                    if (consumed)
                    {
                        var consumedMessage = new KeyValuePair<DataflowMessageHeader, T>(sourceAndMessage.Value.Key, consumedValue!);
                        var consumedSourceAndMessage = new KeyValuePair<ISourceBlock<T>, KeyValuePair<DataflowMessageHeader, T>>(sourceAndMessage.Key, consumedMessage);
                        reserved[i] = consumedSourceAndMessage;

                        // Keep track of the actually consumed messages
                        consumedCount++;
                    }
                }
                lock (IncomingLock)
                {
                    // Increment the bounding count with the number of consumed messages
                    if (_boundingState != null) _boundingState.CurrentCount += consumedCount;

                    // Enqueue the consumed messages
                    foreach (KeyValuePair<ISourceBlock<T>, KeyValuePair<DataflowMessageHeader, T>> sourceAndMessage in reserved)
                    {
                        // If we didn't consume this message, the KeyValuePai will be default, i.e. the source will be null
                        if (sourceAndMessage.Key != null) _messages.Enqueue(sourceAndMessage.Value.Value);
                    }
                }
            }

            /// <summary>
            /// Releases all of the reserved messages stored in the non-greedy state's temporary reserved source list.
            /// </summary>
            /// <param name="throwOnFirstException">
            /// Whether to allow an exception from a release to propagate immediately,
            /// or to delay propagation until all releases have been attempted.
            /// </param>
            internal void ReleaseReservedMessages(bool throwOnFirstException)
            {
                Common.ContractAssertMonitorStatus(IncomingLock, held: false);
                Debug.Assert(_nonGreedyState != null, "Non-greedy state is required for non-greedy mode.");
                Debug.Assert(_nonGreedyState.ReservedSourcesTemp != null, "Should have been initialized");

                List<Exception>? exceptions = null;

                List<KeyValuePair<ISourceBlock<T>, KeyValuePair<DataflowMessageHeader, T>>> reserved = _nonGreedyState.ReservedSourcesTemp;
                for (int i = 0; i < reserved.Count; i++)
                {
                    KeyValuePair<ISourceBlock<T>, KeyValuePair<DataflowMessageHeader, T>> sourceAndMessage = reserved[i];
                    reserved[i] = default(KeyValuePair<ISourceBlock<T>, KeyValuePair<DataflowMessageHeader, T>>);
                    ISourceBlock<T> source = sourceAndMessage.Key;
                    KeyValuePair<DataflowMessageHeader, T> message = sourceAndMessage.Value;
                    if (source != null && message.Key.IsValid)
                    {
                        try { source.ReleaseReservation(message.Key, _owningBatch); }
                        catch (Exception e)
                        {
                            if (throwOnFirstException) throw;
                            if (exceptions == null) exceptions = new List<Exception>(1);
                            exceptions.Add(e);
                        }
                    }
                }

                if (exceptions != null) throw new AggregateException(exceptions);
            }

            /// <summary>Notifies the block that one or more items was removed from the queue.</summary>
            /// <param name="numItemsRemoved">The number of items removed.</param>
            internal void OnItemsRemoved(int numItemsRemoved)
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

                        ProcessAsyncIfNecessary();
                        CompleteBlockIfPossible();
                    }
                }
            }

            /// <summary>Counts the input items in a single output item or in a list of output items.</summary>
            /// <param name="singleOutputItem">A single output item. Only considered if multipleOutputItems == null.</param>
            /// <param name="multipleOutputItems">A list of output items. May be null.</param>
            internal static int CountItems(T[] singleOutputItem, IList<T[]>? multipleOutputItems)
            {
                // If multipleOutputItems == null, then singleOutputItem is the subject of counting
                if (multipleOutputItems == null) return singleOutputItem.Length;

                // multipleOutputItems != null. Count the elements in each item.
                int count = 0;
                foreach (T[] item in multipleOutputItems) count += item.Length;
                return count;
            }

            /// <summary>Gets the number of messages waiting to be processed.  This must only be used from the debugger as it avoids taking necessary locks.</summary>
            private int InputCountForDebugger { get { return _messages.Count; } }

            /// <summary>Gets information about this helper to be used for display in a debugger.</summary>
            /// <returns>Debugging information about this target.</returns>
            internal DebuggingInformation GetDebuggingInformation() { return new DebuggingInformation(this); }

            /// <summary>Gets the object to display in the debugger display attribute.</summary>
            private object DebuggerDisplayContent
            {
                get
                {
                    var displayBatch = _owningBatch as IDebuggerDisplay;
                    return $"Block=\"{(displayBatch != null ? displayBatch.Content : _owningBatch)}\"";
                }
            }

            /// <summary>Provides a wrapper for commonly needed debugging information.</summary>
            internal sealed class DebuggingInformation
            {
                /// <summary>The target being viewed.</summary>
                private readonly BatchBlockTargetCore _target;

                /// <summary>Initializes the debugging helper.</summary>
                /// <param name="target">The target being viewed.</param>
                public DebuggingInformation(BatchBlockTargetCore target) { _target = target; }

                /// <summary>Gets the messages waiting to be processed.</summary>
                public IEnumerable<T> InputQueue { get { return _target._messages.ToList(); } }
                /// <summary>Gets the task being used for input processing.</summary>
                public Task? TaskForInputProcessing { get { return _target._nonGreedyState != null ? _target._nonGreedyState.TaskForInputProcessing : null; } }
                /// <summary>Gets the collection of postponed messages.</summary>
                public QueuedMap<ISourceBlock<T>, DataflowMessageHeader>? PostponedMessages { get { return _target._nonGreedyState != null ? _target._nonGreedyState.PostponedMessages : null; } }
                /// <summary>Gets whether the block is declining further messages.</summary>
                public bool IsDecliningPermanently { get { return _target._decliningPermanently; } }
                /// <summary>Gets the DataflowBlockOptions used to configure this block.</summary>
                public GroupingDataflowBlockOptions DataflowBlockOptions { get { return _target._dataflowBlockOptions; } }
                /// <summary>Gets the number of batches that have been completed.</summary>
                public long NumberOfBatchesCompleted { get { return _target._batchesCompleted; } }
            }
        }
    }
}
