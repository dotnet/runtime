namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal enum SslEncryptionLevel
    {
        Initial = 0,
        EarlyData,
        Handshake,
        Application
    }
}
