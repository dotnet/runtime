// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Net.Security;

namespace System.Net.Quic.Implementations.MsQuic
{
    internal sealed class MsQuicImplementationProvider : QuicImplementationProvider
    {
        private MsQuicSession _clientSession = new MsQuicSession("client");
        private MsQuicSession _serverSession = new MsQuicSession("server");

        internal override QuicListenerProvider CreateListener(IPEndPoint listenEndPoint, SslServerAuthenticationOptions sslServerAuthenticationOptions)
        {
            return _serverSession.ListenerOpen(listenEndPoint, sslServerAuthenticationOptions);
        }

        internal override QuicConnectionProvider CreateConnection(IPEndPoint remoteEndPoint, SslClientAuthenticationOptions sslClientAuthenticationOptions, IPEndPoint localEndPoint)
        {
            return _clientSession.ConnectionOpen(remoteEndPoint, sslClientAuthenticationOptions, localEndPoint);
        }
    }
}
