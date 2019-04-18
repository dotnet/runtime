// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

#if !netstandard
using Internal.Runtime.CompilerServices;
#endif

namespace System.Runtime.CompilerServices
{
    /// <summary>Provides an awaiter for a <see cref="ValueTask"/>.</summary>
    public readonly struct ValueTaskAwaiter : ICriticalNotifyCompletion, IStateMachineBoxAwareAwaiter
    {
        /// <summary>Shim used to invoke an <see cref="Action"/> passed as the state argument to a <see cref="Action{Object}"/>.</summary>
        internal static readonly Action<object?> s_invokeActionDelegate = state =>
        {
            if (!(state is Action action))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.state);
                return;
            }

            action();
        };
        /// <summary>The value being awaited.</summary>
        private readonly ValueTask _value;

        /// <summary>Initializes the awaiter.</summary>
        /// <param name="value">The value to be awaited.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ValueTaskAwaiter(in ValueTask value) => _value = value;

        /// <summary>Gets whether the <see cref="ValueTask"/> has completed.</summary>
        public bool IsCompleted
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _value.IsCompleted;
        }

        /// <summary>Gets the result of the ValueTask.</summary>
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetResult() => _value.ThrowIfCompletedUnsuccessfully();

        /// <summary>Schedules the continuation action for this ValueTask.</summary>
        public void OnCompleted(Action continuation)
        {
            object? obj = _value._obj;
            Debug.Assert(obj == null || obj is Task || obj is IValueTaskSource);

            if (obj is Task t)
            {
                t.GetAwaiter().OnCompleted(continuation);
            }
            else if (obj != null)
            {
                Unsafe.As<IValueTaskSource>(obj).OnCompleted(s_invokeActionDelegate, continuation, _value._token, ValueTaskSourceOnCompletedFlags.UseSchedulingContext | ValueTaskSourceOnCompletedFlags.FlowExecutionContext);
            }
            else
            {
                ValueTask.CompletedTask.GetAwaiter().OnCompleted(continuation);
            }
        }

        /// <summary>Schedules the continuation action for this ValueTask.</summary>
        public void UnsafeOnCompleted(Action continuation)
        {
            object? obj = _value._obj;
            Debug.Assert(obj == null || obj is Task || obj is IValueTaskSource);

            if (obj is Task t)
            {
                t.GetAwaiter().UnsafeOnCompleted(continuation);
            }
            else if (obj != null)
            {
                Unsafe.As<IValueTaskSource>(obj).OnCompleted(s_invokeActionDelegate, continuation, _value._token, ValueTaskSourceOnCompletedFlags.UseSchedulingContext);
            }
            else
            {
                ValueTask.CompletedTask.GetAwaiter().UnsafeOnCompleted(continuation);
            }
        }

        void IStateMachineBoxAwareAwaiter.AwaitUnsafeOnCompleted(IAsyncStateMachineBox box)
        {
            object? obj = _value._obj;
            Debug.Assert(obj == null || obj is Task || obj is IValueTaskSource);

            if (obj is Task t)
            {
                TaskAwaiter.UnsafeOnCompletedInternal(t, box, continueOnCapturedContext: true);
            }
            else if (obj != null)
            {
                Unsafe.As<IValueTaskSource>(obj).OnCompleted(ThreadPoolGlobals.s_invokeAsyncStateMachineBox, box, _value._token, ValueTaskSourceOnCompletedFlags.UseSchedulingContext);
            }
            else
            {
                TaskAwaiter.UnsafeOnCompletedInternal(Task.CompletedTask, box, continueOnCapturedContext: true);
            }
        }
    }

    /// <summary>Provides an awaiter for a <see cref="ValueTask{TResult}"/>.</summary>
    public readonly struct ValueTaskAwaiter<TResult> : ICriticalNotifyCompletion, IStateMachineBoxAwareAwaiter
    {
        /// <summary>The value being awaited.</summary>
        private readonly ValueTask<TResult> _value;

        /// <summary>Initializes the awaiter.</summary>
        /// <param name="value">The value to be awaited.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ValueTaskAwaiter(in ValueTask<TResult> value) => _value = value;

        /// <summary>Gets whether the <see cref="ValueTask{TResult}"/> has completed.</summary>
        public bool IsCompleted
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _value.IsCompleted;
        }

        /// <summary>Gets the result of the ValueTask.</summary>
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult GetResult() => _value.Result;

        /// <summary>Schedules the continuation action for this ValueTask.</summary>
        public void OnCompleted(Action continuation)
        {
            object? obj = _value._obj;
            Debug.Assert(obj == null || obj is Task<TResult> || obj is IValueTaskSource<TResult>);

            if (obj is Task<TResult> t)
            {
                t.GetAwaiter().OnCompleted(continuation);
            }
            else if (obj != null)
            {
                Unsafe.As<IValueTaskSource<TResult>>(obj).OnCompleted(ValueTaskAwaiter.s_invokeActionDelegate, continuation, _value._token, ValueTaskSourceOnCompletedFlags.UseSchedulingContext | ValueTaskSourceOnCompletedFlags.FlowExecutionContext);
            }
            else
            {
                ValueTask.CompletedTask.GetAwaiter().OnCompleted(continuation);
            }
        }

        /// <summary>Schedules the continuation action for this ValueTask.</summary>
        public void UnsafeOnCompleted(Action continuation)
        {
            object? obj = _value._obj;
            Debug.Assert(obj == null || obj is Task<TResult> || obj is IValueTaskSource<TResult>);

            if (obj is Task<TResult> t)
            {
                t.GetAwaiter().UnsafeOnCompleted(continuation);
            }
            else if (obj != null)
            {
                Unsafe.As<IValueTaskSource<TResult>>(obj).OnCompleted(ValueTaskAwaiter.s_invokeActionDelegate, continuation, _value._token, ValueTaskSourceOnCompletedFlags.UseSchedulingContext);
            }
            else
            {
                ValueTask.CompletedTask.GetAwaiter().UnsafeOnCompleted(continuation);
            }
        }

        void IStateMachineBoxAwareAwaiter.AwaitUnsafeOnCompleted(IAsyncStateMachineBox box)
        {
            object? obj = _value._obj;
            Debug.Assert(obj == null || obj is Task<TResult> || obj is IValueTaskSource<TResult>);

            if (obj is Task<TResult> t)
            {
                TaskAwaiter.UnsafeOnCompletedInternal(t, box, continueOnCapturedContext: true);
            }
            else if (obj != null)
            {
                Unsafe.As<IValueTaskSource<TResult>>(obj).OnCompleted(ThreadPoolGlobals.s_invokeAsyncStateMachineBox, box, _value._token, ValueTaskSourceOnCompletedFlags.UseSchedulingContext);
            }
            else
            {
                TaskAwaiter.UnsafeOnCompletedInternal(Task.CompletedTask, box, continueOnCapturedContext: true);
            }
        }
    }

    /// <summary>Internal interface used to enable optimizations from <see cref="AsyncTaskMethodBuilder"/>.</summary>>
    internal interface IStateMachineBoxAwareAwaiter
    {
        /// <summary>Invoked to set <see cref="ITaskCompletionAction.Invoke"/> of the <paramref name="box"/> as the awaiter's continuation.</summary>
        /// <param name="box">The box object.</param>
        void AwaitUnsafeOnCompleted(IAsyncStateMachineBox box);
    }
}
