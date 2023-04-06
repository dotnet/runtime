// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading.Tasks
{
    /// <summary>
    /// Provide extensions methods for <see cref="Task"/> operations with <see cref="TimeProvider"/>.
    /// </summary>
    public static class TimeProviderTaskExtensions
    {
#if !NET8_0_OR_GREATER
        private sealed class DelayState : TaskCompletionSource<bool>
        {
            public DelayState() : base(TaskCreationOptions.RunContinuationsAsynchronously) {}
            public ITimer Timer { get; set; }
            public CancellationTokenRegistration Registration { get; set; }
        }

        private sealed class WaitAsyncState : TaskCompletionSource<bool>
        {
            public WaitAsyncState() : base(TaskCreationOptions.RunContinuationsAsynchronously) { }
            public readonly CancellationTokenSource ContinuationCancellation = new CancellationTokenSource();
            public CancellationTokenRegistration Registration;
            public ITimer? Timer;
        }
#endif // !NET8_0_OR_GREATER

        /// <summary>Creates a task that completes after a specified time interval.</summary>
        /// <param name="timeProvider">The <see cref="TimeProvider"/> with which to interpret <paramref name="delay"/>.</param>
        /// <param name="delay">The <see cref="TimeSpan"/> to wait before completing the returned task, or <see cref="Timeout.InfiniteTimeSpan"/> to wait indefinitely.</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A task that represents the time delay.</returns>
        /// <exception cref="System.ArgumentNullException">The <paramref name="timeProvider"/> argument is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="delay"/> represents a negative time interval other than <see cref="Timeout.InfiniteTimeSpan"/>.</exception>
        public static Task Delay(this TimeProvider timeProvider, TimeSpan delay, CancellationToken cancellationToken = default)
        {
#if NET8_0_OR_GREATER
            return Task.Delay(delay, timeProvider, cancellationToken);
#else
            if (timeProvider == TimeProvider.System)
            {
                return Task.Delay(delay, cancellationToken);
            }

            if (timeProvider is null)
            {
                throw new ArgumentNullException(nameof(timeProvider));
            }

            if (delay != Timeout.InfiniteTimeSpan && delay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(delay));
            }

            if (delay == TimeSpan.Zero)
            {
                return Task.CompletedTask;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            DelayState state = new();

            // To prevent a race condition where the timer may fire before being assigned to s.Timer,
            // we initialize it with an InfiniteTimeSpan and then set it to the state variable, followed by calling Time.Change.
            state.Timer = timeProvider.CreateTimer(delayState =>
            {
                DelayState s = (DelayState)delayState;
                s.TrySetResult(true);
                s.Registration.Dispose();
                s.Timer.Dispose();
            }, state, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            state.Timer.Change(delay, Timeout.InfiniteTimeSpan);

            state.Registration = cancellationToken.Register(delayState =>
            {
                DelayState s = (DelayState)delayState;
                s.TrySetCanceled(cancellationToken);
                s.Timer.Dispose();
                s.Registration.Dispose();
            }, state);

            // To prevent a race condition where the timer fires after we have attached the cancellation callback
            // but before the registration is stored in state.Registration, we perform a subsequent check to ensure
            // that the registration is not left dangling.
            if (state.Task.IsCompleted)
            {
                state.Registration.Dispose();
            }

            return state.Task;
#endif // NET8_0_OR_GREATER
        }

        /// <summary>
        /// Gets a <see cref="Task"/> that will complete when this <see cref="Task"/> completes, when the specified timeout expires, or when the specified <see cref="CancellationToken"/> has cancellation requested.
        /// </summary>
        /// <param name="task">The task for which to wait on until completion.</param>
        /// <param name="timeout">The timeout after which the <see cref="Task"/> should be faulted with a <see cref="TimeoutException"/> if it hasn't otherwise completed.</param>
        /// <param name="timeProvider">The <see cref="TimeProvider"/> with which to interpret <paramref name="timeout"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for a cancellation request.</param>
        /// <returns>The <see cref="Task"/> representing the asynchronous wait.  It may or may not be the same instance as the current instance.</returns>
        /// <exception cref="System.ArgumentNullException">The <paramref name="task"/> argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="timeProvider"/> argument is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="timeout"/> represents a negative time interval other than <see cref="Timeout.InfiniteTimeSpan"/>.</exception>
        public static Task WaitAsync(this Task task, TimeSpan timeout, TimeProvider timeProvider, CancellationToken cancellationToken = default)
        {
#if NET8_0_OR_GREATER
            return task.WaitAsync(timeout, timeProvider, cancellationToken);
#else
            if (task is null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            if (timeout != Timeout.InfiniteTimeSpan && timeout < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            if (timeProvider is null)
            {
                throw new ArgumentNullException(nameof(timeProvider));
            }

            if (task.IsCompleted)
            {
                return task;
            }

            if (timeout == Timeout.InfiniteTimeSpan && !cancellationToken.CanBeCanceled)
            {
                return task;
            }

            if (timeout == TimeSpan.Zero)
            {
                Task.FromException(new TimeoutException());
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            var state = new WaitAsyncState();

            // To prevent a race condition where the timer may fire before being assigned to s.Timer,
            // we initialize it with an InfiniteTimeSpan and then set it to the state variable, followed by calling Time.Change.
            state.Timer = timeProvider.CreateTimer(static s =>
            {
                var state = (WaitAsyncState)s!;

                state.TrySetException(new TimeoutException());

                state.Registration.Dispose();
                state.Timer!.Dispose();
                state.ContinuationCancellation.Cancel();
            }, state, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            state.Timer.Change(timeout, Timeout.InfiniteTimeSpan);

            _ = task.ContinueWith(static (t, s) =>
            {
                var state = (WaitAsyncState)s!;

                if (t.IsFaulted) state.TrySetException(t.Exception.InnerExceptions);
                else if (t.IsCanceled) state.TrySetCanceled();
                else state.TrySetResult(true);

                state.Registration.Dispose();
                state.Timer?.Dispose();
            }, state, state.ContinuationCancellation.Token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

            state.Registration = cancellationToken.Register(static s =>
            {
                var state = (WaitAsyncState)s!;

                state.TrySetCanceled();

                state.Timer?.Dispose();
                state.ContinuationCancellation.Cancel();
            }, state);

            // To prevent a race condition where the timer fires after we have attached the cancellation callback
            // but before the registration is stored in state.Registration, we perform a subsequent check to ensure
            // that the registration is not left dangling.
            if (state.Task.IsCompleted)
            {
                state.Registration.Dispose();
            }

            return state.Task;
#endif // NET8_0_OR_GREATER
        }

        /// <summary>
        /// Gets a <see cref="Task"/> that will complete when this <see cref="Task"/> completes, when the specified timeout expires, or when the specified <see cref="CancellationToken"/> has cancellation requested.
        /// </summary>
        /// <param name="task">The task for which to wait on until completion.</param>
        /// <param name="timeout">The timeout after which the <see cref="Task"/> should be faulted with a <see cref="TimeoutException"/> if it hasn't otherwise completed.</param>
        /// <param name="timeProvider">The <see cref="TimeProvider"/> with which to interpret <paramref name="timeout"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for a cancellation request.</param>
        /// <returns>The <see cref="Task"/> representing the asynchronous wait.  It may or may not be the same instance as the current instance.</returns>
        /// <exception cref="System.ArgumentNullException">The <paramref name="task"/> argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="timeProvider"/> argument is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="timeout"/> represents a negative time interval other than <see cref="Timeout.InfiniteTimeSpan"/>.</exception>
#if NET8_0_OR_GREATER
        public static Task<TResult> WaitAsync<TResult>(this Task<TResult> task, TimeSpan timeout, TimeProvider timeProvider, CancellationToken cancellationToken = default)
            => task.WaitAsync(timeout, timeProvider, cancellationToken);
#else
        public static async Task<TResult> WaitAsync<TResult>(this Task<TResult> task, TimeSpan timeout, TimeProvider timeProvider, CancellationToken cancellationToken = default)
        {
            await ((Task)task).WaitAsync(timeout, timeProvider, cancellationToken).ConfigureAwait(false);
            return task.Result;
        }
#endif // NET8_0_OR_GREATER
    }
}
