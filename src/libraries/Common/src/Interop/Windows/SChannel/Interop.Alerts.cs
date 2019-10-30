// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class SChannel
    {
        // schannel.h;

        //
        //
        // ApplyControlToken PkgParams types
        //
        // These identifiers are the DWORD types
        // to be passed into ApplyControlToken
        // through a PkgParams buffer.

        public const int SCHANNEL_RENEGOTIATE = 0;   // renegotiate a connection
        public const int SCHANNEL_SHUTDOWN = 1;   // gracefully close down a connection
        public const int SCHANNEL_ALERT = 2;   // build an error message
        public const int SCHANNEL_SESSION = 3;   // session control


        // Alert token structure.
        [StructLayout(LayoutKind.Sequential)]
        public struct SCHANNEL_ALERT_TOKEN
        {
            public uint dwTokenType;            // SCHANNEL_ALERT
            public uint dwAlertType;
            public uint dwAlertNumber;
        }

        // Alert types.
        public const int TLS1_ALERT_WARNING = 1;
        public const int TLS1_ALERT_FATAL = 2;

        // Alert messages.
        public const int TLS1_ALERT_CLOSE_NOTIFY = 0; // warning
        public const int TLS1_ALERT_UNEXPECTED_MESSAGE = 10; // error
        public const int TLS1_ALERT_BAD_RECORD_MAC = 20; // error
        public const int TLS1_ALERT_DECRYPTION_FAILED = 21; // reserved
        public const int TLS1_ALERT_RECORD_OVERFLOW = 22; // error
        public const int TLS1_ALERT_DECOMPRESSION_FAIL = 30; // error
        public const int TLS1_ALERT_HANDSHAKE_FAILURE = 40; // error
        public const int TLS1_ALERT_BAD_CERTIFICATE = 42; // warning or error
        public const int TLS1_ALERT_UNSUPPORTED_CERT = 43; // warning or error
        public const int TLS1_ALERT_CERTIFICATE_REVOKED = 44; // warning or error
        public const int TLS1_ALERT_CERTIFICATE_EXPIRED = 45; // warning or error
        public const int TLS1_ALERT_CERTIFICATE_UNKNOWN = 46; // warning or error
        public const int TLS1_ALERT_ILLEGAL_PARAMETER = 47; // error
        public const int TLS1_ALERT_UNKNOWN_CA = 48; // error
        public const int TLS1_ALERT_ACCESS_DENIED = 49; // error
        public const int TLS1_ALERT_DECODE_ERROR = 50; // error
        public const int TLS1_ALERT_DECRYPT_ERROR = 51; // error
        public const int TLS1_ALERT_EXPORT_RESTRICTION = 60; // reserved
        public const int TLS1_ALERT_PROTOCOL_VERSION = 70; // error
        public const int TLS1_ALERT_INSUFFIENT_SECURITY = 71; // error
        public const int TLS1_ALERT_INTERNAL_ERROR = 80; // error
        public const int TLS1_ALERT_USER_CANCELED = 90; // warning or error
        public const int TLS1_ALERT_NO_RENEGOTIATION = 100; // warning
        public const int TLS1_ALERT_UNSUPPORTED_EXT = 110; // error
    }
}
