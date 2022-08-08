// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    [Collection(nameof(DisableParallelization))] // Reduces chance of timing-related issues
    [ConditionalClass(typeof(SocketsHttpHandler), nameof(SocketsHttpHandler.IsSupported))]
    public class SocketsHttpHandler_Cancellation_Test_NonParallel : HttpClientHandlerTestBase
    {
        public SocketsHttpHandler_Cancellation_Test_NonParallel(ITestOutputHelper output) : base(output)
        {
        }

        [OuterLoop("Incurs significant delay.")]
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData("1.1", 10_000, 1_000, 100)]
        [InlineData("2.0", 10_000, 1_000, 100)]
        [InlineData("1.1", 20_000, 10_000, null)]
        [InlineData("2.0", 20_000, 10_000, null)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/72977", TestPlatforms.Linux)]
        public static void CancelPendingRequest_DropsStalledConnectionAttempt(string versionString, int firstConnectionDelayMs, int requestTimeoutMs, int? pendingConnectionTimeoutOnRequestCompletion)
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            if (pendingConnectionTimeoutOnRequestCompletion is not null)
            {
                options.StartInfo.EnvironmentVariables["DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_PENDINGCONNECTIONTIMEOUTONREQUESTCOMPLETION"] = pendingConnectionTimeoutOnRequestCompletion.ToString();
            }

            RemoteExecutor.Invoke(CancelPendingRequest_DropsStalledConnectionAttempt_Impl, versionString, firstConnectionDelayMs.ToString(), requestTimeoutMs.ToString(), options).Dispose();
        }

        private static async Task CancelPendingRequest_DropsStalledConnectionAttempt_Impl(string versionString, string firstConnectionDelayMsString, string requestTimeoutMsString)
        {
            var version = Version.Parse(versionString);
            LoopbackServerFactory factory = GetFactoryForVersion(version);

            const int AttemptCount = 3;
            int firstConnectionDelayMs = int.Parse(firstConnectionDelayMsString);
            int requestTimeoutMs = int.Parse(requestTimeoutMsString);
            bool firstConnection = true;

            using CancellationTokenSource cts0 = new CancellationTokenSource(requestTimeoutMs);

            await factory.CreateClientAndServerAsync(async uri =>
            {
                using var handler = CreateHttpClientHandler(version);
                GetUnderlyingSocketsHttpHandler(handler).ConnectCallback = DoConnect;
                using var client = new HttpClient(handler) { DefaultRequestVersion = version };

                await Assert.ThrowsAnyAsync<TaskCanceledException>(async () =>
                {
                    await client.GetAsync(uri, cts0.Token);
                });

                for (int i = 0; i < AttemptCount; i++)
                {
                    using var cts1 = new CancellationTokenSource(requestTimeoutMs);
                    using var response = await client.GetAsync(uri, cts1.Token);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }, async server =>
            {
                await server.AcceptConnectionAsync(async connection =>
                {
                    for (int i = 0; i < AttemptCount; i++)
                    {
                        await connection.ReadRequestDataAsync();
                        await connection.SendResponseAsync();
                        connection.CompleteRequestProcessing();
                    }
                });
            });

            async ValueTask<Stream> DoConnect(SocketsHttpConnectionContext ctx, CancellationToken cancellationToken)
            {
                if (firstConnection)
                {
                    firstConnection = false;
                    await Task.Delay(100, cancellationToken); // Wait for the request to be pushed to the queue
                    cts0.Cancel(); // cancel the first request faster than RequestTimeoutMs
                    await Task.Delay(firstConnectionDelayMs, cancellationToken); // Simulate stalled connection
                }
                var s = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                await s.ConnectAsync(ctx.DnsEndPoint, cancellationToken);

                return new NetworkStream(s, ownsSocket: true);
            }
        }

        [OuterLoop("Incurs significant delay.")]
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(20_000)]
        [InlineData(Timeout.Infinite)]
        public void PendingConnectionTimeout_HighValue_PendingConnectionIsNotCancelled(int timeout)
        {
            RemoteExecutor.Invoke(async timoutStr =>
            {
                // Setup "infinite" timeout of int.MaxValue milliseconds
                AppContext.SetData("System.Net.SocketsHttpHandler.PendingConnectionTimeoutOnRequestCompletion", int.Parse(timoutStr));

                bool connected = false;
                CancellationTokenSource cts = new CancellationTokenSource();

                await new Http11LoopbackServerFactory().CreateClientAndServerAsync(async uri =>
                {
                    using var handler = CreateHttpClientHandler(HttpVersion.Version11);
                    GetUnderlyingSocketsHttpHandler(handler).ConnectCallback = DoConnect;
                    using var client = new HttpClient(handler) { DefaultRequestVersion = HttpVersion.Version11 };

                    await Assert.ThrowsAnyAsync<TaskCanceledException>(() => client.GetAsync(uri, cts.Token));
                },
                async server => {
                    await server.AcceptConnectionAsync(_ => Task.CompletedTask).WaitAsync(30_000);
                });

                async ValueTask<Stream> DoConnect(SocketsHttpConnectionContext ctx, CancellationToken cancellationToken)
                {
                    var s = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                    await Task.Delay(100, cancellationToken); // Wait for the request to be pushed to the queue
                    cts.Cancel();

                    await Task.Delay(10_000, cancellationToken);
                    await s.ConnectAsync(ctx.DnsEndPoint, cancellationToken);
                    connected = true;
                    return new NetworkStream(s, ownsSocket: true);
                }

                Assert.True(connected);
            }, timeout.ToString()).Dispose();
        }
    }
}
