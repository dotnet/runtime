// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
** Interface:  IReadOnlyDictionary<TKey, TValue>
** 
** 
**
** Purpose: Base interface for read-only generic dictionaries.
** 
===========================================================*/
using System;
using System.Diagnostics.Contracts;

namespace System.Collections.Generic
{
    // Provides a read-only view of a generic dictionary.
#if CONTRACTS_FULL
    [ContractClass(typeof(IReadOnlyDictionaryContract<,>))]
#endif
    public interface IReadOnlyDictionary<TKey, TValue> : IReadOnlyCollection<KeyValuePair<TKey, TValue>>
    {
        bool ContainsKey(TKey key);
        bool TryGetValue(TKey key, out TValue value);

        TValue this[TKey key] { get; }
        IEnumerable<TKey> Keys { get; }
        IEnumerable<TValue> Values { get; }
    }

#if CONTRACTS_FULL
    [ContractClassFor(typeof(IReadOnlyDictionary<,>))]
    internal abstract class IReadOnlyDictionaryContract<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {
        bool IReadOnlyDictionary<TKey, TValue>.ContainsKey(TKey key)
        {
            return default(bool);
        }

        bool IReadOnlyDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value)
        {
            value = default(TValue);
            return default(bool);
        }

        TValue IReadOnlyDictionary<TKey, TValue>.this[TKey key]
        {
            get { return default(TValue); }
        }

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys {
            get {
                Contract.Ensures(Contract.Result<IEnumerable<TKey>>() != null);
                return default(IEnumerable<TKey>);
            }
        }

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values {
            get {
                Contract.Ensures(Contract.Result<IEnumerable<TValue>>() != null);
                return default(IEnumerable<TValue>);
            }
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return default(IEnumerator<KeyValuePair<TKey, TValue>>);
        }

        int IReadOnlyCollection<KeyValuePair<TKey, TValue>>.Count {
            get {
                return default(int);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return default(IEnumerator);
        }
    }
#endif
}
