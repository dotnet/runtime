// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Buffers
{
    internal sealed partial class ArrayMemoryPool<T> : MemoryPool<T>
    {
        public sealed override int MaxBufferSize => Array.MaxLength;

        public sealed override unsafe IMemoryOwner<T> Rent(int minimumBufferSize = -1)
        {
#pragma warning disable 8500 // sizeof of managed types
            if (minimumBufferSize == -1)
                minimumBufferSize = 1 + (4095 / sizeof(T));
            else if (((uint)minimumBufferSize) > Array.MaxLength)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.minimumBufferSize);
#pragma warning restore 8500

            return new ArrayMemoryPoolBuffer(minimumBufferSize);
        }

        protected sealed override void Dispose(bool disposing) { }  // ArrayMemoryPool is a shared pool so Dispose() would be a nop even if there were native resources to dispose.
    }
}
