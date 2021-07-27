// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using System.Net.Test.Common;
using System.Net.Quic;

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
        private HttpClient CreateHttpClient(Version version)
        {
            var client = CreateHttpClient();
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
            client.DefaultRequestVersion = version;
            return client;
        }

        [Theory]
        [MemberData(nameof(AltSvcHeaderUpgradeVersions))]
        public async Task AltSvc_Header_Upgrade_Success(Version fromVersion, bool overrideHost)
        {
            // [ActiveIssue("https://github.com/dotnet/runtime/issues/54050")]
            if (UseQuicImplementationProvider == QuicImplementationProviders.Mock)
            {
                return;
            }

            // The test makes a request to a HTTP/1 or HTTP/2 server first, which supplies an Alt-Svc header pointing to the second server.
            using GenericLoopbackServer firstServer =
                fromVersion.Major switch
                {
                    1 => Http11LoopbackServerFactory.Singleton.CreateServer(new LoopbackServer.Options { UseSsl = true }),
                    2 => Http2LoopbackServer.CreateServer(),
                    _ => throw new Exception("Unknown HTTP version.")
                };

            // The second request is expected to come in on this HTTP/3 server.
            using Http3LoopbackServer secondServer = CreateHttp3LoopbackServer();
            
            if (!overrideHost)
                Assert.Equal(firstServer.Address.IdnHost, secondServer.Address.IdnHost);

            using HttpClient client = CreateHttpClient(fromVersion);

            Task<HttpResponseMessage> firstResponseTask = client.GetAsync(firstServer.Address);
            Task serverTask = firstServer.AcceptConnectionSendResponseAndCloseAsync(additionalHeaders: new[]
            {
                new HttpHeaderData("Alt-Svc", $"h3=\"{(overrideHost ? secondServer.Address.IdnHost : null)}:{secondServer.Address.Port}\"")
            });

            await new[] { firstResponseTask, serverTask }.WhenAllOrAnyFailed(30_000);

            using HttpResponseMessage firstResponse = firstResponseTask.Result;
            Assert.True(firstResponse.IsSuccessStatusCode);

            await AltSvc_Upgrade_Success(firstServer, secondServer, client);
        }

        public static TheoryData<Version, bool> AltSvcHeaderUpgradeVersions =>
            new TheoryData<Version, bool>
            {
                { HttpVersion.Version11, true },
                { HttpVersion.Version11, false },
                { HttpVersion.Version20, true },
                { HttpVersion.Version20, false }
            };

        [Fact]
        public async Task AltSvc_ConnectionFrame_UpgradeFrom20_Success()
        {
            // [ActiveIssue("https://github.com/dotnet/runtime/issues/54050")]
            if (UseQuicImplementationProvider == QuicImplementationProviders.Mock)
            {
                return;
            }

            using Http2LoopbackServer firstServer = Http2LoopbackServer.CreateServer();
            using Http3LoopbackServer secondServer = CreateHttp3LoopbackServer();
            using HttpClient client = CreateHttpClient(HttpVersion.Version20);

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
            // [ActiveIssue("https://github.com/dotnet/runtime/issues/54050")]
            if (UseQuicImplementationProvider == QuicImplementationProviders.Mock)
            {
                return;
            }

            using Http2LoopbackServer firstServer = Http2LoopbackServer.CreateServer();
            using Http3LoopbackServer secondServer = CreateHttp3LoopbackServer();
            using HttpClient client = CreateHttpClient(HttpVersion.Version20);

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
