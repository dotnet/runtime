// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen
{
    /// <summary>Provides a <see cref="FrozenDictionary{TKey, TValue}"/> implementation to use with small item counts.</summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <remarks>
    /// No hashing here, just a straight-up linear scan that compares all the keys.
    /// </remarks>
    internal sealed class SmallFrozenDictionary<TKey, TValue> : FrozenDictionary<TKey, TValue>
        where TKey : notnull
    {
        private readonly TKey[] _keys;
        private readonly TValue[] _values;

        internal SmallFrozenDictionary(Dictionary<TKey, TValue> source, IEqualityComparer<TKey> comparer)
            : base(comparer)
        {
            Debug.Assert(source.Count != 0);

            _keys = source.Keys.ToArray(source.Count);
            _values = source.Values.ToArray(source.Count);
        }

        private protected override TKey[] KeysCore => _keys;
        private protected override TValue[] ValuesCore => _values;
        private protected override int CountCore => _keys.Length;
        private protected sealed override Enumerator GetEnumeratorCore() => new Enumerator(_keys, _values);

        private protected override ref readonly TValue GetValueRefOrNullRefCore(TKey key)
        {
            TKey[] keys = _keys;
            for (int i = 0; i < keys.Length; i++)
            {
                if (Comparer.Equals(keys[i], key))
                {
                    return ref _values[i];
                }
            }

            return ref Unsafe.NullRef<TValue>();
        }
    }
}
