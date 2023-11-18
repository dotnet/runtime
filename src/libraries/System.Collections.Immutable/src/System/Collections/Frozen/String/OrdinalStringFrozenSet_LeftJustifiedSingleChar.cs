// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Collections.Frozen
{
    internal sealed class OrdinalStringFrozenSet_LeftJustifiedSingleChar : OrdinalStringFrozenSet
    {
        internal OrdinalStringFrozenSet_LeftJustifiedSingleChar(
            string[] entries,
            IEqualityComparer<string> comparer,
            int minimumLength,
            int maximumLengthDiff,
            int hashIndex)
            : base(entries, comparer, minimumLength, maximumLengthDiff, hashIndex, 1)
        {
        }

        // This override is necessary to force the jit to emit the code in such a way that it
        // avoids virtual dispatch overhead when calling the Equals/GetHashCode methods. Don't
        // remove this, or you'll tank performance.
        private protected override int FindItemIndex(string item) => base.FindItemIndex(item);

        private protected override bool Equals(string? x, string? y) => string.Equals(x, y);
        private protected override int GetHashCode(string s) => s[HashIndex];
    }
}
