// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Quic;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal abstract class MsQuicSafeHandle : SafeHandle
    {
        private readonly Action<IntPtr> _releaseAction;
        private readonly string _traceId;

        public override bool IsInvalid => handle == IntPtr.Zero;

        public unsafe QUIC_HANDLE* QuicHandle => (QUIC_HANDLE*)DangerousGetHandle();

        protected unsafe MsQuicSafeHandle(QUIC_HANDLE* handle, Action<IntPtr> releaseAction, string prefix)
            : base((IntPtr)handle, ownsHandle: true)
        {
            _releaseAction = releaseAction;
            _traceId = $"[{prefix}][0x{DangerousGetHandle():X11}]";

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(this, "MsQuicSafeHandle created");
            }
        }

        protected override bool ReleaseHandle()
        {
            _releaseAction(handle);
            SetHandle(IntPtr.Zero);

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(this, "MsQuicSafeHandle released");
            }

            return true;
        }

        public override string ToString() => _traceId;
    }
}
