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

            if (rc != 0 || count == 0 || proxies == null)
            {
                return null;
            }

            try
            {
                // ProxySelector returns entries in preference order. We take the first one.
                // SocketsHttpHandler does not currently have an opt-in fallback API for
                // multiple proxies; if the chosen proxy is unreachable the user sees the
                // connect error rather than an automatic retry.
                for (int i = 0; i < count; i++)
                {
                    Interop.AndroidCrypto.AndroidProxyInfo entry = proxies[i];
                    string scheme = entry.Type == (int)Interop.AndroidCrypto.AndroidProxyType.Socks
                        ? "socks5"
                        : "http";

                    string? host = Marshal.PtrToStringUTF8(entry.Host);
                    if (string.IsNullOrEmpty(host))
                    {
                        continue;
                    }

                    return new UriBuilder(scheme, host, entry.Port).Uri;
                }

                return null;
            }
            finally
            {
                Interop.AndroidCrypto.FreeProxyResult(proxies, count);
            }
        }

        public bool IsBypassed(Uri host)
        {
            ArgumentNullException.ThrowIfNull(host);
            // Computing the real answer is as expensive as GetProxy; mirror MacProxy.
            Uri? proxyUri = GetProxy(host);
            return proxyUri is null || Equals(proxyUri, host);
        }
    }
}
