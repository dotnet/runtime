// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Immutable
{
    /// <content>
    /// Contains the inner Builder class.
    /// </content>
    public sealed partial class ImmutableList<T>
    {
        /// <summary>
        /// A list that mutates with little or no memory allocations,
        /// can produce and/or build on immutable list instances very efficiently.
        /// </summary>
        /// <remarks>
        /// <para>
        /// While <see cref="ImmutableList{T}.AddRange"/> and other bulk change methods
        /// already provide fast bulk change operations on the collection, this class allows
        /// multiple combinations of changes to be made to a set with equal efficiency.
        /// </para>
        /// <para>
        /// Instance members of this class are <em>not</em> thread-safe.
        /// </para>
        /// </remarks>
        [DebuggerDisplay("Count = {Count}")]
        [DebuggerTypeProxy(typeof(ImmutableListBuilderDebuggerProxy<>))]
        public sealed class Builder : IList<T>, IList, IOrderedCollection<T>, IImmutableListQueries<T>, IReadOnlyList<T>
        {
            /// <summary>
            /// The binary tree used to store the contents of the list.  Contents are typically not entirely frozen.
            /// </summary>
            private Node _root = Node.EmptyNode;

            /// <summary>
            /// Caches an immutable instance that represents the current state of the collection.
            /// </summary>
            /// <value>Null if no immutable view has been created for the current version.</value>
            private ImmutableList<T>? _immutable;

            /// <summary>
            /// A number that increments every time the builder changes its contents.
            /// </summary>
            private int _version;

            /// <summary>
            /// The object callers may use to synchronize access to this collection.
            /// </summary>
            private object? _syncRoot;

            /// <summary>
            /// Initializes a new instance of the <see cref="Builder"/> class.
            /// </summary>
            /// <param name="list">A list to act as the basis for a new list.</param>
            internal Builder(ImmutableList<T> list)
            {
                Requires.NotNull(list, nameof(list));
                _root = list._root;
                _immutable = list;
            }

            #region IList<T> Properties

            /// <summary>
            /// Gets the number of elements in this list.
            /// </summary>
            public int Count
            {
                get { return this.Root.Count; }
            }

            /// <summary>
            /// Gets a value indicating whether this instance is read-only.
            /// </summary>
            /// <value>Always <c>false</c>.</value>
            bool ICollection<T>.IsReadOnly
            {
                get { return false; }
            }

            #endregion

            /// <summary>
            /// Gets the current version of the contents of this builder.
            /// </summary>
            internal int Version
            {
                get { return _version; }
            }

            /// <summary>
            /// Gets or sets the root node that represents the data in this collection.
            /// </summary>
            internal Node Root
            {
                get
                {
                    return _root;
                }

                private set
                {
                    // We *always* increment the version number because some mutations
                    // may not create a new value of root, although the existing root
                    // instance may have mutated.
                    _version++;

                    if (_root != value)
                    {
                        _root = value;

                        // Clear any cached value for the immutable view since it is now invalidated.
                        _immutable = null;
                    }
                }
            }

            #region Indexers

            /// <summary>
            /// Gets or sets the value for a given index into the list.
            /// </summary>
            /// <param name="index">The index of the desired element.</param>
            /// <returns>The value at the specified index.</returns>
            public T this[int index]
            {
                get
                {
#if !NETSTANDARD1_0
                    return this.Root.ItemRef(index);
#else
                    return this.Root[index];
#endif
                }

                set
                {
                    this.Root = this.Root.ReplaceAt(index, value);
                }
            }

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

#if !NETSTANDARD1_0
            /// <summary>
            /// Gets a read-only reference to the value for a given index into the list.
            /// </summary>
            /// <param name="index">The index of the desired element.</param>
            /// <returns>A read-only reference to the value at the specified index.</returns>
            public ref readonly T ItemRef(int index)
            {
                return ref this.Root.ItemRef(index);
            }
#endif

            #endregion

            #region IList<T> Methods

            /// <summary>
            /// See <see cref="IList{T}"/>
            /// </summary>
            public int IndexOf(T item)
            {
                return this.Root.IndexOf(item, EqualityComparer<T>.Default);
            }

            /// <summary>
            /// See <see cref="IList{T}"/>
            /// </summary>
            public void Insert(int index, T item)
            {
                this.Root = this.Root.Insert(index, item);
            }

            /// <summary>
            /// See <see cref="IList{T}"/>
            /// </summary>
            public void RemoveAt(int index)
            {
                this.Root = this.Root.RemoveAt(index);
            }

            /// <summary>
            /// See <see cref="IList{T}"/>
            /// </summary>
            public void Add(T item)
            {
                this.Root = this.Root.Add(item);
            }

            /// <summary>
            /// See <see cref="IList{T}"/>
            /// </summary>
            public void Clear()
            {
                this.Root = ImmutableList<T>.Node.EmptyNode;
            }

            /// <summary>
            /// See <see cref="IList{T}"/>
            /// </summary>
            public bool Contains(T item)
            {
                return this.IndexOf(item) >= 0;
            }

            /// <summary>
            /// See <see cref="IList{T}"/>
            /// </summary>
            public bool Remove(T item)
            {
                int index = this.IndexOf(item);
                if (index < 0)
                {
                    return false;
                }

                this.Root = this.Root.RemoveAt(index);
                return true;
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
            /// </returns>
            public ImmutableList<T>.Enumerator GetEnumerator()
            {
                return this.Root.GetEnumerator(this);
            }

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

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
            /// </returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            #endregion

            #region IImmutableListQueries<T> methods

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
            /// Copies the entire ImmutableList&lt;T&gt; to a compatible one-dimensional
            /// array, starting at the beginning of the target array.
            /// </summary>
            /// <param name="array">
            /// The one-dimensional System.Array that is the destination of the elements
            /// copied from ImmutableList&lt;T&gt;. The System.Array must have
            /// zero-based indexing.
            /// </param>
            public void CopyTo(T[] array) => _root.CopyTo(array);

            /// <summary>
            /// Copies the entire ImmutableList&lt;T&gt; to a compatible one-dimensional
            /// array, starting at the specified index of the target array.
            /// </summary>
            /// <param name="array">
            /// The one-dimensional System.Array that is the destination of the elements
            /// copied from ImmutableList&lt;T&gt;. The System.Array must have
            /// zero-based indexing.
            /// </param>
            /// <param name="arrayIndex">
            /// The zero-based index in array at which copying begins.
            /// </param>
            public void CopyTo(T[] array, int arrayIndex) => _root.CopyTo(array, arrayIndex);

            /// <summary>
            /// Copies a range of elements from the ImmutableList&lt;T&gt; to
            /// a compatible one-dimensional array, starting at the specified index of the
            /// target array.
            /// </summary>
            /// <param name="index">
            /// The zero-based index in the source ImmutableList&lt;T&gt; at
            /// which copying begins.
            /// </param>
            /// <param name="array">
            /// The one-dimensional System.Array that is the destination of the elements
            /// copied from ImmutableList&lt;T&gt;. The System.Array must have
            /// zero-based indexing.
            /// </param>
            /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
            /// <param name="count">The number of elements to copy.</param>
            public void CopyTo(int index, T[] array, int arrayIndex, int count) => _root.CopyTo(index, array, arrayIndex, count);

            /// <summary>
            /// Creates a shallow copy of a range of elements in the source ImmutableList&lt;T&gt;.
            /// </summary>
            /// <param name="index">
            /// The zero-based ImmutableList&lt;T&gt; index at which the range
            /// starts.
            /// </param>
            /// <param name="count">
            /// The number of elements in the range.
            /// </param>
            /// <returns>
            /// A shallow copy of a range of elements in the source ImmutableList&lt;T&gt;.
            /// </returns>
            public ImmutableList<T> GetRange(int index, int count)
            {
                Requires.Range(index >= 0, nameof(index));
                Requires.Range(count >= 0, nameof(count));
                Requires.Range(index + count <= this.Count, nameof(count));
                return ImmutableList<T>.WrapNode(Node.NodeTreeFromList(this, index, count));
            }

            /// <summary>
            /// Converts the elements in the current ImmutableList&lt;T&gt; to
            /// another type, and returns a list containing the converted elements.
            /// </summary>
            /// <param name="converter">
            /// A System.Converter&lt;TInput,TOutput&gt; delegate that converts each element from
            /// one type to another type.
            /// </param>
            /// <typeparam name="TOutput">
            /// The type of the elements of the target array.
            /// </typeparam>
            /// <returns>
            /// A ImmutableList&lt;T&gt; of the target type containing the converted
            /// elements from the current ImmutableList&lt;T&gt;.
            /// </returns>
            public ImmutableList<TOutput> ConvertAll<TOutput>(Func<T, TOutput> converter)
            {
                Requires.NotNull(converter, nameof(converter));
                return ImmutableList<TOutput>.WrapNode(_root.ConvertAll(converter));
            }

            /// <summary>
            /// Determines whether the ImmutableList&lt;T&gt; contains elements
            /// that match the conditions defined by the specified predicate.
            /// </summary>
            /// <param name="match">
            /// The System.Predicate&lt;T&gt; delegate that defines the conditions of the elements
            /// to search for.
            /// </param>
            /// <returns>
            /// true if the ImmutableList&lt;T&gt; contains one or more elements
            /// that match the conditions defined by the specified predicate; otherwise,
            /// false.
            /// </returns>
            public bool Exists(Predicate<T> match) => _root.Exists(match);

            /// <summary>
            /// Searches for an element that matches the conditions defined by the specified
            /// predicate, and returns the first occurrence within the entire ImmutableList&lt;T&gt;.
            /// </summary>
            /// <param name="match">
            /// The System.Predicate&lt;T&gt; delegate that defines the conditions of the element
            /// to search for.
            /// </param>
            /// <returns>
            /// The first element that matches the conditions defined by the specified predicate,
            /// if found; otherwise, the default value for type T.
            /// </returns>
            public T? Find(Predicate<T> match) => _root.Find(match);

            /// <summary>
            /// Retrieves all the elements that match the conditions defined by the specified
            /// predicate.
            /// </summary>
            /// <param name="match">
            /// The System.Predicate&lt;T&gt; delegate that defines the conditions of the elements
            /// to search for.
            /// </param>
            /// <returns>
            /// A ImmutableList&lt;T&gt; containing all the elements that match
            /// the conditions defined by the specified predicate, if found; otherwise, an
            /// empty ImmutableList&lt;T&gt;.
            /// </returns>
            public ImmutableList<T> FindAll(Predicate<T> match) => _root.FindAll(match);

            /// <summary>
            /// Searches for an element that matches the conditions defined by the specified
            /// predicate, and returns the zero-based index of the first occurrence within
            /// the entire ImmutableList&lt;T&gt;.
            /// </summary>
            /// <param name="match">
            /// The System.Predicate&lt;T&gt; delegate that defines the conditions of the element
            /// to search for.
            /// </param>
            /// <returns>
            /// The zero-based index of the first occurrence of an element that matches the
            /// conditions defined by match, if found; otherwise, -1.
            /// </returns>
            public int FindIndex(Predicate<T> match) => _root.FindIndex(match);

            /// <summary>
            /// Searches for an element that matches the conditions defined by the specified
            /// predicate, and returns the zero-based index of the first occurrence within
            /// the range of elements in the ImmutableList&lt;T&gt; that extends
            /// from the specified index to the last element.
            /// </summary>
            /// <param name="startIndex">The zero-based starting index of the search.</param>
            /// <param name="match">The System.Predicate&lt;T&gt; delegate that defines the conditions of the element to search for.</param>
            /// <returns>
            /// The zero-based index of the first occurrence of an element that matches the
            /// conditions defined by match, if found; otherwise, -1.
            /// </returns>
            public int FindIndex(int startIndex, Predicate<T> match) => _root.FindIndex(startIndex, match);

            /// <summary>
            /// Searches for an element that matches the conditions defined by the specified
            /// predicate, and returns the zero-based index of the first occurrence within
            /// the range of elements in the ImmutableList&lt;T&gt; that starts
            /// at the specified index and contains the specified number of elements.
            /// </summary>
            /// <param name="startIndex">The zero-based starting index of the search.</param>
            /// <param name="count">The number of elements in the section to search.</param>
            /// <param name="match">The System.Predicate&lt;T&gt; delegate that defines the conditions of the element to search for.</param>
            /// <returns>
            /// The zero-based index of the first occurrence of an element that matches the
            /// conditions defined by match, if found; otherwise, -1.
            /// </returns>
            public int FindIndex(int startIndex, int count, Predicate<T> match) => _root.FindIndex(startIndex, count, match);

            /// <summary>
            /// Searches for an element that matches the conditions defined by the specified
            /// predicate, and returns the last occurrence within the entire ImmutableList&lt;T&gt;.
            /// </summary>
            /// <param name="match">
            /// The System.Predicate&lt;T&gt; delegate that defines the conditions of the element
            /// to search for.
            /// </param>
            /// <returns>
            /// The last element that matches the conditions defined by the specified predicate,
            /// if found; otherwise, the default value for type T.
            /// </returns>
            public T? FindLast(Predicate<T> match) => _root.FindLast(match);

            /// <summary>
            /// Searches for an element that matches the conditions defined by the specified
            /// predicate, and returns the zero-based index of the last occurrence within
            /// the entire ImmutableList&lt;T&gt;.
            /// </summary>
            /// <param name="match">
            /// The System.Predicate&lt;T&gt; delegate that defines the conditions of the element
            /// to search for.
            /// </param>
            /// <returns>
            /// The zero-based index of the last occurrence of an element that matches the
            /// conditions defined by match, if found; otherwise, -1.
            /// </returns>
            public int FindLastIndex(Predicate<T> match) => _root.FindLastIndex(match);

            /// <summary>
            /// Searches for an element that matches the conditions defined by the specified
            /// predicate, and returns the zero-based index of the last occurrence within
            /// the range of elements in the ImmutableList&lt;T&gt; that extends
            /// from the first element to the specified index.
            /// </summary>
            /// <param name="startIndex">The zero-based starting index of the backward search.</param>
            /// <param name="match">The System.Predicate&lt;T&gt; delegate that defines the conditions of the element
            /// to search for.</param>
            /// <returns>
            /// The zero-based index of the last occurrence of an element that matches the
            /// conditions defined by match, if found; otherwise, -1.
            /// </returns>
            public int FindLastIndex(int startIndex, Predicate<T> match) => _root.FindLastIndex(startIndex, match);

            /// <summary>
            /// Searches for an element that matches the conditions defined by the specified
            /// predicate, and returns the zero-based index of the last occurrence within
            /// the range of elements in the ImmutableList&lt;T&gt; that contains
            /// the specified number of elements and ends at the specified index.
            /// </summary>
            /// <param name="startIndex">The zero-based starting index of the backward search.</param>
            /// <param name="count">The number of elements in the section to search.</param>
            /// <param name="match">
            /// The System.Predicate&lt;T&gt; delegate that defines the conditions of the element
            /// to search for.
            /// </param>
            /// <returns>
            /// The zero-based index of the last occurrence of an element that matches the
            /// conditions defined by match, if found; otherwise, -1.
            /// </returns>
            public int FindLastIndex(int startIndex, int count, Predicate<T> match) => _root.FindLastIndex(startIndex, count, match);

            /// <summary>
            /// Searches for the specified object and returns the zero-based index of the
            /// first occurrence within the range of elements in the ImmutableList&lt;T&gt;
            /// that extends from the specified index to the last element.
            /// </summary>
            /// <param name="item">
            /// The object to locate in the ImmutableList&lt;T&gt;. The value
            /// can be null for reference types.
            /// </param>
            /// <param name="index">
            /// The zero-based starting index of the search. 0 (zero) is valid in an empty
            /// list.
            /// </param>
            /// <returns>
            /// The zero-based index of the first occurrence of item within the range of
            /// elements in the ImmutableList&lt;T&gt; that extends from index
            /// to the last element, if found; otherwise, -1.
            /// </returns>
            public int IndexOf(T item, int index) =>
                _root.IndexOf(item, index, this.Count - index, EqualityComparer<T>.Default);

            /// <summary>
            /// Searches for the specified object and returns the zero-based index of the
            /// first occurrence within the range of elements in the ImmutableList&lt;T&gt;
            /// that starts at the specified index and contains the specified number of elements.
            /// </summary>
            /// <param name="item">
            /// The object to locate in the ImmutableList&lt;T&gt;. The value
            /// can be null for reference types.
            /// </param>
            /// <param name="index">
            /// The zero-based starting index of the search. 0 (zero) is valid in an empty
            /// list.
            /// </param>
            /// <param name="count">
            /// The number of elements in the section to search.
            /// </param>
            /// <returns>
            /// The zero-based index of the first occurrence of item within the range of
            /// elements in the ImmutableList&lt;T&gt; that starts at index and
            /// contains count number of elements, if found; otherwise, -1.
            /// </returns>
            public int IndexOf(T item, int index, int count) =>
                _root.IndexOf(item, index, count, EqualityComparer<T>.Default);

            /// <summary>
            /// Searches for the specified object and returns the zero-based index of the
            /// first occurrence within the range of elements in the ImmutableList&lt;T&gt;
            /// that starts at the specified index and contains the specified number of elements.
            /// </summary>
            /// <param name="item">
            /// The object to locate in the ImmutableList&lt;T&gt;. The value
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
            /// The zero-based index of the first occurrence of item within the range of
            /// elements in the ImmutableList&lt;T&gt; that starts at index and
            /// contains count number of elements, if found; otherwise, -1.
            /// </returns>
            public int IndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer) =>
                _root.IndexOf(item, index, count, equalityComparer);

            /// <summary>
            /// Searches for the specified object and returns the zero-based index of the
            /// last occurrence within the range of elements in the ImmutableList&lt;T&gt;
            /// that contains the specified number of elements and ends at the specified
            /// index.
            /// </summary>
            /// <param name="item">
            /// The object to locate in the ImmutableList&lt;T&gt;. The value
            /// can be null for reference types.
            /// </param>
            /// <returns>
            /// The zero-based index of the last occurrence of item within the range of elements
            /// in the ImmutableList&lt;T&gt; that contains count number of elements
            /// and ends at index, if found; otherwise, -1.
            /// </returns>
            public int LastIndexOf(T item)
            {
                if (this.Count == 0)
                {
                    return -1;
                }

                return _root.LastIndexOf(item, this.Count - 1, this.Count, EqualityComparer<T>.Default);
            }

            /// <summary>
            /// Searches for the specified object and returns the zero-based index of the
            /// last occurrence within the range of elements in the ImmutableList&lt;T&gt;
            /// that contains the specified number of elements and ends at the specified
            /// index.
            /// </summary>
            /// <param name="item">
            /// The object to locate in the ImmutableList&lt;T&gt;. The value
            /// can be null for reference types.
            /// </param>
            /// <param name="startIndex">The zero-based starting index of the backward search.</param>
            /// <returns>
            /// The zero-based index of the last occurrence of item within the range of elements
            /// in the ImmutableList&lt;T&gt; that contains count number of elements
            /// and ends at index, if found; otherwise, -1.
            /// </returns>
            public int LastIndexOf(T item, int startIndex)
            {
                if (this.Count == 0 && startIndex == 0)
                {
                    return -1;
                }

                return _root.LastIndexOf(item, startIndex, startIndex + 1, EqualityComparer<T>.Default);
            }

            /// <summary>
            /// Searches for the specified object and returns the zero-based index of the
            /// last occurrence within the range of elements in the ImmutableList&lt;T&gt;
            /// that contains the specified number of elements and ends at the specified
            /// index.
            /// </summary>
            /// <param name="item">
            /// The object to locate in the ImmutableList&lt;T&gt;. The value
            /// can be null for reference types.
            /// </param>
            /// <param name="startIndex">The zero-based starting index of the backward search.</param>
            /// <param name="count">The number of elements in the section to search.</param>
            /// <returns>
            /// The zero-based index of the last occurrence of item within the range of elements
            /// in the ImmutableList&lt;T&gt; that contains count number of elements
            /// and ends at index, if found; otherwise, -1.
            /// </returns>
            public int LastIndexOf(T item, int startIndex, int count) =>
                _root.LastIndexOf(item, startIndex, count, EqualityComparer<T>.Default);

            /// <summary>
            /// Searches for the specified object and returns the zero-based index of the
            /// last occurrence within the range of elements in the ImmutableList&lt;T&gt;
            /// that contains the specified number of elements and ends at the specified
            /// index.
            /// </summary>
            /// <param name="item">
            /// The object to locate in the ImmutableList&lt;T&gt;. The value
            /// can be null for reference types.
            /// </param>
            /// <param name="startIndex">The zero-based starting index of the backward search.</param>
            /// <param name="count">The number of elements in the section to search.</param>
            /// <param name="equalityComparer">The equality comparer to use in the search.</param>
            /// <returns>
            /// The zero-based index of the last occurrence of item within the range of elements
            /// in the ImmutableList&lt;T&gt; that contains count number of elements
            /// and ends at index, if found; otherwise, -1.
            /// </returns>
            public int LastIndexOf(T item, int startIndex, int count, IEqualityComparer<T>? equalityComparer) =>
                _root.LastIndexOf(item, startIndex, count, equalityComparer);

            /// <summary>
            /// Determines whether every element in the ImmutableList&lt;T&gt;
            /// matches the conditions defined by the specified predicate.
            /// </summary>
            /// <param name="match">
            /// The System.Predicate&lt;T&gt; delegate that defines the conditions to check against
            /// the elements.
            /// </param>
            /// <returns>
            /// true if every element in the ImmutableList&lt;T&gt; matches the
            /// conditions defined by the specified predicate; otherwise, false. If the list
            /// has no elements, the return value is true.
            /// </returns>
            public bool TrueForAll(Predicate<T> match) => _root.TrueForAll(match);

            #endregion

            #region Public methods

            /// <summary>
            /// Adds the elements of a sequence to the end of this collection.
            /// </summary>
            /// <param name="items">
            /// The sequence whose elements should be appended to this collection.
            /// The sequence itself cannot be null, but it can contain elements that are
            /// null, if type <typeparamref name="T"/> is a reference type.
            /// </param>
            public void AddRange(IEnumerable<T> items)
            {
                Requires.NotNull(items, nameof(items));

                this.Root = this.Root.AddRange(items);
            }

            /// <summary>
            /// Inserts the elements of a collection into the ImmutableList&lt;T&gt;
            /// at the specified index.
            /// </summary>
            /// <param name="index">
            /// The zero-based index at which the new elements should be inserted.
            /// </param>
            /// <param name="items">
            /// The collection whose elements should be inserted into the ImmutableList&lt;T&gt;.
            /// The collection itself cannot be null, but it can contain elements that are
            /// null, if type T is a reference type.
            /// </param>
            public void InsertRange(int index, IEnumerable<T> items)
            {
                Requires.Range(index >= 0 && index <= this.Count, nameof(index));
                Requires.NotNull(items, nameof(items));

                this.Root = this.Root.InsertRange(index, items);
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
            /// The number of elements removed from the ImmutableList&lt;T&gt;
            /// </returns>
            public int RemoveAll(Predicate<T> match)
            {
                Requires.NotNull(match, nameof(match));

                int count = this.Count;
                this.Root = this.Root.RemoveAll(match);
                return count - this.Count;
            }

            /// <summary>
            /// Reverses the order of the elements in the entire ImmutableList&lt;T&gt;.
            /// </summary>
            public void Reverse()
            {
                this.Reverse(0, this.Count);
            }

            /// <summary>
            /// Reverses the order of the elements in the specified range.
            /// </summary>
            /// <param name="index">The zero-based starting index of the range to reverse.</param>
            /// <param name="count">The number of elements in the range to reverse.</param>
            public void Reverse(int index, int count)
            {
                Requires.Range(index >= 0, nameof(index));
                Requires.Range(count >= 0, nameof(count));
                Requires.Range(index + count <= this.Count, nameof(count));

                this.Root = this.Root.Reverse(index, count);
            }

            /// <summary>
            /// Sorts the elements in the entire ImmutableList&lt;T&gt; using
            /// the default comparer.
            /// </summary>
            public void Sort()
            {
                this.Root = this.Root.Sort();
            }

            /// <summary>
            /// Sorts the elements in the entire ImmutableList&lt;T&gt; using
            /// the specified System.Comparison&lt;T&gt;.
            /// </summary>
            /// <param name="comparison">
            /// The <see cref="Comparison{T}"/> to use when comparing elements.
            /// </param>
            /// <exception cref="ArgumentNullException"><paramref name="comparison"/> is null.</exception>
            public void Sort(Comparison<T> comparison)
            {
                Requires.NotNull(comparison, nameof(comparison));
                this.Root = this.Root.Sort(comparison);
            }

            /// <summary>
            /// Sorts the elements in the entire ImmutableList&lt;T&gt; using
            /// the specified comparer.
            /// </summary>
            /// <param name="comparer">
            /// The <see cref="IComparer{T}"/> implementation to use when comparing
            /// elements, or null to use <see cref="Comparer{T}.Default"/>.
            /// </param>
            public void Sort(IComparer<T>? comparer)
            {
                this.Root = this.Root.Sort(comparer);
            }

            /// <summary>
            /// Sorts the elements in a range of elements in ImmutableList&lt;T&gt;
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
            /// elements, or null to use <see cref="Comparer{T}.Default"/>.
            /// </param>
            public void Sort(int index, int count, IComparer<T>? comparer)
            {
                Requires.Range(index >= 0, nameof(index));
                Requires.Range(count >= 0, nameof(count));
                Requires.Range(index + count <= this.Count, nameof(count));
                this.Root = this.Root.Sort(index, count, comparer);
            }

            /// <summary>
            /// Searches the entire sorted System.Collections.Generic.List&lt;T&gt; for an element
            /// using the default comparer and returns the zero-based index of the element.
            /// </summary>
            /// <param name="item">The object to locate. The value can be null for reference types.</param>
            /// <returns>
            /// The zero-based index of item in the sorted System.Collections.Generic.List&lt;T&gt;,
            /// if item is found; otherwise, a negative number that is the bitwise complement
            /// of the index of the next element that is larger than item or, if there is
            /// no larger element, the bitwise complement of System.Collections.Generic.List&lt;T&gt;.Count.
            /// </returns>
            /// <exception cref="InvalidOperationException">
            /// The default comparer System.Collections.Generic.Comparer&lt;T&gt;.Default cannot
            /// find an implementation of the System.IComparable&lt;T&gt; generic interface or
            /// the System.IComparable interface for type T.
            /// </exception>
            public int BinarySearch(T item)
            {
                return this.BinarySearch(item, null);
            }

            /// <summary>
            ///  Searches the entire sorted System.Collections.Generic.List&lt;T&gt; for an element
            ///  using the specified comparer and returns the zero-based index of the element.
            /// </summary>
            /// <param name="item">The object to locate. The value can be null for reference types.</param>
            /// <param name="comparer">
            /// The System.Collections.Generic.IComparer&lt;T&gt; implementation to use when comparing
            /// elements.-or-null to use the default comparer System.Collections.Generic.Comparer&lt;T&gt;.Default.
            /// </param>
            /// <returns>
            /// The zero-based index of item in the sorted System.Collections.Generic.List&lt;T&gt;,
            /// if item is found; otherwise, a negative number that is the bitwise complement
            /// of the index of the next element that is larger than item or, if there is
            /// no larger element, the bitwise complement of System.Collections.Generic.List&lt;T&gt;.Count.
            /// </returns>
            /// <exception cref="InvalidOperationException">
            /// comparer is null, and the default comparer System.Collections.Generic.Comparer&lt;T&gt;.Default
            /// cannot find an implementation of the System.IComparable&lt;T&gt; generic interface
            /// or the System.IComparable interface for type T.
            /// </exception>
            public int BinarySearch(T item, IComparer<T>? comparer)
            {
                return this.BinarySearch(0, this.Count, item, comparer);
            }

            /// <summary>
            /// Searches a range of elements in the sorted System.Collections.Generic.List&lt;T&gt;
            /// for an element using the specified comparer and returns the zero-based index
            /// of the element.
            /// </summary>
            /// <param name="index">The zero-based starting index of the range to search.</param>
            /// <param name="count"> The length of the range to search.</param>
            /// <param name="item">The object to locate. The value can be null for reference types.</param>
            /// <param name="comparer">
            /// The System.Collections.Generic.IComparer&lt;T&gt; implementation to use when comparing
            /// elements, or null to use the default comparer System.Collections.Generic.Comparer&lt;T&gt;.Default.
            /// </param>
            /// <returns>
            /// The zero-based index of item in the sorted System.Collections.Generic.List&lt;T&gt;,
            /// if item is found; otherwise, a negative number that is the bitwise complement
            /// of the index of the next element that is larger than item or, if there is
            /// no larger element, the bitwise complement of System.Collections.Generic.List&lt;T&gt;.Count.
            /// </returns>
            /// <exception cref="ArgumentOutOfRangeException">
            /// index is less than 0.-or-count is less than 0.
            /// </exception>
            /// <exception cref="ArgumentException">
            /// index and count do not denote a valid range in the System.Collections.Generic.List&lt;T&gt;.
            /// </exception>
            /// <exception cref="InvalidOperationException">
            /// comparer is null, and the default comparer System.Collections.Generic.Comparer&lt;T&gt;.Default
            /// cannot find an implementation of the System.IComparable&lt;T&gt; generic interface
            /// or the System.IComparable interface for type T.
            /// </exception>
            public int BinarySearch(int index, int count, T item, IComparer<T>? comparer)
            {
                return this.Root.BinarySearch(index, count, item, comparer);
            }

            /// <summary>
            /// Creates an immutable list based on the contents of this instance.
            /// </summary>
            /// <returns>An immutable list.</returns>
            /// <remarks>
            /// This method is an O(n) operation, and approaches O(1) time as the number of
            /// actual mutations to the set since the last call to this method approaches 0.
            /// </remarks>
            public ImmutableList<T> ToImmutable()
            {
                // Creating an instance of ImmutableList<T> with our root node automatically freezes our tree,
                // ensuring that the returned instance is immutable.  Any further mutations made to this builder
                // will clone (and unfreeze) the spine of modified nodes until the next time this method is invoked.
                if (_immutable == null)
                {
                    _immutable = ImmutableList<T>.WrapNode(this.Root);
                }

                return _immutable;
            }

            #endregion

            #region IList members

            /// <summary>
            /// Adds an item to the <see cref="IList"/>.
            /// </summary>
            /// <param name="value">The object to add to the <see cref="IList"/>.</param>
            /// <returns>
            /// The position into which the new element was inserted, or -1 to indicate that the item was not inserted into the collection,
            /// </returns>
            int IList.Add(object? value)
            {
                this.Add((T)value!);
                return this.Count - 1;
            }

            /// <summary>
            /// Clears this instance.
            /// </summary>
            void IList.Clear()
            {
                this.Clear();
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
                    return this.Contains((T)value!);
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
                    return this.IndexOf((T)value!);
                }

                return -1;
            }

            /// <summary>
            /// Inserts an item to the <see cref="IList"/> at the specified index.
            /// </summary>
            /// <param name="index">The zero-based index at which <paramref name="value"/> should be inserted.</param>
            /// <param name="value">The object to insert into the <see cref="IList"/>.</param>
            void IList.Insert(int index, object? value)
            {
                this.Insert(index, (T)value!);
            }

            /// <summary>
            /// Gets a value indicating whether the <see cref="IList"/> has a fixed size.
            /// </summary>
            /// <returns>true if the <see cref="IList"/> has a fixed size; otherwise, false.</returns>
            bool IList.IsFixedSize
            {
                get { return false; }
            }

            /// <summary>
            /// Gets a value indicating whether the <see cref="ICollection{T}"/> is read-only.
            /// </summary>
            /// <returns>true if the <see cref="ICollection{T}"/> is read-only; otherwise, false.
            ///   </returns>
            bool IList.IsReadOnly
            {
                get { return false; }
            }

            /// <summary>
            /// Removes the first occurrence of a specific object from the <see cref="IList"/>.
            /// </summary>
            /// <param name="value">The object to remove from the <see cref="IList"/>.</param>
            void IList.Remove(object? value)
            {
                if (IsCompatibleObject(value))
                {
                    this.Remove((T)value!);
                }
            }

            /// <summary>
            /// Gets or sets the <see cref="object"/> at the specified index.
            /// </summary>
            /// <value>
            /// The <see cref="object"/>.
            /// </value>
            /// <param name="index">The index.</param>
            /// <returns></returns>
            object? IList.this[int index]
            {
                get { return this[index]; }
                set { this[index] = (T)value!; }
            }

            #endregion

            #region ICollection members

            /// <summary>
            /// Copies the elements of the <see cref="ICollection"/> to an <see cref="Array"/>, starting at a particular <see cref="Array"/> index.
            /// </summary>
            /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="ICollection"/>. The <see cref="Array"/> must have zero-based indexing.</param>
            /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
            void ICollection.CopyTo(Array array, int arrayIndex)
            {
                this.Root.CopyTo(array, arrayIndex);
            }

            /// <summary>
            /// Gets a value indicating whether access to the <see cref="ICollection"/> is synchronized (thread safe).
            /// </summary>
            /// <returns>true if access to the <see cref="ICollection"/> is synchronized (thread safe); otherwise, false.</returns>
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            bool ICollection.IsSynchronized
            {
                get { return false; }
            }

            /// <summary>
            /// Gets an object that can be used to synchronize access to the <see cref="ICollection"/>.
            /// </summary>
            /// <returns>An object that can be used to synchronize access to the <see cref="ICollection"/>.</returns>
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            object ICollection.SyncRoot
            {
                get
                {
                    if (_syncRoot == null)
                    {
                        System.Threading.Interlocked.CompareExchange<object?>(ref _syncRoot, new object(), null);
                    }

                    return _syncRoot;
                }
            }
            #endregion
        }
    }

    /// <summary>
    /// A simple view of the immutable list that the debugger can show to the developer.
    /// </summary>
    internal sealed class ImmutableListBuilderDebuggerProxy<T>
    {
        /// <summary>
        /// The collection to be enumerated.
        /// </summary>
        private readonly ImmutableList<T>.Builder _list;

        /// <summary>
        /// The simple view of the collection.
        /// </summary>
        private T[]? _cachedContents;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableListBuilderDebuggerProxy{T}"/> class.
        /// </summary>
        /// <param name="builder">The list to display in the debugger</param>
        public ImmutableListBuilderDebuggerProxy(ImmutableList<T>.Builder builder)
        {
            Requires.NotNull(builder, nameof(builder));
            _list = builder;
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
