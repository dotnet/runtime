// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.Frozen
{
    /// <summary>Provides a frozen set to use when the value is an <see cref="int"/> and the default comparer is used.</summary>
    /// <remarks>
    /// This set type is specialized as a memory optimization, as the frozen hash table already contains the array of all
    /// int values, and we can thus use its array as the items rather than maintaining a duplicate copy.
    /// </remarks>
    internal sealed partial class Int32FrozenSet : FrozenSetInternalBase<int, Int32FrozenSet.GSW>
    {
        private readonly FrozenHashTable _hashTable;

        internal Int32FrozenSet(HashSet<int> source) : base(EqualityComparer<int>.Default)
        {
            Debug.Assert(source.Count != 0);
            Debug.Assert(ReferenceEquals(source.Comparer, EqualityComparer<int>.Default));

            int count = source.Count;
            int[] entries = ArrayPool<int>.Shared.Rent(count);
            source.CopyTo(entries);

            _hashTable = FrozenHashTable.Create(new Span<int>(entries, 0, count), hashCodesAreUnique: true);

            ArrayPool<int>.Shared.Return(entries);
        }

        /// <inheritdoc />
        private protected override int[] ItemsCore => _hashTable.HashCodes;

        /// <inheritdoc />
        private protected override Enumerator GetEnumeratorCore() => new Enumerator(_hashTable.HashCodes);

        /// <inheritdoc />
        private protected override int CountCore => _hashTable.Count;

        /// <inheritdoc />
        private protected override int FindItemIndex(int item)
        {
            _hashTable.FindMatchingEntries(item, out int index, out int endIndex);

            int[] hashCodes = _hashTable.HashCodes;
            while (index <= endIndex)
            {
                if (item == hashCodes[index])
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        internal struct GSW : IGenericSpecializedWrapper
        {
            private Int32FrozenSet _set;
            public void Store(FrozenSet<int> set) => _set = (Int32FrozenSet)set;

            public int Count => _set.Count;
            public IEqualityComparer<int> Comparer => _set.Comparer;
            public int FindItemIndex(int item) => _set.FindItemIndex(item);
            public Enumerator GetEnumerator() => _set.GetEnumerator();
        }
    }
}
