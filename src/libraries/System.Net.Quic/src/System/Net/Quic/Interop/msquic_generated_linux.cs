#pragma warning disable IDE0073
//
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
//
#pragma warning restore IDE0073

namespace Microsoft.Quic
{
    internal static unsafe partial class MsQuic_Linux
    {
        [NativeTypeName("#define QUIC_STATUS_SUCCESS ((QUIC_STATUS)0)")]
        public const int QUIC_STATUS_SUCCESS = ((int)(0));

        [NativeTypeName("#define QUIC_STATUS_PENDING ((QUIC_STATUS)-2)")]
        public const int QUIC_STATUS_PENDING = unchecked((int)(-2));

        [NativeTypeName("#define QUIC_STATUS_CONTINUE ((QUIC_STATUS)-1)")]
        public const int QUIC_STATUS_CONTINUE = unchecked((int)(-1));

        [NativeTypeName("#define QUIC_STATUS_OUT_OF_MEMORY ((QUIC_STATUS)ENOMEM)")]
        public const int QUIC_STATUS_OUT_OF_MEMORY = ((int)(12));

        [NativeTypeName("#define QUIC_STATUS_INVALID_PARAMETER ((QUIC_STATUS)EINVAL)")]
        public const int QUIC_STATUS_INVALID_PARAMETER = ((int)(22));

        [NativeTypeName("#define QUIC_STATUS_INVALID_STATE ((QUIC_STATUS)EPERM)")]
        public const int QUIC_STATUS_INVALID_STATE = ((int)(1));

        [NativeTypeName("#define QUIC_STATUS_NOT_SUPPORTED ((QUIC_STATUS)EOPNOTSUPP)")]
        public const int QUIC_STATUS_NOT_SUPPORTED = ((int)(95));

        [NativeTypeName("#define QUIC_STATUS_NOT_FOUND ((QUIC_STATUS)ENOENT)")]
        public const int QUIC_STATUS_NOT_FOUND = ((int)(2));

        [NativeTypeName("#define QUIC_STATUS_BUFFER_TOO_SMALL ((QUIC_STATUS)EOVERFLOW)")]
        public const int QUIC_STATUS_BUFFER_TOO_SMALL = ((int)(75));

        [NativeTypeName("#define QUIC_STATUS_HANDSHAKE_FAILURE ((QUIC_STATUS)ECONNABORTED)")]
        public const int QUIC_STATUS_HANDSHAKE_FAILURE = ((int)(103));

        [NativeTypeName("#define QUIC_STATUS_ABORTED ((QUIC_STATUS)ECANCELED)")]
        public const int QUIC_STATUS_ABORTED = ((int)(125));

        [NativeTypeName("#define QUIC_STATUS_ADDRESS_IN_USE ((QUIC_STATUS)EADDRINUSE)")]
        public const int QUIC_STATUS_ADDRESS_IN_USE = ((int)(98));

        [NativeTypeName("#define QUIC_STATUS_INVALID_ADDRESS ((QUIC_STATUS)EAFNOSUPPORT)")]
        public const int QUIC_STATUS_INVALID_ADDRESS = ((int)(97));

        [NativeTypeName("#define QUIC_STATUS_CONNECTION_TIMEOUT ((QUIC_STATUS)ETIMEDOUT)")]
        public const int QUIC_STATUS_CONNECTION_TIMEOUT = ((int)(110));

        [NativeTypeName("#define QUIC_STATUS_CONNECTION_IDLE ((QUIC_STATUS)ETIME)")]
        public const int QUIC_STATUS_CONNECTION_IDLE = ((int)(62));

        [NativeTypeName("#define QUIC_STATUS_INTERNAL_ERROR ((QUIC_STATUS)EIO)")]
        public const int QUIC_STATUS_INTERNAL_ERROR = ((int)(5));

        [NativeTypeName("#define QUIC_STATUS_CONNECTION_REFUSED ((QUIC_STATUS)ECONNREFUSED)")]
        public const int QUIC_STATUS_CONNECTION_REFUSED = ((int)(111));

        [NativeTypeName("#define QUIC_STATUS_PROTOCOL_ERROR ((QUIC_STATUS)EPROTO)")]
        public const int QUIC_STATUS_PROTOCOL_ERROR = ((int)(71));

        [NativeTypeName("#define QUIC_STATUS_VER_NEG_ERROR ((QUIC_STATUS)EPROTONOSUPPORT)")]
        public const int QUIC_STATUS_VER_NEG_ERROR = ((int)(93));

        [NativeTypeName("#define QUIC_STATUS_UNREACHABLE ((QUIC_STATUS)EHOSTUNREACH)")]
        public const int QUIC_STATUS_UNREACHABLE = ((int)(113));

        [NativeTypeName("#define QUIC_STATUS_TLS_ERROR ((QUIC_STATUS)ENOKEY)")]
        public const int QUIC_STATUS_TLS_ERROR = ((int)(126));

        [NativeTypeName("#define QUIC_STATUS_USER_CANCELED ((QUIC_STATUS)EOWNERDEAD)")]
        public const int QUIC_STATUS_USER_CANCELED = ((int)(130));

        [NativeTypeName("#define QUIC_STATUS_ALPN_NEG_FAILURE ((QUIC_STATUS)ENOPROTOOPT)")]
        public const int QUIC_STATUS_ALPN_NEG_FAILURE = ((int)(92));

        [NativeTypeName("#define QUIC_STATUS_STREAM_LIMIT_REACHED ((QUIC_STATUS)ESTRPIPE)")]
        public const int QUIC_STATUS_STREAM_LIMIT_REACHED = ((int)(86));

        [NativeTypeName("#define QUIC_STATUS_ALPN_IN_USE ((QUIC_STATUS)EPROTOTYPE)")]
        public const int QUIC_STATUS_ALPN_IN_USE = unchecked((int)(91));

        [NativeTypeName("#define QUIC_STATUS_CLOSE_NOTIFY QUIC_STATUS_TLS_ALERT(0)")]
        public const int QUIC_STATUS_CLOSE_NOTIFY = ((int)(0xff & 0) + 256 + 200000000);

        [NativeTypeName("#define QUIC_STATUS_BAD_CERTIFICATE QUIC_STATUS_TLS_ALERT(42)")]
        public const int QUIC_STATUS_BAD_CERTIFICATE = ((int)(0xff & 42) + 256 + 200000000);

        [NativeTypeName("#define QUIC_STATUS_UNSUPPORTED_CERTIFICATE QUIC_STATUS_TLS_ALERT(43)")]
        public const int QUIC_STATUS_UNSUPPORTED_CERTIFICATE = ((int)(0xff & 43) + 256 + 200000000);

        [NativeTypeName("#define QUIC_STATUS_REVOKED_CERTIFICATE QUIC_STATUS_TLS_ALERT(44)")]
        public const int QUIC_STATUS_REVOKED_CERTIFICATE = ((int)(0xff & 44) + 256 + 200000000);

        [NativeTypeName("#define QUIC_STATUS_EXPIRED_CERTIFICATE QUIC_STATUS_TLS_ALERT(45)")]
        public const int QUIC_STATUS_EXPIRED_CERTIFICATE = ((int)(0xff & 45) + 256 + 200000000);

        [NativeTypeName("#define QUIC_STATUS_UNKNOWN_CERTIFICATE QUIC_STATUS_TLS_ALERT(46)")]
        public const int QUIC_STATUS_UNKNOWN_CERTIFICATE = ((int)(0xff & 46) + 256 + 200000000);

        [NativeTypeName("#define QUIC_STATUS_REQUIRED_CERTIFICATE QUIC_STATUS_TLS_ALERT(116)")]
        public const int QUIC_STATUS_REQUIRED_CERTIFICATE = ((int)(0xff & 116) + 256 + 200000000);

        [NativeTypeName("#define QUIC_STATUS_CERT_EXPIRED QUIC_STATUS_CERT_ERROR(1)")]
        public const int QUIC_STATUS_CERT_EXPIRED = ((int)(1) + 512 + 200000000);

        [NativeTypeName("#define QUIC_STATUS_CERT_UNTRUSTED_ROOT QUIC_STATUS_CERT_ERROR(2)")]
        public const int QUIC_STATUS_CERT_UNTRUSTED_ROOT = ((int)(2) + 512 + 200000000);

        [NativeTypeName("#define QUIC_STATUS_CERT_NO_CERT QUIC_STATUS_CERT_ERROR(3)")]
        public const int QUIC_STATUS_CERT_NO_CERT = ((int)(3) + 512 + 200000000);

        [NativeTypeName("#define QUIC_STATUS_ADDRESS_NOT_AVAILABLE ((QUIC_STATUS)EADDRNOTAVAIL)")]
        public const int QUIC_STATUS_ADDRESS_NOT_AVAILABLE = ((int)(0x63));

        public const int QUIC_ADDRESS_FAMILY_UNSPEC = 0;
        public const int QUIC_ADDRESS_FAMILY_INET = 2;
        public const int QUIC_ADDRESS_FAMILY_INET6 = 10;
    }
}
