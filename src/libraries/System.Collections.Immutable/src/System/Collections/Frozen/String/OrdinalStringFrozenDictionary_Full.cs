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

        // This override is necessary to force the jit to emit the code in such a way that it
        // avoids virtual dispatch overhead when calling the Equals/GetHashCode methods. Don't
        // remove this, or you'll tank performance.
        private protected override ref readonly TValue GetValueRefOrNullRefCore(string key) => ref base.GetValueRefOrNullRefCore(key);

        private protected override bool Equals(string? x, string? y) => string.Equals(x, y);
        private protected override int GetHashCode(string s) => Hashing.GetHashCodeOrdinal(s.AsSpan());
        private protected override bool CheckLengthQuick(string key) => (_lengthFilter & (1UL << (key.Length & 0x3F))) > 0;
    }
}
