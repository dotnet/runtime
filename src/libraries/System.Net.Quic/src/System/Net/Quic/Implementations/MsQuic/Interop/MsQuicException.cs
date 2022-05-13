// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Quic;
using System.Net.Sockets;
using static Microsoft.Quic.MsQuic;

namespace Microsoft.Quic
{
    internal sealed class MsQuicException : QuicException
    {
        public int Status { get; }

        public MsQuicException(int status, string? message = null, Exception? innerException = null)
            : base($"{(message ?? nameof(MsQuicException))}: {GetErrorCodeForStatus(status)}", innerException, MapMsQuicStatusToHResult(status))
        {
            Status = status;
        }

        public static string GetErrorCodeForStatus(int status)
        {
            if (status == MsQuic.QUIC_STATUS_SUCCESS) return "QUIC_STATUS_SUCCESS";
            else if (status == MsQuic.QUIC_STATUS_PENDING) return "QUIC_STATUS_PENDING";
            else if (status == MsQuic.QUIC_STATUS_CONTINUE) return "QUIC_STATUS_CONTINUE";
            else if (status == MsQuic.QUIC_STATUS_OUT_OF_MEMORY) return "QUIC_STATUS_OUT_OF_MEMORY";
            else if (status == MsQuic.QUIC_STATUS_INVALID_PARAMETER) return "QUIC_STATUS_INVALID_PARAMETER";
            else if (status == MsQuic.QUIC_STATUS_INVALID_STATE) return "QUIC_STATUS_INVALID_STATE";
            else if (status == MsQuic.QUIC_STATUS_NOT_SUPPORTED) return "QUIC_STATUS_NOT_SUPPORTED";
            else if (status == MsQuic.QUIC_STATUS_NOT_FOUND) return "QUIC_STATUS_NOT_FOUND";
            else if (status == MsQuic.QUIC_STATUS_BUFFER_TOO_SMALL) return "QUIC_STATUS_BUFFER_TOO_SMALL";
            else if (status == MsQuic.QUIC_STATUS_HANDSHAKE_FAILURE) return "QUIC_STATUS_HANDSHAKE_FAILURE";
            else if (status == MsQuic.QUIC_STATUS_ABORTED) return "QUIC_STATUS_ABORTED";
            else if (status == MsQuic.QUIC_STATUS_ADDRESS_IN_USE) return "QUIC_STATUS_ADDRESS_IN_USE";
            else if (status == MsQuic.QUIC_STATUS_CONNECTION_TIMEOUT) return "QUIC_STATUS_CONNECTION_TIMEOUT";
            else if (status == MsQuic.QUIC_STATUS_CONNECTION_IDLE) return "QUIC_STATUS_CONNECTION_IDLE";
            else if (status == MsQuic.QUIC_STATUS_UNREACHABLE) return "QUIC_STATUS_UNREACHABLE";
            else if (status == MsQuic.QUIC_STATUS_INTERNAL_ERROR) return "QUIC_STATUS_INTERNAL_ERROR";
            else if (status == MsQuic.QUIC_STATUS_CONNECTION_REFUSED) return "QUIC_STATUS_CONNECTION_REFUSED";
            else if (status == MsQuic.QUIC_STATUS_PROTOCOL_ERROR) return "QUIC_STATUS_PROTOCOL_ERROR";
            else if (status == MsQuic.QUIC_STATUS_VER_NEG_ERROR) return "QUIC_STATUS_VER_NEG_ERROR";
            else if (status == MsQuic.QUIC_STATUS_TLS_ERROR) return "QUIC_STATUS_TLS_ERROR";
            else if (status == MsQuic.QUIC_STATUS_USER_CANCELED) return "QUIC_STATUS_USER_CANCELED";
            else if (status == MsQuic.QUIC_STATUS_ALPN_NEG_FAILURE) return "QUIC_STATUS_ALPN_NEG_FAILURE";
            else if (status == MsQuic.QUIC_STATUS_STREAM_LIMIT_REACHED) return "QUIC_STATUS_STREAM_LIMIT_REACHED";
            else if (status == MsQuic.QUIC_STATUS_CLOSE_NOTIFY) return "QUIC_STATUS_CLOSE_NOTIFY";
            else if (status == MsQuic.QUIC_STATUS_BAD_CERTIFICATE) return "QUIC_STATUS_BAD_CERTIFICATE";
            else if (status == MsQuic.QUIC_STATUS_UNSUPPORTED_CERTIFICATE) return "QUIC_STATUS_UNSUPPORTED_CERTIFICATE";
            else if (status == MsQuic.QUIC_STATUS_REVOKED_CERTIFICATE) return "QUIC_STATUS_REVOKED_CERTIFICATE";
            else if (status == MsQuic.QUIC_STATUS_EXPIRED_CERTIFICATE) return "QUIC_STATUS_EXPIRED_CERTIFICATE";
            else if (status == MsQuic.QUIC_STATUS_UNKNOWN_CERTIFICATE) return "QUIC_STATUS_UNKNOWN_CERTIFICATE";
            else if (status == MsQuic.QUIC_STATUS_CERT_EXPIRED) return "QUIC_STATUS_CERT_EXPIRED";
            else if (status == MsQuic.QUIC_STATUS_CERT_UNTRUSTED_ROOT) return "QUIC_STATUS_CERT_UNTRUSTED_ROOT";
            else return $"Unknown status '{status}'";
        }

        public static int MapMsQuicStatusToHResult(int status)
        {
            if (status == QUIC_STATUS_CONNECTION_REFUSED) return (int)SocketError.ConnectionRefused;  // 0x8007274D - WSAECONNREFUSED
            else if (status == QUIC_STATUS_CONNECTION_TIMEOUT) return (int)SocketError.TimedOut;      // 0x8007274C - WSAETIMEDOUT
            else if (status == QUIC_STATUS_UNREACHABLE) return (int)SocketError.HostUnreachable;
            else return 0;
        }
    }
}
