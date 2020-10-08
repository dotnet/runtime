using System.Collections.Generic;

namespace System.Net.Quic.Tests.Harness
{
    internal interface IFramePacket
    {
        List<FrameBase> Frames { get; }
    }
}
