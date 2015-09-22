// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;

namespace System.Net.Sockets
{
    internal sealed class SafeCloseSocketAndEvent : SafeCloseSocket
    {
        internal SafeCloseSocketAndEvent() : base() { }
        private AutoResetEvent _waitHandle;

        override protected bool ReleaseHandle()
        {
            bool result = base.ReleaseHandle();
            DeleteEvent();
            return result;
        }

        internal static SafeCloseSocketAndEvent CreateWSASocketWithEvent(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType, bool autoReset, bool signaled)
        {
            SafeCloseSocketAndEvent result = new SafeCloseSocketAndEvent();
            CreateSocket(InnerSafeCloseSocket.CreateWSASocket(addressFamily, socketType, protocolType), result);
            if (result.IsInvalid)
            {
                throw new SocketException();
            }

            result._waitHandle = new AutoResetEvent(false);
            CompleteInitialization(result);
            return result;
        }

        internal static void CompleteInitialization(SafeCloseSocketAndEvent socketAndEventHandle)
        {
            SafeWaitHandle handle = socketAndEventHandle._waitHandle.GetSafeWaitHandle();
            bool b = false;
            try
            {
                handle.DangerousAddRef(ref b);
            }
            catch
            {
                if (b)
                {
                    handle.DangerousRelease();
                    socketAndEventHandle._waitHandle = null;
                    b = false;
                }
            }
            finally
            {
                if (b)
                {
                    handle.Dispose();
                }
            }
        }

        private void DeleteEvent()
        {
            try
            {
                if (_waitHandle != null)
                {
                    var waitHandleSafeWaitHandle = _waitHandle.GetSafeWaitHandle();
                    waitHandleSafeWaitHandle.DangerousRelease();
                }
            }
            catch
            {
            }
        }

        internal WaitHandle GetEventHandle()
        {
            return _waitHandle;
        }
    }

    // Because the regular SafeNetHandles has a LocalAlloc with a different return type.
    internal static class SafeNetHandlesSafeOverlappedFree
    {
        [DllImport(Interop.Libraries.Heap, ExactSpelling = true, SetLastError = true)]
        internal static extern SafeOverlappedFree LocalAlloc(int uFlags, UIntPtr sizetdwBytes);
    }

#if DEBUG
    internal sealed class SafeOverlappedFree : DebugSafeHandle
    {
#else
    internal sealed class SafeOverlappedFree : SafeHandleZeroOrMinusOneIsInvalid {
#endif
        private const int LPTR = 0x0040;

        internal static readonly SafeOverlappedFree Zero = new SafeOverlappedFree(false);

        private SafeCloseSocket _socketHandle;

        private SafeOverlappedFree() : base(true) { }
        private SafeOverlappedFree(bool ownsHandle) : base(ownsHandle) { }

        public static SafeOverlappedFree Alloc()
        {
            SafeOverlappedFree result = SafeNetHandlesSafeOverlappedFree.LocalAlloc(LPTR, (UIntPtr)Win32.OverlappedSize);
            if (result.IsInvalid)
            {
                result.SetHandleAsInvalid();
                throw new OutOfMemoryException();
            }
            return result;
        }

        public static SafeOverlappedFree Alloc(SafeCloseSocket socketHandle)
        {
            SafeOverlappedFree result = Alloc();
            result._socketHandle = socketHandle;
            return result;
        }

        public void Close(bool resetOwner)
        {
            if (resetOwner)
            {
                _socketHandle = null;
            }
            Dispose();
        }

        unsafe override protected bool ReleaseHandle()
        {
            SafeCloseSocket socketHandle = _socketHandle;
            if (socketHandle != null && !socketHandle.IsInvalid)
            {
                // We are being finalized while the I/O operation associated
                // with the current overlapped is still pending (e.g. on app
                // domain shutdown). The socket has to be closed first to
                // avoid reuse after delete of the native overlapped structure.
                socketHandle.Dispose();
            }
            // Release the native overlapped structure
            return UnsafeCommonNativeMethods.LocalFree(handle) == IntPtr.Zero;
        }
    }
}

