// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Test.Common;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.PlatformAbstractions;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public abstract class HttpClientHandlerTestBase : FileCleanupTestBase
    {
        public readonly ITestOutputHelper _output;

        protected virtual bool UseHttp2 => false;

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

        protected HttpClientHandler CreateHttpClientHandler() => CreateHttpClientHandler(UseHttp2);

        protected static HttpClientHandler CreateHttpClientHandler(string useHttp2LoopbackServerString) =>
            CreateHttpClientHandler(bool.Parse(useHttp2LoopbackServerString));

        protected static HttpClientHandler CreateHttpClientHandler(bool useHttp2LoopbackServer = false)
        {
            HttpClientHandler handler = new HttpClientHandler();

            if (useHttp2LoopbackServer)
            {
                TestHelper.EnableUnencryptedHttp2IfNecessary(handler);
                handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;
            }

            return handler;
        }

        protected static object GetUnderlyingSocketsHttpHandler(HttpClientHandler handler)
        {
            FieldInfo field = typeof(HttpClientHandler).GetField("_socketsHttpHandler", BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(handler);
        }

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

        protected HttpClient CreateHttpClientForRemoteServer(Configuration.Http.RemoteServer remoteServer, HttpClientHandler httpClientHandler)
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
