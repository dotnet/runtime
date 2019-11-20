using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;

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
