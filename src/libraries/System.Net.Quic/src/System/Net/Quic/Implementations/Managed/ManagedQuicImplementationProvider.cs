namespace System.Net.Quic.Implementations.Managed
{
    internal class ManagedQuicImplementationProvider : QuicImplementationProvider
    {
        public override bool IsSupported => Interop.OpenSslQuic.IsSupported();

        internal override QuicListenerProvider CreateListener(QuicListenerOptions options) => new ManagedQuicListener(options);

        internal override QuicConnectionProvider CreateConnection(QuicClientConnectionOptions options) => new ManagedQuicConnection(options);
    }
}
