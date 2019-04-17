// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                    if (array == null)
                    {
                        ThrowHelper.ThrowObjectDisposedException_ArrayMemoryPoolBuffer();
                    }

                    return new Memory<T>(array!); // TODO-NULLABLE: https://github.com/dotnet/csharplang/issues/538
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
