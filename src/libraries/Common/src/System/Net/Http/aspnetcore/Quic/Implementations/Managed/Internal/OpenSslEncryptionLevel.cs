namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal enum OpenSslEncryptionLevel
    {
        Initial = 0,
        EarlyData,
        Handshake,
        Application
    }
}
