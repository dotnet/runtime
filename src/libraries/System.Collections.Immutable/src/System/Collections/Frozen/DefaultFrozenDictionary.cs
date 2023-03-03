// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen
{
    /// <summary>Provides the default <see cref="FrozenDictionary{TKey, TValue}"/> implementation to use when no other special-cases apply.</summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    internal sealed class DefaultFrozenDictionary<TKey, TValue> : KeysAndValuesFrozenDictionary<TKey, TValue>, IDictionary<TKey, TValue>
        where TKey : notnull
    {
        internal DefaultFrozenDictionary(Dictionary<TKey, TValue> source, IEqualityComparer<TKey> comparer) :
            base(source, comparer)
        {
        }

        /// <inheritdoc />
        private protected override ref readonly TValue GetValueRefOrNullRefCore(TKey key)
        {
            IEqualityComparer<TKey> comparer = Comparer;

            int hashCode = comparer.GetHashCode(key);
            _hashTable.FindMatchingEntries(hashCode, out int index, out int endIndex);

            while (index <= endIndex)
            {
                if (hashCode == _hashTable.HashCodes[index])
                {
                    if (comparer.Equals(key, _keys[index]))
                    {
                        return ref _values[index];
                    }
                }

                index++;
            }

            return ref Unsafe.NullRef<TValue>();
        }
    }
}
