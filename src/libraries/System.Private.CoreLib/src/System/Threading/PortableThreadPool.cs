// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Threading
{
    /// <summary>
    /// A thread-pool run and managed on the CLR.
    /// </summary>
    internal sealed partial class PortableThreadPool
    {
#pragma warning disable IDE1006 // Naming Styles
        public static readonly PortableThreadPool ThreadPoolInstance = new PortableThreadPool();
#pragma warning restore IDE1006 // Naming Styles

        private const int ThreadPoolThreadTimeoutMs = 20 * 1000; // If you change this make sure to change the timeout times in the tests.

#if BIT64
        private const short MaxPossibleThreadCount = short.MaxValue;
#elif BIT32
        private const short MaxPossibleThreadCount = 1023;
#else
        #error Unknown platform
#endif

        private const int CpuUtilizationHigh = 95;
        private const int CpuUtilizationLow = 80;
        private int _cpuUtilization = 0;

        private static readonly short s_forcedMinWorkerThreads = AppContextConfigHelper.GetInt16Config("System.Threading.ThreadPool.MinThreads", 0, false);
        private static readonly short s_forcedMaxWorkerThreads = AppContextConfigHelper.GetInt16Config("System.Threading.ThreadPool.MaxThreads", 0, false);

        private short _minThreads;
        private short _maxThreads;
        private readonly LowLevelLock _maxMinThreadLock = new LowLevelLock();

        [StructLayout(LayoutKind.Explicit, Size = CacheLineSize * 5)]
        private struct CacheLineSeparated
        {
#if ARM64
            private const int CacheLineSize = 128;
#else
            private const int CacheLineSize = 64;
#endif
            [FieldOffset(CacheLineSize * 1)]
            public ThreadCounts counts;
            [FieldOffset(CacheLineSize * 2)]
            public int lastDequeueTime;
            [FieldOffset(CacheLineSize * 3)]
            public int priorCompletionCount;
            [FieldOffset(CacheLineSize * 3 + sizeof(int))]
            public int priorCompletedWorkRequestsTime;
            [FieldOffset(CacheLineSize * 3 + sizeof(int) * 2)]
            public int nextCompletedWorkRequestsTime;
        }

        private CacheLineSeparated _separated;
        private ulong _currentSampleStartTime;
        private readonly ThreadInt64PersistentCounter _completionCounter = new ThreadInt64PersistentCounter();
        private int _threadAdjustmentIntervalMs;

        private LowLevelLock _hillClimbingThreadAdjustmentLock = new LowLevelLock();

        private volatile int _numRequestedWorkers = 0;

        private PortableThreadPool()
        {
            _minThreads = s_forcedMinWorkerThreads > 0 ? s_forcedMinWorkerThreads : (short)ThreadPoolGlobals.processorCount;
            if (_minThreads > MaxPossibleThreadCount)
            {
                _minThreads = MaxPossibleThreadCount;
            }

            _maxThreads = s_forcedMaxWorkerThreads > 0 ? s_forcedMaxWorkerThreads : MaxPossibleThreadCount;
            if (_maxThreads < _minThreads)
            {
                _maxThreads = _minThreads;
            }

            _separated = new CacheLineSeparated
            {
                counts = new ThreadCounts
                {
                    numThreadsGoal = _minThreads
                }
            };
        }

        public bool SetMinThreads(int minThreads)
        {
            _maxMinThreadLock.Acquire();
            try
            {
                if (minThreads < 0 || minThreads > _maxThreads)
                {
                    return false;
                }
                else
                {
                    short threads = (short)Math.Min(minThreads, MaxPossibleThreadCount);
                    if (s_forcedMinWorkerThreads == 0)
                    {
                        _minThreads = threads;

                        ThreadCounts counts = ThreadCounts.VolatileReadCounts(ref _separated.counts);
                        while (counts.numThreadsGoal < _minThreads)
                        {
                            ThreadCounts newCounts = counts;
                            newCounts.numThreadsGoal = _minThreads;

                            ThreadCounts oldCounts = ThreadCounts.CompareExchangeCounts(ref _separated.counts, newCounts, counts);
                            if (oldCounts == counts)
                            {
                                counts = newCounts;

                                if (newCounts.numThreadsGoal > oldCounts.numThreadsGoal && _numRequestedWorkers > 0)
                                {
                                    WorkerThread.MaybeAddWorkingWorker();
                                }
                            }
                            else
                            {
                                counts = oldCounts;
                            }
                        }
                    }
                    return true;
                }
            }
            finally
            {
                _maxMinThreadLock.Release();
            }
        }

        public int GetMinThreads() => _minThreads;

        public bool SetMaxThreads(int maxThreads)
        {
            _maxMinThreadLock.Acquire();
            try
            {
                if (maxThreads < _minThreads || maxThreads == 0)
                {
                    return false;
                }
                else
                {
                    short threads = (short)Math.Min(maxThreads, MaxPossibleThreadCount);
                    if (s_forcedMaxWorkerThreads == 0)
                    {
                        _maxThreads = threads;

                        ThreadCounts counts = ThreadCounts.VolatileReadCounts(ref _separated.counts);
                        while (counts.numThreadsGoal > _maxThreads)
                        {
                            ThreadCounts newCounts = counts;
                            newCounts.numThreadsGoal = _maxThreads;

                            ThreadCounts oldCounts = ThreadCounts.CompareExchangeCounts(ref _separated.counts, newCounts, counts);
                            if (oldCounts == counts)
                            {
                                counts = newCounts;
                            }
                            else
                            {
                                counts = oldCounts;
                            }
                        }
                    }
                    return true;
                }
            }
            finally
            {
                _maxMinThreadLock.Release();
            }
        }

        public int GetMaxThreads() => _maxThreads;

        public int GetAvailableThreads()
        {
            ThreadCounts counts = ThreadCounts.VolatileReadCounts(ref _separated.counts);
            int count = _maxThreads - counts.numProcessingWork;
            if (count < 0)
            {
                return 0;
            }
            return count;
        }

        public int ThreadCount => ThreadCounts.VolatileReadCounts(ref _separated.counts).numExistingThreads;
        public long CompletedWorkItemCount => _completionCounter.Count;

        internal bool NotifyWorkItemComplete()
        {
            _completionCounter.Increment();
            Volatile.Write(ref _separated.lastDequeueTime, Environment.TickCount);
            
            if (ShouldAdjustMaxWorkersActive() && _hillClimbingThreadAdjustmentLock.TryAcquire())
            {
                try
                {
                    AdjustMaxWorkersActive();
                }
                finally
                {
                    _hillClimbingThreadAdjustmentLock.Release();
                }
            }

            return !WorkerThread.ShouldStopProcessingWorkNow();
        }

        //
        // This method must only be called if ShouldAdjustMaxWorkersActive has returned true, *and*
        // _hillClimbingThreadAdjustmentLock is held.
        //
        private void AdjustMaxWorkersActive()
        {
            _hillClimbingThreadAdjustmentLock.VerifyIsLocked();
            int currentTicks = Environment.TickCount;
            int totalNumCompletions = (int)_completionCounter.Count;
            int numCompletions = totalNumCompletions - _separated.priorCompletionCount;
            ulong startTime = _currentSampleStartTime;
            ulong endTime = HighPerformanceCounter.TickCount;
            ulong freq = HighPerformanceCounter.Frequency;

            double elapsedSeconds = (double)(endTime - startTime) / freq;

            if(elapsedSeconds * 1000 >= _threadAdjustmentIntervalMs / 2)
            {
                ThreadCounts currentCounts = ThreadCounts.VolatileReadCounts(ref _separated.counts);
                int newMax;
                (newMax, _threadAdjustmentIntervalMs) = HillClimbing.ThreadPoolHillClimber.Update(currentCounts.numThreadsGoal, elapsedSeconds, numCompletions);

                while(newMax != currentCounts.numThreadsGoal)
                {
                    ThreadCounts newCounts = currentCounts;
                    newCounts.numThreadsGoal = (short)newMax;

                    ThreadCounts oldCounts = ThreadCounts.CompareExchangeCounts(ref _separated.counts, newCounts, currentCounts);
                    if (oldCounts == currentCounts)
                    {
                        //
                        // If we're increasing the max, inject a thread.  If that thread finds work, it will inject
                        // another thread, etc., until nobody finds work or we reach the new maximum.
                        //
                        // If we're reducing the max, whichever threads notice this first will sleep and timeout themselves.
                        //
                        if (newMax > oldCounts.numThreadsGoal)
                        {
                            WorkerThread.MaybeAddWorkingWorker();
                        }
                        break;
                    }
                    else
                    {
                        if(oldCounts.numThreadsGoal > currentCounts.numThreadsGoal && oldCounts.numThreadsGoal >= newMax)
                        {
                            // someone (probably the gate thread) increased the thread count more than
                            // we are about to do.  Don't interfere.
                            break;
                        }

                        currentCounts = oldCounts;
                    }
                }
                _separated.priorCompletionCount = totalNumCompletions;
                _separated.nextCompletedWorkRequestsTime = currentTicks + _threadAdjustmentIntervalMs;
                Volatile.Write(ref _separated.priorCompletedWorkRequestsTime, currentTicks);
                _currentSampleStartTime = endTime;
            }
        }

        private bool ShouldAdjustMaxWorkersActive()
        {
            // We need to subtract by prior time because Environment.TickCount can wrap around, making a comparison of absolute times unreliable.
            int priorTime = Volatile.Read(ref _separated.priorCompletedWorkRequestsTime);
            int requiredInterval = _separated.nextCompletedWorkRequestsTime - priorTime;
            int elapsedInterval = Environment.TickCount - priorTime;
            if(elapsedInterval >= requiredInterval)
            {
                // Avoid trying to adjust the thread count goal if there are already more threads than the thread count goal.
                // In that situation, hill climbing must have previously decided to decrease the thread count goal, so let's
                // wait until the system responds to that change before calling into hill climbing again. This condition should
                // be the opposite of the condition in WorkerThread.ShouldStopProcessingWorkNow that causes
                // threads processing work to stop in response to a decreased thread count goal. The logic here is a bit
                // different from the original CoreCLR code from which this implementation was ported because in this
                // implementation there are no retired threads, so only the count of threads processing work is considered.
                ThreadCounts counts = ThreadCounts.VolatileReadCounts(ref _separated.counts);
                return counts.numProcessingWork <= counts.numThreadsGoal;
            }
            return false;
        }

        internal void RequestWorker()
        {
            Interlocked.Increment(ref _numRequestedWorkers);
            WorkerThread.MaybeAddWorkingWorker();
            GateThread.EnsureRunning();
        }
    }
}
