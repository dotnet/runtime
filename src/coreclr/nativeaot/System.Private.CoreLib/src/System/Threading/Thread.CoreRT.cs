// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Globalization;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Threading
{
    public sealed partial class Thread
    {
        // Extra bits used in _threadState
        private const ThreadState ThreadPoolThread = (ThreadState)0x1000;

        // Bits of _threadState that are returned by the ThreadState property
        private const ThreadState PublicThreadStateMask = (ThreadState)0x1FF;

        internal ExecutionContext? _executionContext;
        internal SynchronizationContext? _synchronizationContext;

        private volatile int _threadState = (int)ThreadState.Unstarted;
        private ThreadPriority _priority;
        private ManagedThreadId _managedThreadId;
        private string? _name;
        private StartHelper? _startHelper;

        // Protects starting the thread and setting its priority
        private Lock _lock = new Lock();

        // This is used for a quick check on thread pool threads after running a work item to determine if the name, background
        // state, or priority were changed by the work item, and if so to reset it. Other threads may also change some of those,
        // but those types of changes may race with the reset anyway, so this field doesn't need to be synchronized.
        private bool _mayNeedResetForThreadPool;

        // so far the only place we initialize it is `WaitForForegroundThreads`
        // and only in the case when there are running foreground threads
        // by the moment of `StartupCodeHelpers.Shutdown()` invocation
        private static ManualResetEvent s_allDone;

        private static int s_foregroundRunningCount;

        private Thread()
        {
            _managedThreadId = System.Threading.ManagedThreadId.GetCurrentThreadId();

            PlatformSpecificInitialize();
            RegisterThreadExitCallback();
        }

        private void Initialize()
        {
            _priority = ThreadPriority.Normal;
            _managedThreadId = new ManagedThreadId();

            PlatformSpecificInitialize();
            RegisterThreadExitCallback();
        }

        private static unsafe void RegisterThreadExitCallback()
        {
            RuntimeImports.RhSetThreadExitCallback(&OnThreadExit);
        }

        internal static ulong CurrentOSThreadId
        {
            get
            {
                return RuntimeImports.RhCurrentOSThreadId();
            }
        }

        // Slow path executed once per thread
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Thread InitializeCurrentThread()
        {
            Debug.Assert(t_currentThread == null);

            var currentThread = new Thread();
            Debug.Assert(currentThread._threadState == (int)ThreadState.Unstarted);

            ThreadState state = 0;

            // The main thread is foreground, other ones are background
            if (currentThread._managedThreadId.Id != System.Threading.ManagedThreadId.IdMainThread)
            {
                state |= ThreadState.Background;
            }
            else
            {
                Interlocked.Increment(ref s_foregroundRunningCount);
            }

            currentThread._threadState = (int)(state | ThreadState.Running);
            currentThread.PlatformSpecificInitializeExistingThread();
            currentThread._priority = currentThread.GetPriorityLive();
            t_currentThread = currentThread;

            return currentThread;
        }

        /// <summary>
        /// Returns true if the underlying OS thread has been created and started execution of managed code.
        /// </summary>
        private bool HasStarted()
        {
            return !GetThreadStateBit(ThreadState.Unstarted);
        }

        public bool IsAlive
        {
            get
            {
                return ((ThreadState)_threadState & (ThreadState.Unstarted | ThreadState.Stopped | ThreadState.Aborted)) == 0;
            }
        }

        private bool IsDead()
        {
            return ((ThreadState)_threadState & (ThreadState.Stopped | ThreadState.Aborted)) != 0;
        }

        public bool IsBackground
        {
            get
            {
                if (IsDead())
                {
                    throw new ThreadStateException(SR.ThreadState_Dead_State);
                }
                return GetThreadStateBit(ThreadState.Background);
            }
            set
            {
                if (IsDead())
                {
                    throw new ThreadStateException(SR.ThreadState_Dead_State);
                }
                // we changing foreground count only for started threads
                // on thread start we count its state in `StartThread`
                if (value)
                {
                    int threadState = SetThreadStateBit(ThreadState.Background);
                    // was foreground and has started
                    if ((threadState & ((int)ThreadState.Background | (int)ThreadState.Unstarted)) == 0)
                    {
                        DecrementRunningForeground();
                    }
                }
                else
                {
                    int threadState = ClearThreadStateBit(ThreadState.Background);
                    // was background and has started
                    if ((threadState & ((int)ThreadState.Background | (int)ThreadState.Unstarted)) == (int)ThreadState.Background)
                    {
                        IncrementRunningForeground();
                        _mayNeedResetForThreadPool = true;
                    }
                }
            }
        }

        public bool IsThreadPoolThread
        {
            get
            {
                if (IsDead())
                {
                    throw new ThreadStateException(SR.ThreadState_Dead_State);
                }
                return GetThreadStateBit(ThreadPoolThread);
            }
            internal set
            {
                if (IsDead())
                {
                    throw new ThreadStateException(SR.ThreadState_Dead_State);
                }
                if (value)
                {
                    SetThreadStateBit(ThreadPoolThread);
                }
                else
                {
                    ClearThreadStateBit(ThreadPoolThread);
                }
            }
        }

        public int ManagedThreadId
        {
            [Intrinsic]
            get => _managedThreadId.Id;
        }

        // TODO: Inform the debugger and the profiler
        // private void ThreadNameChanged(string? value) {}

        public ThreadPriority Priority
        {
            get
            {
                if (IsDead())
                {
                    throw new ThreadStateException(SR.ThreadState_Dead_Priority);
                }
                if (!HasStarted())
                {
                    // The thread has not been started yet; return the value assigned to the Priority property.
                    // Race condition with setting the priority or starting the thread is OK, we may return an old value.
                    return _priority;
                }
                // The priority might have been changed by external means. Obtain the actual value from the OS
                // rather than using the value saved in _priority.
                return GetPriorityLive();
            }
            set
            {
                if ((value < ThreadPriority.Lowest) || (ThreadPriority.Highest < value))
                {
                    throw new ArgumentOutOfRangeException(SR.Argument_InvalidFlag);
                }
                if (IsDead())
                {
                    throw new ThreadStateException(SR.ThreadState_Dead_Priority);
                }

                // Prevent race condition with starting this thread
                using (LockHolder.Hold(_lock))
                {
                    if (HasStarted() && !SetPriorityLive(value))
                    {
                        throw new ThreadStateException(SR.ThreadState_SetPriorityFailed);
                    }
                    _priority = value;
                }

                if (value != ThreadPriority.Normal)
                {
                    _mayNeedResetForThreadPool = true;
                }
            }
        }

        public ThreadState ThreadState => ((ThreadState)_threadState & PublicThreadStateMask);

        private bool GetThreadStateBit(ThreadState bit)
        {
            Debug.Assert((bit & ThreadState.Stopped) == 0, "ThreadState.Stopped bit may be stale; use GetThreadState instead.");
            return (_threadState & (int)bit) != 0;
        }

        private int SetThreadStateBit(ThreadState bit)
        {
            int oldState, newState;
            do
            {
                oldState = _threadState;
                newState = oldState | (int)bit;
            } while (Interlocked.CompareExchange(ref _threadState, newState, oldState) != oldState);
            return oldState;
        }

        private int ClearThreadStateBit(ThreadState bit)
        {
            int oldState, newState;
            do
            {
                oldState = _threadState;
                newState = oldState & ~(int)bit;
            } while (Interlocked.CompareExchange(ref _threadState, newState, oldState) != oldState);
            return oldState;
        }

        internal void SetWaitSleepJoinState()
        {
            Debug.Assert(this == CurrentThread);
            Debug.Assert(!GetThreadStateBit(ThreadState.WaitSleepJoin));

            SetThreadStateBit(ThreadState.WaitSleepJoin);
        }

        internal void ClearWaitSleepJoinState()
        {
            Debug.Assert(this == CurrentThread);
            Debug.Assert(GetThreadStateBit(ThreadState.WaitSleepJoin));

            ClearThreadStateBit(ThreadState.WaitSleepJoin);
        }

        private static int VerifyTimeoutMilliseconds(int millisecondsTimeout)
        {
            if (millisecondsTimeout < -1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(millisecondsTimeout),
                    millisecondsTimeout,
                    SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            }
            return millisecondsTimeout;
        }

        public bool Join(int millisecondsTimeout)
        {
            VerifyTimeoutMilliseconds(millisecondsTimeout);
            if (GetThreadStateBit(ThreadState.Unstarted))
            {
                throw new ThreadStateException(SR.ThreadState_NotStarted);
            }
            return JoinInternal(millisecondsTimeout);
        }

        /// <summary>
        /// Max value to be passed into <see cref="SpinWait(int)"/> for optimal delaying. Currently, the value comes from
        /// defaults in CoreCLR's Thread::InitializeYieldProcessorNormalized(). This value is supposed to be normalized to be
        /// appropriate for the processor.
        /// TODO: See issue https://github.com/dotnet/corert/issues/4430
        /// </summary>
        internal const int OptimalMaxSpinWaitsPerSpinIteration = 64;

        public static void SpinWait(int iterations) => RuntimeImports.RhSpinWait(iterations);

        [MethodImpl(MethodImplOptions.NoInlining)] // Slow path method. Make sure that the caller frame does not pay for PInvoke overhead.
        public static bool Yield() => RuntimeImports.RhYield();

        private void StartCore()
        {
            using (LockHolder.Hold(_lock))
            {
                if (!GetThreadStateBit(ThreadState.Unstarted))
                {
                    throw new ThreadStateException(SR.ThreadState_AlreadyStarted);
                }

                bool waitingForThreadStart = false;
                GCHandle threadHandle = GCHandle.Alloc(this);

                try
                {
                    if (!CreateThread(threadHandle))
                    {
                        throw new OutOfMemoryException();
                    }

                    // Skip cleanup if any asynchronous exception happens while waiting for the thread start
                    waitingForThreadStart = true;

                    // Wait until the new thread either dies or reports itself as started
                    while (GetThreadStateBit(ThreadState.Unstarted) && !JoinInternal(0))
                    {
                        Yield();
                    }

                    waitingForThreadStart = false;
                }
                finally
                {
                    Debug.Assert(!waitingForThreadStart, "Leaked threadHandle");
                    if (!waitingForThreadStart)
                    {
                        threadHandle.Free();
                    }
                }

                if (GetThreadStateBit(ThreadState.Unstarted))
                {
                    // Lack of memory is the only expected reason for thread creation failure
                    throw new ThreadStartException(new OutOfMemoryException());
                }
            }
        }

        private static void StartThread(IntPtr parameter)
        {
            GCHandle threadHandle = (GCHandle)parameter;
            Thread thread = (Thread)threadHandle.Target;

            try
            {
                t_currentThread = thread;
                System.Threading.ManagedThreadId.SetForCurrentThread(thread._managedThreadId);
                thread.InitializeComOnNewThread();
            }
            catch (OutOfMemoryException)
            {
#if TARGET_UNIX
                // This should go away once OnThreadExit stops using t_currentThread to signal
                // shutdown of the thread on Unix.
                thread._stopped!.Set();
#endif
                // Terminate the current thread. The creator thread will throw a ThreadStartException.
                return;
            }

            // Report success to the creator thread, which will free threadHandle
            int state = thread.ClearThreadStateBit(ThreadState.Unstarted);
            if ((state & (int)ThreadState.Background) == 0)
            {
                IncrementRunningForeground();
            }

            try
            {
                StartHelper? startHelper = thread._startHelper;
                Debug.Assert(startHelper != null);
                thread._startHelper = null;

                startHelper.Run();
            }
            finally
            {
                thread.SetThreadStateBit(ThreadState.Stopped);
            }
        }

        private static void StopThread(Thread thread)
        {
            int state = thread._threadState;
            if ((state & (int)(ThreadState.Stopped | ThreadState.Aborted)) == 0)
            {
                thread.SetThreadStateBit(ThreadState.Stopped);
            }
            if ((state & (int)ThreadState.Background) == 0)
            {
                DecrementRunningForeground();
            }
        }

        // The upper bits of t_currentProcessorIdCache are the currentProcessorId. The lower bits of
        // the t_currentProcessorIdCache are counting down to get it periodically refreshed.
        // TODO: Consider flushing the currentProcessorIdCache on Wait operations or similar
        // actions that are likely to result in changing the executing core
        [ThreadStatic]
        private static int t_currentProcessorIdCache;

        private const int ProcessorIdCacheShift = 16;
        private const int ProcessorIdCacheCountDownMask = (1 << ProcessorIdCacheShift) - 1;
        private const int ProcessorIdRefreshRate = 5000;

        private static int RefreshCurrentProcessorId()
        {
            int currentProcessorId = ComputeCurrentProcessorId();

            // Add offset to make it clear that it is not guaranteed to be 0-based processor number
            currentProcessorId += 100;

            Debug.Assert(ProcessorIdRefreshRate <= ProcessorIdCacheCountDownMask);

            // Mask with int.MaxValue to ensure the execution Id is not negative
            t_currentProcessorIdCache = ((currentProcessorId << ProcessorIdCacheShift) & int.MaxValue) + ProcessorIdRefreshRate;

            return currentProcessorId;
        }

        // Cached processor id used as a hint for which per-core stack to access. It is periodically
        // refreshed to trail the actual thread core affinity.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCurrentProcessorId()
        {
            int currentProcessorIdCache = t_currentProcessorIdCache--;
            if ((currentProcessorIdCache & ProcessorIdCacheCountDownMask) == 0)
                return RefreshCurrentProcessorId();
            return (currentProcessorIdCache >> ProcessorIdCacheShift);
        }

        internal static void IncrementRunningForeground()
        {
            Interlocked.Increment(ref s_foregroundRunningCount);
        }

        internal static void DecrementRunningForeground()
        {
            if (Interlocked.Decrement(ref s_foregroundRunningCount) == 0)
            {
                // Interlocked.Decrement issues full memory barrier
                // so most recent write to s_allDone should be visible here
                s_allDone?.Set();
            }
        }

        internal static void WaitForForegroundThreads()
        {
            Thread.CurrentThread.IsBackground = true;
            // last read/write inside `IsBackground` issues full barrier no matter of logic flow
            // so we can just read `s_foregroundRunningCount`
            if (s_foregroundRunningCount == 0)
            {
                // current thread is the last foreground thread, so let the runtime finish
                return;
            }
            Volatile.Write(ref s_allDone, new ManualResetEvent(false));
            // other foreground threads could have their job finished meanwhile
            // Volatile.Write above issues release barrier
            // but we need acquire barrier to observe most recent write to s_foregroundRunningCount
            if (Volatile.Read(ref s_foregroundRunningCount) == 0)
            {
                return;
            }
            s_allDone.WaitOne();
        }
    }
}
