// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libssl
    {
        internal const int SSL_CTRL_OPTIONS = 32;
        internal static bool OPENSSL_NO_COMP = true; //no compression true by default

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int verify_callback(int preverify_ok, IntPtr x509_ctx);

        internal const long ProtocolMask = Options.SSL_OP_NO_SSLv2 | Options.SSL_OP_NO_SSLv3 |
                                           Options.SSL_OP_NO_TLSv1 | Options.SSL_OP_NO_TLSv1_1 |
                                           Options.SSL_OP_NO_TLSv1_2;

        internal class Options
        {
            internal const long SSL_OP_NO_SSLv2 = 0x01000000;
            internal const long SSL_OP_NO_SSLv3 = 0x02000000;
            internal const long SSL_OP_NO_TLSv1 = 0x04000000;
            internal const long SSL_OP_NO_TLSv1_2 = 0x08000000;
            internal const long SSL_OP_NO_TLSv1_1 = 0x10000000;
        }

        internal static class SslSizes
        {
            internal const int SSL3_RT_MAX_PLAIN_LENGTH = 16384;
            internal const int HEADER_SIZE = 5;

            // TODO (Issue #3362) : Trailer size requirement is changing based on protocol
            //       SSL3/TLS1.0 - 68, TLS1.1 - 37 and TLS1.2 - 24
            //       Current usage is only to compute max input buffer size for
            //       encryption and so setting to the max
            internal const int TRAILER_SIZE = 68;
        }

        internal static class SslMethods
        {
            internal static readonly IntPtr TLSv1_method = Ssl.TlsV1Method();
            internal static readonly IntPtr TLSv1_1_method = Ssl.TlsV1_1Method();
            internal static readonly IntPtr TLSv1_2_method = Ssl.TlsV1_2Method();
            internal static readonly IntPtr SSLv3_method = Ssl.SslV3Method();
            internal static readonly IntPtr SSLv23_method = Ssl.SslV2_3Method();

#if DEBUG
            static SslMethods()
            {
                Debug.Assert(TLSv1_method != IntPtr.Zero, "TLSv1_method is null");
                // TLSv1_1_method and TLSv1_2_method do not exist on earlier versions of OpenSSL
                // Debug.Assert(TLSv1_1_method != IntPtr.Zero, "TLSv1_1_method is null");
                // Debug.Assert(TLSv1_2_method != IntPtr.Zero, "TLSv1_2_method is null");
                Debug.Assert(SSLv3_method != IntPtr.Zero, "SSLv3_method is null");
                Debug.Assert(SSLv23_method != IntPtr.Zero, "SSLv23 method is null");
            }
#endif
        }

        internal static class CipherString
        {
            private const string SSL_TXT_eNULL = "eNULL";
            private const string SSL_TXT_ALL = "ALL";

            internal const string AllExceptNull = SSL_TXT_ALL;

            // delimiter ":" is used to allow more than one strings
            // below string is corresponding to "AllowNoEncryption"
            internal const string AllIncludingNull = SSL_TXT_ALL + ":" + SSL_TXT_eNULL;
            internal const string Null = SSL_TXT_eNULL;
        }

        internal enum SslErrorCode
        {
            SSL_ERROR_NONE = 0,
            SSL_ERROR_SSL = 1,
            SSL_ERROR_WANT_READ = 2,
            SSL_ERROR_WANT_WRITE = 3,
            SSL_ERROR_SYSCALL = 5,
            SSL_ERROR_ZERO_RETURN = 6,
            SSL_ERROR_RENEGOTIATE = 10
        }

        internal enum SslState
        {
            SSL_ST_OK = 3
        }

        [FlagsAttribute]
        internal enum ClientCertOption
        {
            SSL_VERIFY_NONE = 0x00,
            SSL_VERIFY_PEER = 0x01,
            SSL_VERIFY_FAIL_IF_NO_PEER_CERT = 0x02,
            SSL_VERIFY_CLIENT_ONCE = 0x04
        }
    }
}
