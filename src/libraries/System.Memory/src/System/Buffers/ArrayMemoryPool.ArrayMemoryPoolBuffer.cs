// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Buffers
{
    internal sealed partial class ArrayMemoryPool<T> : MemoryPool<T>
    {
        private sealed class ArrayMemoryPoolBuffer : IMemoryOwner<T>
        {
            private T[]? _array;

            public ArrayMemoryPoolBuffer(int size)
            {
                _array = ArrayPool<T>.Shared.Rent(size);
            }

            public Memory<T> Memory
            {
                get
                {
                    T[]? array = _array;
                    ObjectDisposedException.ThrowIf(array is null, this);

                    return new Memory<T>(array);
                }
            }

            public void Dispose()
            {
                T[]? array = _array;
                if (array != null)
                {
                    _array = null;
                    ArrayPool<T>.Shared.Return(array);
                }
            }
        }
    }
}
