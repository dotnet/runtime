namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal static class StreamHelpers
    {
        private const ulong StreamTypeMask = 0x03;
        private const ulong StreamTypeUnidirectionalMask = 0x02;
        private const ulong StreamTypeServerInitiationMask = 0x01;

        internal static StreamType GetStreamType(long streamId)
        {
            return (StreamType)((ulong) streamId & StreamTypeMask);
        }

        internal static bool IsBidirectional(long streamId)
        {
            return ((ulong) streamId & StreamTypeUnidirectionalMask) == 0;
        }

        internal static bool IsServerInitiated(long streamId)
        {
            return ((ulong) streamId & StreamTypeServerInitiationMask) != 0;
        }

        internal static StreamType GetOutboundType(bool isServer, bool unidirectional)
        {
            return (StreamType)
                ((isServer ? StreamTypeServerInitiationMask : 0) |
                 (unidirectional ? StreamTypeUnidirectionalMask : 0));
        }

        internal static long ComposeStreamId(StreamType type, long index)
        {
            return (long)type | (index << 2);
        }
    }
}
