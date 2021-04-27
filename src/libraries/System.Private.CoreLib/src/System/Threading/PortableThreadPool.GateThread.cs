// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Threading
{
    internal sealed partial class PortableThreadPool
    {
        private static class GateThread
        {
            private const int GateThreadDelayMs = 500;
            private const int DequeueDelayThresholdMs = GateThreadDelayMs * 2;
            private const int GateThreadRunningMask = 0x4;

            private static readonly AutoResetEvent s_runGateThreadEvent = new AutoResetEvent(initialState: true);

            private const int MaxRuns = 2;

            private static void GateThreadStart()
            {
                bool disableStarvationDetection =
                    AppContextConfigHelper.GetBooleanConfig("System.Threading.ThreadPool.DisableStarvationDetection", false);
                bool debuggerBreakOnWorkStarvation =
                    AppContextConfigHelper.GetBooleanConfig("System.Threading.ThreadPool.DebugBreakOnWorkerStarvation", false);

                // The first reading is over a time range other than what we are focusing on, so we do not use the read other
                // than to send it to any runtime-specific implementation that may also use the CPU utilization.
                CpuUtilizationReader cpuUtilizationReader = default;
                _ = cpuUtilizationReader.CurrentUtilization;

                PortableThreadPool threadPoolInstance = ThreadPoolInstance;
                LowLevelLock hillClimbingThreadAdjustmentLock = threadPoolInstance._hillClimbingThreadAdjustmentLock;

                while (true)
                {
                    s_runGateThreadEvent.WaitOne();

                    bool needGateThreadForRuntime;
                    do
                    {
                        Thread.Sleep(GateThreadDelayMs);

                        if (ThreadPool.EnableWorkerTracking && NativeRuntimeEventSource.Log.IsEnabled())
                        {
                            NativeRuntimeEventSource.Log.ThreadPoolWorkingThreadCount(
                                (uint)threadPoolInstance.GetAndResetHighWatermarkCountOfThreadsProcessingUserCallbacks());
                        }

                        int cpuUtilization = cpuUtilizationReader.CurrentUtilization;
                        threadPoolInstance._cpuUtilization = cpuUtilization;

                        needGateThreadForRuntime = ThreadPool.PerformRuntimeSpecificGateActivities(cpuUtilization);

                        if (!disableStarvationDetection &&
                            threadPoolInstance._separated.numRequestedWorkers > 0 &&
                            SufficientDelaySinceLastDequeue(threadPoolInstance))
                        {
                            try
                            {
                                hillClimbingThreadAdjustmentLock.Acquire();
                                ThreadCounts counts = threadPoolInstance._separated.counts.VolatileRead();

                                // Don't add a thread if we're at max or if we are already in the process of adding threads.
                                // This logic is slightly different from the native implementation in CoreCLR because there are
                                // no retired threads. In the native implementation, when hill climbing reduces the thread count
                                // goal, threads that are stopped from processing work are switched to "retired" state, and they
                                // don't count towards the equivalent existing thread count. In this implementation, the
                                // existing thread count includes any worker thread that has not yet exited, including those
                                // stopped from working by hill climbing, so here the number of threads processing work, instead
                                // of the number of existing threads, is compared with the goal. There may be alternative
                                // solutions, for now this is only to maintain consistency in behavior.
                                while (
                                    counts.NumExistingThreads < threadPoolInstance._maxThreads &&
                                    counts.NumProcessingWork >= counts.NumThreadsGoal)
                                {
                                    if (debuggerBreakOnWorkStarvation)
                                    {
                                        Debugger.Break();
                                    }

                                    ThreadCounts newCounts = counts;
                                    short newNumThreadsGoal = (short)(counts.NumProcessingWork + 1);
                                    newCounts.NumThreadsGoal = newNumThreadsGoal;

                                    ThreadCounts oldCounts = threadPoolInstance._separated.counts.InterlockedCompareExchange(newCounts, counts);
                                    if (oldCounts == counts)
                                    {
                                        HillClimbing.ThreadPoolHillClimber.ForceChange(newNumThreadsGoal, HillClimbing.StateOrTransition.Starvation);
                                        WorkerThread.MaybeAddWorkingWorker(threadPoolInstance);
                                        break;
                                    }

                                    counts = oldCounts;
                                }
                            }
                            finally
                            {
                                hillClimbingThreadAdjustmentLock.Release();
                            }
                        }
                    } while (
                        needGateThreadForRuntime ||
                        threadPoolInstance._separated.numRequestedWorkers > 0 ||
                        Interlocked.Decrement(ref threadPoolInstance._separated.gateThreadRunningState) > GetRunningStateForNumRuns(0));
                }
            }

            // called by logic to spawn new worker threads, return true if it's been too long
            // since the last dequeue operation - takes number of worker threads into account
            // in deciding "too long"
            private static bool SufficientDelaySinceLastDequeue(PortableThreadPool threadPoolInstance)
            {
                int delay = Environment.TickCount - Volatile.Read(ref threadPoolInstance._separated.lastDequeueTime);

                int minimumDelay;

                if (threadPoolInstance._cpuUtilization < CpuUtilizationLow)
                {
                    minimumDelay = GateThreadDelayMs;
                }
                else
                {
                    ThreadCounts counts = threadPoolInstance._separated.counts.VolatileRead();
                    int numThreads = counts.NumThreadsGoal;
                    minimumDelay = numThreads * DequeueDelayThresholdMs;
                }
                return delay > minimumDelay;
            }

            // This is called by a worker thread
            internal static void EnsureRunning(PortableThreadPool threadPoolInstance)
            {
                // The callers ensure that this speculative load is sufficient to ensure that the gate thread is activated when
                // it is needed
                if (threadPoolInstance._separated.gateThreadRunningState != GetRunningStateForNumRuns(MaxRuns))
                {
                    EnsureRunningSlow(threadPoolInstance);
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal static void EnsureRunningSlow(PortableThreadPool threadPoolInstance)
            {
                int numRunsMask = Interlocked.Exchange(ref threadPoolInstance._separated.gateThreadRunningState, GetRunningStateForNumRuns(MaxRuns));
                if (numRunsMask == GetRunningStateForNumRuns(0))
                {
                    s_runGateThreadEvent.Set();
                }
                else if ((numRunsMask & GateThreadRunningMask) == 0)
                {
                    CreateGateThread(threadPoolInstance);
                }
            }

            private static int GetRunningStateForNumRuns(int numRuns)
            {
                Debug.Assert(numRuns >= 0);
                Debug.Assert(numRuns <= MaxRuns);
                return GateThreadRunningMask | numRuns;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void CreateGateThread(PortableThreadPool threadPoolInstance)
            {
                bool created = false;
                try
                {
                    // Thread pool threads must start in the default execution context without transferring the context, so
                    // using UnsafeStart() instead of Start()
                    Thread gateThread = new Thread(GateThreadStart, SmallStackSizeBytes)
                    {
                        IsThreadPoolThread = true,
                        IsBackground = true,
                        Name = ".NET ThreadPool Gate"
                    };
                    gateThread.UnsafeStart();
                    created = true;
                }
                finally
                {
                    if (!created)
                    {
                        Interlocked.Exchange(ref threadPoolInstance._separated.gateThreadRunningState, 0);
                    }
                }
            }
        }

        internal static void EnsureGateThreadRunning() => GateThread.EnsureRunning(ThreadPoolInstance);
    }
}
