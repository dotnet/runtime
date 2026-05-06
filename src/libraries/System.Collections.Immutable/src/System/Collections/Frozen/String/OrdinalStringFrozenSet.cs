// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen
{
    /// <summary>The base class for the specialized frozen string sets.</summary>
    internal abstract partial class OrdinalStringFrozenSet : FrozenSetInternalBase<string, OrdinalStringFrozenSet.GSW>
    {
        private readonly FrozenHashTable _hashTable;
        private readonly string[] _items;
        private readonly int _minimumLength;
        private readonly int _maximumLengthDiff;

        internal OrdinalStringFrozenSet(
            string[] entries,
            IEqualityComparer<string> comparer,
            int minimumLength,
            int maximumLengthDiff,
            int hashIndex = -1,
            int hashCount = -1)
            : base(comparer)
        {
            _items = new string[entries.Length];
            _minimumLength = minimumLength;
            _maximumLengthDiff = maximumLengthDiff;

            HashIndex = hashIndex;
            HashCount = hashCount;

            int[] arrayPoolHashCodes = ArrayPool<int>.Shared.Rent(entries.Length);
            Span<int> hashCodes = arrayPoolHashCodes.AsSpan(0, entries.Length);
            for (int i = 0; i < entries.Length; i++)
            {
                hashCodes[i] = GetHashCode(entries[i]);
            }

            _hashTable = FrozenHashTable.Create(hashCodes);

            for (int srcIndex = 0; srcIndex < hashCodes.Length; srcIndex++)
            {
                int destIndex = hashCodes[srcIndex];

                _items[destIndex] = entries[srcIndex];
            }

            ArrayPool<int>.Shared.Return(arrayPoolHashCodes);
        }

        private protected int HashIndex { get; }
        private protected int HashCount { get; }
        private protected virtual bool Equals(string? x, string? y) => string.Equals(x, y);
        private protected virtual bool Equals(ReadOnlySpan<char> x, string? y) => EqualsOrdinal(x, y);
        private protected abstract int GetHashCode(string s);
        private protected abstract int GetHashCode(ReadOnlySpan<char> s);
        private protected virtual bool CheckLengthQuick(uint length) => true;
        private protected override string[] ItemsCore => _items;
        private protected override Enumerator GetEnumeratorCore() => new Enumerator(_items);
        private protected override int CountCore => _hashTable.Count;

        // We want to avoid having to implement FindItemIndex for each of the multiple types
        // that derive from this one, but each of those needs to supply its own notion of Equals/GetHashCode.
        // To avoid lots of virtual calls, we have every derived type override FindItemIndex and
        // call to that span-based method that's aggressively inlined. That then exposes the implementation
        // to the sealed Equals/GetHashCodes on each derived type, allowing them to be devirtualized and inlined
        // into each unique copy of the code.

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected override int FindItemIndex(string item)
        {
            if (item is not null && // this implementation won't be used for null values
                (uint)(item.Length - _minimumLength) <= (uint)_maximumLengthDiff)
            {
                if (CheckLengthQuick((uint)item.Length))
                {
                    int hashCode = GetHashCode(item);
                    _hashTable.FindMatchingEntries(hashCode, out int index, out int endIndex);

                    while (index <= endIndex)
                    {
                        if (hashCode == _hashTable.HashCodes[index] && Equals(item, _items[index]))
                        {
                            return index;
                        }

                        index++;
                    }
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected static bool EqualsOrdinal(ReadOnlySpan<char> x, string? y) =>
            // Same behavior as the IAlternateEqualityComparer<ReadOnlySpan<char>, string>
            // implementation on StringComparer.Ordinal. See comment on OrdinalComparer.Equals
            // in corelib for explanation.
            (!x.IsEmpty || y is not null) && x.SequenceEqual(y.AsSpan());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected static bool EqualsOrdinalIgnoreCase(ReadOnlySpan<char> x, string? y) =>
            // Same behavior as the IAlternateEqualityComparer<ReadOnlySpan<char>, string>
            // implementation on StringComparer.OrdinalIgnoreCase. See comment on OrdinalComparer.Equals
            // in corelib for explanation.
            (!x.IsEmpty || y is not null) && x.Equals(y.AsSpan(), StringComparison.OrdinalIgnoreCase);

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
