// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen
{
    internal sealed partial class SmallFrozenDictionary<TKey, TValue>
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
            /// on instances known to be of type <see cref="SmallFrozenDictionary{TKey, TValue}"/>.
            /// </summary>
            public static readonly AlternateLookupDelegate<TAlternateKey> Instance = (dictionary, key)
                => ref ((SmallFrozenDictionary<TKey, TValue>)dictionary).GetValueRefOrNullRefCoreAlternate(key);
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

            TKey[] keys = _keys;
            for (int i = 0; i < keys.Length; i++)
            {
                if (comparer.Equals(key, keys[i]))
                {
                    return ref _values[i];
                }
            }

            return ref Unsafe.NullRef<TValue>();
        }
    }
}
