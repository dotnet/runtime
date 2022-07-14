// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Net;
using System.Collections.Generic;

namespace System.Net.Http
{
    internal sealed class HttpEnvironmentProxyCredentials : ICredentials
    {
        // Wrapper class for cases when http and https has different authentication.
        private readonly NetworkCredential? _httpCred;
        private readonly NetworkCredential? _httpsCred;
        private readonly Uri? _httpProxy;
        private readonly Uri? _httpsProxy;

        public HttpEnvironmentProxyCredentials(Uri? httpProxy, NetworkCredential? httpCred,
                                                Uri? httpsProxy, NetworkCredential? httpsCred)
        {
            _httpCred = httpCred;
            _httpsCred = httpsCred;
            _httpProxy = httpProxy;
            _httpsProxy = httpsProxy;
        }

        public NetworkCredential? GetCredential(Uri? uri, string authType)
        {
            if (uri == null)
            {
                return null;
            }
            return uri.Equals(_httpProxy) ? _httpCred :
                   uri.Equals(_httpsProxy) ? _httpsCred : null;
        }

        public static HttpEnvironmentProxyCredentials? TryCreate(Uri? httpProxy, Uri? httpsProxy)
        {
            NetworkCredential? httpCred = null;
            NetworkCredential? httpsCred = null;

            if (httpProxy != null)
            {
                httpCred = GetCredentialsFromString(httpProxy.UserInfo);
            }
            if (httpsProxy != null)
            {
                httpsCred = GetCredentialsFromString(httpsProxy.UserInfo);
            }
            if (httpCred == null && httpsCred == null)
            {
                return null;
            }
            return new HttpEnvironmentProxyCredentials(httpProxy, httpCred, httpsProxy, httpsCred);
        }

        /// <summary>
        /// Converts string containing user:password to NetworkCredential object
        /// </summary>
        private static NetworkCredential? GetCredentialsFromString(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (value == ":")
            {
                return CredentialCache.DefaultNetworkCredentials;
            }

            value = Uri.UnescapeDataString(value);

            string password = "";
            string? domain = null;
            int idx = value.IndexOf(':');
            if (idx != -1)
            {
                password = value.Substring(idx + 1);
                value = value.Substring(0, idx);
            }

            idx = value.IndexOf('\\');
            if (idx != -1)
            {
                domain = value.Substring(0, idx);
                value = value.Substring(idx + 1);
            }

            return new NetworkCredential(value, password, domain);
        }
    }

    internal sealed partial class HttpEnvironmentProxy : IWebProxy
    {
        private const string EnvAllProxyUC = "ALL_PROXY";
        private const string EnvHttpProxyUC = "HTTP_PROXY";
        private const string EnvHttpsProxyUC = "HTTPS_PROXY";
        private const string EnvNoProxyUC = "NO_PROXY";
        private const string EnvCGI = "GATEWAY_INTERFACE"; // Running in a CGI environment.

        private readonly Uri? _httpProxyUri;       // String URI for HTTP requests
        private readonly Uri? _httpsProxyUri;      // String URI for HTTPS requests
        private readonly string[]? _bypass;        // list of domains not to proxy
        private ICredentials? _credentials;

        private HttpEnvironmentProxy(Uri? httpProxy, Uri? httpsProxy, string? bypassList)
        {
            _httpProxyUri = httpProxy;
            _httpsProxyUri = httpsProxy;

            _credentials = HttpEnvironmentProxyCredentials.TryCreate(httpProxy, httpsProxy);
            _bypass = bypassList?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// This function will evaluate given string and it will try to convert
        /// it to Uri object. The string could contain URI fragment, IP address and  port
        /// tuple or just IP address or name. It will return null if parsing fails.
        /// </summary>
        private static Uri? GetUriFromString(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring(7);
            }

            string? user = null;
            string? password = null;
            ushort port = 80;
            string host;

            // Check if there is authentication part with user and possibly password.
            // Curl accepts raw text and that may break URI parser.
            int separatorIndex = value.LastIndexOf('@');
            if (separatorIndex != -1)
            {
                string auth = value.Substring(0, separatorIndex);

                // The User and password may or may not be URL encoded.
                // Curl seems to accept both. To match that,
                // we do opportunistic decode and we use original string if it fails.
                try
                {
                    auth = Uri.UnescapeDataString(auth);
                }
                catch { };

                value = value.Substring(separatorIndex + 1);
                separatorIndex = auth.IndexOf(':');
                if (separatorIndex == -1)
                {
                    user = auth;
                }
                else
                {
                    user = auth.Substring(0, separatorIndex);
                    password = auth.Substring(separatorIndex + 1);
                }
            }

            int ipV6AddressEnd = value.IndexOf(']');
            separatorIndex = value.LastIndexOf(':');
            // No ':' or it is part of IPv6 address.
            if (separatorIndex == -1 || (ipV6AddressEnd != -1 && separatorIndex < ipV6AddressEnd))
            {
                host = value;
            }
            else
            {
                host = value.Substring(0, separatorIndex);
                int endIndex = separatorIndex + 1;
                // Strip any trailing characters after port number.
                while (endIndex < value.Length)
                {
                    if (!char.IsDigit(value[endIndex]))
                    {
                        break;
                    }
                    endIndex += 1;
                }

                if (!ushort.TryParse(value.AsSpan(separatorIndex + 1, endIndex - separatorIndex - 1), out port))
                {
                    return null;
                }
            }

            try
            {
                UriBuilder ub = new UriBuilder("http", host, port);
                if (user != null)
                {
                    ub.UserName = Uri.EscapeDataString(user);
                }

                if (password != null)
                {
                    ub.Password = Uri.EscapeDataString(password);
                }

                Uri uri = ub.Uri;

                // if both user and password exist and are empty we should preserve that and use default credentials.
                // UriBuilder does not handle that now e.g. does not distinguish between empty and missing.
                if (user == "" && password == "")
                {
                    string[] tokens = uri.ToString().Split('/', 3);
                    if (tokens.Length == 3)
                    {
                        uri = new Uri($"{tokens[0]}//:@{tokens[2]}");
                    }
                }

                return uri;
            }
            catch { };
            return null;
        }

        /// <summary>
        /// This function returns true if given Host match bypass list.
        /// Note, that the list is common for http and https.
        /// </summary>
        private bool IsMatchInBypassList(Uri input)
        {
            if (_bypass != null)
            {
                foreach (string s in _bypass)
                {
                    if (s[0] == '.')
                    {
                        // This should match either domain it self or any subdomain or host
                        // .foo.com will match foo.com it self or *.foo.com
                        if ((s.Length - 1) == input.Host.Length &&
                            string.Compare(s, 1, input.Host, 0, input.Host.Length, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            return true;
                        }
                        else if (input.Host.EndsWith(s, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }

                    }
                    else
                    {
                        if (string.Equals(s, input.Host, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the proxy URI. (iWebProxy interface)
        /// </summary>
        public Uri? GetProxy(Uri uri)
        {
            return HttpUtilities.IsSupportedNonSecureScheme(uri.Scheme) ? _httpProxyUri : _httpsProxyUri;
        }

        /// <summary>
        /// Checks if URI is subject to proxy or not.
        /// </summary>
        public bool IsBypassed(Uri uri)
        {
            return GetProxy(uri) == null ? true : IsMatchInBypassList(uri);
        }

        public ICredentials? Credentials
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
    }
}
