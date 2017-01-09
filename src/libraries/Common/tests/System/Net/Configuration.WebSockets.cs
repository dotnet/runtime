// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Test.Common
{
    public static partial class Configuration
    {
        public static partial class WebSockets
        {
            public static string Host => GetValue("COREFX_WEBSOCKETHOST", DefaultAzureServer);

            public static string SecureHost => GetValue("COREFX_SECUREWEBSOCKETHOST", DefaultAzureServer);

            private const string EchoHandler = "WebSocket/EchoWebSocket.ashx";
            private const string EchoHeadersHandler = "WebSocket/EchoWebSocketHeaders.ashx";

            public static readonly Uri RemoteEchoServer = new Uri("ws://" + Host + "/" + EchoHandler);
            public static readonly Uri SecureRemoteEchoServer = new Uri("wss://" + SecureHost + "/" + EchoHandler);

            public static readonly Uri RemoteEchoHeadersServer = new Uri("ws://" + Host + "/" + EchoHeadersHandler);
            public static readonly Uri SecureRemoteEchoHeadersServer = new Uri("wss://" + SecureHost + "/" + EchoHeadersHandler);
            
            public static readonly object[][] EchoServers = { new object[] { RemoteEchoServer }, new object[] { SecureRemoteEchoServer } };
            public static readonly object[][] EchoHeadersServers = { new object[] { RemoteEchoHeadersServer }, new object[] { SecureRemoteEchoHeadersServer } };
        }
    }
}
