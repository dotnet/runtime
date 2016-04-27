// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Reflection.Internal;

namespace System.Reflection.Metadata
{
    internal sealed class PooledBlobBuilder : BlobBuilder
    {
        private const int PoolSize = 128;
        private const int ChunkSize = 1024;

        private static ObjectPool<PooledBlobBuilder> s_chunkPool = new ObjectPool<PooledBlobBuilder>(() => new PooledBlobBuilder(ChunkSize), PoolSize);

        private PooledBlobBuilder(int size)
            : base(size)
        {
        }

        public static PooledBlobBuilder GetInstance()
        {
            return s_chunkPool.Allocate();
        }

        protected override BlobBuilder AllocateChunk(int minimalSize)
        {
            if (minimalSize <= ChunkSize)
            {
                return s_chunkPool.Allocate();
            }

            return new BlobBuilder(minimalSize);
        }

        protected override void FreeChunk()
        {
            s_chunkPool.Free(this);
        }

        public new void Free()
        {
            base.Free();
        }
    }
}
