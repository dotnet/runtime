// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Globalization;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;

namespace System.Threading
{
    public sealed partial class Thread
    {
        private string? _name;
        private StartHelper? _startHelper;

        // This is used for a quick check on thread pool threads after running a work item to determine if the name, background
        // state, or priority were changed by the work item, and if so to reset it. Other threads may also change some of those,
        // but those types of changes may race with the reset anyway, so this field doesn't need to be synchronized.
        private bool _mayNeedResetForThreadPool;

        public bool IsAlive
        {
            get => ThreadPool.UseWindowsThreadPool ? IsAliveCore : IsAlivePortableCore;
        }

        public bool IsBackground
        {
            get => ThreadPool.UseWindowsThreadPool ? IsBackgroundCore : IsBackgroundPortableCore;
            set
            {
                if (ThreadPool.UseWindowsThreadPool)
                {
                    IsBackgroundCore = value;
                }
                else
                {
                    IsBackgroundPortableCore = value;
                }
            }
        }

        public bool IsThreadPoolThread
        {
            get => ThreadPool.UseWindowsThreadPool ? IsThreadPoolThreadCore : IsThreadPoolThreadPortableCore;
            internal set
            {
                if (ThreadPool.UseWindowsThreadPool)
                {
                    IsThreadPoolThreadCore = value;
                }
                else
                {
                    IsThreadPoolThreadPortableCore = value;
                }
            }
        }

        public ThreadPriority Priority
        {
            get => ThreadPool.UseWindowsThreadPool ? PriorityCore : PriorityPortableCore;
            set
            {
                if (ThreadPool.UseWindowsThreadPool)
                {
                    PriorityCore = value;
                }
                else
                {
                    PriorityPortableCore = value;
                }
            }
        }

        public ThreadState ThreadState => ThreadPool.UseWindowsThreadPool ? ThreadStateCore : ThreadStatePortableCore;

        internal static int OptimalMaxSpinWaitsPerSpinIteration
        {
            get => ThreadPool.UseWindowsThreadPool ? OptimalMaxSpinWaitsPerSpinIterationCore : OptimalMaxSpinWaitsPerSpinIterationPortableCore;
        }

        private Thread()
        {
            if (ThreadPool.UseWindowsThreadPool)
            {
                _managedThreadId = System.Threading.ManagedThreadId.GetCurrentThreadId();

                PlatformSpecificInitialize();
                RegisterThreadExitCallback();
            }
        }

        private unsafe void StartCore() => ThreadPool.UseWindowsThreadPool ? StartWindowsThreadPoolCore() : StartCLRCore();

        private static Thread InitializeCurrentThread() => ThreadPool.UseWindowsThreadPool ? InitializeCurrentThreadCore() : InitializeCurrentThreadPortableCore();

        private void Initialize() => ThreadPool.UseWindowsThreadPool ? InitializeCore() : InitializePortableCore();

        public bool Join(int millisecondsTimeout) => ThreadPool.UseWindowsThreadPool ? JoinCore(millisecondsTimeout) : JoinPortableCore(millisecondsTimeout);

        internal void SetWaitSleepJoinState() => SetWaitSleepJoinStateCore();

        internal void ClearWaitSleepJoinState() => ClearWaitSleepJoinStateCrore();

        private static void SpinWaitInternal(int iterations) => ThreadPool.UseWindowsThreadPool ? SpinWaitInternalCore(iterations) : SpinWaitInternalPortableCore(iterations);

        public static void SpinWait(int iterations) => ThreadPool.UseWindowsThreadPool ? SpinWaitCore(iterations) : SpinWaitPortableCore(iterations);

        [MethodImpl(MethodImplOptions.NoInlining)] // Slow path method. Make sure that the caller frame does not pay for PInvoke overhead.
        public static bool Yield() => ThreadPool.UseWindowsThreadPool ? YieldCore() : YieldPortableCore();

        internal static void IncrementRunningForeground() => IncrementRunningForegroundCore();

        internal static void DecrementRunningForeground() => DecrementRunningForegroundCore();

        internal static void WaitForForegroundThreads() => WaitForForegroundThreadsCore();

        /// <summary>Returns handle for interop with EE. The handle is guaranteed to be non-null.</summary>
        internal ThreadHandle GetNativeHandle() => GetNativeHandleCore();

        /// <summary>Clean up the thread when it goes away.</summary>
        ~Thread() => InternalFinalize(); // Delegate to the unmanaged portion.

        partial void ThreadNameChanged(string? value)
        {
            InformThreadNameChange(GetNativeHandle(), value, value?.Length ?? 0);
        }

        public ApartmentState GetApartmentState() => GetApartmentStatePortableCore();

#if FEATURE_COMINTEROP
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern void DisableComObjectEagerCleanup();
#else // !FEATURE_COMINTEROP
        public void DisableComObjectEagerCleanup()
        {
        }
#endif // FEATURE_COMINTEROP

        /// <summary>
        /// Interrupts a thread that is inside a Wait(), Sleep() or Join().  If that
        /// thread is not currently blocked in that manner, it will be interrupted
        /// when it next begins to block.
        /// </summary>
        public void Interrupt() => InterruptCore();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ResetThreadPoolThread() => ResetThreadPoolThreadCore();
    }
}
