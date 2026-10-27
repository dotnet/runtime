// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    public sealed partial class Thread
    {
        // Event signaling that the thread has stopped
        private ManualResetEvent _stopped;

        private SafeWaitHandle GetJoinHandle() => _stopped.SafeWaitHandle;

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
                currentThread.SetJoinHandle();
            }
        }

        private unsafe bool CreateThread(GCHandle<Thread> thisThreadHandle)
        {
            // Create the Stop event before starting the thread to make sure
            // it is ready to be signaled at thread shutdown time.
            // This also avoids OOM after creating the thread.
            _stopped = new ManualResetEvent(false);

            nint stackSize = _startHelper!._maxStackSize;

            if (stackSize <= 0)
            {
                stackSize = RuntimeImports.RhGetDefaultStackSize();
            }

            if (!Interop.Sys.CreateThread(stackSize, RuntimeImports.RhGetThreadEntryPointAddress(), GCHandle<Thread>.ToIntPtr(thisThreadHandle)))
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
        [UnmanagedCallersOnly(EntryPoint = "ThreadEntryPoint")]
        private static IntPtr ThreadEntryPoint(IntPtr parameter)
        {
            StartThread(parameter);
            return IntPtr.Zero;
        }

        public void Interrupt() => WaitSubsystem.Interrupt(this);
    }
}
