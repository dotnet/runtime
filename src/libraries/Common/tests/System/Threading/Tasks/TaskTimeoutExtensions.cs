// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
/// Task timeout helper based on http://blogs.msdn.com/b/pfxteam/archive/2011/11/10/10235834.aspx
/// </summary>
namespace System.Threading.Tasks
{
    public static class TaskTimeoutExtensions
    {
        public static async Task WithCancellation(this Task task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
            {
                if (task != await Task.WhenAny(task, tcs.Task).ConfigureAwait(false))
                {
                    throw new OperationCanceledException(cancellationToken);
                }
                await task; // already completed; propagate any exception
            }
        }

        public static Task TimeoutAfter(this Task task, int millisecondsTimeout)
            => task.TimeoutAfter(TimeSpan.FromMilliseconds(millisecondsTimeout));

        public static async Task TimeoutAfter(this Task task, TimeSpan timeout)
        {
            var cts = new CancellationTokenSource();

            if (task == await Task.WhenAny(task, Task.Delay(timeout, cts.Token)).ConfigureAwait(false))
            {
                cts.Cancel();
                await task.ConfigureAwait(false);
            }
            else
            {
                throw new TimeoutException($"Task timed out after {timeout}");
            }
        }

        public static Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, int millisecondsTimeout)
            => task.TimeoutAfter(TimeSpan.FromMilliseconds(millisecondsTimeout));

        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout)
        {
            var cts = new CancellationTokenSource();

            if (task == await Task<TResult>.WhenAny(task, Task<TResult>.Delay(timeout, cts.Token)).ConfigureAwait(false))
            {
                cts.Cancel();
                return await task.ConfigureAwait(false);
            }
            else
            {
                throw new TimeoutException($"Task timed out after {timeout}");
            }
        }

        public static async Task WhenAllOrAnyFailed(this Task[] tasks, int millisecondsTimeout)
        {
            var cts = new CancellationTokenSource();
            Task task = tasks.WhenAllOrAnyFailed();
            if (task == await Task.WhenAny(task, Task.Delay(millisecondsTimeout, cts.Token)).ConfigureAwait(false))
            {
                cts.Cancel();
                await task.ConfigureAwait(false);
            }
            else
            {
                throw new TimeoutException($"{nameof(WhenAllOrAnyFailed)} timed out after {millisecondsTimeout}ms");
            }
        }

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
                using (var cts = new CancellationTokenSource())
                {
                    await Task.WhenAny(
                        Task.WhenAll(tasks),
                        Task.Delay(3_000, cts.Token)).ConfigureAwait(false); // arbitrary delay; can be dialed up or down in the future
                }

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
