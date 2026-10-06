// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    public sealed partial class Thread
    {
        // Wasm polls through its native helper; retain the GC transition for the managed fallback.
        private static void PollGC() => PollGCInternal();

        // Spinning cannot make progress with only one thread.
        internal static int OptimalMaxSpinWaitsPerSpinIteration => 0;

        // Finalizers execute on the current thread without marking it as a dedicated finalizer thread.
        internal static bool CurrentThreadIsFinalizerThread() => false;

        private static int s_nextManagedThreadId = 1;
        private bool _isBackground;

        // The managed object for the only thread. Set by the runtime during startup.
        private static Thread? s_currentThread;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Thread InitializeCurrentThread()
        {
            Thread? thread = s_currentThread;
            Debug.Assert(thread is not null);
            return t_currentThread = thread;
        }

        // Matches the OS thread ID that the native runtime reports for the only thread.
        private static ulong GetCurrentOSThreadId() => 1;

        // There are no other threads to yield to.
        public static bool Yield() => false;

        private void Initialize()
        {
            _priority = (int)ThreadPriority.Normal;
            if (s_nextManagedThreadId == int.MaxValue)
            {
                throw new OutOfMemoryException();
            }

            _managedThreadId = ++s_nextManagedThreadId;
        }

        private bool GetIsBackgroundCore()
        {
            return _isBackground;
        }

        private void SetIsBackgroundCore(bool value)
        {
            _isBackground = value;
        }

        private void SetPriorityCore(ThreadPriority value)
        {
            _priority = (int)value;
        }

        /// <summary>Returns true if the thread is a threadpool thread.</summary>
        /// <remarks>There are no thread pool threads when multithreading is not supported.</remarks>
        public bool IsThreadPoolThread
        {
            get => false;
            internal set => throw new PlatformNotSupportedException();
        }

        // Finalizers run on the only thread, so it must not be reconfigured as a finalizer thread.
        internal void ResetFinalizerThread()
        {
        }

        private ThreadState GetThreadStateCore()
        {
            if (_DONT_USE_InternalThread != IntPtr.Zero)
            {
                ThreadState state = (ThreadState)GetThreadState(GetNativeHandle());
                GC.KeepAlive(this);
                return state;
            }

            return ThreadState.Unstarted | (_isBackground ? ThreadState.Background : (ThreadState)0);
        }

        public ApartmentState GetApartmentState() => ApartmentState.Unknown;

        private static bool SetApartmentStateUnchecked(ApartmentState state, bool throwOnError)
        {
            if (state != ApartmentState.Unknown)
            {
                if (throwOnError)
                {
                    throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
                }

                return false;
            }

            return true;
        }

        internal const bool ReentrantWaitsEnabled = false;

        public void DisableComObjectEagerCleanup() { }

        /// <summary>
        /// Interrupts a thread that is inside a Wait(), Sleep() or Join().  If that
        /// thread is not currently blocked in that manner, it will be interrupted
        /// when it next begins to block.
        /// </summary>
        public void Interrupt() => WaitSubsystem.Interrupt(this);

        /// <summary>
        /// Wait for a length of time proportional to 'iterations'. Spinning cannot make
        /// progress with only one thread, so this returns immediately.
        /// </summary>
        public static void SpinWait(int iterations)
        {
        }
    }
}
