// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Provides a builder for asynchronous methods that return <see cref="System.Threading.Tasks.Task{TResult}"/>.
    /// This type is intended for compiler use only.
    /// </summary>
    /// <remarks>
    /// AsyncTaskMethodBuilder{TResult} is a value type, and thus it is copied by value.
    /// Prior to being copied, one of its Task, SetResult, or SetException members must be accessed,
    /// or else the copies may end up building distinct Task instances.
    /// </remarks>
    public struct AsyncTaskMethodBuilder<TResult>
    {
        /// <summary>The lazily-initialized built task.</summary>
        private Task<TResult>? m_task; // Debugger depends on the exact name of this field.

        /// <summary>Initializes a new <see cref="AsyncTaskMethodBuilder"/>.</summary>
        /// <returns>The initialized <see cref="AsyncTaskMethodBuilder"/>.</returns>
        public static AsyncTaskMethodBuilder<TResult> Create() => default;

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
            AsyncMethodBuilderCore.SetStateMachine(stateMachine, m_task);

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
            AwaitOnCompleted(ref awaiter, ref stateMachine, ref m_task);

        internal static void AwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine, ref Task<TResult>? taskField)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            try
            {
                awaiter.OnCompleted(GetStateMachineBox(ref stateMachine, ref taskField).MoveNextAction);
            }
            catch (Exception e)
            {
                System.Threading.Tasks.Task.ThrowAsync(e, targetContext: null);
            }
        }

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
            AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine, ref m_task);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine, [NotNull] ref Task<TResult>? taskField)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            IAsyncStateMachineBox box = GetStateMachineBox(ref stateMachine, ref taskField);
            AwaitUnsafeOnCompleted(ref awaiter, box);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)] // workaround boxing allocations in Tier0: https://github.com/dotnet/runtime/issues/9120
        internal static void AwaitUnsafeOnCompleted<TAwaiter>(
            ref TAwaiter awaiter, IAsyncStateMachineBox box)
            where TAwaiter : ICriticalNotifyCompletion
        {
            // The null tests here ensure that the jit can optimize away the interface
            // tests when TAwaiter is a ref type.

            if ((null != (object?)default(TAwaiter)) && (awaiter is ITaskAwaiter))
            {
                ref TaskAwaiter ta = ref Unsafe.As<TAwaiter, TaskAwaiter>(ref awaiter); // relies on TaskAwaiter/TaskAwaiter<T> having the same layout
                TaskAwaiter.UnsafeOnCompletedInternal(ta.m_task, box, continueOnCapturedContext: true);
            }
            else if ((null != (object?)default(TAwaiter)) && (awaiter is IConfiguredTaskAwaiter))
            {
                ref ConfiguredTaskAwaitable.ConfiguredTaskAwaiter ta = ref Unsafe.As<TAwaiter, ConfiguredTaskAwaitable.ConfiguredTaskAwaiter>(ref awaiter);
                TaskAwaiter.UnsafeOnCompletedInternal(ta.m_task, box, ta.m_continueOnCapturedContext);
            }
            else if ((null != (object?)default(TAwaiter)) && (awaiter is IStateMachineBoxAwareAwaiter))
            {
                try
                {
                    ((IStateMachineBoxAwareAwaiter)awaiter).AwaitUnsafeOnCompleted(box);
                }
                catch (Exception e)
                {
                    // Whereas with Task the code that hooks up and invokes the continuation is all local to corelib,
                    // with ValueTaskAwaiter we may be calling out to an arbitrary implementation of IValueTaskSource
                    // wrapped in the ValueTask, and as such we protect against errant exceptions that may emerge.
                    // We don't want such exceptions propagating back into the async method, which can't handle
                    // exceptions well at that location in the state machine, especially if the exception may occur
                    // after the ValueTaskAwaiter already successfully hooked up the callback, in which case it's possible
                    // two different flows of execution could end up happening in the same async method call.
                    System.Threading.Tasks.Task.ThrowAsync(e, targetContext: null);
                }
            }
            else
            {
                // The awaiter isn't specially known. Fall back to doing a normal await.
                try
                {
                    awaiter.UnsafeOnCompleted(box.MoveNextAction);
                }
                catch (Exception e)
                {
                    System.Threading.Tasks.Task.ThrowAsync(e, targetContext: null);
                }
            }
        }

        /// <summary>Gets the "boxed" state machine object.</summary>
        /// <typeparam name="TStateMachine">Specifies the type of the async state machine.</typeparam>
        /// <param name="stateMachine">The state machine.</param>
        /// <param name="taskField">The reference to the Task field storing the Task instance.</param>
        /// <returns>The "boxed" state machine.</returns>
        private static IAsyncStateMachineBox GetStateMachineBox<TStateMachine>(
            ref TStateMachine stateMachine,
            [NotNull] ref Task<TResult>? taskField)
            where TStateMachine : IAsyncStateMachine
        {
            ExecutionContext? currentContext = ExecutionContext.Capture();

            // Check first for the most common case: not the first yield in an async method.
            // In this case, the first yield will have already "boxed" the state machine in
            // a strongly-typed manner into an AsyncStateMachineBox.  It will already contain
            // the state machine as well as a MoveNextDelegate and a context.  The only thing
            // we might need to do is update the context if that's changed since it was stored.
            if (taskField is AsyncStateMachineBox<TStateMachine> stronglyTypedBox)
            {
                if (stronglyTypedBox.Context != currentContext)
                {
                    stronglyTypedBox.Context = currentContext;
                }
                return stronglyTypedBox;
            }

            // The least common case: we have a weakly-typed boxed.  This results if the debugger
            // or some other use of reflection accesses a property like ObjectIdForDebugger or a
            // method like SetNotificationForWaitCompletion prior to the first await happening.  In
            // such situations, we need to get an object to represent the builder, but we don't yet
            // know the type of the state machine, and thus can't use TStateMachine.  Instead, we
            // use the IAsyncStateMachine interface, which all TStateMachines implement.  This will
            // result in a boxing allocation when storing the TStateMachine if it's a struct, but
            // this only happens in active debugging scenarios where such performance impact doesn't
            // matter.
            if (taskField is AsyncStateMachineBox<IAsyncStateMachine> weaklyTypedBox)
            {
                // If this is the first await, we won't yet have a state machine, so store it.
                if (weaklyTypedBox.StateMachine == null)
                {
                    Debugger.NotifyOfCrossThreadDependency(); // same explanation as with usage below
                    weaklyTypedBox.StateMachine = stateMachine;
                }

                // Update the context.  This only happens with a debugger, so no need to spend
                // extra IL checking for equality before doing the assignment.
                weaklyTypedBox.Context = currentContext;
                return weaklyTypedBox;
            }

            // Alert a listening debugger that we can't make forward progress unless it slips threads.
            // If we don't do this, and a method that uses "await foo;" is invoked through funceval,
            // we could end up hooking up a callback to push forward the async method's state machine,
            // the debugger would then abort the funceval after it takes too long, and then continuing
            // execution could result in another callback being hooked up.  At that point we have
            // multiple callbacks registered to push the state machine, which could result in bad behavior.
            Debugger.NotifyOfCrossThreadDependency();

            // At this point, taskField should really be null, in which case we want to create the box.
            // However, in a variety of debugger-related (erroneous) situations, it might be non-null,
            // e.g. if the Task property is examined in a Watch window, forcing it to be lazily-intialized
            // as a Task<TResult> rather than as an AsyncStateMachineBox.  The worst that happens in such
            // cases is we lose the ability to properly step in the debugger, as the debugger uses that
            // object's identity to track this specific builder/state machine.  As such, we proceed to
            // overwrite whatever's there anyway, even if it's non-null.
#if CORERT
            // DebugFinalizableAsyncStateMachineBox looks like a small type, but it actually is not because
            // it will have a copy of all the slots from its parent. It will add another hundred(s) bytes
            // per each async method in CoreRT / ProjectN binaries without adding much value. Avoid
            // generating this extra code until a better solution is implemented.
            var box = new AsyncStateMachineBox<TStateMachine>();
#else
            AsyncStateMachineBox<TStateMachine> box = AsyncMethodBuilderCore.TrackAsyncMethodCompletion ?
                CreateDebugFinalizableAsyncStateMachineBox<TStateMachine>() :
                new AsyncStateMachineBox<TStateMachine>();
#endif
            taskField = box; // important: this must be done before storing stateMachine into box.StateMachine!
            box.StateMachine = stateMachine;
            box.Context = currentContext;

            // Log the creation of the state machine box object / task for this async method.
            if (TplEventSource.Log.IsEnabled())
            {
                TplEventSource.Log.TraceOperationBegin(box.Id, "Async: " + stateMachine.GetType().Name, 0);
            }

            // And if async debugging is enabled, track the task.
            if (System.Threading.Tasks.Task.s_asyncDebuggingEnabled)
            {
                System.Threading.Tasks.Task.AddToActiveTasks(box);
            }

            return box;
        }

#if !CORERT
        // Avoid forcing the JIT to build DebugFinalizableAsyncStateMachineBox<TStateMachine> unless it's actually needed.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static AsyncStateMachineBox<TStateMachine> CreateDebugFinalizableAsyncStateMachineBox<TStateMachine>()
            where TStateMachine : IAsyncStateMachine =>
            new DebugFinalizableAsyncStateMachineBox<TStateMachine>();

        /// <summary>
        /// Provides an async state machine box with a finalizer that will fire an EventSource
        /// event about the state machine if it's being finalized without having been completed.
        /// </summary>
        /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
        private sealed class DebugFinalizableAsyncStateMachineBox<TStateMachine> : // SOS DumpAsync command depends on this name
            AsyncStateMachineBox<TStateMachine>
            where TStateMachine : IAsyncStateMachine
        {
            ~DebugFinalizableAsyncStateMachineBox()
            {
                // If the state machine is being finalized, something went wrong during its processing,
                // e.g. it awaited something that got collected without itself having been completed.
                // Fire an event with details about the state machine to help with debugging.
                if (!IsCompleted) // double-check it's not completed, just to help minimize false positives
                {
                    TplEventSource.Log.IncompleteAsyncMethod(this);
                }
            }
        }
#endif

        /// <summary>A strongly-typed box for Task-based async state machines.</summary>
        /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
        private class AsyncStateMachineBox<TStateMachine> : // SOS DumpAsync command depends on this name
            Task<TResult>, IAsyncStateMachineBox
            where TStateMachine : IAsyncStateMachine
        {
            /// <summary>Delegate used to invoke on an ExecutionContext when passed an instance of this box type.</summary>
            private static readonly ContextCallback s_callback = ExecutionContextCallback;

            // Used to initialize s_callback above. We don't use a lambda for this on purpose: a lambda would
            // introduce a new generic type behind the scenes that comes with a hefty size penalty in AOT builds.
            private static void ExecutionContextCallback(object? s)
            {
                Debug.Assert(s is AsyncStateMachineBox<TStateMachine>);
                // Only used privately to pass directly to EC.Run
                Unsafe.As<AsyncStateMachineBox<TStateMachine>>(s).StateMachine!.MoveNext();
            }

            /// <summary>A delegate to the <see cref="MoveNext()"/> method.</summary>
            private Action? _moveNextAction;
            /// <summary>The state machine itself.</summary>
            public TStateMachine? StateMachine; // mutable struct; do not make this readonly. SOS DumpAsync command depends on this name.
            /// <summary>Captured ExecutionContext with which to invoke <see cref="MoveNextAction"/>; may be null.</summary>
            public ExecutionContext? Context;

            /// <summary>A delegate to the <see cref="MoveNext()"/> method.</summary>
            public Action MoveNextAction => _moveNextAction ??= new Action(MoveNext);

            internal sealed override void ExecuteFromThreadPool(Thread threadPoolThread) => MoveNext(threadPoolThread);

            /// <summary>Calls MoveNext on <see cref="StateMachine"/></summary>
            public void MoveNext() => MoveNext(threadPoolThread: null);

            private void MoveNext(Thread? threadPoolThread)
            {
                Debug.Assert(!IsCompleted);

                bool loggingOn = TplEventSource.Log.IsEnabled();
                if (loggingOn)
                {
                    TplEventSource.Log.TraceSynchronousWorkBegin(this.Id, CausalitySynchronousWork.Execution);
                }

                ExecutionContext? context = Context;
                if (context == null)
                {
                    Debug.Assert(StateMachine != null);
                    StateMachine.MoveNext();
                }
                else
                {
                    if (threadPoolThread is null)
                    {
                        ExecutionContext.RunInternal(context, s_callback, this);
                    }
                    else
                    {
                        ExecutionContext.RunFromThreadPoolDispatchLoop(threadPoolThread, context, s_callback, this);
                    }
                }

                if (IsCompleted)
                {
                    ClearStateUponCompletion();
                }

                if (loggingOn)
                {
                    TplEventSource.Log.TraceSynchronousWorkEnd(CausalitySynchronousWork.Execution);
                }
            }

            /// <summary>Clears out all state associated with a completed box.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ClearStateUponCompletion()
            {
                Debug.Assert(IsCompleted);

                // This logic may be invoked multiple times on the same instance and needs to be robust against that.

                // If async debugging is enabled, remove the task from tracking.
                if (s_asyncDebuggingEnabled)
                {
                    RemoveFromActiveTasks(this);
                }

                // Clear out state now that the async method has completed.
                // This avoids keeping arbitrary state referenced by lifted locals
                // if this Task / state machine box is held onto.
                StateMachine = default;
                Context = default;

#if !CORERT
                // In case this is a state machine box with a finalizer, suppress its finalization
                // as it's now complete.  We only need the finalizer to run if the box is collected
                // without having been completed.
                if (AsyncMethodBuilderCore.TrackAsyncMethodCompletion)
                {
                    GC.SuppressFinalize(this);
                }
#endif
            }

            /// <summary>Gets the state machine as a boxed object.  This should only be used for debugging purposes.</summary>
            IAsyncStateMachine IAsyncStateMachineBox.GetStateMachineObject() => StateMachine!; // likely boxes, only use for debugging
        }

        /// <summary>Gets the <see cref="System.Threading.Tasks.Task{TResult}"/> for this builder.</summary>
        /// <returns>The <see cref="System.Threading.Tasks.Task{TResult}"/> representing the builder's asynchronous operation.</returns>
        public Task<TResult> Task
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
        private Task<TResult> InitializeTaskAsPromise()
        {
            Debug.Assert(m_task == null);
            return m_task = new Task<TResult>();
        }

        internal static Task<TResult> CreateWeaklyTypedStateMachineBox()
        {
#if CORERT
            // DebugFinalizableAsyncStateMachineBox looks like a small type, but it actually is not because
            // it will have a copy of all the slots from its parent. It will add another hundred(s) bytes
            // per each async method in CoreRT / ProjectN binaries without adding much value. Avoid
            // generating this extra code until a better solution is implemented.
            return new AsyncStateMachineBox<IAsyncStateMachine>();
#else
            return AsyncMethodBuilderCore.TrackAsyncMethodCompletion ?
                CreateDebugFinalizableAsyncStateMachineBox<IAsyncStateMachine>() :
                new AsyncStateMachineBox<IAsyncStateMachine>();
#endif
        }

        /// <summary>
        /// Completes the <see cref="System.Threading.Tasks.Task{TResult}"/> in the
        /// <see cref="System.Threading.Tasks.TaskStatus">RanToCompletion</see> state with the specified result.
        /// </summary>
        /// <param name="result">The result to use to complete the task.</param>
        /// <exception cref="System.InvalidOperationException">The task has already completed.</exception>
        public void SetResult(TResult result)
        {
            // Get the currently stored task, which will be non-null if get_Task has already been accessed.
            // If there isn't one, get a task and store it.
            if (m_task is null)
            {
                m_task = Threading.Tasks.Task.FromResult(result);
            }
            else
            {
                // Slow path: complete the existing task.
                SetExistingTaskResult(m_task, result);
            }
        }

        /// <summary>Completes the already initialized task with the specified result.</summary>
        /// <param name="result">The result to use to complete the task.</param>
        /// <param name="task">The task to complete.</param>
        internal static void SetExistingTaskResult(Task<TResult> task, TResult? result)
        {
            Debug.Assert(task != null, "Expected non-null task");

            if (TplEventSource.Log.IsEnabled())
            {
                TplEventSource.Log.TraceOperationEnd(task.Id, AsyncCausalityStatus.Completed);
            }

            if (!task.TrySetResult(result))
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.TaskT_TransitionToFinal_AlreadyCompleted);
            }
        }

        /// <summary>
        /// Completes the <see cref="System.Threading.Tasks.Task{TResult}"/> in the
        /// <see cref="System.Threading.Tasks.TaskStatus">Faulted</see> state with the specified exception.
        /// </summary>
        /// <param name="exception">The <see cref="System.Exception"/> to use to fault the task.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="exception"/> argument is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.InvalidOperationException">The task has already completed.</exception>
        public void SetException(Exception exception) => SetException(exception, ref m_task);

        internal static void SetException(Exception exception, ref Task<TResult>? taskField)
        {
            if (exception == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.exception);
            }

            // Get the task, forcing initialization if it hasn't already been initialized.
            Task<TResult> task = (taskField ??= new Task<TResult>());

            // If the exception represents cancellation, cancel the task.  Otherwise, fault the task.
            bool successfullySet = exception is OperationCanceledException oce ?
                task.TrySetCanceled(oce.CancellationToken, oce) :
                task.TrySetException(exception);

            // Unlike with TaskCompletionSource, we do not need to spin here until _taskAndStateMachine is completed,
            // since AsyncTaskMethodBuilder.SetException should not be immediately followed by any code
            // that depends on the task having completely completed.  Moreover, with correct usage,
            // SetResult or SetException should only be called once, so the Try* methods should always
            // return true, so no spinning would be necessary anyway (the spinning in TCS is only relevant
            // if another thread completes the task first).
            if (!successfullySet)
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.TaskT_TransitionToFinal_AlreadyCompleted);
            }
        }

        /// <summary>
        /// Called by the debugger to request notification when the first wait operation
        /// (await, Wait, Result, etc.) on this builder's task completes.
        /// </summary>
        /// <param name="enabled">
        /// true to enable notification; false to disable a previously set notification.
        /// </param>
        /// <remarks>
        /// This should only be invoked from within an asynchronous method,
        /// and only by the debugger.
        /// </remarks>
        internal void SetNotificationForWaitCompletion(bool enabled) =>
            SetNotificationForWaitCompletion(enabled, ref m_task);

        internal static void SetNotificationForWaitCompletion(bool enabled, [NotNull] ref Task<TResult>? taskField)
        {
            // Get the task (forcing initialization if not already initialized), and set debug notification
            (taskField ??= CreateWeaklyTypedStateMachineBox()).SetNotificationForWaitCompletion(enabled);

            // NOTE: It's important that the debugger use builder.SetNotificationForWaitCompletion
            // rather than builder.Task.SetNotificationForWaitCompletion.  Even though the latter will
            // lazily-initialize the task as well, it'll initialize it to a Task<T> (which is important
            // to minimize size for cases where an ATMB is used directly by user code to avoid the
            // allocation overhead of a TaskCompletionSource).  If that's done prior to the first await,
            // the GetMoveNextDelegate code, which needs an AsyncStateMachineBox, will end up creating
            // a new box and overwriting the previously created task.  That'll change the object identity
            // of the task being used for wait completion notification, and no notification will
            // ever arrive, breaking step-out behavior when stepping out before the first yielding await.
        }

        /// <summary>
        /// Gets an object that may be used to uniquely identify this builder to the debugger.
        /// </summary>
        /// <remarks>
        /// This property lazily instantiates the ID in a non-thread-safe manner.
        /// It must only be used by the debugger and tracing purposes, and only in a single-threaded manner
        /// when no other threads are in the middle of accessing this or other members that lazily initialize the task.
        /// </remarks>
        internal object ObjectIdForDebugger => m_task ??= CreateWeaklyTypedStateMachineBox();
    }
}
