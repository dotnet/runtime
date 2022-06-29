// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Quic
{
    public enum QuicError
    {
        Success,
        InternalError,
        ConnectionAborted,
        StreamAborted,
        AddressInUse,
        InvalidAddress,
        ConnectionTimeout,
        HostUnreachable,
        ConnectionRefused,
        VersionNegotiationError,
        ConnectionIdle,
        ProtocolError,
        OperationAborted,
    }
}
