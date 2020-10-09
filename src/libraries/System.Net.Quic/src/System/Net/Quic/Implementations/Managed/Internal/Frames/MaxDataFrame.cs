// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Net.Quic.Implementations.Managed.Internal.Frames
{
    /// <summary>
    ///     Used in flow control to inform the peer of the maximmum amount of data that can be sent on the connection as a
    ///     whole.
    /// </summary>
    internal readonly struct MaxDataFrame
    {
        /// <summary>
        ///     Maximum amount of data that can be sent on the entire connection in bytes.
        /// </summary>
        internal readonly long MaximumData;

        internal MaxDataFrame(long maximumData)
        {
            MaximumData = maximumData;
        }

        internal int GetSerializedLength()
        {
            return 1 +
                   QuicPrimitives.GetVarIntLength(MaximumData);
        }

        internal static bool Read(QuicReader reader, out MaxDataFrame frame)
        {
            var type = reader.ReadFrameType();
            Debug.Assert(type == FrameType.MaxData);

            if (!reader.TryReadVarInt(out long maxData))
            {
                frame = default;
                return false;
            }

            frame = new MaxDataFrame(maxData);
            return true;
        }

        internal static void Write(QuicWriter writer, MaxDataFrame frame)
        {
            Debug.Assert(writer.BytesAvailable >= frame.GetSerializedLength());

            writer.WriteFrameType(FrameType.MaxData);

            writer.WriteVarInt(frame.MaximumData);
        }
    }
}
