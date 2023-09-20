// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Threading.Tasks
{
    /// <summary>
    /// Provides methods for using <see cref="Task"/> to implement the Asynchronous Programming Model
    /// pattern based on "Begin" and "End" methods.
    /// </summary>
#if SYSTEM_PRIVATE_CORELIB
    public
#else
    internal
#endif
    static class TaskToAsyncResult
    {
        /// <summary>Creates a new <see cref="IAsyncResult"/> from the specified <see cref="Task"/>, optionally invoking <paramref name="callback"/> when the task has completed.</summary>
        /// <param name="task">The <see cref="Task"/> to be wrapped in an <see cref="IAsyncResult"/>.</param>
        /// <param name="callback">The callback to be invoked upon <paramref name="task"/>'s completion. If <see langword="null"/>, no callback will be invoked.</param>
        /// <param name="state">The state to be stored in the <see cref="IAsyncResult"/>.</param>
        /// <returns>An <see cref="IAsyncResult"/> to represent the task's asynchronous operation. This instance will also be passed to <paramref name="callback"/> when it's invoked.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="task"/> is null.</exception>
        /// <remarks>
        /// In conjunction with the <see cref="End(IAsyncResult)"/> or <see cref="End{TResult}(IAsyncResult)"/> methods, this method may be used
        /// to implement the Begin/End pattern (also known as the Asynchronous Programming Model pattern, or APM). It is recommended to not expose this pattern
        /// in new code; the methods on <see cref="TaskToAsyncResult"/> are intended only to help implement such Begin/End methods when they must be exposed, for example
        /// because a base class provides virtual methods for the pattern, or when they've already been exposed and must remain for compatibility.  These methods enable
        /// implementing all of the core asynchronous logic via <see cref="Task"/>s and then easily implementing Begin/End methods around that functionality.
        /// </remarks>
        public static IAsyncResult Begin(Task task, AsyncCallback? callback, object? state)
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(task);
#else
            if (task is null)
            {
                throw new ArgumentNullException(nameof(task));
            }
#endif

            return new TaskAsyncResult(task, state, callback);
        }

        /// <summary>Waits for the <see cref="Task"/> wrapped by the <see cref="IAsyncResult"/> returned by <see cref="Begin"/> to complete.</summary>
        /// <param name="asyncResult">The <see cref="IAsyncResult"/> for which to wait.</param>
        /// <exception cref="ArgumentNullException"><paramref name="asyncResult"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="asyncResult"/> was not produced by a call to <see cref="Begin"/>.</exception>
        /// <remarks>This will propagate any exception stored in the wrapped <see cref="Task"/>.</remarks>
        public static void End(IAsyncResult asyncResult) =>
            Unwrap(asyncResult).GetAwaiter().GetResult();

        /// <summary>Waits for the <see cref="Task{TResult}"/> wrapped by the <see cref="IAsyncResult"/> returned by <see cref="Begin"/> to complete.</summary>
        /// <typeparam name="TResult">The type of the result produced.</typeparam>
        /// <param name="asyncResult">The <see cref="IAsyncResult"/> for which to wait.</param>
        /// <returns>The result of the <see cref="Task{TResult}"/> wrapped by the <see cref="IAsyncResult"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="asyncResult"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="asyncResult"/> was not produced by a call to <see cref="Begin"/>.</exception>
        /// <remarks>This will propagate any exception stored in the wrapped <see cref="Task{TResult}"/>.</remarks>
        public static TResult End<TResult>(IAsyncResult asyncResult) =>
            Unwrap<TResult>(asyncResult).GetAwaiter().GetResult();

        /// <summary>Extracts the underlying <see cref="Task"/> from an <see cref="IAsyncResult"/> created by <see cref="Begin"/>.</summary>
        /// <param name="asyncResult">The <see cref="IAsyncResult"/> created by <see cref="Begin"/>.</param>
        /// <returns>The <see cref="Task"/> wrapped by the <see cref="IAsyncResult"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="asyncResult"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="asyncResult"/> was not produced by a call to <see cref="Begin"/>.</exception>
        public static Task Unwrap(IAsyncResult asyncResult)
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(asyncResult);
#else
            if (asyncResult is null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }
#endif

            if ((asyncResult as TaskAsyncResult)?._task is not Task task)
            {
                throw new ArgumentException(null, nameof(asyncResult));
            }

            return task;
        }

        /// <summary>Extracts the underlying <see cref="Task{TResult}"/> from an <see cref="IAsyncResult"/> created by <see cref="Begin"/>.</summary>
        /// <typeparam name="TResult">The type of the result produced by the returned task.</typeparam>
        /// <param name="asyncResult">The <see cref="IAsyncResult"/> created by <see cref="Begin"/>.</param>
        /// <returns>The <see cref="Task{TResult}"/> wrapped by the <see cref="IAsyncResult"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="asyncResult"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="asyncResult"/> was not produced by a call to <see cref="Begin"/>,
        /// or the <see cref="Task{TResult}"/> provided to <see cref="Begin"/> was used a generic type parameter
        /// that's different from the <typeparamref name="TResult"/> supplied to this call.
        /// </exception>
        public static Task<TResult> Unwrap<TResult>(IAsyncResult asyncResult)
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(asyncResult);
#else
            if (asyncResult is null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }
#endif

            if ((asyncResult as TaskAsyncResult)?._task is not Task<TResult> task)
            {
                throw new ArgumentException(null, nameof(asyncResult));
            }

            return task;
        }

        /// <summary>Provides a simple <see cref="IAsyncResult"/> that wraps a <see cref="Task"/>.</summary>
        /// <remarks>
        /// We could use the Task as the IAsyncResult if the Task's AsyncState is the same as the object state,
        /// but that's very rare, in particular in a situation where someone cares about allocation, and always
        /// using TaskAsyncResult simplifies things and enables additional optimizations.
        /// </remarks>
        private sealed class TaskAsyncResult : IAsyncResult
        {
            /// <summary>The wrapped Task.</summary>
            internal readonly Task _task;
            /// <summary>Callback to invoke when the wrapped task completes.</summary>
            private readonly AsyncCallback? _callback;

            /// <summary>Initializes the IAsyncResult with the Task to wrap and the associated object state.</summary>
            /// <param name="task">The Task to wrap.</param>
            /// <param name="state">The new AsyncState value.</param>
            /// <param name="callback">Callback to invoke when the wrapped task completes.</param>
            internal TaskAsyncResult(Task task, object? state, AsyncCallback? callback)
            {
                Debug.Assert(task is not null);

                _task = task;
                AsyncState = state;

                if (task.IsCompleted)
                {
                    // The task has already completed.  Treat this as synchronous completion.
                    // Invoke the callback; no need to store it.
                    CompletedSynchronously = true;
                    callback?.Invoke(this);
                }
                else if (callback is not null)
                {
                    // Asynchronous completion, and we have a callback; schedule it. We use OnCompleted rather than ContinueWith in
                    // order to avoid running synchronously if the task has already completed by the time we get here but still run
                    // synchronously as part of the task's completion if the task completes after (the more common case).
                    _callback = callback;
                    _task.ConfigureAwait(continueOnCapturedContext: false)
                         .GetAwaiter()
                         .OnCompleted(() => _callback.Invoke(this));
                }
            }

            /// <inheritdoc/>
            public object? AsyncState { get; }

            /// <inheritdoc/>
            public bool CompletedSynchronously { get; }

            /// <inheritdoc/>
            public bool IsCompleted => _task.IsCompleted;

            /// <inheritdoc/>
            public WaitHandle AsyncWaitHandle => ((IAsyncResult)_task).AsyncWaitHandle;
        }
    }
}
