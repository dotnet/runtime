// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Text.Json.Serialization.Tests
{
    public struct GenericStructIListWrapper<T> : IList<T>
    {
        private List<T> _list;
        public T this[int index]
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

        public int Count => _list == null ? 0 : _list.Count;

        public bool IsReadOnly => false;

        private void InitializeIfNull()
        {
            if (_list == null)
            {
                _list = new List<T>();
            }
        }

        public void Add(T item)
        {
            InitializeIfNull();
            _list.Add(item);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(T item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            _list.Insert(index, item);
        }

        public bool Remove(T item)
        {
            return _list.Remove(item);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public struct GenericStructICollectionWrapper<T> : ICollection<T>
    {
        private List<T> _list;

        private void InitializeIfNull()
        {
            if (_list == null)
            {
                _list = new List<T>();
            }
        }

        public int Count => _list.Count;

        public bool IsReadOnly => false;

        public void Add(T item)
        {
            InitializeIfNull();
            _list.Add(item);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(T item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((ICollection<T>)_list).GetEnumerator();
        }

        public bool Remove(T item)
        {
            return _list.Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((ICollection<T>)_list).GetEnumerator();
        }
    }

    public struct GenericStructISetWrapper<T> : ISet<T>
    {
        private HashSet<T> _hashset;

        private void InitializeIfNull()
        {
            if (_hashset == null)
            {
                _hashset = new HashSet<T>();
            }
        }

        public int Count => _hashset == null ? 0 : _hashset.Count;

        public bool IsReadOnly => false;

        public bool Add(T item)
        {
            InitializeIfNull();
            return _hashset.Add(item);
        }

        public void Clear()
        {
            _hashset.Clear();
        }

        public bool Contains(T item)
        {
            return _hashset.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _hashset.CopyTo(array, arrayIndex);
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            _hashset.ExceptWith(other);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((ISet<T>)_hashset).GetEnumerator();
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            _hashset.IntersectWith(other);
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return _hashset.IsProperSubsetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return _hashset.IsProperSupersetOf(other);
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return _hashset.IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return _hashset.IsSupersetOf(other);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            return _hashset.Overlaps(other);
        }

        public bool Remove(T item)
        {
            return _hashset.Remove(item);
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            return _hashset.SetEquals(other);
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            _hashset.SymmetricExceptWith(other);
        }

        public void UnionWith(IEnumerable<T> other)
        {
            _hashset.UnionWith(other);
        }

        void ICollection<T>.Add(T item)
        {
            _hashset.Add(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((ISet<T>)_hashset).GetEnumerator();
        }
    }

    public struct GenericStructIDictionaryWrapper<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private Dictionary<TKey, TValue> _dict;

        private void InitializeIfNull()
        {
            if (_dict == null)
            {
                _dict = new Dictionary<TKey, TValue>();
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                InitializeIfNull();
                return _dict[key];
            }
            set
            {
                InitializeIfNull();
                _dict[key] = value;
            }
        }

        public ICollection<TKey> Keys => ((IDictionary<TKey, TValue>)_dict).Keys;

        public ICollection<TValue> Values => ((IDictionary<TKey, TValue>)_dict).Values;

        public int Count => _dict == null ? 0 : _dict.Count;

        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value)
        {
            InitializeIfNull();
            ((IDictionary<TKey, TValue>)_dict).Add(key, value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            InitializeIfNull();
            ((IDictionary<TKey, TValue>)_dict).Add(item);
        }

        public void Clear()
        {
            ((IDictionary<TKey, TValue>)_dict).Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ((IDictionary<TKey, TValue>)_dict).Contains(item);
        }

        public bool ContainsKey(TKey key)
        {
            return ((IDictionary<TKey, TValue>)_dict).ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((IDictionary<TKey, TValue>)_dict).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return ((IDictionary<TKey, TValue>)_dict).GetEnumerator();
        }

        public bool Remove(TKey key)
        {
            return ((IDictionary<TKey, TValue>)_dict).Remove(key);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return ((IDictionary<TKey, TValue>)_dict).Remove(item);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return ((IDictionary<TKey, TValue>)_dict).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IDictionary<TKey, TValue>)_dict).GetEnumerator();
        }
    }

}
