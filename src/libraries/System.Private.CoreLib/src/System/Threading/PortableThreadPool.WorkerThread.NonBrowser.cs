// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;

namespace System.Threading
{
    internal sealed partial class PortableThreadPool
    {
        private int _numThreadsBeingKeptAlive;

        /// <summary>
        /// The worker thread infastructure for the CLR thread pool.
        /// </summary>
        private static partial class WorkerThread
        {
            private static readonly short ThreadsToKeepAlive = DetermineThreadsToKeepAlive();

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
                    0,
                    MaxPossibleThreadCount,
                    AppContextConfigHelper.GetInt32Config(
                        "System.Threading.ThreadPool.UnfairSemaphoreSpinLimit",
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

            private static void WorkerThreadStart()
            {
                Thread.CurrentThread.SetThreadPoolWorkerThreadName();

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
                    bool spinWait = true;
                    while (semaphore.Wait(timeoutMs, spinWait))
                    {
                        WorkerDoWork(threadPoolInstance, ref spinWait);
                    }

                    if (ShouldExitWorker(threadPoolInstance, threadAdjustmentLock))
                    {
                        break;
                    }
                }
            }

            private static void CreateWorkerThread()
            {
                // Thread pool threads must start in the default execution context without transferring the context, so
                // using UnsafeStart() instead of Start()
                Thread workerThread = new Thread(s_workerThreadStart);
                workerThread.IsThreadPoolThread = true;
                workerThread.IsBackground = true;
                // thread name will be set in thread proc
                workerThread.UnsafeStart();
            }
        }
    }
}
