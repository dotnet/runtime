// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen
{
    /// <summary>Provides a frozen dictionary optimized for ordinal (case-sensitive or case-insensitive) lookup of strings.</summary>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    internal sealed class OrdinalStringFrozenDictionary<TValue> : FrozenDictionary<string, TValue>
    {
        private readonly FrozenHashTable _hashTable;
        private readonly string[] _keys;
        private readonly TValue[] _values;
        private readonly StringComparerBase _partialComparer;
        private readonly int _minimumLength;
        private readonly int _maximumLengthDiff;

        internal OrdinalStringFrozenDictionary(Dictionary<string, TValue> source, IEqualityComparer<string> comparer) :
            base(comparer)
        {
            Debug.Assert(source.Count != 0);
            Debug.Assert(comparer == EqualityComparer<string>.Default || comparer == StringComparer.Ordinal || comparer == StringComparer.OrdinalIgnoreCase);

            var entries = new KeyValuePair<string, TValue>[source.Count];
            ((ICollection<KeyValuePair<string, TValue>>)source).CopyTo(entries, 0);

            _keys = new string[entries.Length];
            _values = new TValue[entries.Length];

            _partialComparer = ComparerPicker.Pick(
                Array.ConvertAll(entries, pair => pair.Key),
                ignoreCase: ReferenceEquals(comparer, StringComparer.OrdinalIgnoreCase),
                out _minimumLength,
                out _maximumLengthDiff);

            _hashTable = FrozenHashTable.Create(
                entries,
                pair => _partialComparer.GetHashCode(pair.Key),
                (index, pair) =>
                {
                    _keys[index] = pair.Key;
                    _values[index] = pair.Value;
                });
        }

        /// <inheritdoc />
        private protected override ImmutableArray<string> KeysCore => new ImmutableArray<string>(_keys);

        /// <inheritdoc />
        private protected override ImmutableArray<TValue> ValuesCore => new ImmutableArray<TValue>(_values);

        /// <inheritdoc />
        private protected override Enumerator GetEnumeratorCore() => new Enumerator(_keys, _values);

        /// <inheritdoc />
        private protected override int CountCore => _hashTable.Count;

        /// <inheritdoc />
        private protected override ref readonly TValue GetValueRefOrNullRefCore(string key)
        {
            if ((uint)(key.Length - _minimumLength) <= (uint)_maximumLengthDiff)
            {
                StringComparerBase partialComparer = _partialComparer;

                int hashCode = partialComparer.GetHashCode(key);
                _hashTable.FindMatchingEntries(hashCode, out int index, out int endIndex);

                while (index <= endIndex)
                {
                    if (hashCode == _hashTable.HashCodes[index])
                    {
                        if (partialComparer.Equals(key, _keys[index])) // partialComparer.Equals always compares the full input (EqualsPartial/GetHashCode don't)
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
