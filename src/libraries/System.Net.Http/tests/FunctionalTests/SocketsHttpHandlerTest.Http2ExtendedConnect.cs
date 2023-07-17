// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Net.Test.Common;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    [ConditionalClass(typeof(SocketsHttpHandler), nameof(SocketsHttpHandler.IsSupported))]
    public sealed class SocketsHttpHandler_Http2ExtendedConnect_Test : HttpClientHandlerTestBase
    {
        public SocketsHttpHandler_Http2ExtendedConnect_Test(ITestOutputHelper output) : base(output) { }

        protected override Version UseVersion => HttpVersion.Version20;

        public static IEnumerable<object[]> UseSsl_MemberData()
        {
            yield return new object[] { false };

            if (PlatformDetection.SupportsAlpn)
            {
                yield return new object[] { true };
            }
        }

        [Theory]
        [MemberData(nameof(UseSsl_MemberData))]
        public async Task Connect_ReadWriteResponseStream(bool useSsl)
        {
            byte[] clientMessage = new byte[] { 1, 2, 3 };
            byte[] serverMessage = new byte[] { 4, 5, 6, 7 };

            TaskCompletionSource clientCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);

            await Http2LoopbackServerFactory.Singleton.CreateClientAndServerAsync(async uri =>
            {
                using HttpClient client = CreateHttpClient();

                HttpRequestMessage request = CreateRequest(HttpMethod.Connect, uri, UseVersion, exactVersion: true);
                request.Headers.Protocol = "foo";

                using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                using Stream responseStream = await response.Content.ReadAsStreamAsync();

                await responseStream.WriteAsync(clientMessage);
                await responseStream.FlushAsync();

                byte[] readBuffer = new byte[serverMessage.Length];
                await responseStream.ReadExactlyAsync(readBuffer);
                Assert.Equal(serverMessage, readBuffer);

                // Receive server's EOS
                Assert.Equal(0, await responseStream.ReadAsync(readBuffer));

                clientCompleted.SetResult();
            },
            async server =>
            {
                await using Http2LoopbackConnection connection = await ((Http2LoopbackServer)server).EstablishConnectionAsync(new SettingsEntry { SettingId = SettingId.EnableConnect, Value = 1 });

                (int streamId, _) = await connection.ReadAndParseRequestHeaderAsync(readBody: false);

                await connection.SendResponseHeadersAsync(streamId, endStream: false).ConfigureAwait(false);

                DataFrame dataFrame = await connection.ReadDataFrameAsync();
                Assert.Equal(clientMessage, dataFrame.Data.ToArray());

                await connection.SendResponseDataAsync(streamId, serverMessage, endStream: true);

                await clientCompleted.Task.WaitAsync(TestHelper.PassingTestTimeout);
            }, options: new GenericLoopbackOptions { UseSsl = useSsl });
        }

        [Theory]
        [MemberData(nameof(UseSsl_MemberData))]
        public async Task Connect_ServerDoesNotSupportExtendedConnect_ClientIncludesExceptionData(bool useSsl)
        {
            TaskCompletionSource clientCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);

            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                using HttpClient client = CreateHttpClient();

                HttpRequestMessage request = CreateRequest(HttpMethod.Connect, uri, UseVersion, exactVersion: true);
                request.Headers.Protocol = "foo";

                HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(request));
                Assert.Equal(HttpRequestError.ExtendedConnectNotSupported, ex.HttpRequestError);

                clientCompleted.SetResult();
            },
            async server =>
            {
                try
                {
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        await clientCompleted.Task.WaitAsync(TestHelper.PassingTestTimeout);
                    });
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"Ignoring exception {ex}");
                }
            }, options: new GenericLoopbackOptions { UseSsl = useSsl });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Connect_Http11Endpoint_Throws(bool useSsl)
        {
            using var server = new LoopbackServer(new LoopbackServer.Options
            {
                UseSsl = useSsl
            });

            await server.ListenAsync();

            TaskCompletionSource clientCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);

            Task serverTask = Task.Run(async () =>
            {
                try
                {
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        if (!useSsl)
                        {
                            byte[] http2GoAwayHttp11RequiredBytes = new byte[17] { 0, 0, 8, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 13 };

                            await connection.SendResponseAsync(http2GoAwayHttp11RequiredBytes);

                            await clientCompleted.Task.WaitAsync(TestHelper.PassingTestTimeout);
                        }
                    });
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"Ignoring exception {ex}");
                }
            });

            Task clientTask = Task.Run(async () =>
            {
                using HttpClient client = CreateHttpClient();

                HttpRequestMessage request = CreateRequest(HttpMethod.Connect, server.Address, UseVersion, exactVersion: true);
                request.Headers.Protocol = "foo";

                HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(request));
                clientCompleted.SetResult();

                if (useSsl)
                {
                    Assert.Equal(HttpRequestError.VersionNegotiationError, ex.HttpRequestError);
                }
            });

            await new[] { serverTask, clientTask }.WhenAllOrAnyFailed().WaitAsync(TestHelper.PassingTestTimeout);
        }
    }
}
