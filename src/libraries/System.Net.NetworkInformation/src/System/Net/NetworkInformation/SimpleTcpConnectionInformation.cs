// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.NetworkInformation
{
    internal class SimpleTcpConnectionInformation : TcpConnectionInformation
    {
        private IPEndPoint _localEndPoint;
        private IPEndPoint _remoteEndPoint;
        private TcpState _state;

        public SimpleTcpConnectionInformation(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, TcpState state)
        {
            _localEndPoint = localEndPoint;
            _remoteEndPoint = remoteEndPoint;
            _state = state;
        }

        public override IPEndPoint LocalEndPoint { get { return _localEndPoint; } }

        public override IPEndPoint RemoteEndPoint { get { return _remoteEndPoint; } }

        public override TcpState State { get { return _state; } }
    }
}
