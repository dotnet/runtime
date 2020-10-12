// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Frames;

namespace System.Net.Quic.Tests.Harness
{
    internal class PingFrame : SimpleFrame
    {
        public PingFrame() : base(FrameType.Ping)
        {
        }
    }

    internal class HandshakeDoneFrame : SimpleFrame
    {
        public HandshakeDoneFrame() : base(FrameType.HandshakeDone)
        {
        }
    }

    internal abstract class SimpleFrame : FrameBase
    {
        private FrameType type;

        protected SimpleFrame(FrameType frameType)
        {
            type = frameType;
        }

        internal override FrameType FrameType => type;

        internal override void Serialize(QuicWriter writer)
        {
            writer.WriteFrameType(FrameType);
        }

        internal override bool Deserialize(QuicReader reader)
        {
            type = reader.ReadFrameType();
            return true;
        }
    }
}
