// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal static partial class MsQuicStatusCodes
    {
        internal const uint Success = 0;
        internal const uint Pending = unchecked((uint)-2);
        internal const uint Continue = unchecked((uint)-1);
        internal const uint OutOfMemory = 12;           // ENOMEM
        internal const uint InvalidParameter = 22;      // EINVAL
        internal const uint InvalidState = 1;           // EPERM
        internal const uint NotSupported = 95;          // EOPNOTSUPP
        internal const uint NotFound = 2;               // ENOENT
        internal const uint BufferTooSmall = 75;        // EOVERFLOW
        internal const uint HandshakeFailure = 103;     // ECONNABORTED
        internal const uint Aborted = 125;              // ECANCELED
        internal const uint AddressInUse = 98;          // EADDRINUSE
        internal const uint ConnectionTimeout = 110;    // ETIMEDOUT
        internal const uint ConnectionIdle = 62;        // ETIME
        internal const uint HostUnreachable = 113;      // EHOSTUNREACH
        internal const uint InternalError = 5;          // EIO
        internal const uint ConnectionRefused = 111;    // ECONNREFUSED
        internal const uint ProtocolError = 71;         // EPROTO
        internal const uint VerNegError = 93;           // EPROTONOSUPPORT
        internal const uint TlsError = 126;             // ENOKEY
        internal const uint UserCanceled = 130;         // EOWNERDEAD
        internal const uint AlpnNegotiationFailure = 92;// ENOPROTOOPT
        internal const uint StreamLimit = 86;           // ESTRPIPE
    }
}
