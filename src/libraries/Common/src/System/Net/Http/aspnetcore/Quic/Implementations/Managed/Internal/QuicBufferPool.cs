using System.Collections.Concurrent;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal abstract class QuicBufferPool
    {
        internal const int BufferSize = 32 * 1024;

        private static ConcurrentStack<byte[]> _stack = new ConcurrentStack<byte[]>();

        internal static byte[] Rent()
        {
            if (!_stack.TryPop(out var ar))
                ar = new byte[BufferSize];

            return ar;
        }

        internal static void Return(byte[] buffer)
        {
            _stack.Push(buffer);
        }
    }
}
