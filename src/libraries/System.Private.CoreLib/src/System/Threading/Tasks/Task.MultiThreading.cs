// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Threading.Tasks
{
    public partial class Task
    {

        // An MRES that gets set when Invoke is called.  This replaces old logic that looked like this:
        //      ManualResetEventSlim mres = new ManualResetEventSlim(false, 0);
        //      Action<Task> completionAction = delegate {mres.Set();}
        //      AddCompletionAction(completionAction);
        // with this:
        //      SetOnInvokeMres mres = new SetOnInvokeMres();
        //      AddCompletionAction(mres, addBeforeOthers: true);
        // which saves a couple of allocations.
        //
        // Used in SpinThenBlockingWait (below), but could be seen as a general purpose mechanism.
        private sealed class SetOnInvokeMres : ManualResetEventSlim, ITaskCompletionAction
        {
            internal SetOnInvokeMres() : base(false, 0) { }
            public void Invoke(Task completingTask) { Set(); }
            public bool InvokeMayRunArbitraryCode => false;
        }

        /// <summary>
        /// Waits for the task to complete, for a timeout to occur, or for cancellation to be requested.
        /// The method first spins and then falls back to blocking on a new event.
        /// </summary>
        /// <param name="millisecondsTimeout">The timeout.</param>
        /// <param name="cancellationToken">The token.</param>
        /// <returns>true if the task is completed; otherwise, false.</returns>
        private bool SpinThenBlockingWait(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            bool infiniteWait = millisecondsTimeout == Timeout.Infinite;
            uint startTimeTicks = infiniteWait ? 0 : (uint)Environment.TickCount;
            bool returnValue = SpinWait(millisecondsTimeout);
            if (!returnValue)
            {
                // We're about to block waiting for the task to complete, which is expensive, and if
                // the task being waited on depends on some other work to run, this thread could end up
                // waiting for some other thread to do work. If the two threads are part of the same scheduler,
                // such as the thread pool, that could lead to a (temporary) deadlock. This is made worse by
                // it also leading to a possible priority inversion on previously queued work. Each thread in
                // the thread pool has a local queue. A key motivator for this local queue is it allows this
                // thread to create work items that it will then prioritize above all other work in the
                // pool. However, while this thread makes its own local queue the top priority, that queue is
                // every other thread's lowest priority. If this thread blocks, all of its created work that's
                // supposed to be high priority becomes low priority, and work that's typically part of a
                // currently in-flight operation gets deprioritized relative to new requests coming into the
                // pool, which can lead to the whole system slowing down or even deadlocking. To address that,
                // just before we block, we move all local work into a global queue, so that it's at least
                // prioritized by other threads more fairly with respect to other work.
                ThreadPoolWorkQueue.TransferAllLocalWorkItemsToHighPriorityGlobalQueue();

                var mres = new SetOnInvokeMres();
                try
                {
                    AddCompletionAction(mres, addBeforeOthers: true);
#pragma warning disable CA1416 // Validate platform compatibility, issue: https://github.com/dotnet/runtime/issues/44622
                    if (infiniteWait)
                    {
                        bool notifyWhenUnblocked = ThreadPool.NotifyThreadBlocked();
                        try
                        {
                            returnValue = mres.Wait(Timeout.Infinite, cancellationToken);
                        }
                        finally
                        {
                            if (notifyWhenUnblocked)
                            {
                                ThreadPool.NotifyThreadUnblocked();
                            }
                        }
                    }
                    else
                    {
                        uint elapsedTimeTicks = ((uint)Environment.TickCount) - startTimeTicks;
                        if (elapsedTimeTicks < millisecondsTimeout)
                        {
                            bool notifyWhenUnblocked = ThreadPool.NotifyThreadBlocked();
                            try
                            {
                                returnValue = mres.Wait((int)(millisecondsTimeout - elapsedTimeTicks), cancellationToken);
                            }
                            finally
                            {
                                if (notifyWhenUnblocked)
                                {
                                    ThreadPool.NotifyThreadUnblocked();
                                }
                            }
                        }
                    }
#pragma warning restore CA1416
                }
                finally
                {
                    if (!IsCompleted) RemoveContinuation(mres);
                    // Don't Dispose of the MRES, because the continuation off of this task may
                    // still be running.  This is ok, however, as we never access the MRES' WaitHandle,
                    // and thus no finalizable resources are actually allocated.
                }
            }
            return returnValue;
        }

        /// <summary>
        /// Spins briefly while checking IsCompleted
        /// </summary>
        /// <param name="millisecondsTimeout">The timeout.</param>
        /// <returns>true if the task is completed; otherwise, false.</returns>
        /// <exception cref="OperationCanceledException">The wait was canceled.</exception>
        private bool SpinWait(int millisecondsTimeout)
        {
            if (IsCompleted) return true;

            Debug.Assert(RuntimeFeature.IsMultithreadingSupported);

            if (millisecondsTimeout == 0)
            {
                // For 0-timeouts, we just return immediately.
                return false;
            }

            int spinCount = Threading.SpinWait.SpinCountForSpinBeforeWait;
            SpinWait spinner = default;
            while (spinner.Count < spinCount)
            {
                spinner.SpinOnce(sleep1Threshold: -1);

                if (IsCompleted)
                {
                    return true;
                }
            }

            return false;
        }

        // Separated out to allow it to be optimized (caller is marked NoOptimization for VS parallel debugger
        // to be able to see the method on the stack and inspect arguments).
        [UnsupportedOSPlatform("browser")]
        private static bool WaitAllCore(ReadOnlySpan<Task> tasks, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            if (millisecondsTimeout < -1)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.millisecondsTimeout);
            }

            cancellationToken.ThrowIfCancellationRequested(); // early check before we make any allocations

            //
            // In this WaitAll() implementation we have 2 alternate code paths for a task to be handled:
            // CODEPATH1: skip an already completed task, CODEPATH2: actually wait on tasks
            // We make sure that the exception behavior of Task.Wait() is replicated the same for tasks handled in either of these codepaths
            //

            List<Exception>? exceptions = null;
            List<Task>? waitedOnTaskList = null;
            List<Task>? notificationTasks = null;

            // If any of the waited-upon tasks end as Faulted or Canceled, set these to true.
            bool exceptionSeen = false, cancellationSeen = false;

            // Collects incomplete tasks in "waitedOnTaskList"
            for (int i = tasks.Length - 1; i >= 0; i--)
            {
                Task task = tasks[i];

                if (task == null)
                {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Task_WaitMulti_NullTask, ExceptionArgument.tasks);
                }

                bool taskIsCompleted = task.IsCompleted;
                if (!taskIsCompleted)
                {
                    // try inlining the task only if we have an infinite timeout and an empty cancellation token
                    if (millisecondsTimeout != Timeout.Infinite || cancellationToken.CanBeCanceled)
                    {
                        // We either didn't attempt inline execution because we had a non-infinite timeout or we had a cancellable token.
                        // In all cases we need to do a full wait on the task (=> add its event into the list.)
                        AddToList(task, ref waitedOnTaskList, initSize: tasks.Length);
                    }
                    else
                    {
                        // We are eligible for inlining.  If it doesn't work, we'll do a full wait.
                        taskIsCompleted = task.WrappedTryRunInline() && task.IsCompleted; // A successful TryRunInline doesn't guarantee completion
                        if (!taskIsCompleted) AddToList(task, ref waitedOnTaskList, initSize: tasks.Length);
                    }
                }

                if (taskIsCompleted)
                {
                    if (task.IsFaulted) exceptionSeen = true;
                    else if (task.IsCanceled) cancellationSeen = true;
                    if (task.IsWaitNotificationEnabled) AddToList(task, ref notificationTasks, initSize: 1);
                }
            }

            if (waitedOnTaskList != null)
            {
                // Block waiting for the tasks to complete.
                if (!WaitAllBlockingCore(waitedOnTaskList, millisecondsTimeout, cancellationToken))
                {
                    return false;
                }

                // The wait didn't time out, so ensure exceptions are propagated, and if a debugger is
                // attached and one of these tasks requires it, that we notify the debugger of a wait completion.
                // Add any exceptions for this task to the collection, and if its wait
                // notification bit is set, store it to operate on at the end.
                foreach (Task task in waitedOnTaskList)
                {
                    if (task.IsFaulted) exceptionSeen = true;
                    else if (task.IsCanceled) cancellationSeen = true;
                    if (task.IsWaitNotificationEnabled) AddToList(task, ref notificationTasks, initSize: 1);
                }
            }

            // Now that we're done and about to exit, if we have
            // any tasks with a notification bit set, signal the debugger if any requires it.
            if (notificationTasks != null)
            {
                // Loop through each task tha that had its bit set, and notify the debugger
                // about the first one that requires it.  The debugger will reset the bit
                // for any tasks we don't notify of as soon as we break, so we only need to notify
                // for one.
                foreach (Task task in notificationTasks)
                {
                    if (task.NotifyDebuggerOfWaitCompletionIfNecessary()) break;
                }
            }

            // If one or more threw exceptions, aggregate and throw them.
            if (exceptionSeen || cancellationSeen)
            {
                // If the WaitAll was canceled and tasks were canceled but not faulted,
                // prioritize throwing an OCE for canceling the WaitAll over throwing an
                // AggregateException for all of the canceled Tasks.  This helps
                // to bring determinism to an otherwise non-determistic case of using
                // the same token to cancel both the WaitAll and the Tasks.
                if (!exceptionSeen) cancellationToken.ThrowIfCancellationRequested();

                // Now gather up and throw all of the exceptions.
                foreach (Task task in tasks) AddExceptionsForCompletedTask(ref exceptions, task);
                Debug.Assert(exceptions != null, "Should have seen at least one exception");
                ThrowHelper.ThrowAggregateException(exceptions);
            }

            return true;
        }

        /// <summary>Performs a blocking WaitAll on the vetted list of tasks.</summary>
        /// <param name="tasks">The tasks, which have already been checked and filtered for completion.</param>
        /// <param name="millisecondsTimeout">The timeout.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>true if all of the tasks completed; otherwise, false.</returns>
        [UnsupportedOSPlatform("browser")]
        private static bool WaitAllBlockingCore(List<Task> tasks, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            Debug.Assert(tasks != null, "Expected a non-null list of tasks");
            Debug.Assert(tasks.Count > 0, "Expected at least one task");
            Debug.Assert(RuntimeFeature.IsMultithreadingSupported);

            bool waitCompleted = false;
            var mres = new SetOnCountdownMres(tasks.Count);
            try
            {
                foreach (Task task in tasks)
                {
                    task.AddCompletionAction(mres, addBeforeOthers: true);
                }
                waitCompleted = mres.Wait(millisecondsTimeout, cancellationToken);
            }
            finally
            {
                if (!waitCompleted)
                {
                    foreach (Task task in tasks)
                    {
                        if (!task.IsCompleted) task.RemoveContinuation(mres);
                    }
                }
                // It's ok that we don't dispose of the MRES here, as we never
                // access the MRES' WaitHandle, and thus no finalizable resources
                // are actually created.  We don't always just Dispose it because
                // a continuation that's accessing the MRES could still be executing.
            }
            return waitCompleted;
        }

        // A ManualResetEventSlim that will get Set after Invoke is called count times.
        // This allows us to replace this logic:
        //      var mres = new ManualResetEventSlim(tasks.Count);
        //      Action<Task> completionAction = delegate { if (Interlocked.Decrement(ref count) == 0) mres.Set(); };
        //      foreach (var task in tasks) task.AddCompletionAction(completionAction);
        // with this logic:
        //      var mres = new SetOnCountdownMres(tasks.Count);
        //      foreach (var task in tasks) task.AddCompletionAction(mres);
        // which saves a couple of allocations.
        //
        // Used in WaitAllBlockingCore (above).
        private sealed class SetOnCountdownMres : ManualResetEventSlim, ITaskCompletionAction
        {
            private int _count;

            internal SetOnCountdownMres(int count)
            {
                Debug.Assert(count > 0, "Expected count > 0");
                _count = count;
            }

            public void Invoke(Task completingTask)
            {
                if (Interlocked.Decrement(ref _count) == 0) Set();
                Debug.Assert(_count >= 0, "Count should never go below 0");
            }

            public bool InvokeMayRunArbitraryCode => false;
        }

        private static int WaitAnyBlockingCore(Task[] tasks, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            Task<Task> firstCompleted = TaskFactory.CommonCWAnyLogic(tasks, isSyncBlocking: true);
            bool waitCompleted = firstCompleted.Wait(millisecondsTimeout, cancellationToken);
            int signaledTaskIndex = -1;
            if (waitCompleted)
            {
                Debug.Assert(firstCompleted.Status == TaskStatus.RanToCompletion);
                signaledTaskIndex = Array.IndexOf(tasks, firstCompleted.Result);
                Debug.Assert(signaledTaskIndex >= 0);
            }
            else
            {
                TaskFactory.CommonCWAnyLogicCleanup(firstCompleted);
            }

            return signaledTaskIndex;
        }

    }
}
