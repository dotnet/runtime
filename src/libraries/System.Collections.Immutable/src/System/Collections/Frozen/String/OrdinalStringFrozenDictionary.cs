// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen
{
    /// <summary>The base class for the specialized frozen string dictionaries.</summary>
    internal abstract class OrdinalStringFrozenDictionary<TValue> : FrozenDictionary<string, TValue>
    {
        private readonly FrozenHashTable _hashTable;
        private readonly string[] _keys;
        private readonly TValue[] _values;
        private readonly int _minimumLength;
        private readonly int _maximumLengthDiff;

        internal OrdinalStringFrozenDictionary(
            string[] keys,
            TValue[] values,
            IEqualityComparer<string> comparer,
            int minimumLength,
            int maximumLengthDiff,
            int hashIndex = -1,
            int hashCount = -1) :
            base(comparer)
        {
            Debug.Assert(keys.Length != 0 && keys.Length == values.Length);
            Debug.Assert(comparer == EqualityComparer<string>.Default || comparer == StringComparer.Ordinal || comparer == StringComparer.OrdinalIgnoreCase);

            // we need an extra copy, as the order of items will change
            _keys = new string[keys.Length];
            _values = new TValue[values.Length];

            _minimumLength = minimumLength;
            _maximumLengthDiff = maximumLengthDiff;

            HashIndex = hashIndex;
            HashCount = hashCount;

            int[] arrayPoolHashCodes = ArrayPool<int>.Shared.Rent(keys.Length);
            Span<int> hashCodes = arrayPoolHashCodes.AsSpan(0, keys.Length);
            for (int i = 0; i < keys.Length; i++)
            {
                hashCodes[i] = GetHashCode(keys[i]);
            }

            _hashTable = FrozenHashTable.Create(hashCodes);

            for (int srcIndex = 0; srcIndex < hashCodes.Length; srcIndex++)
            {
                int destIndex = hashCodes[srcIndex];

                _keys[destIndex] = keys[srcIndex];
                _values[destIndex] = values[srcIndex];
            }

            ArrayPool<int>.Shared.Return(arrayPoolHashCodes);
        }

        private protected int HashIndex { get; }
        private protected int HashCount { get; }
        private protected abstract bool Equals(string? x, string? y);
        private protected abstract int GetHashCode(string s);
        private protected virtual bool CheckLengthQuick(string key) => true;
        private protected override string[] KeysCore => _keys;
        private protected override TValue[] ValuesCore => _values;
        private protected override Enumerator GetEnumeratorCore() => new Enumerator(_keys, _values);
        private protected override int CountCore => _hashTable.Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected override ref readonly TValue GetValueRefOrNullRefCore(string key)
        {
            if ((uint)(key.Length - _minimumLength) <= (uint)_maximumLengthDiff)
            {
                if (CheckLengthQuick(key))
                {
                    int hashCode = GetHashCode(key);
                    _hashTable.FindMatchingEntries(hashCode, out int index, out int endIndex);

                    while (index <= endIndex)
                    {
                        if (hashCode == _hashTable.HashCodes[index])
                        {
                            if (Equals(key, _keys[index]))
                            {
                                return ref _values[index];
                            }
                        }

                        index++;
                    }
                }
            }

            return ref Unsafe.NullRef<TValue>();
        }
    }
}
