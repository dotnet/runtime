// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// DataflowBlock.cs
//
//
// Common functionality for ITargetBlock, ISourceBlock, and IPropagatorBlock.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Dataflow.Internal;

#if USE_INTERNAL_THREADING
using System.Threading.Tasks.Dataflow.Internal.Threading;
#endif

namespace System.Threading.Tasks.Dataflow
{
    /// <summary>
    /// Provides a set of static (Shared in Visual Basic) methods for working with dataflow blocks.
    /// </summary>
    public static class DataflowBlock
    {
        #region LinkTo
        /// <summary>Links the <see cref="ISourceBlock{TOutput}"/> to the specified <see cref="ITargetBlock{TOutput}"/>.</summary>
        /// <param name="source">The source from which to link.</param>
        /// <param name="target">The <see cref="ITargetBlock{TOutput}"/> to which to connect the source.</param>
        /// <returns>An IDisposable that, upon calling Dispose, will unlink the source from the target.</returns>
        /// <exception cref="System.ArgumentNullException">The <paramref name="source"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="target"/> is null (Nothing in Visual Basic).</exception>
        public static IDisposable LinkTo<TOutput>(
            this ISourceBlock<TOutput> source,
            ITargetBlock<TOutput> target)
        {
            // Validate arguments
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));

            // This method exists purely to pass default DataflowLinkOptions
            // to increase usability of the "90%" case.
            return source.LinkTo(target, DataflowLinkOptions.Default);
        }

        /// <summary>Links the <see cref="ISourceBlock{TOutput}"/> to the specified <see cref="ITargetBlock{TOutput}"/> using the specified filter.</summary>
        /// <param name="source">The source from which to link.</param>
        /// <param name="target">The <see cref="ITargetBlock{TOutput}"/> to which to connect the source.</param>
        /// <param name="predicate">The filter a message must pass in order for it to propagate from the source to the target.</param>
        /// <returns>An IDisposable that, upon calling Dispose, will unlink the source from the target.</returns>
        /// <exception cref="System.ArgumentNullException">The <paramref name="source"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="target"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="predicate"/> is null (Nothing in Visual Basic).</exception>
        public static IDisposable LinkTo<TOutput>(
            this ISourceBlock<TOutput> source,
            ITargetBlock<TOutput> target,
            Predicate<TOutput> predicate)
        {
            // All argument validation handled by delegated method.
            return LinkTo(source, target, DataflowLinkOptions.Default, predicate);
        }

        /// <summary>Links the <see cref="ISourceBlock{TOutput}"/> to the specified <see cref="ITargetBlock{TOutput}"/> using the specified filter.</summary>
        /// <param name="source">The source from which to link.</param>
        /// <param name="target">The <see cref="ITargetBlock{TOutput}"/> to which to connect the source.</param>
        /// <param name="predicate">The filter a message must pass in order for it to propagate from the source to the target.</param>
        /// <param name="linkOptions">The options to use to configure the link.</param>
        /// <returns>An IDisposable that, upon calling Dispose, will unlink the source from the target.</returns>
        /// <exception cref="System.ArgumentNullException">The <paramref name="source"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="target"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="linkOptions"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="predicate"/> is null (Nothing in Visual Basic).</exception>
        public static IDisposable LinkTo<TOutput>(
            this ISourceBlock<TOutput> source,
            ITargetBlock<TOutput> target,
            DataflowLinkOptions linkOptions,
            Predicate<TOutput> predicate)
        {
            // Validate arguments
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (linkOptions == null) throw new ArgumentNullException(nameof(linkOptions));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            // Create the filter, which links to the real target, and then
            // link the real source to this intermediate filter.
            var filter = new FilteredLinkPropagator<TOutput>(source, target, predicate);
            return source.LinkTo(filter, linkOptions);
        }

        /// <summary>Provides a synchronous filter for use in filtered LinkTos.</summary>
        /// <typeparam name="T">Specifies the type of data being filtered.</typeparam>
        [DebuggerDisplay("{DebuggerDisplayContent,nq}")]
        [DebuggerTypeProxy(typeof(FilteredLinkPropagator<>.DebugView))]
        private sealed class FilteredLinkPropagator<T> : IPropagatorBlock<T, T>, IDebuggerDisplay
        {
            /// <summary>The source connected with this filter.</summary>
            private readonly ISourceBlock<T> _source;
            /// <summary>The target with which this block is associated.</summary>
            private readonly ITargetBlock<T> _target;
            /// <summary>The predicate provided by the user.</summary>
            private readonly Predicate<T> _userProvidedPredicate;

            /// <summary>Initializes the filter passthrough.</summary>
            /// <param name="source">The source connected to this filter.</param>
            /// <param name="target">The target to which filtered messages should be passed.</param>
            /// <param name="predicate">The predicate to run for each message.</param>
            internal FilteredLinkPropagator(ISourceBlock<T> source, ITargetBlock<T> target, Predicate<T> predicate)
            {
                Debug.Assert(source != null, "Filtered link requires a source to filter on.");
                Debug.Assert(target != null, "Filtered link requires a target to filter to.");
                Debug.Assert(predicate != null, "Filtered link requires a predicate to filter with.");

                // Store the arguments
                _source = source;
                _target = target;
                _userProvidedPredicate = predicate;
            }

            /// <summary>Runs the user-provided predicate over an item in the correct execution context.</summary>
            /// <param name="item">The item to evaluate.</param>
            /// <returns>true if the item passed the filter; otherwise, false.</returns>
            private bool RunPredicate(T item)
            {
                Debug.Assert(_userProvidedPredicate != null, "User-provided predicate is required.");

                return _userProvidedPredicate(item);
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Targets/Member[@name="OfferMessage"]/*' />
#pragma warning disable 8617
            DataflowMessageStatus ITargetBlock<T>.OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T>? source, bool consumeToAccept)
#pragma warning restore 8617
            {
                // Validate arguments.  Some targets may have a null source, but FilteredLinkPropagator
                // is an internal target that should only ever have source non-null.
                if (!messageHeader.IsValid) throw new ArgumentException(SR.Argument_InvalidMessageHeader, nameof(messageHeader));
                if (source == null) throw new ArgumentNullException(nameof(source));

                // Run the filter.
                bool passedFilter = RunPredicate(messageValue);

                // If the predicate matched, pass the message along to the real target.
                if (passedFilter)
                {
                    return _target.OfferMessage(messageHeader, messageValue, this, consumeToAccept);
                }
                // Otherwise, decline.
                else return DataflowMessageStatus.Declined;
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="ConsumeMessage"]/*' />
            T? ISourceBlock<T>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<T> target, out bool messageConsumed)
            {
                // This message should have only made it to the target if it passes the filter, so we shouldn't need to check again.
                // The real source will also be doing verifications, so we don't need to validate args here.
                Debug.Assert(messageHeader.IsValid, "Only valid messages may be consumed.");
                return _source.ConsumeMessage(messageHeader, this, out messageConsumed);
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="ReserveMessage"]/*' />
            bool ISourceBlock<T>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<T> target)
            {
                // This message should have only made it to the target if it passes the filter, so we shouldn't need to check again.
                // The real source will also be doing verifications, so we don't need to validate args here.
                Debug.Assert(messageHeader.IsValid, "Only valid messages may be consumed.");
                return _source.ReserveMessage(messageHeader, this);
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="ReleaseReservation"]/*' />
            void ISourceBlock<T>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<T> target)
            {
                // This message should have only made it to the target if it passes the filter, so we shouldn't need to check again.
                // The real source will also be doing verifications, so we don't need to validate args here.
                Debug.Assert(messageHeader.IsValid, "Only valid messages may be consumed.");
                _source.ReleaseReservation(messageHeader, this);
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Completion"]/*' />
            Task IDataflowBlock.Completion { get { return _source.Completion; } }
            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Complete"]/*' />
            void IDataflowBlock.Complete() { _target.Complete(); }
            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Fault"]/*' />
            void IDataflowBlock.Fault(Exception exception) { _target.Fault(exception); }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="LinkTo"]/*' />
            IDisposable ISourceBlock<T>.LinkTo(ITargetBlock<T> target, DataflowLinkOptions linkOptions) { throw new NotSupportedException(SR.NotSupported_MemberNotNeeded); }

            /// <summary>The data to display in the debugger display attribute.</summary>
            private object DebuggerDisplayContent
            {
                get
                {
                    var displaySource = _source as IDebuggerDisplay;
                    var displayTarget = _target as IDebuggerDisplay;
                    return string.Format("{0} Source=\"{1}\", Target=\"{2}\"",
                        Common.GetNameForDebugger(this),
                        displaySource != null ? displaySource.Content : _source,
                        displayTarget != null ? displayTarget.Content : _target);
                }
            }
            /// <summary>Gets the data to display in the debugger display attribute for this instance.</summary>
            object IDebuggerDisplay.Content { get { return DebuggerDisplayContent; } }

            /// <summary>Provides a debugger type proxy for a filter.</summary>
            private sealed class DebugView
            {
                /// <summary>The filter.</summary>
                private readonly FilteredLinkPropagator<T> _filter;

                /// <summary>Initializes the debug view.</summary>
                /// <param name="filter">The filter to view.</param>
                public DebugView(FilteredLinkPropagator<T> filter)
                {
                    Debug.Assert(filter != null, "Need a filter with which to construct the debug view.");
                    _filter = filter;
                }

                /// <summary>The linked target for this filter.</summary>
                public ITargetBlock<T> LinkedTarget { get { return _filter._target; } }
            }
        }
        #endregion

        #region Post and SendAsync
        /// <summary>Posts an item to the <see cref="System.Threading.Tasks.Dataflow.ITargetBlock{T}"/>.</summary>
        /// <typeparam name="TInput">Specifies the type of data accepted by the target block.</typeparam>
        /// <param name="target">The target block.</param>
        /// <param name="item">The item being offered to the target.</param>
        /// <returns>true if the item was accepted by the target block; otherwise, false.</returns>
        /// <remarks>
        /// This method will return once the target block has decided to accept or decline the item,
        /// but unless otherwise dictated by special semantics of the target block, it does not wait
        /// for the item to actually be processed (for example, <see cref="System.Threading.Tasks.Dataflow.ActionBlock{T}"/>
        /// will return from Post as soon as it has stored the posted item into its input queue).  From the perspective
        /// of the block's processing, Post is asynchronous. For target blocks that support postponing offered messages,
        /// or for blocks that may do more processing in their Post implementation, consider using
        /// <see cref="System.Threading.Tasks.Dataflow.DataflowBlock.SendAsync{TInput}(ITargetBlock{TInput}, TInput)">SendAsync</see>,
        /// which will return immediately and will enable the target to postpone the posted message and later consume it
        /// after SendAsync returns.
        /// </remarks>
        public static bool Post<TInput>(this ITargetBlock<TInput> target, TInput item)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            return target.OfferMessage(Common.SingleMessageHeader, item, source: null, consumeToAccept: false) == DataflowMessageStatus.Accepted;
        }

        /// <summary>Asynchronously offers a message to the target message block, allowing for postponement.</summary>
        /// <typeparam name="TInput">Specifies the type of the data to post to the target.</typeparam>
        /// <param name="target">The target to which to post the data.</param>
        /// <param name="item">The item being offered to the target.</param>
        /// <returns>
        /// A <see cref="System.Threading.Tasks.Task{Boolean}"/> that represents the asynchronous send.  If the target
        /// accepts and consumes the offered element during the call to SendAsync, upon return
        /// from the call the resulting <see cref="System.Threading.Tasks.Task{Boolean}"/> will be completed and its <see cref="System.Threading.Tasks.Task{Boolean}.Result">Result</see>
        /// property will return true.  If the target declines the offered element during the call, upon return from the call the resulting <see cref="System.Threading.Tasks.Task{Boolean}"/> will
        /// be completed and its <see cref="System.Threading.Tasks.Task{Boolean}.Result">Result</see> property will return false. If the target
        /// postpones the offered element, the element will be buffered until such time that the target consumes or releases it, at which
        /// point the Task will complete, with its <see cref="System.Threading.Tasks.Task{Boolean}.Result"/> indicating whether the message was consumed.  If the target
        /// never attempts to consume or release the message, the returned task will never complete.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">The <paramref name="target"/> is null (Nothing in Visual Basic).</exception>
        public static Task<bool> SendAsync<TInput>(this ITargetBlock<TInput> target, TInput item)
        {
            return SendAsync<TInput>(target, item, CancellationToken.None);
        }

        /// <summary>Asynchronously offers a message to the target message block, allowing for postponement.</summary>
        /// <typeparam name="TInput">Specifies the type of the data to post to the target.</typeparam>
        /// <param name="target">The target to which to post the data.</param>
        /// <param name="item">The item being offered to the target.</param>
        /// <param name="cancellationToken">The cancellation token with which to request cancellation of the send operation.</param>
        /// <returns>
        /// <para>
        /// A <see cref="System.Threading.Tasks.Task{Boolean}"/> that represents the asynchronous send.  If the target
        /// accepts and consumes the offered element during the call to SendAsync, upon return
        /// from the call the resulting <see cref="System.Threading.Tasks.Task{Boolean}"/> will be completed and its <see cref="System.Threading.Tasks.Task{Boolean}.Result">Result</see>
        /// property will return true.  If the target declines the offered element during the call, upon return from the call the resulting <see cref="System.Threading.Tasks.Task{Boolean}"/> will
        /// be completed and its <see cref="System.Threading.Tasks.Task{Boolean}.Result">Result</see> property will return false. If the target
        /// postpones the offered element, the element will be buffered until such time that the target consumes or releases it, at which
        /// point the Task will complete, with its <see cref="System.Threading.Tasks.Task{Boolean}.Result"/> indicating whether the message was consumed.  If the target
        /// never attempts to consume or release the message, the returned task will never complete.
        /// </para>
        /// <para>
        /// If cancellation is requested before the target has successfully consumed the sent data,
        /// the returned task will complete in the Canceled state and the data will no longer be available to the target.
        /// </para>
        /// </returns>
        /// <exception cref="System.ArgumentNullException">The <paramref name="target"/> is null (Nothing in Visual Basic).</exception>
        public static Task<bool> SendAsync<TInput>(this ITargetBlock<TInput> target, TInput item, CancellationToken cancellationToken)
        {
            // Validate arguments.  No validation necessary for item.
            if (target == null) throw new ArgumentNullException(nameof(target));

            // Fast path check for cancellation
            if (cancellationToken.IsCancellationRequested)
                return Common.CreateTaskFromCancellation<bool>(cancellationToken);

            SendAsyncSource<TInput> source;

            // Fast path: try to offer the item synchronously.  This first try is done
            // without any form of cancellation, and thus consumeToAccept can be the better-performing "false".
            try
            {
                switch (target.OfferMessage(Common.SingleMessageHeader, item, source: null, consumeToAccept: false))
                {
                    // If the message is immediately accepted, return a cached completed task with a true result
                    case DataflowMessageStatus.Accepted:
                        return Common.CompletedTaskWithTrueResult;

                    // If the target is declining permanently, return a cached completed task with a false result
                    case DataflowMessageStatus.DecliningPermanently:
                        return Common.CompletedTaskWithFalseResult;

#if DEBUG
                    case DataflowMessageStatus.Postponed:
                        Debug.Assert(false, "A message should never be postponed when no source has been provided");
                        break;

                    case DataflowMessageStatus.NotAvailable:
                        Debug.Assert(false, "The message should never be missed, as it's offered to only this one target");
                        break;
#endif
                }

                // Slow path: the target did not accept the synchronous post, nor did it decline it.
                // Create a source for the send, launch the offering, and return the representative task.
                // This ctor attempts to register a cancellation notification which would throw if the
                // underlying CTS has been disposed of. Therefore, keep it inside the try/catch block.
                source = new SendAsyncSource<TInput>(target, item, cancellationToken);
            }
            catch (Exception exc)
            {
                // If the target throws from OfferMessage, return a faulted task
                Common.StoreDataflowMessageValueIntoExceptionData(exc, item);
                return Common.CreateTaskFromException<bool>(exc);
            }

            Debug.Assert(source != null, "The SendAsyncSource instance must have been constructed.");
            source.OfferToTarget(); // synchronous to preserve message ordering
            return source.Task;
        }

        /// <summary>
        /// Provides a source used by SendAsync that will buffer a single message and signal when it's been accepted or declined.
        /// </summary>
        /// <remarks>This source must only be passed to a single target, and must only be used once.</remarks>
        [DebuggerDisplay("{DebuggerDisplayContent,nq}")]
        [DebuggerTypeProxy(typeof(SendAsyncSource<>.DebugView))]
        private sealed class SendAsyncSource<TOutput> : TaskCompletionSource<bool>, ISourceBlock<TOutput>, IDebuggerDisplay
        {
            /// <summary>The target to offer to.</summary>
            private readonly ITargetBlock<TOutput> _target;
            /// <summary>The buffered message.</summary>
            private readonly TOutput _messageValue;

            /// <summary>CancellationToken used to cancel the send.</summary>
            private CancellationToken _cancellationToken;
            /// <summary>Registration with the cancellation token.</summary>
            private CancellationTokenRegistration _cancellationRegistration;
            /// <summary>The cancellation/completion state of the source.</summary>
            private int _cancellationState; // one of the CANCELLATION_STATE_* constant values, defaulting to NONE

            // Cancellation states:
            // _cancellationState starts out as NONE, and will remain that way unless a CancellationToken
            // is provided in the initial OfferToTarget call.  As such, unless a token is provided,
            // all synchronization related to cancellation will be avoided.  Once a token is provided,
            // the state transitions to REGISTERED.  If cancellation then is requested or if the target
            // calls back to consume the message, the state will transition to COMPLETING prior to
            // actually committing the action; if it can't transition to COMPLETING, then the action doesn't
            // take effect (e.g. if cancellation raced with the target consuming, such that the cancellation
            // action was able to transition to COMPLETING but the consumption wasn't, then ConsumeMessage
            // would return false indicating that the message could not be consumed).  The only additional
            // complication here is around reservations.  If a target reserves a message, _cancellationState
            // transitions to RESERVED.  A subsequent ConsumeMessage call can successfully transition from
            // RESERVED to COMPLETING, but cancellation can't; cancellation can only transition from REGISTERED
            // to COMPLETING.  If the reservation on the message is instead released, _cancellationState
            // will transition back to REGISTERED.

            /// <summary>No cancellation registration is used.</summary>
            private const int CANCELLATION_STATE_NONE = 0;
            /// <summary>A cancellation token has been registered.</summary>
            private const int CANCELLATION_STATE_REGISTERED = 1;
            /// <summary>The message has been reserved. Only used if a cancellation token is in play.</summary>
            private const int CANCELLATION_STATE_RESERVED = 2;
            /// <summary>Completion is now in progress. Only used if a cancellation token is in play.</summary>
            private const int CANCELLATION_STATE_COMPLETING = 3;

            /// <summary>Initializes the source.</summary>
            /// <param name="target">The target to offer to.</param>
            /// <param name="messageValue">The message to offer and buffer.</param>
            /// <param name="cancellationToken">The cancellation token with which to cancel the send.</param>
            internal SendAsyncSource(ITargetBlock<TOutput> target, TOutput messageValue, CancellationToken cancellationToken)
            {
                Debug.Assert(target != null, "A valid target to send to is required.");
                _target = target;
                _messageValue = messageValue;

                // If a cancelable CancellationToken is used, update our cancellation state
                // and register with the token.  Only if CanBeCanceled is true due we want
                // to pay the subsequent costs around synchronization between cancellation
                // requests and the target coming back to consume the message.
                if (cancellationToken.CanBeCanceled)
                {
                    _cancellationToken = cancellationToken;
                    _cancellationState = CANCELLATION_STATE_REGISTERED;

                    try
                    {
                        _cancellationRegistration = cancellationToken.Register(
                            _cancellationCallback, new WeakReference<SendAsyncSource<TOutput>>(this));
                    }
                    catch
                    {
                        // Suppress finalization.  Finalization is only required if the target drops a reference
                        // to the source before the source has completed, and we'll never offer to the target.
                        GC.SuppressFinalize(this);

                        // Propagate the exception
                        throw;
                    }
                }
            }

            /// <summary>Finalizer that completes the returned task if all references to this source are dropped.</summary>
            ~SendAsyncSource()
            {
                // CompleteAsDeclined uses synchronization, which is dangerous for a finalizer
                // during shutdown or appdomain unload.
                if (!Environment.HasShutdownStarted)
                {
                    CompleteAsDeclined(runAsync: true);
                }
            }

            /// <summary>Completes the source in an "Accepted" state.</summary>
            /// <param name="runAsync">true to accept asynchronously; false to accept synchronously.</param>
            private void CompleteAsAccepted(bool runAsync)
            {
                RunCompletionAction(state =>
                {
                    try { ((SendAsyncSource<TOutput>)state!).TrySetResult(true); }
                    catch (ObjectDisposedException) { }
                }, this, runAsync);
            }

            /// <summary>Completes the source in an "Declined" state.</summary>
            /// <param name="runAsync">true to decline asynchronously; false to decline synchronously.</param>
            private void CompleteAsDeclined(bool runAsync)
            {
                RunCompletionAction(state =>
                {
                    // The try/catch for ObjectDisposedException handles the case where the
                    // user disposes of the returned task before we're done with it.
                    try { ((SendAsyncSource<TOutput>)state!).TrySetResult(false); }
                    catch (ObjectDisposedException) { }
                }, this, runAsync);
            }

            /// <summary>Completes the source in faulted state.</summary>
            /// <param name="exception">The exception with which to fault.</param>
            /// <param name="runAsync">true to fault asynchronously; false to fault synchronously.</param>
            private void CompleteAsFaulted(Exception exception, bool runAsync)
            {
                RunCompletionAction(state =>
                {
                    var tuple = (Tuple<SendAsyncSource<TOutput>, Exception>)state!;
                    try { tuple.Item1.TrySetException(tuple.Item2); }
                    catch (ObjectDisposedException) { }
                }, Tuple.Create<SendAsyncSource<TOutput>, Exception>(this, exception), runAsync);
            }

            /// <summary>Completes the source in canceled state.</summary>
            /// <param name="runAsync">true to fault asynchronously; false to fault synchronously.</param>
            private void CompleteAsCanceled(bool runAsync)
            {
                RunCompletionAction(state =>
                {
                    try { ((SendAsyncSource<TOutput>)state!).TrySetCanceled(); }
                    catch (ObjectDisposedException) { }
                }, this, runAsync);
            }

            /// <summary>Executes a completion action.</summary>
            /// <param name="completionAction">The action to execute, passed the state.</param>
            /// <param name="completionActionState">The state to pass into the delegate.</param>
            /// <param name="runAsync">true to execute the action asynchronously; false to execute it synchronously.</param>
            /// <remarks>
            /// async should be true if this is being called on a path that has the target on the stack, e.g.
            /// the target is calling to ConsumeMessage.  We don't want to block the target indefinitely
            /// with any synchronous continuations off of the returned send async task.
            /// </remarks>
            private void RunCompletionAction(Action<object?> completionAction, object completionActionState, bool runAsync)
            {
                Debug.Assert(completionAction != null, "Completion action to run is required.");

                // Suppress finalization.  Finalization is only required if the target drops a reference
                // to the source before the source has completed, and here we're completing the source.
                GC.SuppressFinalize(this);

                // Dispose of the cancellation registration if there is one
                if (_cancellationState != CANCELLATION_STATE_NONE)
                {
                    Debug.Assert(_cancellationRegistration != default(CancellationTokenRegistration),
                        "If we're not in NONE, we must have a cancellation token we've registered with.");
                    _cancellationRegistration.Dispose();
                }

                // If we're meant to run asynchronously, launch a task.
                if (runAsync)
                {
                    System.Threading.Tasks.Task.Factory.StartNew(
                        completionAction, completionActionState,
                        CancellationToken.None, Common.GetCreationOptionsForTask(), TaskScheduler.Default);
                }
                // Otherwise, execute directly.
                else
                {
                    completionAction(completionActionState);
                }
            }

            /// <summary>Offers the message to the target asynchronously.</summary>
            private void OfferToTargetAsync()
            {
                System.Threading.Tasks.Task.Factory.StartNew(
                    state => ((SendAsyncSource<TOutput>)state!).OfferToTarget(), this,
                    CancellationToken.None, Common.GetCreationOptionsForTask(), TaskScheduler.Default);
            }

            /// <summary>Cached delegate used to cancel a send in response to a cancellation request.</summary>
            private static readonly Action<object?> _cancellationCallback = CancellationHandler;

            /// <summary>Attempts to cancel the source passed as state in response to a cancellation request.</summary>
            /// <param name="state">
            /// A weak reference to the SendAsyncSource.  A weak reference is used to prevent the source
            /// from being rooted in a long-lived token.
            /// </param>
            private static void CancellationHandler(object? state)
            {
                SendAsyncSource<TOutput>? source = Common.UnwrapWeakReference<SendAsyncSource<TOutput>>(state!);
                if (source != null)
                {
                    Debug.Assert(source._cancellationState != CANCELLATION_STATE_NONE,
                        "If cancellation is in play, we must have already moved out of the NONE state.");

                    // Try to reserve completion, and if we can, complete as canceled.  Note that we can only
                    // achieve cancellation when in the REGISTERED state, and not when in the RESERVED state,
                    // as if a target has reserved the message, we must allow the message to be consumed successfully.
                    if (source._cancellationState == CANCELLATION_STATE_REGISTERED && // fast check to avoid the interlocked if we can
                        Interlocked.CompareExchange(ref source._cancellationState, CANCELLATION_STATE_COMPLETING, CANCELLATION_STATE_REGISTERED) == CANCELLATION_STATE_REGISTERED)
                    {
                        // We've reserved completion, so proceed to cancel the task.
                        source.CompleteAsCanceled(true);
                    }
                }
            }

            /// <summary>Offers the message to the target synchronously.</summary>
            internal void OfferToTarget()
            {
                try
                {
                    // Offer the message to the target.  If there's no cancellation in play, we can just allow the target
                    // to accept the message directly.  But if a CancellationToken is in use, the target needs to come
                    // back to us to get the data; that way, we can ensure we don't race between returning a canceled task but
                    // successfully completing the send.
                    bool consumeToAccept = _cancellationState != CANCELLATION_STATE_NONE;

                    switch (_target.OfferMessage(
                        Common.SingleMessageHeader, _messageValue, this, consumeToAccept: consumeToAccept))
                    {
                        // If the message is immediately accepted, complete the task as accepted
                        case DataflowMessageStatus.Accepted:
                            if (!consumeToAccept)
                            {
                                // Cancellation wasn't in use, and the target accepted the message directly,
                                // so complete the task as accepted.
                                CompleteAsAccepted(runAsync: false);
                            }
                            else
                            {
                                // If cancellation is in use, then since the target accepted,
                                // our state better reflect that we're completing.
                                Debug.Assert(_cancellationState == CANCELLATION_STATE_COMPLETING,
                                    "The message was accepted, so we should have started completion.");
                            }
                            break;

                        // If the message is immediately declined, complete the task as declined
                        case DataflowMessageStatus.Declined:
                        case DataflowMessageStatus.DecliningPermanently:
                            CompleteAsDeclined(runAsync: false);
                            break;
#if DEBUG
                        case DataflowMessageStatus.NotAvailable:
                            Debug.Assert(false, "The message should never be missed, as it's offered to only this one target");
                            break;
                            // If the message was postponed, the source may or may not be complete yet.  Nothing to validate.
                            // Treat an improper DataflowMessageStatus as postponed and do nothing.
#endif
                    }
                }
                // A faulty target might throw from OfferMessage.  If that happens,
                // we'll try to fault the returned task.  A really faulty target might
                // both throw from OfferMessage and call ConsumeMessage,
                // in which case it's possible we might not be able to propagate the exception
                // out to the caller through the task if ConsumeMessage wins the race,
                // which is likely if the exception doesn't occur until after ConsumeMessage is
                // called.  If that happens, we just eat the exception.
                catch (Exception exc)
                {
                    Common.StoreDataflowMessageValueIntoExceptionData(exc, _messageValue);
                    CompleteAsFaulted(exc, runAsync: false);
                }
            }

            /// <summary>Called by the target to consume the buffered message.</summary>
            TOutput? ISourceBlock<TOutput>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target, out bool messageConsumed)
            {
                // Validate arguments
                if (!messageHeader.IsValid) throw new ArgumentException(SR.Argument_InvalidMessageHeader, nameof(messageHeader));
                if (target == null) throw new ArgumentNullException(nameof(target));

                // If the task has already completed, there's nothing to consume.  This could happen if
                // cancellation was already requested and completed the task as a result.
                if (Task.IsCompleted)
                {
                    messageConsumed = false;
                    return default(TOutput);
                }

                // If the message being asked for is not the same as the one that's buffered,
                // something is wrong.  Complete as having failed to transfer the message.
                bool validMessage = (messageHeader.Id == Common.SINGLE_MESSAGE_ID);

                if (validMessage)
                {
                    int curState = _cancellationState;
                    Debug.Assert(
                        curState == CANCELLATION_STATE_NONE || curState == CANCELLATION_STATE_REGISTERED ||
                        curState == CANCELLATION_STATE_RESERVED || curState == CANCELLATION_STATE_COMPLETING,
                        "The current cancellation state is not valid.");

                    // If we're not dealing with cancellation, then if we're currently registered or reserved, try to transition
                    // to completing. If we're able to, allow the message to be consumed, and we're done.  At this point, we
                    // support transitioning out of REGISTERED or RESERVED.
                    if (curState == CANCELLATION_STATE_NONE || // no synchronization necessary if there's no cancellation
                        (curState != CANCELLATION_STATE_COMPLETING && // fast check to avoid unnecessary synchronization
                         Interlocked.CompareExchange(ref _cancellationState, CANCELLATION_STATE_COMPLETING, curState) == curState))
                    {
                        CompleteAsAccepted(runAsync: true);
                        messageConsumed = true;
                        return _messageValue;
                    }
                }

                // Consumption failed
                messageConsumed = false;
                return default(TOutput);
            }

            /// <summary>Called by the target to reserve the buffered message.</summary>
            bool ISourceBlock<TOutput>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
            {
                // Validate arguments
                if (!messageHeader.IsValid) throw new ArgumentException(SR.Argument_InvalidMessageHeader, nameof(messageHeader));
                if (target == null) throw new ArgumentNullException(nameof(target));

                // If the task has already completed, such as due to cancellation, there's nothing to reserve.
                if (Task.IsCompleted) return false;

                // As long as the message is the one being requested and cancellation hasn't been requested, allow it to be reserved.
                bool reservable = (messageHeader.Id == Common.SINGLE_MESSAGE_ID);
                return reservable &&
                    (_cancellationState == CANCELLATION_STATE_NONE || // avoid synchronization when cancellation is not in play
                     Interlocked.CompareExchange(ref _cancellationState, CANCELLATION_STATE_RESERVED, CANCELLATION_STATE_REGISTERED) == CANCELLATION_STATE_REGISTERED);
            }

            /// <summary>Called by the target to release a reservation on the buffered message.</summary>
            void ISourceBlock<TOutput>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
            {
                // Validate arguments
                if (!messageHeader.IsValid) throw new ArgumentException(SR.Argument_InvalidMessageHeader, nameof(messageHeader));
                if (target == null) throw new ArgumentNullException(nameof(target));

                // If this is not the message we posted, bail
                if (messageHeader.Id != Common.SINGLE_MESSAGE_ID)
                    throw new InvalidOperationException(SR.InvalidOperation_MessageNotReservedByTarget);

                // If the task has already completed, there's nothing to release.
                if (Task.IsCompleted) return;

                // If a cancellation token is being used, revert our state back to registered.  In the meantime
                // cancellation could have been requested, so check to see now if cancellation was requested
                // and process it if it was.
                if (_cancellationState != CANCELLATION_STATE_NONE)
                {
                    if (Interlocked.CompareExchange(ref _cancellationState, CANCELLATION_STATE_REGISTERED, CANCELLATION_STATE_RESERVED) != CANCELLATION_STATE_RESERVED)
                        throw new InvalidOperationException(SR.InvalidOperation_MessageNotReservedByTarget);
                    if (_cancellationToken.IsCancellationRequested)
                        CancellationHandler(new WeakReference<SendAsyncSource<TOutput>>(this)); // same code as registered with the CancellationToken
                }

                // Start the process over by reoffering the message asynchronously.
                OfferToTargetAsync();
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Completion"]/*' />
            Task IDataflowBlock.Completion { get { return Task; } }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="LinkTo"]/*' />
            IDisposable ISourceBlock<TOutput>.LinkTo(ITargetBlock<TOutput> target, DataflowLinkOptions linkOptions) { throw new NotSupportedException(SR.NotSupported_MemberNotNeeded); }
            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Complete"]/*' />
            void IDataflowBlock.Complete() { throw new NotSupportedException(SR.NotSupported_MemberNotNeeded); }
            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Fault"]/*' />
            void IDataflowBlock.Fault(Exception exception) { throw new NotSupportedException(SR.NotSupported_MemberNotNeeded); }

            /// <summary>The data to display in the debugger display attribute.</summary>
            private object DebuggerDisplayContent
            {
                get
                {
                    var displayTarget = _target as IDebuggerDisplay;
                    return string.Format("{0} Message={1}, Target=\"{2}\"",
                        Common.GetNameForDebugger(this),
                        _messageValue,
                        displayTarget != null ? displayTarget.Content : _target);
                }
            }
            /// <summary>Gets the data to display in the debugger display attribute for this instance.</summary>
            object IDebuggerDisplay.Content { get { return DebuggerDisplayContent; } }

            /// <summary>Provides a debugger type proxy for the source.</summary>
            private sealed class DebugView
            {
                /// <summary>The source.</summary>
                private readonly SendAsyncSource<TOutput> _source;

                /// <summary>Initializes the debug view.</summary>
                /// <param name="source">The source to view.</param>
                public DebugView(SendAsyncSource<TOutput> source)
                {
                    Debug.Assert(source != null, "Need a source with which to construct the debug view.");
                    _source = source;
                }

                /// <summary>The target to which we're linked.</summary>
                public ITargetBlock<TOutput> Target { get { return _source._target; } }
                /// <summary>The message buffered by the source.</summary>
                public TOutput Message { get { return _source._messageValue; } }
                /// <summary>The Task represented the posting of the message.</summary>
                public Task<bool> Completion { get { return _source.Task; } }
            }
        }
        #endregion

        #region TryReceive, ReceiveAsync, and Receive
        #region TryReceive
        /// <summary>
        /// Attempts to synchronously receive an item from the <see cref="System.Threading.Tasks.Dataflow.ISourceBlock{T}"/>.
        /// </summary>
        /// <param name="source">The source from which to receive.</param>
        /// <param name="item">The item received from the source.</param>
        /// <returns>true if an item could be received; otherwise, false.</returns>
        /// <remarks>
        /// This method does not wait until the source has an item to provide.
        /// It will return whether or not an element was available.
        /// </remarks>
        public static bool TryReceive<TOutput>(this IReceivableSourceBlock<TOutput> source, [MaybeNullWhen(false)] out TOutput item)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return source.TryReceive(null, out item);
        }
        #endregion

        #region ReceiveAsync
        /// <summary>Asynchronously receives a value from the specified source.</summary>
        /// <typeparam name="TOutput">Specifies the type of data contained in the source.</typeparam>
        /// <param name="source">The source from which to asynchronously receive.</param>
        /// <returns>
        /// A <see cref="System.Threading.Tasks.Task{TOutput}"/> that represents the asynchronous receive operation.  When an item is successfully received from the source,
        /// the returned task will be completed and its <see cref="System.Threading.Tasks.Task{TOutput}.Result">Result</see> will return the received item.  If an item cannot be retrieved,
        /// because the source is empty and completed, the returned task will be canceled.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">The <paramref name="source"/> is null (Nothing in Visual Basic).</exception>
        public static Task<TOutput> ReceiveAsync<TOutput>(
            this ISourceBlock<TOutput> source)
        {
            // Argument validation handled by target method
            return ReceiveAsync(source, Common.InfiniteTimeSpan, CancellationToken.None);
        }

        /// <summary>Asynchronously receives a value from the specified source.</summary>
        /// <typeparam name="TOutput">Specifies the type of data contained in the source.</typeparam>
        /// <param name="source">The source from which to asynchronously receive.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> which may be used to cancel the receive operation.</param>
        /// <returns>
        /// A <see cref="System.Threading.Tasks.Task{TOutput}"/> that represents the asynchronous receive operation.  When an item is successfully received from the source,
        /// the returned task will be completed and its <see cref="System.Threading.Tasks.Task{TOutput}.Result">Result</see> will return the received item.  If an item cannot be retrieved,
        /// either because cancellation is requested or the source is empty and completed, the returned task will be canceled.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">The <paramref name="source"/> is null (Nothing in Visual Basic).</exception>
        public static Task<TOutput> ReceiveAsync<TOutput>(
            this ISourceBlock<TOutput> source, CancellationToken cancellationToken)
        {
            // Argument validation handled by target method
            return ReceiveAsync(source, Common.InfiniteTimeSpan, cancellationToken);
        }

        /// <summary>Asynchronously receives a value from the specified source.</summary>
        /// <typeparam name="TOutput">Specifies the type of data contained in the source.</typeparam>
        /// <param name="source">The source from which to asynchronously receive.</param>
        /// <param name="timeout">A <see cref="System.TimeSpan"/> that represents the number of milliseconds to wait, or a TimeSpan that represents -1 milliseconds to wait indefinitely.</param>
        /// <returns>
        /// A <see cref="System.Threading.Tasks.Task{TOutput}"/> that represents the asynchronous receive operation.  When an item is successfully received from the source,
        /// the returned task will be completed and its <see cref="System.Threading.Tasks.Task{TOutput}.Result">Result</see> will return the received item.  If an item cannot be retrieved,
        /// either because the timeout expires or the source is empty and completed, the returned task will be canceled.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">The <paramref name="source"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// timeout is a negative number other than -1 milliseconds, which represents an infinite time-out -or- timeout is greater than <see cref="int.MaxValue"/>.
        /// </exception>
        public static Task<TOutput> ReceiveAsync<TOutput>(
            this ISourceBlock<TOutput> source, TimeSpan timeout)
        {
            // Argument validation handled by target method
            return ReceiveAsync(source, timeout, CancellationToken.None);
        }

        /// <summary>Asynchronously receives a value from the specified source.</summary>
        /// <typeparam name="TOutput">Specifies the type of data contained in the source.</typeparam>
        /// <param name="source">The source from which to asynchronously receive.</param>
        /// <param name="timeout">A <see cref="System.TimeSpan"/> that represents the number of milliseconds to wait, or a TimeSpan that represents -1 milliseconds to wait indefinitely.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> which may be used to cancel the receive operation.</param>
        /// <returns>
        /// A <see cref="System.Threading.Tasks.Task{TOutput}"/> that represents the asynchronous receive operation.  When an item is successfully received from the source,
        /// the returned task will be completed and its <see cref="System.Threading.Tasks.Task{TOutput}.Result">Result</see> will return the received item.  If an item cannot be retrieved,
        /// either because the timeout expires, cancellation is requested, or the source is empty and completed, the returned task will be canceled.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">The <paramref name="source"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// timeout is a negative number other than -1 milliseconds, which represents an infinite time-out -or- timeout is greater than <see cref="int.MaxValue"/>.
        /// </exception>
        public static Task<TOutput> ReceiveAsync<TOutput>(
            this ISourceBlock<TOutput> source, TimeSpan timeout, CancellationToken cancellationToken)
        {
            // Validate arguments


            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!Common.IsValidTimeout(timeout)) throw new ArgumentOutOfRangeException(nameof(timeout), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);

            // Return the task representing the core receive operation
            return ReceiveCore(source, true, timeout, cancellationToken);
        }
        #endregion

        #region Receive
        /// <summary>Synchronously receives an item from the source.</summary>
        /// <typeparam name="TOutput">Specifies the type of data contained in the source.</typeparam>
        /// <param name="source">The source from which to receive.</param>
        /// <returns>The received item.</returns>
        /// <exception cref="System.ArgumentNullException">The <paramref name="source"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.InvalidOperationException">No item could be received from the source.</exception>
        public static TOutput Receive<TOutput>(
            this ISourceBlock<TOutput> source)
        {
            // Argument validation handled by target method
            return Receive(source, Common.InfiniteTimeSpan, CancellationToken.None);
        }

        /// <summary>Synchronously receives an item from the source.</summary>
        /// <typeparam name="TOutput">Specifies the type of data contained in the source.</typeparam>
        /// <param name="source">The source from which to receive.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> which may be used to cancel the receive operation.</param>
        /// <returns>The received item.</returns>
        /// <exception cref="System.ArgumentNullException">The <paramref name="source"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.InvalidOperationException">No item could be received from the source.</exception>
        /// <exception cref="System.OperationCanceledException">The operation was canceled before an item was received from the source.</exception>
        /// <remarks>
        /// If the source successfully offered an item that was received by this operation, it will be returned, even if a concurrent cancellation request occurs.
        /// </remarks>
        public static TOutput Receive<TOutput>(
            this ISourceBlock<TOutput> source, CancellationToken cancellationToken)
        {
            // Argument validation handled by target method
            return Receive(source, Common.InfiniteTimeSpan, cancellationToken);
        }

        /// <summary>Synchronously receives an item from the source.</summary>
        /// <typeparam name="TOutput">Specifies the type of data contained in the source.</typeparam>
        /// <param name="source">The source from which to receive.</param>
        /// <param name="timeout">A <see cref="System.TimeSpan"/> that represents the number of milliseconds to wait, or a TimeSpan that represents -1 milliseconds to wait indefinitely.</param>
        /// <returns>The received item.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// timeout is a negative number other than -1 milliseconds, which represents an infinite time-out -or- timeout is greater than <see cref="int.MaxValue"/>.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="source"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.InvalidOperationException">No item could be received from the source.</exception>
        /// <exception cref="System.TimeoutException">The specified timeout expired before an item was received from the source.</exception>
        /// <remarks>
        /// If the source successfully offered an item that was received by this operation, it will be returned, even if a concurrent timeout occurs.
        /// </remarks>
        public static TOutput Receive<TOutput>(
            this ISourceBlock<TOutput> source, TimeSpan timeout)
        {
            // Argument validation handled by target method
            return Receive(source, timeout, CancellationToken.None);
        }

        /// <summary>Synchronously receives an item from the source.</summary>
        /// <typeparam name="TOutput">Specifies the type of data contained in the source.</typeparam>
        /// <param name="source">The source from which to receive.</param>
        /// <param name="timeout">A <see cref="System.TimeSpan"/> that represents the number of milliseconds to wait, or a TimeSpan that represents -1 milliseconds to wait indefinitely.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> which may be used to cancel the receive operation.</param>
        /// <returns>The received item.</returns>
        /// <exception cref="System.ArgumentNullException">The <paramref name="source"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// timeout is a negative number other than -1 milliseconds, which represents an infinite time-out -or- timeout is greater than <see cref="int.MaxValue"/>.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">No item could be received from the source.</exception>
        /// <exception cref="System.TimeoutException">The specified timeout expired before an item was received from the source.</exception>
        /// <exception cref="System.OperationCanceledException">The operation was canceled before an item was received from the source.</exception>
        /// <remarks>
        /// If the source successfully offered an item that was received by this operation, it will be returned, even if a concurrent timeout or cancellation request occurs.
        /// </remarks>
        public static TOutput Receive<TOutput>(
            this ISourceBlock<TOutput> source, TimeSpan timeout, CancellationToken cancellationToken)
        {
            // Validate arguments
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!Common.IsValidTimeout(timeout)) throw new ArgumentOutOfRangeException(nameof(timeout), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);

            // Do fast path checks for both cancellation and data already existing.
            cancellationToken.ThrowIfCancellationRequested();
            TOutput? fastCheckedItem;
            var receivableSource = source as IReceivableSourceBlock<TOutput>;
            if (receivableSource != null && receivableSource.TryReceive(null, out fastCheckedItem))
            {
                return fastCheckedItem;
            }

            // Get a TCS to represent the receive operation and wait for it to complete.
            // If it completes successfully, return the result. Otherwise, throw the
            // original inner exception representing the cause.  This could be an OCE.
            Task<TOutput> task = ReceiveCore(source, false, timeout, cancellationToken);
            try
            {
                return task.GetAwaiter().GetResult(); // block until the result is available
            }
            catch
            {
                // Special case cancellation in order to ensure the exception contains the token.
                // The public TrySetCanceled, used by ReceiveCore, is parameterless and doesn't
                // accept the token to use.  Thus the exception that we're catching here
                // won't contain the cancellation token we want propagated.
                if (task.IsCanceled) cancellationToken.ThrowIfCancellationRequested();

                // If we get here, propagate the original exception.
                throw;
            }
        }
        #endregion

        #region Shared by Receive and ReceiveAsync
        /// <summary>Receives an item from the source.</summary>
        /// <typeparam name="TOutput">Specifies the type of data contained in the source.</typeparam>
        /// <param name="source">The source from which to receive.</param>
        /// <param name="attemptTryReceive">Whether to first attempt using TryReceive to get a value from the source.</param>
        /// <param name="timeout">A <see cref="System.TimeSpan"/> that represents the number of milliseconds to wait, or a TimeSpan that represents -1 milliseconds to wait indefinitely.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> which may be used to cancel the receive operation.</param>
        /// <returns>A Task for the receive operation.</returns>
        private static Task<TOutput> ReceiveCore<TOutput>(
            this ISourceBlock<TOutput> source, bool attemptTryReceive, TimeSpan timeout, CancellationToken cancellationToken)
        {
            Debug.Assert(source != null, "Need a source from which to receive.");

            // If cancellation has been requested, we're done before we've even started, cancel this receive.
            if (cancellationToken.IsCancellationRequested)
            {
                return Common.CreateTaskFromCancellation<TOutput>(cancellationToken);
            }

            if (attemptTryReceive)
            {
                // If we're able to directly and immediately receive an item, use that item to complete the receive.
                var receivableSource = source as IReceivableSourceBlock<TOutput>;
                if (receivableSource != null)
                {
                    try
                    {
                        TOutput? fastCheckedItem;
                        if (receivableSource.TryReceive(null, out fastCheckedItem))
                        {
                            return Task.FromResult<TOutput>(fastCheckedItem);
                        }
                    }
                    catch (Exception exc)
                    {
                        return Common.CreateTaskFromException<TOutput>(exc);
                    }
                }
            }

            int millisecondsTimeout = (int)timeout.TotalMilliseconds;
            if (millisecondsTimeout == 0)
            {
                return Common.CreateTaskFromException<TOutput>(ReceiveTarget<TOutput>.CreateExceptionForTimeout());
            }

            return ReceiveCoreByLinking<TOutput>(source, millisecondsTimeout, cancellationToken);
        }

        /// <summary>The reason for a ReceiveCoreByLinking call failing.</summary>
        private enum ReceiveCoreByLinkingCleanupReason
        {
            /// <summary>The Receive operation completed successfully, obtaining a value from the source.</summary>
            Success = 0,
            /// <summary>The timer expired before a value could be received.</summary>
            Timer = 1,
            /// <summary>The cancellation token had cancellation requested before a value could be received.</summary>
            Cancellation = 2,
            /// <summary>The source completed before a value could be received.</summary>
            SourceCompletion = 3,
            /// <summary>An error occurred while linking up the target.</summary>
            SourceProtocolError = 4,
            /// <summary>An error during cleanup after completion for another reason.</summary>
            ErrorDuringCleanup = 5
        }

        /// <summary>Cancels a CancellationTokenSource passed as the object state argument.</summary>
        private static readonly Action<object?> _cancelCts = state => ((CancellationTokenSource)state!).Cancel();

        /// <summary>Receives an item from the source by linking a temporary target from it.</summary>
        /// <typeparam name="TOutput">Specifies the type of data contained in the source.</typeparam>
        /// <param name="source">The source from which to receive.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or -1 to wait indefinitely.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> which may be used to cancel the receive operation.</param>
        private static Task<TOutput> ReceiveCoreByLinking<TOutput>(ISourceBlock<TOutput> source, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            // Create a target to link from the source
            var target = new ReceiveTarget<TOutput>();

            // Keep cancellation registrations inside the try/catch in case the underlying CTS is disposed in which case an exception is thrown
            try
            {
                // Create a cancellation token that will be canceled when either the provided token
                // is canceled or the source block completes.
                if (cancellationToken.CanBeCanceled)
                {
                    target._externalCancellationToken = cancellationToken;
                    target._regFromExternalCancellationToken = cancellationToken.Register(_cancelCts, target._cts);
                }

                // We need to cleanup if one of a few things happens:
                // - The target completes successfully due to receiving data.
                // - The user-specified timeout occurs, such that we should bail on the receive.
                // - The cancellation token has cancellation requested, such that we should bail on the receive.
                // - The source completes, since it won't send any more data.
                // Note that there's a potential race here, in that the cleanup delegate could be executed
                // from the timer before the timer variable is set, but that's ok, because then timer variable
                // will just show up as null in the cleanup and there will be nothing to dispose (nor will anything
                // need to be disposed, since it's the timer that fired.  Timer.Dispose is also thread-safe to be
                // called multiple times concurrently.)
                if (millisecondsTimeout > 0)
                {
                    target._timer = new Timer(
                        ReceiveTarget<TOutput>.CachedLinkingTimerCallback, target,
                        millisecondsTimeout, Timeout.Infinite);
                }

                if (target._cts.Token.CanBeCanceled)
                {
                    target._cts.Token.Register(
                        ReceiveTarget<TOutput>.CachedLinkingCancellationCallback, target); // we don't have to cleanup this registration, as this cts is short-lived
                }

                // Link the target to the source
                IDisposable unlink = source.LinkTo(target, DataflowLinkOptions.UnlinkAfterOneAndPropagateCompletion);
                target._unlink = unlink;

                // If completion has started, there is a chance it started after we linked.
                // In that case, we must dispose of the unlinker.
                // If completion started before we linked, the cleanup code will try to unlink.
                // So we are racing to dispose of the unlinker.
                if (Volatile.Read(ref target._cleanupReserved))
                {
                    IDisposable? disposableUnlink = Interlocked.CompareExchange<IDisposable?>(ref target._unlink, null, unlink);
                    if (disposableUnlink != null) disposableUnlink.Dispose();
                }
            }
            catch (Exception exception)
            {
                target._receivedException = exception;
                target.TryCleanupAndComplete(ReceiveCoreByLinkingCleanupReason.SourceProtocolError);
                // If we lose the race here, we may end up eating this exception.
            }

            return target.Task;
        }

        /// <summary>Provides a TaskCompletionSource that is also a dataflow target for use in ReceiveCore.</summary>
        /// <typeparam name="T">Specifies the type of data offered to the target.</typeparam>
        [DebuggerDisplay("{DebuggerDisplayContent,nq}")]
        private sealed class ReceiveTarget<T> : TaskCompletionSource<T>, ITargetBlock<T>, IDebuggerDisplay
        {
            /// <summary>Cached delegate used in ReceiveCoreByLinking on the created timer.  Passed the ReceiveTarget as the argument.</summary>
            /// <remarks>The C# compiler will not cache this delegate by default due to it being a generic method on a non-generic class.</remarks>
            internal static readonly TimerCallback CachedLinkingTimerCallback = state =>
            {
                var receiveTarget = (ReceiveTarget<T>)state!;
                receiveTarget.TryCleanupAndComplete(ReceiveCoreByLinkingCleanupReason.Timer);
            };

            /// <summary>Cached delegate used in ReceiveCoreByLinking on the cancellation token. Passed the ReceiveTarget as the state argument.</summary>
            /// <remarks>The C# compiler will not cache this delegate by default due to it being a generic method on a non-generic class.</remarks>
            internal static readonly Action<object?> CachedLinkingCancellationCallback = state =>
            {
                var receiveTarget = (ReceiveTarget<T>)state!;
                receiveTarget.TryCleanupAndComplete(ReceiveCoreByLinkingCleanupReason.Cancellation);
            };

            /// <summary>The received value if we accepted a value from the source.</summary>
            private T? _receivedValue;

            /// <summary>The cancellation token source representing both external and internal cancellation.</summary>
            internal readonly CancellationTokenSource _cts = new CancellationTokenSource();
            /// <summary>Indicates a code path is already on route to complete the target. 0 is false, 1 is true.</summary>
            internal bool _cleanupReserved; // must only be accessed under IncomingLock
            /// <summary>The external token that cancels the internal token.</summary>
            internal CancellationToken _externalCancellationToken;
            /// <summary>The registration on the external token that cancels the internal token.</summary>
            internal CancellationTokenRegistration _regFromExternalCancellationToken;
            /// <summary>The timer that fires when the timeout has been exceeded.</summary>
            internal Timer? _timer;
            /// <summary>The unlinker from removing this target from the source from which we're receiving.</summary>
            internal IDisposable? _unlink;
            /// <summary>The received exception if an error occurred.</summary>
            internal Exception? _receivedException;

            /// <summary>Gets the sync obj used to synchronize all activity on this target.</summary>
            internal object IncomingLock { get { return _cts; } }

            /// <summary>Initializes the target.</summary>
            internal ReceiveTarget() { }

            /// <summary>Offers a message to be used to complete the TaskCompletionSource.</summary>
            DataflowMessageStatus ITargetBlock<T>.OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T>? source, bool consumeToAccept)
            {
                // Validate arguments
                if (!messageHeader.IsValid) throw new ArgumentException(SR.Argument_InvalidMessageHeader, nameof(messageHeader));
                if (source == null && consumeToAccept) throw new ArgumentException(SR.Argument_CantConsumeFromANullSource, nameof(consumeToAccept));

                DataflowMessageStatus status = DataflowMessageStatus.NotAvailable;

                // If we're already one our way to being done, don't accept anything.
                // This is a fast-path check prior to taking the incoming lock;
                // _cleanupReserved only ever goes from false to true.
                if (Volatile.Read(ref _cleanupReserved)) return DataflowMessageStatus.DecliningPermanently;

                lock (IncomingLock)
                {
                    // Check again now that we've taken the lock
                    if (_cleanupReserved) return DataflowMessageStatus.DecliningPermanently;

                    try
                    {
                        // Accept the message if possible and complete this task with the message's value.
                        bool consumed = true;
                        T? acceptedValue = consumeToAccept ? source!.ConsumeMessage(messageHeader, this, out consumed) : messageValue;
                        if (consumed)
                        {
                            status = DataflowMessageStatus.Accepted;
                            _receivedValue = acceptedValue;
                            _cleanupReserved = true;
                        }
                    }
                    catch (Exception exc)
                    {
                        // An error occurred.  Take ourselves out of the game.
                        status = DataflowMessageStatus.DecliningPermanently;
                        Common.StoreDataflowMessageValueIntoExceptionData(exc, messageValue);
                        _receivedException = exc;
                        _cleanupReserved = true;
                    }
                }

                // Do any cleanup outside of the lock.  The right to cleanup was reserved above for these cases.
                if (status == DataflowMessageStatus.Accepted)
                {
                    CleanupAndComplete(ReceiveCoreByLinkingCleanupReason.Success);
                }
                else if (status == DataflowMessageStatus.DecliningPermanently) // should only be the case if an error occurred
                {
                    CleanupAndComplete(ReceiveCoreByLinkingCleanupReason.SourceProtocolError);
                }

                return status;
            }

            /// <summary>
            /// Attempts to reserve the right to cleanup and complete, and if successfully,
            /// continues to cleanup and complete.
            /// </summary>
            /// <param name="reason">The reason we're completing and cleaning up.</param>
            /// <returns>true if successful in completing; otherwise, false.</returns>
            internal bool TryCleanupAndComplete(ReceiveCoreByLinkingCleanupReason reason)
            {
                // If cleanup was already reserved, bail.
                if (Volatile.Read(ref _cleanupReserved)) return false;

                // Atomically using IncomingLock try to reserve the completion routine.
                lock (IncomingLock)
                {
                    if (_cleanupReserved) return false;
                    _cleanupReserved = true;
                }

                // We've reserved cleanup and completion, so do it.
                CleanupAndComplete(reason);
                return true;
            }

            /// <summary>Cleans up the target for completion.</summary>
            /// <param name="reason">The reason we're completing and cleaning up.</param>
            /// <remarks>This method must only be called once on this instance.</remarks>
            private void CleanupAndComplete(ReceiveCoreByLinkingCleanupReason reason)
            {
                Common.ContractAssertMonitorStatus(IncomingLock, held: false);
                Debug.Assert(Volatile.Read(ref _cleanupReserved), "Should only be called once by whomever reserved the right.");

                // Unlink from the source.  If we're cleaning up because the source
                // completed, this is unnecessary, as the source should have already
                // emptied out its target registry, or at least be in the process of doing so.
                // We are racing with the linking code - only one can dispose of the unlinker.
                IDisposable? unlink = _unlink;
                if (reason != ReceiveCoreByLinkingCleanupReason.SourceCompletion && unlink != null)
                {
                    IDisposable? disposableUnlink = Interlocked.CompareExchange(ref _unlink, null, unlink);
                    if (disposableUnlink != null)
                    {
                        // If an error occurs, fault the target and override the reason to
                        // continue executing, i.e. do the remaining cleanup without completing
                        // the target the way we originally intended to.
                        try
                        {
                            disposableUnlink.Dispose(); // must not be holding IncomingLock, or could deadlock
                        }
                        catch (Exception exc)
                        {
                            _receivedException = exc;
                            reason = ReceiveCoreByLinkingCleanupReason.SourceProtocolError;
                        }
                    }
                }

                // Cleanup the timer.  (Even if we're here because of the timer firing, we still
                // want to aggressively dispose of the timer.)
                if (_timer != null) _timer.Dispose();

                // Cancel the token everyone is listening to.  We also want to unlink
                // from the user-provided cancellation token to prevent a leak.
                // We do *not* dispose of the cts itself here, as there could be a race
                // with the code registering this cleanup delegate with cts; not disposing
                // is ok, though, because there's no resources created by the CTS
                // that needs to be cleaned up since we're not using the wait handle.
                // This is also why we don't use CreateLinkedTokenSource, as that combines
                // both disposing of the token source and disposal of the connection link
                // into a single dispose operation.
                // if we're here because of cancellation, no need to cancel again
                if (reason != ReceiveCoreByLinkingCleanupReason.Cancellation)
                {
                    // if the source complete without receiving a value, we check the cancellation one more time
                    if (reason == ReceiveCoreByLinkingCleanupReason.SourceCompletion &&
                        (_externalCancellationToken.IsCancellationRequested || _cts.IsCancellationRequested))
                    {
                        reason = ReceiveCoreByLinkingCleanupReason.Cancellation;
                    }
                    _cts.Cancel();
                }
                _regFromExternalCancellationToken.Dispose();

                // No need to dispose of the cts, either, as we're not accessing its WaitHandle
                // nor was it created as a linked token source.  Disposing it could also be dangerous
                // if other code tries to access it after we dispose of it... best to leave it available.

                // Complete the task based on the reason
                switch (reason)
                {
                    // Task final state: RanToCompletion
                    case ReceiveCoreByLinkingCleanupReason.Success:
                        System.Threading.Tasks.Task.Factory.StartNew(state =>
                        {
                            // Complete with the received value
                            var target = (ReceiveTarget<T>)state!;
                            try { target.TrySetResult(target._receivedValue!); }
                            catch (ObjectDisposedException) { /* benign race if returned task is already disposed */ }
                        }, this, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
                        break;

                    // Task final state: Canceled
                    case ReceiveCoreByLinkingCleanupReason.Cancellation:
                        System.Threading.Tasks.Task.Factory.StartNew(state =>
                        {
                            // Complete as canceled
                            var target = (ReceiveTarget<T>)state!;
                            try { target.TrySetCanceled(); }
                            catch (ObjectDisposedException) { /* benign race if returned task is already disposed */ }
                        }, this, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
                        break;
                    default:
                        Debug.Assert(false, "Invalid linking cleanup reason specified.");
                        goto case ReceiveCoreByLinkingCleanupReason.Cancellation;

                    // Task final state: Faulted
                    case ReceiveCoreByLinkingCleanupReason.SourceCompletion:
                        if (_receivedException == null) _receivedException = CreateExceptionForSourceCompletion();
                        goto case ReceiveCoreByLinkingCleanupReason.SourceProtocolError;
                    case ReceiveCoreByLinkingCleanupReason.Timer:
                        if (_receivedException == null) _receivedException = CreateExceptionForTimeout();
                        goto case ReceiveCoreByLinkingCleanupReason.SourceProtocolError;
                    case ReceiveCoreByLinkingCleanupReason.SourceProtocolError:
                    case ReceiveCoreByLinkingCleanupReason.ErrorDuringCleanup:
                        System.Threading.Tasks.Task.Factory.StartNew(state =>
                        {
                            // Complete with the received exception
                            var target = (ReceiveTarget<T>)state!;
                            try { target.TrySetException(target._receivedException ?? new InvalidOperationException(SR.InvalidOperation_ErrorDuringCleanup)); }
                            catch (ObjectDisposedException) { /* benign race if returned task is already disposed */ }
                        }, this, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
                        break;
                }
            }

            /// <summary>Creates an exception to use when a source completed before receiving a value.</summary>
            /// <returns>The initialized exception.</returns>
            internal static Exception CreateExceptionForSourceCompletion()
            {
                return Common.InitializeStackTrace(new InvalidOperationException(SR.InvalidOperation_DataNotAvailableForReceive));
            }

            /// <summary>Creates an exception to use when a timeout occurs before receiving a value.</summary>
            /// <returns>The initialized exception.</returns>
            internal static Exception CreateExceptionForTimeout()
            {
                return Common.InitializeStackTrace(new TimeoutException());
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Complete"]/*' />
            void IDataflowBlock.Complete()
            {
                TryCleanupAndComplete(ReceiveCoreByLinkingCleanupReason.SourceCompletion);
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Fault"]/*' />
            void IDataflowBlock.Fault(Exception exception) { ((IDataflowBlock)this).Complete(); }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Completion"]/*' />
            Task IDataflowBlock.Completion { get { throw new NotSupportedException(SR.NotSupported_MemberNotNeeded); } }

            /// <summary>The data to display in the debugger display attribute.</summary>
            private object DebuggerDisplayContent
            {
                get
                {
                    return string.Format("{0} IsCompleted={1}",
                        Common.GetNameForDebugger(this), base.Task.IsCompleted);
                }
            }
            /// <summary>Gets the data to display in the debugger display attribute for this instance.</summary>
            object IDebuggerDisplay.Content { get { return DebuggerDisplayContent; } }
        }
        #endregion
        #endregion

        #region OutputAvailableAsync
        /// <summary>
        /// Provides a <see cref="System.Threading.Tasks.Task{TResult}"/>
        /// that asynchronously monitors the source for available output.
        /// </summary>
        /// <typeparam name="TOutput">Specifies the type of data contained in the source.</typeparam>
        /// <param name="source">The source to monitor.</param>
        /// <returns>
        /// A <see cref="System.Threading.Tasks.Task{Boolean}"/> that informs of whether and when
        /// more output is available.  When the task completes, if its <see cref="System.Threading.Tasks.Task{Boolean}.Result"/> is true, more output
        /// is available in the source (though another consumer of the source may retrieve the data).
        /// If it returns false, more output is not and will never be available, due to the source
        /// completing prior to output being available.
        /// </returns>
        public static Task<bool> OutputAvailableAsync<TOutput>(this ISourceBlock<TOutput> source)
        {
            return OutputAvailableAsync<TOutput>(source, CancellationToken.None);
        }

        /// <summary>
        /// Provides a <see cref="System.Threading.Tasks.Task{TResult}"/>
        /// that asynchronously monitors the source for available output.
        /// </summary>
        /// <typeparam name="TOutput">Specifies the type of data contained in the source.</typeparam>
        /// <param name="source">The source to monitor.</param>
        /// <param name="cancellationToken">The cancellation token with which to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="System.Threading.Tasks.Task{Boolean}"/> that informs of whether and when
        /// more output is available.  When the task completes, if its <see cref="System.Threading.Tasks.Task{Boolean}.Result"/> is true, more output
        /// is available in the source (though another consumer of the source may retrieve the data).
        /// If it returns false, more output is not and will never be available, due to the source
        /// completing prior to output being available.
        /// </returns>
        public static Task<bool> OutputAvailableAsync<TOutput>(
            this ISourceBlock<TOutput> source, CancellationToken cancellationToken)
        {
            // Validate arguments
            if (source == null) throw new ArgumentNullException(nameof(source));

            // Fast path for cancellation
            if (cancellationToken.IsCancellationRequested)
                return Common.CreateTaskFromCancellation<bool>(cancellationToken);

            // In a method like this, normally we would want to check source.Completion.IsCompleted
            // and avoid linking completely by simply returning a completed task.  However,
            // some blocks that are completed still have data available, like WriteOnceBlock,
            // which completes as soon as it gets a value and stores that value forever.
            // As such, OutputAvailableAsync must link from the source so that the source
            // can push data to us if it has it, at which point we can immediately unlink.

            // Create a target task that will complete when it's offered a message (but it won't accept the message)
            var target = new OutputAvailableAsyncTarget<TOutput>();
            try
            {
                // Link from the source.  If the source propagates a message during or immediately after linking
                // such that our target is already completed, just return its task.
                target._unlinker = source.LinkTo(target, DataflowLinkOptions.UnlinkAfterOneAndPropagateCompletion);

                // If the task is already completed (an exception may have occurred, or the source may have propagated
                // a message to the target during LinkTo or soon thereafter), just return the task directly.
                if (target.Task.IsCompleted)
                {
                    return target.Task;
                }

                // If cancellation could be requested, hook everything up to be notified of cancellation requests.
                if (cancellationToken.CanBeCanceled)
                {
                    // When cancellation is requested, unlink the target from the source and cancel the target.
                    target._ctr = cancellationToken.Register(OutputAvailableAsyncTarget<TOutput>.s_cancelAndUnlink, target);
                }

                // We can't return the task directly, as the source block will be completing the task synchronously,
                // and thus any synchronous continuations would run as part of the source block's call.  We don't have to worry
                // about cancellation, as we've coded cancellation to complete the task asynchronously, and with the continuation
                // set as NotOnCanceled, so the continuation will be canceled immediately when the antecedent is canceled, which
                // will thus be asynchronously from the cancellation token source's cancellation call.
                return target.Task.ContinueWith(
                    OutputAvailableAsyncTarget<TOutput>.s_handleCompletion, target,
                    CancellationToken.None, Common.GetContinuationOptions() | TaskContinuationOptions.NotOnCanceled, TaskScheduler.Default);
            }
            catch (Exception exc)
            {
                // Source.LinkTo could throw, as could cancellationToken.Register if cancellation was already requested
                // such that it synchronously invokes the source's unlinker IDisposable, which could throw.
                target.TrySetException(exc);

                // Undo the link from the source to the target
                target.AttemptThreadSafeUnlink();

                // Return the now faulted task
                return target.Task;
            }
        }

        /// <summary>Provides a target used in OutputAvailableAsync operations.</summary>
        /// <typeparam name="T">Specifies the type of data in the data source being checked.</typeparam>
        [DebuggerDisplay("{DebuggerDisplayContent,nq}")]
        private sealed class OutputAvailableAsyncTarget<T> : TaskCompletionSource<bool>, ITargetBlock<T>, IDebuggerDisplay
        {
            /// <summary>
            /// Cached continuation delegate that unregisters from cancellation and
            /// marshals the antecedent's result to the return value.
            /// </summary>
            internal static readonly Func<Task<bool>, object?, bool> s_handleCompletion = (antecedent, state) =>
            {
                var target = state as OutputAvailableAsyncTarget<T>;
                Debug.Assert(target != null, "Expected non-null target");
                target._ctr.Dispose();
                return antecedent.GetAwaiter().GetResult();
            };

            /// <summary>
            /// Cached delegate that cancels the target and unlinks the target from the source.
            /// Expects an OutputAvailableAsyncTarget as the state argument.
            /// </summary>
            internal static readonly Action<object?> s_cancelAndUnlink = CancelAndUnlink;

            /// <summary>Cancels the target and unlinks the target from the source.</summary>
            /// <param name="state">An OutputAvailableAsyncTarget.</param>
            private static void CancelAndUnlink(object? state)
            {
                var target = state as OutputAvailableAsyncTarget<T>;
                Debug.Assert(target != null, "Expected a non-null target");

                // Cancel asynchronously so that we're not completing the task as part of the cts.Cancel() call,
                // since synchronous continuations off that task would then run as part of Cancel.
                // Take advantage of this task and unlink from there to avoid doing the interlocked operation synchronously.
                System.Threading.Tasks.Task.Factory.StartNew(tgt =>
                                                            {
                                                                var thisTarget = (OutputAvailableAsyncTarget<T>)tgt!;
                                                                thisTarget.TrySetCanceled();
                                                                thisTarget.AttemptThreadSafeUnlink();
                                                            },
                    target, CancellationToken.None, Common.GetCreationOptionsForTask(), TaskScheduler.Default);
            }

            /// <summary>Disposes of _unlinker if the target has been linked.</summary>
            internal void AttemptThreadSafeUnlink()
            {
                // A race is possible. Therefore use an interlocked operation.
                IDisposable? cachedUnlinker = _unlinker;
                if (cachedUnlinker != null && Interlocked.CompareExchange(ref _unlinker, null, cachedUnlinker) == cachedUnlinker)
                {
                    cachedUnlinker.Dispose();
                }
            }

            /// <summary>The IDisposable used to unlink this target from its source.</summary>
            internal IDisposable? _unlinker;
            /// <summary>The registration used to unregister this target from the cancellation token.</summary>
            internal CancellationTokenRegistration _ctr;

            /// <summary>Completes the task when offered a message (but doesn't consume the message).</summary>
            DataflowMessageStatus ITargetBlock<T>.OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T>? source, bool consumeToAccept)
            {
                if (!messageHeader.IsValid) throw new ArgumentException(SR.Argument_InvalidMessageHeader, nameof(messageHeader));
                if (source == null) throw new ArgumentNullException(nameof(source));

                TrySetResult(true);
                return DataflowMessageStatus.DecliningPermanently;
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Complete"]/*' />
            void IDataflowBlock.Complete()
            {
                TrySetResult(false);
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Fault"]/*' />
            void IDataflowBlock.Fault(Exception exception)
            {
                if (exception == null) throw new ArgumentNullException(nameof(exception));
                TrySetResult(false);
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Completion"]/*' />
            Task IDataflowBlock.Completion { get { throw new NotSupportedException(SR.NotSupported_MemberNotNeeded); } }

            /// <summary>The data to display in the debugger display attribute.</summary>
            private object DebuggerDisplayContent
            {
                get
                {
                    return string.Format("{0} IsCompleted={1}",
                        Common.GetNameForDebugger(this), base.Task.IsCompleted);
                }
            }
            /// <summary>Gets the data to display in the debugger display attribute for this instance.</summary>
            object IDebuggerDisplay.Content { get { return DebuggerDisplayContent; } }
        }
        #endregion

        #region Encapsulate
        /// <summary>Encapsulates a target and a source into a single propagator.</summary>
        /// <typeparam name="TInput">Specifies the type of input expected by the target.</typeparam>
        /// <typeparam name="TOutput">Specifies the type of output produced by the source.</typeparam>
        /// <param name="target">The target to encapsulate.</param>
        /// <param name="source">The source to encapsulate.</param>
        /// <returns>The encapsulated target and source.</returns>
        /// <remarks>
        /// This method does not in any way connect the target to the source. It creates a
        /// propagator block whose target methods delegate to the specified target and whose
        /// source methods delegate to the specified source.  Any connection between the target
        /// and the source is left for the developer to explicitly provide.  The propagator's
        /// <see cref="IDataflowBlock"/> implementation delegates to the specified source.
        /// </remarks>
        public static IPropagatorBlock<TInput, TOutput> Encapsulate<TInput, TOutput>(
            ITargetBlock<TInput> target, ISourceBlock<TOutput> source)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new EncapsulatingPropagator<TInput, TOutput>(target, source);
        }

        /// <summary>Provides a dataflow block that encapsulates a target and a source to form a single propagator.</summary>
        [DebuggerDisplay("{DebuggerDisplayContent,nq}")]
        [DebuggerTypeProxy(typeof(EncapsulatingPropagator<,>.DebugView))]
        private sealed class EncapsulatingPropagator<TInput, TOutput> : IPropagatorBlock<TInput, TOutput>, IReceivableSourceBlock<TOutput>, IDebuggerDisplay
        {
            /// <summary>The target half.</summary>
            private readonly ITargetBlock<TInput> _target;
            /// <summary>The source half.</summary>
            private readonly ISourceBlock<TOutput> _source;

            public EncapsulatingPropagator(ITargetBlock<TInput> target, ISourceBlock<TOutput> source)
            {
                Debug.Assert(target != null, "The target should never be null; this should be checked by all internal usage.");
                Debug.Assert(source != null, "The source should never be null; this should be checked by all internal usage.");
                _target = target;
                _source = source;
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Complete"]/*' />
            public void Complete()
            {
                _target.Complete();
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Fault"]/*' />
            void IDataflowBlock.Fault(Exception exception)
            {
                if (exception == null) throw new ArgumentNullException(nameof(exception));

                _target.Fault(exception);
            }
            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Targets/Member[@name="OfferMessage"]/*' />
            public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, TInput messageValue, ISourceBlock<TInput>? source, bool consumeToAccept)
            {
                return _target.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Completion"]/*' />
            public Task Completion { get { return _source.Completion; } }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="LinkTo"]/*' />
            public IDisposable LinkTo(ITargetBlock<TOutput> target, DataflowLinkOptions linkOptions)
            {
                return _source.LinkTo(target, linkOptions);
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="TryReceive"]/*' />
            public bool TryReceive(Predicate<TOutput>? filter, [MaybeNullWhen(false)] out TOutput item)
            {
                var receivableSource = _source as IReceivableSourceBlock<TOutput>;
                if (receivableSource != null) return receivableSource.TryReceive(filter, out item);

                item = default(TOutput);
                return false;
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="TryReceiveAll"]/*' />
            public bool TryReceiveAll([NotNullWhen(true)] out IList<TOutput>? items)
            {
                var receivableSource = _source as IReceivableSourceBlock<TOutput>;
                if (receivableSource != null) return receivableSource.TryReceiveAll(out items);

                items = default(IList<TOutput>);
                return false;
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="ConsumeMessage"]/*' />
            public TOutput? ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target, out bool messageConsumed)
            {
                return _source.ConsumeMessage(messageHeader, target, out messageConsumed);
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="ReserveMessage"]/*' />
            public bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
            {
                return _source.ReserveMessage(messageHeader, target);
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="ReleaseReservation"]/*' />
            public void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
            {
                _source.ReleaseReservation(messageHeader, target);
            }

            /// <summary>The data to display in the debugger display attribute.</summary>
            private object DebuggerDisplayContent
            {
                get
                {
                    var displayTarget = _target as IDebuggerDisplay;
                    var displaySource = _source as IDebuggerDisplay;
                    return string.Format("{0} Target=\"{1}\", Source=\"{2}\"",
                        Common.GetNameForDebugger(this),
                        displayTarget != null ? displayTarget.Content : _target,
                        displaySource != null ? displaySource.Content : _source);
                }
            }
            /// <summary>Gets the data to display in the debugger display attribute for this instance.</summary>
            object IDebuggerDisplay.Content { get { return DebuggerDisplayContent; } }

            /// <summary>A debug view for the propagator.</summary>
            private sealed class DebugView
            {
                /// <summary>The propagator being debugged.</summary>
                private readonly EncapsulatingPropagator<TInput, TOutput> _propagator;

                /// <summary>Initializes the debug view.</summary>
                /// <param name="propagator">The propagator being debugged.</param>
                public DebugView(EncapsulatingPropagator<TInput, TOutput> propagator)
                {
                    Debug.Assert(propagator != null, "Need a block with which to construct the debug view.");
                    _propagator = propagator;
                }

                /// <summary>The target.</summary>
                public ITargetBlock<TInput> Target { get { return _propagator._target; } }
                /// <summary>The source.</summary>
                public ISourceBlock<TOutput> Source { get { return _propagator._source; } }
            }
        }
        #endregion

        #region Choose
        #region Choose<T1,T2>
        /// <summary>Monitors two dataflow sources, invoking the provided handler for whichever source makes data available first.</summary>
        /// <typeparam name="T1">Specifies type of data contained in the first source.</typeparam>
        /// <typeparam name="T2">Specifies type of data contained in the second source.</typeparam>
        /// <param name="source1">The first source.</param>
        /// <param name="action1">The handler to execute on data from the first source.</param>
        /// <param name="source2">The second source.</param>
        /// <param name="action2">The handler to execute on data from the second source.</param>
        /// <returns>
        /// <para>
        /// A <see cref="System.Threading.Tasks.Task{Int32}"/> that represents the asynchronous choice.
        /// If both sources are completed prior to the choice completing,
        /// the resulting task will be canceled. When one of the sources has data available and successfully propagates
        /// it to the choice, the resulting task will complete when the handler completes: if the handler throws an exception,
        /// the task will end in the <see cref="System.Threading.Tasks.TaskStatus.Faulted"/> state containing the unhandled exception, otherwise the task
        /// will end with its <see cref="System.Threading.Tasks.Task{Int32}.Result"/> set to either 0 or 1 to
        /// represent the first or second source, respectively.
        /// </para>
        /// <para>
        /// This method will only consume an element from one of the two data sources, never both.
        /// </para>
        /// </returns>
        /// <exception cref="System.ArgumentNullException">The <paramref name="source1"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="action1"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="source2"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="action2"/> is null (Nothing in Visual Basic).</exception>
        public static Task<int> Choose<T1, T2>(
            ISourceBlock<T1> source1, Action<T1> action1,
            ISourceBlock<T2> source2, Action<T2> action2)
        {
            // All argument validation is handled by the delegated method
            return Choose(source1, action1, source2, action2, DataflowBlockOptions.Default);
        }

        /// <summary>Monitors two dataflow sources, invoking the provided handler for whichever source makes data available first.</summary>
        /// <typeparam name="T1">Specifies type of data contained in the first source.</typeparam>
        /// <typeparam name="T2">Specifies type of data contained in the second source.</typeparam>
        /// <param name="source1">The first source.</param>
        /// <param name="action1">The handler to execute on data from the first source.</param>
        /// <param name="source2">The second source.</param>
        /// <param name="action2">The handler to execute on data from the second source.</param>
        /// <param name="dataflowBlockOptions">The options with which to configure this choice.</param>
        /// <returns>
        /// <para>
        /// A <see cref="System.Threading.Tasks.Task{Int32}"/> that represents the asynchronous choice.
        /// If both sources are completed prior to the choice completing, or if the CancellationToken
        /// provided as part of <paramref name="dataflowBlockOptions"/> is canceled prior to the choice completing,
        /// the resulting task will be canceled. When one of the sources has data available and successfully propagates
        /// it to the choice, the resulting task will complete when the handler completes: if the handler throws an exception,
        /// the task will end in the <see cref="System.Threading.Tasks.TaskStatus.Faulted"/> state containing the unhandled exception, otherwise the task
        /// will end with its <see cref="System.Threading.Tasks.Task{Int32}.Result"/> set to either 0 or 1 to
        /// represent the first or second source, respectively.
        /// </para>
        /// <para>
        /// This method will only consume an element from one of the two data sources, never both.
        /// If cancellation is requested after an element has been received, the cancellation request will be ignored,
        /// and the relevant handler will be allowed to execute.
        /// </para>
        /// </returns>
        /// <exception cref="System.ArgumentNullException">The <paramref name="source1"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="action1"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="source2"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="action2"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="dataflowBlockOptions"/> is null (Nothing in Visual Basic).</exception>
        public static Task<int> Choose<T1, T2>(
            ISourceBlock<T1> source1, Action<T1> action1,
            ISourceBlock<T2> source2, Action<T2> action2,
            DataflowBlockOptions dataflowBlockOptions)
        {
            // Validate arguments
            if (source1 == null) throw new ArgumentNullException(nameof(source1));
            if (action1 == null) throw new ArgumentNullException(nameof(action1));
            if (source2 == null) throw new ArgumentNullException(nameof(source2));
            if (action2 == null) throw new ArgumentNullException(nameof(action2));
            if (dataflowBlockOptions == null) throw new ArgumentNullException(nameof(dataflowBlockOptions));

            // Delegate to the shared implementation
            return ChooseCore<T1, T2, VoidResult>(source1, action1, source2, action2, null, null, dataflowBlockOptions);
        }
        #endregion

        #region Choose<T1,T2,T3>
        /// <summary>Monitors three dataflow sources, invoking the provided handler for whichever source makes data available first.</summary>
        /// <typeparam name="T1">Specifies type of data contained in the first source.</typeparam>
        /// <typeparam name="T2">Specifies type of data contained in the second source.</typeparam>
        /// <typeparam name="T3">Specifies type of data contained in the third source.</typeparam>
        /// <param name="source1">The first source.</param>
        /// <param name="action1">The handler to execute on data from the first source.</param>
        /// <param name="source2">The second source.</param>
        /// <param name="action2">The handler to execute on data from the second source.</param>
        /// <param name="source3">The third source.</param>
        /// <param name="action3">The handler to execute on data from the third source.</param>
        /// <returns>
        /// <para>
        /// A <see cref="System.Threading.Tasks.Task{Int32}"/> that represents the asynchronous choice.
        /// If all sources are completed prior to the choice completing,
        /// the resulting task will be canceled. When one of the sources has data available and successfully propagates
        /// it to the choice, the resulting task will complete when the handler completes: if the handler throws an exception,
        /// the task will end in the <see cref="System.Threading.Tasks.TaskStatus.Faulted"/> state containing the unhandled exception, otherwise the task
        /// will end with its <see cref="System.Threading.Tasks.Task{Int32}.Result"/> set to the 0-based index of the source.
        /// </para>
        /// <para>
        /// This method will only consume an element from one of the data sources, never more than one.
        /// </para>
        /// </returns>
        /// <exception cref="System.ArgumentNullException">The <paramref name="source1"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="action1"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="source2"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="action2"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="source3"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="action3"/> is null (Nothing in Visual Basic).</exception>
        public static Task<int> Choose<T1, T2, T3>(
            ISourceBlock<T1> source1, Action<T1> action1,
            ISourceBlock<T2> source2, Action<T2> action2,
            ISourceBlock<T3> source3, Action<T3> action3)
        {
            // All argument validation is handled by the delegated method
            return Choose(source1, action1, source2, action2, source3, action3, DataflowBlockOptions.Default);
        }

        /// <summary>Monitors three dataflow sources, invoking the provided handler for whichever source makes data available first.</summary>
        /// <typeparam name="T1">Specifies type of data contained in the first source.</typeparam>
        /// <typeparam name="T2">Specifies type of data contained in the second source.</typeparam>
        /// <typeparam name="T3">Specifies type of data contained in the third source.</typeparam>
        /// <param name="source1">The first source.</param>
        /// <param name="action1">The handler to execute on data from the first source.</param>
        /// <param name="source2">The second source.</param>
        /// <param name="action2">The handler to execute on data from the second source.</param>
        /// <param name="source3">The third source.</param>
        /// <param name="action3">The handler to execute on data from the third source.</param>
        /// <param name="dataflowBlockOptions">The options with which to configure this choice.</param>
        /// <returns>
        /// <para>
        /// A <see cref="System.Threading.Tasks.Task{Int32}"/> that represents the asynchronous choice.
        /// If all sources are completed prior to the choice completing, or if the CancellationToken
        /// provided as part of <paramref name="dataflowBlockOptions"/> is canceled prior to the choice completing,
        /// the resulting task will be canceled. When one of the sources has data available and successfully propagates
        /// it to the choice, the resulting task will complete when the handler completes: if the handler throws an exception,
        /// the task will end in the <see cref="System.Threading.Tasks.TaskStatus.Faulted"/> state containing the unhandled exception, otherwise the task
        /// will end with its <see cref="System.Threading.Tasks.Task{Int32}.Result"/> set to the 0-based index of the source.
        /// </para>
        /// <para>
        /// This method will only consume an element from one of the data sources, never more than one.
        /// If cancellation is requested after an element has been received, the cancellation request will be ignored,
        /// and the relevant handler will be allowed to execute.
        /// </para>
        /// </returns>
        /// <exception cref="System.ArgumentNullException">The <paramref name="source1"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="action1"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="source2"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="action2"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="source3"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="action3"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="dataflowBlockOptions"/> is null (Nothing in Visual Basic).</exception>
        public static Task<int> Choose<T1, T2, T3>(
            ISourceBlock<T1> source1, Action<T1> action1,
            ISourceBlock<T2> source2, Action<T2> action2,
            ISourceBlock<T3> source3, Action<T3> action3,
            DataflowBlockOptions dataflowBlockOptions)
        {
            // Validate arguments
            if (source1 == null) throw new ArgumentNullException(nameof(source1));
            if (action1 == null) throw new ArgumentNullException(nameof(action1));
            if (source2 == null) throw new ArgumentNullException(nameof(source2));
            if (action2 == null) throw new ArgumentNullException(nameof(action2));
            if (source3 == null) throw new ArgumentNullException(nameof(source3));
            if (action3 == null) throw new ArgumentNullException(nameof(action3));
            if (dataflowBlockOptions == null) throw new ArgumentNullException(nameof(dataflowBlockOptions));

            // Delegate to the shared implementation
            return ChooseCore<T1, T2, T3>(source1, action1, source2, action2, source3, action3, dataflowBlockOptions);
        }
        #endregion

        #region Choose Shared
        /// <summary>Monitors dataflow sources, invoking the provided handler for whichever source makes data available first.</summary>
        /// <typeparam name="T1">Specifies type of data contained in the first source.</typeparam>
        /// <typeparam name="T2">Specifies type of data contained in the second source.</typeparam>
        /// <typeparam name="T3">Specifies type of data contained in the third source.</typeparam>
        /// <param name="source1">The first source.</param>
        /// <param name="action1">The handler to execute on data from the first source.</param>
        /// <param name="source2">The second source.</param>
        /// <param name="action2">The handler to execute on data from the second source.</param>
        /// <param name="source3">The third source.</param>
        /// <param name="action3">The handler to execute on data from the third source.</param>
        /// <param name="dataflowBlockOptions">The options with which to configure this choice.</param>
        private static Task<int> ChooseCore<T1, T2, T3>(
            ISourceBlock<T1> source1, Action<T1> action1,
            ISourceBlock<T2> source2, Action<T2> action2,
            ISourceBlock<T3>? source3, Action<T3>? action3,
            DataflowBlockOptions dataflowBlockOptions)
        {
            Debug.Assert(source1 != null && action1 != null, "The first source and action should not be null.");
            Debug.Assert(source2 != null && action2 != null, "The second source and action should not be null.");
            Debug.Assert((source3 == null) == (action3 == null), "The third action should be null iff the third source is null.");
            Debug.Assert(dataflowBlockOptions != null, "Options are required.");
            bool hasThirdSource = source3 != null; // In the future, if we want higher arities on Choose, we can simply add more such checks on additional arguments

            // Early cancellation check and bail out
            if (dataflowBlockOptions.CancellationToken.IsCancellationRequested)
                return Common.CreateTaskFromCancellation<int>(dataflowBlockOptions.CancellationToken);

            // Fast path: if any of the sources already has data available that can be received immediately.
            Task<int>? resultTask;
            try
            {
                TaskScheduler scheduler = dataflowBlockOptions.TaskScheduler;
                if (TryChooseFromSource(source1, action1, 0, scheduler, out resultTask) ||
                    TryChooseFromSource(source2, action2, 1, scheduler, out resultTask) ||
                    (hasThirdSource && TryChooseFromSource(source3!, action3!, 2, scheduler, out resultTask)))
                {
                    return resultTask;
                }
            }
            catch (Exception exc)
            {
                // In case TryReceive in TryChooseFromSource erroneously throws
                return Common.CreateTaskFromException<int>(exc);
            }

            // Slow path: link up to all of the sources.  Separated out to avoid a closure on the fast path.
            return ChooseCoreByLinking(source1, action1, source2, action2, source3, action3, dataflowBlockOptions);
        }

        /// <summary>
        /// Tries to remove data from a receivable source and schedule an action to process that received item.
        /// </summary>
        /// <typeparam name="T">Specifies the type of data to process.</typeparam>
        /// <param name="source">The source from which to receive the data.</param>
        /// <param name="action">The action to run for the received data.</param>
        /// <param name="branchId">The branch ID associated with this source/action pair.</param>
        /// <param name="scheduler">The scheduler to use to process the action.</param>
        /// <param name="task">The task created for processing the received item.</param>
        /// <returns>true if this try attempt satisfies the choose operation; otherwise, false.</returns>
        private static bool TryChooseFromSource<T>(
            ISourceBlock<T> source, Action<T> action, int branchId, TaskScheduler scheduler,
            [NotNullWhen(true)] out Task<int>? task)
        {
            // Validate arguments
            Debug.Assert(source != null, "Expected a non-null source");
            Debug.Assert(action != null, "Expected a non-null action");
            Debug.Assert(branchId >= 0, "Expected a valid branch ID (> 0)");
            Debug.Assert(scheduler != null, "Expected a non-null scheduler");

            // Try to receive from the source.  If we can't, bail.
            T? result;
            var receivableSource = source as IReceivableSourceBlock<T>;
            if (receivableSource == null || !receivableSource.TryReceive(out result))
            {
                task = null;
                return false;
            }

            // We successfully received an item.  Launch a task to process it.
            task = Task.Factory.StartNew(ChooseTarget<T>.s_processBranchFunction,
                Tuple.Create<Action<T>, T, int>(action, result, branchId),
                CancellationToken.None, Common.GetCreationOptionsForTask(), scheduler);
            return true;
        }

        /// <summary>Monitors dataflow sources, invoking the provided handler for whichever source makes data available first.</summary>
        /// <typeparam name="T1">Specifies type of data contained in the first source.</typeparam>
        /// <typeparam name="T2">Specifies type of data contained in the second source.</typeparam>
        /// <typeparam name="T3">Specifies type of data contained in the third source.</typeparam>
        /// <param name="source1">The first source.</param>
        /// <param name="action1">The handler to execute on data from the first source.</param>
        /// <param name="source2">The second source.</param>
        /// <param name="action2">The handler to execute on data from the second source.</param>
        /// <param name="source3">The third source.</param>
        /// <param name="action3">The handler to execute on data from the third source.</param>
        /// <param name="dataflowBlockOptions">The options with which to configure this choice.</param>
        private static Task<int> ChooseCoreByLinking<T1, T2, T3>(
            ISourceBlock<T1> source1, Action<T1> action1,
            ISourceBlock<T2> source2, Action<T2> action2,
            ISourceBlock<T3>? source3, Action<T3>? action3,
            DataflowBlockOptions dataflowBlockOptions)
        {
            Debug.Assert(source1 != null && action1 != null, "The first source and action should not be null.");
            Debug.Assert(source2 != null && action2 != null, "The second source and action should not be null.");
            Debug.Assert((source3 == null) == (action3 == null), "The third action should be null iff the third source is null.");
            Debug.Assert(dataflowBlockOptions != null, "Options are required.");

            bool hasThirdSource = source3 != null; // In the future, if we want higher arities on Choose, we can simply add more such checks on additional arguments

            // Create object to act as both completion marker and sync obj for targets.
            var boxedCompleted = new StrongBox<Task>();

            // Set up teardown cancellation.  We will request cancellation when a) the supplied options token
            // has cancellation requested or b) when we actually complete somewhere in order to tear down
            // the rest of our configured set up.
            CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(dataflowBlockOptions.CancellationToken, CancellationToken.None);

            // Set up the branches.
            TaskScheduler scheduler = dataflowBlockOptions.TaskScheduler;
            var branchTasks = new Task<int>[hasThirdSource ? 3 : 2];
            branchTasks[0] = CreateChooseBranch(boxedCompleted, cts, scheduler, 0, source1, action1);
            branchTasks[1] = CreateChooseBranch(boxedCompleted, cts, scheduler, 1, source2, action2);
            if (hasThirdSource)
            {
                branchTasks[2] = CreateChooseBranch(boxedCompleted, cts, scheduler, 2, source3!, action3!);
            }

            // Asynchronously wait for all branches to complete, then complete
            // a task to be returned to the caller.
            var result = new TaskCompletionSource<int>();
            Task.Factory.ContinueWhenAll(branchTasks, tasks =>
            {
                // Process the outcome of all branches.  At most one will have completed
                // successfully, returning its branch ID.  Others may have faulted,
                // in which case we need to propagate their exceptions, regardless
                // of whether a branch completed successfully.  Others may have been
                // canceled (or run but found they were not needed), and those
                // we just ignore.
                List<Exception>? exceptions = null;
                int successfulBranchId = -1;
                foreach (Task<int> task in tasks)
                {
                    switch (task.Status)
                    {
                        case TaskStatus.Faulted:
                            Common.AddException(ref exceptions, task.Exception!, unwrapInnerExceptions: true);
                            break;
                        case TaskStatus.RanToCompletion:
                            int resultBranchId = task.Result;
                            if (resultBranchId >= 0)
                            {
                                Debug.Assert(resultBranchId < tasks.Length, "Expected a valid branch ID");
                                Debug.Assert(successfulBranchId == -1, "There should be at most one successful branch.");
                                successfulBranchId = resultBranchId;
                            }
                            else Debug.Assert(resultBranchId == -1, "Expected -1 as a signal of a non-successful branch");
                            break;
                    }
                }

                // If we found any exceptions, fault the Choose task.  Otherwise, if any branch completed
                // successfully, store its result, or if cancellation was request
                if (exceptions != null)
                {
                    result.TrySetException(exceptions);
                }
                else if (successfulBranchId >= 0)
                {
                    result.TrySetResult(successfulBranchId);
                }
                else
                {
                    result.TrySetCanceled();
                }

                // By now we know that all of the tasks have completed, so there
                // can't be any more use of the CancellationTokenSource.
                cts.Dispose();
            }, CancellationToken.None, Common.GetContinuationOptions(), TaskScheduler.Default);
            return result.Task;
        }

        /// <summary>Creates a target for a branch of a Choose.</summary>
        /// <typeparam name="T">Specifies the type of data coming through this branch.</typeparam>
        /// <param name="boxedCompleted">A strong box around the completed Task from any target. Also sync obj for access to the targets.</param>
        /// <param name="cts">The CancellationTokenSource used to issue tear down / cancellation requests.</param>
        /// <param name="scheduler">The TaskScheduler on which to scheduler work.</param>
        /// <param name="branchId">The ID of this branch, used to complete the resultTask.</param>
        /// <param name="source">The source with which this branch is associated.</param>
        /// <param name="action">The action to run for a single element received from the source.</param>
        /// <returns>A task representing the branch.</returns>
        private static Task<int> CreateChooseBranch<T>(
            StrongBox<Task> boxedCompleted, CancellationTokenSource cts,
            TaskScheduler scheduler,
            int branchId, ISourceBlock<T> source, Action<T> action)
        {
            // If the cancellation token is already canceled, there is no need to create and link a target.
            // Instead, directly return a canceled task.
            if (cts.IsCancellationRequested)
                return Common.CreateTaskFromCancellation<int>(cts.Token);

            // Proceed with creating and linking a hidden target. Also get the source's completion task,
            // as we need it to know when the source completes.  Both of these operations
            // could throw an exception if the block is faulty.
            var target = new ChooseTarget<T>(boxedCompleted, cts.Token);
            IDisposable unlink;
            try
            {
                unlink = source.LinkTo(target, DataflowLinkOptions.UnlinkAfterOneAndPropagateCompletion);
            }
            catch (Exception exc)
            {
                cts.Cancel();
                return Common.CreateTaskFromException<int>(exc);
            }

            // The continuation task below is implicitly capturing the right execution context,
            // as CreateChooseBranch is called synchronously from Choose, so we
            // don't need to additionally capture and marshal an ExecutionContext.

            return target.Task.ContinueWith(completed =>
            {
                try
                {
                    // If the target ran to completion, i.e. it got a message,
                    // cancel the other branch(es) and proceed with the user callback.
                    if (completed.Status == TaskStatus.RanToCompletion)
                    {
                        // Cancel the cts to trigger completion of the other branches.
                        cts.Cancel();

                        // Proceed with the user callback.
                        action(completed.Result);

                        // Return the ID of our branch to indicate.
                        return branchId;
                    }
                    return -1;
                }
                finally
                {
                    // Unlink from the source.  This could throw if the block is faulty,
                    // in which case our branch's task will fault.  If this
                    // does throw, it'll end up propagating instead of the
                    // original action's exception if there was one.
                    unlink.Dispose();
                }
            }, CancellationToken.None, Common.GetContinuationOptions(), scheduler);
        }

        /// <summary>Provides a dataflow target used by Choose to receive data from a single source.</summary>
        /// <typeparam name="T">Specifies the type of data offered to this target.</typeparam>
        [DebuggerDisplay("{DebuggerDisplayContent,nq}")]
        private sealed class ChooseTarget<T> : TaskCompletionSource<T>, ITargetBlock<T>, IDebuggerDisplay
        {
            /// <summary>
            /// Delegate used to invoke the action for a branch when that branch is activated
            /// on the fast path.
            /// </summary>
            internal static readonly Func<object?, int> s_processBranchFunction = state =>
            {
                Tuple<Action<T>, T, int> actionResultBranch = (Tuple<Action<T>, T, int>)state!;
                actionResultBranch.Item1(actionResultBranch.Item2);
                return actionResultBranch.Item3;
            };

            /// <summary>
            /// A wrapper for the task that represents the completed branch of this choice.
            /// The wrapper is also the sync object used to protect all choice branch's access to shared state.
            /// </summary>
            private readonly StrongBox<Task> _completed;

            /// <summary>Initializes the target.</summary>
            /// <param name="completed">The completed wrapper shared between all choice branches.</param>
            /// <param name="cancellationToken">The cancellation token used to cancel this target.</param>
            internal ChooseTarget(StrongBox<Task> completed, CancellationToken cancellationToken)
            {
                Debug.Assert(completed != null, "Requires a shared target to complete.");
                _completed = completed;

                // Handle async cancellation by canceling the target without storing it into _completed.
                // _completed must only be set to a RanToCompletion task for a successful branch.
                Common.WireCancellationToComplete(cancellationToken, base.Task,
                    state =>
                    {
                        var thisChooseTarget = (ChooseTarget<T>)state!;
                        lock (thisChooseTarget._completed) thisChooseTarget.TrySetCanceled();
                    }, this);
            }

            /// <summary>Called when this choice branch is being offered a message.</summary>
            public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T>? source, bool consumeToAccept)
            {
                // Validate arguments
                if (!messageHeader.IsValid) throw new ArgumentException(SR.Argument_InvalidMessageHeader, nameof(messageHeader));
                if (source == null && consumeToAccept) throw new ArgumentException(SR.Argument_CantConsumeFromANullSource, nameof(consumeToAccept));

                lock (_completed)
                {
                    // If we or another participating choice has already completed, we're done.
                    if (_completed.Value != null || base.Task.IsCompleted) return DataflowMessageStatus.DecliningPermanently;

                    // Consume the message from the source if necessary
                    if (consumeToAccept)
                    {
                        bool consumed;
                        messageValue = source!.ConsumeMessage(messageHeader, this, out consumed)!;
                        if (!consumed) return DataflowMessageStatus.NotAvailable;
                    }

                    // Store the result and signal our success
                    TrySetResult(messageValue!);
                    _completed.Value = Task;
                    return DataflowMessageStatus.Accepted;
                }
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Complete"]/*' />
            void IDataflowBlock.Complete()
            {
                lock (_completed) TrySetCanceled();
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Fault"]/*' />
            void IDataflowBlock.Fault(Exception exception) { ((IDataflowBlock)this).Complete(); }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Completion"]/*' />
            Task IDataflowBlock.Completion { get { throw new NotSupportedException(SR.NotSupported_MemberNotNeeded); } }

            /// <summary>The data to display in the debugger display attribute.</summary>
            private object DebuggerDisplayContent
            {
                get
                {
                    return string.Format("{0} IsCompleted={1}",
                        Common.GetNameForDebugger(this), base.Task.IsCompleted);
                }
            }
            /// <summary>Gets the data to display in the debugger display attribute for this instance.</summary>
            object IDebuggerDisplay.Content { get { return DebuggerDisplayContent; } }
        }
        #endregion
        #endregion

        #region AsObservable
        /// <summary>Creates a new <see cref="System.IObservable{TOutput}"/> abstraction over the <see cref="ISourceBlock{TOutput}"/>.</summary>
        /// <typeparam name="TOutput">Specifies the type of data contained in the source.</typeparam>
        /// <param name="source">The source to wrap.</param>
        /// <returns>An IObservable{TOutput} that enables observers to be subscribed to the source.</returns>
        /// <exception cref="System.ArgumentNullException">The <paramref name="source"/> is null (Nothing in Visual Basic).</exception>
        public static IObservable<TOutput> AsObservable<TOutput>(this ISourceBlock<TOutput> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return SourceObservable<TOutput>.From(source);
        }

        /// <summary>Cached options for non-greedy processing.</summary>
        private static readonly ExecutionDataflowBlockOptions _nonGreedyExecutionOptions = new ExecutionDataflowBlockOptions { BoundedCapacity = 1 };

        /// <summary>Provides an IObservable veneer over a source block.</summary>
        [DebuggerDisplay("{DebuggerDisplayContent,nq}")]
        [DebuggerTypeProxy(typeof(SourceObservable<>.DebugView))]
        private sealed class SourceObservable<TOutput> : IObservable<TOutput>, IDebuggerDisplay
        {
            /// <summary>The table that maps source to cached observable.</summary>
            /// <remarks>
            /// ConditionalWeakTable doesn't do the initialization under a lock, just the publication.
            /// This means that if there's a race to create two observables off the same source, we could end
            /// up instantiating multiple SourceObservable instances, of which only one will be published.
            /// Worst case, we end up with a few additional continuations off of the source's completion task.
            /// </remarks>
            private static readonly ConditionalWeakTable<ISourceBlock<TOutput>, SourceObservable<TOutput>> _table =
                new ConditionalWeakTable<ISourceBlock<TOutput>, SourceObservable<TOutput>>();

            /// <summary>Gets an observable to represent the source block.</summary>
            /// <param name="source">The source.</param>
            /// <returns>The observable.</returns>
            internal static IObservable<TOutput> From(ISourceBlock<TOutput> source)
            {
                Debug.Assert(source != null, "Requires a source for which to retrieve the observable.");
                return _table.GetValue(source, s => new SourceObservable<TOutput>(s));
            }

            /// <summary>Object used to synchronize all subscriptions, unsubscriptions, and propagations.</summary>
            private readonly object _SubscriptionLock = new object();
            /// <summary>The wrapped source.</summary>
            private readonly ISourceBlock<TOutput> _source;
            /// <summary>
            /// The current target.  We use the same target until the number of subscribers
            /// drops to 0, at which point we substitute in a new target.
            /// </summary>
            private ObserversState _observersState;

            /// <summary>Initializes the SourceObservable.</summary>
            /// <param name="source">The source to wrap.</param>
            internal SourceObservable(ISourceBlock<TOutput> source)
            {
                Debug.Assert(source != null, "The observable requires a source to wrap.");
                _source = source;
                _observersState = new ObserversState(this);
            }

            /// <summary>Gets any exceptions from the source block.</summary>
            /// <returns>The aggregate exception of all errors, or null if everything completed successfully.</returns>
            private AggregateException? GetCompletionError()
            {
                Task? sourceCompletionTask = Common.GetPotentiallyNotSupportedCompletionTask(_source);
                return sourceCompletionTask != null && sourceCompletionTask.IsFaulted ?
                    sourceCompletionTask.Exception : null;
            }

            /// <summary>Subscribes the observer to the source.</summary>
            /// <param name="observer">the observer to subscribe.</param>
            /// <returns>An IDisposable that may be used to unsubscribe the source.</returns>
            IDisposable IObservable<TOutput>.Subscribe(IObserver<TOutput> observer)
            {
                // Validate arguments
                if (observer == null) throw new ArgumentNullException(nameof(observer));
                Common.ContractAssertMonitorStatus(_SubscriptionLock, held: false);

                Task? sourceCompletionTask = Common.GetPotentiallyNotSupportedCompletionTask(_source);

                // Synchronize all observers for this source.
                Exception? error = null;
                lock (_SubscriptionLock)
                {
                    // Fast path for if everything is already done.  We need to ensure that both
                    // the source is complete and that the target has finished propagating data to all observers.
                    // If there  was an error, we grab it here and then we'll complete the observer
                    // outside of the lock.
                    if (sourceCompletionTask != null && sourceCompletionTask.IsCompleted &&
                        _observersState.Target.Completion.IsCompleted)
                    {
                        error = GetCompletionError();
                    }
                    // Otherwise, we need to subscribe this observer.
                    else
                    {
                        // Hook up the observer.  If this is the first observer, link the source to the target.
                        _observersState.Observers = _observersState.Observers.Add(observer);
                        if (_observersState.Observers.Count == 1)
                        {
                            Debug.Assert(_observersState.Unlinker == null, "The source should not be linked to the target.");
                            _observersState.Unlinker = _source.LinkTo(_observersState.Target);
                            if (_observersState.Unlinker == null)
                            {
                                _observersState.Observers = ImmutableArray<IObserver<TOutput>>.Empty;
                                return Disposables.Nop;
                            }
                        }

                        // Return a disposable that will unlink this observer, and if it's the last
                        // observer for the source, shut off the pipe to observers.
                        return Disposables.Create((s, o) => s.Unsubscribe(o), this, observer);
                    }
                }

                // Complete the observer.
                if (error != null) observer.OnError(error);
                else observer.OnCompleted();
                return Disposables.Nop;
            }

            /// <summary>Unsubscribes the observer.</summary>
            /// <param name="observer">The observer being unsubscribed.</param>
            private void Unsubscribe(IObserver<TOutput> observer)
            {
                Debug.Assert(observer != null, "Expected an observer.");
                Common.ContractAssertMonitorStatus(_SubscriptionLock, held: false);

                lock (_SubscriptionLock)
                {
                    ObserversState currentState = _observersState;
                    Debug.Assert(currentState != null, "Observer state should never be null.");

                    // If the observer was already unsubscribed (or is otherwise no longer present in our list), bail.
                    if (!currentState.Observers.Contains(observer)) return;

                    // If this is the last observer being removed, reset to be ready for future subscribers.
                    if (currentState.Observers.Count == 1)
                    {
                        ResetObserverState();
                    }
                    // Otherwise, just remove the observer.  Note that we don't remove the observer
                    // from the current target if this is the last observer. This is done in case the target
                    // has already taken data from the source: we want that data to end up somewhere,
                    // and we can't put it back in the source, so we ensure we send it along to the observer.
                    else
                    {
                        currentState.Observers = currentState.Observers.Remove(observer);
                    }
                }
            }

            /// <summary>Resets the observer state to the original, inactive state.</summary>
            /// <returns>The list of active observers prior to the reset.</returns>
            private ImmutableArray<IObserver<TOutput>> ResetObserverState()
            {
                Common.ContractAssertMonitorStatus(_SubscriptionLock, held: true);

                ObserversState currentState = _observersState;
                Debug.Assert(currentState != null, "Observer state should never be null.");
                Debug.Assert(currentState.Unlinker != null, "The target should be linked.");
                Debug.Assert(currentState.Canceler != null, "The target should have set up continuations.");

                // Replace the target with a clean one, unlink and cancel, and return the previous set of observers
                ImmutableArray<IObserver<TOutput>> currentObservers = currentState.Observers;
                _observersState = new ObserversState(this);
                currentState.Unlinker.Dispose();
                currentState.Canceler.Cancel();
                return currentObservers;
            }

            /// <summary>The data to display in the debugger display attribute.</summary>
            private object DebuggerDisplayContent
            {
                get
                {
                    var displaySource = _source as IDebuggerDisplay;
                    return string.Format("Observers={0}, Block=\"{1}\"",
                        _observersState.Observers.Count,
                        displaySource != null ? displaySource.Content : _source);
                }
            }
            /// <summary>Gets the data to display in the debugger display attribute for this instance.</summary>
            object IDebuggerDisplay.Content { get { return DebuggerDisplayContent; } }

            /// <summary>Provides a debugger type proxy for the observable.</summary>
            private sealed class DebugView
            {
                /// <summary>The observable being debugged.</summary>
                private readonly SourceObservable<TOutput> _observable;

                /// <summary>Initializes the debug view.</summary>
                /// <param name="observable">The target being debugged.</param>
                public DebugView(SourceObservable<TOutput> observable)
                {
                    Debug.Assert(observable != null, "Need a block with which to construct the debug view.");
                    _observable = observable;
                }

                /// <summary>Gets an enumerable of the observers.</summary>
                [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
                public IObserver<TOutput>[] Observers { get { return _observable._observersState.Observers.ToArray(); } }
            }

            /// <summary>State associated with the current target for propagating data to observers.</summary>
            private sealed class ObserversState
            {
                /// <summary>The owning SourceObservable.</summary>
                internal readonly SourceObservable<TOutput> Observable;
                /// <summary>The ActionBlock that consumes data from a source and offers it to targets.</summary>
                internal readonly ActionBlock<TOutput> Target;
                /// <summary>Used to cancel continuations when they're no longer necessary.</summary>
                internal readonly CancellationTokenSource Canceler = new CancellationTokenSource();
                /// <summary>
                /// A list of the observers currently registered with this target.  The list is immutable
                /// to enable iteration through the list while the set of observers may be changing.
                /// </summary>
                internal ImmutableArray<IObserver<TOutput>> Observers = ImmutableArray<IObserver<TOutput>>.Empty;
                /// <summary>Used to unlink the source from this target when the last observer is unsubscribed.</summary>
                internal IDisposable? Unlinker;
                /// <summary>
                /// Temporary list to keep track of SendAsync tasks to TargetObservers with back pressure.
                /// This field gets instantiated on demand. It gets populated and cleared within an offering cycle.
                /// </summary>
                private List<Task<bool>>? _tempSendAsyncTaskList;

                /// <summary>Initializes the target instance.</summary>
                /// <param name="observable">The owning observable.</param>
                internal ObserversState(SourceObservable<TOutput> observable)
                {
                    Debug.Assert(observable != null, "Observe state must be mapped to a source observable.");

                    // Set up the target block
                    Observable = observable;
                    Target = new ActionBlock<TOutput>((Func<TOutput, Task>)ProcessItemAsync, DataflowBlock._nonGreedyExecutionOptions);

                    // If the target block fails due to an unexpected exception (e.g. it calls back to the source and the source throws an error),
                    // we fault currently registered observers and reset the observable.
                    Target.Completion.ContinueWith(
                        (t, state) => ((ObserversState)state!).NotifyObserversOfCompletion(t.Exception!), this,
                        CancellationToken.None,
                        Common.GetContinuationOptions(TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously),
                        TaskScheduler.Default);

                    // When the source completes, complete the target. Then when the target completes,
                    // send completion messages to any observers still registered.
                    Task? sourceCompletionTask = Common.GetPotentiallyNotSupportedCompletionTask(Observable._source);
                    if (sourceCompletionTask != null)
                    {
                        sourceCompletionTask.ContinueWith((_1, state1) =>
                        {
                            var ti = (ObserversState)state1!;
                            ti.Target.Complete();
                            ti.Target.Completion.ContinueWith(
                                (_2, state2) => ((ObserversState)state2!).NotifyObserversOfCompletion(), state1,
                                CancellationToken.None,
                                Common.GetContinuationOptions(TaskContinuationOptions.NotOnFaulted | TaskContinuationOptions.ExecuteSynchronously),
                                TaskScheduler.Default);
                        }, this, Canceler.Token, Common.GetContinuationOptions(TaskContinuationOptions.ExecuteSynchronously), TaskScheduler.Default);
                    }
                }

                /// <summary>Forwards an item to all currently subscribed observers.</summary>
                /// <param name="item">The item to forward.</param>
                private Task ProcessItemAsync(TOutput item)
                {
                    Common.ContractAssertMonitorStatus(Observable._SubscriptionLock, held: false);

                    ImmutableArray<IObserver<TOutput>> currentObservers;
                    lock (Observable._SubscriptionLock) currentObservers = Observers;
                    try
                    {
                        foreach (IObserver<TOutput> observer in currentObservers)
                        {
                            // If the observer is our own TargetObserver, we SendAsync() to it
                            // rather than going through IObserver.OnNext() which allows us to
                            // continue offering to the remaining observers without blocking.
                            var targetObserver = observer as TargetObserver<TOutput>;
                            if (targetObserver != null)
                            {
                                Task<bool> sendAsyncTask = targetObserver.SendAsyncToTarget(item);
                                if (sendAsyncTask.Status != TaskStatus.RanToCompletion)
                                {
                                    // Ensure the SendAsyncTaskList is instantiated
                                    if (_tempSendAsyncTaskList == null) _tempSendAsyncTaskList = new List<Task<bool>>();

                                    // Add the task to the list
                                    _tempSendAsyncTaskList.Add(sendAsyncTask);
                                }
                            }
                            else
                            {
                                observer.OnNext(item);
                            }
                        }

                        // If there are SendAsync tasks to wait on...
                        if (_tempSendAsyncTaskList != null && _tempSendAsyncTaskList.Count > 0)
                        {
                            // Consolidate all SendAsync tasks into one
                            Task<bool[]> allSendAsyncTasksConsolidated = Task.WhenAll(_tempSendAsyncTaskList);

                            // Clear the temp SendAsync task list
                            _tempSendAsyncTaskList.Clear();

                            // Return the consolidated task
                            return allSendAsyncTasksConsolidated;
                        }
                    }
                    catch (Exception exc)
                    {
                        // Return a faulted task
                        return Common.CreateTaskFromException<VoidResult>(exc);
                    }

                    // All observers accepted normally.
                    // Return a completed task.
                    return Common.CompletedTaskWithTrueResult;
                }

                /// <summary>Notifies all currently registered observers that they should complete.</summary>
                /// <param name="targetException">
                /// Non-null when an unexpected exception occurs during processing.  Faults
                /// all subscribed observers and resets the observable back to its original condition.
                /// </param>
                private void NotifyObserversOfCompletion(Exception? targetException = null)
                {
                    Debug.Assert(Target.Completion.IsCompleted, "The target must have already completed in order to notify of completion.");
                    Common.ContractAssertMonitorStatus(Observable._SubscriptionLock, held: false);

                    // Send completion notification to all observers.
                    ImmutableArray<IObserver<TOutput>> currentObservers;
                    lock (Observable._SubscriptionLock)
                    {
                        // Get the currently registered set of observers. Then, if we're being called due to the target
                        // block failing from an unexpected exception, reset the observer state so that subsequent
                        // subscribed observers will get a new target block.  Finally clear out our observer list.
                        currentObservers = Observers;
                        if (targetException != null) Observable.ResetObserverState();
                        Observers = ImmutableArray<IObserver<TOutput>>.Empty;
                    }

                    // If there are any observers to complete...
                    if (currentObservers.Count > 0)
                    {
                        // Determine if we should fault or complete the observers
                        Exception? error = targetException ?? Observable.GetCompletionError();
                        try
                        {
                            // Do it.
                            if (error != null)
                            {
                                foreach (IObserver<TOutput> observer in currentObservers) observer.OnError(error);
                            }
                            else
                            {
                                foreach (IObserver<TOutput> observer in currentObservers) observer.OnCompleted();
                            }
                        }
                        catch (Exception exc)
                        {
                            // If an observer throws an exception at this point (which it shouldn't do),
                            // we have little recourse but to let that exception propagate.  Since allowing it to
                            // propagate here would just result in it getting eaten by the owning task,
                            // we instead have it propagate on the thread pool.
                            Common.ThrowAsync(exc);
                        }
                    }
                }
            }
        }
        #endregion

        #region AsObserver
        /// <summary>Creates a new <see cref="System.IObserver{TInput}"/> abstraction over the <see cref="ITargetBlock{TInput}"/>.</summary>
        /// <typeparam name="TInput">Specifies the type of input accepted by the target block.</typeparam>
        /// <param name="target">The target to wrap.</param>
        /// <returns>An observer that wraps the target block.</returns>
        public static IObserver<TInput> AsObserver<TInput>(this ITargetBlock<TInput> target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            return new TargetObserver<TInput>(target);
        }

        /// <summary>Provides an observer wrapper for a target block.</summary>
        [DebuggerDisplay("{DebuggerDisplayContent,nq}")]
        private sealed class TargetObserver<TInput> : IObserver<TInput>, IDebuggerDisplay
        {
            /// <summary>The wrapped target.</summary>
            private readonly ITargetBlock<TInput> _target;

            /// <summary>Initializes the observer.</summary>
            /// <param name="target">The target to wrap.</param>
            internal TargetObserver(ITargetBlock<TInput> target)
            {
                Debug.Assert(target != null, "A target to observe is required.");
                _target = target;
            }

            /// <summary>Sends the value to the observer.</summary>
            /// <param name="value">The value to send.</param>
            void IObserver<TInput>.OnNext(TInput value)
            {
                // Send the value asynchronously...
                Task<bool> task = SendAsyncToTarget(value);

                // And block until it's received.
                task.GetAwaiter().GetResult(); // propagate original (non-aggregated) exception
            }

            /// <summary>Completes the target.</summary>
            void IObserver<TInput>.OnCompleted()
            {
                _target.Complete();
            }

            /// <summary>Forwards the error to the target.</summary>
            /// <param name="error">The exception to forward.</param>
            void IObserver<TInput>.OnError(Exception error)
            {
                _target.Fault(error);
            }

            /// <summary>Sends a value to the underlying target asynchronously.</summary>
            /// <param name="value">The value to send.</param>
            /// <returns>A Task{bool} to wait on.</returns>
            internal Task<bool> SendAsyncToTarget(TInput value)
            {
                return _target.SendAsync(value);
            }

            /// <summary>The data to display in the debugger display attribute.</summary>
            private object DebuggerDisplayContent
            {
                get
                {
                    var displayTarget = _target as IDebuggerDisplay;
                    return string.Format("Block=\"{0}\"",
                        displayTarget != null ? displayTarget.Content : _target);
                }
            }
            /// <summary>Gets the data to display in the debugger display attribute for this instance.</summary>
            object IDebuggerDisplay.Content { get { return DebuggerDisplayContent; } }
        }
        #endregion

        #region NullTarget
        /// <summary>
        /// Gets a target block that synchronously accepts all messages offered to it and drops them.
        /// </summary>
        /// <typeparam name="TInput">The type of the messages this block can accept.</typeparam>
        /// <returns>A <see cref="System.Threading.Tasks.Dataflow.ITargetBlock{T}"/> that accepts and subsequently drops all offered messages.</returns>
        public static ITargetBlock<TInput> NullTarget<TInput>()
        {
            return new NullTargetBlock<TInput>();
        }

        /// <summary>
        /// Target block that synchronously accepts all messages offered to it and drops them.
        /// </summary>
        /// <typeparam name="TInput">The type of the messages this block can accept.</typeparam>
        private class NullTargetBlock<TInput> : ITargetBlock<TInput>
        {
            private Task? _completion;

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Targets/Member[@name="OfferMessage"]/*' />
            DataflowMessageStatus ITargetBlock<TInput>.OfferMessage(DataflowMessageHeader messageHeader, TInput messageValue, ISourceBlock<TInput>? source, bool consumeToAccept)
            {
                if (!messageHeader.IsValid) throw new ArgumentException(SR.Argument_InvalidMessageHeader, nameof(messageHeader));

                // If the source requires an explicit synchronous consumption, do it
                if (consumeToAccept)
                {
                    if (source == null) throw new ArgumentException(SR.Argument_CantConsumeFromANullSource, nameof(consumeToAccept));
                    bool messageConsumed;

                    // If the source throws during this call, let the exception propagate back to the source
                    source.ConsumeMessage(messageHeader, this, out messageConsumed);
                    if (!messageConsumed) return DataflowMessageStatus.NotAvailable;
                }

                // Always tell the source the message has been accepted
                return DataflowMessageStatus.Accepted;
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Complete"]/*' />
            void IDataflowBlock.Complete() { } // No-op
            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Fault"]/*' />
            void IDataflowBlock.Fault(Exception exception) { } // No-op
            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Completion"]/*' />
            Task IDataflowBlock.Completion
            {
                get { return LazyInitializer.EnsureInitialized(ref _completion, () => new TaskCompletionSource<VoidResult>().Task); }
            }
        }
        #endregion
    }
}
