// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Text.Json.Serialization.Tests
{
    public struct StructWrapperForIList : IList
    {
        private List<object> _list;

        private void InitializeIfNull()
        {
            if (_list == null)
            {
                _list = new List<object>();
            }
        }

        public object this[int index]
        {
            get
            {
                InitializeIfNull();
                return _list[index];
            }
            set
            {
                InitializeIfNull();
                _list[index] = value;
            }
        }

        public bool IsFixedSize => ((IList)_list).IsFixedSize;

        public bool IsReadOnly => false;

        public int Count => _list == null ? 0 : _list.Count;

        public bool IsSynchronized => ((IList)_list).IsSynchronized;

        public object SyncRoot => ((IList)_list).SyncRoot;

        public int Add(object value)
        {
            InitializeIfNull();
            return ((IList)_list).Add(value);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(object value)
        {
            return ((IList)_list).Contains(value);
        }

        public void CopyTo(Array array, int index)
        {
            ((IList)_list).CopyTo(array, index);
        }

        public IEnumerator GetEnumerator()
        {
            return ((IList)_list).GetEnumerator();
        }

        public int IndexOf(object value)
        {
            return _list.IndexOf(value);
        }

        public void Insert(int index, object value)
        {
            _list.Insert(index, value);
        }

        public void Remove(object value)
        {
            _list.Remove(value);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }
    }

    public struct StructWrapperForIDictionary : IDictionary
    {
        private Dictionary<string, object> _dictionary;

        private void InitializeIfNull()
        {
            if (_dictionary == null)
            {
                _dictionary = new Dictionary<string, object>();
            }
        }

        public object this[object key]
        {
            get
            {
                InitializeIfNull();
                return ((IDictionary)_dictionary)[key];
            }
            set
            {
                InitializeIfNull();
                ((IDictionary)_dictionary)[key] = value;
            }
        }

        public bool IsFixedSize => ((IDictionary)_dictionary).IsFixedSize;

        public bool IsReadOnly => false;

        public ICollection Keys => ((IDictionary)_dictionary).Keys;

        public ICollection Values => ((IDictionary)_dictionary).Values;

        public int Count => _dictionary.Count;

        public bool IsSynchronized => ((IDictionary)_dictionary).IsSynchronized;

        public object SyncRoot => ((IDictionary)_dictionary).SyncRoot;

        public void Add(object key, object value)
        {
            ((IDictionary)_dictionary).Add(key, value);
        }

        public void Clear()
        {
            _dictionary.Clear();
        }

        public bool Contains(object key)
        {
            return ((IDictionary)_dictionary).Contains(key);
        }

        public void CopyTo(Array array, int index)
        {
            ((IDictionary)_dictionary).CopyTo(array, index);
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            return ((IDictionary)_dictionary).GetEnumerator();
        }

        public void Remove(object key)
        {
            ((IDictionary)_dictionary).Remove(key);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IDictionary)_dictionary).GetEnumerator();
        }
    }
}
