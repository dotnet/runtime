// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Interface:  IDictionary
** 
** 
**
**
** Purpose: Base interface for all generic dictionaries.
**
** 
===========================================================*/
namespace System.Collections.Generic {
    using System;
    using System.Diagnostics.Contracts;

    // An IDictionary is a possibly unordered set of key-value pairs.
    // Keys can be any non-null object.  Values can be any object.
    // You can look up a value in an IDictionary via the default indexed
    // property, Items.  
#if CONTRACTS_FULL
    [ContractClass(typeof(IDictionaryContract<,>))]
#endif // CONTRACTS_FULL
    public interface IDictionary<TKey, TValue> : ICollection<KeyValuePair<TKey, TValue>>
    {
        // Interfaces are not serializable
        // The Item property provides methods to read and edit entries 
        // in the Dictionary.
        TValue this[TKey key] {
            get;
            set;
        }
    
        // Returns a collections of the keys in this dictionary.
        ICollection<TKey> Keys {
            get;
        }
    
        // Returns a collections of the values in this dictionary.
        ICollection<TValue> Values {
            get;
        }
    
        // Returns whether this dictionary contains a particular key.
        //
        bool ContainsKey(TKey key);
    
        // Adds a key-value pair to the dictionary.
        // 
        void Add(TKey key, TValue value);
    
        // Removes a particular key from the dictionary.
        //
        bool Remove(TKey key);

        bool TryGetValue(TKey key, out TValue value);
    }

#if CONTRACTS_FULL
    [ContractClassFor(typeof(IDictionary<,>))]
    internal abstract class IDictionaryContract<TKey, TValue> : IDictionary<TKey, TValue>
    {
        TValue IDictionary<TKey, TValue>.this[TKey key] {
            get { return default(TValue); }
            set { }
        }
    
        ICollection<TKey> IDictionary<TKey, TValue>.Keys {
            get {
                Contract.Ensures(Contract.Result<ICollection<TKey>>() != null);
                return default(ICollection<TKey>);
            }
        }
    
        // Returns a collections of the values in this dictionary.
        ICollection<TValue> IDictionary<TKey, TValue>.Values {
            get {
                Contract.Ensures(Contract.Result<ICollection<TValue>>() != null);
                return default(ICollection<TValue>);
            }
        }

        bool IDictionary<TKey, TValue>.ContainsKey(TKey key)
        {
            return default(bool);
        }

        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
        }

        bool IDictionary<TKey, TValue>.Remove(TKey key)
        {
            //Contract.Ensures(Contract.Result<bool>() == false || ((ICollection<KeyValuePair<TKey,TValue>>)this).Count == Contract.OldValue(((ICollection<KeyValuePair<TKey,TValue>>)this).Count) - 1);  // not threadsafe
            return default(bool);
        }

        bool IDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value)
        {
            value = default(TValue);
            return default(bool);
        }

        #region ICollection<KeyValuePair<TKey, TValue>> Members

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> value)
        {
            //Contract.Ensures(((ICollection<KeyValuePair<TKey, TValue>>)this).Count == Contract.OldValue(((ICollection<KeyValuePair<TKey, TValue>>)this).Count) + 1);  // not threadsafe
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get { return default(bool); }
        }

        int ICollection<KeyValuePair<TKey, TValue>>.Count
        {
            get {
                return default(int);
            }
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Clear()
        {
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> value)
        {
            // Contract.Ensures(((ICollection<KeyValuePair<TKey, TValue>>)this).Count > 0 || Contract.Result<bool>() == false); // not threadsafe
            return default(bool);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int startIndex)
        {
            //Contract.Requires(array != null);
            //Contract.Requires(startIndex >= 0);
            //Contract.Requires(startIndex + ((ICollection<KeyValuePair<TKey, TValue>>)this).Count <= array.Length);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> value)
        {
            // No information if removal fails.
            return default(bool);
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return default(IEnumerator<KeyValuePair<TKey, TValue>>);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return default(IEnumerator);
        }

        #endregion
    }
#endif // CONTRACTS_FULL
}
