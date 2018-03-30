// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Threading;

#if !netstandard
using Internal.Runtime.CompilerServices;
#else
using System.Runtime.CompilerServices;
#endif

namespace System.Buffers
{
    internal sealed partial class ArrayMemoryPool<T> : MemoryPool<T>
    {
        private sealed class ArrayMemoryPoolBuffer : MemoryManager<T>
        {
            private T[] _array;
            private int _refCount;

            public ArrayMemoryPoolBuffer(int size)
            {
                _array = ArrayPool<T>.Shared.Rent(size);
                _refCount = 1;
            }

            public sealed override int Length => _array.Length;

            public bool IsDisposed => _array == null;

            public bool IsRetained => Volatile.Read(ref _refCount) > 0;

            public sealed override Span<T> GetSpan()
            {
                if (IsDisposed)
                    ThrowHelper.ThrowObjectDisposedException_ArrayMemoryPoolBuffer();

                return _array;
            }

            protected sealed override void Dispose(bool disposing)
            {
                if (_array != null)
                {
                    ArrayPool<T>.Shared.Return(_array);
                    _array = null;
                }
            }

            // TryGetArray is exposed as "protected internal". Normally, the rules of C# dictate we override it as "protected" because the base class is
            // in a different assembly. Except in the netstandard config where the base class is in the same assembly.
            protected
#if netstandard  
            internal
#endif
            sealed override bool TryGetArray(out ArraySegment<T> segment)
            {
                if (IsDisposed)
                    ThrowHelper.ThrowObjectDisposedException_ArrayMemoryPoolBuffer();

                segment = new ArraySegment<T>(_array);
                return true;
            }

            public sealed override MemoryHandle Pin(int elementIndex = 0)
            {
                unsafe
                {
                    while (true)
                    {
                        int currentCount = Volatile.Read(ref _refCount);
                        if (currentCount <= 0)
                            ThrowHelper.ThrowObjectDisposedException_ArrayMemoryPoolBuffer();
                        if (Interlocked.CompareExchange(ref _refCount, currentCount + 1, currentCount) == currentCount)
                            break;
                    }

                    try
                    {
                        if ((uint)elementIndex > (uint)_array.Length)
                        {
                            ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.elementIndex);
                        }

                        GCHandle handle = GCHandle.Alloc(_array, GCHandleType.Pinned);

                        return new MemoryHandle(Unsafe.Add<T>(((void*)handle.AddrOfPinnedObject()), elementIndex), handle, this);
                    }
                    catch
                    {
                        Unpin();
                        throw;
                    }
                }
            }

            public sealed override void Unpin()
            {
                while (true)
                {
                    int currentCount = Volatile.Read(ref _refCount);
                    if (currentCount <= 0)
                        ThrowHelper.ThrowObjectDisposedException_ArrayMemoryPoolBuffer();
                    if (Interlocked.CompareExchange(ref _refCount, currentCount - 1, currentCount) == currentCount)
                    {
                        if (currentCount == 1)
                        {
                            Dispose(true);
                        }
                        break;
                    }
                }
            }
        }
    }
}
