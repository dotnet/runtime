// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Collections.Frozen
{
    /// <summary>
    /// A comparer that operates over a portion of the input strings.
    /// </summary>
    /// <remarks>
    /// This comparer looks from the start of input strings.
    ///
    /// This code doesn't perform any error checks on the input as it assumes
    /// the data is always valid. This is ensured by precondition checks before
    /// a key is used to perform a dictionary lookup.
    /// </remarks>
    internal sealed class LeftJustifiedSubstringComparer : SubstringComparerBase
    {
        public override bool Equals(string? x, string? y) => string.Equals(x, y);
        public override bool EqualsPartial(string? x, string? y) => x.AsSpan(Index, Count).SequenceEqual(y.AsSpan(Index, Count));
        public override int GetHashCode(string s) => GetHashCodeOrdinal(s.AsSpan(Index, Count));
    }
}
