// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    internal static class TaskExtensions
    {
        public static TaskCompletionSource<TResult> ToApm<TResult>(
            this Task<TResult> task,
            AsyncCallback? callback,
            object? state)
        {
            TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>(state);

            task.ContinueWith(completedTask =>
            {
                bool shouldInvokeCallback = tcs.TrySetFromTask(completedTask);

                // Only invoke the callback if it exists AND we were able to transition the TCS
                // to the terminal state. If we couldn't transition the task it is because it was
                // already transitioned to a Canceled state via a previous HttpWebRequest.Abort() call.
                if (shouldInvokeCallback)
                {
                    callback?.Invoke(tcs.Task);
                }
                else
                {
                    // Verify that the current status of the tcs.Task is 'Canceled'.  This
                    // occurred due to a previous call of tcs.TrySetCanceled() from the
                    // HttpWebRequest.Abort() method.
                    Debug.Assert(tcs.Task.IsCanceled);
                }
            }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);

            return tcs;
        }

    }
}
