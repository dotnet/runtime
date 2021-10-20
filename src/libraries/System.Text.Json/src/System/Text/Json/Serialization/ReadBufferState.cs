// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace System.Text.Json.Serialization
{
    internal struct ReadBufferState : IDisposable
    {
        public byte[] Buffer;
        public int BytesInBuffer;
        public int ClearMax;
        public bool IsFirstIteration;
        public bool IsFinalBlock;

        public ReadBufferState(int defaultBufferSize)
        {
            Buffer = ArrayPool<byte>.Shared.Rent(Math.Max(defaultBufferSize, JsonConstants.Utf8Bom.Length));
            BytesInBuffer = ClearMax = 0;
            IsFirstIteration = true;
            IsFinalBlock = false;
        }

        public void Dispose()
        {
            // Clear only what we used and return the buffer to the pool
            new Span<byte>(Buffer, 0, ClearMax).Clear();

            byte[] toReturn = Buffer;
            Buffer = null!;

            ArrayPool<byte>.Shared.Return(toReturn);
        }
    }
}
