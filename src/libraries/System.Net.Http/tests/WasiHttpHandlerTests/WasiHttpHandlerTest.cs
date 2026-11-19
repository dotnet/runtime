// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public sealed class WasiHttpHandler_HttpClientHandlerTest : HttpClientHandlerTest
    {
        public WasiHttpHandler_HttpClientHandlerTest(ITestOutputHelper output) : base(output) { }
    }

    public sealed class WasiHttpHandler_HttpClientHandler_Asynchrony_Test : HttpClientHandler_Asynchrony_Test
    {
        public WasiHttpHandler_HttpClientHandler_Asynchrony_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class WasiHttpHandler_HttpClientHandler_Cancellation_Test : HttpClientHandler_Cancellation_Test
    {
        public WasiHttpHandler_HttpClientHandler_Cancellation_Test(ITestOutputHelper output) : base(output) { }
    }

    [ConditionalClass(typeof(HttpClientHandlerTestBase), nameof(SupportsRedirectConfiguration))]
    public sealed class WasiHttpHandler_HttpClientHandlerTest_AutoRedirect : HttpClientHandlerTest_AutoRedirect
    {
        public WasiHttpHandler_HttpClientHandlerTest_AutoRedirect(ITestOutputHelper output) : base(output) { }
    }

    [ConditionalClass(typeof(HttpClientHandlerTestBase), nameof(SupportsAutomaticDecompression))]
    public sealed class WasiHttpHandler_HttpClientHandler_Decompression_Test : HttpClientHandler_Decompression_Test
    {
        public WasiHttpHandler_HttpClientHandler_Decompression_Test(ITestOutputHelper output) : base(output) { }
    }

    [ConditionalClass(typeof(HttpClientHandlerTestBase), nameof(SupportsProxy))]
    public sealed class WasiHttpHandler_HttpClientHandler_DefaultProxyCredentials_Test : HttpClientHandler_DefaultProxyCredentials_Test
    {
        public WasiHttpHandler_HttpClientHandler_DefaultProxyCredentials_Test(ITestOutputHelper output) : base(output) { }
    }

    [ConditionalClass(typeof(HttpClientHandlerTestBase), nameof(SupportsProxy))]
    public sealed class WasiHttpHandler_HttpClientHandler_Proxy_Test : HttpClientHandler_Proxy_Test
    {
        public WasiHttpHandler_HttpClientHandler_Proxy_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class WasiHttpHandler_IdnaProtocolTests : IdnaProtocolTests
    {
        public WasiHttpHandler_IdnaProtocolTests(ITestOutputHelper output) : base(output) { }
    }

    public sealed class WasiHttpHandler_PostScenarioTest : PostScenarioTest
    {
        public WasiHttpHandler_PostScenarioTest(ITestOutputHelper output) : base(output) { }
    }

    public sealed class WasiHttpHandler_ResponseStreamTest : ResponseStreamTest
    {
        public WasiHttpHandler_ResponseStreamTest(ITestOutputHelper output) : base(output) { }
    }
}
