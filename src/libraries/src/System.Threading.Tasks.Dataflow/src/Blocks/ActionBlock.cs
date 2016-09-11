// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ActionBlock.cs
//
//
// A target block that executes an action for each message.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Dataflow.Internal;

namespace System.Threading.Tasks.Dataflow
{
    /// <summary>Provides a dataflow block that invokes a provided <see cref="System.Action{T}"/> delegate for every data element received.</summary>
    /// <typeparam name="TInput">Specifies the type of data operated on by this <see cref="ActionBlock{T}"/>.</typeparam>
    [DebuggerDisplay("{DebuggerDisplayContent,nq}")]
    [DebuggerTypeProxy(typeof(ActionBlock<>.DebugView))]
    public sealed class ActionBlock<TInput> : ITargetBlock<TInput>, IDebuggerDisplay
    {
        /// <summary>The core implementation of this message block when in default mode.</summary>
        private readonly TargetCore<TInput> _defaultTarget;
        /// <summary>The core implementation of this message block when in SPSC mode.</summary>
        private readonly SpscTargetCore<TInput> _spscTarget;

        /// <summary>Initializes the <see cref="ActionBlock{T}"/> with the specified <see cref="System.Action{T}"/>.</summary>
        /// <param name="action">The action to invoke with each data element received.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="action"/> is null (Nothing in Visual Basic).</exception>
        public ActionBlock(Action<TInput> action) :
            this((Delegate)action, ExecutionDataflowBlockOptions.Default)
        { }

        /// <summary>Initializes the <see cref="ActionBlock{T}"/> with the specified <see cref="System.Action{T}"/> and <see cref="ExecutionDataflowBlockOptions"/>.</summary>
        /// <param name="action">The action to invoke with each data element received.</param>
        /// <param name="dataflowBlockOptions">The options with which to configure this <see cref="ActionBlock{T}"/>.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="action"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="dataflowBlockOptions"/> is null (Nothing in Visual Basic).</exception>
        public ActionBlock(Action<TInput> action, ExecutionDataflowBlockOptions dataflowBlockOptions) :
            this((Delegate)action, dataflowBlockOptions)
        { }

        /// <summary>Initializes the <see cref="ActionBlock{T}"/> with the specified <see cref="System.Func{T,Task}"/>.</summary>
        /// <param name="action">The action to invoke with each data element received.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="action"/> is null (Nothing in Visual Basic).</exception>
        public ActionBlock(Func<TInput, Task> action) :
            this((Delegate)action, ExecutionDataflowBlockOptions.Default)
        { }

        /// <summary>Initializes the <see cref="ActionBlock{T}"/> with the specified <see cref="System.Func{T,Task}"/> and <see cref="ExecutionDataflowBlockOptions"/>.</summary>
        /// <param name="action">The action to invoke with each data element received.</param>
        /// <param name="dataflowBlockOptions">The options with which to configure this <see cref="ActionBlock{T}"/>.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="action"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="dataflowBlockOptions"/> is null (Nothing in Visual Basic).</exception>
        public ActionBlock(Func<TInput, Task> action, ExecutionDataflowBlockOptions dataflowBlockOptions) :
            this((Delegate)action, dataflowBlockOptions)
        { }

        /// <summary>Initializes the <see cref="ActionBlock{T}"/> with the specified delegate and options.</summary>
        /// <param name="action">The action to invoke with each data element received.</param>
        /// <param name="dataflowBlockOptions">The options with which to configure this <see cref="ActionBlock{T}"/>.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="action"/> is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="dataflowBlockOptions"/> is null (Nothing in Visual Basic).</exception>
        private ActionBlock(Delegate action, ExecutionDataflowBlockOptions dataflowBlockOptions)
        {
            // Validate arguments
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (dataflowBlockOptions == null) throw new ArgumentNullException(nameof(dataflowBlockOptions));
            Contract.Ensures((_spscTarget != null) ^ (_defaultTarget != null), "One and only one of the two targets must be non-null after construction");
            Contract.EndContractBlock();

            // Ensure we have options that can't be changed by the caller
            dataflowBlockOptions = dataflowBlockOptions.DefaultOrClone();

            // Based on the mode, initialize the target.  If the user specifies SingleProducerConstrained,
            // we'll try to employ an optimized mode under a limited set of circumstances.
            var syncAction = action as Action<TInput>;
            if (syncAction != null &&
                dataflowBlockOptions.SingleProducerConstrained &&
                dataflowBlockOptions.MaxDegreeOfParallelism == 1 &&
                !dataflowBlockOptions.CancellationToken.CanBeCanceled &&
                dataflowBlockOptions.BoundedCapacity == DataflowBlockOptions.Unbounded)
            {
                // Initialize the SPSC fast target to handle the bulk of the processing.
                // The SpscTargetCore is only supported when BoundedCapacity, CancellationToken,
                // and MaxDOP are all their default values.  It's also only supported for sync
                // delegates and not for async delegates.
                _spscTarget = new SpscTargetCore<TInput>(this, syncAction, dataflowBlockOptions);
            }
            else
            {
                // Initialize the TargetCore which handles the bulk of the processing.
                // The default target core can handle all options and delegate flavors.

                if (syncAction != null) // sync
                {
                    _defaultTarget = new TargetCore<TInput>(this,
                        messageWithId => ProcessMessage(syncAction, messageWithId),
                        null, dataflowBlockOptions, TargetCoreOptions.RepresentsBlockCompletion);
                }
                else // async
                {
                    var asyncAction = action as Func<TInput, Task>;
                    Debug.Assert(asyncAction != null, "action is of incorrect delegate type");
                    _defaultTarget = new TargetCore<TInput>(this,
                        messageWithId => ProcessMessageWithTask(asyncAction, messageWithId),
                        null, dataflowBlockOptions, TargetCoreOptions.RepresentsBlockCompletion | TargetCoreOptions.UsesAsyncCompletion);
                }

                // Handle async cancellation requests by declining on the target
                Common.WireCancellationToComplete(
                    dataflowBlockOptions.CancellationToken, Completion, state => ((TargetCore<TInput>)state).Complete(exception: null, dropPendingMessages: true), _defaultTarget);
            }
#if FEATURE_TRACING
            DataflowEtwProvider etwLog = DataflowEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.DataflowBlockCreated(this, dataflowBlockOptions);
            }
#endif
        }

        /// <summary>Processes the message with a user-provided action.</summary>
        /// <param name="action">The action to use to process the message.</param>
        /// <param name="messageWithId">The message to be processed.</param>
        private void ProcessMessage(Action<TInput> action, KeyValuePair<TInput, long> messageWithId)
        {
            try
            {
                action(messageWithId.Key);
            }
            catch (Exception exc)
            {
                // If this exception represents cancellation, swallow it rather than shutting down the block.
                if (!Common.IsCooperativeCancellation(exc)) throw;
            }
            finally
            {
                // We're done synchronously processing an element, so reduce the bounding count
                // that was incrementing when this element was enqueued.
                if (_defaultTarget.IsBounded) _defaultTarget.ChangeBoundingCount(-1);
            }
        }

        /// <summary>Processes the message with a user-provided action that returns a task.</summary>
        /// <param name="action">The action to use to process the message.</param>
        /// <param name="messageWithId">The message to be processed.</param>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void ProcessMessageWithTask(Func<TInput, Task> action, KeyValuePair<TInput, long> messageWithId)
        {
            Debug.Assert(action != null, "action needed for processing");

            // Run the action to get the task that represents the operation's completion
            Task task = null;
            Exception caughtException = null;
            try
            {
                task = action(messageWithId.Key);
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
                    _defaultTarget.Complete(caughtException, dropPendingMessages: true, storeExceptionEvenIfAlreadyCompleting: true, unwrapInnerExceptions: false);
                }

                // Signal that we're done this async operation.
                _defaultTarget.SignalOneAsyncMessageCompleted(boundingCountChange: -1);
                return;
            }
            else if (task.IsCompleted)
            {
                AsyncCompleteProcessMessageWithTask(task);
            }
            else
            {
                // Otherwise, join with the asynchronous operation when it completes.
                task.ContinueWith((completed, state) =>
                {
                    ((ActionBlock<TInput>)state).AsyncCompleteProcessMessageWithTask(completed);
                }, this, CancellationToken.None, Common.GetContinuationOptions(TaskContinuationOptions.ExecuteSynchronously), TaskScheduler.Default);
            }
        }

        /// <summary>Completes the processing of an asynchronous message.</summary>
        /// <param name="completed">The completed task.</param>
        private void AsyncCompleteProcessMessageWithTask(Task completed)
        {
            Debug.Assert(completed != null, "Need completed task for processing");
            Debug.Assert(completed.IsCompleted, "The task to be processed must be completed by now.");

            // If the task faulted, store its errors. We must add the exception before declining
            // and signaling completion, as the exception is part of the operation, and the completion conditions
            // depend on this.
            if (completed.IsFaulted)
            {
                _defaultTarget.Complete(completed.Exception, dropPendingMessages: true, storeExceptionEvenIfAlreadyCompleting: true, unwrapInnerExceptions: true);
            }

            // Regardless of faults, note that we're done processing.  There are
            // no outputs to keep track of for action block, so we always decrement 
            // the bounding count here (the callee will handle checking whether
            // we're actually in a bounded mode).
            _defaultTarget.SignalOneAsyncMessageCompleted(boundingCountChange: -1);
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Complete"]/*' />
        public void Complete()
        {
            if (_defaultTarget != null)
            {
                _defaultTarget.Complete(exception: null, dropPendingMessages: false);
            }
            else
            {
                _spscTarget.Complete(exception: null);
            }
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Fault"]/*' />
        void IDataflowBlock.Fault(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));
            Contract.EndContractBlock();

            if (_defaultTarget != null)
            {
                _defaultTarget.Complete(exception, dropPendingMessages: true);
            }
            else
            {
                _spscTarget.Complete(exception);
            }
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Completion"]/*' />
        public Task Completion
        {
            get { return _defaultTarget != null ? _defaultTarget.Completion : _spscTarget.Completion; }
        }

        /// <summary>Posts an item to the <see cref="T:System.Threading.Tasks.Dataflow.ITargetBlock`1"/>.</summary>
        /// <param name="item">The item being offered to the target.</param>
        /// <returns>true if the item was accepted by the target block; otherwise, false.</returns>
        /// <remarks>
        /// This method will return once the target block has decided to accept or decline the item,
        /// but unless otherwise dictated by special semantics of the target block, it does not wait
        /// for the item to actually be processed (for example, <see cref="T:System.Threading.Tasks.Dataflow.ActionBlock`1"/>
        /// will return from Post as soon as it has stored the posted item into its input queue).  From the perspective
        /// of the block's processing, Post is asynchronous. For target blocks that support postponing offered messages, 
        /// or for blocks that may do more processing in their Post implementation, consider using
        ///  <see cref="T:System.Threading.Tasks.Dataflow.DataflowBlock.SendAsync">SendAsync</see>, 
        /// which will return immediately and will enable the target to postpone the posted message and later consume it 
        /// after SendAsync returns.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Post(TInput item)
        {
            // Even though this method is available with the exact same functionality as an extension method
            // on ITargetBlock, using that extension method goes through an interface call on ITargetBlock,
            // which for very high-throughput scenarios shows up as noticeable overhead on certain architectures.  
            // We can eliminate that call for direct ActionBlock usage by providing the same method as an instance method.

            return _defaultTarget != null ?
                _defaultTarget.OfferMessage(Common.SingleMessageHeader, item, null, false) == DataflowMessageStatus.Accepted :
                _spscTarget.Post(item);
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Targets/Member[@name="OfferMessage"]/*' />
        DataflowMessageStatus ITargetBlock<TInput>.OfferMessage(DataflowMessageHeader messageHeader, TInput messageValue, ISourceBlock<TInput> source, Boolean consumeToAccept)
        {
            return _defaultTarget != null ?
                _defaultTarget.OfferMessage(messageHeader, messageValue, source, consumeToAccept) :
                _spscTarget.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Targets/Member[@name="InputCount"]/*' />
        public int InputCount
        {
            get { return _defaultTarget != null ? _defaultTarget.InputCount : _spscTarget.InputCount; }
        }

        /// <summary>Gets the number of messages waiting to be processed. This must only be used from the debugger.</summary>
        private int InputCountForDebugger
        {
            get { return _defaultTarget != null ? _defaultTarget.GetDebuggingInformation().InputCount : _spscTarget.InputCount; }
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="ToString"]/*' />
        public override string ToString()
        {
            return Common.GetNameForDebugger(this, _defaultTarget != null ? _defaultTarget.DataflowBlockOptions : _spscTarget.DataflowBlockOptions);
        }

        /// <summary>The data to display in the debugger display attribute.</summary>
        [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider")]
        private object DebuggerDisplayContent
        {
            get
            {
                return string.Format("{0}, InputCount={1}",
                    Common.GetNameForDebugger(this, _defaultTarget != null ? _defaultTarget.DataflowBlockOptions : _spscTarget.DataflowBlockOptions),
                    InputCountForDebugger);
            }
        }
        /// <summary>Gets the data to display in the debugger display attribute for this instance.</summary>
        object IDebuggerDisplay.Content { get { return DebuggerDisplayContent; } }

        /// <summary>Provides a debugger type proxy for the Call.</summary>
        private sealed class DebugView
        {
            /// <summary>The action block being viewed.</summary>
            private readonly ActionBlock<TInput> _actionBlock;
            /// <summary>The action block's default target being viewed.</summary>
            private readonly TargetCore<TInput>.DebuggingInformation _defaultDebugInfo;
            /// <summary>The action block's SPSC target being viewed.</summary>
            private readonly SpscTargetCore<TInput>.DebuggingInformation _spscDebugInfo;

            /// <summary>Initializes the debug view.</summary>
            /// <param name="actionBlock">The target being debugged.</param>
            public DebugView(ActionBlock<TInput> actionBlock)
            {
                Debug.Assert(actionBlock != null, "Need a block with which to construct the debug view.");
                _actionBlock = actionBlock;
                if (_actionBlock._defaultTarget != null)
                {
                    _defaultDebugInfo = actionBlock._defaultTarget.GetDebuggingInformation();
                }
                else
                {
                    _spscDebugInfo = actionBlock._spscTarget.GetDebuggingInformation();
                }
            }

            /// <summary>Gets the messages waiting to be processed.</summary>
            public IEnumerable<TInput> InputQueue
            {
                get { return _defaultDebugInfo != null ? _defaultDebugInfo.InputQueue : _spscDebugInfo.InputQueue; }
            }
            /// <summary>Gets any postponed messages.</summary>
            public QueuedMap<ISourceBlock<TInput>, DataflowMessageHeader> PostponedMessages
            {
                get { return _defaultDebugInfo != null ? _defaultDebugInfo.PostponedMessages : null; }
            }

            /// <summary>Gets the number of outstanding input operations.</summary>
            public Int32 CurrentDegreeOfParallelism
            {
                get { return _defaultDebugInfo != null ? _defaultDebugInfo.CurrentDegreeOfParallelism : _spscDebugInfo.CurrentDegreeOfParallelism; }
            }

            /// <summary>Gets the ExecutionDataflowBlockOptions used to configure this block.</summary>
            public ExecutionDataflowBlockOptions DataflowBlockOptions
            {
                get { return _defaultDebugInfo != null ? _defaultDebugInfo.DataflowBlockOptions : _spscDebugInfo.DataflowBlockOptions; }
            }
            /// <summary>Gets whether the block is declining further messages.</summary>
            public bool IsDecliningPermanently
            {
                get { return _defaultDebugInfo != null ? _defaultDebugInfo.IsDecliningPermanently : _spscDebugInfo.IsDecliningPermanently; }
            }
            /// <summary>Gets whether the block is completed.</summary>
            public bool IsCompleted
            {
                get { return _defaultDebugInfo != null ? _defaultDebugInfo.IsCompleted : _spscDebugInfo.IsCompleted; }
            }
            /// <summary>Gets the block's Id.</summary>
            public int Id { get { return Common.GetBlockId(_actionBlock); } }
        }
    }
}
