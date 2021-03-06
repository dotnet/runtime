// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow.Internal;

namespace System.Threading.Tasks.Dataflow
{
    public partial class TransformManyBlock<TInput, TOutput>
    {
        /// <summary>Initializes the <see cref="TransformManyBlock{TInput,TOutput}"/> with the specified function.</summary>
        /// <param name="transform">
        /// The function to invoke with each data element received.  All of the data from the returned <see cref="System.Collections.Generic.IAsyncEnumerable{TOutput}"/>
        /// will be made available as output from this <see cref="TransformManyBlock{TInput,TOutput}"/>.
        /// </param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="transform"/> is <see langword="null" />.</exception>
        public TransformManyBlock(Func<TInput, IAsyncEnumerable<TOutput>> transform) :
            this(transform, ExecutionDataflowBlockOptions.Default)
        { }

        /// <summary>Initializes the <see cref="TransformManyBlock{TInput,TOutput}"/> with the specified function and <see cref="ExecutionDataflowBlockOptions"/>.</summary>
        /// <param name="transform">
        /// The function to invoke with each data element received.  All of the data from the returned <see cref="System.Collections.Generic.IAsyncEnumerable{TOutput}"/>
        /// will be made available as output from this <see cref="TransformManyBlock{TInput,TOutput}"/>.
        /// </param>
        /// <param name="dataflowBlockOptions">The options with which to configure this <see cref="TransformManyBlock{TInput,TOutput}"/>.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="transform"/> or <paramref name="dataflowBlockOptions"/> is <see langword="null" />.</exception>
        public TransformManyBlock(Func<TInput, IAsyncEnumerable<TOutput>> transform, ExecutionDataflowBlockOptions dataflowBlockOptions)
        {
            // Validate arguments.
            if (transform == null) throw new ArgumentNullException(nameof(transform));
            Initialize(messageWithId => ProcessMessage(transform, messageWithId), dataflowBlockOptions, ref _source!, ref _target!, ref _reorderingBuffer, TargetCoreOptions.UsesAsyncCompletion);
        }

        /// <summary>Processes the message with a user-provided transform function that returns an async enumerable.</summary>
        /// <param name="transformFunction">The transform function to use to process the message.</param>
        /// <param name="messageWithId">The message to be processed.</param>
        private void ProcessMessage(Func<TInput, IAsyncEnumerable<TOutput>> transformFunction, KeyValuePair<TInput, long> messageWithId)
        {
            Debug.Assert(transformFunction != null, "Function to invoke is required.");

            try
            {
                // Run the user transform and store the results.
                IAsyncEnumerable<TOutput> outputItems = transformFunction(messageWithId.Key);
                StoreOutputItemsAsync(messageWithId, outputItems).GetAwaiter().GetResult();
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
            finally
            {
                // Let the target know that one of the asynchronous operations it launched has completed.
                _target.SignalOneAsyncMessageCompleted();
            }
        }

        /// <summary>
        /// Stores the output items, either into the reordering buffer or into the source half.
        /// Ensures that the bounding count is correctly updated.
        /// </summary>
        /// <param name="messageWithId">The message with id.</param>
        /// <param name="outputItems">The output items to be persisted.</param>
        private async Task StoreOutputItemsAsync(
            KeyValuePair<TInput, long> messageWithId, IAsyncEnumerable<TOutput>? outputItems)
        {
            // If there's a reordering buffer, pass the data along to it.
            // The reordering buffer will handle all details, including bounding.
            if (_reorderingBuffer != null)
            {
                await StoreOutputItemsReorderedAsync(messageWithId.Value, outputItems).ConfigureAwait(false);
            }
            // Otherwise, output the data directly.
            else if (outputItems != null)
            {
                await StoreOutputItemsNonReorderedWithIterationAsync(outputItems).ConfigureAwait(false);
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
        /// <param name="item">The async enumerable.</param>
        private async Task StoreOutputItemsReorderedAsync(long id, IAsyncEnumerable<TOutput>? item)
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

            // Determine whether this id is the next item, and if it is and if we have a trusted list,
            // try to output it immediately on the fast path.  If it can be output, we're done.
            // Otherwise, make forward progress based on whether we're next in line.
            bool? isNextNullable = _reorderingBuffer.AddItemIfNextAndTrusted(id, null, false);
            if (!isNextNullable.HasValue) return; // data was successfully output
            bool isNextItem = isNextNullable.Value;

            // By this point, either we're not the next item, in which case we need to make a copy of the
            // data and store it, or we are the next item and can store it immediately but we need to enumerate
            // the items and store them individually because we don't want to enumerate while holding a lock.
            List<TOutput>? itemCopy = null;
            try
            {
                // If this is the next item, we can output it now.
                if (isNextItem)
                {
                    await StoreOutputItemsNonReorderedWithIterationAsync(item).ConfigureAwait(false);
                    // here itemCopy remains null, so that base.AddItem will finish our interactions with the reordering buffer
                }
                else
                {
                    // We're not the next item, and we're not trusted, so copy the data into a list.
                    // We need to enumerate outside of the lock in the base class.
                    int itemCount = 0;
                    try
                    {
                        itemCopy = new List<TOutput>();
                        await foreach (TOutput element in item.ConfigureAwait(false))
                        {
                            itemCopy.Add(element);
                        }
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
        /// Stores the untrusted async enumerable into the source core.
        /// This method does not go through the reordering buffer.
        /// </summary>
        /// <param name="outputItems">The untrusted enumerable.</param>
        private async Task StoreOutputItemsNonReorderedWithIterationAsync(IAsyncEnumerable<TOutput> outputItems)
        {
            // The _source we're adding to isn't thread-safe, so we need to determine
            // whether we need to lock.  If the block is configured with a max degree
            // of parallelism of 1, then only one transform can run at a time, and so
            // we don't need to lock.  Similarly, if there's a reordering buffer, then
            // it guarantees that we're invoked serially, and we don't need to lock.
            bool isSerial =
                _target.DataflowBlockOptions.MaxDegreeOfParallelism == 1 ||
                _reorderingBuffer != null;

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
                    await foreach (TOutput item in outputItems.ConfigureAwait(false))
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
                    await foreach (TOutput item in outputItems.ConfigureAwait(false))
                        _source.AddMessage(item);
                }
                else
                {
                    await foreach (TOutput item in outputItems.ConfigureAwait(false))
                    {
                        lock (ParallelSourceLock) // don't hold lock while enumerating
                        {
                            _source.AddMessage(item);
                        }
                    }
                }
            }
        }
    }
}
