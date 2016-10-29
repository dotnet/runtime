// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net.Sockets
{
    internal sealed class SafeNativeOverlapped : SafeHandle
    {
        internal static SafeNativeOverlapped Zero { get; } = new SafeNativeOverlapped();

        private SafeNativeOverlapped()
            : this(IntPtr.Zero)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Info(this);
        }

        private SafeNativeOverlapped(IntPtr handle)
            : base(IntPtr.Zero, true)
        {
            SetHandle(handle);
        }

        public unsafe SafeNativeOverlapped(SafeCloseSocket socketHandle, NativeOverlapped* handle)
            : this((IntPtr)handle)
        {
            SocketHandle = socketHandle;

            if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"socketHandle:{socketHandle}");

#if DEBUG
            SocketHandle.AddRef();
#endif
        }

        internal unsafe void ReplaceHandle(NativeOverlapped* overlapped)
        {
            Debug.Assert(handle == IntPtr.Zero, "We should only be replacing the handle when it's already been freed.");
            SetHandle((IntPtr)overlapped);
        }

        internal SafeCloseSocket SocketHandle { get; }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }

        protected override bool ReleaseHandle()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Info(this);

            FreeNativeOverlapped();

#if DEBUG
            SocketHandle.Release();
#endif
            return true;
        }

        internal void FreeNativeOverlapped()
        {
            // Do not call free during AppDomain shutdown, there may be an outstanding operation.
            // Overlapped will take care calling free when the native callback completes.
            IntPtr oldHandle = Interlocked.Exchange(ref handle, IntPtr.Zero);
            if (oldHandle != IntPtr.Zero && !Environment.HasShutdownStarted)
            {
                unsafe
                {
                    Debug.Assert(SocketHandle != null, "SocketHandle is null.");

                    ThreadPoolBoundHandle boundHandle = SocketHandle.IOCPBoundHandle;
                    Debug.Assert(boundHandle != null, "SafeNativeOverlapped::FreeNativeOverlapped - boundHandle is null");

                    // FreeNativeOverlapped will be called even if boundHandle was previously disposed.
                    boundHandle?.FreeNativeOverlapped((NativeOverlapped*)oldHandle);
                }
            }
        }
    }
}
