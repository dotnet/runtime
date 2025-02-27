// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    // --- Loopback Echo Server "overrides" ---

    [ConditionalClass(typeof(ClientWebSocketTestBase), nameof(WebSocketsSupported))]
    [SkipOnPlatform(TestPlatforms.Browser, "System.Net.Sockets are not supported on browser")]
    public abstract class CloseTest_Loopback : CloseTestBase
    {
        public CloseTest_Loopback(ITestOutputHelper output) : base(output) { }

        [Theory, MemberData(nameof(UseSslAndBoolean))]
        public Task CloseAsync_ServerInitiatedClose_Success(bool useSsl, bool useCloseOutputAsync) => RunEchoAsync(
            server => RunClient_CloseAsync_ServerInitiatedClose_Success(server, useCloseOutputAsync), useSsl);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task CloseAsync_ClientInitiatedClose_Success(bool useSsl) => RunEchoAsync(
            RunClient_CloseAsync_ClientInitiatedClose_Success, useSsl);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task CloseAsync_CloseDescriptionIsMaxLength_Success(bool useSsl) => RunEchoAsync(
            RunClient_CloseAsync_CloseDescriptionIsMaxLength_Success, useSsl);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task CloseAsync_CloseDescriptionIsMaxLengthPlusOne_ThrowsArgumentException(bool useSsl) => RunEchoAsync(
            RunClient_CloseAsync_CloseDescriptionIsMaxLengthPlusOne_ThrowsArgumentException, useSsl);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task CloseAsync_CloseDescriptionHasUnicode_Success(bool useSsl) => RunEchoAsync(
            RunClient_CloseAsync_CloseDescriptionHasUnicode_Success, useSsl);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task CloseAsync_CloseDescriptionIsNull_Success(bool useSsl) => RunEchoAsync(
            RunClient_CloseAsync_CloseDescriptionIsNull_Success, useSsl);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task CloseOutputAsync_ExpectedStates(bool useSsl) => RunEchoAsync(
            RunClient_CloseOutputAsync_ExpectedStates, useSsl);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task CloseAsync_CloseOutputAsync_Throws(bool useSsl) => RunEchoAsync(
            RunClient_CloseAsync_CloseOutputAsync_Throws, useSsl);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task CloseOutputAsync_ClientInitiated_CanReceive_CanClose(bool useSsl) => RunEchoAsync(
            RunClient_CloseOutputAsync_ClientInitiated_CanReceive_CanClose, useSsl);

        [Theory, MemberData(nameof(UseSslAndBoolean))]
        public Task CloseOutputAsync_ServerInitiated_CanReceive(bool useSsl, bool delayReceiving) => RunEchoAsync(
            server => RunClient_CloseOutputAsync_ServerInitiated_CanReceive(server, delayReceiving), useSsl);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task CloseOutputAsync_ServerInitiated_CanSend(bool useSsl) => RunEchoAsync(
            RunClient_CloseOutputAsync_ServerInitiated_CanSend, useSsl);

        [Theory, MemberData(nameof(UseSslAndBoolean))]
        public Task CloseOutputAsync_ServerInitiated_CanReceiveAfterClose(bool useSsl, bool syncState) => RunEchoAsync(
            server => RunClient_CloseOutputAsync_ServerInitiated_CanReceiveAfterClose(server, syncState), useSsl);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task CloseOutputAsync_CloseDescriptionIsNull_Success(bool useSsl) => RunEchoAsync(
            RunClient_CloseOutputAsync_CloseDescriptionIsNull_Success, useSsl);

        // TODO
        /*[ActiveIssue("https://github.com/dotnet/runtime/issues/22000", TargetFrameworkMonikers.Netcoreapp)]
        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task CloseOutputAsync_DuringConcurrentReceiveAsync_ExpectedStates(bool useSsl) => RunEchoAsync(
            RunClient_CloseOutputAsync_DuringConcurrentReceiveAsync_ExpectedStates, useSsl);*/

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task CloseAsync_DuringConcurrentReceiveAsync_ExpectedStates(bool useSsl) => RunEchoAsync(
            RunClient_CloseAsync_DuringConcurrentReceiveAsync_ExpectedStates, useSsl);
    }

    // --- HTTP/1.1 WebSocket loopback tests ---

    public sealed class CloseTest_Invoker_Loopback : CloseTest_Loopback
    {
        public CloseTest_Invoker_Loopback(ITestOutputHelper output) : base(output) { }
        protected override bool UseCustomInvoker => true;
    }

    public sealed class CloseTest_HttpClient_Loopback : CloseTest_Loopback
    {
        public CloseTest_HttpClient_Loopback(ITestOutputHelper output) : base(output) { }
        protected override bool UseHttpClient => true;
    }

    // TODO
    public sealed class CloseTest_SharedHandler_Loopback : CloseTestBase //!
    {
        public CloseTest_SharedHandler_Loopback(ITestOutputHelper output) : base(output) { }

        // TODO
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

        [Theory, InlineData(false), InlineData(true)]
        public Task CloseAsync_ServerInitiatedClose_Success(bool useCloseOutputAsync) => RunEchoAsync(
            server => RunClient_CloseAsync_ServerInitiatedClose_Success(server, useCloseOutputAsync), useSsl: false);

        [Fact]
        public Task CloseAsync_ClientInitiatedClose_Success() => RunEchoAsync(
            RunClient_CloseAsync_ClientInitiatedClose_Success, useSsl: false);

        [Fact]
        public Task CloseAsync_CloseDescriptionIsMaxLength_Success() => RunEchoAsync(
            RunClient_CloseAsync_CloseDescriptionIsMaxLength_Success, useSsl: false);

        [Fact]
        public Task CloseAsync_CloseDescriptionIsMaxLengthPlusOne_ThrowsArgumentException() => RunEchoAsync(
            RunClient_CloseAsync_CloseDescriptionIsMaxLengthPlusOne_ThrowsArgumentException, useSsl: false);

        [Fact]
        public Task CloseAsync_CloseDescriptionHasUnicode_Success() => RunEchoAsync(
            RunClient_CloseAsync_CloseDescriptionHasUnicode_Success, useSsl: false);

        [Fact]
        public Task CloseAsync_CloseDescriptionIsNull_Success() => RunEchoAsync(
            RunClient_CloseAsync_CloseDescriptionIsNull_Success, useSsl: false);

        [Fact]
        public Task CloseOutputAsync_ExpectedStates() => RunEchoAsync(
            RunClient_CloseOutputAsync_ExpectedStates, useSsl: false);

        [Fact]
        public Task CloseAsync_CloseOutputAsync_Throws() => RunEchoAsync(
            RunClient_CloseAsync_CloseOutputAsync_Throws, useSsl: false);

        [Fact]
        public Task CloseOutputAsync_ClientInitiated_CanReceive_CanClose() => RunEchoAsync(
            RunClient_CloseOutputAsync_ClientInitiated_CanReceive_CanClose, useSsl: false);

        [Theory, InlineData(false), InlineData(true)]
        public Task CloseOutputAsync_ServerInitiated_CanReceive(bool delayReceiving) => RunEchoAsync(
            server => RunClient_CloseOutputAsync_ServerInitiated_CanReceive(server, delayReceiving), useSsl: false);

        [Fact]
        public Task CloseOutputAsync_ServerInitiated_CanSend() => RunEchoAsync(
            RunClient_CloseOutputAsync_ServerInitiated_CanSend, useSsl: false);

        [Theory, InlineData(false), InlineData(true)]
        public Task CloseOutputAsync_ServerInitiated_CanReceiveAfterClose(bool syncState) => RunEchoAsync(
            server => RunClient_CloseOutputAsync_ServerInitiated_CanReceiveAfterClose(server, syncState), useSsl: false);

        [Fact]
        public Task CloseOutputAsync_CloseDescriptionIsNull_Success() => RunEchoAsync(
            RunClient_CloseOutputAsync_CloseDescriptionIsNull_Success, useSsl: false);

        // TODO
        /*[ActiveIssue("https://github.com/dotnet/runtime/issues/22000", TargetFrameworkMonikers.Netcoreapp)]
        [Fact]
        public Task CloseOutputAsync_DuringConcurrentReceiveAsync_ExpectedStates() => RunEchoAsync(
            RunClient_CloseOutputAsync_DuringConcurrentReceiveAsync_ExpectedStates, useSsl: false);*/

        [Fact]
        public Task CloseAsync_DuringConcurrentReceiveAsync_ExpectedStates() => RunEchoAsync(
            RunClient_CloseAsync_DuringConcurrentReceiveAsync_ExpectedStates, useSsl: false);
    }

    // --- HTTP/2 WebSocket loopback tests ---

    public sealed class CloseTest_Invoker_Http2Loopback : CloseTest_Loopback
    {
        public CloseTest_Invoker_Http2Loopback(ITestOutputHelper output) : base(output) { }
        protected override bool UseCustomInvoker => true;
        internal override Version HttpVersion => Net.HttpVersion.Version20;
    }

    public sealed class CloseTest_HttpClient_Http2Loopback : CloseTest_Loopback
    {
        public CloseTest_HttpClient_Http2Loopback(ITestOutputHelper output) : base(output) { }
        protected override bool UseHttpClient => true;
        internal override Version HttpVersion => Net.HttpVersion.Version20;
    }
}
