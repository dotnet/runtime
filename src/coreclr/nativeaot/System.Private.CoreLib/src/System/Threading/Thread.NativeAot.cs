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
        internal ExecutionContext? _executionContext;
        internal SynchronizationContext? _synchronizationContext;

        private string? _name;
        private StartHelper? _startHelper;

        private ThreadPriority _priority;
        private ManagedThreadId _managedThreadId;

        // This is used for a quick check on thread pool threads after running a work item to determine if the name, background
        // state, or priority were changed by the work item, and if so to reset it. Other threads may also change some of those,
        // but those types of changes may race with the reset anyway, so this field doesn't need to be synchronized.
        private bool _mayNeedResetForThreadPool;

        public int ManagedThreadId
        {
            [Intrinsic]
            get => _managedThreadId.Id;
        }

        public bool IsAlive
        {
            get => IsAliveCore;
        }

        public bool IsBackground
        {
            get => IsBackgroundCore;
            set
            {
                IsBackgroundCore = value;
            }
        }

        public bool IsThreadPoolThread
        {
            get => IsThreadPoolThreadCore;
            internal set
            {
                IsThreadPoolThreadCore = value;
            }
        }

        public ThreadPriority Priority
        {
            get => PriorityCore;
            set
            {
                PriorityCore = value;
            }
        }

        public ThreadState ThreadState => ThreadStateCore;

        internal const int OptimalMaxSpinWaitsPerSpinIteration = OptimalMaxSpinWaitsPerSpinIterationCore;

        internal static ulong CurrentOSThreadId
        {
            get => CurrentOSThreadIdCore;
        }

        private Thread()
        {
            _managedThreadId = System.Threading.ManagedThreadId.GetCurrentThreadId();

            PlatformSpecificInitialize();
            RegisterThreadExitCallback();
        }

        private void StartCore() => StartWindowsThreadPoolCore();

        private static Thread InitializeCurrentThread() => InitializeCurrentThreadCore();

        private void Initialize() => InitializeCore();

        internal void SetWaitSleepJoinState() => SetWaitSleepJoinStateCore();

        internal void ClearWaitSleepJoinState() => ClearWaitSleepJoinStateCrore();

        public bool Join(int millisecondsTimeout) => JoinCore(millisecondsTimeout);

        internal static void SpinWaitInternal(int iterations) => SpinWaitInternalCore(iterations);

        public static void SpinWait(int iterations) => SpinWaitCore();

        [MethodImpl(MethodImplOptions.NoInlining)] // Slow path method. Make sure that the caller frame does not pay for PInvoke overhead.
        public static bool Yield() => YieldCore();

        internal static void IncrementRunningForeground() => IncrementRunningForegroundCore();

        internal static void DecrementRunningForeground() => DecrementRunningForegroundCore();

        internal static void WaitForForegroundThreads() => WaitForForegroundThreadsCore();
    }
}
