// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Provides a builder for asynchronous methods that return <see cref="System.Threading.Tasks.Task"/>.
    /// This type is intended for compiler use only.
    /// </summary>
    /// <remarks>
    /// AsyncTaskMethodBuilder is a value type, and thus it is copied by value.
    /// Prior to being copied, one of its Task, SetResult, or SetException members must be accessed,
    /// or else the copies may end up building distinct Task instances.
    /// </remarks>
    public struct AsyncTaskMethodBuilder
    {
        /// <summary>The lazily-initialized built task.</summary>
        private Task<VoidTaskResult>? m_task; // Debugger depends on the exact name of this field.

        /// <summary>Initializes a new <see cref="AsyncTaskMethodBuilder"/>.</summary>
        /// <returns>The initialized <see cref="AsyncTaskMethodBuilder"/>.</returns>
        public static AsyncTaskMethodBuilder Create() => default;

        /// <summary>Initiates the builder's execution with the associated state machine.</summary>
        /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
        /// <param name="stateMachine">The state machine instance, passed by reference.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine =>
            AsyncMethodBuilderCore.Start(ref stateMachine);

        /// <summary>Associates the builder with the state machine it represents.</summary>
        /// <param name="stateMachine">The heap-allocated state machine object.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="stateMachine"/> argument was null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.InvalidOperationException">The builder is incorrectly initialized.</exception>
        public void SetStateMachine(IAsyncStateMachine stateMachine) =>
            AsyncMethodBuilderCore.SetStateMachine(stateMachine, task: null);

        /// <summary>
        /// Schedules the specified state machine to be pushed forward when the specified awaiter completes.
        /// </summary>
        /// <typeparam name="TAwaiter">Specifies the type of the awaiter.</typeparam>
        /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
        /// <param name="awaiter">The awaiter.</param>
        /// <param name="stateMachine">The state machine.</param>
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine =>
            AsyncTaskMethodBuilder<VoidTaskResult>.AwaitOnCompleted(ref awaiter, ref stateMachine, ref m_task);

        /// <summary>
        /// Schedules the specified state machine to be pushed forward when the specified awaiter completes.
        /// </summary>
        /// <typeparam name="TAwaiter">Specifies the type of the awaiter.</typeparam>
        /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
        /// <param name="awaiter">The awaiter.</param>
        /// <param name="stateMachine">The state machine.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine =>
            AsyncTaskMethodBuilder<VoidTaskResult>.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine, ref m_task);

        /// <summary>Gets the <see cref="System.Threading.Tasks.Task"/> for this builder.</summary>
        /// <returns>The <see cref="System.Threading.Tasks.Task"/> representing the builder's asynchronous operation.</returns>
        /// <exception cref="System.InvalidOperationException">The builder is not initialized.</exception>
        public Task Task
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_task ?? InitializeTaskAsPromise();
        }

        /// <summary>
        /// Initializes the task, which must not yet be initialized.  Used only when the Task is being forced into
        /// existence when no state machine is needed, e.g. when the builder is being synchronously completed with
        /// an exception, when the builder is being used out of the context of an async method, etc.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private Task<VoidTaskResult> InitializeTaskAsPromise()
        {
            Debug.Assert(m_task is null);
            return m_task = new Task<VoidTaskResult>();
        }

        /// <summary>
        /// Completes the <see cref="System.Threading.Tasks.Task"/> in the
        /// <see cref="System.Threading.Tasks.TaskStatus">RanToCompletion</see> state.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">The builder is not initialized.</exception>
        /// <exception cref="System.InvalidOperationException">The task has already completed.</exception>
        public void SetResult()
        {
            // Get the currently stored task, which will be non-null if get_Task has already been accessed.
            // If there isn't one, store the supplied completed task.
            if (m_task is null)
            {
                m_task = Task.s_cachedCompleted;
            }
            else
            {
                // Otherwise, complete the task that's there.
                AsyncTaskMethodBuilder<VoidTaskResult>.SetExistingTaskResult(m_task, default!);
            }
        }

        /// <summary>
        /// Completes the <see cref="System.Threading.Tasks.Task"/> in the
        /// <see cref="System.Threading.Tasks.TaskStatus">Faulted</see> state with the specified exception.
        /// </summary>
        /// <param name="exception">The <see cref="System.Exception"/> to use to fault the task.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="exception"/> argument is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.InvalidOperationException">The builder is not initialized.</exception>
        /// <exception cref="System.InvalidOperationException">The task has already completed.</exception>
        public void SetException(Exception exception) =>
            AsyncTaskMethodBuilder<VoidTaskResult>.SetException(exception, ref m_task);

        /// <summary>
        /// Called by the debugger to request notification when the first wait operation
        /// (await, Wait, Result, etc.) on this builder's task completes.
        /// </summary>
        /// <param name="enabled">
        /// true to enable notification; false to disable a previously set notification.
        /// </param>
        internal void SetNotificationForWaitCompletion(bool enabled) =>
            AsyncTaskMethodBuilder<VoidTaskResult>.SetNotificationForWaitCompletion(enabled, ref m_task);

        internal static void ExecuteFromThreadPool(Thread threadPoolThread, Task asyncStateMachineBox, ExecutionContext? context, ContextCallback callback)
        {
            Debug.Assert(!asyncStateMachineBox.IsCompleted);
            Debug.Assert(threadPoolThread == Thread.CurrentThread);

            bool loggingOn = TplEventSource.Log.IsEnabled();
            if (loggingOn)
            {
                // Jump forward for logging, so its not picked.
                goto LogStart;
            }

        Start:
            if (context is not null && !context.IsDefault)
            {
                ExecutionContext.RestoreNonDefaultContextToThreadPool(threadPoolThread, context);
            }

            callback(asyncStateMachineBox);
            // ThreadPoolWorkQueue.Dispatch will handle notifications and reset EC and SyncCtx back to default

            // Can't do much here to work with the branch predictor without excessive gotos.
            if (asyncStateMachineBox.IsCompleted)
            {
                // If async debugging is enabled, remove the task from tracking.
                if (System.Threading.Tasks.Task.s_asyncDebuggingEnabled)
                {
                    System.Threading.Tasks.Task.RemoveFromActiveTasks(asyncStateMachineBox);
                }

#if !CORERT
                // In case this is a state machine box with a finalizer, suppress its finalization
                // as it's now complete.  We only need the finalizer to run if the box is collected
                // without having been completed.
                if (AsyncMethodBuilderCore.TrackAsyncMethodCompletion)
                {
                    GC.SuppressFinalize(asyncStateMachineBox);
                }
#endif
            }

            if (!loggingOn)
            {
                // Logging off: Preferred.
                return;
            }

            // Logging on: Not preferred.
            TplEventSource.Log.TraceSynchronousWorkEnd(CausalitySynchronousWork.Execution);
            return;

        LogStart:
            TplEventSource.Log.TraceSynchronousWorkBegin(asyncStateMachineBox.Id, CausalitySynchronousWork.Execution);
            goto Start;
        }

        internal static void Execute(Task asyncStateMachineBox, ExecutionContext? context, ContextCallback callback)
        {
            Debug.Assert(!asyncStateMachineBox.IsCompleted);
            // As we can't annotate to Jit which branch is unlikely to be taken
            // this is arranged to prefer specific paths.
            bool loggingOn = TplEventSource.Log.IsEnabled();
            if (loggingOn)
            {
                // Jump forward for logging, so its not picked.
                goto LogStart;
            }

        Start:
            if (context is not null)
            {
                if (context.IsDefault)
                {
                    // 1st preference: Default context.
                    Thread currentThread = Thread.CurrentThread;
                    ExecutionContext? currentContext = currentThread._executionContext;
                    if (currentContext is null || currentContext.IsDefault)
                    {
                        // Preferred: On Default and to run on Default; however we need to undo any changes that happen in call.
                        SynchronizationContext? previousSyncCtx = currentThread._synchronizationContext;
                        ExceptionDispatchInfo? edi = null;
                        try
                        {
                            // Run directly
                            callback(asyncStateMachineBox);
                        }
                        catch (Exception ex)
                        {
                            edi = ExceptionDispatchInfo.Capture(ex);
                        }

                        if (currentThread._executionContext is null)
                        {
                            if (currentThread._synchronizationContext != previousSyncCtx)
                            {
                                currentThread._synchronizationContext = previousSyncCtx;
                            }

                            edi?.Throw();
                        }
                        else
                        {
                            ExecutionContext.RestoreDefaultContextThrowIfNeeded(currentThread, previousSyncCtx, edi);
                        }
                    }
                    else
                    {
                        // Not preferred: Current thread is not on Default.
                        ExecutionContext.RunOnDefaultContext(currentThread, currentContext, callback, asyncStateMachineBox);
                    }
                }
                else
                {
                    // 2nd preference: non-default context.
                    ExecutionContext.RunInternal(context, callback, asyncStateMachineBox);
                }
            }
            else
            {
                // 3rd preference: flow supressed context.
                callback(asyncStateMachineBox);
            }

            // Can't do much here to work with the branch predictor without excessive gotos.
            if (asyncStateMachineBox.IsCompleted)
            {
                // If async debugging is enabled, remove the task from tracking.
                if (System.Threading.Tasks.Task.s_asyncDebuggingEnabled)
                {
                    System.Threading.Tasks.Task.RemoveFromActiveTasks(asyncStateMachineBox);
                }

#if !CORERT
                // In case this is a state machine box with a finalizer, suppress its finalization
                // as it's now complete.  We only need the finalizer to run if the box is collected
                // without having been completed.
                if (AsyncMethodBuilderCore.TrackAsyncMethodCompletion)
                {
                    GC.SuppressFinalize(asyncStateMachineBox);
                }
#endif
            }

            if (!loggingOn)
            {
                // Logging Off: Preferred.
                return;
            }

            // Logging on: Not preferred.
            TplEventSource.Log.TraceSynchronousWorkEnd(CausalitySynchronousWork.Execution);
            return;

        LogStart:
            TplEventSource.Log.TraceSynchronousWorkBegin(asyncStateMachineBox.Id, CausalitySynchronousWork.Execution);
            goto Start;

        }


        /// <summary>
        /// Gets an object that may be used to uniquely identify this builder to the debugger.
        /// </summary>
        /// <remarks>
        /// This property lazily instantiates the ID in a non-thread-safe manner.
        /// It must only be used by the debugger and tracing purposes, and only in a single-threaded manner
        /// when no other threads are in the middle of accessing this property or this.Task.
        /// </remarks>
        internal object ObjectIdForDebugger =>
            m_task ??= AsyncTaskMethodBuilder<VoidTaskResult>.CreateWeaklyTypedStateMachineBox();
    }
}
