// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using SafeWinHttpHandle = Interop.WinHttp.SafeWinHttpHandle;

namespace System.Net.Http
{
    internal sealed class HttpSystemProxy : IWebProxy, IDisposable
    {
        private readonly Uri _insecureProxyUri;         // URI of the http system proxy if set
        private readonly Uri _secureProxyUri;         // URI of the https system proxy if set
        private readonly List<Regex> _bypass;           // list of domains not to proxy
        private readonly bool _bypassLocal = false;     // we should bypass domain considered local
        private readonly List<IPAddress> _localIp;
        private ICredentials _credentials;
        private readonly WinInetProxyHelper _proxyHelper;
        private SafeWinHttpHandle _sessionHandle;
        private bool _disposed;
        private static readonly char[] s_proxyDelimiters = {';', ' ', '\n', '\r', '\t'};

        public static bool TryCreate(out IWebProxy proxy)
        {
            // This will get basic proxy setting from system using existing
            // WinInetProxyHelper functions. If no proxy is enabled, it will return null.
            SafeWinHttpHandle sessionHandle = null;
            proxy = null;

            WinInetProxyHelper proxyHelper = new WinInetProxyHelper();
            if (!proxyHelper.ManualSettingsOnly && !proxyHelper.AutoSettingsUsed)
            {
                return false;
            }

            if (proxyHelper.AutoSettingsUsed)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Info(proxyHelper, $"AutoSettingsUsed, calling {nameof(Interop.WinHttp.WinHttpOpen)}");
                sessionHandle = Interop.WinHttp.WinHttpOpen(
                    IntPtr.Zero,
                    Interop.WinHttp.WINHTTP_ACCESS_TYPE_NO_PROXY,
                    Interop.WinHttp.WINHTTP_NO_PROXY_NAME,
                    Interop.WinHttp.WINHTTP_NO_PROXY_BYPASS,
                    (int)Interop.WinHttp.WINHTTP_FLAG_ASYNC);

                if (sessionHandle.IsInvalid)
                {
                    // Proxy failures are currently ignored by managed handler.
                    if (NetEventSource.IsEnabled) NetEventSource.Error(proxyHelper, $"{nameof(Interop.WinHttp.WinHttpOpen)} returned invalid handle");
                    return false;
                }
            }

            proxy  = new HttpSystemProxy(proxyHelper, sessionHandle);
            return true;
        }

        private HttpSystemProxy(WinInetProxyHelper proxyHelper, SafeWinHttpHandle sessionHandle)
        {
            _proxyHelper = proxyHelper;
            _sessionHandle = sessionHandle;

            if (proxyHelper.ManualSettingsOnly)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Info(proxyHelper, $"ManualSettingsUsed, {proxyHelper.Proxy}");
                ParseProxyConfig(proxyHelper.Proxy, out _insecureProxyUri, out _secureProxyUri);
                if (_insecureProxyUri == null && _secureProxyUri == null)
                {
                    // If advanced parsing by protocol fails, fall-back to simplified parsing.
                    _insecureProxyUri = _secureProxyUri = GetUriFromString(proxyHelper.Proxy);
                }

                if (!string.IsNullOrWhiteSpace(proxyHelper.ProxyBypass))
                {
                    int idx = 0;
                    int start = 0;
                    string tmp;

                    // Process bypass list for manual setting.
                    // Initial list size is best guess based on string length assuming each entry is at least 5 characters on average.
                    _bypass = new List<Regex>(proxyHelper.ProxyBypass.Length / 5);

                    while (idx < proxyHelper.ProxyBypass.Length)
                    {
                        // Strip leading spaces and scheme if any.
                        while (idx < proxyHelper.ProxyBypass.Length && proxyHelper.ProxyBypass[idx] == ' ') { idx += 1; };
                        if (string.Compare(proxyHelper.ProxyBypass, idx, "http://", 0, 7, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            idx += 7;
                        }
                        else if (string.Compare(proxyHelper.ProxyBypass, idx, "https://", 0, 8, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            idx += 8;
                        }

                        if (idx < proxyHelper.ProxyBypass.Length && proxyHelper.ProxyBypass[idx] == '[')
                        {
                            // Strip [] from IPv6 so we can use IdnHost laster for matching.
                            idx +=1;
                        }

                        start = idx;
                        while (idx < proxyHelper.ProxyBypass.Length && proxyHelper.ProxyBypass[idx] != ' ' && proxyHelper.ProxyBypass[idx] != ';' && proxyHelper.ProxyBypass[idx] != ']') {idx += 1; };

                        if (idx == start)
                        {
                            // Empty string.
                            tmp = null;
                        }
                        else if (string.Compare(proxyHelper.ProxyBypass, start, "<local>", 0, 7, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            _bypassLocal = true;
                            tmp = null;
                        }
                        else
                        {
                            tmp = proxyHelper.ProxyBypass.Substring(start, idx-start);
                        }

                        // Skip trailing characters if any.
                        if (idx < proxyHelper.ProxyBypass.Length && proxyHelper.ProxyBypass[idx] != ';')
                        {
                            // Got stopped at space or ']'. Strip until next ';' or end.
                            while (idx < proxyHelper.ProxyBypass.Length && proxyHelper.ProxyBypass[idx] != ';' ) {idx += 1; };
                        }
                        if  (idx < proxyHelper.ProxyBypass.Length && proxyHelper.ProxyBypass[idx] == ';')
                        {
                            idx ++;
                        }
                        if (tmp == null)
                        {
                            continue;
                        }

                        try
                        {
                            // Escape any special characters and unescape * to get wildcard pattern match.
                            Regex re = new Regex(Regex.Escape(tmp).Replace("\\*", ".*?") + "$",
                                            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                            _bypass.Add(re);
                        }
                        catch (Exception ex)
                        {
                            if (NetEventSource.IsEnabled)
                            {
                                NetEventSource.Error(this, $"Failed to process {tmp} from bypass list: {ex}");
                            }
                        }
                    }
                    if (_bypass.Count == 0)
                    {
                        // Bypass string only had garbage we did not parse.
                        _bypass = null;
                    }
                }

                if (_bypassLocal)
                {
                    _localIp =  new List<IPAddress>();
                    foreach (NetworkInterface netInterface in NetworkInterface.GetAllNetworkInterfaces())
                    {
                        IPInterfaceProperties ipProps = netInterface.GetIPProperties();
                        foreach (UnicastIPAddressInformation addr in ipProps.UnicastAddresses)
                        {
                            _localIp.Add(addr.Address);
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                if (_sessionHandle != null && !_sessionHandle.IsInvalid)
                {
                    SafeWinHttpHandle.DisposeAndClearHandle(ref _sessionHandle);
                }
            }
        }

        /// <summary>
        /// This function will evaluate given string and it will try to convert
        /// it to a Uri object. The string could contain URI fragment, IP address and  port
        /// tuple or just IP address or name. It will return null if parsing fails.
        /// </summary>
        private static Uri GetUriFromString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            if (!value.Contains("://"))
            {
                value = "http://" + value;
            }

            if (Uri.TryCreate(value, UriKind.Absolute, out Uri uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                // We only support http and https for now.
                return uri;
            }
            return null;
        }

        /// <summary>
        /// This function is used to parse WinINet Proxy strings. The strings are a semicolon
        /// or whitespace separated list, with each entry in the following format:
        /// ([&lt;scheme&gt;=][&lt;scheme&gt;"://"]&lt;server&gt;[":"&lt;port&gt;])
        /// </summary>
        private static void ParseProxyConfig(string value, out Uri insecureProxy, out Uri secureProxy )
        {
            secureProxy = null;
            insecureProxy = null;
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            int idx = value.IndexOf("http://");
            if (idx >= 0)
            {
                int proxyLength = GetProxySubstringLength(value, idx);
                Uri.TryCreate(value.Substring(idx, proxyLength) , UriKind.Absolute, out insecureProxy);
            }

            if (insecureProxy == null)
            {
                idx = value.IndexOf("http=");
                if (idx >= 0)
                {
                    idx += 5; // Skip "http=" so we can replace it with "http://"
                    int proxyLength = GetProxySubstringLength(value, idx);
                    Uri.TryCreate("http://" + value.Substring(idx, proxyLength) , UriKind.Absolute, out insecureProxy);
                }
            }

            idx = value.IndexOf("https://");
            if (idx >= 0)
            {
                idx += 8; // Skip "https://" so we can replace it with "http://"
                int proxyLength = GetProxySubstringLength(value, idx);
                Uri.TryCreate("http://" + value.Substring(idx, proxyLength) , UriKind.Absolute, out secureProxy);
            }

            if (secureProxy == null)
            {
                idx = value.IndexOf("https=");
                if (idx >= 0)
                {
                    idx += 6; // Skip "https=" so we can replace it with "http://"
                    int proxyLength = GetProxySubstringLength(value, idx);
                    Uri.TryCreate("http://" + value.Substring(idx, proxyLength) , UriKind.Absolute, out secureProxy);
                }
            }
        }

        private static int GetProxySubstringLength(string proxyString, int idx)
        {
            int endOfProxy = proxyString.IndexOfAny(s_proxyDelimiters, idx);
            return (endOfProxy == -1) ? proxyString.Length - idx : endOfProxy - idx;
        }

        /// <summary>
        /// Gets the proxy URI. (IWebProxy interface)
        /// </summary>
        public Uri GetProxy(Uri uri)
        {
            if (_proxyHelper.ManualSettingsOnly)
            {
                if (_bypassLocal)
                {
                    IPAddress address = null;

                    if (uri.IsLoopback)
                    {
                        // This is optimization for loopback addresses.
                        // Unfortunately this does not work for all local addresses.
                        return null;
                    }

                    // Pre-Check if host may be IP address to avoid parsing.
                    if (uri.HostNameType == UriHostNameType.IPv6 || uri.HostNameType == UriHostNameType.IPv4)
                    {
                        // RFC1123 allows labels to start with number.
                        // Leading number may or may not be IP address.
                        // IPv6 [::1] notation. '[' is not valid character in names.
                        if (IPAddress.TryParse(uri.IdnHost, out address))
                        {
                            // Host is valid IP address.
                            // Check if it belongs to local system.
                            foreach (IPAddress a in _localIp)
                            {
                                if (a.Equals(address))
                                {
                                    return null;
                                }
                            }
                        }
                    }
                    if (uri.HostNameType != UriHostNameType.IPv6 && !uri.IdnHost.Contains('.'))
                    {
                        // Not address and does not have a dot.
                        // Hosts without FQDN are considered local.
                        return null;
                    }
                }

                // Check if we have other rules for bypass.
                if (_bypass != null)
                {
                    foreach (Regex entry in _bypass)
                    {
                        // IdnHost does not have [].
                        if (entry.IsMatch(uri.IdnHost))
                        {
                            return null;
                        }
                    }
                }

                // We did not find match on bypass list.
                return (uri.Scheme == UriScheme.Https || uri.Scheme == UriScheme.Wss) ? _secureProxyUri : _insecureProxyUri;
            }

            // For anything else ask WinHTTP. To improve performance, we don't call into
            // WinHTTP if there was a recent failure to detect a PAC file on the network.
            if (!_proxyHelper.RecentAutoDetectionFailure)
            {
                var proxyInfo = new Interop.WinHttp.WINHTTP_PROXY_INFO();
                try
                {
                    if (_proxyHelper.GetProxyForUrl(_sessionHandle, uri, out proxyInfo))
                    {
                        return GetUriFromString(Marshal.PtrToStringUni(proxyInfo.Proxy));
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(proxyInfo.Proxy);
                    Marshal.FreeHGlobal(proxyInfo.ProxyBypass);
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if URI is subject to proxy or not.
        /// </summary>
        public bool IsBypassed(Uri uri)
        {
            if (_proxyHelper.ManualSettingsOnly)
            {
                if (_bypassLocal)
                {
                    // TODO #23150: implement bypass match.
                }
                return false;
            }
            else if (_proxyHelper.AutoSettingsUsed)
            {
                // Always return false for now to avoid query to WinHtttp.
                // If URI should be bypassed GetProxy() will return null;
                return false;
            }
            return true;
        }

        public ICredentials Credentials
        {
            get
            {
                return _credentials;
            }
            set
            {
                _credentials = value;
            }
        }

        // Access function for unit tests.
        internal List<Regex> BypassList => _bypass;
    }
}
