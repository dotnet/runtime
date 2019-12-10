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
