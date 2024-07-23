// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.WebSockets.Tests
{
    public class WebSocketKeepAliveTests
    {
        public static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);
        public static readonly TimeSpan KeepAliveInterval = TimeSpan.FromMilliseconds(100);
        public static readonly TimeSpan KeepAliveTimeout = TimeSpan.FromMilliseconds(500);
        public const int FramesToTestCount = 3;

        public const int MinHeaderLength = 2;
        public const int MaskLength = 4;
        public const int PingPayloadLength = 8;

        // 0b_1_***_**** -- fin=true
        public const byte FirstByteBits_FinFlag = 0b_1_000_0000;

        // 0b_*_***_1001 -- opcode=PING (0x09)
        public const byte FirstByteBits_OpcodePing = 0b_0_000_1001;

        // 0b_*_***_1010 -- opcode=PONG (0x10)
        public const byte FirstByteBits_OpcodePong = 0b_0_000_1010;

        // 0b_1_******* -- mask=true
        public const byte SecondByteBits_MaskFlag = 0b_1_0000000;

        // 0b_*_0001000 -- length=8
        public const byte SecondByteBits_PayloadLength8 = PingPayloadLength;

        public const byte FirstByte_PingFrame = FirstByteBits_FinFlag | FirstByteBits_OpcodePing;
        public const byte FirstByte_PongFrame = FirstByteBits_FinFlag | FirstByteBits_OpcodePong;

        public const byte SecondByte_Server_NoPayload = 0;
        public const byte SecondByte_Client_NoPayload = SecondByteBits_MaskFlag;

        public const byte SecondByte_Server_8bPayload = SecondByteBits_PayloadLength8;
        public const byte SecondByte_Client_8bPayload = SecondByteBits_MaskFlag | SecondByteBits_PayloadLength8;

        public const int Server_FrameHeaderLength = MinHeaderLength;
        public const int Client_FrameHeaderLength = MinHeaderLength + MaskLength;

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task UnsolicitedPong_NoReadOrWrite_Success(bool isServer)
        {
            var cancellationToken = new CancellationTokenSource(TestTimeout).Token;

            using WebSocketTestStream stream = new();
            using WebSocket websocket = WebSocket.CreateFromStream(stream.Remote, new WebSocketCreationOptions
            {
                IsServer = isServer,
                KeepAliveInterval = KeepAliveInterval
            });

            int pongFrameLength = isServer ? Server_FrameHeaderLength : Client_FrameHeaderLength;
            var pongBuffer = new byte[pongFrameLength];
            for (int i = 0; i < FramesToTestCount; i++) // WS should be sending pongs "indefinitely", let's check a few
            {
                await stream.ReadExactlyAsync(pongBuffer, cancellationToken);

                Assert.Equal(FirstByte_PongFrame, pongBuffer[0]);
                Assert.Equal(
                    isServer ? SecondByte_Server_NoPayload : SecondByte_Client_NoPayload,
                    pongBuffer[1]);
            }
        }

        [Fact]
        public async Task Server_KeepAlivePing_NoReadOrWrite_Success()
        {
            var cancellationToken = new CancellationTokenSource(TestTimeout).Token;

            using WebSocketTestStream stream = new();
            using WebSocket websocket = WebSocket.CreateFromStream(stream.Remote, new WebSocketCreationOptions
            {
                IsServer = true,
                KeepAliveInterval = KeepAliveInterval,
                KeepAliveTimeout = TestTimeout // we don't care about the actual timeout here
            });

            // --- "client" side ---

            var buffer = new byte[Client_FrameHeaderLength + PingPayloadLength]; // client frame is bigger because of masking

            for (int i = 0; i < FramesToTestCount; i++) // WS should be sending pings "indefinitely", let's check a few
            {
                await stream.ReadExactlyAsync(
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

                await stream.WriteAsync(buffer, cancellationToken);
            }
        }

        [Fact]
        public async Task Client_KeepAlivePing_NoReadOrWrite_Success()
        {
            var cancellationToken = new CancellationTokenSource(TestTimeout).Token;

            using WebSocketTestStream stream = new();
            using WebSocket websocket = WebSocket.CreateFromStream(stream.Remote, new WebSocketCreationOptions
            {
                IsServer = false,
                KeepAliveInterval = KeepAliveInterval,
                KeepAliveTimeout = TestTimeout // we don't care about the actual timeout here
            });

            // --- "server" side ---

            var buffer = new byte[Client_FrameHeaderLength + PingPayloadLength]; // client frame is bigger because of masking

            for (int i = 0; i < FramesToTestCount; i++) // WS should be sending pings "indefinitely", let's check a few
            {
                await stream.ReadExactlyAsync(buffer, cancellationToken);

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

                await stream.WriteAsync(
                    buffer.AsMemory(0, Server_FrameHeaderLength + PingPayloadLength),
                    cancellationToken);
            }

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
    }
}
