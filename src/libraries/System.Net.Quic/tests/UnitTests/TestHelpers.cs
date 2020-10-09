// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Linq;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Tests.Harness;
using Xunit;

namespace System.Net.Quic.Tests
{
    internal static class TestHelpers
    {
        public static TFrame ShouldHaveFrame<TFrame>(this IFramePacket packet) where TFrame : FrameBase
        {
            var frame = packet.Frames.OfType<TFrame>().SingleOrDefault();
            Assert.True(frame != null, $"Packet does not contain {typeof(TFrame).Name}s.");
            return frame!;
        }

        public static void ShouldNotHaveFrame<TFrame>(this IFramePacket packet) where TFrame : FrameBase
        {
            var frame = packet.Frames.OfType<TFrame>().SingleOrDefault();
            Assert.True(frame == null, $"Packet does contain {typeof(TFrame).Name}, but was expected not to.");
        }

        public static void ShouldHaveConnectionClose(this IFramePacket packet, TransportErrorCode error,
            string? reason = null, FrameType frameType = FrameType.Padding)
        {
            var frame = packet.ShouldHaveFrame<ConnectionCloseFrame>();

            Assert.Equal(error, frame.ErrorCode);
            // if (reason != null)
                Assert.Equal(reason, frame.ReasonPhrase);
            // if (frameType != FrameType.Padding)
                Assert.Equal(frameType, frame.ErrorFrameType);
        }
    }
}
