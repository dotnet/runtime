// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace System.Reflection.Internal
{
    /// <summary>
    /// Represents a disposable blob of memory accessed via unsafe pointer.
    /// </summary>
    internal abstract class AbstractMemoryBlock : IDisposable
    {
        /// <summary>
        /// Pointer to the underlying data (not valid after disposal).
        /// </summary>
        public unsafe abstract byte* Pointer
        {
            get;
        }

        public abstract int Size
        {
            get;
        }

        /// <summary>
        /// Returns the content of the memory block. 
        /// </summary>
        /// <remarks>
        /// Only creates a copy of the data if they are not represented by a managed byte array, or the offset is non-zero.
        /// </remarks>
        public abstract ImmutableArray<byte> GetContent(int offset);

        public ImmutableArray<byte> GetContent()
        {
            return GetContent(0);
        }

        /// <summary>
        /// Disposes the block. 
        /// </summary>
        /// <remarks>
        /// The operation is idempotent, but must not be called concurrently with any other operations on the block
        /// or with another call to Dispose.
        /// 
        /// Using the block after dispose is an error in our code and therefore no effort is made to throw a tidy 
        /// ObjectDisposedException and null ref or AV is possible.
        /// </remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);

        protected static unsafe ImmutableArray<byte> CreateImmutableArray(byte* ptr, int length)
        {
            byte[] bytes = new byte[length];
            Marshal.Copy((IntPtr)ptr, bytes, 0, length);
            return ImmutableByteArrayInterop.DangerousCreateFromUnderlyingArray(ref bytes);
        }
    }
}
