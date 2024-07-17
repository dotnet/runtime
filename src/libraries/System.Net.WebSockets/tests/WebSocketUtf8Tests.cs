// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.WebSockets.Tests
{
    public class WebSocketUtf8Tests
    {
        [Theory]
        [InlineData(new byte[] { })]
        [InlineData(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x20, 0x57, 0x6F, 0x72, 0x6C, 0x64 })] // Hello World
        [InlineData(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x2D, 0xC2, 0xB5, 0x40, 0xC3, 0x9F, 0xC3, 0xB6, 0xC3, 0xA4, 0xC3, 0xBC, 0xC3, 0xA0, 0xC3, 0xA1 })] // "Hello-µ@ßöäüàá";
        [InlineData(new byte[] { 0x68, 0x65, 0x6c, 0x6c, 0x6f, 0xf0, 0xa4, 0xad, 0xa2, 0x77, 0x6f, 0x72, 0x6c, 0x64 })] // "hello\U00024b62world"
        [InlineData(new byte[] { 0xf0, 0xa4, 0xad, 0xa2 })] // "\U00024b62"
        public async Task ValidateSingleValidSegments_Valid(byte[] data)
        {
            await WithConnectedWebSockets(async (ws1, ws2) =>
            {
                Assert.True(await IsValidUtf8Async(ws1, ws2, data, endOfMessage: true));

                for (int i = 0 ; i < data.Length; i++)
                {
                    Assert.True(await IsValidUtf8Async(ws1, ws2, data.AsMemory(i, 1), endOfMessage: i == data.Length - 1));
                }
            });
        }

        [Theory]
        [InlineData(new byte[] { }, new byte[] { }, new byte[] { })]
        [InlineData(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x20 }, new byte[] { }, new byte[] { 0x57, 0x6F, 0x72, 0x6C, 0x64 })] // Hello ,, World
        [InlineData(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x2D, 0xC2, }, new byte[] { 0xB5, 0x40, 0xC3, 0x9F, 0xC3, 0xB6, 0xC3, 0xA4, }, new byte[] { 0xC3, 0xBC, 0xC3, 0xA0, 0xC3, 0xA1 })] // "Hello-µ@ßöäüàá";
        public async Task ValidateMultipleValidSegments_Valid(byte[] data1, byte[] data2, byte[] data3)
        {
            await WithConnectedWebSockets(async (ws1, ws2) =>
            {
                Assert.True(await IsValidUtf8Async(ws1, ws2, data1, endOfMessage: false));
                Assert.True(await IsValidUtf8Async(ws1, ws2, data2, endOfMessage: false));
                Assert.True(await IsValidUtf8Async(ws1, ws2, data3, endOfMessage: false));

                for (int i = 0; i < data1.Length; i++)
                {
                    Assert.True(await IsValidUtf8Async(ws1, ws2, data1.AsMemory(i, 1), endOfMessage: false));
                }
                for (int i = 0; i < data2.Length; i++)
                {
                    Assert.True(await IsValidUtf8Async(ws1, ws2, data2.AsMemory(i, 1), endOfMessage: false));
                }
                for (int i = 0; i < data3.Length; i++)
                {
                    Assert.True(await IsValidUtf8Async(ws1, ws2, data3.AsMemory(i, 1), endOfMessage: i == data3.Length - 1));
                }
            });
        }

        [Theory]
        [InlineData(new byte[] { 0xfe })]
        [InlineData(new byte[] { 0xff })]
        [InlineData(new byte[] { 0xfe, 0xfe, 0xff, 0xff })]
        [InlineData(new byte[] { 0xc0, 0xb1 })] // Overlong Ascii
        [InlineData(new byte[] { 0xc1, 0xb1 })] // Overlong Ascii
        [InlineData(new byte[] { 0xe0, 0x80, 0xaf })] // Overlong
        [InlineData(new byte[] { 0xf0, 0x80, 0x80, 0xaf })] // Overlong
        [InlineData(new byte[] { 0xf8, 0x80, 0x80, 0x80, 0xaf })] // Overlong
        [InlineData(new byte[] { 0xfc, 0x80, 0x80, 0x80, 0x80, 0xaf })] // Overlong
        [InlineData(new byte[] { 0xed, 0xa0, 0x80, 0x65, 0x64, 0x69, 0x74, 0x65, 0x64 })] // 0xEDA080 decodes to 0xD800, which is a reserved high surrogate character.
        public async Task ValidateSingleInvalidSegment_Invalid(byte[] data)
        {
            await WithConnectedWebSockets(async (ws1, ws2) =>
            {
                Assert.False(await IsValidUtf8Async(ws1, ws2, data, endOfMessage: true));
            });
        }

        [Fact]
        public async Task ValidateIndividualInvalidSegments_Invalid()
        {
            byte[] data = [0xce, 0xba, 0xe1, 0xbd, 0xb9, 0xcf, 0x83, 0xce, 0xbc, 0xce, 0xb5, 0xed, 0xa0, 0x80, 0x65, 0x64, 0x69, 0x74, 0x65, 0x64];

            await WithConnectedWebSockets(async (ws1, ws2) =>
            {
                Assert.False(await IsValidUtf8Async(ws1, ws2, data, endOfMessage: false));
            });

            await WithConnectedWebSockets(async (ws1, ws2) =>
            {
                for (int i = 0; i < 12; i++)
                {
                    Assert.True(await IsValidUtf8Async(ws1, ws2, data.AsMemory(i, 1), endOfMessage: false), i.ToString());
                }

                Assert.False(await IsValidUtf8Async(ws1, ws2, data.AsMemory(12, 1), endOfMessage: false), 12.ToString());
            });
        }

        [Fact]
        public async Task ValidateMultipleInvalidSegments_Invalid()
        {
            byte[] data0 = [0xce, 0xba, 0xe1, 0xbd, 0xb9, 0xcf, 0x83, 0xce, 0xbc, 0xce, 0xb5, 0xf4];
            byte[] data1 = [0x90];

            await WithConnectedWebSockets(async (ws1, ws2) =>
            {
                Assert.True(await IsValidUtf8Async(ws1, ws2, data0, endOfMessage: false));
                Assert.False(await IsValidUtf8Async(ws1, ws2, data1, endOfMessage: false));
            });

            await WithConnectedWebSockets(async (ws1, ws2) =>
            {
                for (int i = 0; i < data0.Length; i++)
                {
                    Assert.True(await IsValidUtf8Async(ws1, ws2, data0.AsMemory(i, 1), endOfMessage: false));
                }

                Assert.False(await IsValidUtf8Async(ws1, ws2, data1, endOfMessage: false));
            });
        }

        private static async ValueTask<bool> IsValidUtf8Async(WebSocket sender, WebSocket receiver, Memory<byte> buffer, bool endOfMessage)
        {
            await sender.SendAsync(buffer, WebSocketMessageType.Text, endOfMessage, CancellationToken.None).ConfigureAwait(false);
            try
            {
                await receiver.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);
                return true;
            }
            catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.Faulted)
            {
                return false;
            }
        }

        private static async Task WithConnectedWebSockets(Func<WebSocket, WebSocket, Task> callback)
        {
            (Stream stream1, Stream stream2) = ConnectedStreams.CreateBidirectional();
            using WebSocket ws1 = WebSocket.CreateFromStream(stream1, isServer: false, subProtocol: null, Timeout.InfiniteTimeSpan);
            using WebSocket ws2 = WebSocket.CreateFromStream(stream2, isServer: true, subProtocol: null, Timeout.InfiniteTimeSpan);
            await callback(ws1, ws2).ConfigureAwait(false);
        }
    }
}
