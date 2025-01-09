// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Collections.Frozen
{
    internal sealed class OrdinalStringFrozenSet_Full : OrdinalStringFrozenSet
    {
        private readonly ulong _lengthFilter;

        internal OrdinalStringFrozenSet_Full(
            string[] entries,
            IEqualityComparer<string> comparer,
            int minimumLength,
            int maximumLengthDiff,
            ulong lengthFilter)
            : base(entries, comparer, minimumLength, maximumLengthDiff)
        {
            _lengthFilter = lengthFilter;
        }

        // See comment in OrdinalStringFrozenSet for why these overrides exist. Do not remove.
        private protected override int FindItemIndex(string item) => base.FindItemIndex(item);
        private protected override int FindItemIndex<TAlternate>(TAlternate item) => base.FindItemIndex(item);

        private protected override int GetHashCode(string s) => Hashing.GetHashCodeOrdinal(s.AsSpan());
        private protected override int GetHashCode(ReadOnlySpan<char> s) => Hashing.GetHashCodeOrdinal(s);
        private protected override bool CheckLengthQuick(uint length) => (_lengthFilter & (1UL << (int)(length % 64))) > 0;
    }
}
