// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text
{
    /// <summary>Provides a struct-based list builder.</summary>
    [DebuggerDisplay("Count = {_count}")]
    internal struct StructListBuilder<T>
    {
        /// <summary>The array backing the builder, obtained from <see cref="ArrayPool{T}.Shared"/>.</summary>
        private T[] _array = [];
        /// <summary>The number of items in <see cref="_array"/>, and thus also the next position in the array to be filled.</summary>
        private int _count;

        /// <summary>Creates a new builder.</summary>
        /// <remarks>Should be used instead of default struct initialization.</remarks>
        public StructListBuilder() { }

        /// <summary>Gets the number of items in the builder.</summary>
        public int Count => _count;

        /// <summary>Gets a span of the items in the builder.</summary>
        public Span<T> AsSpan() => _array.AsSpan(0, _count);

        /// <summary>Adds an item to the builder.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            T[] array = _array;
            int pos = _count;
            if ((uint)pos < (uint)array.Length)
            {
                array[pos] = item;
                _count = pos + 1;
            }
            else
            {
                GrowAndAdd(item);
            }
        }

        /// <summary>Disposes the builder, returning any array it's storing to the pool.</summary>
        public void Dispose()
        {
            if (_array != null)
            {
                ArrayPool<T>.Shared.Return(_array, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>());
                _array = null!;
            }
        }

        /// <summary>Grows the builder to accommodate another item.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void GrowAndAdd(T item)
        {
            T[] array = _array;
            Debug.Assert(array.Length == _count);

            const int DefaultArraySize = 256;
            int newSize = array.Length == 0 ? DefaultArraySize : array.Length * 2;

            T[] newArray = _array = ArrayPool<T>.Shared.Rent(newSize);
            Array.Copy(array, newArray, _count);
            ArrayPool<T>.Shared.Return(array, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>());
            newArray[_count++] = item;
        }
    }
}
