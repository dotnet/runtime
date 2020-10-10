namespace System.Net.Quic.Implementations.Managed.Internal.Tracing
{
    internal enum CongestionState
    {
        SlowStart,
        CongestionAvoidance,
        ApplicationLimited,
        Recovery
    }
}