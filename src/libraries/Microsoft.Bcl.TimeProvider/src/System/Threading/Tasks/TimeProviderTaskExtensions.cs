// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading.Tasks
{
    public static class TimeProviderTaskExtensions
    {
        private sealed class DelayState : TaskCompletionSource<bool>
        {
            public DelayState() : base(TaskCreationOptions.RunContinuationsAsynchronously) {}
            public ITimer Timer { get; set; }
            public CancellationTokenRegistration Registration { get; set; }
        }

        public static Task Delay(this TimeProvider timeProvider, TimeSpan delay, CancellationToken cancellationToken = default)
        {
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

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            if (delay == TimeSpan.Zero)
            {
                return Task.CompletedTask;
            }

            DelayState state = new();
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

            if (state.Task.IsCompleted)
            {
                state.Registration.Dispose();
            }

            return state.Task;
        }

        private sealed class WaitAsyncState : TaskCompletionSource<bool>
        {
            public WaitAsyncState() : base(TaskCreationOptions.RunContinuationsAsynchronously) { }
            public readonly CancellationTokenSource ContinuationCancellation = new CancellationTokenSource();
            public CancellationTokenRegistration Registration;
            public ITimer? Timer;
        }

        public static Task WaitAsync(this Task task, TimeSpan timeout, TimeProvider timeProvider, CancellationToken cancellationToken = default)
        {
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

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            var state = new WaitAsyncState();

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

            if (state.Task.IsCompleted)
            {
                state.Registration.Dispose();
            }

            return state.Task;
        }

        public static Task<TResult> WaitAsync<TResult>(this Task<TResult> task, TimeSpan timeout, TimeProvider timeProvider, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<TResult>();

            WaitAsync((Task)task, timeout, timeProvider, cancellationToken).ConfigureAwait(false).GetAwaiter().OnCompleted(() =>
            {
                if (task.IsCompleted)
                {
                    tcs.TrySetResult(task.Result);
                }
                else if (cancellationToken.IsCancellationRequested)
                {
                    tcs.TrySetCanceled(cancellationToken);
                }
                else
                {
                    tcs.TrySetException(new TimeoutException());
                }
            });

            return tcs.Task;
        }
    }
}
