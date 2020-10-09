// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal;

namespace System.Net.Quic.Tests.Harness
{
    using ImplFrame = Implementations.Managed.Internal.Frames.MaxStreamsFrame;

    /// <summary>
    ///     Informs peer about cumulative number of streams of a given type it is permitted to open.
    /// </summary>
    internal class MaxStreamsFrame : FrameBase
    {
        /// <summary>
        ///     Count of the cumulative number of streams of the corresponding type that can be opened over the lifetime of the
        ///     connection.
        /// </summary>
        internal long MaximumStreams;

        /// <summary>
        ///     True if <see cref="MaximumStreams" /> is intended for bidirectional streams. Otherwise the count is meant as
        ///     unidirectional streams.
        /// </summary>
        internal bool Bidirectional;

        internal override FrameType FrameType =>
            Bidirectional ? FrameType.MaxStreamsBidirectional : FrameType.MaxStreamsUnidirectional;

        internal override void Serialize(QuicWriter writer)
        {
            ImplFrame.Write(writer, new ImplFrame(MaximumStreams, Bidirectional));
        }

        internal override bool Deserialize(QuicReader reader)
        {
            if (!ImplFrame.Read(reader, out var frame))
                return false;

            MaximumStreams = frame.MaximumStreams;
            Bidirectional = frame.Bidirectional;

            return true;
        }
    }
}
