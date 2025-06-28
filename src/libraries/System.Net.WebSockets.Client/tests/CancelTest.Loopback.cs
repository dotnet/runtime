// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    // --- Loopback Echo Server "overrides" ---

    [ConditionalClass(typeof(ClientWebSocketTestBase), nameof(WebSocketsSupported))]
    [SkipOnPlatform(TestPlatforms.Browser, "System.Net.Sockets are not supported on browser")]
    public abstract class CancelTest_Loopback(ITestOutputHelper output) : CancelTestBase(output)
    {
        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task ConnectAsync_Cancel_ThrowsCancellationException(bool useSsl) => RunEchoAsync(
            RunClient_ConnectAsync_Cancel_ThrowsCancellationException, useSsl);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task SendAsync_Cancel_Success(bool useSsl) => RunEchoAsync(
            RunClient_SendAsync_Cancel_Success, useSsl);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task ReceiveAsync_Cancel_Success(bool useSsl) => RunEchoAsync(
            RunClient_ReceiveAsync_Cancel_Success, useSsl);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task CloseAsync_Cancel_Success(bool useSsl) => RunEchoAsync(
            RunClient_CloseAsync_Cancel_Success, useSsl);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task CloseOutputAsync_Cancel_Success(bool useSsl) => RunEchoAsync(
            RunClient_CloseOutputAsync_Cancel_Success, useSsl);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task ReceiveAsync_CancelThenReceive_ThrowsOperationCanceledException(bool useSsl) => RunEchoAsync(
            RunClient_ReceiveAsync_CancelThenReceive_ThrowsOperationCanceledException, useSsl);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task ReceiveAsync_ReceiveThenCancel_ThrowsOperationCanceledException(bool useSsl) => RunEchoAsync(
            RunClient_ReceiveAsync_ReceiveThenCancel_ThrowsOperationCanceledException, useSsl);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task ReceiveAsync_AfterCancellationDoReceiveAsync_ThrowsWebSocketException(bool useSsl) => RunEchoAsync(
            RunClient_ReceiveAsync_AfterCancellationDoReceiveAsync_ThrowsWebSocketException, useSsl);
    }

    // --- HTTP/1.1 WebSocket loopback tests ---

    public sealed class CancelTest_SharedHandler_Loopback(ITestOutputHelper output) : CancelTest_Loopback(output) { }

    public sealed class CancelTest_Invoker_Loopback(ITestOutputHelper output) : CancelTest_Loopback(output)
    {
        protected override bool UseCustomInvoker => true;
    }

    public sealed class CancelTest_HttpClient_Loopback(ITestOutputHelper output) : CancelTest_Loopback(output)
    {
        protected override bool UseHttpClient => true;
    }

    // --- HTTP/2 WebSocket loopback tests ---

    public abstract class CancelTest_Http2Loopback(ITestOutputHelper output) : CancelTest_Loopback(output)
    {
        internal override Version HttpVersion => Net.HttpVersion.Version20;
    }

    public sealed class CancelTest_Invoker_Http2Loopback(ITestOutputHelper output) : CancelTest_Http2Loopback(output)
    {
        protected override bool UseCustomInvoker => true;
    }

    public sealed class CancelTest_HttpClient_Http2Loopback(ITestOutputHelper output) : CancelTest_Http2Loopback(output)
    {
        protected override bool UseHttpClient => true;
    }
}
