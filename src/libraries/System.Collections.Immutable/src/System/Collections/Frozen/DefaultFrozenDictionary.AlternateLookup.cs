// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen
{
    internal sealed partial class DefaultFrozenDictionary<TKey, TValue>
    {

        private protected override AlternateLookupDelegate<TAlternateKey> GetAlternateLookupDelegate<TAlternateKey>()
            => AlternateKeyDelegateHolder<TAlternateKey>.Instance;

        private static class AlternateKeyDelegateHolder<TAlternateKey>
            where TAlternateKey : notnull
#if NET9_0_OR_GREATER
#pragma warning disable SA1001 // Commas should be spaced correctly
            , allows ref struct
#pragma warning restore SA1001
#endif
        {
            public static AlternateLookupDelegate<TAlternateKey> Instance = (dictionary, key)
                => ref ((DefaultFrozenDictionary<TKey, TValue>)dictionary).GetValueRefOrNullRefCoreAlternate(key);
        }

        private ref readonly TValue GetValueRefOrNullRefCoreAlternate<TAlternateKey>(TAlternateKey key)
            where TAlternateKey : notnull
#if NET9_0_OR_GREATER
#pragma warning disable SA1001 // Commas should be spaced correctly
            , allows ref struct
#pragma warning restore SA1001
#endif
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
