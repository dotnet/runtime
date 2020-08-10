// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using System.Net.Test.Common;

namespace System.Net.Http.Functional.Tests
{
    public abstract class HttpClientHandler_AltSvc_Test : HttpClientHandlerTestBase
    {
        public HttpClientHandler_AltSvc_Test(ITestOutputHelper output) : base(output)
        {
        }

        /// <summary>
        /// HTTP/3 tests by default use prenegotiated HTTP/3. To test Alt-Svc upgrades, that must be disabled.
        /// </summary>
        protected override HttpClient CreateHttpClient()
        {
            HttpClientHandler handler = CreateHttpClientHandler(HttpVersion30);

            return CreateHttpClient(handler);
        }

        [Theory]
        [MemberData(nameof(AltSvcHeaderUpgradeVersions))]
        public async Task AltSvc_Header_Upgrade_Success(Version fromVersion)
        {
            // The test makes a request to a HTTP/1 or HTTP/2 server first, which supplies an Alt-Svc header pointing to the second server.
            using GenericLoopbackServer firstServer =
                fromVersion.Major switch
                {
                    1 => new LoopbackServer(new LoopbackServer.Options { UseSsl = true }),
                    2 => Http2LoopbackServer.CreateServer(),
                    _ => throw new Exception("Unknown HTTP version.")
                };

            // The second request is expected to come in on this HTTP/3 server.
            using var secondServer = new Http3LoopbackServer();

            using HttpClient client = CreateHttpClient();

            Task<HttpResponseMessage> firstResponseTask = client.GetAsync(firstServer.Address);
            Task serverTask = firstServer.AcceptConnectionSendResponseAndCloseAsync(additionalHeaders: new[]
            {
                new HttpHeaderData("Alt-Svc", $"h3=\"{secondServer.Address.IdnHost}:{secondServer.Address.Port}\"")
            });

            await new[] { firstResponseTask, serverTask }.WhenAllOrAnyFailed(30_000);

            using HttpResponseMessage firstResponse = firstResponseTask.Result;
            Assert.True(firstResponse.IsSuccessStatusCode);

            await AltSvc_Upgrade_Success(firstServer, secondServer, client);
        }

        public static TheoryData<Version> AltSvcHeaderUpgradeVersions =>
            new TheoryData<Version>
            {
                { HttpVersion.Version11 },
                { HttpVersion.Version20 }
            };

        [Fact]
        public async Task AltSvc_ConnectionFrame_UpgradeFrom20_Success()
        {
            using Http2LoopbackServer firstServer = Http2LoopbackServer.CreateServer();
            using Http3LoopbackServer secondServer = new Http3LoopbackServer();
            using HttpClient client = CreateHttpClient();

            Task<HttpResponseMessage> firstResponseTask = client.GetAsync(firstServer.Address);
            Task serverTask = Task.Run(async () =>
            {
                using Http2LoopbackConnection connection = await firstServer.EstablishConnectionAsync();

                int streamId = await connection.ReadRequestHeaderAsync();
                await connection.WriteFrameAsync(new AltSvcFrame($"https://{firstServer.Address.IdnHost}:{firstServer.Address.Port}", $"h3=\"{secondServer.Address.IdnHost}:{secondServer.Address.Port}\"", streamId: 0));
                await connection.SendDefaultResponseAsync(streamId);
            });

            await new[] { firstResponseTask, serverTask }.WhenAllOrAnyFailed(30_000);

            HttpResponseMessage firstResponse = firstResponseTask.Result;
            Assert.True(firstResponse.IsSuccessStatusCode);

            await AltSvc_Upgrade_Success(firstServer, secondServer, client);
        }

        [Fact]
        public async Task AltSvc_ResponseFrame_UpgradeFrom20_Success()
        {
            using Http2LoopbackServer firstServer = Http2LoopbackServer.CreateServer();
            using Http3LoopbackServer secondServer = new Http3LoopbackServer();
            using HttpClient client = CreateHttpClient();

            Task<HttpResponseMessage> firstResponseTask = client.GetAsync(firstServer.Address);
            Task serverTask = Task.Run(async () =>
            {
                using Http2LoopbackConnection connection = await firstServer.EstablishConnectionAsync();

                int streamId = await connection.ReadRequestHeaderAsync();
                await connection.SendDefaultResponseHeadersAsync(streamId);
                await connection.WriteFrameAsync(new AltSvcFrame("", $"h3=\"{secondServer.Address.IdnHost}:{secondServer.Address.Port}\"", streamId));
                await connection.SendResponseDataAsync(streamId, Array.Empty<byte>(), true);
            });

            await new[] { firstResponseTask, serverTask }.WhenAllOrAnyFailed(30_000);

            HttpResponseMessage firstResponse = firstResponseTask.Result;
            Assert.True(firstResponse.IsSuccessStatusCode);

            await AltSvc_Upgrade_Success(firstServer, secondServer, client);
        }

        private async Task AltSvc_Upgrade_Success(GenericLoopbackServer firstServer, Http3LoopbackServer secondServer, HttpClient client)
        {
            Task<HttpResponseMessage> secondResponseTask = client.GetAsync(firstServer.Address);
            Task<HttpRequestData> secondRequestTask = secondServer.AcceptConnectionSendResponseAndCloseAsync();

            await new[] { (Task)secondResponseTask, secondRequestTask }.WhenAllOrAnyFailed(30_000);

            HttpRequestData secondRequest = secondRequestTask.Result;
            using HttpResponseMessage secondResponse = secondResponseTask.Result;

            string altUsed = secondRequest.GetSingleHeaderValue("Alt-Used");
            Assert.Equal($"{secondServer.Address.IdnHost}:{secondServer.Address.Port}", altUsed);
            Assert.True(secondResponse.IsSuccessStatusCode);
        }
    }
}
