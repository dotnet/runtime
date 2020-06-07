// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

public class MyCollection<T> : ICollection<T>
{
    private List<T> _items = new List<T>();

    public MyCollection()
    {
    }

    public MyCollection(params T[] values)
    {
        _items.AddRange(values);
    }

    public void Add(T item)
    {
        _items.Add(item);
    }

    public void Clear()
    {
        _items.Clear();
    }

    public bool Contains(T item)
    {
        return _items.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        _items.CopyTo(array, arrayIndex);
    }

    public int Count
    {
        get { return _items.Count; }
    }

    public bool IsReadOnly
    {
        get { return ((ICollection<T>)_items).IsReadOnly; }
    }

    public bool Remove(T item)
    {
        return _items.Remove(item);
    }

    public IEnumerator<T> GetEnumerator()
    {
        return ((ICollection<T>)_items).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_items).GetEnumerator();
    }
}

class Bug
{
    public static int Main()
    {
        int v = 0;
        MyCollection<string> x = new MyCollection<string>("a1", "a2");
        foreach (string item in x)
        {
            v += item[0];
        }
        return v - 94;
    }
}
