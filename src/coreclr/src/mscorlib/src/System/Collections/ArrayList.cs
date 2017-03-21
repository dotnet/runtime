// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** Purpose: Implements a dynamically sized List as an array,
**          and provides many convenience methods for treating
**          an array as an IList.
**
** 
===========================================================*/

using System;
using System.Runtime;
using System.Security;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

namespace System.Collections
{
    // Implements a variable-size List that uses an array of objects to store the
    // elements. A ArrayList has a capacity, which is the allocated length
    // of the internal array. As elements are added to a ArrayList, the capacity
    // of the ArrayList is automatically increased as required by reallocating the
    // internal array.
    // 
    [FriendAccessAllowed]
    [DebuggerTypeProxy(typeof(System.Collections.ArrayList.ArrayListDebugView))]
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    internal class ArrayList : IList, ICloneable
    {
        private Object[] _items;
        [ContractPublicPropertyName("Count")]
        private int _size;
        private int _version;
        [NonSerialized]
        private Object _syncRoot;

        private const int _defaultCapacity = 4;
        private static readonly Object[] emptyArray = EmptyArray<Object>.Value;

        // Constructs a ArrayList. The list is initially empty and has a capacity
        // of zero. Upon adding the first element to the list the capacity is
        // increased to _defaultCapacity, and then increased in multiples of two as required.
        public ArrayList()
        {
            _items = emptyArray;
        }

        // Constructs a ArrayList with a given initial capacity. The list is
        // initially empty, but will have room for the given number of elements
        // before any reallocations are required.
        // 
        public ArrayList(int capacity)
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity), SR.Format(SR.ArgumentOutOfRange_MustBeNonNegNum, nameof(capacity)));
            Contract.EndContractBlock();

            if (capacity == 0)
                _items = emptyArray;
            else
                _items = new Object[capacity];
        }

        // Constructs a ArrayList, copying the contents of the given collection. The
        // size and capacity of the new list will both be equal to the size of the
        // given collection.
        // 
        public ArrayList(ICollection c)
        {
            if (c == null)
                throw new ArgumentNullException(nameof(c), SR.ArgumentNull_Collection);
            Contract.EndContractBlock();

            int count = c.Count;
            if (count == 0)
            {
                _items = emptyArray;
            }
            else
            {
                _items = new Object[count];
                AddRange(c);
            }
        }

        // Gets and sets the capacity of this list.  The capacity is the size of
        // the internal array used to hold items.  When set, the internal 
        // array of the list is reallocated to the given capacity.
        // 
        public virtual int Capacity
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= Count);
                return _items.Length;
            }
            set
            {
                if (value < _size)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_SmallCapacity);
                }
                Contract.Ensures(Capacity >= 0);
                Contract.EndContractBlock();
                // We don't want to update the version number when we change the capacity.
                // Some existing applications have dependency on this.
                if (value != _items.Length)
                {
                    if (value > 0)
                    {
                        Object[] newItems = new Object[value];
                        if (_size > 0)
                        {
                            Array.Copy(_items, 0, newItems, 0, _size);
                        }
                        _items = newItems;
                    }
                    else
                    {
                        _items = new Object[_defaultCapacity];
                    }
                }
            }
        }

        // Read-only property describing how many elements are in the List.
        public virtual int Count
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return _size;
            }
        }

        public virtual bool IsFixedSize
        {
            get { return false; }
        }


        // Is this ArrayList read-only?
        public virtual bool IsReadOnly
        {
            get { return false; }
        }

        // Is this ArrayList synchronized (thread-safe)?
        public virtual bool IsSynchronized
        {
            get { return false; }
        }

        // Synchronization root for this object.
        public virtual Object SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    System.Threading.Interlocked.CompareExchange<Object>(ref _syncRoot, new Object(), null);
                }
                return _syncRoot;
            }
        }

        // Sets or Gets the element at the given index.
        // 
        public virtual Object this[int index]
        {
            get
            {
                if (index < 0 || index >= _size) throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_Index);
                Contract.EndContractBlock();
                return _items[index];
            }
            set
            {
                if (index < 0 || index >= _size) throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_Index);
                Contract.EndContractBlock();
                _items[index] = value;
                _version++;
            }
        }

        // Adds the given object to the end of this list. The size of the list is
        // increased by one. If required, the capacity of the list is doubled
        // before adding the new element.
        //
        public virtual int Add(Object value)
        {
            Contract.Ensures(Contract.Result<int>() >= 0);
            if (_size == _items.Length) EnsureCapacity(_size + 1);
            _items[_size] = value;
            _version++;
            return _size++;
        }

        // Adds the elements of the given collection to the end of this list. If
        // required, the capacity of the list is increased to twice the previous
        // capacity or the new size, whichever is larger.
        //
        public virtual void AddRange(ICollection c)
        {
            InsertRange(_size, c);
        }


        // Clears the contents of ArrayList.
        public virtual void Clear()
        {
            if (_size > 0)
            {
                Array.Clear(_items, 0, _size); // Don't need to doc this but we clear the elements so that the gc can reclaim the references.
                _size = 0;
            }
            _version++;
        }

        // Clones this ArrayList, doing a shallow copy.  (A copy is made of all
        // Object references in the ArrayList, but the Objects pointed to 
        // are not cloned).
        public virtual Object Clone()
        {
            Contract.Ensures(Contract.Result<Object>() != null);
            ArrayList la = new ArrayList(_size);
            la._size = _size;
            la._version = _version;
            Array.Copy(_items, 0, la._items, 0, _size);
            return la;
        }


        // Contains returns true if the specified element is in the ArrayList.
        // It does a linear, O(n) search.  Equality is determined by calling
        // item.Equals().
        //
        public virtual bool Contains(Object item)
        {
            if (item == null)
            {
                for (int i = 0; i < _size; i++)
                    if (_items[i] == null)
                        return true;
                return false;
            }
            else
            {
                for (int i = 0; i < _size; i++)
                    if ((_items[i] != null) && (_items[i].Equals(item)))
                        return true;
                return false;
            }
        }

        // Copies this ArrayList into array, which must be of a 
        // compatible array type.  
        //
        public virtual void CopyTo(Array array, int arrayIndex)
        {
            if ((array != null) && (array.Rank != 1))
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported);
            Contract.EndContractBlock();
            // Delegate rest of error checking to Array.Copy.
            Array.Copy(_items, 0, array, arrayIndex, _size);
        }

        // Ensures that the capacity of this list is at least the given minimum
        // value. If the currect capacity of the list is less than min, the
        // capacity is increased to twice the current capacity or to min,
        // whichever is larger.
        private void EnsureCapacity(int min)
        {
            if (_items.Length < min)
            {
                int newCapacity = _items.Length == 0 ? _defaultCapacity : _items.Length * 2;
                // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
                // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
                if ((uint)newCapacity > Array.MaxArrayLength) newCapacity = Array.MaxArrayLength;
                if (newCapacity < min) newCapacity = min;
                Capacity = newCapacity;
            }
        }

        // Returns an enumerator for this list with the given
        // permission for removal of elements. If modifications made to the list 
        // while an enumeration is in progress, the MoveNext and 
        // GetObject methods of the enumerator will throw an exception.
        //
        public virtual IEnumerator GetEnumerator()
        {
            Contract.Ensures(Contract.Result<IEnumerator>() != null);
            return new ArrayListEnumeratorSimple(this);
        }

        // Returns the index of the first occurrence of a given value in a range of
        // this list. The list is searched forwards from beginning to end.
        // The elements of the list are compared to the given value using the
        // Object.Equals method.
        // 
        // This method uses the Array.IndexOf method to perform the
        // search.
        // 
        public virtual int IndexOf(Object value)
        {
            Contract.Ensures(Contract.Result<int>() < Count);
            return Array.IndexOf((Array)_items, value, 0, _size);
        }

        // Inserts an element into this list at a given index. The size of the list
        // is increased by one. If required, the capacity of the list is doubled
        // before inserting the new element.
        // 
        public virtual void Insert(int index, Object value)
        {
            // Note that insertions at the end are legal.
            if (index < 0 || index > _size) throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_ArrayListInsert);
            //Contract.Ensures(Count == Contract.OldValue(Count) + 1);
            Contract.EndContractBlock();

            if (_size == _items.Length) EnsureCapacity(_size + 1);
            if (index < _size)
            {
                Array.Copy(_items, index, _items, index + 1, _size - index);
            }
            _items[index] = value;
            _size++;
            _version++;
        }

        // Inserts the elements of the given collection at a given index. If
        // required, the capacity of the list is increased to twice the previous
        // capacity or the new size, whichever is larger.  Ranges may be added
        // to the end of the list by setting index to the ArrayList's size.
        //
        public virtual void InsertRange(int index, ICollection c)
        {
            if (c == null)
                throw new ArgumentNullException(nameof(c), SR.ArgumentNull_Collection);
            if (index < 0 || index > _size) throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_Index);
            //Contract.Ensures(Count == Contract.OldValue(Count) + c.Count);
            Contract.EndContractBlock();

            int count = c.Count;
            if (count > 0)
            {
                EnsureCapacity(_size + count);
                // shift existing items
                if (index < _size)
                {
                    Array.Copy(_items, index, _items, index + count, _size - index);
                }

                Object[] itemsToInsert = new Object[count];
                c.CopyTo(itemsToInsert, 0);
                itemsToInsert.CopyTo(_items, index);
                _size += count;
                _version++;
            }
        }

        // Returns a read-only IList wrapper for the given IList.
        //
        [FriendAccessAllowed]
        public static IList ReadOnly(IList list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            Contract.Ensures(Contract.Result<IList>() != null);
            Contract.EndContractBlock();
            return new ReadOnlyList(list);
        }

        // Removes the element at the given index. The size of the list is
        // decreased by one.
        // 
        public virtual void Remove(Object obj)
        {
            Contract.Ensures(Count >= 0);

            int index = IndexOf(obj);
            BCLDebug.Correctness(index >= 0 || !(obj is Int32), "You passed an Int32 to Remove that wasn't in the ArrayList." + Environment.NewLine + "Did you mean RemoveAt?  int: " + obj + "  Count: " + Count);
            if (index >= 0)
                RemoveAt(index);
        }

        // Removes the element at the given index. The size of the list is
        // decreased by one.
        // 
        public virtual void RemoveAt(int index)
        {
            if (index < 0 || index >= _size) throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_Index);
            Contract.Ensures(Count >= 0);
            //Contract.Ensures(Count == Contract.OldValue(Count) - 1);
            Contract.EndContractBlock();

            _size--;
            if (index < _size)
            {
                Array.Copy(_items, index + 1, _items, index, _size - index);
            }
            _items[_size] = null;
            _version++;
        }

        // ToArray returns a new array of a particular type containing the contents 
        // of the ArrayList.  This requires copying the ArrayList and potentially
        // downcasting all elements.  This copy may fail and is an O(n) operation.
        // Internally, this implementation calls Array.Copy.
        //
        public virtual Array ToArray(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            Contract.Ensures(Contract.Result<Array>() != null);
            Contract.EndContractBlock();
            Array array = Array.UnsafeCreateInstance(type, _size);
            Array.Copy(_items, 0, array, 0, _size);
            return array;
        }

        [Serializable]
        private class ReadOnlyList : IList
        {
            private IList _list;

            internal ReadOnlyList(IList l)
            {
                _list = l;
            }

            public virtual int Count
            {
                get { return _list.Count; }
            }

            public virtual bool IsReadOnly
            {
                get { return true; }
            }

            public virtual bool IsFixedSize
            {
                get { return true; }
            }

            public virtual bool IsSynchronized
            {
                get { return _list.IsSynchronized; }
            }

            public virtual Object this[int index]
            {
                get
                {
                    return _list[index];
                }
                set
                {
                    throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
                }
            }

            public virtual Object SyncRoot
            {
                get { return _list.SyncRoot; }
            }

            public virtual int Add(Object obj)
            {
                throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
            }

            public virtual void Clear()
            {
                throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
            }

            public virtual bool Contains(Object obj)
            {
                return _list.Contains(obj);
            }

            public virtual void CopyTo(Array array, int index)
            {
                _list.CopyTo(array, index);
            }

            public virtual IEnumerator GetEnumerator()
            {
                return _list.GetEnumerator();
            }

            public virtual int IndexOf(Object value)
            {
                return _list.IndexOf(value);
            }

            public virtual void Insert(int index, Object obj)
            {
                throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
            }

            public virtual void Remove(Object value)
            {
                throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
            }

            public virtual void RemoveAt(int index)
            {
                throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
            }
        }

        [Serializable]
        private sealed class ArrayListEnumeratorSimple : IEnumerator, ICloneable
        {
            private ArrayList list;
            private int index;
            private int version;
            private Object currentElement;
            [NonSerialized]
            private bool isArrayList;
            // this object is used to indicate enumeration has not started or has terminated
            private static Object dummyObject = new Object();

            internal ArrayListEnumeratorSimple(ArrayList list)
            {
                this.list = list;
                index = -1;
                version = list._version;
                isArrayList = (list.GetType() == typeof(ArrayList));
                currentElement = dummyObject;
            }

            public Object Clone()
            {
                return MemberwiseClone();
            }

            public bool MoveNext()
            {
                if (version != list._version)
                {
                    throw new InvalidOperationException(SR.GetResourceString(ResId.InvalidOperation_EnumFailedVersion));
                }

                if (isArrayList)
                {  // avoid calling virtual methods if we are operating on ArrayList to improve performance
                    if (index < list._size - 1)
                    {
                        currentElement = list._items[++index];
                        return true;
                    }
                    else
                    {
                        currentElement = dummyObject;
                        index = list._size;
                        return false;
                    }
                }
                else
                {
                    if (index < list.Count - 1)
                    {
                        currentElement = list[++index];
                        return true;
                    }
                    else
                    {
                        index = list.Count;
                        currentElement = dummyObject;
                        return false;
                    }
                }
            }

            public Object Current
            {
                get
                {
                    object temp = currentElement;
                    if (dummyObject == temp)
                    { // check if enumeration has not started or has terminated
                        if (index == -1)
                        {
                            throw new InvalidOperationException(SR.GetResourceString(ResId.InvalidOperation_EnumNotStarted));
                        }
                        else
                        {
                            throw new InvalidOperationException(SR.GetResourceString(ResId.InvalidOperation_EnumEnded));
                        }
                    }

                    return temp;
                }
            }

            public void Reset()
            {
                if (version != list._version)
                {
                    throw new InvalidOperationException(SR.GetResourceString(ResId.InvalidOperation_EnumFailedVersion));
                }

                currentElement = dummyObject;
                index = -1;
            }
        }

        internal class ArrayListDebugView
        {
            private ArrayList arrayList;
        }
    }
}
