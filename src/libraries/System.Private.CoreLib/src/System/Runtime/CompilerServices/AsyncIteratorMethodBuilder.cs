// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    /// <summary>Represents a builder for asynchronous iterators.</summary>
    [StructLayout(LayoutKind.Auto)]
    public struct AsyncIteratorMethodBuilder
    {
        /// <summary>The lazily-initialized box/task object, created the first time the iterator awaits something not yet completed.</summary>
        /// <remarks>
        /// This will be the async state machine box created for the compiler-generated class (not struct) state machine
        /// object for the async enumerator.  Even though its not exposed as a Task property as on AsyncTaskMethodBuilder,
        /// it needs to be stored if for no other reason than <see cref="Complete"/> needs to mark it completed in order to clean up.
        /// </remarks>
        private Task<VoidTaskResult>? m_task; // Debugger depends on the exact name of this field.

        /// <summary>Creates an instance of the <see cref="AsyncIteratorMethodBuilder"/> struct.</summary>
        /// <returns>The initialized instance.</returns>
        public static AsyncIteratorMethodBuilder Create() => default;

        /// <summary>Invokes <see cref="IAsyncStateMachine.MoveNext"/> on the state machine while guarding the <see cref="ExecutionContext"/>.</summary>
        /// <typeparam name="TStateMachine">The type of the state machine.</typeparam>
        /// <param name="stateMachine">The state machine instance, passed by reference.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MoveNext<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine =>
            AsyncMethodBuilderCore.Start(ref stateMachine);

        /// <summary>Schedules the state machine to proceed to the next action when the specified awaiter completes.</summary>
        /// <typeparam name="TAwaiter">The type of the awaiter.</typeparam>
        /// <typeparam name="TStateMachine">The type of the state machine.</typeparam>
        /// <param name="awaiter">The awaiter.</param>
        /// <param name="stateMachine">The state machine.</param>
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine =>
            AsyncTaskMethodBuilder<VoidTaskResult>.AwaitOnCompleted(ref awaiter, ref stateMachine, ref m_task);

        /// <summary>Schedules the state machine to proceed to the next action when the specified awaiter completes.</summary>
        /// <typeparam name="TAwaiter">The type of the awaiter.</typeparam>
        /// <typeparam name="TStateMachine">The type of the state machine.</typeparam>
        /// <param name="awaiter">The awaiter.</param>
        /// <param name="stateMachine">The state machine.</param>
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine =>
            AsyncTaskMethodBuilder<VoidTaskResult>.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine, ref m_task);

        /// <summary>Marks iteration as being completed, whether successfully or otherwise.</summary>
        public void Complete()
        {
            if (m_task is null)
            {
                m_task = Task.s_cachedCompleted;
            }
            else
            {
                AsyncTaskMethodBuilder<VoidTaskResult>.SetExistingTaskResult(m_task, default);

                // Ensure the box's state is cleared so that we don't inadvertently keep things
                // alive, such as any locals referenced by the async enumerator.  For async tasks,
                // this is implicitly handled as part of the box/task's MoveNext, with it invoking
                // the completion logic after invoking the state machine's MoveNext after the last
                // await (or it won't have been necessary because the box was never created in the
                // first place).  But with async iterators, the task represents the entire lifetime
                // of the iterator, across any number of MoveNextAsync/DisposeAsync calls, and the
                // only hook we have to know when the whole thing is completed is this call to Complete
                // as inserted by the compiler in the compiler-generated MoveNext on the state machine.
                // If the last MoveNextAsync/DisposeAsync call to the iterator completes asynchronously,
                // then that same clearing logic will handle the iterator as well, but if the last
                // MoveNextAsync/DisposeAsync completes synchronously, that logic will be skipped, and
                // we'll need to handle it here.  Thus, it's possible we could double clear by always
                // doing it here, and the logic needs to be idempotent.
                if (m_task is IAsyncStateMachineBox box)
                {
                    box.ClearStateUponCompletion();
                }
            }
        }

        /// <summary>Gets an object that may be used to uniquely identify this builder to the debugger.</summary>
        internal object ObjectIdForDebugger => m_task ??= AsyncTaskMethodBuilder<VoidTaskResult>.CreateWeaklyTypedStateMachineBox();
    }
}
