// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// Common.cs
//
//
// Helper routines for the rest of the TPL Dataflow implementation.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Collections;
using System.Runtime.ExceptionServices;

#if USE_INTERNAL_THREADING
using System.Threading.Tasks.Dataflow.Internal.Threading;
#endif

namespace System.Threading.Tasks.Dataflow.Internal
{
    /// <summary>Internal helper utilities.</summary>
    internal static class Common
    {
        /// <summary>
        /// An invalid ID to assign for reordering purposes.  This value is chosen to be the last of the 64-bit integers that
        /// could ever be assigned as a reordering ID.
        /// </summary>
        internal const long INVALID_REORDERING_ID = -1;
        /// <summary>A well-known message ID for code that will send exactly one message or
        /// where the exact message ID is not important.</summary>
        internal const int SINGLE_MESSAGE_ID = 1;
        /// <summary>A perf optimization for caching a well-known message header instead of
        /// constructing one every time it is needed.</summary>
        internal static readonly DataflowMessageHeader SingleMessageHeader = new DataflowMessageHeader(SINGLE_MESSAGE_ID);
        /// <summary>The cached completed Task{bool} with a result of true.</summary>
        internal static readonly Task<bool> CompletedTaskWithTrueResult = CreateCachedBooleanTask(true);
        /// <summary>The cached completed Task{bool} with a result of false.</summary>
        internal static readonly Task<bool> CompletedTaskWithFalseResult = CreateCachedBooleanTask(false);
        /// <summary>The cached completed TaskCompletionSource{VoidResult}.</summary>
        internal static readonly TaskCompletionSource<VoidResult> CompletedVoidResultTaskCompletionSource = CreateCachedTaskCompletionSource<VoidResult>();

        /// <summary>Asserts that a given synchronization object is either held or not held.</summary>
        /// <param name="syncObj">The monitor to check.</param>
        /// <param name="held">Whether we want to assert that it's currently held or not held.</param>
        [Conditional("DEBUG")]
        internal static void ContractAssertMonitorStatus(object syncObj, bool held)
        {
            Debug.Assert(syncObj != null, "The monitor object to check must be provided.");
            Debug.Assert(Monitor.IsEntered(syncObj) == held, "The locking scheme was not correctly followed.");
        }

        /// <summary>Keeping alive processing tasks: maximum number of processed messages.</summary>
        internal const int KEEP_ALIVE_NUMBER_OF_MESSAGES_THRESHOLD = 1;
        /// <summary>Keeping alive processing tasks: do not attempt this many times.</summary>
        internal const int KEEP_ALIVE_BAN_COUNT = 1000;

        /// <summary>A predicate type for TryKeepAliveUntil.</summary>
        /// <param name="stateIn">Input state for the predicate in order to avoid closure allocations.</param>
        /// <param name="stateOut">Output state for the predicate in order to avoid closure allocations.</param>
        /// <returns>The state of the predicate.</returns>
        internal delegate bool KeepAlivePredicate<TStateIn, TStateOut>(TStateIn stateIn, out TStateOut stateOut);

        /// <summary>Actively waits for a predicate to become true.</summary>
        /// <param name="predicate">The predicate to become true.</param>
        /// <param name="stateIn">Input state for the predicate in order to avoid closure allocations.</param>
        /// <param name="stateOut">Output state for the predicate in order to avoid closure allocations.</param>
        /// <returns>True if the predicate was evaluated and it returned true. False otherwise.</returns>
        internal static bool TryKeepAliveUntil<TStateIn, TStateOut>(KeepAlivePredicate<TStateIn, TStateOut> predicate,
                                                                    TStateIn stateIn, [MaybeNullWhen(false)] out TStateOut stateOut)
        {
            Debug.Assert(predicate != null, "Non-null predicate to execute is required.");
            const int ITERATION_LIMIT = 16;

            for (int c = ITERATION_LIMIT; c > 0; c--)
            {
                if (!Thread.Yield())
                {
                    // There was no other thread waiting.
                    // We may spend some more cycles to evaluate the predicate.
                    if (predicate(stateIn, out stateOut)) return true;
                }
            }

            stateOut = default(TStateOut);
            return false;
        }

        /// <summary>Unwraps an instance T from object state that is a WeakReference to that instance.</summary>
        /// <typeparam name="T">The type of the data to be unwrapped.</typeparam>
        /// <param name="state">The weak reference.</param>
        /// <returns>The T instance.</returns>
        internal static T? UnwrapWeakReference<T>(object state) where T : class
        {
            var wr = state as WeakReference<T>;
            Debug.Assert(wr != null, "Expected a WeakReference<T> as the state argument");
            return wr.TryGetTarget(out T? item) ? item : null;
        }

        /// <summary>Gets an ID for the dataflow block.</summary>
        /// <param name="block">The dataflow block.</param>
        /// <returns>An ID for the dataflow block.</returns>
        internal static int GetBlockId(IDataflowBlock block)
        {
            Debug.Assert(block != null, "Block required to extract an Id.");
            const int NOTASKID = 0; // tasks don't have 0 as ids
            Task? t = Common.GetPotentiallyNotSupportedCompletionTask(block);
            return t != null ? t.Id : NOTASKID;
        }

        /// <summary>Gets the name for the specified block, suitable to be rendered in a debugger window.</summary>
        /// <param name="block">The block for which a name is needed.</param>
        /// <param name="options">
        /// The options to use when rendering the name. If no options are provided, the block's name is used directly.
        /// </param>
        /// <returns>The name of the object.</returns>
        /// <remarks>This is used from DebuggerDisplay attributes.</remarks>
        internal static string GetNameForDebugger(
            IDataflowBlock block, DataflowBlockOptions? options = null)
        {
            Debug.Assert(block != null, "Should only be used with valid objects being displayed in the debugger.");
            Debug.Assert(options == null || options.NameFormat != null, "If options are provided, NameFormat must be valid.");

            if (block == null) return string.Empty;

            string blockName = block.GetType().Name;
            if (options == null) return blockName;

            // {0} == block name
            // {1} == block id
            int blockId = GetBlockId(block);

            // Since NameFormat is public, formatting may throw if the user has set
            // a string that contains a reference to an argument higher than {1}.
            // In the case of an exception, show the exception message.
            try
            {
                return string.Format(options.NameFormat, blockName, blockId);
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
        }

        /// <summary>
        /// Gets whether the exception represents a cooperative cancellation acknowledgment.
        /// </summary>
        /// <param name="exception">The exception to check.</param>
        /// <returns>true if this exception represents a cooperative cancellation acknowledgment; otherwise, false.</returns>
        internal static bool IsCooperativeCancellation(Exception exception)
        {
            Debug.Assert(exception != null, "An exception to check for cancellation must be provided.");
            return exception is OperationCanceledException;
            // Note that the behavior of this method does not exactly match that of Parallel.*, PLINQ, and Task.Factory.StartNew,
            // in that it's more liberal and treats any OCE as acknowledgment of cancellation; in contrast, the other
            // libraries only treat OCEs as such if they contain the same token that was provided by the user
            // and if that token has cancellation requested.  Such logic could be achieved here with:
            //   var oce = exception as OperationCanceledException;
            //   return oce != null &&
            //          oce.CancellationToken == dataflowBlockOptions.CancellationToken &&
            //          oce.CancellationToken.IsCancellationRequested;
            // However, that leads to a discrepancy with the async processing case of dataflow blocks,
            // where tasks are returned to represent the message processing, potentially in the Canceled state,
            // and we simply ignore such tasks.  Further, for blocks like TransformBlock, it's useful to be able
            // to cancel an individual operation which must return a TOutput value, simply by throwing an OperationCanceledException.
            // In such cases, you wouldn't want cancellation tied to the token, because you would only be able to
            // cancel an individual message processing if the whole block was canceled.
        }

        /// <summary>Registers a block for cancellation by completing when cancellation is requested.</summary>
        /// <param name="cancellationToken">The block's cancellation token.</param>
        /// <param name="completionTask">The task that will complete when the block is completely done processing.</param>
        /// <param name="completeAction">An action that will decline permanently on the state passed to it.</param>
        /// <param name="completeState">The block on which to decline permanently.</param>
        internal static void WireCancellationToComplete(
            CancellationToken cancellationToken, Task completionTask, Action<object?> completeAction, object completeState)
        {
            Debug.Assert(completionTask != null, "A task to wire up for completion is needed.");
            Debug.Assert(completeAction != null, "An action to invoke upon cancellation is required.");

            // If a cancellation request has already occurred, just invoke the declining action synchronously.
            // CancellationToken would do this anyway but we can short-circuit it further and avoid a bunch of unnecessary checks.
            if (cancellationToken.IsCancellationRequested)
            {
                completeAction(completeState);
            }
            // Otherwise, if a cancellation request occurs, we want to prevent the block from accepting additional
            // data, and we also want to dispose of that registration when we complete so that we don't
            // leak into a long-living cancellation token.
            else if (cancellationToken.CanBeCanceled)
            {
                CancellationTokenRegistration reg = cancellationToken.Register(completeAction, completeState);
                completionTask.ContinueWith((completed, state) => ((CancellationTokenRegistration)state!).Dispose(),
                    reg, cancellationToken, Common.GetContinuationOptions(), TaskScheduler.Default);
            }
        }

        /// <summary>Initializes the stack trace and watson bucket of an inactive exception.</summary>
        /// <param name="exception">The exception to initialize.</param>
        /// <returns>The initialized exception.</returns>
        internal static Exception InitializeStackTrace(Exception exception)
        {
            Debug.Assert(exception != null && exception.StackTrace == null,
                "A valid but uninitialized exception should be provided.");
            try { throw exception; }
            catch { return exception; }
        }

        /// <summary>The name of the key in an Exception's Data collection used to store information on a dataflow message.</summary>
        internal const string EXCEPTIONDATAKEY_DATAFLOWMESSAGEVALUE = "DataflowMessageValue"; // should not be localized

        /// <summary>Stores details on a dataflow message into an Exception's Data collection.</summary>
        /// <typeparam name="T">Specifies the type of data stored in the message.</typeparam>
        /// <param name="exc">The Exception whose Data collection should store message information.</param>
        /// <param name="messageValue">The message information to be stored.</param>
        /// <param name="targetInnerExceptions">Whether to store the data into the exception's inner exception(s) in addition to the exception itself.</param>
        internal static void StoreDataflowMessageValueIntoExceptionData<T>(Exception exc, T messageValue, bool targetInnerExceptions = false)
        {
            Debug.Assert(exc != null, "The exception into which data should be stored must be provided.");

            // Get the string value to store
            string? strValue = messageValue as string;
            if (strValue == null && messageValue != null)
            {
                try
                {
                    strValue = messageValue.ToString();
                }
                catch { /* It's ok to eat all exceptions here.  If ToString throws, we'll just ignore it. */ }
            }
            if (strValue == null) return;

            // Store the data into the exception itself
            StoreStringIntoExceptionData(exc, Common.EXCEPTIONDATAKEY_DATAFLOWMESSAGEVALUE, strValue);

            // If we also want to target inner exceptions...
            if (targetInnerExceptions)
            {
                // If this is an aggregate, store into all inner exceptions.
                var aggregate = exc as AggregateException;
                if (aggregate != null)
                {
                    foreach (Exception innerException in aggregate.InnerExceptions)
                    {
                        StoreStringIntoExceptionData(innerException, Common.EXCEPTIONDATAKEY_DATAFLOWMESSAGEVALUE, strValue);
                    }
                }
                // Otherwise, if there's an Exception.InnerException, store into that.
                else if (exc.InnerException != null)
                {
                    StoreStringIntoExceptionData(exc.InnerException, Common.EXCEPTIONDATAKEY_DATAFLOWMESSAGEVALUE, strValue);
                }
            }
        }

        /// <summary>Stores the specified string value into the specified key slot of the specified exception's data dictionary.</summary>
        /// <param name="exception">The exception into which the key/value should be stored.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value to be serialized as a string and stored.</param>
        /// <remarks>If the key is already present in the exception's data dictionary, the value is not overwritten.</remarks>
        private static void StoreStringIntoExceptionData(Exception exception, string key, string value)
        {
            Debug.Assert(exception != null, "An exception is needed to store the data into.");
            Debug.Assert(key != null, "A key into the exception's data collection is needed.");
            Debug.Assert(value != null, "The value to store must be provided.");
            try
            {
                IDictionary data = exception.Data;
                if (data != null && !data.IsFixedSize && !data.IsReadOnly && data[key] == null)
                {
                    data[key] = value;
                }
            }
            catch
            {
                // It's ok to eat all exceptions here.  This could throw if an Exception type
                // has overridden Data to behave differently than we expect.
            }
        }

        /// <summary>Throws an exception asynchronously on the thread pool.</summary>
        /// <param name="error">The exception to throw.</param>
        /// <remarks>
        /// This function is used when an exception needs to be propagated from a thread
        /// other than the current context.  This could happen, for example, if the exception
        /// should cause the standard CLR exception escalation behavior, but we're inside
        /// of a task that will squirrel the exception away.
        /// </remarks>
        internal static void ThrowAsync(Exception error)
        {
            ExceptionDispatchInfo edi = ExceptionDispatchInfo.Capture(error);
            ThreadPool.QueueUserWorkItem(state => { ((ExceptionDispatchInfo)state!).Throw(); }, edi);
        }

        /// <summary>Adds the exception to the list, first initializing the list if the list is null.</summary>
        /// <param name="list">The list to add the exception to, and initialize if null.</param>
        /// <param name="exception">The exception to add or whose inner exception(s) should be added.</param>
        /// <param name="unwrapInnerExceptions">Unwrap and add the inner exception(s) rather than the specified exception directly.</param>
        /// <remarks>This method is not thread-safe, in that it manipulates <paramref name="list"/> without any synchronization.</remarks>
        internal static void AddException([NotNull] ref List<Exception>? list, Exception exception, bool unwrapInnerExceptions = false)
        {
            Debug.Assert(exception != null, "An exception to add is required.");
            Debug.Assert(!unwrapInnerExceptions || exception.InnerException != null,
                "If unwrapping is requested, an inner exception is required.");

            // Make sure the list of exceptions is initialized (lazily).
            list ??= new List<Exception>();

            if (unwrapInnerExceptions)
            {
                AggregateException? aggregate = exception as AggregateException;
                if (aggregate != null)
                {
                    list.AddRange(aggregate.InnerExceptions);
                }
                else
                {
                    list.Add(exception.InnerException!);
                }
            }
            else list.Add(exception);
        }

        /// <summary>Creates a task we can cache for the desired Boolean result.</summary>
        /// <param name="value">The value of the Boolean.</param>
        /// <returns>A task that may be cached.</returns>
        private static Task<bool> CreateCachedBooleanTask(bool value)
        {
            // AsyncTaskMethodBuilder<Boolean> caches tasks that are non-disposable.
            // By using these same tasks, we're a bit more robust against disposals,
            // in that such a disposed task's ((IAsyncResult)task).AsyncWaitHandle
            // is still valid.
            var atmb = System.Runtime.CompilerServices.AsyncTaskMethodBuilder<bool>.Create();
            atmb.SetResult(value);
            return atmb.Task; // must be accessed after SetResult to get the cached task
        }

        /// <summary>Creates a TaskCompletionSource{T} completed with a value of default(T) that we can cache.</summary>
        /// <returns>Completed TaskCompletionSource{T} that may be cached.</returns>
        private static TaskCompletionSource<T> CreateCachedTaskCompletionSource<T>()
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetResult(default(T)!);
            return tcs;
        }

        /// <summary>Creates a task faulted with the specified exception.</summary>
        /// <typeparam name="TResult">Specifies the type of the result for this task.</typeparam>
        /// <param name="exception">The exception with which to complete the task.</param>
        /// <returns>The faulted task.</returns>
        internal static Task<TResult> CreateTaskFromException<TResult>(Exception exception)
        {
            var atmb = System.Runtime.CompilerServices.AsyncTaskMethodBuilder<TResult>.Create();
            atmb.SetException(exception);
            return atmb.Task;
        }

        /// <summary>Creates a task canceled with the specified cancellation token.</summary>
        /// <typeparam name="TResult">Specifies the type of the result for this task.</typeparam>
        /// <returns>The canceled task.</returns>
        internal static Task<TResult> CreateTaskFromCancellation<TResult>(CancellationToken cancellationToken)
        {
            Debug.Assert(cancellationToken.IsCancellationRequested,
                "The task will only be immediately canceled if the token has cancellation requested already.");
            var t = new Task<TResult>(CachedGenericDelegates<TResult>.DefaultTResultFunc, cancellationToken);
            Debug.Assert(t.IsCanceled, "Task's constructor should cancel the task synchronously in the ctor.");
            return t;
        }

        /// <summary>Gets the completion task of a block, and protects against common cases of the completion task not being implemented or supported.</summary>
        /// <param name="block">The block.</param>
        /// <returns>The completion task, or null if the block's completion task is not implemented or supported.</returns>
        internal static Task? GetPotentiallyNotSupportedCompletionTask(IDataflowBlock block)
        {
            Debug.Assert(block != null, "We need a block from which to retrieve a cancellation task.");
            try
            {
                return block.Completion;
            }
            catch (NotImplementedException) { }
            catch (NotSupportedException) { }
            return null;
        }

        /// <summary>
        /// Creates an IDisposable that, when disposed, will acquire the outgoing lock while removing
        /// the target block from the target registry.
        /// </summary>
        /// <typeparam name="TOutput">Specifies the type of data in the block.</typeparam>
        /// <param name="outgoingLock">The outgoing lock used to protect the target registry.</param>
        /// <param name="targetRegistry">The target registry from which the target should be removed.</param>
        /// <param name="targetBlock">The target to remove from the registry.</param>
        /// <returns>An IDisposable that will unregister the target block from the registry while holding the outgoing lock.</returns>
        internal static IDisposable CreateUnlinker<TOutput>(object outgoingLock, TargetRegistry<TOutput> targetRegistry, ITargetBlock<TOutput> targetBlock)
        {
            Debug.Assert(outgoingLock != null, "Monitor object needed to protect the operation.");
            Debug.Assert(targetRegistry != null, "Registry from which to remove is required.");
            Debug.Assert(targetBlock != null, "Target block to unlink is required.");
            return Disposables.Create(CachedGenericDelegates<TOutput>.CreateUnlinkerShimAction,
                outgoingLock, targetRegistry, targetBlock);
        }

        /// <summary>An infinite TimeSpan.</summary>
        internal static readonly TimeSpan InfiniteTimeSpan = Timeout.InfiniteTimeSpan;

        /// <summary>Validates that a timeout either is -1 or is non-negative and within the range of an Int32.</summary>
        /// <param name="timeout">The timeout to validate.</param>
        /// <returns>true if the timeout is valid; otherwise, false.</returns>
        internal static bool IsValidTimeout(TimeSpan timeout)
        {
            long millisecondsTimeout = (long)timeout.TotalMilliseconds;
            return millisecondsTimeout >= Timeout.Infinite && millisecondsTimeout <= int.MaxValue;
        }

        /// <summary>Gets the options to use for continuation tasks.</summary>
        /// <param name="toInclude">Any options to include in the result.</param>
        /// <returns>The options to use.</returns>
        internal static TaskContinuationOptions GetContinuationOptions(TaskContinuationOptions toInclude = TaskContinuationOptions.None)
        {
            return toInclude | TaskContinuationOptions.DenyChildAttach;
        }

        /// <summary>Gets the options to use for tasks.</summary>
        /// <param name="isReplacementReplica">If this task is being created to replace another.</param>
        /// <remarks>
        /// These options should be used for all tasks that have the potential to run user code or
        /// that are repeatedly spawned and thus need a modicum of fair treatment.
        /// </remarks>
        /// <returns>The options to use.</returns>
        internal static TaskCreationOptions GetCreationOptionsForTask(bool isReplacementReplica = false)
        {
            TaskCreationOptions options = TaskCreationOptions.DenyChildAttach;
            if (isReplacementReplica) options |= TaskCreationOptions.PreferFairness;
            return options;
        }

        /// <summary>Starts an already constructed task with handling and observing exceptions that may come from the scheduling process.</summary>
        /// <param name="task">Task to be started.</param>
        /// <param name="scheduler">TaskScheduler to schedule the task on.</param>
        /// <returns>null on success, an exception reference on scheduling error. In the latter case, the task reference is nulled out.</returns>
        internal static Exception? StartTaskSafe(Task task, TaskScheduler scheduler)
        {
            Debug.Assert(task != null, "Task to start is required.");
            Debug.Assert(scheduler != null, "Scheduler on which to start the task is required.");

            if (scheduler == TaskScheduler.Default)
            {
                task.Start(scheduler);
                return null; // We don't need to worry about scheduler exceptions from the default scheduler.
            }
            // Slow path with try/catch separated out so that StartTaskSafe may be inlined in the common case.
            else return StartTaskSafeCore(task, scheduler);
        }

        /// <summary>Starts an already constructed task with handling and observing exceptions that may come from the scheduling process.</summary>
        /// <param name="task">Task to be started.</param>
        /// <param name="scheduler">TaskScheduler to schedule the task on.</param>
        /// <returns>null on success, an exception reference on scheduling error. In the latter case, the task reference is nulled out.</returns>
        private static Exception? StartTaskSafeCore(Task task, TaskScheduler scheduler)
        {
            Debug.Assert(task != null, "Task to start is needed.");
            Debug.Assert(scheduler != null, "Scheduler on which to start the task is required.");

            Exception? schedulingException = null;

            try
            {
                task.Start(scheduler);
            }
            catch (Exception caughtException)
            {
                // Verify TPL has faulted the task
                Debug.Assert(task.IsFaulted, "The task should have been faulted if it failed to start.");

                // Observe the task's exception
                _ = task.Exception;

                schedulingException = caughtException;
            }

            return schedulingException;
        }

        /// <summary>Pops and explicitly releases postponed messages after the block is done with processing.</summary>
        /// <remarks>No locks should be held at this time. Unfortunately we cannot assert that.</remarks>
        internal static void ReleaseAllPostponedMessages<T>(ITargetBlock<T> target,
                                    QueuedMap<ISourceBlock<T>, DataflowMessageHeader> postponedMessages,
                                    ref List<Exception>? exceptions)
        {
            Debug.Assert(target != null, "There must be a subject target.");
            Debug.Assert(postponedMessages != null, "The stacked map of postponed messages must exist.");

            // Note that we don't synchronize on lockObject for postponedMessages here,
            // because no one should be adding to it at this time.  We do a bit of
            // checking just for sanity's sake.
            int initialCount = postponedMessages.Count;
            int processedCount = 0;

            KeyValuePair<ISourceBlock<T>, DataflowMessageHeader> sourceAndMessage;
            while (postponedMessages.TryPop(out sourceAndMessage))
            {
                // Loop through all postponed messages declining each messages.
                // The only way we have to do this is by reserving and then immediately releasing each message.
                // This is important for sources like SendAsyncSource, which keep state around until
                // they get a response to a postponed message.
                try
                {
                    Debug.Assert(sourceAndMessage.Key != null, "Postponed messages must have an associated source.");
                    if (sourceAndMessage.Key.ReserveMessage(sourceAndMessage.Value, target))
                    {
                        sourceAndMessage.Key.ReleaseReservation(sourceAndMessage.Value, target);
                    }
                }
                catch (Exception exc)
                {
                    Common.AddException(ref exceptions, exc);
                }

                processedCount++;
            }

            Debug.Assert(processedCount == initialCount,
                "We should have processed the exact number of elements that were initially there.");
        }

        /// <summary>Cache ThrowAsync to avoid allocations when it is passed into PropagateCompletionXxx.</summary>
        internal static readonly Action<Exception> AsyncExceptionHandler = ThrowAsync;

        /// <summary>
        /// Propagates completion of sourceCompletionTask to target synchronously.
        /// </summary>
        /// <param name="sourceCompletionTask">The task whose completion is to be propagated. It must be completed.</param>
        /// <param name="target">The block where completion is propagated.</param>
        /// <param name="exceptionHandler">Handler for exceptions from the target. May be null which would propagate the exception to the caller.</param>
        internal static void PropagateCompletion(Task sourceCompletionTask, IDataflowBlock target, Action<Exception>? exceptionHandler)
        {
            Debug.Assert(sourceCompletionTask != null, "sourceCompletionTask may not be null.");
            Debug.Assert(target != null, "The target where completion is to be propagated may not be null.");
            Debug.Assert(sourceCompletionTask.IsCompleted, "sourceCompletionTask must be completed in order to propagate its completion.");

            AggregateException? exception = sourceCompletionTask.IsFaulted ? sourceCompletionTask.Exception : null;

            try
            {
                if (exception != null) target.Fault(exception);
                else target.Complete();
            }
            catch (Exception exc)
            {
                if (exceptionHandler != null) exceptionHandler(exc);
                else throw;
            }
        }

        /// <summary>
        /// Creates a continuation off sourceCompletionTask to complete target. See PropagateCompletion.
        /// </summary>
        private static void PropagateCompletionAsContinuation(Task sourceCompletionTask, IDataflowBlock target)
        {
            Debug.Assert(sourceCompletionTask != null, "sourceCompletionTask may not be null.");
            Debug.Assert(target != null, "The target where completion is to be propagated may not be null.");
            sourceCompletionTask.ContinueWith((task, state) => Common.PropagateCompletion(task, (IDataflowBlock)state!, AsyncExceptionHandler),
                target, CancellationToken.None, Common.GetContinuationOptions(), TaskScheduler.Default);
        }

        /// <summary>
        /// Propagates completion of sourceCompletionTask to target based on sourceCompletionTask's current state. See PropagateCompletion.
        /// </summary>
        internal static void PropagateCompletionOnceCompleted(Task sourceCompletionTask, IDataflowBlock target)
        {
            Debug.Assert(sourceCompletionTask != null, "sourceCompletionTask may not be null.");
            Debug.Assert(target != null, "The target where completion is to be propagated may not be null.");

            // If sourceCompletionTask is completed, propagate completion synchronously.
            // Otherwise hook up a continuation.
            if (sourceCompletionTask.IsCompleted) PropagateCompletion(sourceCompletionTask, target, exceptionHandler: null);
            else PropagateCompletionAsContinuation(sourceCompletionTask, target);
        }

        /// <summary>Static class used to cache generic delegates the C# compiler doesn't cache by default.</summary>
        /// <remarks>Without this, we end up allocating the generic delegate each time the operation is used.</remarks>
        private static class CachedGenericDelegates<T>
        {
            /// <summary>A function that returns the default value of T.</summary>
            internal static readonly Func<T> DefaultTResultFunc = () => default(T)!;
            /// <summary>
            /// A function to use as the body of ActionOnDispose in CreateUnlinkerShim.
            /// Passed a tuple of the sync obj, the target registry, and the target block as the state parameter.
            /// </summary>
            internal static readonly Action<object, TargetRegistry<T>, ITargetBlock<T>> CreateUnlinkerShimAction =
                (syncObj, registry, target) =>
            {
                lock (syncObj) registry.Remove(target);
            };
        }
    }

    /// <summary>State used only when bounding.</summary>
    [DebuggerDisplay("BoundedCapacity={BoundedCapacity}}")]
    internal class BoundingState
    {
        /// <summary>The maximum number of messages allowed to be buffered.</summary>
        internal readonly int BoundedCapacity;
        /// <summary>The number of messages currently stored.</summary>
        /// <remarks>
        /// This value may temporarily be higher than the actual number stored.
        /// That's ok, we just can't accept any new messages if CurrentCount >= BoundedCapacity.
        /// Worst case is that we may temporarily have fewer items in the block than our maximum allows,
        /// but we'll never have more.
        /// </remarks>
        internal int CurrentCount;

        /// <summary>Initializes the BoundingState.</summary>
        /// <param name="boundedCapacity">The positive bounded capacity.</param>
        internal BoundingState(int boundedCapacity)
        {
            Debug.Assert(boundedCapacity > 0, "Bounded is only supported with positive values.");
            BoundedCapacity = boundedCapacity;
        }

        /// <summary>Gets whether there's room available to add another message.</summary>
        internal bool CountIsLessThanBound { get { return CurrentCount < BoundedCapacity; } }
    }

    /// <summary>Stated used only when bounding and when postponed messages are stored.</summary>
    /// <typeparam name="TInput">Specifies the type of input messages.</typeparam>
    [DebuggerDisplay("BoundedCapacity={BoundedCapacity}, PostponedMessages={PostponedMessagesCountForDebugger}")]
    internal class BoundingStateWithPostponed<TInput> : BoundingState
    {
        /// <summary>Queue of postponed messages.</summary>
        internal readonly QueuedMap<ISourceBlock<TInput>, DataflowMessageHeader> PostponedMessages =
            new QueuedMap<ISourceBlock<TInput>, DataflowMessageHeader>();
        /// <summary>
        /// The number of transfers from the postponement queue to the input queue currently being processed.
        /// </summary>
        /// <remarks>
        /// Blocks that use TargetCore need to transfer messages from the postponed queue to the input messages
        /// queue.  While doing that, new incoming messages may arrive, and if they view the postponed queue
        /// as being empty (after the block has removed the last postponed message and is consuming it, before
        /// storing it into the input queue), they might go directly into the input queue... that will then mess
        /// up the ordering between those postponed messages and the newly incoming messages.  To address that,
        /// OutstandingTransfers is used to track the number of transfers currently in progress.  Incoming
        /// messages must be postponed not only if there are already any postponed messages, but also if
        /// there are any transfers in progress (i.e. this value is > 0).  It's an integer because the DOP could
        /// be greater than 1, and thus we need to ref count multiple transfers that might be in progress.
        /// </remarks>
        internal int OutstandingTransfers;

        /// <summary>Initializes the BoundingState.</summary>
        /// <param name="boundedCapacity">The positive bounded capacity.</param>
        internal BoundingStateWithPostponed(int boundedCapacity) : base(boundedCapacity)
        {
        }

        /// <summary>Gets the number of postponed messages for the debugger.</summary>
        private int PostponedMessagesCountForDebugger { get { return PostponedMessages.Count; } }
    }

    /// <summary>Stated used only when bounding and when postponed messages and a task are stored.</summary>
    /// <typeparam name="TInput">Specifies the type of input messages.</typeparam>
    internal class BoundingStateWithPostponedAndTask<TInput> : BoundingStateWithPostponed<TInput>
    {
        /// <summary>The task used to process messages.</summary>
        internal Task? TaskForInputProcessing;

        /// <summary>Initializes the BoundingState.</summary>
        /// <param name="boundedCapacity">The positive bounded capacity.</param>
        internal BoundingStateWithPostponedAndTask(int boundedCapacity) : base(boundedCapacity)
        {
        }
    }

    /// <summary>
    /// Type used with TaskCompletionSource(Of TResult) as the TResult
    /// to ensure that the resulting task can't be upcast to something
    /// that in the future could lead to compat problems.
    /// </summary>
    [DebuggerNonUserCode]
    internal struct VoidResult { }
}
