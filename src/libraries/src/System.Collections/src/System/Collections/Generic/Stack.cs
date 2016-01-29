// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
** Purpose: An array implementation of a generic stack.
**
**
=============================================================================*/
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Generic
{
    // A simple stack of objects.  Internally it is implemented as an array,
    // so Push can be O(n).  Pop is O(1).

    [DebuggerTypeProxy(typeof(StackDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    public class Stack<T> : IEnumerable<T>,
        System.Collections.ICollection,
        IReadOnlyCollection<T>
    {
        private T[] _array;     // Storage for stack elements
        private int _size;           // Number of items in the stack.
        private int _version;        // Used to keep enumerator in sync w/ collection.
        private Object _syncRoot;

        private const int DefaultCapacity = 4;

        /// <include file='doc\Stack.uex' path='docs/doc[@for="Stack.Stack"]/*' />
        public Stack()
        {
            _array = Array.Empty<T>();
        }

        // Create a stack with a specific initial capacity.  The initial capacity
        // must be a non-negative number.
        /// <include file='doc\Stack.uex' path='docs/doc[@for="Stack.Stack1"]/*' />
        public Stack(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException("capacity", capacity, SR.ArgumentOutOfRange_NeedNonNegNum);
            _array = new T[capacity];
        }

        // Fills a Stack with the contents of a particular collection.  The items are
        // pushed onto the stack in the same order they are read by the enumerator.
        //
        /// <include file='doc\Stack.uex' path='docs/doc[@for="Stack.Stack2"]/*' />
        public Stack(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");
            _array = EnumerableHelpers.ToArray(collection, out _size);
        }

        /// <include file='doc\Stack.uex' path='docs/doc[@for="Stack.Count"]/*' />
        public int Count
        {
            get { return _size; }
        }

        /// <include file='doc\Stack.uex' path='docs/doc[@for="Stack.IsSynchronized"]/*' />
        bool System.Collections.ICollection.IsSynchronized
        {
            get { return false; }
        }

        /// <include file='doc\Stack.uex' path='docs/doc[@for="Stack.SyncRoot"]/*' />
        Object System.Collections.ICollection.SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    System.Threading.Interlocked.CompareExchange<Object>(ref _syncRoot, new Object(), null);
                }
                return _syncRoot;
            }
        }

        // Removes all Objects from the Stack.
        /// <include file='doc\Stack.uex' path='docs/doc[@for="Stack.Clear"]/*' />
        public void Clear()
        {
            Array.Clear(_array, 0, _size); // Don't need to doc this but we clear the elements so that the gc can reclaim the references.
            _size = 0;
            _version++;
        }

        /// <include file='doc\Stack.uex' path='docs/doc[@for="Stack.Contains"]/*' />
        public bool Contains(T item)
        {
            int count = _size;

            EqualityComparer<T> c = EqualityComparer<T>.Default;
            while (count-- > 0)
            {
                if (((Object)item) == null)
                {
                    if (((Object)_array[count]) == null)
                        return true;
                }
                else if (_array[count] != null && c.Equals(_array[count], item))
                {
                    return true;
                }
            }
            return false;
        }

        // Copies the stack into an array.
        /// <include file='doc\Stack.uex' path='docs/doc[@for="Stack.CopyTo"]/*' />
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            if (arrayIndex < 0 || arrayIndex > array.Length)
            {
                throw new ArgumentOutOfRangeException("arrayIndex", arrayIndex, SR.ArgumentOutOfRange_Index);
            }

            if (array.Length - arrayIndex < _size)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }

            Debug.Assert(array != _array);
            int srcIndex = 0;
            int dstIndex = arrayIndex + _size;
            for (int i = 0; i < _size; i++)
                array[--dstIndex] = _array[srcIndex++];
        }

        void System.Collections.ICollection.CopyTo(Array array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            if (array.Rank != 1)
            {
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, "array");
            }

            if (array.GetLowerBound(0) != 0)
            {
                throw new ArgumentException(SR.Arg_NonZeroLowerBound, "array");
            }

            if (arrayIndex < 0 || arrayIndex > array.Length)
            {
                throw new ArgumentOutOfRangeException("arrayIndex", arrayIndex, SR.ArgumentOutOfRange_Index);
            }

            if (array.Length - arrayIndex < _size)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }

            try
            {
                Array.Copy(_array, 0, array, arrayIndex, _size);
                Array.Reverse(array, arrayIndex, _size);
            }
            catch (ArrayTypeMismatchException)
            {
                throw new ArgumentException(SR.Argument_InvalidArrayType, "array");
            }
        }

        // Returns an IEnumerator for this Stack.
        /// <include file='doc\Stack.uex' path='docs/doc[@for="Stack.GetEnumerator"]/*' />
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <include file='doc\Stack.uex' path='docs/doc[@for="Stack.IEnumerable.GetEnumerator"]/*' />
        /// <internalonly/>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public void TrimExcess()
        {
            int threshold = (int)(((double)_array.Length) * 0.9);
            if (_size < threshold)
            {
                Array.Resize(ref _array, _size);
                _version++;
            }
        }

        // Returns the top object on the stack without removing it.  If the stack
        // is empty, Peek throws an InvalidOperationException.
        /// <include file='doc\Stack.uex' path='docs/doc[@for="Stack.Peek"]/*' />
        public T Peek()
        {
            if (_size == 0)
                throw new InvalidOperationException(SR.InvalidOperation_EmptyStack);
            return _array[_size - 1];
        }

        // Pops an item from the top of the stack.  If the stack is empty, Pop
        // throws an InvalidOperationException.
        /// <include file='doc\Stack.uex' path='docs/doc[@for="Stack.Pop"]/*' />
        public T Pop()
        {
            if (_size == 0)
                throw new InvalidOperationException(SR.InvalidOperation_EmptyStack);
            _version++;
            T item = _array[--_size];
            _array[_size] = default(T);     // Free memory quicker.
            return item;
        }

        // Pushes an item to the top of the stack.
        // 
        /// <include file='doc\Stack.uex' path='docs/doc[@for="Stack.Push"]/*' />
        public void Push(T item)
        {
            if (_size == _array.Length)
            {
                Array.Resize(ref _array, (_array.Length == 0) ? DefaultCapacity : 2 * _array.Length);
            }
            _array[_size++] = item;
            _version++;
        }

        // Copies the Stack to an array, in the same order Pop would return the items.
        public T[] ToArray()
        {
            if (_size == 0)
                return Array.Empty<T>();

            T[] objArray = new T[_size];
            int i = 0;
            while (i < _size)
            {
                objArray[i] = _array[_size - i - 1];
                i++;
            }
            return objArray;
        }

        /// <include file='doc\Stack.uex' path='docs/doc[@for="StackEnumerator"]/*' />
        [SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes", Justification = "not an expected scenario")]
        public struct Enumerator : IEnumerator<T>,
            System.Collections.IEnumerator
        {
            private Stack<T> _stack;
            private int _index;
            private int _version;
            private T _currentElement;

            internal Enumerator(Stack<T> stack)
            {
                _stack = stack;
                _version = _stack._version;
                _index = -2;
                _currentElement = default(T);
            }

            /// <include file='doc\Stack.uex' path='docs/doc[@for="StackEnumerator.Dispose"]/*' />
            public void Dispose()
            {
                _index = -1;
            }

            /// <include file='doc\Stack.uex' path='docs/doc[@for="StackEnumerator.MoveNext"]/*' />
            public bool MoveNext()
            {
                bool retval;
                if (_version != _stack._version) throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                if (_index == -2)
                {  // First call to enumerator.
                    _index = _stack._size - 1;
                    retval = (_index >= 0);
                    if (retval)
                        _currentElement = _stack._array[_index];
                    return retval;
                }
                if (_index == -1)
                {  // End of enumeration.
                    return false;
                }

                retval = (--_index >= 0);
                if (retval)
                    _currentElement = _stack._array[_index];
                else
                    _currentElement = default(T);
                return retval;
            }

            /// <include file='doc\Stack.uex' path='docs/doc[@for="StackEnumerator.Current"]/*' />
            public T Current
            {
                get
                {
                    if (_index == -2) throw new InvalidOperationException(SR.InvalidOperation_EnumNotStarted);
                    if (_index == -1) throw new InvalidOperationException(SR.InvalidOperation_EnumEnded);
                    return _currentElement;
                }
            }

            Object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            void System.Collections.IEnumerator.Reset()
            {
                if (_version != _stack._version) throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                _index = -2;
                _currentElement = default(T);
            }
        }
    }
}
