// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace XUnitWrapperGenerator;

internal sealed class ImmutableDictionaryValueComparer<TKey, TValue> : IEqualityComparer<ImmutableDictionary<TKey, TValue>>
    where TKey : notnull
{
    private readonly IEqualityComparer<TValue> _valueComparer;

    public ImmutableDictionaryValueComparer(IEqualityComparer<TValue> valueComparer)
    {
        _valueComparer = valueComparer;
    }

    public bool Equals(ImmutableDictionary<TKey, TValue> x, ImmutableDictionary<TKey, TValue> y)
    {
        if (x.Count != y.Count)
        {
            return false;
        }

        foreach (var pair in x)
        {
            if (!y.TryGetValue(pair.Key, out TValue? value) || !_valueComparer.Equals(value, pair.Value))
            {
                return false;
            }
        }
        return true;
    }

    public int GetHashCode(ImmutableDictionary<TKey, TValue> obj) => throw new NotImplementedException();
}
