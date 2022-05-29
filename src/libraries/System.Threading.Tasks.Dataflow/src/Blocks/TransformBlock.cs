// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// TransformBlock.cs
//
//
// A propagator block that runs a function on each input to produce a single output.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow.Internal;
using System.Diagnostics.CodeAnalysis;

namespace System.Threading.Tasks.Dataflow
{
    /// <summary>Provides a dataflow block that invokes a provided <see cref="System.Func{TInput,TOutput}"/> delegate for every data element received.</summary>
    /// <typeparam name="TInput">Specifies the type of data received and operated on by this <see cref="TransformBlock{TInput,TOutput}"/>.</typeparam>
    /// <typeparam name="TOutput">Specifies the type of data output by this <see cref="TransformBlock{TInput,TOutput}"/>.</typeparam>
    [DebuggerDisplay("{DebuggerDisplayContent,nq}")]
    [DebuggerTypeProxy(typeof(TransformBlock<,>.DebugView))]
    public sealed class TransformBlock<TInput, TOutput> : IPropagatorBlock<TInput, TOutput>, IReceivableSourceBlock<TOutput>, IDebuggerDisplay
    {
        /// <summary>The target side.</summary>
        private readonly TargetCore<TInput> _target;
        /// <summary>Buffer used to reorder outputs that may have completed out-of-order between the target half and the source half.</summary>
        private readonly ReorderingBuffer<TOutput>? _reorderingBuffer;
        /// <summary>The source side.</summary>
        private readonly SourceCore<TOutput> _source;

        /// <summary>Gets the object to use for writing to the source when multiple threads may be involved.</summary>
        /// <remarks>
        /// If a reordering buffer is used, it is safe for multiple threads to write to concurrently and handles safe
        /// access to the source. If there's no reordering buffer because no parallelism is used, then only one thread at
        /// a time will try to access the source, anyway.  But, if there's no reordering buffer and parallelism is being
        /// employed, then multiple threads may try to access the source concurrently, in which case we need to manually
        /// synchronize all such access, and this lock is used for that purpose.
        /// </remarks>
        private object ParallelSourceLock { get { return _source; } }

        /// <summary>Initializes the <see cref="TransformBlock{TInput,TOutput}"/> with the specified <see cref="System.Func{TInput,TOutput}"/>.</summary>
        /// <param name="transform">The function to invoke with each data element received.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="transform"/> is null (Nothing in Visual Basic).</exception>
        public TransformBlock(Func<TInput, TOutput> transform) :
            this(transform, null, ExecutionDataflowBlockOptions.Default)
        { }

        /// <summary>
        /// Initializes the <see cref="TransformBlock{TInput,TOutput}"/> with the specified <see cref="System.Func{TInput,TOutput}"/> and
        /// <see cref="ExecutionDataflowBlockOptions"/>.
        /// </summary>
        /// <param name="transform">The function to invoke with each data element received.</param>
        /// <param name="dataflowBlockOptions">The options with which to configure this <see cref="TransformBlock{TInput,TOutput}"/>.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="transform"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="dataflowBlockOptions"/> is null (Nothing in Visual Basic).</exception>
        public TransformBlock(Func<TInput, TOutput> transform, ExecutionDataflowBlockOptions dataflowBlockOptions) :
            this(transform, null, dataflowBlockOptions)
        { }

        /// <summary>Initializes the <see cref="TransformBlock{TInput,TOutput}"/> with the specified <see cref="System.Func{TInput,TOutput}"/>.</summary>
        /// <param name="transform">The function to invoke with each data element received.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="transform"/> is null (Nothing in Visual Basic).</exception>
        public TransformBlock(Func<TInput, Task<TOutput>> transform) :
            this(null, transform, ExecutionDataflowBlockOptions.Default)
        { }

        /// <summary>
        /// Initializes the <see cref="TransformBlock{TInput,TOutput}"/> with the specified <see cref="System.Func{TInput,TOutput}"/>
        /// and <see cref="ExecutionDataflowBlockOptions"/>.
        /// </summary>
        /// <param name="transform">The function to invoke with each data element received.</param>
        /// <param name="dataflowBlockOptions">The options with which to configure this <see cref="TransformBlock{TInput,TOutput}"/>.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="transform"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="dataflowBlockOptions"/> is null (Nothing in Visual Basic).</exception>
        public TransformBlock(Func<TInput, Task<TOutput>> transform, ExecutionDataflowBlockOptions dataflowBlockOptions) :
            this(null, transform, dataflowBlockOptions)
        { }

        /// <summary>
        /// Initializes the <see cref="TransformBlock{TInput,TOutput}"/> with the specified <see cref="System.Func{TInput,TOutput}"/>
        /// and <see cref="DataflowBlockOptions"/>.
        /// </summary>
        /// <param name="transformSync">The synchronous function to invoke with each data element received.</param>
        /// <param name="transformAsync">The asynchronous function to invoke with each data element received.</param>
        /// <param name="dataflowBlockOptions">The options with which to configure this <see cref="TransformBlock{TInput,TOutput}"/>.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="transformSync"/> and <paramref name="transformAsync"/> are both null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="dataflowBlockOptions"/> is null (Nothing in Visual Basic).</exception>
        private TransformBlock(Func<TInput, TOutput>? transformSync, Func<TInput, Task<TOutput>>? transformAsync, ExecutionDataflowBlockOptions dataflowBlockOptions)
        {
            if (dataflowBlockOptions is null)
            {
                throw new ArgumentNullException(nameof(dataflowBlockOptions));
            }

            if (transformSync == null && transformAsync == null) throw new ArgumentNullException("transform");
            if (dataflowBlockOptions == null) throw new ArgumentNullException(nameof(dataflowBlockOptions));

            Debug.Assert(transformSync == null ^ transformAsync == null, "Exactly one of transformSync and transformAsync must be null.");

            // Ensure we have options that can't be changed by the caller
            dataflowBlockOptions = dataflowBlockOptions.DefaultOrClone();

            // Initialize onItemsRemoved delegate if necessary
            Action<ISourceBlock<TOutput>, int>? onItemsRemoved = null;
            if (dataflowBlockOptions.BoundedCapacity > 0)
                onItemsRemoved = (owningSource, count) => ((TransformBlock<TInput, TOutput>)owningSource)._target.ChangeBoundingCount(-count);

            // Initialize source component.
            _source = new SourceCore<TOutput>(this, dataflowBlockOptions,
                owningSource => ((TransformBlock<TInput, TOutput>)owningSource)._target.Complete(exception: null, dropPendingMessages: true),
                onItemsRemoved);

            // If parallelism is employed, we will need to support reordering messages that complete out-of-order.
            // However, a developer can override this with EnsureOrdered == false.
            if (dataflowBlockOptions.SupportsParallelExecution && dataflowBlockOptions.EnsureOrdered)
            {
                _reorderingBuffer = new ReorderingBuffer<TOutput>(this, (owningSource, message) => ((TransformBlock<TInput, TOutput>)owningSource)._source.AddMessage(message));
            }

            // Create the underlying target
            if (transformSync != null) // sync
            {
                _target = new TargetCore<TInput>(this,
                    messageWithId => ProcessMessage(transformSync, messageWithId),
                    _reorderingBuffer, dataflowBlockOptions, TargetCoreOptions.None);
            }
            else // async
            {
                Debug.Assert(transformAsync != null, "Incorrect delegate type.");
                _target = new TargetCore<TInput>(this,
                    messageWithId => ProcessMessageWithTask(transformAsync, messageWithId),
                    _reorderingBuffer, dataflowBlockOptions, TargetCoreOptions.UsesAsyncCompletion);
            }

            // Link up the target half with the source half.  In doing so,
            // ensure exceptions are propagated, and let the source know no more messages will arrive.
            // As the target has completed, and as the target synchronously pushes work
            // through the reordering buffer when async processing completes,
            // we know for certain that no more messages will need to be sent to the source.
            _target.Completion.ContinueWith((completed, state) =>
            {
                var sourceCore = (SourceCore<TOutput>)state!;
                if (completed.IsFaulted) sourceCore.AddAndUnwrapAggregateException(completed.Exception!);
                sourceCore.Complete();
            }, _source, CancellationToken.None, Common.GetContinuationOptions(), TaskScheduler.Default);

            // It is possible that the source half may fault on its own, e.g. due to a task scheduler exception.
            // In those cases we need to fault the target half to drop its buffered messages and to release its
            // reservations. This should not create an infinite loop, because all our implementations are designed
            // to handle multiple completion requests and to carry over only one.
            _source.Completion.ContinueWith((completed, state) =>
            {
                var thisBlock = ((TransformBlock<TInput, TOutput>)state!) as IDataflowBlock;
                Debug.Assert(completed.IsFaulted, "The source must be faulted in order to trigger a target completion.");
                thisBlock.Fault(completed.Exception!);
            }, this, CancellationToken.None, Common.GetContinuationOptions() | TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);

            // Handle async cancellation requests by declining on the target
            Common.WireCancellationToComplete(
                dataflowBlockOptions.CancellationToken, Completion, state => ((TargetCore<TInput>)state!).Complete(exception: null, dropPendingMessages: true), _target);
            DataflowEtwProvider etwLog = DataflowEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.DataflowBlockCreated(this, dataflowBlockOptions);
            }
        }

        /// <summary>Processes the message with a user-provided transform function that returns a TOutput.</summary>
        /// <param name="transform">The transform function to use to process the message.</param>
        /// <param name="messageWithId">The message to be processed.</param>
        private void ProcessMessage(Func<TInput, TOutput> transform, KeyValuePair<TInput, long> messageWithId)
        {
            // Process the input message to get the output message
            TOutput? outputItem = default(TOutput);
            bool itemIsValid = false;
            try
            {
                outputItem = transform(messageWithId.Key);
                itemIsValid = true;
            }
            catch (Exception exc)
            {
                // If this exception represents cancellation, swallow it rather than shutting down the block.
                if (!Common.IsCooperativeCancellation(exc)) throw;
            }
            finally
            {
                // If we were not successful in producing an item, update the bounding
                // count to reflect that we're done with this input item.
                if (!itemIsValid) _target.ChangeBoundingCount(-1);

                // If there's no reordering buffer (because we're running sequentially or ordering was disabled),
                // simply pass the output message through. Otherwise, there's a reordering buffer,
                // so add to it instead (if a reordering buffer is used, we always need
                // to output the message to it, even if the operation failed and outputMessage
                // is null... this is because the reordering buffer cares about a strict sequence
                // of IDs, and it needs to know when a particular ID has completed. It will eliminate
                // null messages accordingly.)
                if (_reorderingBuffer == null)
                {
                    if (itemIsValid)
                    {
                        if (_target.DataflowBlockOptions.MaxDegreeOfParallelism == 1)
                        {
                            _source.AddMessage(outputItem!);
                        }
                        else
                        {
                            lock (ParallelSourceLock)
                            {
                                _source.AddMessage(outputItem!);
                            }
                        }
                    }
                }
                else _reorderingBuffer.AddItem(messageWithId.Value, outputItem!, itemIsValid);
            }
        }

        /// <summary>Processes the message with a user-provided transform function that returns a task of TOutput.</summary>
        /// <param name="transform">The transform function to use to process the message.</param>
        /// <param name="messageWithId">The message to be processed.</param>
        private void ProcessMessageWithTask(Func<TInput, Task<TOutput>> transform, KeyValuePair<TInput, long> messageWithId)
        {
            Debug.Assert(transform != null, "Function to invoke is required.");

            // Run the transform function to get the task that represents the operation's completion
            Task<TOutput>? task = null;
            Exception? caughtException = null;
            try
            {
                task = transform(messageWithId.Key);
            }
            catch (Exception exc) { caughtException = exc; }

            // If no task is available, we're done.
            if (task == null)
            {
                // If we didn't get a task because an exception occurred,
                // store it (if the exception was cancellation, just ignore it).
                if (caughtException != null && !Common.IsCooperativeCancellation(caughtException))
                {
                    Common.StoreDataflowMessageValueIntoExceptionData(caughtException, messageWithId.Key);
                    _target.Complete(caughtException, dropPendingMessages: true, storeExceptionEvenIfAlreadyCompleting: true, unwrapInnerExceptions: false);
                }

                // If there's a reordering buffer, notify it that this message is done.
                if (_reorderingBuffer != null) _reorderingBuffer.IgnoreItem(messageWithId.Value);

                // Signal that we're done this async operation, and remove the bounding
                // count for the input item that didn't yield any output.
                _target.SignalOneAsyncMessageCompleted(boundingCountChange: -1);
                return;
            }

            // Otherwise, join with the asynchronous operation when it completes.
            task.ContinueWith((completed, state) =>
            {
                var tuple = (Tuple<TransformBlock<TInput, TOutput>, KeyValuePair<TInput, long>>)state!;
                tuple.Item1.AsyncCompleteProcessMessageWithTask(completed, tuple.Item2);
            }, Tuple.Create(this, messageWithId), CancellationToken.None,
            Common.GetContinuationOptions(TaskContinuationOptions.ExecuteSynchronously), TaskScheduler.Default);
        }

        /// <summary>Completes the processing of an asynchronous message.</summary>
        /// <param name="completed">The completed task storing the output data generated for an input message.</param>
        /// <param name="messageWithId">The originating message</param>
        private void AsyncCompleteProcessMessageWithTask(Task<TOutput> completed, KeyValuePair<TInput, long> messageWithId)
        {
            Debug.Assert(completed != null, "Completed task is required.");
            Debug.Assert(completed.IsCompleted, "Task must be completed to be here.");

            bool isBounded = _target.IsBounded;
            bool gotOutputItem = false;
            TOutput? outputItem = default(TOutput);

            switch (completed.Status)
            {
                case TaskStatus.RanToCompletion:
                    outputItem = completed.Result;
                    gotOutputItem = true;
                    break;

                case TaskStatus.Faulted:
                    // We must add the exception before declining and signaling completion, as the exception
                    // is part of the operation, and the completion conditions depend on this.
                    AggregateException aggregate = completed.Exception!;
                    Common.StoreDataflowMessageValueIntoExceptionData(aggregate, messageWithId.Key, targetInnerExceptions: true);
                    _target.Complete(aggregate, dropPendingMessages: true, storeExceptionEvenIfAlreadyCompleting: true, unwrapInnerExceptions: true);
                    break;
                    // Nothing special to do for cancellation
            }

            // Adjust the bounding count if necessary (we only need to decrement it for faulting
            // and cancellation, since in the case of success we still have an item that's now in the output buffer).
            // Even though this is more costly (again, only in the non-success case, we do this before we store the
            // message, so that if there's a race to remove the element from the source buffer, the count is
            // appropriately incremented before it's decremented.
            if (!gotOutputItem && isBounded) _target.ChangeBoundingCount(-1);

            // If there's no reordering buffer (because we're running sequentially or ordering is disabled),
            // and we got a message, simply pass the output message through.
            if (_reorderingBuffer == null)
            {
                if (gotOutputItem)
                {
                    if (_target.DataflowBlockOptions.MaxDegreeOfParallelism == 1)
                    {
                        _source.AddMessage(outputItem!);
                    }
                    else
                    {
                        lock (ParallelSourceLock)
                        {
                            _source.AddMessage(outputItem!);
                        }
                    }
                }
            }
            // Otherwise, there's a reordering buffer, so add to it instead.
            // Even if something goes wrong, we need to update the
            // reordering buffer, so it knows that an item isn't missing.
            else _reorderingBuffer.AddItem(messageWithId.Value, outputItem!, itemIsValid: gotOutputItem);

            // Let the target know that one of the asynchronous operations it launched has completed.
            _target.SignalOneAsyncMessageCompleted();
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Complete"]/*' />
        public void Complete() { _target.Complete(exception: null, dropPendingMessages: false); }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Fault"]/*' />
        void IDataflowBlock.Fault(Exception exception)
        {
            if (exception is null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            _target.Complete(exception, dropPendingMessages: true);
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="LinkTo"]/*' />
        public IDisposable LinkTo(ITargetBlock<TOutput> target, DataflowLinkOptions linkOptions)
        {
            return _source.LinkTo(target, linkOptions);
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="TryReceive"]/*' />
        public bool TryReceive(Predicate<TOutput>? filter, [MaybeNullWhen(false)] out TOutput item)
        {
            return _source.TryReceive(filter, out item);
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="TryReceiveAll"]/*' />
        public bool TryReceiveAll([NotNullWhen(true)] out IList<TOutput>? items) { return _source.TryReceiveAll(out items); }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Completion"]/*' />
        public Task Completion { get { return _source.Completion; } }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Targets/Member[@name="InputCount"]/*' />
        public int InputCount { get { return _target.InputCount; } }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="OutputCount"]/*' />
        public int OutputCount { get { return _source.OutputCount; } }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Targets/Member[@name="OfferMessage"]/*' />
        DataflowMessageStatus ITargetBlock<TInput>.OfferMessage(DataflowMessageHeader messageHeader, TInput messageValue, ISourceBlock<TInput>? source, bool consumeToAccept)
        {
            return _target.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="ConsumeMessage"]/*' />
        TOutput? ISourceBlock<TOutput>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target, out bool messageConsumed)
        {
            return _source.ConsumeMessage(messageHeader, target, out messageConsumed);
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="ReserveMessage"]/*' />
        bool ISourceBlock<TOutput>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
        {
            return _source.ReserveMessage(messageHeader, target);
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="ReleaseReservation"]/*' />
        void ISourceBlock<TOutput>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
        {
            _source.ReleaseReservation(messageHeader, target);
        }

        /// <summary>Gets the number of messages waiting to be processed.  This must only be used from the debugger as it avoids taking necessary locks.</summary>
        private int InputCountForDebugger { get { return _target.GetDebuggingInformation().InputCount; } }
        /// <summary>Gets the number of messages waiting to be processed.  This must only be used from the debugger as it avoids taking necessary locks.</summary>
        private int OutputCountForDebugger { get { return _source.GetDebuggingInformation().OutputCount; } }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="ToString"]/*' />
        public override string ToString() { return Common.GetNameForDebugger(this, _source.DataflowBlockOptions); }

        /// <summary>The data to display in the debugger display attribute.</summary>
        private object DebuggerDisplayContent =>
            $"{Common.GetNameForDebugger(this, _source.DataflowBlockOptions)}, InputCount={InputCountForDebugger}, OutputCount={OutputCountForDebugger}";

        /// <summary>Gets the data to display in the debugger display attribute for this instance.</summary>
        object IDebuggerDisplay.Content { get { return DebuggerDisplayContent; } }

        /// <summary>Provides a debugger type proxy for the TransformBlock.</summary>
        private sealed class DebugView
        {
            /// <summary>The transform being viewed.</summary>
            private readonly TransformBlock<TInput, TOutput> _transformBlock;
            /// <summary>The target half of the block being viewed.</summary>
            private readonly TargetCore<TInput>.DebuggingInformation _targetDebuggingInformation;
            /// <summary>The source half of the block being viewed.</summary>
            private readonly SourceCore<TOutput>.DebuggingInformation _sourceDebuggingInformation;

            /// <summary>Initializes the debug view.</summary>
            /// <param name="transformBlock">The transform being viewed.</param>
            public DebugView(TransformBlock<TInput, TOutput> transformBlock)
            {
                Debug.Assert(transformBlock != null, "Need a block with which to construct the debug view.");
                _transformBlock = transformBlock;
                _targetDebuggingInformation = transformBlock._target.GetDebuggingInformation();
                _sourceDebuggingInformation = transformBlock._source.GetDebuggingInformation();
            }

            /// <summary>Gets the messages waiting to be processed.</summary>
            public IEnumerable<TInput> InputQueue { get { return _targetDebuggingInformation.InputQueue; } }
            /// <summary>Gets any postponed messages.</summary>
            public QueuedMap<ISourceBlock<TInput>, DataflowMessageHeader>? PostponedMessages { get { return _targetDebuggingInformation.PostponedMessages; } }
            /// <summary>Gets the messages waiting to be received.</summary>
            public IEnumerable<TOutput> OutputQueue { get { return _sourceDebuggingInformation.OutputQueue; } }

            /// <summary>Gets the number of outstanding input operations.</summary>
            public int CurrentDegreeOfParallelism { get { return _targetDebuggingInformation.CurrentDegreeOfParallelism; } }
            /// <summary>Gets the task being used for output processing.</summary>
            public Task? TaskForOutputProcessing { get { return _sourceDebuggingInformation.TaskForOutputProcessing; } }

            /// <summary>Gets the DataflowBlockOptions used to configure this block.</summary>
            public ExecutionDataflowBlockOptions DataflowBlockOptions { get { return _targetDebuggingInformation.DataflowBlockOptions; } }
            /// <summary>Gets whether the block is declining further messages.</summary>
            public bool IsDecliningPermanently { get { return _targetDebuggingInformation.IsDecliningPermanently; } }
            /// <summary>Gets whether the block is completed.</summary>
            public bool IsCompleted { get { return _sourceDebuggingInformation.IsCompleted; } }
            /// <summary>Gets the block's Id.</summary>
            public int Id { get { return Common.GetBlockId(_transformBlock); } }

            /// <summary>Gets the set of all targets linked from this block.</summary>
            public TargetRegistry<TOutput> LinkedTargets { get { return _sourceDebuggingInformation.LinkedTargets; } }
            /// <summary>Gets the target that holds a reservation on the next message, if any.</summary>
            public ITargetBlock<TOutput>? NextMessageReservedFor { get { return _sourceDebuggingInformation.NextMessageReservedFor; } }
        }
    }
}
