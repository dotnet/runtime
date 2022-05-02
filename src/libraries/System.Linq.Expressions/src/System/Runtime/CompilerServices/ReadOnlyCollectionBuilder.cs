// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Builder for read only collections.
    /// </summary>
    /// <typeparam name="T">The type of the collection element.</typeparam>
    public sealed class ReadOnlyCollectionBuilder<T> : IList<T>, IList
    {
        private const int DefaultCapacity = 4;

        private T[] _items;
        private int _size;
        private int _version;

        /// <summary>
        /// Constructs a <see cref="ReadOnlyCollectionBuilder{T}"/>.
        /// </summary>
        public ReadOnlyCollectionBuilder()
        {
            _items = Array.Empty<T>();
        }

        /// <summary>
        /// Constructs a <see cref="ReadOnlyCollectionBuilder{T}"/> with a given initial capacity.
        /// The contents are empty but builder will have reserved room for the given
        /// number of elements before any reallocations are required.
        /// </summary>
        /// <param name="capacity">Initial capacity of the builder.</param>
        public ReadOnlyCollectionBuilder(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            _items = new T[capacity];
        }

        /// <summary>
        /// Constructs a <see cref="ReadOnlyCollectionBuilder{T}"/>, copying contents of the given collection.
        /// </summary>
        /// <param name="collection">The collection whose elements to copy to the builder.</param>
        public ReadOnlyCollectionBuilder(IEnumerable<T> collection)
        {
            ArgumentNullException.ThrowIfNull(collection);

            if (collection is ICollection<T> c)
            {
                int count = c.Count;
                _items = new T[count];
                c.CopyTo(_items, 0);
                _size = count;
            }
            else
            {
                _size = 0;
                _items = new T[DefaultCapacity];

                using (IEnumerator<T> en = collection.GetEnumerator())
                {
                    while (en.MoveNext())
                    {
                        Add(en.Current);
                    }
                }
            }
        }

        /// <summary>
        /// Gets and sets the capacity of this <see cref="ReadOnlyCollectionBuilder{T}"/>.
        /// </summary>
        public int Capacity
        {
            get { return _items.Length; }
            set
            {
                if (value < _size)
                    throw new ArgumentOutOfRangeException(nameof(value));

                if (value != _items.Length)
                {
                    if (value > 0)
                    {
                        T[] newItems = new T[value];
                        if (_size > 0)
                        {
                            Array.Copy(_items, newItems, _size);
                        }
                        _items = newItems;
                    }
                    else
                    {
                        _items = Array.Empty<T>();
                    }
                }
            }
        }

        /// <summary>
        /// Returns number of elements in the <see cref="ReadOnlyCollectionBuilder{T}"/>.
        /// </summary>
        public int Count => _size;

        #region IList<T> Members

        /// <summary>
        /// Returns the index of the first occurrence of a given value in the builder.
        /// </summary>
        /// <param name="item">An item to search for.</param>
        /// <returns>The index of the first occurrence of an item.</returns>
        public int IndexOf(T item)
        {
            return Array.IndexOf(_items, item, 0, _size);
        }

        /// <summary>
        /// Inserts an item to the <see cref="ReadOnlyCollectionBuilder{T}"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which item should be inserted.</param>
        /// <param name="item">The object to insert into the <see cref="ReadOnlyCollectionBuilder{T}"/>.</param>
        public void Insert(int index, T item)
        {
            if (index > _size)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (_size == _items.Length)
            {
                EnsureCapacity(_size + 1);
            }
            if (index < _size)
            {
                Array.Copy(_items, index, _items, index + 1, _size - index);
            }
            _items[index] = item;
            _size++;
            _version++;
        }

        /// <summary>
        /// Removes the <see cref="ReadOnlyCollectionBuilder{T}"/> item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _size)
                throw new ArgumentOutOfRangeException(nameof(index));

            _size--;
            if (index < _size)
            {
                Array.Copy(_items, index + 1, _items, index, _size - index);
            }
            _items[_size] = default(T)!;
            _version++;
        }

        /// <summary>
        ///  Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        public T this[int index]
        {
            get
            {
                if (index >= _size)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return _items[index];
            }
            set
            {
                if (index >= _size)
                    throw new ArgumentOutOfRangeException(nameof(index));

                _items[index] = value;
                _version++;
            }
        }

        #endregion

        #region ICollection<T> Members

        /// <summary>
        /// Adds an item to the <see cref="ReadOnlyCollectionBuilder{T}"/>.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="ReadOnlyCollectionBuilder{T}"/>.</param>
        public void Add(T item)
        {
            if (_size == _items.Length)
            {
                EnsureCapacity(_size + 1);
            }
            _items[_size++] = item;
            _version++;
        }

        /// <summary>
        /// Removes all items from the <see cref="ReadOnlyCollectionBuilder{T}"/>.
        /// </summary>
        public void Clear()
        {
            if (_size > 0)
            {
                Array.Clear(_items, 0, _size);
                _size = 0;
            }
            _version++;
        }

        /// <summary>
        /// Determines whether the <see cref="ReadOnlyCollectionBuilder{T}"/> contains a specific value
        /// </summary>
        /// <param name="item">the object to locate in the <see cref="ReadOnlyCollectionBuilder{T}"/>.</param>
        /// <returns>true if item is found in the <see cref="ReadOnlyCollectionBuilder{T}"/>; otherwise, false.</returns>
        public bool Contains(T item)
        {
            if ((object?)item == null)
            {
                for (int i = 0; i < _size; i++)
                {
                    if ((object?)_items[i] == null)
                    {
                        return true;
                    }
                }
                return false;
            }
            else
            {
                EqualityComparer<T> c = EqualityComparer<T>.Default;
                for (int i = 0; i < _size; i++)
                {
                    if (c.Equals(_items[i], item))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Copies the elements of the <see cref="ReadOnlyCollectionBuilder{T}"/> to an <see cref="Array"/>,
        /// starting at particular <see cref="Array"/> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="ReadOnlyCollectionBuilder{T}"/>.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(_items, 0, array, arrayIndex, _size);
        }

        bool ICollection<T>.IsReadOnly => false;

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="ReadOnlyCollectionBuilder{T}"/>.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="ReadOnlyCollectionBuilder{T}"/>.</param>
        /// <returns>true if item was successfully removed from the <see cref="ReadOnlyCollectionBuilder{T}"/>;
        /// otherwise, false. This method also returns false if item is not found in the original <see cref="ReadOnlyCollectionBuilder{T}"/>.
        /// </returns>
        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }

            return false;
        }

        #endregion

        #region IEnumerable<T> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.</returns>
        public IEnumerator<T> GetEnumerator() => new Enumerator(this);

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region IList Members

        bool IList.IsReadOnly => false;

        int IList.Add(object? value)
        {
            ValidateNullValue(value, nameof(value));
            try
            {
                Add((T)value!);
            }
            catch (InvalidCastException)
            {
                throw Error.InvalidTypeException(value, typeof(T), nameof(value));
            }
            return Count - 1;
        }

        bool IList.Contains(object? value)
        {
            if (IsCompatibleObject(value))
            {
                return Contains((T)value!);
            }
            else return false;
        }

        int IList.IndexOf(object? value)
        {
            if (IsCompatibleObject(value))
            {
                return IndexOf((T)value!);
            }
            return -1;
        }

        void IList.Insert(int index, object? value)
        {
            ValidateNullValue(value, nameof(value));
            try
            {
                Insert(index, (T)value!);
            }
            catch (InvalidCastException)
            {
                throw Error.InvalidTypeException(value, typeof(T), nameof(value));
            }
        }

        bool IList.IsFixedSize => false;

        void IList.Remove(object? value)
        {
            if (IsCompatibleObject(value))
            {
                Remove((T)value!);
            }
        }

        object? IList.this[int index]
        {
            get
            {
                return this[index];
            }
            set
            {
                ValidateNullValue(value, nameof(value));

                try
                {
                    this[index] = (T)value!;
                }
                catch (InvalidCastException)
                {
                    throw Error.InvalidTypeException(value, typeof(T), nameof(value));
                }
            }
        }

        #endregion

        #region ICollection Members

        void ICollection.CopyTo(Array array, int index)
        {
            ArgumentNullException.ThrowIfNull(array);

            if (array.Rank != 1)
                throw new ArgumentException(nameof(array));

            Array.Copy(_items, 0, array, index, _size);
        }

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        #endregion

        /// <summary>
        /// Reverses the order of the elements in the entire <see cref="ReadOnlyCollectionBuilder{T}"/>.
        /// </summary>
        public void Reverse()
        {
            Reverse(0, Count);
        }

        /// <summary>
        /// Reverses the order of the elements in the specified range.
        /// </summary>
        /// <param name="index">The zero-based starting index of the range to reverse.</param>
        /// <param name="count">The number of elements in the range to reverse.</param>
        public void Reverse(int index, int count)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            Array.Reverse(_items, index, count);
            _version++;
        }

        /// <summary>
        /// Copies the elements of the <see cref="ReadOnlyCollectionBuilder{T}"/> to a new array.
        /// </summary>
        /// <returns>An array containing copies of the elements of the <see cref="ReadOnlyCollectionBuilder{T}"/>.</returns>
        public T[] ToArray()
        {
            T[] array = new T[_size];
            Array.Copy(_items, array, _size);
            return array;
        }

        /// <summary>
        /// Creates a <see cref="ReadOnlyCollection{T}"/> containing all of the elements of the <see cref="ReadOnlyCollectionBuilder{T}"/>,
        /// avoiding copying the elements to the new array if possible. Resets the <see cref="ReadOnlyCollectionBuilder{T}"/> after the
        /// <see cref="ReadOnlyCollection{T}"/> has been created.
        /// </summary>
        /// <returns>A new instance of <see cref="ReadOnlyCollection{T}"/>.</returns>
        public ReadOnlyCollection<T> ToReadOnlyCollection()
        {
            // Can we use the stored array?
            T[] items;
            if (_size == _items.Length)
            {
                items = _items;
            }
            else
            {
                items = ToArray();
            }
            _items = Array.Empty<T>();
            _size = 0;
            _version++;

            return new TrueReadOnlyCollection<T>(items);
        }

        private void EnsureCapacity(int min)
        {
            if (_items.Length < min)
            {
                int newCapacity = DefaultCapacity;
                if (_items.Length > 0)
                {
                    newCapacity = _items.Length * 2;
                }
                if (newCapacity < min)
                {
                    newCapacity = min;
                }
                Capacity = newCapacity;
            }
        }

        private static bool IsCompatibleObject(object? value)
        {
            return ((value is T) || (value == null && default(T) == null));
        }

        private static void ValidateNullValue(object? value, string argument)
        {
            if (value == null && default(T) != null)
            {
                throw Error.InvalidNullValue(typeof(T), argument);
            }
        }

        private sealed class Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly ReadOnlyCollectionBuilder<T> _builder;
            private readonly int _version;

            private int _index;
            private T _current;

            internal Enumerator(ReadOnlyCollectionBuilder<T> builder)
            {
                _builder = builder;
                _version = builder._version;
                _index = 0;
                _current = default(T)!;
            }

            #region IEnumerator<T> Members

            public T Current => _current;

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
            }

            #endregion

            #region IEnumerator Members

            object? IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || _index > _builder._size)
                    {
                        throw Error.EnumerationIsDone();
                    }
                    return _current;
                }
            }

            public bool MoveNext()
            {
                if (_version == _builder._version)
                {
                    if (_index < _builder._size)
                    {
                        _current = _builder._items[_index++];
                        return true;
                    }
                    else
                    {
                        _index = _builder._size + 1;
                        _current = default(T)!;
                        return false;
                    }
                }
                else
                {
                    throw Error.CollectionModifiedWhileEnumerating();
                }
            }

            #endregion

            #region IEnumerator Members

            void IEnumerator.Reset()
            {
                if (_version != _builder._version)
                {
                    throw Error.CollectionModifiedWhileEnumerating();
                }
                _index = 0;
                _current = default(T)!;
            }

            #endregion
        }
    }
}
