// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.Frozen
{
    /// <summary>Provides a frozen set to use when the value is an <see cref="int"/> and the default comparer is used.</summary>
    internal sealed class Int32FrozenSet : FrozenSetInternalBase<int, Int32FrozenSet.GSW>
    {
        private readonly FrozenHashTable _hashTable;

        internal Int32FrozenSet(int[] entries) : base(EqualityComparer<int>.Default)
        {
            Debug.Assert(entries.Length != 0);

            _hashTable = FrozenHashTable.Create(
                entries,
                item => item,
                (_, _) => { });
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
