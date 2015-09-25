// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.Sockets.Tests
{
    internal static class TestPortBases
    {
        private const int Base = 8600;

        public const int AcceptAsync = Base;
        public const int AgnosticListener = Base + 10;
        public const int ConnectAsync = Base + 20;
        public const int ConnectEx = Base + 30;
        public const int Disconnect = Base + 40;
        public const int DisconnectAsync = Base + 50;
        public const int ReceiveMessageFrom = Base + 60;
        public const int DnsEndPoint = Base + 80;
        public const int ReceiveMessageFromAsync = Base + 90;
        public const int SendPacketsAsync = Base + 100;
        public const int Timeout = Base + 110;
        public const int DualMode = Base + 200;
        public const int UDPClient = Base + 500;
    }
}
