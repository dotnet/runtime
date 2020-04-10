namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal readonly struct PacketNumberRange
    {
        internal readonly long Start;
        internal readonly long End;

        public PacketNumberRange(long start, long end)
        {
            Start = start;
            End = end;
        }
    }
}
