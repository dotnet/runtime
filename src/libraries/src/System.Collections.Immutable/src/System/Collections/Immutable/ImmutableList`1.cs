// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

namespace System.Collections.Immutable
{
    /// <summary>
    /// An immutable list implementation.
    /// </summary>
    /// <typeparam name="T">The type of elements in the set.</typeparam>
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(ImmutableListDebuggerProxy<>))]
    public sealed partial class ImmutableList<T> : IImmutableList<T>, IList<T>, IList, IOrderedCollection<T>, IImmutableListQueries<T>, IStrongEnumerable<T, ImmutableList<T>.Enumerator>
    {
        /// <summary>
        /// An empty immutable list.
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly ImmutableList<T> Empty = new ImmutableList<T>();

        /// <summary>
        /// The root node of the AVL tree that stores this set.
        /// </summary>
        private readonly Node _root;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableList{T}"/> class.
        /// </summary>
        internal ImmutableList()
        {
            _root = Node.EmptyNode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableList{T}"/> class.
        /// </summary>
        /// <param name="root">The root of the AVL tree with the contents of this set.</param>
        private ImmutableList(Node root)
        {
            Requires.NotNull(root, "root");

            root.Freeze();
            _root = root;
        }

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        public ImmutableList<T> Clear()
        {
            Contract.Ensures(Contract.Result<ImmutableList<T>>() != null);
            Contract.Ensures(Contract.Result<ImmutableList<T>>().IsEmpty);
            return Empty;
        }

        /// <summary>
        /// Searches the entire sorted <see cref="ImmutableList{T}"/> for an element
        /// using the default comparer and returns the zero-based index of the element.
        /// </summary>
        /// <param name="item">The object to locate. The value can be null for reference types.</param>
        /// <returns>
        /// The zero-based index of item in the sorted <see cref="ImmutableList{T}"/>,
        /// if item is found; otherwise, a negative number that is the bitwise complement
        /// of the index of the next element that is larger than item or, if there is
        /// no larger element, the bitwise complement of <see cref="ImmutableList{T}.Count"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The default comparer <see cref="Comparer{T}.Default"/> cannot
        /// find an implementation of the <see cref="IComparable{T}"/> generic interface or
        /// the <see cref="IComparable"/> interface for type <typeparamref name="T"/>.
        /// </exception>
        public int BinarySearch(T item)
        {
            return this.BinarySearch(item, null);
        }

        /// <summary>
        ///  Searches the entire sorted <see cref="ImmutableList{T}"/> for an element
        ///  using the specified comparer and returns the zero-based index of the element.
        /// </summary>
        /// <param name="item">The object to locate. The value can be null for reference types.</param>
        /// <param name="comparer">
        /// The <see cref="IComparer{T}"/> implementation to use when comparing
        /// elements.-or-null to use the default comparer <see cref="Comparer{T}.Default"/>.
        /// </param>
        /// <returns>
        /// The zero-based index of item in the sorted <see cref="ImmutableList{T}"/>,
        /// if item is found; otherwise, a negative number that is the bitwise complement
        /// of the index of the next element that is larger than item or, if there is
        /// no larger element, the bitwise complement of <see cref="ImmutableList{T}.Count"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="comparer"/> is null, and the default comparer <see cref="Comparer{T}.Default"/>
        /// cannot find an implementation of the <see cref="IComparable{T}"/> generic interface
        /// or the <see cref="IComparable"/> interface for type <typeparamref name="T"/>.
        /// </exception>
        public int BinarySearch(T item, IComparer<T> comparer)
        {
            return this.BinarySearch(0, this.Count, item, comparer);
        }

        /// <summary>
        /// Searches a range of elements in the sorted <see cref="ImmutableList{T}"/>
        /// for an element using the specified comparer and returns the zero-based index
        /// of the element.
        /// </summary>
        /// <param name="index">The zero-based starting index of the range to search.</param>
        /// <param name="count"> The length of the range to search.</param>
        /// <param name="item">The object to locate. The value can be null for reference types.</param>
        /// <param name="comparer">
        /// The <see cref="IComparer{T}"/> implementation to use when comparing
        /// elements, or null to use the default comparer <see cref="Comparer{T}.Default"/>.
        /// </param>
        /// <returns>
        /// The zero-based index of item in the sorted <see cref="ImmutableList{T}"/>,
        /// if item is found; otherwise, a negative number that is the bitwise complement
        /// of the index of the next element that is larger than item or, if there is
        /// no larger element, the bitwise complement of <see cref="ImmutableList{T}.Count"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index"/> is less than 0.-or-<paramref name="count"/> is less than 0.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="index"/> and <paramref name="count"/> do not denote a valid range in the <see cref="ImmutableList{T}"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="comparer"/> is null, and the default comparer <see cref="Comparer{T}.Default"/>
        /// cannot find an implementation of the <see cref="IComparable{T}"/> generic interface
        /// or the <see cref="IComparable"/> interface for type <typeparamref name="T"/>.
        /// </exception>
        public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
        {
            return _root.BinarySearch(index, count, item, comparer);
        }

        #region IImmutableList<T> Properties

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsEmpty
        {
            get
            {
                Contract.Ensures(Contract.Result<bool>() == (this.Count == 0));
                return _root.IsEmpty;
            }
        }

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        IImmutableList<T> IImmutableList<T>.Clear()
        {
            return this.Clear();
        }

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        public int Count
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);
                Contract.Ensures((Contract.Result<int>() == 0) == this.IsEmpty);
                return _root.Count;
            }
        }

        #endregion

        #region ICollection Properties

        /// <summary>
        /// See <see cref="ICollection"/>.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        object ICollection.SyncRoot
        {
            get { return this; }
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

        #endregion

        #region IImmutableList<T> Indexers

        /// <summary>
        /// Gets the element of the set at the given index.
        /// </summary>
        /// <param name="index">The 0-based index of the element in the set to return.</param>
        /// <returns>The element at the given position.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown from getter when <paramref name="index"/> is negative or not less than <see cref="Count"/>.</exception>
        public T this[int index]
        {
            get
            {
                return _root[index];
            }
        }

        #endregion

        #region IOrderedCollection<T> Indexers

        /// <summary>
        /// Gets the element in the collection at a given index.
        /// </summary>
        T IOrderedCollection<T>.this[int index]
        {
            get
            {
                return this[index];
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Creates a collection with the same contents as this collection that
        /// can be efficiently mutated across multiple operations using standard
        /// mutable interfaces.
        /// </summary>
        /// <remarks>
        /// This is an O(1) operation and results in only a single (small) memory allocation.
        /// The mutable collection that is returned is *not* thread-safe.
        /// </remarks>
        [Pure]
        public Builder ToBuilder()
        {
            // We must not cache the instance created here and return it to various callers.
            // Those who request a mutable collection must get references to the collection
            // that version independently of each other.
            return new Builder(this);
        }

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        [Pure]
        public ImmutableList<T> Add(T value)
        {
            Contract.Ensures(Contract.Result<ImmutableList<T>>() != null);
            Contract.Ensures(Contract.Result<ImmutableList<T>>().Count == this.Count + 1);
            var result = _root.Add(value);
            return this.Wrap(result);
        }

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        [Pure]
        public ImmutableList<T> AddRange(IEnumerable<T> items)
        {
            Requires.NotNull(items, "items");
            Contract.Ensures(Contract.Result<ImmutableList<T>>() != null);
            Contract.Ensures(Contract.Result<ImmutableList<T>>().Count >= this.Count);

            // Some optimizations may apply if we're an empty list.
            if (this.IsEmpty)
            {
                return this.FillFromEmpty(items);
            }

            var result = _root.AddRange(items);

            return this.Wrap(result);
        }

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        [Pure]
        public ImmutableList<T> Insert(int index, T item)
        {
            Requires.Range(index >= 0 && index <= this.Count, "index");
            Contract.Ensures(Contract.Result<ImmutableList<T>>() != null);
            Contract.Ensures(Contract.Result<ImmutableList<T>>().Count == this.Count + 1);
            return this.Wrap(_root.Insert(index, item));
        }

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        [Pure]
        public ImmutableList<T> InsertRange(int index, IEnumerable<T> items)
        {
            Requires.Range(index >= 0 && index <= this.Count, "index");
            Requires.NotNull(items, "items");
            Contract.Ensures(Contract.Result<ImmutableList<T>>() != null);

            var result = _root.InsertRange(index, items);

            return this.Wrap(result);
        }

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        [Pure]
        public ImmutableList<T> Remove(T value)
        {
            return this.Remove(value, EqualityComparer<T>.Default);
        }

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        [Pure]
        public ImmutableList<T> Remove(T value, IEqualityComparer<T> equalityComparer)
        {
            Contract.Ensures(Contract.Result<ImmutableList<T>>() != null);
            int index = this.IndexOf(value, equalityComparer);
            return index < 0 ? this : this.RemoveAt(index);
        }

        /// <summary>
        /// Removes the specified values from this list.
        /// </summary>
        /// <param name="index">The starting index to begin removal.</param>
        /// <param name="count">The number of elements to remove.</param>
        /// <returns>A new list with the elements removed.</returns>
        [Pure]
        public ImmutableList<T> RemoveRange(int index, int count)
        {
            Requires.Range(index >= 0 && index <= this.Count, "index");
            Requires.Range(count >= 0 && index + count <= this.Count, "count");
            Contract.Ensures(Contract.Result<ImmutableList<T>>() != null);

            var result = _root;
            int remaining = count;
            while (remaining-- > 0)
            {
                result = result.RemoveAt(index);
            }

            return this.Wrap(result);
        }

        /// <summary>
        /// Removes the specified values from this list.
        /// </summary>
        /// <param name="items">The items to remove if matches are found in this list.</param>
        /// <returns>
        /// A new list with the elements removed.
        /// </returns>
        [Pure]
        public ImmutableList<T> RemoveRange(IEnumerable<T> items)
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
        public ImmutableList<T> RemoveRange(IEnumerable<T> items, IEqualityComparer<T> equalityComparer)
        {
            Requires.NotNull(items, "items");
            Contract.Ensures(Contract.Result<ImmutableList<T>>() != null);
            Contract.Ensures(Contract.Result<ImmutableList<T>>().Count <= this.Count);

            // Some optimizations may apply if we're an empty list.
            if (this.IsEmpty)
            {
                return this;
            }

            // Let's not implement in terms of ImmutableList.Remove so that we're
            // not unnecessarily generating a new list object for each item.
            var result = _root;
            foreach (T item in items.GetEnumerableDisposable<T, Enumerator>())
            {
                int index = result.IndexOf(item, equalityComparer);
                if (index >= 0)
                {
                    result = result.RemoveAt(index);
                }
            }

            return this.Wrap(result);
        }

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        [Pure]
        public ImmutableList<T> RemoveAt(int index)
        {
            Requires.Range(index >= 0 && index < this.Count, "index");
            Contract.Ensures(Contract.Result<ImmutableList<T>>() != null);
            Contract.Ensures(Contract.Result<ImmutableList<T>>().Count == this.Count - 1);
            var result = _root.RemoveAt(index);
            return this.Wrap(result);
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
        public ImmutableList<T> RemoveAll(Predicate<T> match)
        {
            Requires.NotNull(match, "match");
            Contract.Ensures(Contract.Result<ImmutableList<T>>() != null);

            return this.Wrap(_root.RemoveAll(match));
        }

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        [Pure]
        public ImmutableList<T> SetItem(int index, T value)
        {
            return this.Wrap(_root.ReplaceAt(index, value));
        }

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        [Pure]
        public ImmutableList<T> Replace(T oldValue, T newValue)
        {
            return this.Replace(oldValue, newValue, EqualityComparer<T>.Default);
        }

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        [Pure]
        public ImmutableList<T> Replace(T oldValue, T newValue, IEqualityComparer<T> equalityComparer)
        {
            Contract.Ensures(Contract.Result<ImmutableList<T>>() != null);
            Contract.Ensures(Contract.Result<ImmutableList<T>>().Count == this.Count);

            int index = this.IndexOf(oldValue, equalityComparer);
            if (index < 0)
            {
                throw new ArgumentException(SR.CannotFindOldValue, nameof(oldValue));
            }

            return this.SetItem(index, newValue);
        }

        /// <summary>
        /// Reverses the order of the elements in the entire <see cref="ImmutableList{T}"/>.
        /// </summary>
        /// <returns>The reversed list.</returns>
        [Pure]
        public ImmutableList<T> Reverse()
        {
            Contract.Ensures(Contract.Result<ImmutableList<T>>() != null);
            return this.Wrap(_root.Reverse());
        }

        /// <summary>
        /// Reverses the order of the elements in the specified range.
        /// </summary>
        /// <param name="index">The zero-based starting index of the range to reverse.</param>
        /// <param name="count">The number of elements in the range to reverse.</param> 
        /// <returns>The reversed list.</returns>
        [Pure]
        public ImmutableList<T> Reverse(int index, int count)
        {
            return this.Wrap(_root.Reverse(index, count));
        }

        /// <summary>
        /// Sorts the elements in the entire <see cref="ImmutableList{T}"/> using
        /// the default comparer.
        /// </summary>
        [Pure]
        public ImmutableList<T> Sort()
        {
            Contract.Ensures(Contract.Result<ImmutableList<T>>() != null);
            return this.Wrap(_root.Sort());
        }

        /// <summary>
        /// Sorts the elements in the entire <see cref="ImmutableList{T}"/> using
        /// the specified <see cref="Comparison{T}"/>.
        /// </summary>
        /// <param name="comparison">
        /// The <see cref="Comparison{T}"/> to use when comparing elements.
        /// </param>
        /// <returns>The sorted list.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparison"/> is null.</exception>
        [Pure]
        public ImmutableList<T> Sort(Comparison<T> comparison)
        {
            Requires.NotNull(comparison, "comparison");
            Contract.Ensures(Contract.Result<ImmutableList<T>>() != null);
            return this.Wrap(_root.Sort(comparison));
        }

        /// <summary>
        /// Sorts the elements in the entire <see cref="ImmutableList{T}"/> using
        /// the specified comparer.
        /// </summary>
        /// <param name="comparer">
        /// The <see cref="IComparer{T}"/> implementation to use when comparing
        /// elements, or null to use the default comparer <see cref="Comparer{T}.Default"/>.
        /// </param>
        /// <returns>The sorted list.</returns>
        [Pure]
        public ImmutableList<T> Sort(IComparer<T> comparer)
        {
            Contract.Ensures(Contract.Result<ImmutableList<T>>() != null);
            return this.Wrap(_root.Sort(comparer));
        }

        /// <summary>
        /// Sorts the elements in a range of elements in <see cref="ImmutableList{T}"/>
        /// using the specified comparer.
        /// </summary>
        /// <param name="index">
        /// The zero-based starting index of the range to sort.
        /// </param>
        /// <param name="count">
        /// The length of the range to sort.
        /// </param>
        /// <param name="comparer">
        /// The <see cref="IComparer{T}"/> implementation to use when comparing
        /// elements, or null to use the default comparer <see cref="Comparer{T}.Default"/>.
        /// </param>
        /// <returns>The sorted list.</returns>
        [Pure]
        public ImmutableList<T> Sort(int index, int count, IComparer<T> comparer)
        {
            Requires.Range(index >= 0, "index");
            Requires.Range(count >= 0, "count");
            Requires.Range(index + count <= this.Count, "count");
            Contract.Ensures(Contract.Result<ImmutableList<T>>() != null);

            return this.Wrap(_root.Sort(index, count, comparer));
        }

        #endregion

        #region IImmutableListQueries<T> Methods

        /// <summary>
        /// Performs the specified action on each element of the list.
        /// </summary>
        /// <param name="action">The System.Action&lt;T&gt; delegate to perform on each element of the list.</param>
        public void ForEach(Action<T> action)
        {
            Requires.NotNull(action, "action");

            foreach (T item in this)
            {
                action(item);
            }
        }

        /// <summary>
        /// Copies the entire <see cref="ImmutableList{T}"/> to a compatible one-dimensional
        /// array, starting at the beginning of the target array.
        /// </summary>
        /// <param name="array">
        /// The one-dimensional <see cref="Array"/> that is the destination of the elements
        /// copied from <see cref="ImmutableList{T}"/>. The <see cref="Array"/> must have
        /// zero-based indexing.
        /// </param>
        public void CopyTo(T[] array)
        {
            Requires.NotNull(array, "array");
            Requires.Range(array.Length >= this.Count, "array");
            _root.CopyTo(array);
        }

        /// <summary>
        /// Copies the entire <see cref="ImmutableList{T}"/> to a compatible one-dimensional
        /// array, starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">
        /// The one-dimensional <see cref="Array"/> that is the destination of the elements
        /// copied from <see cref="ImmutableList{T}"/>. The <see cref="Array"/> must have
        /// zero-based indexing.
        /// </param>
        /// <param name="arrayIndex">
        /// The zero-based index in array at which copying begins.
        /// </param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            Requires.NotNull(array, "array");
            Requires.Range(arrayIndex >= 0, "arrayIndex");
            Requires.Range(array.Length >= arrayIndex + this.Count, "arrayIndex");
            _root.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Copies a range of elements from the <see cref="ImmutableList{T}"/> to
        /// a compatible one-dimensional array, starting at the specified index of the
        /// target array.
        /// </summary>
        /// <param name="index">
        /// The zero-based index in the source <see cref="ImmutableList{T}"/> at
        /// which copying begins.
        /// </param>
        /// <param name="array">
        /// The one-dimensional <see cref="Array"/> that is the destination of the elements
        /// copied from <see cref="ImmutableList{T}"/>. The <see cref="Array"/> must have
        /// zero-based indexing.
        /// </param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        /// <param name="count">The number of elements to copy.</param>
        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            _root.CopyTo(index, array, arrayIndex, count);
        }

        /// <summary>
        /// Creates a shallow copy of a range of elements in the source <see cref="ImmutableList{T}"/>.
        /// </summary>
        /// <param name="index">
        /// The zero-based <see cref="ImmutableList{T}"/> index at which the range
        /// starts.
        /// </param>
        /// <param name="count">
        /// The number of elements in the range.
        /// </param>
        /// <returns>
        /// A shallow copy of a range of elements in the source <see cref="ImmutableList{T}"/>.
        /// </returns>
        public ImmutableList<T> GetRange(int index, int count)
        {
            Requires.Range(index >= 0, "index");
            Requires.Range(count >= 0, "count");
            Requires.Range(index + count <= this.Count, "count");
            return this.Wrap(Node.NodeTreeFromList(this, index, count));
        }

        /// <summary>
        /// Converts the elements in the current <see cref="ImmutableList{T}"/> to
        /// another type, and returns a list containing the converted elements.
        /// </summary>
        /// <param name="converter">
        /// A <see cref="Func{T, TResult}"/> delegate that converts each element from
        /// one type to another type.
        /// </param>
        /// <typeparam name="TOutput">
        /// The type of the elements of the target array.
        /// </typeparam>
        /// <returns>
        /// A <see cref="ImmutableList{T}"/> of the target type containing the converted
        /// elements from the current <see cref="ImmutableList{T}"/>.
        /// </returns>
        public ImmutableList<TOutput> ConvertAll<TOutput>(Func<T, TOutput> converter)
        {
            Requires.NotNull(converter, "converter");
            return ImmutableList<TOutput>.WrapNode(_root.ConvertAll(converter));
        }

        /// <summary>
        /// Determines whether the <see cref="ImmutableList{T}"/> contains elements
        /// that match the conditions defined by the specified predicate.
        /// </summary>
        /// <param name="match">
        /// The <see cref="Predicate{T}"/> delegate that defines the conditions of the elements
        /// to search for.
        /// </param>
        /// <returns>
        /// true if the <see cref="ImmutableList{T}"/> contains one or more elements
        /// that match the conditions defined by the specified predicate; otherwise,
        /// false.
        /// </returns>
        public bool Exists(Predicate<T> match)
        {
            Requires.NotNull(match, "match");
            return _root.Exists(match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the first occurrence within the entire <see cref="ImmutableList{T}"/>.
        /// </summary>
        /// <param name="match">
        /// The <see cref="Predicate{T}"/> delegate that defines the conditions of the element
        /// to search for.
        /// </param>
        /// <returns>
        /// The first element that matches the conditions defined by the specified predicate,
        /// if found; otherwise, the default value for type <typeparamref name="T"/>.
        /// </returns>
        public T Find(Predicate<T> match)
        {
            Requires.NotNull(match, "match");
            return _root.Find(match);
        }

        /// <summary>
        /// Retrieves all the elements that match the conditions defined by the specified
        /// predicate.
        /// </summary>
        /// <param name="match">
        /// The <see cref="Predicate{T}"/> delegate that defines the conditions of the elements
        /// to search for.
        /// </param>
        /// <returns>
        /// A <see cref="ImmutableList{T}"/> containing all the elements that match
        /// the conditions defined by the specified predicate, if found; otherwise, an
        /// empty <see cref="ImmutableList{T}"/>.
        /// </returns>
        public ImmutableList<T> FindAll(Predicate<T> match)
        {
            Requires.NotNull(match, "match");
            return _root.FindAll(match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the zero-based index of the first occurrence within
        /// the entire <see cref="ImmutableList{T}"/>.
        /// </summary>
        /// <param name="match">
        /// The <see cref="Predicate{T}"/> delegate that defines the conditions of the element
        /// to search for.
        /// </param>
        /// <returns>
        /// The zero-based index of the first occurrence of an element that matches the
        /// conditions defined by <paramref name="match"/>, if found; otherwise, -1.
        /// </returns>
        public int FindIndex(Predicate<T> match)
        {
            Requires.NotNull(match, "match");
            return _root.FindIndex(match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the zero-based index of the first occurrence within
        /// the range of elements in the <see cref="ImmutableList{T}"/> that extends
        /// from the specified index to the last element.
        /// </summary>
        /// <param name="startIndex">The zero-based starting index of the search.</param>
        /// <param name="match">The <see cref="Predicate{T}"/> delegate that defines the conditions of the element to search for.</param>
        /// <returns>
        /// The zero-based index of the first occurrence of an element that matches the
        /// conditions defined by <paramref name="match"/>, if found; otherwise, -1.
        /// </returns>
        public int FindIndex(int startIndex, Predicate<T> match)
        {
            Requires.NotNull(match, "match");
            Requires.Range(startIndex >= 0, "startIndex");
            Requires.Range(startIndex <= this.Count, "startIndex");
            return _root.FindIndex(startIndex, match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the zero-based index of the first occurrence within
        /// the range of elements in the <see cref="ImmutableList{T}"/> that starts
        /// at the specified index and contains the specified number of elements.
        /// </summary>
        /// <param name="startIndex">The zero-based starting index of the search.</param>
        /// <param name="count">The number of elements in the section to search.</param>
        /// <param name="match">The <see cref="Predicate{T}"/> delegate that defines the conditions of the element to search for.</param>
        /// <returns>
        /// The zero-based index of the first occurrence of an element that matches the
        /// conditions defined by <paramref name="match"/>, if found; otherwise, -1.
        /// </returns>
        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            Requires.NotNull(match, "match");
            Requires.Range(startIndex >= 0, "startIndex");
            Requires.Range(count >= 0, "count");
            Requires.Range(startIndex + count <= this.Count, "count");

            return _root.FindIndex(startIndex, count, match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the last occurrence within the entire <see cref="ImmutableList{T}"/>.
        /// </summary>
        /// <param name="match">
        /// The <see cref="Predicate{T}"/> delegate that defines the conditions of the element
        /// to search for.
        /// </param>
        /// <returns>
        /// The last element that matches the conditions defined by the specified predicate,
        /// if found; otherwise, the default value for type <typeparamref name="T"/>.
        /// </returns>
        public T FindLast(Predicate<T> match)
        {
            Requires.NotNull(match, "match");
            return _root.FindLast(match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the zero-based index of the last occurrence within
        /// the entire <see cref="ImmutableList{T}"/>.
        /// </summary>
        /// <param name="match">
        /// The <see cref="Predicate{T}"/> delegate that defines the conditions of the element
        /// to search for.
        /// </param>
        /// <returns>
        /// The zero-based index of the last occurrence of an element that matches the
        /// conditions defined by <paramref name="match"/>, if found; otherwise, -1.
        /// </returns>
        public int FindLastIndex(Predicate<T> match)
        {
            Requires.NotNull(match, "match");
            return _root.FindLastIndex(match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the zero-based index of the last occurrence within
        /// the range of elements in the <see cref="ImmutableList{T}"/> that extends
        /// from the first element to the specified index.
        /// </summary>
        /// <param name="startIndex">The zero-based starting index of the backward search.</param>
        /// <param name="match">The <see cref="Predicate{T}"/> delegate that defines the conditions of the element
        /// to search for.</param>
        /// <returns>
        /// The zero-based index of the last occurrence of an element that matches the
        /// conditions defined by <paramref name="match"/>, if found; otherwise, -1.
        /// </returns>
        public int FindLastIndex(int startIndex, Predicate<T> match)
        {
            Requires.NotNull(match, "match");
            Requires.Range(startIndex >= 0, "startIndex");
            Requires.Range(startIndex == 0 || startIndex < this.Count, "startIndex");
            return _root.FindLastIndex(startIndex, match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the zero-based index of the last occurrence within
        /// the range of elements in the <see cref="ImmutableList{T}"/> that contains
        /// the specified number of elements and ends at the specified index.
        /// </summary>
        /// <param name="startIndex">The zero-based starting index of the backward search.</param>
        /// <param name="count">The number of elements in the section to search.</param>
        /// <param name="match">
        /// The <see cref="Predicate{T}"/> delegate that defines the conditions of the element
        /// to search for.
        /// </param>
        /// <returns>
        /// The zero-based index of the last occurrence of an element that matches the
        /// conditions defined by <paramref name="match"/>, if found; otherwise, -1.
        /// </returns>
        public int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            Requires.NotNull(match, "match");
            Requires.Range(startIndex >= 0, "startIndex");
            Requires.Range(count <= this.Count, "count");
            Requires.Range(startIndex - count + 1 >= 0, "startIndex");

            return _root.FindLastIndex(startIndex, count, match);
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the
        /// first occurrence within the range of elements in the <see cref="ImmutableList{T}"/>
        /// that starts at the specified index and contains the specified number of elements.
        /// </summary>
        /// <param name="item">
        /// The object to locate in the <see cref="ImmutableList{T}"/>. The value
        /// can be null for reference types.
        /// </param>
        /// <param name="index">
        /// The zero-based starting index of the search. 0 (zero) is valid in an empty
        /// list.
        /// </param>
        /// <param name="count">
        /// The number of elements in the section to search.
        /// </param>
        /// <param name="equalityComparer">
        /// The equality comparer to use in the search.
        /// </param>
        /// <returns>
        /// The zero-based index of the first occurrence of <paramref name="item"/> within the range of
        /// elements in the <see cref="ImmutableList{T}"/> that starts at <paramref name="index"/> and
        /// contains <paramref name="count"/> number of elements, if found; otherwise, -1.
        /// </returns>
        [Pure]
        public int IndexOf(T item, int index, int count, IEqualityComparer<T> equalityComparer)
        {
            return _root.IndexOf(item, index, count, equalityComparer);
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the
        /// last occurrence within the range of elements in the <see cref="ImmutableList{T}"/>
        /// that contains the specified number of elements and ends at the specified
        /// index.
        /// </summary>
        /// <param name="item">
        /// The object to locate in the <see cref="ImmutableList{T}"/>. The value
        /// can be null for reference types.
        /// </param>
        /// <param name="index">The zero-based starting index of the backward search.</param>
        /// <param name="count">The number of elements in the section to search.</param>
        /// <param name="equalityComparer">
        /// The equality comparer to use in the search.
        /// </param>
        /// <returns>
        /// The zero-based index of the last occurrence of <paramref name="item"/> within the range of elements
        /// in the <see cref="ImmutableList{T}"/> that contains <paramref name="count"/> number of elements
        /// and ends at <paramref name="index"/>, if found; otherwise, -1.
        /// </returns>
        [Pure]
        public int LastIndexOf(T item, int index, int count, IEqualityComparer<T> equalityComparer)
        {
            return _root.LastIndexOf(item, index, count, equalityComparer);
        }

        /// <summary>
        /// Determines whether every element in the <see cref="ImmutableList{T}"/>
        /// matches the conditions defined by the specified predicate.
        /// </summary>
        /// <param name="match">
        /// The <see cref="Predicate{T}"/> delegate that defines the conditions to check against
        /// the elements.
        /// </param>
        /// <returns>
        /// true if every element in the <see cref="ImmutableList{T}"/> matches the
        /// conditions defined by the specified predicate; otherwise, false. If the list
        /// has no elements, the return value is true.
        /// </returns>
        public bool TrueForAll(Predicate<T> match)
        {
            Requires.NotNull(match, "match");
            return _root.TrueForAll(match);
        }

        #endregion

        #region IImmutableList<T> Methods

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        public bool Contains(T value)
        {
            Contract.Ensures(!this.IsEmpty || !Contract.Result<bool>());
            return this.IndexOf(value) >= 0;
        }

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        public int IndexOf(T value)
        {
            return this.IndexOf(value, EqualityComparer<T>.Default);
        }

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableList<T> IImmutableList<T>.Add(T value)
        {
            return this.Add(value);
        }

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableList<T> IImmutableList<T>.AddRange(IEnumerable<T> items)
        {
            return this.AddRange(items);
        }

        /// <summary>
        /// Inserts the specified value at the specified index.
        /// </summary>
        /// <param name="index">The index at which to insert the value.</param>
        /// <param name="item">The element to add.</param>
        /// <returns>The new immutable list.</returns>
        [ExcludeFromCodeCoverage]
        IImmutableList<T> IImmutableList<T>.Insert(int index, T item)
        {
            return this.Insert(index, item);
        }

        /// <summary>
        /// Inserts the specified value at the specified index.
        /// </summary>
        /// <param name="index">The index at which to insert the value.</param>
        /// <param name="items">The elements to add.</param>
        /// <returns>The new immutable list.</returns>
        [ExcludeFromCodeCoverage]
        IImmutableList<T> IImmutableList<T>.InsertRange(int index, IEnumerable<T> items)
        {
            return this.InsertRange(index, items);
        }

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableList<T> IImmutableList<T>.Remove(T value, IEqualityComparer<T> equalityComparer)
        {
            return this.Remove(value, equalityComparer);
        }

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableList<T> IImmutableList<T>.RemoveAll(Predicate<T> match)
        {
            return this.RemoveAll(match);
        }

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableList<T> IImmutableList<T>.RemoveRange(IEnumerable<T> items, IEqualityComparer<T> equalityComparer)
        {
            return this.RemoveRange(items, equalityComparer);
        }

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableList<T> IImmutableList<T>.RemoveRange(int index, int count)
        {
            return this.RemoveRange(index, count);
        }

        /// <summary>
        /// Removes the element at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>A new list with the elements removed.</returns>
        [ExcludeFromCodeCoverage]
        IImmutableList<T> IImmutableList<T>.RemoveAt(int index)
        {
            return this.RemoveAt(index);
        }

        /// <summary>
        /// Replaces an element in the list at a given position with the specified element.
        /// </summary>
        /// <param name="index">The position in the list of the element to replace.</param>
        /// <param name="value">The element to replace the old element with.</param>
        /// <returns>The new list.</returns>
        [ExcludeFromCodeCoverage]
        IImmutableList<T> IImmutableList<T>.SetItem(int index, T value)
        {
            return this.SetItem(index, value);
        }

        /// <summary>
        /// Replaces an element in the list with the specified element.
        /// </summary>
        /// <param name="oldValue">The element to replace.</param>
        /// <param name="newValue">The element to replace the old element with.</param>
        /// <param name="equalityComparer">
        /// The equality comparer to use in the search.
        /// If <c>null</c>, <see cref="EqualityComparer{T}.Default"/> is used.
        /// </param>
        /// <returns>The new list.</returns>
        /// <exception cref="ArgumentException">Thrown when the old value does not exist in the list.</exception>
        IImmutableList<T> IImmutableList<T>.Replace(T oldValue, T newValue, IEqualityComparer<T> equalityComparer)
        {
            return this.Replace(oldValue, newValue, equalityComparer);
        }

        #endregion

        #region IEnumerable<T> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
        /// </returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        #region IList<T> Members

        /// <summary>
        /// Inserts the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        void IList<T>.Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Removes the value at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        void IList<T>.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Gets or sets the value at the specified index.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException">Thrown from getter when <paramref name="index"/> is negative or not less than <see cref="Count"/>.</exception>
        /// <exception cref="NotSupportedException">Always thrown from the setter.</exception>
        T IList<T>.this[int index]
        {
            get { return this[index]; }
            set { throw new NotSupportedException(); }
        }

        #endregion

        #region ICollection<T> Members

        /// <summary>
        /// Adds the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        void ICollection<T>.Add(T item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        void ICollection<T>.Clear()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="ICollection{T}"/> is read-only.
        /// </summary>
        /// <returns>true if the <see cref="ICollection{T}"/> is read-only; otherwise, false.
        ///   </returns>
        bool ICollection<T>.IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        /// Removes the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>Nothing. An exception is always thrown.</returns>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        bool ICollection<T>.Remove(T item)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region ICollection Methods

        /// <summary>
        /// See the <see cref="ICollection"/> interface.
        /// </summary>
        void System.Collections.ICollection.CopyTo(Array array, int arrayIndex)
        {
            _root.CopyTo(array, arrayIndex);
        }

        #endregion

        #region IList members

        /// <summary>
        /// Adds an item to the <see cref="IList"/>.
        /// </summary>
        /// <param name="value">The object to add to the <see cref="IList"/>.</param>
        /// <returns>
        /// Nothing. An exception is always thrown.
        /// </returns>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        int IList.Add(object value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Removes the <see cref="IList"/> item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        void IList.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
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
        bool IList.Contains(object value)
        {
            if (IsCompatibleObject(value))
            {
                return this.Contains((T)value);
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
        int IList.IndexOf(object value)
        {
            if (IsCompatibleObject(value))
            {
                return this.IndexOf((T)value);
            }

            return -1;
        }

        /// <summary>
        /// Inserts an item to the <see cref="IList"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="value"/> should be inserted.</param>
        /// <param name="value">The object to insert into the <see cref="IList"/>.</param>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        void IList.Insert(int index, object value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="IList"/> has a fixed size.
        /// </summary>
        /// <returns>true if the <see cref="IList"/> has a fixed size; otherwise, false.</returns>
        bool IList.IsFixedSize
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="ICollection{T}"/> is read-only.
        /// </summary>
        /// <returns>true if the <see cref="ICollection{T}"/> is read-only; otherwise, false.
        ///   </returns>
        bool IList.IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="IList"/>.
        /// </summary>
        /// <param name="value">The object to remove from the <see cref="IList"/>.</param>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        void IList.Remove(object value)
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
        /// <returns>The value at the specified index.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown from getter when <paramref name="index"/> is negative or not less than <see cref="Count"/>.</exception>
        /// <exception cref="NotSupportedException">Always thrown from the setter.</exception>
        object IList.this[int index]
        {
            get { return this[index]; }
            set { throw new NotSupportedException(); }
        }

        #endregion

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
        /// </returns>
        /// <remarks>
        /// CAUTION: when this enumerator is actually used as a valuetype (not boxed) do NOT copy it by assigning to a second variable 
        /// or by passing it to another method.  When this enumerator is disposed of it returns a mutable reference type stack to a resource pool,
        /// and if the value type enumerator is copied (which can easily happen unintentionally if you pass the value around) there is a risk
        /// that a stack that has already been returned to the resource pool may still be in use by one of the enumerator copies, leading to data
        /// corruption and/or exceptions.
        /// </remarks>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(_root);
        }

        /// <summary>
        /// Returns the root <see cref="Node"/> of the list
        /// </summary>
        internal Node Root
        {
            get
            {
                return _root;
            }
        }

        /// <summary>
        /// Creates a new sorted set wrapper for a node tree.
        /// </summary>
        /// <param name="root">The root of the collection.</param>
        /// <returns>The immutable sorted set instance.</returns>
        [Pure]
        private static ImmutableList<T> WrapNode(Node root)
        {
            return root.IsEmpty
                ? ImmutableList<T>.Empty
                : new ImmutableList<T>(root);
        }

        /// <summary>
        /// Attempts to discover an <see cref="ImmutableList{T}"/> instance beneath some enumerable sequence
        /// if one exists.
        /// </summary>
        /// <param name="sequence">The sequence that may have come from an immutable list.</param>
        /// <param name="other">Receives the concrete <see cref="ImmutableList{T}"/> typed value if one can be found.</param>
        /// <returns><c>true</c> if the cast was successful; <c>false</c> otherwise.</returns>
        private static bool TryCastToImmutableList(IEnumerable<T> sequence, out ImmutableList<T> other)
        {
            other = sequence as ImmutableList<T>;
            if (other != null)
            {
                return true;
            }

            var builder = sequence as Builder;
            if (builder != null)
            {
                other = builder.ToImmutable();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tests whether a value is one that might be found in this collection.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <returns><c>true</c> if this value might appear in the collection.</returns>
        /// <devremarks>
        /// This implementation comes from <see cref="List{T}"/>.
        /// </devremarks>
        private static bool IsCompatibleObject(object value)
        {
            // Non-null values are fine.  Only accept nulls if T is a class or Nullable<U>.
            // Note that default(T) is not equal to null for value types except when T is Nullable<U>. 
            return ((value is T) || (value == null && default(T) == null));
        }

        /// <summary>
        /// Creates a wrapping collection type around a root node.
        /// </summary>
        /// <param name="root">The root node to wrap.</param>
        /// <returns>A wrapping collection type for the new tree.</returns>
        [Pure]
        private ImmutableList<T> Wrap(Node root)
        {
            if (root != _root)
            {
                return root.IsEmpty ? this.Clear() : new ImmutableList<T>(root);
            }
            else
            {
                return this;
            }
        }

        /// <summary>
        /// Creates an immutable list with the contents from a sequence of elements.
        /// </summary>
        /// <param name="items">The sequence of elements from which to create the list.</param>
        /// <returns>The immutable list.</returns>
        [Pure]
        private ImmutableList<T> FillFromEmpty(IEnumerable<T> items)
        {
            Debug.Assert(this.IsEmpty);

            // If the items being added actually come from an ImmutableList<T>
            // then there is no value in reconstructing it.
            ImmutableList<T> other;
            if (TryCastToImmutableList(items, out other))
            {
                return other;
            }

            // Rather than build up the immutable structure in the incremental way,
            // build it in such a way as to generate minimal garbage, by assembling
            // the immutable binary tree from leaf to root.  This requires
            // that we know the length of the item sequence in advance, and can
            // index into that sequence like a list, so the one possible piece of 
            // garbage produced is a temporary array to store the list while
            // we build the tree.
            var list = items.AsOrderedCollection();
            if (list.Count == 0)
            {
                return this;
            }

            Node root = Node.NodeTreeFromList(list, 0, list.Count);
            return new ImmutableList<T>(root);
        }

        /// <summary>
        /// Enumerates the contents of a binary tree.
        /// </summary>
        /// <remarks>
        /// This struct can and should be kept in exact sync with the other binary tree enumerators: 
        /// <see cref="ImmutableList{T}.Enumerator"/>, <see cref="ImmutableSortedDictionary{TKey, TValue}.Enumerator"/>, and <see cref="ImmutableSortedSet{T}.Enumerator"/>.
        /// 
        /// CAUTION: when this enumerator is actually used as a valuetype (not boxed) do NOT copy it by assigning to a second variable 
        /// or by passing it to another method.  When this enumerator is disposed of it returns a mutable reference type stack to a resource pool,
        /// and if the value type enumerator is copied (which can easily happen unintentionally if you pass the value around) there is a risk
        /// that a stack that has already been returned to the resource pool may still be in use by one of the enumerator copies, leading to data
        /// corruption and/or exceptions.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public struct Enumerator : IEnumerator<T>, ISecurePooledObjectUser, IStrongEnumerator<T>
        {
            /// <summary>
            /// The resource pool of reusable mutable stacks for purposes of enumeration.
            /// </summary>
            /// <remarks>
            /// We utilize this resource pool to make "allocation free" enumeration achievable.
            /// </remarks>
            private static readonly SecureObjectPool<Stack<RefAsValueType<Node>>, Enumerator> s_EnumeratingStacks =
                new SecureObjectPool<Stack<RefAsValueType<Node>>, Enumerator>();

            /// <summary>
            /// The builder being enumerated, if applicable.
            /// </summary>
            private readonly Builder _builder;

            /// <summary>
            /// A unique ID for this instance of this enumerator.
            /// Used to protect pooled objects from use after they are recycled.
            /// </summary>
            private readonly int _poolUserId;

            /// <summary>
            /// The starting index of the collection at which to begin enumeration.
            /// </summary>
            private readonly int _startIndex;

            /// <summary>
            /// The number of elements to include in the enumeration.
            /// </summary>
            private readonly int _count;

            /// <summary>
            /// The number of elements left in the enumeration.
            /// </summary>
            private int _remainingCount;

            /// <summary>
            /// A value indicating whether this enumerator walks in reverse order.
            /// </summary>
            private bool _reversed;

            /// <summary>
            /// The set being enumerated.
            /// </summary>
            private Node _root;

            /// <summary>
            /// The stack to use for enumerating the binary tree.
            /// </summary>
            private SecurePooledObject<Stack<RefAsValueType<Node>>> _stack;

            /// <summary>
            /// The node currently selected.
            /// </summary>
            private Node _current;

            /// <summary>
            /// The version of the builder (when applicable) that is being enumerated.
            /// </summary>
            private int _enumeratingBuilderVersion;

            /// <summary>
            /// Initializes an <see cref="Enumerator"/> structure.
            /// </summary>
            /// <param name="root">The root of the set to be enumerated.</param>
            /// <param name="builder">The builder, if applicable.</param>
            /// <param name="startIndex">The index of the first element to enumerate.</param>
            /// <param name="count">The number of elements in this collection.</param>
            /// <param name="reversed"><c>true</c> if the list should be enumerated in reverse order.</param>
            internal Enumerator(Node root, Builder builder = null, int startIndex = -1, int count = -1, bool reversed = false)
            {
                Requires.NotNull(root, "root");
                Requires.Range(startIndex >= -1, "startIndex");
                Requires.Range(count >= -1, "count");
                Requires.Argument(reversed || count == -1 || (startIndex == -1 ? 0 : startIndex) + count <= root.Count);
                Requires.Argument(!reversed || count == -1 || (startIndex == -1 ? root.Count - 1 : startIndex) - count + 1 >= 0);

                _root = root;
                _builder = builder;
                _current = null;
                _startIndex = startIndex >= 0 ? startIndex : (reversed ? root.Count - 1 : 0);
                _count = count == -1 ? root.Count : count;
                _remainingCount = _count;
                _reversed = reversed;
                _enumeratingBuilderVersion = builder != null ? builder.Version : -1;
                _poolUserId = SecureObjectPool.NewId();
                _stack = null;
                if (_count > 0)
                {
                    if (!s_EnumeratingStacks.TryTake(this, out _stack))
                    {
                        _stack = s_EnumeratingStacks.PrepNew(this, new Stack<RefAsValueType<Node>>(root.Height));
                    }

                    this.ResetStack();
                }
            }

            /// <inheritdoc/>
            int ISecurePooledObjectUser.PoolUserId
            {
                get { return _poolUserId; }
            }

            /// <summary>
            /// The current element.
            /// </summary>
            public T Current
            {
                get
                {
                    this.ThrowIfDisposed();
                    if (_current != null)
                    {
                        return _current.Value;
                    }

                    throw new InvalidOperationException();
                }
            }

            /// <summary>
            /// The current element.
            /// </summary>
            object System.Collections.IEnumerator.Current
            {
                get { return this.Current; }
            }

            /// <summary>
            /// Disposes of this enumerator and returns the stack reference to the resource pool.
            /// </summary>
            public void Dispose()
            {
                _root = null;
                _current = null;
                Stack<RefAsValueType<Node>> stack;
                if (_stack != null && _stack.TryUse(ref this, out stack))
                {
                    stack.ClearFastWhenEmpty();
                    s_EnumeratingStacks.TryAdd(this, _stack);
                }

                _stack = null;
            }

            /// <summary>
            /// Advances enumeration to the next element.
            /// </summary>
            /// <returns>A value indicating whether there is another element in the enumeration.</returns>
            public bool MoveNext()
            {
                this.ThrowIfDisposed();
                this.ThrowIfChanged();

                if (_stack != null)
                {
                    var stack = _stack.Use(ref this);
                    if (_remainingCount > 0 && stack.Count > 0)
                    {
                        Node n = stack.Pop().Value;
                        _current = n;
                        this.PushNext(this.NextBranch(n));
                        _remainingCount--;
                        return true;
                    }
                }

                _current = null;
                return false;
            }

            /// <summary>
            /// Restarts enumeration.
            /// </summary>
            public void Reset()
            {
                this.ThrowIfDisposed();

                _enumeratingBuilderVersion = _builder != null ? _builder.Version : -1;
                _remainingCount = _count;
                if (_stack != null)
                {
                    this.ResetStack();
                }
            }

            /// <summary>Resets the stack used for enumeration.</summary>
            private void ResetStack()
            {
                var stack = _stack.Use(ref this);
                stack.ClearFastWhenEmpty();

                var node = _root;
                var skipNodes = _reversed ? _root.Count - _startIndex - 1 : _startIndex;
                while (!node.IsEmpty && skipNodes != this.PreviousBranch(node).Count)
                {
                    if (skipNodes < this.PreviousBranch(node).Count)
                    {
                        stack.Push(new RefAsValueType<Node>(node));
                        node = this.PreviousBranch(node);
                    }
                    else
                    {
                        skipNodes -= this.PreviousBranch(node).Count + 1;
                        node = this.NextBranch(node);
                    }
                }

                if (!node.IsEmpty)
                {
                    stack.Push(new RefAsValueType<Node>(node));
                }
            }

            /// <summary>
            /// Obtains the right branch of the given node (or the left, if walking in reverse).
            /// </summary>
            private Node NextBranch(Node node)
            {
                return _reversed ? node.Left : node.Right;
            }

            /// <summary>
            /// Obtains the left branch of the given node (or the right, if walking in reverse).
            /// </summary>
            private Node PreviousBranch(Node node)
            {
                return _reversed ? node.Right : node.Left;
            }

            /// <summary>
            /// Throws an <see cref="ObjectDisposedException"/> if this enumerator has been disposed.
            /// </summary>
            private void ThrowIfDisposed()
            {
                Contract.Ensures(_root != null);
                Contract.EnsuresOnThrow<ObjectDisposedException>(_root == null);

                // Since this is a struct, copies might not have been marked as disposed.
                // But the stack we share across those copies would know.
                // This trick only works when we have a non-null stack.
                // For enumerators of empty collections, there isn't any natural
                // way to know when a copy of the struct has been disposed of.

                if (_root == null || (_stack != null && !_stack.IsOwned(ref this)))
                {
                    Requires.FailObjectDisposed(this);
                }
            }

            /// <summary>
            /// Throws an exception if the underlying builder's contents have been changed since enumeration started.
            /// </summary>
            /// <exception cref="System.InvalidOperationException">Thrown if the collection has changed.</exception>
            private void ThrowIfChanged()
            {
                if (_builder != null && _builder.Version != _enumeratingBuilderVersion)
                {
                    throw new InvalidOperationException(SR.CollectionModifiedDuringEnumeration);
                }
            }

            /// <summary>
            /// Pushes this node and all its Left descendants onto the stack.
            /// </summary>
            /// <param name="node">The starting node to push onto the stack.</param>
            private void PushNext(Node node)
            {
                Requires.NotNull(node, "node");
                if (!node.IsEmpty)
                {
                    var stack = _stack.Use(ref this);
                    while (!node.IsEmpty)
                    {
                        stack.Push(new RefAsValueType<Node>(node));
                        node = this.PreviousBranch(node);
                    }
                }
            }
        }

        /// <summary>
        /// A node in the AVL tree storing this set.
        /// </summary>
        [DebuggerDisplay("{_key}")]
        internal sealed class Node : IBinaryTree<T>, IEnumerable<T>
        {
            /// <summary>
            /// The default empty node.
            /// </summary>
            internal static readonly Node EmptyNode = new Node();

            /// <summary>
            /// The key associated with this node.
            /// </summary>
            private T _key;

            /// <summary>
            /// A value indicating whether this node has been frozen (made immutable).
            /// </summary>
            /// <remarks>
            /// Nodes must be frozen before ever being observed by a wrapping collection type
            /// to protect collections from further mutations.
            /// </remarks>
            private bool _frozen;

            /// <summary>
            /// The depth of the tree beneath this node.
            /// </summary>
            private byte _height; // AVL tree max height <= ~1.44 * log2(maxNodes + 2)

            /// <summary>
            /// The number of elements contained by this subtree starting at this node.
            /// </summary>
            /// <remarks>
            /// If this node would benefit from saving 4 bytes, we could have only a few nodes 
            /// scattered throughout the graph actually record the count of nodes beneath them.
            /// Those without the count could query their descendants, which would often short-circuit
            /// when they hit a node that *does* include a count field.
            /// </remarks>
            private int _count;

            /// <summary>
            /// The left tree.
            /// </summary>
            private Node _left;

            /// <summary>
            /// The right tree.
            /// </summary>
            private Node _right;

            /// <summary>
            /// Initializes a new instance of the <see cref="ImmutableList{T}.Node"/> class
            /// that is pre-frozen.
            /// </summary>
            private Node()
            {
                Contract.Ensures(this.IsEmpty);
                _frozen = true; // the empty node is *always* frozen.
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ImmutableList{T}.Node"/> class
            /// that is not yet frozen.
            /// </summary>
            /// <param name="key">The value stored by this node.</param>
            /// <param name="left">The left branch.</param>
            /// <param name="right">The right branch.</param>
            /// <param name="frozen">Whether this node is prefrozen.</param>
            private Node(T key, Node left, Node right, bool frozen = false)
            {
                Requires.NotNull(left, "left");
                Requires.NotNull(right, "right");
                Debug.Assert(!frozen || (left._frozen && right._frozen));
                Contract.Ensures(!this.IsEmpty);

                _key = key;
                _left = left;
                _right = right;
                _height = checked((byte)(1 + Math.Max(left._height, right._height)));
                _count = 1 + left._count + right._count;
                _frozen = frozen;
            }

            /// <summary>
            /// Gets a value indicating whether this instance is empty.
            /// </summary>
            /// <value>
            /// <c>true</c> if this instance is empty; otherwise, <c>false</c>.
            /// </value>
            public bool IsEmpty
            {
                get
                {
                    Contract.Ensures(Contract.Result<bool>() == (_left == null));
                    return _left == null;
                }
            }

            /// <summary>
            /// Gets the height of the tree beneath this node.
            /// </summary>
            public int Height
            {
                get { return _height; }
            }

            /// <summary>
            /// Gets the left branch of this node.
            /// </summary>
            public Node Left { get { return _left; } }

            /// <summary>
            /// Gets the left branch of this node.
            /// </summary>
            IBinaryTree IBinaryTree.Left
            {
                get { return _left; }
            }

            /// <summary>
            /// Gets the right branch of this node.
            /// </summary>
            public Node Right { get { return _right; } }

            /// <summary>
            /// Gets the right branch of this node.
            /// </summary>
            IBinaryTree IBinaryTree.Right
            {
                get { return _right; }
            }

            /// <summary>
            /// Gets the left branch of this node.
            /// </summary>
            IBinaryTree<T> IBinaryTree<T>.Left
            {
                get { return _left; }
            }

            /// <summary>
            /// Gets the right branch of this node.
            /// </summary>
            IBinaryTree<T> IBinaryTree<T>.Right
            {
                get { return _right; }
            }

            /// <summary>
            /// Gets the value represented by the current node.
            /// </summary>
            public T Value
            {
                get { return _key; }
            }

            /// <summary>
            /// Gets the number of elements contained by this subtree starting at this node.
            /// </summary>
            public int Count
            {
                get
                {
                    Contract.Ensures(Contract.Result<int>() == _count);
                    return _count;
                }
            }

            /// <summary>
            /// Gets the key.
            /// </summary>
            internal T Key
            {
                get { return _key; }
            }

            /// <summary>
            /// Gets the element of the set at the given index.
            /// </summary>
            /// <param name="index">The 0-based index of the element in the set to return.</param>
            /// <returns>The element at the given position.</returns>
            internal T this[int index]
            {
                get
                {
                    Requires.Range(index >= 0 && index < this.Count, "index");

                    if (index < _left._count)
                    {
                        return _left[index];
                    }

                    if (index > _left._count)
                    {
                        return _right[index - _left._count - 1];
                    }

                    return _key;
                }
            }

            #region IEnumerable<T> Members

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
            /// </returns>
            public Enumerator GetEnumerator()
            {
                return new Enumerator(this);
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
            /// </returns>
            [ExcludeFromCodeCoverage] // internal, never called, but here for interface implementation
            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
            /// </returns>
            [ExcludeFromCodeCoverage] // internal, never called, but here for interface implementation
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            #endregion

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <param name="builder">The builder, if applicable.</param>
            /// <returns>
            /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
            /// </returns>
            internal Enumerator GetEnumerator(Builder builder)
            {
                return new Enumerator(this, builder);
            }

            /// <summary>
            /// Creates a node tree that contains the contents of a list.
            /// </summary>
            /// <param name="items">An indexable list with the contents that the new node tree should contain.</param>
            /// <param name="start">The starting index within <paramref name="items"/> that should be captured by the node tree.</param>
            /// <param name="length">The number of elements from <paramref name="items"/> that should be captured by the node tree.</param>
            /// <returns>The root of the created node tree.</returns>
            [Pure]
            internal static Node NodeTreeFromList(IOrderedCollection<T> items, int start, int length)
            {
                Requires.NotNull(items, "items");
                Requires.Range(start >= 0, "start");
                Requires.Range(length >= 0, "length");

                if (length == 0)
                {
                    return EmptyNode;
                }

                int rightCount = (length - 1) / 2;
                int leftCount = (length - 1) - rightCount;
                Node left = NodeTreeFromList(items, start, leftCount);
                Node right = NodeTreeFromList(items, start + leftCount + 1, rightCount);
                return new Node(items[start + leftCount], left, right, true);
            }

            /// <summary>
            /// Adds the specified key to the tree.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <returns>The new tree.</returns>
            internal Node Add(T key)
            {
                return this.Insert(_count, key);
            }

            /// <summary>
            /// Adds a value at a given index to this node.
            /// </summary>
            /// <param name="index">The location for the new value.</param>
            /// <param name="key">The value to add.</param>
            /// <returns>The new tree.</returns>
            internal Node Insert(int index, T key)
            {
                Requires.Range(index >= 0 && index <= this.Count, "index");

                if (this.IsEmpty)
                {
                    return new Node(key, this, this);
                }
                else
                {
                    Node result;
                    if (index <= _left._count)
                    {
                        var newLeft = _left.Insert(index, key);
                        result = this.Mutate(left: newLeft);
                    }
                    else
                    {
                        var newRight = _right.Insert(index - _left._count - 1, key);
                        result = this.Mutate(right: newRight);
                    }

                    return MakeBalanced(result);
                }
            }

            /// <summary>
            /// Adds the specified keys to the tree.
            /// </summary>
            /// <param name="keys">The keys.</param>
            /// <returns>The new tree.</returns>
            internal Node AddRange(IEnumerable<T> keys)
            {
                return this.InsertRange(_count, keys);
            }

            /// <summary>
            /// Adds a collection of values at a given index to this node.
            /// </summary>
            /// <param name="index">The location for the new values.</param>
            /// <param name="keys">The values to add.</param>
            /// <returns>The new tree.</returns>
            internal Node InsertRange(int index, IEnumerable<T> keys)
            {
                Requires.Range(index >= 0 && index <= this.Count, "index");
                Requires.NotNull(keys, "keys");

                if (this.IsEmpty)
                {
                    ImmutableList<T> other;
                    if (TryCastToImmutableList(keys, out other))
                    {
                        return other._root;
                    }

                    var list = keys.AsOrderedCollection();
                    return Node.NodeTreeFromList(list, 0, list.Count);
                }
                else
                {
                    Node result;
                    if (index <= _left._count)
                    {
                        var newLeft = _left.InsertRange(index, keys);
                        result = this.Mutate(left: newLeft);
                    }
                    else
                    {
                        var newRight = _right.InsertRange(index - _left._count - 1, keys);
                        result = this.Mutate(right: newRight);
                    }

                    return BalanceNode(result);
                }
            }

            /// <summary>
            /// Removes a value at a given index to this node.
            /// </summary>
            /// <param name="index">The location for the new value.</param>
            /// <returns>The new tree.</returns>
            internal Node RemoveAt(int index)
            {
                Requires.Range(index >= 0 && index < this.Count, "index");

                Node result = this;
                if (index == _left._count)
                {
                    // We have a match. If this is a leaf, just remove it 
                    // by returning Empty.  If we have only one child,
                    // replace the node with the child.
                    if (_right.IsEmpty && _left.IsEmpty)
                    {
                        result = EmptyNode;
                    }
                    else if (_right.IsEmpty && !_left.IsEmpty)
                    {
                        result = _left;
                    }
                    else if (!_right.IsEmpty && _left.IsEmpty)
                    {
                        result = _right;
                    }
                    else
                    {
                        // We have two children. Remove the next-highest node and replace
                        // this node with it.
                        var successor = _right;
                        while (!successor._left.IsEmpty)
                        {
                            successor = successor._left;
                        }

                        var newRight = _right.RemoveAt(0);
                        result = successor.Mutate(left: _left, right: newRight);
                    }
                }
                else if (index < _left._count)
                {
                    var newLeft = _left.RemoveAt(index);
                    result = this.Mutate(left: newLeft);
                }
                else
                {
                    var newRight = _right.RemoveAt(index - _left._count - 1);
                    result = this.Mutate(right: newRight);
                }

                return result.IsEmpty ? result : MakeBalanced(result);
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
            /// The new node tree.
            /// </returns>
            internal Node RemoveAll(Predicate<T> match)
            {
                Requires.NotNull(match, "match");
                Contract.Ensures(Contract.Result<Node>() != null);

                var result = this;
                int index = 0;
                foreach (var item in this)
                {
                    if (match(item))
                    {
                        result = result.RemoveAt(index);
                    }
                    else
                    {
                        index++;
                    }
                }

                return result;
            }

            /// <summary>
            /// Replaces a value at a given index.
            /// </summary>
            /// <param name="index">The location for the new value.</param>
            /// <param name="value">The new value for the node.</param>
            /// <returns>The new tree.</returns>
            internal Node ReplaceAt(int index, T value)
            {
                Requires.Range(index >= 0 && index < this.Count, "index");

                Node result = this;
                if (index == _left._count)
                {
                    // We have a match. 
                    result = this.Mutate(value);
                }
                else if (index < _left._count)
                {
                    var newLeft = _left.ReplaceAt(index, value);
                    result = this.Mutate(left: newLeft);
                }
                else
                {
                    var newRight = _right.ReplaceAt(index - _left._count - 1, value);
                    result = this.Mutate(right: newRight);
                }

                return result;
            }

            /// <summary>
            /// Reverses the order of the elements in the entire <see cref="ImmutableList{T}"/>.
            /// </summary>
            /// <returns>The reversed list.</returns>
            internal Node Reverse()
            {
                Contract.Ensures(Contract.Result<Node>() != null);
                return this.Reverse(0, this.Count);
            }

            /// <summary>
            /// Reverses the order of the elements in the specified range.
            /// </summary>
            /// <param name="index">The zero-based starting index of the range to reverse.</param>
            /// <param name="count">The number of elements in the range to reverse.</param> 
            /// <returns>The reversed list.</returns>
            internal Node Reverse(int index, int count)
            {
                Requires.Range(index >= 0, "index");
                Requires.Range(count >= 0, "count");
                Requires.Range(index + count <= this.Count, "index");

                Node result = this;
                int start = index;
                int end = index + count - 1;
                while (start < end)
                {
                    T a = result[start];
                    T b = result[end];
                    result = result
                        .ReplaceAt(end, a)
                        .ReplaceAt(start, b);
                    start++;
                    end--;
                }

                return result;
            }

            /// <summary>
            /// Sorts the elements in the entire <see cref="ImmutableList{T}"/> using
            /// the default comparer.
            /// </summary>
            internal Node Sort()
            {
                Contract.Ensures(Contract.Result<Node>() != null);
                return this.Sort(Comparer<T>.Default);
            }

            /// <summary>
            /// Sorts the elements in the entire <see cref="ImmutableList{T}"/> using
            /// the specified <see cref="Comparison{T}"/>.
            /// </summary>
            /// <param name="comparison">
            /// The <see cref="Comparison{T}"/> to use when comparing elements.
            /// </param>
            /// <returns>The sorted list.</returns>
            internal Node Sort(Comparison<T> comparison)
            {
                Requires.NotNull(comparison, "comparison");
                Contract.Ensures(Contract.Result<Node>() != null);

                // PERF: Eventually this might be reimplemented in a way that does not require allocating an array.
                var array = new T[this.Count];
                this.CopyTo(array);
                Array.Sort(array, comparison);
                return NodeTreeFromList(array.AsOrderedCollection(), 0, this.Count);
            }

            /// <summary>
            /// Sorts the elements in the entire <see cref="ImmutableList{T}"/> using
            /// the specified comparer.
            /// </summary>
            /// <param name="comparer">
            /// The <see cref="IComparer{T}"/> implementation to use when comparing
            /// elements, or null to use the default comparer <see cref="Comparer{T}.Default"/>.
            /// </param>
            /// <returns>The sorted list.</returns>
            internal Node Sort(IComparer<T> comparer)
            {
                Contract.Ensures(Contract.Result<Node>() != null);
                return this.Sort(0, this.Count, comparer);
            }

            /// <summary>
            /// Sorts the elements in a range of elements in <see cref="ImmutableList{T}"/>
            /// using the specified comparer.
            /// </summary>
            /// <param name="index">
            /// The zero-based starting index of the range to sort.
            /// </param>
            /// <param name="count">
            /// The length of the range to sort.
            /// </param>
            /// <param name="comparer">
            /// The <see cref="IComparer{T}"/> implementation to use when comparing
            /// elements, or null to use the default comparer <see cref="Comparer{T}.Default"/>.
            /// </param>
            /// <returns>The sorted list.</returns>
            internal Node Sort(int index, int count, IComparer<T> comparer)
            {
                Requires.Range(index >= 0, "index");
                Requires.Range(count >= 0, "count");
                Requires.Argument(index + count <= this.Count);

                // PERF: Eventually this might be reimplemented in a way that does not require allocating an array.
                var array = new T[this.Count];
                this.CopyTo(array);
                Array.Sort(array, index, count, comparer);
                return NodeTreeFromList(array.AsOrderedCollection(), 0, this.Count);
            }

            /// <summary>
            /// Searches a range of elements in the sorted <see cref="ImmutableList{T}"/>
            /// for an element using the specified comparer and returns the zero-based index
            /// of the element.
            /// </summary>
            /// <param name="index">The zero-based starting index of the range to search.</param>
            /// <param name="count"> The length of the range to search.</param>
            /// <param name="item">The object to locate. The value can be null for reference types.</param>
            /// <param name="comparer">
            /// The <see cref="IComparer{T}"/> implementation to use when comparing
            /// elements, or null to use the default comparer <see cref="Comparer{T}.Default"/>.
            /// </param>
            /// <returns>
            /// The zero-based index of item in the sorted <see cref="ImmutableList{T}"/>,
            /// if item is found; otherwise, a negative number that is the bitwise complement
            /// of the index of the next element that is larger than item or, if there is
            /// no larger element, the bitwise complement of <see cref="ImmutableList{T}.Count"/>.
            /// </returns>
            /// <exception cref="ArgumentOutOfRangeException">
            /// <paramref name="index"/> is less than 0.-or-<paramref name="count"/> is less than 0.
            /// </exception>
            /// <exception cref="ArgumentException">
            /// <paramref name="index"/> and <paramref name="count"/> do not denote a valid range in the <see cref="ImmutableList{T}"/>.
            /// </exception>
            /// <exception cref="InvalidOperationException">
            /// <paramref name="comparer"/> is null, and the default comparer <see cref="Comparer{T}.Default"/>
            /// cannot find an implementation of the <see cref="IComparable{T}"/> generic interface
            /// or the <see cref="IComparable"/> interface for type <typeparamref name="T"/>.
            /// </exception>
            internal int BinarySearch(int index, int count, T item, IComparer<T> comparer)
            {
                Requires.Range(index >= 0, "index");
                Requires.Range(count >= 0, "count");
                comparer = comparer ?? Comparer<T>.Default;

                if (this.IsEmpty || count <= 0)
                {
                    return ~index;
                }

                // If this node is not within range, defer to either branch as appropriate.
                int thisNodeIndex = _left.Count; // this is only the index within the AVL tree, treating this node as root rather than a member of a larger tree.
                if (index + count <= thisNodeIndex)
                {
                    return _left.BinarySearch(index, count, item, comparer);
                }
                else if (index > thisNodeIndex)
                {
                    int result = _right.BinarySearch(index - thisNodeIndex - 1, count, item, comparer);
                    int offset = thisNodeIndex + 1;
                    return result < 0 ? result - offset : result + offset;
                }

                // We're definitely in the caller's designated range now. 
                // Any possible match will be a descendant of this node (or this immediate one).
                // Some descendants may not be in range, but if we hit any it means no match was found,
                // and a negative response would come back from the above code to the below code.
                int compare = comparer.Compare(item, _key);
                if (compare == 0)
                {
                    return thisNodeIndex;
                }
                else if (compare > 0)
                {
                    int adjustedCount = count - (thisNodeIndex - index) - 1;
                    int result = adjustedCount < 0 ? -1 : _right.BinarySearch(0, adjustedCount, item, comparer);
                    int offset = thisNodeIndex + 1;
                    return result < 0 ? result - offset : result + offset;
                }
                else
                {
                    if (index == thisNodeIndex)
                    {
                        // We can't go any further left.
                        return ~index;
                    }

                    int result = _left.BinarySearch(index, count, item, comparer);
                    //return result < 0 ? result - thisNodeIndex : result + thisNodeIndex;
                    return result;
                }
            }

            /// <summary>
            /// Searches for the specified object and returns the zero-based index of the
            /// first occurrence within the range of elements in the <see cref="ImmutableList{T}"/>
            /// that starts at the specified index and contains the specified number of elements.
            /// </summary>
            /// <param name="item">
            /// The object to locate in the <see cref="ImmutableList{T}"/>. The value
            /// can be null for reference types.
            /// </param>
            /// <param name="equalityComparer">The equality comparer to use for testing the match of two elements.</param>
            /// <returns>
            /// The zero-based index of the first occurrence of <paramref name="item"/> within the entire
            /// <see cref="ImmutableList{T}"/>, if found; otherwise, -1.
            /// </returns>
            [Pure]
            internal int IndexOf(T item, IEqualityComparer<T> equalityComparer)
            {
                return this.IndexOf(item, 0, this.Count, equalityComparer);
            }

            /// <summary>
            /// Searches for the specified object and returns the zero-based index of the
            /// first occurrence within the range of elements in the <see cref="ImmutableList{T}"/>
            /// that starts at the specified index and contains the specified number of elements.
            /// </summary>
            /// <param name="item">
            /// The object to locate in the <see cref="ImmutableList{T}"/>. The value
            /// can be null for reference types.
            /// </param>
            /// <param name="index">
            /// The zero-based starting index of the search. 0 (zero) is valid in an empty
            /// list.
            /// </param>
            /// <param name="count">
            /// The number of elements in the section to search.
            /// </param>
            /// <param name="equalityComparer">
            /// The equality comparer to use in the search.
            /// If <c>null</c>, <see cref="EqualityComparer{T}.Default"/> is used.
            /// </param>
            /// <returns>
            /// The zero-based index of the first occurrence of <paramref name="item"/> within the range of
            /// elements in the <see cref="ImmutableList{T}"/> that starts at <paramref name="index"/> and
            /// contains <paramref name="count"/> number of elements, if found; otherwise, -1.
            /// </returns>
            [Pure]
            internal int IndexOf(T item, int index, int count, IEqualityComparer<T> equalityComparer)
            {
                Requires.Range(index >= 0, "index");
                Requires.Range(count >= 0, "count");
                Requires.Range(count <= this.Count, "count");
                Requires.Range(index + count <= this.Count, "count");

                equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
                using (var enumerator = new Enumerator(this, startIndex: index, count: count))
                {
                    while (enumerator.MoveNext())
                    {
                        if (equalityComparer.Equals(item, enumerator.Current))
                        {
                            return index;
                        }

                        index++;
                    }
                }

                return -1;
            }

            /// <summary>
            /// Searches for the specified object and returns the zero-based index of the
            /// last occurrence within the range of elements in the <see cref="ImmutableList{T}"/>
            /// that contains the specified number of elements and ends at the specified
            /// index.
            /// </summary>
            /// <param name="item">
            /// The object to locate in the <see cref="ImmutableList{T}"/>. The value
            /// can be null for reference types.
            /// </param>
            /// <param name="index">The zero-based starting index of the backward search.</param>
            /// <param name="count">The number of elements in the section to search.</param>
            /// <param name="equalityComparer">The equality comparer to use for testing the match of two elements.</param>
            /// <returns>
            /// The zero-based index of the last occurrence of <paramref name="item"/> within the range of elements
            /// in the <see cref="ImmutableList{T}"/> that contains <paramref name="count"/> number of elements
            /// and ends at <paramref name="index"/>, if found; otherwise, -1.
            /// </returns>
            [Pure]
            internal int LastIndexOf(T item, int index, int count, IEqualityComparer<T> equalityComparer)
            {
                Requires.Range(index >= 0, "index");
                Requires.Range(count >= 0 && count <= this.Count, "count");
                Requires.Argument(index - count + 1 >= 0);

                equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
                using (var enumerator = new Enumerator(this, startIndex: index, count: count, reversed: true))
                {
                    while (enumerator.MoveNext())
                    {
                        if (equalityComparer.Equals(item, enumerator.Current))
                        {
                            return index;
                        }

                        index--;
                    }
                }

                return -1;
            }

            /// <summary>
            /// Copies the entire <see cref="ImmutableList{T}"/> to a compatible one-dimensional
            /// array, starting at the beginning of the target array.
            /// </summary>
            /// <param name="array">
            /// The one-dimensional <see cref="Array"/> that is the destination of the elements
            /// copied from <see cref="ImmutableList{T}"/>. The <see cref="Array"/> must have
            /// zero-based indexing.
            /// </param>
            internal void CopyTo(T[] array)
            {
                Requires.NotNull(array, "array");
                Requires.Argument(array.Length >= this.Count);

                int index = 0;
                foreach (var element in this)
                {
                    array[index++] = element;
                }
            }

            /// <summary>
            /// Copies the entire <see cref="ImmutableList{T}"/> to a compatible one-dimensional
            /// array, starting at the specified index of the target array.
            /// </summary>
            /// <param name="array">
            /// The one-dimensional <see cref="Array"/> that is the destination of the elements
            /// copied from <see cref="ImmutableList{T}"/>. The <see cref="Array"/> must have
            /// zero-based indexing.
            /// </param>
            /// <param name="arrayIndex">
            /// The zero-based index in <paramref name="array"/> at which copying begins.
            /// </param>
            internal void CopyTo(T[] array, int arrayIndex)
            {
                Requires.NotNull(array, "array");
                Requires.Range(arrayIndex >= 0, "arrayIndex");
                Requires.Range(arrayIndex <= array.Length, "arrayIndex");
                Requires.Argument(arrayIndex + this.Count <= array.Length);

                foreach (var element in this)
                {
                    array[arrayIndex++] = element;
                }
            }

            /// <summary>
            /// Copies a range of elements from the <see cref="ImmutableList{T}"/> to
            /// a compatible one-dimensional array, starting at the specified index of the
            /// target array.
            /// </summary>
            /// <param name="index">
            /// The zero-based index in the source <see cref="ImmutableList{T}"/> at
            /// which copying begins.
            /// </param>
            /// <param name="array">
            /// The one-dimensional <see cref="Array"/> that is the destination of the elements
            /// copied from <see cref="ImmutableList{T}"/>. The <see cref="Array"/> must have
            /// zero-based indexing.
            /// </param>
            /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
            /// <param name="count">The number of elements to copy.</param>
            internal void CopyTo(int index, T[] array, int arrayIndex, int count)
            {
                Requires.NotNull(array, "array");
                Requires.Range(index >= 0, "index");
                Requires.Range(count >= 0, "count");
                Requires.Range(index + count <= this.Count, "count");
                Requires.Range(arrayIndex >= 0, "arrayIndex");
                Requires.Range(arrayIndex + count <= array.Length, "arrayIndex");

                using (var enumerator = new Enumerator(this, startIndex: index, count: count))
                {
                    while (enumerator.MoveNext())
                    {
                        array[arrayIndex++] = enumerator.Current;
                    }
                }
            }

            /// <summary>
            /// Copies the elements of the <see cref="ICollection"/> to an <see cref="Array"/>, starting at a particular <see cref="Array"/> index.
            /// </summary>
            /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="ICollection"/>. The <see cref="Array"/> must have zero-based indexing.</param>
            /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
            internal void CopyTo(Array array, int arrayIndex)
            {
                Requires.NotNull(array, "array");
                Requires.Range(arrayIndex >= 0, "arrayIndex");
                Requires.Range(array.Length >= arrayIndex + this.Count, "arrayIndex");

                foreach (var element in this)
                {
                    array.SetValue(element, arrayIndex++);
                }
            }

            /// <summary>
            /// Converts the elements in the current <see cref="ImmutableList{T}"/> to
            /// another type, and returns a list containing the converted elements.
            /// </summary>
            /// <param name="converter">
            /// A <see cref="Func{T, TResult}"/> delegate that converts each element from
            /// one type to another type.
            /// </param>
            /// <typeparam name="TOutput">
            /// The type of the elements of the target array.
            /// </typeparam>
            /// <returns>
            /// A node tree with the transformed list.
            /// </returns>
            internal ImmutableList<TOutput>.Node ConvertAll<TOutput>(Func<T, TOutput> converter)
            {
                var root = ImmutableList<TOutput>.Node.EmptyNode;

                if (this.IsEmpty)
                {
                    return root;
                }

                return root.AddRange(Linq.Enumerable.Select(this, converter));
            }

            /// <summary>
            /// Determines whether every element in the <see cref="ImmutableList{T}"/>
            /// matches the conditions defined by the specified predicate.
            /// </summary>
            /// <param name="match">
            /// The <see cref="Predicate{T}"/> delegate that defines the conditions to check against
            /// the elements.
            /// </param>
            /// <returns>
            /// true if every element in the <see cref="ImmutableList{T}"/> matches the
            /// conditions defined by the specified predicate; otherwise, false. If the list
            /// has no elements, the return value is true.
            /// </returns>
            internal bool TrueForAll(Predicate<T> match)
            {
                foreach (var item in this)
                {
                    if (!match(item))
                    {
                        return false;
                    }
                }

                return true;
            }

            /// <summary>
            /// Determines whether the <see cref="ImmutableList{T}"/> contains elements
            /// that match the conditions defined by the specified predicate.
            /// </summary>
            /// <param name="match">
            /// The <see cref="Predicate{T}"/> delegate that defines the conditions of the elements
            /// to search for.
            /// </param>
            /// <returns>
            /// true if the <see cref="ImmutableList{T}"/> contains one or more elements
            /// that match the conditions defined by the specified predicate; otherwise,
            /// false.
            /// </returns>
            internal bool Exists(Predicate<T> match)
            {
                Requires.NotNull(match, "match");

                foreach (T item in this)
                {
                    if (match(item))
                    {
                        return true;
                    }
                }

                return false;
            }

            /// <summary>
            /// Searches for an element that matches the conditions defined by the specified
            /// predicate, and returns the first occurrence within the entire <see cref="ImmutableList{T}"/>.
            /// </summary>
            /// <param name="match">
            /// The <see cref="Predicate{T}"/> delegate that defines the conditions of the element
            /// to search for.
            /// </param>
            /// <returns>
            /// The first element that matches the conditions defined by the specified predicate,
            /// if found; otherwise, the default value for type <typeparamref name="T"/>.
            /// </returns>
            internal T Find(Predicate<T> match)
            {
                Requires.NotNull(match, "match");

                foreach (var item in this)
                {
                    if (match(item))
                    {
                        return item;
                    }
                }

                return default(T);
            }

            /// <summary>
            /// Retrieves all the elements that match the conditions defined by the specified
            /// predicate.
            /// </summary>
            /// <param name="match">
            /// The <see cref="Predicate{T}"/> delegate that defines the conditions of the elements
            /// to search for.
            /// </param>
            /// <returns>
            /// A <see cref="ImmutableList{T}"/> containing all the elements that match
            /// the conditions defined by the specified predicate, if found; otherwise, an
            /// empty <see cref="ImmutableList{T}"/>.
            /// </returns>
            internal ImmutableList<T> FindAll(Predicate<T> match)
            {
                Requires.NotNull(match, "match");
                Contract.Ensures(Contract.Result<ImmutableList<T>>() != null);

                if (this.IsEmpty)
                {
                    return ImmutableList<T>.Empty;
                }

                List<T> list = null;
                foreach (var item in this)
                {
                    if (match(item))
                    {
                        if (list == null)
                        {
                            list = new List<T>();
                        }

                        list.Add(item);
                    }
                }

                return list != null ?
                    ImmutableList.CreateRange(list) :
                    ImmutableList<T>.Empty;
            }

            /// <summary>
            /// Searches for an element that matches the conditions defined by the specified
            /// predicate, and returns the zero-based index of the first occurrence within
            /// the entire <see cref="ImmutableList{T}"/>.
            /// </summary>
            /// <param name="match">
            /// The <see cref="Predicate{T}"/> delegate that defines the conditions of the element
            /// to search for.
            /// </param>
            /// <returns>
            /// The zero-based index of the first occurrence of an element that matches the
            /// conditions defined by <paramref name="match"/>, if found; otherwise, -1.
            /// </returns>
            internal int FindIndex(Predicate<T> match)
            {
                Requires.NotNull(match, "match");
                Contract.Ensures(Contract.Result<int>() >= -1);

                return this.FindIndex(0, _count, match);
            }

            /// <summary>
            /// Searches for an element that matches the conditions defined by the specified
            /// predicate, and returns the zero-based index of the first occurrence within
            /// the range of elements in the <see cref="ImmutableList{T}"/> that extends
            /// from the specified index to the last element.
            /// </summary>
            /// <param name="startIndex">The zero-based starting index of the search.</param>
            /// <param name="match">The <see cref="Predicate{T}"/> delegate that defines the conditions of the element to search for.</param>
            /// <returns>
            /// The zero-based index of the first occurrence of an element that matches the
            /// conditions defined by <paramref name="match"/>, if found; otherwise, -1.
            /// </returns>
            internal int FindIndex(int startIndex, Predicate<T> match)
            {
                Requires.Range(startIndex >= 0, "startIndex");
                Requires.Range(startIndex <= this.Count, "startIndex");
                Requires.NotNull(match, "match");

                return this.FindIndex(startIndex, this.Count - startIndex, match);
            }

            /// <summary>
            /// Searches for an element that matches the conditions defined by the specified
            /// predicate, and returns the zero-based index of the first occurrence within
            /// the range of elements in the <see cref="ImmutableList{T}"/> that starts
            /// at the specified index and contains the specified number of elements.
            /// </summary>
            /// <param name="startIndex">The zero-based starting index of the search.</param>
            /// <param name="count">The number of elements in the section to search.</param>
            /// <param name="match">The <see cref="Predicate{T}"/> delegate that defines the conditions of the element to search for.</param>
            /// <returns>
            /// The zero-based index of the first occurrence of an element that matches the
            /// conditions defined by <paramref name="match"/>, if found; otherwise, -1.
            /// </returns>
            internal int FindIndex(int startIndex, int count, Predicate<T> match)
            {
                Requires.Range(startIndex >= 0, "startIndex");
                Requires.Range(count >= 0, "count");
                Requires.Argument(startIndex + count <= this.Count);
                Requires.NotNull(match, "match");

                using (var enumerator = new Enumerator(this, startIndex: startIndex, count: count))
                {
                    int index = startIndex;
                    while (enumerator.MoveNext())
                    {
                        if (match(enumerator.Current))
                        {
                            return index;
                        }

                        index++;
                    }
                }

                return -1;
            }

            /// <summary>
            /// Searches for an element that matches the conditions defined by the specified
            /// predicate, and returns the last occurrence within the entire <see cref="ImmutableList{T}"/>.
            /// </summary>
            /// <param name="match">
            /// The <see cref="Predicate{T}"/> delegate that defines the conditions of the element
            /// to search for.
            /// </param>
            /// <returns>
            /// The last element that matches the conditions defined by the specified predicate,
            /// if found; otherwise, the default value for type <typeparamref name="T"/>.
            /// </returns>
            internal T FindLast(Predicate<T> match)
            {
                Requires.NotNull(match, "match");

                using (var enumerator = new Enumerator(this, reversed: true))
                {
                    while (enumerator.MoveNext())
                    {
                        if (match(enumerator.Current))
                        {
                            return enumerator.Current;
                        }
                    }
                }

                return default(T);
            }

            /// <summary>
            /// Searches for an element that matches the conditions defined by the specified
            /// predicate, and returns the zero-based index of the last occurrence within
            /// the entire <see cref="ImmutableList{T}"/>.
            /// </summary>
            /// <param name="match">
            /// The <see cref="Predicate{T}"/> delegate that defines the conditions of the element
            /// to search for.
            /// </param>
            /// <returns>
            /// The zero-based index of the last occurrence of an element that matches the
            /// conditions defined by <paramref name="match"/>, if found; otherwise, -1.
            /// </returns>
            internal int FindLastIndex(Predicate<T> match)
            {
                Requires.NotNull(match, "match");
                Contract.Ensures(Contract.Result<int>() >= -1);

                if (this.IsEmpty)
                {
                    return -1;
                }

                return this.FindLastIndex(this.Count - 1, this.Count, match);
            }

            /// <summary>
            /// Searches for an element that matches the conditions defined by the specified
            /// predicate, and returns the zero-based index of the last occurrence within
            /// the range of elements in the <see cref="ImmutableList{T}"/> that extends
            /// from the first element to the specified index.
            /// </summary>
            /// <param name="startIndex">The zero-based starting index of the backward search.</param>
            /// <param name="match">The <see cref="Predicate{T}"/> delegate that defines the conditions of the element
            /// to search for.</param>
            /// <returns>
            /// The zero-based index of the last occurrence of an element that matches the
            /// conditions defined by <paramref name="match"/>, if found; otherwise, -1.
            /// </returns>
            internal int FindLastIndex(int startIndex, Predicate<T> match)
            {
                Requires.NotNull(match, "match");
                Requires.Range(startIndex >= 0, "startIndex");
                Requires.Range(startIndex == 0 || startIndex < this.Count, "startIndex");

                if (this.IsEmpty)
                {
                    return -1;
                }

                return this.FindLastIndex(startIndex, startIndex + 1, match);
            }

            /// <summary>
            /// Searches for an element that matches the conditions defined by the specified
            /// predicate, and returns the zero-based index of the last occurrence within
            /// the range of elements in the <see cref="ImmutableList{T}"/> that contains
            /// the specified number of elements and ends at the specified index.
            /// </summary>
            /// <param name="startIndex">The zero-based starting index of the backward search.</param>
            /// <param name="count">The number of elements in the section to search.</param>
            /// <param name="match">
            /// The <see cref="Predicate{T}"/> delegate that defines the conditions of the element
            /// to search for.
            /// </param>
            /// <returns>
            /// The zero-based index of the last occurrence of an element that matches the
            /// conditions defined by <paramref name="match"/>, if found; otherwise, -1.
            /// </returns>
            internal int FindLastIndex(int startIndex, int count, Predicate<T> match)
            {
                Requires.NotNull(match, "match");
                Requires.Range(startIndex >= 0, "startIndex");
                Requires.Range(count <= this.Count, "count");
                Requires.Argument(startIndex - count + 1 >= 0);

                using (var enumerator = new Enumerator(this, startIndex: startIndex, count: count, reversed: true))
                {
                    int index = startIndex;
                    while (enumerator.MoveNext())
                    {
                        if (match(enumerator.Current))
                        {
                            return index;
                        }

                        index--;
                    }
                }

                return -1;
            }

            /// <summary>
            /// Freezes this node and all descendant nodes so that any mutations require a new instance of the nodes.
            /// </summary>
            internal void Freeze()
            {
                // If this node is frozen, all its descendants must already be frozen.
                if (!_frozen)
                {
                    _left.Freeze();
                    _right.Freeze();
                    _frozen = true;
                }
            }

            #region Tree balancing methods

            /// <summary>
            /// AVL rotate left operation.
            /// </summary>
            /// <param name="tree">The tree.</param>
            /// <returns>The rotated tree.</returns>
            private static Node RotateLeft(Node tree)
            {
                Requires.NotNull(tree, "tree");
                Debug.Assert(!tree.IsEmpty);
                Contract.Ensures(Contract.Result<Node>() != null);

                if (tree._right.IsEmpty)
                {
                    return tree;
                }

                var right = tree._right;
                return right.Mutate(left: tree.Mutate(right: right._left));
            }

            /// <summary>
            /// AVL rotate right operation.
            /// </summary>
            /// <param name="tree">The tree.</param>
            /// <returns>The rotated tree.</returns>
            private static Node RotateRight(Node tree)
            {
                Requires.NotNull(tree, "tree");
                Debug.Assert(!tree.IsEmpty);
                Contract.Ensures(Contract.Result<Node>() != null);

                if (tree._left.IsEmpty)
                {
                    return tree;
                }

                var left = tree._left;
                return left.Mutate(right: tree.Mutate(left: left._right));
            }

            /// <summary>
            /// AVL rotate double-left operation.
            /// </summary>
            /// <param name="tree">The tree.</param>
            /// <returns>The rotated tree.</returns>
            private static Node DoubleLeft(Node tree)
            {
                Requires.NotNull(tree, "tree");
                Debug.Assert(!tree.IsEmpty);
                Contract.Ensures(Contract.Result<Node>() != null);

                if (tree._right.IsEmpty)
                {
                    return tree;
                }

                Node rotatedRightChild = tree.Mutate(right: RotateRight(tree._right));
                return RotateLeft(rotatedRightChild);
            }

            /// <summary>
            /// AVL rotate double-right operation.
            /// </summary>
            /// <param name="tree">The tree.</param>
            /// <returns>The rotated tree.</returns>
            private static Node DoubleRight(Node tree)
            {
                Requires.NotNull(tree, "tree");
                Debug.Assert(!tree.IsEmpty);
                Contract.Ensures(Contract.Result<Node>() != null);

                if (tree._left.IsEmpty)
                {
                    return tree;
                }

                Node rotatedLeftChild = tree.Mutate(left: RotateLeft(tree._left));
                return RotateRight(rotatedLeftChild);
            }

            /// <summary>
            /// Returns a value indicating whether the tree is in balance.
            /// </summary>
            /// <param name="tree">The tree.</param>
            /// <returns>0 if the tree is in balance, a positive integer if the right side is heavy, or a negative integer if the left side is heavy.</returns>
            [Pure]
            private static int Balance(Node tree)
            {
                Requires.NotNull(tree, "tree");
                Debug.Assert(!tree.IsEmpty);

                return tree._right._height - tree._left._height;
            }

            /// <summary>
            /// Determines whether the specified tree is right heavy.
            /// </summary>
            /// <param name="tree">The tree.</param>
            /// <returns>
            /// <c>true</c> if [is right heavy] [the specified tree]; otherwise, <c>false</c>.
            /// </returns>
            [Pure]
            private static bool IsRightHeavy(Node tree)
            {
                Requires.NotNull(tree, "tree");
                Debug.Assert(!tree.IsEmpty);
                return Balance(tree) >= 2;
            }

            /// <summary>
            /// Determines whether the specified tree is left heavy.
            /// </summary>
            [Pure]
            private static bool IsLeftHeavy(Node tree)
            {
                Requires.NotNull(tree, "tree");
                Debug.Assert(!tree.IsEmpty);
                return Balance(tree) <= -2;
            }

            /// <summary>
            /// Balances the specified tree.
            /// </summary>
            /// <param name="tree">The tree.</param>
            /// <returns>A balanced tree.</returns>
            [Pure]
            private static Node MakeBalanced(Node tree)
            {
                Requires.NotNull(tree, "tree");
                Debug.Assert(!tree.IsEmpty);
                Contract.Ensures(Contract.Result<Node>() != null);

                if (IsRightHeavy(tree))
                {
                    return Balance(tree._right) < 0 ? DoubleLeft(tree) : RotateLeft(tree);
                }

                if (IsLeftHeavy(tree))
                {
                    return Balance(tree._left) > 0 ? DoubleRight(tree) : RotateRight(tree);
                }

                return tree;
            }

            /// <summary>
            /// Balance the specified node.  Allows for a large imbalance between left and
            /// right nodes, but assumes left and right nodes are individually balanced.
            /// </summary>
            /// <param name="node">The node.</param>
            /// <returns>A balanced node</returns>
            private static Node BalanceNode(Node node)
            {
                while (IsRightHeavy(node) || IsLeftHeavy(node))
                {
                    if (IsRightHeavy(node))
                    {
                        node = Balance(node._right) < 0 ? DoubleLeft(node) : RotateLeft(node);
                        node.Mutate(left: BalanceNode(node._left));
                    }
                    else
                    {
                        node = Balance(node._left) > 0 ? DoubleRight(node) : RotateRight(node);
                        node.Mutate(right: BalanceNode(node._right));
                    }
                }

                return node;
            }

            #endregion

            /// <summary>
            /// Creates a node mutation, either by mutating this node (if not yet frozen) or by creating a clone of this node
            /// with the described changes.
            /// </summary>
            /// <param name="left">The left branch of the mutated node.</param>
            /// <param name="right">The right branch of the mutated node.</param>
            /// <returns>The mutated (or created) node.</returns>
            private Node Mutate(Node left = null, Node right = null)
            {
                if (_frozen)
                {
                    return new Node(_key, left ?? _left, right ?? _right);
                }
                else
                {
                    if (left != null)
                    {
                        _left = left;
                    }

                    if (right != null)
                    {
                        _right = right;
                    }

                    _height = checked((byte)(1 + Math.Max(_left._height, _right._height)));
                    _count = 1 + _left._count + _right._count;
                    return this;
                }
            }

            /// <summary>
            /// Creates a node mutation, either by mutating this node (if not yet frozen) or by creating a clone of this node
            /// with the described changes.
            /// </summary>
            /// <param name="value">The new value for this node.</param>
            /// <returns>The mutated (or created) node.</returns>
            private Node Mutate(T value)
            {
                if (_frozen)
                {
                    return new Node(value, _left, _right);
                }
                else
                {
                    _key = value;
                    return this;
                }
            }
        }
    }

    /// <summary>
    /// A simple view of the immutable list that the debugger can show to the developer.
    /// </summary>
    internal class ImmutableListDebuggerProxy<T>
    {
        /// <summary>
        /// The collection to be enumerated.
        /// </summary>
        private readonly ImmutableList<T> _list;

        /// <summary>
        /// The simple view of the collection.
        /// </summary>
        private T[] _cachedContents;

        /// <summary>   
        /// Initializes a new instance of the <see cref="ImmutableListDebuggerProxy{T}"/> class.
        /// </summary>
        /// <param name="list">The list to display in the debugger</param>
        public ImmutableListDebuggerProxy(ImmutableList<T> list)
        {
            Requires.NotNull(list, "list");
            _list = list;
        }

        /// <summary>
        /// Gets a simple debugger-viewable list.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Contents
        {
            get
            {
                if (_cachedContents == null)
                {
                    _cachedContents = _list.ToArray(_list.Count);
                }

                return _cachedContents;
            }
        }
    }
}
