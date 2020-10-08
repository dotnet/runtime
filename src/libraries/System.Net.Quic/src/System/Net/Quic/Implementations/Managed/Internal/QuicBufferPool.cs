using System.Buffers;
using System.Collections.Concurrent;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal static class QuicBufferPool
    {
        private static readonly ArrayPool<byte> _pool = ArrayPool<byte>.Create(BufferSize, 64);
        internal const int BufferSize = 32 * 1024;

        internal static byte[] Rent()
        {
            return _pool.Rent(BufferSize);
        }

        internal static void Return(byte[] buffer)
        {
            _pool.Return(buffer);
        }
    }
}
