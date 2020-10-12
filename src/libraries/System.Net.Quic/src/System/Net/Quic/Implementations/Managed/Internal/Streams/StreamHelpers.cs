// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Quic.Implementations.Managed.Internal.Streams
{
    internal static class StreamHelpers
    {
        private const long StreamTypeMask = 0x03;
        private const long StreamTypeUnidirectionalMask = 0x02;
        private const long StreamTypeServerInitiationMask = 0x01;

        internal const long MaxStreamId = QuicPrimitives.MaxVarIntValue;
        internal const long MaxStreamIndex = MaxStreamId >> 2;
        internal const long MaxStreamOffset = QuicPrimitives.MaxVarIntValue;

        internal static StreamType GetStreamType(long streamId)
        {
            return (StreamType)(streamId & StreamTypeMask);
        }

        internal static long GetStreamIndex(long streamId)
        {
            return streamId >> 2;
        }

        internal static bool IsBidirectional(long streamId)
        {
            return (streamId & StreamTypeUnidirectionalMask) == 0;
        }

        internal static bool IsBidirectional(StreamType type)
        {
            return ((long) type & StreamTypeUnidirectionalMask) == 0;
        }

        internal static bool IsServerInitiated(long streamId)
        {
            return (streamId & StreamTypeServerInitiationMask) != 0;
        }

        internal static bool CanRead(bool isServer, long streamId)
        {
            return (isServer, GetStreamType(streamId)) switch
            {
                (true, var type) => type != StreamType.ServerInitiatedUnidirectional,
                (false, var type) => type != StreamType.ClientInitiatedUnidirectional,
            };
        }

        internal static bool CanWrite(bool isServer, long streamId)
        {
            // what is readable for client, is writable for server and vice versa
            return CanRead(!isServer, streamId);
        }

        internal static bool IsLocallyInitiated(bool isServer, long streamId)
        {
            return isServer == IsServerInitiated(streamId);
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
