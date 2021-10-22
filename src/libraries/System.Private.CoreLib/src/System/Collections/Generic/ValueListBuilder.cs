// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{
    internal ref partial struct ValueListBuilder<T>
    {
        private Span<T> _span;

#if !NETFRAMEWORK && !NETSTANDARD2_0
#if NICE_SYNTAX
        private T[64] _inlineArray;     // 64 arbitrarily chosen
#else
        private ValueArray<T, object[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,]> _inlineArray;
#endif
#endif
        private T[]? _arrayFromPool;
        private int _pos;

        public int Length
        {
            get => _pos;
            set
            {
                Debug.Assert(value >= 0);
                Debug.Assert(value <= _span.Length);
                _pos = value;
            }
        }

        public ref T this[int index]
        {
            get
            {
                Debug.Assert(index < _pos);
                return ref _span[index];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(T item)
        {
            int pos = _pos;
            if (pos >= _span.Length)
                Grow();

            _span[pos] = item;
            _pos = pos + 1;
        }

        public ReadOnlySpan<T> AsSpan()
        {
            return _span.Slice(0, _pos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            T[]? toReturn = _arrayFromPool;
            if (toReturn != null)
            {
                _arrayFromPool = null;
                ArrayPool<T>.Shared.Return(toReturn);
            }
        }

        private void Grow()
        {
#if !NETFRAMEWORK && !NETSTANDARD2_0
            if (_span.Length == 0)
            {
                _span = _inlineArray.Slice(0);
                return;
            }
#endif

            Rent();
        }

        private void Rent()
        {
            T[] array = ArrayPool<T>.Shared.Rent(Math.Max(_span.Length * 2, 64));

            bool success = _span.TryCopyTo(array);
            Debug.Assert(success);

            T[]? toReturn = _arrayFromPool;
            _span = _arrayFromPool = array;
            if (toReturn != null)
            {
                ArrayPool<T>.Shared.Return(toReturn);
            }
        }
    }
}
