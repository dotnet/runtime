// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace System.Collections.Immutable
{
    /// <summary>
    /// An immutable list implementation.
    /// </summary>
    /// <typeparam name="T">The type of elements in the set.</typeparam>
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(ImmutableEnumerableDebuggerProxy<>))]
    public sealed partial class ImmutableList<T> : IImmutableList<T>, IList<T>, IList, IOrderedCollection<T>, IImmutableListQueries<T>, IStrongEnumerable<T, ImmutableList<T>.Enumerator>
    {
        /// <summary>
        /// An empty immutable list.
        /// </summary>
        public static readonly ImmutableList<T> Empty = new ImmutableList<T>();

        /// <summary>
        /// The root node of the AVL tree that stores this set.
        /// </summary>
        private readonly Node _root;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableList{T}"/> class.
        /// </summary>
        internal ImmutableList() => _root = Node.EmptyNode;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableList{T}"/> class.
        /// </summary>
        /// <param name="root">The root of the AVL tree with the contents of this set.</param>
        private ImmutableList(Node root)
        {
            Requires.NotNull(root, nameof(root));

            root.Freeze();
            _root = root;
        }

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        public ImmutableList<T> Clear() => Empty;

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
        public int BinarySearch(T item) => this.BinarySearch(item, null);

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
        public int BinarySearch(T item, IComparer<T>? comparer) => this.BinarySearch(0, this.Count, item, comparer);

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
        public int BinarySearch(int index, int count, T item, IComparer<T>? comparer) => _root.BinarySearch(index, count, item, comparer);

        #region IImmutableList<T> Properties

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsEmpty => _root.IsEmpty;

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        IImmutableList<T> IImmutableList<T>.Clear() => this.Clear();

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        public int Count => _root.Count;

        #endregion

        #region ICollection Properties

        /// <summary>
        /// See <see cref="ICollection"/>.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        object ICollection.SyncRoot => this;

        /// <summary>
        /// See the <see cref="ICollection"/> interface.
        /// </summary>
        /// <devremarks>
        /// This type is immutable, so it is always thread-safe.
        /// </devremarks>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        bool ICollection.IsSynchronized => true;

        #endregion

        #region IImmutableList<T> Indexers

        /// <summary>
        /// Gets the element of the set at the given index.
        /// </summary>
        /// <param name="index">The 0-based index of the element in the set to return.</param>
        /// <returns>The element at the given position.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown from getter when <paramref name="index"/> is negative or not less than <see cref="Count"/>.</exception>
        public T this[int index] => _root.ItemRef(index);

        /// <summary>
        /// Gets a read-only reference to the element of the set at the given index.
        /// </summary>
        /// <param name="index">The 0-based index of the element in the set to return.</param>
        /// <returns>A read-only reference to the element at the given position.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown when <paramref name="index"/> is negative or not less than <see cref="Count"/>.</exception>
        public ref readonly T ItemRef(int index) => ref _root.ItemRef(index);

        #endregion

        #region IOrderedCollection<T> Indexers

        /// <summary>
        /// Gets the element in the collection at a given index.
        /// </summary>
        T IOrderedCollection<T>.this[int index] => this[index];

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
        public ImmutableList<T> Add(T value)
        {
            ImmutableList<T>.Node result = _root.Add(value);
            return this.Wrap(result);
        }

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        public ImmutableList<T> AddRange(IEnumerable<T> items)
        {
            Requires.NotNull(items, nameof(items));

            // Some optimizations may apply if we're an empty list.
            if (this.IsEmpty)
            {
                return CreateRange(items);
            }

            ImmutableList<T>.Node result = _root.AddRange(items);

            return this.Wrap(result);
        }

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        public ImmutableList<T> Insert(int index, T item)
        {
            Requires.Range(index >= 0 && index <= this.Count, nameof(index));
            return this.Wrap(_root.Insert(index, item));
        }

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        public ImmutableList<T> InsertRange(int index, IEnumerable<T> items)
        {
            Requires.Range(index >= 0 && index <= this.Count, nameof(index));
            Requires.NotNull(items, nameof(items));

            ImmutableList<T>.Node result = _root.InsertRange(index, items);

            return this.Wrap(result);
        }

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        public ImmutableList<T> Remove(T value) => this.Remove(value, EqualityComparer<T>.Default);

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        public ImmutableList<T> Remove(T value, IEqualityComparer<T>? equalityComparer)
        {
            int index = this.IndexOf(value, equalityComparer);
            return index < 0 ? this : this.RemoveAt(index);
        }

        /// <summary>
        /// Removes the specified values from this list.
        /// </summary>
        /// <param name="index">The starting index to begin removal.</param>
        /// <param name="count">The number of elements to remove.</param>
        /// <returns>A new list with the elements removed.</returns>
        public ImmutableList<T> RemoveRange(int index, int count)
        {
            Requires.Range(index >= 0 && index <= this.Count, nameof(index));
            Requires.Range(count >= 0 && index + count <= this.Count, nameof(count));

            ImmutableList<T>.Node result = _root;
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
        public ImmutableList<T> RemoveRange(IEnumerable<T> items) => this.RemoveRange(items, EqualityComparer<T>.Default);

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
        public ImmutableList<T> RemoveRange(IEnumerable<T> items, IEqualityComparer<T>? equalityComparer)
        {
            Requires.NotNull(items, nameof(items));

            // Some optimizations may apply if we're an empty list.
            if (this.IsEmpty)
            {
                return this;
            }

            // Let's not implement in terms of ImmutableList.Remove so that we're
            // not unnecessarily generating a new list object for each item.
            ImmutableList<T>.Node result = _root;
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
        public ImmutableList<T> RemoveAt(int index)
        {
            Requires.Range(index >= 0 && index < this.Count, nameof(index));
            ImmutableList<T>.Node result = _root.RemoveAt(index);
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
        public ImmutableList<T> RemoveAll(Predicate<T> match)
        {
            Requires.NotNull(match, nameof(match));

            return this.Wrap(_root.RemoveAll(match));
        }

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        public ImmutableList<T> SetItem(int index, T value) => this.Wrap(_root.ReplaceAt(index, value));

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        public ImmutableList<T> Replace(T oldValue, T newValue) => this.Replace(oldValue, newValue, EqualityComparer<T>.Default);

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        public ImmutableList<T> Replace(T oldValue, T newValue, IEqualityComparer<T>? equalityComparer)
        {
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
        public ImmutableList<T> Reverse() => this.Wrap(_root.Reverse());

        /// <summary>
        /// Reverses the order of the elements in the specified range.
        /// </summary>
        /// <param name="index">The zero-based starting index of the range to reverse.</param>
        /// <param name="count">The number of elements in the range to reverse.</param>
        /// <returns>The reversed list.</returns>
        public ImmutableList<T> Reverse(int index, int count) => this.Wrap(_root.Reverse(index, count));

        /// <summary>
        /// Sorts the elements in the entire <see cref="ImmutableList{T}"/> using
        /// the default comparer.
        /// </summary>
        public ImmutableList<T> Sort() => this.Wrap(_root.Sort());

        /// <summary>
        /// Sorts the elements in the entire <see cref="ImmutableList{T}"/> using
        /// the specified <see cref="Comparison{T}"/>.
        /// </summary>
        /// <param name="comparison">
        /// The <see cref="Comparison{T}"/> to use when comparing elements.
        /// </param>
        /// <returns>The sorted list.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparison"/> is null.</exception>
        public ImmutableList<T> Sort(Comparison<T> comparison)
        {
            Requires.NotNull(comparison, nameof(comparison));
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
        public ImmutableList<T> Sort(IComparer<T>? comparer) => this.Wrap(_root.Sort(comparer));

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
        public ImmutableList<T> Sort(int index, int count, IComparer<T>? comparer)
        {
            Requires.Range(index >= 0, nameof(index));
            Requires.Range(count >= 0, nameof(count));
            Requires.Range(index + count <= this.Count, nameof(count));

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
            Requires.NotNull(action, nameof(action));

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
        public void CopyTo(T[] array) => _root.CopyTo(array);

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
        public void CopyTo(T[] array, int arrayIndex) => _root.CopyTo(array, arrayIndex);

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
        public void CopyTo(int index, T[] array, int arrayIndex, int count) => _root.CopyTo(index, array, arrayIndex, count);

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
            Requires.Range(index >= 0, nameof(index));
            Requires.Range(count >= 0, nameof(count));
            Requires.Range(index + count <= this.Count, nameof(count));
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
            Requires.NotNull(converter, nameof(converter));
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
        public bool Exists(Predicate<T> match) => _root.Exists(match);

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
        public T? Find(Predicate<T> match) => _root.Find(match);

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
        public ImmutableList<T> FindAll(Predicate<T> match) => _root.FindAll(match);

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
        public int FindIndex(Predicate<T> match) => _root.FindIndex(match);

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
        public int FindIndex(int startIndex, Predicate<T> match) => _root.FindIndex(startIndex, match);

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
        public int FindIndex(int startIndex, int count, Predicate<T> match) => _root.FindIndex(startIndex, count, match);

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
        public T? FindLast(Predicate<T> match) => _root.FindLast(match);

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
        public int FindLastIndex(Predicate<T> match) => _root.FindLastIndex(match);

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
        public int FindLastIndex(int startIndex, Predicate<T> match) => _root.FindLastIndex(startIndex, match);

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
        public int FindLastIndex(int startIndex, int count, Predicate<T> match) => _root.FindLastIndex(startIndex, count, match);

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
        public int IndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer) => _root.IndexOf(item, index, count, equalityComparer);

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
        public int LastIndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer) => _root.LastIndexOf(item, index, count, equalityComparer);

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
        public bool TrueForAll(Predicate<T> match) => _root.TrueForAll(match);

        #endregion

        #region IImmutableList<T> Methods

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        public bool Contains(T value) => _root.Contains(value, EqualityComparer<T>.Default);

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        public int IndexOf(T value) => this.IndexOf(value, EqualityComparer<T>.Default);

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        IImmutableList<T> IImmutableList<T>.Add(T value) => this.Add(value);

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        IImmutableList<T> IImmutableList<T>.AddRange(IEnumerable<T> items) => this.AddRange(items);

        /// <summary>
        /// Inserts the specified value at the specified index.
        /// </summary>
        /// <param name="index">The index at which to insert the value.</param>
        /// <param name="item">The element to add.</param>
        /// <returns>The new immutable list.</returns>
        IImmutableList<T> IImmutableList<T>.Insert(int index, T item) => this.Insert(index, item);

        /// <summary>
        /// Inserts the specified value at the specified index.
        /// </summary>
        /// <param name="index">The index at which to insert the value.</param>
        /// <param name="items">The elements to add.</param>
        /// <returns>The new immutable list.</returns>
        IImmutableList<T> IImmutableList<T>.InsertRange(int index, IEnumerable<T> items)
        {
            return this.InsertRange(index, items);
        }

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        IImmutableList<T> IImmutableList<T>.Remove(T value, IEqualityComparer<T>? equalityComparer) => this.Remove(value, equalityComparer);

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        IImmutableList<T> IImmutableList<T>.RemoveAll(Predicate<T> match) => this.RemoveAll(match);

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        IImmutableList<T> IImmutableList<T>.RemoveRange(IEnumerable<T> items, IEqualityComparer<T>? equalityComparer) => this.RemoveRange(items, equalityComparer);

        /// <summary>
        /// See the <see cref="IImmutableList{T}"/> interface.
        /// </summary>
        IImmutableList<T> IImmutableList<T>.RemoveRange(int index, int count) => this.RemoveRange(index, count);

        /// <summary>
        /// Removes the element at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>A new list with the elements removed.</returns>
        IImmutableList<T> IImmutableList<T>.RemoveAt(int index) => this.RemoveAt(index);

        /// <summary>
        /// Replaces an element in the list at a given position with the specified element.
        /// </summary>
        /// <param name="index">The position in the list of the element to replace.</param>
        /// <param name="value">The element to replace the old element with.</param>
        /// <returns>The new list.</returns>
        IImmutableList<T> IImmutableList<T>.SetItem(int index, T value) => this.SetItem(index, value);

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
        IImmutableList<T> IImmutableList<T>.Replace(T oldValue, T newValue, IEqualityComparer<T>? equalityComparer) => this.Replace(oldValue, newValue, equalityComparer);

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
            return this.IsEmpty ?
                Enumerable.Empty<T>().GetEnumerator() :
                this.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => this.GetEnumerator();

        #endregion

        #region IList<T> Members

        /// <summary>
        /// Inserts the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        void IList<T>.Insert(int index, T item) => throw new NotSupportedException();

        /// <summary>
        /// Removes the value at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        void IList<T>.RemoveAt(int index) => throw new NotSupportedException();

        /// <summary>
        /// Gets or sets the value at the specified index.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException">Thrown from getter when <paramref name="index"/> is negative or not less than <see cref="Count"/>.</exception>
        /// <exception cref="NotSupportedException">Always thrown from the setter.</exception>
        T IList<T>.this[int index]
        {
            get => this[index];
            set => throw new NotSupportedException();
        }

        #endregion

        #region ICollection<T> Members

        /// <summary>
        /// Adds the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        void ICollection<T>.Add(T item) => throw new NotSupportedException();

        /// <summary>
        /// Clears this instance.
        /// </summary>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        void ICollection<T>.Clear() => throw new NotSupportedException();

        /// <summary>
        /// Gets a value indicating whether the <see cref="ICollection{T}"/> is read-only.
        /// </summary>
        /// <returns>true if the <see cref="ICollection{T}"/> is read-only; otherwise, false.
        ///   </returns>
        bool ICollection<T>.IsReadOnly => true;

        /// <summary>
        /// Removes the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>Nothing. An exception is always thrown.</returns>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        bool ICollection<T>.Remove(T item) => throw new NotSupportedException();

        #endregion

        #region ICollection Methods

        /// <summary>
        /// See the <see cref="ICollection"/> interface.
        /// </summary>
        void System.Collections.ICollection.CopyTo(Array array, int arrayIndex) => _root.CopyTo(array, arrayIndex);

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
        int IList.Add(object? value) => throw new NotSupportedException();

        /// <summary>
        /// Removes the <see cref="IList"/> item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        void IList.RemoveAt(int index) => throw new NotSupportedException();

        /// <summary>
        /// Clears this instance.
        /// </summary>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        void IList.Clear() => throw new NotSupportedException();

        /// <summary>
        /// Determines whether the <see cref="IList"/> contains a specific value.
        /// </summary>
        /// <param name="value">The object to locate in the <see cref="IList"/>.</param>
        /// <returns>
        /// true if the <see cref="object"/> is found in the <see cref="IList"/>; otherwise, false.
        /// </returns>
        bool IList.Contains(object? value) => IsCompatibleObject(value) && this.Contains((T)value!);

        /// <summary>
        /// Determines the index of a specific item in the <see cref="IList"/>.
        /// </summary>
        /// <param name="value">The object to locate in the <see cref="IList"/>.</param>
        /// <returns>
        /// The index of <paramref name="value"/> if found in the list; otherwise, -1.
        /// </returns>
        int IList.IndexOf(object? value) => IsCompatibleObject(value) ? this.IndexOf((T)value!) : -1;

        /// <summary>
        /// Inserts an item to the <see cref="IList"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="value"/> should be inserted.</param>
        /// <param name="value">The object to insert into the <see cref="IList"/>.</param>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        void IList.Insert(int index, object? value) => throw new NotSupportedException();

        /// <summary>
        /// Gets a value indicating whether the <see cref="IList"/> has a fixed size.
        /// </summary>
        /// <returns>true if the <see cref="IList"/> has a fixed size; otherwise, false.</returns>
        bool IList.IsFixedSize => true;

        /// <summary>
        /// Gets a value indicating whether the <see cref="ICollection{T}"/> is read-only.
        /// </summary>
        /// <returns>true if the <see cref="ICollection{T}"/> is read-only; otherwise, false.
        ///   </returns>
        bool IList.IsReadOnly => true;

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="IList"/>.
        /// </summary>
        /// <param name="value">The object to remove from the <see cref="IList"/>.</param>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        void IList.Remove(object? value) => throw new NotSupportedException();

        /// <summary>
        /// Gets or sets the <see cref="object"/> at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="object"/>.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns>The value at the specified index.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown from getter when <paramref name="index"/> is negative or not less than <see cref="Count"/>.</exception>
        /// <exception cref="NotSupportedException">Always thrown from the setter.</exception>
        object? IList.this[int index]
        {
            get => this[index];
            set => throw new NotSupportedException();
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
        public Enumerator GetEnumerator() => new Enumerator(_root);

        /// <summary>
        /// Returns the root <see cref="Node"/> of the list
        /// </summary>
        internal Node Root => _root;

        /// <summary>
        /// Creates a new sorted set wrapper for a node tree.
        /// </summary>
        /// <param name="root">The root of the collection.</param>
        /// <returns>The immutable sorted set instance.</returns>
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
        private static bool TryCastToImmutableList(IEnumerable<T> sequence, [NotNullWhen(true)] out ImmutableList<T>? other)
        {
            other = sequence as ImmutableList<T>;
            if (other != null)
            {
                return true;
            }

            if (sequence is Builder builder)
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
        private static bool IsCompatibleObject(object? value)
        {
            // Non-null values are fine.  Only accept nulls if T is a class or Nullable<U>.
            // Note that default(T) is not equal to null for value types except when T is Nullable<U>.
            return (value is T) || (default(T) == null && value == null);
        }

        /// <summary>
        /// Creates a wrapping collection type around a root node.
        /// </summary>
        /// <param name="root">The root node to wrap.</param>
        /// <returns>A wrapping collection type for the new tree.</returns>
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
        private static ImmutableList<T> CreateRange(IEnumerable<T> items)
        {
            // If the items being added actually come from an ImmutableList<T>
            // then there is no value in reconstructing it.
            if (TryCastToImmutableList(items, out ImmutableList<T>? other))
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
            IOrderedCollection<T> list = items.AsOrderedCollection();
            if (list.Count == 0)
            {
                return Empty;
            }

            Node root = Node.NodeTreeFromList(list, 0, list.Count);
            return new ImmutableList<T>(root);
        }
    }
}
