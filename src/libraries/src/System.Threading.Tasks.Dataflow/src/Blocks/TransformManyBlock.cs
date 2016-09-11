// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// TransformManyBlock.cs
//
//
// A propagator block that runs a function on each input to produce zero or more outputs.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks.Dataflow.Internal;
using System.Collections.ObjectModel;

namespace System.Threading.Tasks.Dataflow
{
    /// <summary>Provides a dataflow block that invokes a provided <see cref="System.Func{T,TResult}"/> delegate for every data element received.</summary>
    /// <typeparam name="TInput">Specifies the type of data received and operated on by this <see cref="TransformManyBlock{TInput,TOutput}"/>.</typeparam>
    /// <typeparam name="TOutput">Specifies the type of data output by this <see cref="TransformManyBlock{TInput,TOutput}"/>.</typeparam>
    [DebuggerDisplay("{DebuggerDisplayContent,nq}")]
    [DebuggerTypeProxy(typeof(TransformManyBlock<,>.DebugView))]
    public sealed class TransformManyBlock<TInput, TOutput> : IPropagatorBlock<TInput, TOutput>, IReceivableSourceBlock<TOutput>, IDebuggerDisplay
    {
        /// <summary>The target side.</summary>
        private readonly TargetCore<TInput> _target;
        /// <summary>
        /// Buffer used to reorder output sets that may have completed out-of-order between the target half and the source half.
        /// This specialized reordering buffer supports streaming out enumerables if the message is the next in line.
        /// </summary>
        private readonly ReorderingBuffer<IEnumerable<TOutput>> _reorderingBuffer;
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

        /// <summary>Initializes the <see cref="TransformManyBlock{TInput,TOutput}"/> with the specified function.</summary>
        /// <param name="transform">
        /// The function to invoke with each data element received.  All of the data from the returned <see cref="System.Collections.Generic.IEnumerable{TOutput}"/>
        /// will be made available as output from this <see cref="TransformManyBlock{TInput,TOutput}"/>.
        /// </param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="transform"/> is null (Nothing in Visual Basic).</exception>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public TransformManyBlock(Func<TInput, IEnumerable<TOutput>> transform) :
            this(transform, null, ExecutionDataflowBlockOptions.Default)
        { }

        /// <summary>Initializes the <see cref="TransformManyBlock{TInput,TOutput}"/> with the specified function and <see cref="ExecutionDataflowBlockOptions"/>.</summary>
        /// <param name="transform">
        /// The function to invoke with each data element received.  All of the data from the returned in the <see cref="System.Collections.Generic.IEnumerable{TOutput}"/>
        /// will be made available as output from this <see cref="TransformManyBlock{TInput,TOutput}"/>.
        /// </param>
        /// <param name="dataflowBlockOptions">The options with which to configure this <see cref="TransformManyBlock{TInput,TOutput}"/>.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="transform"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="dataflowBlockOptions"/> is null (Nothing in Visual Basic).</exception>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public TransformManyBlock(Func<TInput, IEnumerable<TOutput>> transform, ExecutionDataflowBlockOptions dataflowBlockOptions) :
            this(transform, null, dataflowBlockOptions)
        { }

        /// <summary>Initializes the <see cref="TransformManyBlock{TInput,TOutput}"/> with the specified function.</summary>
        /// <param name="transform">
        /// The function to invoke with each data element received. All of the data asynchronously returned in the <see cref="System.Collections.Generic.IEnumerable{TOutput}"/>
        /// will be made available as output from this <see cref="TransformManyBlock{TInput,TOutput}"/>.
        /// </param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="transform"/> is null (Nothing in Visual Basic).</exception>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public TransformManyBlock(Func<TInput, Task<IEnumerable<TOutput>>> transform) :
            this(null, transform, ExecutionDataflowBlockOptions.Default)
        { }

        /// <summary>Initializes the <see cref="TransformManyBlock{TInput,TOutput}"/> with the specified function and <see cref="ExecutionDataflowBlockOptions"/>.</summary>
        /// <param name="transform">
        /// The function to invoke with each data element received. All of the data asynchronously returned in the <see cref="System.Collections.Generic.IEnumerable{TOutput}"/>
        /// will be made available as output from this <see cref="TransformManyBlock{TInput,TOutput}"/>.
        /// </param>
        /// <param name="dataflowBlockOptions">The options with which to configure this <see cref="TransformManyBlock{TInput,TOutput}"/>.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="transform"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="dataflowBlockOptions"/> is null (Nothing in Visual Basic).</exception>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public TransformManyBlock(Func<TInput, Task<IEnumerable<TOutput>>> transform, ExecutionDataflowBlockOptions dataflowBlockOptions) :
            this(null, transform, dataflowBlockOptions)
        { }

        /// <summary>Initializes the <see cref="TransformManyBlock{TInput,TOutput}"/> with the specified function and <see cref="ExecutionDataflowBlockOptions"/>.</summary>
        /// <param name="transformSync">The synchronous function to invoke with each data element received.</param>
        /// <param name="transformAsync">The asynchronous function to invoke with each data element received.</param>
        /// <param name="dataflowBlockOptions">The options with which to configure this <see cref="TransformManyBlock{TInput,TOutput}"/>.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="transformSync"/> and <paramref name="transformAsync"/> are both null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="dataflowBlockOptions"/> is null (Nothing in Visual Basic).</exception>
        private TransformManyBlock(Func<TInput, IEnumerable<TOutput>> transformSync, Func<TInput, Task<IEnumerable<TOutput>>> transformAsync, ExecutionDataflowBlockOptions dataflowBlockOptions)
        {
            // Validate arguments.  It's ok for the filterFunction to be null, but not the other parameters.
            if (transformSync == null && transformAsync == null) throw new ArgumentNullException("transform");
            if (dataflowBlockOptions == null) throw new ArgumentNullException(nameof(dataflowBlockOptions));

            Debug.Assert(transformSync == null ^ transformAsync == null, "Exactly one of transformSync and transformAsync must be null.");
            Contract.EndContractBlock();

            // Ensure we have options that can't be changed by the caller
            dataflowBlockOptions = dataflowBlockOptions.DefaultOrClone();

            // Initialize onItemsRemoved delegate if necessary
            Action<ISourceBlock<TOutput>, int> onItemsRemoved = null;
            if (dataflowBlockOptions.BoundedCapacity > 0)
                onItemsRemoved = (owningSource, count) => ((TransformManyBlock<TInput, TOutput>)owningSource)._target.ChangeBoundingCount(-count);

            // Initialize source component
            _source = new SourceCore<TOutput>(this, dataflowBlockOptions,
                owningSource => ((TransformManyBlock<TInput, TOutput>)owningSource)._target.Complete(exception: null, dropPendingMessages: true),
                onItemsRemoved);

            // If parallelism is employed, we will need to support reordering messages that complete out-of-order.
            // However, a developer can override this with EnsureOrdered == false.
            if (dataflowBlockOptions.SupportsParallelExecution && dataflowBlockOptions.EnsureOrdered)
            {
                _reorderingBuffer = new ReorderingBuffer<IEnumerable<TOutput>>(
                    this, (source, messages) => ((TransformManyBlock<TInput, TOutput>)source)._source.AddMessages(messages));
            }

            // Create the underlying target and source
            if (transformSync != null) // sync
            {
                // If an enumerable function was provided, we can use synchronous completion, meaning
                // that the target will consider a message fully processed as soon as the
                // delegate returns.
                _target = new TargetCore<TInput>(this,
                    messageWithId => ProcessMessage(transformSync, messageWithId),
                    _reorderingBuffer, dataflowBlockOptions, TargetCoreOptions.None);
            }
            else // async
            {
                Debug.Assert(transformAsync != null, "Incorrect delegate type.");

                // If a task-based function was provided, we need to use asynchronous completion, meaning
                // that the target won't consider a message completed until the task
                // returned from that delegate has completed.
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
                var sourceCore = (SourceCore<TOutput>)state;
                if (completed.IsFaulted) sourceCore.AddAndUnwrapAggregateException(completed.Exception);
                sourceCore.Complete();
            }, _source, CancellationToken.None, Common.GetContinuationOptions(), TaskScheduler.Default);

            // It is possible that the source half may fault on its own, e.g. due to a task scheduler exception.
            // In those cases we need to fault the target half to drop its buffered messages and to release its 
            // reservations. This should not create an infinite loop, because all our implementations are designed
            // to handle multiple completion requests and to carry over only one.
            _source.Completion.ContinueWith((completed, state) =>
            {
                var thisBlock = ((TransformManyBlock<TInput, TOutput>)state) as IDataflowBlock;
                Debug.Assert(completed.IsFaulted, "The source must be faulted in order to trigger a target completion.");
                thisBlock.Fault(completed.Exception);
            }, this, CancellationToken.None, Common.GetContinuationOptions() | TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);

            // Handle async cancellation requests by declining on the target
            Common.WireCancellationToComplete(
                dataflowBlockOptions.CancellationToken, Completion, state => ((TargetCore<TInput>)state).Complete(exception: null, dropPendingMessages: true), _target);
#if FEATURE_TRACING
            DataflowEtwProvider etwLog = DataflowEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.DataflowBlockCreated(this, dataflowBlockOptions);
            }
#endif
        }

        /// <summary>Processes the message with a user-provided transform function that returns an enumerable.</summary>
        /// <param name="transformFunction">The transform function to use to process the message.</param>
        /// <param name="messageWithId">The message to be processed.</param>
        private void ProcessMessage(Func<TInput, IEnumerable<TOutput>> transformFunction, KeyValuePair<TInput, long> messageWithId)
        {
            Debug.Assert(transformFunction != null, "Function to invoke is required.");

            bool userDelegateSucceeded = false;
            try
            {
                // Run the user transform and store the results.
                IEnumerable<TOutput> outputItems = transformFunction(messageWithId.Key);
                userDelegateSucceeded = true;
                StoreOutputItems(messageWithId, outputItems);
            }
            catch (Exception exc)
            {
                // If this exception represents cancellation, swallow it rather than shutting down the block.
                if (!Common.IsCooperativeCancellation(exc)) throw;
            }
            finally
            {
                // If the user delegate failed, store an empty set in order 
                // to update the bounding count and reordering buffer.
                if (!userDelegateSucceeded) StoreOutputItems(messageWithId, null);
            }
        }

        /// <summary>Processes the message with a user-provided transform function that returns an observable.</summary>
        /// <param name="function">The transform function to use to process the message.</param>
        /// <param name="messageWithId">The message to be processed.</param>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void ProcessMessageWithTask(Func<TInput, Task<IEnumerable<TOutput>>> function, KeyValuePair<TInput, long> messageWithId)
        {
            Debug.Assert(function != null, "Function to invoke is required.");

            // Run the transform function to get the resulting task
            Task<IEnumerable<TOutput>> task = null;
            Exception caughtException = null;
            try
            {
                task = function(messageWithId.Key);
            }
            catch (Exception exc) { caughtException = exc; }

            // If no task is available, either because null was returned or an exception was thrown, we're done.
            if (task == null)
            {
                // If we didn't get a task because an exception occurred, store it 
                // (or if the exception was cancellation, just ignore it).
                if (caughtException != null && !Common.IsCooperativeCancellation(caughtException))
                {
                    Common.StoreDataflowMessageValueIntoExceptionData(caughtException, messageWithId.Key);
                    _target.Complete(caughtException, dropPendingMessages: true, storeExceptionEvenIfAlreadyCompleting: true, unwrapInnerExceptions: false);
                }

                // Notify that we're done with this input and that we got no output for the input.
                if (_reorderingBuffer != null)
                {
                    // If there's a reordering buffer, "store" an empty output.  This will
                    // internally both update the output buffer and decrement the bounding count
                    // accordingly.
                    StoreOutputItems(messageWithId, null);
                    _target.SignalOneAsyncMessageCompleted();
                }
                else
                {
                    // As a fast path if we're not reordering, decrement the bounding
                    // count as part of our signaling that we're done, since this will 
                    // internally take the lock only once, whereas the above path will
                    // take the lock twice.
                    _target.SignalOneAsyncMessageCompleted(boundingCountChange: -1);
                }
                return;
            }

            // We got back a task.  Now wait for it to complete and store its results.
            // Unlike with TransformBlock and ActionBlock, We run the continuation on the user-provided 
            // scheduler as we'll be running user code through enumerating the returned enumerable.
            task.ContinueWith((completed, state) =>
            {
                var tuple = (Tuple<TransformManyBlock<TInput, TOutput>, KeyValuePair<TInput, long>>)state;
                tuple.Item1.AsyncCompleteProcessMessageWithTask(completed, tuple.Item2);
            }, Tuple.Create(this, messageWithId),
            CancellationToken.None,
            Common.GetContinuationOptions(TaskContinuationOptions.ExecuteSynchronously),
            _source.DataflowBlockOptions.TaskScheduler);
        }

        /// <summary>Completes the processing of an asynchronous message.</summary>
        /// <param name="completed">The completed task storing the output data generated for an input message.</param>
        /// <param name="messageWithId">The originating message</param>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void AsyncCompleteProcessMessageWithTask(
            Task<IEnumerable<TOutput>> completed, KeyValuePair<TInput, long> messageWithId)
        {
            Debug.Assert(completed != null, "A task should have been provided.");
            Debug.Assert(completed.IsCompleted, "The task should have been in a final state.");

            switch (completed.Status)
            {
                case TaskStatus.RanToCompletion:
                    IEnumerable<TOutput> outputItems = completed.Result;
                    try
                    {
                        // Get the resulting enumerable and persist it.
                        StoreOutputItems(messageWithId, outputItems);
                    }
                    catch (Exception exc)
                    {
                        // Enumerating the user's collection failed. If this exception represents cancellation, 
                        // swallow it rather than shutting down the block.
                        if (!Common.IsCooperativeCancellation(exc))
                        {
                            // The exception was not for cancellation. We must add the exception before declining 
                            // and signaling completion, as the exception is part of the operation, and the completion 
                            // conditions depend on this.
                            Common.StoreDataflowMessageValueIntoExceptionData(exc, messageWithId.Key);
                            _target.Complete(exc, dropPendingMessages: true, storeExceptionEvenIfAlreadyCompleting: true, unwrapInnerExceptions: false);
                        }
                    }
                    break;

                case TaskStatus.Faulted:
                    // We must add the exception before declining and signaling completion, as the exception 
                    // is part of the operation, and the completion conditions depend on this.
                    AggregateException aggregate = completed.Exception;
                    Common.StoreDataflowMessageValueIntoExceptionData(aggregate, messageWithId.Key, targetInnerExceptions: true);
                    _target.Complete(aggregate, dropPendingMessages: true, storeExceptionEvenIfAlreadyCompleting: true, unwrapInnerExceptions: true);
                    goto case TaskStatus.Canceled;
                case TaskStatus.Canceled:
                    StoreOutputItems(messageWithId, null); // notify the reordering buffer and decrement the bounding count
                    break;

                default:
                    Debug.Assert(false, "The task should have been in a final state.");
                    break;
            }

            // Let the target know that one of the asynchronous operations it launched has completed.
            _target.SignalOneAsyncMessageCompleted();
        }

        /// <summary>
        /// Stores the output items, either into the reordering buffer or into the source half.
        /// Ensures that the bounding count is correctly updated.
        /// </summary>
        /// <param name="messageWithId">The message with id.</param>
        /// <param name="outputItems">The output items to be persisted.</param>
        private void StoreOutputItems(
            KeyValuePair<TInput, long> messageWithId, IEnumerable<TOutput> outputItems)
        {
            // If there's a reordering buffer, pass the data along to it.
            // The reordering buffer will handle all details, including bounding.
            if (_reorderingBuffer != null)
            {
                StoreOutputItemsReordered(messageWithId.Value, outputItems);
            }
            // Otherwise, output the data directly.
            else if (outputItems != null)
            {
                // If this is a trusted type, output the data en mass.
                if (outputItems is TOutput[] || outputItems is List<TOutput>)
                {
                    StoreOutputItemsNonReorderedAtomic(outputItems);
                }
                else
                {
                    // Otherwise, we need to take the slow path of enumerating
                    // each individual item.
                    StoreOutputItemsNonReorderedWithIteration(outputItems);
                }
            }
            else if (_target.IsBounded)
            {
                // outputItems is null and there's no reordering buffer
                // and we're bounding, so decrement the bounding count to
                // signify that the input element we already accounted for
                // produced no output
                _target.ChangeBoundingCount(count: -1);
            }
            // else there's no reordering buffer, there are no output items, and we're not bounded,
            // so there's nothing more to be done.
        }

        /// <summary>Stores the next item using the reordering buffer.</summary>
        /// <param name="id">The ID of the item.</param>
        /// <param name="item">The completed item.</param>
        private void StoreOutputItemsReordered(long id, IEnumerable<TOutput> item)
        {
            Debug.Assert(_reorderingBuffer != null, "Expected a reordering buffer");
            Debug.Assert(id != Common.INVALID_REORDERING_ID, "This ID should never have been handed out.");

            // Grab info about the transform
            TargetCore<TInput> target = _target;
            bool isBounded = target.IsBounded;

            // Handle invalid items (null enumerables) by delegating to the base
            if (item == null)
            {
                _reorderingBuffer.AddItem(id, null, false);
                if (isBounded) target.ChangeBoundingCount(count: -1);
                return;
            }

            // If we can eagerly get the number of items in the collection, update the bounding count.
            // This avoids the cost of updating it once per output item (since each update requires synchronization).
            // Even if we're not bounding, we still want to determine whether the item is trusted so that we 
            // can immediately dump it out once we take the lock if we're the next item.
            IList<TOutput> itemAsTrustedList = item as TOutput[];
            if (itemAsTrustedList == null) itemAsTrustedList = item as List<TOutput>;
            if (itemAsTrustedList != null && isBounded)
            {
                UpdateBoundingCountWithOutputCount(count: itemAsTrustedList.Count);
            }

            // Determine whether this id is the next item, and if it is and if we have a trusted list,
            // try to output it immediately on the fast path.  If it can be output, we're done.
            // Otherwise, make forward progress based on whether we're next in line.
            bool? isNextNullable = _reorderingBuffer.AddItemIfNextAndTrusted(id, itemAsTrustedList, itemAsTrustedList != null);
            if (!isNextNullable.HasValue) return; // data was successfully output
            bool isNextItem = isNextNullable.Value;

            // By this point, either we're not the next item, in which case we need to make a copy of the
            // data and store it, or we are the next item and can store it immediately but we need to enumerate
            // the items and store them individually because we don't want to enumerate while holding a lock.
            List<TOutput> itemCopy = null;
            try
            {
                // If this is the next item, we can output it now.
                if (isNextItem)
                {
                    StoreOutputItemsNonReorderedWithIteration(item);
                    // here itemCopy remains null, so that base.AddItem will finish our interactions with the reordering buffer
                }
                else if (itemAsTrustedList != null)
                {
                    itemCopy = itemAsTrustedList.ToList();
                    // we already got the count and updated the bounding count previously
                }
                else
                {
                    // We're not the next item, and we're not trusted, so copy the data into a list.
                    // We need to enumerate outside of the lock in the base class.
                    int itemCount = 0;
                    try
                    {
                        itemCopy = item.ToList(); // itemCopy will remain null in the case of exception
                        itemCount = itemCopy.Count;
                    }
                    finally
                    {
                        // If we're here successfully, then itemCount is the number of output items
                        // we actually received, and we should update the bounding count with it.
                        // If we're here because ToList threw an exception, then itemCount will be 0,
                        // and we still need to update the bounding count with this in order to counteract
                        // the increased bounding count for the corresponding input.
                        if (isBounded) UpdateBoundingCountWithOutputCount(count: itemCount);
                    }
                }
                // else if the item isn't valid, the finally block will see itemCopy as null and output invalid
            }
            finally
            {
                // Tell the base reordering buffer that we're done.  If we already output
                // all of the data, itemCopy will be null, and we just pass down the invalid item.  
                // If we haven't, pass down the real thing.  We do this even in the case of an exception,
                // in which case this will be a dummy element.
                _reorderingBuffer.AddItem(id, itemCopy, itemIsValid: itemCopy != null);
            }
        }

        /// <summary>
        /// Stores the trusted enumerable en mass into the source core.
        /// This method does not go through the reordering buffer.
        /// </summary>
        /// <param name="outputItems"></param>
        private void StoreOutputItemsNonReorderedAtomic(IEnumerable<TOutput> outputItems)
        {
            Debug.Assert(_reorderingBuffer == null, "Expected not to have a reordering buffer");
            Debug.Assert(outputItems is TOutput[] || outputItems is List<TOutput>, "outputItems must be a list we've already vetted as trusted");
            if (_target.IsBounded) UpdateBoundingCountWithOutputCount(count: ((ICollection<TOutput>)outputItems).Count);

            if (_target.DataflowBlockOptions.MaxDegreeOfParallelism == 1)
            {
                _source.AddMessages(outputItems);
            }
            else
            {
                lock (ParallelSourceLock)
                {
                    _source.AddMessages(outputItems);
                }
            }
        }

        /// <summary>
        /// Stores the untrusted enumerable into the source core.
        /// This method does not go through the reordering buffer.
        /// </summary>
        /// <param name="outputItems">The untrusted enumerable.</param>
        private void StoreOutputItemsNonReorderedWithIteration(IEnumerable<TOutput> outputItems)
        {
            bool isSerial = _target.DataflowBlockOptions.MaxDegreeOfParallelism == 1;

            // If we're bounding, we need to increment the bounded count
            // for each individual item as we enumerate it.
            if (_target.IsBounded)
            {
                // When the input item that generated this
                // output was loaded, we incremented the bounding count.  If it only
                // output a single a item, then we don't need to touch the bounding count.
                // Otherwise, we need to adjust the bounding count accordingly.
                bool outputFirstItem = false;
                try
                {
                    foreach (TOutput item in outputItems)
                    {
                        if (outputFirstItem) _target.ChangeBoundingCount(count: 1);
                        else outputFirstItem = true;

                        if (isSerial)
                        {
                            _source.AddMessage(item);
                        }
                        else
                        {
                            lock (ParallelSourceLock) // don't hold lock while enumerating
                            {
                                _source.AddMessage(item);
                            }
                        }
                    }
                }
                finally
                {
                    if (!outputFirstItem) _target.ChangeBoundingCount(count: -1);
                }
            }
            // If we're not bounding, just output each individual item.
            else
            {
                if (isSerial)
                {
                    foreach (TOutput item in outputItems)
                        _source.AddMessage(item);
                }
                else
                {
                    lock (ParallelSourceLock) // don't hold lock while enumerating
                    {
                        foreach (TOutput item in outputItems)
                            _source.AddMessage(item);
                    }
                }
            }
        }

        /// <summary>
        /// Updates the bounding count based on the number of output items
        /// generated for a single input.
        /// </summary>
        /// <param name="count">The number of output items.</param>
        private void UpdateBoundingCountWithOutputCount(int count)
        {
            // We already incremented the count for a single input item, and
            // that input spawned 0 or more outputs.  Take the input tracking
            // into account when figuring out how much to increment or decrement
            // the bounding count.

            Debug.Assert(_target.IsBounded, "Expected to be in bounding mode.");
            if (count > 1) _target.ChangeBoundingCount(count - 1);
            else if (count == 0) _target.ChangeBoundingCount(-1);
            else Debug.Assert(count == 1, "Count shouldn't be negative.");
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Complete"]/*' />
        public void Complete() { _target.Complete(exception: null, dropPendingMessages: false); }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Fault"]/*' />
        void IDataflowBlock.Fault(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));
            Contract.EndContractBlock();

            _target.Complete(exception, dropPendingMessages: true);
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="LinkTo"]/*' />
        public IDisposable LinkTo(ITargetBlock<TOutput> target, DataflowLinkOptions linkOptions) { return _source.LinkTo(target, linkOptions); }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="TryReceive"]/*' />
        public Boolean TryReceive(Predicate<TOutput> filter, out TOutput item) { return _source.TryReceive(filter, out item); }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="TryReceiveAll"]/*' />
        public bool TryReceiveAll(out IList<TOutput> items) { return _source.TryReceiveAll(out items); }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Completion"]/*' />
        public Task Completion { get { return _source.Completion; } }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Targets/Member[@name="InputCount"]/*' />
        public int InputCount { get { return _target.InputCount; } }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="OutputCount"]/*' />
        public int OutputCount { get { return _source.OutputCount; } }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Targets/Member[@name="OfferMessage"]/*' />
        DataflowMessageStatus ITargetBlock<TInput>.OfferMessage(DataflowMessageHeader messageHeader, TInput messageValue, ISourceBlock<TInput> source, Boolean consumeToAccept)
        {
            return _target.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="ConsumeMessage"]/*' />
        TOutput ISourceBlock<TOutput>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target, out Boolean messageConsumed)
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
        [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider")]
        private object DebuggerDisplayContent
        {
            get
            {
                return string.Format("{0}, InputCount={1}, OutputCount={2}",
                    Common.GetNameForDebugger(this, _source.DataflowBlockOptions),
                    InputCountForDebugger,
                    OutputCountForDebugger);
            }
        }
        /// <summary>Gets the data to display in the debugger display attribute for this instance.</summary>
        object IDebuggerDisplay.Content { get { return DebuggerDisplayContent; } }

        /// <summary>Provides a debugger type proxy for the TransformManyBlock.</summary>
        private sealed class DebugView
        {
            /// <summary>The transform many block being viewed.</summary>
            private readonly TransformManyBlock<TInput, TOutput> _transformManyBlock;
            /// <summary>The target half of the block being viewed.</summary>
            private readonly TargetCore<TInput>.DebuggingInformation _targetDebuggingInformation;
            /// <summary>The source half of the block being viewed.</summary>
            private readonly SourceCore<TOutput>.DebuggingInformation _sourceDebuggingInformation;

            /// <summary>Initializes the debug view.</summary>
            /// <param name="transformManyBlock">The transform being viewed.</param>
            public DebugView(TransformManyBlock<TInput, TOutput> transformManyBlock)
            {
                Debug.Assert(transformManyBlock != null, "Need a block with which to construct the debug view.");
                _transformManyBlock = transformManyBlock;
                _targetDebuggingInformation = transformManyBlock._target.GetDebuggingInformation();
                _sourceDebuggingInformation = transformManyBlock._source.GetDebuggingInformation();
            }

            /// <summary>Gets the messages waiting to be processed.</summary>
            public IEnumerable<TInput> InputQueue { get { return _targetDebuggingInformation.InputQueue; } }
            /// <summary>Gets any postponed messages.</summary>
            public QueuedMap<ISourceBlock<TInput>, DataflowMessageHeader> PostponedMessages { get { return _targetDebuggingInformation.PostponedMessages; } }
            /// <summary>Gets the messages waiting to be received.</summary>
            public IEnumerable<TOutput> OutputQueue { get { return _sourceDebuggingInformation.OutputQueue; } }

            /// <summary>Gets the number of input operations currently in flight.</summary>
            public Int32 CurrentDegreeOfParallelism { get { return _targetDebuggingInformation.CurrentDegreeOfParallelism; } }
            /// <summary>Gets the task being used for output processing.</summary>
            public Task TaskForOutputProcessing { get { return _sourceDebuggingInformation.TaskForOutputProcessing; } }

            /// <summary>Gets the DataflowBlockOptions used to configure this block.</summary>
            public ExecutionDataflowBlockOptions DataflowBlockOptions { get { return _targetDebuggingInformation.DataflowBlockOptions; } }
            /// <summary>Gets whether the block is declining further messages.</summary>
            public bool IsDecliningPermanently { get { return _targetDebuggingInformation.IsDecliningPermanently; } }
            /// <summary>Gets whether the block is completed.</summary>
            public bool IsCompleted { get { return _sourceDebuggingInformation.IsCompleted; } }
            /// <summary>Gets the block's Id.</summary>
            public int Id { get { return Common.GetBlockId(_transformManyBlock); } }

            /// <summary>Gets the set of all targets linked from this block.</summary>
            public TargetRegistry<TOutput> LinkedTargets { get { return _sourceDebuggingInformation.LinkedTargets; } }
            /// <summary>Gets the set of all targets linked from this block.</summary>
            public ITargetBlock<TOutput> NextMessageReservedFor { get { return _sourceDebuggingInformation.NextMessageReservedFor; } }
        }
    }
}
