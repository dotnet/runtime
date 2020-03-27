// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading
{
    internal partial class PortableThreadPool
    {
        /// <summary>
        /// The worker thread infastructure for the CLR thread pool.
        /// </summary>
        private static class WorkerThread
        {
            /// <summary>
            /// Semaphore for controlling how many threads are currently working.
            /// </summary>
            private static readonly LowLevelLifoSemaphore s_semaphore = new LowLevelLifoSemaphore(0, MaxPossibleThreadCount, SemaphoreSpinCount);

            /// <summary>
            /// Maximum number of spins a thread pool worker thread performs before waiting for work
            /// </summary>
            private static int SemaphoreSpinCount
            {
                get => AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.UnfairSemaphoreSpinLimit", 70, false);
            }

            private static void WorkerThreadStart()
            {
                Thread.CurrentThread.SetThreadPoolWorkerThreadName();

                PortableThreadPoolEventSource log = PortableThreadPoolEventSource.Log;
                if (log.IsEnabled())
                {
                    log.ThreadPoolWorkerThreadStart(ThreadPoolInstance._separated.counts.VolatileRead().NumExistingThreads);
                }

                while (true)
                {
                    while (WaitForRequest())
                    {
                        if (TakeActiveRequest())
                        {
                            Volatile.Write(ref ThreadPoolInstance._separated.lastDequeueTime, Environment.TickCount);
                            if (ThreadPoolWorkQueue.Dispatch())
                            {
                                // If the queue runs out of work for us, we need to update the number of working workers to reflect that we are done working for now
                                RemoveWorkingWorker();
                            }
                        }
                        else
                        {
                            // If we woke up but couldn't find a request, we need to update the number of working workers to reflect that we are done working for now
                            RemoveWorkingWorker();
                        }
                    }

                    ThreadPoolInstance._hillClimbingThreadAdjustmentLock.Acquire();
                    try
                    {
                        // At this point, the thread's wait timed out. We are shutting down this thread.
                        // We are going to decrement the number of exisiting threads to no longer include this one
                        // and then change the max number of threads in the thread pool to reflect that we don't need as many
                        // as we had. Finally, we are going to tell hill climbing that we changed the max number of threads.
                        ThreadCounts counts = ThreadPoolInstance._separated.counts.VolatileRead();
                        while (true)
                        {
                            // Since this thread is currently registered as an existing thread, if more work comes in meanwhile,
                            // this thread would be expected to satisfy the new work. Ensure that NumExistingThreads is not
                            // decreased below NumProcessingWork, as that would be indicative of such a case.
                            short numExistingThreads = counts.NumExistingThreads;
                            if (numExistingThreads <= counts.NumProcessingWork)
                            {
                                // In this case, enough work came in that this thread should not time out and should go back to work.
                                break;
                            }

                            ThreadCounts newCounts = counts;
                            newCounts.SubtractNumExistingThreads(1);
                            short newNumExistingThreads = (short)(numExistingThreads - 1);
                            short newNumThreadsGoal = Math.Max(ThreadPoolInstance._minThreads, Math.Min(newNumExistingThreads, newCounts.NumThreadsGoal));
                            newCounts.NumThreadsGoal = newNumThreadsGoal;

                            ThreadCounts oldCounts = ThreadPoolInstance._separated.counts.InterlockedCompareExchange(newCounts, counts);
                            if (oldCounts == counts)
                            {
                                HillClimbing.ThreadPoolHillClimber.ForceChange(newNumThreadsGoal, HillClimbing.StateOrTransition.ThreadTimedOut);

                                if (log.IsEnabled())
                                {
                                    log.ThreadPoolWorkerThreadStop(newNumExistingThreads);
                                }
                                return;
                            }

                            counts = oldCounts;
                        }
                    }
                    finally
                    {
                        ThreadPoolInstance._hillClimbingThreadAdjustmentLock.Release();
                    }
                }
            }

            /// <summary>
            /// Waits for a request to work.
            /// </summary>
            /// <returns>If this thread was woken up before it timed out.</returns>
            private static bool WaitForRequest()
            {
                PortableThreadPoolEventSource log = PortableThreadPoolEventSource.Log;
                if (log.IsEnabled())
                {
                    log.ThreadPoolWorkerThreadWait(ThreadPoolInstance._separated.counts.VolatileRead().NumExistingThreads);
                }

                return s_semaphore.Wait(ThreadPoolThreadTimeoutMs);
            }

            /// <summary>
            /// Reduce the number of working workers by one, but maybe add back a worker (possibily this thread) if a thread request comes in while we are marking this thread as not working.
            /// </summary>
            private static void RemoveWorkingWorker()
            {
                ThreadCounts currentCounts = ThreadPoolInstance._separated.counts.VolatileRead();
                while (true)
                {
                    ThreadCounts newCounts = currentCounts;
                    newCounts.SubtractNumProcessingWork(1);
                    ThreadCounts oldCounts = ThreadPoolInstance._separated.counts.InterlockedCompareExchange(newCounts, currentCounts);

                    if (oldCounts == currentCounts)
                    {
                        break;
                    }
                    currentCounts = oldCounts;
                }

                // It's possible that we decided we had thread requests just before a request came in,
                // but reduced the worker count *after* the request came in.  In this case, we might
                // miss the notification of a thread request.  So we wake up a thread (maybe this one!)
                // if there is work to do.
                if (ThreadPoolInstance._separated.numRequestedWorkers > 0)
                {
                    MaybeAddWorkingWorker();
                }
            }

            internal static void MaybeAddWorkingWorker()
            {
                ThreadCounts counts = ThreadPoolInstance._separated.counts.VolatileRead();
                short numExistingThreads, numProcessingWork, newNumExistingThreads, newNumProcessingWork;
                while (true)
                {
                    numProcessingWork = counts.NumProcessingWork;
                    if (numProcessingWork >= counts.NumThreadsGoal)
                    {
                        return;
                    }

                    newNumProcessingWork = (short)(numProcessingWork + 1);
                    numExistingThreads = counts.NumExistingThreads;
                    newNumExistingThreads = Math.Max(numExistingThreads, newNumProcessingWork);

                    ThreadCounts newCounts = counts;
                    newCounts.NumProcessingWork = newNumProcessingWork;
                    newCounts.NumExistingThreads = newNumExistingThreads;

                    ThreadCounts oldCounts = ThreadPoolInstance._separated.counts.InterlockedCompareExchange(newCounts, counts);

                    if (oldCounts == counts)
                    {
                        break;
                    }

                    counts = oldCounts;
                }

                int toCreate = newNumExistingThreads - numExistingThreads;
                int toRelease = newNumProcessingWork - numProcessingWork;

                if (toRelease > 0)
                {
                    s_semaphore.Release(toRelease);
                }

                while (toCreate > 0)
                {
                    if (TryCreateWorkerThread())
                    {
                        toCreate--;
                        continue;
                    }

                    counts = ThreadPoolInstance._separated.counts.VolatileRead();
                    while (true)
                    {
                        ThreadCounts newCounts = counts;
                        newCounts.SubtractNumProcessingWork((short)toCreate);
                        newCounts.SubtractNumExistingThreads((short)toCreate);

                        ThreadCounts oldCounts = ThreadPoolInstance._separated.counts.InterlockedCompareExchange(newCounts, counts);
                        if (oldCounts == counts)
                        {
                            break;
                        }
                        counts = oldCounts;
                    }
                    break;
                }
            }

            /// <summary>
            /// Returns if the current thread should stop processing work on the thread pool.
            /// A thread should stop processing work on the thread pool when work remains only when
            /// there are more worker threads in the thread pool than we currently want.
            /// </summary>
            /// <returns>Whether or not this thread should stop processing work even if there is still work in the queue.</returns>
            internal static bool ShouldStopProcessingWorkNow()
            {
                ThreadCounts counts = ThreadPoolInstance._separated.counts.VolatileRead();
                while (true)
                {
                    // When there are more threads processing work than the thread count goal, hill climbing must have decided
                    // to decrease the number of threads. Stop processing if the counts can be updated. We may have more
                    // threads existing than the thread count goal and that is ok, the cold ones will eventually time out if
                    // the thread count goal is not increased again. This logic is a bit different from the original CoreCLR
                    // code from which this implementation was ported, which turns a processing thread into a retired thread
                    // and checks for pending requests like RemoveWorkingWorker. In this implementation there are
                    // no retired threads, so only the count of threads processing work is considered.
                    if (counts.NumProcessingWork <= counts.NumThreadsGoal)
                    {
                        return false;
                    }

                    ThreadCounts newCounts = counts;
                    newCounts.SubtractNumProcessingWork(1);

                    ThreadCounts oldCounts = ThreadPoolInstance._separated.counts.InterlockedCompareExchange(newCounts, counts);

                    if (oldCounts == counts)
                    {
                        return true;
                    }
                    counts = oldCounts;
                }
            }

            private static bool TakeActiveRequest()
            {
                int count = ThreadPoolInstance._separated.numRequestedWorkers;
                while (count > 0)
                {
                    int prevCount = Interlocked.CompareExchange(ref ThreadPoolInstance._separated.numRequestedWorkers, count - 1, count);
                    if (prevCount == count)
                    {
                        return true;
                    }
                    count = prevCount;
                }
                return false;
            }

            private static bool TryCreateWorkerThread()
            {
                try
                {
                    Thread workerThread = new Thread(WorkerThreadStart);
                    workerThread.IsThreadPoolThread = true;
                    workerThread.IsBackground = true;
                    workerThread.Start();
                }
                catch (ThreadStartException)
                {
                    return false;
                }
                catch (OutOfMemoryException)
                {
                    return false;
                }
                return true;
            }
        }
    }
}
