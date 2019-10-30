// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.NetworkInformation
{
    // Represents an active TCP connection.
    internal class SystemTcpConnectionInformation : TcpConnectionInformation
    {
        private readonly IPEndPoint _localEndPoint;
        private readonly IPEndPoint _remoteEndPoint;
        private readonly TcpState _state;

        internal SystemTcpConnectionInformation(Interop.IpHlpApi.MibTcpRow row)
        {
            _state = row.state;

            // Port is returned in Big-Endian - most significant bit on left.
            // Unfortunately, its done at the word level and not the DWORD level.
            int localPort = row.localPort1 << 8 | row.localPort2;
            int remotePort = ((_state == TcpState.Listen) ? 0 : row.remotePort1 << 8 | row.remotePort2);

            _localEndPoint = new IPEndPoint(row.localAddr, (int)localPort);
            _remoteEndPoint = new IPEndPoint(row.remoteAddr, (int)remotePort);
        }

        // IPV6 version of the Tcp row.
        internal SystemTcpConnectionInformation(Interop.IpHlpApi.MibTcp6RowOwnerPid row)
        {
            _state = row.state;

            // Port is returned in Big-Endian - most significant bit on left.
            // Unfortunately, its done at the word level and not the DWORD level.
            int localPort = row.localPort1 << 8 | row.localPort2;
            int remotePort = ((_state == TcpState.Listen) ? 0 : row.remotePort1 << 8 | row.remotePort2);

            _localEndPoint = new IPEndPoint(new IPAddress(row.localAddr, row.localScopeId), (int)localPort);
            _remoteEndPoint = new IPEndPoint(new IPAddress(row.remoteAddr, row.remoteScopeId), (int)remotePort);
        }

        public override TcpState State { get { return _state; } }

        public override IPEndPoint LocalEndPoint { get { return _localEndPoint; } }

        public override IPEndPoint RemoteEndPoint { get { return _remoteEndPoint; } }
    }
}
