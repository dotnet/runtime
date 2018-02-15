// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{
    internal ref struct ResizableValueListBuilder<T>
    {
        private Span<T> _span;
        private T[] _arrayFromPool;
        private int _pos;

        public ResizableValueListBuilder(Span<T> initialSpan)
        {
            _span = initialSpan;
            _arrayFromPool = null;
            _pos = 0;
        }

        public ref T this[int index] => ref _span[index];

        public int Length => _pos;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(T item)
        {
            int pos = _pos;
            if (pos >= _span.Length)
                Grow();

            _span[pos] = item;
            _pos = pos + 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Pop()
        {
            _pos--;
            return _span[_pos];
        }

        public ReadOnlySpan<T> AsReadOnlySpan()
        {
            return _span.Slice(0, _pos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (_arrayFromPool != null)
            {
                ArrayPool<T>.Shared.Return(_arrayFromPool);
                _arrayFromPool = null;
            }
        }

        private void Grow()
        {
            T[] array = ArrayPool<T>.Shared.Rent(_span.Length * 2);

            bool success = _span.TryCopyTo(array);
            Debug.Assert(success);

            T[] toReturn = _arrayFromPool;
            _span = _arrayFromPool = array;
            if (toReturn != null)
            {
                ArrayPool<T>.Shared.Return(toReturn);
            }
        }
    }
}
