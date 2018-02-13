// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net.Http;
using System.Net;
using System.Collections.Generic;

namespace System.Net.Http
{
    internal sealed class HttpEnvironmentProxyCredentials : ICredentials
    {
        // Wrapper class for cases when http and https has different authentication.
        private readonly NetworkCredential _httpCred;
        private readonly NetworkCredential _httpsCred;
        private readonly Uri _httpProxy;
        private readonly Uri _httpsProxy;

        public HttpEnvironmentProxyCredentials(Uri httpProxy, NetworkCredential httpCred,
                                                Uri httpsProxy, NetworkCredential httpsCred)
        {
            _httpCred = httpCred;
            _httpsCred = httpsCred;
            _httpProxy = httpProxy;
            _httpsProxy = httpsProxy;
        }

        public NetworkCredential GetCredential(Uri uri, string authType)
        {
            if (uri == null)
            {
                return null;
            }
            return uri.Equals(_httpProxy) ? _httpCred :
                   uri.Equals(_httpsProxy) ? _httpsCred : null;
        }

        public static HttpEnvironmentProxyCredentials TryCreate(Uri httpProxy, Uri httpsProxy)
        {
            NetworkCredential httpCred = null;
            NetworkCredential httpsCred = null;

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
        private static NetworkCredential GetCredentialsFromString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }
            int idx = value.IndexOf(":");
            if (idx < 0)
            {
                // only user name without password
                return new NetworkCredential(value, "");
            }
            else
            {
                return new NetworkCredential(value.Substring(0, idx), value.Substring(idx+1, value.Length - idx -1 ));
            }
        }
    }

    internal sealed class HttpEnvironmentProxy : IWebProxy
    {
        private const string EnvAllProxyUC = "ALL_PROXY";
        private const string EnvAllProxyLC = "all_proxy";
        private const string EnvHttpProxyLC = "http_proxy";
        private const string EnvHttpsProxyLC = "https_proxy";
        private const string EnvHttpsProxyUC = "HTTPS_PROXY";
        private const string EnvNoProxyLC = "no_proxy";

        private Uri _httpProxyUri;      // String URI for HTTP requests
        private Uri _httpsProxyUri;     // String URI for HTTPS requests
        private string[] _bypass = null;// list of domains not to proxy
        private ICredentials _credentials;

        public static bool TryCreate(out IWebProxy proxy)
        {
            // Get environmental variables. Protocol specific take precedence over
            // general all_*, lower case variable has precedence over upper case.
            // Note that curl uses HTTPS_PROXY but not HTTP_PROXY.
            // For http, only http_proxy and generic variables are used.

            Uri httpProxy = GetUriFromString(Environment.GetEnvironmentVariable(EnvHttpProxyLC));
            Uri httpsProxy = GetUriFromString(Environment.GetEnvironmentVariable(EnvHttpsProxyLC)) ??
                             GetUriFromString(Environment.GetEnvironmentVariable(EnvHttpsProxyUC));

            if (httpProxy == null || httpsProxy == null)
            {
                Uri allProxy = GetUriFromString(Environment.GetEnvironmentVariable(EnvAllProxyLC)) ??
                                GetUriFromString(Environment.GetEnvironmentVariable(EnvAllProxyUC));

                if (httpProxy == null)
                {
                    httpProxy = allProxy;
                }
                if (httpsProxy == null)
                {
                    httpsProxy = allProxy;
                }
            }

            // Do not instantiate if nothing is set.
            // Caller may pick some other proxy type.
            if (httpProxy == null && httpsProxy == null)
            {
                proxy = null;
                return false;
            }

            proxy = new HttpEnvironmentProxy(httpProxy, httpsProxy, Environment.GetEnvironmentVariable(EnvNoProxyLC));
            return true;
        }

        private HttpEnvironmentProxy(Uri httpProxy, Uri httpsProxy, string bypassList)
        {
            _httpProxyUri = httpProxy;
            _httpsProxyUri = httpsProxy;

            _credentials = HttpEnvironmentProxyCredentials.TryCreate(httpProxy, httpsProxy);

            if (!string.IsNullOrWhiteSpace(bypassList))
            {
                string[] list = bypassList.Split(',');
                List<string> tmpList = new List<string>();

                foreach (string value in list)
                {
                    string tmp = value.Trim();
                    if (tmp.Length > 0)
                    {
                        tmpList.Add(tmp);
                    }
                }
                if (tmpList.Count > 0)
                {
                    _bypass = tmpList.ToArray();
                }
            }
        }

        /// <summary>
        /// This function will evaluate given string and it will try to convert
        /// it to Uri object. The string could contain URI fragment, IP address and  port
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
                            String.Compare(s, 1, input.Host, 0, input.Host.Length, StringComparison.OrdinalIgnoreCase) == 0)
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
                        if (String.Equals(s, input.Host, StringComparison.OrdinalIgnoreCase))
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
        public Uri GetProxy(Uri uri)
        {
            return uri.Scheme == Uri.UriSchemeHttp ? _httpProxyUri : _httpsProxyUri;
        }

        /// <summary>
        /// Checks if URI is subject to proxy or not.
        /// </summary>
        public bool IsBypassed(Uri uri)
        {
            return GetProxy(uri) == null ? true : IsMatchInBypassList(uri);
        }

        public ICredentials Credentials
        {
            get
            {
                return _credentials;
            }
            set { throw new NotSupportedException(); }
        }
    }
}
