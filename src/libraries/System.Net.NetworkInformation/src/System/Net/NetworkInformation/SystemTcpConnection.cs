// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.NetworkInformation
{
    // Represents an active TCP connection.
    internal sealed class SystemTcpConnectionInformation : TcpConnectionInformation
    {
        private readonly IPEndPoint _localEndPoint;
        private readonly IPEndPoint _remoteEndPoint;
        private readonly TcpState _state;

        internal SystemTcpConnectionInformation(in Interop.IpHlpApi.MibTcpRow row)
        {
            _state = row.State;
            _localEndPoint = row.LocalEndPoint;
            _remoteEndPoint = row.RemoteEndPoint;
        }

        // IPV6 version of the Tcp row.
        internal SystemTcpConnectionInformation(in Interop.IpHlpApi.MibTcp6RowOwnerPid row)
        {
            _state = row.State;
            _localEndPoint = row.LocalEndPoint;
            _remoteEndPoint = row.RemoteEndPoint;
        }

        public override TcpState State { get { return _state; } }

        public override IPEndPoint LocalEndPoint { get { return _localEndPoint; } }

        public override IPEndPoint RemoteEndPoint { get { return _remoteEndPoint; } }
    }
}
