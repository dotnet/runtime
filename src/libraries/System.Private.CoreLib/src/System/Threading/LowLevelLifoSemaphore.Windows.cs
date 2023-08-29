// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Threading
{
    /// <summary>
    /// A LIFO semaphore implemented using Win32 IO Completion Ports.
    /// </summary>
    /// <remarks>
    /// IO Completion ports release waiting threads in LIFO order, so we can use them to create a LIFO semaphore.
    /// See https://msdn.microsoft.com/en-us/library/windows/desktop/aa365198(v=vs.85).aspx under How I/O Completion Ports Work.
    /// From the docs "Threads that block their execution on an I/O completion port are released in last-in-first-out (LIFO) order."
    /// </remarks>
    internal sealed partial class LowLevelLifoSemaphore : IDisposable
    {
        private IntPtr _completionPort;

        private void Create(int maximumSignalCount)
        {
            Debug.Assert(maximumSignalCount > 0);

            _completionPort =
                Interop.Kernel32.CreateIoCompletionPort(new IntPtr(-1), IntPtr.Zero, UIntPtr.Zero, maximumSignalCount);
            if (_completionPort == IntPtr.Zero)
            {
                int hr = Marshal.GetHRForLastWin32Error();
                var exception = new OutOfMemoryException();
                exception.HResult = hr;
                throw exception;
            }
        }

        ~LowLevelLifoSemaphore()
        {
            if (_completionPort != IntPtr.Zero)
            {
                Dispose();
            }
        }

        public bool WaitCore(int timeoutMs)
        {
            Debug.Assert(timeoutMs >= -1);

            bool success = Interop.Kernel32.GetQueuedCompletionStatus(_completionPort, out _, out _, out _, timeoutMs);
            Debug.Assert(success || (Marshal.GetLastPInvokeError() == WaitHandle.WaitTimeout));
            return success;
        }

        protected override void ReleaseCore(int count)
        {
            Debug.Assert(count > 0);

            for (int i = 0; i < count; i++)
            {
                if (!Interop.Kernel32.PostQueuedCompletionStatus(_completionPort, 1, UIntPtr.Zero, IntPtr.Zero))
                {
                    int lastError = Marshal.GetLastPInvokeError();
                    var exception = new OutOfMemoryException();
                    exception.HResult = lastError;
                    throw exception;
                }
            }
        }

        public void Dispose()
        {
            Debug.Assert(_completionPort != IntPtr.Zero);

            Interop.Kernel32.CloseHandle(_completionPort);
            _completionPort = IntPtr.Zero;
            GC.SuppressFinalize(this);
        }
    }
}
