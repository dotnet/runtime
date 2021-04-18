// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal sealed class SafeMsQuicConnectionHandle : SafeHandle
    {
        public override bool IsInvalid => handle == IntPtr.Zero;

        private SafeMsQuicConnectionHandle()
            : base(IntPtr.Zero, ownsHandle: true)
        { }

        public SafeMsQuicConnectionHandle(IntPtr connectionHandle)
            : this()
        {
            SetHandle(connectionHandle);
        }

        protected override bool ReleaseHandle()
        {
            MsQuicApi.Api.ConnectionCloseDelegate(handle);
            return true;
        }
    }
}
