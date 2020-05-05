#nullable enable

using System.Linq;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Tests.Harness;
using Xunit;

namespace System.Net.Quic.Tests
{
    internal static class TestHelpers
    {
        public static TFrame ShouldHaveFrame<TFrame>(this InitialPacket packet) where TFrame : FrameBase
        {
            return ShouldHaveFrameCommonImpl<TFrame, InitialPacket>(packet);
        }
        private static TFrame ShouldHaveFrameCommonImpl<TFrame, TPacket>(this TPacket packet) where TFrame : FrameBase where TPacket : CommonPacket
        {
            var frame = packet.Frames.OfType<TFrame>().SingleOrDefault();
            Assert.True(frame != null, $"Packet does not contain {typeof(TFrame).Name}s.");
            return frame!;
        }

        public static TFrame ShouldHaveFrame<TFrame>(this OneRttPacket packet) where TFrame : FrameBase
        {
            var frame = packet.Frames.OfType<TFrame>().SingleOrDefault();
            Assert.True(frame != null, $"Packet does not contain {typeof(TFrame).Name}s, but was expected to.");
            return frame!;
        }

        public static TFrame ShouldNotHaveFrame<TFrame>(this OneRttPacket packet) where TFrame : FrameBase
        {
            var frame = packet.Frames.OfType<TFrame>().SingleOrDefault();
            Assert.True(frame == null, $"Packet does contain {typeof(TFrame).Name}, but was expected not to.");
            return frame!;
        }

        public static void ShouldContainConnectionClose(this OneRttPacket packet, TransportErrorCode error,
            string? reason = null, FrameType frameType = FrameType.Padding)
        {
            var frame = packet.ShouldHaveFrame<ConnectionCloseFrame>();

            Assert.Equal(frame.ErrorCode, error);
            if (reason != null)
                Assert.Equal(frame.ReasonPhrase, reason);
            if (frameType != FrameType.Padding)
                Assert.Equal(frame.ErrorFrameType, frameType);
        }
    }
}
