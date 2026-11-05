// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Http
{
    public partial class HttpClientHandler : HttpMessageHandler
    {
        private WasiHttpHandler WasiHandler => (WasiHttpHandler)_nativeUnderlyingHandler!;

        private ICredentials? GetDefaultProxyCredentials() => WasiHandler.DefaultProxyCredentials;

        private void SetDefaultProxyCredentials(ICredentials? value) => WasiHandler.DefaultProxyCredentials = value;

        private int GetMaxConnectionsPerServer() => WasiHandler.MaxConnectionsPerServer;

        private void SetMaxConnectionsPerServer(int value) => WasiHandler.MaxConnectionsPerServer = value;

        private int GetMaxResponseHeadersLength() => WasiHandler.MaxResponseHeadersLength;

        private void SetMaxResponseHeadersLength(int value) => WasiHandler.MaxResponseHeadersLength = value;

        private ClientCertificateOption GetClientCertificateOptions() => WasiHandler.ClientCertificateOptions;

        private void SetClientCertificateOptions(ClientCertificateOption value)
        {
            if (value is not (ClientCertificateOption.Manual or ClientCertificateOption.Automatic))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            WasiHandler.ClientCertificateOptions = value;
        }

        private X509CertificateCollection GetClientCertificates()
        {
            if (GetClientCertificateOptions() != ClientCertificateOption.Manual)
            {
                throw new InvalidOperationException(SR.Format(SR.net_http_invalid_enable_first, nameof(ClientCertificateOptions), nameof(ClientCertificateOption.Manual)));
            }

            throw new PlatformNotSupportedException();
        }

        private static Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> GetServerCertificateCustomValidationCallback() =>
            throw new PlatformNotSupportedException();

        private static void SetServerCertificateCustomValidationCallback(Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool>? value) =>
            throw new PlatformNotSupportedException();

        private static bool GetCheckCertificateRevocationList() => throw new PlatformNotSupportedException();

        private static void SetCheckCertificateRevocationList(bool value) => throw new PlatformNotSupportedException();

        private static SslProtocols GetSslProtocols() => throw new PlatformNotSupportedException();

        private static void SetSslProtocols(SslProtocols value) => throw new PlatformNotSupportedException();

        private IDictionary<string, object?> GetProperties() => WasiHandler.Properties;

        private static bool GetSupportsAutomaticDecompression() => WasiHttpHandler.SupportsAutomaticDecompression;

        private static bool GetSupportsProxy() => WasiHttpHandler.SupportsProxy;

        private static bool GetSupportsRedirectConfiguration() => WasiHttpHandler.SupportsRedirectConfiguration;

        private DecompressionMethods GetAutomaticDecompression() => WasiHandler.AutomaticDecompression;

        private void SetAutomaticDecompression(DecompressionMethods value) => WasiHandler.AutomaticDecompression = value;

        private bool GetUseProxy() => WasiHandler.UseProxy;

        private void SetUseProxy(bool value) => WasiHandler.UseProxy = value;

        private IWebProxy GetProxy() => WasiHandler.Proxy!;

        private void SetProxy(IWebProxy value) => WasiHandler.Proxy = value;

        private bool GetPreAuthenticate() => WasiHandler.PreAuthenticate;

        private void SetPreAuthenticate(bool value) => WasiHandler.PreAuthenticate = value;

        private int GetMaxAutomaticRedirections() => WasiHandler.MaxAutomaticRedirections;

        private void SetMaxAutomaticRedirections(int value) => WasiHandler.MaxAutomaticRedirections = value;

        private bool GetUseCookies() => WasiHandler.UseCookies;

        private void SetUseCookies(bool value) => WasiHandler.UseCookies = value;

        private CookieContainer GetCookieContainer() => WasiHandler.CookieContainer;

        private void SetCookieContainer(CookieContainer value) => WasiHandler.CookieContainer = value;

        private bool GetAllowAutoRedirect() => WasiHandler.AllowAutoRedirect;

        private void SetAllowAutoRedirect(bool value) => WasiHandler.AllowAutoRedirect = value;

        private ICredentials GetCredentials() => WasiHandler.Credentials!;

        private void SetCredentials(ICredentials? value) => WasiHandler.Credentials = value;

        private static WasiHttpHandler CreateNativeHandler() => new WasiHttpHandler();
    }
}
