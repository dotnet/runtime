// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Quic;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal abstract class MsQuicSafeHandle : SafeHandle
    {
        // The index must corespond to SafeHandleType enum value and the value must correspond to MsQuic logging abbreviation string.
        // This is used for our logging that uses the same format of object identification as MsQuic to easily correlate log events.
        private static readonly string[] TypeName = new string[]
        {
            " reg",
            "cnfg",
            "list",
            "conn",
            "strm"
        };

        private readonly Action<IntPtr> _releaseAction;
        private readonly string _traceId;

        public override bool IsInvalid => handle == IntPtr.Zero;

        public unsafe QUIC_HANDLE* QuicHandle => (QUIC_HANDLE*)DangerousGetHandle();

        protected unsafe MsQuicSafeHandle(QUIC_HANDLE* handle, Action<IntPtr> releaseAction, SafeHandleType safeHandleType)
            : base((IntPtr)handle, ownsHandle: true)
        {
            _releaseAction = releaseAction;
            _traceId = $"[{TypeName[(int)safeHandleType]}][0x{DangerousGetHandle():X11}]";

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

    internal enum SafeHandleType
    {
        Registration,
        Configuration,
        Listener,
        Connection,
        Stream
    }
}
