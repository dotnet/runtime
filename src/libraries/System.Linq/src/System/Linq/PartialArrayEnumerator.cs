// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    /// <summary>Enumerator for iterating through part of an array.</summary>
    internal sealed class PartialArrayEnumerator<T> : IEnumerator<T>
    {
        private readonly T[] _array;
        private readonly int _count;
        private int _index = -1;

        public PartialArrayEnumerator(T[] array, int count)
        {
            Debug.Assert(array is not null);
            _array = array;
            _count = count;
        }

        public bool MoveNext()
        {
            if (_index + 1 < _count)
            {
                _index++;
                return true;
            }

            return false;
        }

        public T Current => _array[_index];
        object? IEnumerator.Current => Current;

        public void Dispose() { }

        public void Reset() => _index = -1;
    }
}
