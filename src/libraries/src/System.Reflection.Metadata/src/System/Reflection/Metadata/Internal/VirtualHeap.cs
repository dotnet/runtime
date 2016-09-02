// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection.Internal;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Reflection.Metadata.Ecma335
{
    internal struct VirtualHeapBlob
    {
        private GCHandle _pinned;
        private readonly byte[] _array;

        public VirtualHeapBlob(byte[] array)
        {
            _pinned = GCHandle.Alloc(array, GCHandleType.Pinned);
            _array = array;
        }

        public unsafe MemoryBlock GetMemoryBlock()
        {
            return new MemoryBlock((byte*)_pinned.AddrOfPinnedObject(), _array.Length);
        }

        public void Free()
        {
            _pinned.Free();
        }
    }

    // Container for virtual heap blobs that unpins handles on finalization.
    // This is not handled via dispose because the only resource is managed memory
    // and we don't have user visible disposable object that could own this memory.
    //
    // Since the number of virtual blobs we need is small (the number of attribute classes in .winmd files)
    // we can create a pinned handle for each of them.
    // If we needed many more blobs we could create and pin a single byte[] and allocate blobs there.
    internal sealed class VirtualHeap
    {
        public readonly Dictionary<uint, VirtualHeapBlob> Table;

        private VirtualHeap()
        {
            Table = new Dictionary<uint, VirtualHeapBlob>();
        }

        ~VirtualHeap()
        {
            if (Table != null)
            {
                foreach (var blob in Table.Values)
                {
                    blob.Free();
                }
            }
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
