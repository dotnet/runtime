namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal static class StreamHelpers
    {
        private const ulong StreamTypeMask = 0x03;
        private const ulong StreamTypeUnidirectionalMask = 0x02;
        private const ulong StreamTypeServerInitiationMask = 0x01;

        internal const ulong MaxStreamId = QuicPrimitives.MaxVarintValue;
        internal const ulong MaxStreamIndex = MaxStreamId >> 2;
        internal const ulong MaxStreamOffset = QuicPrimitives.MaxVarintValue;

        internal static StreamType GetStreamType(long streamId)
        {
            return (StreamType)((ulong) streamId & StreamTypeMask);
        }

        internal static long GetStreamIndex(long streamId)
        {
            return streamId >> 2;
        }

        internal static bool IsBidirectional(long streamId)
        {
            return ((ulong) streamId & StreamTypeUnidirectionalMask) == 0;
        }

        internal static bool IsBidirectional(StreamType type)
        {
            return ((ulong) type & StreamTypeUnidirectionalMask) == 0;
        }

        internal static bool IsServerInitiated(long streamId)
        {
            return ((ulong) streamId & StreamTypeServerInitiationMask) != 0;
        }

        internal static bool IsReadable(bool isServer, long streamId)
        {
            return (isServer, GetStreamType(streamId)) switch
            {
                (true, var type) => type != StreamType.ClientInitiatedUnidirectional,
                (false, var type) => type != StreamType.ServerInitiatedUnidirectional,
            };
        }

        internal static StreamType GetLocallyInitiatedType(bool isServer, bool unidirectional)
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
