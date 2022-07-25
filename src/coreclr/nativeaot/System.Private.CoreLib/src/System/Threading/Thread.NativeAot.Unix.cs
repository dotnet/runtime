// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;

namespace System.Threading
{
    public sealed partial class Thread
    {
        // Event signaling that the thread has stopped
        private ManualResetEvent _stopped;

        private WaitSubsystem.ThreadWaitInfo _waitInfo;

        internal WaitSubsystem.ThreadWaitInfo WaitInfo => _waitInfo;

        private void PlatformSpecificInitialize()
        {
            _waitInfo = new WaitSubsystem.ThreadWaitInfo(this);
        }

        // Platform-specific initialization of foreign threads, i.e. threads not created by Thread.Start
        private void PlatformSpecificInitializeExistingThread()
        {
            _stopped = new ManualResetEvent(false);
        }

#pragma warning disable CA1822
        private ThreadPriority GetPriorityLive()
        {
            return ThreadPriority.Normal;
        }

        private bool SetPriorityLive(ThreadPriority priority)
        {
            return true;
        }
#pragma warning restore CA1822

        [UnmanagedCallersOnly]
        private static void OnThreadExit()
        {
            Thread? currentThread = t_currentThread;
            if (currentThread != null)
            {
                // Inform the wait subsystem that the thread is exiting. For instance, this would abandon any mutexes locked by
                // the thread.
                currentThread._waitInfo.OnThreadExiting();
                StopThread(currentThread);
                currentThread._stopped.Set();
            }
        }

        private bool JoinInternal(int millisecondsTimeout)
        {
            // This method assumes the thread has been started
            Debug.Assert(!GetThreadStateBit(ThreadState.Unstarted) || (millisecondsTimeout == 0));
            SafeWaitHandle waitHandle = _stopped.SafeWaitHandle;

            // If an OS thread is terminated and its Thread object is resurrected, waitHandle may be finalized and closed
            if (waitHandle.IsClosed)
            {
                return true;
            }

            // Prevent race condition with the finalizer
            try
            {
                waitHandle.DangerousAddRef();
            }
            catch (ObjectDisposedException)
            {
                return true;
            }

            try
            {
                return _stopped.WaitOne(millisecondsTimeout);
            }
            finally
            {
                waitHandle.DangerousRelease();
            }
        }

        private unsafe bool CreateThread(GCHandle thisThreadHandle)
        {
            // Create the Stop event before starting the thread to make sure
            // it is ready to be signaled at thread shutdown time.
            // This also avoids OOM after creating the thread.
            _stopped = new ManualResetEvent(false);

            if (!Interop.Sys.CreateThread((IntPtr)_startHelper!._maxStackSize, &ThreadEntryPoint, (IntPtr)thisThreadHandle))
            {
                return false;
            }

            // CoreCLR ignores OS errors while setting the priority, so do we
            SetPriorityLive(_priority);

            return true;
        }

        /// <summary>
        /// This is an entry point for managed threads created by application
        /// </summary>
        [UnmanagedCallersOnly]
        private static IntPtr ThreadEntryPoint(IntPtr parameter)
        {
            StartThread(parameter);
            return IntPtr.Zero;
        }

        public ApartmentState GetApartmentState()
        {
            return ApartmentState.Unknown;
        }

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

        partial void InitializeComOnNewThread();

        internal static void InitializeComForFinalizerThread()
        {
        }

        public void DisableComObjectEagerCleanup() { }

        public void Interrupt() => WaitSubsystem.Interrupt(this);

        internal const bool ReentrantWaitsEnabled = false;

        internal static void SuppressReentrantWaits()
        {
            throw new PlatformNotSupportedException();
        }

        internal static void RestoreReentrantWaits()
        {
            throw new PlatformNotSupportedException();
        }

        private static int ComputeCurrentProcessorId()
        {
            int processorId = Interop.Sys.SchedGetCpu();

            // sched_getcpu doesn't exist on all platforms. On those it doesn't exist on, the shim
            // returns -1.  As a fallback in that case and to spread the threads across the buckets
            // by default, we use the current managed thread ID as a proxy.
            if (processorId < 0) processorId = Environment.CurrentManagedThreadId;

            return processorId;
        }
    }
}
