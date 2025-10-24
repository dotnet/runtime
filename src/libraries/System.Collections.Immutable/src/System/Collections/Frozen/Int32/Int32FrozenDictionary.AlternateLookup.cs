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
            /// Invokes <see cref="GetValueRefOrNullRefCoreAlternate{TAlternate}(TAlternate)"/>
            /// on instances known to be of type <see cref="Int32FrozenDictionary{TValue}"/>.
            /// </summary>
            public static readonly AlternateLookupDelegate<TAlternateKey> Instance = (dictionary, key)
                => ref ((Int32FrozenDictionary<TValue>)dictionary).GetValueRefOrNullRefCoreAlternate(key);
        }

        /// <inheritdoc cref="GetValueRefOrNullRefCore(int)" />
        private ref readonly TValue GetValueRefOrNullRefCoreAlternate<TAlternateKey>(TAlternateKey key)
            where TAlternateKey : notnull
#if NET9_0_OR_GREATER
#pragma warning disable SA1001 // Commas should be spaced correctly
            , allows ref struct
#pragma warning restore SA1001
#endif
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
