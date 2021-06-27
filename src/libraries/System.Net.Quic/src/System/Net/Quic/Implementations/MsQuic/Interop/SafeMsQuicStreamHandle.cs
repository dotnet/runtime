// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal sealed class SafeMsQuicStreamHandle : SafeHandle
    {
        public override bool IsInvalid => handle == IntPtr.Zero;

        private SafeMsQuicStreamHandle()
            : base(IntPtr.Zero, ownsHandle: true)
        { }

        public SafeMsQuicStreamHandle(IntPtr streamHandle)
            : this()
        {
            SetHandle(streamHandle);
        }

        protected override bool ReleaseHandle()
        {
            MsQuicApi.Api.StreamCloseDelegate(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }
    }
}
