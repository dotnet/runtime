// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Threading
{
    /// <summary>
    /// A thread-pool run and managed on the CLR.
    /// </summary>
    internal sealed partial class PortableThreadPool
    {
        private const int SmallStackSizeBytes = 256 * 1024;

        private const short MaxPossibleThreadCount = short.MaxValue;

#if TARGET_BROWSER
        private const short DefaultMaxWorkerThreadCount = 10;
#elif TARGET_64BIT
        private const short DefaultMaxWorkerThreadCount = MaxPossibleThreadCount;
#elif TARGET_32BIT
        private const short DefaultMaxWorkerThreadCount = 1023;
#else
        #error Unknown platform
#endif

        private const int CpuUtilizationHigh = 95;
        private const int CpuUtilizationLow = 80;

#if CORECLR
#pragma warning disable CA1823
        private static readonly bool s_initialized = ThreadPool.EnsureConfigInitialized();
#pragma warning restore CA1823
#endif

        private static readonly short ForcedMinWorkerThreads =
            AppContextConfigHelper.GetInt16Config("System.Threading.ThreadPool.MinThreads", 0, false);
        private static readonly short ForcedMaxWorkerThreads =
            AppContextConfigHelper.GetInt16Config("System.Threading.ThreadPool.MaxThreads", 0, false);

        private static readonly int ThreadPoolThreadTimeoutMs = DetermineThreadPoolThreadTimeoutMs();

        private static int DetermineThreadPoolThreadTimeoutMs()
        {
            const int DefaultThreadPoolThreadTimeoutMs = 20 * 1000; // If you change this make sure to change the timeout times in the tests.

            // The amount of time in milliseconds a thread pool thread waits without having done any work before timing out and
            // exiting. Set to -1 to disable the timeout. Applies to worker threads and wait threads. Also see the
            // ThreadsToKeepAlive config value for relevant information.
            int timeoutMs =
                AppContextConfigHelper.GetInt32Config(
                    "System.Threading.ThreadPool.ThreadTimeoutMs",
                    "DOTNET_ThreadPool_ThreadTimeoutMs",
                    DefaultThreadPoolThreadTimeoutMs);
            return timeoutMs >= -1 ? timeoutMs : DefaultThreadPoolThreadTimeoutMs;
        }

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
        private short _legacy_minIOCompletionThreads;
        private short _legacy_maxIOCompletionThreads;

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
            [FieldOffset(Internal.PaddingHelpers.CACHE_LINE_SIZE * 4 + sizeof(int))]
            public int gateThreadRunningState;
        }

        private long _currentSampleStartTime;
        private readonly ThreadInt64PersistentCounter _completionCounter = new ThreadInt64PersistentCounter();
        private int _threadAdjustmentIntervalMs;

        private short _numBlockedThreads;
        private short _numThreadsAddedDueToBlocking;
        private PendingBlockingAdjustment _pendingBlockingAdjustment;

        private long _memoryUsageBytes;
        private long _memoryLimitBytes;

#if TARGET_WINDOWS
        private readonly nint _ioPort;
        private IOCompletionPoller[]? _ioCompletionPollers;
#endif

        private readonly LowLevelLock _threadAdjustmentLock = new LowLevelLock();

        private CacheLineSeparated _separated; // SOS's ThreadPool command depends on this name

        private PortableThreadPool()
        {
            _minThreads = HasForcedMinThreads ? ForcedMinWorkerThreads : (short)Environment.ProcessorCount;
            if (_minThreads > MaxPossibleThreadCount)
            {
                _minThreads = MaxPossibleThreadCount;
            }

            _maxThreads = HasForcedMaxThreads ? ForcedMaxWorkerThreads : DefaultMaxWorkerThreadCount;
            if (_maxThreads > MaxPossibleThreadCount)
            {
                _maxThreads = MaxPossibleThreadCount;
            }
            else if (_maxThreads < _minThreads)
            {
                _maxThreads = _minThreads;
            }

            _legacy_minIOCompletionThreads = 1;
            _legacy_maxIOCompletionThreads = 1000;

            if (NativeRuntimeEventSource.Log.IsEnabled())
            {
                NativeRuntimeEventSource.Log.ThreadPoolMinMaxThreads(
                    (ushort)_minThreads,
                    (ushort)_maxThreads,
                    (ushort)_legacy_minIOCompletionThreads,
                    (ushort)_legacy_maxIOCompletionThreads);
            }

            _separated.counts.NumThreadsGoal = _minThreads;

#if TARGET_WINDOWS
            _ioPort = CreateIOCompletionPort();
#endif
        }

        private static bool HasForcedMinThreads =>
            ForcedMinWorkerThreads > 0 && (ForcedMaxWorkerThreads <= 0 || ForcedMinWorkerThreads <= ForcedMaxWorkerThreads);
        private static bool HasForcedMaxThreads =>
            ForcedMaxWorkerThreads > 0 && (ForcedMinWorkerThreads <= 0 || ForcedMinWorkerThreads <= ForcedMaxWorkerThreads);

        public bool SetMinThreads(int workerThreads, int ioCompletionThreads)
        {
            if (workerThreads < 0 || ioCompletionThreads < 0)
            {
                return false;
            }

            bool addWorker = false;
            bool wakeGateThread = false;

            _threadAdjustmentLock.Acquire();
            try
            {
                if (workerThreads > _maxThreads)
                {
                    return false;
                }

                if (ioCompletionThreads > _legacy_maxIOCompletionThreads)
                {
                    return false;
                }

                if (HasForcedMinThreads && workerThreads != ForcedMinWorkerThreads)
                {
                    return false;
                }

                _legacy_minIOCompletionThreads = (short)Math.Max(1, ioCompletionThreads);

                short newMinThreads = (short)Math.Max(1, workerThreads);
                if (newMinThreads == _minThreads)
                {
                    return true;
                }

                _minThreads = newMinThreads;
                if (_numBlockedThreads > 0)
                {
                    // Blocking adjustment will adjust the goal according to its heuristics
                    if (_pendingBlockingAdjustment != PendingBlockingAdjustment.Immediately)
                    {
                        _pendingBlockingAdjustment = PendingBlockingAdjustment.Immediately;
                        wakeGateThread = true;
                    }
                }
                else if (_separated.counts.NumThreadsGoal < newMinThreads)
                {
                    _separated.counts.InterlockedSetNumThreadsGoal(newMinThreads);
                    if (_separated.numRequestedWorkers > 0)
                    {
                        addWorker = true;
                    }
                }

                if (NativeRuntimeEventSource.Log.IsEnabled())
                {
                    NativeRuntimeEventSource.Log.ThreadPoolMinMaxThreads(
                        (ushort)_minThreads,
                        (ushort)_maxThreads,
                        (ushort)_legacy_minIOCompletionThreads,
                        (ushort)_legacy_maxIOCompletionThreads);
                }
            }
            finally
            {
                _threadAdjustmentLock.Release();
            }

            if (addWorker)
            {
                WorkerThread.MaybeAddWorkingWorker(this);
            }
            else if (wakeGateThread)
            {
                GateThread.Wake(this);
            }
            return true;
        }

        public void GetMinThreads(out int workerThreads, out int ioCompletionThreads)
        {
            workerThreads = Volatile.Read(ref _minThreads);
            ioCompletionThreads = _legacy_minIOCompletionThreads;
        }

        public bool SetMaxThreads(int workerThreads, int ioCompletionThreads)
        {
            if (workerThreads <= 0 || ioCompletionThreads <= 0)
            {
                return false;
            }

            _threadAdjustmentLock.Acquire();
            try
            {
                if (workerThreads < _minThreads)
                {
                    return false;
                }

                if (ioCompletionThreads < _legacy_minIOCompletionThreads)
                {
                    return false;
                }

                if (HasForcedMaxThreads && workerThreads != ForcedMaxWorkerThreads)
                {
                    return false;
                }

                _legacy_maxIOCompletionThreads = (short)Math.Min(ioCompletionThreads, MaxPossibleThreadCount);

                short newMaxThreads = (short)Math.Min(workerThreads, MaxPossibleThreadCount);
                if (newMaxThreads == _maxThreads)
                {
                    return true;
                }

                _maxThreads = newMaxThreads;
                if (_separated.counts.NumThreadsGoal > newMaxThreads)
                {
                    _separated.counts.InterlockedSetNumThreadsGoal(newMaxThreads);
                }

                if (NativeRuntimeEventSource.Log.IsEnabled())
                {
                    NativeRuntimeEventSource.Log.ThreadPoolMinMaxThreads(
                        (ushort)_minThreads,
                        (ushort)_maxThreads,
                        (ushort)_legacy_minIOCompletionThreads,
                        (ushort)_legacy_maxIOCompletionThreads);
                }
                return true;
            }
            finally
            {
                _threadAdjustmentLock.Release();
            }
        }

        public void GetMaxThreads(out int workerThreads, out int ioCompletionThreads)
        {
            workerThreads = Volatile.Read(ref _maxThreads);
            ioCompletionThreads = _legacy_maxIOCompletionThreads;
        }

        public void GetAvailableThreads(out int workerThreads, out int ioCompletionThreads)
        {
            ThreadCounts counts = _separated.counts.VolatileRead();
            workerThreads = Math.Max(0, _maxThreads - counts.NumProcessingWork);
            ioCompletionThreads = _legacy_maxIOCompletionThreads;
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
            _separated.lastDequeueTime = currentTimeMs;

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
            LowLevelLock threadAdjustmentLock = _threadAdjustmentLock;
            if (!threadAdjustmentLock.TryAcquire())
            {
                // The lock is held by someone else, they will take care of this for us
                return;
            }

            bool addWorker = false;
            try
            {
                // Repeated checks from ShouldAdjustMaxWorkersActive() inside the lock
                ThreadCounts counts = _separated.counts;
                if (counts.NumProcessingWork > counts.NumThreadsGoal ||
                    _pendingBlockingAdjustment != PendingBlockingAdjustment.None)
                {
                    return;
                }

                long endTime = Stopwatch.GetTimestamp();
                double elapsedSeconds = Stopwatch.GetElapsedTime(_currentSampleStartTime, endTime).TotalSeconds;

                if (elapsedSeconds * 1000 >= _threadAdjustmentIntervalMs / 2)
                {
                    int currentTicks = Environment.TickCount;
                    int totalNumCompletions = (int)_completionCounter.Count;
                    int numCompletions = totalNumCompletions - _separated.priorCompletionCount;

                    short oldNumThreadsGoal = counts.NumThreadsGoal;
                    int newNumThreadsGoal;
                    (newNumThreadsGoal, _threadAdjustmentIntervalMs) =
                        HillClimbing.ThreadPoolHillClimber.Update(oldNumThreadsGoal, elapsedSeconds, numCompletions);
                    if (oldNumThreadsGoal != (short)newNumThreadsGoal)
                    {
                        _separated.counts.InterlockedSetNumThreadsGoal((short)newNumThreadsGoal);

                        //
                        // If we're increasing the goal, inject a thread.  If that thread finds work, it will inject
                        // another thread, etc., until nobody finds work or we reach the new goal.
                        //
                        // If we're reducing the goal, whichever threads notice this first will sleep and timeout themselves.
                        //
                        if (newNumThreadsGoal > oldNumThreadsGoal)
                        {
                            addWorker = true;
                        }
                    }

                    _separated.priorCompletionCount = totalNumCompletions;
                    _separated.nextCompletedWorkRequestsTime = currentTicks + _threadAdjustmentIntervalMs;
                    Volatile.Write(ref _separated.priorCompletedWorkRequestsTime, currentTicks);
                    _currentSampleStartTime = endTime;
                }
            }
            finally
            {
                threadAdjustmentLock.Release();
            }

            if (addWorker)
            {
                WorkerThread.MaybeAddWorkingWorker(this);
            }
        }

        private bool ShouldAdjustMaxWorkersActive(int currentTimeMs)
        {
            if (HillClimbing.IsDisabled)
            {
                return false;
            }

            // We need to subtract by prior time because Environment.TickCount can wrap around, making a comparison of absolute
            // times unreliable. Intervals are unsigned to avoid wrapping around on the subtract after enough time elapses, and
            // this also prevents the initial elapsed interval from being negative due to the prior and next times being
            // initialized to zero.
            int priorTime = Volatile.Read(ref _separated.priorCompletedWorkRequestsTime);
            uint requiredInterval = (uint)(_separated.nextCompletedWorkRequestsTime - priorTime);
            uint elapsedInterval = (uint)(currentTimeMs - priorTime);
            if (elapsedInterval < requiredInterval)
            {
                return false;
            }

            // Avoid trying to adjust the thread count goal if there are already more threads than the thread count goal.
            // In that situation, hill climbing must have previously decided to decrease the thread count goal, so let's
            // wait until the system responds to that change before calling into hill climbing again. This condition should
            // be the opposite of the condition in WorkerThread.ShouldStopProcessingWorkNow that causes
            // threads processing work to stop in response to a decreased thread count goal. The logic here is a bit
            // different from the original CoreCLR code from which this implementation was ported because in this
            // implementation there are no retired threads, so only the count of threads processing work is considered.
            ThreadCounts counts = _separated.counts;
            if (counts.NumProcessingWork > counts.NumThreadsGoal)
            {
                return false;
            }

            // Skip hill climbing when there is a pending blocking adjustment. Hill climbing may otherwise bypass the
            // blocking adjustment heuristics and increase the thread count too quickly.
            return _pendingBlockingAdjustment == PendingBlockingAdjustment.None;
        }

        internal void RequestWorker()
        {
            // The order of operations here is important. MaybeAddWorkingWorker() and EnsureRunning() use speculative checks to
            // do their work and the memory barrier from the interlocked operation is necessary in this case for correctness.
            Interlocked.Increment(ref _separated.numRequestedWorkers);
            WorkerThread.MaybeAddWorkingWorker(this);
            GateThread.EnsureRunning(this);
        }

        private bool OnGen2GCCallback()
        {
            // Gen 2 GCs may be very infrequent in some cases. If it becomes an issue, consider updating the memory usage more
            // frequently. The memory usage is only used for fallback purposes in blocking adjustment, so an artificially higher
            // memory usage may cause blocking adjustment to fall back to slower adjustments sooner than necessary.
            GCMemoryInfo gcMemoryInfo = GC.GetGCMemoryInfo();
            _memoryLimitBytes = gcMemoryInfo.HighMemoryLoadThresholdBytes;
            _memoryUsageBytes = Math.Min(gcMemoryInfo.MemoryLoadBytes, gcMemoryInfo.HighMemoryLoadThresholdBytes);
            return true; // continue receiving gen 2 GC callbacks
        }

        internal static RegisteredWaitHandle RegisterWaitForSingleObject(
             WaitHandle waitObject,
             WaitOrTimerCallback callBack,
             object? state,
             uint millisecondsTimeOutInterval,
             bool executeOnlyOnce,
             bool flowExecutionContext)
        {
            ArgumentNullException.ThrowIfNull(waitObject);
            ArgumentNullException.ThrowIfNull(callBack);

            RegisteredWaitHandle registeredWaitHandle = new RegisteredWaitHandle(
                waitObject,
                new _ThreadPoolWaitOrTimerCallback(callBack, state, flowExecutionContext),
                (int)millisecondsTimeOutInterval,
                !executeOnlyOnce);

            PortableThreadPool.ThreadPoolInstance.RegisterWaitHandle(registeredWaitHandle);

            return registeredWaitHandle;
        }
    }
}
