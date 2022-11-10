// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen
{
    /// <summary>Provides a frozen dictionary to use when the key is an <see cref="int"/> and the default comparer is used.</summary>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <remarks>
    /// This key type is specialized as a memory optimization, as the frozen hash table already contains the array of all
    /// int values, and we can thus use its array as the keys rather than maintaining a duplicate copy.
    /// </remarks>
    internal sealed class Int32FrozenDictionary<TValue> : FrozenDictionary<int, TValue>
    {
        private readonly FrozenHashTable _hashTable;
        private readonly TValue[] _values;

        internal Int32FrozenDictionary(Dictionary<int, TValue> source) : base(EqualityComparer<int>.Default)
        {
            Debug.Assert(source.Count != 0);

            KeyValuePair<int, TValue>[] entries = new KeyValuePair<int, TValue>[source.Count];
            ((ICollection<KeyValuePair<int, TValue>>)source).CopyTo(entries, 0);

            _values = new TValue[entries.Length];

            _hashTable = FrozenHashTable.Create(
                entries,
                pair => pair.Key,
                (index, pair) => _values[index] = pair.Value);
        }

        /// <inheritdoc />
        private protected override ImmutableArray<int> KeysCore => new ImmutableArray<int>(_hashTable.HashCodes);

        /// <inheritdoc />
        private protected override ImmutableArray<TValue> ValuesCore => new ImmutableArray<TValue>(_values);

        /// <inheritdoc />
        private protected override Enumerator GetEnumeratorCore() => new Enumerator(_hashTable.HashCodes, _values);

        /// <inheritdoc />
        private protected override int CountCore => _hashTable.Count;

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected override ref readonly TValue GetValueRefOrNullRefCore(int key)
        {
            _hashTable.FindMatchingEntries(key, out int index, out int endIndex);

            while (index <= endIndex)
            {
                if (key == _hashTable.HashCodes[index])
                {
                    return ref _values[index];
                }

                index++;
            }

            return ref Unsafe.NullRef<TValue>();
        }
    }
}
