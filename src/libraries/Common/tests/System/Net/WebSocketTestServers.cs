// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.Tests
{
    internal class WebSocketTestServers
    {
        public const string Host = "corefx-net.cloudapp.net";

        private const string EchoHandler = "WebSocket/EchoWebSocket.ashx";
        private const string EchoHeadersHandler = "WebSocket/EchoWebSocketHeaders.ashx";

        public readonly static Uri RemoteEchoServer = new Uri("ws://" + Host + "/" + EchoHandler);
        public readonly static Uri SecureRemoteEchoServer = new Uri("wss://" + Host + "/" + EchoHandler);

        public readonly static Uri RemoteEchoHeadersServer = new Uri("ws://" + Host + "/" + EchoHeadersHandler);
        public readonly static Uri SecureRemoteEchoHeadersServer = new Uri("wss://" + Host + "/" + EchoHeadersHandler);
        
        public readonly static object[][] EchoServers = { new object[] { RemoteEchoServer }, new object[] { SecureRemoteEchoServer } };
        public readonly static object[][] EchoHeadersServers = { new object[] { RemoteEchoHeadersServer }, new object[] { SecureRemoteEchoHeadersServer } };
    }
}
