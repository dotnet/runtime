// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Quic;
using System.Security.Authentication;
using static Microsoft.Quic.MsQuic;

namespace System.Net.Quic.Implementations.MsQuic
{
    internal static class ThrowHelper
    {
        internal static QuicException GetConnectionAbortedException(long errorCode)
        {
            return errorCode switch
            {
                -1 => GetOperationAbortedException(), // Shutdown initiated by us.
                long err => new QuicException(QuicError.ConnectionAborted, err, SR.Format(SR.net_quic_connectionaborted, err), null) // Shutdown initiated by peer.
            };
        }

        internal static QuicException GetStreamAbortedException(long errorCode)
        {
            return errorCode switch
            {
                -1 => GetOperationAbortedException(), // Shutdown initiated by us.
                long err => new QuicException(QuicError.StreamAborted, err, SR.Format(SR.net_quic_streamaborted, err), null) // Shutdown initiated by peer.
            };
        }

        internal static QuicException GetOperationAbortedException(string? message = null)
        {
            return new QuicException(QuicError.OperationAborted, null, message ?? SR.net_quic_operationaborted, null);
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
                if (status == QUIC_STATUS_ADDRESS_IN_USE) return new QuicException(QuicError.AddressInUse, null, SR.net_quic_address_in_use, null);
                if (status == QUIC_STATUS_UNREACHABLE) return new QuicException(QuicError.HostUnreachable, null, SR.net_quic_host_unreachable, null);
                if (status == QUIC_STATUS_CONNECTION_REFUSED) return new QuicException(QuicError.ConnectionRefused, null, SR.net_quic_connection_refused, null);
                if (status == QUIC_STATUS_VER_NEG_ERROR) return new QuicException(QuicError.VersionNegotiationError, null, SR.net_quic_ver_neg_error, null);
                if (status == QUIC_STATUS_INVALID_ADDRESS) return new QuicException(QuicError.invalidAddress, null, SR.net_quic_invalid_address, null);
                if (status == QUIC_STATUS_CONNECTION_IDLE) return new QuicException(QuicError.ConnectionIdle, null, SR.net_quic_connection_idle, null);
                if (status == QUIC_STATUS_PROTOCOL_ERROR) return new QuicException(QuicError.ProtocolError, null, SR.net_quic_protocol_error, null);

                if (status == QUIC_STATUS_TLS_ERROR ||
                    status == QUIC_STATUS_CERT_EXPIRED ||
                    status == QUIC_STATUS_CERT_UNTRUSTED_ROOT ||
                    status == QUIC_STATUS_CERT_NO_CERT)
                {
                    return new AuthenticationException(SR.net_auth_SSPI, new MsQuicException(status, message));
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
                    return new AuthenticationException(SR.Format(SR.net_auth_tls_alert, alert), new MsQuicException(status, message));
                }

                //
                // for everything else, use general InternalError
                //
                return new QuicException(QuicError.InternalError, null, SR.net_quic_internal_error, new MsQuicException(status, message));
            }
        }

        internal static void ThrowIfMsQuicError(int status, string? message = null)
        {
            if (StatusFailed(status))
            {
                throw GetExceptionForMsQuicStatus(status, message);
            }
        }
    }
}
