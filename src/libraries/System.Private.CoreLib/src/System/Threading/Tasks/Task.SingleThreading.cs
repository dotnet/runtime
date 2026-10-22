// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace System.Threading.Tasks;

public partial class Task
{
    private bool SpinThenBlockingWait(int millisecondsTimeout, CancellationToken cancellationToken)
    {
        if (IsCompleted)
        {
            return true;
        }
        TimeoutHelper.WaitForTimeout(millisecondsTimeout, cancellationToken);
        return false;
    }

    [UnsupportedOSPlatform("browser")]
    private static bool WaitAllCore(ReadOnlySpan<Task> tasks, int millisecondsTimeout, CancellationToken cancellationToken)
    {
        if (millisecondsTimeout < -1)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.millisecondsTimeout);
        }

        cancellationToken.ThrowIfCancellationRequested();

        bool hasIncompleteTask = false;
        bool exceptionSeen = false;
        bool cancellationSeen = false;
        List<Task>? notificationTasks = null;

        for (int i = tasks.Length - 1; i >= 0; i--)
        {
            Task task = tasks[i];
            if (task is null)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Task_WaitMulti_NullTask, ExceptionArgument.tasks);
            }

            bool taskIsCompleted = task.IsCompleted;
            if (!taskIsCompleted && millisecondsTimeout == Timeout.Infinite && !cancellationToken.CanBeCanceled)
            {
                taskIsCompleted = task.WrappedTryRunInline() && task.IsCompleted;
            }

            if (!taskIsCompleted)
            {
                // Continue validating and attempting inline execution before waiting.
                hasIncompleteTask = true;
            }
            else
            {
                if (task.IsFaulted)
                {
                    exceptionSeen = true;
                }
                else if (task.IsCanceled)
                {
                    cancellationSeen = true;
                }
                if (task.IsWaitNotificationEnabled)
                {
                    AddToList(task, ref notificationTasks, initSize: 1);
                }
            }
        }

        if (hasIncompleteTask)
        {
            TimeoutHelper.WaitForTimeout(millisecondsTimeout, cancellationToken);
            return false;
        }

        if (notificationTasks is not null)
        {
            foreach (Task task in notificationTasks)
            {
                if (task.NotifyDebuggerOfWaitCompletionIfNecessary())
                {
                    break;
                }
            }
        }

        if (exceptionSeen || cancellationSeen)
        {
            if (!exceptionSeen)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            List<Exception>? exceptions = null;
            foreach (Task task in tasks)
            {
                AddExceptionsForCompletedTask(ref exceptions, task);
            }
            Debug.Assert(exceptions is not null);
            ThrowHelper.ThrowAggregateException(exceptions);
        }

        return true;
    }

    private static int WaitAnyBlockingCore(Task[] tasks, int millisecondsTimeout, CancellationToken cancellationToken)
    {
        TimeoutHelper.WaitForTimeout(millisecondsTimeout, cancellationToken);
        return -1;
    }
}
