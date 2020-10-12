using System.Net.Quic.Implementations.Managed.Internal.Tls.OpenSsl;

namespace System.Net.Quic.Implementations.Managed.Internal.Tls
{
    internal abstract class TlsFactory
    {
        internal static readonly TlsFactory Instance =
            Environment.GetEnvironmentVariable("DOTNETQUIC_OPENSSL") != null
                ? (TlsFactory)new OpenSslTlsFactory()
                // : (TlsFactory)new OpenSslTlsFactory();
                : (TlsFactory) new MockTlsFactory();

        internal abstract ITls CreateClient(ManagedQuicConnection connection, QuicClientConnectionOptions options,
            TransportParameters localTransportParams);

        internal abstract ITls CreateServer(ManagedQuicConnection connection, QuicListenerOptions options,
            TransportParameters localTransportParams);
    }
}
