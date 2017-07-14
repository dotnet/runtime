// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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

        internal const string OpenSsl10Description = "openssl/1.0";
        internal const string SecureTransportDescription = "SecureTransport";
        internal const string LibreSslDescription = "LibreSSL";
    }
}
