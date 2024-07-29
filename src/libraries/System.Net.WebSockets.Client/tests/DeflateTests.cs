// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Http;
using System.Net.Test.Common;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    public sealed class InvokerDeflateTests : DeflateTests
    {
        public InvokerDeflateTests(ITestOutputHelper output) : base(output) { }

        protected override bool UseCustomInvoker => true;
    }

    public sealed class HttpClientDeflateTests : DeflateTests
    {
        public HttpClientDeflateTests(ITestOutputHelper output) : base(output) { }

        protected override bool UseHttpClient => true;
    }

    [PlatformSpecific(~TestPlatforms.Browser)]
    public class DeflateTests : ClientWebSocketTestBase
    {
        public DeflateTests(ITestOutputHelper output) : base(output)
        {
        }

        [ConditionalTheory(nameof(WebSocketsSupported))]
        [InlineData(15, true, 15, true, "permessage-deflate; client_max_window_bits")]
        [InlineData(14, true, 15, true, "permessage-deflate; client_max_window_bits=14")]
        [InlineData(15, true, 14, true, "permessage-deflate; client_max_window_bits; server_max_window_bits=14")]
        [InlineData(10, true, 11, true, "permessage-deflate; client_max_window_bits=10; server_max_window_bits=11")]
        [InlineData(15, false, 15, true, "permessage-deflate; client_max_window_bits; client_no_context_takeover")]
        [InlineData(15, true, 15, false, "permessage-deflate; client_max_window_bits; server_no_context_takeover")]
        public async Task PerMessageDeflateHeaders(int clientWindowBits, bool clientContextTakeover,
                                                   int serverWindowBits, bool serverContextTakover,
                                                   string expected)
        {
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using var client = new ClientWebSocket();
                using var cancellation = new CancellationTokenSource(TimeOutMilliseconds);

                client.Options.DangerousDeflateOptions = new WebSocketDeflateOptions
                {
                    ClientMaxWindowBits = clientWindowBits,
                    ClientContextTakeover = clientContextTakeover,
                    ServerMaxWindowBits = serverWindowBits,
                    ServerContextTakeover = serverContextTakover
                };

                await ConnectAsync(client, uri, cancellation.Token);

                object webSocketHandle = typeof(ClientWebSocket).GetField("_innerWebSocket", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(client);
                WebSocketDeflateOptions negotiatedDeflateOptions = (WebSocketDeflateOptions)Type.GetType("System.Net.WebSockets.WebSocketHandle, System.Net.WebSockets.Client")
                    .GetField("_negotiatedDeflateOptions", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(webSocketHandle);

                Assert.Equal(clientWindowBits - 1, negotiatedDeflateOptions.ClientMaxWindowBits);
                Assert.Equal(clientContextTakeover, negotiatedDeflateOptions.ClientContextTakeover);
                Assert.Equal(serverWindowBits - 1, negotiatedDeflateOptions.ServerMaxWindowBits);
                Assert.Equal(serverContextTakover, negotiatedDeflateOptions.ServerContextTakeover);
            }, server => server.AcceptConnectionAsync(async connection =>
            {
                var extensionsReply = CreateDeflateOptionsHeader(new WebSocketDeflateOptions
                {
                    ClientMaxWindowBits = clientWindowBits - 1,
                    ClientContextTakeover = clientContextTakeover,
                    ServerMaxWindowBits = serverWindowBits - 1,
                    ServerContextTakeover = serverContextTakover
                });
                Dictionary<string, string> headers = await LoopbackHelper.WebSocketHandshakeAsync(connection, extensionsReply);
                Assert.NotNull(headers);
                Assert.True(headers.TryGetValue("Sec-WebSocket-Extensions", out string extensions));
                Assert.Equal(expected, extensions);
            }), new LoopbackServer.Options { WebSocketEndpoint = true });
        }

        [ConditionalFact(nameof(WebSocketsSupported))]
        public async Task ThrowsWhenContinuationHasDifferentCompressionFlags()
        {
            var deflateOpt = new WebSocketDeflateOptions
            {
                ClientMaxWindowBits = 14,
                ClientContextTakeover = true,
                ServerMaxWindowBits = 14,
                ServerContextTakeover = true
            };
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using var cws = new ClientWebSocket();
                using var cts = new CancellationTokenSource(TimeOutMilliseconds);

                cws.Options.DangerousDeflateOptions = deflateOpt;
                await ConnectAsync(cws, uri, cts.Token);


                await cws.SendAsync(Memory<byte>.Empty, WebSocketMessageType.Text, WebSocketMessageFlags.DisableCompression, default);
                Assert.Throws<ArgumentException>("messageFlags", () =>
                   cws.SendAsync(Memory<byte>.Empty, WebSocketMessageType.Binary, WebSocketMessageFlags.EndOfMessage, default));
            }, server => server.AcceptConnectionAsync(async connection =>
            {
                var extensionsReply = CreateDeflateOptionsHeader(deflateOpt);
                await LoopbackHelper.WebSocketHandshakeAsync(connection, extensionsReply);
            }), new LoopbackServer.Options { WebSocketEndpoint = true });
        }

        [ConditionalFact(nameof(WebSocketsSupported))]
        public async Task SendHelloWithDisableCompression()
        {
            byte[] bytes = "Hello"u8.ToArray();

            int prefixLength = 2;
            byte[] rawPrefix = new byte[] { 0x81, 0x85 }; // fin=1, rsv=0, opcode=text; mask=1, len=5
            int rawRemainingBytes = 9; // mask bytes (4) + payload bytes (5)
            byte[] compressedPrefix = new byte[] { 0xc1, 0x87 }; // fin=1, rsv=compressed, opcode=text; mask=1, len=7
            int compressedRemainingBytes = 11; // mask bytes (4) + payload bytes (7)

            var deflateOpt = new WebSocketDeflateOptions
            {
                ClientMaxWindowBits = 14,
                ClientContextTakeover = true,
                ServerMaxWindowBits = 14,
                ServerContextTakeover = true
            };

            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using var cws = new ClientWebSocket();
                using var cts = new CancellationTokenSource(TimeOutMilliseconds);

                cws.Options.DangerousDeflateOptions = deflateOpt;
                await ConnectAsync(cws, uri, cts.Token);

                await cws.SendAsync(bytes, WebSocketMessageType.Text, true, cts.Token);

                WebSocketMessageFlags flags = WebSocketMessageFlags.DisableCompression | WebSocketMessageFlags.EndOfMessage;
                await cws.SendAsync(bytes, WebSocketMessageType.Text, flags, cts.Token);
            }, server => server.AcceptConnectionAsync(async connection =>
            {
                var buffer = new byte[compressedRemainingBytes];
                var extensionsReply = CreateDeflateOptionsHeader(deflateOpt);
                await LoopbackHelper.WebSocketHandshakeAsync(connection, extensionsReply);

                // first message is compressed
                await ReadExactAsync(buffer, prefixLength);
                Assert.Equal(compressedPrefix, buffer[..prefixLength]);
                // read rest of the frame
                await ReadExactAsync(buffer, compressedRemainingBytes);

                // second message is not compressed
                await ReadExactAsync(buffer, prefixLength);
                Assert.Equal(rawPrefix, buffer[..prefixLength]);
                // read rest of the frame
                await ReadExactAsync(buffer, rawRemainingBytes);

                async Task ReadExactAsync(byte[] buf, int n)
                {
                    await connection.Stream.ReadAtLeastAsync(buf.AsMemory(0, n), n);
                }
            }), new LoopbackServer.Options { WebSocketEndpoint = true });
        }

        private static string CreateDeflateOptionsHeader(WebSocketDeflateOptions options)
        {
            var builder = new StringBuilder();
            builder.Append("permessage-deflate");

            if (options.ClientMaxWindowBits != 15)
            {
                builder.Append("; client_max_window_bits=").Append(options.ClientMaxWindowBits);
            }

            if (!options.ClientContextTakeover)
            {
                builder.Append("; client_no_context_takeover");
            }

            if (options.ServerMaxWindowBits != 15)
            {
                builder.Append("; server_max_window_bits=").Append(options.ServerMaxWindowBits);
            }

            if (!options.ServerContextTakeover)
            {
                builder.Append("; server_no_context_takeover");
            }

            return builder.ToString();
        }
    }
}
