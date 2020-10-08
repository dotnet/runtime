namespace System.Net.Quic.Implementations.Managed
{
    internal enum EncryptionLevel
    {
        Initial,
        Handshake,
        Application,
        EarlyData,
        None,
    }
}
