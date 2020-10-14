// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Internal.Runtime.CompilerServices;

using StateMachineBox = System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder<System.Threading.Tasks.VoidTaskResult>.StateMachineBox;

namespace System.Runtime.CompilerServices
{
    /// <summary>Represents a builder for asynchronous methods that return a <see cref="ValueTask"/>.</summary>
    [StructLayout(LayoutKind.Auto)]
    public struct AsyncValueTaskMethodBuilder
    {
        /// <summary>Sentinel object used to indicate that the builder completed synchronously and successfully.</summary>
        private static readonly object s_syncSuccessSentinel = AsyncValueTaskMethodBuilder<VoidTaskResult>.s_syncSuccessSentinel;

        /// <summary>The wrapped state machine box or task, based on the value of <see cref="AsyncTaskCache.s_valueTaskPoolingEnabled"/>.</summary>
        /// <remarks>
        /// If the operation completed synchronously and successfully, this will be <see cref="s_syncSuccessSentinel"/>.
        /// </remarks>
        private object? m_task; // Debugger depends on the exact name of this field.

        /// <summary>Creates an instance of the <see cref="AsyncValueTaskMethodBuilder"/> struct.</summary>
        /// <returns>The initialized instance.</returns>
        public static AsyncValueTaskMethodBuilder Create() => default;

        /// <summary>Begins running the builder with the associated state machine.</summary>
        /// <typeparam name="TStateMachine">The type of the state machine.</typeparam>
        /// <param name="stateMachine">The state machine instance, passed by reference.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine =>
            AsyncMethodBuilderCore.Start(ref stateMachine);

        /// <summary>Associates the builder with the specified state machine.</summary>
        /// <param name="stateMachine">The state machine instance to associate with the builder.</param>
        public void SetStateMachine(IAsyncStateMachine stateMachine) =>
            AsyncMethodBuilderCore.SetStateMachine(stateMachine, task: null);

        /// <summary>Marks the task as successfully completed.</summary>
        public void SetResult()
        {
            if (m_task is null)
            {
                m_task = s_syncSuccessSentinel;
            }
            else if (AsyncTaskCache.s_valueTaskPoolingEnabled)
            {
                Unsafe.As<StateMachineBox>(m_task).SetResult(default);
            }
            else
            {
                AsyncTaskMethodBuilder<VoidTaskResult>.SetExistingTaskResult(Unsafe.As<Task<VoidTaskResult>>(m_task), default);
            }
        }

        /// <summary>Marks the task as failed and binds the specified exception to the task.</summary>
        /// <param name="exception">The exception to bind to the task.</param>
        public void SetException(Exception exception)
        {
            if (AsyncTaskCache.s_valueTaskPoolingEnabled)
            {
                AsyncValueTaskMethodBuilder<VoidTaskResult>.SetException(exception, ref Unsafe.As<object?, StateMachineBox?>(ref m_task));
            }
            else
            {
                AsyncTaskMethodBuilder<VoidTaskResult>.SetException(exception, ref Unsafe.As<object?, Task<VoidTaskResult>?>(ref m_task));
            }
        }

        /// <summary>Gets the task for this builder.</summary>
        public ValueTask Task
        {
            get
            {
                if (m_task == s_syncSuccessSentinel)
                {
                    return default;
                }

                // With normal access paterns, m_task should always be non-null here: the async method should have
                // either completed synchronously, in which case SetResult would have set m_task to a non-null object,
                // or it should be completing asynchronously, in which case AwaitUnsafeOnCompleted would have similarly
                // initialized m_task to a state machine object.  However, if the type is used manually (not via
                // compiler-generated code) and accesses Task directly, we force it to be initialized.  Things will then
                // "work" but in a degraded mode, as we don't know the TStateMachine type here, and thus we use a box around
                // the interface instead.

                if (AsyncTaskCache.s_valueTaskPoolingEnabled)
                {
                    var box = Unsafe.As<StateMachineBox?>(m_task);
                    if (box is null)
                    {
                        m_task = box = AsyncValueTaskMethodBuilder<VoidTaskResult>.CreateWeaklyTypedStateMachineBox();
                    }
                    return new ValueTask(box, box.Version);
                }
                else
                {
                    var task = Unsafe.As<Task<VoidTaskResult>?>(m_task);
                    if (task is null)
                    {
                        m_task = task = new Task<VoidTaskResult>(); // base task used rather than box to minimize size when used as manual promise
                    }
                    return new ValueTask(task);
                }
            }
        }

        /// <summary>Schedules the state machine to proceed to the next action when the specified awaiter completes.</summary>
        /// <typeparam name="TAwaiter">The type of the awaiter.</typeparam>
        /// <typeparam name="TStateMachine">The type of the state machine.</typeparam>
        /// <param name="awaiter">The awaiter.</param>
        /// <param name="stateMachine">The state machine.</param>
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            if (AsyncTaskCache.s_valueTaskPoolingEnabled)
            {
                AsyncValueTaskMethodBuilder<VoidTaskResult>.AwaitOnCompleted(ref awaiter, ref stateMachine, ref Unsafe.As<object?, StateMachineBox?>(ref m_task));
            }
            else
            {
                AsyncTaskMethodBuilder<VoidTaskResult>.AwaitOnCompleted(ref awaiter, ref stateMachine, ref Unsafe.As<object?, Task<VoidTaskResult>?>(ref m_task));
            }
        }

        /// <summary>Schedules the state machine to proceed to the next action when the specified awaiter completes.</summary>
        /// <typeparam name="TAwaiter">The type of the awaiter.</typeparam>
        /// <typeparam name="TStateMachine">The type of the state machine.</typeparam>
        /// <param name="awaiter">The awaiter.</param>
        /// <param name="stateMachine">The state machine.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            if (AsyncTaskCache.s_valueTaskPoolingEnabled)
            {
                AsyncValueTaskMethodBuilder<VoidTaskResult>.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine, ref Unsafe.As<object?, StateMachineBox?>(ref m_task));
            }
            else
            {
                AsyncTaskMethodBuilder<VoidTaskResult>.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine, ref Unsafe.As<object?, Task<VoidTaskResult>?>(ref m_task));
            }
        }

        /// <summary>
        /// Gets an object that may be used to uniquely identify this builder to the debugger.
        /// </summary>
        /// <remarks>
        /// This property lazily instantiates the ID in a non-thread-safe manner.
        /// It must only be used by the debugger and tracing purposes, and only in a single-threaded manner
        /// when no other threads are in the middle of accessing this or other members that lazily initialize the box.
        /// </remarks>
        internal object ObjectIdForDebugger
        {
            get
            {
                if (m_task is null)
                {
                    m_task = AsyncTaskCache.s_valueTaskPoolingEnabled ? (object)
                        AsyncValueTaskMethodBuilder<VoidTaskResult>.CreateWeaklyTypedStateMachineBox() :
                        AsyncTaskMethodBuilder<VoidTaskResult>.CreateWeaklyTypedStateMachineBox();
                }

                return m_task;
            }
        }

        internal static void Execute(object stateMachineBox, ExecutionContext? context, ContextCallback callback)
        {

            // As we can't annotate to Jit which branch is unlikely to be taken
            // this is arranged to prefer specific paths.
            if (context is not null)
            {
                if (context.IsDefault)
                {
                    // 1st preference: Default context
                    Thread currentThread = Thread.CurrentThread;
                    ExecutionContext? currentContext = currentThread._executionContext;
                    if (currentContext == null || currentContext.IsDefault)
                    {
                        // Preferred: On Default and to run on Default; however we need to undo any changes that happen in call.
                        SynchronizationContext? previousSyncCtx = currentThread._synchronizationContext;
                        ExceptionDispatchInfo? edi = null;
                        try
                        {
                            // Run directly
                            callback(stateMachineBox);
                        }
                        catch (Exception ex)
                        {
                            edi = ExceptionDispatchInfo.Capture(ex);
                        }

                        if (currentThread._executionContext == null)
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
                        // Not preferred: Current thread is not on Default
                        ExecutionContext.RunOnDefaultContext(currentThread, currentContext, callback, stateMachineBox);
                    }
                }
                else
                {
                    // 2nd preference: non-default context
                    ExecutionContext.RunInternal(context, callback, stateMachineBox);
                }
            }
            else
            {
                // 3rd preference: flow supressed context
                callback(stateMachineBox);
            }
        }
    }
}
