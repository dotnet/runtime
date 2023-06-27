// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
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

        protected KeysAndValuesFrozenDictionary(Dictionary<TKey, TValue> source) : base(source.Comparer)
        {
            Debug.Assert(source.Count != 0);

            KeyValuePair<TKey, TValue>[] entries = new KeyValuePair<TKey, TValue>[source.Count];
            ((ICollection<KeyValuePair<TKey, TValue>>)source).CopyTo(entries, 0);

            _keys = new TKey[entries.Length];
            _values = new TValue[entries.Length];

            int[] arrayPoolHashCodes = ArrayPool<int>.Shared.Rent(entries.Length);
            Span<int> hashCodes = arrayPoolHashCodes.AsSpan(0, entries.Length);
            for (int i = 0; i < entries.Length; i++)
            {
                hashCodes[i] = Comparer.GetHashCode(entries[i].Key);
            }

            _hashTable = FrozenHashTable.Create(hashCodes);

            for (int srcIndex = 0; srcIndex < hashCodes.Length; srcIndex++)
            {
                int destIndex = hashCodes[srcIndex];

                _keys[destIndex] = entries[srcIndex].Key;
                _values[destIndex] = entries[srcIndex].Value;
            }

            ArrayPool<int>.Shared.Return(arrayPoolHashCodes);
        }

        /// <inheritdoc />
        private protected sealed override TKey[] KeysCore => _keys;

        /// <inheritdoc />
        private protected sealed override TValue[] ValuesCore => _values;

        /// <inheritdoc />
        private protected sealed override Enumerator GetEnumeratorCore() => new Enumerator(_keys, _values);

        /// <inheritdoc />
        private protected sealed override int CountCore => _hashTable.Count;
    }
}
