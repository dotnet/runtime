// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http
{
    internal sealed partial class HttpEnvironmentProxy : IWebProxy
    {
        private const string EnvAllProxyLC = "all_proxy";
        private const string EnvHttpProxyLC = "http_proxy";
        private const string EnvHttpsProxyLC = "https_proxy";
        private const string EnvNoProxyLC = "no_proxy";

        public static bool TryCreate([NotNullWhen(true)] out IWebProxy? proxy)
        {
            // Get environment variables. Protocol specific take precedence over
            // general all_*, lower case variable has precedence over upper case.
            // Note that curl uses HTTPS_PROXY but not HTTP_PROXY.

            Uri? httpProxy = GetUriFromString(Environment.GetEnvironmentVariable(EnvHttpProxyLC));
            if (httpProxy is null && Environment.GetEnvironmentVariable(EnvCGI) is null)
            {
                httpProxy = GetUriFromString(Environment.GetEnvironmentVariable(EnvHttpProxyUC));
            }

            Uri? httpsProxy = GetUriFromString(Environment.GetEnvironmentVariable(EnvHttpsProxyLC)) ??
                             GetUriFromString(Environment.GetEnvironmentVariable(EnvHttpsProxyUC));

            if (httpProxy is null || httpsProxy is null)
            {
                Uri? allProxy = GetUriFromString(Environment.GetEnvironmentVariable(EnvAllProxyLC)) ??
                                GetUriFromString(Environment.GetEnvironmentVariable(EnvAllProxyUC));

                if (httpProxy is null)
                {
                    httpProxy = allProxy;
                }

                if (httpsProxy is null)
                {
                    httpsProxy = allProxy;
                }
            }

            // Do not instantiate if nothing is set.
            // Caller may pick some other proxy type.
            if (httpProxy is null && httpsProxy is null)
            {
                proxy = null;
                return false;
            }

            string? noProxy = Environment.GetEnvironmentVariable(EnvNoProxyLC) ??
                Environment.GetEnvironmentVariable(EnvNoProxyUC);
            proxy = new HttpEnvironmentProxy(httpProxy, httpsProxy, noProxy);

            return true;
        }
    }
}
