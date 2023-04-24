// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Net.Http.Headers
{
    internal sealed class UnvalidatedObjectCollection<T> : ObjectCollection<T> where T : class
    {
        public override void Validate(T item)
        {
            ArgumentNullException.ThrowIfNull(item);
        }
    }

    /// <summary>An <see cref="ICollection{T}"/> list that prohibits null elements and that is optimized for a small number of elements.</summary>
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(ObjectCollection<>.DebugView))]
    internal abstract class ObjectCollection<T> : ICollection<T> where T : class
    {
        private const int DefaultSize = 4;

        /// <summary>null, a T, or a T[].</summary>
        internal object? _items;
        /// <summary>Number of elements stored in the collection.</summary>
        internal int _size;

        public ObjectCollection() { }

        public int Count => _size;

        public bool IsReadOnly => false;

        public abstract void Validate(T item);

        public void Add(T item)
        {
            Validate(item);
            Debug.Assert(item != null);

            if (_items is null)
            {
                // The collection is empty. Just store the new item directly.
                _items = item;
                _size = 1;
            }
            else if (_items is T existingItem)
            {
                // The collection has a single item stored directly.  Upgrade to
                // an array, and store both the existing and new items.
                Debug.Assert(_size == 1);
                T[] items = new T[DefaultSize];
                items[0] = existingItem;
                items[1] = item;
                _items = items;
                _size = 2;
            }
            else
            {
                T[] array = (T[])_items;
                int size = _size;
                if ((uint)size < (uint)array.Length)
                {
                    // There's room in the existing array.  Add the item.
                    array[size] = item;
                }
                else
                {
                    // We need to grow the array.  Do so, and store the new item.
                    Debug.Assert(_size > 0);
                    Debug.Assert(_size == array.Length);

                    var newItems = new T[array.Length * 2];
                    Array.Copy(array, newItems, size);
                    _items = newItems;
                    newItems[size] = item;
                }
                _size = size + 1;
            }
        }

        public void Clear()
        {
            _items = null;
            _size = 0;
        }

        public bool Contains(T item) =>
            _size <= 0 ? false :
            _items is T o ? o.Equals(item) :
            _items is T[] items && Array.IndexOf(items, item, 0, _size) != -1;

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (_items is T[] items)
            {
                Array.Copy(items, 0, array, arrayIndex, _size);
            }
            else
            {
                Debug.Assert(_size == 0 || _size == 1);
                if (array is null || _size > array.Length - arrayIndex)
                {
                    // Use Array.CopyTo to throw the right exceptions.
                    new T[] { (T)_items! }.CopyTo(array!, arrayIndex);
                }
                else if (_size == 1)
                {
                    array[arrayIndex] = (T)_items!;
                }
            }
        }

        public bool Remove(T item)
        {
            if (_items is T o)
            {
                if (o.Equals(item))
                {
                    _items = null;
                    _size = 0;
                    return true;
                }
            }
            else if (_items is T[] items)
            {
                int index = Array.IndexOf(items, item, 0, _size);
                if (index != -1)
                {
                    _size--;
                    if (index < _size)
                    {
                        Array.Copy(items, index + 1, items, index, _size - index);
                    }
                    items[_size] = null!;

                    return true;
                }
            }

            return false;
        }

        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<T>
        {
            private readonly ObjectCollection<T> _list;
            private int _index;
            private T _current;

            internal Enumerator(ObjectCollection<T> list)
            {
                _list = list;
                _index = 0;
                _current = default!;
            }

            public void Dispose() { }

            public bool MoveNext()
            {
                ObjectCollection<T> list = _list;

                if ((uint)_index < (uint)list._size)
                {
                    _current = list._items is T[] items ? items[_index] : (T)list._items!;
                    _index++;
                    return true;
                }

                _index = _list._size + 1;
                _current = default!;
                return false;
            }

            public T Current => _current!;

            object? IEnumerator.Current => _current;

            void IEnumerator.Reset()
            {
                _index = 0;
                _current = default!;
            }
        }

        internal sealed class DebugView
        {
            private readonly ObjectCollection<T> _collection;

            public DebugView(ObjectCollection<T> collection)
            {
                ArgumentNullException.ThrowIfNull(collection);
                _collection = collection;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public T[] Items
            {
                get
                {
                    T[] items = new T[_collection.Count];
                    _collection.CopyTo(items, 0);
                    return items;
                }
            }
        }
    }
}
