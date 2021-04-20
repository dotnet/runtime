// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public sealed class SyncHttpHandler_HttpProtocolTests : HttpProtocolTests
    {
        public SyncHttpHandler_HttpProtocolTests(ITestOutputHelper output) : base(output) { }
        protected override bool TestAsync => false;
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public sealed class SyncHttpHandler_HttpProtocolTests_Dribble : HttpProtocolTests_Dribble
    {
        public SyncHttpHandler_HttpProtocolTests_Dribble(ITestOutputHelper output) : base(output) { }
        protected override bool TestAsync => false;
    }

    public sealed class SyncHttpHandler_DiagnosticsTest : DiagnosticsTest
    {
        public SyncHttpHandler_DiagnosticsTest(ITestOutputHelper output) : base(output) { }
        protected override bool TestAsync => false;
    }

    public sealed class SyncHttpHandler_PostScenarioTest : PostScenarioTest
    {
        public SyncHttpHandler_PostScenarioTest(ITestOutputHelper output) : base(output) { }
        protected override bool TestAsync => false;
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public sealed class SyncHttpHandler_HttpClientHandlerTest : HttpClientHandlerTest
    {
        public SyncHttpHandler_HttpClientHandlerTest(ITestOutputHelper output) : base(output) { }
        protected override bool TestAsync => false;
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public sealed class SyncHttpHandlerTest_AutoRedirect : HttpClientHandlerTest_AutoRedirect
    {
        public SyncHttpHandlerTest_AutoRedirect(ITestOutputHelper output) : base(output) { }
        protected override bool TestAsync => false;
    }

    public sealed class SyncHttpHandler_HttpClientHandler_Decompression_Tests : HttpClientHandler_Decompression_Test
    {
        public SyncHttpHandler_HttpClientHandler_Decompression_Tests(ITestOutputHelper output) : base(output) { }
        protected override bool TestAsync => false;
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public sealed class SyncHttpHandler_IdnaProtocolTests : IdnaProtocolTests
    {
        public SyncHttpHandler_IdnaProtocolTests(ITestOutputHelper output) : base(output) { }
        protected override bool TestAsync => false;
        protected override bool SupportsIdna => true;
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public sealed class SyncHttpHandlerTest_RequestRetry : HttpClientHandlerTest_RequestRetry
    {
        public SyncHttpHandlerTest_RequestRetry(ITestOutputHelper output) : base(output) { }
        protected override bool TestAsync => false;
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public sealed class SyncHttpHandlerTest_Cookies : HttpClientHandlerTest_Cookies
    {
        public SyncHttpHandlerTest_Cookies(ITestOutputHelper output) : base(output) { }
        protected override bool TestAsync => false;
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public sealed class SyncHttpHandlerTest_Cookies_Http11 : HttpClientHandlerTest_Cookies_Http11
    {
        public SyncHttpHandlerTest_Cookies_Http11(ITestOutputHelper output) : base(output) { }
        protected override bool TestAsync => false;
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public sealed class SyncHttpHandler_HttpClientHandler_Cancellation_Test : HttpClientHandler_Http11_Cancellation_Test
    {
        public SyncHttpHandler_HttpClientHandler_Cancellation_Test(ITestOutputHelper output) : base(output) { }
        protected override bool TestAsync => false;
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public sealed class SyncHttpHandler_HttpClientHandler_Authentication_Test : HttpClientHandler_Authentication_Test
    {
        public SyncHttpHandler_HttpClientHandler_Authentication_Test(ITestOutputHelper output) : base(output) { }
        protected override bool TestAsync => false;
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public sealed class SyncHttpHandler_Connect_Test : HttpClientHandler_Connect_Test
    {
        public SyncHttpHandler_Connect_Test(ITestOutputHelper output) : base(output) { }
        protected override bool TestAsync => false;
    }

    [SkipOnPlatform(TestPlatforms.Browser, "System.Net.Sockets is not supported on this platform.")]
    public sealed class SyncHttpHandlerTest_HttpClientHandlerTest_Headers : HttpClientHandlerTest_Headers
    {
        public SyncHttpHandlerTest_HttpClientHandlerTest_Headers(ITestOutputHelper output) : base(output) { }
        protected override bool TestAsync => false;
    }
}
