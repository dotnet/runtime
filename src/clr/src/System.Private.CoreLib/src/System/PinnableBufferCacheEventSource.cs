// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;

namespace System
{
    internal sealed class PinnableBufferCacheEventSource
    {
        public static readonly PinnableBufferCacheEventSource Log = new PinnableBufferCacheEventSource();

        public bool IsEnabled() { return false; }
        public void DebugMessage(string message) { }
        public void Create(string cacheName) { }
        public void AllocateBuffer(string cacheName, ulong objectId, int objectHash, int objectGen, int freeCountAfter) { }
        public void AllocateBufferFromNotGen2(string cacheName, int notGen2CountAfter) { }
        public void AllocateBufferCreatingNewBuffers(string cacheName, int totalBuffsBefore, int objectCount) { }
        public void AllocateBufferAged(string cacheName, int agedCount) { }
        public void AllocateBufferFreeListEmpty(string cacheName, int notGen2CountBefore) { }
        public void FreeBuffer(string cacheName, ulong objectId, int objectHash, int freeCountBefore) { }
        public void FreeBufferStillTooYoung(string cacheName, int notGen2CountBefore) { }
        public void TrimCheck(string cacheName, int totalBuffs, bool neededMoreThanFreeList, int deltaMSec) { }
        public void TrimFree(string cacheName, int totalBuffs, int freeListCount, int toBeFreed) { }
        public void TrimExperiment(string cacheName, int totalBuffs, int freeListCount, int numTrimTrial) { }
        public void TrimFreeSizeOK(string cacheName, int totalBuffs, int freeListCount) { }
        public void TrimFlush(string cacheName, int totalBuffs, int freeListCount, int notGen2CountBefore) { }
        public void AgePendingBuffersResults(string cacheName, int promotedToFreeListCount, int heldBackCount) { }
        public void WalkFreeListResult(string cacheName, int freeListCount, int gen0BuffersInFreeList) { }

        internal static ulong AddressOf(object obj)
        {
            return 0;
        }
    }
}
