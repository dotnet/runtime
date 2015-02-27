// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks
{
    /// <summary>Value type discriminated union for a TResult and a <see cref="Task{TResult}"/>.</summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    internal struct TaskValue<TResult>
    {
        /// <summary>The task. this will be non-null iff the operation didn't complete successfully synchronously.</summary>
        private readonly Task<TResult> _task;
        /// <summary>The result to be used if the operation completed successfully synchronously.</summary>
        private readonly TResult _result;

        /// <summary>Initialize the TaskValue with the result of the successful operation.</summary>
        /// <param name="result">The result.</param>
        public TaskValue(TResult result)
        {
            _result = result;
            _task = null;
        }

        /// <summary>
        /// Initialize the TaskValue with a <see cref="Task{TResult}"/> that represents 
        /// the non-successful or incomplete operation.
        /// </summary>
        /// <param name="task"></param>
        public TaskValue(Task<TResult> task)
        {
            Debug.Assert(task != null);
            _result = default(TResult);
            _task = task;
        }

        /// <summary>Implicit operator to wrap a TaskValue around a task.</summary>
        public static implicit operator TaskValue<TResult>(Task<TResult> task)
        {
            return new TaskValue<TResult>(task);
        }

        /// <summary>Implicit operator to wrap a TaskValue around a result.</summary>
        public static implicit operator TaskValue<TResult>(TResult result)
        {
            return new TaskValue<TResult>(result);
        }

        /// <summary>
        /// Gets a <see cref="Task{TResult}"/> object to represent this TaskValue.  It will
        /// either return the wrapped task object if one exists, or it'll manufacture a new
        /// task object to represent the result.
        /// </summary>
        public Task<TResult> AsTask()
        {
            return _task ?? Task.FromResult(_result);
        }

        /// <summary>Gets whether the TaskValue represents a successfully completed operation.</summary>
        public bool IsRanToCompletion
        {
            get { return _task == null || _task.Status == TaskStatus.RanToCompletion; }
        }

        /// <summary>Gets the result.</summary>
        public TResult Result
        {
            get { return _task == null ? _result : _task.GetAwaiter().GetResult(); }
        }

        /// <summary>Gets an awaiter for this value.</summary>
        public TaskValueAwaiter GetAwaiter()
        {
            return new TaskValueAwaiter(this);
        }

        /// <summary>Provides an awaiter for a TaskValue.</summary>
        public struct TaskValueAwaiter : ICriticalNotifyCompletion
        {
            /// <summary>The value being awaited.</summary>
            private readonly TaskValue<TResult> _value;

            /// <summary>Initializes the awaiter.</summary>
            /// <param name="value">The value to be awaited.</param>
            public TaskValueAwaiter(TaskValue<TResult> value)
            {
                _value = value;
            }

            /// <summary>Gets whether the TaskValue has completed.</summary>
            public bool IsCompleted
            {
                get { return _value._task == null || _value._task.IsCompleted; }
            }

            /// <summary>Gets the result of the TaskValue.</summary>
            public TResult GetResult()
            {
                return _value._task == null ?
                    _value._result :
                    _value._task.GetAwaiter().GetResult();
            }

            /// <summary>Schedules the continuation action for this TaskValue.</summary>
            public void OnCompleted(Action continuation)
            {
                _value.AsTask().ConfigureAwait(false).GetAwaiter().OnCompleted(continuation);
            }

            /// <summary>Schedules the continuation action for this TaskValue.</summary>
            public void UnsafeOnCompleted(Action continuation)
            {
                _value.AsTask().ConfigureAwait(false).GetAwaiter().UnsafeOnCompleted(continuation);
            }
        }
    }
}
