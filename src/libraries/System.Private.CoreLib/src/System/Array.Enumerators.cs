// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System
{
    internal sealed class ArrayEnumerator : IEnumerator, ICloneable
    {
        private Array array;
        private int index;
        private int endIndex;
        private int startIndex;    // Save for Reset.
        private int[] _indices;    // The current position in a multidim array
        private bool _complete;

        internal ArrayEnumerator(Array array, int index, int count)
        {
            this.array = array;
            this.index = index - 1;
            startIndex = index;
            endIndex = index + count;
            _indices = new int[array.Rank];
            int checkForZero = 1;  // Check for dimensions of size 0.
            for (int i = 0; i < array.Rank; i++)
            {
                _indices[i] = array.GetLowerBound(i);
                checkForZero *= array.GetLength(i);
            }
            // To make MoveNext simpler, decrement least significant index.
            _indices[_indices.Length - 1]--;
            _complete = (checkForZero == 0);
        }

        private void IncArray()
        {
            // This method advances us to the next valid array index,
            // handling all the multiple dimension & bounds correctly.
            // Think of it like an odometer in your car - we start with
            // the last digit, increment it, and check for rollover.  If
            // it rolls over, we set all digits to the right and including 
            // the current to the appropriate lower bound.  Do these overflow
            // checks for each dimension, and if the most significant digit 
            // has rolled over it's upper bound, we're done.
            //
            int rank = array.Rank;
            _indices[rank - 1]++;
            for (int dim = rank - 1; dim >= 0; dim--)
            {
                if (_indices[dim] > array.GetUpperBound(dim))
                {
                    if (dim == 0)
                    {
                        _complete = true;
                        break;
                    }
                    for (int j = dim; j < rank; j++)
                        _indices[j] = array.GetLowerBound(j);
                    _indices[dim - 1]++;
                }
            }
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public bool MoveNext()
        {
            if (_complete)
            {
                index = endIndex;
                return false;
            }
            index++;
            IncArray();
            return !_complete;
        }

        public object? Current
        {
            get
            {
                if (index < startIndex) ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumNotStarted();
                if (_complete) ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumEnded();
                return array.GetValue(_indices);
            }
        }

        public void Reset()
        {
            index = startIndex - 1;
            int checkForZero = 1;
            for (int i = 0; i < array.Rank; i++)
            {
                _indices[i] = array.GetLowerBound(i);
                checkForZero *= array.GetLength(i);
            }
            _complete = (checkForZero == 0);
            // To make MoveNext simpler, decrement least significant index.
            _indices[_indices.Length - 1]--;
        }
    }

    internal sealed class SZArrayEnumerator : IEnumerator, ICloneable
    {
        private readonly Array _array;
        private int _index;
        private int _endIndex; // Cache Array.Length, since it's a little slow.

        internal SZArrayEnumerator(Array array)
        {
            Debug.Assert(array.Rank == 1 && array.GetLowerBound(0) == 0, "SZArrayEnumerator only works on single dimension arrays w/ a lower bound of zero.");

            _array = array;
            _index = -1;
            _endIndex = array.Length;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public bool MoveNext()
        {
            if (_index < _endIndex)
            {
                _index++;
                return (_index < _endIndex);
            }
            return false;
        }

        public object? Current
        {
            get
            {
                if (_index < 0)
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumNotStarted();
                if (_index >= _endIndex)
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumEnded();
                return _array.GetValue(_index);
            }
        }

        public void Reset()
        {
            _index = -1;
        }
    }    

    internal sealed class SZGenericArrayEnumerator<T> : IEnumerator<T>
    {
        private readonly T[] _array;
        private int _index;

        // Array.Empty is intentionally omitted here, since we don't want to pay for generic instantiations that
        // wouldn't have otherwise been used.
        internal static readonly SZGenericArrayEnumerator<T> Empty = new SZGenericArrayEnumerator<T>(new T[0]);

        internal SZGenericArrayEnumerator(T[] array)
        {
            Debug.Assert(array != null);

            _array = array;
            _index = -1;
        }

        public bool MoveNext()
        {
            int index = _index + 1;
            if ((uint)index >= (uint)_array.Length)
            {
                _index = _array.Length;
                return false;
            }
            _index = index;
            return true;
        }

        public T Current
        {
            get
            {
                int index = _index;
                T[] array = _array;

                if ((uint)index >= (uint)array.Length)
                {
                    ThrowHelper.ThrowInvalidOperationException_EnumCurrent(index);
                }

                return array[index];
            }
        }

        object? IEnumerator.Current => Current;

        void IEnumerator.Reset() => _index = -1;

        public void Dispose()
        {
        }
    }
}
