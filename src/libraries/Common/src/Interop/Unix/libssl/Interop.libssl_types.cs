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

        internal const long ProtocolMask = Options.SSL_OP_NO_SSLv2 | Options.SSL_OP_NO_SSLv3 |
                                           Options.SSL_OP_NO_TLSv1 | Options.SSL_OP_NO_TLSv1_1 |
                                           Options.SSL_OP_NO_TLSv1_2;

        [StructLayout(LayoutKind.Sequential)]
        internal struct SSL_CIPHER
        {
            internal int valid;
            internal string name;
            internal long id;
            internal long algorithm_mkey;
            internal long algorithm_auth;
            internal long algorithm_enc;
            internal long algorithm_mac;
            internal long algorithm_ssl;
            internal long algo_strength;
            internal long algorithm2;
            internal int strength_bits;
            internal int alg_bits;
        }

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
            //From /usr/include/openssl/ssl3.h
            internal const int SSL3_RT_MAX_MD_SIZE = 64;
            internal const int SSL3_RT_MAX_PLAIN_LENGTH = 16384;
            private const int SSL3_RT_MAX_COMPRESSED_OVERHEAD = 1024;
            private const int SSL3_RT_MAX_ENCRYPTED_OVERHEAD = (256 + SSL3_RT_MAX_MD_SIZE);
            // TODO (Issue #3362) compression is allowed? size will vary when compression is there

            private static readonly int SSL3_RT_MAX_COMPRESSED_LENGTH = OPENSSL_NO_COMP
                ? SSL3_RT_MAX_PLAIN_LENGTH
                : SSL3_RT_MAX_PLAIN_LENGTH + SSL3_RT_MAX_COMPRESSED_OVERHEAD;

            internal static int SSL3_RT_MAX_ENCRYPTED_LENGTH = (SSL3_RT_MAX_ENCRYPTED_OVERHEAD +
                                                                SSL3_RT_MAX_COMPRESSED_LENGTH);
            internal const int HEADER_SIZE = 5;

            // TODO (Issue #3362) : Trailer size requirement is changing based on protocol
            //       SSL3/TLS1.0 - 68, TLS1.1 - 37 and TLS1.2 - 24
            //       Current usage is only to compute max input buffer size for
            //       encryption and so setting to the max
            internal const int TRAILER_SIZE = 68;
        }

        internal static class SslMethods
        {
            internal static readonly IntPtr TLSv1_method = libssl.TLSv1_method();
            internal static readonly IntPtr TLSv1_1_method = libssl.TLSv1_1_method();
            internal static readonly IntPtr TLSv1_2_method = libssl.TLSv1_2_method();
            internal static readonly IntPtr SSLv3_method = libssl.SSLv3_method();
            internal static readonly IntPtr SSLv23_method = libssl.SSLv23_method();

#if DEBUG
            static SslMethods()
            {
                Debug.Assert(TLSv1_method != IntPtr.Zero, "TLSv1_method is null");
                Debug.Assert(TLSv1_1_method != IntPtr.Zero, "TLSv1_1_method is null");
                Debug.Assert(TLSv1_2_method != IntPtr.Zero, "TLSv1_2_method is null");
                Debug.Assert(SSLv3_method != IntPtr.Zero, "SSLv3_method is null");
                Debug.Assert(SSLv23_method != IntPtr.Zero, "SSLv23 method is null");
            }
#endif
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
    }
}
