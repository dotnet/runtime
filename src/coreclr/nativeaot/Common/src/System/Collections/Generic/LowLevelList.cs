// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
** Private version of List<T> for internal System.Private.TypeLoader use. Type
** loader itself can't use generic types without [ForceDictionaryLookups], and
** we don't want to annotate List<T> with it because it would impact size and
** performance for general usages.
**
** This does not strive to implement the full api surface area
** (but any portion it does implement should match the real List<T>'s
** behavior.)
**
** This file is a subset of
** src\libraries\System.Private.CoreLib\src\System\Collections\Generics\List.cs
** and should be kept in sync with that file when necessary.
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
    [DebuggerDisplay("Count = {Count}")]
    [System.Runtime.CompilerServices.ForceDictionaryLookups]
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
                ArgumentOutOfRangeException.ThrowIfLessThan(value, _size);

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
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)_size, nameof(index));
                return _items[index];
            }

            set
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)_size, nameof(index));
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
    }
}
