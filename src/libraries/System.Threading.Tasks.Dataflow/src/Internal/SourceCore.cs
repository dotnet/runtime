// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// SourceCore.cs
//
//
// The core implementation of a standard ISourceBlock<TOutput>.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security;

namespace System.Threading.Tasks.Dataflow.Internal
{
    // LOCK-LEVELING SCHEME
    // --------------------
    // SourceCore employs two locks: OutgoingLock and ValueLock.  Additionally, targets we call out to
    // likely utilize their own IncomingLock.  We can hold OutgoingLock while acquiring ValueLock or IncomingLock.
    // However, we cannot hold ValueLock while calling out to external code or while acquiring OutgoingLock, and
    // we cannot hold IncomingLock when acquiring OutgoingLock. Additionally, the locks employed must be reentrant.

    /// <summary>Provides a core implementation for blocks that implement <see cref="ISourceBlock{TOutput}"/>.</summary>
    /// <typeparam name="TOutput">Specifies the type of data supplied by the <see cref="SourceCore{TOutput}"/>.</typeparam>
    [DebuggerDisplay("{DebuggerDisplayContent,nq}")]
    internal sealed class SourceCore<TOutput>
    {
        // *** These fields are readonly and are initialized to new instances at construction.

        /// <summary>A TaskCompletionSource that represents the completion of this block.</summary>
        private readonly TaskCompletionSource<VoidResult> _completionTask = new TaskCompletionSource<VoidResult>();
        /// <summary>A registry used to store all linked targets and information about them.</summary>
        private readonly TargetRegistry<TOutput> _targetRegistry;
        /// <summary>The output messages queued up to be received by consumers/targets.</summary>
        /// <remarks>
        /// The queue is only ever accessed by a single producer and single consumer at a time.  On the producer side,
        /// we require that AddMessage/AddMessages are the only places the queue is added to, and we require that those
        /// methods not be used concurrently with anything else.  All of our target halves today follow that restriction;
        /// for example, TransformBlock with DOP==1 will have at most a single task processing the user provided delegate,
        /// and thus at most one task calling AddMessage.  If it has a DOP > 1, it'll go through the ReorderingBuffer,
        /// which will use a lock to synchronize the output of all of the processing tasks such that only one is using
        /// AddMessage at a time.  On the consumer side of SourceCore, all consumption is protected by ValueLock, and thus
        /// all consumption is serialized.
        /// </remarks>
        private readonly SingleProducerSingleConsumerQueue<TOutput> _messages = new SingleProducerSingleConsumerQueue<TOutput>(); // protected by AddMessage/ValueLock

        /// <summary>Gets the object to use as the outgoing lock.</summary>
        private object OutgoingLock { get { return _completionTask; } }
        /// <summary>Gets the object to use as the value lock.</summary>
        private object ValueLock { get { return _targetRegistry; } }

        // *** These fields are readonly and are initialized by arguments to the constructor.

        /// <summary>The source utilizing this helper.</summary>
        private readonly ISourceBlock<TOutput> _owningSource;
        /// <summary>The options used to configure this block's execution.</summary>
        private readonly DataflowBlockOptions _dataflowBlockOptions;
        /// <summary>
        /// An action to be invoked on the owner block to stop accepting messages.
        /// This action is invoked when SourceCore encounters an exception.
        /// </summary>
        private readonly Action<ISourceBlock<TOutput>> _completeAction;
        /// <summary>
        /// An action to be invoked on the owner block when an item is removed.
        /// This may be null if the owner block doesn't need to be notified.
        /// </summary>
        private readonly Action<ISourceBlock<TOutput>, int>? _itemsRemovedAction;
        /// <summary>Item counting function</summary>
        private readonly Func<ISourceBlock<TOutput>, TOutput, IList<TOutput>?, int>? _itemCountingFunc;

        // *** These fields are mutated during execution.

        /// <summary>The task used to process the output and offer it to targets.</summary>
        private Task? _taskForOutputProcessing; // protected by ValueLock
        /// <summary>Counter for message IDs unique within this source block.</summary>
        private PaddedInt64 _nextMessageId = new PaddedInt64 { Value = 1 }; // We are going to use this value before incrementing.  Protected by ValueLock.
        /// <summary>The target that the next message is reserved for, or null if nothing is reserved.</summary>
        private ITargetBlock<TOutput>? _nextMessageReservedFor; // protected by OutgoingLock
        /// <summary>Whether all future messages should be declined.</summary>
        private bool _decliningPermanently; // Protected by ValueLock
        /// <summary>Whether this block should again attempt to offer messages to targets.</summary>
        private bool _enableOffering = true; // Protected by ValueLock, sometimes read with volatile reads
        /// <summary>Whether someone has reserved the right to call CompleteBlockOncePossible.</summary>
        private bool _completionReserved; // Protected by OutgoingLock
        /// <summary>Exceptions that may have occurred and gone unhandled during processing.</summary>
        private List<Exception>? _exceptions; // Protected by ValueLock, sometimes read with volatile reads

        /// <summary>Initializes the source core.</summary>
        /// <param name="owningSource">The source utilizing this core.</param>
        /// <param name="dataflowBlockOptions">The options to use to configure the block.</param>
        /// <param name="completeAction">Action to invoke in order to decline the associated target half, which will in turn decline this source core.</param>
        /// <param name="itemsRemovedAction">Action to invoke when one or more items is removed.  This may be null.</param>
        /// <param name="itemCountingFunc">
        /// Action to invoke when the owner needs to be able to count the number of individual
        /// items in an output or set of outputs.
        /// </param>
        internal SourceCore(
            ISourceBlock<TOutput> owningSource, DataflowBlockOptions dataflowBlockOptions,
            Action<ISourceBlock<TOutput>> completeAction,
            Action<ISourceBlock<TOutput>, int>? itemsRemovedAction = null,
            Func<ISourceBlock<TOutput>, TOutput, IList<TOutput>?, int>? itemCountingFunc = null)
        {
            Debug.Assert(owningSource != null, "Core must be associated with a source.");
            Debug.Assert(dataflowBlockOptions != null, "Options must be provided to configure the core.");
            Debug.Assert(completeAction != null, "Action to invoke on completion is required.");

            // Store the args
            _owningSource = owningSource;
            _dataflowBlockOptions = dataflowBlockOptions;
            _itemsRemovedAction = itemsRemovedAction;
            _itemCountingFunc = itemCountingFunc;
            _completeAction = completeAction;

            // Construct members that depend on the args
            _targetRegistry = new TargetRegistry<TOutput>(_owningSource);
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="LinkTo"]/*' />
        internal IDisposable LinkTo(ITargetBlock<TOutput> target, DataflowLinkOptions linkOptions)
        {
            // Validate arguments
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (linkOptions == null) throw new ArgumentNullException(nameof(linkOptions));

            // If the block is already completed, there is not much to do -
            // we have to propagate completion if that was requested, and
            // then bail without taking the lock.
            if (_completionTask.Task.IsCompleted)
            {
                if (linkOptions.PropagateCompletion) Common.PropagateCompletion(_completionTask.Task, target, exceptionHandler: null);
                return Disposables.Nop;
            }

            lock (OutgoingLock)
            {
                // If completion has been reserved, the target registry has either been cleared already
                // or is about to be cleared. So we can link and offer only if completion is not reserved.
                if (!_completionReserved)
                {
                    _targetRegistry.Add(ref target, linkOptions);
                    OfferToTargets(linkToTarget: target);
                    return Common.CreateUnlinker(OutgoingLock, _targetRegistry, target);
                }
            }

            // The block should not offer any messages when it is in this state, but
            // it should still propagate completion if that has been requested.
            if (linkOptions.PropagateCompletion) Common.PropagateCompletionOnceCompleted(_completionTask.Task, target);
            return Disposables.Nop;
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="ConsumeMessage"]/*' />
        internal TOutput? ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target, out bool messageConsumed)
        {
            // Validate arguments
            if (!messageHeader.IsValid) throw new ArgumentException(SR.Argument_InvalidMessageHeader, nameof(messageHeader));
            if (target == null) throw new ArgumentNullException(nameof(target));

            TOutput? consumedMessageValue = default(TOutput);

            lock (OutgoingLock)
            {
                // If this target doesn't hold the reservation, then for this ConsumeMessage
                // to be valid, there must not be any reservation (since otherwise we can't
                // consume a message destined for someone else).
                if (_nextMessageReservedFor != target &&
                    _nextMessageReservedFor != null)
                {
                    messageConsumed = false;
                    return default(TOutput);
                }

                lock (ValueLock)
                {
                    // If the requested message isn't the next message to be served up, bail.
                    // Otherwise, we're good to go: dequeue the message as it will now be owned by the target,
                    // signal that we can resume enabling offering as there's potentially a new "next message",
                    // complete if necessary, and offer asynchronously all messages as is appropriate.

                    if (messageHeader.Id != _nextMessageId.Value ||
                        !_messages.TryDequeue(out consumedMessageValue))
                    {
                        messageConsumed = false;
                        return default(TOutput);
                    }

                    _nextMessageReservedFor = null;
                    _targetRegistry.Remove(target, onlyIfReachedMaxMessages: true);
                    _enableOffering = true; // reenable offering if it was disabled
                    _nextMessageId.Value++;
                    CompleteBlockIfPossible();
                    OfferAsyncIfNecessary(isReplacementReplica: false, outgoingLockKnownAcquired: true);
                }
            }

            // Notify the owner block that our count has decreased
            if (_itemsRemovedAction != null)
            {
                int count = _itemCountingFunc != null ? _itemCountingFunc(_owningSource, consumedMessageValue, null) : 1;
                _itemsRemovedAction(_owningSource, count);
            }

            // Return the consumed message value
            messageConsumed = true;
            return consumedMessageValue;
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
                        // ...and if the requested message is next in the queue, allow it
                        if (messageHeader.Id == _nextMessageId.Value && !_messages.IsEmpty)
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

                lock (ValueLock)
                {
                    // If this is not the message at the head of the queue, bail
                    if (messageHeader.Id != _nextMessageId.Value || _messages.IsEmpty) throw new InvalidOperationException(SR.InvalidOperation_MessageNotReservedByTarget);

                    // Otherwise, release the reservation
                    _nextMessageReservedFor = null;
                    Debug.Assert(!_enableOffering, "Offering should have been disabled if there was a valid reservation");
                    _enableOffering = true;

                    // Now there is at least one message ready for offering. So offer it.
                    // If a cancellation is pending, this method will bail out.
                    OfferAsyncIfNecessary(isReplacementReplica: false, outgoingLockKnownAcquired: true);

                    // This reservation may be holding the block's completion. So try to complete.
                    CompleteBlockIfPossible();
                }
            }
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Completion"]/*' />
        internal Task Completion { get { return _completionTask.Task; } }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="TryReceive"]/*' />
        internal bool TryReceive(Predicate<TOutput>? filter, [MaybeNullWhen(false)] out TOutput item)
        {
            item = default(TOutput);
            bool itemReceived = false;

            lock (OutgoingLock)
            {
                // If the next message is reserved for someone, we can't receive right now.  Otherwise...
                if (_nextMessageReservedFor == null)
                {
                    lock (ValueLock)
                    {
                        // If there's at least one message, and there's no filter or the next item
                        // passes the filter, dequeue it to be returned.
                        if (_messages.TryDequeueIf(filter, out item))
                        {
                            _nextMessageId.Value++;

                            // Now that the next message has changed, reenable offering if it was disabled
                            _enableOffering = true;

                            // If removing this item was the last thing this block will ever do, complete it,
                            CompleteBlockIfPossible();

                            // Now, try to offer up messages asynchronously, since we've
                            // changed what's at the head of the queue
                            OfferAsyncIfNecessary(isReplacementReplica: false, outgoingLockKnownAcquired: true);

                            itemReceived = true;
                        }
                    }
                }
            }

            if (itemReceived)
            {
                // Notify the owner block that our count has decreased
                if (_itemsRemovedAction != null)
                {
                    int count = _itemCountingFunc != null ? _itemCountingFunc(_owningSource, item!, null) : 1;
                    _itemsRemovedAction(_owningSource, count);
                }
            }
            return itemReceived;
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="TryReceiveAll"]/*' />
        internal bool TryReceiveAll([NotNullWhen(true)] out IList<TOutput>? items)
        {
            items = null;
            int countReceived = 0;

            lock (OutgoingLock)
            {
                // If the next message is reserved for someone, we can't receive right now.  Otherwise...
                if (_nextMessageReservedFor == null)
                {
                    lock (ValueLock)
                    {
                        if (!_messages.IsEmpty)
                        {
                            // Receive all of the data, clearing it out in the process.
                            var tmpList = new List<TOutput>();
                            TOutput? item;
                            while (_messages.TryDequeue(out item)) tmpList.Add(item);
                            countReceived = tmpList.Count;
                            items = tmpList;

                            // Increment the next ID. Any new value is good.
                            _nextMessageId.Value++;

                            // Now that the next message has changed, reenable offering if it was disabled
                            _enableOffering = true;

                            // Now that the block is empty, check to see whether we should complete.
                            CompleteBlockIfPossible();
                        }
                    }
                }
            }

            if (countReceived > 0)
            {
                // Notify the owner block that our count has decreased
                if (_itemsRemovedAction != null)
                {
                    int count = _itemCountingFunc != null ? _itemCountingFunc(_owningSource, default(TOutput)!, items) : countReceived;
                    _itemsRemovedAction(_owningSource, count);
                }
#pragma warning disable CS8762 // Parameter may not have a null value when exiting in some condition.
                return true;
#pragma warning restore CS8762
            }
            else return false;
        }

        /// <summary>Gets the number of items available to be received from this block.</summary>
        internal int OutputCount { get { lock (OutgoingLock) lock (ValueLock) return _messages.Count; } }

        /// <summary>
        /// Adds a message to the source block for propagation.
        /// This method must only be used by one thread at a time, and must not be used concurrently
        /// with any other producer side methods, e.g. AddMessages, Complete.
        /// </summary>
        /// <param name="item">The item to be wrapped in a message to be added.</param>
        internal void AddMessage(TOutput item)
        {
            // This method must not take the OutgoingLock, as it will likely be called in situations
            // where an IncomingLock is held.

            if (_decliningPermanently) return;
            _messages.Enqueue(item);

            Interlocked.MemoryBarrier(); // ensure the read of _taskForOutputProcessing doesn't move up before the writes in Enqueue

            if (_taskForOutputProcessing == null)
            {
                // Separated out to enable inlining of AddMessage
                OfferAsyncIfNecessaryWithValueLock();
            }
        }

        /// <summary>
        /// Adds messages to the source block for propagation.
        /// This method must only be used by one thread at a time, and must not be used concurrently
        /// with any other producer side methods, e.g. AddMessage, Complete.
        /// </summary>
        /// <param name="items">The list of items to be wrapped in messages to be added.</param>
        internal void AddMessages(IEnumerable<TOutput> items)
        {
            Debug.Assert(items != null, "Items list must be valid.");

            // This method must not take the OutgoingLock, as it will likely be called in situations
            // where an IncomingLock is held.

            if (_decliningPermanently) return;

            // Special case arrays and lists, for which we can avoid the
            // enumerator allocation that'll result from using a foreach.
            // This also avoids virtual method calls that we'd get if we
            // didn't special case.
            var itemsAsList = items as List<TOutput>;
            if (itemsAsList != null)
            {
                for (int i = 0; i < itemsAsList.Count; i++)
                {
                    _messages.Enqueue(itemsAsList[i]);
                }
            }
            else
            {
                TOutput[]? itemsAsArray = items as TOutput[];
                if (itemsAsArray != null)
                {
                    for (int i = 0; i < itemsAsArray.Length; i++)
                    {
                        _messages.Enqueue(itemsAsArray[i]);
                    }
                }
                else
                {
                    foreach (TOutput item in items)
                    {
                        _messages.Enqueue(item);
                    }
                }
            }

            Interlocked.MemoryBarrier(); // ensure the read of _taskForOutputProcessing doesn't move up before the writes in Enqueue

            if (_taskForOutputProcessing == null)
            {
                OfferAsyncIfNecessaryWithValueLock();
            }
        }

        /// <summary>Adds an individual exception to this source.</summary>
        /// <param name="exception">The exception to add</param>
        internal void AddException(Exception exception)
        {
            Debug.Assert(exception != null, "Valid exception must be provided to be added.");
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
            Debug.Assert(exceptions != null, "Valid exceptions must be provided to be added.");
            Debug.Assert(!Completion.IsCompleted || Completion.IsFaulted, "The block must either not be completed or be faulted if we're still storing exceptions.");
            lock (ValueLock)
            {
                foreach (Exception exception in exceptions)
                {
                    Common.AddException(ref _exceptions, exception);
                }
            }
        }

        /// <summary>Adds the exceptions contained in an AggregateException to this source.</summary>
        /// <param name="aggregateException">The exception to add</param>
        internal void AddAndUnwrapAggregateException(AggregateException aggregateException)
        {
            Debug.Assert(aggregateException != null && aggregateException.InnerExceptions.Count > 0, "Aggregate must be valid and contain inner exceptions to unwrap.");
            Debug.Assert(!Completion.IsCompleted || Completion.IsFaulted, "The block must either not be completed or be faulted if we're still storing exceptions.");
            lock (ValueLock)
            {
                Common.AddException(ref _exceptions, aggregateException, unwrapInnerExceptions: true);
            }
        }

        /// <summary>Gets whether the _exceptions list is non-null.</summary>
        internal bool HasExceptions
        {
            get
            {
                // We may check whether _exceptions is null without taking a lock because it is volatile
                return Volatile.Read(ref _exceptions) != null;
            }
        }

        /// <summary>Informs the block that it will not be receiving additional messages.</summary>
        internal void Complete()
        {
            lock (ValueLock)
            {
                _decliningPermanently = true;

                // CompleteAdding may be called in a context where an incoming lock is held.  We need to
                // call CompleteBlockIfPossible, but we can't do so if the incoming lock is held.
                // However, we know that _decliningPermanently has been set, and thus the timing of
                // CompleteBlockIfPossible doesn't matter, so we schedule it to run asynchronously
                // and take the necessary locks in a situation where we're sure it won't cause a problem.
                Task.Factory.StartNew(state =>
                {
                    var thisSourceCore = (SourceCore<TOutput>)state!;
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

        /// <summary>Gets the DataflowBlockOptions used to configure this block.</summary>
        internal DataflowBlockOptions DataflowBlockOptions { get { return _dataflowBlockOptions; } }

        /// <summary>Offers messages to all targets.</summary>
        /// <param name="linkToTarget">
        /// The newly linked target, if OfferToTargets is being called to synchronously
        /// propagate to a target during a LinkTo operation.
        /// </param>
        private bool OfferToTargets(ITargetBlock<TOutput>? linkToTarget = null)
        {
            Common.ContractAssertMonitorStatus(OutgoingLock, held: true);
            Common.ContractAssertMonitorStatus(ValueLock, held: false);

            // If the next message is reserved, we can't offer anything
            if (_nextMessageReservedFor != null)
                return false;

            // Peek at the next message if there is one, so we can offer it.
            DataflowMessageHeader header = default(DataflowMessageHeader);
            TOutput? message = default(TOutput);
            bool offerJustToLinkToTarget = false;

            // If offering isn't enabled and if we're not doing this as
            // a result of LinkTo, bail. Otherwise, with offering disabled, we must have
            // already offered this message to all existing targets, so we can just offer
            // it to the newly linked target.
            if (!Volatile.Read(ref _enableOffering))
            {
                if (linkToTarget == null) return false;
                else offerJustToLinkToTarget = true;
            }

            // Otherwise, peek at message to offer
            if (_messages.TryPeek(out message))
            {
                header = new DataflowMessageHeader(_nextMessageId.Value);
            }

            // If there is a message, offer it.
            bool messageWasAccepted = false;
            if (header.IsValid)
            {
                if (offerJustToLinkToTarget)
                {
                    // If we've already offered the message to everyone else,
                    // we can just offer it to the newly linked target
                    Debug.Assert(linkToTarget != null, "Must have a valid target to offer to.");
                    OfferMessageToTarget(header, message!, linkToTarget, out messageWasAccepted);
                }
                else
                {
                    // Otherwise, we've not yet offered this message to anyone, so even
                    // if linkToTarget is non-null, we need to propagate the message in order
                    // through all of the registered targets, the last of which will be the linkToTarget
                    // if it's non-null (no need to special-case it, though).

                    // Note that during OfferMessageToTarget, a target may call ConsumeMessage (taking advantage of the
                    // reentrancy of OutgoingLock), which may unlink the target if the target is registered as "unlinkAfterOne".
                    // Doing so will remove the target from the targets list. As such, we maintain the next node
                    // separately from cur.Next, in case cur.Next changes by cur being removed from the list.
                    // No other node in the list should change, as we're protected by OutgoingLock.

                    TargetRegistry<TOutput>.LinkedTargetInfo? cur = _targetRegistry.FirstTargetNode;
                    while (cur != null)
                    {
                        TargetRegistry<TOutput>.LinkedTargetInfo? next = cur.Next;
                        if (OfferMessageToTarget(header, message!, cur.Target, out messageWasAccepted)) break;
                        cur = next;
                    }

                    // If none of the targets accepted the message, disable offering.
                    if (!messageWasAccepted)
                    {
                        lock (ValueLock)
                        {
                            _enableOffering = false;
                        }
                    }
                }
            }

            // If a message got accepted, consume it and reenable offering.
            if (messageWasAccepted)
            {
                lock (ValueLock)
                {
                    // SourceCore set consumeToAccept to false.  However, it's possible
                    // that an incorrectly written target may ignore that parameter and synchronously consume
                    // even though they weren't supposed to.  To recover from that,
                    // we'll only dequeue if the correct message is still at the head of the queue.
                    // However, we'll assert so that we can at least catch this in our own debug builds.
                    TOutput? dropped;
                    if (_nextMessageId.Value != header.Id ||
                        !_messages.TryDequeue(out dropped)) // remove the next message
                    {
                        Debug.Assert(false, "The target did not follow the protocol.");
                    }
                    _nextMessageId.Value++;

                    // The message was accepted, so there's now going to be a new next message.
                    // If offering had been disabled, reenable it.
                    _enableOffering = true;

                    // Now that a message has been removed, we need to complete if possible or
                    // or asynchronously offer if necessary.  However, if we're calling this as part of our
                    // offering loop, we won't be able to do either, since by definition there's already
                    // a processing task spun up (us) that would prevent these things.  So we only
                    // do the checks if we're being called to link a new target rather than as part
                    // of normal processing.
                    if (linkToTarget != null)
                    {
                        CompleteBlockIfPossible();
                        OfferAsyncIfNecessary(isReplacementReplica: false, outgoingLockKnownAcquired: true);
                    }
                }

                // Notify the owner block that our count has decreased
                if (_itemsRemovedAction != null)
                {
                    int count = _itemCountingFunc != null ? _itemCountingFunc(_owningSource, message!, null) : 1;
                    _itemsRemovedAction(_owningSource, count);
                }
            }

            return messageWasAccepted;
        }

        /// <summary>Offers the message to the target.</summary>
        /// <param name="header">The header of the message to offer.</param>
        /// <param name="message">The message being offered.</param>
        /// <param name="target">The single target to which the message should be offered.</param>
        /// <param name="messageWasAccepted">true if the message was accepted by the target; otherwise, false.</param>
        /// <returns>
        /// true if the message should not be offered to additional targets;
        /// false if propagation should be allowed to continue.
        /// </returns>
        private bool OfferMessageToTarget(
            DataflowMessageHeader header, TOutput message, ITargetBlock<TOutput> target,
            out bool messageWasAccepted)
        {
            Debug.Assert(target != null, "Valid target to offer to is required.");
            Common.ContractAssertMonitorStatus(OutgoingLock, held: true);
            Common.ContractAssertMonitorStatus(ValueLock, held: false);

            DataflowMessageStatus result = target.OfferMessage(header, message, _owningSource, consumeToAccept: false);
            Debug.Assert(result != DataflowMessageStatus.NotAvailable, "Messages are not being offered concurrently, so nothing should be missed.");
            messageWasAccepted = false;

            // If accepted, note it, and if the target was linked as "once", remove it
            if (result == DataflowMessageStatus.Accepted)
            {
                _targetRegistry.Remove(target, onlyIfReachedMaxMessages: true);
                messageWasAccepted = true;
                return true; // the message should not be offered to anyone else
            }
            // If declined permanently, remove the target
            else if (result == DataflowMessageStatus.DecliningPermanently)
            {
                _targetRegistry.Remove(target);
            }
            // If the message was reserved by the target, stop propagating
            else if (_nextMessageReservedFor != null)
            {
                Debug.Assert(result == DataflowMessageStatus.Postponed,
                    "If the message was reserved, it should also have been postponed.");
                return true; // the message should not be offered to anyone else
            }
            // If the result was Declined, there's nothing more to be done.
            // This message will sit at the front of the queue until someone claims it.

            return false; // allow the message to be offered to someone else
        }

        /// <summary>
        /// Called when we want to enable asynchronously offering message to targets.
        /// Takes the ValueLock before delegating to OfferAsyncIfNecessary.
        /// </summary>
        private void OfferAsyncIfNecessaryWithValueLock()
        {
            lock (ValueLock)
            {
                OfferAsyncIfNecessary(isReplacementReplica: false, outgoingLockKnownAcquired: false);
            }
        }

        /// <summary>Called when we want to enable asynchronously offering message to targets.</summary>
        /// <param name="isReplacementReplica">Whether this call is the continuation of a previous message loop.</param>
        /// <param name="outgoingLockKnownAcquired">Whether the caller is sure that the outgoing lock is currently held by this thread.</param>
        private void OfferAsyncIfNecessary(bool isReplacementReplica, bool outgoingLockKnownAcquired)
        {
            Common.ContractAssertMonitorStatus(ValueLock, held: true);

            // Fast path to enable OfferAsyncIfNecessary to be inlined.  We only need
            // to proceed if there's no task processing, offering is enabled, and
            // there are no messages to be processed.
            if (_taskForOutputProcessing == null && _enableOffering && !_messages.IsEmpty)
            {
                // Slow path: do additional checks and potentially launch new task
                OfferAsyncIfNecessary_Slow(isReplacementReplica, outgoingLockKnownAcquired);
            }
        }

        /// <summary>Called when we want to enable asynchronously offering message to targets.</summary>
        /// <param name="isReplacementReplica">Whether this call is the continuation of a previous message loop.</param>
        /// <param name="outgoingLockKnownAcquired">Whether the caller is sure that the outgoing lock is currently held by this thread.</param>
        private void OfferAsyncIfNecessary_Slow(bool isReplacementReplica, bool outgoingLockKnownAcquired)
        {
            Common.ContractAssertMonitorStatus(ValueLock, held: true);
            Debug.Assert(_taskForOutputProcessing == null && _enableOffering && !_messages.IsEmpty,
                "The block must be enabled for offering, not currently be processing, and have messages available to process.");

            // This method must not take the outgoing lock, as it will likely be called in situations
            // where a derived type's incoming lock is held.

            bool targetsAvailable = true;
            if (outgoingLockKnownAcquired || Monitor.IsEntered(OutgoingLock))
            {
                Common.ContractAssertMonitorStatus(OutgoingLock, held: true);
                targetsAvailable = _targetRegistry.FirstTargetNode != null;
            }

            // If there's any work to be done...
            if (targetsAvailable && !CanceledOrFaulted)
            {
                // Create task and store into _taskForOutputProcessing prior to scheduling the task
                // so that _taskForOutputProcessing will be visibly set in the task loop.
                _taskForOutputProcessing = new Task(thisSourceCore => ((SourceCore<TOutput>)thisSourceCore!).OfferMessagesLoopCore(), this,
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
                    _taskForOutputProcessing = null;
                    _decliningPermanently = true;

                    // Get out from under currently held locks - ValueLock is taken, but OutgoingLock may not be.
                    // Re-take the locks on a separate thread.
                    Task.Factory.StartNew(state =>
                    {
                        var thisSourceCore = (SourceCore<TOutput>)state!;
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
            Debug.Assert(_taskForOutputProcessing != null && _taskForOutputProcessing.Id == Task.CurrentId,
                "Must be part of the current processing task.");
            try
            {
                int maxMessagesPerTask = _dataflowBlockOptions.ActualMaxMessagesPerTask;

                // We need to hold the outgoing lock while offering messages.  We can either
                // lock and unlock for each individual offering, or we can lock around multiple or all
                // possible offerings.  The former ensures that other operations don't get starved,
                // while the latter is much more efficient (not continually acquiring and releasing
                // the lock).  For blocks that aren't linked to any targets, this won't matter
                // (no offering is done), and for blocks that are only linked to targets, this shouldn't
                // matter (no one is contending for the lock), thus
                // the only case it would matter is when a block both has targets and is being
                // explicitly received from, which is an uncommon scenario.  Thus, we want to lock
                // around the whole thing to improve performance, but just in case we do hit
                // an uncommon scenario, in the default case we release the lock every now and again.
                // If a developer wants to control this, they can limit the duration of the
                // lock by using MaxMessagesPerTask.

                const int DEFAULT_RELEASE_LOCK_ITERATIONS = 10; // Dialable
                int releaseLockIterations =
                    _dataflowBlockOptions.MaxMessagesPerTask == DataflowBlockOptions.Unbounded ?
                        DEFAULT_RELEASE_LOCK_ITERATIONS : maxMessagesPerTask;

                for (int messageCounter = 0;
                    messageCounter < maxMessagesPerTask && !CanceledOrFaulted;)
                {
                    lock (OutgoingLock)
                    {
                        // While there are more messages to process, offer each in turn
                        // to the targets.  If we're unable to propagate a particular message,
                        // stop trying until something changes in the future.
                        for (
                            int lockReleaseCounter = 0;
                            messageCounter < maxMessagesPerTask && lockReleaseCounter < releaseLockIterations && !CanceledOrFaulted;
                            ++messageCounter, ++lockReleaseCounter)
                        {
                            if (!OfferToTargets()) return;
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                // Record the exception
                AddException(exc);

                // Notify the owning block it should stop accepting new messages
                _completeAction(_owningSource);
            }
            finally
            {
                lock (OutgoingLock)
                {
                    lock (ValueLock)
                    {
                        // We're no longer processing, so null out the processing task
                        Debug.Assert(_taskForOutputProcessing != null && _taskForOutputProcessing.Id == Task.CurrentId,
                            "Must be part of the current processing task.");
                        _taskForOutputProcessing = null;
                        Interlocked.MemoryBarrier(); // synchronize with AddMessage(s) and its read of _taskForOutputProcessing

                        // However, we may have given up early because we hit our own configured
                        // processing limits rather than because we ran out of work to do.  If that's
                        // the case, make sure we spin up another task to keep going.
                        OfferAsyncIfNecessary(isReplacementReplica: true, outgoingLockKnownAcquired: true);

                        // If, however, we stopped because we ran out of work to do and we
                        // know we'll never get more, then complete.
                        CompleteBlockIfPossible();
                    }
                }
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
                    (HasExceptions && _decliningPermanently);
            }
        }

        /// <summary>Completes the block's processing if there's nothing left to do and never will be.</summary>
        private void CompleteBlockIfPossible()
        {
            Common.ContractAssertMonitorStatus(OutgoingLock, held: true);
            Common.ContractAssertMonitorStatus(ValueLock, held: true);

            if (!_completionReserved)
            {
                if (_decliningPermanently && // declining permanently, so no more messages will arrive
                    _taskForOutputProcessing == null && // no current processing
                    _nextMessageReservedFor == null) // no pending reservation
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
            Debug.Assert(
                _decliningPermanently && _taskForOutputProcessing == null && _nextMessageReservedFor == null,
                "The block must be declining permanently, there must be no reservations, and there must be no processing tasks");
            Common.ContractAssertMonitorStatus(OutgoingLock, held: true);
            Common.ContractAssertMonitorStatus(ValueLock, held: true);

            if (_messages.IsEmpty || CanceledOrFaulted)
            {
                _completionReserved = true;

                // Get out from under currently held locks.  This is to avoid
                // invoking synchronous continuations off of _completionTask.Task
                // while holding a lock.
                Task.Factory.StartNew(state => ((SourceCore<TOutput>)state!).CompleteBlockOncePossible(),
                    this, CancellationToken.None, Common.GetCreationOptionsForTask(), TaskScheduler.Default);
            }
        }

        /// <summary>
        /// Completes the block.  This must only be called once, and only once all of the completion conditions are met.
        /// As such, it must only be called from CompleteBlockIfPossible.
        /// </summary>
        private void CompleteBlockOncePossible()
        {
            TargetRegistry<TOutput>.LinkedTargetInfo? linkedTargets;
            List<Exception>? exceptions;

            // Avoid completing while the code that caused this completion to occur is still holding a lock.
            // Clear out the target registry and buffers to help avoid memory leaks.
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

            // If it's due to an unhandled exception, finish in an error state
            if (exceptions != null)
            {
                _completionTask.TrySetException(exceptions);
            }
            // If it's due to cancellation, finish in a canceled state
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
            private readonly SourceCore<TOutput> _source;

            /// <summary>Initializes the type proxy.</summary>
            /// <param name="source">The source being viewed.</param>
            internal DebuggingInformation(SourceCore<TOutput> source) { _source = source; }

            /// <summary>Gets the number of messages available for receiving.</summary>
            internal int OutputCount { get { return _source._messages.Count; } }
            /// <summary>Gets the messages available for receiving.</summary>
            internal IEnumerable<TOutput> OutputQueue { get { return _source._messages.ToList(); } }
            /// <summary>Gets the task being used for output processing.</summary>
            internal Task? TaskForOutputProcessing { get { return _source._taskForOutputProcessing; } }

            /// <summary>Gets the DataflowBlockOptions used to configure this block.</summary>
            internal DataflowBlockOptions DataflowBlockOptions { get { return _source._dataflowBlockOptions; } }

            /// <summary>Gets whether the block is completed.</summary>
            internal bool IsCompleted { get { return _source.Completion.IsCompleted; } }

            /// <summary>Gets the set of all targets linked from this block.</summary>
            internal TargetRegistry<TOutput> LinkedTargets { get { return _source._targetRegistry; } }
            /// <summary>Gets the target that holds a reservation on the next message, if any.</summary>
            internal ITargetBlock<TOutput>? NextMessageReservedFor { get { return _source._nextMessageReservedFor; } }
        }
    }
}
