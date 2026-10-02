// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Reflection.Internal;

namespace System.Reflection.Metadata
{
    internal sealed class PooledBlobBuilder : BlobBuilder
    {
        private const int PoolSize = 128;
        private const int ChunkSize = 1024;

        private static readonly ObjectPool<PooledBlobBuilder> s_chunkPool = new ObjectPool<PooledBlobBuilder>(() => new PooledBlobBuilder(), PoolSize);

        private PooledBlobBuilder()
            : base([], ChunkSize)
        {
        }

        public static PooledBlobBuilder GetInstance()
        {
            return s_chunkPool.Allocate();
        }

        protected override BlobBuilder AllocateChunk(int minimalSize)
        {
            PooledBlobBuilder builder = s_chunkPool.Allocate();
            builder.Buffer = ArrayPool<byte>.Shared.Rent(minimalSize);
            return builder;
        }

        protected override void FreeChunk()
        {
            ArrayPool<byte>.Shared.Return(Buffer);
            Buffer = [];
            s_chunkPool.Free(this);
        }

        protected override void OnLinking(BlobBuilder other)
        {
            if (other is not PooledBlobBuilder)
            {
                throw new InvalidOperationException("Cannot link with a non-pooled builder.");
            }
        }

        public new void Free()
        {
            base.Free();
        }
    }
}
