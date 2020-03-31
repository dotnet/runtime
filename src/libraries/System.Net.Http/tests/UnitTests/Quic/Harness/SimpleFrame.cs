using System.Net.Quic.Implementations.Managed.Internal;

namespace System.Net.Quic.Tests.Harness
{
    internal class SimpleFrame : FrameBase
    {
        private FrameType type;

        public SimpleFrame(FrameType frameType)
        {
            type = frameType;
        }

        public SimpleFrame()
        {
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
