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
                throw new TimeoutException();
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
                throw new TimeoutException();
            }
        }
    }
}
