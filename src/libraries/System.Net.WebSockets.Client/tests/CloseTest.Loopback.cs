// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{

    [ConditionalClass(typeof(ClientWebSocketTestBase), nameof(WebSocketsSupported))]
    [SkipOnPlatform(TestPlatforms.Browser, "System.Net.Sockets are not supported on browser")]
    public abstract class CloseTest_LoopbackBase(ITestOutputHelper output) : CloseTestBase(output)
    {
        #region Common (Echo Server) tests

        [Theory, MemberData(nameof(UseSslAndBoolean))]
        public Task CloseAsync_ServerInitiatedClose_Success(bool useSsl, bool useCloseOutputAsync) => RunEchoAsync(
            server => RunClient_CloseAsync_ServerInitiatedClose_Success(server, useCloseOutputAsync), useSsl);

        [Theory, MemberData(nameof(UseSsl))]
        public Task CloseAsync_ClientInitiatedClose_Success(bool useSsl) => RunEchoAsync(
            RunClient_CloseAsync_ClientInitiatedClose_Success, useSsl);

        [Theory, MemberData(nameof(UseSsl))]
        public Task CloseAsync_CloseDescriptionIsMaxLength_Success(bool useSsl) => RunEchoAsync(
            RunClient_CloseAsync_CloseDescriptionIsMaxLength_Success, useSsl);

        [Theory, MemberData(nameof(UseSsl))]
        public Task CloseAsync_CloseDescriptionIsMaxLengthPlusOne_ThrowsArgumentException(bool useSsl) => RunEchoAsync(
            RunClient_CloseAsync_CloseDescriptionIsMaxLengthPlusOne_ThrowsArgumentException, useSsl);

        [Theory, MemberData(nameof(UseSsl))]
        public Task CloseAsync_CloseDescriptionHasUnicode_Success(bool useSsl) => RunEchoAsync(
            RunClient_CloseAsync_CloseDescriptionHasUnicode_Success, useSsl);

        [Theory, MemberData(nameof(UseSsl))]
        public Task CloseAsync_CloseDescriptionIsNull_Success(bool useSsl) => RunEchoAsync(
            RunClient_CloseAsync_CloseDescriptionIsNull_Success, useSsl);

        [Theory, MemberData(nameof(UseSsl))]
        public Task CloseOutputAsync_ExpectedStates(bool useSsl) => RunEchoAsync(
            RunClient_CloseOutputAsync_ExpectedStates, useSsl);

        [Theory, MemberData(nameof(UseSsl))]
        public Task CloseAsync_CloseOutputAsync_Throws(bool useSsl) => RunEchoAsync(
            RunClient_CloseAsync_CloseOutputAsync_Throws, useSsl);

        [OuterLoop("Uses Task.Delay")]
        [Theory, MemberData(nameof(UseSsl))]
        public Task CloseOutputAsync_ClientInitiated_CanReceive_CanClose(bool useSsl) => RunEchoAsync(
            RunClient_CloseOutputAsync_ClientInitiated_CanReceive_CanClose, useSsl);

        [Theory, MemberData(nameof(UseSsl))]
        public Task CloseOutputAsync_ServerInitiated_CanReceive(bool useSsl) => RunEchoAsync(
            server => RunClient_CloseOutputAsync_ServerInitiated_CanReceive(server, delayReceiving: false), useSsl);

        [OuterLoop("Uses Task.Delay")]
        [Theory, MemberData(nameof(UseSsl))]
        public Task CloseOutputAsync_ServerInitiated_DelayReceiving_CanReceive(bool useSsl) => RunEchoAsync(
            server => RunClient_CloseOutputAsync_ServerInitiated_CanReceive(server, delayReceiving: true), useSsl);

        [Theory, MemberData(nameof(UseSsl))]
        public Task CloseOutputAsync_ServerInitiated_CanSend(bool useSsl) => RunEchoAsync(
            RunClient_CloseOutputAsync_ServerInitiated_CanSend, useSsl);

        [OuterLoop("Uses Task.Delay")]
        [Theory, MemberData(nameof(UseSslAndBoolean))]
        public Task CloseOutputAsync_ServerInitiated_CanReceiveAfterClose(bool useSsl, bool syncState) => RunEchoAsync(
            server => RunClient_CloseOutputAsync_ServerInitiated_CanReceiveAfterClose(server, syncState), useSsl);

        [Theory, MemberData(nameof(UseSsl))]
        public Task CloseOutputAsync_CloseDescriptionIsNull_Success(bool useSsl) => RunEchoAsync(
            RunClient_CloseOutputAsync_CloseDescriptionIsNull_Success, useSsl);

        [ActiveIssue("https://github.com/dotnet/runtime/issues/22000", TargetFrameworkMonikers.Netcoreapp)]
        [Theory, MemberData(nameof(UseSsl))]
        public Task CloseOutputAsync_DuringConcurrentReceiveAsync_ExpectedStates(bool useSsl) => RunEchoAsync(
            RunClient_CloseOutputAsync_DuringConcurrentReceiveAsync_ExpectedStates, useSsl);

        [Theory, MemberData(nameof(UseSsl))]
        public Task CloseAsync_DuringConcurrentReceiveAsync_ExpectedStates(bool useSsl) => RunEchoAsync(
            RunClient_CloseAsync_DuringConcurrentReceiveAsync_ExpectedStates, useSsl);

        #endregion
    }

    public abstract class CloseTest_Loopback(ITestOutputHelper output) : CloseTest_LoopbackBase(output)
    {
        #region HTTP/1.1-only loopback tests

        [Fact]
        public async Task CloseAsync_CancelableEvenWhenPendingReceive_Throws()
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                try
                {
                    using (var cws = new ClientWebSocket())
                    using (var testTimeoutCts = new CancellationTokenSource(TimeOutMilliseconds))
                    {
                        await ConnectAsync(cws, uri, testTimeoutCts.Token);

                        Task receiveTask = cws.ReceiveAsync(new byte[1], testTimeoutCts.Token);

                        var cancelCloseCts = new CancellationTokenSource();
                        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                        {
                            Task t = cws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cancelCloseCts.Token);
                            cancelCloseCts.Cancel();
                            await t;
                        });

                        Assert.True(cancelCloseCts.Token.IsCancellationRequested);
                        Assert.False(testTimeoutCts.Token.IsCancellationRequested);

                        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => receiveTask);

                        Assert.False(testTimeoutCts.Token.IsCancellationRequested);
                    }
                }
                finally
                {
                    tcs.SetResult();
                }
            }, server => server.AcceptConnectionAsync(async connection =>
            {
                Dictionary<string, string> headers = await LoopbackHelper.WebSocketHandshakeAsync(connection);
                Assert.NotNull(headers);

                await tcs.Task;

            }), new LoopbackServer.Options { WebSocketEndpoint = true });
        }

        // Regression test for https://github.com/dotnet/runtime/issues/80116.
        [OuterLoop("Uses Task.Delay")]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task CloseHandshake_ExceptionsAreObserved()
        {
            await RemoteExecutor.Invoke(static (typeName) =>
            {
                ClientWebSocketTestBase test = (ClientWebSocketTestBase)Activator.CreateInstance(typeof(ClientWebSocketTestBase).Assembly.GetType(typeName), new object[] { null });
                using CancellationTokenSource timeoutCts = new CancellationTokenSource(TimeOutMilliseconds);

                Exception unobserved = null;
                TaskScheduler.UnobservedTaskException += (obj, args) =>
                {
                    unobserved = args.Exception;
                };

                TaskCompletionSource clientCompleted = new TaskCompletionSource();

                return LoopbackWebSocketServer.RunAsync(async uri =>
                {
                    var ct = timeoutCts.Token;
                    using var clientWs = await test.GetConnectedWebSocket(uri);
                    await clientWs.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", ct);
                    await clientWs.ReceiveAsync(new byte[16], ct);
                    await Task.Delay(1500);
                    GC.Collect(2);
                    GC.WaitForPendingFinalizers();
                    clientCompleted.SetResult();
                    Assert.Null(unobserved);
                },
                async (serverWs, ct) =>
                {
                    await serverWs.ReceiveAsync(new byte[16], ct);
                    await serverWs.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", ct);
                    await clientCompleted.Task;
                }, new LoopbackWebSocketServer.Options(Net.HttpVersion.Version11, UseSsl: PlatformDetection.SupportsAlpn), timeoutCts.Token);
            }, GetType().FullName).DisposeAsync();
        }

        #endregion
    }

    public abstract class CloseTest_Http2Loopback(ITestOutputHelper output) : CloseTest_LoopbackBase(output)
    {
        internal override Version HttpVersion => Net.HttpVersion.Version20;
    }

    #region Runnable test classes: HTTP/1.1 Loopback

    public sealed class CloseTest_SharedHandler_Loopback(ITestOutputHelper output) : CloseTest_Loopback(output) { }

    public sealed class CloseTest_Invoker_Loopback(ITestOutputHelper output) : CloseTest_Loopback(output)
    {
        protected override bool UseCustomInvoker => true;
    }

    public sealed class CloseTest_HttpClient_Loopback(ITestOutputHelper output) : CloseTest_Loopback(output)
    {
        protected override bool UseHttpClient => true;
    }

    #endregion

    #region Runnable test classes: HTTP/2 Loopback

    public sealed class CloseTest_Invoker_Http2Loopback(ITestOutputHelper output) : CloseTest_Http2Loopback(output)
    {
        protected override bool UseCustomInvoker => true;
    }

    public sealed class CloseTest_HttpClient_Http2Loopback(ITestOutputHelper output) : CloseTest_Http2Loopback(output)
    {
        protected override bool UseHttpClient => true;
    }
    #endregion
}
