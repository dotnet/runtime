using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal;

namespace System.Net.Quic.Tests.Harness
{
    using ImplFrame = Implementations.Managed.Internal.Frames.CryptoFrame;
    /// <summary>
    ///     Used to transmit opaque cryptographic handshake messages.
    /// </summary>
    internal class CryptoFrame : FrameBase
    {
        /// <summary>
        ///     Byte offset of the stream carrying the cryptographic data.
        /// </summary>
        internal ulong Offset;

        /// <summary>
        ///     Cryptographic message data;
        /// </summary>
        internal byte[] CryptoData;

        internal override FrameType FrameType => FrameType.Crypto;

        protected override string GetAdditionalInfo() => $"[Off={Offset}, Len={CryptoData.Length}]";

        internal override void Serialize(QuicWriter writer)
        {
            ImplFrame.Write(writer, new ImplFrame(Offset, CryptoData));
        }

        internal override bool Deserialize(QuicReader reader)
        {
            if (!ImplFrame.Read(reader, out var frame))
                return false;

            Offset = frame.Offset;
            CryptoData = frame.CryptoData.ToArray();

            return true;
        }
    }
}
