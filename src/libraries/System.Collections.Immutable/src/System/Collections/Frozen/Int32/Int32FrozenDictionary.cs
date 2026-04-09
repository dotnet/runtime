// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen
{
    /// <summary>Provides a frozen dictionary to use when the key is an <see cref="int"/> and the default comparer is used.</summary>
    /// <remarks>
    /// This dictionary type is specialized as a memory optimization, as the frozen hash table already contains the array of all
    /// int values, and we can thus use its array as the keys rather than maintaining a duplicate copy.
    /// </remarks>
    internal sealed partial class Int32FrozenDictionary<TValue> : FrozenDictionary<int, TValue>
    {
        private readonly FrozenHashTable _hashTable;
        private readonly TValue[] _values;

        internal Int32FrozenDictionary(Dictionary<int, TValue> source) : base(EqualityComparer<int>.Default)
        {
            Debug.Assert(ReferenceEquals(source.Comparer, EqualityComparer<int>.Default));
            Debug.Assert(source.Count != 0);

            KeyValuePair<int, TValue>[] entries = new KeyValuePair<int, TValue>[source.Count];
            ((ICollection<KeyValuePair<int, TValue>>)source).CopyTo(entries, 0);

            _values = new TValue[entries.Length];

            int[] arrayPoolHashCodes = ArrayPool<int>.Shared.Rent(entries.Length);
            Span<int> hashCodes = arrayPoolHashCodes.AsSpan(0, entries.Length);
            for (int i = 0; i < entries.Length; i++)
            {
                hashCodes[i] = entries[i].Key;
            }

            _hashTable = FrozenHashTable.Create(hashCodes, hashCodesAreUnique: true);

            for (int srcIndex = 0; srcIndex < hashCodes.Length; srcIndex++)
            {
                int destIndex = hashCodes[srcIndex];

                _values[destIndex] = entries[srcIndex].Value;
            }

            ArrayPool<int>.Shared.Return(arrayPoolHashCodes);
        }

        /// <inheritdoc />
        private protected override int[] KeysCore => _hashTable.HashCodes;

        /// <inheritdoc />
        private protected override TValue[] ValuesCore => _values;

        /// <inheritdoc />
        private protected override Enumerator GetEnumeratorCore() => new Enumerator(_hashTable.HashCodes, _values);

        /// <inheritdoc />
        private protected override int CountCore => _hashTable.Count;

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected override ref readonly TValue GetValueRefOrNullRefCore(int key)
        {
            _hashTable.FindMatchingEntries(key, out int index, out int endIndex);

            int[] hashCodes = _hashTable.HashCodes;
            while (index <= endIndex)
            {
                if (key == hashCodes[index])
                {
                    return ref _values[index];
                }

                index++;
            }

            return ref Unsafe.NullRef<TValue>();
        }
    }
}
