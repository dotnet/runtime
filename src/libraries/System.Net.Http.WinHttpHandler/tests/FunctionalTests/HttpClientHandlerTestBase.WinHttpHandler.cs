// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Test.Common;
using System.IO;

namespace System.Net.Http.Functional.Tests
{
    public abstract partial class HttpClientHandlerTestBase : FileCleanupTestBase
    {
        protected static bool IsWinHttpHandler => true;

        // WinHTTP owns TLS and does not expose SslStream's resumption setting.
        protected static WinHttpClientHandler CreateHttpClientHandler(Version useVersion = null, bool allowAllCertificates = true, bool allowTlsResume = true)
        {
            useVersion ??= HttpVersion.Version11;

            WinHttpClientHandler handler = new WinHttpClientHandler(useVersion);

            if (allowAllCertificates)
            {
                handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;
            }

            return handler;
        }

        protected WinHttpClientHandler CreateHttpClientHandler() => CreateHttpClientHandler(UseVersion);

        protected WinHttpClientHandler CreateHttpClientHandler(bool allowTlsResume) =>
            CreateHttpClientHandler(UseVersion, allowTlsResume: allowTlsResume);

        protected static WinHttpClientHandler CreateHttpClientHandler(string useVersionString, bool allowTlsResume = true) =>
            CreateHttpClientHandler(Version.Parse(useVersionString), allowTlsResume: allowTlsResume);

        protected static HttpRequestMessage CreateRequest(HttpMethod method, Uri uri, Version version, bool exactVersion = false) =>
            new HttpRequestMessage(method, uri)
            {
                Version = version
            };

        protected LoopbackServerFactory LoopbackServerFactory => GetFactoryForVersion(UseVersion);

        protected static LoopbackServerFactory GetFactoryForVersion(Version useVersion)
        {
            return useVersion.Major switch
            {
                2 => Http2LoopbackServerFactory.Singleton,
                _ => Http11LoopbackServerFactory.Singleton
            };
        }

    }
}
