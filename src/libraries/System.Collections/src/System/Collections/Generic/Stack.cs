// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
**
** Purpose: An array implementation of a generic stack.
**
**
=============================================================================*/

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{
    // A simple stack of objects.  Internally it is implemented as an array,
    // so Push can be O(n).  Pop is O(1).

    [DebuggerTypeProxy(typeof(StackDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class Stack<T> : IEnumerable<T>,
        System.Collections.ICollection,
        IReadOnlyCollection<T>
    {
        private T[] _array; // Storage for stack elements. Do not rename (binary serialization)
        private int _size; // Number of items in the stack. Do not rename (binary serialization)
        private int _version; // Used to keep enumerator in sync w/ collection. Do not rename (binary serialization)

        private const int DefaultCapacity = 4;

        public Stack()
        {
            _array = Array.Empty<T>();
        }

        // Create a stack with a specific initial capacity.  The initial capacity
        // must be a non-negative number.
        public Stack(int capacity)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(capacity);
            _array = new T[capacity];
        }

        // Fills a Stack with the contents of a particular collection.  The items are
        // pushed onto the stack in the same order they are read by the enumerator.
        public Stack(IEnumerable<T> collection)
        {
            ArgumentNullException.ThrowIfNull(collection);

            _array = EnumerableHelpers.ToArray(collection, out _size);
        }

        public int Count => _size;


        /// <summary>
        /// Gets the total numbers of elements the internal data structure can hold without resizing.
        /// </summary>
        public int Capacity => _array.Length;

        /// <inheritdoc cref="ICollection{T}"/>
        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        // Removes all Objects from the Stack.
        public void Clear()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array.Clear(_array, 0, _size); // Don't need to doc this but we clear the elements so that the gc can reclaim the references.
            }
            _size = 0;
            _version++;
        }

        public bool Contains(T item)
        {
            // Compare items using the default equality comparer

            // PERF: Internally Array.LastIndexOf calls
            // EqualityComparer<T>.Default.LastIndexOf, which
            // is specialized for different types. This
            // boosts performance since instead of making a
            // virtual method call each iteration of the loop,
            // via EqualityComparer<T>.Default.Equals, we
            // only make one virtual call to EqualityComparer.LastIndexOf.

            return _size != 0 && Array.LastIndexOf(_array, item, _size - 1) != -1;
        }

        // Copies the stack into an array.
        public void CopyTo(T[] array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);

            if (arrayIndex < 0 || arrayIndex > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), arrayIndex, SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);
            }

            if (array.Length - arrayIndex < _size)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }

            Debug.Assert(array != _array);
            int srcIndex = 0;
            int dstIndex = arrayIndex + _size;
            while (srcIndex < _size)
            {
                array[--dstIndex] = _array[srcIndex++];
            }
        }

        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);

            if (array.Rank != 1)
            {
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, nameof(array));
            }

            if (array.GetLowerBound(0) != 0)
            {
                throw new ArgumentException(SR.Arg_NonZeroLowerBound, nameof(array));
            }

            if (arrayIndex < 0 || arrayIndex > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), arrayIndex, SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);
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
                throw new ArgumentException(SR.Argument_IncompatibleArrayType, nameof(array));
            }
        }

        // Returns an IEnumerator for this Stack.
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <internalonly/>
        IEnumerator<T> IEnumerable<T>.GetEnumerator() =>
            Count == 0 ? EnumerableHelpers.GetEmptyEnumerator<T>() :
            GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();

        public void TrimExcess()
        {
            int threshold = (int)(_array.Length * 0.9);
            if (_size < threshold)
            {
                Array.Resize(ref _array, _size);
            }
        }

        /// <summary>
        /// Sets the capacity of a <see cref="Stack{T}"/> object to a specified number of entries.
        /// </summary>
        /// <param name="capacity">The new capacity.</param>
        /// <exception cref="ArgumentOutOfRangeException">Passed capacity is lower than 0 or entries count.</exception>
        public void TrimExcess(int capacity)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(capacity);
            ArgumentOutOfRangeException.ThrowIfLessThan(capacity, _size);

            if (capacity == _array.Length)
                return;

            Array.Resize(ref _array, capacity);
        }

        // Returns the top object on the stack without removing it.  If the stack
        // is empty, Peek throws an InvalidOperationException.
        public T Peek()
        {
            int size = _size - 1;
            T[] array = _array;

            if ((uint)size >= (uint)array.Length)
            {
                ThrowForEmptyStack();
            }

            return array[size];
        }

        public bool TryPeek([MaybeNullWhen(false)] out T result)
        {
            int size = _size - 1;
            T[] array = _array;

            if ((uint)size >= (uint)array.Length)
            {
                result = default!;
                return false;
            }
            result = array[size];
            return true;
        }

        // Pops an item from the top of the stack.  If the stack is empty, Pop
        // throws an InvalidOperationException.
        public T Pop()
        {
            int size = _size - 1;
            T[] array = _array;

            // if (_size == 0) is equivalent to if (size == -1), and this case
            // is covered with (uint)size, thus allowing bounds check elimination
            // https://github.com/dotnet/coreclr/pull/9773
            if ((uint)size >= (uint)array.Length)
            {
                ThrowForEmptyStack();
            }

            _version++;
            _size = size;
            T item = array[size];
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                array[size] = default!;     // Free memory quicker.
            }
            return item;
        }

        public bool TryPop([MaybeNullWhen(false)] out T result)
        {
            int size = _size - 1;
            T[] array = _array;

            if ((uint)size >= (uint)array.Length)
            {
                result = default!;
                return false;
            }

            _version++;
            _size = size;
            result = array[size];
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                array[size] = default!;
            }
            return true;
        }

        // Pushes an item to the top of the stack.
        public void Push(T item)
        {
            int size = _size;
            T[] array = _array;

            if ((uint)size < (uint)array.Length)
            {
                array[size] = item;
                _version++;
                _size = size + 1;
            }
            else
            {
                PushWithResize(item);
            }
        }

        // Non-inline from Stack.Push to improve its code quality as uncommon path
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void PushWithResize(T item)
        {
            Debug.Assert(_size == _array.Length);
            Grow(_size + 1);
            _array[_size] = item;
            _version++;
            _size++;
        }

        /// <summary>
        /// Ensures that the capacity of this Stack is at least the specified <paramref name="capacity"/>.
        /// If the current capacity of the Stack is less than specified <paramref name="capacity"/>,
        /// the capacity is increased by continuously twice current capacity until it is at least the specified <paramref name="capacity"/>.
        /// </summary>
        /// <param name="capacity">The minimum capacity to ensure.</param>
        /// <returns>The new capacity of this stack.</returns>
        public int EnsureCapacity(int capacity)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(capacity);

            if (_array.Length < capacity)
            {
                Grow(capacity);
            }

            return _array.Length;
        }

        private void Grow(int capacity)
        {
            Debug.Assert(_array.Length < capacity);

            int newcapacity = _array.Length == 0 ? DefaultCapacity : 2 * _array.Length;

            // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
            // Note that this check works even when _items.Length overflowed thanks to the (uint) cast.
            if ((uint)newcapacity > Array.MaxLength) newcapacity = Array.MaxLength;

            // If computed capacity is still less than specified, set to the original argument.
            // Capacities exceeding Array.MaxLength will be surfaced as OutOfMemoryException by Array.Resize.
            if (newcapacity < capacity) newcapacity = capacity;

            Array.Resize(ref _array, newcapacity);
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

        private void ThrowForEmptyStack()
        {
            Debug.Assert(_size == 0);
            throw new InvalidOperationException(SR.InvalidOperation_EmptyStack);
        }

        public struct Enumerator : IEnumerator<T>, System.Collections.IEnumerator
        {
            private readonly Stack<T> _stack;
            private readonly int _version;
            private int _index;
            private T? _currentElement;

            internal Enumerator(Stack<T> stack)
            {
                _stack = stack;
                _version = stack._version;
                _index = -2;
                _currentElement = default;
            }

            public void Dispose()
            {
                _index = -1;
            }

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
                    _currentElement = default;
                return retval;
            }

            public T Current
            {
                get
                {
                    if (_index < 0)
                        ThrowEnumerationNotStartedOrEnded();
                    return _currentElement!;
                }
            }

            private void ThrowEnumerationNotStartedOrEnded()
            {
                Debug.Assert(_index == -1 || _index == -2);
                throw new InvalidOperationException(_index == -2 ? SR.InvalidOperation_EnumNotStarted : SR.InvalidOperation_EnumEnded);
            }

            object? System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            void IEnumerator.Reset()
            {
                if (_version != _stack._version) throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                _index = -2;
                _currentElement = default;
            }
        }
    }
}
