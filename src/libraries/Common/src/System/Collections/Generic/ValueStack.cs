// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Generic
{
    /// <summary>Provides a value type-based stack that uses provided scrath space or pooled arrays as a backing store.</summary>
    /// <typeparam name="T">Specifies the type of the items in the stack.</typeparam>
    internal ref struct ValueStack<T>
    {
        private Span<T> _span;
        private T[]? _arrayFromPool;
        private int _pos;

        public ValueStack(Span<T> initialSpan)
        {
            _span = initialSpan;
            _arrayFromPool = null;
            _pos = 0;
        }

        public ValueStack(int capacity)
        {
            _arrayFromPool = ArrayPool<T>.Shared.Rent(capacity);
            _span = _arrayFromPool;
            _pos = 0;
        }

        public int Count => _pos;

        public void Dispose()
        {
            T[]? toReturn = _arrayFromPool;
            if (toReturn != null)
            {
                _arrayFromPool = null;
                ArrayPool<T>.Shared.Return(toReturn);
            }
        }

        public void Push(T item)
        {
            int pos = _pos;
            if (pos >= _span.Length)
            {
                Grow();
            }

            _span[pos] = item;
            _pos = pos + 1;
        }

        public T Pop()
        {
            if (_pos <= 0)
            {
               throw new InvalidOperationException();
            }

            return _span[--_pos];
        }

        public bool TryPop([MaybeNullWhen(false)] out T item)
        {
            if (_pos > 0)
            {
                item = _span[--_pos];
                return true;
            }

            item = default;
            return false;
        }

        private void Grow()
        {
            T[] array = ArrayPool<T>.Shared.Rent(_span.Length == 0 ? 4 : _span.Length * 2);

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
