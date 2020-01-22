// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal static class MsQuicStatusCodes
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
        internal const uint ConnectionTimeout = 0x800704CF;
        internal const uint ConnectionIdle = 0x800704D4;
        internal const uint InternalError = 0x80004005;
        internal const uint ServerBusy = 0x800704C9;
        internal const uint ProtocolError = 0x800704CD;
        internal const uint HostUnreachable = 0x800704D0;
        internal const uint VerNegError = 0x80410001;

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
                InternalError => "INTERNAL_ERROR",
                ServerBusy => "SERVER_BUSY",
                ProtocolError => "PROTOCOL_ERROR",
                VerNegError => "VER_NEG_ERROR",
                _ => status.ToString()
            };
        }
    }
}
