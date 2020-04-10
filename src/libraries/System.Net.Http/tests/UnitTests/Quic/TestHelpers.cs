#nullable enable

using System.Linq;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Tests.Harness;
using Xunit;

namespace System.Net.Quic.Tests
{
    internal static class TestHelpers
    {
        public static TFrame ShouldHaveFrame<TFrame>(this OneRttPacket packet) where TFrame : FrameBase
        {
            var frame = packet.Frames.OfType<TFrame>().SingleOrDefault();
            Assert.True(frame != null, $"Packet does not contain {typeof(TFrame).Name}s.");
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
