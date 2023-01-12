// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen
{
    /// <summary>Provides a frozen dictionary optimized for integer keys using the default comparer.</summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    internal sealed class IntegerFrozenDictionary<TKey, TValue> : KeysAndValuesFrozenDictionary<TKey, TValue>, IDictionary<TKey, TValue>
        where TKey : struct, IBinaryInteger<TKey>
    {
        internal IntegerFrozenDictionary(Dictionary<TKey, TValue> source) :
            base(source, EqualityComparer<TKey>.Default)
        {
            Debug.Assert(typeof(TKey).IsValueType);
        }

        /// <inheritdoc />
        private protected override ref readonly TValue GetValueRefOrNullRefCore(TKey key)
        {
            int hashCode = EqualityComparer<TKey>.Default.GetHashCode(key);
            _hashTable.FindMatchingEntries(hashCode, out int index, out int endIndex);

            while (index <= endIndex)
            {
                if (hashCode == _hashTable.HashCodes[index])
                {
                    if (key == _keys[index])
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
