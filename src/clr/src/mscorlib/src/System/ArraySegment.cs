// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: Convenient wrapper for an array, an offset, and
**          a count.  Ideally used in streams & collections.
**          Net Classes will consume an array of these.
**
**
===========================================================*/

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace System
{
    // Note: users should make sure they copy the fields out of an ArraySegment onto their stack
    // then validate that the fields describe valid bounds within the array.  This must be done
    // because assignments to value types are not atomic, and also because one thread reading 
    // three fields from an ArraySegment may not see the same ArraySegment from one call to another
    // (ie, users could assign a new value to the old location).  
    [Serializable]
    public struct ArraySegment<T> : IList<T>, IReadOnlyList<T>
    {
        private readonly T[] _array;
        private readonly int _offset;
        private readonly int _count;

        public ArraySegment(T[] array)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            Contract.EndContractBlock();

            _array = array;
            _offset = 0;
            _count = array.Length;
        }

        public ArraySegment(T[] array, int offset, int count)
        {
            // Validate arguments, check is minimal instructions with reduced branching for inlinable fast-path
            // Negative values discovered though conversion to high values when converted to unsigned
            // Failure should be rare and location determination and message is delegated to failure functions
            if (array == null || (uint)offset > (uint)array.Length || (uint)count > (uint)(array.Length - offset))
                ThrowHelper.ThrowArraySegmentCtorValidationFailedExceptions(array, offset, count);
            Contract.EndContractBlock();

            _array = array;
            _offset = offset;
            _count = count;
        }

        public T[] Array
        {
            get
            {
                Debug.Assert((null == _array && 0 == _offset && 0 == _count)
                                 || (null != _array && _offset >= 0 && _count >= 0 && _offset + _count <= _array.Length),
                                "ArraySegment is invalid");

                return _array;
            }
        }

        public int Offset
        {
            get
            {
                // Since copying value types is not atomic & callers cannot atomically 
                // read all three fields, we cannot guarantee that Offset is within 
                // the bounds of Array.  That is our intent, but let's not specify 
                // it as a postcondition - force callers to re-verify this themselves
                // after reading each field out of an ArraySegment into their stack.
                Contract.Ensures(Contract.Result<int>() >= 0);

                Debug.Assert((null == _array && 0 == _offset && 0 == _count)
                                 || (null != _array && _offset >= 0 && _count >= 0 && _offset + _count <= _array.Length),
                                "ArraySegment is invalid");

                return _offset;
            }
        }

        public int Count
        {
            get
            {
                // Since copying value types is not atomic & callers cannot atomically 
                // read all three fields, we cannot guarantee that Count is within 
                // the bounds of Array.  That's our intent, but let's not specify 
                // it as a postcondition - force callers to re-verify this themselves
                // after reading each field out of an ArraySegment into their stack.
                Contract.Ensures(Contract.Result<int>() >= 0);

                Debug.Assert((null == _array && 0 == _offset && 0 == _count)
                                  || (null != _array && _offset >= 0 && _count >= 0 && _offset + _count <= _array.Length),
                                "ArraySegment is invalid");

                return _count;
            }
        }

        public Enumerator GetEnumerator()
        {
            if (_array == null)
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_NullArray);
            Contract.EndContractBlock();

            return new Enumerator(this);
        }

        public override int GetHashCode()
        {
            if (_array == null)
            {
                return 0;
            }

            int hash = 5381;
            hash = System.Numerics.Hashing.HashHelpers.Combine(hash, _offset);
            hash = System.Numerics.Hashing.HashHelpers.Combine(hash, _count);

            // The array hash is expected to be an evenly-distributed mixture of bits,
            // so rather than adding the cost of another rotation we just xor it.
            hash ^= _array.GetHashCode();
            return hash;
        }

        public override bool Equals(Object obj)
        {
            if (obj is ArraySegment<T>)
                return Equals((ArraySegment<T>)obj);
            else
                return false;
        }

        public bool Equals(ArraySegment<T> obj)
        {
            return obj._array == _array && obj._offset == _offset && obj._count == _count;
        }

        public static bool operator ==(ArraySegment<T> a, ArraySegment<T> b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(ArraySegment<T> a, ArraySegment<T> b)
        {
            return !(a == b);
        }

        #region IList<T>
        T IList<T>.this[int index]
        {
            get
            {
                if (_array == null)
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_NullArray);
                if (index < 0 || index >= _count)
                    ThrowHelper.ThrowArgumentOutOfRange_IndexException();
                Contract.EndContractBlock();

                return _array[_offset + index];
            }

            set
            {
                if (_array == null)
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_NullArray);
                if (index < 0 || index >= _count)
                    ThrowHelper.ThrowArgumentOutOfRange_IndexException();
                Contract.EndContractBlock();

                _array[_offset + index] = value;
            }
        }

        int IList<T>.IndexOf(T item)
        {
            if (_array == null)
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_NullArray);
            Contract.EndContractBlock();

            int index = System.Array.IndexOf<T>(_array, item, _offset, _count);

            Debug.Assert(index == -1 ||
                            (index >= _offset && index < _offset + _count));

            return index >= 0 ? index - _offset : -1;
        }

        void IList<T>.Insert(int index, T item)
        {
            ThrowHelper.ThrowNotSupportedException();
        }

        void IList<T>.RemoveAt(int index)
        {
            ThrowHelper.ThrowNotSupportedException();
        }
        #endregion

        #region IReadOnlyList<T>
        T IReadOnlyList<T>.this[int index]
        {
            get
            {
                if (_array == null)
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_NullArray);
                if (index < 0 || index >= _count)
                    ThrowHelper.ThrowArgumentOutOfRange_IndexException();
                Contract.EndContractBlock();

                return _array[_offset + index];
            }
        }
        #endregion IReadOnlyList<T>

        #region ICollection<T>
        bool ICollection<T>.IsReadOnly
        {
            get
            {
                // the indexer setter does not throw an exception although IsReadOnly is true.
                // This is to match the behavior of arrays.
                return true;
            }
        }

        void ICollection<T>.Add(T item)
        {
            ThrowHelper.ThrowNotSupportedException();
        }

        void ICollection<T>.Clear()
        {
            ThrowHelper.ThrowNotSupportedException();
        }

        bool ICollection<T>.Contains(T item)
        {
            if (_array == null)
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_NullArray);
            Contract.EndContractBlock();

            int index = System.Array.IndexOf<T>(_array, item, _offset, _count);

            Debug.Assert(index == -1 ||
                            (index >= _offset && index < _offset + _count));

            return index >= 0;
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            if (_array == null)
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_NullArray);
            Contract.EndContractBlock();

            System.Array.Copy(_array, _offset, array, arrayIndex, _count);
        }

        bool ICollection<T>.Remove(T item)
        {
            ThrowHelper.ThrowNotSupportedException();
            return default(bool);
        }
        #endregion

        #region IEnumerable<T>

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        #endregion

        #region IEnumerable

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion

        [Serializable]
        public struct Enumerator : IEnumerator<T>
        {
            private readonly T[] _array;
            private readonly int _start;
            private readonly int _end; // cache Offset + Count, since it's a little slow
            private int _current;

            internal Enumerator(ArraySegment<T> arraySegment)
            {
                Contract.Requires(arraySegment.Array != null);
                Contract.Requires(arraySegment.Offset >= 0);
                Contract.Requires(arraySegment.Count >= 0);
                Contract.Requires(arraySegment.Offset + arraySegment.Count <= arraySegment.Array.Length);

                _array = arraySegment.Array;
                _start = arraySegment.Offset;
                _end = arraySegment.Offset + arraySegment.Count;
                _current = arraySegment.Offset - 1;
            }

            public bool MoveNext()
            {
                if (_current < _end)
                {
                    _current++;
                    return (_current < _end);
                }
                return false;
            }

            public T Current
            {
                get
                {
                    if (_current < _start)
                        ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumNotStarted();
                    if (_current >= _end)
                        ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumEnded();
                    return _array[_current];
                }
            }

            object IEnumerator.Current => Current;

            void IEnumerator.Reset()
            {
                _current = _start - 1;
            }

            public void Dispose()
            {
            }
        }
    }
}
