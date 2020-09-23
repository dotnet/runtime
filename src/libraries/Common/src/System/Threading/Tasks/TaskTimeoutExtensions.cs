// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace System.Threading.Tasks
{
    /// <summary>
    /// Task timeout helper based on https://devblogs.microsoft.com/pfxteam/crafting-a-task-timeoutafter-method/
    /// </summary>
    internal static class TaskTimeoutExtensions
    {
        public static async Task WithCancellation(this Task task, CancellationToken cancellationToken)
        {
            if (task is null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            if (task.IsCompleted || !cancellationToken.CanBeCanceled)
            {
                await task.ConfigureAwait(false);
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(static s => ((TaskCompletionSource<bool>)s!).TrySetResult(true), tcs))
            {
                if (task != await Task.WhenAny(task, tcs.Task).ConfigureAwait(false))
                {
                    throw new OperationCanceledException(cancellationToken);
                }

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
                await task; // already completed; propagate any exception
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
            }
        }
    }
}
