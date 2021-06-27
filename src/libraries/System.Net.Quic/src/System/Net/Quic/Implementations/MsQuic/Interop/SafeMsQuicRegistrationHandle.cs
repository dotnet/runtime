// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal sealed class SafeMsQuicRegistrationHandle : SafeHandle
    {
        public override bool IsInvalid => handle == IntPtr.Zero;

        private SafeMsQuicRegistrationHandle()
            : base(IntPtr.Zero, ownsHandle: true)
        { }

        protected override bool ReleaseHandle()
        {
            MsQuicApi.Api.RegistrationCloseDelegate(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }
    }
}
