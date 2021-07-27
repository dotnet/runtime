// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal static partial class MsQuicStatusCodes
    {
        // TODO return better error messages here.
        public static string GetError(uint status)
        {
            return status switch
            {
                Success => "SUCCESS",
                Pending => "PENDING",
                Continue => "CONTINUE",
                OutOfMemory => "OUT_OF_MEMORY",
                InvalidParameter => "INVALID_PARAMETER",
                InvalidState => "INVALID_STATE",
                NotSupported => "NOT_SUPPORTED",
                NotFound => "NOT_FOUND",
                BufferTooSmall => "BUFFER_TOO_SMALL",
                HandshakeFailure => "HANDSHAKE_FAILURE",
                Aborted => "ABORTED",
                AddressInUse => "ADDRESS_IN_USE",
                ConnectionTimeout => "CONNECTION_TIMEOUT",
                ConnectionIdle => "CONNECTION_IDLE",
                HostUnreachable => "UNREACHABLE",
                InternalError => "INTERNAL_ERROR",
                ConnectionRefused => "CONNECTION_REFUSED",
                ProtocolError => "PROTOCOL_ERROR",
                VerNegError => "VER_NEG_ERROR",
                TlsError => "TLS_ERROR",
                UserCanceled => "USER_CANCELED",
                AlpnNegotiationFailure => "ALPN_NEG_FAILURE",
                StreamLimit => "STREAM_LIMIT_REACHED",
                _ => $"0x{status:X8}"
            };
        }
    }
}
