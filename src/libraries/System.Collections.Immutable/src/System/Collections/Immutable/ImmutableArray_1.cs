// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;

namespace System.Collections.Immutable
{
    /// <summary>
    /// A readonly array with O(1) indexable lookup time.
    /// </summary>
    /// <typeparam name="T">The type of element stored by the array.</typeparam>
    /// <devremarks>
    /// This type has a documented contract of being exactly one reference-type field in size.
    /// Our own <see cref="ImmutableInterlocked"/> class depends on it, as well as others externally.
    /// IMPORTANT NOTICE FOR MAINTAINERS AND REVIEWERS:
    /// This type should be thread-safe. As a struct, it cannot protect its own fields
    /// from being changed from one thread while its members are executing on other threads
    /// because structs can change *in place* simply by reassigning the field containing
    /// this struct. Therefore it is extremely important that
    /// ** Every member should only dereference <c>this</c> ONCE. **
    /// If a member needs to reference the array field, that counts as a dereference of <c>this</c>.
    /// Calling other instance members (properties or methods) also counts as dereferencing <c>this</c>.
    /// Any member that needs to use <c>this</c> more than once must instead
    /// assign <c>this</c> to a local variable and use that for the rest of the code instead.
    /// This effectively copies the one field in the struct to a local variable so that
    /// it is insulated from other threads.
    /// </devremarks>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    [NonVersionable] // Applies to field layout
    public partial struct ImmutableArray<T> : IReadOnlyList<T>, IList<T>, IEquatable<ImmutableArray<T>>, IImmutableList<T>, IList, IImmutableArray, IStructuralComparable, IStructuralEquatable
    {
        /// <summary>
        /// An empty (initialized) instance of <see cref="ImmutableArray{T}"/>.
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
        /// Initializes a new instance of the <see cref="ImmutableArray{T}"/> struct
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
        [NonVersionable]
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
        [NonVersionable]
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
            [NonVersionable]
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
        /// <exception cref="InvalidOperationException">Thrown if the <see cref="IsDefault"/> property returns true.</exception>
        T IList<T>.this[int index]
        {
            get
            {
                var self = this;
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
        /// Gets a value indicating whether this collection is empty.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsEmpty
        {
            [NonVersionable]
            get { return this.Length == 0; }
        }

        /// <summary>
        /// Gets the number of array in the collection.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int Length
        {
            [NonVersionable]
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
        /// <exception cref="InvalidOperationException">Thrown if the <see cref="IsDefault"/> property returns true.</exception>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        int ICollection<T>.Count
        {
            get
            {
                var self = this;
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
                var self = this;
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
                var self = this;
                self.ThrowInvalidOperationIfNotInitialized();
                return self[index];
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
            get
            {
                var self = this;
                return self.array == null || self.array.Length == 0;
            }
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
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay
        {
            get
            {
                var self = this;
                return self.IsDefault ? "Uninitialized" : String.Format(CultureInfo.CurrentCulture, "Length = {0}", self.Length);
            }
        }

        /// <summary>
        /// Searches the array for the specified item.
        /// </summary>
        /// <param name="item">The item to search for.</param>
        /// <returns>The 0-based index into the array where the item was found; or -1 if it could not be found.</returns>
        [Pure]
        public int IndexOf(T item)
        {
            var self = this;
            return self.IndexOf(item, 0, self.Length, EqualityComparer<T>.Default);
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
            var self = this;
            return self.IndexOf(item, startIndex, self.Length - startIndex, equalityComparer);
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
            var self = this;
            return self.IndexOf(item, startIndex, self.Length - startIndex, EqualityComparer<T>.Default);
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
        /// <param name="equalityComparer">
        /// The equality comparer to use in the search.
        /// If <c>null</c>, <see cref="EqualityComparer{T}.Default"/> is used.
        /// </param>
        /// <returns>The 0-based index into the array where the item was found; or -1 if it could not be found.</returns>
        [Pure]
        public int IndexOf(T item, int startIndex, int count, IEqualityComparer<T> equalityComparer)
        {
            var self = this;
            self.ThrowNullRefIfNotInitialized();

            if (count == 0 && startIndex == 0)
            {
                return -1;
            }

            Requires.Range(startIndex >= 0 && startIndex < self.Length, nameof(startIndex));
            Requires.Range(count >= 0 && startIndex + count <= self.Length, nameof(count));

            equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
            if (equalityComparer == EqualityComparer<T>.Default)
            {
                return Array.IndexOf(self.array, item, startIndex, count);
            }
            else
            {
                for (int i = startIndex; i < startIndex + count; i++)
                {
                    if (equalityComparer.Equals(self.array[i], item))
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
            var self = this;
            if (self.Length == 0)
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
        [Pure]
        public int LastIndexOf(T item, int startIndex)
        {
            var self = this;
            if (self.Length == 0 && startIndex == 0)
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
            var self = this;
            self.ThrowNullRefIfNotInitialized();

            if (startIndex == 0 && count == 0)
            {
                return -1;
            }

            Requires.Range(startIndex >= 0 && startIndex < self.Length, nameof(startIndex));
            Requires.Range(count >= 0 && startIndex - count + 1 >= 0, nameof(count));

            equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
            if (equalityComparer == EqualityComparer<T>.Default)
            {
                return Array.LastIndexOf(self.array, item, startIndex, count);
            }
            else
            {
                for (int i = startIndex; i >= startIndex - count + 1; i--)
                {
                    if (equalityComparer.Equals(item, self.array[i]))
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
            var self = this;
            self.ThrowNullRefIfNotInitialized();
            Array.Copy(self.array, 0, destination, 0, self.Length);
        }

        /// <summary>
        /// Copies the contents of this array to the specified array.
        /// </summary>
        /// <param name="destination">The array to copy to.</param>
        /// <param name="destinationIndex">The index into the destination array to which the first copied element is written.</param>
        [Pure]
        public void CopyTo(T[] destination, int destinationIndex)
        {
            var self = this;
            self.ThrowNullRefIfNotInitialized();
            Array.Copy(self.array, 0, destination, destinationIndex, self.Length);
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
            var self = this;
            self.ThrowNullRefIfNotInitialized();
            Array.Copy(self.array, sourceIndex, destination, destinationIndex, length);
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
            var self = this;
            self.ThrowNullRefIfNotInitialized();
            Requires.Range(index >= 0 && index <= self.Length, nameof(index));

            if (self.Length == 0)
            {
                return ImmutableArray.Create(item);
            }

            T[] tmp = new T[self.Length + 1];
            tmp[index] = item;

            if (index != 0)
            {
                Array.Copy(self.array, 0, tmp, 0, index);
            }
            if (index != self.Length)
            {
                Array.Copy(self.array, index, tmp, index + 1, self.Length - index);
            }
            
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
            var self = this;
            self.ThrowNullRefIfNotInitialized();
            Requires.Range(index >= 0 && index <= self.Length, nameof(index));
            Requires.NotNull(items, nameof(items));

            if (self.Length == 0)
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
                Array.Copy(self.array, 0, tmp, 0, index);
            }
            if (index != self.Length)
            {
                Array.Copy(self.array, index, tmp, index + count, self.Length - index);
            }

            // We want to copy over the items we need to insert.
            // Check first to see if items is a well-known collection we can call CopyTo
            // on to the array, which is an order of magnitude faster than foreach.
            // Otherwise, go to the fallback route where we manually enumerate the sequence
            // and place the items in the array one-by-one.

            if (!items.TryCopyTo(tmp, index))
            {
                int sequenceIndex = index;
                foreach (var item in items)
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
        [Pure]
        public ImmutableArray<T> InsertRange(int index, ImmutableArray<T> items)
        {
            var self = this;
            self.ThrowNullRefIfNotInitialized();
            ThrowNullRefIfNotInitialized(items);
            Requires.Range(index >= 0 && index <= self.Length, nameof(index));

            if (self.IsEmpty)
            {
                return items;
            }
            else if (items.IsEmpty)
            {
                return self;
            }

            T[] tmp = new T[self.Length + items.Length];
            
            if (index != 0)
            {
                Array.Copy(self.array, 0, tmp, 0, index);
            }
            if (index != self.Length)
            {
                Array.Copy(self.array, index, tmp, index + items.Length, self.Length - index);
            }

            Array.Copy(items.array, 0, tmp, index, items.Length);

            return new ImmutableArray<T>(tmp);
        }

        /// <summary>
        /// Returns a new array with the specified value inserted at the end.
        /// </summary>
        /// <param name="item">The item to insert at the end of the array.</param>
        /// <returns>A new array.</returns>
        [Pure]
        public ImmutableArray<T> Add(T item)
        {
            var self = this;
            if (self.Length == 0)
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
        [Pure]
        public ImmutableArray<T> AddRange(IEnumerable<T> items)
        {
            var self = this;
            return self.InsertRange(self.Length, items);
        }

        /// <summary>
        /// Adds the specified values to this list.
        /// </summary>
        /// <param name="items">The values to add.</param>
        /// <returns>A new list with the elements added.</returns>
        [Pure]
        public ImmutableArray<T> AddRange(ImmutableArray<T> items)
        {
            var self = this;
            return self.InsertRange(self.Length, items);
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
            var self = this;
            Requires.Range(index >= 0 && index < self.Length, nameof(index));

            T[] tmp = new T[self.Length];
            Array.Copy(self.array, 0, tmp, 0, self.Length);
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
        /// If <c>null</c>, <see cref="EqualityComparer{T}.Default"/> is used.
        /// </param>
        /// <returns>The new list -- even if the value being replaced is equal to the new value for that position.</returns>
        /// <exception cref="ArgumentException">Thrown when the old value does not exist in the list.</exception>
        [Pure]
        public ImmutableArray<T> Replace(T oldValue, T newValue, IEqualityComparer<T> equalityComparer)
        {
            var self = this;
            int index = self.IndexOf(oldValue, equalityComparer);
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
        /// If <c>null</c>, <see cref="EqualityComparer{T}.Default"/> is used.
        /// </param>
        /// <returns>The new array.</returns>
        [Pure]
        public ImmutableArray<T> Remove(T item, IEqualityComparer<T> equalityComparer)
        {
            var self = this;
            self.ThrowNullRefIfNotInitialized();
            int index = self.IndexOf(item, equalityComparer);
            return index < 0
                ? self
                : self.RemoveAt(index);
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
            var self = this;
            Requires.Range(index >= 0 && index <= self.Length, nameof(index));
            Requires.Range(length >= 0 && index + length <= self.Length, nameof(length));

            if (length == 0)
            {
                return self;
            }

            T[] tmp = new T[self.Length - length];
            Array.Copy(self.array, 0, tmp, 0, index);
            Array.Copy(self.array, index + length, tmp, index, self.Length - index - length);
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
        /// If <c>null</c>, <see cref="EqualityComparer{T}.Default"/> is used.
        /// </param>
        /// <returns>
        /// A new list with the elements removed.
        /// </returns>
        [Pure]
        public ImmutableArray<T> RemoveRange(IEnumerable<T> items, IEqualityComparer<T> equalityComparer)
        {
            var self = this;
            self.ThrowNullRefIfNotInitialized();
            Requires.NotNull(items, nameof(items));

            var indexesToRemove = new SortedSet<int>();
            foreach (var item in items)
            {
                int index = self.IndexOf(item, equalityComparer);
                while (index >= 0 && !indexesToRemove.Add(index) && index + 1 < self.Length)
                {
                    // This is a duplicate of one we've found. Try hard to find another instance in the list to remove.
                    index = self.IndexOf(item, index + 1, equalityComparer);
                }
            }

            return self.RemoveAtRange(indexesToRemove);
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
        public ImmutableArray<T> RemoveRange(ImmutableArray<T> items, IEqualityComparer<T> equalityComparer)
        {
            var self = this;
            Requires.NotNull(items.array, nameof(items));

            if (items.IsEmpty)
            {
                self.ThrowNullRefIfNotInitialized();
                return self;
            }
            else if (items.Length == 1)
            {
                return self.Remove(items[0], equalityComparer);
            }
            else
            {
                return self.RemoveRange(items.array, equalityComparer);
            }
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
        [Pure]
        public ImmutableArray<T> RemoveAll(Predicate<T> match)
        {
            var self = this;
            self.ThrowNullRefIfNotInitialized();
            Requires.NotNull(match, nameof(match));

            if (self.IsEmpty)
            {
                return self;
            }

            List<int> removeIndexes = null;
            for (int i = 0; i < self.array.Length; i++)
            {
                if (match(self.array[i]))
                {
                    if (removeIndexes == null)
                    {
                        removeIndexes = new List<int>();
                    }

                    removeIndexes.Add(i);
                }
            }

            return removeIndexes != null ?
                self.RemoveAtRange(removeIndexes) :
                self;
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
            var self = this;
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
        [Pure]
        public ImmutableArray<T> Sort(Comparison<T> comparison)
        {
            Requires.NotNull(comparison, nameof(comparison));

            var self = this;
            return self.Sort(Comparer<T>.Create(comparison));
        }

        /// <summary>
        /// Returns a sorted instance of this array.
        /// </summary>
        /// <param name="comparer">The comparer to use in sorting. If <c>null</c>, the default comparer is used.</param>
        [Pure]
        public ImmutableArray<T> Sort(IComparer<T> comparer)
        {
            var self = this;
            return self.Sort(0, self.Length, comparer);
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
            var self = this;
            self.ThrowNullRefIfNotInitialized();
            Requires.Range(index >= 0, nameof(index));
            Requires.Range(count >= 0 && index + count <= self.Length, nameof(count));

            // 0 and 1 element arrays don't need to be sorted.
            if (count > 1)
            {
                if (comparer == null)
                {
                    comparer = Comparer<T>.Default;
                }

                // Avoid copying the entire array when the array is already sorted.
                bool outOfOrder = false;
                for (int i = index + 1; i < index + count; i++)
                {
                    if (comparer.Compare(self.array[i - 1], self.array[i]) > 0)
                    {
                        outOfOrder = true;
                        break;
                    }
                }

                if (outOfOrder)
                {
                    var tmp = new T[self.Length];
                    Array.Copy(self.array, 0, tmp, 0, self.Length);
                    Array.Sort(tmp, index, count, comparer);
                    return new ImmutableArray<T>(tmp);
                }
            }

            return self;
        }

        /// <summary>
        /// Returns a builder that is populated with the same contents as this array.
        /// </summary>
        /// <returns>The new builder.</returns>
        [Pure]
        public ImmutableArray<T>.Builder ToBuilder()
        {
            var self = this;
            if (self.Length == 0)
            {
                return new Builder(); // allow the builder to create itself with a reasonable default capacity
            }

            var builder = new Builder(self.Length);
            builder.AddRange(self);
            return builder;
        }

        /// <summary>
        /// Returns an enumerator for the contents of the array.
        /// </summary>
        /// <returns>An enumerator.</returns>
        [Pure]
        public Enumerator GetEnumerator()
        {
            var self = this;
            self.ThrowNullRefIfNotInitialized();
            return new Enumerator(self.array);
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
            var self = this;
            return self.array == null ? 0 : self.array.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="Object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        [Pure]
        public override bool Equals(object obj)
        {
            IImmutableArray other = obj as IImmutableArray;
            if (other != null)
            {
                return this.array == other.Array;
            }

            return false;
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        [Pure]
        [NonVersionable]
        public bool Equals(ImmutableArray<T> other)
        {
            return this.array == other.array;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableArray{T}"/> struct based on the contents
        /// of an existing instance, allowing a covariant static cast to efficiently reuse the existing array.
        /// </summary>
        /// <param name="items">The array to initialize the array with. No copy is made.</param>
        /// <remarks>
        /// Covariant upcasts from this method may be reversed by calling the
        /// <see cref="ImmutableArray{T}.As{TOther}"/>  or <see cref="ImmutableArray{T}.CastArray{TOther}"/>method.
        /// </remarks>
        [Pure]
        public static ImmutableArray<T> CastUp<TDerived>(ImmutableArray<TDerived> items)
            where TDerived : class, T
        {
            return new ImmutableArray<T>(items.array);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableArray{T}"/> struct by casting the underlying
        /// array to an array of type <typeparam name="TOther"/>.
        /// </summary>
        /// <exception cref="InvalidCastException">Thrown if the cast is illegal.</exception>
        [Pure]
        public ImmutableArray<TOther> CastArray<TOther>() where TOther : class
        {
            return new ImmutableArray<TOther>((TOther[])(object)array);
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
        /// An <see cref="IEnumerable{T}"/> that contains elements from
        /// the input sequence of type <typeparamref name="TResult"/>.
        /// </returns>
        [Pure]
        public IEnumerable<TResult> OfType<TResult>()
        {
            var self = this;
            if (self.array == null || self.array.Length == 0)
            {
                return Enumerable.Empty<TResult>();
            }

            return self.array.OfType<TResult>();
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
        /// <exception cref="InvalidOperationException">Thrown if the <see cref="IsDefault"/> property returns true.</exception>
        [Pure]
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            var self = this;
            self.ThrowInvalidOperationIfNotInitialized();
            return EnumeratorObject.Create(self.array);
        }

        /// <summary>
        /// Returns an enumerator for the contents of the array.
        /// </summary>
        /// <returns>An enumerator.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the <see cref="IsDefault"/> property returns true.</exception>
        [Pure]
        IEnumerator IEnumerable.GetEnumerator()
        {
            var self = this;
            self.ThrowInvalidOperationIfNotInitialized();
            return EnumeratorObject.Create(self.array);
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableList<T> IImmutableList<T>.Clear()
        {
            var self = this;
            self.ThrowInvalidOperationIfNotInitialized();
            return self.Clear();
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableList<T> IImmutableList<T>.Add(T value)
        {
            var self = this;
            self.ThrowInvalidOperationIfNotInitialized();
            return self.Add(value);
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableList<T> IImmutableList<T>.AddRange(IEnumerable<T> items)
        {
            var self = this;
            self.ThrowInvalidOperationIfNotInitialized();
            return self.AddRange(items);
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableList<T> IImmutableList<T>.Insert(int index, T element)
        {
            var self = this;
            self.ThrowInvalidOperationIfNotInitialized();
            return self.Insert(index, element);
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableList<T> IImmutableList<T>.InsertRange(int index, IEnumerable<T> items)
        {
            var self = this;
            self.ThrowInvalidOperationIfNotInitialized();
            return self.InsertRange(index, items);
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableList<T> IImmutableList<T>.Remove(T value, IEqualityComparer<T> equalityComparer)
        {
            var self = this;
            self.ThrowInvalidOperationIfNotInitialized();
            return self.Remove(value, equalityComparer);
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableList<T> IImmutableList<T>.RemoveAll(Predicate<T> match)
        {
            var self = this;
            self.ThrowInvalidOperationIfNotInitialized();
            return self.RemoveAll(match);
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableList<T> IImmutableList<T>.RemoveRange(IEnumerable<T> items, IEqualityComparer<T> equalityComparer)
        {
            var self = this;
            self.ThrowInvalidOperationIfNotInitialized();
            return self.RemoveRange(items, equalityComparer);
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableList<T> IImmutableList<T>.RemoveRange(int index, int count)
        {
            var self = this;
            self.ThrowInvalidOperationIfNotInitialized();
            return self.RemoveRange(index, count);
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableList<T> IImmutableList<T>.RemoveAt(int index)
        {
            var self = this;
            self.ThrowInvalidOperationIfNotInitialized();
            return self.RemoveAt(index);
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableList<T> IImmutableList<T>.SetItem(int index, T value)
        {
            var self = this;
            self.ThrowInvalidOperationIfNotInitialized();
            return self.SetItem(index, value);
        }

        /// <summary>
        /// See <see cref="IImmutableList{T}"/>
        /// </summary>
        IImmutableList<T> IImmutableList<T>.Replace(T oldValue, T newValue, IEqualityComparer<T> equalityComparer)
        {
            var self = this;
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
        [ExcludeFromCodeCoverage]
        int IList.Add(object value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Removes all items from the <see cref="ICollection{T}"/>.
        /// </summary>
        /// <exception cref="System.NotSupportedException"></exception>
        [ExcludeFromCodeCoverage]
        void IList.Clear()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Determines whether the <see cref="IList"/> contains a specific value.
        /// </summary>
        /// <param name="value">The object to locate in the <see cref="IList"/>.</param>
        /// <returns>
        /// true if the <see cref="object"/> is found in the <see cref="IList"/>; otherwise, false.
        /// </returns>
        [ExcludeFromCodeCoverage]
        bool IList.Contains(object value)
        {
            var self = this;
            self.ThrowInvalidOperationIfNotInitialized();
            return self.Contains((T)value);
        }

        /// <summary>
        /// Determines the index of a specific item in the <see cref="IList"/>.
        /// </summary>
        /// <param name="value">The object to locate in the <see cref="IList"/>.</param>
        /// <returns>
        /// The index of <paramref name="value"/> if found in the list; otherwise, -1.
        /// </returns>
        [ExcludeFromCodeCoverage]
        int IList.IndexOf(object value)
        {
            var self = this;
            self.ThrowInvalidOperationIfNotInitialized();
            return self.IndexOf((T)value);
        }

        /// <summary>
        /// Inserts an item to the <see cref="IList"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="value"/> should be inserted.</param>
        /// <param name="value">The object to insert into the <see cref="IList"/>.</param>
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
        /// <exception cref="InvalidOperationException">Thrown if the <see cref="IsDefault"/> property returns true.</exception>
        [ExcludeFromCodeCoverage]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        int ICollection.Count
        {
            get
            {
                var self = this;
                self.ThrowInvalidOperationIfNotInitialized();
                return self.Length;
            }
        }

        /// <summary>
        /// See the <see cref="ICollection"/> interface.
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
            get { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="IList"/>.
        /// </summary>
        /// <param name="value">The object to remove from the <see cref="IList"/>.</param>
        /// <exception cref="System.NotSupportedException"></exception>
        [ExcludeFromCodeCoverage]
        void IList.Remove(object value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Removes the <see cref="IList{T}"/> item at the specified index.
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
        /// <exception cref="InvalidOperationException">Thrown if the <see cref="IsDefault"/> property returns true.</exception>
        [ExcludeFromCodeCoverage]
        object IList.this[int index]
        {
            get
            {
                var self = this;
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
        [ExcludeFromCodeCoverage]
        void ICollection.CopyTo(Array array, int index)
        {
            var self = this;
            self.ThrowInvalidOperationIfNotInitialized();
            Array.Copy(self.array, 0, array, index, self.Length);
        }

        /// <summary>
        /// Determines whether an object is structurally equal to the current instance.
        /// </summary>
        /// <param name="other">The object to compare with the current instance.</param>
        /// <param name="comparer">An object that determines whether the current instance and other are equal.</param>
        /// <returns>true if the two objects are equal; otherwise, false.</returns>
        bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
        {
            var self = this;
            Array otherArray = other as Array;
            if (otherArray == null)
            {
                var theirs = other as IImmutableArray;
                if (theirs != null)
                {
                    if (self.array == null && theirs.Array == null)
                    {
                        return true;
                    }
                    else if (self.array == null)
                    {
                        return false;
                    }

                    otherArray = theirs.Array;
                }
            }

            IStructuralEquatable ours = self.array;
            return ours.Equals(otherArray, comparer);
        }

        /// <summary>
        /// Returns a hash code for the current instance.
        /// </summary>
        /// <param name="comparer">An object that computes the hash code of the current object.</param>
        /// <returns>The hash code for the current instance.</returns>
        int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
        {
            var self = this;
            IStructuralEquatable ours = self.array;
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
        int IStructuralComparable.CompareTo(object other, IComparer comparer)
        {
            var self = this;
            Array otherArray = other as Array;
            if (otherArray == null)
            {
                var theirs = other as IImmutableArray;
                if (theirs != null)
                {
                    if (self.array == null && theirs.Array == null)
                    {
                        return 0;
                    }
                    else if (self.array == null ^ theirs.Array == null)
                    {
                        throw new ArgumentException(SR.ArrayInitializedStateNotEqual, nameof(other));
                    }

                    otherArray = theirs.Array;
                }
            }

            if (otherArray != null)
            {
                IStructuralComparable ours = self.array;
                return ours.CompareTo(otherArray, comparer);
            }

            throw new ArgumentException(SR.ArrayLengthsNotEqual, nameof(other));
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
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if the <see cref="array"/> field is null, i.e. the
        /// <see cref="IsDefault"/> property returns true.  The
        /// <see cref="InvalidOperationException"/> message specifies that the operation cannot be performed
        /// on a default instance of <see cref="ImmutableArray{T}"/>.
        ///
        /// This is intended for explicitly implemented interface method and property implementations.
        /// </summary>
        private void ThrowInvalidOperationIfNotInitialized()
        {
            if (this.IsDefault)
            {
                throw new InvalidOperationException(SR.InvalidOperationOnDefaultArray);
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
            var self = this;
            self.ThrowNullRefIfNotInitialized();
            Requires.NotNull(indexesToRemove, nameof(indexesToRemove));

            if (indexesToRemove.Count == 0)
            {
                // Be sure to return a !IsDefault instance.
                return self;
            }

            var newArray = new T[self.Length - indexesToRemove.Count];
            int copied = 0;
            int removed = 0;
            int lastIndexRemoved = -1;
            foreach (var indexToRemove in indexesToRemove)
            {
                int copyLength = lastIndexRemoved == -1 ? indexToRemove : (indexToRemove - lastIndexRemoved - 1);
                Debug.Assert(indexToRemove > lastIndexRemoved); // We require that the input be a sorted set.
                Array.Copy(self.array, copied + removed, newArray, copied, copyLength);
                removed++;
                copied += copyLength;
                lastIndexRemoved = indexToRemove;
            }

            Array.Copy(self.array, copied + removed, newArray, copied, self.Length - (copied + removed));

            return new ImmutableArray<T>(newArray);
        }

        /// <summary>
        /// Throws a <see cref="NullReferenceException"/> if the specified array is uninitialized.
        /// </summary>
        private static void ThrowNullRefIfNotInitialized(ImmutableArray<T> array)
        {
            array.ThrowNullRefIfNotInitialized();
        }
    }
}
