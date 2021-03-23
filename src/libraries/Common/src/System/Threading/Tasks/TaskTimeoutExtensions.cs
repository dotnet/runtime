// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading.Tasks
{
    /// <summary>
    /// Task timeout helper based on https://devblogs.microsoft.com/pfxteam/crafting-a-task-timeoutafter-method/
    /// </summary>
    internal static class TaskTimeoutExtensions
    {
        public static Task WithCancellation(this Task task, CancellationToken cancellationToken)
        {
            if (task is null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            if (task.IsCompleted || !cancellationToken.CanBeCanceled)
            {
                return task;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            return WithCancellationCore(task, cancellationToken);

            static async Task WithCancellationCore(Task task, CancellationToken cancellationToken)
            {
                var tcs = new TaskCompletionSource();
                using CancellationTokenRegistration _ = cancellationToken.UnsafeRegister(static s => ((TaskCompletionSource)s!).TrySetResult(), tcs);

                if (task != await Task.WhenAny(task, tcs.Task).ConfigureAwait(false))
                {
                    throw new TaskCanceledException(Task.FromCanceled(cancellationToken));
                }

                task.GetAwaiter().GetResult(); // already completed; propagate any exception
            }
        }
    }
}
