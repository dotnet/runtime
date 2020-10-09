// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal;

namespace System.Net.Quic.Tests.Harness
{
    using ImplFrame = Implementations.Managed.Internal.Frames.MaxDataFrame;

    /// <summary>
    ///     Used in flow control to inform the peer of the maximmum amount of data that can be sent on the connection as a
    ///     whole.
    /// </summary>
    internal class MaxDataFrame : FrameBase
    {
        /// <summary>
        ///     Maximum amount of data that can be sent on the entire connection in bytes.
        /// </summary>
        internal long MaximumData;

        internal override FrameType FrameType => FrameType.MaxData;

        internal override void Serialize(QuicWriter writer)
        {
            ImplFrame.Write(writer, new ImplFrame(MaximumData));
        }

        internal override bool Deserialize(QuicReader reader)
        {
            if (!ImplFrame.Read(reader, out var frame))
                return false;

            MaximumData = frame.MaximumData;

            return true;
        }
    }
}
