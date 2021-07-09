// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.Immutable
{
    public partial struct ImmutableArray<T>
    {
        /// <summary>
        /// A writable array accessor that can be converted into an <see cref="ImmutableArray{T}"/>
        /// instance without allocating memory.
        /// </summary>
        [DebuggerDisplay("Count = {Count}")]
        [DebuggerTypeProxy(typeof(ImmutableArrayBuilderDebuggerProxy<>))]
        public sealed class Builder : IList<T>, IReadOnlyList<T>
        {
            /// <summary>
            /// The backing array for the builder.
            /// </summary>
            private T[] _elements;

            /// <summary>
            /// The number of initialized elements in the array.
            /// </summary>
            private int _count;

            /// <summary>
            /// Initializes a new instance of the <see cref="Builder"/> class.
            /// </summary>
            /// <param name="capacity">The initial capacity of the internal array.</param>
            internal Builder(int capacity)
            {
                Requires.Range(capacity >= 0, nameof(capacity));
                _elements = new T[capacity];
                _count = 0;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Builder"/> class.
            /// </summary>
            internal Builder()
                : this(8)
            {
            }

            /// <summary>
            /// Get and sets the length of the internal array.  When set the internal array is
            /// reallocated to the given capacity if it is not already the specified length.
            /// </summary>
            public int Capacity
            {
                get { return _elements.Length; }
                set
                {
                    if (value < _count)
                    {
                        throw new ArgumentException(SR.CapacityMustBeGreaterThanOrEqualToCount, paramName: nameof(value));
                    }

                    if (value != _elements.Length)
                    {
                        if (value > 0)
                        {
                            var temp = new T[value];
                            if (_count > 0)
                            {
                                Array.Copy(_elements, temp, _count);
                            }

                            _elements = temp;
                        }
                        else
                        {
                            _elements = ImmutableArray<T>.Empty.array!;
                        }
                    }
                }
            }

            /// <summary>
            /// Gets or sets the length of the builder.
            /// </summary>
            /// <remarks>
            /// If the value is decreased, the array contents are truncated.
            /// If the value is increased, the added elements are initialized to the default value of type <typeparamref name="T"/>.
            /// </remarks>
            public int Count
            {
                get
                {
                    return _count;
                }

                set
                {
                    Requires.Range(value >= 0, nameof(value));
                    if (value < _count)
                    {
                        // truncation mode
                        // Clear the elements of the elements that are effectively removed.

                        // PERF: Array.Clear works well for big arrays,
                        //       but may have too much overhead with small ones (which is the common case here)
                        if (_count - value > 64)
                        {
                            Array.Clear(_elements, value, _count - value);
                        }
                        else
                        {
                            for (int i = value; i < this.Count; i++)
                            {
                                _elements[i] = default(T)!;
                            }
                        }
                    }
                    else if (value > _count)
                    {
                        // expansion
                        this.EnsureCapacity(value);
                    }

                    _count = value;
                }
            }

            private static void ThrowIndexOutOfRangeException() => throw new IndexOutOfRangeException();

            /// <summary>
            /// Gets or sets the element at the specified index.
            /// </summary>
            /// <param name="index">The index.</param>
            /// <returns></returns>
            /// <exception cref="IndexOutOfRangeException">
            /// </exception>
            public T this[int index]
            {
                get
                {
                    if (index >= this.Count)
                    {
                        ThrowIndexOutOfRangeException();
                    }

                    return _elements[index];
                }

                set
                {
                    if (index >= this.Count)
                    {
                        ThrowIndexOutOfRangeException();
                    }

                    _elements[index] = value;
                }
            }

            /// <summary>
            /// Gets a read-only reference to the element at the specified index.
            /// </summary>
            /// <param name="index">The index.</param>
            /// <returns></returns>
            /// <exception cref="IndexOutOfRangeException">
            /// </exception>
            public ref readonly T ItemRef(int index)
            {
                if (index >= this.Count)
                {
                    ThrowIndexOutOfRangeException();
                }

                return ref this._elements[index];
            }

            /// <summary>
            /// Gets a value indicating whether the <see cref="ICollection{T}"/> is read-only.
            /// </summary>
            /// <returns>true if the <see cref="ICollection{T}"/> is read-only; otherwise, false.
            ///   </returns>
            bool ICollection<T>.IsReadOnly
            {
                get { return false; }
            }

            /// <summary>
            /// Returns an immutable copy of the current contents of this collection.
            /// </summary>
            /// <returns>An immutable array.</returns>
            public ImmutableArray<T> ToImmutable()
            {
                return new ImmutableArray<T>(this.ToArray());
            }

            /// <summary>
            /// Extracts the internal array as an <see cref="ImmutableArray{T}"/> and replaces it
            /// with a zero length array.
            /// </summary>
            /// <exception cref="InvalidOperationException">When <see cref="ImmutableArray{T}.Builder.Count"/> doesn't
            /// equal <see cref="ImmutableArray{T}.Builder.Capacity"/>.</exception>
            public ImmutableArray<T> MoveToImmutable()
            {
                if (Capacity != Count)
                {
                    throw new InvalidOperationException(SR.CapacityMustEqualCountOnMove);
                }

                T[] temp = _elements;
                _elements = ImmutableArray<T>.Empty.array!;
                _count = 0;
                return new ImmutableArray<T>(temp);
            }

            /// <summary>
            /// Removes all items from the <see cref="ICollection{T}"/>.
            /// </summary>
            public void Clear()
            {
                this.Count = 0;
            }

            /// <summary>
            /// Inserts an item to the <see cref="IList{T}"/> at the specified index.
            /// </summary>
            /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
            /// <param name="item">The object to insert into the <see cref="IList{T}"/>.</param>
            public void Insert(int index, T item)
            {
                Requires.Range(index >= 0 && index <= this.Count, nameof(index));
                this.EnsureCapacity(this.Count + 1);

                if (index < this.Count)
                {
                    Array.Copy(_elements, index, _elements, index + 1, this.Count - index);
                }

                _count++;
                _elements[index] = item;
            }

            /// <summary>
            /// Adds an item to the <see cref="ICollection{T}"/>.
            /// </summary>
            /// <param name="item">The object to add to the <see cref="ICollection{T}"/>.</param>
            public void Add(T item)
            {
                int newCount = _count + 1;
                this.EnsureCapacity(newCount);
                _elements[_count] = item;
                _count = newCount;
            }

            /// <summary>
            /// Adds the specified items to the end of the array.
            /// </summary>
            /// <param name="items">The items.</param>
            public void AddRange(IEnumerable<T> items)
            {
                Requires.NotNull(items, nameof(items));

                int count;
                if (items.TryGetCount(out count))
                {
                    this.EnsureCapacity(this.Count + count);

                    if (items.TryCopyTo(_elements, _count))
                    {
                        _count += count;
                        return;
                    }
                }

                foreach (var item in items)
                {
                    this.Add(item);
                }
            }

            /// <summary>
            /// Adds the specified items to the end of the array.
            /// </summary>
            /// <param name="items">The items.</param>
            public void AddRange(params T[] items)
            {
                Requires.NotNull(items, nameof(items));

                var offset = this.Count;
                this.Count += items.Length;

                Array.Copy(items, 0, _elements, offset, items.Length);
            }

            /// <summary>
            /// Adds the specified items to the end of the array.
            /// </summary>
            /// <param name="items">The items.</param>
            public void AddRange<TDerived>(TDerived[] items) where TDerived : T
            {
                Requires.NotNull(items, nameof(items));

                var offset = this.Count;
                this.Count += items.Length;

                Array.Copy(items, 0, _elements, offset, items.Length);
            }

            /// <summary>
            /// Adds the specified items to the end of the array.
            /// </summary>
            /// <param name="items">The items.</param>
            /// <param name="length">The number of elements from the source array to add.</param>
            public void AddRange(T[] items, int length)
            {
                Requires.NotNull(items, nameof(items));
                Requires.Range(length >= 0 && length <= items.Length, nameof(length));

                var offset = this.Count;
                this.Count += length;

                Array.Copy(items, 0, _elements, offset, length);
            }

            /// <summary>
            /// Adds the specified items to the end of the array.
            /// </summary>
            /// <param name="items">The items.</param>
            public void AddRange(ImmutableArray<T> items)
            {
                this.AddRange(items, items.Length);
            }

            /// <summary>
            /// Adds the specified items to the end of the array.
            /// </summary>
            /// <param name="items">The items.</param>
            /// <param name="length">The number of elements from the source array to add.</param>
            public void AddRange(ImmutableArray<T> items, int length)
            {
                Requires.Range(length >= 0, nameof(length));

                if (items.array != null)
                {
                    this.AddRange(items.array, length);
                }
            }

            /// <summary>
            /// Adds the specified items to the end of the array.
            /// </summary>
            /// <param name="items">The items.</param>
            public void AddRange<TDerived>(ImmutableArray<TDerived> items) where TDerived : T
            {
                if (items.array != null)
                {
                    this.AddRange(items.array);
                }
            }

            /// <summary>
            /// Adds the specified items to the end of the array.
            /// </summary>
            /// <param name="items">The items.</param>
            public void AddRange(Builder items)
            {
                Requires.NotNull(items, nameof(items));
                this.AddRange(items._elements, items.Count);
            }

            /// <summary>
            /// Adds the specified items to the end of the array.
            /// </summary>
            /// <param name="items">The items.</param>
            public void AddRange<TDerived>(ImmutableArray<TDerived>.Builder items) where TDerived : T
            {
                Requires.NotNull(items, nameof(items));
                this.AddRange(items._elements, items.Count);
            }

            /// <summary>
            /// Removes the specified element.
            /// </summary>
            /// <param name="element">The element.</param>
            /// <returns>A value indicating whether the specified element was found and removed from the collection.</returns>
            public bool Remove(T element)
            {
                int index = this.IndexOf(element);
                if (index >= 0)
                {
                    this.RemoveAt(index);
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Removes the <see cref="IList{T}"/> item at the specified index.
            /// </summary>
            /// <param name="index">The zero-based index of the item to remove.</param>
            public void RemoveAt(int index)
            {
                Requires.Range(index >= 0 && index < this.Count, nameof(index));

                if (index < this.Count - 1)
                {
                    Array.Copy(_elements, index + 1, _elements, index, this.Count - index - 1);
                }

                this.Count--;
            }

            /// <summary>
            /// Determines whether the <see cref="ICollection{T}"/> contains a specific value.
            /// </summary>
            /// <param name="item">The object to locate in the <see cref="ICollection{T}"/>.</param>
            /// <returns>
            /// true if <paramref name="item"/> is found in the <see cref="ICollection{T}"/>; otherwise, false.
            /// </returns>
            public bool Contains(T item)
            {
                return this.IndexOf(item) >= 0;
            }

            /// <summary>
            /// Creates a new array with the current contents of this Builder.
            /// </summary>
            public T[] ToArray()
            {
                if (this.Count == 0)
                {
                    return Empty.array!;
                }

                T[] result = new T[this.Count];
                Array.Copy(_elements, result, this.Count);
                return result;
            }

            /// <summary>
            /// Copies the current contents to the specified array.
            /// </summary>
            /// <param name="array">The array to copy to.</param>
            /// <param name="index">The starting index of the target array.</param>
            public void CopyTo(T[] array, int index)
            {
                Requires.NotNull(array, nameof(array));
                Requires.Range(index >= 0 && index + this.Count <= array.Length, nameof(index));
                Array.Copy(_elements, 0, array, index, this.Count);
            }

            /// <summary>
            /// Resizes the array to accommodate the specified capacity requirement.
            /// </summary>
            /// <param name="capacity">The required capacity.</param>
            private void EnsureCapacity(int capacity)
            {
                if (_elements.Length < capacity)
                {
                    int newCapacity = Math.Max(_elements.Length * 2, capacity);
                    Array.Resize(ref _elements, newCapacity);
                }
            }

            /// <summary>
            /// Determines the index of a specific item in the <see cref="IList{T}"/>.
            /// </summary>
            /// <param name="item">The object to locate in the <see cref="IList{T}"/>.</param>
            /// <returns>
            /// The index of <paramref name="item"/> if found in the list; otherwise, -1.
            /// </returns>
            public int IndexOf(T item)
            {
                return this.IndexOf(item, 0, _count, EqualityComparer<T>.Default);
            }

            /// <summary>
            /// Searches the array for the specified item.
            /// </summary>
            /// <param name="item">The item to search for.</param>
            /// <param name="startIndex">The index at which to begin the search.</param>
            /// <returns>The 0-based index into the array where the item was found; or -1 if it could not be found.</returns>
            public int IndexOf(T item, int startIndex)
            {
                return this.IndexOf(item, startIndex, this.Count - startIndex, EqualityComparer<T>.Default);
            }

            /// <summary>
            /// Searches the array for the specified item.
            /// </summary>
            /// <param name="item">The item to search for.</param>
            /// <param name="startIndex">The index at which to begin the search.</param>
            /// <param name="count">The number of elements to search.</param>
            /// <returns>The 0-based index into the array where the item was found; or -1 if it could not be found.</returns>
            public int IndexOf(T item, int startIndex, int count)
            {
                return this.IndexOf(item, startIndex, count, EqualityComparer<T>.Default);
            }

            /// <summary>
            /// Searches the array for the specified item.
            /// </summary>
            /// <param name="item">The item to search for.</param>
            /// <param name="startIndex">The index at which to begin the search.</param>
            /// <param name="count">The number of elements to search.</param>
            /// <param name="equalityComparer">
            /// The equality comparer to use in the search.
            /// If <c>null</c>, <see cref="EqualityComparer{T}.Default"/> is used.
            /// </param>
            /// <returns>The 0-based index into the array where the item was found; or -1 if it could not be found.</returns>
            public int IndexOf(T item, int startIndex, int count, IEqualityComparer<T>? equalityComparer)
            {
                if (count == 0 && startIndex == 0)
                {
                    return -1;
                }

                Requires.Range(startIndex >= 0 && startIndex < this.Count, nameof(startIndex));
                Requires.Range(count >= 0 && startIndex + count <= this.Count, nameof(count));

                equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
                if (equalityComparer == EqualityComparer<T>.Default)
                {
                    return Array.IndexOf(_elements, item, startIndex, count);
                }
                else
                {
                    for (int i = startIndex; i < startIndex + count; i++)
                    {
                        if (equalityComparer.Equals(_elements[i], item))
                        {
                            return i;
                        }
                    }

                    return -1;
                }
            }

            /// <summary>
            /// Searches the array for the specified item in reverse.
            /// </summary>
            /// <param name="item">The item to search for.</param>
            /// <returns>The 0-based index into the array where the item was found; or -1 if it could not be found.</returns>
            public int LastIndexOf(T item)
            {
                if (this.Count == 0)
                {
                    return -1;
                }

                return this.LastIndexOf(item, this.Count - 1, this.Count, EqualityComparer<T>.Default);
            }

            /// <summary>
            /// Searches the array for the specified item in reverse.
            /// </summary>
            /// <param name="item">The item to search for.</param>
            /// <param name="startIndex">The index at which to begin the search.</param>
            /// <returns>The 0-based index into the array where the item was found; or -1 if it could not be found.</returns>
            public int LastIndexOf(T item, int startIndex)
            {
                if (this.Count == 0 && startIndex == 0)
                {
                    return -1;
                }

                Requires.Range(startIndex >= 0 && startIndex < this.Count, nameof(startIndex));

                return this.LastIndexOf(item, startIndex, startIndex + 1, EqualityComparer<T>.Default);
            }

            /// <summary>
            /// Searches the array for the specified item in reverse.
            /// </summary>
            /// <param name="item">The item to search for.</param>
            /// <param name="startIndex">The index at which to begin the search.</param>
            /// <param name="count">The number of elements to search.</param>
            /// <returns>The 0-based index into the array where the item was found; or -1 if it could not be found.</returns>
            public int LastIndexOf(T item, int startIndex, int count)
            {
                return this.LastIndexOf(item, startIndex, count, EqualityComparer<T>.Default);
            }

            /// <summary>
            /// Searches the array for the specified item in reverse.
            /// </summary>
            /// <param name="item">The item to search for.</param>
            /// <param name="startIndex">The index at which to begin the search.</param>
            /// <param name="count">The number of elements to search.</param>
            /// <param name="equalityComparer">The equality comparer to use in the search.</param>
            /// <returns>The 0-based index into the array where the item was found; or -1 if it could not be found.</returns>
            public int LastIndexOf(T item, int startIndex, int count, IEqualityComparer<T>? equalityComparer)
            {
                if (count == 0 && startIndex == 0)
                {
                    return -1;
                }

                Requires.Range(startIndex >= 0 && startIndex < this.Count, nameof(startIndex));
                Requires.Range(count >= 0 && startIndex - count + 1 >= 0, nameof(count));

                equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
                if (equalityComparer == EqualityComparer<T>.Default)
                {
                    return Array.LastIndexOf(_elements, item, startIndex, count);
                }
                else
                {
                    for (int i = startIndex; i >= startIndex - count + 1; i--)
                    {
                        if (equalityComparer.Equals(item, _elements[i]))
                        {
                            return i;
                        }
                    }

                    return -1;
                }
            }

            /// <summary>
            /// Reverses the order of elements in the collection.
            /// </summary>
            public void Reverse()
            {
                // The non-generic Array.Reverse is not used because it does not perform
                // well for non-primitive value types.
                // If/when a generic Array.Reverse<T> becomes available, the below code
                // can be deleted and replaced with a call to Array.Reverse<T>.
                int i = 0;
                int j = _count - 1;
                T[] array = _elements;
                while (i < j)
                {
                    T temp = array[i];
                    array[i] = array[j];
                    array[j] = temp;
                    i++;
                    j--;
                }
            }

            /// <summary>
            /// Sorts the array.
            /// </summary>
            public void Sort()
            {
                if (Count > 1)
                {
                    Array.Sort(_elements, 0, this.Count, Comparer<T>.Default);
                }
            }

            /// <summary>
            /// Sorts the elements in the entire array using
            /// the specified <see cref="Comparison{T}"/>.
            /// </summary>
            /// <param name="comparison">
            /// The <see cref="Comparison{T}"/> to use when comparing elements.
            /// </param>
            /// <exception cref="ArgumentNullException"><paramref name="comparison"/> is null.</exception>
            public void Sort(Comparison<T> comparison)
            {
                Requires.NotNull(comparison, nameof(comparison));

                if (Count > 1)
                {
                    // Array.Sort does not have an overload that takes both bounds and a Comparison.
                    // We could special case _count == _elements.Length in order to try to avoid
                    // the IComparer allocation, but the Array.Sort overload that takes a Comparison
                    // allocates such an IComparer internally, anyway.
                    Array.Sort(_elements, 0, _count, Comparer<T>.Create(comparison));
                }
            }

            /// <summary>
            /// Sorts the array.
            /// </summary>
            /// <param name="comparer">The comparer to use in sorting. If <c>null</c>, the default comparer is used.</param>
            public void Sort(IComparer<T>? comparer)
            {
                if (Count > 1)
                {
                    Array.Sort(_elements, 0, _count, comparer);
                }
            }

            /// <summary>
            /// Sorts the array.
            /// </summary>
            /// <param name="index">The index of the first element to consider in the sort.</param>
            /// <param name="count">The number of elements to include in the sort.</param>
            /// <param name="comparer">The comparer to use in sorting. If <c>null</c>, the default comparer is used.</param>
            public void Sort(int index, int count, IComparer<T>? comparer)
            {
                // Don't rely on Array.Sort's argument validation since our internal array may exceed
                // the bounds of the publicly addressable region.
                Requires.Range(index >= 0, nameof(index));
                Requires.Range(count >= 0 && index + count <= this.Count, nameof(count));

                if (count > 1)
                {
                    Array.Sort(_elements, index, count, comparer);
                }
            }

            /// <summary>
            /// Returns an enumerator for the contents of the array.
            /// </summary>
            /// <returns>An enumerator.</returns>
            public IEnumerator<T> GetEnumerator()
            {
                for (int i = 0; i < this.Count; i++)
                {
                    yield return this[i];
                }
            }

            /// <summary>
            /// Returns an enumerator for the contents of the array.
            /// </summary>
            /// <returns>An enumerator.</returns>
            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            /// <summary>
            /// Returns an enumerator for the contents of the array.
            /// </summary>
            /// <returns>An enumerator.</returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            /// <summary>
            /// Adds items to this collection.
            /// </summary>
            /// <typeparam name="TDerived">The type of source elements.</typeparam>
            /// <param name="items">The source array.</param>
            /// <param name="length">The number of elements to add to this array.</param>
            private void AddRange<TDerived>(TDerived[] items, int length) where TDerived : T
            {
                this.EnsureCapacity(this.Count + length);

                var offset = this.Count;
                this.Count += length;

                var nodes = _elements;
                for (int i = 0; i < length; i++)
                {
                    nodes[offset + i] = items[i];
                }
            }
        }
    }
}
