// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Buffers
{
    /// <summary>
    /// Provides a mechanism for pinning and unpinning objects to prevent the GC from moving them.
    /// </summary>
    public interface IPinnable
    {
        /// <summary>
        /// Call this method to indicate that the IPinnable object can not be moved by the garbage collector.
        /// The address of the pinned object can be taken.
        /// <param name="elementIndex">The offset to the element within the memory at which the returned <see cref="MemoryHandle"/> points to.</param>
        /// </summary>
        MemoryHandle Pin(int elementIndex);

        /// <summary>
        /// Call this method to indicate that the IPinnable object no longer needs to be pinned.
        /// The garbage collector is free to move the object now.
        /// </summary>
        void Unpin();
    }
}
