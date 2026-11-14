// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Test.Common;

namespace System.Net.Http.Functional.Tests
{
    public abstract partial class HttpClientHandlerTestBase : FileCleanupTestBase
    {
        protected static bool IsWinHttpHandler => false;

        public static bool IsWasiHttpHandler => true;

        // wasi:http negotiates the protocol version itself and owns TLS.
        protected static HttpClientHandler CreateHttpClientHandler(Version useVersion = null, bool allowAllCertificates = true, bool allowTlsResume = true) =>
            new HttpClientHandler();

        protected HttpClientHandler CreateHttpClientHandler() => CreateHttpClientHandler(UseVersion);

        protected HttpClientHandler CreateHttpClientHandler(bool allowTlsResume) =>
            CreateHttpClientHandler(UseVersion, allowTlsResume: allowTlsResume);

        protected static HttpClientHandler CreateHttpClientHandler(string useVersionString, bool allowTlsResume = true) =>
            CreateHttpClientHandler(Version.Parse(useVersionString), allowTlsResume: allowTlsResume);

        protected static HttpRequestMessage CreateRequest(HttpMethod method, Uri uri, Version version, bool exactVersion = false) =>
            new HttpRequestMessage(method, uri)
            {
                Version = version,
                VersionPolicy = exactVersion ? HttpVersionPolicy.RequestVersionExact : HttpVersionPolicy.RequestVersionOrLower
            };

        protected LoopbackServerFactory LoopbackServerFactory => GetFactoryForVersion(UseVersion);

        protected static LoopbackServerFactory GetFactoryForVersion(string useVersion) =>
            GetFactoryForVersion(Version.Parse(useVersion));

        protected static LoopbackServerFactory GetFactoryForVersion(Version useVersion) =>
            useVersion.Major switch
            {
                2 => Http2LoopbackServerFactory.Singleton,
                _ => Http11LoopbackServerFactory.Singleton
            };
    }
}
