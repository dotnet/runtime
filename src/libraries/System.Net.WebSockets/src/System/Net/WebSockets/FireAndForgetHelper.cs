// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    internal static class FireAndForgetHelper
    {
        // "Observe" either a ValueTask result, or any exception, ignoring it
        // to prevent the unobserved exception event from being raised.
        public static void Observe(ValueTask t, [CallerMemberName] string? memberName = null)
        {
            if (t.IsCompletedSuccessfully)
            {
                t.GetAwaiter().GetResult();
            }
            else
            {
                ObserveException(t.AsTask(), memberName);
            }
        }

        // "Observe" either a Task result, or any exception, ignoring it
        // to prevent the unobserved exception event from being raised.
        public static void Observe(Task t, [CallerMemberName] string? memberName = null)
        {
            if (t.IsCompletedSuccessfully)
            {
                t.GetAwaiter().GetResult();
            }
            else
            {
                ObserveException(t, memberName);
            }
        }

        public static void ObserveException(Task t, [CallerMemberName] string? memberName = null)
        {
            // todo: log on success (use memberName)
            _ = memberName;

            t.ContinueWith(
                LogFaulted,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

            static void LogFaulted(Task task)
            {
                Debug.Assert(task.IsFaulted);
                _ = task.Exception; // "observing" the exception
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace($"Exception from asynchronous processing: {task.Exception}");
            }
        }

        public static void DisposeSafe(this IDisposable resource, AsyncMutex mutex)
        {
            Task lockTask = mutex.EnterAsync(CancellationToken.None);
            if (lockTask.IsCompletedSuccessfully)
            {
                resource.Dispose();
                mutex.Exit();
            }
            else
            {
                Observe(
                    DisposeSafeAsync(resource, lockTask, mutex));
            }

            static async ValueTask DisposeSafeAsync(IDisposable resource, Task lockTask, AsyncMutex mutex)
            {
                await lockTask.ConfigureAwait(false);
                resource.Dispose();
                mutex.Exit();
            }
        }
    }
}
