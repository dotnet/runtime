// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen
{
    internal sealed partial class SmallFrozenDictionary<TKey, TValue>
    {
        private protected override ref readonly TValue GetValueRefOrNullRefCore<TAlternateKey>(TAlternateKey key)
        {
            IAlternateEqualityComparer<TAlternateKey, TKey> comparer = GetAlternateEqualityComparer<TAlternateKey>();

            TKey[] keys = _keys;
            for (int i = 0; i < keys.Length; i++)
            {
                if (comparer.Equals(key, keys[i]))
                {
                    return ref _values[i];
                }
            }

            return ref Unsafe.NullRef<TValue>();
        }
    }
}
