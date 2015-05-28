// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http.Headers;

namespace System.Net.Http
{
    internal static class HttpUtilities
    {
#if NETNative
        internal static readonly Version DefaultRequestVersion = HttpVersion.Version20;
#else
        internal static readonly Version DefaultRequestVersion = HttpVersion.Version11;
#endif
        internal static readonly Version DefaultResponseVersion = HttpVersion.Version11;

        internal static bool IsHttpUri(Uri uri)
        {
            Debug.Assert(uri != null);

            string scheme = uri.Scheme;
            return ((string.Compare("http", scheme, StringComparison.OrdinalIgnoreCase) == 0) ||
                (string.Compare("https", scheme, StringComparison.OrdinalIgnoreCase) == 0));
        }

        // Returns true if the task was faulted or canceled and sets tcs accordingly.
        internal static bool HandleFaultsAndCancelation<T>(Task task, TaskCompletionSource<T> tcs)
        {
            Debug.Assert(task.IsCompleted); // Success, faulted, or cancelled
            if (task.IsFaulted)
            {
                tcs.TrySetException(task.Exception.GetBaseException());
                return true;
            }
            else if (task.IsCanceled)
            {
                tcs.TrySetCanceled();
                return true;
            }
            return false;
        }

        // Always specify TaskScheduler.Default to prevent us from using a user defined TaskScheduler.Current.
        // 
        // Since we're not doing any CPU and/or I/O intensive operations, continue on the same thread.
        // This results in better performance since the continuation task doesn't get scheduled by the
        // scheduler and there are no context switches required.
        internal static Task ContinueWithStandard(this Task task, Action<Task> continuation)
        {
            return task.ContinueWith(continuation, CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        internal static Task ContinueWithStandard<T>(this Task<T> task, Action<Task<T>> continuation)
        {
            return task.ContinueWith(continuation, CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
    }
}
