// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace System.Collections.Frozen
{
    /// <summary>Provides a frozen set optimized for ordinal (case-sensitive or case-insensitive) lookup of strings.</summary>
    internal sealed class OrdinalStringFrozenSet : FrozenSetInternalBase<string, OrdinalStringFrozenSet.GSW>
    {
        private readonly FrozenHashTable _hashTable;
        private readonly string[] _items;
        private readonly StringComparerBase _partialComparer;
        private readonly int _minimumLength;
        private readonly int _maximumLengthDiff;

        internal OrdinalStringFrozenSet(HashSet<string> source, IEqualityComparer<string> comparer) :
            base(comparer)
        {
            Debug.Assert(source.Count != 0);
            Debug.Assert(comparer == EqualityComparer<string>.Default || comparer == StringComparer.Ordinal || comparer == StringComparer.OrdinalIgnoreCase);

            string[] entries = new string[source.Count];
            source.CopyTo(entries);

            _items = new string[entries.Length];

            _partialComparer = ComparerPicker.Pick(
                entries,
                ignoreCase: ReferenceEquals(comparer, StringComparer.OrdinalIgnoreCase),
                out _minimumLength,
                out _maximumLengthDiff);

            _hashTable = FrozenHashTable.Create(
                entries,
                _partialComparer.GetHashCode,
                (index, item) => _items[index] = item);
        }

        /// <inheritdoc />
        private protected override ImmutableArray<string> ItemsCore => new ImmutableArray<string>(_items);

        /// <inheritdoc />
        private protected override Enumerator GetEnumeratorCore() => new Enumerator(_items);

        /// <inheritdoc />
        private protected override int CountCore => _hashTable.Count;

        /// <inheritdoc />
        private protected override int FindItemIndex(string item)
        {
            if (item is not null && // this implementation won't be used for null values
                (uint)(item.Length - _minimumLength) <= (uint)_maximumLengthDiff)
            {
                StringComparerBase partialComparer = _partialComparer;

                int hashCode = partialComparer.GetHashCode(item);
                _hashTable.FindMatchingEntries(hashCode, out int index, out int endIndex);

                while (index <= endIndex)
                {
                    if (hashCode == _hashTable.HashCodes[index])
                    {
                        if (partialComparer.Equals(item, _items[index])) // partialComparer.Equals always compares the full input (EqualsPartial/GetHashCode don't)
                        {
                            return index;
                        }
                    }

                    index++;
                }
            }

            return -1;
        }

        internal struct GSW : IGenericSpecializedWrapper
        {
            private OrdinalStringFrozenSet _set;
            public void Store(FrozenSet<string> set) => _set = (OrdinalStringFrozenSet)set;

            public int Count => _set.Count;
            public IEqualityComparer<string> Comparer => _set.Comparer;
            public int FindItemIndex(string item) => _set.FindItemIndex(item);
            public Enumerator GetEnumerator() => _set.GetEnumerator();
        }
    }
}
