// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/// <summary>
/// Task timeout helper based on http://blogs.msdn.com/b/pfxteam/archive/2011/11/10/10235834.aspx
/// </summary>
namespace System.Threading.Tasks
{
    public static class TaskTimeoutExtensions
    {
        public static async Task TimeoutAfter(this Task task, int millisecondsTimeout)
        {
            var cts = new CancellationTokenSource();

            if (task == await Task.WhenAny(task, Task.Delay(millisecondsTimeout, cts.Token)))
            {
                cts.Cancel();
                await task;
            }
            else
            {
                throw new TimeoutException($"Task timed out after {millisecondsTimeout}");
            }
        }

        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, int millisecondsTimeout)
        {
            var cts = new CancellationTokenSource();

            if (task == await Task<TResult>.WhenAny(task, Task<TResult>.Delay(millisecondsTimeout, cts.Token)))
            {
                cts.Cancel();
                return await task;
            }
            else
            {
                throw new TimeoutException($"Task timed out after {millisecondsTimeout}");
            }
        }

        public static async Task WhenAllOrAnyFailed(this Task[] tasks, int millisecondsTimeout)
        {
            var cts = new CancellationTokenSource();
            Task task = tasks.WhenAllOrAnyFailed();
            if (task == await Task.WhenAny(task, Task.Delay(millisecondsTimeout, cts.Token)).ConfigureAwait(false))
            {
                cts.Cancel();
                await task;
            }
            else
            {
                throw new TimeoutException($"{nameof(WhenAllOrAnyFailed)} timed out after {millisecondsTimeout}");
            }
        }

        public static Task WhenAllOrAnyFailed(this Task[] tasks)
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
