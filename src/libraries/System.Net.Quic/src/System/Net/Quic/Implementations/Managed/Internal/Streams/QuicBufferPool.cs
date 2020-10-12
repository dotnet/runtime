// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace System.Net.Quic.Implementations.Managed.Internal.Streams
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
