// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    /// <summary>Provides an awaiter for a <see cref="ValueTask{TResult}"/>.</summary>
    public struct ValueTaskAwaiter<TResult> : ICriticalNotifyCompletion, IValueTaskAwaiter
    {
        /// <summary>The value being awaited.</summary>
        private ValueTask<TResult> _value; // Methods are called on this; avoid making it readonly so as to avoid unnecessary copies

        /// <summary>Initializes the awaiter.</summary>
        /// <param name="value">The value to be awaited.</param>
        internal ValueTaskAwaiter(ValueTask<TResult> value) => _value = value;

        /// <summary>Gets whether the <see cref="ValueTask{TResult}"/> has completed.</summary>
        public bool IsCompleted => _value.IsCompleted;

        /// <summary>Gets the result of the ValueTask.</summary>
        [StackTraceHidden]
        public TResult GetResult() =>
            _value._task == null ? 
                _value._result : 
                _value._task.GetAwaiter().GetResult();

        /// <summary>Schedules the continuation action for this ValueTask.</summary>
        public void OnCompleted(Action continuation) =>
            _value.AsTask().ConfigureAwait(continueOnCapturedContext: true).GetAwaiter().OnCompleted(continuation);

        /// <summary>Schedules the continuation action for this ValueTask.</summary>
        public void UnsafeOnCompleted(Action continuation) =>
            _value.AsTask().ConfigureAwait(continueOnCapturedContext: true).GetAwaiter().UnsafeOnCompleted(continuation);

        /// <summary>Gets the task underlying <see cref="_value"/>.</summary>
        internal Task<TResult> AsTask() => _value.AsTask();

        /// <summary>Gets the task underlying the incomplete <see cref="_value"/>.</summary>
        /// <remarks>This method is used when awaiting and IsCompleted returned false; thus we expect the value task to be wrapping a non-null task.</remarks>
        Task IValueTaskAwaiter.GetTask() => _value.AsTaskExpectNonNull();
    }

    /// <summary>
    /// Internal interface used to enable extract the Task from arbitrary ValueTask awaiters.
    /// </summary>>
    internal interface IValueTaskAwaiter
    {
        Task GetTask();
    }
}
