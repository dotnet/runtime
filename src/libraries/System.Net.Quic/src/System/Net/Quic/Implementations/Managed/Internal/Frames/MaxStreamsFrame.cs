// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Net.Quic.Implementations.Managed.Internal.Frames
{
    /// <summary>
    ///     Informs peer about cumulative number of streams of a given type it is permitted to open.
    /// </summary>
    internal readonly struct MaxStreamsFrame
    {
        /// <summary>
        ///     Count of the cumulative number of streams of the corresponding type that can be opened over the lifetime of the
        ///     connection.
        /// </summary>
        internal readonly long MaximumStreams;

        /// <summary>
        ///     True if <see cref="MaximumStreams" /> is intended for bidirectional streams. Otherwise the count is meant as
        ///     unidirectional streams.
        /// </summary>
        internal readonly bool Bidirectional;

        public MaxStreamsFrame(long maximumStreams, bool bidirectional)
        {
            MaximumStreams = maximumStreams;
            Bidirectional = bidirectional;
        }

        internal int GetSerializedLength()
        {
            return 1 +
                   QuicPrimitives.GetVarIntLength(MaximumStreams);
        }

        internal static bool Read(QuicReader reader, out MaxStreamsFrame frame)
        {
            var type = reader.ReadFrameType();
            Debug.Assert(type == FrameType.MaxStreamsBidirectional || type == FrameType.MaxStreamsUnidirectional);

            if (!reader.TryReadVarInt(out long maxStreams) ||
                maxStreams > StreamHelpers.MaxStreamIndex)
            {
                frame = default;
                return false;
            }

            frame = new MaxStreamsFrame(maxStreams, type == FrameType.MaxStreamsBidirectional);
            return true;
        }

        internal static void Write(QuicWriter writer, in MaxStreamsFrame frame)
        {
            Debug.Assert(writer.BytesAvailable >= frame.GetSerializedLength());

            writer.WriteFrameType(frame.Bidirectional
                ? FrameType.MaxStreamsBidirectional
                : FrameType.MaxStreamsUnidirectional);

            writer.WriteVarInt(frame.MaximumStreams);
        }
    }
}
