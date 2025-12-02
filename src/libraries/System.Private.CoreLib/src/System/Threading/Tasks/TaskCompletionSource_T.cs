// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace System.Threading.Tasks
{
    /// <summary>
    /// Represents the producer side of a <see cref="Task{TResult}"/> unbound to a
    /// delegate, providing access to the consumer side through the <see cref="Task"/> property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It is often the case that a <see cref="Task{TResult}"/> is desired to
    /// represent another asynchronous operation.
    /// <see cref="TaskCompletionSource{TResult}">TaskCompletionSource</see> is provided for this purpose. It enables
    /// the creation of a task that can be handed out to consumers, and those consumers can use the members
    /// of the task as they would any other. However, unlike most tasks, the state of a task created by a
    /// TaskCompletionSource is controlled explicitly by the methods on TaskCompletionSource. This enables the
    /// completion of the external asynchronous operation to be propagated to the underlying Task. The
    /// separation also ensures that consumers are not able to transition the state without access to the
    /// corresponding TaskCompletionSource.
    /// </para>
    /// <para>
    /// All members of <see cref="TaskCompletionSource{TResult}"/> are thread-safe
    /// and may be used from multiple threads concurrently.
    /// </para>
    /// </remarks>
    /// <typeparam name="TResult">The type of the result value associated with this <see
    /// cref="TaskCompletionSource{TResult}"/>.</typeparam>
    public class TaskCompletionSource<TResult>
    {
        private readonly Task<TResult> _task;

        /// <summary>Creates a <see cref="TaskCompletionSource{TResult}"/>.</summary>
        public TaskCompletionSource() => _task = new Task<TResult>();

        /// <summary>Creates a <see cref="TaskCompletionSource{TResult}"/> with the specified options.</summary>
        /// <remarks>
        /// The <see cref="Task{TResult}"/> created by this instance and accessible through its <see cref="Task"/> property
        /// will be instantiated using the specified <paramref name="creationOptions"/>.
        /// </remarks>
        /// <param name="creationOptions">The options to use when creating the underlying <see cref="Task{TResult}"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The <paramref name="creationOptions"/> represent options invalid for use
        /// with a <see cref="TaskCompletionSource{TResult}"/>.
        /// </exception>
        public TaskCompletionSource(TaskCreationOptions creationOptions) :
            this(null, creationOptions)
        {
        }

        /// <summary>Creates a <see cref="TaskCompletionSource{TResult}"/> with the specified state.</summary>
        /// <param name="state">The state to use as the underlying
        /// <see cref="Task{TResult}"/>'s AsyncState.</param>
        public TaskCompletionSource(object? state) :
            this(state, TaskCreationOptions.None)
        {
        }

        /// <summary>Creates a <see cref="TaskCompletionSource{TResult}"/> with the specified state and options.</summary>
        /// <param name="creationOptions">The options to use when creating the underlying <see cref="Task{TResult}"/>.</param>
        /// <param name="state">The state to use as the underlying <see cref="Task{TResult}"/>'s AsyncState.</param>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="creationOptions"/> represent options invalid for use with a <see cref="TaskCompletionSource{TResult}"/>.</exception>
        public TaskCompletionSource(object? state, TaskCreationOptions creationOptions) =>
            _task = new Task<TResult>(state, creationOptions);

        /// <summary>Gets the <see cref="Task{TResult}"/> created by this <see cref="TaskCompletionSource{TResult}"/>.</summary>
        /// <remarks>
        /// This property enables a consumer access to the <see cref="Task{TResult}"/> that is controlled by this instance.
        /// The <see cref="SetResult"/>, <see cref="SetException(Exception)"/>, <see cref="SetException(IEnumerable{Exception})"/>,
        /// and <see cref="SetCanceled"/> methods (and their "Try" variants) on this instance all result in the relevant state
        /// transitions on this underlying Task.
        /// </remarks>
        public Task<TResult> Task => _task;

        /// <summary>Transitions the underlying <see cref="Task{TResult}"/> into the <see cref="TaskStatus.Faulted"/> state.</summary>
        /// <param name="exception">The exception to bind to this <see cref="Task{TResult}"/>.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="exception"/> argument is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// The underlying <see cref="Task{TResult}"/> is already in one of the three final states:
        /// <see cref="TaskStatus.RanToCompletion"/>,
        /// <see cref="TaskStatus.Faulted"/>, or
        /// <see cref="TaskStatus.Canceled"/>.
        /// </exception>
        public void SetException(Exception exception)
        {
            if (!TrySetException(exception))
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.TaskT_TransitionToFinal_AlreadyCompleted);
            }
        }

        /// <summary>Transitions the underlying <see cref="Task{TResult}"/> into the <see cref="TaskStatus.Faulted"/> state.</summary>
        /// <param name="exceptions">The collection of exceptions to bind to this <see cref="Task{TResult}"/>.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="exceptions"/> argument is null.</exception>
        /// <exception cref="ArgumentException">There are one or more null elements in <paramref name="exceptions"/>.</exception>
        /// <exception cref="InvalidOperationException">
        /// The underlying <see cref="Task{TResult}"/> is already in one of the three final states:
        /// <see cref="TaskStatus.RanToCompletion"/>,
        /// <see cref="TaskStatus.Faulted"/>, or
        /// <see cref="TaskStatus.Canceled"/>.
        /// </exception>
        public void SetException(IEnumerable<Exception> exceptions)
        {
            if (!TrySetException(exceptions))
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.TaskT_TransitionToFinal_AlreadyCompleted);
            }
        }

        /// <summary>
        /// Attempts to transition the underlying <see cref="Task{TResult}"/> into the <see cref="TaskStatus.Faulted"/> state.
        /// </summary>
        /// <param name="exception">The exception to bind to this <see cref="Task{TResult}"/>.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        /// <remarks>
        /// This operation will return false if the <see cref="Task{TResult}"/> is already in one of the three final states:
        /// <see cref="TaskStatus.RanToCompletion"/>,
        /// <see cref="TaskStatus.Faulted"/>, or
        /// <see cref="TaskStatus.Canceled"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">The <paramref name="exception"/> argument is null.</exception>
        public bool TrySetException(Exception exception)
        {
            if (exception is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.exception);
            }

            bool rval = _task.TrySetException(exception);
            if (!rval && !_task.IsCompleted)
            {
                _task.SpinUntilCompleted();
            }

            return rval;
        }

        /// <summary>
        /// Attempts to transition the underlying <see cref="Task{TResult}"/> into the <see cref="TaskStatus.Faulted"/> state.
        /// </summary>
        /// <param name="exceptions">The collection of exceptions to bind to this <see cref="Task{TResult}"/>.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        /// <remarks>
        /// This operation will return false if the <see cref="Task{TResult}"/> is already in one of the three final states:
        /// <see cref="TaskStatus.RanToCompletion"/>,
        /// <see cref="TaskStatus.Faulted"/>, or
        /// <see cref="TaskStatus.Canceled"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">The <paramref name="exceptions"/> argument is null.</exception>
        /// <exception cref="ArgumentException">There are one or more null elements in <paramref name="exceptions"/>.</exception>
        /// <exception cref="ArgumentException">The <paramref name="exceptions"/> collection is empty.</exception>
        public bool TrySetException(IEnumerable<Exception> exceptions)
        {
            if (exceptions is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.exceptions);
            }

            var defensiveCopy = new List<Exception>();
            foreach (Exception e in exceptions)
            {
                if (e is null)
                {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.TaskCompletionSourceT_TrySetException_NullException, ExceptionArgument.exceptions);
                }

                defensiveCopy.Add(e);
            }

            if (defensiveCopy.Count == 0)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.TaskCompletionSourceT_TrySetException_NoExceptions, ExceptionArgument.exceptions);
            }

            bool rval = _task.TrySetException(defensiveCopy);
            if (!rval && !_task.IsCompleted)
            {
                _task.SpinUntilCompleted();
            }

            return rval;
        }

        /// <summary>
        /// Transitions the underlying <see cref="Task{TResult}"/> into the <see cref="TaskStatus.RanToCompletion"/> state.
        /// </summary>
        /// <param name="result">The result value to bind to this <see cref="Task{TResult}"/>.</param>
        /// <exception cref="InvalidOperationException">
        /// The underlying <see cref="Task{TResult}"/> is already in one of the three final states:
        /// <see cref="TaskStatus.RanToCompletion"/>,
        /// <see cref="TaskStatus.Faulted"/>, or
        /// <see cref="TaskStatus.Canceled"/>.
        /// </exception>
        public void SetResult(TResult result)
        {
            if (!TrySetResult(result))
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.TaskT_TransitionToFinal_AlreadyCompleted);
            }
        }

        /// <summary>
        /// Attempts to transition the underlying <see cref="Task{TResult}"/> into the <see cref="TaskStatus.RanToCompletion"/> state.
        /// </summary>
        /// <param name="result">The result value to bind to this <see cref="Task{TResult}"/>.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        /// <remarks>
        /// This operation will return false if the <see cref="Task{TResult}"/> is already in one of the three final states:
        /// <see cref="TaskStatus.RanToCompletion"/>,
        /// <see cref="TaskStatus.Faulted"/>, or
        /// <see cref="TaskStatus.Canceled"/>.
        /// </remarks>
        public bool TrySetResult(TResult result)
        {
            bool rval = _task.TrySetResult(result);
            if (!rval)
            {
                _task.SpinUntilCompleted();
            }

            return rval;
        }

        /// <summary>
        /// Transitions the underlying <see cref="Task{TResult}"/> into the <see cref="TaskStatus.Canceled"/> state.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The underlying <see cref="Task{TResult}"/> is already in one of the three final states:
        /// <see cref="TaskStatus.RanToCompletion"/>,
        /// <see cref="TaskStatus.Faulted"/>, or
        /// <see cref="TaskStatus.Canceled"/>.
        /// </exception>
        public void SetCanceled() => SetCanceled(default);

        /// <summary>
        /// Transitions the underlying <see cref="Task{TResult}"/> into the <see cref="TaskStatus.Canceled"/> state
        /// using the specified token.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token with which to cancel the <see cref="Task{TResult}"/>.</param>
        /// <exception cref="InvalidOperationException">
        /// The underlying <see cref="Task{TResult}"/> is already in one of the three final states:
        /// <see cref="TaskStatus.RanToCompletion"/>,
        /// <see cref="TaskStatus.Faulted"/>, or
        /// <see cref="TaskStatus.Canceled"/>.
        /// </exception>
        public void SetCanceled(CancellationToken cancellationToken)
        {
            if (!TrySetCanceled(cancellationToken))
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.TaskT_TransitionToFinal_AlreadyCompleted);
            }
        }

        /// <summary>
        /// Attempts to transition the underlying <see cref="Task{TResult}"/> into the <see cref="TaskStatus.Canceled"/> state.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        /// <remarks>
        /// This operation will return false if the <see cref="Task{TResult}"/> is already in one of the three final states:
        /// <see cref="TaskStatus.RanToCompletion"/>,
        /// <see cref="TaskStatus.Faulted"/>, or
        /// <see cref="TaskStatus.Canceled"/>.
        /// </remarks>
        public bool TrySetCanceled() => TrySetCanceled(default);

        /// <summary>
        /// Attempts to transition the underlying <see cref="Task{TResult}"/> into the <see cref="TaskStatus.Canceled"/> state.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token with which to cancel the <see cref="Task{TResult}"/>.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        /// <remarks>
        /// This operation will return false if the <see cref="Task{TResult}"/> is already in one of the three final states:
        /// <see cref="TaskStatus.RanToCompletion"/>,
        /// <see cref="TaskStatus.Faulted"/>, or
        /// <see cref="TaskStatus.Canceled"/>.
        /// </remarks>
        public bool TrySetCanceled(CancellationToken cancellationToken)
        {
            bool rval = _task.TrySetCanceled(cancellationToken);
            if (!rval && !_task.IsCompleted)
            {
                _task.SpinUntilCompleted();
            }

            return rval;
        }

        /// <summary>
        /// Transition the underlying <see cref="Task{TResult}"/> into the same completion state as the specified <paramref name="completedTask"/>.
        /// </summary>
        /// <param name="completedTask">The completed task whose completion status (including result, exception, or cancellation information) should be copied to the underlying task.</param>
        /// <exception cref="ArgumentNullException"><paramref name="completedTask"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="completedTask"/> is not completed.</exception>
        /// <exception cref="InvalidOperationException">
        /// The underlying <see cref="Task{TResult}"/> is already in one of the three final states:
        /// <see cref="TaskStatus.RanToCompletion"/>, <see cref="TaskStatus.Faulted"/>, or <see cref="TaskStatus.Canceled"/>.
        /// </exception>
        /// <remarks>
        /// This operation will return false if the <see cref="Task{TResult}"/> is already in one of the three final states:
        /// <see cref="TaskStatus.RanToCompletion"/>, <see cref="TaskStatus.Faulted"/>, or <see cref="TaskStatus.Canceled"/>.
        /// </remarks>
        public void SetFromTask(Task<TResult> completedTask)
        {
            if (!TrySetFromTask(completedTask))
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.TaskT_TransitionToFinal_AlreadyCompleted);
            }
        }

        /// <summary>
        /// Attempts to transition the underlying <see cref="Task{TResult}"/> into the same completion state as the specified <paramref name="completedTask"/>.
        /// </summary>
        /// <param name="completedTask">The completed task whose completion status (including result, exception, or cancellation information) should be copied to the underlying task.</param>
        /// <returns><see langword="true"/> if the operation was successful; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="completedTask"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="completedTask"/> is not completed.</exception>
        /// <remarks>
        /// This operation will return false if the <see cref="Task{TResult}"/> is already in one of the three final states:
        /// <see cref="TaskStatus.RanToCompletion"/>, <see cref="TaskStatus.Faulted"/>, or <see cref="TaskStatus.Canceled"/>.
        /// </remarks>
        public bool TrySetFromTask(Task<TResult> completedTask)
        {
            ArgumentNullException.ThrowIfNull(completedTask);
            if (!completedTask.IsCompleted)
            {
                throw new ArgumentException(SR.Task_MustBeCompleted, nameof(completedTask));
            }

            // Try to transition to the appropriate final state based on the state of completedTask.
            bool result = false;
            switch (completedTask.Status)
            {
                case TaskStatus.RanToCompletion:
                    result = _task.TrySetResult(completedTask.Result);
                    break;

                case TaskStatus.Canceled:
                    result = _task.TrySetCanceled(completedTask.CancellationToken, completedTask.GetCancellationExceptionDispatchInfo());
                    break;

                case TaskStatus.Faulted:
                    result = _task.TrySetException(completedTask.GetExceptionDispatchInfos());
                    break;
            }

            // If we successfully transitioned to a final state, we're done. If we didn't, it's possible a concurrent operation
            // is still in the process of completing the task, and callers of this method expect the task to already be fully
            // completed when this method returns. As such, we spin until the task is completed, and then return whether this
            // call successfully did the transition.
            if (!result && !_task.IsCompleted)
            {
                _task.SpinUntilCompleted();
            }

            return result;
        }
    }
}
