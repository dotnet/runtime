// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Test.Common
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
