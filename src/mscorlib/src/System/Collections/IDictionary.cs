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
** Purpose: Base interface for all dictionaries.
**
** 
===========================================================*/
namespace System.Collections {
    using System;
    using System.Diagnostics.Contracts;

    // An IDictionary is a possibly unordered set of key-value pairs.
    // Keys can be any non-null object.  Values can be any object.
    // You can look up a value in an IDictionary via the default indexed
    // property, Items.  
#if CONTRACTS_FULL
    [ContractClass(typeof(IDictionaryContract))]
#endif // CONTRACTS_FULL
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface IDictionary : ICollection
    {
        // Interfaces are not serializable
        // The Item property provides methods to read and edit entries 
        // in the Dictionary.
        Object this[Object key] {
            get;
            set;
        }
    
        // Returns a collections of the keys in this dictionary.
        ICollection Keys {
            get;
        }
    
        // Returns a collections of the values in this dictionary.
        ICollection Values {
            get;
        }
    
        // Returns whether this dictionary contains a particular key.
        //
        bool Contains(Object key);
    
        // Adds a key-value pair to the dictionary.
        // 
        void Add(Object key, Object value);
    
        // Removes all pairs from the dictionary.
        void Clear();
    
        bool IsReadOnly 
        { get; }

        bool IsFixedSize
        { get; }

        // Returns an IDictionaryEnumerator for this dictionary.
        new IDictionaryEnumerator GetEnumerator();
    
        // Removes a particular key from the dictionary.
        //
        void Remove(Object key);
    }

#if CONTRACTS_FULL
    [ContractClassFor(typeof(IDictionary))]
    internal abstract class IDictionaryContract : IDictionary
    {
        Object IDictionary.this[Object key] {
            get { return default(Object); }
            set { }
        }

        ICollection IDictionary.Keys {
            get {
                Contract.Ensures(Contract.Result<ICollection>() != null);
                //Contract.Ensures(Contract.Result<ICollection>().Count == ((ICollection)this).Count);
                return default(ICollection);
            }
        }

        ICollection IDictionary.Values {
            get {
                Contract.Ensures(Contract.Result<ICollection>() != null);
                return default(ICollection);
            }
        }

        bool IDictionary.Contains(Object key)
        {
            return default(bool);
        }

        void IDictionary.Add(Object key, Object value)
        {
        }

        void IDictionary.Clear()
        {
        }

        bool IDictionary.IsReadOnly {
            get { return default(bool); }
        }

        bool IDictionary.IsFixedSize { 
            get { return default(bool); }
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            Contract.Ensures(Contract.Result<IDictionaryEnumerator>() != null);
            return default(IDictionaryEnumerator);
        }

        void IDictionary.Remove(Object key)
        {
        }

        #region ICollection members

        void ICollection.CopyTo(Array array, int index)
        {
        }

        int ICollection.Count { 
            get {
                return default(int);
            }
        }

        Object ICollection.SyncRoot {
            get {
                return default(Object);
            }
        }

        bool ICollection.IsSynchronized {
            get { return default(bool); }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return default(IEnumerator);
        }

        #endregion ICollection Members
    }
#endif // CONTRACTS_FULL
}
