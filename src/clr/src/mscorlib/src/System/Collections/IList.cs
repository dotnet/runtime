// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Interface:  IList
** 
** 
**
**
** Purpose: Base interface for all Lists.
**
** 
===========================================================*/
namespace System.Collections {
    
    using System;
    using System.Diagnostics.Contracts;

    // An IList is an ordered collection of objects.  The exact ordering
    // is up to the implementation of the list, ranging from a sorted
    // order to insertion order.  
#if CONTRACTS_FULL
    [ContractClass(typeof(IListContract))]
#endif // CONTRACTS_FULL
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface IList : ICollection
    {
        // The Item property provides methods to read and edit entries in the List.
        Object this[int index] {
            get;
            set;
        }
    
        // Adds an item to the list.  The exact position in the list is 
        // implementation-dependent, so while ArrayList may always insert
        // in the last available location, a SortedList most likely would not.
        // The return value is the position the new element was inserted in.
        int Add(Object value);
    
        // Returns whether the list contains a particular item.
        bool Contains(Object value);
    
        // Removes all items from the list.
        void Clear();

        bool IsReadOnly 
        { get; }

    
        bool IsFixedSize
        {
            get;
        }

        
        // Returns the index of a particular item, if it is in the list.
        // Returns -1 if the item isn't in the list.
        int IndexOf(Object value);
    
        // Inserts value into the list at position index.
        // index must be non-negative and less than or equal to the 
        // number of elements in the list.  If index equals the number
        // of items in the list, then value is appended to the end.
        void Insert(int index, Object value);
    
        // Removes an item from the list.
        void Remove(Object value);
    
        // Removes the item at position index.
        void RemoveAt(int index);
    }

#if CONTRACTS_FULL
    [ContractClassFor(typeof(IList))]
    internal abstract class IListContract : IList
    {
        int IList.Add(Object value)
        {
            //Contract.Ensures(((IList)this).Count == Contract.OldValue(((IList)this).Count) + 1);  // Not threadsafe
            // This method should return the index in which an item was inserted, but we have
            // some internal collections that don't always insert items into the list, as well
            // as an MSDN sample code showing us returning -1.  Allow -1 to mean "did not insert".
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(Contract.Result<int>() < ((IList)this).Count);
            return default(int);
        }

        Object IList.this[int index] {
            get {
                //Contract.Requires(index >= 0);
                //Contract.Requires(index < ((IList)this).Count);
                return default(int);
            }
            set {
                //Contract.Requires(index >= 0);
                //Contract.Requires(index < ((IList)this).Count);
            }
        }

        bool IList.IsFixedSize {
            get { return default(bool); }
        }

        bool IList.IsReadOnly {
            get { return default(bool); }
        }

        bool ICollection.IsSynchronized {
            get { return default(bool); }
        }

        void IList.Clear()
        {
            //Contract.Ensures(((IList)this).Count == 0  || ((IList)this).IsFixedSize);  // not threadsafe
        }

        bool IList.Contains(Object value)
        {
            return default(bool);
        }

        void ICollection.CopyTo(Array array, int startIndex)
        {
            //Contract.Requires(array != null);
            //Contract.Requires(startIndex >= 0);
            //Contract.Requires(startIndex + ((IList)this).Count <= array.Length);
        }

        int ICollection.Count {
            get {
                return default(int);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return default(IEnumerator);
        }

        [Pure]
        int IList.IndexOf(Object value)
        {
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(Contract.Result<int>() < ((IList)this).Count);
            return default(int);
        }

        void IList.Insert(int index, Object value)
        {
            //Contract.Requires(index >= 0);
            //Contract.Requires(index <= ((IList)this).Count);  // For inserting immediately after the end.
            //Contract.Ensures(((IList)this).Count == Contract.OldValue(((IList)this).Count) + 1);  // Not threadsafe
        }

        void IList.Remove(Object value)
        {
            // No information if removal fails.
        }

        void IList.RemoveAt(int index)
        {
            //Contract.Requires(index >= 0);
            //Contract.Requires(index < ((IList)this).Count);
            //Contract.Ensures(((IList)this).Count == Contract.OldValue(((IList)this).Count) - 1);  // Not threadsafe
        }
        
        Object ICollection.SyncRoot {
            get {
                return default(Object);
            }
        }
    }
#endif // CONTRACTS_FULL
}
