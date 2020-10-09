// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal;

namespace System.Net.Quic.Tests.Harness
{
    using ImplFrame = Implementations.Managed.Internal.Frames.PathChallengeFrame;

    /// <summary>
    ///     Used to check reachability of the peer and validation of the path during connection migration.
    /// </summary>
    internal class PathChallengeFrame : FrameBase
    {
        /// <summary>
        ///     Arbitrary data to be repeated by the peer.
        /// </summary>
        internal long Data;

        /// <summary>
        ///     True if the frame is a challenge frame, otherwise it is a response frame.
        /// </summary>
        internal bool IsChallenge;

        internal override FrameType FrameType => IsChallenge ? FrameType.PathChallenge : FrameType.PathResponse;

        internal override void Serialize(QuicWriter writer)
        {
            ImplFrame.Write(writer, new ImplFrame(Data, IsChallenge));
        }

        internal override bool Deserialize(QuicReader reader)
        {
            if (!ImplFrame.Read(reader, out var frame))
                return false;

            Data = frame.Data;
            IsChallenge = frame.IsChallenge;

            return true;
        }
    }
}
