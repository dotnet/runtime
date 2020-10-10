namespace System.Net.Quic.Implementations.Managed.Internal.Tracing
{
    internal enum PacketLossTrigger
    {
        ReorderingThreshold,
        TimeThreshold,
        PtoExpired
    }
}