// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal;

namespace System.Net.Quic.Tests.Harness
{
    using ImplFrame = Implementations.Managed.Internal.Frames.StreamDataBlockedFrame;

    /// <summary>
    ///     Indicates that the peer has data to send on particular stream but is unable to do so due to stream-level flow
    ///     control.
    /// </summary>
    internal class StreamDataBlockedFrame : FrameBase
    {
        /// <summary>
        ///     Id of the blocked stream.
        /// </summary>
        internal long StreamId;

        /// <summary>
        ///     Offset in the stream at which the blocking occured.
        /// </summary>
        internal long StreamDataLimit;

        internal override FrameType FrameType => FrameType.StreamDataBlocked;

        internal override void Serialize(QuicWriter writer)
        {
            ImplFrame.Write(writer, new ImplFrame(StreamId, StreamDataLimit));
        }

        internal override bool Deserialize(QuicReader reader)
        {
            if (!ImplFrame.Read(reader, out var frame))
                return false;

            StreamId = frame.StreamId;
            StreamDataLimit = frame.StreamDataLimit;

            return true;
        }
    }
}
