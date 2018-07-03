// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public sealed class PlatformHandler_HttpClientHandler_Asynchrony_Test : HttpClientHandler_Asynchrony_Test
    {
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_HttpProtocolTests : HttpProtocolTests
    {
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_HttpProtocolTests_Dribble : HttpProtocolTests_Dribble
    {
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_HttpClientTest : HttpClientTest
    {
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_DiagnosticsTest : DiagnosticsTest
    {
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_HttpClient_SelectedSites_Test : HttpClient_SelectedSites_Test
    {
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_HttpClientEKUTest : HttpClientEKUTest
    {
        protected override bool UseSocketsHttpHandler => false;
    }

#if netcoreapp
    public sealed class PlatformHandler_HttpClientHandler_Decompression_Tests : HttpClientHandler_Decompression_Test
    {
        public PlatformHandler_HttpClientHandler_Decompression_Tests(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_HttpClientHandler_DangerousAcceptAllCertificatesValidator_Test : HttpClientHandler_DangerousAcceptAllCertificatesValidator_Test
    {
        protected override bool UseSocketsHttpHandler => false;
    }
#endif

    public sealed class PlatformHandler_HttpClientHandler_ClientCertificates_Test : HttpClientHandler_ClientCertificates_Test
    {
        public PlatformHandler_HttpClientHandler_ClientCertificates_Test(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_HttpClientHandler_DefaultProxyCredentials_Test : HttpClientHandler_DefaultProxyCredentials_Test
    {
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_HttpClientHandler_MaxConnectionsPerServer_Test : HttpClientHandler_MaxConnectionsPerServer_Test
    {
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_HttpClientHandler_ServerCertificates_Test : HttpClientHandler_ServerCertificates_Test
    {
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_PostScenarioTest : PostScenarioTest
    {
        public PlatformHandler_PostScenarioTest(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_ResponseStreamTest : ResponseStreamTest
    {
        public PlatformHandler_ResponseStreamTest(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_HttpClientHandler_SslProtocols_Test : HttpClientHandler_SslProtocols_Test
    {
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_HttpClientHandler_Proxy_Test : HttpClientHandler_Proxy_Test
    {
        public PlatformHandler_HttpClientHandler_Proxy_Test(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_SchSendAuxRecordHttpTest : SchSendAuxRecordHttpTest
    {
        public PlatformHandler_SchSendAuxRecordHttpTest(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_HttpClientMiniStress : HttpClientMiniStress
    {
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_HttpClientHandlerTest : HttpClientHandlerTest
    {
        public PlatformHandler_HttpClientHandlerTest(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_DefaultCredentialsTest : DefaultCredentialsTest
    {
        public PlatformHandler_DefaultCredentialsTest(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_IdnaProtocolTests : IdnaProtocolTests
    {
        protected override bool UseSocketsHttpHandler => false;
        // WinHttp on Win7 does not support IDNA
        protected override bool SupportsIdna => !PlatformDetection.IsWindows7 && !PlatformDetection.IsFullFramework;
    }

    public sealed class PlatformHandler_HttpRetryProtocolTests : HttpRetryProtocolTests
    {
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_HttpCookieProtocolTests : HttpCookieProtocolTests
    {
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_HttpClientHandler_MaxResponseHeadersLength_Test : HttpClientHandler_MaxResponseHeadersLength_Test
    {
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_HttpClientHandler_Cancellation_Test : HttpClientHandler_Cancellation_Test
    {
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_HttpClientHandler_Authentication_Test : HttpClientHandler_Authentication_Test
    {
        protected override bool UseSocketsHttpHandler => false;
    }
}
