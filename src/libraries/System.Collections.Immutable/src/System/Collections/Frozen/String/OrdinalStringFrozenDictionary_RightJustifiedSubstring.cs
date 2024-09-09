// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Collections.Frozen
{
    internal sealed class OrdinalStringFrozenDictionary_RightJustifiedSubstring<TValue> : OrdinalStringFrozenDictionary<TValue>
    {
        internal OrdinalStringFrozenDictionary_RightJustifiedSubstring(
            string[] keys,
            TValue[] values,
            IEqualityComparer<string> comparer,
            int minimumLength,
            int maximumLengthDiff,
            int hashIndex,
            int hashCount)
            : base(keys, values, comparer, minimumLength, maximumLengthDiff, hashIndex, hashCount)
        {
        }

        // See comment in OrdinalStringFrozenDictionary for why these overrides exist. Do not remove.
        private protected override ref readonly TValue GetValueRefOrNullRefCore(string key) => ref base.GetValueRefOrNullRefCore(key);
        private protected override ref readonly TValue GetValueRefOrNullRefCore<TAlternateKey>(TAlternateKey key) => ref base.GetValueRefOrNullRefCore(key);

        private protected override bool Equals(string? x, string? y) => string.Equals(x, y);
        private protected override bool Equals(ReadOnlySpan<char> x, string? y) => x.SequenceEqual(y.AsSpan());
        private protected override int GetHashCode(string s) => Hashing.GetHashCodeOrdinal(s.AsSpan(s.Length + HashIndex, HashCount));
        private protected override int GetHashCode(ReadOnlySpan<char> s) => Hashing.GetHashCodeOrdinal(s.Slice(s.Length + HashIndex, HashCount));
    }
}
