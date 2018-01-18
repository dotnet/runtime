// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
    /// <summary>
    /// Owner of Memory<typeparamref name="T"/> that provides appropriate lifetime management mechanisms for it.
    /// </summary>
    public abstract class OwnedMemory<T> : IDisposable, IRetainable
    {
        /// <summary>
        /// The number of items in the Memory<typeparamref name="T"/>.
        /// </summary>
        public abstract int Length { get; }

        /// <summary>
        /// Returns a span wrapping the underlying memory.
        /// </summary>
        public abstract Span<T> Span { get; }

        /// <summary>
        /// Returns a Memory<typeparamref name="T"/> if the underlying memory has not been freed.
        /// </summary>
        /// <exception cref="System.ObjectDisposedException">
        /// Thrown when the underlying memory has already been disposed.
        /// </exception>
        public Memory<T> Memory
        {
            get
            {
                if (IsDisposed)
                {
                    ThrowHelper.ThrowObjectDisposedException_MemoryDisposed(nameof(OwnedMemory<T>));
                }
                return new Memory<T>(owner: this, 0, Length);
            }
        }

        /// <summary>
        /// Returns a handle for the array that has been pinned and hence its address can be taken
        /// </summary>
        public abstract MemoryHandle Pin();

        /// <summary>
        /// Returns an array segment.
        /// </summary>
        protected internal abstract bool TryGetArray(out ArraySegment<T> arraySegment);

        /// <summary>
        /// Implements IDisposable.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// Throw when there are still retained references to the memory
        /// </exception>
        public void Dispose()
        {
            if (IsRetained)
            {
                ThrowHelper.ThrowInvalidOperationException_OutstandingReferences();
            }
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Clean up of any leftover managed and unmanaged resources.
        /// </summary>
        protected abstract void Dispose(bool disposing);

        /// <summary>
        /// Return true if someone is holding a reference to the memory.
        /// </summary>
        protected abstract bool IsRetained { get; }

        /// <summary>
        /// Return true if the underlying memory has been freed.
        /// </summary>
        public abstract bool IsDisposed { get; }

        /// <summary>
        /// Implements IRetainable. Prevent accidental disposal of the memory.
        /// </summary>
        public abstract void Retain();

        /// <summary>
        /// Implements IRetainable. The memory can now be diposed.
        /// </summary>
        public abstract bool Release();

    }
}
