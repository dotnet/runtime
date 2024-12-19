// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.Frozen
{
    /// <summary>Provides a frozen set to use when the values are <see cref="char"/>s, the default comparer is used, and some values are &gt;= 255.</summary>
    /// <remarks>
    /// This is logically similar to using a <see cref="FrozenHashTable"/>, but where that is bucketized,
    /// <see cref="PerfectHashLookup"/> is not, so we can avoid some indirection during lookups.
    /// The latter is also specialized for <see cref="char"/> values, with lower memory consumption and a slightly cheaper FastMod.
    /// </remarks>
    internal sealed partial class PerfectHashCharFrozenSet : FrozenSetInternalBase<char, PerfectHashCharFrozenSet.GSW>
    {
        private readonly uint _multiplier;
        private readonly char[] _hashEntries;
        private char[]? _items;

        internal PerfectHashCharFrozenSet(ReadOnlySpan<char> values) : base(EqualityComparer<char>.Default)
        {
            Debug.Assert(!values.IsEmpty);

            int max = 0;
            foreach (char c in values)
            {
                max = Math.Max(max, c);
            }

            PerfectHashLookup.Initialize(values, max, out _multiplier, out _hashEntries);
        }

        private char[] AllocateItemsArray()
        {
            var set = new HashSet<char>(_hashEntries);
            char[] items = new char[set.Count];
            set.CopyTo(items);
            _items = items;
            return items;
        }

        /// <inheritdoc />
        private protected override char[] ItemsCore => _items ?? AllocateItemsArray();

        /// <inheritdoc />
        private protected override Enumerator GetEnumeratorCore() => new Enumerator(ItemsCore);

        /// <inheritdoc />
        private protected override int CountCore => ItemsCore.Length;

        /// <inheritdoc />
        /// <remarks>
        /// This is an internal helper where results are not exposed to the user.
        /// The returned index does not have to correspond to the value in the <see cref="ItemsCore"/> array.
        /// In this case, calculating the real index would be costly, so we return the offset into <see cref="_hashEntries"/> instead.
        /// </remarks>
        private protected override int FindItemIndex(char item) => PerfectHashLookup.IndexOf(_hashEntries, _multiplier, item);

        /// <inheritdoc />
        private protected override bool ContainsCore(char item) => PerfectHashLookup.Contains(_hashEntries, _multiplier, item);

        /// <inheritdoc />
        /// <remarks>
        /// We're overriding this method to account for the fact that the indexes returned by <see cref="FindItemIndex(char)"/>
        /// are based on <see cref="_hashEntries"/> instead of <see cref="ItemsCore"/>.
        /// </remarks>
        private protected override KeyValuePair<int, int> CheckUniqueAndUnfoundElements(IEnumerable<char> other, bool returnIfUnfound) =>
            CheckUniqueAndUnfoundElements(other, returnIfUnfound, _hashEntries.Length);

        internal struct GSW : IGenericSpecializedWrapper
        {
            private PerfectHashCharFrozenSet _set;
            public void Store(FrozenSet<char> set) => _set = (PerfectHashCharFrozenSet)set;

            public int Count => _set.Count;
            public IEqualityComparer<char> Comparer => _set.Comparer;
            public int FindItemIndex(char item) => _set.FindItemIndex(item);
            public Enumerator GetEnumerator() => _set.GetEnumerator();
        }
    }
}
