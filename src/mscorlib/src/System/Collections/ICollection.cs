// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Interface:  ICollection
** 
** 
**
**
** Purpose: Base interface for all collections.
**
** 
===========================================================*/
namespace System.Collections {
    using System;
    using System.Diagnostics.Contracts;

    // Base interface for all collections, defining enumerators, size, and 
    // synchronization methods.
#if CONTRACTS_FULL
    [ContractClass(typeof(ICollectionContract))]
#endif // CONTRACTS_FULL
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface ICollection : IEnumerable
    {
        // Interfaces are not serialable
        // CopyTo copies a collection into an Array, starting at a particular
        // index into the array.
        // 
        void CopyTo(Array array, int index);
        
        // Number of items in the collections.
        int Count
        { get; }
        
        
        // SyncRoot will return an Object to use for synchronization 
        // (thread safety).  You can use this object in your code to take a
        // lock on the collection, even if this collection is a wrapper around
        // another collection.  The intent is to tunnel through to a real 
        // implementation of a collection, and use one of the internal objects
        // found in that code.
        //
        // In the absense of a static Synchronized method on a collection, 
        // the expected usage for SyncRoot would look like this:
        // 
        // ICollection col = ...
        // lock (col.SyncRoot) {
        //     // Some operation on the collection, which is now thread safe.
        //     // This may include multiple operations.
        // }
        // 
        // 
        // The system-provided collections have a static method called 
        // Synchronized which will create a thread-safe wrapper around the 
        // collection.  All access to the collection that you want to be 
        // thread-safe should go through that wrapper collection.  However, if
        // you need to do multiple calls on that collection (such as retrieving
        // two items, or checking the count then doing something), you should
        // NOT use our thread-safe wrapper since it only takes a lock for the
        // duration of a single method call.  Instead, use Monitor.Enter/Exit
        // or your language's equivalent to the C# lock keyword as mentioned 
        // above.
        // 
        // For collections with no publically available underlying store, the 
        // expected implementation is to simply return the this pointer.  Note 
        // that the this pointer may not be sufficient for collections that 
        // wrap other collections;  those should return the underlying 
        // collection's SyncRoot property.
        Object SyncRoot
        { get; }
            
        // Is this collection synchronized (i.e., thread-safe)?  If you want a 
        // thread-safe collection, you can use SyncRoot as an object to 
        // synchronize your collection with.  If you're using one of the 
        // collections in System.Collections, you could call the static 
        // Synchronized method to get a thread-safe wrapper around the 
        // underlying collection.
        bool IsSynchronized
        { get; }
    }

#if CONTRACTS_FULL
    [ContractClassFor(typeof(ICollection))]
    internal abstract class ICollectionContract : ICollection
    {
        void ICollection.CopyTo(Array array, int index)
        {
        }

        int ICollection.Count { 
            get {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return default(int);
            }
        }

        Object ICollection.SyncRoot {
            get {
                Contract.Ensures(Contract.Result<Object>() != null);
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
    }
#endif // CONTRACTS_FULL
}
