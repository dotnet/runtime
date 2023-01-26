// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            Dictionary<string, TValue> source,
            string[] keys,
            IEqualityComparer<string> comparer,
            int minimumLength,
            int maximumLengthDiff,
            int hashIndex = -1,
            int hashCount = -1) :
            base(comparer)
        {
            Debug.Assert(source.Count != 0);
            Debug.Assert(comparer == EqualityComparer<string>.Default || comparer == StringComparer.Ordinal || comparer == StringComparer.OrdinalIgnoreCase);

            var entries = new KeyValuePair<string, TValue>[source.Count];
            ((ICollection<KeyValuePair<string, TValue>>)source).CopyTo(entries, 0);

            _keys = keys;
            _values = new TValue[entries.Length];
            _minimumLength = minimumLength;
            _maximumLengthDiff = maximumLengthDiff;

            HashIndex = hashIndex;
            HashCount = hashCount;

            _hashTable = FrozenHashTable.Create(
                entries,
                pair => GetHashCode(pair.Key),
                (index, pair) =>
                {
                    _keys[index] = pair.Key;
                    _values[index] = pair.Value;
                });
        }

        private protected int HashIndex { get; }
        private protected int HashCount { get; }
        private protected abstract bool Equals(string? x, string? y);
        private protected abstract int GetHashCode(string s);
        private protected override string[] KeysCore => _keys;
        private protected override TValue[] ValuesCore => _values;
        private protected override Enumerator GetEnumeratorCore() => new Enumerator(_keys, _values);
        private protected override int CountCore => _hashTable.Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected override ref readonly TValue GetValueRefOrNullRefCore(string key)
        {
            if ((uint)(key.Length - _minimumLength) <= (uint)_maximumLengthDiff)
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

            return ref Unsafe.NullRef<TValue>();
        }
    }
}
