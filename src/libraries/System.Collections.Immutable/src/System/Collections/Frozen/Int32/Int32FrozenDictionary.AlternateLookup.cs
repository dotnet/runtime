// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen
{
    internal sealed partial class Int32FrozenDictionary<TValue>
    {
        /// <inheritdoc/>
        private protected override ref readonly TValue GetValueRefOrNullRefCore<TAlternateKey>(TAlternateKey key)
        {
            IAlternateEqualityComparer<TAlternateKey, int> comparer = GetAlternateEqualityComparer<TAlternateKey>();

            _hashTable.FindMatchingEntries(comparer.GetHashCode(key), out int index, out int endIndex);

            int[] hashCodes = _hashTable.HashCodes;
            while (index <= endIndex)
            {
                if (comparer.Equals(key, hashCodes[index]))
                {
                    return ref _values[index];
                }

                index++;
            }

            return ref Unsafe.NullRef<TValue>();
        }
    }
}
