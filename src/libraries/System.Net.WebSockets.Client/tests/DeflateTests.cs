// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Test.Common;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    [PlatformSpecific(~TestPlatforms.Browser)]
    public class DeflateTests : ClientWebSocketTestBase
    {
        public DeflateTests(ITestOutputHelper output) : base(output)
        {
        }

        [ConditionalTheory(nameof(WebSocketsSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34690", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
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

                await client.ConnectAsync(uri, cancellation.Token);

                object webSocketHandle = client.GetType().GetField("_innerWebSocket", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(client);
                WebSocketDeflateOptions negotiatedDeflateOptions = (WebSocketDeflateOptions)webSocketHandle.GetType()
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
