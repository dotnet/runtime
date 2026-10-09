// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System;

// Helper to build lists without allocating backing storage for lists of one element.
internal struct ListBuilder<T> where T : class
{
    private T[]? _items;
    private T _item;
    private int _count;
    private int _capacity;

    public ListBuilder(int capacity)
    {
        _items = null;
        _item = null!;
        _count = 0;
        _capacity = capacity;
    }

    public T this[int index]
    {
        get
        {
            Debug.Assert(index < Count);
            return (_items is not null) ? _items[index] : _item;
        }
    }

    // For multiple elements, this returns the backing array, not an independent snapshot.
    public T[] ToArray()
    {
        if (_count == 0)
            return [];
        if (_count == 1)
            return [_item];

        Array.Resize(ref _items, _count);
        _capacity = _count;
        return _items!;
    }

    public void CopyTo(object[] array, int index)
    {
        if (_count == 0)
            return;

        if (_count == 1)
        {
            array[index] = _item;
            return;
        }

        Array.Copy(_items!, 0, array, index, _count);
    }

    public int Count => _count;

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

            _items![_count] = item;
        }
        _count++;
    }
}
