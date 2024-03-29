// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Collections.Frozen
{
    internal sealed class OrdinalStringFrozenSet_FullCaseInsensitive : OrdinalStringFrozenSet
    {
        private readonly ulong _lengthFilter;

        internal OrdinalStringFrozenSet_FullCaseInsensitive(
            string[] entries,
            IEqualityComparer<string> comparer,
            int minimumLength,
            int maximumLengthDiff,
            ulong lengthFilter)
            : base(entries, comparer, minimumLength, maximumLengthDiff)
        {
            _lengthFilter = lengthFilter;
        }

        // This override is necessary to force the jit to emit the code in such a way that it
        // avoids virtual dispatch overhead when calling the Equals/GetHashCode methods. Don't
        // remove this, or you'll tank performance.
        private protected override int FindItemIndex(string item) => base.FindItemIndex(item);

        private protected override bool Equals(string? x, string? y) => StringComparer.OrdinalIgnoreCase.Equals(x, y);
        private protected override int GetHashCode(string s) => Hashing.GetHashCodeOrdinalIgnoreCase(s.AsSpan());
        private protected override bool CheckLengthQuick(string key) => (_lengthFilter & (1UL << (key.Length % 64))) > 0;
    }
}
