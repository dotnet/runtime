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
        private static readonly Action<Task> ObserveExceptionDelegate = static task => { _ = task.Exception; };

        public static void DisposeSafe(this IDisposable resource, AsyncMutex mutex)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.AsyncProcessingTrace(resource, $"Disposing resource within mutex {NetEventSource.IdOf(mutex)}");

            Task lockTask = mutex.EnterAsync(CancellationToken.None);
            if (lockTask.IsCompletedSuccessfully)
            {
                DisposeSafeLockAquired(resource, mutex);
            }
            else
            {
                resource.Observe(
                    DisposeSafeAsync(resource, lockTask, mutex));
            }

            static void DisposeSafeLockAquired(IDisposable resource, AsyncMutex mutex)
            {
                Debug.Assert(mutex.IsHeld);

                if (NetEventSource.Log.IsEnabled()) NetEventSource.MutexEntered(mutex);

                resource.Dispose();

                mutex.Exit();
                if (NetEventSource.Log.IsEnabled()) NetEventSource.MutexExited(mutex);
            }

            static async ValueTask DisposeSafeAsync(IDisposable resource, Task lockTask, AsyncMutex mutex)
            {
                await lockTask.ConfigureAwait(false);
                DisposeSafeLockAquired(resource, mutex);
            }
        }

        #region Observe ValueTask

        // "Observe" either a ValueTask result, or any exception, ignoring it
        // to prevent the unobserved exception event from being raised.
        public static void Observe(ValueTask t)
        {
            if (t.IsCompletedSuccessfully)
            {
                t.GetAwaiter().GetResult();
            }
            else
            {
                ObserveException(t.AsTask());
            }
        }

        public static void Observe(this object? caller, ValueTask t, bool logSuccessfulCompletion = false, [CallerMemberName] string? memberName = null)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                ObserveAndLogCompletion(caller, t, memberName, logSuccessfulCompletion);
            }
            else
            {
                Observe(t);
            }
        }

        private static void ObserveAndLogCompletion(object? caller, ValueTask t, string? memberName, bool logSuccessfulCompletion)
        {
            Debug.Assert(NetEventSource.Log.IsEnabled());

            if (t.IsCompletedSuccessfully)
            {
                t.GetAwaiter().GetResult();

                if (logSuccessfulCompletion)
                {
                    NetEventSource.AsyncProcessingSuccess(caller, memberName);
                }
            }
            else
            {
                ObserveAndLogCompletion(caller, t.AsTask(), memberName, logSuccessfulCompletion);
            }
        }

        #endregion

        #region Observe Task

        // "Observe" either a Task result, or any exception, ignoring it
        // to prevent the unobserved exception event from being raised.
        public static void Observe(Task t)
        {
            if (t.IsCompletedSuccessfully)
            {
                t.GetAwaiter().GetResult();
            }
            else
            {
                ObserveException(t);
            }
        }

        public static void Observe(this object? caller, Task t, bool logSuccessfulCompletion = false, [CallerMemberName] string? memberName = null)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                ObserveAndLogCompletion(caller, t, memberName, logSuccessfulCompletion);
            }
            else
            {
                Observe(t);
            }
        }

        private static void ObserveException(Task task, Action<Task>? observeException = null)
        {
            task.ContinueWith(
                observeException ?? ObserveExceptionDelegate,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        public static void ObserveException(this object? caller, Task t, [CallerMemberName] string? memberName = null)
        {
            ObserveException(t, LogFaulted);

            void LogFaulted(Task task)
            {
                Debug.Assert(task.IsFaulted);

                _ = task.Exception; // accessing exception anyway, to observe it regardless of whether the tracing is enabled

                if (NetEventSource.Log.IsEnabled()) NetEventSource.AsyncProcessingFailure(caller, memberName, task.Exception);
            }
        }

        private static void ObserveAndLogCompletion(object? caller, Task t, string? memberName, bool logSuccessfulCompletion)
        {
            Debug.Assert(NetEventSource.Log.IsEnabled());

            if (logSuccessfulCompletion)
            {
                Observe(
                    AwaitAndLogCompletionAsync(caller, t, memberName));
            }
            else
            {
                ObserveException(caller, t, memberName);
            }

            static async ValueTask AwaitAndLogCompletionAsync(object? caller, Task t, string? memberName)
            {
                try
                {
                    await t.ConfigureAwait(false);
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.AsyncProcessingSuccess(caller, memberName);
                }
                catch (Exception e)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.AsyncProcessingFailure(caller, memberName, e);
                }
            }
        }

        #endregion
    }
}
