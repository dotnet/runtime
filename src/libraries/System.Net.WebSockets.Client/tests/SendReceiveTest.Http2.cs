// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Net.Http;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    // Memory segment

    // Invoker
    [SkipOnPlatform(TestPlatforms.Browser, "HTTP/2 WebSockets aren't supported on Browser")]
    public sealed class InvokerMemorySendReceiveLocalTest_Http2 : InvokerMemorySendReceiveLocalTest
    {
        public InvokerMemorySendReceiveLocalTest_Http2(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version20;
    }

    [SkipOnPlatform(TestPlatforms.Browser, "Self-signed certificates are not supported on browser")]
    public sealed class InvokerMemorySendReceiveLocalSslTest_Http2 : InvokerMemorySendReceiveLocalSslTest
    {
        public InvokerMemorySendReceiveLocalSslTest_Http2(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version20;
    }

    //HttpClient
    [SkipOnPlatform(TestPlatforms.Browser, "HTTP/2 WebSockets aren't supported on Browser")]
    public sealed class HttpClientMemorySendReceiveLocalTest_Http2 : HttpClientMemorySendReceiveLocalTest
    {
        public HttpClientMemorySendReceiveLocalTest_Http2(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version20;
    }

    [SkipOnPlatform(TestPlatforms.Browser, "Self-signed certificates are not supported on browser")]
    public sealed class HttpClientMemorySendReceiveLocalSslTest_Http2 : HttpClientMemorySendReceiveLocalSslTest
    {
        public HttpClientMemorySendReceiveLocalSslTest_Http2(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version20;
    }

    // Array segment

    //Invoker
    [SkipOnPlatform(TestPlatforms.Browser, "HTTP/2 WebSockets aren't supported on Browser")]
    public sealed class InvokerArraySegmentSendReceiveLocalTest_Http2 : InvokerArraySegmentSendReceiveLocalTest
    {
        public InvokerArraySegmentSendReceiveLocalTest_Http2(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version20;
    }

    [SkipOnPlatform(TestPlatforms.Browser, "Self-signed certificates are not supported on browser")]
    public sealed class InvokerArraySegmentSendReceiveLocalSslTest_Http2 : InvokerArraySegmentSendReceiveLocalSslTest
    {
        public InvokerArraySegmentSendReceiveLocalSslTest_Http2(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version20;
    }

    //HttpClient
    [SkipOnPlatform(TestPlatforms.Browser, "HTTP/2 WebSockets aren't supported on Browser")]
    public sealed class HttpClientArraySegmentSendReceiveLocalTest_Http2 : HttpClientArraySegmentSendReceiveLocalTest
    {
        public HttpClientArraySegmentSendReceiveLocalTest_Http2(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version20;
    }

    [SkipOnPlatform(TestPlatforms.Browser, "Self-signed certificates are not supported on browser")]
    public sealed class HttpClientArraySegmentSendReceiveLocalSslTest_Http2 : HttpClientArraySegmentSendReceiveLocalSslTest
    {
        public HttpClientArraySegmentSendReceiveLocalSslTest_Http2(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version20;
    }
}
