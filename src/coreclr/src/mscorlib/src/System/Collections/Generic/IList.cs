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
** Purpose: Base interface for all generic lists.
**
** 
===========================================================*/
namespace System.Collections.Generic {
    
    using System;
    using System.Collections;
    using System.Runtime.CompilerServices;
    using System.Diagnostics.Contracts;

    // An IList is an ordered collection of objects.  The exact ordering
    // is up to the implementation of the list, ranging from a sorted
    // order to insertion order.  

    // Note that T[] : IList<T>, and we want to ensure that if you use
    // IList<YourValueType>, we ensure a YourValueType[] can be used 
    // without jitting.  Hence the TypeDependencyAttribute on SZArrayHelper.
    // This is a special workaround internally though - see VM\compile.cpp.
    // The same attribute is on IEnumerable<T> and ICollection<T>.
    [TypeDependencyAttribute("System.SZArrayHelper")]
#if CONTRACTS_FULL
    [ContractClass(typeof(IListContract<>))]
#endif // CONTRACTS_FULL
    public interface IList<T> : ICollection<T>
    {
        // The Item property provides methods to read and edit entries in the List.
        T this[int index] {
            get;
            set;
        }
    
        // Returns the index of a particular item, if it is in the list.
        // Returns -1 if the item isn't in the list.
        int IndexOf(T item);
    
        // Inserts value into the list at position index.
        // index must be non-negative and less than or equal to the 
        // number of elements in the list.  If index equals the number
        // of items in the list, then value is appended to the end.
        void Insert(int index, T item);
        
        // Removes the item at position index.
        void RemoveAt(int index);
    }

#if CONTRACTS_FULL
    [ContractClassFor(typeof(IList<>))]
    internal abstract class IListContract<T> : IList<T>
    {
        T IList<T>.this[int index] {
            get {
                //Contract.Requires(index >= 0);
                //Contract.Requires(index < ((ICollection<T>)this).Count);
                return default(T);
            }
            set {
                //Contract.Requires(index >= 0);
                //Contract.Requires(index < ((ICollection<T>)this).Count);
            }
        }
        
        IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return default(IEnumerator);
        }
        
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return default(IEnumerator<T>);
        }

        [Pure]
        int IList<T>.IndexOf(T value)
        {
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(Contract.Result<int>() < ((ICollection<T>)this).Count);
            return default(int);
        }

        void IList<T>.Insert(int index, T value)
        {
            //Contract.Requires(index >= 0);
            //Contract.Requires(index <= ((ICollection<T>)this).Count);  // For inserting immediately after the end.
            //Contract.Ensures(((ICollection<T>)this).Count == Contract.OldValue(((ICollection<T>)this).Count) + 1);  // Not threadsafe
        }

        void IList<T>.RemoveAt(int index)
        {
            //Contract.Requires(index >= 0);
            //Contract.Requires(index < ((ICollection<T>)this).Count);
            //Contract.Ensures(((ICollection<T>)this).Count == Contract.OldValue(((ICollection<T>)this).Count) - 1);  // Not threadsafe
        }
        
        #region ICollection<T> Members

        void ICollection<T>.Add(T value)
        {
            //Contract.Ensures(((ICollection<T>)this).Count == Contract.OldValue(((ICollection<T>)this).Count) + 1);  // Not threadsafe
        }

        bool ICollection<T>.IsReadOnly {
            get { return default(bool); }
        }

        int ICollection<T>.Count {
            get {
                return default(int);
            }
        }

        void ICollection<T>.Clear()
        {
            // For fixed-sized collections like arrays, Clear will not change the Count property.
            // But we can't express that in a contract because we have no IsFixedSize property on
            // our generic collection interfaces.
        }

        bool ICollection<T>.Contains(T value)
        {
            return default(bool);
        }

        void ICollection<T>.CopyTo(T[] array, int startIndex)
        {
            //Contract.Requires(array != null);
            //Contract.Requires(startIndex >= 0);
            //Contract.Requires(startIndex + ((ICollection<T>)this).Count <= array.Length);
        }

        bool ICollection<T>.Remove(T value)
        {
            // No information if removal fails.
            return default(bool);
        }

        #endregion
    }
#endif // CONTRACTS_FULL
}
