// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Quic;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal sealed class SafeMsQuicStreamHandle : MsQuicSafeHandle
    {
        public unsafe SafeMsQuicStreamHandle(QUIC_HANDLE* handle)
            : base(handle, ptr => MsQuicApi.Api.ApiTable->StreamClose((QUIC_HANDLE*)ptr), SafeHandleType.Stream)
        { }
    }
}
