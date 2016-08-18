// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DPStressHarness
{
    public class DeadlockDetectionTaskScheduler : TaskScheduler
    {
        private readonly WaitCallback _runTaskCallback;
        private readonly ParameterizedThreadStart _runTaskThreadStart;
#if DEBUG
        private readonly ConcurrentDictionary<Task, object> _queuedItems = new ConcurrentDictionary<Task, object>();
#endif

        public DeadlockDetectionTaskScheduler()
        {
            _runTaskCallback = new WaitCallback(RunTask);
            _runTaskThreadStart = new ParameterizedThreadStart(RunTask);
        }

        // This is only used for debugging, so for retail we'd prefer the perf
        protected override IEnumerable<Task> GetScheduledTasks()
        {
#if DEBUG
            return _queuedItems.Keys;
#else
            return new Task[0];
#endif
        }

        protected override void QueueTask(Task task)
        {
            if ((task.CreationOptions & TaskCreationOptions.LongRunning) == TaskCreationOptions.LongRunning)
            {
                // Create a new background thread for long running tasks
                Thread thread = new Thread(_runTaskThreadStart) { IsBackground = true };
                thread.Start(task);
            }
            else
            {
                // Otherwise queue the work on the threadpool
#if DEBUG
                _queuedItems.TryAdd(task, null);
#endif

                ThreadPool.QueueUserWorkItem(_runTaskCallback, task);
            }
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (!taskWasPreviouslyQueued)
            {
                // Run the task inline
                RunTask(task);
                return true;
            }

            // Couldn't run the task
            return false;
        }

        private void RunTask(object state)
        {
            Task inTask = state as Task;

#if DEBUG
            // Remove from the dictionary of queued items
            object ignored;
            _queuedItems.TryRemove(inTask, out ignored);
#endif

            // Note when the thread started work
            DeadlockDetection.AddTaskThread();

            try
            {
                // Run the task
                base.TryExecuteTask(inTask);
            }
            finally
            {
                // Remove the thread from the list when complete
                DeadlockDetection.RemoveThread();
            }
        }
    }
}
