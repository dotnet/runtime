// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal static partial class MsQuicStatusCodes
    {
        internal const uint Success = 0;
        internal const uint Pending = 0x703E5;
        internal const uint Continue = 0x704DE;
        internal const uint OutOfMemory = 0x8007000E;
        internal const uint InvalidParameter = 0x80070057;
        internal const uint InvalidState = 0x8007139F;
        internal const uint NotSupported = 0x80004002;
        internal const uint NotFound = 0x80070490;
        internal const uint BufferTooSmall = 0x8007007A;
        internal const uint HandshakeFailure = 0x80410000;
        internal const uint Aborted = 0x80004004;
        internal const uint AddressInUse = 0x80072740;
        internal const uint ConnectionTimeout = 0x80410006;
        internal const uint ConnectionIdle = 0x80410005;
        internal const uint HostUnreachable = 0x800704D0;
        internal const uint InternalError = 0x80410003;
        internal const uint ConnectionRefused = 0x800704C9;
        internal const uint ProtocolError = 0x80410004;
        internal const uint VerNegError = 0x80410001;
        internal const uint TlsError = 0x80072B18;
        internal const uint UserCanceled = 0x80410002;
        internal const uint AlpnNegotiationFailure = 0x80410007;
        internal const uint StreamLimit = 0x80410008;
    }
}
