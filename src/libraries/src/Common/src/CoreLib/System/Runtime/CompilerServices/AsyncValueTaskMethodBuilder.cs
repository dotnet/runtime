// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    /// <summary>Represents a builder for asynchronous methods that return a <see cref="ValueTask"/>.</summary>
    [StructLayout(LayoutKind.Auto)]
    public struct AsyncValueTaskMethodBuilder
    {
        /// <summary>The <see cref="AsyncTaskMethodBuilder"/> to which most operations are delegated.</summary>
        private AsyncTaskMethodBuilder _methodBuilder; // mutable struct; do not make it readonly
        /// <summary>true if completed synchronously and successfully; otherwise, false.</summary>
        private bool _haveResult;
        /// <summary>true if the builder should be used for setting/getting the result; otherwise, false.</summary>
        private bool _useBuilder;

        /// <summary>Creates an instance of the <see cref="AsyncValueTaskMethodBuilder"/> struct.</summary>
        /// <returns>The initialized instance.</returns>
        public static AsyncValueTaskMethodBuilder Create() =>
#if PROJECTN
            // ProjectN's AsyncTaskMethodBuilder.Create() currently does additional debugger-related
            // work, so we need to delegate to it.
            new AsyncValueTaskMethodBuilder() { _methodBuilder = AsyncTaskMethodBuilder.Create() };
#else
            // _methodBuilder should be initialized to AsyncTaskMethodBuilder.Create(), but on coreclr
            // that Create() is a nop, so we can just return the default here.
            default;
#endif

        /// <summary>Begins running the builder with the associated state machine.</summary>
        /// <typeparam name="TStateMachine">The type of the state machine.</typeparam>
        /// <param name="stateMachine">The state machine instance, passed by reference.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine =>
            // will provide the right ExecutionContext semantics
            AsyncMethodBuilderCore.Start(ref stateMachine);

        /// <summary>Associates the builder with the specified state machine.</summary>
        /// <param name="stateMachine">The state machine instance to associate with the builder.</param>
        public void SetStateMachine(IAsyncStateMachine stateMachine) => _methodBuilder.SetStateMachine(stateMachine);

        /// <summary>Marks the task as successfully completed.</summary>
        public void SetResult()
        {
            if (_useBuilder)
            {
                _methodBuilder.SetResult();
            }
            else
            {
                _haveResult = true;
            }
        }

        /// <summary>Marks the task as failed and binds the specified exception to the task.</summary>
        /// <param name="exception">The exception to bind to the task.</param>
        public void SetException(Exception exception) => _methodBuilder.SetException(exception);

        /// <summary>Gets the task for this builder.</summary>
        public ValueTask Task
        {
            get
            {
                if (_haveResult)
                {
                    return default;
                }
                else
                {
                    _useBuilder = true;
                    return new ValueTask(_methodBuilder.Task);
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
            _useBuilder = true;
            _methodBuilder.AwaitOnCompleted(ref awaiter, ref stateMachine);
        }

        /// <summary>Schedules the state machine to proceed to the next action when the specified awaiter completes.</summary>
        /// <typeparam name="TAwaiter">The type of the awaiter.</typeparam>
        /// <typeparam name="TStateMachine">The type of the state machine.</typeparam>
        /// <param name="awaiter">The awaiter.</param>
        /// <param name="stateMachine">The state machine.</param>
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            _useBuilder = true;
            _methodBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
        }
    }

    /// <summary>Represents a builder for asynchronous methods that returns a <see cref="ValueTask{TResult}"/>.</summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    [StructLayout(LayoutKind.Auto)]
    public struct AsyncValueTaskMethodBuilder<TResult>
    {
        /// <summary>The <see cref="AsyncTaskMethodBuilder{TResult}"/> to which most operations are delegated.</summary>
        private AsyncTaskMethodBuilder<TResult> _methodBuilder; // mutable struct; do not make it readonly
        /// <summary>The result for this builder, if it's completed before any awaits occur.</summary>
        private TResult _result;
        /// <summary>true if <see cref="_result"/> contains the synchronous result for the async method; otherwise, false.</summary>
        private bool _haveResult;
        /// <summary>true if the builder should be used for setting/getting the result; otherwise, false.</summary>
        private bool _useBuilder;

        /// <summary>Creates an instance of the <see cref="AsyncValueTaskMethodBuilder{TResult}"/> struct.</summary>
        /// <returns>The initialized instance.</returns>
        public static AsyncValueTaskMethodBuilder<TResult> Create() =>
#if PROJECTN
            // ProjectN's AsyncTaskMethodBuilder<TResult>.Create() currently does additional debugger-related
            // work, so we need to delegate to it.
            new AsyncValueTaskMethodBuilder<TResult>() { _methodBuilder = AsyncTaskMethodBuilder<TResult>.Create() };
#else
            // _methodBuilder should be initialized to AsyncTaskMethodBuilder<TResult>.Create(), but on coreclr
            // that Create() is a nop, so we can just return the default here.
            default;
#endif

        /// <summary>Begins running the builder with the associated state machine.</summary>
        /// <typeparam name="TStateMachine">The type of the state machine.</typeparam>
        /// <param name="stateMachine">The state machine instance, passed by reference.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine =>
            // will provide the right ExecutionContext semantics
            AsyncMethodBuilderCore.Start(ref stateMachine);

        /// <summary>Associates the builder with the specified state machine.</summary>
        /// <param name="stateMachine">The state machine instance to associate with the builder.</param>
        public void SetStateMachine(IAsyncStateMachine stateMachine) => _methodBuilder.SetStateMachine(stateMachine);

        /// <summary>Marks the task as successfully completed.</summary>
        /// <param name="result">The result to use to complete the task.</param>
        public void SetResult(TResult result)
        {
            if (_useBuilder)
            {
                _methodBuilder.SetResult(result);
            }
            else
            {
                _result = result;
                _haveResult = true;
            }
        }

        /// <summary>Marks the task as failed and binds the specified exception to the task.</summary>
        /// <param name="exception">The exception to bind to the task.</param>
        public void SetException(Exception exception) => _methodBuilder.SetException(exception);

        /// <summary>Gets the task for this builder.</summary>
        public ValueTask<TResult> Task
        {
            get
            {
                if (_haveResult)
                {
                    return new ValueTask<TResult>(_result);
                }
                else
                {
                    _useBuilder = true;
                    return new ValueTask<TResult>(_methodBuilder.Task);
                }
            }
        }

        /// <summary>Schedules the state machine to proceed to the next action when the specified awaiter completes.</summary>
        /// <typeparam name="TAwaiter">The type of the awaiter.</typeparam>
        /// <typeparam name="TStateMachine">The type of the state machine.</typeparam>
        /// <param name="awaiter">the awaiter</param>
        /// <param name="stateMachine">The state machine.</param>
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            _useBuilder = true;
            _methodBuilder.AwaitOnCompleted(ref awaiter, ref stateMachine);
        }

        /// <summary>Schedules the state machine to proceed to the next action when the specified awaiter completes.</summary>
        /// <typeparam name="TAwaiter">The type of the awaiter.</typeparam>
        /// <typeparam name="TStateMachine">The type of the state machine.</typeparam>
        /// <param name="awaiter">the awaiter</param>
        /// <param name="stateMachine">The state machine.</param>
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion 
            where TStateMachine : IAsyncStateMachine
        {
            _useBuilder = true;
            _methodBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
        }
    }
}
