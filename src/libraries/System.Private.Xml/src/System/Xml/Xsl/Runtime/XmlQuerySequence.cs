// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Text;
using System.Xml.Schema;
using System.Xml.Xsl;
using System.Xml.XPath;
using System.Diagnostics;
using System.ComponentModel;

namespace System.Xml.Xsl.Runtime
{
    /// <summary>
    /// A sequence of Xml values that dynamically expands and allows random access to items.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class XmlQuerySequence<T> : IList<T>, System.Collections.IList
    {
        public static readonly XmlQuerySequence<T> Empty = new XmlQuerySequence<T>();

        private static readonly Type s_XPathItemType = typeof(XPathItem);

        private T[] _items;
        private int _size;

#if DEBUG
        private const int DefaultCacheSize = 2;
#else
        private const int DefaultCacheSize = 16;
#endif

        /// <summary>
        /// If "seq" is non-null, then clear it and reuse it.  Otherwise, create a new XmlQuerySequence.
        /// </summary>
        public static XmlQuerySequence<T> CreateOrReuse(XmlQuerySequence<T> seq)
        {
            if (seq != null)
            {
                seq.Clear();
                return seq;
            }

            return new XmlQuerySequence<T>();
        }

        /// <summary>
        /// If "seq" is non-null, then clear it and reuse it.  Otherwise, create a new XmlQuerySequence.
        /// Add "item" to the sequence.
        /// </summary>
        public static XmlQuerySequence<T> CreateOrReuse(XmlQuerySequence<T> seq, T item)
        {
            if (seq != null)
            {
                seq.Clear();
                seq.Add(item);
                return seq;
            }

            return new XmlQuerySequence<T>(item);
        }

        /// <summary>
        /// Construct new sequence.
        /// </summary>
        public XmlQuerySequence()
        {
            _items = new T[DefaultCacheSize];
        }

        /// <summary>
        /// Construct new sequence.
        /// </summary>
        public XmlQuerySequence(int capacity)
        {
            _items = new T[capacity];
        }

        /// <summary>
        /// Construct sequence from the specified array.
        /// </summary>
        public XmlQuerySequence(T[] array, int size)
        {
            _items = array;
            _size = size;
        }

        /// <summary>
        /// Construct singleton sequence having "value" as its only element.
        /// </summary>
        public XmlQuerySequence(T value)
        {
            _items = new T[1];
            _items[0] = value;
            _size = 1;
        }


        //-----------------------------------------------
        // IEnumerable implementation
        //-----------------------------------------------

        /// <summary>
        /// Return IEnumerator implementation.
        /// </summary>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new IListEnumerator<T>(this);
        }


        //-----------------------------------------------
        // IEnumerable<T> implementation
        //-----------------------------------------------

        /// <summary>
        /// Return IEnumerator{T} implementation.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            return new IListEnumerator<T>(this);
        }


        //-----------------------------------------------
        // ICollection implementation
        //-----------------------------------------------

        /// <summary>
        /// Return the number of items in the sequence.
        /// </summary>
        public int Count
        {
            get { return _size; }
        }

        /// <summary>
        /// The XmlQuerySequence is not thread-safe.
        /// </summary>
        bool System.Collections.ICollection.IsSynchronized
        {
            get { return false; }
        }

        /// <summary>
        /// This instance can be used to synchronize access.
        /// </summary>
        object System.Collections.ICollection.SyncRoot
        {
            get { return this; }
        }

        /// <summary>
        /// Copy contents of this sequence to the specified Array, starting at the specified index in the target array.
        /// </summary>
        void System.Collections.ICollection.CopyTo(Array array, int index)
        {
            if (_size == 0)
                return;

            Array.Copy(_items, 0, array, index, _size);
        }


        //-----------------------------------------------
        // ICollection<T> implementation
        //-----------------------------------------------

        /// <summary>
        /// Items may not be added, removed, or modified through the ICollection{T} interface.
        /// </summary>
        bool ICollection<T>.IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        /// Items may not be added through the ICollection{T} interface.
        /// </summary>
        void ICollection<T>.Add(T value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Items may not be cleared through the ICollection{T} interface.
        /// </summary>
        void ICollection<T>.Clear()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns true if the specified value is in the sequence.
        /// </summary>
        public bool Contains(T value)
        {
            return IndexOf(value) != -1;
        }

        /// <summary>
        /// Copy contents of this sequence to the specified Array, starting at the specified index in the target array.
        /// </summary>
        public void CopyTo(T[] array, int index)
        {
            for (int i = 0; i < Count; i++)
                array[index + i] = this[i];
        }

        /// <summary>
        /// Items may not be removed through the ICollection{T} interface.
        /// </summary>
        bool ICollection<T>.Remove(T value)
        {
            throw new NotSupportedException();
        }


        //-----------------------------------------------
        // IList implementation
        //-----------------------------------------------

        /// <summary>
        /// Items may not be added, removed, or modified through the IList interface.
        /// </summary>
        bool System.Collections.IList.IsFixedSize
        {
            get { return true; }
        }

        /// <summary>
        /// Items may not be added, removed, or modified through the IList interface.
        /// </summary>
        bool System.Collections.IList.IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        /// Return item at the specified index.
        /// </summary>
        object System.Collections.IList.this[int index]
        {
            get
            {
                if (index >= _size)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return _items[index];
            }
            set { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Items may not be added through the IList interface.
        /// </summary>
        int System.Collections.IList.Add(object value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Items may not be cleared through the IList interface.
        /// </summary>
        void System.Collections.IList.Clear()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns true if the specified value is in the sequence.
        /// </summary>
        bool System.Collections.IList.Contains(object value)
        {
            return Contains((T)value);
        }

        /// <summary>
        /// Returns the index of the specified value in the sequence.
        /// </summary>
        int System.Collections.IList.IndexOf(object value)
        {
            return IndexOf((T)value);
        }

        /// <summary>
        /// Items may not be added through the IList interface.
        /// </summary>
        void System.Collections.IList.Insert(int index, object value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Items may not be removed through the IList interface.
        /// </summary>
        void System.Collections.IList.Remove(object value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Items may not be removed through the IList interface.
        /// </summary>
        void System.Collections.IList.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }


        //-----------------------------------------------
        // IList<T> implementation
        //-----------------------------------------------

        /// <summary>
        /// Return item at the specified index.
        /// </summary>
        public T this[int index]
        {
            get
            {
                if (index >= _size)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return _items[index];
            }
            set { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Returns the index of the specified value in the sequence.
        /// </summary>
        public int IndexOf(T value)
        {
            int index = Array.IndexOf(_items, value);
            return (index < _size) ? index : -1;
        }

        /// <summary>
        /// Items may not be added through the IList{T} interface.
        /// </summary>
        void IList<T>.Insert(int index, T value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Items may not be removed through the IList{T} interface.
        /// </summary>
        void IList<T>.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }


        //-----------------------------------------------
        // XmlQuerySequence methods
        //-----------------------------------------------

        /// <summary>
        /// Clear the cache.
        /// </summary>
        public void Clear()
        {
            _size = 0;
            OnItemsChanged();
        }

        /// <summary>
        /// Add an item to the sequence.
        /// </summary>
        public void Add(T value)
        {
            EnsureCache();
            _items[_size++] = value;
            OnItemsChanged();
        }

        /// <summary>
        /// Sort the items in the cache using the keys contained in the provided array.
        /// </summary>
        public void SortByKeys(Array keys)
        {
            if (_size <= 1)
                return;

            Debug.Assert(keys.Length >= _size, "Number of keys must be >= number of items.");
            Array.Sort(keys, _items, 0, _size);
            OnItemsChanged();
        }

        /// <summary>
        /// Ensure that an array of the specified type is created and has room for at least one more item.
        /// </summary>
        private void EnsureCache()
        {
            T[] cacheNew;

            if (_size >= _items.Length)
            {
                cacheNew = new T[_size * 2];
                CopyTo(cacheNew, 0);
                _items = cacheNew;
            }
        }

        /// <summary>
        /// This method is called when one or more items in the cache have been added or removed.
        /// By default, it does nothing, but subclasses can override it.
        /// </summary>
        protected virtual void OnItemsChanged()
        {
        }
    }

    /// <summary>
    /// A sequence of Xml items that dynamically expands and allows random access to items.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class XmlQueryItemSequence : XmlQuerySequence<XPathItem>
    {
        public new static readonly XmlQueryItemSequence Empty = new XmlQueryItemSequence();

        /// <summary>
        /// If "seq" is non-null, then clear it and reuse it.  Otherwise, create a new XmlQueryItemSequence.
        /// </summary>
        public static XmlQueryItemSequence CreateOrReuse(XmlQueryItemSequence seq)
        {
            if (seq != null)
            {
                seq.Clear();
                return seq;
            }

            return new XmlQueryItemSequence();
        }

        /// <summary>
        /// If "seq" is non-null, then clear it and reuse it.  Otherwise, create a new XmlQueryItemSequence.
        /// Add "item" to the sequence.
        /// </summary>
        public static XmlQueryItemSequence CreateOrReuse(XmlQueryItemSequence seq, XPathItem item)
        {
            if (seq != null)
            {
                seq.Clear();
                seq.Add(item);
                return seq;
            }

            return new XmlQueryItemSequence(item);
        }

        /// <summary>
        /// Construct sequence from the specified array.
        /// </summary>
        public XmlQueryItemSequence() : base()
        {
        }

        /// <summary>
        /// Construct sequence with the specified initial capacity.
        /// </summary>
        public XmlQueryItemSequence(int capacity) : base(capacity)
        {
        }

        /// <summary>
        /// Construct singleton sequence from a single item.
        /// </summary>
        public XmlQueryItemSequence(XPathItem item) : base(1)
        {
            AddClone(item);
        }

        /// <summary>
        /// Add an item to the sequence; clone the item before doing so if it's a navigator.
        /// </summary>
        public void AddClone(XPathItem item)
        {
            if (item.IsNode)
                Add(((XPathNavigator)item).Clone());
            else
                Add(item);
        }
    }

    /// <summary>
    /// A sequence of Xml nodes that dynamically expands and allows random access to items.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class XmlQueryNodeSequence : XmlQuerySequence<XPathNavigator>, IList<XPathItem>
    {
        public new static readonly XmlQueryNodeSequence Empty = new XmlQueryNodeSequence();

        private XmlQueryNodeSequence _docOrderDistinct;

        /// <summary>
        /// If "seq" is non-null, then clear it and reuse it.  Otherwise, create a new XmlQueryNodeSequence.
        /// </summary>
        public static XmlQueryNodeSequence CreateOrReuse(XmlQueryNodeSequence seq)
        {
            if (seq != null)
            {
                seq.Clear();
                return seq;
            }

            return new XmlQueryNodeSequence();
        }

        /// <summary>
        /// If "seq" is non-null, then clear it and reuse it.  Otherwise, create a new XmlQueryNodeSequence.
        /// Add "nav" to the sequence.
        /// </summary>
        public static XmlQueryNodeSequence CreateOrReuse(XmlQueryNodeSequence seq, XPathNavigator navigator)
        {
            if (seq != null)
            {
                seq.Clear();
                seq.Add(navigator);
                return seq;
            }

            return new XmlQueryNodeSequence(navigator);
        }

        /// <summary>
        /// Construct sequence with the specified initial capacity.
        /// </summary>
        public XmlQueryNodeSequence() : base()
        {
        }

        /// <summary>
        /// Construct sequence from the specified array.
        /// </summary>
        public XmlQueryNodeSequence(int capacity) : base(capacity)
        {
        }

        /// <summary>
        /// Construct sequence from the specified array, cloning each navigator before adding it.
        /// </summary>
        public XmlQueryNodeSequence(IList<XPathNavigator> list) : base(list.Count)
        {
            for (int idx = 0; idx < list.Count; idx++)
                AddClone(list[idx]);
        }

        /// <summary>
        /// Construct sequence from the specified array.
        /// </summary>
        public XmlQueryNodeSequence(XPathNavigator[] array, int size) : base(array, size)
        {
        }

        /// <summary>
        /// Construct singleton sequence from a single navigator.
        /// </summary>
        public XmlQueryNodeSequence(XPathNavigator navigator) : base(1)
        {
            AddClone(navigator);
        }

        /// <summary>
        /// If this property is true, then the nodes in this cache are already in document order with no duplicates.
        /// </summary>
        public bool IsDocOrderDistinct
        {
            get { return (_docOrderDistinct == this) || Count <= 1; }
            set
            {
#if DEBUG
                if (Count > 1)
                {
                    if (value)
                    {
                        for (int iNav = 0; iNav < Count - 1; iNav++)
                        {
                            XmlNodeOrder cmp = this[iNav].ComparePosition(this[iNav + 1]);
                            Debug.Assert(cmp == XmlNodeOrder.Before || cmp == XmlNodeOrder.Unknown);
                        }
                    }
                }
#endif
                _docOrderDistinct = value ? this : null;
            }
        }

        /// <summary>
        /// Return a sequence which contains all distinct nodes in this cache, sorted by document order.
        /// </summary>
        public XmlQueryNodeSequence DocOrderDistinct(IComparer<XPathNavigator> comparer)
        {
            int iEach, iDistinct;
            XPathNavigator[] sortArray;

            if (_docOrderDistinct != null)
                return _docOrderDistinct;

            if (Count <= 1)
                return this;

            // Create a copy of this sequence
            sortArray = new XPathNavigator[Count];
            for (iEach = 0; iEach < sortArray.Length; iEach++)
                sortArray[iEach] = this[iEach];

            // Sort the navigators using a custom IComparer implementation that uses XPathNavigator.ComparePosition
            Array.Sort(sortArray, 0, Count, comparer);

            iDistinct = 0;
            for (iEach = 1; iEach < sortArray.Length; iEach++)
            {
                if (!sortArray[iDistinct].IsSamePosition(sortArray[iEach]))
                {
                    // Not a duplicate, so keep it in the cache
                    iDistinct++;

                    if (iDistinct != iEach)
                    {
                        // Fill in "hole" left by duplicate navigators
                        sortArray[iDistinct] = sortArray[iEach];
                    }
                }
            }

            _docOrderDistinct = new XmlQueryNodeSequence(sortArray, iDistinct + 1);
            _docOrderDistinct._docOrderDistinct = _docOrderDistinct;

            return _docOrderDistinct;
        }

        /// <summary>
        /// Add a node to the sequence; clone the navigator before doing so.
        /// </summary>
        public void AddClone(XPathNavigator navigator)
        {
            Add(navigator.Clone());
        }

        /// <summary>
        /// If any items in the sequence change, then clear docOrderDistinct pointer as well.
        /// </summary>
        protected override void OnItemsChanged()
        {
            _docOrderDistinct = null;
        }

        //-----------------------------------------------
        // IEnumerable<XPathItem> implementation
        //-----------------------------------------------

        /// <summary>
        /// Return IEnumerator{XPathItem} implementation.
        /// </summary>
        IEnumerator<XPathItem> IEnumerable<XPathItem>.GetEnumerator()
        {
            return new IListEnumerator<XPathItem>(this);
        }

        //-----------------------------------------------
        // ICollection<XPathItem> implementation
        //-----------------------------------------------

        /// <summary>
        /// Items may not be added, removed, or modified through the ICollection{T} interface.
        /// </summary>
        bool ICollection<XPathItem>.IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        /// Items may not be added through the ICollection{T} interface.
        /// </summary>
        void ICollection<XPathItem>.Add(XPathItem value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Items may not be cleared through the ICollection{T} interface.
        /// </summary>
        void ICollection<XPathItem>.Clear()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns true if the specified value is in the sequence.
        /// </summary>
        bool ICollection<XPathItem>.Contains(XPathItem value)
        {
            return IndexOf((XPathNavigator)value) != -1;
        }

        /// <summary>
        /// Copy contents of this sequence to the specified Array, starting at the specified index in the target array.
        /// </summary>
        void ICollection<XPathItem>.CopyTo(XPathItem[] array, int index)
        {
            for (int i = 0; i < Count; i++)
                array[index + i] = this[i];
        }

        /// <summary>
        /// Items may not be removed through the ICollection{T} interface.
        /// </summary>
        bool ICollection<XPathItem>.Remove(XPathItem value)
        {
            throw new NotSupportedException();
        }

        //-----------------------------------------------
        // IList<XPathItem> implementation
        //-----------------------------------------------

        /// <summary>
        /// Return item at the specified index.
        /// </summary>
        XPathItem IList<XPathItem>.this[int index]
        {
            get
            {
                if (index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return base[index];
            }
            set { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Returns the index of the specified value in the sequence.
        /// </summary>
        int IList<XPathItem>.IndexOf(XPathItem value)
        {
            return IndexOf((XPathNavigator)value);
        }

        /// <summary>
        /// Items may not be added through the IList{T} interface.
        /// </summary>
        void IList<XPathItem>.Insert(int index, XPathItem value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Items may not be removed through the IList{T} interface.
        /// </summary>
        void IList<XPathItem>.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }
    }
}

