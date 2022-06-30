// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Buffers
{
    internal sealed partial class ArrayMemoryPool<T> : MemoryPool<T>
    {
        public sealed override int MaxBufferSize => Array.MaxLength;

        public sealed override IMemoryOwner<T> Rent(int minimumBufferSize = -1)
        {
            if (minimumBufferSize == -1)
                minimumBufferSize = 1 + (4095 / Unsafe.SizeOf<T>());
            else if (((uint)minimumBufferSize) > Array.MaxLength)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.minimumBufferSize);

            return new ArrayMemoryPoolBuffer(minimumBufferSize);
        }

        protected sealed override void Dispose(bool disposing) { }  // ArrayMemoryPool is a shared pool so Dispose() would be a nop even if there were native resources to dispose.
    }
}
