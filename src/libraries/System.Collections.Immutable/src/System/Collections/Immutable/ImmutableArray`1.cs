// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using Validation;

namespace System.Collections.Immutable
{
    /// <summary>
    /// A readonly array with O(1) indexable lookup time.
    /// </summary>
    /// <typeparam name="T">The type of element stored by the array.</typeparam>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public partial struct ImmutableArray<T> : IReadOnlyList<T>, IList<T>, IEquatable<ImmutableArray<T>>, IImmutableList<T>, IList, IImmutableArray, IStructuralComparable, IStructuralEquatable
    {
        /// <summary>
        /// An empty (initialized) instance of ImmutableArray{T}.
        /// </summary>
        public static readonly ImmutableArray<T> Empty = new ImmutableArray<T>(new T[0]);

        /// <summary>
        /// The backing field for this instance. References to this value should never be shared with outside code. 
        /// </summary>
        /// <remarks>
        /// This would be private, but we make it internal so that our own extension methods can access it.
        /// </remarks>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        internal T[] array;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableArray"/> struct
        /// *without making a defensive copy*.
        /// </summary>
        /// <param name="items">The array to use. May be null for "default" arrays.</param>
        internal ImmutableArray(T[] items)
        {
            this.array = items;
        }

        #region Operators

        /// <summary>
        /// Checks equality between two instances.
        /// </summary>
        /// <param name="left">The instance to the left of the operator.</param>
        /// <param name="right">The instance to the right of the operator.</param>
        /// <returns><c>true</c> if the values' underlying arrays are reference equal; <c>false</c> otherwise.</returns>
        public static bool operator ==(ImmutableArray<T> left, ImmutableArray<T> right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Checks inequality between two instances.
        /// </summary>
        /// <param name="left">The instance to the left of the operator.</param>
        /// <param name="right">The instance to the right of the operator.</param>
        /// <returns><c>true</c> if the values' underlying arrays are reference not equal; <c>false</c> otherwise.</returns>
        public static bool operator !=(ImmutableArray<T> left, ImmutableArray<T> right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Checks equality between two instances.
        /// </summary>
        /// <param name="left">The instance to the left of the operator.</param>
        /// <param name="right">The instance to the right of the operator.</param>
        /// <returns><c>true</c> if the values' underlying arrays are reference equal; <c>false</c> otherwise.</returns>
        public static bool operator ==(ImmutableArray<T>? left, ImmutableArray<T>? right)
        {
            return left.GetValueOrDefault().Equals(right.GetValueOrDefault());
        }

        /// <summary>
        /// Checks inequality between two instances.
        /// </summary>
        /// <param name="left">The instance to the left of the operator.</param>
        /// <param name="right">The instance to the right of the operator.</param>
        /// <returns><c>true</c> if the values' underlying arrays are reference not equal; <c>false</c> otherwise.</returns>
        public static bool operator !=(ImmutableArray<T>? left, ImmutableArray<T>? right)
        {
            return !left.GetValueOrDefault().Equals(right.GetValueOrDefault());
        }

        #endregion

        /// <summary>
        /// Gets the element at the specified index in the read-only list.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <returns>The element at the specified index in the read-only list.</returns>
        public T this[int index]
        {
            get
            {
                // We intentionally do not check this.array != null, and throw NullReferenceException
                // if this is called while uninitialized.
                // The reason for this is perf.
                // Length and the indexer must be absolutely trivially implemented for the JIT optimization
                // of removing array bounds checking to work.
                return this.array[index];
            }
        }

        /// <summary>
        /// Gets or sets the element at the specified index in the read-only list.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <returns>The element at the specified index in the read-only list.</returns>
        /// <exception cref="NotSupportedException">Always thrown from the setter.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the <see cref="IsDefault" /> property returns true.</exception>
        T IList<T>.this[int index]
        {
            get
            {
                this.ThrowInvalidOperationIfNotInitialized();
                return this[index];
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
        /// Gets a value indicating whether this collection is empty.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsEmpty
        {
            get { return this.Length == 0; }
        }

        /// <summary>
        /// Gets the number of array in the collection.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int Length
        {
            get
            {
                // We intentionally do not check this.array != null, and throw NullReferenceException
                // if this is called while uninitialized.
                // The reason for this is perf.
                // Length and the indexer must be absolutely trivially implemented for the JIT optimization
                // of removing array bounds checking to work.
                return this.array.Length;
            }
        }

        /// <summary>
        /// Gets the number of array in the collection.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the <see cref="IsDefault" /> property returns true.</exception>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        int ICollection<T>.Count
        {
            get
            {
                this.ThrowInvalidOperationIfNotInitialized();
                return this.Length;
            }
        }

        /// <summary>
        /// Gets the number of array in the collection.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the <see cref="IsDefault" /> property returns true.</exception>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        int IReadOnlyCollection<T>.Count
        {
            get
            {
                this.ThrowInvalidOperationIfNotInitialized();
                return this.Length;
            }
        }

        /// <summary>
        /// Gets the element at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>
        /// The element.
        /// </returns>
        /// <exception cref="InvalidOperationException">Thrown if the <see cref="IsDefault" /> property returns true.</exception>
        T IReadOnlyList<T>.this[int index]
        {
            get
            {
                this.ThrowInvalidOperationIfNotInitialized();
                return this[index];
            }
        }

        /// <summary>
        /// Gets a value indicating whether this struct was initialized without an actual array instance.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsDefault
        {
            get { return this.array == null; }
        }

        /// <summary>
        /// Gets a value indicating whether this struct is empty or uninitialized.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsDefaultOrEmpty
        {
            get { return this.array == null || this.array.Length == 0; }
        }

        /// <summary>
        /// Gets an untyped reference to the array.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        Array IImmutableArray.Array
        {
            get { return this.array; }
        }

        /// <summary>
        /// Gets the string to display in the debugger watches window for this instance.
        /// </summary>
        [ExcludeFromCodeCoverage]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay
        {
            get { return this.IsDefault ? "Uninitialized" : String.Format(CultureInfo.CurrentCulture, "Length = {0}", this.Length); }
        }

        /// <summary>
        /// Searches the array for the specified item.
        /// </summary>
        /// <param name="item">The item to search for.</param>
        /// <returns>The 0-based index into the array where the item was found; or -1 if it could not be found.</returns>
        [Pure]
        public int IndexOf(T item)
        {
            return this.IndexOf(item, 0, this.Length, EqualityComparer<T>.Default);
        }

        /// <summary>
        /// Searches the array for the specified item.
        /// </summary>
        /// <param name="item">The item to search for.</param>
        /// <param name="startIndex">The index at which to begin the search.</param>
        /// <param name="equalityComparer">The equality comparer to use in the search.</param>
        /// <returns>The 0-based index into the array where the item was found; or -1 if it could not be found.</returns>
        [Pure]
        public int IndexOf(T item, int startIndex, IEqualityComparer<T> equalityComparer)
        {
            return this.IndexOf(item, startIndex, this.Length - startIndex, equalityComparer);
        }

        /// <summary>
        /// Searches the array for the specified item.
        /// </summary>
        /// <param name="item">The item to search for.</param>
        /// <param name="startIndex">The index at which to begin the search.</param>
        /// <returns>The 0-based index into the array where the item was found; or -1 if it could not be found.</returns>
        [Pure]
        public int IndexOf(T item, int startIndex)
        {
            return this.IndexOf(item, startIndex, this.Length - startIndex, EqualityComparer<T>.Default);
        }

        /// <summary>
        /// Searches the array for the specified item.
        /// </summary>
        /// <param name="item">The item to search for.</param>
        /// <param name="startIndex">The index at which to begin the search.</param>
        /// <param name="count">The number of elements to search.</param>
        /// <returns>The 0-based index into the array where the item was found; or -1 if it could not be found.</returns>
        [Pure]
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
        /// <param name="equalityComparer">The equality comparer to use in the search.</param>
        /// <returns>The 0-based index into the array where the item was found; or -1 if it could not be found.</returns>
        [Pure]
        public int IndexOf(T item, int startIndex, int count, IEqualityComparer<T> equalityComparer)
        {
            this.ThrowNullRefIfNotInitialized();
            Requires.NotNull(equalityComparer, "equalityComparer");

            if (count == 0 && startIndex == 0)
            {
                return -1;
            }

            Requires.Range(startIndex >= 0 && startIndex < this.Length, "startIndex");
            Requires.Range(count >= 0 && startIndex + count <= this.Length, "count");

            if (equalityComparer == EqualityComparer<T>.Default)
            {
                return Array.IndexOf(this.array, item, startIndex, count);
            }
            else
            {
                for (int i = startIndex; i < startIndex + count; i++)
                {
                    if (equalityComparer.Equals(this.array[i], item))
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
        [Pure]
        public int LastIndexOf(T item)
        {
            if (this.Length == 0)
            {
                return -1;
            }

            return this.LastIndexOf(item, this.Length - 1, this.Length, EqualityComparer<T>.Default);
        }

        /// <summary>
        /// Searches the array for the specified item in reverse.
        /// </summary>
        /// <param name="item">The item to search for.</param>
        /// <param name="startIndex">The index at which to begin the search.</param>
        /// <returns>The 0-based index into the array where the item was found; or -1 if it could not be found.</returns>
        [Pure]
        public int LastIndexOf(T item, int startIndex)
        {
            if (this.Length == 0 && startIndex == 0)
            {
                return -1;
            }

            return this.LastIndexOf(item, startIndex, startIndex + 1, EqualityComparer<T>.Default);
        }

        /// <summary>
        /// Searches the array for the specified item in reverse.
        /// </summary>
        /// <param name="item">The item to search for.</param>
        /// <param name="startIndex">The index at which to begin the search.</param>
        /// <param name="count">The number of elements to search.</param>
        /// <returns>The 0-based index into the array where the item was found; or -1 if it could not be found.</returns>
        [Pure]
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
        [Pure]
        public int LastIndexOf(T item, int startIndex, int count, IEqualityComparer<T> equalityComparer)
        {
            this.ThrowNullRefIfNotInitialized();
            Requires.NotNull(equalityComparer, "equalityComparer");

            if (startIndex == 0 && count == 0)
            {
                return -1;
            }

            Requires.Range(startIndex >= 0 && startIndex < this.Length, "startIndex");
            Requires.Range(count >= 0 && startIndex - count + 1 >= 0, "count");

            if (equalityComparer == EqualityComparer<T>.Default)
            {
                return Array.LastIndexOf(this.array, item, startIndex, count);
            }
            else
            {
                for (int i = startIndex; i >= startIndex - count + 1; i--)
                {
                    if (equalityComparer.Equals(item, this.array[i]))
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
        [Pure]
        public bool Contains(T item)
        {
            return this.IndexOf(item) >= 0;
        }

        /// <summary>
        /// Copies the contents of this array to the specified array.
        /// </summary>
        /// <param name="destination">The array to copy to.</param>
        [Pure]
        public void CopyTo(T[] destination)
        {
            this.ThrowNullRefIfNotInitialized();
            Array.Copy(this.array, 0, destination, 0, this.Length);
        }

        /// <summary>
        /// Copies the contents of this array to the specified array.
        /// </summary>
        /// <param name="destination">The array to copy to.</param>
        /// <param name="destinationIndex">The index into the destination array to which the first copied element is written.</param>
        [Pure]
        public void CopyTo(T[] destination, int destinationIndex)
        {
            this.ThrowNullRefIfNotInitialized();
            Array.Copy(this.array, 0, destination, destinationIndex, this.Length);
        }

        /// <summary>
        /// Copies the contents of this array to the specified array.
        /// </summary>
        /// <param name="sourceIndex">The index into this collection of the first element to copy.</param>
        /// <param name="destination">The array to copy to.</param>
        /// <param name="destinationIndex">The index into the destination array to which the first copied element is written.</param>
        /// <param name="length">The number of elements to copy.</param>
        [Pure]
        public void CopyTo(int sourceIndex, T[] destination, int destinationIndex, int length)
        {
            this.ThrowNullRefIfNotInitialized();
            Array.Copy(this.array, sourceIndex, destination, destinationIndex, length);
        }

        /// <summary>
        /// Returns a new array with the specified value inserted at the specified position.
        /// </summary>
        /// <param name="index">The 0-based index into the array at which the new item should be added.</param>
        /// <param name="item">The item to insert at the start of the array.</param>
        /// <returns>A new array.</returns>
        [Pure]
        public ImmutableArray<T> Insert(int index, T item)
        {
            this.ThrowNullRefIfNotInitialized();
            Requires.Range(index >= 0 && index <= this.Length, "index");

            if (this.Length == 0)
            {
                return ImmutableArray.Create(item);
            }

            T[] tmp = new T[this.Length + 1];
            Array.Copy(this.array, 0, tmp, 0, index);
            tmp[index] = item;
            Array.Copy(this.array, index, tmp, index + 1, this.Length - index);
            return new ImmutableArray<T>(tmp);
        }

        /// <summary>
        /// Inserts the specified values at the specified index.
        /// </summary>
        /// <param name="index">The index at which to insert the value.</param>
        /// <param name="items">The elements to insert.</param>
        /// <returns>The new immutable collection.</returns>
        [Pure]
        public ImmutableArray<T> InsertRange(int index, IEnumerable<T> items)
        {
            this.ThrowNullRefIfNotInitialized();
            Requires.Range(index >= 0 && index <= this.Length, "index");
            Requires.NotNull(items, "items");

            if (this.Length == 0)
            {
                return ImmutableArray.CreateRange(items);
            }

            int count = ImmutableExtensions.GetCount(ref items);
            if (count == 0)
            {
                return this;
            }

            T[] tmp = new T[this.Length + count];
            Array.Copy(this.array, 0, tmp, 0, index);
            int sequenceIndex = index;
            foreach (var item in items)
            {
                tmp[sequenceIndex++] = item;
            }

            Array.Copy(this.array, index, tmp, index + count, this.Length - index);
            return new ImmutableArray<T>(tmp);
        }

        /// <summary>
        /// Inserts the specified values at the specified index.
        /// </summary>
        /// <param name="index">The index at which to insert the value.</param>
        /// <param name="items">The elements to insert.</param>
        /// <returns>The new immutable collection.</returns>
        [Pure]
        public ImmutableArray<T> InsertRange(int index, ImmutableArray<T> items)
        {
            this.ThrowNullRefIfNotInitialized();
            ThrowNullRefIfNotInitialized(items);
            Requires.Range(index >= 0 && index <= this.Length, "index");

            if (this.IsEmpty)
            {
                return new ImmutableArray<T>(items.array);
            }
            else if (items.IsEmpty)
            {
                return new ImmutableArray<T>(this.array);
            }

            return this.InsertRange(index, items.array);
        }

        /// <summary>
        /// Returns a new array with the specified value inserted at the end.
        /// </summary>
        /// <param name="item">The item to insert at the end of the array.</param>
        /// <returns>A new array.</returns>
        [Pure]
        public ImmutableArray<T> Add(T item)
        {
            if (this.Length == 0)
            {
                return ImmutableArray.Create(item);
            }

            return this.Insert(this.Length, item);
        }

        /// <summary>
        /// Adds the specified values to this list.
        /// </summary>
        /// <param name="items">The values to add.</param>
        /// <returns>A new list with the elements added.</returns>
        [Pure]
        public ImmutableArray<T> AddRange(IEnumerable<T> items)
        {
            return this.InsertRange(this.Length, items);
        }

        /// <summary>
        /// Adds the specified values to this list.
        /// </summary>
        /// <param name="items">The values to add.</param>
        /// <returns>A new list with the elements added.</returns>
        [Pure]
        public ImmutableArray<T> AddRange(ImmutableArray<T> items)
        {
            this.ThrowNullRefIfNotInitialized();
            ThrowNullRefIfNotInitialized(items);
            if (this.IsEmpty)
            {
                // Be sure what we return is marked as initialized.
                return new ImmutableArray<T>(items.array);
            }
            else if (items.IsEmpty)
            {
                return this;
            }

            return this.AddRange(items.array);
        }

        /// <summary>
        /// Returns an array with the item at the specified position replaced.
        /// </summary>
        /// <param name="index">The index of the item to replace.</param>
        /// <param name="item">The new item.</param>
        /// <returns>The new array.</returns>
        [Pure]
        public ImmutableArray<T> SetItem(int index, T item)
        {
            Requires.Range(index >= 0 && index < this.Length, "index");

            T[] tmp = new T[this.Length];
            Array.Copy(this.array, tmp, this.Length);
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
        [Pure]
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
        /// </param>
        /// <returns>The new list -- even if the value being replaced is equal to the new value for that position.</returns>
        /// <exception cref="ArgumentException">Thrown when the old value does not exist in the list.</exception>
        [Pure]
        public ImmutableArray<T> Replace(T oldValue, T newValue, IEqualityComparer<T> equalityComparer)
        {
            int index = this.IndexOf(oldValue, equalityComparer);
            if (index < 0)
            {
                throw new ArgumentException(Strings.CannotFindOldValue, "oldValue");
            }

            return this.SetItem(index, newValue);
        }

        /// <summary>
        /// Returns an array with the first occurrence of the specified element removed from the array.
        /// If no match is found, the current array is returned.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <returns>The new array.</returns>
        [Pure]
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
        /// </param>
        /// <returns>The new array.</returns>
        [Pure]
        public ImmutableArray<T> Remove(T item, IEqualityComparer<T> equalityComparer)
        {
            this.ThrowNullRefIfNotInitialized();
            int index = this.IndexOf(item, equalityComparer);
            return index < 0
                ? new ImmutableArray<T>(this.array)
                : this.RemoveAt(index);
        }

        /// <summary>
        /// Returns an array with the element at the specified position removed.
        /// </summary>
        /// <param name="index">The 0-based index into the array for the element to omit from the returned array.</param>
        /// <returns>The new array.</returns>
        [Pure]
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
        [Pure]
        public ImmutableArray<T> RemoveRange(int index, int length)
        {
            Requires.Range(index >= 0 && index < this.Length, "index");
            Requires.Range(length >= 0 && index + length <= this.Length, "length");

            T[] tmp = new T[this.Length - length];
            Array.Copy(this.array, 0, tmp, 0, index);
            Array.Copy(this.array, index + length, tmp, index, this.Length - index - length);
            return new ImmutableArray<T>(tmp);
        }

        /// <summary>
        /// Removes the specified values from this list.
        /// </summary>
        /// <param name="items">The items to remove if matches are found in this list.</param>
        /// <returns>
        /// A new list with the elements removed.
        /// </returns>
        [Pure]
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
        /// </param>
        /// <returns>
        /// A new list with the elements removed.
        /// </returns>
        [Pure]
        public ImmutableArray<T> RemoveRange(IEnumerable<T> items, IEqualityComparer<T> equalityComparer)
        {
            this.ThrowNullRefIfNotInitialized();
            Requires.NotNull(items, "items");
            Requires.NotNull(equalityComparer, "equalityComparer");

            var indexesToRemove = new SortedSet<int>();
            foreach (var item in items)
            {
                int index = this.IndexOf(item, equalityComparer);
                while (index >= 0 && !indexesToRemove.Add(index) && index + 1 < this.Length)
                {
                    // This is a duplicate of one we've found. Try hard to find another instance in the list to remove.
                    index = this.IndexOf(item, index + 1, equalityComparer);
                }
            }

            return this.RemoveAtRange(indexesToRemove);
        }

        /// <summary>
        /// Removes the specified values from this list.
        /// </summary>
        /// <param name="items">The items to remove if matches are found in this list.</param>
        /// <returns>
        /// A new list with the elements removed.
        /// </returns>
        [Pure]
        public ImmutableArray<T> RemoveRange(ImmutableArray<T> items)
        {
            return this.RemoveRange(items.array);
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
        [Pure]
        public ImmutableArray<T> RemoveRange(ImmutableArray<T> items, IEqualityComparer<T> equalityComparer)
        {
            return this.RemoveRange(items.array, equalityComparer);
        }

        /// <summary>
        /// Removes all the elements that match the conditions defined by the specified
        /// predicate.
        /// </summary>
        /// <param name="match">
        /// The System.Predicate&lt;T&gt; delegate that defines the conditions of the elements
        /// to remove.
        /// </param>
        /// <returns>
        /// The new list.
        /// </returns>
        [Pure]
        public ImmutableArray<T> RemoveAll(Predicate<T> match)
        {
            this.ThrowNullRefIfNotInitialized();
            Requires.NotNull(match, "match");

            if (this.IsEmpty)
            {
                return new ImmutableArray<T>(this.array);
            }

            var removeIndexes = new List<int>();
            for (int i = 0; i < this.array.Length; i++)
            {
                if (match(this.array[i]))
                {
                    removeIndexes.Add(i);
                }
            }

            return this.RemoveAtRange(removeIndexes);
        }

        /// <summary>
        /// Returns an empty array.
        /// </summary>
        [Pure]
        public ImmutableArray<T> Clear()
        {
            return Empty;
        }

        /// <summary>
        /// Returns a sorted instance of this array.
        /// </summary>
        [Pure]
        public ImmutableArray<T> Sort()
        {
            return this.Sort(0, this.Length, Comparer<T>.Default);
        }

        /// <summary>
        /// Returns a sorted instance of this array.
        /// </summary>
        /// <param name="comparer">The comparer to use in sorting. If <c>null</c>, the default comparer is used.</param>
        [Pure]
        public ImmutableArray<T> Sort(IComparer<T> comparer)
        {
            return this.Sort(0, this.Length, comparer);
        }

        /// <summary>
        /// Returns a sorted instance of this array.
        /// </summary>
        /// <param name="index">The index of the first element to consider in the sort.</param>
        /// <param name="count">The number of elements to include in the sort.</param>
        /// <param name="comparer">The comparer to use in sorting. If <c>null</c>, the default comparer is used.</param>
        [Pure]
        public ImmutableArray<T> Sort(int index, int count, IComparer<T> comparer)
        {
            this.ThrowNullRefIfNotInitialized();
            Requires.Range(index >= 0, "index");
            Requires.Range(count >= 0 && index + count <= this.Length, "count");

            if (comparer == null)
            {
                comparer = Comparer<T>.Default;
            }

            // 0 and 1 element arrays don't need to be sorted.
            if (count > 1)
            {
                // Avoid copying the entire array when the array is already sorted.
                bool outOfOrder = false;
                for (int i = index + 1; i < index + count; i++)
                {
                    if (comparer.Compare(this.array[i - 1], this.array[i]) > 0)
                    {
                        outOfOrder = true;
                        break;
                    }
                }

                if (outOfOrder)
                {
                    var tmp = new T[this.Length];
                    Array.Copy(this.array, tmp, this.Length);
                    Array.Sort(tmp, index, count, comparer);
                    return new ImmutableArray<T>(tmp);
                }
            }

            return new ImmutableArray<T>(this.array);
        }

        /// <summary>
        /// Returns a builder that is populated with the same contents as this array.
        /// </summary>
        /// <returns>The new builder.</returns>
        [Pure]
        public ImmutableArray<T>.Builder ToBuilder()
        {
            if (this.Length == 0)
            {
                return new Builder(); // allow the builder to create itself with a reasonable default capacity
            }

            var builder = new Builder(this.Length);
            builder.AddRange(this);
            return builder;
        }

        /// <summary>
        /// Returns an enumerator for the contents of the array.
        /// </summary>
        /// <returns>An enumerator.</returns>
        [Pure]
        public Enumerator GetEnumerator()
        {
            this.ThrowNullRefIfNotInitialized();
            return new Enumerator(this.array);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        [Pure]
        public override int GetHashCode()
        {
            return this.array == null ? 0 : this.array.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        [Pure]
        public override bool Equals(object obj)
        {
            if (obj is ImmutableArray<T>)
            {
                return this.Equals((ImmutableArray<T>)obj);
            }

            return false;
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        [Pure]
        public bool Equals(ImmutableArray<T> other)
        {
            return this.array == other.array;
        }

        /// <summary>
        /// Creates an immutable array for this array, cast to a different element type.
        /// </summary>
        /// <typeparam name="TOther">The type of array element to return.</typeparam>
        /// <returns>
        /// A struct typed for the base element type. If the cast fails, an instance
        /// is returned whose <see cref="IsDefault"/> property returns <c>true</c>.
        /// </returns>
        /// <remarks>
        /// Arrays of derived elements types can be cast to arrays of base element types
        /// without reallocating the array.
        /// These upcasts can be reversed via this same method, casting an array of base
        /// element types to their derived types. However, downcasting is only successful
        /// when it reverses a prior upcasting operation.
        /// </remarks>
        [Pure]
        public ImmutableArray<TOther> As<TOther>() where TOther : class
        {
            return new ImmutableArray<TOther>(this.array as TOther[]);
        }

        /// <summary>
        /// Filters the elements of this array to those assignable to the specified type.
        /// </summary>
        /// <typeparam name="TResult">The type to filter the elements of the sequence on.</typeparam>
        /// <returns>
        /// An System.Collections.Generic.IEnumerable&lt;T&gt; that contains elements from
        /// the input sequence of type TResult.
        /// </returns>
        [Pure]
        public IEnumerable<TResult> OfType<TResult>()
        {
            if (this.array == null || this.array.Length == 0)
            {
                return Enumerable.Empty<TResult>();
            }

            return this.array.OfType<TResult>();
        }

        #region Explicit interface methods

        [Pure]
        void IList<T>.Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        [Pure]
        void IList<T>.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        [Pure]
        void ICollection<T>.Add(T item)
        {
            throw new NotSupportedException();
        }

        [Pure]
        void ICollection<T>.Clear()
        {
            throw new NotSupportedException();
        }

        [Pure]
        bool ICollection<T>.Remove(T item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns an enumerator for the contents of the array.
        /// </summary>
        /// <returns>An enumerator.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the <see cref="IsDefault" /> property returns true.</exception>
        [Pure]
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            this.ThrowInvalidOperationIfNotInitialized();
            return EnumeratorObject.Create(this.array);
        }

        /// <summary>
        /// Returns an enumerator for the contents of the array.
        /// </summary>
        /// <returns>An enumerator.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the <see cref="IsDefault" /> property returns true.</exception>
        [Pure]
        IEnumerator IEnumerable.GetEnumerator()
        {
            this.ThrowInvalidOperationIfNotInitialized();
            return EnumeratorObject.Create(this.array);
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableList<T> IImmutableList<T>.Clear()
        {
            this.ThrowInvalidOperationIfNotInitialized();
            return this.Clear();
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableList<T> IImmutableList<T>.Add(T value)
        {
            this.ThrowInvalidOperationIfNotInitialized();
            return this.Add(value);
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableList<T> IImmutableList<T>.AddRange(IEnumerable<T> items)
        {
            this.ThrowInvalidOperationIfNotInitialized();
            return this.AddRange(items);
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableList<T> IImmutableList<T>.Insert(int index, T element)
        {
            this.ThrowInvalidOperationIfNotInitialized();
            return this.Insert(index, element);
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableList<T> IImmutableList<T>.InsertRange(int index, IEnumerable<T> items)
        {
            this.ThrowInvalidOperationIfNotInitialized();
            return this.InsertRange(index, items);
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableList<T> IImmutableList<T>.Remove(T value, IEqualityComparer<T> equalityComparer)
        {
            this.ThrowInvalidOperationIfNotInitialized();
            return this.Remove(value, equalityComparer);
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableList<T> IImmutableList<T>.RemoveAll(Predicate<T> match)
        {
            this.ThrowInvalidOperationIfNotInitialized();
            return this.RemoveAll(match);
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableList<T> IImmutableList<T>.RemoveRange(IEnumerable<T> items, IEqualityComparer<T> equalityComparer)
        {
            this.ThrowInvalidOperationIfNotInitialized();
            return this.RemoveRange(items, equalityComparer);
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableList<T> IImmutableList<T>.RemoveRange(int index, int count)
        {
            this.ThrowInvalidOperationIfNotInitialized();
            return this.RemoveRange(index, count);
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableList<T> IImmutableList<T>.RemoveAt(int index)
        {
            this.ThrowInvalidOperationIfNotInitialized();
            return this.RemoveAt(index);
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableList<T> IImmutableList<T>.SetItem(int index, T value)
        {
            this.ThrowInvalidOperationIfNotInitialized();
            return this.SetItem(index, value);
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableList<T> IImmutableList<T>.Replace(T oldValue, T newValue, IEqualityComparer<T> equalityComparer)
        {
            this.ThrowInvalidOperationIfNotInitialized();
            return this.Replace(oldValue, newValue, equalityComparer);
        }

        /// <summary>
        /// Adds an item to the <see cref="T:System.Collections.IList" />.
        /// </summary>
        /// <param name="value">The object to add to the <see cref="T:System.Collections.IList" />.</param>
        /// <returns>
        /// The position into which the new element was inserted, or -1 to indicate that the item was not inserted into the collection,
        /// </returns>
        /// <exception cref="System.NotSupportedException"></exception>
        [ExcludeFromCodeCoverage]
        int IList.Add(object value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <exception cref="System.NotSupportedException"></exception>
        [ExcludeFromCodeCoverage]
        void IList.Clear()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.IList" /> contains a specific value.
        /// </summary>
        /// <param name="value">The object to locate in the <see cref="T:System.Collections.IList" />.</param>
        /// <returns>
        /// true if the <see cref="T:System.Object" /> is found in the <see cref="T:System.Collections.IList" />; otherwise, false.
        /// </returns>
        [ExcludeFromCodeCoverage]
        bool IList.Contains(object value)
        {
            this.ThrowInvalidOperationIfNotInitialized();
            return this.Contains((T)value);
        }

        /// <summary>
        /// Determines the index of a specific item in the <see cref="T:System.Collections.IList" />.
        /// </summary>
        /// <param name="value">The object to locate in the <see cref="T:System.Collections.IList" />.</param>
        /// <returns>
        /// The index of <paramref name="value" /> if found in the list; otherwise, -1.
        /// </returns>
        [ExcludeFromCodeCoverage]
        int IList.IndexOf(object value)
        {
            this.ThrowInvalidOperationIfNotInitialized();
            return this.IndexOf((T)value);
        }

        /// <summary>
        /// Inserts an item to the <see cref="T:System.Collections.IList" /> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="value" /> should be inserted.</param>
        /// <param name="value">The object to insert into the <see cref="T:System.Collections.IList" />.</param>
        /// <exception cref="System.NotSupportedException"></exception>
        [ExcludeFromCodeCoverage]
        void IList.Insert(int index, object value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Gets a value indicating whether this instance is fixed size.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is fixed size; otherwise, <c>false</c>.
        /// </value>
        [ExcludeFromCodeCoverage]
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
        [ExcludeFromCodeCoverage]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        bool IList.IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        /// Gets the size of the array.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the <see cref="IsDefault" /> property returns true.</exception>
        [ExcludeFromCodeCoverage]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        int ICollection.Count
        {
            get
            {
                this.ThrowInvalidOperationIfNotInitialized();
                return this.Length;
            }
        }

        /// <summary>
        /// See the ICollection interface.
        /// </summary>
        [ExcludeFromCodeCoverage]
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
        [ExcludeFromCodeCoverage]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        object ICollection.SyncRoot
        {
            get
            {
                // As a struct, this instance will be boxed in order to call this property getter.
                // We will return the boxed instance. If the caller boxes and reboxes repeatedly,
                // they'll get a different SyncRoot each time.
                // No real alternative, but we don't anticipate this will be much problem anyway.
                return this;
            }
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.IList" />.
        /// </summary>
        /// <param name="value">The object to remove from the <see cref="T:System.Collections.IList" />.</param>
        /// <exception cref="System.NotSupportedException"></exception>
        [ExcludeFromCodeCoverage]
        void IList.Remove(object value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Removes the <see cref="T:System.Collections.Generic.IList`1" /> item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="System.NotSupportedException"></exception>
        [ExcludeFromCodeCoverage]
        void IList.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Gets or sets the <see cref="System.Object"/> at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="System.Object"/>.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">Always thrown from the setter.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the <see cref="IsDefault" /> property returns true.</exception>
        [ExcludeFromCodeCoverage]
        object IList.this[int index]
        {
            get
            {
                this.ThrowInvalidOperationIfNotInitialized();
                return this[index];
            }
            set { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.ICollection" /> to an <see cref="T:System.Array" />, starting at a particular <see cref="T:System.Array" /> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from <see cref="T:System.Collections.ICollection" />. The <see cref="T:System.Array" /> must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array" /> at which copying begins.</param>
        [ExcludeFromCodeCoverage]
        void ICollection.CopyTo(Array array, int index)
        {
            this.ThrowInvalidOperationIfNotInitialized();
            Array.Copy(this.array, 0, array, index, this.Length);
        }

        /// <summary>
        /// Determines whether an object is structurally equal to the current instance.
        /// </summary>
        /// <param name="other">The object to compare with the current instance.</param>
        /// <param name="comparer">An object that determines whether the current instance and other are equal.</param>
        /// <returns>true if the two objects are equal; otherwise, false.</returns>
        bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
        {
            Array otherArray = other as Array;
            if (otherArray == null)
            {
                var theirs = other as IImmutableArray;
                if (theirs != null)
                {
                    if (this.array == null && theirs.Array == null)
                    {
                        return true;
                    }
                    else if (this.array == null)
                    {
                        return false;
                    }

                    otherArray = theirs.Array;
                }
            }

            IStructuralEquatable ours = this.array;
            return ours.Equals(otherArray, comparer);
        }

        /// <summary>
        /// Returns a hash code for the current instance.
        /// </summary>
        /// <param name="comparer">An object that computes the hash code of the current object.</param>
        /// <returns>The hash code for the current instance.</returns>
        int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
        {
            IStructuralEquatable ours = this.array;
            return ours != null ? ours.GetHashCode(comparer) : this.GetHashCode();
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
        int IStructuralComparable.CompareTo(object other, IComparer comparer)
        {
            Array otherArray = other as Array;
            if (otherArray == null)
            {
                var theirs = other as IImmutableArray;
                if (theirs != null)
                {
                    if (this.array == null && theirs.Array == null)
                    {
                        return 0;
                    }
                    else if (this.array == null ^ theirs.Array == null)
                    {
                        throw new ArgumentException(Strings.ArrayInitializedStateNotEqual, "other");
                    }

                    otherArray = theirs.Array;
                }
            }

            if (otherArray != null)
            {
                IStructuralComparable ours = this.array;
                return ours.CompareTo(otherArray, comparer);
            }

            throw new ArgumentException(Strings.ArrayLengthsNotEqual, "other");
        }

        #endregion

        /// <summary>
        /// Throws a null reference exception if the array field is null.
        /// </summary>
        internal void ThrowNullRefIfNotInitialized()
        {
            // Force NullReferenceException if array is null by touching its Length.
            // This way of checking has a nice property of requiring very little code
            // and not having any conditions/branches. 
            // In a faulting scenario we are relying on hardware to generate the fault.
            // And in the non-faulting scenario (most common) the check is virtually free since 
            // if we are going to do anything with the array, we will need Length anyways
            // so touching it, and potentially causing a cache miss, is not going to be an 
            // extra expense.
            var unused = this.array.Length;

            // This line is a workaround for a bug in C# compiler
            // The code in this line will not be emitted, but it will prevent incorrect 
            // optimizing away of "Length" call above in Release builds.
            // TODO: remove the workaround when building with Roslyn which does not have this bug.
            var unused2 = unused;
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if the array field is null, ie. the
        /// <see cref="IsDefault"/> property returns true.  The
        /// InvalidOperationException message specifies that the operation cannot be performed
        /// on a default instance of ImmutableArray.
        /// 
        /// This is intended for explicitly implemented interface method and property implementations.
        /// </summary>
        private void ThrowInvalidOperationIfNotInitialized()
        {
            if (this.IsDefault)
            {
                throw new InvalidOperationException(Strings.InvalidOperationOnDefaultArray);
            }
        }

        void IImmutableArray.ThrowInvalidOperationIfNotInitialized()
        {
            this.ThrowInvalidOperationIfNotInitialized();
        }

        /// <summary>
        /// Returns an array with items at the specified indexes removed.
        /// </summary>
        /// <param name="indexesToRemove">A **sorted set** of indexes to elements that should be omitted from the returned array.</param>
        /// <returns>The new array.</returns>
        private ImmutableArray<T> RemoveAtRange(ICollection<int> indexesToRemove)
        {
            this.ThrowNullRefIfNotInitialized();
            Requires.NotNull(indexesToRemove, "indexesToRemove");

            if (indexesToRemove.Count == 0)
            {
                // Be sure to return a !IsDefault instance.
                return new ImmutableArray<T>(this.array);
            }

            var newArray = new T[this.Length - indexesToRemove.Count];
            int copied = 0;
            int removed = 0;
            int lastIndexRemoved = -1;
            foreach (var indexToRemove in indexesToRemove)
            {
                int copyLength = lastIndexRemoved == -1 ? indexToRemove : (indexToRemove - lastIndexRemoved - 1);
                Debug.Assert(indexToRemove > lastIndexRemoved); // We require that the input be a sorted set.
                Array.Copy(this.array, copied + removed, newArray, copied, copyLength);
                removed++;
                copied += copyLength;
                lastIndexRemoved = indexToRemove;
            }

            Array.Copy(this.array, copied + removed, newArray, copied, this.Length - (copied + removed));

            return new ImmutableArray<T>(newArray);
        }

        /// <summary>
        /// Throws a NullReferenceException if the specified array is uninitialized.
        /// </summary>
        private static void ThrowNullRefIfNotInitialized(ImmutableArray<T> array)
        {
            array.ThrowNullRefIfNotInitialized();
        }
    }
}
