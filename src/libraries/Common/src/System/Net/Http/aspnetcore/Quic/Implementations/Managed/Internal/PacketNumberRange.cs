namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal readonly struct PacketNumberRange
    {
        internal readonly ulong Start;
        internal readonly ulong End;

        public PacketNumberRange(ulong start, ulong end)
        {
            Start = start;
            End = end;
        }
    }
}
