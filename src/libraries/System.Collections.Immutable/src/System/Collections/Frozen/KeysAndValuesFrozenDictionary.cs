// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace System.Collections.Frozen
{
    /// <summary>Provides a base class for frozen dictionaries that store their keys and values in dedicated arrays.</summary>
    internal abstract class KeysAndValuesFrozenDictionary<TKey, TValue> : FrozenDictionary<TKey, TValue>, IDictionary<TKey, TValue>
        where TKey : notnull
    {
        private protected readonly FrozenHashTable _hashTable;
        private protected readonly TKey[] _keys;
        private protected readonly TValue[] _values;

        protected KeysAndValuesFrozenDictionary(Dictionary<TKey, TValue> source, IEqualityComparer<TKey> comparer) : base(comparer)
        {
            Debug.Assert(source.Count != 0);

            KeyValuePair<TKey, TValue>[] entries = new KeyValuePair<TKey, TValue>[source.Count];
            ((ICollection<KeyValuePair<TKey, TValue>>)source).CopyTo(entries, 0);

            _keys = new TKey[entries.Length];
            _values = new TValue[entries.Length];

            _hashTable = FrozenHashTable.Create(
                entries,
                pair => comparer.GetHashCode(pair.Key),
                (index, pair) =>
                {
                    _keys[index] = pair.Key;
                    _values[index] = pair.Value;
                });
        }

        /// <inheritdoc />
        private protected sealed override ImmutableArray<TKey> KeysCore => new ImmutableArray<TKey>(_keys);

        /// <inheritdoc />
        private protected sealed override ImmutableArray<TValue> ValuesCore => new ImmutableArray<TValue>(_values);

        /// <inheritdoc />
        private protected sealed override Enumerator GetEnumeratorCore() => new Enumerator(_keys, _values);

        /// <inheritdoc />
        private protected sealed override int CountCore => _hashTable.Count;
    }
}
