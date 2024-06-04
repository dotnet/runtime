// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Reflection.Runtime.General
{
    //
    // Struct-based list builder that's special cased to avoid allocations for lists of one element.
    //
    internal struct ListBuilder<T> where T : class
    {
        public ListBuilder(int capacity)
        {
            _items = null;
            _item = null;
            _count = 0;
            _capacity = capacity;
#if DEBUG
            _toArrayAlreadyCalled = false;
#endif // DEBUG
        }

        public T this[int index]
        {
            get
            {
                Debug.Assert(index < Count);
                return (_items != null) ? _items[index] : _item;
            }
        }

        public T[] ToArray()
        {
#if DEBUG
            // ListBuilder does not always allocate a new array, though the ToArray() name connotates that it does (and in fact, we do pass the results
            // of this method across api boundaries.) The minimizing of allocations is desirable, however, so instead, we restrict you to one call per ListBuilder.
            Debug.Assert(!_toArrayAlreadyCalled, "Cannot call ListBuilder.ToArray() a second time. Copy the one you already got.");
#endif // DEBUG
            if (_count == 0)
                return Array.Empty<T>();
            if (_count == 1)
                return new T[1] { _item };

            Array.Resize(ref _items, _count);

#if DEBUG
            _toArrayAlreadyCalled = true;
#endif // DEBUG

            return _items;
        }

        public int Count
        {
            get
            {
                return _count;
            }
        }

        public void Add(T item)
        {
            if (_count == 0)
            {
                _item = item;
            }
            else
            {
                if (_count == 1)
                {
                    if (_capacity < 2)
                        _capacity = 4;
                    _items = new T[_capacity];
                    _items[0] = _item;
                }
                else if (_capacity == _count)
                {
                    int newCapacity = 2 * _capacity;
                    Array.Resize(ref _items, newCapacity);
                    _capacity = newCapacity;
                }

                _items[_count] = item;
            }
            _count++;
        }

        private T[] _items;
        private T _item;
        private int _count;
        private int _capacity;
#if DEBUG
        private bool _toArrayAlreadyCalled;
#endif // DEBUG
    }
}
