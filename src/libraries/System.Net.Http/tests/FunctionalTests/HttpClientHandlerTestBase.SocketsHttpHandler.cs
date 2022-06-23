// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Quic;
using System.Net.Test.Common;
using System.Reflection;
using System.Threading.Tasks;

namespace System.Net.Http.Functional.Tests
{
    public abstract partial class HttpClientHandlerTestBase : FileCleanupTestBase
    {
        protected static bool IsWinHttpHandler => false;

        public static bool IsQuicSupported
        {
            get
            {
                try
                {
                    return QuicConnection.IsSupported;
                }
                catch (System.PlatformNotSupportedException)
                {
                    return false;
                }
            }
        }

        // This should correspond to platforms on which System.Net.Quic is supported. See also HttpConnectionPool.IsHttp3Supported().
        public static bool IsMockQuicSupported
            => (OperatingSystem.IsLinux() && !OperatingSystem.IsAndroid()) || OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();

        protected static HttpClientHandler CreateHttpClientHandler(Version useVersion = null, bool allowAllCertificates = true)
        {
            useVersion ??= HttpVersion.Version11;

            HttpClientHandler handler = (PlatformDetection.SupportsAlpn && useVersion != HttpVersion.Version30) ? new HttpClientHandler() : new VersionHttpClientHandler(useVersion);

            if (useVersion >= HttpVersion.Version20 && allowAllCertificates)
            {
                handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;
            }

            return handler;
        }

        protected Http3LoopbackServer CreateHttp3LoopbackServer(Http3Options options = default)
        {
            return new Http3LoopbackServer(options);
        }

        protected HttpClientHandler CreateHttpClientHandler() => CreateHttpClientHandler(UseVersion);

        protected static HttpClientHandler CreateHttpClientHandler(string useVersionString) =>
            CreateHttpClientHandler(Version.Parse(useVersionString));

        protected static SocketsHttpHandler GetUnderlyingSocketsHttpHandler(HttpClientHandler handler)
        {
            var fieldName = PlatformDetection.IsMobile ? "_socketHandler" : "_underlyingHandler";
            FieldInfo field = typeof(HttpClientHandler).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            return (SocketsHttpHandler)field?.GetValue(handler);
        }

        protected static HttpRequestMessage CreateRequest(HttpMethod method, Uri uri, Version version, bool exactVersion = false) =>
            new HttpRequestMessage(method, uri)
            {
                Version = version,
                VersionPolicy = exactVersion ? HttpVersionPolicy.RequestVersionExact : HttpVersionPolicy.RequestVersionOrLower
            };

        protected LoopbackServerFactory LoopbackServerFactory => GetFactoryForVersion(UseVersion);

        protected static LoopbackServerFactory GetFactoryForVersion(string useVersion) =>
            GetFactoryForVersion(Version.Parse(useVersion));

        protected static LoopbackServerFactory GetFactoryForVersion(Version useVersion)
        {
            return useVersion.Major switch
            {
#if NETCOREAPP
#if HTTP3
                3 => Http3LoopbackServerFactory.Singleton,
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
