// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Security
{
    /// <summary>
    /// A wrapper around TaskCompletionSource that allows resetting the task to a pending state.
    /// This is useful for scenarios like TLS handshakes where the completion may need to be
    /// deferred if additional operations (like writes) occur before the handshake is fully complete.
    /// </summary>
    internal sealed class ResettableTaskSource<T>
    {
        private volatile TaskCompletionSource<T> _tcs;
        private readonly object _lock = new object();

        public ResettableTaskSource()
        {
            _tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        /// <summary>
        /// Gets the current task that represents the completion state.
        /// </summary>
        public Task<T> Task
        {
            get
            {
                lock (_lock)
                {
                    return _tcs.Task;
                }
            }
        }

        /// <summary>
        /// Gets whether the current task is completed.
        /// </summary>
        public bool IsCompleted
        {
            get
            {
                lock (_lock)
                {
                    return _tcs.Task.IsCompleted;
                }
            }
        }

        /// <summary>
        /// Resets the task to a pending state if it's not already completed.
        /// If the task is already completed, creates a new pending task.
        /// </summary>
        /// <returns>True if the task was reset (either it was pending or completed), false if it was already pending.</returns>
        public bool Reset()
        {
            lock (_lock)
            {
                if (_tcs.Task.IsCompleted)
                {
                    // Task was completed, create a new pending one
                    _tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
                    return true;
                }

                // Task is already pending, no need to reset
                return false;
            }
        }

        /// <summary>
        /// Resets the task to a pending state unconditionally.
        /// This will always create a new TaskCompletionSource regardless of the current state.
        /// </summary>
        public void ForceReset()
        {
            lock (_lock)
            {
                _tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }

        /// <summary>
        /// Completes the task successfully if it's not already completed.
        /// </summary>
        /// <param name="result">The result to complete the task with.</param>
        /// <returns>True if the task was successfully completed, false if it was already completed.</returns>
        public bool TrySetResult(T result)
        {
            lock (_lock)
            {
                return _tcs.TrySetResult(result);
            }
        }

        /// <summary>
        /// Completes the task with an exception if it's not already completed.
        /// </summary>
        /// <param name="exception">The exception to complete the task with.</param>
        /// <returns>True if the task was successfully completed with the exception, false if it was already completed.</returns>
        public bool TrySetException(Exception exception)
        {
            lock (_lock)
            {
                return _tcs.TrySetException(exception);
            }
        }

        /// <summary>
        /// Cancels the task if it's not already completed.
        /// </summary>
        /// <returns>True if the task was successfully canceled, false if it was already completed.</returns>
        public bool TrySetCanceled()
        {
            lock (_lock)
            {
                return _tcs.TrySetCanceled();
            }
        }

        /// <summary>
        /// Cancels the task with a specific cancellation token if it's not already completed.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to cancel the task with.</param>
        /// <returns>True if the task was successfully canceled, false if it was already completed.</returns>
        public bool TrySetCanceled(CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                return _tcs.TrySetCanceled(cancellationToken);
            }
        }

        /// <summary>
        /// Resets the task and immediately sets it to a pending state.
        /// This is equivalent to calling Reset() but is more explicit about the intent.
        /// </summary>
        public void SetToPending()
        {
            ForceReset();
        }

        /// <summary>
        /// Gets a awaiter for the current task.
        /// </summary>
        public TaskAwaiter<T> GetAwaiter()
        {
            return Task.GetAwaiter();
        }

        /// <summary>
        /// Configures an awaiter for the current task.
        /// </summary>
        /// <param name="continueOnCapturedContext">Whether to continue on the captured context.</param>
        public ConfiguredTaskAwaitable<T> ConfigureAwait(bool continueOnCapturedContext)
        {
            return Task.ConfigureAwait(continueOnCapturedContext);
        }
    }
}
