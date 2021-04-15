// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Buffers
{
    /// <summary>Provides a mechanism for pinning and unpinning objects to prevent the garbage collector from moving them.</summary>
    /// <remarks>The <see cref="System.Buffers.MemoryManager{T}" /> class implements the <see cref="IPinnable" /> interface.</remarks>
    public interface IPinnable
    {
        /// <summary>Pins a block of memory.</summary>
        /// <param name="elementIndex">The offset to the element within the memory buffer to which the returned <see cref="System.Buffers.MemoryHandle" /> points.</param>
        /// <returns>A handle to the block of memory.</returns>
        /// <remarks>A developer can access an object that implements the <see cref="System.Buffers.IPinnable" /> interface without pinning it only through managed APIs. Pinning is required for access by unmanaged APIs.
        /// Call this method to indicate that the <see cref="System.Buffers.IPinnable" /> object cannot be moved by the garbage collector so that the address of the pinned object can be used.</remarks>
        MemoryHandle Pin(int elementIndex);

        /// <summary>Frees a block of pinned memory.</summary>
        /// <remarks>Call this method to indicate that the <see cref="System.Buffers.IPinnable" /> object no longer needs to be pinned, and that the garbage collector can now move the object.</remarks>
        void Unpin();
    }
}
