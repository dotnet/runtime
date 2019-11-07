// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Http
    {
        [Flags]
        internal enum CurlFeatures : int
        {
            CURL_VERSION_IPV6 = (1 << 0),
            CURL_VERSION_KERBEROS4 = (1 << 1),
            CURL_VERSION_SSL = (1 << 2),
            CURL_VERSION_LIBZ = (1 << 3),
            CURL_VERSION_NTLM = (1 << 4),
            CURL_VERSION_GSSNEGOTIATE = (1 << 5),
            CURL_VERSION_DEBUG = (1 << 6),
            CURL_VERSION_ASYNCHDNS = (1 << 7),
            CURL_VERSION_SPNEGO = (1 << 8),
            CURL_VERSION_LARGEFILE = (1 << 9),
            CURL_VERSION_IDN = (1 << 10),
            CURL_VERSION_SSPI = (1 << 11),
            CURL_VERSION_CONV = (1 << 12),
            CURL_VERSION_CURLDEBUG = (1 << 13),
            CURL_VERSION_TLSAUTH_SRP = (1 << 14),
            CURL_VERSION_NTLM_WB = (1 << 15),
            CURL_VERSION_HTTP2 = (1 << 16),
            CURL_VERSION_GSSAPI = (1 << 17),
            CURL_VERSION_KERBEROS5 = (1 << 18),
            CURL_VERSION_UNIX_SOCKETS = (1 << 19),
            CURL_VERSION_PSL = (1 << 20),
        };

        [DllImport(Libraries.HttpNative, EntryPoint = "HttpNative_GetSupportedFeatures")]
        internal static extern CurlFeatures GetSupportedFeatures();

        [DllImport(Libraries.HttpNative, EntryPoint = "HttpNative_GetSupportsHttp2Multiplexing")]
        internal static extern bool GetSupportsHttp2Multiplexing();

        [DllImport(Libraries.HttpNative, EntryPoint = "HttpNative_GetVersionDescription")]
        internal static extern string GetVersionDescription();

        [DllImport(Libraries.HttpNative, EntryPoint = "HttpNative_GetSslVersionDescription")]
        internal static extern string GetSslVersionDescription();

        internal const string OpenSslDescriptionPrefix = "OpenSSL/";
        internal const string SecureTransportDescription = "SecureTransport";
        internal const string LibreSslDescription = "LibreSSL";

#if !SYSNETHTTP_NO_OPENSSL
        private static readonly Lazy<string> s_requiredOpenSslDescription =
            new Lazy<string>(() => DetermineRequiredOpenSslDescription());

        private static readonly Lazy<bool> s_hasMatchingOpenSsl =
            new Lazy<bool>(() => RequiredOpenSslDescription == GetSslVersionDescription());

        internal static string RequiredOpenSslDescription => s_requiredOpenSslDescription.Value;
        internal static bool HasMatchingOpenSslVersion => s_hasMatchingOpenSsl.Value;

        private static string DetermineRequiredOpenSslDescription()
        {
            long ver = Interop.OpenSsl.OpenSslVersionNumber();

            // OpenSSL version numbers are encoded as
            // 0xMNNFFPPS: major (one nybble), minor (one byte, unaligned),
            // "fix" (one byte, unaligned), patch (one byte, unaligned), status (one nybble)
            //
            // e.g. 1.0.2a final is 0x1000201F
            //
            // libcurl's OpenSSL vtls backend ignores status in the version string.
            // Major, minor, and fix are encoded (by libcurl) as unpadded hex
            // (0 => "0", 15 => "f", 16 => "10").
            //
            // Patch is encoded as in the way OpenSSL would do it.
            // 0x00 => ""
            // 0x01 => "a"
            // 0x1a (26) => "z"
            // 0x1b (27) => "za"
            // 0x34 (52) => "zz"
            // 0x35 (53) should probably be "zza", but it never came up, and is not currently
            // handled correctly by libcurl (which would call it "z{").

            byte patchValue = (byte)((ver & 0xFF0) >> 4);
            string patch = string.Empty;

            if (patchValue > 52)
            {
                Debug.Fail($"OpenSSL version ({ver:8X}) patch value ({patchValue}) is out of range");
                throw new InvalidOperationException();
            }
            else if (patchValue > 26)
            {
                Span<char> patchStr = stackalloc char[2];
                patchStr[0] = 'z';
                // backtick is ('a'-1)
                patchStr[1] = (char)('`' + (patchValue - 26));
                patch = new string(patchStr);
            }
            else if (patchValue > 0)
            {
                // backtick is ('a'-1)
                patch = new string((char)('`' + patchValue), 1);
            }

            return $"{OpenSslDescriptionPrefix}{(ver >> 28) & 0xF:x}.{(byte)(ver >> 20):x}.{(byte)(ver >> 12):x}{patch}";
        }
#endif
    }
}
