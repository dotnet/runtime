// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Class:  SortedList
**
** Purpose: Represents a collection of key/value pairs
**          that are sorted by the keys and are accessible
**          by key and by index.
**
===========================================================*/

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;

namespace System.Collections
{
    // The SortedList class implements a sorted list of keys and values. Entries in
    // a sorted list are sorted by their keys and are accessible both by key and by
    // index. The keys of a sorted list can be ordered either according to a
    // specific IComparer implementation given when the sorted list is
    // instantiated, or according to the IComparable implementation provided
    // by the keys themselves. In either case, a sorted list does not allow entries
    // with duplicate keys.
    // 
    // A sorted list internally maintains two arrays that store the keys and
    // values of the entries. The capacity of a sorted list is the allocated
    // length of these internal arrays. As elements are added to a sorted list, the
    // capacity of the sorted list is automatically increased as required by
    // reallocating the internal arrays.  The capacity is never automatically 
    // decreased, but users can call either TrimToSize or 
    // Capacity explicitly.
    // 
    // The GetKeyList and GetValueList methods of a sorted list
    // provides access to the keys and values of the sorted list in the form of
    // List implementations. The List objects returned by these
    // methods are aliases for the underlying sorted list, so modifications
    // made to those lists are directly reflected in the sorted list, and vice
    // versa.
    // 
    // The SortedList class provides a convenient way to create a sorted
    // copy of another dictionary, such as a Hashtable. For example:
    // 
    // Hashtable h = new Hashtable();
    // h.Add(...);
    // h.Add(...);
    // ...
    // SortedList s = new SortedList(h);
    // 
    // The last line above creates a sorted list that contains a copy of the keys
    // and values stored in the hashtable. In this particular example, the keys
    // will be ordered according to the IComparable interface, which they
    // all must implement. To impose a different ordering, SortedList also
    // has a constructor that allows a specific IComparer implementation to
    // be specified.
    // 
    [DebuggerTypeProxy(typeof(System.Collections.SortedList.SortedListDebugView))]
    [DebuggerDisplay("Count = {Count}")]
#if FEATURE_CORECLR
    [Obsolete("Non-generic collections have been deprecated. Please use collections in System.Collections.Generic.")]
#endif
    [Serializable]
    public class SortedList : IDictionary, ICloneable
    {
        private Object[] _keys;
        private Object[] _values;
        private int _size;
        private int _version;
        private IComparer _comparer;
        private KeyList _keyList;
        private ValueList _valueList;
        [NonSerialized]
        private Object _syncRoot;

        private const int _defaultCapacity = 16;

        // Copy of Array.MaxArrayLength
        internal const int MaxArrayLength = 0X7FEFFFFF;

        // Constructs a new sorted list. The sorted list is initially empty and has
        // a capacity of zero. Upon adding the first element to the sorted list the
        // capacity is increased to 16, and then increased in multiples of two as
        // required. The elements of the sorted list are ordered according to the
        // IComparable interface, which must be implemented by the keys of
        // all entries added to the sorted list.
        public SortedList()
        {
            Init();
        }
        
        private void Init()
        {
            _keys = Array.Empty<Object>();
            _values = Array.Empty<Object>();
            _size = 0;
            _comparer = new Comparer(CultureInfo.CurrentCulture);
        }

        // Constructs a new sorted list. The sorted list is initially empty and has
        // a capacity of zero. Upon adding the first element to the sorted list the
        // capacity is increased to 16, and then increased in multiples of two as
        // required. The elements of the sorted list are ordered according to the
        // IComparable interface, which must be implemented by the keys of
        // all entries added to the sorted list.
        //
        public SortedList(int initialCapacity)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), SR.ArgumentOutOfRange_NeedNonNegNum);
            Contract.EndContractBlock();
            _keys = new Object[initialCapacity];
            _values = new Object[initialCapacity];
            _comparer = new Comparer(CultureInfo.CurrentCulture);
        }

        // Constructs a new sorted list with a given IComparer
        // implementation. The sorted list is initially empty and has a capacity of
        // zero. Upon adding the first element to the sorted list the capacity is
        // increased to 16, and then increased in multiples of two as required. The
        // elements of the sorted list are ordered according to the given
        // IComparer implementation. If comparer is null, the
        // elements are compared to each other using the IComparable
        // interface, which in that case must be implemented by the keys of all
        // entries added to the sorted list.
        // 
        public SortedList(IComparer comparer)
            : this()
        {
            if (comparer != null) _comparer = comparer;
        }

        // Constructs a new sorted list with a given IComparer
        // implementation and a given initial capacity. The sorted list is
        // initially empty, but will have room for the given number of elements
        // before any reallocations are required. The elements of the sorted list
        // are ordered according to the given IComparer implementation. If
        // comparer is null, the elements are compared to each other using
        // the IComparable interface, which in that case must be implemented
        // by the keys of all entries added to the sorted list.
        // 
        public SortedList(IComparer comparer, int capacity)
            : this(comparer)
        {
            Capacity = capacity;
        }

        // Constructs a new sorted list containing a copy of the entries in the
        // given dictionary. The elements of the sorted list are ordered according
        // to the IComparable interface, which must be implemented by the
        // keys of all entries in the the given dictionary as well as keys
        // subsequently added to the sorted list.
        // 
        public SortedList(IDictionary d)
            : this(d, null)
        {
        }

        // Constructs a new sorted list containing a copy of the entries in the
        // given dictionary. The elements of the sorted list are ordered according
        // to the given IComparer implementation. If comparer is
        // null, the elements are compared to each other using the
        // IComparable interface, which in that case must be implemented
        // by the keys of all entries in the the given dictionary as well as keys
        // subsequently added to the sorted list.
        // 
        public SortedList(IDictionary d, IComparer comparer)
            : this(comparer, (d != null ? d.Count : 0))
        {
            if (d == null)
                throw new ArgumentNullException(nameof(d), SR.ArgumentNull_Dictionary);
            Contract.EndContractBlock();
            d.Keys.CopyTo(_keys, 0);
            d.Values.CopyTo(_values, 0);

            // Array.Sort(Array keys, Array values, IComparer comparer) does not exist in System.Runtime contract v4.0.10.0.
            // This works around that by sorting only on the keys and then assigning values accordingly.
            Array.Sort(_keys, comparer);
            for (int i = 0; i < _keys.Length; i++)
            {
                _values[i] = d[_keys[i]];
            }
            _size = d.Count;
        }

        // Adds an entry with the given key and value to this sorted list. An
        // ArgumentException is thrown if the key is already present in the sorted list.
        // 
        public virtual void Add(Object key, Object value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key), SR.ArgumentNull_Key);
            Contract.EndContractBlock();
            int i = Array.BinarySearch(_keys, 0, _size, key, _comparer);
            if (i >= 0)
                throw new ArgumentException(SR.Format(SR.Argument_AddingDuplicate__, GetKey(i), key));
            Insert(~i, key, value);
        }

        // Returns the capacity of this sorted list. The capacity of a sorted list
        // represents the allocated length of the internal arrays used to store the
        // keys and values of the list, and thus also indicates the maximum number
        // of entries the list can contain before a reallocation of the internal
        // arrays is required.
        // 
        public virtual int Capacity
        {
            get
            {
                return _keys.Length;
            }
            set
            {
                if (value < Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_SmallCapacity);
                }
                Contract.EndContractBlock();

                if (value != _keys.Length)
                {
                    if (value > 0)
                    {
                        Object[] newKeys = new Object[value];
                        Object[] newValues = new Object[value];
                        if (_size > 0)
                        {
                            Array.Copy(_keys, 0, newKeys, 0, _size);
                            Array.Copy(_values, 0, newValues, 0, _size);
                        }
                        _keys = newKeys;
                        _values = newValues;
                    }
                    else
                    {
                        // size can only be zero here.
                        Debug.Assert(_size == 0, "Size is not zero");
                        _keys = Array.Empty<Object>();
                        _values = Array.Empty<Object>();
                    }
                }
            }
        }

        // Returns the number of entries in this sorted list.
        // 
        public virtual int Count
        {
            get
            {
                return _size;
            }
        }

        // Returns a collection representing the keys of this sorted list. This
        // method returns the same object as GetKeyList, but typed as an
        // ICollection instead of an IList.
        // 
        public virtual ICollection Keys
        {
            get
            {
                return GetKeyList();
            }
        }

        // Returns a collection representing the values of this sorted list. This
        // method returns the same object as GetValueList, but typed as an
        // ICollection instead of an IList.
        // 
        public virtual ICollection Values
        {
            get
            {
                return GetValueList();
            }
        }

        // Is this SortedList read-only?
        public virtual bool IsReadOnly
        {
            get { return false; }
        }

        public virtual bool IsFixedSize
        {
            get { return false; }
        }

        // Is this SortedList synchronized (thread-safe)?
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

        // Removes all entries from this sorted list.
        public virtual void Clear()
        {
            // clear does not change the capacity
            _version++;
            Array.Clear(_keys, 0, _size); // Don't need to doc this but we clear the elements so that the gc can reclaim the references.
            Array.Clear(_values, 0, _size); // Don't need to doc this but we clear the elements so that the gc can reclaim the references.
            _size = 0;
        }

        // Makes a virtually identical copy of this SortedList.  This is a shallow 
        // copy.  IE, the Objects in the SortedList are not cloned - we copy the 
        // references to those objects.
        public virtual Object Clone()
        {
            SortedList sl = new SortedList(_size);
            Array.Copy(_keys, 0, sl._keys, 0, _size);
            Array.Copy(_values, 0, sl._values, 0, _size);
            sl._size = _size;
            sl._version = _version;
            sl._comparer = _comparer;
            // Don't copy keyList nor valueList.
            return sl;
        }


        // Checks if this sorted list contains an entry with the given key.
        // 
        public virtual bool Contains(Object key)
        {
            return IndexOfKey(key) >= 0;
        }

        // Checks if this sorted list contains an entry with the given key.
        // 
        public virtual bool ContainsKey(Object key)
        {
            // Yes, this is a SPEC'ed duplicate of Contains().
            return IndexOfKey(key) >= 0;
        }

        // Checks if this sorted list contains an entry with the given value. The
        // values of the entries of the sorted list are compared to the given value
        // using the Object.Equals method. This method performs a linear
        // search and is substantially slower than the Contains
        // method.
        // 
        public virtual bool ContainsValue(Object value)
        {
            return IndexOfValue(value) >= 0;
        }

        // Copies the values in this SortedList to an array.
        public virtual void CopyTo(Array array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array), SR.ArgumentNull_Array);
            if (array.Rank != 1)
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, nameof(array));
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (array.Length - arrayIndex < Count)
                throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);
            Contract.EndContractBlock();
            for (int i = 0; i < Count; i++)
            {
                DictionaryEntry entry = new DictionaryEntry(_keys[i], _values[i]);
                array.SetValue(entry, i + arrayIndex);
            }
        }

        // Copies the values in this SortedList to an KeyValuePairs array.
        // KeyValuePairs is different from Dictionary Entry in that it has special
        // debugger attributes on its fields.

        internal virtual KeyValuePairs[] ToKeyValuePairsArray()
        {
            KeyValuePairs[] array = new KeyValuePairs[Count];
            for (int i = 0; i < Count; i++)
            {
                array[i] = new KeyValuePairs(_keys[i], _values[i]);
            }
            return array;
        }

        // Ensures that the capacity of this sorted list is at least the given
        // minimum value. If the current capacity of the list is less than
        // min, the capacity is increased to twice the current capacity or
        // to min, whichever is larger.
        private void EnsureCapacity(int min)
        {
            int newCapacity = _keys.Length == 0 ? 16 : _keys.Length * 2;
            // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
            // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
            if ((uint)newCapacity > MaxArrayLength) newCapacity = MaxArrayLength;
            if (newCapacity < min) newCapacity = min;
            Capacity = newCapacity;
        }

        // Returns the value of the entry at the given index.
        // 
        public virtual Object GetByIndex(int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_Index);
            Contract.EndContractBlock();
            return _values[index];
        }

        // Returns an IEnumerator for this sorted list.  If modifications 
        // made to the sorted list while an enumeration is in progress, 
        // the MoveNext and Remove methods
        // of the enumerator will throw an exception.
        //
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new SortedListEnumerator(this, 0, _size, SortedListEnumerator.DictEntry);
        }

        // Returns an IDictionaryEnumerator for this sorted list.  If modifications 
        // made to the sorted list while an enumeration is in progress, 
        // the MoveNext and Remove methods
        // of the enumerator will throw an exception.
        //
        public virtual IDictionaryEnumerator GetEnumerator()
        {
            return new SortedListEnumerator(this, 0, _size, SortedListEnumerator.DictEntry);
        }

        // Returns the key of the entry at the given index.
        // 
        public virtual Object GetKey(int index)
        {
            if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_Index);
            Contract.EndContractBlock();
            return _keys[index];
        }

        // Returns an IList representing the keys of this sorted list. The
        // returned list is an alias for the keys of this sorted list, so
        // modifications made to the returned list are directly reflected in the
        // underlying sorted list, and vice versa. The elements of the returned
        // list are ordered in the same way as the elements of the sorted list. The
        // returned list does not support adding, inserting, or modifying elements
        // (the Add, AddRange, Insert, InsertRange,
        // Reverse, Set, SetRange, and Sort methods
        // throw exceptions), but it does allow removal of elements (through the
        // Remove and RemoveRange methods or through an enumerator).
        // Null is an invalid key value.
        // 
        public virtual IList GetKeyList()
        {
            if (_keyList == null) _keyList = new KeyList(this);
            return _keyList;
        }

        // Returns an IList representing the values of this sorted list. The
        // returned list is an alias for the values of this sorted list, so
        // modifications made to the returned list are directly reflected in the
        // underlying sorted list, and vice versa. The elements of the returned
        // list are ordered in the same way as the elements of the sorted list. The
        // returned list does not support adding or inserting elements (the
        // Add, AddRange, Insert and InsertRange
        // methods throw exceptions), but it does allow modification and removal of
        // elements (through the Remove, RemoveRange, Set and
        // SetRange methods or through an enumerator).
        // 
        public virtual IList GetValueList()
        {
            if (_valueList == null) _valueList = new ValueList(this);
            return _valueList;
        }

        // Returns the value associated with the given key. If an entry with the
        // given key is not found, the returned value is null.
        // 
        public virtual Object this[Object key]
        {
            get
            {
                int i = IndexOfKey(key);
                if (i >= 0) return _values[i];
                return null;
            }
            set
            {
                if (key == null) throw new ArgumentNullException(nameof(key), SR.ArgumentNull_Key);
                Contract.EndContractBlock();
                int i = Array.BinarySearch(_keys, 0, _size, key, _comparer);
                if (i >= 0)
                {
                    _values[i] = value;
                    _version++;
                    return;
                }
                Insert(~i, key, value);
            }
        }

        // Returns the index of the entry with a given key in this sorted list. The
        // key is located through a binary search, and thus the average execution
        // time of this method is proportional to Log2(size), where
        // size is the size of this sorted list. The returned value is -1 if
        // the given key does not occur in this sorted list. Null is an invalid 
        // key value.
        // 
        public virtual int IndexOfKey(Object key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key), SR.ArgumentNull_Key);
            Contract.EndContractBlock();
            int ret = Array.BinarySearch(_keys, 0, _size, key, _comparer);
            return ret >= 0 ? ret : -1;
        }

        // Returns the index of the first occurrence of an entry with a given value
        // in this sorted list. The entry is located through a linear search, and
        // thus the average execution time of this method is proportional to the
        // size of this sorted list. The elements of the list are compared to the
        // given value using the Object.Equals method.
        // 
        public virtual int IndexOfValue(Object value)
        {
            return Array.IndexOf(_values, value, 0, _size);
        }

        // Inserts an entry with a given key and value at a given index.
        private void Insert(int index, Object key, Object value)
        {
            if (_size == _keys.Length) EnsureCapacity(_size + 1);
            if (index < _size)
            {
                Array.Copy(_keys, index, _keys, index + 1, _size - index);
                Array.Copy(_values, index, _values, index + 1, _size - index);
            }
            _keys[index] = key;
            _values[index] = value;
            _size++;
            _version++;
        }

        // Removes the entry at the given index. The size of the sorted list is
        // decreased by one.
        // 
        public virtual void RemoveAt(int index)
        {
            if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_Index);
            Contract.EndContractBlock();
            _size--;
            if (index < _size)
            {
                Array.Copy(_keys, index + 1, _keys, index, _size - index);
                Array.Copy(_values, index + 1, _values, index, _size - index);
            }
            _keys[_size] = null;
            _values[_size] = null;
            _version++;
        }

        // Removes an entry from this sorted list. If an entry with the specified
        // key exists in the sorted list, it is removed. An ArgumentException is
        // thrown if the key is null.
        // 
        public virtual void Remove(Object key)
        {
            int i = IndexOfKey(key);
            if (i >= 0)
                RemoveAt(i);
        }

        // Sets the value at an index to a given value.  The previous value of
        // the given entry is overwritten.
        // 
        public virtual void SetByIndex(int index, Object value)
        {
            if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_Index);
            Contract.EndContractBlock();
            _values[index] = value;
            _version++;
        }

        // Returns a thread-safe SortedList.
        //
        public static SortedList Synchronized(SortedList list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            Contract.EndContractBlock();
            return new SyncSortedList(list);
        }

        // Sets the capacity of this sorted list to the size of the sorted list.
        // This method can be used to minimize a sorted list's memory overhead once
        // it is known that no new elements will be added to the sorted list. To
        // completely clear a sorted list and release all memory referenced by the
        // sorted list, execute the following statements:
        // 
        // sortedList.Clear();
        // sortedList.TrimToSize();
        // 
        public virtual void TrimToSize()
        {
            Capacity = _size;
        }

        [Serializable]
        private class SyncSortedList : SortedList
        {
            private SortedList _list;
            private Object _root;


            internal SyncSortedList(SortedList list)
            {
                _list = list;
                _root = list.SyncRoot;
            }

            public override int Count
            {
                get { lock (_root) { return _list.Count; } }
            }

            public override Object SyncRoot
            {
                get { return _root; }
            }

            public override bool IsReadOnly
            {
                get { return _list.IsReadOnly; }
            }

            public override bool IsFixedSize
            {
                get { return _list.IsFixedSize; }
            }


            public override bool IsSynchronized
            {
                get { return true; }
            }

            public override Object this[Object key]
            {
                get
                {
                    lock (_root)
                    {
                        return _list[key];
                    }
                }
                set
                {
                    lock (_root)
                    {
                        _list[key] = value;
                    }
                }
            }

            public override void Add(Object key, Object value)
            {
                lock (_root)
                {
                    _list.Add(key, value);
                }
            }

            public override int Capacity
            {
                get { lock (_root) { return _list.Capacity; } }
            }

            public override void Clear()
            {
                lock (_root)
                {
                    _list.Clear();
                }
            }

            public override Object Clone()
            {
                lock (_root)
                {
                    return _list.Clone();
                }
            }

            public override bool Contains(Object key)
            {
                lock (_root)
                {
                    return _list.Contains(key);
                }
            }

            public override bool ContainsKey(Object key)
            {
                lock (_root)
                {
                    return _list.ContainsKey(key);
                }
            }

            public override bool ContainsValue(Object key)
            {
                lock (_root)
                {
                    return _list.ContainsValue(key);
                }
            }

            public override void CopyTo(Array array, int index)
            {
                lock (_root)
                {
                    _list.CopyTo(array, index);
                }
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override Object GetByIndex(int index)
            {
                lock (_root)
                {
                    return _list.GetByIndex(index);
                }
            }

            public override IDictionaryEnumerator GetEnumerator()
            {
                lock (_root)
                {
                    return _list.GetEnumerator();
                }
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override Object GetKey(int index)
            {
                lock (_root)
                {
                    return _list.GetKey(index);
                }
            }

            public override IList GetKeyList()
            {
                lock (_root)
                {
                    return _list.GetKeyList();
                }
            }

            public override IList GetValueList()
            {
                lock (_root)
                {
                    return _list.GetValueList();
                }
            }

            public override int IndexOfKey(Object key)
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key), SR.ArgumentNull_Key);
                Contract.EndContractBlock();

                lock (_root)
                {
                    return _list.IndexOfKey(key);
                }
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override int IndexOfValue(Object value)
            {
                lock (_root)
                {
                    return _list.IndexOfValue(value);
                }
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override void RemoveAt(int index)
            {
                lock (_root)
                {
                    _list.RemoveAt(index);
                }
            }

            public override void Remove(Object key)
            {
                lock (_root)
                {
                    _list.Remove(key);
                }
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override void SetByIndex(int index, Object value)
            {
                lock (_root)
                {
                    _list.SetByIndex(index, value);
                }
            }

            internal override KeyValuePairs[] ToKeyValuePairsArray()
            {
                return _list.ToKeyValuePairsArray();
            }

            public override void TrimToSize()
            {
                lock (_root)
                {
                    _list.TrimToSize();
                }
            }
        }

        [Serializable]
        private class SortedListEnumerator : IDictionaryEnumerator, ICloneable
        {
            private SortedList _sortedList;
            private Object _key;
            private Object _value;
            private int _index;
            private int _startIndex;        // Store for Reset.
            private int _endIndex;
            private int _version;
            private bool _current;       // Is the current element valid?
            private int _getObjectRetType;  // What should GetObject return?

            internal const int Keys = 1;
            internal const int Values = 2;
            internal const int DictEntry = 3;

            internal SortedListEnumerator(SortedList sortedList, int index, int count,
                                 int getObjRetType)
            {
                _sortedList = sortedList;
                _index = index;
                _startIndex = index;
                _endIndex = index + count;
                _version = sortedList._version;
                _getObjectRetType = getObjRetType;
                _current = false;
            }

            public object Clone() => MemberwiseClone();

            public virtual Object Key
            {
                get
                {
                    if (_version != _sortedList._version) throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                    if (_current == false) throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                    return _key;
                }
            }

            public virtual bool MoveNext()
            {
                if (_version != _sortedList._version) throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                if (_index < _endIndex)
                {
                    _key = _sortedList._keys[_index];
                    _value = _sortedList._values[_index];
                    _index++;
                    _current = true;
                    return true;
                }
                _key = null;
                _value = null;
                _current = false;
                return false;
            }

            public virtual DictionaryEntry Entry
            {
                get
                {
                    if (_version != _sortedList._version) throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                    if (_current == false) throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                    return new DictionaryEntry(_key, _value);
                }
            }

            public virtual Object Current
            {
                get
                {
                    if (_current == false) throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);

                    if (_getObjectRetType == Keys)
                        return _key;
                    else if (_getObjectRetType == Values)
                        return _value;
                    else
                        return new DictionaryEntry(_key, _value);
                }
            }

            public virtual Object Value
            {
                get
                {
                    if (_version != _sortedList._version) throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                    if (_current == false) throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                    return _value;
                }
            }

            public virtual void Reset()
            {
                if (_version != _sortedList._version) throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                _index = _startIndex;
                _current = false;
                _key = null;
                _value = null;
            }
        }

        [Serializable]
        private class KeyList : IList
        {
            private SortedList _sortedList;

            internal KeyList(SortedList sortedList)
            {
                _sortedList = sortedList;
            }

            public virtual int Count
            {
                get { return _sortedList._size; }
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
                get { return _sortedList.IsSynchronized; }
            }

            public virtual Object SyncRoot
            {
                get { return _sortedList.SyncRoot; }
            }

            public virtual int Add(Object key)
            {
                throw new NotSupportedException(SR.NotSupported_SortedListNestedWrite);
                //            return 0; // suppress compiler warning
            }

            public virtual void Clear()
            {
                throw new NotSupportedException(SR.NotSupported_SortedListNestedWrite);
            }

            public virtual bool Contains(Object key)
            {
                return _sortedList.Contains(key);
            }

            public virtual void CopyTo(Array array, int arrayIndex)
            {
                if (array != null && array.Rank != 1)
                    throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, nameof(array));
                Contract.EndContractBlock();

                // defer error checking to Array.Copy
                Array.Copy(_sortedList._keys, 0, array, arrayIndex, _sortedList.Count);
            }

            public virtual void Insert(int index, Object value)
            {
                throw new NotSupportedException(SR.NotSupported_SortedListNestedWrite);
            }

            public virtual Object this[int index]
            {
                get
                {
                    return _sortedList.GetKey(index);
                }
                set
                {
                    throw new NotSupportedException(SR.NotSupported_KeyCollectionSet);
                }
            }

            public virtual IEnumerator GetEnumerator()
            {
                return new SortedListEnumerator(_sortedList, 0, _sortedList.Count, SortedListEnumerator.Keys);
            }

            public virtual int IndexOf(Object key)
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key), SR.ArgumentNull_Key);
                Contract.EndContractBlock();

                int i = Array.BinarySearch(_sortedList._keys, 0,
                                           _sortedList.Count, key, _sortedList._comparer);
                if (i >= 0) return i;
                return -1;
            }

            public virtual void Remove(Object key)
            {
                throw new NotSupportedException(SR.NotSupported_SortedListNestedWrite);
            }

            public virtual void RemoveAt(int index)
            {
                throw new NotSupportedException(SR.NotSupported_SortedListNestedWrite);
            }
        }

        [Serializable]
        private class ValueList : IList
        {
            private SortedList _sortedList;

            internal ValueList(SortedList sortedList)
            {
                _sortedList = sortedList;
            }

            public virtual int Count
            {
                get { return _sortedList._size; }
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
                get { return _sortedList.IsSynchronized; }
            }

            public virtual Object SyncRoot
            {
                get { return _sortedList.SyncRoot; }
            }

            public virtual int Add(Object key)
            {
                throw new NotSupportedException(SR.NotSupported_SortedListNestedWrite);
            }

            public virtual void Clear()
            {
                throw new NotSupportedException(SR.NotSupported_SortedListNestedWrite);
            }

            public virtual bool Contains(Object value)
            {
                return _sortedList.ContainsValue(value);
            }

            public virtual void CopyTo(Array array, int arrayIndex)
            {
                if (array != null && array.Rank != 1)
                    throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, nameof(array));
                Contract.EndContractBlock();

                // defer error checking to Array.Copy
                Array.Copy(_sortedList._values, 0, array, arrayIndex, _sortedList.Count);
            }

            public virtual void Insert(int index, Object value)
            {
                throw new NotSupportedException(SR.NotSupported_SortedListNestedWrite);
            }

            public virtual Object this[int index]
            {
                get
                {
                    return _sortedList.GetByIndex(index);
                }
                set
                {
                    throw new NotSupportedException(SR.NotSupported_SortedListNestedWrite);
                }
            }

            public virtual IEnumerator GetEnumerator()
            {
                return new SortedListEnumerator(_sortedList, 0, _sortedList.Count, SortedListEnumerator.Values);
            }

            public virtual int IndexOf(Object value)
            {
                return Array.IndexOf(_sortedList._values, value, 0, _sortedList.Count);
            }

            public virtual void Remove(Object value)
            {
                throw new NotSupportedException(SR.NotSupported_SortedListNestedWrite);
            }

            public virtual void RemoveAt(int index)
            {
                throw new NotSupportedException(SR.NotSupported_SortedListNestedWrite);
            }
        }

        // internal debug view class for sorted list
        internal class SortedListDebugView
        {
            private SortedList _sortedList;

            public SortedListDebugView(SortedList sortedList)
            {
                if (sortedList == null)
                {
                    throw new ArgumentNullException(nameof(sortedList));
                }
                Contract.EndContractBlock();

                _sortedList = sortedList;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public KeyValuePairs[] Items
            {
                get
                {
                    return _sortedList.ToKeyValuePairsArray();
                }
            }
        }
    }
}
