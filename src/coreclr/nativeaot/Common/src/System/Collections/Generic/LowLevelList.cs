// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
** Private version of List<T> for internal System.Private.CoreLib use. This
** permits sharing more source between BCL and System.Private.CoreLib (as well as the
** fact that List<T> is just a useful class in general.)
**
** This does not strive to implement the full api surface area
** (but any portion it does implement should match the real List<T>'s
** behavior.)
**
** This file is a subset of System.Collections\System\Collections\Generics\List.cs
** and should be kept in sync with that file.
**
===========================================================*/

using System;
using System.Diagnostics;

namespace System.Collections.Generic
{
    // Implements a variable-size List that uses an array of objects to store the
    // elements. A List has a capacity, which is the allocated length
    // of the internal array. As elements are added to a List, the capacity
    // of the List is automatically increased as required by reallocating the
    // internal array.
    //
    // LowLevelList with no interface implementation minimizes both code and data size.
    // Data size is smaller because there will be minimal virtual function table.
    // Code size is smaller because only functions called will be in the binary.
    // Use LowLevelListWithIList<T> for IList support
    [DebuggerDisplay("Count = {Count}")]
#if TYPE_LOADER_IMPLEMENTATION
    [System.Runtime.CompilerServices.ForceDictionaryLookups]
#endif
    internal class LowLevelList<T>
    {
        private const int _defaultCapacity = 4;

        protected T[] _items;
        protected int _size;
        protected int _version;

#pragma warning disable CA1825 // avoid the extra generic instantiation for Array.Empty<T>()
        private static readonly T[] s_emptyArray = new T[0];
#pragma warning restore CA1825

        // Constructs a List. The list is initially empty and has a capacity
        // of zero. Upon adding the first element to the list the capacity is
        // increased to 4, and then increased in multiples of two as required.
        public LowLevelList()
        {
            _items = s_emptyArray;
        }

        // Constructs a List with a given initial capacity. The list is
        // initially empty, but will have room for the given number of elements
        // before any reallocations are required.
        //
        public LowLevelList(int capacity)
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));

            if (capacity == 0)
                _items = s_emptyArray;
            else
                _items = new T[capacity];
        }

        // Constructs a List, copying the contents of the given collection. The
        // size and capacity of the new list will both be equal to the size of the
        // given collection.
        //
        public LowLevelList(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            ICollection<T>? c = collection as ICollection<T>;
            if (c != null)
            {
                int count = c.Count;
                if (count == 0)
                {
                    _items = s_emptyArray;
                }
                else
                {
                    _items = new T[count];
                    c.CopyTo(_items, 0);
                    _size = count;
                }
            }
            else
            {
                _size = 0;
                _items = s_emptyArray;
                // This enumerable could be empty.  Let Add allocate a new array, if needed.
                // Note it will also go to _defaultCapacity first, not 1, then 2, etc.

                using (IEnumerator<T> en = collection.GetEnumerator())
                {
                    while (en.MoveNext())
                    {
                        Add(en.Current);
                    }
                }
            }
        }

        // Gets and sets the capacity of this list.  The capacity is the size of
        // the internal array used to hold items.  When set, the internal
        // array of the list is reallocated to the given capacity.
        //
        public int Capacity
        {
            get
            {
                return _items.Length;
            }
            set
            {
                if (value < _size)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                if (value != _items.Length)
                {
                    if (value > 0)
                    {
                        T[] newItems = new T[value];
                        Array.Copy(_items, 0, newItems, 0, _size);
                        _items = newItems;
                    }
                    else
                    {
                        _items = s_emptyArray;
                    }
                }
            }
        }

        // Read-only property describing how many elements are in the List.
        public int Count
        {
            get
            {
                return _size;
            }
        }

        // Sets or Gets the element at the given index.
        //
        public T this[int index]
        {
            get
            {
                // Following trick can reduce the range check by one
                if ((uint)index >= (uint)_size)
                {
                    throw new ArgumentOutOfRangeException();
                }
                return _items[index];
            }

            set
            {
                if ((uint)index >= (uint)_size)
                {
                    throw new ArgumentOutOfRangeException();
                }
                _items[index] = value;
                _version++;
            }
        }


        // Adds the given object to the end of this list. The size of the list is
        // increased by one. If required, the capacity of the list is doubled
        // before adding the new element.
        //
        public void Add(T item)
        {
            if (_size == _items.Length) EnsureCapacity(_size + 1);
            _items[_size++] = item;
            _version++;
        }

        // Ensures that the capacity of this list is at least the given minimum
        // value. If the current capacity of the list is less than min, the
        // capacity is increased to twice the current capacity or to min,
        // whichever is larger.
        private void EnsureCapacity(int min)
        {
            if (_items.Length < min)
            {
                int newCapacity = _items.Length == 0 ? _defaultCapacity : _items.Length * 2;
                // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
                // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
                //if ((uint)newCapacity > Array.MaxLength) newCapacity = Array.MaxLength;
                if (newCapacity < min) newCapacity = min;
                Capacity = newCapacity;
            }
        }

#if !TYPE_LOADER_IMPLEMENTATION
        // Adds the elements of the given collection to the end of this list. If
        // required, the capacity of the list is increased to twice the previous
        // capacity or the new size, whichever is larger.
        //
        public void AddRange(IEnumerable<T> collection)
        {

            InsertRange(_size, collection);
        }

        // Clears the contents of List.
        public void Clear()
        {
            if (_size > 0)
            {
                Array.Clear(_items, 0, _size); // Don't need to doc this but we clear the elements so that the gc can reclaim the references.
                _size = 0;
            }
            _version++;
        }

        // Contains returns true if the specified element is in the List.
        // It does a linear, O(n) search.  Equality is determined by calling
        // item.Equals().
        //
        public bool Contains(T item)
        {
            if ((object?)item == null)
            {
                for (int i = 0; i < _size; i++)
                    if ((object?)_items[i] == null)
                        return true;
                return false;
            }
            else
            {
                int index = IndexOf(item);
                if (index >= 0)
                    return true;
                return false;
            }
        }


        // Copies a section of this list to the given array at the given index.
        //
        // The method uses the Array.Copy method to copy the elements.
        //
        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            if (_size - index < count)
            {
                throw new ArgumentException();
            }

            // Delegate rest of error checking to Array.Copy.
            Array.Copy(_items, index, array, arrayIndex, count);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            // Delegate rest of error checking to Array.Copy.
            Array.Copy(_items, 0, array, arrayIndex, _size);
        }

        // Returns the index of the first occurrence of a given value in a range of
        // this list. The list is searched forwards from beginning to end.
        // The elements of the list are compared to the given value using the
        // Object.Equals method.
        //
        // This method uses the Array.IndexOf method to perform the
        // search.
        //
        public int IndexOf(T item)
        {
            return Array.IndexOf(_items, item, 0, _size);
        }


        // Returns the index of the first occurrence of a given value in a range of
        // this list. The list is searched forwards, starting at index
        // index and ending at count number of elements. The
        // elements of the list are compared to the given value using the
        // Object.Equals method.
        //
        // This method uses the Array.IndexOf method to perform the
        // search.
        //
        public int IndexOf(T item, int index)
        {
            if (index > _size)
                throw new ArgumentOutOfRangeException(nameof(index));
            return Array.IndexOf(_items, item, index, _size - index);
        }

        // Inserts an element into this list at a given index. The size of the list
        // is increased by one. If required, the capacity of the list is doubled
        // before inserting the new element.
        //
        public void Insert(int index, T item)
        {
            // Note that insertions at the end are legal.
            if ((uint)index > (uint)_size)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            if (_size == _items.Length) EnsureCapacity(_size + 1);
            if (index < _size)
            {
                Array.Copy(_items, index, _items, index + 1, _size - index);
            }
            _items[index] = item;
            _size++;
            _version++;
        }

        // Inserts the elements of the given collection at a given index. If
        // required, the capacity of the list is increased to twice the previous
        // capacity or the new size, whichever is larger.  Ranges may be added
        // to the end of the list by setting index to the List's size.
        //
        public void InsertRange(int index, IEnumerable<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if ((uint)index > (uint)_size)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            ICollection<T>? c = collection as ICollection<T>;
            if (c != null)
            {    // if collection is ICollection<T>
                int count = c.Count;
                if (count > 0)
                {
                    EnsureCapacity(_size + count);
                    if (index < _size)
                    {
                        Array.Copy(_items, index, _items, index + count, _size - index);
                    }

                    // If we're inserting a List into itself, we want to be able to deal with that.
                    if (this == c)
                    {
                        // Copy first part of _items to insert location
                        Array.Copy(_items, 0, _items, index, index);
                        // Copy last part of _items back to inserted location
                        Array.Copy(_items, index + count, _items, index * 2, _size - index);
                    }
                    else
                    {
                        T[] itemsToInsert = new T[count];
                        c.CopyTo(itemsToInsert, 0);
                        Array.Copy(itemsToInsert, 0, _items, index, count);
                    }
                    _size += count;
                }
            }
            else
            {
                using (IEnumerator<T> en = collection.GetEnumerator())
                {
                    while (en.MoveNext())
                    {
                        Insert(index++, en.Current);
                    }
                }
            }
            _version++;
        }

        // Removes the element at the given index. The size of the list is
        // decreased by one.
        //
        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }

            return false;
        }

        // This method removes all items which matches the predicate.
        // The complexity is O(n).
        public int RemoveAll(Predicate<T> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            int freeIndex = 0;   // the first free slot in items array

            // Find the first item which needs to be removed.
            while (freeIndex < _size && !match(_items[freeIndex]!)) freeIndex++;
            if (freeIndex >= _size) return 0;

            int current = freeIndex + 1;
            while (current < _size)
            {
                // Find the first item which needs to be kept.
                while (current < _size && match(_items[current]!)) current++;

                if (current < _size)
                {
                    // copy item to the free slot.
                    _items[freeIndex++] = _items[current++];
                }
            }

            Array.Clear(_items, freeIndex, _size - freeIndex);
            int result = _size - freeIndex;
            _size = freeIndex;
            _version++;
            return result;
        }

        // Removes the element at the given index. The size of the list is
        // decreased by one.
        //
        public void RemoveAt(int index)
        {
            if ((uint)index >= (uint)_size)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            _size--;
            if (index < _size)
            {
                Array.Copy(_items, index + 1, _items, index, _size - index);
            }
            _items[_size] = default!;
            _version++;
        }

        // ToArray returns a new Object array containing the contents of the List.
        // This requires copying the List, which is an O(n) operation.
        public T[] ToArray()
        {
            T[] array = new T[_size];
            Array.Copy(_items, 0, array, 0, _size);
            return array;
        }
#endif
    }

#if !TYPE_LOADER_IMPLEMENTATION
    // LowLevelList<T> with full IList<T> implementation
    internal sealed class LowLevelListWithIList<T> : LowLevelList<T>, IList<T>
    {
        public LowLevelListWithIList()
        {
        }

        public LowLevelListWithIList(int capacity)
            : base(capacity)
        {
        }

        public LowLevelListWithIList(IEnumerable<T> collection)
            : base(collection)
        {
        }

        // Is this List read-only?
        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        /// <internalonly/>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        private struct Enumerator : IEnumerator<T>, System.Collections.IEnumerator
        {
            private LowLevelListWithIList<T> _list;
            private int _index;
            private int _version;
            private T? _current;

            internal Enumerator(LowLevelListWithIList<T> list)
            {
                _list = list;
                _index = 0;
                _version = list._version;
                _current = default(T);
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                LowLevelListWithIList<T> localList = _list;

                if (_version == localList._version && ((uint)_index < (uint)localList._size))
                {
                    _current = localList._items[_index];
                    _index++;
                    return true;
                }
                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                if (_version != _list._version)
                {
                    throw new InvalidOperationException();
                }

                _index = _list._size + 1;
                _current = default(T);
                return false;
            }

            public T Current
            {
                get
                {
                    return _current!;
                }
            }

            object? System.Collections.IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || _index == _list._size + 1)
                    {
                        throw new InvalidOperationException();
                    }
                    return Current;
                }
            }

            void System.Collections.IEnumerator.Reset()
            {
                if (_version != _list._version)
                {
                    throw new InvalidOperationException();
                }

                _index = 0;
                _current = default(T);
            }
        }
    }
#endif // !TYPE_LOADER_IMPLEMENTATION
}
