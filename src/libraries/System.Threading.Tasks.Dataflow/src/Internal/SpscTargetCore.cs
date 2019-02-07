// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// SpscTargetCore.cs
//
//
// A fast single-producer-single-consumer core for a target block.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Threading.Tasks.Dataflow.Internal
{
    // SpscTargetCore provides a fast target core for use in blocks that will only have single-producer-single-consumer
    // semantics.  Blocks configured with the default DOP==1 will be single consumer, so whether this core may be
    // used is largely up to whether the block is also single-producer.  The ExecutionDataflowBlockOptions.SingleProducerConstrained
    // option can be used by a developer to inform a block that it will only be accessed by one producer at a time,
    // and a block like ActionBlock can utilize that knowledge to choose this target instead of the default TargetCore.
    // However, there are further constraints that might prevent this core from being used.
    //     - If the user specifies a CancellationToken, this core can't be used, as the cancellation request
    //       could come in concurrently with the single producer accessing the block, thus resulting in multiple producers.
    //     - If the user specifies a bounding capacity, this core can't be used, as the consumer processing items
    //       needs to synchronize with producers around the change in bounding count, and the consumer is again
    //       in effect another producer.
    //     - If the block has a source half (e.g. TransformBlock) and that source could potentially call back
    //       to the target half to, for example, notify it of exceptions occurring, again there would potentially
    //       be multiple producers.
    // Thus, when and how this SpscTargetCore may be applied is significantly constrained.

    /// <summary>
    /// Provides a core implementation of <see cref="ITargetBlock{TInput}"/> for use when there's only a single producer posting data.
    /// </summary>
    /// <typeparam name="TInput">Specifies the type of data accepted by the <see cref="TargetCore{TInput}"/>.</typeparam>
    [DebuggerDisplay("{DebuggerDisplayContent,nq}")]
    internal sealed class SpscTargetCore<TInput>
    {
        /// <summary>The target block using this helper.</summary>
        private readonly ITargetBlock<TInput> _owningTarget;
        /// <summary>The messages in this target.</summary>
        private readonly SingleProducerSingleConsumerQueue<TInput> _messages = new SingleProducerSingleConsumerQueue<TInput>();
        /// <summary>The options to use to configure this block. The target core assumes these options are immutable.</summary>
        private readonly ExecutionDataflowBlockOptions _dataflowBlockOptions;
        /// <summary>An action to invoke for every accepted message.</summary>
        private readonly Action<TInput> _action;

        /// <summary>Exceptions that may have occurred and gone unhandled during processing.  This field is lazily initialized.</summary>
        private volatile List<Exception> _exceptions;
        /// <summary>Whether to stop accepting new messages.</summary>
        private volatile bool _decliningPermanently;
        /// <summary>A task has reserved the right to run the completion routine.</summary>
        private volatile bool _completionReserved;
        /// <summary>
        /// The Task currently active to process the block. This field is used to synchronize between producer and consumer, 
        /// and it should not be set to null once the block completes, as doing so would allow for races where the producer
        /// gets another consumer task queued even though the block has completed.
        /// </summary>
        private volatile Task _activeConsumer;
        /// <summary>A task representing the completion of the block.  This field is lazily initialized.</summary>
        private TaskCompletionSource<VoidResult> _completionTask;

        /// <summary>Initialize the SPSC target core.</summary>
        /// <param name="owningTarget">The owning target block.</param>
        /// <param name="action">The action to be invoked for every message.</param>
        /// <param name="dataflowBlockOptions">The options to use to configure this block. The target core assumes these options are immutable.</param>
        internal SpscTargetCore(
            ITargetBlock<TInput> owningTarget, Action<TInput> action, ExecutionDataflowBlockOptions dataflowBlockOptions)
        {
            Debug.Assert(owningTarget != null, "Expected non-null owningTarget");
            Debug.Assert(action != null, "Expected non-null action");
            Debug.Assert(dataflowBlockOptions != null, "Expected non-null dataflowBlockOptions");

            _owningTarget = owningTarget;
            _action = action;
            _dataflowBlockOptions = dataflowBlockOptions;
        }

        internal bool Post(TInput messageValue)
        {
            if (_decliningPermanently)
                return false;

            // Store the offered message into the queue.
            _messages.Enqueue(messageValue);

            Interlocked.MemoryBarrier(); // ensure the read of _activeConsumer doesn't move up before the writes in Enqueue

            // Make sure there's an active task available to handle processing this message.  If we find the task
            // is null, we'll try to schedule one using an interlocked operation.  If we find the task is non-null,
            // then there must be a task actively running.  If there's a race where the task is about to complete
            // and nulls out its reference (using a barrier), it'll subsequently check whether there are any messages in the queue,
            // and since we put the messages into the queue before now, it'll find them and use an interlocked
            // to re-launch itself.
            if (_activeConsumer == null)
            {
                ScheduleConsumerIfNecessary(false);
            }

            return true;
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Targets/Member[@name="OfferMessage"]/*' />
        internal DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, TInput messageValue, ISourceBlock<TInput> source, bool consumeToAccept)
        {
            // If we're not required to go back to the source to consume the offered message, try fast path.
            return !consumeToAccept && Post(messageValue) ? 
                DataflowMessageStatus.Accepted :
                OfferMessage_Slow(messageHeader, messageValue, source, consumeToAccept);
        }

        /// <summary>Implements the slow path for OfferMessage.</summary>
        /// <param name="messageHeader">The message header for the offered value.</param>
        /// <param name="messageValue">The offered value.</param>
        /// <param name="source">The source offering the message. This may be null.</param>
        /// <param name="consumeToAccept">true if we need to call back to the source to consume the message; otherwise, false if we can simply accept it directly.</param>
        /// <returns>The status of the message.</returns>
        private DataflowMessageStatus OfferMessage_Slow(DataflowMessageHeader messageHeader, TInput messageValue, ISourceBlock<TInput> source, bool consumeToAccept)
        {
            // If we're declining permanently, let the caller know.
            if (_decliningPermanently)
            {
                return DataflowMessageStatus.DecliningPermanently;
            }

            // If the message header is invalid, throw.
            if (!messageHeader.IsValid)
            {
                throw new ArgumentException(SR.Argument_InvalidMessageHeader, nameof(messageHeader));
            }

            // If the caller has requested we consume the message using ConsumeMessage, do so.
            if (consumeToAccept)
            {
                if (source == null) throw new ArgumentException(SR.Argument_CantConsumeFromANullSource, nameof(consumeToAccept));
                bool consumed;
                messageValue = source.ConsumeMessage(messageHeader, _owningTarget, out consumed);
                if (!consumed) return DataflowMessageStatus.NotAvailable;
            }

            // See the "fast path" comments in Post
            _messages.Enqueue(messageValue);
            Interlocked.MemoryBarrier(); // ensure the read of _activeConsumer doesn't move up before the writes in Enqueue
            if (_activeConsumer == null)
            {
                ScheduleConsumerIfNecessary(isReplica: false);
            }
            return DataflowMessageStatus.Accepted;
        }

        /// <summary>Schedules a consumer task if there's none currently running.</summary>
        /// <param name="isReplica">Whether the new consumer is being scheduled to replace a currently running consumer.</param>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        private void ScheduleConsumerIfNecessary(bool isReplica)
        {
            // If there's currently no active task...
            if (_activeConsumer == null)
            {
                // Create a new consumption task and try to set it as current as long as there's still no other task
                var newConsumer = new Task(
                    state => ((SpscTargetCore<TInput>)state).ProcessMessagesLoopCore(),
                    this, CancellationToken.None, Common.GetCreationOptionsForTask(isReplica));
                if (Interlocked.CompareExchange(ref _activeConsumer, newConsumer, null) == null)
                {
                    // We won the race.  This task is now the consumer.

#if FEATURE_TRACING
                    DataflowEtwProvider etwLog = DataflowEtwProvider.Log;
                    if (etwLog.IsEnabled())
                    {
                        etwLog.TaskLaunchedForMessageHandling(
                            _owningTarget, newConsumer, DataflowEtwProvider.TaskLaunchedReason.ProcessingInputMessages, _messages.Count);
                    }
#endif

                    // Start the task.  In the erroneous case where the scheduler throws an exception, 
                    // just allow it to propagate. Our other option would be to fault the block with 
                    // that exception, but in order for the block to complete we need to schedule a consumer
                    // task to do so, and it's very likely that if the scheduler is throwing an exception 
                    // now, it would do so again.
                    newConsumer.Start(_dataflowBlockOptions.TaskScheduler);
                }
            }
        }

        /// <summary>Task body used to process messages.</summary>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void ProcessMessagesLoopCore()
        {
            Debug.Assert(
                _activeConsumer != null && _activeConsumer.Id == Task.CurrentId,
                "This method should only be called when it's the active consumer.");

            int messagesProcessed = 0;
            int maxMessagesToProcess = _dataflowBlockOptions.ActualMaxMessagesPerTask;

            // Continue processing as long as there's more processing to be done
            bool continueProcessing = true;
            while (continueProcessing)
            {
                continueProcessing = false;
                TInput nextMessage = default(TInput);
                try
                {
                    // While there are more messages to be processed, process each one.
                    // NOTE: This loop is critical for performance.  It must be super lean.
                    while (
                        _exceptions == null &&
                        messagesProcessed < maxMessagesToProcess &&
                        _messages.TryDequeue(out nextMessage))
                    {
                        messagesProcessed++; // done before _action invoked in case it throws exception
                        _action(nextMessage);
                    }
                }
                catch (Exception exc)
                {
                    // If the exception is for cancellation, just ignore it.
                    // Otherwise, store it, and the finally block will handle completion.
                    if (!Common.IsCooperativeCancellation(exc))
                    {
                        _decliningPermanently = true; // stop accepting from producers
                        Common.StoreDataflowMessageValueIntoExceptionData<TInput>(exc, nextMessage, false);
                        StoreException(exc);
                    }
                }
                finally
                {
                    // If more messages just arrived and we should still process them,
                    // loop back around and keep going.
                    if (!_messages.IsEmpty && _exceptions == null && (messagesProcessed < maxMessagesToProcess))
                    {
                        continueProcessing = true;
                    }
                    else
                    {
                        // If messages are being declined and we're empty, or if there's an exception,
                        // then there's no more work to be done and we should complete the block.
                        bool wasDecliningPermanently = _decliningPermanently;
                        if ((wasDecliningPermanently && _messages.IsEmpty) || _exceptions != null)
                        {
                            // Complete the block, as long as we're not already completing or completed.
                            if (!_completionReserved) // no synchronization necessary; this can't happen concurrently
                            {
                                _completionReserved = true;
                                CompleteBlockOncePossible();
                            }
                        }
                        else
                        {
                            // Mark that we're exiting.
                            Task previousConsumer = Interlocked.Exchange(ref _activeConsumer, null);
                            Debug.Assert(previousConsumer != null && previousConsumer.Id == Task.CurrentId,
                                "The running task should have been denoted as the active task.");

                            // Now that we're no longer the active task, double
                            // check to make sure there's really nothing to do,
                            // which could include processing more messages or completing.
                            // If there is more to do, schedule a task to try to do it.
                            // This is to handle a race with Post/Complete/Fault and this
                            // task completing.
                            if (!_messages.IsEmpty || // messages to be processed
                                (!wasDecliningPermanently && _decliningPermanently) || // potentially completion to be processed
                                _exceptions != null) // exceptions/completion to be processed
                            {
                                ScheduleConsumerIfNecessary(isReplica: true);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>Gets the number of messages waiting to be processed.</summary>
        internal int InputCount { get { return _messages.Count; } }

        /// <summary>
        /// Completes the target core.  If an exception is provided, the block will end up in a faulted state.
        /// If Complete is invoked more than once, or if it's invoked after the block is already
        /// completing, all invocations after the first are ignored.
        /// </summary>
        /// <param name="exception">The exception to be stored.</param>
        internal void Complete(Exception exception)
        {
            // If we're not yet declining permanently...
            if (!_decliningPermanently)
            {
                // Mark us as declining permanently, and then kick off a processing task
                // if we need one.  It's this processing task's job to complete the block
                // once all data has been consumed and/or we're in a valid state for completion.
                if (exception != null) StoreException(exception);
                _decliningPermanently = true;
                ScheduleConsumerIfNecessary(isReplica: false);
            }
        }

        /// <summary>
        /// Ensures the exceptions list is initialized and stores the exception into the list using a lock.
        /// </summary>
        /// <param name="exception">The exception to store.</param>
        private void StoreException(Exception exception)
        {
            // Ensure that the _exceptions field has been initialized.
            // We need to synchronize the initialization and storing of
            // the exception because this method could be accessed concurrently
            // by the producer and consumer, a producer calling Fault and the 
            // processing task processing the user delegate which might throw.
#pragma warning disable 0420
            lock (LazyInitializer.EnsureInitialized(ref _exceptions, () => new List<Exception>()))
#pragma warning restore 0420
            {
                _exceptions.Add(exception);
            }
        }

        /// <summary>
        /// Completes the block.  This must only be called once, and only once all of the completion conditions are met.
        /// </summary>
        private void CompleteBlockOncePossible()
        {
            Debug.Assert(_completionReserved, "Should only invoke once completion has been reserved.");

            // Dump any messages that might remain in the queue, which could happen if we completed due to exceptions.
            TInput dumpedMessage;
            while (_messages.TryDequeue(out dumpedMessage)) ;

            // Complete the completion task
            bool result;
            if (_exceptions != null)
            {
                Exception[] exceptions;
                lock (_exceptions) exceptions = _exceptions.ToArray();
                result = CompletionSource.TrySetException(exceptions);
            }
            else
            {
                result = CompletionSource.TrySetResult(default(VoidResult));
            }
            Debug.Assert(result, "Expected completion task to not yet be completed");
            // We explicitly do not set the _activeTask to null here, as that would
            // allow for races where a producer calling OfferMessage could end up
            // seeing _activeTask as null and queueing a new consumer task even
            // though the block has completed.

#if FEATURE_TRACING
            DataflowEtwProvider etwLog = DataflowEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.DataflowBlockCompleted(_owningTarget);
            }
#endif
        }

        /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Completion"]/*' />
        internal Task Completion { get { return CompletionSource.Task; } }

        /// <summary>Gets the lazily-initialized completion source.</summary>
        private TaskCompletionSource<VoidResult> CompletionSource
        {
            get { return LazyInitializer.EnsureInitialized(ref _completionTask, () => new TaskCompletionSource<VoidResult>()); }
        }

        /// <summary>Gets the DataflowBlockOptions used to configure this block.</summary>
        internal ExecutionDataflowBlockOptions DataflowBlockOptions { get { return _dataflowBlockOptions; } }

        /// <summary>Gets information about this helper to be used for display in a debugger.</summary>
        /// <returns>Debugging information about this target.</returns>
        internal DebuggingInformation GetDebuggingInformation() { return new DebuggingInformation(this); }

        /// <summary>Gets the object to display in the debugger display attribute.</summary>
        [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider")]
        private object DebuggerDisplayContent
        {
            get
            {
                var displayTarget = _owningTarget as IDebuggerDisplay;
                return string.Format("Block=\"{0}\"",
                    displayTarget != null ? displayTarget.Content : _owningTarget);
            }
        }

        /// <summary>Provides a wrapper for commonly needed debugging information.</summary>
        internal sealed class DebuggingInformation
        {
            /// <summary>The target being viewed.</summary>
            private readonly SpscTargetCore<TInput> _target;

            /// <summary>Initializes the debugging helper.</summary>
            /// <param name="target">The target being viewed.</param>
            internal DebuggingInformation(SpscTargetCore<TInput> target) { _target = target; }
            
            /// <summary>Gets the messages waiting to be processed.</summary>
            internal IEnumerable<TInput> InputQueue { get { return _target._messages.ToList(); } }

            /// <summary>Gets the current number of outstanding input processing operations.</summary>
            internal int CurrentDegreeOfParallelism { get { return _target._activeConsumer != null && !_target.Completion.IsCompleted ? 1 : 0; } }
            /// <summary>Gets the DataflowBlockOptions used to configure this block.</summary>
            internal ExecutionDataflowBlockOptions DataflowBlockOptions { get { return _target._dataflowBlockOptions; } }
            /// <summary>Gets whether the block is declining further messages.</summary>
            internal bool IsDecliningPermanently { get { return _target._decliningPermanently; } }
            /// <summary>Gets whether the block is completed.</summary>
            internal bool IsCompleted { get { return _target.Completion.IsCompleted; } }
        }
    }
}
