// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Threading
{
    internal partial class PortableThreadPool
    {
        private static class GateThread
        {
            private const int GateThreadDelayMs = 500;
            private const int DequeueDelayThresholdMs = GateThreadDelayMs * 2;
            private const int GateThreadRunningMask = 0x4;

            private static int s_runningState;

            private static readonly AutoResetEvent s_runGateThreadEvent = new AutoResetEvent(initialState: true);

            private const int MaxRuns = 2;

            // TODO: PortableThreadPool - CoreCLR: Worker Tracking in CoreCLR? (Config name: ThreadPool_EnableWorkerTracking)
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

                while (true)
                {
                    s_runGateThreadEvent.WaitOne();

                    bool needGateThreadForRuntime;
                    do
                    {
                        Thread.Sleep(GateThreadDelayMs);

                        int cpuUtilization = cpuUtilizationReader.CurrentUtilization;
                        ThreadPoolInstance._cpuUtilization = cpuUtilization;

                        needGateThreadForRuntime = ThreadPool.PerformRuntimeSpecificGateActivities(cpuUtilization);

                        if (!disableStarvationDetection &&
                            ThreadPoolInstance._separated.numRequestedWorkers > 0 &&
                            SufficientDelaySinceLastDequeue())
                        {
                            try
                            {
                                ThreadPoolInstance._hillClimbingThreadAdjustmentLock.Acquire();
                                ThreadCounts counts = ThreadPoolInstance._separated.counts.VolatileRead();
                                // don't add a thread if we're at max or if we are already in the process of adding threads
                                while (counts.NumExistingThreads < ThreadPoolInstance._maxThreads && counts.NumExistingThreads >= counts.NumThreadsGoal)
                                {
                                    if (debuggerBreakOnWorkStarvation)
                                    {
                                        Debugger.Break();
                                    }

                                    ThreadCounts newCounts = counts;
                                    short newNumThreadsGoal = (short)(newCounts.NumExistingThreads + 1);
                                    newCounts.NumThreadsGoal = newNumThreadsGoal;
                                    ThreadCounts oldCounts = ThreadPoolInstance._separated.counts.InterlockedCompareExchange(newCounts, counts);
                                    if (oldCounts == counts)
                                    {
                                        HillClimbing.ThreadPoolHillClimber.ForceChange(newNumThreadsGoal, HillClimbing.StateOrTransition.Starvation);
                                        WorkerThread.MaybeAddWorkingWorker();
                                        break;
                                    }

                                    counts = oldCounts;
                                }
                            }
                            finally
                            {
                                ThreadPoolInstance._hillClimbingThreadAdjustmentLock.Release();
                            }
                        }
                    } while (
                        needGateThreadForRuntime ||
                        ThreadPoolInstance._separated.numRequestedWorkers > 0 ||
                        Interlocked.Decrement(ref s_runningState) > GetRunningStateForNumRuns(0));
                }
            }

            // called by logic to spawn new worker threads, return true if it's been too long
            // since the last dequeue operation - takes number of worker threads into account
            // in deciding "too long"
            private static bool SufficientDelaySinceLastDequeue()
            {
                int delay = Environment.TickCount - Volatile.Read(ref ThreadPoolInstance._separated.lastDequeueTime);

                int minimumDelay;

                if (ThreadPoolInstance._cpuUtilization < CpuUtilizationLow)
                {
                    minimumDelay = GateThreadDelayMs;
                }
                else
                {
                    ThreadCounts counts = ThreadPoolInstance._separated.counts.VolatileRead();
                    int numThreads = counts.NumThreadsGoal;
                    minimumDelay = numThreads * DequeueDelayThresholdMs;
                }
                return delay > minimumDelay;
            }

            // This is called by a worker thread
            internal static void EnsureRunning()
            {
                int numRunsMask = Interlocked.Exchange(ref s_runningState, GetRunningStateForNumRuns(MaxRuns));
                if ((numRunsMask & GateThreadRunningMask) == 0)
                {
                    bool created = false;
                    try
                    {
                        CreateGateThread();
                        created = true;
                    }
                    finally
                    {
                        if (!created)
                        {
                            Interlocked.Exchange(ref s_runningState, 0);
                        }
                    }
                }
                else if (numRunsMask == GetRunningStateForNumRuns(0))
                {
                    s_runGateThreadEvent.Set();
                }
            }

            private static int GetRunningStateForNumRuns(int numRuns)
            {
                Debug.Assert(numRuns >= 0);
                Debug.Assert(numRuns <= MaxRuns);
                return GateThreadRunningMask | numRuns;
            }

            private static void CreateGateThread()
            {
                Thread gateThread = new Thread(GateThreadStart);
                gateThread.IsThreadPoolThread = true;
                gateThread.IsBackground = true;
                gateThread.Name = ".NET ThreadPool Gate";
                gateThread.Start();
            }
        }

        internal static void EnsureGateThreadRunning() => GateThread.EnsureRunning();
    }
}
