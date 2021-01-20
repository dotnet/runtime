// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Quic;
using System.Net.Quic.Implementations;
using System.Net.Test.Common;
using System.Reflection;
using System.Threading.Tasks;

namespace System.Net.Http.Functional.Tests
{
    public abstract partial class HttpClientHandlerTestBase : FileCleanupTestBase
    {
        protected static bool IsWinHttpHandler => false;

        protected virtual QuicImplementationProvider UseQuicImplementationProvider => null;

        public static bool IsMsQuicSupported => QuicImplementationProviders.MsQuic.IsSupported;

        protected static HttpClientHandler CreateHttpClientHandler(Version useVersion = null, QuicImplementationProvider quicImplementationProvider = null)
        {
            useVersion ??= HttpVersion.Version11;

            HttpClientHandler handler = (PlatformDetection.SupportsAlpn && useVersion != HttpVersion.Version30) ? new HttpClientHandler() : new VersionHttpClientHandler(useVersion);

            if (useVersion >= HttpVersion.Version20)
            {
                handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;
            }

            if (quicImplementationProvider != null)
            {
                SocketsHttpHandler socketsHttpHandler = (SocketsHttpHandler)GetUnderlyingSocketsHttpHandler(handler);
                socketsHttpHandler.QuicImplementationProvider = quicImplementationProvider;
            }

            return handler;
        }

        protected HttpClientHandler CreateHttpClientHandler() => CreateHttpClientHandler(UseVersion, UseQuicImplementationProvider);

        protected static HttpClientHandler CreateHttpClientHandler(string useVersionString) =>
            CreateHttpClientHandler(Version.Parse(useVersionString));


        protected static object GetUnderlyingSocketsHttpHandler(HttpClientHandler handler)
        {
            FieldInfo field = typeof(HttpClientHandler).GetField("_underlyingHandler", BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(handler);
        }

        protected static HttpRequestMessage CreateRequest(HttpMethod method, Uri uri, Version version, bool exactVersion = false) =>
            new HttpRequestMessage(method, uri)
            {
                Version = version,
                VersionPolicy = exactVersion ? HttpVersionPolicy.RequestVersionExact : HttpVersionPolicy.RequestVersionOrLower
            };

        protected LoopbackServerFactory LoopbackServerFactory => GetFactoryForVersion(UseVersion, UseQuicImplementationProvider);

        protected static LoopbackServerFactory GetFactoryForVersion(Version useVersion, QuicImplementationProvider quicImplementationProvider = null)
        {
            return useVersion.Major switch
            {
#if NETCOREAPP
#if HTTP3
                3 => new Http3LoopbackServerFactory(quicImplementationProvider),
#endif
                2 => Http2LoopbackServerFactory.Singleton,
#endif
                _ => Http11LoopbackServerFactory.Singleton
            };
        }

    }

    internal class VersionHttpClientHandler : HttpClientHandler
    {
        private readonly Version _useVersion;
        
        public VersionHttpClientHandler(Version useVersion)
        {
            _useVersion = useVersion;
        }

        protected override HttpResponseMessage Send(HttpRequestMessage request, Threading.CancellationToken cancellationToken)
        {
            if (request.Version == _useVersion)
            {
                request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
            }

            return base.Send(request, cancellationToken);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, Threading.CancellationToken cancellationToken)
        {

            if (request.Version == _useVersion)
            {
                request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
            }
            
            return base.SendAsync(request, cancellationToken);
        }

        protected static HttpRequestMessage CreateRequest(HttpMethod method, Uri uri, Version version, bool exactVersion = false) =>
            new HttpRequestMessage(method, uri)
            {
                Version = version,
            };
    }
}
