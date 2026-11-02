// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal sealed class CachedValue<T>
{
    private readonly Func<T> _initializer;
    private T _value = default!;
    private bool _hasValue;

    public CachedValue(Func<T> initializer)
    {
        _initializer = initializer;
    }

    public void Clear()
    {
        _hasValue = false;
    }

    public static implicit operator T(CachedValue<T> cached)
    {
        if (!cached._hasValue)
        {
            cached._value = cached._initializer();
            cached._hasValue = true;
        }

        return cached._value;
    }
}
