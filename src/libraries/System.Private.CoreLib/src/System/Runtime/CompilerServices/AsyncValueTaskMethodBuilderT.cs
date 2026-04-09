// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    /// <summary>Represents a builder for asynchronous methods that returns a <see cref="ValueTask{TResult}"/>.</summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    [StructLayout(LayoutKind.Auto)]
    public struct AsyncValueTaskMethodBuilder<TResult>
    {
        /// <summary>Sentinel object used to indicate that the builder completed synchronously and successfully.</summary>
        /// <remarks>
        /// To avoid memory safety issues even in the face of invalid race conditions, we ensure that the type of this object
        /// is valid for the mode in which we're operating.  As such, it's cached on the generic builder per TResult
        /// rather than having one sentinel instance for all types.
        /// </remarks>
        internal static readonly Task<TResult> s_syncSuccessSentinel = new Task<TResult>(default(TResult)!);

        /// <summary>The wrapped task.  If the operation completed synchronously and successfully, this will be a sentinel object compared by reference identity.</summary>
        private Task<TResult>? m_task; // Debugger depends on the exact name of this field.
        /// <summary>The result for this builder if it's completed synchronously, in which case <see cref="m_task"/> will be <see cref="s_syncSuccessSentinel"/>.</summary>
        private TResult _result;

        /// <summary>Creates an instance of the <see cref="AsyncValueTaskMethodBuilder{TResult}"/> struct.</summary>
        /// <returns>The initialized instance.</returns>
        public static AsyncValueTaskMethodBuilder<TResult> Create() => default;

        /// <summary>Begins running the builder with the associated state machine.</summary>
        /// <typeparam name="TStateMachine">The type of the state machine.</typeparam>
        /// <param name="stateMachine">The state machine instance, passed by reference.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine =>
            AsyncMethodBuilderCore.Start(ref stateMachine);

        /// <summary>Associates the builder with the specified state machine.</summary>
        /// <param name="stateMachine">The state machine instance to associate with the builder.</param>
        public void SetStateMachine(IAsyncStateMachine stateMachine) =>
            AsyncMethodBuilderCore.SetStateMachine(stateMachine, task: null);

        /// <summary>Marks the value task as successfully completed.</summary>
        /// <param name="result">The result to use to complete the value task.</param>
        public void SetResult(TResult result)
        {
            if (m_task is null)
            {
                _result = result;
                m_task = s_syncSuccessSentinel;
            }
            else
            {
                AsyncTaskMethodBuilder<TResult>.SetExistingTaskResult(m_task, result);
            }
        }

        /// <summary>Marks the value task as failed and binds the specified exception to the value task.</summary>
        /// <param name="exception">The exception to bind to the value task.</param>
        public void SetException(Exception exception) =>
            AsyncTaskMethodBuilder<TResult>.SetException(exception, ref m_task);

        /// <summary>Gets the value task for this builder.</summary>
        public ValueTask<TResult> Task
        {
            get
            {
                if (m_task == s_syncSuccessSentinel)
                {
                    return new ValueTask<TResult>(_result);
                }

                // With normal access paterns, m_task should always be non-null here: the async method should have
                // either completed synchronously, in which case SetResult would have set m_task to a non-null object,
                // or it should be completing asynchronously, in which case AwaitUnsafeOnCompleted would have similarly
                // initialized m_task to a state machine object.  However, if the type is used manually (not via
                // compiler-generated code) and accesses Task directly, we force it to be initialized.  Things will then
                // "work" but in a degraded mode, as we don't know the TStateMachine type here, and thus we use a
                // normal task object instead.

                Task<TResult>? task = m_task ??= new Task<TResult>(); // base task used rather than box to minimize size when used as manual promise
                return new ValueTask<TResult>(task);
            }
        }

        /// <summary>Schedules the state machine to proceed to the next action when the specified awaiter completes.</summary>
        /// <typeparam name="TAwaiter">The type of the awaiter.</typeparam>
        /// <typeparam name="TStateMachine">The type of the state machine.</typeparam>
        /// <param name="awaiter">the awaiter</param>
        /// <param name="stateMachine">The state machine.</param>
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine =>
            AsyncTaskMethodBuilder<TResult>.AwaitOnCompleted(ref awaiter, ref stateMachine, ref m_task);

        /// <summary>Schedules the state machine to proceed to the next action when the specified awaiter completes.</summary>
        /// <typeparam name="TAwaiter">The type of the awaiter.</typeparam>
        /// <typeparam name="TStateMachine">The type of the state machine.</typeparam>
        /// <param name="awaiter">the awaiter</param>
        /// <param name="stateMachine">The state machine.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine =>
            AsyncTaskMethodBuilder<TResult>.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine, ref m_task);

        /// <summary>
        /// Gets an object that may be used to uniquely identify this builder to the debugger.
        /// </summary>
        /// <remarks>
        /// This property lazily instantiates the ID in a non-thread-safe manner.
        /// It must only be used by the debugger and tracing purposes, and only in a single-threaded manner
        /// when no other threads are in the middle of accessing this or other members that lazily initialize the box.
        /// </remarks>
        internal object ObjectIdForDebugger => m_task ??= AsyncTaskMethodBuilder<TResult>.CreateWeaklyTypedStateMachineBox();
    }
}
