// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace System.Threading
{
    //
    // Windows-specific implementation of Timer
    //
    internal partial class TimerQueue
    {
        private IntPtr _nativeTimer;
        private readonly int _id;

        private TimerQueue(int id)
        {
            _id = id;
        }

        [UnmanagedCallersOnly]
        private static unsafe void TimerCallback(void* instance, void* context, void* timer)
        {
            int id = (int)context;
            var wrapper = ThreadPoolCallbackWrapper.Enter();
            Instances[id].FireNextTimers();
            ThreadPool.IncrementCompletedWorkItemCount();
            wrapper.Exit();
        }

        private unsafe bool SetTimer(uint actualDuration)
        {
            if (_nativeTimer == IntPtr.Zero)
            {
                _nativeTimer = Interop.Kernel32.CreateThreadpoolTimer(&TimerCallback, (IntPtr)_id, IntPtr.Zero);
                if (_nativeTimer == IntPtr.Zero)
                    throw new OutOfMemoryException();
            }

            // Negative time indicates the amount of time to wait relative to the current time, in 100 nanosecond units
            long dueTime = -10000 * (long)actualDuration;
            Interop.Kernel32.SetThreadpoolTimer(_nativeTimer, &dueTime, 0, 0);

            return true;
        }
    }
}
