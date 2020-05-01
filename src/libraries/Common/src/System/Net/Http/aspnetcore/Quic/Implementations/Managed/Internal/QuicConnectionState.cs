namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal enum QuicConnectionState
    {
        None,
        Connected,
        Draining,
        Closing,
        Closed
    }
}
