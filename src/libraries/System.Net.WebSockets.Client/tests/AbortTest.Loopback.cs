// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    [ConditionalClass(typeof(ClientWebSocketTestBase), nameof(WebSocketsSupported))]
    [SkipOnPlatform(TestPlatforms.Browser, "System.Net.Sockets are not supported on browser")]
    public abstract class AbortTest_Loopback : ClientWebSocketTestBase
    {
        public AbortTest_Loopback(ITestOutputHelper output) : base(output) { }

        protected virtual Version HttpVersion => Net.HttpVersion.Version11;

        public static object[][] AbortClient_MemberData = ToMemberData(Enum.GetValues<AbortType>(), UseSsl_Values, /* verifySendReceive */ Bool_Values);

        [Theory]
        [MemberData(nameof(AbortClient_MemberData))]
        public Task AbortClient_ServerGetsCorrectException(AbortType abortType, bool useSsl, bool verifySendReceive)
        {
            var clientMsg = new byte[] { 1, 2, 3, 4, 5, 6 };
            var serverMsg = new byte[] { 42 };
            var clientAckTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var serverAckTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            var timeoutCts = new CancellationTokenSource(TimeOutMilliseconds);

            return LoopbackWebSocketServer.RunAsync(
                async (clientWebSocket, token) =>
                {
                    if (verifySendReceive)
                    {
                        await VerifySendReceiveAsync(clientWebSocket, clientMsg, serverMsg, clientAckTcs, serverAckTcs.Task, token);
                    }

                    switch (abortType)
                    {
                        case AbortType.Abort:
                            clientWebSocket.Abort();
                            break;
                        case AbortType.Dispose:
                            clientWebSocket.Dispose();
                            break;
                    }
                },
                async (serverWebSocket, token) =>
                {
                    if (verifySendReceive)
                    {
                        await VerifySendReceiveAsync(serverWebSocket, serverMsg, clientMsg, serverAckTcs, clientAckTcs.Task, token);
                    }

                    var readBuffer = new byte[1];
                    var exception = await Assert.ThrowsAsync<WebSocketException>(async () =>
                        await serverWebSocket.ReceiveAsync(readBuffer, token));

                    Assert.Equal(WebSocketError.ConnectionClosedPrematurely, exception.WebSocketErrorCode);
                    Assert.Equal(WebSocketState.Aborted, serverWebSocket.State);
                },
                new LoopbackWebSocketServer.Options(HttpVersion, useSsl, GetInvoker()),
                timeoutCts.Token);
        }

        public static object[][] ServerPrematureEos_MemberData = ToMemberData(Enum.GetValues<ServerEosType>(), UseSsl_Values);

        [Theory]
        [MemberData(nameof(ServerPrematureEos_MemberData))]
        public Task ServerPrematureEos_ClientGetsCorrectException(ServerEosType serverEosType, bool useSsl)
        {
            var clientMsg = new byte[] { 1, 2, 3, 4, 5, 6 };
            var serverMsg = new byte[] { 42 };
            var clientAckTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var serverAckTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            var timeoutCts = new CancellationTokenSource(TimeOutMilliseconds);

            var globalOptions = new LoopbackWebSocketServer.Options(HttpVersion, useSsl, HttpInvoker: null)
            {
                DisposeServerWebSocket = false,
                ManualServerHandshakeResponse = true
            };

            var serverReceivedEosTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var clientReceivedEosTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            return LoopbackWebSocketServer.RunAsync(
                async uri =>
                {
                    var token = timeoutCts.Token;
                    var clientOptions = globalOptions with { HttpInvoker = GetInvoker() };
                    var clientWebSocket = await LoopbackWebSocketServer.GetConnectedClientAsync(uri, clientOptions, token).ConfigureAwait(false);

                    if (serverEosType == ServerEosType.AfterSomeData)
                    {
                        await VerifySendReceiveAsync(clientWebSocket, clientMsg, serverMsg, clientAckTcs, serverAckTcs.Task, token).ConfigureAwait(false);
                    }

                    // only one side of the stream was closed. the other should work
                    await clientWebSocket.SendAsync(clientMsg, WebSocketMessageType.Binary, endOfMessage: true, token).ConfigureAwait(false);

                    var exception = await Assert.ThrowsAsync<WebSocketException>(() => clientWebSocket.ReceiveAsync(new byte[1], token));
                    Assert.Equal(WebSocketError.ConnectionClosedPrematurely, exception.WebSocketErrorCode);

                    clientReceivedEosTcs.SetResult();
                    clientWebSocket.Dispose();
                },
                async (requestData, token) =>
                {
                    WebSocket serverWebSocket = null!;
                    await SendServerResponseAndEosAsync(
                        requestData,
                        serverEosType,
                        (wsData, ct) =>
                        {
                            var wsOptions = new WebSocketCreationOptions { IsServer = true };
                            serverWebSocket = WebSocket.CreateFromStream(wsData.WebSocketStream, wsOptions);

                            return serverEosType == ServerEosType.AfterSomeData
                                ? VerifySendReceiveAsync(serverWebSocket, serverMsg, clientMsg, serverAckTcs, clientAckTcs.Task, ct)
                                : Task.CompletedTask;
                        },
                        token);

                    Assert.NotNull(serverWebSocket);

                    // only one side of the stream was closed. the other should work
                    var readBuffer = new byte[clientMsg.Length];
                    var result = await serverWebSocket.ReceiveAsync(readBuffer, token);
                    Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
                    Assert.Equal(clientMsg.Length, result.Count);
                    Assert.True(result.EndOfMessage);
                    Assert.Equal(clientMsg, readBuffer);

                    await clientReceivedEosTcs.Task.WaitAsync(token).ConfigureAwait(false);

                    var exception = await Assert.ThrowsAsync<WebSocketException>(() => serverWebSocket.ReceiveAsync(readBuffer, token));
                    Assert.Equal(WebSocketError.ConnectionClosedPrematurely, exception.WebSocketErrorCode);

                    serverWebSocket.Dispose();
                },
                globalOptions,
                timeoutCts.Token);
        }

        protected virtual Task SendServerResponseAndEosAsync(WebSocketRequestData requestData, ServerEosType serverEosType, Func<WebSocketRequestData, CancellationToken, Task> serverFunc, CancellationToken cancellationToken)
            => WebSocketHandshakeHelper.SendHttp11ServerResponseAndEosAsync(requestData, serverFunc, cancellationToken); // override for HTTP/2

        public enum AbortType
        {
            Abort,
            Dispose
        }

        public enum ServerEosType
        {
            WithHeaders,
            RightAfterHeaders,
            AfterSomeData
        }

        protected static async Task VerifySendReceiveAsync(WebSocket ws, byte[] localMsg, byte[] remoteMsg,
            TaskCompletionSource localAckTcs, Task remoteAck, CancellationToken cancellationToken)
        {
            var sendTask = ws.SendAsync(localMsg, WebSocketMessageType.Binary, endOfMessage: true, cancellationToken);

            var recvBuf = new byte[remoteMsg.Length * 2];
            var recvResult = await ws.ReceiveAsync(recvBuf, cancellationToken).ConfigureAwait(false);

            Assert.Equal(WebSocketMessageType.Binary, recvResult.MessageType);
            Assert.Equal(remoteMsg.Length, recvResult.Count);
            Assert.True(recvResult.EndOfMessage);
            Assert.Equal(remoteMsg, recvBuf[..recvResult.Count]);

            localAckTcs.SetResult();

            await sendTask.ConfigureAwait(false);
            await remoteAck.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    // --- HTTP/1.1 WebSocket loopback tests ---

    public class AbortTest_Invoker_Loopback : AbortTest_Loopback
    {
        public AbortTest_Invoker_Loopback(ITestOutputHelper output) : base(output) { }
        protected override bool UseCustomInvoker => true;
    }

    public class AbortTest_HttpClient_Loopback : AbortTest_Loopback
    {
        public AbortTest_HttpClient_Loopback(ITestOutputHelper output) : base(output) { }
        protected override bool UseHttpClient => true;
    }

    public class AbortTest_SharedHandler_Loopback : AbortTest_Loopback
    {
        public AbortTest_SharedHandler_Loopback(ITestOutputHelper output) : base(output) { }
    }

    // --- HTTP/2 WebSocket loopback tests ---

    public abstract class AbortTest_Http2 : AbortTest_Loopback
    {
        public AbortTest_Http2(ITestOutputHelper output) : base(output) { }
        protected override Version HttpVersion => Net.HttpVersion.Version20;
        protected override Task SendServerResponseAndEosAsync(WebSocketRequestData rd, ServerEosType eos, Func<WebSocketRequestData, CancellationToken, Task> callback, CancellationToken ct)
            => WebSocketHandshakeHelper.SendHttp2ServerResponseAndEosAsync(rd, eosInHeadersFrame: eos == ServerEosType.WithHeaders, callback, ct);

        public static object[][] ServerResetsAfterCloseHandshake_MemberData = ToMemberData(Bool_Values, UseSsl_Values);

        [Theory]
        [MemberData(nameof(ServerResetsAfterCloseHandshake_MemberData))]
        public Task ServerResetsAfterCloseHandshake_NoExceptionOnClient(bool sendGoAway, bool useSsl)
        {
            Assert.True(HttpVersion == Net.HttpVersion.Version20);

            var clientMsg = new byte[] { 1, 2, 3, 4, 5, 6 };
            var serverMsg = new byte[] { 42 };
            var serverFinalMsg = new byte[] { 123 };
            var clientAckTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var serverAckTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            var timeoutCts = new CancellationTokenSource(TimeOutMilliseconds);

            var globalOptions = new LoopbackWebSocketServer.Options(HttpVersion, useSsl, HttpInvoker: null)
            {
                DisposeServerWebSocket = false
            };

            var serverSentResetTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var clientReceivedEosTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            return LoopbackWebSocketServer.RunAsync(
                async uri =>
                {
                    var token = timeoutCts.Token;
                    var clientOptions = globalOptions with { HttpInvoker = GetInvoker() };
                    var clientWebSocket = await LoopbackWebSocketServer.GetConnectedClientAsync(uri, clientOptions, token).ConfigureAwait(false);

                    await VerifySendReceiveAsync(clientWebSocket, clientMsg, serverMsg, clientAckTcs, serverAckTcs.Task, token).ConfigureAwait(false);

                    await clientWebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", token).ConfigureAwait(false);

                    await serverSentResetTcs.Task.WaitAsync(token).ConfigureAwait(false);

                    // even though the server sent a reset, the client should receive all the data sent before the reset

                    // receive final message
                    var readBuffer = new byte[1];
                    var result = await clientWebSocket.ReceiveAsync(readBuffer, token).ConfigureAwait(false);
                    Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
                    Assert.Equal(1, result.Count);
                    Assert.True(result.EndOfMessage);
                    Assert.Equal(serverFinalMsg, readBuffer);

                    // receive close frame
                    result = await clientWebSocket.ReceiveAsync(readBuffer, token).ConfigureAwait(false);
                    Assert.Equal(WebSocketMessageType.Close, result.MessageType);

                    // we've already closed our side, so we should be fully closed now
                    Assert.Equal(WebSocketState.Closed, clientWebSocket.State);
                    clientWebSocket.Dispose();
                    clientReceivedEosTcs.SetResult();
                },
                async (requestData, token) =>
                {
                    var connection = requestData.Http2Connection!;
                    var streamId = requestData.Http2StreamId!.Value;

                    var wsOptions = new WebSocketCreationOptions { IsServer = true };
                    var serverWebSocket = WebSocket.CreateFromStream(requestData.WebSocketStream, wsOptions);

                    await VerifySendReceiveAsync(serverWebSocket, serverMsg, clientMsg, serverAckTcs, clientAckTcs.Task, token).ConfigureAwait(false);

                    // wait for client to send close frame
                    var result = await serverWebSocket.ReceiveAsync(new byte[1], token).ConfigureAwait(false);
                    Assert.Equal(WebSocketMessageType.Close, result.MessageType);

                    // send final message
                    await serverWebSocket.SendAsync(serverFinalMsg, WebSocketMessageType.Binary, endOfMessage: true, token).ConfigureAwait(false);

                    await serverWebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", token).ConfigureAwait(false);
                    // we've already closed our side, so we should be fully closed now
                    Assert.Equal(WebSocketState.Closed, serverWebSocket.State);

                    try
                    {
                        await connection.SendResponseDataAsync(streamId, Memory<byte>.Empty, endStream: true).ConfigureAwait(false);

                        if (sendGoAway)
                        {
                            await connection.SendGoAway(streamId).ConfigureAwait(false);
                        }
                        else
                        {
                            var rstFrame = new RstStreamFrame(FrameFlags.None, (int)ProtocolErrors.NO_ERROR, streamId);
                            await connection.WriteFrameAsync(rstFrame).ConfigureAwait(false);
                        }
                    }
                    catch (IOException)
                    {
                        // Ignore connection errors
                    }
                    catch (SocketException)
                    {
                        // Ignore connection errors
                    }

                    await Task.Delay(1000); // give the client some time to process the reset

                    serverSentResetTcs.SetResult();
                    await clientReceivedEosTcs.Task.WaitAsync(token).ConfigureAwait(false);
                    serverWebSocket.Dispose();
                },
                globalOptions,
                timeoutCts.Token);
        }
    }

    public class AbortTest_Invoker_Http2 : AbortTest_Http2
    {
        public AbortTest_Invoker_Http2(ITestOutputHelper output) : base(output) { }
        protected override bool UseCustomInvoker => true;
    }

    public class AbortTest_HttpClient_Http2 : AbortTest_Http2
    {
        public AbortTest_HttpClient_Http2(ITestOutputHelper output) : base(output) { }
        protected override bool UseHttpClient => true;
    }
}
