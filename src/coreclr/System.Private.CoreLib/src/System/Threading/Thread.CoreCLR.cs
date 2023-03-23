// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
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

        /// <summary>Returns true if the thread has been started and is not dead.</summary>
        public bool IsAlive
        {
            get
            {
                return IsAlivePortableCore;
            }
        }

        public bool IsBackground
        {
            get => IsBackgroundPortableCore;
            set
            {
                IsBackgroundPortableCore = value;
            }
        }

        public bool IsThreadPoolThread
        {
            get => IsThreadPoolThreadPortableCore;
            internal set {
                IsThreadPoolThreadPortableCore = value;
            }
        }

        public ThreadPriority Priority
        {
            get => PriorityPortableCore;
            set
            {
                PriorityPortableCore = value;
            }
        }

        public ThreadState ThreadState => ThreadStatePortableCore;

        internal static int OptimalMaxSpinWaitsPerSpinIteration
        {
            get => OptimalMaxSpinWaitsPerSpinIterationPortableCore;
        }

        private Thread() { }

        private unsafe void StartCore() => StartCLRCore();

        private static void SpinWaitInternal(int iterations) => SpinWaitInternalPortableCore(iterations);

        private static Thread InitializeCurrentThread() => InitializeCurrentThreadPortableCore();

        private void Initialize() => InitializePortableCore();

        /// <summary>Returns handle for interop with EE. The handle is guaranteed to be non-null.</summary>
        internal ThreadHandle GetNativeHandle() => GetNativeHandleCore();

        public static void SpinWait(int iterations) => SpinWaitPortableCore(iterations);

        public static bool Yield() => YieldPortableCore();

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
        public extern void Interrupt() => InterruptCore();

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
        public bool Join(int millisecondsTimeout) => JoinPortableCore(millisecondsTimeout);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ResetThreadPoolThread() => ResetThreadPoolThreadCore();
    }
}
