// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
//
//
// Compiler-targeted types that build tasks for use as the return types of asynchronous methods.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
#if FEATURE_COMINTEROP
using System.Runtime.InteropServices.WindowsRuntime;
#endif // FEATURE_COMINTEROP

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Provides a builder for asynchronous methods that return void.
    /// This type is intended for compiler use only.
    /// </summary>
    public struct AsyncVoidMethodBuilder
    {
        /// <summary>The synchronization context associated with this operation.</summary>
        private SynchronizationContext _synchronizationContext;
        /// <summary>The builder this void builder wraps.</summary>
        private AsyncTaskMethodBuilder _builder; // mutable struct: must not be readonly

        /// <summary>Initializes a new <see cref="AsyncVoidMethodBuilder"/>.</summary>
        /// <returns>The initialized <see cref="AsyncVoidMethodBuilder"/>.</returns>
        public static AsyncVoidMethodBuilder Create()
        {
            SynchronizationContext sc = SynchronizationContext.CurrentNoFlow;
            sc?.OperationStarted();
            return new AsyncVoidMethodBuilder() { _synchronizationContext = sc };
        }

        /// <summary>Initiates the builder's execution with the associated state machine.</summary>
        /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
        /// <param name="stateMachine">The state machine instance, passed by reference.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="stateMachine"/> argument was null (Nothing in Visual Basic).</exception>
        [DebuggerStepThrough]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine =>
            _builder.Start(ref stateMachine);

        /// <summary>Associates the builder with the state machine it represents.</summary>
        /// <param name="stateMachine">The heap-allocated state machine object.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="stateMachine"/> argument was null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.InvalidOperationException">The builder is incorrectly initialized.</exception>
        public void SetStateMachine(IAsyncStateMachine stateMachine) =>
            _builder.SetStateMachine(stateMachine);

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
            _builder.AwaitOnCompleted(ref awaiter, ref stateMachine);

        /// <summary>
        /// Schedules the specified state machine to be pushed forward when the specified awaiter completes.
        /// </summary>
        /// <typeparam name="TAwaiter">Specifies the type of the awaiter.</typeparam>
        /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
        /// <param name="awaiter">The awaiter.</param>
        /// <param name="stateMachine">The state machine.</param>
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine =>
            _builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);

        /// <summary>Completes the method builder successfully.</summary>
        public void SetResult()
        {
            if (AsyncCausalityTracer.LoggingOn)
            {
                AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, this.Task.Id, AsyncCausalityStatus.Completed);
            }

            if (_synchronizationContext != null)
            {
                NotifySynchronizationContextOfCompletion();
            }

            // No need to call _builder.SetResult, as no one pays attention to the task's completion.
        }

        /// <summary>Faults the method builder with an exception.</summary>
        /// <param name="exception">The exception that is the cause of this fault.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="exception"/> argument is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.InvalidOperationException">The builder is not initialized.</exception>
        public void SetException(Exception exception)
        {
            if (exception == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.exception);
            }

            if (AsyncCausalityTracer.LoggingOn)
            {
                AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, this.Task.Id, AsyncCausalityStatus.Error);
            }

            if (_synchronizationContext != null)
            {
                // If we captured a synchronization context, Post the throwing of the exception to it 
                // and decrement its outstanding operation count.
                try
                {
                    AsyncMethodBuilderCore.ThrowAsync(exception, targetContext: _synchronizationContext);
                }
                finally
                {
                    NotifySynchronizationContextOfCompletion();
                }
            }
            else
            {
                // Otherwise, queue the exception to be thrown on the ThreadPool.  This will
                // result in a crash unless legacy exception behavior is enabled by a config
                // file or a CLR host.
                AsyncMethodBuilderCore.ThrowAsync(exception, targetContext: null);
            }

            // No need to call _builder.SetException, as no one pays attention to the task's completion.
        }

        /// <summary>Notifies the current synchronization context that the operation completed.</summary>
        private void NotifySynchronizationContextOfCompletion()
        {
            Debug.Assert(_synchronizationContext != null, "Must only be used with a non-null context.");
            try
            {
                _synchronizationContext.OperationCompleted();
            }
            catch (Exception exc)
            {
                // If the interaction with the SynchronizationContext goes awry,
                // fall back to propagating on the ThreadPool.
                AsyncMethodBuilderCore.ThrowAsync(exc, targetContext: null);
            }
        }

        /// <summary>Lazily instantiate the Task in a non-thread-safe manner.</summary>
        private Task Task => _builder.Task;

        /// <summary>
        /// Gets an object that may be used to uniquely identify this builder to the debugger.
        /// </summary>
        /// <remarks>
        /// This property lazily instantiates the ID in a non-thread-safe manner.  
        /// It must only be used by the debugger and AsyncCausalityTracer in a single-threaded manner.
        /// </remarks>
        internal object ObjectIdForDebugger => _builder.ObjectIdForDebugger;
    }

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
        /// <summary>A cached VoidTaskResult task used for builders that complete synchronously.</summary>
        private readonly static Task<VoidTaskResult> s_cachedCompleted = AsyncTaskMethodBuilder<VoidTaskResult>.s_defaultResultTask;

        /// <summary>The generic builder object to which this non-generic instance delegates.</summary>
        private AsyncTaskMethodBuilder<VoidTaskResult> m_builder; // mutable struct: must not be readonly. Debugger depends on the exact name of this field.

        /// <summary>Initializes a new <see cref="AsyncTaskMethodBuilder"/>.</summary>
        /// <returns>The initialized <see cref="AsyncTaskMethodBuilder"/>.</returns>
        public static AsyncTaskMethodBuilder Create() => default(AsyncTaskMethodBuilder);

        /// <summary>Initiates the builder's execution with the associated state machine.</summary>
        /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
        /// <param name="stateMachine">The state machine instance, passed by reference.</param>
        [DebuggerStepThrough]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine =>
            m_builder.Start(ref stateMachine);

        /// <summary>Associates the builder with the state machine it represents.</summary>
        /// <param name="stateMachine">The heap-allocated state machine object.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="stateMachine"/> argument was null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.InvalidOperationException">The builder is incorrectly initialized.</exception>
        public void SetStateMachine(IAsyncStateMachine stateMachine) =>
            m_builder.SetStateMachine(stateMachine);

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
            m_builder.AwaitOnCompleted(ref awaiter, ref stateMachine);

        /// <summary>
        /// Schedules the specified state machine to be pushed forward when the specified awaiter completes.
        /// </summary>
        /// <typeparam name="TAwaiter">Specifies the type of the awaiter.</typeparam>
        /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
        /// <param name="awaiter">The awaiter.</param>
        /// <param name="stateMachine">The state machine.</param>
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine =>
            m_builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);

        /// <summary>Gets the <see cref="System.Threading.Tasks.Task"/> for this builder.</summary>
        /// <returns>The <see cref="System.Threading.Tasks.Task"/> representing the builder's asynchronous operation.</returns>
        /// <exception cref="System.InvalidOperationException">The builder is not initialized.</exception>
        public Task Task
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_builder.Task;
        }

        /// <summary>
        /// Completes the <see cref="System.Threading.Tasks.Task"/> in the 
        /// <see cref="System.Threading.Tasks.TaskStatus">RanToCompletion</see> state.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">The builder is not initialized.</exception>
        /// <exception cref="System.InvalidOperationException">The task has already completed.</exception>
        public void SetResult() => m_builder.SetResult(s_cachedCompleted); // Using s_cachedCompleted is faster than using s_defaultResultTask.

        /// <summary>
        /// Completes the <see cref="System.Threading.Tasks.Task"/> in the 
        /// <see cref="System.Threading.Tasks.TaskStatus">Faulted</see> state with the specified exception.
        /// </summary>
        /// <param name="exception">The <see cref="System.Exception"/> to use to fault the task.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="exception"/> argument is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.InvalidOperationException">The builder is not initialized.</exception>
        /// <exception cref="System.InvalidOperationException">The task has already completed.</exception>
        public void SetException(Exception exception) => m_builder.SetException(exception);

        /// <summary>
        /// Called by the debugger to request notification when the first wait operation
        /// (await, Wait, Result, etc.) on this builder's task completes.
        /// </summary>
        /// <param name="enabled">
        /// true to enable notification; false to disable a previously set notification.
        /// </param>
        internal void SetNotificationForWaitCompletion(bool enabled) => m_builder.SetNotificationForWaitCompletion(enabled);

        /// <summary>
        /// Gets an object that may be used to uniquely identify this builder to the debugger.
        /// </summary>
        /// <remarks>
        /// This property lazily instantiates the ID in a non-thread-safe manner.  
        /// It must only be used by the debugger and tracing purposes, and only in a single-threaded manner
        /// when no other threads are in the middle of accessing this property or this.Task.
        /// </remarks>
        internal object ObjectIdForDebugger => m_builder.ObjectIdForDebugger;
    }

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
        /// <summary>A cached task for default(TResult).</summary>
        internal readonly static Task<TResult> s_defaultResultTask = AsyncTaskCache.CreateCacheableTask(default(TResult));

        /// <summary>The lazily-initialized built task.</summary>
        private Task<TResult> m_task; // lazily-initialized: must not be readonly. Debugger depends on the exact name of this field.

        /// <summary>Initializes a new <see cref="AsyncTaskMethodBuilder"/>.</summary>
        /// <returns>The initialized <see cref="AsyncTaskMethodBuilder"/>.</returns>
        public static AsyncTaskMethodBuilder<TResult> Create()
        {
            return default(AsyncTaskMethodBuilder<TResult>);
            // NOTE: If this method is ever updated to perform more initialization,
            //       other Create methods like AsyncTaskMethodBuilder.Create and
            //       AsyncValueTaskMethodBuilder.Create must be updated to call this.
        }

        /// <summary>Initiates the builder's execution with the associated state machine.</summary>
        /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
        /// <param name="stateMachine">The state machine instance, passed by reference.</param>
        [DebuggerStepThrough]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            if (stateMachine == null) // TStateMachines are generally non-nullable value types, so this check will be elided
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stateMachine);
            }

            // Run the MoveNext method within a copy-on-write ExecutionContext scope.
            // This allows us to undo any ExecutionContext changes made in MoveNext,
            // so that they won't "leak" out of the first await.

            Thread currentThread = Thread.CurrentThread;
            ExecutionContextSwitcher ecs = default(ExecutionContextSwitcher);
            try
            {
                ExecutionContext.EstablishCopyOnWriteScope(currentThread, ref ecs);
                stateMachine.MoveNext();
            }
            finally
            {
                ecs.Undo(currentThread);
            }
        }

        /// <summary>Associates the builder with the state machine it represents.</summary>
        /// <param name="stateMachine">The heap-allocated state machine object.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="stateMachine"/> argument was null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.InvalidOperationException">The builder is incorrectly initialized.</exception>
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            if (stateMachine == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stateMachine);
            }

            if (m_task != null)
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.AsyncMethodBuilder_InstanceNotInitialized);
            }

            // SetStateMachine was originally needed in order to store the boxed state machine reference into
            // the boxed copy.  Now that a normal box is no longer used, SetStateMachine is also legacy.  We need not
            // do anything here, and thus assert to ensure we're not calling this from our own implementations.
            Debug.Fail("SetStateMachine should not be used.");
        }

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
            where TStateMachine : IAsyncStateMachine
        {
            try
            {
                awaiter.OnCompleted(GetStateMachineBox(ref stateMachine).MoveNextAction);
            }
            catch (Exception e)
            {
                AsyncMethodBuilderCore.ThrowAsync(e, targetContext: null);
            }
        }

        /// <summary>
        /// Schedules the specified state machine to be pushed forward when the specified awaiter completes.
        /// </summary>
        /// <typeparam name="TAwaiter">Specifies the type of the awaiter.</typeparam>
        /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
        /// <param name="awaiter">The awaiter.</param>
        /// <param name="stateMachine">The state machine.</param>
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            IAsyncStateMachineBox box = GetStateMachineBox(ref stateMachine);

            // TODO https://github.com/dotnet/coreclr/issues/12877:
            // Once the JIT is able to recognize "awaiter is ITaskAwaiter" and "awaiter is IConfiguredTaskAwaiter",
            // use those in order to a) consolidate a lot of this code, and b) handle all Task/Task<T> and not just
            // the few types special-cased here.  For now, handle common {Configured}TaskAwaiter.  Having the types
            // explicitly listed here allows the JIT to generate the best code for them; otherwise we'll fall through
            // to the later workaround.
            if (typeof(TAwaiter) == typeof(TaskAwaiter) ||
                typeof(TAwaiter) == typeof(TaskAwaiter<object>) ||
                typeof(TAwaiter) == typeof(TaskAwaiter<string>) ||
                typeof(TAwaiter) == typeof(TaskAwaiter<byte[]>) ||
                typeof(TAwaiter) == typeof(TaskAwaiter<bool>) ||
                typeof(TAwaiter) == typeof(TaskAwaiter<byte>) ||
                typeof(TAwaiter) == typeof(TaskAwaiter<int>) ||
                typeof(TAwaiter) == typeof(TaskAwaiter<long>))
            {
                ref TaskAwaiter ta = ref Unsafe.As<TAwaiter, TaskAwaiter>(ref awaiter); // relies on TaskAwaiter/TaskAwaiter<T> having the same layout
                TaskAwaiter.UnsafeOnCompletedInternal(ta.m_task, box, continueOnCapturedContext: true);
            }
            else if (
                typeof(TAwaiter) == typeof(ConfiguredTaskAwaitable.ConfiguredTaskAwaiter) ||
                typeof(TAwaiter) == typeof(ConfiguredTaskAwaitable<object>.ConfiguredTaskAwaiter) ||
                typeof(TAwaiter) == typeof(ConfiguredTaskAwaitable<string>.ConfiguredTaskAwaiter) ||
                typeof(TAwaiter) == typeof(ConfiguredTaskAwaitable<byte[]>.ConfiguredTaskAwaiter) ||
                typeof(TAwaiter) == typeof(ConfiguredTaskAwaitable<bool>.ConfiguredTaskAwaiter) ||
                typeof(TAwaiter) == typeof(ConfiguredTaskAwaitable<byte>.ConfiguredTaskAwaiter) ||
                typeof(TAwaiter) == typeof(ConfiguredTaskAwaitable<int>.ConfiguredTaskAwaiter) ||
                typeof(TAwaiter) == typeof(ConfiguredTaskAwaitable<long>.ConfiguredTaskAwaiter))
            {
                ref ConfiguredTaskAwaitable.ConfiguredTaskAwaiter ta = ref Unsafe.As<TAwaiter, ConfiguredTaskAwaitable.ConfiguredTaskAwaiter>(ref awaiter);
                TaskAwaiter.UnsafeOnCompletedInternal(ta.m_task, box, ta.m_continueOnCapturedContext);
            }

            // Handle common {Configured}ValueTaskAwaiter<T> types.  Unfortunately these need to be special-cased
            // individually, as we don't have good way to extract the task from a ValueTaskAwaiter<T> when we don't
            // know what the T is; we could make ValueTaskAwaiter<T> implement an IValueTaskAwaiter interface, but
            // calling a GetTask method on that would end up boxing the awaiter.  This hard-coded list here is
            // somewhat arbitrary and is based on types currently in use with ValueTask<T> in coreclr/corefx.
            else if (typeof(TAwaiter) == typeof(ValueTaskAwaiter<int>))
            {
                var vta = (ValueTaskAwaiter<int>)(object)awaiter;
                TaskAwaiter.UnsafeOnCompletedInternal(vta.AsTask(), box, continueOnCapturedContext: true);
            }
            else if (typeof(TAwaiter) == typeof(ConfiguredValueTaskAwaitable<int>.ConfiguredValueTaskAwaiter))
            {
                var vta = (ConfiguredValueTaskAwaitable<int>.ConfiguredValueTaskAwaiter)(object)awaiter;
                TaskAwaiter.UnsafeOnCompletedInternal(vta.AsTask(), box, vta._continueOnCapturedContext);
            }
            else if (typeof(TAwaiter) == typeof(ConfiguredValueTaskAwaitable<System.IO.Stream>.ConfiguredValueTaskAwaiter))
            {
                var vta = (ConfiguredValueTaskAwaitable<System.IO.Stream>.ConfiguredValueTaskAwaiter)(object)awaiter;
                TaskAwaiter.UnsafeOnCompletedInternal(vta.AsTask(), box, vta._continueOnCapturedContext);
            }
            else if (typeof(TAwaiter) == typeof(ConfiguredValueTaskAwaitable<ArraySegment<byte>>.ConfiguredValueTaskAwaiter))
            {
                var vta = (ConfiguredValueTaskAwaitable<ArraySegment<byte>>.ConfiguredValueTaskAwaiter)(object)awaiter;
                TaskAwaiter.UnsafeOnCompletedInternal(vta.AsTask(), box, vta._continueOnCapturedContext);
            }
            else if (typeof(TAwaiter) == typeof(ConfiguredValueTaskAwaitable<object>.ConfiguredValueTaskAwaiter))
            {
                var vta = (ConfiguredValueTaskAwaitable<object>.ConfiguredValueTaskAwaiter)(object)awaiter;
                TaskAwaiter.UnsafeOnCompletedInternal(vta.AsTask(), box, vta._continueOnCapturedContext);
            }

            // To catch all Task/Task<T> awaits, do the currently more expensive interface checks.
            // Eventually these and the above Task/Task<T> checks should be replaced by "is" checks,
            // once that's recognized and optimized by the JIT.  We do these after all of the hardcoded
            // checks above so that they don't incur the costs of these checks.
            else if (InterfaceIsCheckWorkaround<TAwaiter>.IsITaskAwaiter)
            {
                ref TaskAwaiter ta = ref Unsafe.As<TAwaiter, TaskAwaiter>(ref awaiter);
                TaskAwaiter.UnsafeOnCompletedInternal(ta.m_task, box, continueOnCapturedContext: true);
            }
            else if (InterfaceIsCheckWorkaround<TAwaiter>.IsIConfiguredTaskAwaiter)
            {
                ref ConfiguredTaskAwaitable.ConfiguredTaskAwaiter ta = ref Unsafe.As<TAwaiter, ConfiguredTaskAwaitable.ConfiguredTaskAwaiter>(ref awaiter);
                TaskAwaiter.UnsafeOnCompletedInternal(ta.m_task, box, ta.m_continueOnCapturedContext);
            }

            // The awaiter isn't specially known. Fall back to doing a normal await.
            else
            {
                // TODO https://github.com/dotnet/coreclr/issues/14177:
                // Move the code back into this method once the JIT is able to
                // elide it successfully when one of the previous branches is hit.
                AwaitArbitraryAwaiterUnsafeOnCompleted(ref awaiter, box);
            }
        }

        /// <summary>Schedules the specified state machine to be pushed forward when the specified awaiter completes.</summary>
        /// <typeparam name="TAwaiter">Specifies the type of the awaiter.</typeparam>
        /// <param name="awaiter">The awaiter.</param>
        /// <param name="box">The state machine box.</param>
        private static void AwaitArbitraryAwaiterUnsafeOnCompleted<TAwaiter>(ref TAwaiter awaiter, IAsyncStateMachineBox box)
            where TAwaiter : ICriticalNotifyCompletion
        {
            try
            {
                awaiter.UnsafeOnCompleted(box.MoveNextAction);
            }
            catch (Exception e)
            {
                AsyncMethodBuilderCore.ThrowAsync(e, targetContext: null);
            }
        }

        /// <summary>Gets the "boxed" state machine object.</summary>
        /// <typeparam name="TStateMachine">Specifies the type of the async state machine.</typeparam>
        /// <param name="stateMachine">The state machine.</param>
        /// <returns>The "boxed" state machine.</returns>
        private IAsyncStateMachineBox GetStateMachineBox<TStateMachine>(
            ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            ExecutionContext currentContext = ExecutionContext.Capture();

            // Check first for the most common case: not the first yield in an async method.
            // In this case, the first yield will have already "boxed" the state machine in
            // a strongly-typed manner into an AsyncStateMachineBox.  It will already contain
            // the state machine as well as a MoveNextDelegate and a context.  The only thing
            // we might need to do is update the context if that's changed since it was stored.
            if (m_task is AsyncStateMachineBox<TStateMachine> stronglyTypedBox)
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
            if (m_task is AsyncStateMachineBox<IAsyncStateMachine> weaklyTypedBox)
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

            // At this point, m_task should really be null, in which case we want to create the box.
            // However, in a variety of debugger-related (erroneous) situations, it might be non-null,
            // e.g. if the Task property is examined in a Watch window, forcing it to be lazily-intialized
            // as a Task<TResult> rather than as an AsyncStateMachineBox.  The worst that happens in such
            // cases is we lose the ability to properly step in the debugger, as the debugger uses that
            // object's identity to track this specific builder/state machine.  As such, we proceed to
            // overwrite whatever's there anyway, even if it's non-null.
            var box = new AsyncStateMachineBox<TStateMachine>();
            m_task = box; // important: this must be done before storing stateMachine into box.StateMachine!
            box.StateMachine = stateMachine;
            box.Context = currentContext;
            return box;
        }

        /// <summary>A strongly-typed box for Task-based async state machines.</summary>
        /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
        /// <typeparam name="TResult">Specifies the type of the Task's result.</typeparam>
        private sealed class AsyncStateMachineBox<TStateMachine> :
            Task<TResult>, IAsyncStateMachineBox
            where TStateMachine : IAsyncStateMachine
        {
            /// <summary>Delegate used to invoke on an ExecutionContext when passed an instance of this box type.</summary>
            private static readonly ContextCallback s_callback = s => ((AsyncStateMachineBox<TStateMachine>)s).StateMachine.MoveNext();

            /// <summary>A delegate to the <see cref="MoveNext"/> method.</summary>
            private Action _moveNextAction;
            /// <summary>The state machine itself.</summary>
            public TStateMachine StateMachine; // mutable struct; do not make this readonly
            /// <summary>Captured ExecutionContext with which to invoke <see cref="MoveNextAction"/>; may be null.</summary>
            public ExecutionContext Context;

            /// <summary>A delegate to the <see cref="MoveNext"/> method.</summary>
            public Action MoveNextAction =>
                _moveNextAction ??
                (_moveNextAction = AsyncCausalityTracer.LoggingOn ? AsyncMethodBuilderCore.OutputAsyncCausalityEvents(this, new Action(MoveNext)) : new Action(MoveNext));

            /// <summary>Calls MoveNext on <see cref="StateMachine"/></summary>
            public void MoveNext()
            {
                if (Context == null)
                {
                    StateMachine.MoveNext();
                }
                else
                {
                    ExecutionContext.Run(Context, s_callback, this);
                }
            }

            /// <summary>
            /// Calls MoveNext on <see cref="StateMachine"/>. Implements ITaskCompletionAction.Invoke so
            /// that the state machine object may be queued directly as a continuation into a Task's
            /// continuation slot/list.
            /// </summary>
            /// <param name="completedTask">The completing task that caused this method to be invoked, if there was one.</param>
            void ITaskCompletionAction.Invoke(Task completedTask) => MoveNext();

            /// <summary>Signals to Task's continuation logic that <see cref="Invoke"/> runs arbitrary user code via MoveNext.</summary>
            bool ITaskCompletionAction.InvokeMayRunArbitraryCode => true;

            /// <summary>Gets the state machine as a boxed object.  This should only be used for debugging purposes.</summary>
            IAsyncStateMachine IAsyncStateMachineBox.GetStateMachineObject() => StateMachine; // likely boxes, only use for debugging
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
            return (m_task = new Task<TResult>());
        }

        /// <summary>
        /// Initializes the task, which must not yet be initialized.  Used only when the Task is being forced into
        /// existence due to the debugger trying to enable step-out/step-over/etc. prior to the first await yielding
        /// in an async method.  In that case, we don't know the actual TStateMachine type, so we're forced to
        /// use IAsyncStateMachine instead.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private Task<TResult> InitializeTaskAsStateMachineBox()
        {
            Debug.Assert(m_task == null);
            return (m_task = new AsyncStateMachineBox<IAsyncStateMachine>());
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
            if (m_task == null)
            {
                m_task = GetTaskForResult(result);
                Debug.Assert(m_task != null, $"{nameof(GetTaskForResult)} should never return null");
            }
            else
            {
                // Slow path: complete the existing task.
                SetExistingTaskResult(result);
            }
        }

        /// <summary>Completes the already initialized task with the specified result.</summary>
        /// <param name="result">The result to use to complete the task.</param>
        private void SetExistingTaskResult(TResult result)
        {
            Debug.Assert(m_task != null, "Expected non-null task");

            if (AsyncCausalityTracer.LoggingOn || System.Threading.Tasks.Task.s_asyncDebuggingEnabled)
            {
                LogExistingTaskCompletion();
            }

            if (!m_task.TrySetResult(result))
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.TaskT_TransitionToFinal_AlreadyCompleted);
            }
        }

        /// <summary>Handles logging for the successful completion of an operation.</summary>
        private void LogExistingTaskCompletion()
        {
            Debug.Assert(m_task != null);

            if (AsyncCausalityTracer.LoggingOn)
            {
                AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, m_task.Id, AsyncCausalityStatus.Completed);
            }

            // only log if we have a real task that was previously created
            if (System.Threading.Tasks.Task.s_asyncDebuggingEnabled)
            {
                System.Threading.Tasks.Task.RemoveFromActiveTasks(m_task.Id);
            }
        }

        /// <summary>
        /// Completes the builder by using either the supplied completed task, or by completing
        /// the builder's previously accessed task using default(TResult).
        /// </summary>
        /// <param name="completedTask">A task already completed with the value default(TResult).</param>
        /// <exception cref="System.InvalidOperationException">The task has already completed.</exception>
        internal void SetResult(Task<TResult> completedTask)
        {
            Debug.Assert(completedTask != null, "Expected non-null task");
            Debug.Assert(completedTask.IsCompletedSuccessfully, "Expected a successfully completed task");

            // Get the currently stored task, which will be non-null if get_Task has already been accessed.
            // If there isn't one, store the supplied completed task.
            if (m_task == null)
            {
                m_task = completedTask;
            }
            else
            {
                // Otherwise, complete the task that's there.
                SetExistingTaskResult(default(TResult));
            }
        }

        /// <summary>
        /// Completes the <see cref="System.Threading.Tasks.Task{TResult}"/> in the 
        /// <see cref="System.Threading.Tasks.TaskStatus">Faulted</see> state with the specified exception.
        /// </summary>
        /// <param name="exception">The <see cref="System.Exception"/> to use to fault the task.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="exception"/> argument is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.InvalidOperationException">The task has already completed.</exception>
        public void SetException(Exception exception)
        {
            if (exception == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.exception);
            }

            // Get the task, forcing initialization if it hasn't already been initialized.
            Task<TResult> task = this.Task;

            // If the exception represents cancellation, cancel the task.  Otherwise, fault the task.
            var oce = exception as OperationCanceledException;
            bool successfullySet = oce != null ?
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
        internal void SetNotificationForWaitCompletion(bool enabled)
        {
            // Get the task (forcing initialization if not already initialized), and set debug notification
            (m_task ?? InitializeTaskAsStateMachineBox()).SetNotificationForWaitCompletion(enabled);

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
        internal object ObjectIdForDebugger => m_task ?? InitializeTaskAsStateMachineBox();

        /// <summary>
        /// Gets a task for the specified result.  This will either
        /// be a cached or new task, never null.
        /// </summary>
        /// <param name="result">The result for which we need a task.</param>
        /// <returns>The completed task containing the result.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // method looks long, but for a given TResult it results in a relatively small amount of asm
        internal static Task<TResult> GetTaskForResult(TResult result)
        {
            // The goal of this function is to be give back a cached task if possible,
            // or to otherwise give back a new task.  To give back a cached task,
            // we need to be able to evaluate the incoming result value, and we need
            // to avoid as much overhead as possible when doing so, as this function
            // is invoked as part of the return path from every async method.
            // Most tasks won't be cached, and thus we need the checks for those that are 
            // to be as close to free as possible. This requires some trickiness given the 
            // lack of generic specialization in .NET.
            //
            // Be very careful when modifying this code.  It has been tuned
            // to comply with patterns recognized by both 32-bit and 64-bit JITs.
            // If changes are made here, be sure to look at the generated assembly, as
            // small tweaks can have big consequences for what does and doesn't get optimized away.
            //
            // Note that this code only ever accesses a static field when it knows it'll
            // find a cached value, since static fields (even if readonly and integral types) 
            // require special access helpers in this NGEN'd and domain-neutral.

            if (null != (object)default(TResult)) // help the JIT avoid the value type branches for ref types
            {
                // Special case simple value types:
                // - Boolean
                // - Byte, SByte
                // - Char
                // - Decimal
                // - Int32, UInt32
                // - Int64, UInt64
                // - Int16, UInt16
                // - IntPtr, UIntPtr
                // As of .NET 4.5, the (Type)(object)result pattern used below
                // is recognized and optimized by both 32-bit and 64-bit JITs.

                // For Boolean, we cache all possible values.
                if (typeof(TResult) == typeof(Boolean)) // only the relevant branches are kept for each value-type generic instantiation
                {
                    Boolean value = (Boolean)(object)result;
                    Task<Boolean> task = value ? AsyncTaskCache.TrueTask : AsyncTaskCache.FalseTask;
                    return Unsafe.As<Task<TResult>>(task); // UnsafeCast avoids type check we know will succeed
                }
                // For Int32, we cache a range of common values, e.g. [-1,4).
                else if (typeof(TResult) == typeof(Int32))
                {
                    // Compare to constants to avoid static field access if outside of cached range.
                    // We compare to the upper bound first, as we're more likely to cache miss on the upper side than on the 
                    // lower side, due to positive values being more common than negative as return values.
                    Int32 value = (Int32)(object)result;
                    if (value < AsyncTaskCache.EXCLUSIVE_INT32_MAX &&
                        value >= AsyncTaskCache.INCLUSIVE_INT32_MIN)
                    {
                        Task<Int32> task = AsyncTaskCache.Int32Tasks[value - AsyncTaskCache.INCLUSIVE_INT32_MIN];
                        return Unsafe.As<Task<TResult>>(task); // UnsafeCast avoids a type check we know will succeed
                    }
                }
                // For other known value types, we only special-case 0 / default(TResult).
                else if (
                    (typeof(TResult) == typeof(UInt32) && default(UInt32) == (UInt32)(object)result) ||
                    (typeof(TResult) == typeof(Byte) && default(Byte) == (Byte)(object)result) ||
                    (typeof(TResult) == typeof(SByte) && default(SByte) == (SByte)(object)result) ||
                    (typeof(TResult) == typeof(Char) && default(Char) == (Char)(object)result) ||
                    (typeof(TResult) == typeof(Decimal) && default(Decimal) == (Decimal)(object)result) ||
                    (typeof(TResult) == typeof(Int64) && default(Int64) == (Int64)(object)result) ||
                    (typeof(TResult) == typeof(UInt64) && default(UInt64) == (UInt64)(object)result) ||
                    (typeof(TResult) == typeof(Int16) && default(Int16) == (Int16)(object)result) ||
                    (typeof(TResult) == typeof(UInt16) && default(UInt16) == (UInt16)(object)result) ||
                    (typeof(TResult) == typeof(IntPtr) && default(IntPtr) == (IntPtr)(object)result) ||
                    (typeof(TResult) == typeof(UIntPtr) && default(UIntPtr) == (UIntPtr)(object)result))
                {
                    return s_defaultResultTask;
                }
            }
            else if (result == null) // optimized away for value types
            {
                return s_defaultResultTask;
            }

            // No cached task is available.  Manufacture a new one for this result.
            return new Task<TResult>(result);
        }
    }

    /// <summary>Provides a cache of closed generic tasks for async methods.</summary>
    internal static class AsyncTaskCache
    {
        // All static members are initialized inline to ensure type is beforefieldinit

        /// <summary>A cached Task{Boolean}.Result == true.</summary>
        internal readonly static Task<Boolean> TrueTask = CreateCacheableTask(true);
        /// <summary>A cached Task{Boolean}.Result == false.</summary>
        internal readonly static Task<Boolean> FalseTask = CreateCacheableTask(false);

        /// <summary>The cache of Task{Int32}.</summary>
        internal readonly static Task<Int32>[] Int32Tasks = CreateInt32Tasks();
        /// <summary>The minimum value, inclusive, for which we want a cached task.</summary>
        internal const Int32 INCLUSIVE_INT32_MIN = -1;
        /// <summary>The maximum value, exclusive, for which we want a cached task.</summary>
        internal const Int32 EXCLUSIVE_INT32_MAX = 9;
        /// <summary>Creates an array of cached tasks for the values in the range [INCLUSIVE_MIN,EXCLUSIVE_MAX).</summary>
        private static Task<Int32>[] CreateInt32Tasks()
        {
            Debug.Assert(EXCLUSIVE_INT32_MAX >= INCLUSIVE_INT32_MIN, "Expected max to be at least min");
            var tasks = new Task<Int32>[EXCLUSIVE_INT32_MAX - INCLUSIVE_INT32_MIN];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = CreateCacheableTask(i + INCLUSIVE_INT32_MIN);
            }
            return tasks;
        }

        /// <summary>Creates a non-disposable task.</summary>
        /// <typeparam name="TResult">Specifies the result type.</typeparam>
        /// <param name="result">The result for the task.</param>
        /// <returns>The cacheable task.</returns>
        internal static Task<TResult> CreateCacheableTask<TResult>(TResult result) =>
            new Task<TResult>(false, result, (TaskCreationOptions)InternalTaskOptions.DoNotDispose, default(CancellationToken));
    }

    /// <summary>Temporary workaround for https://github.com/dotnet/coreclr/issues/12877.</summary>
    internal static class InterfaceIsCheckWorkaround<TAwaiter>
    {
        internal static readonly bool IsITaskAwaiter = typeof(TAwaiter).GetInterface("ITaskAwaiter") != null;
        internal static readonly bool IsIConfiguredTaskAwaiter = typeof(TAwaiter).GetInterface("IConfiguredTaskAwaiter") != null;
    }

    /// <summary>
    /// An interface implemented by all <see cref="AsyncStateMachineBox{TStateMachine, TResult}"/> instances, regardless of generics.
    /// </summary>
    interface IAsyncStateMachineBox : ITaskCompletionAction
    {
        /// <summary>
        /// Gets an action for moving forward the contained state machine.
        /// This will lazily-allocate the delegate as needed.
        /// </summary>
        Action MoveNextAction { get; }

        /// <summary>Gets the state machine as a boxed object.  This should only be used for debugging purposes.</summary>
        IAsyncStateMachine GetStateMachineObject();
    }

    /// <summary>Shared helpers for manipulating state related to async state machines.</summary>
    internal static class AsyncMethodBuilderCore // debugger depends on this exact name
    {
        internal static Action OutputAsyncCausalityEvents(Task task, Action continuation) =>
            CreateContinuationWrapper(continuation, (innerContinuation, innerTask) =>
            {
                AsyncCausalityTracer.TraceSynchronousWorkStart(CausalityTraceLevel.Required, innerTask.Id, CausalitySynchronousWork.Execution);
                innerContinuation.Invoke(); // Invoke the original continuation
                AsyncCausalityTracer.TraceSynchronousWorkCompletion(CausalityTraceLevel.Required, CausalitySynchronousWork.Execution);
            }, task);

        internal static Action CreateContinuationWrapper(Action continuation, Action<Action,Task> invokeAction, Task innerTask) =>
            new ContinuationWrapper(continuation, invokeAction, innerTask).Invoke;

        internal static Action TryGetStateMachineForDebugger(Action action) // debugger depends on this exact name/signature
        {
            object target = action.Target;
            return
                target is IAsyncStateMachineBox sm ? sm.GetStateMachineObject().MoveNext :
                target is ContinuationWrapper cw ? TryGetStateMachineForDebugger(cw._continuation) :
                action;
        }

        internal static Task TryGetContinuationTask(Action continuation) =>
            (continuation?.Target as ContinuationWrapper)?._innerTask;

        /// <summary>Throws the exception on the ThreadPool.</summary>
        /// <param name="exception">The exception to propagate.</param>
        /// <param name="targetContext">The target context on which to propagate the exception.  Null to use the ThreadPool.</param>
        internal static void ThrowAsync(Exception exception, SynchronizationContext targetContext)
        {
            // Capture the exception into an ExceptionDispatchInfo so that its 
            // stack trace and Watson bucket info will be preserved
            var edi = ExceptionDispatchInfo.Capture(exception);

            // If the user supplied a SynchronizationContext...
            if (targetContext != null)
            {
                try
                {
                    // Post the throwing of the exception to that context, and return.
                    targetContext.Post(state => ((ExceptionDispatchInfo)state).Throw(), edi);
                    return;
                }
                catch (Exception postException)
                {
                    // If something goes horribly wrong in the Post, we'll 
                    // propagate both exceptions on the ThreadPool
                    edi = ExceptionDispatchInfo.Capture(new AggregateException(exception, postException));
                }
            }

            // If we have the new error reporting APIs, report this error.  Otherwise, Propagate the exception(s) on the ThreadPool
#if FEATURE_COMINTEROP
            if (!WindowsRuntimeMarshal.ReportUnhandledError(edi.SourceException))
#endif // FEATURE_COMINTEROP
            {
                ThreadPool.QueueUserWorkItem(state => ((ExceptionDispatchInfo)state).Throw(), edi);
            }
        }

        /// <summary>
        /// Logically we pass just an Action (delegate) to a task for its action to 'ContinueWith' when it completes.
        /// However debuggers and profilers need more information about what that action is. (In particular what 
        /// the action after that is and after that.   To solve this problem we create a 'ContinuationWrapper 
        /// which when invoked just does the original action (the invoke action), but also remembers other information
        /// (like the action after that (which is also a ContinuationWrapper and thus form a linked list).  
        //  We also store that task if the action is associate with at task.  
        /// </summary>
        private sealed class ContinuationWrapper
        {
            private readonly Action<Action, Task> _invokeAction; // This wrapper is an action that wraps another action, this is that Action.  
            internal readonly Action _continuation;              // This is continuation which will happen after m_invokeAction  (and is probably a ContinuationWrapper)
            internal readonly Task _innerTask;                   // If the continuation is logically going to invoke a task, this is that task (may be null)

            internal ContinuationWrapper(Action continuation, Action<Action, Task> invokeAction, Task innerTask)
            {
                Debug.Assert(continuation != null, "Expected non-null continuation");
                Debug.Assert(invokeAction != null, "Expected non-null continuation");

                _invokeAction = invokeAction;
                _continuation = continuation;
                _innerTask = innerTask ?? TryGetContinuationTask(continuation); // if we don't have a task, see if our continuation is a wrapper and use that.
            }

            internal void Invoke() => _invokeAction(_continuation, _innerTask);
        }
    }
}
