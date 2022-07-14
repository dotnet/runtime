// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Quic;
using System.Security.Authentication;
using static Microsoft.Quic.MsQuic;

namespace System.Net.Quic;

internal static class ThrowHelper
{
    internal static QuicException GetConnectionAbortedException(long errorCode)
    {
        return errorCode switch
        {
            -1 => GetOperationAbortedException(), // Shutdown initiated by us.
            long err => new QuicException(QuicError.ConnectionAborted, err, SR.Format(SR.net_quic_connectionaborted, err)) // Shutdown initiated by peer.
        };
    }

    internal static QuicException GetStreamAbortedException(long errorCode)
    {
        return errorCode switch
        {
            -1 => GetOperationAbortedException(), // Shutdown initiated by us.
            long err => new QuicException(QuicError.StreamAborted, err, SR.Format(SR.net_quic_streamaborted, err)) // Shutdown initiated by peer.
        };
    }

    internal static QuicException GetOperationAbortedException(string? message = null)
    {
        return new QuicException(QuicError.OperationAborted, null, message ?? SR.net_quic_operationaborted);
    }

    internal static Exception GetExceptionForMsQuicStatus(int status, string? message = null)
    {
        Exception ex = GetExceptionInternal(status, message);
        if (status != 0)
        {
            // Include the raw MsQuic status in the HResult property for better diagnostics
            ex.HResult = status;
        }

        return ex;

        static Exception GetExceptionInternal(int status, string? message)
        {
            //
            // Start by checking for statuses mapped to QuicError enum
            //
            if (status == QUIC_STATUS_ADDRESS_IN_USE) return new QuicException(QuicError.AddressInUse, null, SR.net_quic_address_in_use);
            if (status == QUIC_STATUS_UNREACHABLE) return new QuicException(QuicError.HostUnreachable, null, SR.net_quic_host_unreachable);
            if (status == QUIC_STATUS_CONNECTION_REFUSED) return new QuicException(QuicError.ConnectionRefused, null, SR.net_quic_connection_refused);
            if (status == QUIC_STATUS_VER_NEG_ERROR) return new QuicException(QuicError.VersionNegotiationError, null, SR.net_quic_ver_neg_error);
            if (status == QUIC_STATUS_INVALID_ADDRESS) return new QuicException(QuicError.InvalidAddress, null, SR.net_quic_invalid_address);
            if (status == QUIC_STATUS_CONNECTION_IDLE) return new QuicException(QuicError.ConnectionIdle, null, SR.net_quic_connection_idle);
            if (status == QUIC_STATUS_PROTOCOL_ERROR) return new QuicException(QuicError.ProtocolError, null, SR.net_quic_protocol_error);

            if (status == QUIC_STATUS_TLS_ERROR ||
                status == QUIC_STATUS_CERT_EXPIRED ||
                status == QUIC_STATUS_CERT_UNTRUSTED_ROOT ||
                status == QUIC_STATUS_CERT_NO_CERT)
            {
                return new AuthenticationException(SR.Format(SR.net_quic_auth, GetErrorMessageForStatus(status, message)));
            }

            //
            // Although ALPN negotiation failure is triggered by a TLS Alert, it is mapped differently
            //
            if (status == QUIC_STATUS_ALPN_NEG_FAILURE)
            {
                return new AuthenticationException(SR.net_quic_alpn_neg_error);
            }

            //
            // other TLS Alerts: MsQuic maps TLS alerts by offsetting them by a
            // certain value. CloseNotify is the TLS Alert with value 0x00, so
            // all TLS Alert codes are mapped to [QUIC_STATUS_CLOSE_NOTIFY,
            // QUIC_STATUS_CLOSE_NOTIFY + 255]
            //
            // Mapped TLS alerts include following statuses:
            //  - QUIC_STATUS_CLOSE_NOTIFY
            //  - QUIC_STATUS_BAD_CERTIFICATE
            //  - QUIC_STATUS_UNSUPPORTED_CERTIFICATE
            //  - QUIC_STATUS_REVOKED_CERTIFICATE
            //  - QUIC_STATUS_EXPIRED_CERTIFICATE
            //  - QUIC_STATUS_UNKNOWN_CERTIFICATE
            //  - QUIC_STATUS_REQUIRED_CERTIFICATE
            //
            if ((uint)status >= (uint)QUIC_STATUS_CLOSE_NOTIFY && (uint)status < (uint)QUIC_STATUS_CLOSE_NOTIFY + 256)
            {
                int alert = status - QUIC_STATUS_CLOSE_NOTIFY;
                return new AuthenticationException(SR.Format(SR.net_auth_tls_alert, alert));
            }

            //
            // for everything else, use general InternalError
            //
            return new QuicException(QuicError.InternalError, null, SR.Format(SR.net_quic_internal_error, GetErrorMessageForStatus(status, message)));
        }
    }

    internal static void ThrowIfMsQuicError(int status, string? message = null)
    {
        if (StatusFailed(status))
        {
            throw GetExceptionForMsQuicStatus(status, message);
        }
    }

    internal static string GetErrorMessageForStatus(int status, string? message)
    {
        return (message ?? "Status code") + ": " + GetErrorMessageForStatus(status);
    }

    internal static string GetErrorMessageForStatus(int status)
    {
        if (status == QUIC_STATUS_SUCCESS) return "QUIC_STATUS_SUCCESS";
        else if (status == QUIC_STATUS_PENDING) return "QUIC_STATUS_PENDING";
        else if (status == QUIC_STATUS_CONTINUE) return "QUIC_STATUS_CONTINUE";
        else if (status == QUIC_STATUS_OUT_OF_MEMORY) return "QUIC_STATUS_OUT_OF_MEMORY";
        else if (status == QUIC_STATUS_INVALID_PARAMETER) return "QUIC_STATUS_INVALID_PARAMETER";
        else if (status == QUIC_STATUS_INVALID_STATE) return "QUIC_STATUS_INVALID_STATE";
        else if (status == QUIC_STATUS_NOT_SUPPORTED) return "QUIC_STATUS_NOT_SUPPORTED";
        else if (status == QUIC_STATUS_NOT_FOUND) return "QUIC_STATUS_NOT_FOUND";
        else if (status == QUIC_STATUS_BUFFER_TOO_SMALL) return "QUIC_STATUS_BUFFER_TOO_SMALL";
        else if (status == QUIC_STATUS_HANDSHAKE_FAILURE) return "QUIC_STATUS_HANDSHAKE_FAILURE";
        else if (status == QUIC_STATUS_ABORTED) return "QUIC_STATUS_ABORTED";
        else if (status == QUIC_STATUS_ADDRESS_IN_USE) return "QUIC_STATUS_ADDRESS_IN_USE";
        else if (status == QUIC_STATUS_INVALID_ADDRESS) return "QUIC_STATUS_INVALID_ADDRESS";
        else if (status == QUIC_STATUS_CONNECTION_TIMEOUT) return "QUIC_STATUS_CONNECTION_TIMEOUT";
        else if (status == QUIC_STATUS_CONNECTION_IDLE) return "QUIC_STATUS_CONNECTION_IDLE";
        else if (status == QUIC_STATUS_UNREACHABLE) return "QUIC_STATUS_UNREACHABLE";
        else if (status == QUIC_STATUS_INTERNAL_ERROR) return "QUIC_STATUS_INTERNAL_ERROR";
        else if (status == QUIC_STATUS_CONNECTION_REFUSED) return "QUIC_STATUS_CONNECTION_REFUSED";
        else if (status == QUIC_STATUS_PROTOCOL_ERROR) return "QUIC_STATUS_PROTOCOL_ERROR";
        else if (status == QUIC_STATUS_VER_NEG_ERROR) return "QUIC_STATUS_VER_NEG_ERROR";
        else if (status == QUIC_STATUS_TLS_ERROR) return "QUIC_STATUS_TLS_ERROR";
        else if (status == QUIC_STATUS_USER_CANCELED) return "QUIC_STATUS_USER_CANCELED";
        else if (status == QUIC_STATUS_ALPN_NEG_FAILURE) return "QUIC_STATUS_ALPN_NEG_FAILURE";
        else if (status == QUIC_STATUS_STREAM_LIMIT_REACHED) return "QUIC_STATUS_STREAM_LIMIT_REACHED";
        else if (status == QUIC_STATUS_CLOSE_NOTIFY) return "QUIC_STATUS_CLOSE_NOTIFY";
        else if (status == QUIC_STATUS_BAD_CERTIFICATE) return "QUIC_STATUS_BAD_CERTIFICATE";
        else if (status == QUIC_STATUS_UNSUPPORTED_CERTIFICATE) return "QUIC_STATUS_UNSUPPORTED_CERTIFICATE";
        else if (status == QUIC_STATUS_REVOKED_CERTIFICATE) return "QUIC_STATUS_REVOKED_CERTIFICATE";
        else if (status == QUIC_STATUS_EXPIRED_CERTIFICATE) return "QUIC_STATUS_EXPIRED_CERTIFICATE";
        else if (status == QUIC_STATUS_UNKNOWN_CERTIFICATE) return "QUIC_STATUS_UNKNOWN_CERTIFICATE";
        else if (status == QUIC_STATUS_REQUIRED_CERTIFICATE) return "QUIC_STATUS_REQUIRED_CERTIFICATE";
        else if (status == QUIC_STATUS_CERT_EXPIRED) return "QUIC_STATUS_CERT_EXPIRED";
        else if (status == QUIC_STATUS_CERT_UNTRUSTED_ROOT) return "QUIC_STATUS_CERT_UNTRUSTED_ROOT";
        else if (status == QUIC_STATUS_CERT_NO_CERT) return "QUIC_STATUS_CERT_NO_CERT";
        else return $"Unknown (0x{status:x})";
    }
}
