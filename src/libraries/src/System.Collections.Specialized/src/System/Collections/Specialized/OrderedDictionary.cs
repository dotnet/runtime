// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.Serialization;

namespace System.Collections.Specialized
{
    /// <devdoc>
    /// <para>
    /// OrderedDictionary offers IDictionary syntax with ordering.  Objects
    /// added or inserted in an IOrderedDictionary must have both a key and an index, and
    /// can be retrieved by either.
    /// OrderedDictionary is used by the ParameterCollection because MSAccess relies on ordering of
    /// parameters, while almost all other DBs do not.  DataKeyArray also uses it so
    /// DataKeys can be retrieved by either their name or their index.
    /// 
    /// OrderedDictionary implements IDeserializationCallback because it needs to have the
    /// contained ArrayList and Hashtable deserialized before it tries to get its count and objects.
    /// </para>
    /// </devdoc>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class OrderedDictionary : IOrderedDictionary, ISerializable, IDeserializationCallback
    {
        private ArrayList _objectsArray;
        private Hashtable _objectsTable;
        private int _initialCapacity;
        private IEqualityComparer _comparer;
        private bool _readOnly;
        private Object _syncRoot;
        private SerializationInfo _siInfo; //A temporary variable which we need during deserialization.

        private const string KeyComparerName = "KeyComparer"; // Do not rename (binary serialization)
        private const string ArrayListName = "ArrayList"; // Do not rename (binary serialization)
        private const string ReadOnlyName = "ReadOnly"; // Do not rename (binary serialization)
        private const string InitCapacityName = "InitialCapacity"; // Do not rename (binary serialization)

        public OrderedDictionary() : this(0)
        {
        }

        public OrderedDictionary(int capacity) : this(capacity, null)
        {
        }

        public OrderedDictionary(IEqualityComparer comparer) : this(0, comparer)
        {
        }

        public OrderedDictionary(int capacity, IEqualityComparer comparer)
        {
            _initialCapacity = capacity;
            _comparer = comparer;
        }

        private OrderedDictionary(OrderedDictionary dictionary)
        {
            Debug.Assert(dictionary != null);

            _readOnly = true;
            _objectsArray = dictionary._objectsArray;
            _objectsTable = dictionary._objectsTable;
            _comparer = dictionary._comparer;
            _initialCapacity = dictionary._initialCapacity;
        }

        protected OrderedDictionary(SerializationInfo info, StreamingContext context)
        {
            // We can't do anything with the keys and values until the entire graph has been deserialized
            // and getting Counts and objects won't fail.  For the time being, we'll just cache this.  
            // The graph is not valid until OnDeserialization has been called.
            _siInfo = info;
        }

        /// <devdoc>
        /// Gets the size of the table.
        /// </devdoc>
        public int Count
        {
            get
            {
                return objectsArray.Count;
            }
        }

        /// <devdoc>
        /// Indicates that the collection can grow.
        /// </devdoc>
        bool IDictionary.IsFixedSize
        {
            get
            {
                return _readOnly;
            }
        }

        /// <devdoc>
        /// Indicates that the collection is not read-only
        /// </devdoc>
        public bool IsReadOnly
        {
            get
            {
                return _readOnly;
            }
        }

        /// <devdoc>
        /// Indicates that this class is not synchronized
        /// </devdoc>
        bool ICollection.IsSynchronized
        {
            get
            {
                return false;
            }
        }

        /// <devdoc>
        /// Gets the collection of keys in the table in order.
        /// </devdoc>
        public ICollection Keys
        {
            get
            {
                return new OrderedDictionaryKeyValueCollection(objectsArray, true);
            }
        }

        private ArrayList objectsArray
        {
            get
            {
                if (_objectsArray == null)
                {
                    _objectsArray = new ArrayList(_initialCapacity);
                }
                return _objectsArray;
            }
        }

        private Hashtable objectsTable
        {
            get
            {
                if (_objectsTable == null)
                {
                    _objectsTable = new Hashtable(_initialCapacity, _comparer);
                }
                return _objectsTable;
            }
        }

        /// <devdoc>
        /// The SyncRoot object.  Not used because IsSynchronized is false
        /// </devdoc>
        object ICollection.SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    System.Threading.Interlocked.CompareExchange(ref _syncRoot, new Object(), null);
                }
                return _syncRoot;
            }
        }

        /// <devdoc>
        /// Gets or sets the object at the specified index
        /// </devdoc>
        public object this[int index]
        {
            get
            {
                return ((DictionaryEntry)objectsArray[index]).Value;
            }
            set
            {
                if (_readOnly)
                {
                    throw new NotSupportedException(SR.OrderedDictionary_ReadOnly);
                }
                if (index < 0 || index >= objectsArray.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                object key = ((DictionaryEntry)objectsArray[index]).Key;
                objectsArray[index] = new DictionaryEntry(key, value);
                objectsTable[key] = value;
            }
        }

        /// <devdoc>
        /// Gets or sets the object with the specified key
        /// </devdoc>
        public object this[object key]
        {
            get
            {
                return objectsTable[key];
            }
            set
            {
                if (_readOnly)
                {
                    throw new NotSupportedException(SR.OrderedDictionary_ReadOnly);
                }
                if (objectsTable.Contains(key))
                {
                    objectsTable[key] = value;
                    objectsArray[IndexOfKey(key)] = new DictionaryEntry(key, value);
                }
                else
                {
                    Add(key, value);
                }
            }
        }

        /// <devdoc>
        /// Returns an arrayList of the values in the table
        /// </devdoc>
        public ICollection Values
        {
            get
            {
                return new OrderedDictionaryKeyValueCollection(objectsArray, false);
            }
        }

        /// <devdoc>
        /// Adds a new entry to the table with the lowest-available index.
        /// </devdoc>
        public void Add(object key, object value)
        {
            if (_readOnly)
            {
                throw new NotSupportedException(SR.OrderedDictionary_ReadOnly);
            }
            objectsTable.Add(key, value);
            objectsArray.Add(new DictionaryEntry(key, value));
        }

        /// <devdoc>
        /// Clears all elements in the table.
        /// </devdoc>
        public void Clear()
        {
            if (_readOnly)
            {
                throw new NotSupportedException(SR.OrderedDictionary_ReadOnly);
            }
            objectsTable.Clear();
            objectsArray.Clear();
        }

        /// <devdoc>
        /// Returns a readonly OrderedDictionary for the given OrderedDictionary.
        /// </devdoc>
        public OrderedDictionary AsReadOnly()
        {
            return new OrderedDictionary(this);
        }

        /// <devdoc>
        /// Returns true if the key exists in the table, false otherwise.
        /// </devdoc>
        public bool Contains(object key)
        {
            return objectsTable.Contains(key);
        }

        /// <devdoc>
        /// Copies the table to an array.  This will not preserve order.
        /// </devdoc>
        public void CopyTo(Array array, int index)
        {
            objectsTable.CopyTo(array, index);
        }

        private int IndexOfKey(object key)
        {
            for (int i = 0; i < objectsArray.Count; i++)
            {
                object o = ((DictionaryEntry)objectsArray[i]).Key;
                if (_comparer != null)
                {
                    if (_comparer.Equals(o, key))
                    {
                        return i;
                    }
                }
                else
                {
                    if (o.Equals(key))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        /// <devdoc>
        /// Inserts a new object at the given index with the given key.
        /// </devdoc>
        public void Insert(int index, object key, object value)
        {
            if (_readOnly)
            {
                throw new NotSupportedException(SR.OrderedDictionary_ReadOnly);
            }
            if (index > Count || index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            objectsTable.Add(key, value);
            objectsArray.Insert(index, new DictionaryEntry(key, value));
        }

        /// <devdoc>
        /// Removes the entry at the given index.
        /// </devdoc>
        public void RemoveAt(int index)
        {
            if (_readOnly)
            {
                throw new NotSupportedException(SR.OrderedDictionary_ReadOnly);
            }
            if (index >= Count || index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            object key = ((DictionaryEntry)objectsArray[index]).Key;
            objectsArray.RemoveAt(index);
            objectsTable.Remove(key);
        }

        /// <devdoc>
        /// Removes the entry with the given key.
        /// </devdoc>
        public void Remove(object key)
        {
            if (_readOnly)
            {
                throw new NotSupportedException(SR.OrderedDictionary_ReadOnly);
            }
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            int index = IndexOfKey(key);
            if (index < 0)
            {
                return;
            }

            objectsTable.Remove(key);
            objectsArray.RemoveAt(index);
        }

#region IDictionary implementation
        public virtual IDictionaryEnumerator GetEnumerator()
        {
            return new OrderedDictionaryEnumerator(objectsArray, OrderedDictionaryEnumerator.DictionaryEntry);
        }
#endregion

#region IEnumerable implementation
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new OrderedDictionaryEnumerator(objectsArray, OrderedDictionaryEnumerator.DictionaryEntry);
        }
#endregion

#region ISerializable implementation 
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue(KeyComparerName, _comparer, typeof(IEqualityComparer));
            info.AddValue(ReadOnlyName, _readOnly);
            info.AddValue(InitCapacityName, _initialCapacity);

            object[] serArray = new object[Count];
            _objectsArray.CopyTo(serArray);
            info.AddValue(ArrayListName, serArray);
        }
#endregion

#region IDeserializationCallback implementation
        void IDeserializationCallback.OnDeserialization(object sender) {
            OnDeserialization(sender);
        }
        
        protected virtual void OnDeserialization(object sender)
        {
            if (_siInfo == null)
            {
                throw new SerializationException(SR.Serialization_InvalidOnDeser);
            }
            _comparer = (IEqualityComparer)_siInfo.GetValue(KeyComparerName, typeof(IEqualityComparer));
            _readOnly = _siInfo.GetBoolean(ReadOnlyName);
            _initialCapacity = _siInfo.GetInt32(InitCapacityName);

            object[] serArray = (object[])_siInfo.GetValue(ArrayListName, typeof(object[]));

            if (serArray != null)
            {
                foreach (object o in serArray)
                {
                    DictionaryEntry entry;
                    try
                    {
                        // DictionaryEntry is a value type, so it can only be casted.
                        entry = (DictionaryEntry)o;
                    }
                    catch
                    {
                        throw new SerializationException(SR.OrderedDictionary_SerializationMismatch);
                    }
                    objectsArray.Add(entry);
                    objectsTable.Add(entry.Key, entry.Value);
                }
            }
        }
#endregion

        /// <devdoc>
        /// OrderedDictionaryEnumerator works just like any other IDictionaryEnumerator, but it retrieves DictionaryEntries
        /// in the order by index.
        /// </devdoc>
        private class OrderedDictionaryEnumerator : IDictionaryEnumerator
        {
            private int _objectReturnType;
            internal const int Keys = 1;
            internal const int Values = 2;
            internal const int DictionaryEntry = 3;
            private IEnumerator _arrayEnumerator;

            internal OrderedDictionaryEnumerator(ArrayList array, int objectReturnType)
            {
                _arrayEnumerator = array.GetEnumerator();
                _objectReturnType = objectReturnType;
            }

            /// <devdoc>
            /// Retrieves the current DictionaryEntry.  This is the same as Entry, but not strongly-typed.
            /// </devdoc>
            public object Current
            {
                get
                {
                    if (_objectReturnType == Keys)
                    {
                        return ((DictionaryEntry)_arrayEnumerator.Current).Key;
                    }
                    if (_objectReturnType == Values)
                    {
                        return ((DictionaryEntry)_arrayEnumerator.Current).Value;
                    }
                    return Entry;
                }
            }

            /// <devdoc>
            /// Retrieves the current DictionaryEntry
            /// </devdoc>
            public DictionaryEntry Entry
            {
                get
                {
                    return new DictionaryEntry(((DictionaryEntry)_arrayEnumerator.Current).Key, ((DictionaryEntry)_arrayEnumerator.Current).Value);
                }
            }

            /// <devdoc>
            /// Retrieves the key of the current DictionaryEntry
            /// </devdoc>
            public object Key
            {
                get
                {
                    return ((DictionaryEntry)_arrayEnumerator.Current).Key;
                }
            }

            /// <devdoc>
            /// Retrieves the value of the current DictionaryEntry
            /// </devdoc>
            public object Value
            {
                get
                {
                    return ((DictionaryEntry)_arrayEnumerator.Current).Value;
                }
            }

            /// <devdoc>
            /// Moves the enumerator pointer to the next member
            /// </devdoc>
            public bool MoveNext()
            {
                return _arrayEnumerator.MoveNext();
            }

            /// <devdoc>
            /// Resets the enumerator pointer to the beginning.
            /// </devdoc>
            public void Reset()
            {
                _arrayEnumerator.Reset();
            }
        }

        /// <devdoc>
        /// OrderedDictionaryKeyValueCollection implements a collection for the Values and Keys properties
        /// that is "live"- it will reflect changes to the OrderedDictionary on the collection made after the getter
        /// was called.
        /// </devdoc>
        private class OrderedDictionaryKeyValueCollection : ICollection
        {
            private ArrayList _objects;
            private bool _isKeys;

            public OrderedDictionaryKeyValueCollection(ArrayList array, bool isKeys)
            {
                _objects = array;
                _isKeys = isKeys;
            }

            void ICollection.CopyTo(Array array, int index)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));
                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_NeedNonNegNum);
                foreach (object o in _objects)
                {
                    array.SetValue(_isKeys ? ((DictionaryEntry)o).Key : ((DictionaryEntry)o).Value, index);
                    index++;
                }
            }

            int ICollection.Count
            {
                get
                {
                    return _objects.Count;
                }
            }

            bool ICollection.IsSynchronized
            {
                get
                {
                    return false;
                }
            }

            object ICollection.SyncRoot
            {
                get
                {
                    return _objects.SyncRoot;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new OrderedDictionaryEnumerator(_objects, _isKeys == true ? OrderedDictionaryEnumerator.Keys : OrderedDictionaryEnumerator.Values);
            }
        }
    }
}

