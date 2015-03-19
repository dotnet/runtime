// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace System.Linq.Expressions
{
    /// <summary>
    /// A simple hashset, built on Dictionary{K, V}
    /// </summary>
    internal sealed class Set<T> : ICollection<T>
    {
        private readonly Dictionary<T, object> _data;

        internal Set()
        {
            _data = new Dictionary<T, object>();
        }

        internal Set(IEqualityComparer<T> comparer)
        {
            _data = new Dictionary<T, object>(comparer);
        }

        internal Set(IList<T> list)
        {
            _data = new Dictionary<T, object>(list.Count);
            foreach (T t in list)
            {
                Add(t);
            }
        }

        internal Set(IEnumerable<T> list)
        {
            _data = new Dictionary<T, object>();
            foreach (T t in list)
            {
                Add(t);
            }
        }

        internal Set(int capacity)
        {
            _data = new Dictionary<T, object>(capacity);
        }

        public void Add(T item)
        {
            _data[item] = null;
        }

        public void Clear()
        {
            _data.Clear();
        }

        public bool Contains(T item)
        {
            return _data.ContainsKey(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _data.Keys.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _data.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            return _data.Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _data.Keys.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _data.Keys.GetEnumerator();
        }
    }
}
