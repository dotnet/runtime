// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Frames;

namespace System.Net.Quic.Tests.Harness
{
    using ImplFrame = Implementations.Managed.Internal.Frames.DataBlockedFrame;

    /// <summary>
    ///     Indicates that the peer has data to send but is blocked by the flow control limits.
    /// </summary>
    internal class DataBlockedFrame : FrameBase
    {
        /// <summary>
        ///     Connection-level limit at which the blocking occured.
        /// </summary>
        internal long DataLimit;

        internal override FrameType FrameType => FrameType.DataBlocked;

        internal override void Serialize(QuicWriter writer)
        {
            ImplFrame.Write(writer, new ImplFrame(DataLimit));
        }

        internal override bool Deserialize(QuicReader reader)
        {
            if (!ImplFrame.Read(reader, out var frame))
                return false;

            DataLimit = frame.DataLimit;

            return true;
        }
    }
}
