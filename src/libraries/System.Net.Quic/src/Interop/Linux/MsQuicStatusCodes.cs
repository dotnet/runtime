// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal static class MsQuicStatusCodes
    {
        internal const uint Success = 0;
        internal const uint Pending = unchecked((uint)-2);
        internal const uint Continue = unchecked((uint)-1);
        internal const uint OutOfMemory = 12;
        internal const uint InvalidParameter = 22;
        internal const uint InvalidState = 200000002;
        internal const uint NotSupported = 95;
        internal const uint NotFound = 2;
        internal const uint BufferTooSmall = 75;
        internal const uint HandshakeFailure = 200000009;
        internal const uint Aborted = 200000008;
        internal const uint AddressInUse = 98;
        internal const uint ConnectionTimeout = 110;
        internal const uint ConnectionIdle = 200000011;
        internal const uint InternalError = 200000012;
        internal const uint ServerBusy = 200000007;
        internal const uint ProtocolError = 200000013;
        internal const uint VerNegError = 200000014;

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
