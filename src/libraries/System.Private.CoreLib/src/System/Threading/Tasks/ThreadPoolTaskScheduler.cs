// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// TaskScheduler.cs
//
//
// This file contains the primary interface and management of tasks and queues.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Threading.Tasks
{
    /// <summary>
    /// An implementation of TaskScheduler that uses the ThreadPool scheduler
    /// </summary>
    internal sealed class ThreadPoolTaskScheduler : TaskScheduler
    {
        /// <summary>
        /// Constructs a new ThreadPool task scheduler object
        /// </summary>
        internal ThreadPoolTaskScheduler()
        {
            _ = base.Id; // force ID creation of the default scheduler
        }

        // static delegate for threads allocated to handle LongRunning tasks.
        private static readonly ParameterizedThreadStart s_longRunningThreadWork = static s =>
        {
            Debug.Assert(s is Task);
            ((Task)s).ExecuteEntryUnsafe(threadPoolThread: null);
        };

        /// <summary>
        /// Schedules a task to the ThreadPool.
        /// </summary>
        /// <param name="task">The task to schedule.</param>
        protected internal override void QueueTask(Task task)
        {
            TaskCreationOptions options = task.Options;
            if (Thread.IsThreadStartSupported && (options & TaskCreationOptions.LongRunning) != 0)
            {
                // Run LongRunning tasks on their own dedicated thread.
                new Thread(s_longRunningThreadWork)
                {
                    IsBackground = true,
                    Name = ".NET Long Running Task"
                }.UnsafeStart(task);
            }
            else
            {
                // Normal handling for non-LongRunning tasks.
                ThreadPool.UnsafeQueueUserWorkItemInternal(task, (options & TaskCreationOptions.PreferFairness) == 0);
            }
        }

        /// <summary>
        /// This internal function will do this:
        ///   (1) If the task had previously been queued, attempt to pop it and return false if that fails.
        ///   (2) Return whether the task is executed
        ///
        /// IMPORTANT NOTE: TryExecuteTaskInline will NOT throw task exceptions itself. Any wait code path using this function needs
        /// to account for exceptions that need to be propagated, and throw themselves accordingly.
        /// </summary>
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            // If the task was previously scheduled, and we can't pop it, then return false.
            if (taskWasPreviouslyQueued && !ThreadPool.TryPopCustomWorkItem(task))
                return false;

            try
            {
                task.ExecuteEntryUnsafe(threadPoolThread: null); // handles switching Task.Current etc.
            }
            finally
            {
                // Only call NWIP() if task was previously queued
                if (taskWasPreviouslyQueued) NotifyWorkItemProgress();
            }

            return true;
        }

        protected internal override bool TryDequeue(Task task)
        {
            // just delegate to TP
            return ThreadPool.TryPopCustomWorkItem(task);
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return FilterTasksFromWorkItems(ThreadPool.GetQueuedWorkItems());
        }

        private static IEnumerable<Task> FilterTasksFromWorkItems(IEnumerable<object> tpwItems)
        {
            foreach (object tpwi in tpwItems)
            {
                if (tpwi is Task t)
                {
                    yield return t;
                }
            }
        }

        /// <summary>
        /// Notifies the scheduler that work is progressing (no-op).
        /// </summary>
        internal override void NotifyWorkItemProgress()
        {
            ThreadPool.NotifyWorkItemProgress();
        }

        /// <summary>
        /// This is the only scheduler that returns false for this property, indicating that the task entry codepath is unsafe (CAS free)
        /// since we know that the underlying scheduler already takes care of atomic transitions from queued to non-queued.
        /// </summary>
        internal override bool RequiresAtomicStartTransition => false;
    }
}
