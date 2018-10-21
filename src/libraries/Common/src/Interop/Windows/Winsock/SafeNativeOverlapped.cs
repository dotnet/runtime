// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

// TODO: Investigate removing SafeNativeOverlapped entirely.  It shouldn't be needed.

namespace System.Net.Sockets
{
    internal sealed class SafeNativeOverlapped : SafeHandle
    {
        private readonly SafeSocketHandle _socketHandle;

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

        public unsafe SafeNativeOverlapped(SafeSocketHandle socketHandle, NativeOverlapped* handle)
            : this((IntPtr)handle)
        {
            _socketHandle = socketHandle;

            if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"socketHandle:{socketHandle}");

#if DEBUG
            _socketHandle.AddRef();
#endif
        }
        
        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }

        protected override bool ReleaseHandle()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Info(this);

            FreeNativeOverlapped();

#if DEBUG
            _socketHandle.Release();
#endif
            return true;
        }

        private void FreeNativeOverlapped()
        {
            // Do not call free during AppDomain shutdown, there may be an outstanding operation.
            // Overlapped will take care calling free when the native callback completes.
            IntPtr oldHandle = Interlocked.Exchange(ref handle, IntPtr.Zero);
            if (oldHandle != IntPtr.Zero && !Environment.HasShutdownStarted)
            {
                unsafe
                {
                    Debug.Assert(_socketHandle != null, "_socketHandle is null.");

                    ThreadPoolBoundHandle boundHandle = _socketHandle.IOCPBoundHandle;
                    Debug.Assert(boundHandle != null, "SafeNativeOverlapped::FreeNativeOverlapped - boundHandle is null");

                    // FreeNativeOverlapped will be called even if boundHandle was previously disposed.
                    boundHandle?.FreeNativeOverlapped((NativeOverlapped*)oldHandle);
                }
            }
        }
    }
}
