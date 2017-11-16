// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335
{
    internal readonly struct GuidHeap
    {
        internal readonly MemoryBlock Block;

        public GuidHeap(MemoryBlock block)
        {
            this.Block = block;
        }

        internal Guid GetGuid(GuidHandle handle)
        {
            if (handle.IsNil)
            {
                return default(Guid);
            }

            // Metadata Spec: The Guid heap is an array of GUIDs, each 16 bytes wide. 
            // Its first element is numbered 1, its second 2, and so on.
            return this.Block.PeekGuid((handle.Index - 1) * BlobUtilities.SizeOfGuid);
        }
    }
}
