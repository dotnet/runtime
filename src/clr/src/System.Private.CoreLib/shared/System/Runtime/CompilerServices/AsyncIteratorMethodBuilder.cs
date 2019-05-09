// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Threading;

namespace System.Runtime.CompilerServices
{
    /// <summary>Represents a builder for asynchronous iterators.</summary>
    [StructLayout(LayoutKind.Auto)]
    public struct AsyncIteratorMethodBuilder
    {
        // AsyncIteratorMethodBuilder is used by the language compiler as part of generating
        // async iterators. For now, the implementation just wraps AsyncTaskMethodBuilder, as
        // most of the logic is shared.  However, in the future this could be changed and
        // optimized.  For example, we do need to allocate an object (once) to flow state like
        // ExecutionContext, which AsyncTaskMethodBuilder handles, but it handles it by
        // allocating a Task-derived object.  We could optimize this further by removing
        // the Task from the hierarchy, but in doing so we'd also lose a variety of optimizations
        // related to it, so we'd need to replicate all of those optimizations (e.g. storing
        // that box object directly into a Task's continuation field).

        private AsyncTaskMethodBuilder _methodBuilder; // mutable struct; do not make it readonly

        /// <summary>Creates an instance of the <see cref="AsyncIteratorMethodBuilder"/> struct.</summary>
        /// <returns>The initialized instance.</returns>
        public static AsyncIteratorMethodBuilder Create() =>
#if PROJECTN
            // ProjectN's AsyncTaskMethodBuilder.Create() currently does additional debugger-related
            // work, so we need to delegate to it.
            new AsyncIteratorMethodBuilder() { _methodBuilder = AsyncTaskMethodBuilder.Create() };
#else
            // _methodBuilder should be initialized to AsyncTaskMethodBuilder.Create(), but on coreclr
            // that Create() is a nop, so we can just return the default here.
            default; 
#endif

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
            _methodBuilder.AwaitOnCompleted(ref awaiter, ref stateMachine);

        /// <summary>Schedules the state machine to proceed to the next action when the specified awaiter completes.</summary>
        /// <typeparam name="TAwaiter">The type of the awaiter.</typeparam>
        /// <typeparam name="TStateMachine">The type of the state machine.</typeparam>
        /// <param name="awaiter">The awaiter.</param>
        /// <param name="stateMachine">The state machine.</param>
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine =>
            _methodBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);

        /// <summary>Marks iteration as being completed, whether successfully or otherwise.</summary>
        public void Complete() => _methodBuilder.SetResult();

        /// <summary>Gets an object that may be used to uniquely identify this builder to the debugger.</summary>
        internal object ObjectIdForDebugger => _methodBuilder.ObjectIdForDebugger;
    }
}
