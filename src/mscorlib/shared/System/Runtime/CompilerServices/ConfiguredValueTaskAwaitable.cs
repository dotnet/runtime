// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    /// <summary>Provides an awaitable type that enables configured awaits on a <see cref="ValueTask{TResult}"/>.</summary>
    /// <typeparam name="TResult">The type of the result produced.</typeparam>
    [StructLayout(LayoutKind.Auto)]
    public readonly struct ConfiguredValueTaskAwaitable<TResult>
    {
        /// <summary>The wrapped <see cref="ValueTask{TResult}"/>.</summary>
        private readonly ValueTask<TResult> _value;
        /// <summary>true to attempt to marshal the continuation back to the original context captured; otherwise, false.</summary>
        private readonly bool _continueOnCapturedContext;

        /// <summary>Initializes the awaitable.</summary>
        /// <param name="value">The wrapped <see cref="ValueTask{TResult}"/>.</param>
        /// <param name="continueOnCapturedContext">
        /// true to attempt to marshal the continuation back to the original synchronization context captured; otherwise, false.
        /// </param>
        internal ConfiguredValueTaskAwaitable(ValueTask<TResult> value, bool continueOnCapturedContext)
        {
            _value = value;
            _continueOnCapturedContext = continueOnCapturedContext;
        }

        /// <summary>Returns an awaiter for this <see cref="ConfiguredValueTaskAwaitable{TResult}"/> instance.</summary>
        public ConfiguredValueTaskAwaiter GetAwaiter() =>
            new ConfiguredValueTaskAwaiter(_value, _continueOnCapturedContext);

        /// <summary>Provides an awaiter for a <see cref="ConfiguredValueTaskAwaitable{TResult}"/>.</summary>
        [StructLayout(LayoutKind.Auto)]
        public struct ConfiguredValueTaskAwaiter : ICriticalNotifyCompletion, IConfiguredValueTaskAwaiter
        {
            /// <summary>The value being awaited.</summary>
            private ValueTask<TResult> _value; // Methods are called on this; avoid making it readonly so as to avoid unnecessary copies
            /// <summary>The value to pass to ConfigureAwait.</summary>
            internal readonly bool _continueOnCapturedContext;

            /// <summary>Initializes the awaiter.</summary>
            /// <param name="value">The value to be awaited.</param>
            /// <param name="continueOnCapturedContext">The value to pass to ConfigureAwait.</param>
            internal ConfiguredValueTaskAwaiter(ValueTask<TResult> value, bool continueOnCapturedContext)
            {
                _value = value;
                _continueOnCapturedContext = continueOnCapturedContext;
            }

            /// <summary>Gets whether the <see cref="ConfiguredValueTaskAwaitable{TResult}"/> has completed.</summary>
            public bool IsCompleted => _value.IsCompleted;

            /// <summary>Gets the result of the ValueTask.</summary>
            [StackTraceHidden]
            public TResult GetResult() =>
                _value._task == null ? 
                    _value._result : 
                    _value._task.GetAwaiter().GetResult();

            /// <summary>Schedules the continuation action for the <see cref="ConfiguredValueTaskAwaitable{TResult}"/>.</summary>
            public void OnCompleted(Action continuation) =>
                _value.AsTask().ConfigureAwait(_continueOnCapturedContext).GetAwaiter().OnCompleted(continuation);

            /// <summary>Schedules the continuation action for the <see cref="ConfiguredValueTaskAwaitable{TResult}"/>.</summary>
            public void UnsafeOnCompleted(Action continuation) =>
                _value.AsTask().ConfigureAwait(_continueOnCapturedContext).GetAwaiter().UnsafeOnCompleted(continuation);

            /// <summary>Gets the task underlying <see cref="_value"/>.</summary>
            internal Task<TResult> AsTask() => _value.AsTask();

            /// <summary>Gets the task underlying the incomplete <see cref="_value"/>.</summary>
            /// <remarks>This method is used when awaiting and IsCompleted returned false; thus we expect the value task to be wrapping a non-null task.</remarks>
            (Task task, bool continueOnCapturedContext) IConfiguredValueTaskAwaiter.GetTask() => (_value.AsTaskExpectNonNull(), _continueOnCapturedContext);
        }
    }

    /// <summary>
    /// Internal interface used to enable extract the Task from arbitrary configured ValueTask awaiters.
    /// </summary>
    internal interface IConfiguredValueTaskAwaiter
    {
        (Task task, bool continueOnCapturedContext) GetTask();
    }
}
