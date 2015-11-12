// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.Tests
{
    internal class WebSocketTestServers
    {
        public const string Host = "corefx-networking.azurewebsites.net";

        private const string EchoHandler = "WebSocket/EchoWebSocket.ashx";

        public readonly static Uri RemoteEchoServer = new Uri("ws://" + Host + "/" + EchoHandler);
        public readonly static Uri SecureRemoteEchoServer = new Uri("wss://" + Host + "/" + EchoHandler);

        public readonly static object[][] EchoServers = { new object[] { RemoteEchoServer }, new object[] { SecureRemoteEchoServer } };
    }
}
