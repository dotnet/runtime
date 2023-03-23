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

        private Thread()
        {
            if (ThreadPool.UseWindowsThreadPool)
            {
                _managedThreadId = System.Threading.ManagedThreadId.GetCurrentThreadId();

                PlatformSpecificInitialize();
                RegisterThreadExitCallback();
            }
        }

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

        /// <summary>Returns handle for interop with EE. The handle is guaranteed to be non-null.</summary>
        internal ThreadHandle GetNativeHandle() => GetNativeHandleCore();

        public static void SpinWait(int iterations) => SpinWaitCore(iterations);

        public static bool Yield() => YieldCore();

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

        /// <summary>
        /// Waits for the thread to die or for timeout milliseconds to elapse.
        /// </summary>
        /// <returns>
        /// Returns true if the thread died, or false if the wait timed out. If
        /// -1 is given as the parameter, no timeout will occur.
        /// </returns>
        /// <exception cref="ArgumentException">if timeout &lt; -1 (Timeout.Infinite)</exception>
        /// <exception cref="ThreadInterruptedException">if the thread is interrupted while waiting</exception>
        /// <exception cref="ThreadStateException">if the thread has not been started yet</exception>
        public bool Join(int millisecondsTimeout) => JoinCore(millisecondsTimeout);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ResetThreadPoolThread() => ResetThreadPoolThreadCore();
    }
}
