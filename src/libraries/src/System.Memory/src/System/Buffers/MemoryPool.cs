// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Buffers
{
    /// <summary>
    /// Represents a pool of memory blocks.
    /// </summary>
    public abstract class MemoryPool<T> : IDisposable
    {
        // Store the shared ArrayMemoryPool in a field of its derived sealed type so the Jit can "see" the exact type
        // when the Shared property is inlined which will allow it to devirtualize calls made on it.
        //
        // Roslyn proposal https://github.com/dotnet/roslyn/issues/30797 where field initalizer, 
        // backing field and property could be combined via `{ get } = ` to support devirtualization 
        // by having the auto-backing field be created as the derived type rather than the exposed type.
        private static readonly ArrayMemoryPool<T> s_shared = new ArrayMemoryPool<T>();

        /// <summary>
        /// Returns a singleton instance of a MemoryPool based on arrays.
        /// </summary>
        public static MemoryPool<T> Shared => s_shared;

        /// <summary>
        /// Returns a memory block capable of holding at least <paramref name="minBufferSize" /> elements of T.
        /// </summary>
        /// <param name="minBufferSize">If -1 is passed, this is set to a default value for the pool.</param>
        public abstract IMemoryOwner<T> Rent(int minBufferSize = -1);

        /// <summary>
        /// Returns the maximum buffer size supported by this pool.
        /// </summary>
        public abstract int MaxBufferSize { get; }

        /// <summary>
        /// Constructs a new instance of a memory pool.
        /// </summary>
        protected MemoryPool() { }

        /// <summary>
        /// Frees all resources used by the memory pool.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Frees all resources used by the memory pool.
        /// </summary>
        /// <param name="disposing"></param>
        protected abstract void Dispose(bool disposing);
    }
}
