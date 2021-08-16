// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks
{
    public static class TaskTimeoutExtensions
    {
        #region WaitAsync polyfills
        // Test polyfills when targeting a platform that doesn't have these ConfigureAwait overloads on Task

        public static Task WaitAsync(this Task task, int millisecondsTimeout) =>
            WaitAsync(task, TimeSpan.FromMilliseconds(millisecondsTimeout), default);

        public static Task WaitAsync(this Task task, TimeSpan timeout) =>
            WaitAsync(task, timeout, default);

        public static Task WaitAsync(this Task task, CancellationToken cancellationToken) =>
            WaitAsync(task, Timeout.InfiniteTimeSpan, cancellationToken);

        public async static Task WaitAsync(this Task task, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            using (new Timer(s => ((TaskCompletionSource<bool>)s).TrySetException(new TimeoutException()), tcs, timeout, Timeout.InfiniteTimeSpan))
            using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetCanceled(), tcs))
            {
                await(await Task.WhenAny(task, tcs.Task).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        public static Task<TResult> WaitAsync<TResult>(this Task<TResult> task, int millisecondsTimeout) =>
            WaitAsync(task, TimeSpan.FromMilliseconds(millisecondsTimeout), default);

        public static Task<TResult> WaitAsync<TResult>(this Task<TResult> task, TimeSpan timeout) =>
            WaitAsync(task, timeout, default);

        public static Task<TResult> WaitAsync<TResult>(this Task<TResult> task, CancellationToken cancellationToken) =>
            WaitAsync(task, Timeout.InfiniteTimeSpan, cancellationToken);

        public static async Task<TResult> WaitAsync<TResult>(this Task<TResult> task, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<TResult>();
            using (new Timer(s => ((TaskCompletionSource<TResult>)s).TrySetException(new TimeoutException()), tcs, timeout, Timeout.InfiniteTimeSpan))
            using (cancellationToken.Register(s => ((TaskCompletionSource<TResult>)s).TrySetCanceled(), tcs))
            {
                return await (await Task.WhenAny(task, tcs.Task).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }
        #endregion

        public static async Task WhenAllOrAnyFailed(this Task[] tasks, int millisecondsTimeout) =>
            await tasks.WhenAllOrAnyFailed().WaitAsync(TimeSpan.FromMilliseconds(millisecondsTimeout));

        public static async Task WhenAllOrAnyFailed(Task t1, Task t2, int millisecondsTimeout) =>
            await new Task[] {t1, t2}.WhenAllOrAnyFailed(millisecondsTimeout);

        public static async Task WhenAllOrAnyFailed(this Task[] tasks)
        {
            try
            {
                await WhenAllOrAnyFailedCore(tasks).ConfigureAwait(false);
            }
            catch
            {
                // Wait a bit to allow other tasks to complete so we can include their exceptions
                // in the error we throw.
                try
                {
                    await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(3)); // arbitrary delay; can be dialed up or down in the future
                }
                catch { }

                var exceptions = new List<Exception>();
                foreach (Task t in tasks)
                {
                    switch (t.Status)
                    {
                        case TaskStatus.Faulted: exceptions.Add(t.Exception); break;
                        case TaskStatus.Canceled: exceptions.Add(new TaskCanceledException(t)); break;
                    }
                }

                Debug.Assert(exceptions.Count > 0);
                if (exceptions.Count > 1)
                {
                    throw new AggregateException(exceptions);
                }
                throw;
            }
        }

        private static Task WhenAllOrAnyFailedCore(this Task[] tasks)
        {
            int remaining = tasks.Length;
            var tcs = new TaskCompletionSource<bool>();
            foreach (Task t in tasks)
            {
                t.ContinueWith(a =>
                {
                    if (a.IsFaulted)
                    {
                        tcs.TrySetException(a.Exception.InnerExceptions);
                        Interlocked.Decrement(ref remaining);
                    }
                    else if (a.IsCanceled)
                    {
                        tcs.TrySetCanceled();
                        Interlocked.Decrement(ref remaining);
                    }
                    else if (Interlocked.Decrement(ref remaining) == 0)
                    {
                        tcs.TrySetResult(true);
                    }
                }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
            }
            return tcs.Task;
        }
    }
}
