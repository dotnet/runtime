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
}
