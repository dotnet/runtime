// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Quic;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal sealed class SafeMsQuicRegistrationHandle : MsQuicSafeHandle
    {
        public unsafe SafeMsQuicRegistrationHandle(QUIC_HANDLE* handle)
            : base(handle, ptr => MsQuicApi.Api.ApiTable->RegistrationClose((QUIC_HANDLE*)ptr), SafeHandleType.Registration)
        { }
    }
}
