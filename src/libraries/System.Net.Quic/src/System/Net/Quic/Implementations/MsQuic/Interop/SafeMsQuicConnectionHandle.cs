// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Quic;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal sealed class SafeMsQuicConnectionHandle : MsQuicSafeHandle
    {
        public unsafe SafeMsQuicConnectionHandle(QUIC_HANDLE* handle)
            : base(handle, ptr => MsQuicApi.Api.ApiTable->ConnectionClose((QUIC_HANDLE*)ptr), SafeHandleType.Connection)
        { }
    }
}
