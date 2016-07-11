// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
** Purpose: Implements a generic, dynamically sized list as an 
**          array.
**
** 
===========================================================*/
namespace System.Collections.Generic {

    using System;
    using System.Runtime;
    using System.Runtime.Versioning;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Collections.ObjectModel;
    using System.Security.Permissions;

    // Implements a variable-size List that uses an array of objects to store the
    // elements. A List has a capacity, which is the allocated length
    // of the internal array. As elements are added to a List, the capacity
    // of the List is automatically increased as required by reallocating the
    // internal array.
    // 
    [DebuggerTypeProxy(typeof(Mscorlib_CollectionDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    public class List<T> : IList<T>, System.Collections.IList, IReadOnlyList<T>
    {
        private const int _defaultCapacity = 4;

        private T[] _items;
        [ContractPublicPropertyName("Count")]
        private int _size;
        private int _version;
        [NonSerialized]
        private Object _syncRoot;
        
        static readonly T[]  _emptyArray = new T[0];        
            
        // Constructs a List. The list is initially empty and has a capacity
        // of zero. Upon adding the first element to the list the capacity is
        // increased to _defaultCapacity, and then increased in multiples of two
        // as required.
        public List() {
            _items = _emptyArray;
        }
    
        // Constructs a List with a given initial capacity. The list is
        // initially empty, but will have room for the given number of elements
        // before any reallocations are required.
        // 
        public List(int capacity) {
            if (capacity < 0) ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            Contract.EndContractBlock();

            if (capacity == 0)
                _items = _emptyArray;
            else
                _items = new T[capacity];
        }
    
        // Constructs a List, copying the contents of the given collection. The
        // size and capacity of the new list will both be equal to the size of the
        // given collection.
        // 
        public List(IEnumerable<T> collection) {
            if (collection==null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.collection);
            Contract.EndContractBlock();

            ICollection<T> c = collection as ICollection<T>;
            if( c != null) {
                int count = c.Count;
                if (count == 0)
                {
                    _items = _emptyArray;
                }
                else {
                    _items = new T[count];
                    c.CopyTo(_items, 0);
                    _size = count;
                }
            }    
            else {                
                _size = 0;
                _items = _emptyArray;
                // This enumerable could be empty.  Let Add allocate a new array, if needed.
                // Note it will also go to _defaultCapacity first, not 1, then 2, etc.
                
                using(IEnumerator<T> en = collection.GetEnumerator()) {
                    while(en.MoveNext()) {
                        Add(en.Current);                                    
                    }
                }
            }
        }
        
        // Gets and sets the capacity of this list.  The capacity is the size of
        // the internal array used to hold items.  When set, the internal 
        // array of the list is reallocated to the given capacity.
        // 
        public int Capacity {
            get {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return _items.Length;
            }
            set {
                if (value < _size) {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value, ExceptionResource.ArgumentOutOfRange_SmallCapacity);
                }
                Contract.EndContractBlock();

                if (value != _items.Length) {
                    if (value > 0) {
                        T[] newItems = new T[value];
                        if (_size > 0) {
                            Array.Copy(_items, 0, newItems, 0, _size);
                        }
                        _items = newItems;
                    }
                    else {
                        _items = _emptyArray;
                    }
                }
            }
        }
            
        // Read-only property describing how many elements are in the List.
        public int Count {
            get {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return _size; 
            }
        }

        bool System.Collections.IList.IsFixedSize {
            get { return false; }
        }

            
        // Is this List read-only?
        bool ICollection<T>.IsReadOnly {
            get { return false; }
        }

        bool System.Collections.IList.IsReadOnly {
            get { return false; }
        }

        // Is this List synchronized (thread-safe)?
        bool System.Collections.ICollection.IsSynchronized {
            get { return false; }
        }
    
        // Synchronization root for this object.
        Object System.Collections.ICollection.SyncRoot {
            get { 
                if( _syncRoot == null) {
                    System.Threading.Interlocked.CompareExchange<Object>(ref _syncRoot, new Object(), null);    
                }
                return _syncRoot;
            }
        }
        // Sets or Gets the element at the given index.
        // 
        public T this[int index] {
            get {
                // Following trick can reduce the range check by one
                if ((uint) index >= (uint)_size) {
                    ThrowHelper.ThrowArgumentOutOfRangeException();
                }
                Contract.EndContractBlock();
                return _items[index]; 
            }

            set {
                if ((uint) index >= (uint)_size) {
                    ThrowHelper.ThrowArgumentOutOfRangeException();
                }
                Contract.EndContractBlock();
                _items[index] = value;
                _version++;
            }
        }

        private static bool IsCompatibleObject(object value) {
            // Non-null values are fine.  Only accept nulls if T is a class or Nullable<U>.
            // Note that default(T) is not equal to null for value types except when T is Nullable<U>. 
            return ((value is T) || (value == null && default(T) == null));
        }

        Object System.Collections.IList.this[int index] {
            get {
                return this[index];
            }
            set {
                ThrowHelper.IfNullAndNullsAreIllegalThenThrow<T>(value, ExceptionArgument.value);

                try { 
                    this[index] = (T)value;               
                }
                catch (InvalidCastException) { 
                    ThrowHelper.ThrowWrongValueTypeArgumentException(value, typeof(T));            
                }
            }
        }

        // Adds the given object to the end of this list. The size of the list is
        // increased by one. If required, the capacity of the list is doubled
        // before adding the new element.
        //
        public void Add(T item) {
            if (_size == _items.Length) EnsureCapacity(_size + 1);
            _items[_size++] = item;
            _version++;
        }

        int System.Collections.IList.Add(Object item)
        {
            ThrowHelper.IfNullAndNullsAreIllegalThenThrow<T>(item, ExceptionArgument.item);

            try { 
                Add((T) item);            
            }
            catch (InvalidCastException) { 
                ThrowHelper.ThrowWrongValueTypeArgumentException(item, typeof(T));            
            }

            return Count - 1;
        }


        // Adds the elements of the given collection to the end of this list. If
        // required, the capacity of the list is increased to twice the previous
        // capacity or the new size, whichever is larger.
        //
        public void AddRange(IEnumerable<T> collection) {
            Contract.Ensures(Count >= Contract.OldValue(Count));

            InsertRange(_size, collection);
        }

        public ReadOnlyCollection<T> AsReadOnly() {
            Contract.Ensures(Contract.Result<ReadOnlyCollection<T>>() != null);
            return new ReadOnlyCollection<T>(this);
        }
           
        // Searches a section of the list for a given element using a binary search
        // algorithm. Elements of the list are compared to the search value using
        // the given IComparer interface. If comparer is null, elements of
        // the list are compared to the search value using the IComparable
        // interface, which in that case must be implemented by all elements of the
        // list and the given search value. This method assumes that the given
        // section of the list is already sorted; if this is not the case, the
        // result will be incorrect.
        //
        // The method returns the index of the given value in the list. If the
        // list does not contain the given value, the method returns a negative
        // integer. The bitwise complement operator (~) can be applied to a
        // negative result to produce the index of the first element (if any) that
        // is larger than the given search value. This is also the index at which
        // the search value should be inserted into the list in order for the list
        // to remain sorted.
        // 
        // The method uses the Array.BinarySearch method to perform the
        // search.
        // 
        public int BinarySearch(int index, int count, T item, IComparer<T> comparer) {
            if (index < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            if (count < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            if (_size - index < count)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
            Contract.Ensures(Contract.Result<int>() <= index + count);
            Contract.EndContractBlock();

            return Array.BinarySearch<T>(_items, index, count, item, comparer);
        }
    
        public int BinarySearch(T item)
        {
            Contract.Ensures(Contract.Result<int>() <= Count);
            return BinarySearch(0, Count, item, null);
        }

        public int BinarySearch(T item, IComparer<T> comparer)
        {
            Contract.Ensures(Contract.Result<int>() <= Count);
            return BinarySearch(0, Count, item, comparer);
        }

    
        // Clears the contents of List.
        public void Clear() {
            if (_size > 0)
            {
                Array.Clear(_items, 0, _size); // Don't need to doc this but we clear the elements so that the gc can reclaim the references.
                _size = 0;
            }
            _version++;
        }
    
        // Contains returns true if the specified element is in the List.
        // It does a linear, O(n) search.  Equality is determined by calling
        // EqualityComparer<T>.Default.Equals().

        public bool Contains(T item)
        {
            // PERF: IndexOf calls Array.IndexOf, which internally
            // calls EqualityComparer<T>.Default.IndexOf, which
            // is specialized for different types. This
            // boosts performance since instead of making a
            // virtual method call each iteration of the loop,
            // via EqualityComparer<T>.Default.Equals, we
            // only make one virtual call to EqualityComparer.IndexOf.

            return _size != 0 && IndexOf(item) != -1;
        }

        bool System.Collections.IList.Contains(Object item)
        {
            if(IsCompatibleObject(item)) {            
                return Contains((T) item);                
            }
            return false;
        }

        public List<TOutput> ConvertAll<TOutput>(Converter<T,TOutput> converter) {
            if( converter == null) {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.converter);
            }

            Contract.EndContractBlock();

            List<TOutput> list = new List<TOutput>(_size);
            for( int i = 0; i< _size; i++) {
                list._items[i] = converter(_items[i]);
            }
            list._size = _size;
            return list;
        }

        // Copies this List into array, which must be of a 
        // compatible array type.  
        //
        public void CopyTo(T[] array) {
            CopyTo(array, 0);
        }

        // Copies this List into array, which must be of a 
        // compatible array type.  
        //
        void System.Collections.ICollection.CopyTo(Array array, int arrayIndex) {
            if ((array != null) && (array.Rank != 1)) {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankMultiDimNotSupported);
            }
            Contract.EndContractBlock();

            try {                
                // Array.Copy will check for NULL.
                Array.Copy(_items, 0, array, arrayIndex, _size);
            }
            catch(ArrayTypeMismatchException){
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
            }
        }
    
        // Copies a section of this list to the given array at the given index.
        // 
        // The method uses the Array.Copy method to copy the elements.
        // 
        public void CopyTo(int index, T[] array, int arrayIndex, int count) {
            if (_size - index < count) {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
            }
            Contract.EndContractBlock();
            
            // Delegate rest of error checking to Array.Copy.
            Array.Copy(_items, index, array, arrayIndex, count);
        }

        public void CopyTo(T[] array, int arrayIndex) {
            // Delegate rest of error checking to Array.Copy.
            Array.Copy(_items, 0, array, arrayIndex, _size);
        }

        // Ensures that the capacity of this list is at least the given minimum
        // value. If the currect capacity of the list is less than min, the
        // capacity is increased to twice the current capacity or to min,
        // whichever is larger.
        private void EnsureCapacity(int min) {
            if (_items.Length < min) {
                int newCapacity = _items.Length == 0? _defaultCapacity : _items.Length * 2;
                // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
                // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
                if ((uint)newCapacity > Array.MaxArrayLength) newCapacity = Array.MaxArrayLength;
                if (newCapacity < min) newCapacity = min;
                Capacity = newCapacity;
            }
        }
   
        public bool Exists(Predicate<T> match) {
            return FindIndex(match) != -1;
        }

        public T Find(Predicate<T> match) {
            if( match == null) {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
            }
            Contract.EndContractBlock();

            for(int i = 0 ; i < _size; i++) {
                if(match(_items[i])) {
                    return _items[i];
                }
            }
            return default(T);
        }
  
        public List<T> FindAll(Predicate<T> match) { 
            if( match == null) {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
            }
            Contract.EndContractBlock();

            List<T> list = new List<T>(); 
            for(int i = 0 ; i < _size; i++) {
                if(match(_items[i])) {
                    list.Add(_items[i]);
                }
            }
            return list;
        }
  
        public int FindIndex(Predicate<T> match) {
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(Contract.Result<int>() < Count);
            return FindIndex(0, _size, match);
        }
  
        public int FindIndex(int startIndex, Predicate<T> match) {
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(Contract.Result<int>() < startIndex + Count);
            return FindIndex(startIndex, _size - startIndex, match);
        }
 
        public int FindIndex(int startIndex, int count, Predicate<T> match) {
            if( (uint)startIndex > (uint)_size ) {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);                
            }

            if (count < 0 || startIndex > _size - count) {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_Count);
            }

            if( match == null) {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
            }
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(Contract.Result<int>() < startIndex + count);
            Contract.EndContractBlock();

            int endIndex = startIndex + count;
            for( int i = startIndex; i < endIndex; i++) {
                if( match(_items[i])) return i;
            }
            return -1;
        }
 
        public T FindLast(Predicate<T> match) {
            if( match == null) {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
            }
            Contract.EndContractBlock();

            for(int i = _size - 1 ; i >= 0; i--) {
                if(match(_items[i])) {
                    return _items[i];
                }
            }
            return default(T);
        }

        public int FindLastIndex(Predicate<T> match) {
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(Contract.Result<int>() < Count);
            return FindLastIndex(_size - 1, _size, match);
        }
   
        public int FindLastIndex(int startIndex, Predicate<T> match) {
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(Contract.Result<int>() <= startIndex);
            return FindLastIndex(startIndex, startIndex + 1, match);
        }

        public int FindLastIndex(int startIndex, int count, Predicate<T> match) {
            if( match == null) {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
            }
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(Contract.Result<int>() <= startIndex);
            Contract.EndContractBlock();

            if(_size == 0) {
                // Special case for 0 length List
                if( startIndex != -1) {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
                }
            }
            else {
                // Make sure we're not out of range            
                if ( (uint)startIndex >= (uint)_size) {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
                }
            }
            
            // 2nd have of this also catches when startIndex == MAXINT, so MAXINT - 0 + 1 == -1, which is < 0.
            if (count < 0 || startIndex - count + 1 < 0) {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_Count);
            }
                        
            int endIndex = startIndex - count;
            for( int i = startIndex; i > endIndex; i--) {
                if( match(_items[i])) {
                    return i;
                }
            }
            return -1;
        }

        public void ForEach(Action<T> action) {
            if( action == null) {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.action);
            }
            Contract.EndContractBlock();

            int version = _version;

            for(int i = 0 ; i < _size; i++) {
                if (version != _version && BinaryCompatibility.TargetsAtLeast_Desktop_V4_5) {
                    break;
                }
                action(_items[i]);
            }

            if (version != _version && BinaryCompatibility.TargetsAtLeast_Desktop_V4_5)
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
        }

        // Returns an enumerator for this list with the given
        // permission for removal of elements. If modifications made to the list 
        // while an enumeration is in progress, the MoveNext and 
        // GetObject methods of the enumerator will throw an exception.
        //
        public Enumerator GetEnumerator() {
            return new Enumerator(this);
        }

        /// <internalonly/>
        IEnumerator<T> IEnumerable<T>.GetEnumerator() {
            return new Enumerator(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return new Enumerator(this);
        }

        public List<T> GetRange(int index, int count) {
            if (index < 0) {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (count < 0) {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (_size - index < count) {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);                
            }
            Contract.Ensures(Contract.Result<List<T>>() != null);
            Contract.EndContractBlock();

            List<T> list = new List<T>(count);
            Array.Copy(_items, index, list._items, 0, count);            
            list._size = count;
            return list;
        }


        // Returns the index of the first occurrence of a given value in a range of
        // this list. The list is searched forwards from beginning to end.
        // The elements of the list are compared to the given value using the
        // Object.Equals method.
        // 
        // This method uses the Array.IndexOf method to perform the
        // search.
        // 
        public int IndexOf(T item) {
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(Contract.Result<int>() < Count);
            return Array.IndexOf(_items, item, 0, _size);
        }

        int System.Collections.IList.IndexOf(Object item)
        {
            if(IsCompatibleObject(item)) {            
                return IndexOf((T)item);
            }
            return -1;
        }

        // Returns the index of the first occurrence of a given value in a range of
        // this list. The list is searched forwards, starting at index
        // index and ending at count number of elements. The
        // elements of the list are compared to the given value using the
        // Object.Equals method.
        // 
        // This method uses the Array.IndexOf method to perform the
        // search.
        // 
        public int IndexOf(T item, int index) {
            if (index > _size)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_Index);
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(Contract.Result<int>() < Count);
            Contract.EndContractBlock();
            return Array.IndexOf(_items, item, index, _size - index);
        }

        // Returns the index of the first occurrence of a given value in a range of
        // this list. The list is searched forwards, starting at index
        // index and upto count number of elements. The
        // elements of the list are compared to the given value using the
        // Object.Equals method.
        // 
        // This method uses the Array.IndexOf method to perform the
        // search.
        // 
        public int IndexOf(T item, int index, int count) {
            if (index > _size)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_Index);

            if (count <0 || index > _size - count) ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_Count);
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(Contract.Result<int>() < Count);
            Contract.EndContractBlock();

            return Array.IndexOf(_items, item, index, count);
        }
    
        // Inserts an element into this list at a given index. The size of the list
        // is increased by one. If required, the capacity of the list is doubled
        // before inserting the new element.
        // 
        public void Insert(int index, T item) {
            // Note that insertions at the end are legal.
            if ((uint) index > (uint)_size) {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_ListInsert);
            }
            Contract.EndContractBlock();
            if (_size == _items.Length) EnsureCapacity(_size + 1);
            if (index < _size) {
                Array.Copy(_items, index, _items, index + 1, _size - index);
            }
            _items[index] = item;
            _size++;            
            _version++;
        }
    
        void System.Collections.IList.Insert(int index, Object item)
        {
            ThrowHelper.IfNullAndNullsAreIllegalThenThrow<T>(item, ExceptionArgument.item);

            try { 
                Insert(index, (T) item);
            }
            catch (InvalidCastException) { 
                ThrowHelper.ThrowWrongValueTypeArgumentException(item, typeof(T));            
            }
        }

        // Inserts the elements of the given collection at a given index. If
        // required, the capacity of the list is increased to twice the previous
        // capacity or the new size, whichever is larger.  Ranges may be added
        // to the end of the list by setting index to the List's size.
        //
        public void InsertRange(int index, IEnumerable<T> collection) {
            if (collection==null) {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.collection);
            }
            
            if ((uint)index > (uint)_size) {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_Index);
            }
            Contract.EndContractBlock();

            ICollection<T> c = collection as ICollection<T>;
            if( c != null ) {    // if collection is ICollection<T>
                int count = c.Count;
                if (count > 0) {
                    EnsureCapacity(_size + count);
                    if (index < _size) {
                        Array.Copy(_items, index, _items, index + count, _size - index);
                    }
                    
                    // If we're inserting a List into itself, we want to be able to deal with that.
                    if (this == c) {
                        // Copy first part of _items to insert location
                        Array.Copy(_items, 0, _items, index, index);
                        // Copy last part of _items back to inserted location
                        Array.Copy(_items, index+count, _items, index*2, _size-index);
                    }
                    else {
                        T[] itemsToInsert = new T[count];
                        c.CopyTo(itemsToInsert, 0);
                        itemsToInsert.CopyTo(_items, index);                    
                    }
                    _size += count;
                }                
            }
            else {
                using(IEnumerator<T> en = collection.GetEnumerator()) {
                    while(en.MoveNext()) {
                        Insert(index++, en.Current);                                    
                    }                
                }
            }
            _version++;            
        }
    
        // Returns the index of the last occurrence of a given value in a range of
        // this list. The list is searched backwards, starting at the end 
        // and ending at the first element in the list. The elements of the list 
        // are compared to the given value using the Object.Equals method.
        // 
        // This method uses the Array.LastIndexOf method to perform the
        // search.
        // 
        public int LastIndexOf(T item)
        {
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(Contract.Result<int>() < Count);
            if (_size == 0) {  // Special case for empty list
                return -1;
            }
            else {
                return LastIndexOf(item, _size - 1, _size);
            }
        }

        // Returns the index of the last occurrence of a given value in a range of
        // this list. The list is searched backwards, starting at index
        // index and ending at the first element in the list. The 
        // elements of the list are compared to the given value using the 
        // Object.Equals method.
        // 
        // This method uses the Array.LastIndexOf method to perform the
        // search.
        // 
        public int LastIndexOf(T item, int index)
        {
            if (index >= _size)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_Index);
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(((Count == 0) && (Contract.Result<int>() == -1)) || ((Count > 0) && (Contract.Result<int>() <= index)));
            Contract.EndContractBlock();
            return LastIndexOf(item, index, index + 1);
        }

        // Returns the index of the last occurrence of a given value in a range of
        // this list. The list is searched backwards, starting at index
        // index and upto count elements. The elements of
        // the list are compared to the given value using the Object.Equals
        // method.
        // 
        // This method uses the Array.LastIndexOf method to perform the
        // search.
        // 
        public int LastIndexOf(T item, int index, int count) {
            if ((Count != 0) && (index < 0)) {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            if ((Count !=0) && (count < 0)) {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(((Count == 0) && (Contract.Result<int>() == -1)) || ((Count > 0) && (Contract.Result<int>() <= index)));
            Contract.EndContractBlock();

            if (_size == 0) {  // Special case for empty list
                return -1;
            }

            if (index >= _size) {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_BiggerThanCollection);
            }

            if (count > index + 1) {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_BiggerThanCollection);
            } 

            return Array.LastIndexOf(_items, item, index, count);
        }
    
        // Removes the element at the given index. The size of the list is
        // decreased by one.
        // 
        public bool Remove(T item) {
            int index = IndexOf(item);
            if (index >= 0) {
                RemoveAt(index);
                return true;
            }

            return false;
        }

        void System.Collections.IList.Remove(Object item)
        {
            if(IsCompatibleObject(item)) {            
                Remove((T) item);
            }
        }

        // This method removes all items which matches the predicate.
        // The complexity is O(n).   
        public int RemoveAll(Predicate<T> match) {
            if( match == null) {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
            }
            Contract.Ensures(Contract.Result<int>() >= 0);
            Contract.Ensures(Contract.Result<int>() <= Contract.OldValue(Count));
            Contract.EndContractBlock();
    
            int freeIndex = 0;   // the first free slot in items array

            // Find the first item which needs to be removed.
            while( freeIndex < _size && !match(_items[freeIndex])) freeIndex++;            
            if( freeIndex >= _size) return 0;
            
            int current = freeIndex + 1;
            while( current < _size) {
                // Find the first item which needs to be kept.
                while( current < _size && match(_items[current])) current++;            

                if( current < _size) {
                    // copy item to the free slot.
                    _items[freeIndex++] = _items[current++];
                }
            }                       
            
            Array.Clear(_items, freeIndex, _size - freeIndex);
            int result = _size - freeIndex;
            _size = freeIndex;
            _version++;
            return result;
        }

        // Removes the element at the given index. The size of the list is
        // decreased by one.
        // 
        public void RemoveAt(int index) {
            if ((uint)index >= (uint)_size) {
                ThrowHelper.ThrowArgumentOutOfRangeException();
            }
            Contract.EndContractBlock();
            _size--;
            if (index < _size) {
                Array.Copy(_items, index + 1, _items, index, _size - index);
            }
            _items[_size] = default(T);
            _version++;
        }
    
        // Removes a range of elements from this list.
        // 
        public void RemoveRange(int index, int count) {
            if (index < 0) {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (count < 0) {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }
                
            if (_size - index < count)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
            Contract.EndContractBlock();
    
            if (count > 0) {
                int i = _size;
                _size -= count;
                if (index < _size) {
                    Array.Copy(_items, index + count, _items, index, _size - index);
                }
                Array.Clear(_items, _size, count);
                _version++;
            }
        }
    
        // Reverses the elements in this list.
        public void Reverse() {
            Reverse(0, Count);
        }
    
        // Reverses the elements in a range of this list. Following a call to this
        // method, an element in the range given by index and count
        // which was previously located at index i will now be located at
        // index index + (index + count - i - 1).
        // 
        public void Reverse(int index, int count) {
            if (index < 0) {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }
                
            if (count < 0) {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (_size - index < count)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
            Contract.EndContractBlock();

            // The non-generic Array.Reverse is not used because it does not perform
            // well for non-primitive value types.
            // If/when a generic Array.Reverse<T> becomes available, the below code
            // can be deleted and replaced with a call to Array.Reverse<T>.
            int i = index;
            int j = index + count - 1;
            T[] array = _items;
            while (i < j)
            {
                T temp = array[i];
                array[i] = array[j];
                array[j] = temp;
                i++;
                j--;
            }

            _version++;
        }
        
        // Sorts the elements in this list.  Uses the default comparer and 
        // Array.Sort.
        public void Sort()
        {
            Sort(0, Count, null);
        }

        // Sorts the elements in this list.  Uses Array.Sort with the
        // provided comparer.
        public void Sort(IComparer<T> comparer)
        {
            Sort(0, Count, comparer);
        }

        // Sorts the elements in a section of this list. The sort compares the
        // elements to each other using the given IComparer interface. If
        // comparer is null, the elements are compared to each other using
        // the IComparable interface, which in that case must be implemented by all
        // elements of the list.
        // 
        // This method uses the Array.Sort method to sort the elements.
        // 
        public void Sort(int index, int count, IComparer<T> comparer) {
            if (index < 0) {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }
            
            if (count < 0) {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }
                
            if (_size - index < count)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
            Contract.EndContractBlock();

            Array.Sort<T>(_items, index, count, comparer);
            _version++;
        }

        public void Sort(Comparison<T> comparison) {
            if( comparison == null) {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.comparison);
            }
            Contract.EndContractBlock();

            if( _size > 0) {
                IComparer<T> comparer = Comparer<T>.Create(comparison);
                Array.Sort(_items, 0, _size, comparer);
            }
        }

        // ToArray returns an array containing the contents of the List.
        // This requires copying the List, which is an O(n) operation.
        public T[] ToArray() {
            Contract.Ensures(Contract.Result<T[]>() != null);
            Contract.Ensures(Contract.Result<T[]>().Length == Count);

#if FEATURE_CORECLR
            if (_size == 0)
            {
                return _emptyArray;
            }
#endif

            T[] array = new T[_size];
            Array.Copy(_items, 0, array, 0, _size);
            return array;
        }
    
        // Sets the capacity of this list to the size of the list. This method can
        // be used to minimize a list's memory overhead once it is known that no
        // new elements will be added to the list. To completely clear a list and
        // release all memory referenced by the list, execute the following
        // statements:
        // 
        // list.Clear();
        // list.TrimExcess();
        // 
        public void TrimExcess() {
            int threshold = (int)(((double)_items.Length) * 0.9);             
            if( _size < threshold ) {
                Capacity = _size;                
            }
        }    

        public bool TrueForAll(Predicate<T> match) {
            if( match == null) {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
            }
            Contract.EndContractBlock();

            for(int i = 0 ; i < _size; i++) {
                if( !match(_items[i])) {
                    return false;
                }
            }
            return true;
        }

        [Serializable]
        public struct Enumerator : IEnumerator<T>, System.Collections.IEnumerator
        {
            private List<T> list;
            private int index;
            private int version;
            private T current;

            internal Enumerator(List<T> list) {
                this.list = list;
                index = 0;
                version = list._version;
                current = default(T);
            }

            public void Dispose() {
            }

            public bool MoveNext() {

                List<T> localList = list;

                if (version == localList._version && ((uint)index < (uint)localList._size)) 
                {                                                     
                    current = localList._items[index];                    
                    index++;
                    return true;
                }
                return MoveNextRare();
            }

            private bool MoveNextRare()
            {                
                if (version != list._version) {
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
                }

                index = list._size + 1;
                current = default(T);
                return false;                
            }

            public T Current {
                get {
                    return current;
                }
            }

            Object System.Collections.IEnumerator.Current {
                get {
                    if( index == 0 || index == list._size + 1) {
                         ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);
                    }
                    return Current;
                }
            }
    
            void System.Collections.IEnumerator.Reset() {
                if (version != list._version) {
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
                }
                
                index = 0;
                current = default(T);
            }

        }
    }
}

