// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// NOTE: This is a copy of
// https://github.com/dotnet/coreclr/blame/07b3afc27304800f00975c8fd4836b319aaa8820/src/System.Private.CoreLib/shared/System/Runtime/CompilerServices/AsyncIteratorMethodBuilder.cs
// modified to be compilable against .NET Standard 2.0.  Key differences:
// - Uses the wrapped AsyncTaskMethodBuilder for Create and MoveNext.
// - Uses a custom object for the debugger identity.
// - Nullable annotations removed.

using System.Runtime.InteropServices;
using System.Threading;

namespace System.Runtime.CompilerServices
{
    /// <summary>Represents a builder for asynchronous iterators.</summary>
    [StructLayout(LayoutKind.Auto)]
    public struct AsyncIteratorMethodBuilder
    {
        private AsyncTaskMethodBuilder _methodBuilder; // mutable struct; do not make it readonly
        private object _id;

        /// <summary>Creates an instance of the <see cref="AsyncIteratorMethodBuilder"/> struct.</summary>
        /// <returns>The initialized instance.</returns>
        public static AsyncIteratorMethodBuilder Create() =>
            new AsyncIteratorMethodBuilder() { _methodBuilder = AsyncTaskMethodBuilder.Create() };

        /// <summary>Invokes <see cref="IAsyncStateMachine.MoveNext"/> on the state machine while guarding the <see cref="ExecutionContext"/>.</summary>
        /// <typeparam name="TStateMachine">The type of the state machine.</typeparam>
        /// <param name="stateMachine">The state machine instance, passed by reference.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MoveNext<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine =>
            _methodBuilder.Start(ref stateMachine);

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
        internal object ObjectIdForDebugger => _id ?? Interlocked.CompareExchange(ref _id, new object(), null) ?? _id;
    }
}
