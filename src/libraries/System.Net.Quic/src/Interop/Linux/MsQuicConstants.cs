// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal static class MsQuicConstants
    {
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
            switch (status)
            {
                case Success:
                    return "SUCCESS";
                case Pending:
                    return "PENDING";
                case Continue:
                    return "CONTINUE";
                case OutOfMemory:
                    return "OUT_OF_MEMORY";
                case InvalidParameter:
                    return "INVALID_PARAMETER";
                case InvalidState:
                    return "INVALID_STATE";
                case NotSupported:
                    return "NOT_SUPPORTED";
                case NotFound:
                    return "NOT_FOUND";
                case BufferTooSmall:
                    return "BUFFER_TOO_SMALL";
                case HandshakeFailure:
                    return "HANDSHAKE_FAILURE";
                case Aborted:
                    return "ABORTED";
                case AddressInUse:
                    return "ADDRESS_IN_USE";
                case ConnectionTimeout:
                    return "CONNECTION_TIMEOUT";
                case ConnectionIdle:
                    return "CONNECTION_IDLE";
                case InternalError:
                    return "INTERNAL_ERROR";
                case ServerBusy:
                    return "SERVER_BUSY";
                case ProtocolError:
                    return "PROTOCOL_ERROR";
                case VerNegError:
                    return "VER_NEG_ERROR";
            }

            return status.ToString();
        }
    }
}
