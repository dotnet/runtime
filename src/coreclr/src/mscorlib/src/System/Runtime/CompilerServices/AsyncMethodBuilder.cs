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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Security.Permissions;
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
    [HostProtection(Synchronization = true, ExternalThreading = true)]
    public struct AsyncVoidMethodBuilder
    {
        /// <summary>The synchronization context associated with this operation.</summary>
        private SynchronizationContext m_synchronizationContext;
        /// <summary>State related to the IAsyncStateMachine.</summary>
        private AsyncMethodBuilderCore m_coreState; // mutable struct: must not be readonly
        /// <summary>Task used for debugging and logging purposes only.  Lazily initialized.</summary>
        private Task m_task;

        /// <summary>Initializes a new <see cref="AsyncVoidMethodBuilder"/>.</summary>
        /// <returns>The initialized <see cref="AsyncVoidMethodBuilder"/>.</returns>
        public static AsyncVoidMethodBuilder Create()
        {
            // Capture the current sync context.  If there isn't one, use the dummy s_noContextCaptured
            // instance; this allows us to tell the state of no captured context apart from the state
            // of an improperly constructed builder instance.
            SynchronizationContext sc = SynchronizationContext.CurrentNoFlow;
            if (sc != null)
                sc.OperationStarted();
            return new AsyncVoidMethodBuilder() { m_synchronizationContext = sc };
        }

        /// <summary>Initiates the builder's execution with the associated state machine.</summary>
        /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
        /// <param name="stateMachine">The state machine instance, passed by reference.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="stateMachine"/> argument was null (Nothing in Visual Basic).</exception>
        [SecuritySafeCritical]
        [DebuggerStepThrough]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            // See comment on AsyncMethodBuilderCore.Start
            // AsyncMethodBuilderCore.Start(ref stateMachine);

            if (stateMachine == null) throw new ArgumentNullException("stateMachine");
            Contract.EndContractBlock();

            // Run the MoveNext method within a copy-on-write ExecutionContext scope.
            // This allows us to undo any ExecutionContext changes made in MoveNext,
            // so that they won't "leak" out of the first await.

            Thread currentThread = Thread.CurrentThread;
            ExecutionContextSwitcher ecs = default(ExecutionContextSwitcher);
            RuntimeHelpers.PrepareConstrainedRegions();
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
            m_coreState.SetStateMachine(stateMachine); // argument validation handled by AsyncMethodBuilderCore
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
                AsyncMethodBuilderCore.MoveNextRunner runnerToInitialize = null;
                var continuation = m_coreState.GetCompletionAction(AsyncCausalityTracer.LoggingOn ? this.Task : null, ref runnerToInitialize);
                Contract.Assert(continuation != null, "GetCompletionAction should always return a valid action.");

                // If this is our first await, such that we've not yet boxed the state machine, do so now.
                if (m_coreState.m_stateMachine == null)
                {
                    if (AsyncCausalityTracer.LoggingOn)
                        AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Required, this.Task.Id, "Async: " + stateMachine.GetType().Name, 0);

                    // Box the state machine, then tell the boxed instance to call back into its own builder,
                    // so we can cache the boxed reference.  NOTE: The language compiler may choose to use
                    // a class instead of a struct for the state machine for debugging purposes; in such cases,
                    // the stateMachine will already be an object.
                    m_coreState.PostBoxInitialization(stateMachine, runnerToInitialize, null);
                }

                awaiter.OnCompleted(continuation);
            }
            catch (Exception exc)
            {
                // Prevent exceptions from leaking to the call site, which could
                // then allow multiple flows of execution through the same async method
                // if the awaiter had already scheduled the continuation by the time
                // the exception was thrown.  We propagate the exception on the
                // ThreadPool because we can trust it to not throw, unlike
                // if we were to go to a user-supplied SynchronizationContext,
                // whose Post method could easily throw.
                AsyncMethodBuilderCore.ThrowAsync(exc, targetContext: null);
            }
        }

        /// <summary>
        /// Schedules the specified state machine to be pushed forward when the specified awaiter completes.
        /// </summary>
        /// <typeparam name="TAwaiter">Specifies the type of the awaiter.</typeparam>
        /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
        /// <param name="awaiter">The awaiter.</param>
        /// <param name="stateMachine">The state machine.</param>
        [SecuritySafeCritical]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            try
            {
                AsyncMethodBuilderCore.MoveNextRunner runnerToInitialize = null;
                var continuation = m_coreState.GetCompletionAction(AsyncCausalityTracer.LoggingOn ? this.Task : null, ref runnerToInitialize);
                Contract.Assert(continuation != null, "GetCompletionAction should always return a valid action.");

                // If this is our first await, such that we've not yet boxed the state machine, do so now.
                if (m_coreState.m_stateMachine == null)
                {
                    if (AsyncCausalityTracer.LoggingOn)
                        AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Required, this.Task.Id, "Async: " + stateMachine.GetType().Name, 0);

                    // Box the state machine, then tell the boxed instance to call back into its own builder,
                    // so we can cache the boxed reference. NOTE: The language compiler may choose to use
                    // a class instead of a struct for the state machine for debugging purposes; in such cases,
                    // the stateMachine will already be an object.
                    m_coreState.PostBoxInitialization(stateMachine, runnerToInitialize, null);
                }

                awaiter.UnsafeOnCompleted(continuation);
            }
            catch (Exception e)
            {
                AsyncMethodBuilderCore.ThrowAsync(e, targetContext: null);
            }
        }

        /// <summary>Completes the method builder successfully.</summary>
        public void SetResult()
        {
            if (AsyncCausalityTracer.LoggingOn)
                AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, this.Task.Id, AsyncCausalityStatus.Completed);

            if (m_synchronizationContext != null)
            {
                NotifySynchronizationContextOfCompletion();
            }
        }

        /// <summary>Faults the method builder with an exception.</summary>
        /// <param name="exception">The exception that is the cause of this fault.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="exception"/> argument is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.InvalidOperationException">The builder is not initialized.</exception>
        public void SetException(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException("exception");
            Contract.EndContractBlock();

            if (AsyncCausalityTracer.LoggingOn)
                AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, this.Task.Id, AsyncCausalityStatus.Error);

            if (m_synchronizationContext != null)
            {
                // If we captured a synchronization context, Post the throwing of the exception to it 
                // and decrement its outstanding operation count.
                try
                {
                    AsyncMethodBuilderCore.ThrowAsync(exception, targetContext: m_synchronizationContext);
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
        }

        /// <summary>Notifies the current synchronization context that the operation completed.</summary>
        private void NotifySynchronizationContextOfCompletion()
        {
            Contract.Assert(m_synchronizationContext != null, "Must only be used with a non-null context.");
            try
            {
                m_synchronizationContext.OperationCompleted();
            }
            catch (Exception exc)
            {
                // If the interaction with the SynchronizationContext goes awry,
                // fall back to propagating on the ThreadPool.
                AsyncMethodBuilderCore.ThrowAsync(exc, targetContext: null);
            }
        }

        // This property lazily instantiates the Task in a non-thread-safe manner.  
        private Task Task 
        {
            get
            {
                if (m_task == null) m_task = new Task();
                return m_task;
            }
        }

        /// <summary>
        /// Gets an object that may be used to uniquely identify this builder to the debugger.
        /// </summary>
        /// <remarks>
        /// This property lazily instantiates the ID in a non-thread-safe manner.  
        /// It must only be used by the debugger and AsyncCausalityTracer in a single-threaded manner.
        /// </remarks>
        private object ObjectIdForDebugger { get { return this.Task; } }
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
    [HostProtection(Synchronization = true, ExternalThreading = true)]
    public struct AsyncTaskMethodBuilder
    {
        /// <summary>A cached VoidTaskResult task used for builders that complete synchronously.</summary>
        private readonly static Task<VoidTaskResult> s_cachedCompleted = AsyncTaskMethodBuilder<VoidTaskResult>.s_defaultResultTask;

        /// <summary>The generic builder object to which this non-generic instance delegates.</summary>
        private AsyncTaskMethodBuilder<VoidTaskResult> m_builder; // mutable struct: must not be readonly

        /// <summary>Initializes a new <see cref="AsyncTaskMethodBuilder"/>.</summary>
        /// <returns>The initialized <see cref="AsyncTaskMethodBuilder"/>.</returns>
        public static AsyncTaskMethodBuilder Create()
        {
            return default(AsyncTaskMethodBuilder);
            // Note: If ATMB<T>.Create is modified to do any initialization, this
            //       method needs to be updated to do m_builder = ATMB<T>.Create().
        }

        /// <summary>Initiates the builder's execution with the associated state machine.</summary>
        /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
        /// <param name="stateMachine">The state machine instance, passed by reference.</param>
        [SecuritySafeCritical]
        [DebuggerStepThrough]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            // See comment on AsyncMethodBuilderCore.Start
            // AsyncMethodBuilderCore.Start(ref stateMachine);

            if (stateMachine == null) throw new ArgumentNullException("stateMachine");
            Contract.EndContractBlock();

            // Run the MoveNext method within a copy-on-write ExecutionContext scope.
            // This allows us to undo any ExecutionContext changes made in MoveNext,
            // so that they won't "leak" out of the first await.

            Thread currentThread = Thread.CurrentThread;
            ExecutionContextSwitcher ecs = default(ExecutionContextSwitcher);
            RuntimeHelpers.PrepareConstrainedRegions();
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
            m_builder.SetStateMachine(stateMachine); // argument validation handled by AsyncMethodBuilderCore
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
            m_builder.AwaitOnCompleted<TAwaiter, TStateMachine>(ref awaiter, ref stateMachine);
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
            m_builder.AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref awaiter, ref stateMachine);
        }

        /// <summary>Gets the <see cref="System.Threading.Tasks.Task"/> for this builder.</summary>
        /// <returns>The <see cref="System.Threading.Tasks.Task"/> representing the builder's asynchronous operation.</returns>
        /// <exception cref="System.InvalidOperationException">The builder is not initialized.</exception>
        public Task Task { get { return m_builder.Task; } }

        /// <summary>
        /// Completes the <see cref="System.Threading.Tasks.Task"/> in the 
        /// <see cref="System.Threading.Tasks.TaskStatus">RanToCompletion</see> state.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">The builder is not initialized.</exception>
        /// <exception cref="System.InvalidOperationException">The task has already completed.</exception>
        public void SetResult()
        {
            // Accessing AsyncTaskMethodBuilder.s_cachedCompleted is faster than
            // accessing AsyncTaskMethodBuilder<T>.s_defaultResultTask.
            m_builder.SetResult(s_cachedCompleted);
        }

        /// <summary>
        /// Completes the <see cref="System.Threading.Tasks.Task"/> in the 
        /// <see cref="System.Threading.Tasks.TaskStatus">Faulted</see> state with the specified exception.
        /// </summary>
        /// <param name="exception">The <see cref="System.Exception"/> to use to fault the task.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="exception"/> argument is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.InvalidOperationException">The builder is not initialized.</exception>
        /// <exception cref="System.InvalidOperationException">The task has already completed.</exception>
        public void SetException(Exception exception) { m_builder.SetException(exception); }

        /// <summary>
        /// Called by the debugger to request notification when the first wait operation
        /// (await, Wait, Result, etc.) on this builder's task completes.
        /// </summary>
        /// <param name="enabled">
        /// true to enable notification; false to disable a previously set notification.
        /// </param>
        internal void SetNotificationForWaitCompletion(bool enabled)
        {
            m_builder.SetNotificationForWaitCompletion(enabled);
        }

        /// <summary>
        /// Gets an object that may be used to uniquely identify this builder to the debugger.
        /// </summary>
        /// <remarks>
        /// This property lazily instantiates the ID in a non-thread-safe manner.  
        /// It must only be used by the debugger and tracing pruposes, and only in a single-threaded manner
        /// when no other threads are in the middle of accessing this property or this.Task.
        /// </remarks>
        private object ObjectIdForDebugger { get { return this.Task; } }
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
    [HostProtection(Synchronization = true, ExternalThreading = true)]
    public struct AsyncTaskMethodBuilder<TResult>
    {
        /// <summary>A cached task for default(TResult).</summary>
        internal readonly static Task<TResult> s_defaultResultTask = AsyncTaskCache.CreateCacheableTask(default(TResult));

        // WARNING: For performance reasons, the m_task field is lazily initialized.
        //          For correct results, the struct AsyncTaskMethodBuilder<TResult> must 
        //          always be used from the same location/copy, at least until m_task is 
        //          initialized.  If that guarantee is broken, the field could end up being 
        //          initialized on the wrong copy.

        /// <summary>State related to the IAsyncStateMachine.</summary>
        private AsyncMethodBuilderCore m_coreState; // mutable struct: must not be readonly
        /// <summary>The lazily-initialized built task.</summary>
        private Task<TResult> m_task; // lazily-initialized: must not be readonly

        /// <summary>Initializes a new <see cref="AsyncTaskMethodBuilder"/>.</summary>
        /// <returns>The initialized <see cref="AsyncTaskMethodBuilder"/>.</returns>
        public static AsyncTaskMethodBuilder<TResult> Create()
        {
            return default(AsyncTaskMethodBuilder<TResult>);
            // NOTE:  If this method is ever updated to perform more initialization,
            //        ATMB.Create must also be updated to call this Create method.
        }

        /// <summary>Initiates the builder's execution with the associated state machine.</summary>
        /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
        /// <param name="stateMachine">The state machine instance, passed by reference.</param>
        [SecuritySafeCritical]
        [DebuggerStepThrough]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            // See comment on AsyncMethodBuilderCore.Start
            // AsyncMethodBuilderCore.Start(ref stateMachine);

            if (stateMachine == null) throw new ArgumentNullException("stateMachine");
            Contract.EndContractBlock();

            // Run the MoveNext method within a copy-on-write ExecutionContext scope.
            // This allows us to undo any ExecutionContext changes made in MoveNext,
            // so that they won't "leak" out of the first await.

            Thread currentThread = Thread.CurrentThread;
            ExecutionContextSwitcher ecs = default(ExecutionContextSwitcher);
            RuntimeHelpers.PrepareConstrainedRegions();
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
            m_coreState.SetStateMachine(stateMachine); // argument validation handled by AsyncMethodBuilderCore
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
                AsyncMethodBuilderCore.MoveNextRunner runnerToInitialize = null;
                var continuation = m_coreState.GetCompletionAction(AsyncCausalityTracer.LoggingOn ? this.Task : null, ref runnerToInitialize);
                Contract.Assert(continuation != null, "GetCompletionAction should always return a valid action.");

                // If this is our first await, such that we've not yet boxed the state machine, do so now.
                if (m_coreState.m_stateMachine == null)
                {
                    // Force the Task to be initialized prior to the first suspending await so 
                    // that the original stack-based builder has a reference to the right Task.
                    var builtTask = this.Task;

                    // Box the state machine, then tell the boxed instance to call back into its own builder,
                    // so we can cache the boxed reference. NOTE: The language compiler may choose to use
                    // a class instead of a struct for the state machine for debugging purposes; in such cases,
                    // the stateMachine will already be an object.
                    m_coreState.PostBoxInitialization(stateMachine, runnerToInitialize, builtTask);
                }

                awaiter.OnCompleted(continuation);
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
        [SecuritySafeCritical]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            try
            {
                AsyncMethodBuilderCore.MoveNextRunner runnerToInitialize = null;
                var continuation = m_coreState.GetCompletionAction(AsyncCausalityTracer.LoggingOn ? this.Task : null, ref runnerToInitialize);
                Contract.Assert(continuation != null, "GetCompletionAction should always return a valid action.");

                // If this is our first await, such that we've not yet boxed the state machine, do so now.
                if (m_coreState.m_stateMachine == null)
                {
                    // Force the Task to be initialized prior to the first suspending await so 
                    // that the original stack-based builder has a reference to the right Task.
                    var builtTask = this.Task;

                    // Box the state machine, then tell the boxed instance to call back into its own builder,
                    // so we can cache the boxed reference. NOTE: The language compiler may choose to use
                    // a class instead of a struct for the state machine for debugging purposes; in such cases,
                    // the stateMachine will already be an object.
                    m_coreState.PostBoxInitialization(stateMachine, runnerToInitialize, builtTask);
                }

                awaiter.UnsafeOnCompleted(continuation);
            }
            catch (Exception e)
            {
                AsyncMethodBuilderCore.ThrowAsync(e, targetContext: null);
            }
        }

        /// <summary>Gets the <see cref="System.Threading.Tasks.Task{TResult}"/> for this builder.</summary>
        /// <returns>The <see cref="System.Threading.Tasks.Task{TResult}"/> representing the builder's asynchronous operation.</returns>
        public Task<TResult> Task
        {
            get
            {
                // Get and return the task. If there isn't one, first create one and store it.
                var task = m_task;
                if (task == null) { m_task = task = new Task<TResult>(); }
                return task;
            }
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
            var task = m_task;
            if (task == null)
            {
                m_task = GetTaskForResult(result);
                Contract.Assert(m_task != null, "GetTaskForResult should never return null");
            }
            // Slow path: complete the existing task.
            else
            {
                if (AsyncCausalityTracer.LoggingOn)
                    AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, task.Id, AsyncCausalityStatus.Completed);

                //only log if we have a real task that was previously created
                if (System.Threading.Tasks.Task.s_asyncDebuggingEnabled)
                {
                    System.Threading.Tasks.Task.RemoveFromActiveTasks(task.Id);
                }

                if (!task.TrySetResult(result))
                {
                    throw new InvalidOperationException(Environment.GetResourceString("TaskT_TransitionToFinal_AlreadyCompleted"));
                }
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
            Contract.Requires(completedTask != null, "Expected non-null task");
            Contract.Requires(completedTask.Status == TaskStatus.RanToCompletion, "Expected a successfully completed task");

            // Get the currently stored task, which will be non-null if get_Task has already been accessed.
            // If there isn't one, store the supplied completed task.
            var task = m_task;
            if (task == null)
            {
                m_task = completedTask;
            }
            else
            {
                // Otherwise, complete the task that's there.
                SetResult(default(TResult));
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
            if (exception == null) throw new ArgumentNullException("exception");
            Contract.EndContractBlock();


            var task = m_task;
            if (task == null)
            {
                // Get the task, forcing initialization if it hasn't already been initialized.
                task = this.Task;
            }

            // If the exception represents cancellation, cancel the task.  Otherwise, fault the task.
            var oce = exception as OperationCanceledException;
            bool successfullySet = oce != null ?
                task.TrySetCanceled(oce.CancellationToken, oce) :
                task.TrySetException(exception);

            // Unlike with TaskCompletionSource, we do not need to spin here until m_task is completed,
            // since AsyncTaskMethodBuilder.SetException should not be immediately followed by any code
            // that depends on the task having completely completed.  Moreover, with correct usage, 
            // SetResult or SetException should only be called once, so the Try* methods should always
            // return true, so no spinning would be necessary anyway (the spinning in TCS is only relevant
            // if another thread completes the task first).

            if (!successfullySet)
            {
                throw new InvalidOperationException(Environment.GetResourceString("TaskT_TransitionToFinal_AlreadyCompleted"));
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
            this.Task.SetNotificationForWaitCompletion(enabled);
        }

        /// <summary>
        /// Gets an object that may be used to uniquely identify this builder to the debugger.
        /// </summary>
        /// <remarks>
        /// This property lazily instantiates the ID in a non-thread-safe manner.  
        /// It must only be used by the debugger and tracing purposes, and only in a single-threaded manner
        /// when no other threads are in the middle of accessing this property or this.Task.
        /// </remarks>
        private object ObjectIdForDebugger { get { return this.Task; } }

        /// <summary>
        /// Gets a task for the specified result.  This will either
        /// be a cached or new task, never null.
        /// </summary>
        /// <param name="result">The result for which we need a task.</param>
        /// <returns>The completed task containing the result.</returns>
        [SecuritySafeCritical] // for JitHelpers.UnsafeCast
        private Task<TResult> GetTaskForResult(TResult result)
        {
            Contract.Ensures(
                EqualityComparer<TResult>.Default.Equals(result, Contract.Result<Task<TResult>>().Result),
                "The returned task's Result must return the same value as the specified result value.");

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
                    return JitHelpers.UnsafeCast<Task<TResult>>(task); // UnsafeCast avoids type check we know will succeed
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
                        return JitHelpers.UnsafeCast<Task<TResult>>(task); // UnsafeCast avoids a type check we know will succeed
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
            Contract.Assert(EXCLUSIVE_INT32_MAX >= INCLUSIVE_INT32_MIN, "Expected max to be at least min");
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
        internal static Task<TResult> CreateCacheableTask<TResult>(TResult result)
        {
            return new Task<TResult>(false, result, (TaskCreationOptions)InternalTaskOptions.DoNotDispose, default(CancellationToken));
        }
    }

    /// <summary>Holds state related to the builder's IAsyncStateMachine.</summary>
    /// <remarks>This is a mutable struct.  Be very delicate with it.</remarks>
    internal struct AsyncMethodBuilderCore
    {
        /// <summary>A reference to the heap-allocated state machine object associated with this builder.</summary>
        internal IAsyncStateMachine m_stateMachine;
        /// <summary>A cached Action delegate used when dealing with a default ExecutionContext.</summary>
        internal Action m_defaultContextAction;

        // This method is copy&pasted into the public Start methods to avoid size overhead of valuetype generic instantiations.
        // Ideally, we would build intrinsics to get the raw ref address and raw code address of MoveNext, and just use the shared implementation.
#if false
        /// <summary>Initiates the builder's execution with the associated state machine.</summary>
        /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
        /// <param name="stateMachine">The state machine instance, passed by reference.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="stateMachine"/> argument is null (Nothing in Visual Basic).</exception>
        [SecuritySafeCritical]
        [DebuggerStepThrough]
        internal static void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            if (stateMachine == null) throw new ArgumentNullException("stateMachine");
            Contract.EndContractBlock();

            // Run the MoveNext method within a copy-on-write ExecutionContext scope.
            // This allows us to undo any ExecutionContext changes made in MoveNext,
            // so that they won't "leak" out of the first await.

            Thread currentThread = Thread.CurrentThread;
            ExecutionContextSwitcher ecs = default(ExecutionContextSwitcher);
            RuntimeHelpers.PrepareConstrainedRegions();
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
#endif

        /// <summary>Associates the builder with the state machine it represents.</summary>
        /// <param name="stateMachine">The heap-allocated state machine object.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="stateMachine"/> argument was null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.InvalidOperationException">The builder is incorrectly initialized.</exception>
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            if (stateMachine == null) throw new ArgumentNullException("stateMachine");
            Contract.EndContractBlock();
            if (m_stateMachine != null) throw new InvalidOperationException(Environment.GetResourceString("AsyncMethodBuilder_InstanceNotInitialized"));
            m_stateMachine = stateMachine;
        }

        /// <summary>
        /// Gets the Action to use with an awaiter's OnCompleted or UnsafeOnCompleted method.
        /// On first invocation, the supplied state machine will be boxed.
        /// </summary>
        /// <typeparam name="TMethodBuilder">Specifies the type of the method builder used.</typeparam>
        /// <typeparam name="TStateMachine">Specifies the type of the state machine used.</typeparam>
        /// <param name="builder">The builder.</param>
        /// <param name="stateMachine">The state machine.</param>
        /// <returns>An Action to provide to the awaiter.</returns>
        [SecuritySafeCritical]
        internal Action GetCompletionAction(Task taskForTracing, ref MoveNextRunner runnerToInitialize)
        {
            Contract.Assert(m_defaultContextAction == null || m_stateMachine != null,
                "Expected non-null m_stateMachine on non-null m_defaultContextAction");

            // Alert a listening debugger that we can't make forward progress unless it slips threads.
            // If we don't do this, and a method that uses "await foo;" is invoked through funceval,
            // we could end up hooking up a callback to push forward the async method's state machine,
            // the debugger would then abort the funceval after it takes too long, and then continuing
            // execution could result in another callback being hooked up.  At that point we have
            // multiple callbacks registered to push the state machine, which could result in bad behavior.
            Debugger.NotifyOfCrossThreadDependency();

            // The builder needs to flow ExecutionContext, so capture it.
            var capturedContext = ExecutionContext.FastCapture(); // ok to use FastCapture as we haven't made any permission demands/asserts

            // If the ExecutionContext is the default context, try to use a cached delegate, creating one if necessary.
            Action action;
            MoveNextRunner runner;
            if (capturedContext != null && capturedContext.IsPreAllocatedDefault)
            {
                // Get the cached delegate, and if it's non-null, return it.
                action = m_defaultContextAction;
                if (action != null)
                {
                    Contract.Assert(m_stateMachine != null, "If the delegate was set, the state machine should have been as well.");
                    return action;
                }

                // There wasn't a cached delegate, so create one and cache it.
                // The delegate won't be usable until we set the MoveNextRunner's target state machine.
                runner = new MoveNextRunner(m_stateMachine);

                action = new Action(runner.RunWithDefaultContext);
                if (taskForTracing != null)
                {
                    action = OutputAsyncCausalityEvents(taskForTracing, action);
                }
                m_defaultContextAction = action;
            }
            // Otherwise, create an Action that flows this context.  The context may be null.
            // The delegate won't be usable until we set the MoveNextRunner's target state machine.
            else
            {
                var runnerWithContext = new MoveNextRunnerWithContext(capturedContext, m_stateMachine);
                runner = runnerWithContext;
                action = new Action(runnerWithContext.RunWithCapturedContext);

                if (taskForTracing != null)
                {
                    action = OutputAsyncCausalityEvents(taskForTracing, action);
                }

                // NOTE: If capturedContext is null, we could create the Action to point directly
                // to m_stateMachine.MoveNext.  However, that follows a much more expensive
                // delegate creation path.
            }

            if (m_stateMachine == null)
                runnerToInitialize = runner;

            return action;
        }

        private Action OutputAsyncCausalityEvents(Task innerTask, Action continuation)
        {
            return CreateContinuationWrapper(continuation, () =>
            {
                AsyncCausalityTracer.TraceSynchronousWorkStart(CausalityTraceLevel.Required, innerTask.Id, CausalitySynchronousWork.Execution);

                // Invoke the original continuation
                continuation.Invoke();

                AsyncCausalityTracer.TraceSynchronousWorkCompletion(CausalityTraceLevel.Required, CausalitySynchronousWork.Execution);
            }, innerTask);
        }

        internal void PostBoxInitialization(IAsyncStateMachine stateMachine, MoveNextRunner runner, Task builtTask)
        {
            if (builtTask != null)
            {
                if (AsyncCausalityTracer.LoggingOn)
                    AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Required, builtTask.Id, "Async: " + stateMachine.GetType().Name, 0);

                if (System.Threading.Tasks.Task.s_asyncDebuggingEnabled)
                    System.Threading.Tasks.Task.AddToActiveTasks(builtTask);
            }

            m_stateMachine = stateMachine;
            m_stateMachine.SetStateMachine(m_stateMachine);

            Contract.Assert(runner.m_stateMachine == null, "The runner's state machine should not yet have been populated.");
            Contract.Assert(m_stateMachine != null, "The builder's state machine field should have been initialized.");

            // Now that we have the state machine, store it into the runner that the action delegate points to.
            // And return the action.
            runner.m_stateMachine = m_stateMachine; // only after this line is the Action delegate usable
        }

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

        /// <summary>Provides the ability to invoke a state machine's MoveNext method under a supplied ExecutionContext.</summary>
        internal sealed class MoveNextRunnerWithContext : MoveNextRunner
        {
            /// <summary>The context with which to run MoveNext.</summary>
            private readonly ExecutionContext m_context;

            /// <summary>Initializes the runner.</summary>
            /// <param name="context">The context with which to run MoveNext.</param>
            [SecurityCritical] // Run needs to be SSC to map to Action delegate, so to prevent misuse, we only allow construction through SC
            internal MoveNextRunnerWithContext(ExecutionContext context, IAsyncStateMachine stateMachine) : base(stateMachine)
            {
                m_context = context;
            }

            /// <summary>Invokes MoveNext under the provided context.</summary>
            [SecuritySafeCritical]
            internal void RunWithCapturedContext()
            {
                Contract.Assert(m_stateMachine != null, "The state machine must have been set before calling Run.");

                if (m_context != null)
                {
                    try
                    {
                        // Use the context and callback to invoke m_stateMachine.MoveNext.
                        ExecutionContext.Run(m_context, InvokeMoveNextCallback, m_stateMachine, preserveSyncCtx: true);
                    }
                    finally { m_context.Dispose(); }
                }
                else
                {
                    m_stateMachine.MoveNext();
                }
            }
        }

        /// <summary>Provides the ability to invoke a state machine's MoveNext method.</summary>
        internal class MoveNextRunner
        {
            /// <summary>The state machine whose MoveNext method should be invoked.</summary>
            internal IAsyncStateMachine m_stateMachine;

            /// <summary>Initializes the runner.</summary>
            [SecurityCritical] // Run needs to be SSC to map to Action delegate, so to prevent misuse, we only allow construction through SC
            internal MoveNextRunner(IAsyncStateMachine stateMachine)
            {
                m_stateMachine = stateMachine;
            }

            /// <summary>Invokes MoveNext under the default context.</summary>
            [SecuritySafeCritical]
            internal void RunWithDefaultContext()
            {
                Contract.Assert(m_stateMachine != null, "The state machine must have been set before calling Run.");
                ExecutionContext.Run(ExecutionContext.PreAllocatedDefault, InvokeMoveNextCallback, m_stateMachine, preserveSyncCtx: true);
            }

            /// <summary>Gets a delegate to the InvokeMoveNext method.</summary>
            protected static ContextCallback InvokeMoveNextCallback
            {
                [SecuritySafeCritical]
                get { return s_invokeMoveNext ?? (s_invokeMoveNext = InvokeMoveNext); }
            }

            /// <summary>Cached delegate used with ExecutionContext.Run.</summary>
            [SecurityCritical]
            private static ContextCallback s_invokeMoveNext; // lazily-initialized due to SecurityCritical attribution

            /// <summary>Invokes the MoveNext method on the supplied IAsyncStateMachine.</summary>
            /// <param name="stateMachine">The IAsyncStateMachine machine instance.</param>
            [SecurityCritical] // necessary for ContextCallback in CoreCLR
            private static void InvokeMoveNext(object stateMachine)
            {
                ((IAsyncStateMachine)stateMachine).MoveNext();
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
        private class ContinuationWrapper
        {
            internal readonly Action m_continuation;        // This is continuation which will happen after m_invokeAction  (and is probably a ContinuationWrapper)
            private readonly Action m_invokeAction;         // This wrapper is an action that wraps another action, this is that Action.  
            internal readonly Task m_innerTask;             // If the continuation is logically going to invoke a task, this is that task (may be null)

            internal ContinuationWrapper(Action continuation, Action invokeAction, Task innerTask)
            {
                Contract.Requires(continuation != null, "Expected non-null continuation");

                // If we don't have a task, see if our continuation is a wrapper and use that. 
                if (innerTask == null)
                    innerTask = TryGetContinuationTask(continuation);

                m_continuation = continuation;
                m_innerTask = innerTask;
                m_invokeAction = invokeAction;
            }

            internal void Invoke()
            {
                m_invokeAction();
            }
        }

        internal static Action CreateContinuationWrapper(Action continuation, Action invokeAction, Task innerTask = null)
        {
            return new ContinuationWrapper(continuation, invokeAction, innerTask).Invoke;
        }

        internal static Action TryGetStateMachineForDebugger(Action action)
        {
            object target = action.Target;
            var runner = target as AsyncMethodBuilderCore.MoveNextRunner;
            if (runner != null)
            {
                return new Action(runner.m_stateMachine.MoveNext);
            }

            var continuationWrapper = target as ContinuationWrapper;
            if (continuationWrapper != null)
            {
                return TryGetStateMachineForDebugger(continuationWrapper.m_continuation);
            }

            return action;
        }

    ///<summary>
    /// Given an action, see if it is a contiunation wrapper and has a Task associated with it.  If so return it (null otherwise)
    ///</summary>
        internal static Task TryGetContinuationTask(Action action)
        {
            if (action != null) 
            {
                var asWrapper = action.Target as ContinuationWrapper;
                if (asWrapper != null)
                    return asWrapper.m_innerTask;
            }
            return null;
        }
    }
}
