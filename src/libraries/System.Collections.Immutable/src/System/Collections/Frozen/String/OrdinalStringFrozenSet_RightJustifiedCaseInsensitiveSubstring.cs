// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Collections.Frozen
{
    internal sealed class OrdinalStringFrozenSet_RightJustifiedCaseInsensitiveSubstring : OrdinalStringFrozenSet
    {
        internal OrdinalStringFrozenSet_RightJustifiedCaseInsensitiveSubstring(
            string[] entries,
            IEqualityComparer<string> comparer,
            int minimumLength,
            int maximumLengthDiff,
            int hashIndex,
            int hashCount)
            : base(entries, comparer, minimumLength, maximumLengthDiff, hashIndex, hashCount)
        {
        }

        // This override is necessary to force the jit to emit the code in such a way that it
        // avoids virtual dispatch overhead when calling the Equals/GetHashCode methods. Don't
        // remove this, or you'll tank performance.
        private protected override int FindItemIndex(string item) => base.FindItemIndex(item);

        private protected override bool Equals(string? x, string? y) => StringComparer.OrdinalIgnoreCase.Equals(x, y);
        private protected override int GetHashCode(string s) => Hashing.GetHashCodeOrdinalIgnoreCase(s.AsSpan(s.Length + HashIndex, HashCount));
    }
}
