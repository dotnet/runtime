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
            public const uint GateActivitiesPeriodMs = 500;
            private const uint DequeueDelayThresholdMs = GateActivitiesPeriodMs * 2;
            private const int GateThreadRunningMask = 0x4;
            private const int MaxRuns = 2;

            private static readonly AutoResetEvent RunGateThreadEvent = new AutoResetEvent(initialState: true);
            private static readonly AutoResetEvent DelayEvent = new AutoResetEvent(initialState: false);

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
                LowLevelLock threadAdjustmentLock = threadPoolInstance._threadAdjustmentLock;
                DelayHelper delayHelper = default;

                if (BlockingConfig.IsCooperativeBlockingEnabled && !BlockingConfig.IgnoreMemoryUsage)
                {
                    // Initialize memory usage and limits, and register to update them on gen 2 GCs
                    threadPoolInstance.OnGen2GCCallback();
                    Gen2GcCallback.Register(threadPoolInstance.OnGen2GCCallback);
                }

                while (true)
                {
                    RunGateThreadEvent.WaitOne();
                    int currentTimeMs = Environment.TickCount;
                    delayHelper.SetGateActivitiesTime(currentTimeMs);

                    while (true)
                    {
                        bool wasSignaledToWake = DelayEvent.WaitOne((int)delayHelper.GetNextDelay(currentTimeMs));
                        currentTimeMs = Environment.TickCount;

                        // Thread count adjustment for cooperative blocking
                        do
                        {
                            PendingBlockingAdjustment pendingBlockingAdjustment = threadPoolInstance._pendingBlockingAdjustment;
                            if (pendingBlockingAdjustment == PendingBlockingAdjustment.None)
                            {
                                delayHelper.ClearBlockingAdjustmentDelay();
                                break;
                            }

                            bool previousDelayElapsed = false;
                            if (delayHelper.HasBlockingAdjustmentDelay)
                            {
                                previousDelayElapsed =
                                    delayHelper.HasBlockingAdjustmentDelayElapsed(currentTimeMs, wasSignaledToWake);
                                if (pendingBlockingAdjustment == PendingBlockingAdjustment.WithDelayIfNecessary &&
                                    !previousDelayElapsed)
                                {
                                    break;
                                }
                            }

                            uint nextDelayMs = threadPoolInstance.PerformBlockingAdjustment(previousDelayElapsed);
                            if (nextDelayMs <= 0)
                            {
                                delayHelper.ClearBlockingAdjustmentDelay();
                            }
                            else
                            {
                                delayHelper.SetBlockingAdjustmentTimeAndDelay(currentTimeMs, nextDelayMs);
                            }
                        } while (false);

                        //
                        // Periodic gate activities
                        //

                        if (!delayHelper.ShouldPerformGateActivities(currentTimeMs, wasSignaledToWake))
                        {
                            continue;
                        }

                        if (ThreadPool.EnableWorkerTracking && NativeRuntimeEventSource.Log.IsEnabled())
                        {
                            NativeRuntimeEventSource.Log.ThreadPoolWorkingThreadCount(
                                (uint)threadPoolInstance.GetAndResetHighWatermarkCountOfThreadsProcessingUserCallbacks());
                        }

                        int cpuUtilization = cpuUtilizationReader.CurrentUtilization;
                        threadPoolInstance._cpuUtilization = cpuUtilization;

                        bool needGateThreadForRuntime = ThreadPool.PerformRuntimeSpecificGateActivities(cpuUtilization);

                        if (!disableStarvationDetection &&
                            threadPoolInstance._pendingBlockingAdjustment == PendingBlockingAdjustment.None &&
                            threadPoolInstance._separated.numRequestedWorkers > 0 &&
                            SufficientDelaySinceLastDequeue(threadPoolInstance))
                        {
                            bool addWorker = false;
                            threadAdjustmentLock.Acquire();
                            try
                            {
                                // Don't add a thread if we're at max or if we are already in the process of adding threads.
                                // This logic is slightly different from the native implementation in CoreCLR because there are
                                // no retired threads. In the native implementation, when hill climbing reduces the thread count
                                // goal, threads that are stopped from processing work are switched to "retired" state, and they
                                // don't count towards the equivalent existing thread count. In this implementation, the
                                // existing thread count includes any worker thread that has not yet exited, including those
                                // stopped from working by hill climbing, so here the number of threads processing work, instead
                                // of the number of existing threads, is compared with the goal. There may be alternative
                                // solutions, for now this is only to maintain consistency in behavior.
                                ThreadCounts counts = threadPoolInstance._separated.counts;
                                while (
                                    counts.NumProcessingWork < threadPoolInstance._maxThreads &&
                                    counts.NumProcessingWork >= counts.NumThreadsGoal)
                                {
                                    if (debuggerBreakOnWorkStarvation)
                                    {
                                        Debugger.Break();
                                    }

                                    ThreadCounts newCounts = counts;
                                    short newNumThreadsGoal = (short)(counts.NumProcessingWork + 1);
                                    newCounts.NumThreadsGoal = newNumThreadsGoal;

                                    ThreadCounts countsBeforeUpdate =
                                        threadPoolInstance._separated.counts.InterlockedCompareExchange(newCounts, counts);
                                    if (countsBeforeUpdate == counts)
                                    {
                                        HillClimbing.ThreadPoolHillClimber.ForceChange(
                                            newNumThreadsGoal,
                                            HillClimbing.StateOrTransition.Starvation);
                                        addWorker = true;
                                        break;
                                    }

                                    counts = countsBeforeUpdate;
                                }
                            }
                            finally
                            {
                                threadAdjustmentLock.Release();
                            }

                            if (addWorker)
                            {
                                WorkerThread.MaybeAddWorkingWorker(threadPoolInstance);
                            }
                        }

                        if (!needGateThreadForRuntime &&
                            threadPoolInstance._separated.numRequestedWorkers <= 0 &&
                            threadPoolInstance._pendingBlockingAdjustment == PendingBlockingAdjustment.None &&
                            Interlocked.Decrement(ref threadPoolInstance._separated.gateThreadRunningState) <= GetRunningStateForNumRuns(0))
                        {
                            break;
                        }
                    }
                }
            }

            public static void Wake(PortableThreadPool threadPoolInstance)
            {
                DelayEvent.Set();
                EnsureRunning(threadPoolInstance);
            }

            // called by logic to spawn new worker threads, return true if it's been too long
            // since the last dequeue operation - takes number of worker threads into account
            // in deciding "too long"
            private static bool SufficientDelaySinceLastDequeue(PortableThreadPool threadPoolInstance)
            {
                uint delay = (uint)(Environment.TickCount - threadPoolInstance._separated.lastDequeueTime);
                uint minimumDelay;
                if (threadPoolInstance._cpuUtilization < CpuUtilizationLow)
                {
                    minimumDelay = GateActivitiesPeriodMs;
                }
                else
                {
                    minimumDelay = (uint)threadPoolInstance._separated.counts.NumThreadsGoal * DequeueDelayThresholdMs;
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
                    RunGateThreadEvent.Set();
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

            private struct DelayHelper
            {
                private int _previousGateActivitiesTimeMs;
                private int _previousBlockingAdjustmentDelayStartTimeMs;
                private uint _previousBlockingAdjustmentDelayMs;
                private bool _runGateActivitiesAfterNextDelay;
                private bool _adjustForBlockingAfterNextDelay;

                public void SetGateActivitiesTime(int currentTimeMs)
                {
                    _previousGateActivitiesTimeMs = currentTimeMs;
                }

                public void SetBlockingAdjustmentTimeAndDelay(int currentTimeMs, uint delayMs)
                {
                    _previousBlockingAdjustmentDelayStartTimeMs = currentTimeMs;
                    _previousBlockingAdjustmentDelayMs = delayMs;
                }

                public void ClearBlockingAdjustmentDelay() => _previousBlockingAdjustmentDelayMs = 0;
                public bool HasBlockingAdjustmentDelay => _previousBlockingAdjustmentDelayMs != 0;

                public uint GetNextDelay(int currentTimeMs)
                {
                    uint elapsedMsSincePreviousGateActivities = (uint)(currentTimeMs - _previousGateActivitiesTimeMs);
                    uint nextDelayForGateActivities =
                        elapsedMsSincePreviousGateActivities < GateActivitiesPeriodMs
                            ? GateActivitiesPeriodMs - elapsedMsSincePreviousGateActivities
                            : 1;
                    if (_previousBlockingAdjustmentDelayMs == 0)
                    {
                        _runGateActivitiesAfterNextDelay = true;
                        _adjustForBlockingAfterNextDelay = false;
                        return nextDelayForGateActivities;
                    }

                    uint elapsedMsSincePreviousBlockingAdjustmentDelay =
                        (uint)(currentTimeMs - _previousBlockingAdjustmentDelayStartTimeMs);
                    uint nextDelayForBlockingAdjustment =
                        elapsedMsSincePreviousBlockingAdjustmentDelay < _previousBlockingAdjustmentDelayMs
                            ? _previousBlockingAdjustmentDelayMs - elapsedMsSincePreviousBlockingAdjustmentDelay
                            : 1;
                    uint nextDelay = Math.Min(nextDelayForGateActivities, nextDelayForBlockingAdjustment);
                    _runGateActivitiesAfterNextDelay = nextDelay == nextDelayForGateActivities;
                    _adjustForBlockingAfterNextDelay = nextDelay == nextDelayForBlockingAdjustment;
                    Debug.Assert(nextDelay <= GateActivitiesPeriodMs);
                    return nextDelay;
                }

                public bool ShouldPerformGateActivities(int currentTimeMs, bool wasSignaledToWake)
                {
                    bool result =
                        (!wasSignaledToWake && _runGateActivitiesAfterNextDelay) ||
                        (uint)(currentTimeMs - _previousGateActivitiesTimeMs) >= GateActivitiesPeriodMs;
                    if (result)
                    {
                        SetGateActivitiesTime(currentTimeMs);
                    }
                    return result;
                }

                public bool HasBlockingAdjustmentDelayElapsed(int currentTimeMs, bool wasSignaledToWake)
                {
                    Debug.Assert(HasBlockingAdjustmentDelay);

                    if (!wasSignaledToWake && _adjustForBlockingAfterNextDelay)
                    {
                        return true;
                    }

                    uint elapsedMsSincePreviousBlockingAdjustmentDelay =
                        (uint)(currentTimeMs - _previousBlockingAdjustmentDelayStartTimeMs);
                    return elapsedMsSincePreviousBlockingAdjustmentDelay >= _previousBlockingAdjustmentDelayMs;
                }
            }
        }

        internal static void EnsureGateThreadRunning() => GateThread.EnsureRunning(ThreadPoolInstance);
    }
}
