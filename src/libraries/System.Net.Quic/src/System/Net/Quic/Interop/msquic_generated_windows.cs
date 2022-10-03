#pragma warning disable IDE0073
//
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
//
#pragma warning restore IDE0073

namespace Microsoft.Quic
{
    internal static partial class MsQuic_Windows
    {
        [NativeTypeName("#define QUIC_STATUS_SUCCESS S_OK")]
        public const int QUIC_STATUS_SUCCESS = ((int)(0));

        [NativeTypeName("#define QUIC_STATUS_PENDING SUCCESS_HRESULT_FROM_WIN32(ERROR_IO_PENDING)")]
        public const int QUIC_STATUS_PENDING = ((int)(((997) & 0x0000FFFF) | (7 << 16)));

        [NativeTypeName("#define QUIC_STATUS_CONTINUE SUCCESS_HRESULT_FROM_WIN32(ERROR_CONTINUE)")]
        public const int QUIC_STATUS_CONTINUE = ((int)(((1246) & 0x0000FFFF) | (7 << 16)));

        [NativeTypeName("#define QUIC_STATUS_OUT_OF_MEMORY E_OUTOFMEMORY")]
        public const int QUIC_STATUS_OUT_OF_MEMORY = unchecked((int)(0x8007000E));

        [NativeTypeName("#define QUIC_STATUS_INVALID_PARAMETER E_INVALIDARG")]
        public const int QUIC_STATUS_INVALID_PARAMETER = unchecked((int)(0x80070057));

        [NativeTypeName("#define QUIC_STATUS_INVALID_STATE QUIC_STATUS_HRESULT_FROM_WIN32(ERROR_INVALID_STATE)")]
        public const int QUIC_STATUS_INVALID_STATE = unchecked((int)(5023) <= 0 ? ((int)(5023)) : ((int)(((5023) & 0x0000FFFF) | (7 << 16) | 0x80000000)));

        [NativeTypeName("#define QUIC_STATUS_NOT_SUPPORTED E_NOINTERFACE")]
        public const int QUIC_STATUS_NOT_SUPPORTED = unchecked((int)(0x80004002));

        [NativeTypeName("#define QUIC_STATUS_NOT_FOUND QUIC_STATUS_HRESULT_FROM_WIN32(ERROR_NOT_FOUND)")]
        public const int QUIC_STATUS_NOT_FOUND = unchecked((int)(1168) <= 0 ? ((int)(1168)) : ((int)(((1168) & 0x0000FFFF) | (7 << 16) | 0x80000000)));

        [NativeTypeName("#define QUIC_STATUS_BUFFER_TOO_SMALL QUIC_STATUS_HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER)")]
        public const int QUIC_STATUS_BUFFER_TOO_SMALL = unchecked((int)(122) <= 0 ? ((int)(122)) : ((int)(((122) & 0x0000FFFF) | (7 << 16) | 0x80000000)));

        [NativeTypeName("#define QUIC_STATUS_HANDSHAKE_FAILURE ERROR_QUIC_HANDSHAKE_FAILURE")]
        public const int QUIC_STATUS_HANDSHAKE_FAILURE = unchecked((int)(0x80410000));

        [NativeTypeName("#define QUIC_STATUS_ABORTED E_ABORT")]
        public const int QUIC_STATUS_ABORTED = unchecked((int)(0x80004004));

        [NativeTypeName("#define QUIC_STATUS_ADDRESS_IN_USE QUIC_STATUS_HRESULT_FROM_WIN32(WSAEADDRINUSE)")]
        public const int QUIC_STATUS_ADDRESS_IN_USE = unchecked((int)(10048) <= 0 ? ((int)(10048)) : ((int)(((10048) & 0x0000FFFF) | (7 << 16) | 0x80000000)));

        [NativeTypeName("#define QUIC_STATUS_INVALID_ADDRESS QUIC_STATUS_HRESULT_FROM_WIN32(WSAEADDRNOTAVAIL)")]
        public const int QUIC_STATUS_INVALID_ADDRESS = unchecked((int)(10049) <= 0 ? ((int)(10049)) : ((int)(((10049) & 0x0000FFFF) | (7 << 16) | 0x80000000)));

        [NativeTypeName("#define QUIC_STATUS_CONNECTION_TIMEOUT ERROR_QUIC_CONNECTION_TIMEOUT")]
        public const int QUIC_STATUS_CONNECTION_TIMEOUT = unchecked((int)(0x80410006));

        [NativeTypeName("#define QUIC_STATUS_CONNECTION_IDLE ERROR_QUIC_CONNECTION_IDLE")]
        public const int QUIC_STATUS_CONNECTION_IDLE = unchecked((int)(0x80410005));

        [NativeTypeName("#define QUIC_STATUS_UNREACHABLE QUIC_STATUS_HRESULT_FROM_WIN32(ERROR_HOST_UNREACHABLE)")]
        public const int QUIC_STATUS_UNREACHABLE = unchecked((int)(1232) <= 0 ? ((int)(1232)) : ((int)(((1232) & 0x0000FFFF) | (7 << 16) | 0x80000000)));

        [NativeTypeName("#define QUIC_STATUS_INTERNAL_ERROR ERROR_QUIC_INTERNAL_ERROR")]
        public const int QUIC_STATUS_INTERNAL_ERROR = unchecked((int)(0x80410003));

        [NativeTypeName("#define QUIC_STATUS_CONNECTION_REFUSED QUIC_STATUS_HRESULT_FROM_WIN32(ERROR_CONNECTION_REFUSED)")]
        public const int QUIC_STATUS_CONNECTION_REFUSED = unchecked((int)(1225) <= 0 ? ((int)(1225)) : ((int)(((1225) & 0x0000FFFF) | (7 << 16) | 0x80000000)));

        [NativeTypeName("#define QUIC_STATUS_PROTOCOL_ERROR ERROR_QUIC_PROTOCOL_VIOLATION")]
        public const int QUIC_STATUS_PROTOCOL_ERROR = unchecked((int)(0x80410004));

        [NativeTypeName("#define QUIC_STATUS_VER_NEG_ERROR ERROR_QUIC_VER_NEG_FAILURE")]
        public const int QUIC_STATUS_VER_NEG_ERROR = unchecked((int)(0x80410001));

        [NativeTypeName("#define QUIC_STATUS_TLS_ERROR QUIC_STATUS_HRESULT_FROM_WIN32(WSA_SECURE_HOST_NOT_FOUND)")]
        public const int QUIC_STATUS_TLS_ERROR = unchecked((int)(11032) <= 0 ? ((int)(11032)) : ((int)(((11032) & 0x0000FFFF) | (7 << 16) | 0x80000000)));

        [NativeTypeName("#define QUIC_STATUS_USER_CANCELED ERROR_QUIC_USER_CANCELED")]
        public const int QUIC_STATUS_USER_CANCELED = unchecked((int)(0x80410002));

        [NativeTypeName("#define QUIC_STATUS_ALPN_NEG_FAILURE ERROR_QUIC_ALPN_NEG_FAILURE")]
        public const int QUIC_STATUS_ALPN_NEG_FAILURE = unchecked((int)(0x80410007));

        [NativeTypeName("#define QUIC_STATUS_STREAM_LIMIT_REACHED ERROR_QUIC_STREAM_LIMIT_REACHED")]
        public const int QUIC_STATUS_STREAM_LIMIT_REACHED = unchecked((int)(0x80410008));

        [NativeTypeName("#define QUIC_STATUS_CLOSE_NOTIFY QUIC_STATUS_TLS_ALERT(0)")]
        public const int QUIC_STATUS_CLOSE_NOTIFY = unchecked(((int)(0x80410100)) | (0xff & 0));

        [NativeTypeName("#define QUIC_STATUS_BAD_CERTIFICATE QUIC_STATUS_TLS_ALERT(42)")]
        public const int QUIC_STATUS_BAD_CERTIFICATE = unchecked(((int)(0x80410100)) | (0xff & 42));

        [NativeTypeName("#define QUIC_STATUS_UNSUPPORTED_CERTIFICATE QUIC_STATUS_TLS_ALERT(43)")]
        public const int QUIC_STATUS_UNSUPPORTED_CERTIFICATE = unchecked(((int)(0x80410100)) | (0xff & 43));

        [NativeTypeName("#define QUIC_STATUS_REVOKED_CERTIFICATE QUIC_STATUS_TLS_ALERT(44)")]
        public const int QUIC_STATUS_REVOKED_CERTIFICATE = unchecked(((int)(0x80410100)) | (0xff & 44));

        [NativeTypeName("#define QUIC_STATUS_EXPIRED_CERTIFICATE QUIC_STATUS_TLS_ALERT(45)")]
        public const int QUIC_STATUS_EXPIRED_CERTIFICATE = unchecked(((int)(0x80410100)) | (0xff & 45));

        [NativeTypeName("#define QUIC_STATUS_UNKNOWN_CERTIFICATE QUIC_STATUS_TLS_ALERT(46)")]
        public const int QUIC_STATUS_UNKNOWN_CERTIFICATE = unchecked(((int)(0x80410100)) | (0xff & 46));

        [NativeTypeName("#define QUIC_STATUS_REQUIRED_CERTIFICATE QUIC_STATUS_TLS_ALERT(116)")]
        public const int QUIC_STATUS_REQUIRED_CERTIFICATE = unchecked(((int)(0x80410100)) | (0xff & 116));

        [NativeTypeName("#define QUIC_STATUS_CERT_EXPIRED CERT_E_EXPIRED")]
        public const int QUIC_STATUS_CERT_EXPIRED = unchecked((int)(0x800B0101));

        [NativeTypeName("#define QUIC_STATUS_CERT_UNTRUSTED_ROOT CERT_E_UNTRUSTEDROOT")]
        public const int QUIC_STATUS_CERT_UNTRUSTED_ROOT = unchecked((int)(0x800B0109));

        [NativeTypeName("#define QUIC_STATUS_CERT_NO_CERT SEC_E_NO_CREDENTIALS")]
        public const int QUIC_STATUS_CERT_NO_CERT = unchecked((int)(0x8009030E));

        public const int QUIC_ADDRESS_FAMILY_UNSPEC = 0;
        public const int QUIC_ADDRESS_FAMILY_INET = 2;
        public const int QUIC_ADDRESS_FAMILY_INET6 = 23;
    }
}
