using System.Diagnostics;

namespace System.Net.Quic.Implementations.Managed.Internal.Frames
{
    /// <summary>
    ///     Used to check reachability of the peer and validation of the path during connection migration.
    /// </summary>
    internal readonly struct PathChallengeFrame
    {
        /// <summary>
        ///     Arbitrary data to be repeated by the peer.
        /// </summary>
        internal readonly long Data;

        /// <summary>
        ///     True if the frame is a challenge frame, otherwise it is a response frame.
        /// </summary>
        internal readonly bool IsChallenge;

        internal PathChallengeFrame(long data, bool isChallenge)
        {
            Data = data;
            IsChallenge = isChallenge;
        }

        internal int GetSerializedLength()
        {
            return 1 +
                   sizeof(long);
        }

        internal static bool Read(QuicReader reader, out PathChallengeFrame frame)
        {
            var type = reader.ReadFrameType();
            Debug.Assert(type == FrameType.PathChallenge || type == FrameType.PathResponse);

            if (!reader.TryReadUInt64(out long data))
            {
                frame = default;
                return false;
            }

            frame = new PathChallengeFrame(data, type == FrameType.PathChallenge);
            return true;
        }

        internal static void Write(QuicWriter writer, in PathChallengeFrame frame)
        {
            Debug.Assert(writer.BytesAvailable >= frame.GetSerializedLength());

            writer.WriteFrameType(frame.IsChallenge ? FrameType.PathChallenge : FrameType.PathResponse);

            writer.WriteUInt64(frame.Data);
        }
    }
}
