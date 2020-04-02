using System.Collections.Generic;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal class CryptoStream
    {
        private int offset;
        private List<byte[]> data = new List<byte[]>();
        internal void Add(ReadOnlySpan<byte> data)
        {
            this.data.Add(data.ToArray());
        }

        internal (byte[] data, int streamOffset) PeekDataToSend()
        {
            var data = this.data[0];
            var streamOffset = offset;

            return (data, streamOffset);
        }

        internal (byte[] data, int streamOffset) GetDataToSend()
        {
            var data = this.data[0];
            this.data.RemoveAt(0);

            var streamOffset = offset;
            offset += data.Length;

            return (data, streamOffset);
        }

        internal int NextSizeToSend => data.Count > 0 ? data[0].Length : 0;
    }
}
