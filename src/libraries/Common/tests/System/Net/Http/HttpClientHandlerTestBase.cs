// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Test.Common;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

#if WINHTTPHANDLER_TEST
    using HttpClientHandler = System.Net.Http.WinHttpHandler;
#endif

    public abstract partial class HttpClientHandlerTestBase : FileCleanupTestBase
    {
        public readonly ITestOutputHelper _output;

        protected virtual bool UseHttp2 => false;

        protected bool GetAllowAutoRedirect(HttpClientHandler handler)
        {        
#if WINHTTPHANDLER_TEST
            return handler.AutomaticRedirection;
#else
            return handler.AllowAutoRedirect;
#endif
        }

        protected bool SetAllowAutoRedirect(HttpClientHandler handler, bool redirect)
        {        
#if WINHTTPHANDLER_TEST
            return handler.AutomaticRedirection = redirect;
#else
            return handler.AllowAutoRedirect = redirect;
#endif
        }

        protected ClientCertificateOption GetClientCertificateOptions(HttpClientHandler handler)
        {        
#if WINHTTPHANDLER_TEST
            return handler.ClientCertificateOption;
#else
            return handler.ClientCertificateOptions;
#endif
        }

        protected ClientCertificateOption SetClientCertificateOptions(HttpClientHandler handler, ClientCertificateOption option)
        {        
#if WINHTTPHANDLER_TEST
            return handler.ClientCertificateOption = option;
#else
            return handler.ClientCertificateOptions = option;
#endif
        }

        protected ICredentials GetCredentials(HttpClientHandler handler)
        {        
#if WINHTTPHANDLER_TEST
            return handler.ServerCredentials;
#else
            return handler.Credentials;
#endif
        }

        protected ICredentials SetCredentials(HttpClientHandler handler, ICredentials credentials)
        {        
#if WINHTTPHANDLER_TEST
            return handler.ServerCredentials = credentials;
#else
            return handler.Credentials = credentials;
#endif
        }

        protected bool GetUseDefaultCredentials(HttpClientHandler handler)
        {
            return GetCredentials(handler) == CredentialCache.DefaultCredentials;
        }

        protected bool SetUseDefaultCredentials(HttpClientHandler handler, bool defaultCredentials)
        {
            if (defaultCredentials)
            {
                SetCredentials(handler, CredentialCache.DefaultCredentials);
            }
            else
            {
                if (GetCredentials(handler) == CredentialCache.DefaultCredentials)
                {
                    SetCredentials(handler, null);
                }
            }

            return defaultCredentials;
        }

        protected bool GetUseCookies(HttpClientHandler handler)
        {        
#if WINHTTPHANDLER_TEST
            return handler.CookieUsePolicy == CookieUsePolicy.UseSpecifiedCookieContainer;
#else
            return handler.UseCookies;
#endif
        }

        protected bool SetUseCookies(HttpClientHandler handler, bool useCookies)
        {        
#if WINHTTPHANDLER_TEST
            handler.CookieUsePolicy = useCookies ? CookieUsePolicy.UseSpecifiedCookieContainer : CookieUsePolicy.IgnoreCookies;
            return useCookies;
#else
            return handler.UseCookies = useCookies;
#endif
        }

        protected Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> GetServerCertificateCustomValidationCallback(HttpClientHandler handler)
        {        
#if WINHTTPHANDLER_TEST
            return handler.ServerCertificateValidationCallback;
#else
            return handler.ServerCertificateCustomValidationCallback;
#endif
        }

        protected Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> SetServerCertificateCustomValidationCallback(HttpClientHandler handler, Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> callback)
        {        
#if WINHTTPHANDLER_TEST
            return handler.ServerCertificateValidationCallback = callback;
#else
            return handler.ServerCertificateCustomValidationCallback = callback;
#endif
        }

        public HttpClientHandlerTestBase(ITestOutputHelper output)
        {
            _output = output;
        }

        protected Version VersionFromUseHttp2 => GetVersion(UseHttp2);

        protected static Version GetVersion(bool http2) => http2 ? new Version(2, 0) : HttpVersion.Version11;

        protected virtual HttpClient CreateHttpClient() => CreateHttpClient(CreateHttpClientHandler());

        protected HttpClient CreateHttpClient(HttpMessageHandler handler) =>
            new HttpClient(handler) { DefaultRequestVersion = VersionFromUseHttp2 };

        protected static HttpClient CreateHttpClient(string useHttp2String) =>
            CreateHttpClient(CreateHttpClientHandler(useHttp2String), useHttp2String);

        protected static HttpClient CreateHttpClient(HttpMessageHandler handler, string useHttp2String) =>
            new HttpClient(handler) { DefaultRequestVersion = GetVersion(bool.Parse(useHttp2String)) };

        protected LoopbackServerFactory LoopbackServerFactory =>
#if NETCOREAPP
            UseHttp2 ?
                (LoopbackServerFactory)Http2LoopbackServerFactory.Singleton :
#endif
                Http11LoopbackServerFactory.Singleton;

        // For use by remote server tests

        public static readonly IEnumerable<object[]> RemoteServersMemberData = Configuration.Http.RemoteServersMemberData;

        protected HttpClient CreateHttpClientForRemoteServer(Configuration.Http.RemoteServer remoteServer)
        {
            return CreateHttpClientForRemoteServer(remoteServer, CreateHttpClientHandler());
        }

        protected HttpClient CreateHttpClientForRemoteServer(Configuration.Http.RemoteServer remoteServer, HttpMessageHandler httpClientHandler)
        {
            HttpMessageHandler wrappedHandler = httpClientHandler;

            // ActiveIssue #39293: WinHttpHandler will downgrade to 1.1 if you set Transfer-Encoding: chunked.
            // So, skip this verification if we're not using SocketsHttpHandler.
            if (PlatformDetection.SupportsAlpn)
            {
                wrappedHandler = new VersionCheckerHttpHandler(httpClientHandler, remoteServer.HttpVersion);
            }

            return new HttpClient(wrappedHandler) { DefaultRequestVersion = remoteServer.HttpVersion };
        }

        private sealed class VersionCheckerHttpHandler : DelegatingHandler
        {
            private readonly Version _expectedVersion;

            public VersionCheckerHttpHandler(HttpMessageHandler innerHandler, Version expectedVersion)
                : base(innerHandler)
            {
                _expectedVersion = expectedVersion;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (request.Version != _expectedVersion)
                {
                    throw new Exception($"Unexpected request version: expected {_expectedVersion}, saw {request.Version}");
                }

                HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

                if (response.Version != _expectedVersion)
                {
                    throw new Exception($"Unexpected response version: expected {_expectedVersion}, saw {response.Version}");
                }

                return response;
            }
        }
    }
}
