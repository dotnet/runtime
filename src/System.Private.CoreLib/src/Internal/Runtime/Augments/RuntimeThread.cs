// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

namespace Internal.Runtime.Augments
{
    public class RuntimeThread : CriticalFinalizerObject
    {
        private static int s_optimalMaxSpinWaitsPerSpinIteration;

        internal RuntimeThread() { }

        public static RuntimeThread Create(ThreadStart start) => new Thread(start);
        public static RuntimeThread Create(ThreadStart start, int maxStackSize) => new Thread(start, maxStackSize);
        public static RuntimeThread Create(ParameterizedThreadStart start) => new Thread(start);
        public static RuntimeThread Create(ParameterizedThreadStart start, int maxStackSize) => new Thread(start, maxStackSize);

        private Thread AsThread()
        {
            Debug.Assert(this is Thread);
            return (Thread)this;
        }

        public static RuntimeThread CurrentThread => Thread.CurrentThread;

        /*=========================================================================
        ** Returns true if the thread has been started and is not dead.
        =========================================================================*/
        public extern bool IsAlive
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        /*=========================================================================
        ** Return whether or not this thread is a background thread.  Background
        ** threads do not affect when the Execution Engine shuts down.
        **
        ** Exceptions: ThreadStateException if the thread is dead.
        =========================================================================*/
        public bool IsBackground
        {
            get { return IsBackgroundNative(); }
            set { SetBackgroundNative(value); }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern bool IsBackgroundNative();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void SetBackgroundNative(bool isBackground);

        /*=========================================================================
        ** Returns true if the thread is a threadpool thread.
        =========================================================================*/
        public extern bool IsThreadPoolThread
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        public int ManagedThreadId => AsThread().ManagedThreadId;
        public string Name { get { return AsThread().Name; } set { AsThread().Name = value; } }

        /*=========================================================================
        ** Returns the priority of the thread.
        **
        ** Exceptions: ThreadStateException if the thread is dead.
        =========================================================================*/
        public ThreadPriority Priority
        {
            get { return (ThreadPriority)GetPriorityNative(); }
            set { SetPriorityNative((int)value); }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern int GetPriorityNative();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void SetPriorityNative(int priority);

        /*=========================================================================
        ** Returns the operating system identifier for the current thread.
        =========================================================================*/
        internal static ulong CurrentOSThreadId
        {
            get
            {
                return GetCurrentOSThreadId();
            }
        }
        [DllImport(JitHelpers.QCall)]
        private static extern ulong GetCurrentOSThreadId();

        /*=========================================================================
        ** Return the thread state as a consistent set of bits.  This is more
        ** general then IsAlive or IsBackground.
        =========================================================================*/
        public ThreadState ThreadState
        {
            get { return (ThreadState)GetThreadStateNative(); }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern int GetThreadStateNative();

        public ApartmentState GetApartmentState()
        {
#if FEATURE_COMINTEROP_APARTMENT_SUPPORT
            return (ApartmentState)GetApartmentStateNative();
#else // !FEATURE_COMINTEROP_APARTMENT_SUPPORT
            Debug.Assert(false); // the Thread class in CoreFX should have handled this case
            return ApartmentState.MTA;
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT
        }

        /*=========================================================================
        ** An unstarted thread can be marked to indicate that it will host a
        ** single-threaded or multi-threaded apartment.
        =========================================================================*/
        public bool TrySetApartmentState(ApartmentState state)
        {
#if FEATURE_COMINTEROP_APARTMENT_SUPPORT
            return SetApartmentStateHelper(state, false);
#else // !FEATURE_COMINTEROP_APARTMENT_SUPPORT
            Debug.Assert(false); // the Thread class in CoreFX should have handled this case
            return false;
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT
        }

#if FEATURE_COMINTEROP_APARTMENT_SUPPORT
        internal bool SetApartmentStateHelper(ApartmentState state, bool fireMDAOnMismatch)
        {
            ApartmentState retState = (ApartmentState)SetApartmentStateNative((int)state, fireMDAOnMismatch);

            // Special case where we pass in Unknown and get back MTA.
            //  Once we CoUninitialize the thread, the OS will still
            //  report the thread as implicitly in the MTA if any
            //  other thread in the process is CoInitialized.
            if ((state == System.Threading.ApartmentState.Unknown) && (retState == System.Threading.ApartmentState.MTA))
                return true;

            if (retState != state)
                return false;

            return true;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern int GetApartmentStateNative();

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern int SetApartmentStateNative(int state, bool fireMDAOnMismatch);
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

#if FEATURE_COMINTEROP
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern void DisableComObjectEagerCleanup();
#else // !FEATURE_COMINTEROP
        public void DisableComObjectEagerCleanup()
        {
            Debug.Assert(false); // the Thread class in CoreFX should have handled this case
        }
#endif // FEATURE_COMINTEROP

        /*=========================================================================
        ** Interrupts a thread that is inside a Wait(), Sleep() or Join().  If that
        ** thread is not currently blocked in that manner, it will be interrupted
        ** when it next begins to block.
        =========================================================================*/
        public void Interrupt() => InterruptInternal();

        // Internal helper (since we can't place security demands on
        // ecalls/fcalls).
        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void InterruptInternal();

        /*=========================================================================
        ** Waits for the thread to die or for timeout milliseconds to elapse.
        ** Returns true if the thread died, or false if the wait timed out. If
        ** Timeout.Infinite is given as the parameter, no timeout will occur.
        **
        ** Exceptions: ArgumentException if timeout < -1 (Timeout.Infinite).
        **             ThreadInterruptedException if the thread is interrupted while waiting.
        **             ThreadStateException if the thread has not been started yet.
        =========================================================================*/
        public void Join() => JoinInternal(Timeout.Infinite);

        public bool Join(int millisecondsTimeout) => JoinInternal(millisecondsTimeout);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern bool JoinInternal(int millisecondsTimeout);

        public static void Sleep(int millisecondsTimeout) => Thread.Sleep(millisecondsTimeout);

        [DllImport(JitHelpers.QCall)]
        private static extern int GetOptimalMaxSpinWaitsPerSpinIterationInternal();

        /// <summary>
        /// Max value to be passed into <see cref="SpinWait(int)"/> for optimal delaying. This value is normalized to be
        /// appropriate for the processor.
        /// </summary>
        internal static int OptimalMaxSpinWaitsPerSpinIteration
        {
            get
            {
                if (s_optimalMaxSpinWaitsPerSpinIteration != 0)
                {
                    return s_optimalMaxSpinWaitsPerSpinIteration;
                }

                // This is done lazily because the first call to the function below in the process triggers a measurement that
                // takes a nontrivial amount of time if the measurement has not already been done in the backgorund.
                // See Thread::InitializeYieldProcessorNormalized(), which describes and calculates this value.
                s_optimalMaxSpinWaitsPerSpinIteration = GetOptimalMaxSpinWaitsPerSpinIterationInternal();
                Debug.Assert(s_optimalMaxSpinWaitsPerSpinIteration > 0);
                return s_optimalMaxSpinWaitsPerSpinIteration;
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int GetCurrentProcessorNumber();

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
            int currentProcessorId = GetCurrentProcessorNumber();

            // On Unix, GetCurrentProcessorNumber() is implemented in terms of sched_getcpu, which
            // doesn't exist on all platforms.  On those it doesn't exist on, GetCurrentProcessorNumber()
            // returns -1.  As a fallback in that case and to spread the threads across the buckets
            // by default, we use the current managed thread ID as a proxy.
            if (currentProcessorId < 0) currentProcessorId = Environment.CurrentManagedThreadId;

            // Add offset to make it clear that it is not guaranteed to be 0-based processor number
            currentProcessorId += 100;

            Debug.Assert(ProcessorIdRefreshRate <= ProcessorIdCacheCountDownMask);

            // Mask with int.MaxValue to ensure the execution Id is not negative
            t_currentProcessorIdCache = ((currentProcessorId << ProcessorIdCacheShift) & int.MaxValue) | ProcessorIdRefreshRate;

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

        public static void SpinWait(int iterations) => Thread.SpinWait(iterations);
        public static bool Yield() => Thread.Yield();

        public void Start() => AsThread().Start();
        public void Start(object parameter) => AsThread().Start(parameter);
    }
}
