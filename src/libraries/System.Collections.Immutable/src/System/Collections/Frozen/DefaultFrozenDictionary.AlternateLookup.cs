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
        private protected override AlternateLookupDelegate<TAlternateKey> GetAlternateLookupDelegate<TAlternateKey>()
            => AlternateLookupDelegateHolder<TAlternateKey>.Instance;

        private static class AlternateLookupDelegateHolder<TAlternateKey>
            where TAlternateKey : notnull
#if NET9_0_OR_GREATER
#pragma warning disable SA1001 // Commas should be spaced correctly
            , allows ref struct
#pragma warning restore SA1001
#endif
        {
            /// <summary>
            /// Invokes <see cref="GetValueRefOrNullRefCoreAlternate{TAlternateKey}(TAlternateKey)"/>
            /// on instances known to be of type <see cref="DefaultFrozenDictionary{TKey, TValue}"/>.
            /// </summary>
            public static readonly AlternateLookupDelegate<TAlternateKey> Instance = (dictionary, key)
                => ref ((DefaultFrozenDictionary<TKey, TValue>)dictionary).GetValueRefOrNullRefCoreAlternate(key);
        }

        /// <inheritdoc cref="GetValueRefOrNullRefCore(TKey)" />
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
