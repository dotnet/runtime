// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// WriteOnceBlock.cs
//
//
// A propagator block capable of receiving and storing only one message, ever.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Threading.Tasks.Dataflow.Internal;

namespace System.Threading.Tasks.Dataflow
{
    /// <summary>Provides a buffer for receiving and storing at most one element in a network of dataflow blocks.</summary>
    /// <typeparam name="T">Specifies the type of the data buffered by this dataflow block.</typeparam>
    [DebuggerDisplay("{DebuggerDisplayContent,nq}")]
    [DebuggerTypeProxy(typeof(WriteOnceBlock<>.DebugView))]
    public sealed class WriteOnceBlock<T> : IPropagatorBlock<T, T>, IReceivableSourceBlock<T>, IDebuggerDisplay
    {
        /// <summary>A registry used to store all linked targets and information about them.</summary>
        private readonly TargetRegistry<T> _targetRegistry;
        /// <summary>The cloning function.</summary>
        private readonly Func<T, T>? _cloningFunction;
        /// <summary>The options used to configure this block's execution.</summary>
        private readonly DataflowBlockOptions _dataflowBlockOptions;
        /// <summary>Lazily initialized task completion source that produces the actual completion task when needed.</summary>
        private TaskCompletionSource<VoidResult>? _lazyCompletionTaskSource;
        /// <summary>Whether all future messages should be declined.</summary>
        private bool _decliningPermanently;
        /// <summary>Whether block completion is disallowed.</summary>
        private bool _completionReserved;
        /// <summary>The header of the singly-assigned value.</summary>
        private DataflowMessageHeader _header;
        /// <summary>The singly-assigned value.</summary>
        private T? _value;

        /// <summary>Gets the object used as the value lock.</summary>
        private object ValueLock { get { return _targetRegistry; } }

        /// <summary>Initializes the <see cref="WriteOnceBlock{T}"/>.</summary>
        /// <param name="cloningFunction">
        /// The function to use to clone the data when offered to other blocks.
        /// This may be null to indicate that no cloning need be performed.
        /// </param>
        public WriteOnceBlock(Func<T, T>? cloningFunction) :
            this(cloningFunction, DataflowBlockOptions.Default)
        { }

        /// <summary>Initializes the <see cref="WriteOnceBlock{T}"/> with the specified <see cref="DataflowBlockOptions"/>.</summary>
        /// <param name="cloningFunction">
        /// The function to use to clone the data when offered to other blocks.
        /// This may be null to indicate that no cloning need be performed.
        /// </param>
        /// <param name="dataflowBlockOptions">The options with which to configure this <see cref="WriteOnceBlock{T}"/>.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="dataflowBlockOptions"/> is null (Nothing in Visual Basic).</exception>
        public WriteOnceBlock(Func<T, T>? cloningFunction, DataflowBlockOptions dataflowBlockOptions)
        {
            // Validate arguments
            if (dataflowBlockOptions == null) throw new ArgumentNullException(nameof(dataflowBlockOptions));

            // Store the option
            _cloningFunction = cloningFunction;
            _dataflowBlockOptions = dataflowBlockOptions.DefaultOrClone();

            // The target registry also serves as our ValueLock,
            // and thus must always be initialized, even if the block is pre-canceled, as
            // subsequent usage of the block may run through code paths that try to take this lock.
            _targetRegistry = new TargetRegistry<T>(this);

            // If a cancelable CancellationToken has been passed in,
            // we need to initialize the completion task's TCS now.
            if (dataflowBlockOptions.CancellationToken.CanBeCanceled)
            {
                _lazyCompletionTaskSource = new TaskCompletionSource<VoidResult>();

                // If we've already had cancellation requested, do as little work as we have to
                // in order to be done.
                if (dataflowBlockOptions.CancellationToken.IsCancellationRequested)
                {
                    _completionReserved = _decliningPermanently = true;

                    // Cancel the completion task's TCS
                    _lazyCompletionTaskSource.SetCanceled();
                }
                else
                {
                    // Handle async cancellation requests by declining on the target
                    Common.WireCancellationToComplete(
                        dataflowBlockOptions.CancellationToken, _lazyCompletionTaskSource.Task, state => ((WriteOnceBlock<T>)state!).Complete(), this);
                }
            }
#if FEATURE_TRACING
            DataflowEtwProvider etwLog = DataflowEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.DataflowBlockCreated(this, dataflowBlockOptions);
            }
#endif
        }

        /// <summary>Asynchronously completes the block on another task.</summary>
        /// <remarks>
        /// This must only be called once all of the completion conditions are met.
        /// </remarks>
        private void CompleteBlockAsync(IList<Exception>? exceptions)
        {
            Debug.Assert(_decliningPermanently, "We may get here only after we have started to decline permanently.");
            Debug.Assert(_completionReserved, "We may get here only after we have reserved completion.");
            Common.ContractAssertMonitorStatus(ValueLock, held: false);

            // If there is no exceptions list, we offer the message around, and then complete.
            // If there is an exception list, we complete without offering the message.
            if (exceptions == null)
            {
                // Offer the message to any linked targets and complete the block asynchronously to avoid blocking the caller
                var taskForOutputProcessing = new Task(state => ((WriteOnceBlock<T>)state!).OfferToTargetsAndCompleteBlock(), this,
                                                        Common.GetCreationOptionsForTask());

#if FEATURE_TRACING
                DataflowEtwProvider etwLog = DataflowEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.TaskLaunchedForMessageHandling(
                        this, taskForOutputProcessing, DataflowEtwProvider.TaskLaunchedReason.OfferingOutputMessages, _header.IsValid ? 1 : 0);
                }
#endif

                // Start the task handling scheduling exceptions
                Exception? exception = Common.StartTaskSafe(taskForOutputProcessing, _dataflowBlockOptions.TaskScheduler);
                if (exception != null) CompleteCore(exception, storeExceptionEvenIfAlreadyCompleting: true);
            }
            else
            {
                // Complete the block asynchronously to avoid blocking the caller
                Task.Factory.StartNew(state =>
                {
                    Tuple<WriteOnceBlock<T>, IList<Exception>> blockAndList = (Tuple<WriteOnceBlock<T>, IList<Exception>>)state!;
                    blockAndList.Item1.CompleteBlock(blockAndList.Item2);
                },
                Tuple.Create(this, exceptions), CancellationToken.None, Common.GetCreationOptionsForTask(), TaskScheduler.Default);
            }
        }

        /// <summary>Offers the message and completes the block.</summary>
        /// <remarks>
        /// This is called only once.
        /// </remarks>
        private void OfferToTargetsAndCompleteBlock()
        {
            // OfferToTargets calls to potentially multiple targets, each of which
            // could be faulty and throw an exception.  OfferToTargets creates a
            // list of all such exceptions and returns it.
            // If _value is null, OfferToTargets does nothing.
            List<Exception>? exceptions = OfferToTargets();
            CompleteBlock(exceptions);
        }

        /// <summary>Completes the block.</summary>
        /// <remarks>
        /// This is called only once.
        /// </remarks>
        private void CompleteBlock(IList<Exception>? exceptions)
        {
            // Do not invoke the CompletionTaskSource property if there is a chance that _lazyCompletionTaskSource
            // has not been initialized yet and we may have to complete normally, because that would defeat the
            // sole purpose of the TCS being lazily initialized.

            Debug.Assert(_lazyCompletionTaskSource == null || !_lazyCompletionTaskSource.Task.IsCompleted, "The task completion source must not be completed. This must be the only thread that ever completes the block.");

            // Save the linked list of targets so that it could be traversed later to propagate completion
            TargetRegistry<T>.LinkedTargetInfo? linkedTargets = _targetRegistry.ClearEntryPoints();

            // Complete the block's completion task
            if (exceptions != null && exceptions.Count > 0)
            {
                CompletionTaskSource.TrySetException(exceptions);
            }
            else if (_dataflowBlockOptions.CancellationToken.IsCancellationRequested)
            {
                CompletionTaskSource.TrySetCanceled();
            }
            else
            {
                // Safely try to initialize the completion task's TCS with a cached completed TCS.
                // If our attempt succeeds (CompareExchange returns null), we have nothing more to do.
                // If the completion task's TCS was already initialized (CompareExchange returns non-null),
                // we have to complete that TCS instance.
                if (Interlocked.CompareExchange(ref _lazyCompletionTaskSource, Common.CompletedVoidResultTaskCompletionSource, null) != null)
                {
                    _lazyCompletionTaskSource.TrySetResult(default(VoidResult));
                }
            }

            // Now that the completion task is completed, we may propagate completion to the linked targets
            _targetRegistry.PropagateCompletion(linkedTargets);
#if FEATURE_TRACING
            DataflowEtwProvider etwLog = DataflowEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.DataflowBlockCompleted(this);
            }
#endif
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Fault"]/*' />
        void IDataflowBlock.Fault(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));

            CompleteCore(exception, storeExceptionEvenIfAlreadyCompleting: false);
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Complete"]/*' />
        public void Complete()
        {
            CompleteCore(exception: null, storeExceptionEvenIfAlreadyCompleting: false);
        }

        private void CompleteCore(Exception? exception, bool storeExceptionEvenIfAlreadyCompleting)
        {
            Debug.Assert(exception != null || !storeExceptionEvenIfAlreadyCompleting,
                            "When storeExceptionEvenIfAlreadyCompleting is set to true, an exception must be provided.");

            bool thisThreadReservedCompletion = false;
            lock (ValueLock)
            {
                // Faulting from outside is allowed until we start declining permanently
                if (_decliningPermanently && !storeExceptionEvenIfAlreadyCompleting) return;

                // Decline further messages
                _decliningPermanently = true;

                // Reserve Completion.
                // If storeExceptionEvenIfAlreadyCompleting is true, we are here to fault the block,
                // because we couldn't launch the offer-and-complete task.
                // We have to retry to just complete. We do that by pretending completion wasn't reserved.
                if (!_completionReserved || storeExceptionEvenIfAlreadyCompleting) thisThreadReservedCompletion = _completionReserved = true;
            }

            // This call caused us to start declining further messages,
            // there's nothing more this block needs to do... complete it if we just reserved completion.
            if (thisThreadReservedCompletion)
            {
                List<Exception>? exceptions = null;
                if (exception != null)
                {
                    exceptions = new List<Exception>();
                    exceptions.Add(exception);
                }

                CompleteBlockAsync(exceptions);
            }
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="TryReceive"]/*' />
        public bool TryReceive(Predicate<T>? filter, [MaybeNullWhen(false)] out T item)
        {
            // No need to take the outgoing lock, as we don't need to synchronize with other
            // targets, and _value only ever goes from null to non-null, not the other way around.

            // If we have a value, give it up.  All receives on a successfully
            // completed WriteOnceBlock will return true, as long as the message
            // passes the filter (all messages pass a null filter).
            if (_header.IsValid && (filter == null || filter(_value!)))
            {
                item = CloneItem(_value!);
                return true;
            }
            // Otherwise, nothing to receive
            else
            {
                item = default(T);
                return false;
            }
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="TryReceiveAll"]/*' />
        bool IReceivableSourceBlock<T>.TryReceiveAll([NotNullWhen(true)] out IList<T>? items)
        {
            // Try to receive the one item this block may have.
            // If we can, give back an array of one item. Otherwise,
            // give back null.
            T? item;
            if (TryReceive(null, out item))
            {
                items = new T[] { item };
                return true;
            }
            else
            {
                items = null;
                return false;
            }
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="LinkTo"]/*' />
        public IDisposable LinkTo(ITargetBlock<T> target, DataflowLinkOptions linkOptions)
        {
            // Validate arguments
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (linkOptions == null) throw new ArgumentNullException(nameof(linkOptions));

            bool hasValue;
            bool isCompleted;
            lock (ValueLock)
            {
                hasValue = HasValue;
                isCompleted = _completionReserved;

                // If we haven't gotten a value yet and the block is not complete, add the target and bail
                if (!hasValue && !isCompleted)
                {
                    _targetRegistry.Add(ref target, linkOptions);
                    return Common.CreateUnlinker(ValueLock, _targetRegistry, target);
                }
            }

            // If we already have a value, send it along to the linking target
            if (hasValue)
            {
                bool useCloning = _cloningFunction != null;
                target.OfferMessage(_header, _value!, this, consumeToAccept: useCloning);
            }

            // If completion propagation has been requested, do it safely.
            // The Completion property will ensure the lazy TCS is initialized.
            if (linkOptions.PropagateCompletion) Common.PropagateCompletionOnceCompleted(Completion, target);

            return Disposables.Nop;
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Completion"]/*' />
        public Task Completion { get { return CompletionTaskSource.Task; } }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Targets/Member[@name="OfferMessage"]/*' />
        DataflowMessageStatus ITargetBlock<T>.OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T>? source, bool consumeToAccept)
        {
            // Validate arguments
            if (!messageHeader.IsValid) throw new ArgumentException(SR.Argument_InvalidMessageHeader, nameof(messageHeader));
            if (source == null && consumeToAccept) throw new ArgumentException(SR.Argument_CantConsumeFromANullSource, nameof(consumeToAccept));

            bool thisThreadReservedCompletion = false;
            lock (ValueLock)
            {
                // If we are declining messages, bail
                if (_decliningPermanently) return DataflowMessageStatus.DecliningPermanently;

                // Consume the message from the source if necessary. We do this while holding ValueLock to prevent multiple concurrent
                // offers from all succeeding.
                if (consumeToAccept)
                {
                    bool consumed;
                    messageValue = source!.ConsumeMessage(messageHeader, this, out consumed)!;
                    if (!consumed) return DataflowMessageStatus.NotAvailable;
                }

                // Update the header and the value
                _header = Common.SingleMessageHeader;
                _value = messageValue;

                // We got what we needed. Start declining permanently.
                _decliningPermanently = true;

                // Reserve Completion
                if (!_completionReserved) thisThreadReservedCompletion = _completionReserved = true;
            }

            // Since this call to OfferMessage succeeded (and only one can ever), complete the block
            // (but asynchronously so as not to block the Post call while offering to
            // targets, running synchronous continuations off of the completion task, etc.)
            if (thisThreadReservedCompletion) CompleteBlockAsync(exceptions: null);
            return DataflowMessageStatus.Accepted;
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="ConsumeMessage"]/*' />
        T? ISourceBlock<T>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<T> target, out bool messageConsumed)
        {
            // Validate arguments
            if (!messageHeader.IsValid) throw new ArgumentException(SR.Argument_InvalidMessageHeader, nameof(messageHeader));
            if (target == null) throw new ArgumentNullException(nameof(target));

            // As long as the message being requested is the one we have, allow it to be consumed,
            // but make a copy using the provided cloning function.
            if (_header.Id == messageHeader.Id)
            {
                messageConsumed = true;
                return CloneItem(_value!);
            }
            else
            {
                messageConsumed = false;
                return default;
            }
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="ReserveMessage"]/*' />
        bool ISourceBlock<T>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<T> target)
        {
            // Validate arguments
            if (!messageHeader.IsValid) throw new ArgumentException(SR.Argument_InvalidMessageHeader, nameof(messageHeader));
            if (target == null) throw new ArgumentNullException(nameof(target));

            // As long as the message is the one we have, it can be "reserved."
            // Reservations on a WriteOnceBlock are not exclusive, because
            // everyone who wants a copy can get one.
            return _header.Id == messageHeader.Id;
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="ReleaseReservation"]/*' />
        void ISourceBlock<T>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<T> target)
        {
            // Validate arguments
            if (!messageHeader.IsValid) throw new ArgumentException(SR.Argument_InvalidMessageHeader, nameof(messageHeader));
            if (target == null) throw new ArgumentNullException(nameof(target));

            // As long as the message is the one we have, everything's fine.
            if (_header.Id != messageHeader.Id) throw new InvalidOperationException(SR.InvalidOperation_MessageNotReservedByTarget);

            // In other blocks, upon release we typically re-offer the message to all linked targets.
            // We need to do the same thing for WriteOnceBlock, in order to account for cases where the block
            // may be linked to a join or similar block, such that the join could never again be satisfied
            // if it didn't receive another offer from this source.  However, since the message is broadcast
            // and all targets can get a copy, we don't need to broadcast to all targets, only to
            // the target that released the message.  Note that we don't care whether it's accepted
            // or not, nor do we care about any exceptions which may emerge (they should just propagate).
            Debug.Assert(_header.IsValid, "A valid header is required.");
            bool useCloning = _cloningFunction != null;
            target.OfferMessage(_header, _value!, this, consumeToAccept: useCloning);
        }

        /// <summary>Clones the item.</summary>
        /// <param name="item">The item to clone.</param>
        /// <returns>The cloned item.</returns>
        private T CloneItem(T item)
        {
            return _cloningFunction != null ?
                _cloningFunction(item) :
                item;
        }

        /// <summary>Offers the WriteOnceBlock's message to all targets.</summary>
        private List<Exception>? OfferToTargets()
        {
            Common.ContractAssertMonitorStatus(ValueLock, held: false);

            // If there is a message, offer it to everyone.  Return values
            // don't matter, because we only get one message and then complete,
            // and everyone who wants a copy can get a copy.
            List<Exception>? exceptions = null;
            if (HasValue)
            {
                TargetRegistry<T>.LinkedTargetInfo? cur = _targetRegistry.FirstTargetNode;
                while (cur != null)
                {
                    TargetRegistry<T>.LinkedTargetInfo? next = cur.Next;
                    ITargetBlock<T> target = cur.Target;
                    try
                    {
                        // Offer the message.  If there's a cloning function, we force the target to
                        // come back to us to consume the message, allowing us the opportunity to run
                        // the cloning function once we know they want the data.  If there is no cloning
                        // function, there's no reason for them to call back here.
                        bool useCloning = _cloningFunction != null;
                        target.OfferMessage(_header, _value!, this, consumeToAccept: useCloning);
                    }
                    catch (Exception exc)
                    {
                        // Track any erroneous exceptions that may occur
                        // and return them to the caller so that they may
                        // be logged in the completion task.
                        Common.StoreDataflowMessageValueIntoExceptionData(exc, _value!);
                        Common.AddException(ref exceptions, exc);
                    }
                    cur = next;
                }
            }
            return exceptions;
        }

        /// <summary>Ensures the completion task's TCS is initialized.</summary>
        /// <returns>The completion task's TCS.</returns>
        private TaskCompletionSource<VoidResult> CompletionTaskSource
        {
            get
            {
                // If the completion task's TCS has not been initialized by now, safely try to initialize it.
                // It is very important that once a completion task/source instance has been handed out,
                // it remains the block's completion task.
                if (_lazyCompletionTaskSource == null)
                {
                    Interlocked.CompareExchange(ref _lazyCompletionTaskSource, new TaskCompletionSource<VoidResult>(), null);
                }

                return _lazyCompletionTaskSource;
            }
        }

        /// <summary>Gets whether the block is storing a value.</summary>
        private bool HasValue { get { return _header.IsValid; } }
        /// <summary>Gets the value being stored by the block.</summary>
        private T? Value { get { return _header.IsValid ? _value : default(T); } }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="ToString"]/*' />
        public override string ToString() { return Common.GetNameForDebugger(this, _dataflowBlockOptions); }

        /// <summary>The data to display in the debugger display attribute.</summary>
        private object DebuggerDisplayContent
        {
            get
            {
                return string.Format("{0}, HasValue={1}, Value={2}",
                    Common.GetNameForDebugger(this, _dataflowBlockOptions), HasValue, Value);
            }
        }
        /// <summary>Gets the data to display in the debugger display attribute for this instance.</summary>
        object IDebuggerDisplay.Content { get { return DebuggerDisplayContent; } }

        /// <summary>Provides a debugger type proxy for WriteOnceBlock.</summary>
        private sealed class DebugView
        {
            /// <summary>The WriteOnceBlock being viewed.</summary>
            private readonly WriteOnceBlock<T> _writeOnceBlock;

            /// <summary>Initializes the debug view.</summary>
            /// <param name="writeOnceBlock">The WriteOnceBlock to view.</param>
            public DebugView(WriteOnceBlock<T> writeOnceBlock)
            {
                Debug.Assert(writeOnceBlock != null, "Need a block with which to construct the debug view.");
                _writeOnceBlock = writeOnceBlock;
            }

            /// <summary>Gets whether the WriteOnceBlock has completed.</summary>
            public bool IsCompleted { get { return _writeOnceBlock.Completion.IsCompleted; } }
            /// <summary>Gets the block's Id.</summary>
            public int Id { get { return Common.GetBlockId(_writeOnceBlock); } }

            /// <summary>Gets whether the WriteOnceBlock has a value.</summary>
            public bool HasValue { get { return _writeOnceBlock.HasValue; } }
            /// <summary>Gets the WriteOnceBlock's value if it has one, or default(T) if it doesn't.</summary>
            public T? Value { get { return _writeOnceBlock.Value; } }

            /// <summary>Gets the DataflowBlockOptions used to configure this block.</summary>
            public DataflowBlockOptions DataflowBlockOptions { get { return _writeOnceBlock._dataflowBlockOptions; } }
            /// <summary>Gets the set of all targets linked from this block.</summary>
            public TargetRegistry<T> LinkedTargets { get { return _writeOnceBlock._targetRegistry; } }
        }
    }
}
