// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    /// <summary>Provides an awaiter for a <see cref="ValueTask{TResult}"/>.</summary>
    public readonly struct ValueTaskAwaiter<TResult> : ICriticalNotifyCompletion
    {
        /// <summary>The value being awaited.</summary>
        private readonly ValueTask<TResult> _value;

        /// <summary>Initializes the awaiter.</summary>
        /// <param name="value">The value to be awaited.</param>
        internal ValueTaskAwaiter(ValueTask<TResult> value) => _value = value;

        /// <summary>Gets whether the <see cref="ValueTask{TResult}"/> has completed.</summary>
        public bool IsCompleted => _value.IsCompleted;

        /// <summary>Gets the result of the ValueTask.</summary>
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
    }
}
