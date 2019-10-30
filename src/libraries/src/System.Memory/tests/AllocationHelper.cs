// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Threading;

namespace System.SpanTests
{
    /// <summary>
    /// This class is used in testing functions that are allocating significantly large blocks
    /// of memory that could conceivably cause the machine to go OOM if more than one of the
    /// tests were run at the same time. This class will block any large allocation call until
    /// any prior tests have released the memory back to the system.
    /// </summary>
    static class AllocationHelper
    {
        private static readonly Mutex s_memoryLock = new Mutex();
        private static readonly TimeSpan s_waitTimeout = TimeSpan.FromSeconds(120);

        public static bool TryAllocNative(IntPtr size, out IntPtr memory)
        {
            memory = IntPtr.Zero;

            if (!s_memoryLock.WaitOne(s_waitTimeout))
                return false;

            try
            {
                memory = Marshal.AllocHGlobal(size);
            }
            catch (OutOfMemoryException)
            {
                memory = IntPtr.Zero;
                s_memoryLock.ReleaseMutex();
            }

            return memory != IntPtr.Zero;
        }

        public static void ReleaseNative(ref IntPtr memory)
        {
            try
            {
                Marshal.FreeHGlobal(memory);
                memory = IntPtr.Zero;
            }
            finally
            {
                s_memoryLock.ReleaseMutex();
            }
        }
    }
}
