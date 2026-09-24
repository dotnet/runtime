// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.WebSockets.Tests
{
    public class WebSocketSendTests
    {
        public static IEnumerable<object[]> FrameLengths()
        {
            int[] lengths = Enumerable.Range(0, 17).Concat(new[]
            {
                31, 32, 33, 63, 64, 65, 125, 126, 127, 128, 129,
                255, 256, 257, 4095, 4096, 4097, 65535, 65536, 65537,
                Vector<byte>.Count - 1, Vector<byte>.Count, Vector<byte>.Count + 1,
                2 * Vector<byte>.Count - 1, 2 * Vector<byte>.Count, 2 * Vector<byte>.Count + 1
            }).Distinct().ToArray();

            foreach (int length in lengths)
            foreach (bool isServer in new[] { false, true })
            foreach (bool cancelable in new[] { false, true })
                yield return new object[] { length, isServer, cancelable };
        }

        [Theory]
        [MemberData(nameof(FrameLengths))]
        public async Task SendAsync_WritesFrameWithoutChangingSource(int length, bool isServer, bool cancelable)
        {
            using WebSocketTestStream stream = new();
            using WebSocket socket = WebSocket.CreateFromStream(stream, isServer, null, Timeout.InfiniteTimeSpan);
            using CancellationTokenSource cts = new();
            CancellationToken token = cancelable ? cts.Token : default;

            foreach (int offset in new[] { 0, 1, 3, 17, 31, 63 })
            {
                byte[] source = new byte[offset + length + 16];
                new Random(42).NextBytes(source);
                byte[] original = (byte[])source.Clone();

                // Exercise an initial fragment, a continuation and a new complete message.
                for (int frame = 0; frame < 3; frame++)
                {
                    await socket.SendAsync(source.AsMemory(offset, length), WebSocketMessageType.Binary, frame != 0, token);
                    AssertFrame(stream.Remote.NextAvailableBytes, original.AsSpan(offset, length), isServer, frame);
                    Assert.Equal(original, source);
                    stream.Remote.Clear();
                }
            }
        }

        private static void AssertFrame(ReadOnlySpan<byte> frame, ReadOnlySpan<byte> payload, bool isServer, int fragment)
        {
            Assert.Equal(fragment == 0 ? 0x02 : fragment == 1 ? 0x80 : 0x82, frame[0]);
            Assert.Equal(!isServer, (frame[1] & 0x80) != 0);
            int lengthCode = frame[1] & 0x7F;
            int headerLength = 2;
            ulong length = (ulong)lengthCode;
            if (lengthCode == 126)
            {
                length = BinaryPrimitives.ReadUInt16BigEndian(frame.Slice(2));
                headerLength += 2;
            }
            else if (lengthCode == 127)
            {
                length = BinaryPrimitives.ReadUInt64BigEndian(frame.Slice(2));
                headerLength += 8;
            }

            Assert.Equal((ulong)payload.Length, length);
            int maskOffset = headerLength;
            if (!isServer)
                headerLength += 4;

            Assert.Equal(headerLength + payload.Length, frame.Length);
            byte[] decoded = frame.Slice(headerLength).ToArray();
            for (int i = 0; i < decoded.Length; i++)
            {
                if (!isServer)
                    decoded[i] ^= frame[maskOffset + (i & 3)];
            }
            Assert.True(payload.SequenceEqual(decoded));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task SendAsync_CompressedFragments_PreservesPayload(bool contextTakeover, bool cancelable)
        {
            using WebSocketTestStream stream = new();
            WebSocketDeflateOptions options = new() { ClientContextTakeover = contextTakeover };
            using WebSocket client = WebSocket.CreateFromStream(stream, new WebSocketCreationOptions
            {
                KeepAliveInterval = Timeout.InfiniteTimeSpan,
                DangerousDeflateOptions = options
            });
            using WebSocket server = WebSocket.CreateFromStream(stream.Remote, new WebSocketCreationOptions
            {
                IsServer = true,
                KeepAliveInterval = Timeout.InfiniteTimeSpan,
                DangerousDeflateOptions = options
            });
            using CancellationTokenSource cts = new();
            CancellationToken token = cancelable ? cts.Token : default;
            byte[] source = new byte[65540];
            new Random(42).NextBytes(source);
            byte[] original = (byte[])source.Clone();

            foreach (bool disableCompression in new[] { false, false, true, false })
            {
                WebSocketMessageFlags flags = disableCompression ? WebSocketMessageFlags.DisableCompression : 0;
                await client.SendAsync(source.AsMemory(1, 31), WebSocketMessageType.Binary, flags, token);
                await client.SendAsync(ReadOnlyMemory<byte>.Empty, WebSocketMessageType.Binary, flags, token);
                // Also exercise the path where writing the frame completes asynchronously.
                stream.DelayForNextSend = TimeSpan.FromMilliseconds(1);
                await client.SendAsync(source.AsMemory(32, source.Length - 33), WebSocketMessageType.Binary,
                    flags | WebSocketMessageFlags.EndOfMessage, token);

                byte[] received = new byte[source.Length];
                int count = 0;
                ValueWebSocketReceiveResult result;
                do
                {
                    result = await server.ReceiveAsync(received.AsMemory(count, Math.Min(257, received.Length - count)), token);
                    Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
                    count += result.Count;
                }
                while (!result.EndOfMessage);

                Assert.Equal(source.Length - 2, count);
                Assert.True(original.AsSpan(1, count).SequenceEqual(received.AsSpan(0, count)));
                Assert.Equal(original, source);
            }
        }
    }
}
