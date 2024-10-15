// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Collections.Frozen
{
    internal sealed class OrdinalStringFrozenDictionary_Full<TValue> : OrdinalStringFrozenDictionary<TValue>
    {
        private readonly ulong _lengthFilter;

        internal OrdinalStringFrozenDictionary_Full(
            string[] keys,
            TValue[] values,
            IEqualityComparer<string> comparer,
            int minimumLength,
            int maximumLengthDiff,
            ulong lengthFilter)
            : base(keys, values, comparer, minimumLength, maximumLengthDiff)
        {
            _lengthFilter = lengthFilter;
        }

        // See comment in OrdinalStringFrozenDictionary for why these overrides exist. Do not remove.
        private protected override ref readonly TValue GetValueRefOrNullRefCore(string key) => ref base.GetValueRefOrNullRefCore(key);
        private protected override ref readonly TValue GetValueRefOrNullRefCore<TAlternateKey>(TAlternateKey key) => ref base.GetValueRefOrNullRefCore(key);

        private protected override bool Equals(string? x, string? y) => string.Equals(x, y);
        private protected override bool Equals(ReadOnlySpan<char> x, string? y) => x.SequenceEqual(y.AsSpan());
        private protected override int GetHashCode(string s) => Hashing.GetHashCodeOrdinal(s.AsSpan());
        private protected override int GetHashCode(ReadOnlySpan<char> s) => Hashing.GetHashCodeOrdinal(s);
        private protected override bool CheckLengthQuick(uint length) => (_lengthFilter & (1UL << (int)(length % 64))) > 0;
    }
}
