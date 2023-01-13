// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Collections.Frozen
{
    /// <summary>
    /// A comparer for ordinal case-insensitive string comparisons.
    /// </summary>
    /// <remarks>
    /// This code doesn't perform any error checks on the input as it assumes
    /// the data is always valid. This is ensured by precondition checks before
    /// a key is used to perform a dictionary lookup.
    /// </remarks>
    internal sealed class FullCaseInsensitiveStringComparer : StringComparerBase
    {
        public override bool Equals(string? x, string? y) => StringComparer.OrdinalIgnoreCase.Equals(x, y);
        public override int GetHashCode(string s) => GetHashCodeOrdinalIgnoreCase(s.AsSpan());
    }
}
