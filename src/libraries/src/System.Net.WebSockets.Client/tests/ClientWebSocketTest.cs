// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Tests;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    /// <summary>
    /// ClientWebSocket tests that do require a remote server.
    /// </summary>
    public class ClientWebSocketTest
    {
        public readonly static object[][] EchoServers = WebSocketTestServers.EchoServers;

        private const int s_TimeOutMilliseconds = 2000;

        private readonly ITestOutputHelper _output;
        
        public ClientWebSocketTest(ITestOutputHelper output)
        {
            _output = output;
        }

        private static bool WebSocketsSupported { get { return WebSocketHelper.WebSocketsSupported; } }

        [ConditionalTheory("WebSocketsSupported"), MemberData("EchoServers")]
        public async Task EchoBinaryMessage_Success(Uri server)
        {
            var helper = new WebSocketHelper(server, s_TimeOutMilliseconds);
            await helper.TestEcho(WebSocketMessageType.Binary);
        }

        [ConditionalTheory("WebSocketsSupported"), MemberData("EchoServers")]
        public async Task EchoTextMessage_Success(Uri server)
        {
            var helper = new WebSocketHelper(server, s_TimeOutMilliseconds);
            await helper.TestEcho(WebSocketMessageType.Text);
        }        
    }
}
