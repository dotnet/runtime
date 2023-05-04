// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;

namespace System.Threading
{
    internal sealed partial class PortableThreadPool
    {
        /// <summary>
        /// The worker thread infastructure for the CLR thread pool.
        /// </summary>
        private static partial class WorkerThread
        {

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

                while (true)
                {
                    bool spinWait = true;
                    while (semaphore.Wait(ThreadPoolThreadTimeoutMs, spinWait))
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
