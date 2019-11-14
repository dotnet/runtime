// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

            private static AutoResetEvent s_runGateThreadEvent = new AutoResetEvent(true);

            private static LowLevelLock s_createdLock = new LowLevelLock();

            private static readonly CpuUtilizationReader s_cpu = new CpuUtilizationReader();
            private const int MaxRuns = 2;

            // TODO: CoreCLR: Worker Tracking in CoreCLR? (Config name: ThreadPool_EnableWorkerTracking)
            private static void GateThreadStart()
            {
                var initialCpuRead = s_cpu.CurrentUtilization; // The first reading is over a time range other than what we are focusing on, so we do not use the read.

                AppContext.TryGetSwitch("System.Threading.ThreadPool.DisableStarvationDetection", out bool disableStarvationDetection);
                AppContext.TryGetSwitch("System.Threading.ThreadPool.DebugBreakOnWorkerStarvation", out bool debuggerBreakOnWorkStarvation);

                while (true)
                {
                    s_runGateThreadEvent.WaitOne();
                    do
                    {
                        Thread.Sleep(GateThreadDelayMs);

                        ThreadPoolInstance._cpuUtilization = s_cpu.CurrentUtilization;

                        if (!disableStarvationDetection)
                        {
                            if (ThreadPoolInstance._numRequestedWorkers > 0 && SufficientDelaySinceLastDequeue())
                            {
                                try
                                {
                                    ThreadPoolInstance._hillClimbingThreadAdjustmentLock.Acquire();
                                    ThreadCounts counts = ThreadCounts.VolatileReadCounts(ref ThreadPoolInstance._separated.counts);
                                    // don't add a thread if we're at max or if we are already in the process of adding threads
                                    while (counts.numExistingThreads < ThreadPoolInstance._maxThreads && counts.numExistingThreads >= counts.numThreadsGoal)
                                    {
                                        if (debuggerBreakOnWorkStarvation)
                                        {
                                            Debugger.Break();
                                        }

                                        ThreadCounts newCounts = counts;
                                        newCounts.numThreadsGoal = (short)(newCounts.numExistingThreads + 1);
                                        ThreadCounts oldCounts = ThreadCounts.CompareExchangeCounts(ref ThreadPoolInstance._separated.counts, newCounts, counts);
                                        if (oldCounts == counts)
                                        {
                                            HillClimbing.ThreadPoolHillClimber.ForceChange(newCounts.numThreadsGoal, HillClimbing.StateOrTransition.Starvation);
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
                        }
                    } while (ThreadPoolInstance._numRequestedWorkers > 0 || Interlocked.Decrement(ref s_runningState) > GetRunningStateForNumRuns(0));
                }
            }

            // called by logic to spawn new worker threads, return true if it's been too long
            // since the last dequeue operation - takes number of worker threads into account
            // in deciding "too long"
            private static bool SufficientDelaySinceLastDequeue()
            {
                int delay = Environment.TickCount - Volatile.Read(ref ThreadPoolInstance._separated.lastDequeueTime);

                int minimumDelay;

                if(ThreadPoolInstance._cpuUtilization < CpuUtilizationLow)
                {
                    minimumDelay = GateThreadDelayMs;
                }
                else
                {
                    ThreadCounts counts = ThreadCounts.VolatileReadCounts(ref ThreadPoolInstance._separated.counts);
                    int numThreads = counts.numThreadsGoal;
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
                gateThread.IsBackground = true;
                gateThread.Start();
            }
        }
    }
}
