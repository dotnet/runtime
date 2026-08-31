// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection.PortableExecutable;

namespace System.Reflection.Internal
{
    internal abstract class MemoryBlockProvider : IDisposable
    {
        /// <summary>
        /// Creates and hydrates a memory block representing all data.
        /// </summary>
        /// <exception cref="IOException">Error while reading from the memory source.</exception>
        public AbstractMemoryBlock GetMemoryBlock()
        {
            return GetMemoryBlockImpl(0, Size);
        }

        /// <summary>
        /// Creates and hydrates a memory block representing data in the specified range.
        /// </summary>
        /// <param name="start">Starting offset relative to the beginning of the data represented by this provider.</param>
        /// <param name="size">Size of the resulting block.</param>
        /// <exception cref="IOException">Error while reading from the memory source.</exception>
        public AbstractMemoryBlock GetMemoryBlock(int start, int size)
        {
            // Add cannot overflow as it is the sum of two 32-bit values done in 64 bits.
            // Negative start or size is handle by overflow to greater than maximum size = int.MaxValue.
            if ((ulong)(unchecked((uint)start)) + unchecked((uint)size) > (ulong)this.Size)
            {
                Throw.ImageTooSmallOrContainsInvalidOffsetOrCount();
            }

            return GetMemoryBlockImpl(start, size);
        }

        /// <exception cref="IOException">IO error while reading from the underlying stream.</exception>
        protected abstract AbstractMemoryBlock GetMemoryBlockImpl(int start, int size);

        /// <summary>
        /// Gets the <see cref="Stream"/> backing the <see cref="MemoryBlockProvider"/>, if there is one.
        /// </summary>
        /// <remarks>
        /// It is the caller's responsibility to use <paramref name="stream"/> only
        /// while locking on <paramref name="streamGuard"/>, and not read outside the
        /// bounds defined by <paramref name="imageStart"/> and <paramref name="imageSize"/>.
        /// </remarks>
        public virtual bool TryGetUnderlyingStream([NotNullWhen(true)] out Stream? stream, out long imageStart, out int imageSize, [NotNullWhen(true)] out object? streamGuard)
        {
            stream = null;
            imageStart = 0;
            imageSize = 0;
            streamGuard = null;
            return false;
        }

        /// <summary>
        /// The size of the data.
        /// </summary>
        public abstract int Size { get; }

        protected abstract void Dispose(bool disposing);

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
