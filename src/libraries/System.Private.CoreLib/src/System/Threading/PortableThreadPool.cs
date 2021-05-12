// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Threading
{
    /// <summary>
    /// A thread-pool run and managed on the CLR.
    /// </summary>
    internal sealed partial class PortableThreadPool
    {
        private const int ThreadPoolThreadTimeoutMs = 20 * 1000; // If you change this make sure to change the timeout times in the tests.
        private const int SmallStackSizeBytes = 256 * 1024;

        private const short MaxPossibleThreadCount = short.MaxValue;

#if TARGET_64BIT
        private const short DefaultMaxWorkerThreadCount = MaxPossibleThreadCount;
#elif TARGET_32BIT
        private const short DefaultMaxWorkerThreadCount = 1023;
#else
        #error Unknown platform
#endif

        private const int CpuUtilizationHigh = 95;
        private const int CpuUtilizationLow = 80;

        private static readonly short s_forcedMinWorkerThreads = AppContextConfigHelper.GetInt16Config("System.Threading.ThreadPool.MinThreads", 0, false);
        private static readonly short s_forcedMaxWorkerThreads = AppContextConfigHelper.GetInt16Config("System.Threading.ThreadPool.MaxThreads", 0, false);

        [ThreadStatic]
        private static object? t_completionCountObject;

#pragma warning disable IDE1006 // Naming Styles
        // The singleton must be initialized after the static variables above, as the constructor may be dependent on them.
        // SOS's ThreadPool command depends on this name.
        public static readonly PortableThreadPool ThreadPoolInstance = new PortableThreadPool();
#pragma warning restore IDE1006 // Naming Styles

        private int _cpuUtilization; // SOS's ThreadPool command depends on this name
        private short _minThreads;
        private short _maxThreads;
        private readonly LowLevelLock _maxMinThreadLock = new LowLevelLock();

        [StructLayout(LayoutKind.Explicit, Size = Internal.PaddingHelpers.CACHE_LINE_SIZE * 6)]
        private struct CacheLineSeparated
        {
            [FieldOffset(Internal.PaddingHelpers.CACHE_LINE_SIZE * 1)]
            public ThreadCounts counts; // SOS's ThreadPool command depends on this name
            [FieldOffset(Internal.PaddingHelpers.CACHE_LINE_SIZE * 2)]
            public int lastDequeueTime;
            [FieldOffset(Internal.PaddingHelpers.CACHE_LINE_SIZE * 3)]
            public int priorCompletionCount;
            [FieldOffset(Internal.PaddingHelpers.CACHE_LINE_SIZE * 3 + sizeof(int))]
            public int priorCompletedWorkRequestsTime;
            [FieldOffset(Internal.PaddingHelpers.CACHE_LINE_SIZE * 3 + sizeof(int) * 2)]
            public int nextCompletedWorkRequestsTime;
            [FieldOffset(Internal.PaddingHelpers.CACHE_LINE_SIZE * 4)]
            public volatile int numRequestedWorkers;
            [FieldOffset(Internal.PaddingHelpers.CACHE_LINE_SIZE * 5)]
            public int gateThreadRunningState;
        }

        private CacheLineSeparated _separated; // SOS's ThreadPool command depends on this name
        private long _currentSampleStartTime;
        private readonly ThreadInt64PersistentCounter _completionCounter = new ThreadInt64PersistentCounter();
        private int _threadAdjustmentIntervalMs;

        private readonly LowLevelLock _hillClimbingThreadAdjustmentLock = new LowLevelLock();

        private PortableThreadPool()
        {
            _minThreads = s_forcedMinWorkerThreads > 0 ? s_forcedMinWorkerThreads : (short)Environment.ProcessorCount;
            if (_minThreads > MaxPossibleThreadCount)
            {
                _minThreads = MaxPossibleThreadCount;
            }

            _maxThreads = s_forcedMaxWorkerThreads > 0 ? s_forcedMaxWorkerThreads : DefaultMaxWorkerThreadCount;
            if (_maxThreads > MaxPossibleThreadCount)
            {
                _maxThreads = MaxPossibleThreadCount;
            }
            else if (_maxThreads < _minThreads)
            {
                _maxThreads = _minThreads;
            }

            _separated = new CacheLineSeparated
            {
                counts = new ThreadCounts
                {
                    NumThreadsGoal = _minThreads
                }
            };
        }

        public bool SetMinThreads(int workerThreads, int ioCompletionThreads)
        {
            if (workerThreads < 0 || ioCompletionThreads < 0)
            {
                return false;
            }

            _maxMinThreadLock.Acquire();
            try
            {
                if (workerThreads > _maxThreads || !ThreadPool.CanSetMinIOCompletionThreads(ioCompletionThreads))
                {
                    return false;
                }

                ThreadPool.SetMinIOCompletionThreads(ioCompletionThreads);

                if (s_forcedMinWorkerThreads != 0)
                {
                    return true;
                }

                short newMinThreads = (short)Math.Max(1, Math.Min(workerThreads, MaxPossibleThreadCount));
                _minThreads = newMinThreads;

                ThreadCounts counts = _separated.counts.VolatileRead();
                while (counts.NumThreadsGoal < newMinThreads)
                {
                    ThreadCounts newCounts = counts;
                    newCounts.NumThreadsGoal = newMinThreads;

                    ThreadCounts oldCounts = _separated.counts.InterlockedCompareExchange(newCounts, counts);
                    if (oldCounts == counts)
                    {
                        if (_separated.numRequestedWorkers > 0)
                        {
                            WorkerThread.MaybeAddWorkingWorker(this);
                        }
                        break;
                    }

                    counts = oldCounts;
                }

                return true;
            }
            finally
            {
                _maxMinThreadLock.Release();
            }
        }

        public int GetMinThreads() => Volatile.Read(ref _minThreads);

        public bool SetMaxThreads(int workerThreads, int ioCompletionThreads)
        {
            if (workerThreads <= 0 || ioCompletionThreads <= 0)
            {
                return false;
            }

            _maxMinThreadLock.Acquire();
            try
            {
                if (workerThreads < _minThreads || !ThreadPool.CanSetMaxIOCompletionThreads(ioCompletionThreads))
                {
                    return false;
                }

                ThreadPool.SetMaxIOCompletionThreads(ioCompletionThreads);

                if (s_forcedMaxWorkerThreads != 0)
                {
                    return true;
                }

                short newMaxThreads = (short)Math.Min(workerThreads, MaxPossibleThreadCount);
                _maxThreads = newMaxThreads;

                ThreadCounts counts = _separated.counts.VolatileRead();
                while (counts.NumThreadsGoal > newMaxThreads)
                {
                    ThreadCounts newCounts = counts;
                    newCounts.NumThreadsGoal = newMaxThreads;

                    ThreadCounts oldCounts = _separated.counts.InterlockedCompareExchange(newCounts, counts);
                    if (oldCounts == counts)
                    {
                        break;
                    }

                    counts = oldCounts;
                }

                return true;
            }
            finally
            {
                _maxMinThreadLock.Release();
            }
        }

        public int GetMaxThreads() => Volatile.Read(ref _maxThreads);

        public int GetAvailableThreads()
        {
            ThreadCounts counts = _separated.counts.VolatileRead();
            int count = _maxThreads - counts.NumProcessingWork;
            if (count < 0)
            {
                return 0;
            }
            return count;
        }

        public int ThreadCount => _separated.counts.VolatileRead().NumExistingThreads;
        public long CompletedWorkItemCount => _completionCounter.Count;

        public object GetOrCreateThreadLocalCompletionCountObject() =>
            t_completionCountObject ?? CreateThreadLocalCompletionCountObject();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private object CreateThreadLocalCompletionCountObject()
        {
            Debug.Assert(t_completionCountObject == null);

            object threadLocalCompletionCountObject = _completionCounter.CreateThreadLocalCountObject();
            t_completionCountObject = threadLocalCompletionCountObject;
            return threadLocalCompletionCountObject;
        }

        private void NotifyWorkItemProgress(object threadLocalCompletionCountObject, int currentTimeMs)
        {
            ThreadInt64PersistentCounter.Increment(threadLocalCompletionCountObject);
            Volatile.Write(ref _separated.lastDequeueTime, Environment.TickCount);

            if (ShouldAdjustMaxWorkersActive(currentTimeMs))
            {
                AdjustMaxWorkersActive();
            }
        }

        internal void NotifyWorkItemProgress() =>
            NotifyWorkItemProgress(GetOrCreateThreadLocalCompletionCountObject(), Environment.TickCount);

        internal bool NotifyWorkItemComplete(object? threadLocalCompletionCountObject, int currentTimeMs)
        {
            Debug.Assert(threadLocalCompletionCountObject != null);

            NotifyWorkItemProgress(threadLocalCompletionCountObject!, currentTimeMs);
            return !WorkerThread.ShouldStopProcessingWorkNow(this);
        }

        //
        // This method must only be called if ShouldAdjustMaxWorkersActive has returned true, *and*
        // _hillClimbingThreadAdjustmentLock is held.
        //
        private void AdjustMaxWorkersActive()
        {
            LowLevelLock hillClimbingThreadAdjustmentLock = _hillClimbingThreadAdjustmentLock;
            if (!hillClimbingThreadAdjustmentLock.TryAcquire())
            {
                // The lock is held by someone else, they will take care of this for us
                return;
            }

            try
            {
                long startTime = _currentSampleStartTime;
                long endTime = Stopwatch.GetTimestamp();
                long freq = Stopwatch.Frequency;

                double elapsedSeconds = (double)(endTime - startTime) / freq;

                if (elapsedSeconds * 1000 >= _threadAdjustmentIntervalMs / 2)
                {
                    int currentTicks = Environment.TickCount;
                    int totalNumCompletions = (int)_completionCounter.Count;
                    int numCompletions = totalNumCompletions - _separated.priorCompletionCount;

                    ThreadCounts currentCounts = _separated.counts.VolatileRead();
                    int newMax;
                    (newMax, _threadAdjustmentIntervalMs) = HillClimbing.ThreadPoolHillClimber.Update(currentCounts.NumThreadsGoal, elapsedSeconds, numCompletions);

                    while (newMax != currentCounts.NumThreadsGoal)
                    {
                        ThreadCounts newCounts = currentCounts;
                        newCounts.NumThreadsGoal = (short)newMax;

                        ThreadCounts oldCounts = _separated.counts.InterlockedCompareExchange(newCounts, currentCounts);
                        if (oldCounts == currentCounts)
                        {
                            //
                            // If we're increasing the max, inject a thread.  If that thread finds work, it will inject
                            // another thread, etc., until nobody finds work or we reach the new maximum.
                            //
                            // If we're reducing the max, whichever threads notice this first will sleep and timeout themselves.
                            //
                            if (newMax > oldCounts.NumThreadsGoal)
                            {
                                WorkerThread.MaybeAddWorkingWorker(this);
                            }
                            break;
                        }

                        if (oldCounts.NumThreadsGoal > currentCounts.NumThreadsGoal && oldCounts.NumThreadsGoal >= newMax)
                        {
                            // someone (probably the gate thread) increased the thread count more than
                            // we are about to do.  Don't interfere.
                            break;
                        }

                        currentCounts = oldCounts;
                    }

                    _separated.priorCompletionCount = totalNumCompletions;
                    _separated.nextCompletedWorkRequestsTime = currentTicks + _threadAdjustmentIntervalMs;
                    Volatile.Write(ref _separated.priorCompletedWorkRequestsTime, currentTicks);
                    _currentSampleStartTime = endTime;
                }
            }
            finally
            {
                hillClimbingThreadAdjustmentLock.Release();
            }
        }

        private bool ShouldAdjustMaxWorkersActive(int currentTimeMs)
        {
            // We need to subtract by prior time because Environment.TickCount can wrap around, making a comparison of absolute
            // times unreliable. Intervals are unsigned to avoid wrapping around on the subtract after enough time elapses, and
            // this also prevents the initial elapsed interval from being negative due to the prior and next times being
            // initialized to zero.
            int priorTime = Volatile.Read(ref _separated.priorCompletedWorkRequestsTime);
            uint requiredInterval = (uint)(_separated.nextCompletedWorkRequestsTime - priorTime);
            uint elapsedInterval = (uint)(currentTimeMs - priorTime);
            if (elapsedInterval >= requiredInterval)
            {
                // Avoid trying to adjust the thread count goal if there are already more threads than the thread count goal.
                // In that situation, hill climbing must have previously decided to decrease the thread count goal, so let's
                // wait until the system responds to that change before calling into hill climbing again. This condition should
                // be the opposite of the condition in WorkerThread.ShouldStopProcessingWorkNow that causes
                // threads processing work to stop in response to a decreased thread count goal. The logic here is a bit
                // different from the original CoreCLR code from which this implementation was ported because in this
                // implementation there are no retired threads, so only the count of threads processing work is considered.
                ThreadCounts counts = _separated.counts.VolatileRead();
                return counts.NumProcessingWork <= counts.NumThreadsGoal && !HillClimbing.IsDisabled;
            }
            return false;
        }

        internal void RequestWorker()
        {
            // The order of operations here is important. MaybeAddWorkingWorker() and EnsureRunning() use speculative checks to
            // do their work and the memory barrier from the interlocked operation is necessary in this case for correctness.
            Interlocked.Increment(ref _separated.numRequestedWorkers);
            WorkerThread.MaybeAddWorkingWorker(this);
            GateThread.EnsureRunning(this);
        }
    }
}
