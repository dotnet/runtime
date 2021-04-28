// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Quic;
using System.Net.Security;
using System.Net.Test.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public abstract class HttpClientHandlerTest_Http3 : HttpClientHandlerTestBase
    {
        protected override Version UseVersion => HttpVersion.Version30;

        public HttpClientHandlerTest_Http3(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [InlineData(10)] // 2 bytes settings value.
        [InlineData(100)] // 4 bytes settings value.
        [InlineData(10_000_000)] // 8 bytes settings value.
        public async Task ClientSettingsReceived_Success(int headerSizeLimit)
        {
            using Http3LoopbackServer server = CreateHttp3LoopbackServer();

            Task serverTask = Task.Run(async () =>
            {
                using Http3LoopbackConnection connection = (Http3LoopbackConnection)await server.EstablishGenericConnectionAsync();

                (Http3LoopbackStream settingsStream, Http3LoopbackStream requestStream) = await connection.AcceptControlAndRequestStreamAsync();

                using (settingsStream)
                using (requestStream)
                {
                    Assert.False(settingsStream.CanWrite, "Expected unidirectional control stream.");

                    long? streamType = await settingsStream.ReadIntegerAsync();
                    Assert.Equal(Http3LoopbackStream.ControlStream, streamType);

                    List<(long settingId, long settingValue)> settings = await settingsStream.ReadSettingsAsync();
                    (long settingId, long settingValue) = Assert.Single(settings);

                    Assert.Equal(Http3LoopbackStream.MaxHeaderListSize, settingId);
                    Assert.Equal(headerSizeLimit * 1024L, settingValue);

                    await requestStream.ReadRequestDataAsync();
                    await requestStream.SendResponseAsync();
                }
            });

            Task clientTask = Task.Run(async () =>
            {
                using HttpClientHandler handler = CreateHttpClientHandler();
                handler.MaxResponseHeadersLength = headerSizeLimit;

                using HttpClient client = CreateHttpClient(handler);
                using HttpRequestMessage request = new()
                {
                    Method = HttpMethod.Get,
                    RequestUri = server.Address,
                    Version = HttpVersion30,
                    VersionPolicy = HttpVersionPolicy.RequestVersionExact
                };
                using HttpResponseMessage response = await client.SendAsync(request);
            });

            await new[] { clientTask, serverTask }.WhenAllOrAnyFailed(20_000);
        }

        [Theory]
        [InlineData(100)]
        public async Task SendMoreThanStreamLimitRequests_Succeeds(int streamLimit)
        {
            using Http3LoopbackServer server = CreateHttp3LoopbackServer();

            Task serverTask = Task.Run(async () =>
            {
                using Http3LoopbackConnection connection = (Http3LoopbackConnection)await server.EstablishGenericConnectionAsync();
                for (int i = 0; i < streamLimit + 1; ++i)
                {
                    using Http3LoopbackStream stream = await connection.AcceptRequestStreamAsync();
                    await stream.HandleRequestAsync();
                }
            });

            Task clientTask = Task.Run(async () =>
            {
                using HttpClient client = CreateHttpClient();

                for (int i = 0; i < streamLimit + 1; ++i)
                {
                    using HttpRequestMessage request = new()
                    {
                        Method = HttpMethod.Get,
                        RequestUri = server.Address,
                        Version = HttpVersion30,
                        VersionPolicy = HttpVersionPolicy.RequestVersionExact
                    };
                    using var response = await client.SendAsync(request).WaitAsync(TimeSpan.FromSeconds(10));
                }
            });

            await new[] { clientTask, serverTask }.WhenAllOrAnyFailed(20_000);
        }


        [Fact]
        public async Task ReservedFrameType_Throws()
        {
            const int ReservedHttp2PriorityFrameId = 0x2;
            const long UnexpectedFrameErrorCode = 0x105;

            using Http3LoopbackServer server = CreateHttp3LoopbackServer();

            Task serverTask = Task.Run(async () =>
            {
                using Http3LoopbackConnection connection = (Http3LoopbackConnection)await server.EstablishGenericConnectionAsync();
                using Http3LoopbackStream stream = await connection.AcceptRequestStreamAsync();

                await stream.SendFrameAsync(ReservedHttp2PriorityFrameId, new byte[8]);

                QuicConnectionAbortedException ex = await Assert.ThrowsAsync<QuicConnectionAbortedException>(async () =>
                {
                    await stream.HandleRequestAsync();
                    using Http3LoopbackStream stream2 = await connection.AcceptRequestStreamAsync();
                });

                Assert.Equal(UnexpectedFrameErrorCode, ex.ErrorCode);
            });

            Task clientTask = Task.Run(async () =>
            {
                using HttpClient client = CreateHttpClient();
                using HttpRequestMessage request = new()
                {
                    Method = HttpMethod.Get,
                    RequestUri = server.Address,
                    Version = HttpVersion30,
                    VersionPolicy = HttpVersionPolicy.RequestVersionExact
                };

                await Assert.ThrowsAsync<HttpRequestException>(async () => await client.SendAsync(request));
            });

            await new[] { clientTask, serverTask }.WhenAllOrAnyFailed(20_000);
        }

        [OuterLoop]
        [ConditionalTheory(nameof(IsMsQuicSupported))]
        [MemberData(nameof(InteropUris))]
        public async Task Public_Interop_ExactVersion_Success(string uri)
        {
            using HttpClient client = CreateHttpClient();
            using HttpRequestMessage request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(uri, UriKind.Absolute),
                Version = HttpVersion.Version30,
                VersionPolicy = HttpVersionPolicy.RequestVersionExact
            };
            using HttpResponseMessage response = await client.SendAsync(request).WaitAsync(TimeSpan.FromSeconds(20));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(3, response.Version.Major);
        }

        [OuterLoop]
        [ConditionalTheory(nameof(IsMsQuicSupported))]
        [MemberData(nameof(InteropUris))]
        public async Task Public_Interop_Upgrade_Success(string uri)
        {
            using HttpClient client = CreateHttpClient();

            // First request uses HTTP/1 or HTTP/2 and receives an Alt-Svc either by header or (with HTTP/2) by frame.

            using (HttpRequestMessage requestA = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(uri, UriKind.Absolute),
                Version = HttpVersion.Version30,
                VersionPolicy = HttpVersionPolicy.RequestVersionOrLower
            })
            {
                using HttpResponseMessage responseA = await client.SendAsync(requestA).WaitAsync(TimeSpan.FromSeconds(20));
                Assert.Equal(HttpStatusCode.OK, responseA.StatusCode);
                Assert.NotEqual(3, responseA.Version.Major);
            }

            // Second request uses HTTP/3.

            using (HttpRequestMessage requestB = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(uri, UriKind.Absolute),
                Version = HttpVersion.Version30,
                VersionPolicy = HttpVersionPolicy.RequestVersionOrLower
            })
            {
                using HttpResponseMessage responseB = await client.SendAsync(requestB).WaitAsync(TimeSpan.FromSeconds(20));

                Assert.Equal(HttpStatusCode.OK, responseB.StatusCode);
                Assert.NotEqual(3, responseB.Version.Major);
            }
        }

        /// <summary>
        /// These are public interop test servers for various QUIC and HTTP/3 implementations,
        /// taken from https://github.com/quicwg/base-drafts/wiki/Implementations
        /// </summary>
        public static TheoryData<string> InteropUris() =>
            new TheoryData<string>
            {
                { "https://quic.rocks:4433/" }, // Chromium
                { "https://http3-test.litespeedtech.com:4433/" }, // LiteSpeed
                { "https://quic.tech:8443/" } // Cloudflare
            };
    }
}
