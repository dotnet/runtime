// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            HttpClientHandler handler = CreateHttpClientHandler(HttpVersion.Version30);
            SetUsePrenegotiatedHttp3(handler, usePrenegotiatedHttp3: false);

            return CreateHttpClient(handler);
        }

        [Fact]
        public async Task AltSvc_Header_UpgradeFrom11_Success()
        {
            await AltSvc_Header_Upgrade_Success(HttpVersion.Version11).ConfigureAwait(false);
        }

        [Fact]
        public async Task AltSvc_Header_UpgradeFrom20_Success()
        {
            await AltSvc_Header_Upgrade_Success(HttpVersion.Version20).ConfigureAwait(false);
        }

        private async Task AltSvc_Header_Upgrade_Success(Version fromVersion)
        {
            using GenericLoopbackServer firstServer = GetFactoryForVersion(fromVersion).CreateServer();
            using Http3LoopbackServer secondServer = new Http3LoopbackServer();
            using HttpClient client = CreateHttpClient();

            Task<HttpResponseMessage> firstResponseTask = client.GetAsync(firstServer.Address);

            await firstServer.AcceptConnectionSendResponseAndCloseAsync(additionalHeaders: new[]
            {
                new HttpHeaderData("Alt-Svc", $"h3={secondServer.Address.IdnHost}:{secondServer.Address.Port}")
            });

            HttpResponseMessage firstResponse = await firstResponseTask;
            Assert.True(firstResponse.IsSuccessStatusCode);

            await AltSvc_Upgrade_Success(firstServer, secondServer, client);
        }

        [Fact]
        public async Task AltSvc_ConnectionFrame_UpgradeFrom20_Success()
        {
            using Http2LoopbackServer firstServer = Http2LoopbackServer.CreateServer();
            using Http3LoopbackServer secondServer = new Http3LoopbackServer();
            using HttpClient client = CreateHttpClient();

            Task<HttpResponseMessage> firstResponseTask = client.GetAsync(firstServer.Address);

            using (Http2LoopbackConnection connection = await firstServer.EstablishConnectionAsync())
            {
                int streamId = await connection.ReadRequestHeaderAsync();
                await connection.SendDefaultResponseAsync(streamId);
                await connection.WriteFrameAsync(new AltSvcFrame($"https://{firstServer.Address.IdnHost}:{firstServer.Address.Port}", $"h3={secondServer.Address.IdnHost}:{secondServer.Address.Port}", 0));
            }

            HttpResponseMessage firstResponse = await firstResponseTask;
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

            using (Http2LoopbackConnection connection = await firstServer.EstablishConnectionAsync())
            {
                int streamId = await connection.ReadRequestHeaderAsync();
                await connection.SendDefaultResponseHeadersAsync(streamId);
                await connection.WriteFrameAsync(new AltSvcFrame("", $"h3={secondServer.Address.IdnHost}:{secondServer.Address.Port}", streamId));
                await connection.SendResponseDataAsync(streamId, Array.Empty<byte>(), true);
            }

            HttpResponseMessage firstResponse = await firstResponseTask;
            Assert.True(firstResponse.IsSuccessStatusCode);

            await AltSvc_Upgrade_Success(firstServer, secondServer, client);
        }

        private async Task AltSvc_Upgrade_Success(GenericLoopbackServer firstServer, Http3LoopbackServer secondServer, HttpClient client)
        {
            Task<HttpResponseMessage> secondResponseTask = client.GetAsync(firstServer.Address);

            HttpRequestData secondRequest = await secondServer.AcceptConnectionSendResponseAndCloseAsync();
            string altUsed = secondRequest.GetSingleHeaderValue("Alt-Used");
            Assert.Equal($"{secondServer.Address.IdnHost}:{secondServer.Address.Port}", altUsed);

            HttpResponseMessage secondResponse = await secondResponseTask;
            Assert.True(secondResponse.IsSuccessStatusCode);
        }
    }
}
