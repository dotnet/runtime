// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

#if WINHTTPHANDLER_TEST
    using HttpClientHandler = System.Net.Http.WinHttpClientHandler;
#endif

    public abstract partial class HttpClientHandlerTestBase : FileCleanupTestBase
    {
        public readonly ITestOutputHelper _output;

        protected virtual Version UseVersion => HttpVersion.Version11;

        public HttpClientHandlerTestBase(ITestOutputHelper output)
        {
            _output = output;
        }

        protected virtual HttpClient CreateHttpClient() => CreateHttpClient(CreateHttpClientHandler());

        protected HttpClient CreateHttpClient(HttpMessageHandler handler) =>
            new HttpClient(handler) {
#if !NETFRAMEWORK
                DefaultRequestVersion = UseVersion
#endif
            };

        protected static HttpClient CreateHttpClient(string useVersionString) =>
            CreateHttpClient(CreateHttpClientHandler(useVersionString), useVersionString);

        protected static HttpClient CreateHttpClient(HttpMessageHandler handler, string useVersionString) =>
            new HttpClient(handler) {
#if !NETFRAMEWORK
                DefaultRequestVersion = Version.Parse(useVersionString)
#endif
            };

        protected HttpClientHandler CreateHttpClientHandler() => CreateHttpClientHandler(UseVersion);

        protected static HttpClientHandler CreateHttpClientHandler(string useVersionString) =>
            CreateHttpClientHandler(Version.Parse(useVersionString));

        protected LoopbackServerFactory LoopbackServerFactory => GetFactoryForVersion(UseVersion);

        protected static LoopbackServerFactory GetFactoryForVersion(Version useVersion)
        {
            return useVersion.Major switch
            {
#if NETCOREAPP || WINHTTPHANDLER_TEST
#if HTTP3
                3 => Http3LoopbackServerFactory.Singleton,
#endif
                2 => Http2LoopbackServerFactory.Singleton,
#endif
                _ => Http11LoopbackServerFactory.Singleton
            };
        }

        // For use by remote server tests

        public static readonly IEnumerable<object[]> RemoteServersMemberData = Configuration.Http.RemoteServersMemberData;

        protected HttpClient CreateHttpClientForRemoteServer(Configuration.Http.RemoteServer remoteServer)
        {
            return CreateHttpClientForRemoteServer(remoteServer, CreateHttpClientHandler());
        }

        protected HttpClient CreateHttpClientForRemoteServer(Configuration.Http.RemoteServer remoteServer, HttpMessageHandler httpClientHandler)
        {
            HttpMessageHandler wrappedHandler = httpClientHandler;

            // WinHttpHandler will downgrade to 1.1 if you set Transfer-Encoding: chunked.
            // So, skip this verification if we're not using SocketsHttpHandler.
            if (PlatformDetection.SupportsAlpn && !IsWinHttpHandler)
            {
                wrappedHandler = new VersionCheckerHttpHandler(httpClientHandler, remoteServer.HttpVersion);
            }

            return new HttpClient(wrappedHandler) {
#if !NETFRAMEWORK
                DefaultRequestVersion = remoteServer.HttpVersion
#endif
            };
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
