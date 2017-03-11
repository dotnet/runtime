// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// BufferBlock.cs
//
//
// A propagator block that provides support for unbounded and bounded FIFO buffers.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Security;
using System.Threading.Tasks.Dataflow.Internal;
using System.Diagnostics.CodeAnalysis;

namespace System.Threading.Tasks.Dataflow
{
    /// <summary>Provides a buffer for storing data.</summary>
    /// <typeparam name="T">Specifies the type of the data buffered by this dataflow block.</typeparam>
    [DebuggerDisplay("{DebuggerDisplayContent,nq}")]
    [DebuggerTypeProxy(typeof(BufferBlock<>.DebugView))]
    public sealed class BufferBlock<T> : IPropagatorBlock<T, T>, IReceivableSourceBlock<T>, IDebuggerDisplay
    {
        /// <summary>The core logic for the buffer block.</summary>
        private readonly SourceCore<T> _source;
        /// <summary>The bounding state for when in bounding mode; null if not bounding.</summary>
        private readonly BoundingStateWithPostponedAndTask<T> _boundingState;
        /// <summary>Whether all future messages should be declined on the target.</summary>
        private bool _targetDecliningPermanently;
        /// <summary>A task has reserved the right to run the target's completion routine.</summary>
        private bool _targetCompletionReserved;
        /// <summary>Gets the lock object used to synchronize incoming requests.</summary>
        private object IncomingLock { get { return _source; } }

        /// <summary>Initializes the <see cref="BufferBlock{T}"/>.</summary>
        public BufferBlock() :
            this(DataflowBlockOptions.Default)
        { }

        /// <summary>Initializes the <see cref="BufferBlock{T}"/> with the specified <see cref="DataflowBlockOptions"/>.</summary>
        /// <param name="dataflowBlockOptions">The options with which to configure this <see cref="BufferBlock{T}"/>.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="dataflowBlockOptions"/> is null (Nothing in Visual Basic).</exception>
        public BufferBlock(DataflowBlockOptions dataflowBlockOptions)
        {
            if (dataflowBlockOptions == null) throw new ArgumentNullException(nameof(dataflowBlockOptions));
            Contract.EndContractBlock();

            // Ensure we have options that can't be changed by the caller
            dataflowBlockOptions = dataflowBlockOptions.DefaultOrClone();

            // Initialize bounding state if necessary
            Action<ISourceBlock<T>, int> onItemsRemoved = null;
            if (dataflowBlockOptions.BoundedCapacity > 0)
            {
                onItemsRemoved = (owningSource, count) => ((BufferBlock<T>)owningSource).OnItemsRemoved(count);
                _boundingState = new BoundingStateWithPostponedAndTask<T>(dataflowBlockOptions.BoundedCapacity);
            }

            // Initialize the source state
            _source = new SourceCore<T>(this, dataflowBlockOptions,
                owningSource => ((BufferBlock<T>)owningSource).Complete(),
                onItemsRemoved);

            // It is possible that the source half may fault on its own, e.g. due to a task scheduler exception.
            // In those cases we need to fault the target half to drop its buffered messages and to release its 
            // reservations. This should not create an infinite loop, because all our implementations are designed
            // to handle multiple completion requests and to carry over only one.
            _source.Completion.ContinueWith((completed, state) =>
            {
                var thisBlock = ((BufferBlock<T>)state) as IDataflowBlock;
                Debug.Assert(completed.IsFaulted, "The source must be faulted in order to trigger a target completion.");
                thisBlock.Fault(completed.Exception);
            }, this, CancellationToken.None, Common.GetContinuationOptions() | TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);

            // Handle async cancellation requests by declining on the target
            Common.WireCancellationToComplete(
                dataflowBlockOptions.CancellationToken, _source.Completion, owningSource => ((BufferBlock<T>)owningSource).Complete(), this);
#if FEATURE_TRACING
            DataflowEtwProvider etwLog = DataflowEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.DataflowBlockCreated(this, dataflowBlockOptions);
            }
#endif
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Targets/Member[@name="OfferMessage"]/*' />
        DataflowMessageStatus ITargetBlock<T>.OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T> source, Boolean consumeToAccept)
        {
            // Validate arguments
            if (!messageHeader.IsValid) throw new ArgumentException(SR.Argument_InvalidMessageHeader, nameof(messageHeader));
            if (source == null && consumeToAccept) throw new ArgumentException(SR.Argument_CantConsumeFromANullSource, nameof(consumeToAccept));
            Contract.EndContractBlock();

            lock (IncomingLock)
            {
                // If we've already stopped accepting messages, decline permanently
                if (_targetDecliningPermanently)
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
                        messageValue = source.ConsumeMessage(messageHeader, this, out consumed);
                        if (!consumed) return DataflowMessageStatus.NotAvailable;
                    }

                    // Once consumed, pass it to the source
                    _source.AddMessage(messageValue);
                    if (_boundingState != null) _boundingState.CurrentCount++;

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

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Complete"]/*' />
        public void Complete() { CompleteCore(exception: null, storeExceptionEvenIfAlreadyCompleting: false); }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Fault"]/*' />
        void IDataflowBlock.Fault(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));
            Contract.EndContractBlock();

            CompleteCore(exception, storeExceptionEvenIfAlreadyCompleting: false);
        }

        private void CompleteCore(Exception exception, bool storeExceptionEvenIfAlreadyCompleting, bool revertProcessingState = false)
        {
            Debug.Assert(storeExceptionEvenIfAlreadyCompleting || !revertProcessingState,
                            "Indicating dirty processing state may only come with storeExceptionEvenIfAlreadyCompleting==true.");
            Contract.EndContractBlock();

            lock (IncomingLock)
            {
                // Faulting from outside is allowed until we start declining permanently.
                // Faulting from inside is allowed at any time.
                if (exception != null && (!_targetDecliningPermanently || storeExceptionEvenIfAlreadyCompleting))
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

                // Trigger completion
                _targetDecliningPermanently = true;
                CompleteTargetIfPossible();
            }
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="LinkTo"]/*' />
        public IDisposable LinkTo(ITargetBlock<T> target, DataflowLinkOptions linkOptions) { return _source.LinkTo(target, linkOptions); }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="TryReceive"]/*' />
        public Boolean TryReceive(Predicate<T> filter, out T item) { return _source.TryReceive(filter, out item); }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="TryReceiveAll"]/*' />
        public Boolean TryReceiveAll(out IList<T> items) { return _source.TryReceiveAll(out items); }

        /// <summary>Gets the number of items currently stored in the buffer.</summary>
        public Int32 Count { get { return _source.OutputCount; } }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Completion"]/*' />
        public Task Completion { get { return _source.Completion; } }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="ConsumeMessage"]/*' />
        T ISourceBlock<T>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<T> target, out Boolean messageConsumed)
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

        /// <summary>Notifies the block that one or more items was removed from the queue.</summary>
        /// <param name="numItemsRemoved">The number of items removed.</param>
        private void OnItemsRemoved(int numItemsRemoved)
        {
            Debug.Assert(numItemsRemoved > 0, "A positive number of items to remove is required.");
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

            if (!_targetDecliningPermanently &&
                _boundingState.TaskForInputProcessing == null &&
                _boundingState.PostponedMessages.Count > 0 &&
                _boundingState.CountIsLessThanBound)
            {
                // Create task and store into _taskForInputProcessing prior to scheduling the task
                // so that _taskForInputProcessing will be visibly set in the task loop.
                _boundingState.TaskForInputProcessing =
                    new Task(state => ((BufferBlock<T>)state).ConsumeMessagesLoopCore(), this,
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
                Exception exception = Common.StartTaskSafe(_boundingState.TaskForInputProcessing, _source.DataflowBlockOptions.TaskScheduler);
                if (exception != null)
                {
                    // Get out from under currently held locks. CompleteCore re-acquires the locks it needs.
                    Task.Factory.StartNew(exc => CompleteCore(exception: (Exception)exc, storeExceptionEvenIfAlreadyCompleting: true, revertProcessingState: true),
                                        exception, CancellationToken.None, Common.GetCreationOptionsForTask(), TaskScheduler.Default);
                }
            }
        }


        /// <summary>Task body used to consume postponed messages.</summary>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
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
            catch (Exception exc)
            {
                // Prevent the creation of new processing tasks
                CompleteCore(exc, storeExceptionEvenIfAlreadyCompleting: true);
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
                    if (_targetDecliningPermanently) return false;
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
                    T consumedValue = sourceAndMessage.Key.ConsumeMessage(sourceAndMessage.Value, this, out consumed);
                    if (consumed)
                    {
                        _source.AddMessage(consumedValue);
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
            if (_targetDecliningPermanently &&
                !_targetCompletionReserved &&
                (_boundingState == null || _boundingState.TaskForInputProcessing == null))
            {
                _targetCompletionReserved = true;

                // If we're in bounding mode and we have any postponed messages, we need to clear them,
                // which means calling back to the source, which means we need to escape the incoming lock.
                if (_boundingState != null && _boundingState.PostponedMessages.Count > 0)
                {
                    Task.Factory.StartNew(state =>
                    {
                        var thisBufferBlock = (BufferBlock<T>)state;

                        // Release any postponed messages
                        List<Exception> exceptions = null;
                        if (thisBufferBlock._boundingState != null)
                        {
                            // Note: No locks should be held at this point
                            Common.ReleaseAllPostponedMessages(thisBufferBlock,
                                                               thisBufferBlock._boundingState.PostponedMessages,
                                                               ref exceptions);
                        }

                        if (exceptions != null)
                        {
                            // It is important to migrate these exceptions to the source part of the owning batch,
                            // because that is the completion task that is publicly exposed.
                            thisBufferBlock._source.AddExceptions(exceptions);
                        }

                        thisBufferBlock._source.Complete();
                    }, this, CancellationToken.None, Common.GetCreationOptionsForTask(), TaskScheduler.Default);
                }
                // Otherwise, we can just decline the source directly.
                else
                {
                    _source.Complete();
                }
            }
        }

        /// <summary>Gets the number of messages in the buffer.  This must only be used from the debugger as it avoids taking necessary locks.</summary>
        private int CountForDebugger { get { return _source.GetDebuggingInformation().OutputCount; } }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="ToString"]/*' />
        public override string ToString() { return Common.GetNameForDebugger(this, _source.DataflowBlockOptions); }

        /// <summary>The data to display in the debugger display attribute.</summary>
        [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider")]
        private object DebuggerDisplayContent
        {
            get
            {
                return string.Format("{0}, Count={1}",
                    Common.GetNameForDebugger(this, _source.DataflowBlockOptions),
                    CountForDebugger);
            }
        }
        /// <summary>Gets the data to display in the debugger display attribute for this instance.</summary>
        object IDebuggerDisplay.Content { get { return DebuggerDisplayContent; } }

        /// <summary>Provides a debugger type proxy for the BufferBlock.</summary>
        private sealed class DebugView
        {
            /// <summary>The buffer block.</summary>
            private readonly BufferBlock<T> _bufferBlock;
            /// <summary>The buffer's source half.</summary>
            private readonly SourceCore<T>.DebuggingInformation _sourceDebuggingInformation;

            /// <summary>Initializes the debug view.</summary>
            /// <param name="bufferBlock">The BufferBlock being viewed.</param>
            public DebugView(BufferBlock<T> bufferBlock)
            {
                Debug.Assert(bufferBlock != null, "Need a block with which to construct the debug view.");
                _bufferBlock = bufferBlock;
                _sourceDebuggingInformation = bufferBlock._source.GetDebuggingInformation();
            }

            /// <summary>Gets the collection of postponed message headers.</summary>
            public QueuedMap<ISourceBlock<T>, DataflowMessageHeader> PostponedMessages
            {
                get { return _bufferBlock._boundingState != null ? _bufferBlock._boundingState.PostponedMessages : null; }
            }
            /// <summary>Gets the messages in the buffer.</summary>
            public IEnumerable<T> Queue { get { return _sourceDebuggingInformation.OutputQueue; } }

            /// <summary>The task used to process messages.</summary>
            public Task TaskForInputProcessing { get { return _bufferBlock._boundingState != null ? _bufferBlock._boundingState.TaskForInputProcessing : null; } }
            /// <summary>Gets the task being used for output processing.</summary>
            public Task TaskForOutputProcessing { get { return _sourceDebuggingInformation.TaskForOutputProcessing; } }

            /// <summary>Gets the DataflowBlockOptions used to configure this block.</summary>
            public DataflowBlockOptions DataflowBlockOptions { get { return _sourceDebuggingInformation.DataflowBlockOptions; } }

            /// <summary>Gets whether the block is declining further messages.</summary>
            public bool IsDecliningPermanently { get { return _bufferBlock._targetDecliningPermanently; } }
            /// <summary>Gets whether the block is completed.</summary>
            public bool IsCompleted { get { return _sourceDebuggingInformation.IsCompleted; } }
            /// <summary>Gets the block's Id.</summary>
            public int Id { get { return Common.GetBlockId(_bufferBlock); } }

            /// <summary>Gets the set of all targets linked from this block.</summary>
            public TargetRegistry<T> LinkedTargets { get { return _sourceDebuggingInformation.LinkedTargets; } }
            /// <summary>Gets the set of all targets linked from this block.</summary>
            public ITargetBlock<T> NextMessageReservedFor { get { return _sourceDebuggingInformation.NextMessageReservedFor; } }
        }
    }
}
