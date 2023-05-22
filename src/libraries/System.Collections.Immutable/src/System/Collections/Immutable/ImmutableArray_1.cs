// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;

namespace System.Collections.Immutable
{
    public readonly partial struct ImmutableArray<T> : IReadOnlyList<T>, IList<T>, IEquatable<ImmutableArray<T>>, IList, IImmutableArray, IStructuralComparable, IStructuralEquatable, IImmutableList<T>
    {
        /// <summary>
        /// Gets or sets the element at the specified index in the read-only list.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <returns>The element at the specified index in the read-only list.</returns>
        /// <exception cref="NotSupportedException">Always thrown from the setter.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the <see cref="IsDefault"/> property returns true.</exception>
        T IList<T>.this[int index]
        {
            get
            {
                ImmutableArray<T> self = this;
                self.ThrowInvalidOperationIfNotInitialized();
                return self[index];
            }
            set { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is read only.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is read only; otherwise, <c>false</c>.
        /// </value>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        bool ICollection<T>.IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        /// Gets the number of array in the collection.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the <see cref="IsDefault"/> property returns true.</exception>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        int ICollection<T>.Count
        {
            get
            {
                ImmutableArray<T> self = this;
                self.ThrowInvalidOperationIfNotInitialized();
                return self.Length;
            }
        }

        /// <summary>
        /// Gets the number of array in the collection.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the <see cref="IsDefault"/> property returns true.</exception>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        int IReadOnlyCollection<T>.Count
        {
            get
            {
                ImmutableArray<T> self = this;
                self.ThrowInvalidOperationIfNotInitialized();
                return self.Length;
            }
        }

        /// <summary>
        /// Gets the element at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>
        /// The element.
        /// </returns>
        /// <exception cref="InvalidOperationException">Thrown if the <see cref="IsDefault"/> property returns true.</exception>
        T IReadOnlyList<T>.this[int index]
        {
            get
            {
                ImmutableArray<T> self = this;
                self.ThrowInvalidOperationIfNotInitialized();
                return self[index];
            }
        }

        public ReadOnlySpan<T> AsSpan() => new ReadOnlySpan<T>(array);

        public ReadOnlyMemory<T> AsMemory() => new ReadOnlyMemory<T>(array);

        /// <summary>
        /// Searches the array for the specified item.
        /// </summary>
        /// <param name="item">The item to search for.</param>
        /// <returns>The 0-based index into the array where the item was found; or -1 if it could not be found.</returns>
        public int IndexOf(T item)
        {
            ImmutableArray<T> self = this;
            return self.IndexOf(item, 0, self.Length, EqualityComparer<T>.Default);
        }

        /// <summary>
        /// Searches the array for the specified item.
        /// </summary>
        /// <param name="item">The item to search for.</param>
        /// <param name="startIndex">The index at which to begin the search.</param>
        /// <param name="equalityComparer">The equality comparer to use in the search.</param>
        /// <returns>The 0-based index into the array where the item was found; or -1 if it could not be found.</returns>
        public int IndexOf(T item, int startIndex, IEqualityComparer<T>? equalityComparer)
        {
            ImmutableArray<T> self = this;
            return self.IndexOf(item, startIndex, self.Length - startIndex, equalityComparer);
        }

        /// <summary>
        /// Searches the array for the specified item.
        /// </summary>
        /// <param name="item">The item to search for.</param>
        /// <param name="startIndex">The index at which to begin the search.</param>
        /// <returns>The 0-based index into the array where the item was found; or -1 if it could not be found.</returns>
        public int IndexOf(T item, int startIndex)
        {
            ImmutableArray<T> self = this;
            return self.IndexOf(item, startIndex, self.Length - startIndex, EqualityComparer<T>.Default);
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
            ImmutableArray<T> self = this;
            self.ThrowNullRefIfNotInitialized();

            if (count == 0 && startIndex == 0)
            {
                return -1;
            }

            Requires.Range(startIndex >= 0 && startIndex < self.Length, nameof(startIndex));
            Requires.Range(count >= 0 && startIndex + count <= self.Length, nameof(count));

            equalityComparer ??= EqualityComparer<T>.Default;
            if (equalityComparer == EqualityComparer<T>.Default)
            {
                return Array.IndexOf(self.array!, item, startIndex, count);
            }
            else
            {
                for (int i = startIndex; i < startIndex + count; i++)
                {
                    if (equalityComparer.Equals(self.array![i], item))
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
            ImmutableArray<T> self = this;
            if (self.IsEmpty)
            {
                return -1;
            }

            return self.LastIndexOf(item, self.Length - 1, self.Length, EqualityComparer<T>.Default);
        }

        /// <summary>
        /// Searches the array for the specified item in reverse.
        /// </summary>
        /// <param name="item">The item to search for.</param>
        /// <param name="startIndex">The index at which to begin the search.</param>
        /// <returns>The 0-based index into the array where the item was found; or -1 if it could not be found.</returns>
        public int LastIndexOf(T item, int startIndex)
        {
            ImmutableArray<T> self = this;
            if (self.IsEmpty && startIndex == 0)
            {
                return -1;
            }

            return self.LastIndexOf(item, startIndex, startIndex + 1, EqualityComparer<T>.Default);
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
            ImmutableArray<T> self = this;
            self.ThrowNullRefIfNotInitialized();

            if (startIndex == 0 && count == 0)
            {
                return -1;
            }

            Requires.Range(startIndex >= 0 && startIndex < self.Length, nameof(startIndex));
            Requires.Range(count >= 0 && startIndex - count + 1 >= 0, nameof(count));

            equalityComparer ??= EqualityComparer<T>.Default;
            if (equalityComparer == EqualityComparer<T>.Default)
            {
                return Array.LastIndexOf(self.array!, item, startIndex, count);
            }
            else
            {
                for (int i = startIndex; i >= startIndex - count + 1; i--)
                {
                    if (equalityComparer.Equals(item, self.array![i]))
                    {
                        return i;
                    }
                }

                return -1;
            }
        }

        /// <summary>
        /// Determines whether the specified item exists in the array.
        /// </summary>
        /// <param name="item">The item to search for.</param>
        /// <returns><c>true</c> if an equal value was found in the array; <c>false</c> otherwise.</returns>
        public bool Contains(T item)
        {
            return this.IndexOf(item) >= 0;
        }

        /// <summary>
        /// Determines whether the specified item exists in the array.
        /// </summary>
        /// <param name="item">The item to search for.</param>
        /// <param name="equalityComparer">
        /// The equality comparer to use in the search.
        /// If <c>null</c>, <see cref="EqualityComparer{T}.Default"/> is used.
        /// </param>
        /// <returns><c>true</c> if an equal value was found in the array; <c>false</c> otherwise.</returns>
        public bool Contains(T item, IEqualityComparer<T>? equalityComparer)
        {
            return this.IndexOf(item, equalityComparer) >= 0;
        }

        /// <summary>
        /// Returns a new array with the specified value inserted at the specified position.
        /// </summary>
        /// <param name="index">The 0-based index into the array at which the new item should be added.</param>
        /// <param name="item">The item to insert at the start of the array.</param>
        /// <returns>A new array.</returns>
        public ImmutableArray<T> Insert(int index, T item)
        {
            ImmutableArray<T> self = this;
            self.ThrowNullRefIfNotInitialized();
            Requires.Range(index >= 0 && index <= self.Length, nameof(index));

            if (self.IsEmpty)
            {
                return ImmutableArray.Create(item);
            }

            T[] tmp = new T[self.Length + 1];
            tmp[index] = item;

            if (index != 0)
            {
                Array.Copy(self.array!, tmp, index);
            }
            if (index != self.Length)
            {
                Array.Copy(self.array!, index, tmp, index + 1, self.Length - index);
            }

            return new ImmutableArray<T>(tmp);
        }

        /// <summary>
        /// Inserts the specified values at the specified index.
        /// </summary>
        /// <param name="index">The index at which to insert the value.</param>
        /// <param name="items">The elements to insert.</param>
        /// <returns>The new immutable collection.</returns>
        public ImmutableArray<T> InsertRange(int index, IEnumerable<T> items)
        {
            ImmutableArray<T> self = this;
            self.ThrowNullRefIfNotInitialized();
            Requires.Range(index >= 0 && index <= self.Length, nameof(index));
            Requires.NotNull(items, nameof(items));

            if (self.IsEmpty)
            {
                return ImmutableArray.CreateRange(items);
            }

            int count = ImmutableExtensions.GetCount(ref items);
            if (count == 0)
            {
                return self;
            }

            T[] tmp = new T[self.Length + count];

            if (index != 0)
            {
                Array.Copy(self.array!, tmp, index);
            }
            if (index != self.Length)
            {
                Array.Copy(self.array!, index, tmp, index + count, self.Length - index);
            }

            // We want to copy over the items we need to insert.
            // Check first to see if items is a well-known collection we can call CopyTo
            // on to the array, which is an order of magnitude faster than foreach.
            // Otherwise, go to the fallback route where we manually enumerate the sequence
            // and place the items in the array one-by-one.

            if (!items.TryCopyTo(tmp, index))
            {
                int sequenceIndex = index;
                foreach (T item in items)
                {
                    tmp[sequenceIndex++] = item;
                }
            }

            return new ImmutableArray<T>(tmp);
        }

        /// <summary>
        /// Inserts the specified values at the specified index.
        /// </summary>
        /// <param name="index">The index at which to insert the value.</param>
        /// <param name="items">The elements to insert.</param>
        /// <returns>The new immutable collection.</returns>
        public ImmutableArray<T> InsertRange(int index, ImmutableArray<T> items)
        {
            ImmutableArray<T> self = this;
            self.ThrowNullRefIfNotInitialized();
            items.ThrowNullRefIfNotInitialized();
            Requires.Range(index >= 0 && index <= self.Length, nameof(index));

            if (self.IsEmpty)
            {
                return items;
            }
            if (items.IsEmpty)
            {
                return self;
            }

            return self.InsertSpanRangeInternal(index, items.AsSpan());
        }

        /// <summary>
        /// Returns a new array with the specified value inserted at the end.
        /// </summary>
        /// <param name="item">The item to insert at the end of the array.</param>
        /// <returns>A new array.</returns>
        public ImmutableArray<T> Add(T item)
        {
            ImmutableArray<T> self = this;
            if (self.IsEmpty)
            {
                return ImmutableArray.Create(item);
            }

            return self.Insert(self.Length, item);
        }

        /// <summary>
        /// Adds the specified values to this list.
        /// </summary>
        /// <param name="items">The values to add.</param>
        /// <returns>A new list with the elements added.</returns>
        public ImmutableArray<T> AddRange(IEnumerable<T> items)
        {
            ImmutableArray<T> self = this;
            return self.InsertRange(self.Length, items);
        }

        /// <summary>
        /// Adds the specified items to the end of the array.
        /// </summary>
        /// <param name="items">The values to add.</param>
        /// <param name="length">The number of elements from the source array to add.</param>
        /// <returns>A new list with the elements added.</returns>
        public ImmutableArray<T> AddRange(T[] items, int length)
        {
            ImmutableArray<T> self = this;
            self.ThrowNullRefIfNotInitialized();
            Requires.NotNull(items, nameof(items));
            Requires.Range(length >= 0 && length <= items.Length, nameof(length));

            if (items.Length == 0 || length == 0)
            {
                return self;
            }
            else if (self.IsEmpty)
            {
                return ImmutableArray.Create(items, 0, length);
            }

            T[] tmp = new T[self.Length + length];
            Array.Copy(self.array!, tmp, self.Length);
            Array.Copy(items, 0, tmp, self.Length, length);

            return new ImmutableArray<T>(tmp);
        }

        /// <summary>
        /// Adds the specified items to the end of the array.
        /// </summary>
        /// <typeparam name="TDerived">The type that derives from the type of item already in the array.</typeparam>
        /// <param name="items">The values to add.</param>
        /// <returns>A new list with the elements added.</returns>
        public ImmutableArray<T> AddRange<TDerived>(TDerived[] items) where TDerived : T
        {
            ImmutableArray<T> self = this;
            self.ThrowNullRefIfNotInitialized();
            Requires.NotNull(items, nameof(items));

            if (items.Length == 0)
            {
                return self;
            }

            T[] tmp = new T[self.Length + items.Length];
            Array.Copy(self.array!, tmp, self.Length);
            Array.Copy(items, 0, tmp, self.Length, items.Length);

            return new ImmutableArray<T>(tmp);
        }

        /// <summary>
        /// Adds the specified items to the end of the array.
        /// </summary>
        /// <param name="items">The values to add.</param>
        /// <param name="length">The number of elements from the source array to add.</param>
        /// <returns>A new list with the elements added.</returns>
        public ImmutableArray<T> AddRange(ImmutableArray<T> items, int length)
        {
            ImmutableArray<T> self = this;
            Requires.Range(length >= 0, nameof(length));

            if (items.array != null)
            {
                return self.AddRange(items.array, length);
            }
            else
            {
                return self;
            }
        }

        /// <summary>
        /// Adds the specified items to the end of the array.
        /// </summary>
        /// <typeparam name="TDerived">The type that derives from the type of item already in the array.</typeparam>
        /// <param name="items">The values to add.</param>
        /// <returns>A new list with the elements added.</returns>
        public ImmutableArray<T> AddRange<TDerived>(ImmutableArray<TDerived> items) where TDerived : T
        {
            ImmutableArray<T> self = this;
            if (items.array != null)
            {
                return self.AddRange(items.array);
            }
            else
            {
                return self;
            }
        }

        /// <summary>
        /// Adds the specified values to this list.
        /// </summary>
        /// <param name="items">The values to add.</param>
        /// <returns>A new list with the elements added.</returns>
        public ImmutableArray<T> AddRange(ImmutableArray<T> items)
        {
            ImmutableArray<T> self = this;
            return self.InsertRange(self.Length, items);
        }

        /// <summary>
        /// Returns an array with the item at the specified position replaced.
        /// </summary>
        /// <param name="index">The index of the item to replace.</param>
        /// <param name="item">The new item.</param>
        /// <returns>The new array.</returns>
        public ImmutableArray<T> SetItem(int index, T item)
        {
            ImmutableArray<T> self = this;
            self.ThrowNullRefIfNotInitialized();
            Requires.Range(index >= 0 && index < self.Length, nameof(index));

            T[] tmp = new T[self.Length];
            Array.Copy(self.array!, tmp, self.Length);
            tmp[index] = item;
            return new ImmutableArray<T>(tmp);
        }

        /// <summary>
        /// Replaces the first equal element in the list with the specified element.
        /// </summary>
        /// <param name="oldValue">The element to replace.</param>
        /// <param name="newValue">The element to replace the old element with.</param>
        /// <returns>The new list -- even if the value being replaced is equal to the new value for that position.</returns>
        /// <exception cref="ArgumentException">Thrown when the old value does not exist in the list.</exception>
        public ImmutableArray<T> Replace(T oldValue, T newValue)
        {
            return this.Replace(oldValue, newValue, EqualityComparer<T>.Default);
        }

        /// <summary>
        /// Replaces the first equal element in the list with the specified element.
        /// </summary>
        /// <param name="oldValue">The element to replace.</param>
        /// <param name="newValue">The element to replace the old element with.</param>
        /// <param name="equalityComparer">
        /// The equality comparer to use in the search.
        /// If <c>null</c>, <see cref="EqualityComparer{T}.Default"/> is used.
        /// </param>
        /// <returns>The new list -- even if the value being replaced is equal to the new value for that position.</returns>
        /// <exception cref="ArgumentException">Thrown when the old value does not exist in the list.</exception>
        public ImmutableArray<T> Replace(T oldValue, T newValue, IEqualityComparer<T>? equalityComparer)
        {
            ImmutableArray<T> self = this;
            int index = self.IndexOf(oldValue, 0, self.Length, equalityComparer);
            if (index < 0)
            {
                throw new ArgumentException(SR.CannotFindOldValue, nameof(oldValue));
            }

            return self.SetItem(index, newValue);
        }

        /// <summary>
        /// Returns an array with the first occurrence of the specified element removed from the array.
        /// If no match is found, the current array is returned.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <returns>The new array.</returns>
        public ImmutableArray<T> Remove(T item)
        {
            return this.Remove(item, EqualityComparer<T>.Default);
        }

        /// <summary>
        /// Returns an array with the first occurrence of the specified element removed from the array.
        /// If no match is found, the current array is returned.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <param name="equalityComparer">
        /// The equality comparer to use in the search.
        /// If <c>null</c>, <see cref="EqualityComparer{T}.Default"/> is used.
        /// </param>
        /// <returns>The new array.</returns>
        public ImmutableArray<T> Remove(T item, IEqualityComparer<T>? equalityComparer)
        {
            ImmutableArray<T> self = this;
            self.ThrowNullRefIfNotInitialized();
            int index = self.IndexOf(item, 0, self.Length, equalityComparer);
            return index < 0
                ? self
                : self.RemoveAt(index);
        }

        /// <summary>
        /// Returns an array with the element at the specified position removed.
        /// </summary>
        /// <param name="index">The 0-based index into the array for the element to omit from the returned array.</param>
        /// <returns>The new array.</returns>
        public ImmutableArray<T> RemoveAt(int index)
        {
            return this.RemoveRange(index, 1);
        }

        /// <summary>
        /// Returns an array with the elements at the specified position removed.
        /// </summary>
        /// <param name="index">The 0-based index into the array for the element to omit from the returned array.</param>
        /// <param name="length">The number of elements to remove.</param>
        /// <returns>The new array.</returns>
        public ImmutableArray<T> RemoveRange(int index, int length)
        {
            ImmutableArray<T> self = this;
            self.ThrowNullRefIfNotInitialized();
            Requires.Range(index >= 0 && index <= self.Length, nameof(index));
            Requires.Range(length >= 0 && index + length <= self.Length, nameof(length));

            if (length == 0)
            {
                return self;
            }

            T[] tmp = new T[self.Length - length];
            Array.Copy(self.array!, tmp, index);
            Array.Copy(self.array!, index + length, tmp, index, self.Length - index - length);
            return new ImmutableArray<T>(tmp);
        }

        /// <summary>
        /// Removes the specified values from this list.
        /// </summary>
        /// <param name="items">The items to remove if matches are found in this list.</param>
        /// <returns>
        /// A new list with the elements removed.
        /// </returns>
        public ImmutableArray<T> RemoveRange(IEnumerable<T> items)
        {
            return this.RemoveRange(items, EqualityComparer<T>.Default);
        }

        /// <summary>
        /// Removes the specified values from this list.
        /// </summary>
        /// <param name="items">The items to remove if matches are found in this list.</param>
        /// <param name="equalityComparer">
        /// The equality comparer to use in the search.
        /// If <c>null</c>, <see cref="EqualityComparer{T}.Default"/> is used.
        /// </param>
        /// <returns>
        /// A new list with the elements removed.
        /// </returns>
        public ImmutableArray<T> RemoveRange(IEnumerable<T> items, IEqualityComparer<T>? equalityComparer)
        {
            ImmutableArray<T> self = this;
            self.ThrowNullRefIfNotInitialized();
            Requires.NotNull(items, nameof(items));

            var indicesToRemove = new SortedSet<int>();
            foreach (T item in items)
            {
                int index = -1;
                do
                {
                    index = self.IndexOf(item, index + 1, equalityComparer);
                } while (index >= 0 && !indicesToRemove.Add(index) && index < self.Length - 1);
            }

            return self.RemoveAtRange(indicesToRemove);
        }

        /// <summary>
        /// Removes the specified values from this list.
        /// </summary>
        /// <param name="items">The items to remove if matches are found in this list.</param>
        /// <returns>
        /// A new list with the elements removed.
        /// </returns>
        public ImmutableArray<T> RemoveRange(ImmutableArray<T> items)
        {
            return this.RemoveRange(items, EqualityComparer<T>.Default);
        }

        /// <summary>
        /// Removes the specified values from this list.
        /// </summary>
        /// <param name="items">The items to remove if matches are found in this list.</param>
        /// <param name="equalityComparer">
        /// The equality comparer to use in the search.
        /// </param>
        /// <returns>
        /// A new list with the elements removed.
        /// </returns>
        public ImmutableArray<T> RemoveRange(ImmutableArray<T> items, IEqualityComparer<T>? equalityComparer)
        {
            Requires.NotNull(items.array!, nameof(items));

            return RemoveRange(items.AsSpan(), equalityComparer);
        }

        /// <summary>
        /// Removes all the elements that match the conditions defined by the specified
        /// predicate.
        /// </summary>
        /// <param name="match">
        /// The <see cref="Predicate{T}"/> delegate that defines the conditions of the elements
        /// to remove.
        /// </param>
        /// <returns>
        /// The new list.
        /// </returns>
        public ImmutableArray<T> RemoveAll(Predicate<T> match)
        {
            ImmutableArray<T> self = this;
            self.ThrowNullRefIfNotInitialized();
            Requires.NotNull(match, nameof(match));

            if (self.IsEmpty)
            {
                return self;
            }

            List<int>? removeIndices = null;
            for (int i = 0; i < self.array!.Length; i++)
            {
                if (match(self.array[i]))
                {
                    removeIndices ??= new List<int>();

                    removeIndices.Add(i);
                }
            }

            return removeIndices != null ?
                self.RemoveAtRange(removeIndices) :
                self;
        }

        /// <summary>
        /// Returns an empty array.
        /// </summary>
        public ImmutableArray<T> Clear()
        {
            return Empty;
        }

        /// <summary>
        /// Returns a sorted instance of this array.
        /// </summary>
        public ImmutableArray<T> Sort()
        {
            ImmutableArray<T> self = this;
            return self.Sort(0, self.Length, Comparer<T>.Default);
        }

        /// <summary>
        /// Sorts the elements in the entire <see cref="ImmutableArray{T}"/> using
        /// the specified <see cref="Comparison{T}"/>.
        /// </summary>
        /// <param name="comparison">
        /// The <see cref="Comparison{T}"/> to use when comparing elements.
        /// </param>
        /// <returns>The sorted list.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparison"/> is null.</exception>
        public ImmutableArray<T> Sort(Comparison<T> comparison)
        {
            Requires.NotNull(comparison, nameof(comparison));

            ImmutableArray<T> self = this;
            return self.Sort(Comparer<T>.Create(comparison));
        }

        /// <summary>
        /// Returns a sorted instance of this array.
        /// </summary>
        /// <param name="comparer">The comparer to use in sorting. If <c>null</c>, the default comparer is used.</param>
        public ImmutableArray<T> Sort(IComparer<T>? comparer)
        {
            ImmutableArray<T> self = this;
            return self.Sort(0, self.Length, comparer);
        }

        /// <summary>
        /// Returns a sorted instance of this array.
        /// </summary>
        /// <param name="index">The index of the first element to consider in the sort.</param>
        /// <param name="count">The number of elements to include in the sort.</param>
        /// <param name="comparer">The comparer to use in sorting. If <c>null</c>, the default comparer is used.</param>
        public ImmutableArray<T> Sort(int index, int count, IComparer<T>? comparer)
        {
            ImmutableArray<T> self = this;
            self.ThrowNullRefIfNotInitialized();
            Requires.Range(index >= 0, nameof(index));
            Requires.Range(count >= 0 && index + count <= self.Length, nameof(count));

            // 0 and 1 element arrays don't need to be sorted.
            if (count > 1)
            {
                comparer ??= Comparer<T>.Default;

                // Avoid copying the entire array when the array is already sorted.
                bool outOfOrder = false;
                for (int i = index + 1; i < index + count; i++)
                {
                    if (comparer.Compare(self.array![i - 1], self.array[i]) > 0)
                    {
                        outOfOrder = true;
                        break;
                    }
                }

                if (outOfOrder)
                {
                    var tmp = new T[self.Length];
                    Array.Copy(self.array!, tmp, self.Length);
                    Array.Sort(tmp, index, count, comparer);
                    return new ImmutableArray<T>(tmp);
                }
            }

            return self;
        }
        /// <summary>
        /// Filters the elements of this array to those assignable to the specified type.
        /// </summary>
        /// <typeparam name="TResult">The type to filter the elements of the sequence on.</typeparam>
        /// <returns>
        /// An <see cref="IEnumerable{T}"/> that contains elements from
        /// the input sequence of type <typeparamref name="TResult"/>.
        /// </returns>
        public IEnumerable<TResult> OfType<TResult>()
        {
            ImmutableArray<T> self = this;
            if (self.array == null || self.array.Length == 0)
            {
                return Enumerable.Empty<TResult>();
            }

            return self.array.OfType<TResult>();
        }

        /// <summary>
        /// Adds the specified values to this list.
        /// </summary>
        /// <param name="items">The values to add.</param>
        /// <returns>A new list with the elements added.</returns>
        public ImmutableArray<T> AddRange(ReadOnlySpan<T> items)
        {
            ImmutableArray<T> self = this;
            return self.InsertRange(self.Length, items);
        }

        /// <summary>
        /// Adds the specified values to this list.
        /// </summary>
        /// <param name="items">The values to add.</param>
        /// <returns>A new list with the elements added.</returns>
        public ImmutableArray<T> AddRange(params T[] items)
        {
            ImmutableArray<T> self = this;
            return self.InsertRange(self.Length, items);
        }

        /// <summary>
        /// Creates a <see cref="ReadOnlySpan{T}"/> over the portion of current <see cref="ImmutableArray{T}"/> beginning at a specified position for a specified length.
        /// </summary>
        /// <param name="start">The index at which to begin the span.</param>
        /// <param name="length">The number of items in the span.</param>
        /// <returns>The <see cref="ReadOnlySpan{T}"/> representation of the <see cref="ImmutableArray{T}"/></returns>
        public ReadOnlySpan<T> AsSpan(int start, int length) => new ReadOnlySpan<T>(array, start, length);

        /// <summary>
        /// Copies the elements of current <see cref="ImmutableArray{T}"/> to an <see cref="Span{T}"/>.
        /// </summary>
        /// <param name="destination">The <see cref="Span{T}"/> that is the destination of the elements copied from current <see cref="ImmutableArray{T}"/>.</param>
        public void CopyTo(Span<T> destination)
        {
            ImmutableArray<T> self = this;
            self.ThrowNullRefIfNotInitialized();
            Requires.Range(self.Length <= destination.Length, nameof(destination));

            self.AsSpan().CopyTo(destination);
        }

        /// <summary>
        /// Inserts the specified values at the specified index.
        /// </summary>
        /// <param name="index">The index at which to insert the value.</param>
        /// <param name="items">The elements to insert.</param>
        /// <returns>The new immutable collection.</returns>
        public ImmutableArray<T> InsertRange(int index, T[] items)
        {
            ImmutableArray<T> self = this;
            self.ThrowNullRefIfNotInitialized();
            Requires.Range(index >= 0 && index <= self.Length, nameof(index));
            Requires.NotNull(items, nameof(items));

            if (items.Length == 0)
            {
                return self;
            }
            if (self.IsEmpty)
            {
                return new ImmutableArray<T>(items);
            }

            return self.InsertSpanRangeInternal(index, items);
        }

        /// <summary>
        /// Inserts the specified values at the specified index.
        /// </summary>
        /// <param name="index">The index at which to insert the value.</param>
        /// <param name="items">The elements to insert.</param>
        /// <returns>The new immutable collection.</returns>
        public ImmutableArray<T> InsertRange(int index, ReadOnlySpan<T> items)
        {
            ImmutableArray<T> self = this;
            self.ThrowNullRefIfNotInitialized();
            Requires.Range(index >= 0 && index <= self.Length, nameof(index));

            if (items.IsEmpty)
            {
                return self;
            }
            if (self.IsEmpty)
            {
                return items.ToImmutableArray();
            }

            return self.InsertSpanRangeInternal(index, items);
        }

        /// <summary>
        /// Removes the specified values from this list.
        /// </summary>
        /// <param name="items">The items to remove if matches are found in this list.</param>
        /// <param name="equalityComparer">
        /// The equality comparer to use in the search.
        /// </param>
        /// <returns>
        /// A new list with the elements removed.
        /// </returns>
        public ImmutableArray<T> RemoveRange(ReadOnlySpan<T> items, IEqualityComparer<T>? equalityComparer = null)
        {
            ImmutableArray<T> self = this;
            self.ThrowNullRefIfNotInitialized();

            if (items.IsEmpty || self.IsEmpty)
            {
                return self;
            }

            if (items.Length == 1)
            {
                return self.Remove(items[0], equalityComparer);
            }

            var indicesToRemove = new SortedSet<int>();
            foreach (T item in items)
            {
                int index = -1;
                do
                {
                    index = self.IndexOf(item, index + 1, equalityComparer);
                } while (index >= 0 && !indicesToRemove.Add(index) && index < self.Length - 1);
            }

            return self.RemoveAtRange(indicesToRemove);
        }

        /// <summary>
        /// Removes the specified values from this list.
        /// </summary>
        /// <param name="items">The items to remove if matches are found in this list.</param>
        /// <param name="equalityComparer">
        /// The equality comparer to use in the search.
        /// </param>
        /// <returns>
        /// A new list with the elements removed.
        /// </returns>
        public ImmutableArray<T> RemoveRange(T[] items, IEqualityComparer<T>? equalityComparer = null)
        {
            ImmutableArray<T> self = this;
            self.ThrowNullRefIfNotInitialized();

            Requires.NotNull(items, nameof(items));

            return self.RemoveRange(new ReadOnlySpan<T>(items), equalityComparer);
        }

        /// <summary>
        /// Forms a slice out of the current <see cref="ImmutableArray{T}"/> starting at a specified index for a specified length.
        /// </summary>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="length">The desired length for the slice.</param>
        /// <returns>A <see cref="ImmutableArray{T}"/> that consists of length elements from the current <see cref="ImmutableArray{T}"/> starting at start.</returns>
        public ImmutableArray<T> Slice(int start, int length)
        {
            ImmutableArray<T> self = this;
            self.ThrowNullRefIfNotInitialized();
            return ImmutableArray.Create(self, start, length);
        }

        #region Explicit interface methods

        void IList<T>.Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        void IList<T>.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        void ICollection<T>.Add(T item)
        {
            throw new NotSupportedException();
        }

        void ICollection<T>.Clear()
        {
            throw new NotSupportedException();
        }

        bool ICollection<T>.Remove(T item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        IImmutableList<T> IImmutableList<T>.Clear()
        {
            ImmutableArray<T> self = this;
            self.ThrowInvalidOperationIfNotInitialized();
            return self.Clear();
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        IImmutableList<T> IImmutableList<T>.Add(T value)
        {
            ImmutableArray<T> self = this;
            self.ThrowInvalidOperationIfNotInitialized();
            return self.Add(value);
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        IImmutableList<T> IImmutableList<T>.AddRange(IEnumerable<T> items)
        {
            ImmutableArray<T> self = this;
            self.ThrowInvalidOperationIfNotInitialized();
            return self.AddRange(items);
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        IImmutableList<T> IImmutableList<T>.Insert(int index, T element)
        {
            ImmutableArray<T> self = this;
            self.ThrowInvalidOperationIfNotInitialized();
            return self.Insert(index, element);
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        IImmutableList<T> IImmutableList<T>.InsertRange(int index, IEnumerable<T> items)
        {
            ImmutableArray<T> self = this;
            self.ThrowInvalidOperationIfNotInitialized();
            return self.InsertRange(index, items);
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        IImmutableList<T> IImmutableList<T>.Remove(T value, IEqualityComparer<T>? equalityComparer)
        {
            ImmutableArray<T> self = this;
            self.ThrowInvalidOperationIfNotInitialized();
            return self.Remove(value, equalityComparer);
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        IImmutableList<T> IImmutableList<T>.RemoveAll(Predicate<T> match)
        {
            ImmutableArray<T> self = this;
            self.ThrowInvalidOperationIfNotInitialized();
            return self.RemoveAll(match);
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        IImmutableList<T> IImmutableList<T>.RemoveRange(IEnumerable<T> items, IEqualityComparer<T>? equalityComparer)
        {
            ImmutableArray<T> self = this;
            self.ThrowInvalidOperationIfNotInitialized();
            return self.RemoveRange(items, equalityComparer);
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        IImmutableList<T> IImmutableList<T>.RemoveRange(int index, int count)
        {
            ImmutableArray<T> self = this;
            self.ThrowInvalidOperationIfNotInitialized();
            return self.RemoveRange(index, count);
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        IImmutableList<T> IImmutableList<T>.RemoveAt(int index)
        {
            ImmutableArray<T> self = this;
            self.ThrowInvalidOperationIfNotInitialized();
            return self.RemoveAt(index);
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        IImmutableList<T> IImmutableList<T>.SetItem(int index, T value)
        {
            ImmutableArray<T> self = this;
            self.ThrowInvalidOperationIfNotInitialized();
            return self.SetItem(index, value);
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        IImmutableList<T> IImmutableList<T>.Replace(T oldValue, T newValue, IEqualityComparer<T>? equalityComparer)
        {
            ImmutableArray<T> self = this;
            self.ThrowInvalidOperationIfNotInitialized();
            return self.Replace(oldValue, newValue, equalityComparer);
        }

        /// <summary>
        /// Adds an item to the <see cref="IList"/>.
        /// </summary>
        /// <param name="value">The object to add to the <see cref="IList"/>.</param>
        /// <returns>
        /// The position into which the new element was inserted, or -1 to indicate that the item was not inserted into the collection,
        /// </returns>
        /// <exception cref="System.NotSupportedException"></exception>
        int IList.Add(object? value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Removes all items from the <see cref="ICollection{T}"/>.
        /// </summary>
        /// <exception cref="System.NotSupportedException"></exception>
        void IList.Clear()
        {
            throw new NotSupportedException();
        }

        private static bool IsCompatibleObject(object? value)
        {
            // Non-null values are fine.  Only accept nulls if T is a class or Nullable<U>.
            // Note that default(T) is not equal to null for value types except when T is Nullable<U>.
            return (value is T) || (default(T) == null && value == null);
        }

        /// <summary>
        /// Determines whether the <see cref="IList"/> contains a specific value.
        /// </summary>
        /// <param name="value">The object to locate in the <see cref="IList"/>.</param>
        /// <returns>
        /// true if the <see cref="object"/> is found in the <see cref="IList"/>; otherwise, false.
        /// </returns>
        bool IList.Contains(object? value)
        {
            if (IsCompatibleObject(value))
            {
                ImmutableArray<T> self = this;
                self.ThrowInvalidOperationIfNotInitialized();
                return self.Contains((T)value!);
            }
            return false;
        }

        /// <summary>
        /// Determines the index of a specific item in the <see cref="IList"/>.
        /// </summary>
        /// <param name="value">The object to locate in the <see cref="IList"/>.</param>
        /// <returns>
        /// The index of <paramref name="value"/> if found in the list; otherwise, -1.
        /// </returns>
        int IList.IndexOf(object? value)
        {
            if (IsCompatibleObject(value))
            {
                ImmutableArray<T> self = this;
                self.ThrowInvalidOperationIfNotInitialized();
                return self.IndexOf((T)value!);
            }
            return -1;
        }

        /// <summary>
        /// Inserts an item to the <see cref="IList"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="value"/> should be inserted.</param>
        /// <param name="value">The object to insert into the <see cref="IList"/>.</param>
        /// <exception cref="System.NotSupportedException"></exception>
        void IList.Insert(int index, object? value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Gets a value indicating whether this instance is fixed size.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is fixed size; otherwise, <c>false</c>.
        /// </value>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        bool IList.IsFixedSize
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is read only.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is read only; otherwise, <c>false</c>.
        /// </value>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        bool IList.IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        /// Gets the size of the array.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the <see cref="IsDefault"/> property returns true.</exception>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        int ICollection.Count
        {
            get
            {
                ImmutableArray<T> self = this;
                self.ThrowInvalidOperationIfNotInitialized();
                return self.Length;
            }
        }

        /// <summary>
        /// See the <see cref="ICollection"/> interface.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        bool ICollection.IsSynchronized
        {
            get
            {
                // This is immutable, so it is always thread-safe.
                return true;
            }
        }

        /// <summary>
        /// Gets the sync root.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        object ICollection.SyncRoot
        {
            get { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="IList"/>.
        /// </summary>
        /// <param name="value">The object to remove from the <see cref="IList"/>.</param>
        /// <exception cref="System.NotSupportedException"></exception>
        void IList.Remove(object? value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Removes the <see cref="IList{T}"/> item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="System.NotSupportedException"></exception>
        void IList.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Gets or sets the <see cref="object"/> at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="object"/>.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">Always thrown from the setter.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the <see cref="IsDefault"/> property returns true.</exception>
        object? IList.this[int index]
        {
            get
            {
                ImmutableArray<T> self = this;
                self.ThrowInvalidOperationIfNotInitialized();
                return self[index];
            }
            set { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Copies the elements of the <see cref="ICollection"/> to an <see cref="Array"/>, starting at a particular <see cref="Array"/> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="ICollection"/>. The <see cref="Array"/> must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        void ICollection.CopyTo(Array array, int index)
        {
            ImmutableArray<T> self = this;
            self.ThrowInvalidOperationIfNotInitialized();
            Array.Copy(self.array!, 0, array, index, self.Length);
        }

        /// <summary>
        /// Determines whether an object is structurally equal to the current instance.
        /// </summary>
        /// <param name="other">The object to compare with the current instance.</param>
        /// <param name="comparer">An object that determines whether the current instance and other are equal.</param>
        /// <returns>true if the two objects are equal; otherwise, false.</returns>
        bool IStructuralEquatable.Equals(object? other, IEqualityComparer comparer)
        {
            ImmutableArray<T> self = this;
            Array? otherArray = other as Array;
            if (otherArray == null)
            {
                if (other is IImmutableArray theirs)
                {
                    otherArray = theirs.Array;

                    if (self.array == null && otherArray == null)
                    {
                        return true;
                    }
                    else if (self.array == null)
                    {
                        return false;
                    }
                }
            }

            IStructuralEquatable ours = self.array!;
            return ours.Equals(otherArray, comparer);
        }

        /// <summary>
        /// Returns a hash code for the current instance.
        /// </summary>
        /// <param name="comparer">An object that computes the hash code of the current object.</param>
        /// <returns>The hash code for the current instance.</returns>
        int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
        {
            ImmutableArray<T> self = this;
            IStructuralEquatable? ours = self.array;
            return ours != null ? ours.GetHashCode(comparer) : self.GetHashCode();
        }

        /// <summary>
        /// Determines whether the current collection object precedes, occurs in the
        /// same position as, or follows another object in the sort order.
        /// </summary>
        /// <param name="other">The object to compare with the current instance.</param>
        /// <param name="comparer">
        /// An object that compares members of the current collection object with the
        /// corresponding members of other.
        /// </param>
        /// <returns>
        /// An integer that indicates the relationship of the current collection object
        /// to other.
        /// </returns>
        int IStructuralComparable.CompareTo(object? other, IComparer comparer)
        {
            ImmutableArray<T> self = this;
            Array? otherArray = other as Array;
            if (otherArray == null)
            {
                if (other is IImmutableArray theirs)
                {
                    otherArray = theirs.Array;

                    if (self.array == null && otherArray == null)
                    {
                        return 0;
                    }
                    else if (self.array == null ^ otherArray == null)
                    {
                        throw new ArgumentException(SR.ArrayInitializedStateNotEqual, nameof(other));
                    }
                }
            }

            if (otherArray != null)
            {
                IStructuralComparable? ours = self.array;
                if (ours == null)
                {
                    throw new ArgumentException(SR.ArrayInitializedStateNotEqual, nameof(other));
                }

                return ours.CompareTo(otherArray, comparer);
            }

            throw new ArgumentException(SR.ArrayLengthsNotEqual, nameof(other));
        }

        #endregion


        /// <summary>
        /// Returns an array with items at the specified indices removed.
        /// </summary>
        /// <param name="indicesToRemove">A **sorted set** of indices to elements that should be omitted from the returned array.</param>
        /// <returns>The new array.</returns>
        private ImmutableArray<T> RemoveAtRange(ICollection<int> indicesToRemove)
        {
            ImmutableArray<T> self = this;
            self.ThrowNullRefIfNotInitialized();
            Requires.NotNull(indicesToRemove, nameof(indicesToRemove));

            if (indicesToRemove.Count == 0)
            {
                // Be sure to return a !IsDefault instance.
                return self;
            }

            var newArray = new T[self.Length - indicesToRemove.Count];
            int copied = 0;
            int removed = 0;
            int lastIndexRemoved = -1;
            foreach (int indexToRemove in indicesToRemove)
            {
                int copyLength = lastIndexRemoved == -1 ? indexToRemove : (indexToRemove - lastIndexRemoved - 1);
                Debug.Assert(indexToRemove > lastIndexRemoved); // We require that the input be a sorted set.
                Array.Copy(self.array!, copied + removed, newArray, copied, copyLength);
                removed++;
                copied += copyLength;
                lastIndexRemoved = indexToRemove;
            }

            Array.Copy(self.array!, copied + removed, newArray, copied, self.Length - (copied + removed));

            return new ImmutableArray<T>(newArray);
        }

        private ImmutableArray<T> InsertSpanRangeInternal(int index, ReadOnlySpan<T> items)
        {
            Debug.Assert(array != null);
            Debug.Assert(!IsEmpty);
            Debug.Assert(!items.IsEmpty);

            var tmp = new T[Length + items.Length];
            if (index != 0)
            {
                Array.Copy(array!, tmp, index);
            }
            items.CopyTo(new Span<T>(tmp, index, items.Length));
            if (index != Length)
            {
                Array.Copy(array!, index, tmp, index + items.Length, Length - index);
            }

            return new ImmutableArray<T>(tmp);
        }
    }
}
