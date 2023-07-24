// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.NetworkInformation
{
    internal sealed class SimpleTcpConnectionInformation : TcpConnectionInformation
    {
        public SimpleTcpConnectionInformation(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, TcpState state)
        {
            LocalEndPoint = localEndPoint;
            RemoteEndPoint = remoteEndPoint;
            State = state;
        }

        public override IPEndPoint LocalEndPoint { get; }

        public override IPEndPoint RemoteEndPoint { get; }

        public override TcpState State { get; }
    }
}
