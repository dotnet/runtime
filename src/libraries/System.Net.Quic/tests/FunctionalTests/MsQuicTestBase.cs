using System.Collections.Generic;
using System.Net.Security;

namespace System.Net.Quic.Tests
{
    public class MsQuicTestBase : IDisposable
    {
        public MsQuicTestBase()
        {
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Loopback, 8000);
            DefaultListener = CreateQuicListener(endpoint);
        }

        public QuicListener DefaultListener { get; }

        public SslServerAuthenticationOptions GetSslServerAuthenticationOptions()
        {
            return new SslServerAuthenticationOptions()
            {
                ApplicationProtocols = new List<SslApplicationProtocol>() { new SslApplicationProtocol("quictest") }
            };
        }

        public SslClientAuthenticationOptions GetSslClientAuthenticationOptions()
        {
            return new SslClientAuthenticationOptions()
            {
                ApplicationProtocols = new List<SslApplicationProtocol>() { new SslApplicationProtocol("quictest") }
            };
        }

        public QuicConnection CreateQuicConnection(IPEndPoint endpoint)
        {
            return new QuicConnection(QuicImplementationProviders.MsQuic, endpoint, GetSslClientAuthenticationOptions());
        }

        public QuicListener CreateQuicListener(IPEndPoint endpoint)
        {
            QuicListener listener = new QuicListener(QuicImplementationProviders.MsQuic, endpoint, GetSslServerAuthenticationOptions());
            listener.Start();
            return listener;
        }

        public void Dispose()
        {
            DefaultListener.Dispose();
        }
    }
}
