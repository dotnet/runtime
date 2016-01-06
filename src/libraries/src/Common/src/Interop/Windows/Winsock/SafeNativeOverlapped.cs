// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net.Sockets
{
    internal class SafeNativeOverlapped : SafeHandle
    {
        private static readonly SafeNativeOverlapped s_zero = new SafeNativeOverlapped();
        private SafeCloseSocket _safeCloseSocket;

        internal static SafeNativeOverlapped Zero { get { return s_zero; } }

        protected SafeNativeOverlapped()
            : this(IntPtr.Zero)
        {
            GlobalLog.Print("SafeNativeOverlapped#" + LoggingHash.HashString(this) + "::ctor(null)");
        }

        protected SafeNativeOverlapped(IntPtr handle)
            : base(IntPtr.Zero, true)
        {
            SetHandle(handle);
        }

        public unsafe SafeNativeOverlapped(SafeCloseSocket socketHandle, NativeOverlapped* handle)
            : this((IntPtr)handle)
        {
            _safeCloseSocket = socketHandle;

            GlobalLog.Print("SafeNativeOverlapped#" + LoggingHash.HashString(this) + "::ctor(socket#" + LoggingHash.HashString(socketHandle) + ")");
#if DEBUG
            _safeCloseSocket.AddRef();
#endif
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // It is important that the boundHandle is released immediately to allow new overlapped operations.
                GlobalLog.Print("SafeNativeOverlapped#" + LoggingHash.HashString(this) + "::Dispose(true)");
                FreeNativeOverlapped();
            }
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }

        protected override bool ReleaseHandle()
        {
            GlobalLog.Print("SafeNativeOverlapped#" + LoggingHash.HashString(this) + "::ReleaseHandle()");

            FreeNativeOverlapped();
            return true;
        }

        private void FreeNativeOverlapped()
        {
            IntPtr oldHandle = Interlocked.Exchange(ref handle, IntPtr.Zero);

            // Do not call free during AppDomain shutdown, there may be an outstanding operation.
            // Overlapped will take care calling free when the native callback completes.
            if (oldHandle != IntPtr.Zero && !Environment.HasShutdownStarted)
            {
                unsafe
                {
                    Debug.Assert(_safeCloseSocket != null, "m_SafeCloseSocket is null.");

                    ThreadPoolBoundHandle boundHandle = _safeCloseSocket.IOCPBoundHandle;
                    Debug.Assert(boundHandle != null, "SafeNativeOverlapped::ImmediatelyFreeNativeOverlapped - boundHandle is null");

                    if (boundHandle != null)
                    {
                        // FreeNativeOverlapped will be called even if boundHandle was previously disposed.
                        boundHandle.FreeNativeOverlapped((NativeOverlapped*)oldHandle);
                    }

#if DEBUG
                    _safeCloseSocket.Release();
#endif
                    _safeCloseSocket = null;
                }
            }
            return;
        }
    }
}
