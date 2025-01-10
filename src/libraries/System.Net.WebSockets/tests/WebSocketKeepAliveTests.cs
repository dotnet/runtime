// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.WebSockets.Tests
{
    public class WebSocketKeepAliveTests
    {
        public static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);
        public static readonly TimeSpan KeepAliveInterval = TimeSpan.FromMilliseconds(100);
        public static readonly TimeSpan KeepAliveTimeout = TimeSpan.FromSeconds(1);
        public const int FramesToTestCount = 5;

#region Frame format helper constants

        public const int MinHeaderLength = 2;
        public const int MaskLength = 4;
        public const int SingleInt64PayloadLength = sizeof(long);
        public const int PingPayloadLength = SingleInt64PayloadLength;

        // 0b_1_***_**** -- fin=true
        public const byte FirstByteBits_FinFlag = 0b_1_000_0000;

        // 0b_*_***_0010 -- opcode=BINARY (0x02)
        public const byte FirstByteBits_OpcodeBinary = 0b_0_000_0010;

        // 0b_*_***_1001 -- opcode=PING (0x09)
        public const byte FirstByteBits_OpcodePing = 0b_0_000_1001;

        // 0b_*_***_1010 -- opcode=PONG (0x10)
        public const byte FirstByteBits_OpcodePong = 0b_0_000_1010;

        // 0b_1_******* -- mask=true
        public const byte SecondByteBits_MaskFlag = 0b_1_0000000;

        // 0b_*_0001000 -- length=8
        public const byte SecondByteBits_PayloadLength8 = SingleInt64PayloadLength;

        public const byte FirstByte_PingFrame = FirstByteBits_FinFlag | FirstByteBits_OpcodePing;
        public const byte FirstByte_PongFrame = FirstByteBits_FinFlag | FirstByteBits_OpcodePong;
        public const byte FirstByte_DataFrame = FirstByteBits_FinFlag | FirstByteBits_OpcodeBinary;

        public const byte SecondByte_Server_NoPayload = 0;
        public const byte SecondByte_Client_NoPayload = SecondByteBits_MaskFlag;

        public const byte SecondByte_Server_8bPayload = SecondByteBits_PayloadLength8;
        public const byte SecondByte_Client_8bPayload = SecondByteBits_MaskFlag | SecondByteBits_PayloadLength8;

        public const int Server_FrameHeaderLength = MinHeaderLength;
        public const int Client_FrameHeaderLength = MinHeaderLength + MaskLength;

#endregion

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task WebSocket_NoUserReadOrWrite_SendsUnsolicitedPong(bool isServer)
        {
            var cancellationToken = new CancellationTokenSource(TestTimeout).Token;

            using WebSocketTestStream testStream = new();
            Stream localEndpointStream = testStream;
            Stream remoteEndpointStream = testStream.Remote;

            using WebSocket webSocket = WebSocket.CreateFromStream(localEndpointStream, new WebSocketCreationOptions
            {
                IsServer = isServer,
                KeepAliveInterval = KeepAliveInterval
            });

            // --- "remote endpoint" side ---

            int pongFrameLength = isServer ? Server_FrameHeaderLength : Client_FrameHeaderLength;
            var pongBuffer = new byte[pongFrameLength];
            for (int i = 0; i < FramesToTestCount; i++) // WS should be sending pongs "indefinitely", let's check a few
            {
                await remoteEndpointStream.ReadExactlyAsync(pongBuffer, cancellationToken);

                Assert.Equal(FirstByte_PongFrame, pongBuffer[0]);
                Assert.Equal(
                    isServer ? SecondByte_Server_NoPayload : SecondByte_Client_NoPayload,
                    pongBuffer[1]);
            }
        }

        [Fact]
        public async Task WebSocketServer_NoUserReadOrWrite_SendsPingAndReadsPongResponse()
        {
            var cancellationToken = new CancellationTokenSource(TestTimeout).Token;

            using WebSocketTestStream testStream = new();
            Stream serverStream = testStream;
            Stream clientStream = testStream.Remote;

            using WebSocket webSocketServer = WebSocket.CreateFromStream(serverStream, new WebSocketCreationOptions
            {
                IsServer = true,
                KeepAliveInterval = KeepAliveInterval,
                KeepAliveTimeout = TestTimeout // we don't care about the actual timeout here
            });

            // we need an outstanding read to ensure the client receives pongs
            var readCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var serverReadTask = webSocketServer.ReceiveAsync(Memory<byte>.Empty, readCts.Token);

            // --- "client" side ---

            var buffer = new byte[Client_FrameHeaderLength + PingPayloadLength]; // client frame is bigger because of masking

            for (int i = 0; i < FramesToTestCount; i++) // WS should be sending pings "indefinitely", let's check a few
            {
                Assert.Equal(WebSocketState.Open, webSocketServer.State);
                Assert.False(serverReadTask.IsCompleted);

                buffer.AsSpan().Clear();
                await clientStream.ReadExactlyAsync(
                    buffer.AsMemory(0, Server_FrameHeaderLength + PingPayloadLength),
                    cancellationToken);

                Assert.Equal(FirstByte_PingFrame, buffer[0]);

                // implementation detail: payload is a long counter starting from 1
                Assert.Equal(SecondByte_Server_8bPayload, buffer[1]);

                var payloadBytes = buffer.AsSpan().Slice(Server_FrameHeaderLength, PingPayloadLength);
                long pingCounter = BinaryPrimitives.ReadInt64BigEndian(payloadBytes);

                Assert.Equal(i+1, pingCounter);

                // --- sending pong back ---

                buffer[0] = FirstByte_PongFrame;
                buffer[1] = SecondByte_Client_8bPayload;

                // using zeroes as a "mask" -- applying such a mask is a no-op
                Array.Clear(buffer, MinHeaderLength, MaskLength);

                // sending the same payload back
                BinaryPrimitives.WriteInt64BigEndian(buffer.AsSpan().Slice(Client_FrameHeaderLength), pingCounter);

                await clientStream.WriteAsync(buffer, cancellationToken);
            }

            Assert.Equal(WebSocketState.Open, webSocketServer.State);
            Assert.False(serverReadTask.IsCompleted);

            readCts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() => serverReadTask.AsTask());
            Assert.Equal(WebSocketState.Aborted, webSocketServer.State);
        }

        [Fact]
        public async Task WebSocketClient_NoServerDataSent_SendsPingAndReadsPongResponse()
        {
            var cancellationToken = new CancellationTokenSource(TestTimeout).Token;

            using WebSocketTestStream testStream = new();
            Stream clientStream = testStream;
            Stream serverStream = testStream.Remote;

            using WebSocket webSocketClient = WebSocket.CreateFromStream(clientStream, new WebSocketCreationOptions
            {
                IsServer = false,
                KeepAliveInterval = KeepAliveInterval,
                KeepAliveTimeout = TestTimeout // we don't care about the actual timeout here
            });

            // we need an outstanding read to ensure the client receives pongs
            var readCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var clientReadTask = webSocketClient.ReceiveAsync(Memory<byte>.Empty, readCts.Token);


            // --- "server" side ---

            var buffer = new byte[Client_FrameHeaderLength + PingPayloadLength]; // client frame is bigger because of masking

            for (int i = 0; i < FramesToTestCount; i++) // WS should be sending pings "indefinitely", let's check a few
            {
                Assert.Equal(WebSocketState.Open, webSocketClient.State);
                Assert.False(clientReadTask.IsCompleted);

                buffer.AsSpan().Clear();
                await serverStream.ReadExactlyAsync(buffer, cancellationToken);

                Assert.Equal(FirstByte_PingFrame, buffer[0]);

                // implementation detail: payload is a long counter starting from 1
                Assert.Equal(SecondByte_Client_8bPayload, buffer[1]);

                var payloadBytes = buffer.AsSpan().Slice(Client_FrameHeaderLength, PingPayloadLength);
                ApplyMask(payloadBytes, buffer.AsSpan().Slice(Client_FrameHeaderLength - MaskLength, MaskLength));
                long pingCounter = BinaryPrimitives.ReadInt64BigEndian(payloadBytes);
                Assert.Equal(i+1, pingCounter);

                // --- sending pong back ---

                buffer[0] = FirstByte_PongFrame;
                buffer[1] = SecondByte_Server_8bPayload;

                // sending the same payload back
                BinaryPrimitives.WriteInt64BigEndian(buffer.AsSpan().Slice(Server_FrameHeaderLength), pingCounter);

                await serverStream.WriteAsync(
                    buffer.AsMemory(0, Server_FrameHeaderLength + PingPayloadLength),
                    cancellationToken);
            }

            Assert.Equal(WebSocketState.Open, webSocketClient.State);
            Assert.False(clientReadTask.IsCompleted);

            readCts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() => clientReadTask.AsTask());
            Assert.Equal(WebSocketState.Aborted, webSocketClient.State);

            // Octet i of the transformed data ("transformed-octet-i") is the XOR of
            // octet i of the original data ("original-octet-i") with octet at index
            // i modulo 4 of the masking key ("masking-key-octet-j"):
            //
            //     j                   = i MOD 4
            //     transformed-octet-i = original-octet-i XOR masking-key-octet-j
            //
            static void ApplyMask(Span<byte> buffer, Span<byte> mask)
            {

                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] ^= mask[i % MaskLength];
                }
            }
        }

        [OuterLoop("Uses Task.Delay")]
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task WebSocket_NoPongResponseWithinTimeout_Aborted(bool outstandingUserRead)
        {
            var cancellationToken = new CancellationTokenSource(TestTimeout).Token;

            using WebSocketTestStream testStream = new();
            Stream localEndpointStream = testStream;
            Stream remoteEndpointStream = testStream.Remote;

            using WebSocket webSocket = WebSocket.CreateFromStream(localEndpointStream, new WebSocketCreationOptions
            {
                IsServer = true,
                KeepAliveInterval = KeepAliveInterval,
                KeepAliveTimeout = KeepAliveTimeout
            });

            Debug.Assert(webSocket.State == WebSocketState.Open);

            ValueTask<ValueWebSocketReceiveResult> userReadTask = default;
            if (outstandingUserRead)
            {
                userReadTask = webSocket.ReceiveAsync(Memory<byte>.Empty, cancellationToken);
            }

            await Task.Delay(2 * (KeepAliveTimeout + KeepAliveInterval), cancellationToken);

            Assert.Equal(WebSocketState.Aborted, webSocket.State);

            Exception readException = outstandingUserRead
                ? await Assert.ThrowsAsync<OperationCanceledException>(() => userReadTask.AsTask())
                : await Assert.ThrowsAsync<WebSocketException>(() => webSocket.ReceiveAsync(Memory<byte>.Empty, cancellationToken).AsTask());

            var wse = Assert.IsType<WebSocketException>(readException.InnerException);
            Assert.Equal(WebSocketError.Faulted, wse.WebSocketErrorCode);
            Assert.Equal(SR.net_Websockets_KeepAlivePingTimeout, wse.Message);
        }
    }
}
