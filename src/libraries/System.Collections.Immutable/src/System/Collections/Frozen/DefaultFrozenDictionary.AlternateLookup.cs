// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen
{
    internal sealed partial class DefaultFrozenDictionary<TKey, TValue>
    {
        /// <inheritdoc/>
        private protected override ref readonly TValue GetValueRefOrNullRefCore<TAlternateKey>(TAlternateKey key)
        {
            IAlternateEqualityComparer<TAlternateKey, TKey> comparer = GetAlternateEqualityComparer<TAlternateKey>();

            int hashCode = comparer.GetHashCode(key);
            _hashTable.FindMatchingEntries(hashCode, out int index, out int endIndex);

            while (index <= endIndex)
            {
                if (hashCode == _hashTable.HashCodes[index] && comparer.Equals(key, _keys[index]))
                {
                    return ref _values[index];
                }

                index++;
            }

            return ref Unsafe.NullRef<TValue>();
        }
    }
}
