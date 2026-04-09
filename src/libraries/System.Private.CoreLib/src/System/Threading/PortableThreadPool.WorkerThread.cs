// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    internal sealed partial class PortableThreadPool
    {
        /// <summary>
        /// The worker thread infastructure for the CLR thread pool.
        /// </summary>
        private static partial class WorkerThread
        {
            private static readonly short ThreadsToKeepAlive = DetermineThreadsToKeepAlive();

            // Spinning in the threadpool semaphore is not always useful.
            // For example the new workitems may be produced by non-pool threads and could only arrive if pool threads start blocking.
            // We will limit spinning to roughly 512-1024 spinwaits, each taking 35-50ns. That should be under 50 usec total.
            // For reference the wakeup latency of a futex/event with threads queued up is reported to be in 5-50 usec range. (year 2025)
            private const int SemaphoreSpinCountDefault = 9;

            // This value represents an assumption of how much uncommitted stack space a worker thread may use in the future.
            // Used in calculations to estimate when to throttle the rate of thread injection to reduce the possibility of
            // preexisting threads from running out of memory when using new stack space in low-memory situations.
            public const int EstimatedAdditionalStackUsagePerThreadBytes = 64 << 10; // 64 KB

            private static short DetermineThreadsToKeepAlive()
            {
                const short DefaultThreadsToKeepAlive = 0;

                // The number of worker threads to keep alive after they are created. Set to -1 to keep all created worker
                // threads alive. When the ThreadTimeoutMs config value is also set, for worker threads the timeout applies to
                // worker threads that are in excess of the number configured for ThreadsToKeepAlive.
                short threadsToKeepAlive =
                    AppContextConfigHelper.GetInt16Config(
                        "System.Threading.ThreadPool.ThreadsToKeepAlive",
                        "DOTNET_ThreadPool_ThreadsToKeepAlive",
                        DefaultThreadsToKeepAlive);
                return threadsToKeepAlive >= -1 ? threadsToKeepAlive : DefaultThreadsToKeepAlive;
            }

            /// <summary>
            /// Semaphore for controlling how many threads are currently working.
            /// </summary>
            private static readonly LowLevelLifoSemaphore s_semaphore =
                new LowLevelLifoSemaphore(
                    MaxPossibleThreadCount,
                    (uint)AppContextConfigHelper.GetInt32ComPlusOrDotNetConfig(
                        "System.Threading.ThreadPool.UnfairSemaphoreSpinLimit",
                        "ThreadPool_UnfairSemaphoreSpinLimit",
                        SemaphoreSpinCountDefault,
                        false),
                    onWait: () =>
                    {
                        if (NativeRuntimeEventSource.Log.IsEnabled())
                        {
                            NativeRuntimeEventSource.Log.ThreadPoolWorkerThreadWait(
                                (uint)ThreadPoolInstance._separated.counts.VolatileRead().NumExistingThreads);
                        }
                    });

            private static readonly ThreadStart s_workerThreadStart = WorkerThreadStart;

            private static void CreateWorkerThread()
            {
                // Thread pool threads must start in the default execution context without transferring the context, so
                // using UnsafeStart() instead of Start()
                Thread workerThread = new Thread(s_workerThreadStart);
                workerThread.IsThreadPoolThread = true;
                workerThread.IsBackground = true;
                workerThread.SetThreadPoolWorkerThreadName();
                workerThread.UnsafeStart();
            }

            private static void WorkerThreadStart()
            {
                PortableThreadPool threadPoolInstance = ThreadPoolInstance;

                if (NativeRuntimeEventSource.Log.IsEnabled())
                {
                    NativeRuntimeEventSource.Log.ThreadPoolWorkerThreadStart(
                        (uint)threadPoolInstance._separated.counts.VolatileRead().NumExistingThreads);
                }

                LowLevelLock threadAdjustmentLock = threadPoolInstance._threadAdjustmentLock;
                LowLevelLifoSemaphore semaphore = s_semaphore;

                // Determine the idle timeout to use for this thread. Some threads may always be kept alive based on config.
                int timeoutMs = ThreadPoolThreadTimeoutMs;
                if (ThreadsToKeepAlive != 0)
                {
                    if (ThreadsToKeepAlive < 0)
                    {
                        timeoutMs = Timeout.Infinite;
                    }
                    else
                    {
                        int count = threadPoolInstance._numThreadsBeingKeptAlive;
                        while (count < ThreadsToKeepAlive)
                        {
                            int countBeforeUpdate =
                                Interlocked.CompareExchange(ref threadPoolInstance._numThreadsBeingKeptAlive, count + 1, count);
                            if (countBeforeUpdate == count)
                            {
                                timeoutMs = Timeout.Infinite;
                                break;
                            }

                            count = countBeforeUpdate;
                        }
                    }
                }

                while (true)
                {
                    while (semaphore.Wait(timeoutMs))
                    {
                        WorkerDoWork(threadPoolInstance);
                    }

                    // We've timed out waiting on the semaphore. Time to exit.
                    // In rare cases we may be asked to keep running/waiting.
                    if (ShouldExitWorker(threadPoolInstance, threadAdjustmentLock))
                    {
                        break;
                    }
                }
            }

            private static void WorkerDoWork(PortableThreadPool threadPoolInstance)
            {
                do
                {
                    // We generally avoid spurious wakes as they are wasteful, so we nearly always should see a request.
                    // However, we allow external wakes when thread goals change, which can result in "stolen" requests,
                    // thus sometimes there is no active request and we need to check.
                    if (threadPoolInstance._separated._hasOutstandingThreadRequest != 0 &&
                        Interlocked.Exchange(ref threadPoolInstance._separated._hasOutstandingThreadRequest, 0) != 0)
                    {
                        // We took the request, now we must Dispatch some work items.
                        threadPoolInstance.NotifyDispatchProgress(Environment.TickCount);
                        if (!ThreadPoolWorkQueue.Dispatch())
                        {
                            // We are above goal and would have already removed this working worker in the counts.
                            return;
                        }
                    }

                    // We could not find more work in the queue and will try to stop being active.
                    // One caveat - in Saturated state we have seen a thread request but could not signal for a worker
                    // to come and see to it. Thus in Saturated state, one thread will clear the state and will come
                    // back for another try to clear the thread request and do Dispatch - without consuming a signal.
                    // See `TryIncrementProcessingWork` for details about Saturated state.
                } while (!TryRemoveWorkingWorker(threadPoolInstance));
            }

            // returns true if the worker is shutting down
            // returns false if we should do another iteration
            private static bool ShouldExitWorker(PortableThreadPool threadPoolInstance, LowLevelLock threadAdjustmentLock)
            {
                // The thread cannot exit if it has IO pending, otherwise the IO may be canceled
                if (IsIOPending)
                {
                    return false;
                }

                threadAdjustmentLock.Acquire();
                try
                {
                    // At this point, the thread's wait timed out. We are shutting down this thread.
                    // We are going to decrement the number of existing threads to no longer include this one
                    // and then change the max number of threads in the thread pool to reflect that we don't need as many
                    // as we had. Finally, we are going to tell hill climbing that we changed the max number of threads.
                    ThreadCounts counts = threadPoolInstance._separated.counts;
                    while (true)
                    {
                        // Since this thread is currently registered as an existing thread, if more work comes in meanwhile,
                        // this thread would be expected to satisfy the new work. Ensure that NumExistingThreads is not
                        // decreased below NumProcessingWork, as that would be indicative of such a case.
                        if (counts.NumExistingThreads <= counts.NumProcessingWork)
                        {
                            // In this case, enough work came in that this thread should not time out and should go back to work.
                            return false;
                        }

                        ThreadCounts newCounts = counts;
                        short newNumExistingThreads = --newCounts.NumExistingThreads;
                        short newNumThreadsGoal =
                            Math.Max(
                                threadPoolInstance.MinThreadsGoal,
                                Math.Min(newNumExistingThreads, counts.NumThreadsGoal));
                        newCounts.NumThreadsGoal = newNumThreadsGoal;

                        ThreadCounts oldCounts =
                            threadPoolInstance._separated.counts.InterlockedCompareExchange(newCounts, counts);
                        if (oldCounts == counts)
                        {
                            HillClimbing.ThreadPoolHillClimber.ForceChange(
                                newNumThreadsGoal,
                                HillClimbing.StateOrTransition.ThreadTimedOut);
                            if (NativeRuntimeEventSource.Log.IsEnabled())
                            {
                                NativeRuntimeEventSource.Log.ThreadPoolWorkerThreadStop((uint)newNumExistingThreads);
                            }
                            return true;
                        }

                        counts = oldCounts;
                    }
                }
                finally
                {
                    threadAdjustmentLock.Release();
                }
            }

            /// <summary>
            /// Tries to reduce the number of working workers by one.
            /// If we are in a Saturated state, clears the state instead and returns false.
            /// Returns true if number of active threads was actually reduced.
            /// See `TryDecrementProcessingWork` for details about Saturated state.
            /// </summary>
            private static bool TryRemoveWorkingWorker(PortableThreadPool threadPoolInstance)
            {
                uint collisionCount = 0;
                while (true)
                {
                    ThreadCounts oldCounts = threadPoolInstance._separated.counts;
                    ThreadCounts newCounts = oldCounts;
                    bool decremented = newCounts.TryDecrementProcessingWork();
                    if (threadPoolInstance._separated.counts.InterlockedCompareExchange(newCounts, oldCounts) == oldCounts)
                    {
                        return decremented;
                    }

                    // This can be fairly contentious when threadpool runs out of work and all threads try to leave.
                    Backoff.Exponential(collisionCount++);
                }
            }

            /// In Saturated state does nothing.
            /// Otherwise increments the active worker count and signals the semaphore.
            /// Incrementing the count turns on the Saturated state if the active thread limit is reached.
            /// See `TryIncrementProcessingWork` for details about Saturated state.
            internal static void MaybeAddWorkingWorker(PortableThreadPool threadPoolInstance)
            {
                ThreadCounts oldCounts, newCounts;
                bool incremented;
                uint collisionCount = 0;
                while (true)
                {
                    oldCounts = threadPoolInstance._separated.counts;
                    newCounts = oldCounts;
                    incremented = newCounts.TryIncrementProcessingWork();
                    newCounts.NumExistingThreads = Math.Max(newCounts.NumProcessingWork, newCounts.NumExistingThreads);
                    if (threadPoolInstance._separated.counts.InterlockedCompareExchange(newCounts, oldCounts) == oldCounts)
                    {
                        break;
                    }

                    // This is less contentious than Remove as reasons to add threads are more complex to avoid adding too many too fast.
                    // We can still see some amount of failed interlocked operations here when a burst of work is scheduled.
                    Backoff.Exponential(collisionCount++);
                }

                if (!incremented)
                {
                    return;
                }

                Debug.Assert(newCounts.NumProcessingWork - oldCounts.NumProcessingWork == 1);
                s_semaphore.Signal();

                int toCreate = newCounts.NumExistingThreads - oldCounts.NumExistingThreads;
                Debug.Assert(toCreate == 0 || toCreate == 1);
                if (toCreate != 0)
                {
                    CreateWorkerThread();
                }
            }

            /// <summary>
            /// Returns if the current thread should stop processing work on the thread pool.
            /// A thread should stop processing work on the thread pool when work remains only when
            /// there are more worker threads in the thread pool than we currently want.
            /// </summary>
            /// <returns>Whether or not this thread should stop processing work even if there is still work in the queue.</returns>
            internal static bool ShouldStopProcessingWorkNow(PortableThreadPool threadPoolInstance)
            {
                ThreadCounts counts = threadPoolInstance._separated.counts;
                while (true)
                {
                    // When there are more threads processing work than the thread count goal, it may have been decided
                    // to decrease the number of threads. Stop processing if the counts can be updated. We may have more
                    // threads existing than the thread count goal and that is ok, the cold ones will eventually time out if
                    // the thread count goal is not increased again.
                    if (counts.NumProcessingWork <= counts.NumThreadsGoal)
                    {
                        return false;
                    }

                    ThreadCounts newCounts = counts;
                    newCounts.NumProcessingWork--;

                    ThreadCounts oldCounts = threadPoolInstance._separated.counts.InterlockedCompareExchange(newCounts, counts);

                    if (oldCounts == counts)
                    {
                        return true;
                    }
                    counts = oldCounts;
                }
            }
        }
    }
}
