namespace System.Net.Quic.Implementations.Managed.Internal.Tls
{
    internal sealed class  MockTlsFactory : TlsFactory
    {
        internal override ITls CreateClient(ManagedQuicConnection connection, QuicClientConnectionOptions options,
            TransportParameters localTransportParams) => new MockTls(connection, options, localTransportParams);


        internal override ITls CreateServer(ManagedQuicConnection connection, QuicListenerOptions options,
            TransportParameters localTransportParams) => new MockTls(connection, options, localTransportParams);
    }
}
