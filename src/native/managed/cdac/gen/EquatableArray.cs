// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Diagnostics.DataContractReader.DataGenerator;

/// <summary>
/// A small structurally-equatable array used in the incremental pipeline.
/// Records with array members would compare arrays by reference and defeat
/// the cache; this wrapper compares by element-wise equality.
/// </summary>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IReadOnlyList<T>
    where T : IEquatable<T>
{
    private readonly T[]? _items;

    public EquatableArray(T[]? items)
    {
        _items = items;
    }

    public int Count => _items?.Length ?? 0;

    public T this[int index] => (_items ?? Array.Empty<T>())[index];

    public bool Equals(EquatableArray<T> other)
    {
        if (_items is null && other._items is null)
        {
            return true;
        }

        if (_items is null || other._items is null)
        {
            return false;
        }

        if (_items.Length != other._items.Length)
        {
            return false;
        }

        for (int i = 0; i < _items.Length; i++)
        {
            if (!_items[i].Equals(other._items[i]))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        if (_items is null)
        {
            return 0;
        }

        int hash = 17;
        foreach (T item in _items)
        {
            hash = unchecked((hash * 31) + item.GetHashCode());
        }

        return hash;
    }

    public IEnumerator<T> GetEnumerator()
        => ((IEnumerable<T>)(_items ?? Array.Empty<T>())).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static EquatableArray<T> FromEnumerable(IEnumerable<T> source)
        => new(source.ToArray());
}
