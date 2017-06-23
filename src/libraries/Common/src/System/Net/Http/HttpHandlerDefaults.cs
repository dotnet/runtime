// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Http
{
    /// <summary>
    /// Defines default values for http handler properties which is meant to be re-used across WinHttp and UnixHttp Handlers
    /// </summary>
    internal static class HttpHandlerDefaults
    {
        public const int DefaultMaxAutomaticRedirections = 50;
        public const int DefaultMaxConnectionsPerServer = int.MaxValue;
        public const int DefaultMaxResponseHeadersLength = 64; // Units in K (1024) bytes.
        public const DecompressionMethods DefaultAutomaticDecompression = DecompressionMethods.None;
        public const bool DefaultAutomaticRedirection = true;
        public const bool DefaultUseCookies = true;
        public const bool DefaultPreAuthenticate = false;
        public const ClientCertificateOption DefaultClientCertificateOption = ClientCertificateOption.Manual;
        public const bool DefaultUseProxy = true;
        public const bool DefaultUseDefaultCredentials = false;
        public const bool DefaultCheckCertificateRevocationList = false;

        public static TimeSpan DefaultConnectTimeout => TimeSpan.FromSeconds(60);
    }
}
