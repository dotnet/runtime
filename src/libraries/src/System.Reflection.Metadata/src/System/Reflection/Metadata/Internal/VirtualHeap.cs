// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection.Internal;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Reflection.Metadata.Ecma335
{
    // Container for virtual heap blobs that unpins handles on finalization.
    // This is not handled via dispose because the only resource is managed memory
    // and we don't have user visible disposable object that could own this memory.
    //
    // Since the number of virtual blobs we need is small (the number of attribute classes in .winmd files)
    // we can create a pinned handle for each of them.
    // If we needed many more blobs we could create and pin a single byte[] and allocate blobs there.
    internal sealed class VirtualHeap : CriticalDisposableObject
    {
        private struct PinnedBlob
        {
            // can't be read-only since GCHandle is a mutable struct
            public GCHandle Handle;

            public readonly int Length;

            public PinnedBlob(GCHandle handle, int length)
            {
                Handle = handle;
                Length = length;
            }

            public unsafe MemoryBlock GetMemoryBlock() => 
                new MemoryBlock((byte*)Handle.AddrOfPinnedObject(), Length);
        }

        // maps raw value of StringHandle or BlobHandle to the corresponding pinned array
        private Dictionary<uint, PinnedBlob> _blobs;

        private VirtualHeap()
        {
            _blobs = new Dictionary<uint, PinnedBlob>();
        }

        protected override void Release()
        {
            // Make sure the current thread isn't aborted in the middle of the operation.
#if !NETSTANDARD11
            RuntimeHelpers.PrepareConstrainedRegions();
#endif
            try
            {
            }
            finally
            {
                var blobs = Interlocked.Exchange(ref _blobs, null);

                if (blobs != null)
                {
                    foreach (var blobPair in blobs)
                    {
                        blobPair.Value.Handle.Free();
                    }
                }
            }
        }

        private Dictionary<uint, PinnedBlob> GetBlobs()
        {
            var blobs = _blobs;
            if (blobs == null)
            {
                throw new ObjectDisposedException(nameof(VirtualHeap));
            }

            return blobs;
        }

        public bool TryGetMemoryBlock(uint rawHandle, out MemoryBlock block)
        {
            if (!GetBlobs().TryGetValue(rawHandle, out var blob))
            {
                block = default(MemoryBlock);
                return false;
            }

            block = blob.GetMemoryBlock();
            return true;
        }

        internal MemoryBlock AddBlob(uint rawHandle, byte[] value)
        {
            var blobs = GetBlobs();

            MemoryBlock result;
#if !NETSTANDARD11
            RuntimeHelpers.PrepareConstrainedRegions();
#endif
            try
            {
            }
            finally
            {
                var blob = new PinnedBlob(GCHandle.Alloc(value, GCHandleType.Pinned), value.Length);
                blobs.Add(rawHandle, blob);
                result = blob.GetMemoryBlock();
            }

            return result;
        }

        internal static VirtualHeap GetOrCreateVirtualHeap(ref VirtualHeap lazyHeap)
        {
            if (lazyHeap == null)
            {
                Interlocked.CompareExchange(ref lazyHeap, new VirtualHeap(), null);
            }

            return lazyHeap;
        }
    }
}
