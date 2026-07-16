// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Runtime.InteropServices;

namespace System.Net.Http
{
    /// <summary>
    /// An <see cref="IWebProxy"/> that defers proxy resolution to the
    /// Android platform via <c>java.net.ProxySelector</c>. Honors Wi-Fi
    /// proxy, MDM-deployed system proxy, PAC scripts, and per-network
    /// or VPN proxies — the same source <c>java.net.HttpURLConnection</c>
    /// consults.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Android proxy APIs (<c>ProxySelector</c>, <c>ProxyInfo</c>,
    /// <c>ConnectivityManager</c>) do not surface credentials. The
    /// <see cref="Credentials"/> property is required by
    /// <see cref="IWebProxy"/> but is never populated by this class.
    /// Apps that need to authenticate to a system-detected proxy should
    /// set <c>HttpClientHandler.DefaultProxyCredentials</c> (or
    /// <c>SocketsHttpHandler.DefaultProxyCredentials</c>) — the same
    /// behavior as on macOS and Windows.
    /// </para>
    /// </remarks>
    internal sealed class AndroidPlatformProxy : IWebProxy
    {
        public ICredentials? Credentials { get; set; }

        public unsafe Uri? GetProxy(Uri destination)
        {
            ArgumentNullException.ThrowIfNull(destination);

            string url = destination.AbsoluteUri;

            Interop.AndroidCrypto.AndroidProxyInfo* proxies = null;
            int count = 0;
            int rc = Interop.AndroidCrypto.GetProxyForUrl(url, out count, out proxies);

            if (rc != 0 || proxies == null)
            {
                return null;
            }

            try
            {
                // ProxySelector returns entries in preference order. We surface only the
                // first usable one as a single Uri. SocketsHttpHandler can fail over across
                // multiple proxies when the IWebProxy also implements IMultiWebProxy (see
                // HttpConnectionPoolManager / HttpWindowsProxy), but AndroidPlatformProxy does
                // not implement that interface, so the remaining ProxySelector entries are not
                // used for automatic fallback: if the chosen proxy is unreachable the caller
                // sees the connect error rather than an automatic retry.
                return SelectProxyUri(new ReadOnlySpan<Interop.AndroidCrypto.AndroidProxyInfo>(proxies, count));
            }
            finally
            {
                Interop.AndroidCrypto.FreeProxyResult(proxies, count);
            }
        }

        internal static Uri? SelectProxyUri(ReadOnlySpan<Interop.AndroidCrypto.AndroidProxyInfo> entries)
        {
            foreach (Interop.AndroidCrypto.AndroidProxyInfo entry in entries)
            {
                // Java's Proxy.NO_PROXY is represented as Proxy.Type.DIRECT. Preserve
                // ProxySelector ordering by treating the first DIRECT entry as the selected
                // result (no proxy) rather than skipping ahead to a later fallback proxy.
                if ((Interop.AndroidCrypto.AndroidProxyType)entry.Type == Interop.AndroidCrypto.AndroidProxyType.Direct)
                {
                    return null;
                }

                if (TryCreateProxyUri(entry, out Uri? proxyUri))
                {
                    return proxyUri;
                }
            }

            return null;
        }

        private static bool TryCreateProxyUri(Interop.AndroidCrypto.AndroidProxyInfo entry, out Uri? proxyUri)
        {
            proxyUri = null;

            // SOCKS is a transport-level proxy protocol (RFC 1928 for SOCKS5).
            // Unlike HTTP CONNECT, SOCKS tunnels arbitrary TCP at the socket
            // layer. SocketsHttpHandler accepts "socks5://" via
            // HttpUtilities.IsSupportedProxyScheme. Android's
            // java.net.Proxy.Type.SOCKS maps to SOCKS5 on modern Android.
            // DIRECT (java.net.Proxy.NO_PROXY) yields no scheme and is handled by
            // SelectProxyUri before reaching this helper.
            string? scheme = (Interop.AndroidCrypto.AndroidProxyType)entry.Type switch
            {
                Interop.AndroidCrypto.AndroidProxyType.Http => "http",
                Interop.AndroidCrypto.AndroidProxyType.Socks => "socks5",
                _ => null,
            };

            if (scheme is null)
            {
                return false;
            }

            // The native PAL allocates the host as NUL-terminated UTF-16
            // (Marshal.PtrToStringUni is a zero-conversion copy).
            string? host = Marshal.PtrToStringUni(entry.Host);
            if (string.IsNullOrEmpty(host))
            {
                return false;
            }

            // entry.Port originates from java.net.InetSocketAddress.getPort() and crosses the
            // JNI boundary, so treat it as untrusted. A usable proxy needs a real port; anything
            // outside the valid TCP range would make UriBuilder throw, which the caller would see
            // as a failed entry. Skip it instead so a later ProxySelector entry can be tried.
            if (entry.Port is < 1 or > 65535)
            {
                return false;
            }

            proxyUri = new UriBuilder(scheme, host, entry.Port).Uri;

            return true;
        }

        // SocketsHttpHandler's pattern is: call IsBypassed first; if it returns false,
        // call GetProxy. For us, computing IsBypassed *correctly* would require calling
        // GetProxy first, which would mean two JNI round-trips per request.
        // Instead, we always return false (same approach as HttpWindowsProxy:349–360) so
        // that SHH always calls GetProxy exactly once and discovers "no proxy" via a
        // null return.
        public bool IsBypassed(Uri host) => false;
    }
}
