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
        private sealed class ArrayMemoryPoolBuffer : OwnedMemory<T>
        {
            private T[] _array;
            private int _refCount;

            public ArrayMemoryPoolBuffer(int size)
            {
                _array = ArrayPool<T>.Shared.Rent(size);
                _refCount = 1;
            }

            public sealed override int Length => _array.Length;

            public sealed override bool IsDisposed => _array == null;

            protected sealed override bool IsRetained => Volatile.Read(ref _refCount) > 0;

            public sealed override Span<T> Span
            {
                get
                {
                    if (IsDisposed)
                        ThrowHelper.ThrowObjectDisposedException_ArrayMemoryPoolBuffer();

                    return _array;
                }
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

            public sealed override MemoryHandle Pin(int byteOffset = 0)
            {
                unsafe
                {
                    Retain(); // this checks IsDisposed

                    try
                    {
                        if ((IntPtr.Size == 4 && (uint)byteOffset > (uint)_array.Length * (uint)Unsafe.SizeOf<T>())
                            || (IntPtr.Size != 4 && (ulong)byteOffset > (uint)_array.Length * (ulong)Unsafe.SizeOf<T>()))
                        {
                            ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.byteOffset);
                        }

                        GCHandle handle = GCHandle.Alloc(_array, GCHandleType.Pinned);
                        return new MemoryHandle(this, ((byte*)handle.AddrOfPinnedObject()) + byteOffset, handle);
                    }
                    catch
                    {
                        Release();
                        throw;
                    }
                }
            }

            public sealed override void Retain()
            {
                while (true)
                {
                    int currentCount = Volatile.Read(ref _refCount);
                    if (currentCount <= 0)
                        ThrowHelper.ThrowObjectDisposedException_ArrayMemoryPoolBuffer();
                    if (Interlocked.CompareExchange(ref _refCount, currentCount + 1, currentCount) == currentCount)
                        break;
                }
            }

            public sealed override bool Release()
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
                            Dispose();
                            return false;
                        }
                        return true;
                    }
                }
            }
        }
    }
}
