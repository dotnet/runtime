// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Collections.Frozen
{
    internal sealed class OrdinalStringFrozenDictionary_LeftJustifiedSingleChar<TValue> : OrdinalStringFrozenDictionary<TValue>
    {
        internal OrdinalStringFrozenDictionary_LeftJustifiedSingleChar(
            string[] keys,
            TValue[] values,
            IEqualityComparer<string> comparer,
            int minimumLength,
            int maximumLengthDiff,
            int hashIndex)
            : base(keys, values, comparer, minimumLength, maximumLengthDiff, hashIndex, 1)
        {
        }

        // This override is necessary to force the jit to emit the code in such a way that it
        // avoids virtual dispatch overhead when calling the Equals/GetHashCode methods. Don't
        // remove this, or you'll tank performance.
        private protected override ref readonly TValue GetValueRefOrNullRefCore(string key) => ref base.GetValueRefOrNullRefCore(key);

        private protected override bool Equals(string? x, string? y) => string.Equals(x, y);
        private protected override int GetHashCode(string s) => s[HashIndex];
    }
}
