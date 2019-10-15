// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
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
        /// <summary>A cached VoidTaskResult task used for builders that complete synchronously.</summary>
        private static readonly Task<VoidTaskResult> s_cachedCompleted = AsyncTaskMethodBuilder<VoidTaskResult>.s_defaultResultTask;

        /// <summary>The generic builder object to which this non-generic instance delegates.</summary>
        private AsyncTaskMethodBuilder<VoidTaskResult> m_builder; // mutable struct: must not be readonly. Debugger depends on the exact name of this field.

        /// <summary>Initializes a new <see cref="AsyncTaskMethodBuilder"/>.</summary>
        /// <returns>The initialized <see cref="AsyncTaskMethodBuilder"/>.</returns>
        public static AsyncTaskMethodBuilder Create() =>
            // m_builder should be initialized to AsyncTaskMethodBuilder<VoidTaskResult>.Create(), but on coreclr
            // that Create() is a nop, so we can just return the default here.
            default;

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
}
