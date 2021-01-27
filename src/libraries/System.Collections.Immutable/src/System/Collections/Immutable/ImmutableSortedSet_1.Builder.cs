// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Immutable
{
    /// <content>
    /// Contains the inner <see cref="ImmutableSortedSet{T}.Builder"/> class.
    /// </content>
    public sealed partial class ImmutableSortedSet<T>
    {
        /// <summary>
        /// A sorted set that mutates with little or no memory allocations,
        /// can produce and/or build on immutable sorted set instances very efficiently.
        /// </summary>
        /// <remarks>
        /// <para>
        /// While <see cref="ImmutableSortedSet{T}.Union"/> and other bulk change methods
        /// already provide fast bulk change operations on the collection, this class allows
        /// multiple combinations of changes to be made to a set with equal efficiency.
        /// </para>
        /// <para>
        /// Instance members of this class are <em>not</em> thread-safe.
        /// </para>
        /// </remarks>
        [DebuggerDisplay("Count = {Count}")]
        [DebuggerTypeProxy(typeof(ImmutableSortedSetBuilderDebuggerProxy<>))]
        public sealed class Builder : ISortKeyCollection<T>, IReadOnlyCollection<T>, ISet<T>, ICollection
        {
            /// <summary>
            /// The root of the binary tree that stores the collection.  Contents are typically not entirely frozen.
            /// </summary>
            private ImmutableSortedSet<T>.Node _root = ImmutableSortedSet<T>.Node.EmptyNode;

            /// <summary>
            /// The comparer to use for sorting the set.
            /// </summary>
            private IComparer<T> _comparer = Comparer<T>.Default;

            /// <summary>
            /// Caches an immutable instance that represents the current state of the collection.
            /// </summary>
            /// <value>Null if no immutable view has been created for the current version.</value>
            private ImmutableSortedSet<T>? _immutable;

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
            /// <param name="set">A set to act as the basis for a new set.</param>
            internal Builder(ImmutableSortedSet<T> set)
            {
                Requires.NotNull(set, nameof(set));
                _root = set._root;
                _comparer = set.KeyComparer;
                _immutable = set;
            }

            #region ISet<T> Properties

            /// <summary>
            /// Gets the number of elements in this set.
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
            /// Gets the element of the set at the given index.
            /// </summary>
            /// <param name="index">The 0-based index of the element in the set to return.</param>
            /// <returns>The element at the given position.</returns>
            /// <remarks>
            /// No index setter is offered because the element being replaced may not sort
            /// to the same position in the sorted collection as the replacing element.
            /// </remarks>
            public T this[int index]
            {
#if !NETSTANDARD1_0
                get { return _root.ItemRef(index); }
#else
                get { return _root[index]; }
#endif
            }

#if !NETSTANDARD1_0
            /// <summary>
            /// Gets a read-only reference to the element of the set at the given index.
            /// </summary>
            /// <param name="index">The 0-based index of the element in the set to return.</param>
            /// <returns>A read-only reference to the element at the given position.</returns>
            public ref readonly T ItemRef(int index)
            {
                return ref _root.ItemRef(index);
            }
#endif

            /// <summary>
            /// Gets the maximum value in the collection, as defined by the comparer.
            /// </summary>
            /// <value>The maximum value in the set.</value>
            public T? Max
            {
                get { return _root.Max; }
            }

            /// <summary>
            /// Gets the minimum value in the collection, as defined by the comparer.
            /// </summary>
            /// <value>The minimum value in the set.</value>
            public T? Min
            {
                get { return _root.Min; }
            }

            /// <summary>
            ///  Gets or sets the <see cref="IComparer{T}"/> object that is used to determine equality for the values in the <see cref="ImmutableSortedSet{T}"/>.
            /// </summary>
            /// <value>The comparer that is used to determine equality for the values in the set.</value>
            /// <remarks>
            /// When changing the comparer in such a way as would introduce collisions, the conflicting elements are dropped,
            /// leaving only one of each matching pair in the collection.
            /// </remarks>
            public IComparer<T> KeyComparer
            {
                get
                {
                    return _comparer;
                }

                set
                {
                    Requires.NotNull(value, nameof(value));

                    if (value != _comparer)
                    {
                        var newRoot = Node.EmptyNode;
                        foreach (T item in this)
                        {
                            bool mutated;
                            newRoot = newRoot.Add(item, value, out mutated);
                        }

                        _immutable = null;
                        _comparer = value;
                        this.Root = newRoot;
                    }
                }
            }

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
            private Node Root
            {
                get
                {
                    return _root;
                }

                set
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

            #region ISet<T> Methods

            /// <summary>
            /// Adds an element to the current set and returns a value to indicate if the
            /// element was successfully added.
            /// </summary>
            /// <param name="item">The element to add to the set.</param>
            /// <returns>true if the element is added to the set; false if the element is already in the set.</returns>
            public bool Add(T item)
            {
                bool mutated;
                this.Root = this.Root.Add(item, _comparer, out mutated);
                return mutated;
            }

            /// <summary>
            /// Removes all elements in the specified collection from the current set.
            /// </summary>
            /// <param name="other">The collection of items to remove from the set.</param>
            public void ExceptWith(IEnumerable<T> other)
            {
                Requires.NotNull(other, nameof(other));

                foreach (T item in other)
                {
                    bool mutated;
                    this.Root = this.Root.Remove(item, _comparer, out mutated);
                }
            }

            /// <summary>
            /// Modifies the current set so that it contains only elements that are also in a specified collection.
            /// </summary>
            /// <param name="other">The collection to compare to the current set.</param>
            public void IntersectWith(IEnumerable<T> other)
            {
                Requires.NotNull(other, nameof(other));

                var result = ImmutableSortedSet<T>.Node.EmptyNode;
                foreach (T item in other)
                {
                    if (this.Contains(item))
                    {
                        bool mutated;
                        result = result.Add(item, _comparer, out mutated);
                    }
                }

                this.Root = result;
            }

            /// <summary>
            /// Determines whether the current set is a proper (strict) subset of a specified collection.
            /// </summary>
            /// <param name="other">The collection to compare to the current set.</param>
            /// <returns>true if the current set is a correct subset of other; otherwise, false.</returns>
            public bool IsProperSubsetOf(IEnumerable<T> other)
            {
                return this.ToImmutable().IsProperSubsetOf(other);
            }

            /// <summary>
            /// Determines whether the current set is a proper (strict) superset of a specified collection.
            /// </summary>
            /// <param name="other">The collection to compare to the current set.</param>
            /// <returns>true if the current set is a superset of other; otherwise, false.</returns>
            public bool IsProperSupersetOf(IEnumerable<T> other)
            {
                return this.ToImmutable().IsProperSupersetOf(other);
            }

            /// <summary>
            /// Determines whether the current set is a subset of a specified collection.
            /// </summary>
            /// <param name="other">The collection to compare to the current set.</param>
            /// <returns>true if the current set is a subset of other; otherwise, false.</returns>
            public bool IsSubsetOf(IEnumerable<T> other)
            {
                return this.ToImmutable().IsSubsetOf(other);
            }

            /// <summary>
            /// Determines whether the current set is a superset of a specified collection.
            /// </summary>
            /// <param name="other">The collection to compare to the current set.</param>
            /// <returns>true if the current set is a superset of other; otherwise, false.</returns>
            public bool IsSupersetOf(IEnumerable<T> other)
            {
                return this.ToImmutable().IsSupersetOf(other);
            }

            /// <summary>
            /// Determines whether the current set overlaps with the specified collection.
            /// </summary>
            /// <param name="other">The collection to compare to the current set.</param>
            /// <returns>true if the current set and other share at least one common element; otherwise, false.</returns>
            public bool Overlaps(IEnumerable<T> other)
            {
                return this.ToImmutable().Overlaps(other);
            }

            /// <summary>
            /// Determines whether the current set and the specified collection contain the same elements.
            /// </summary>
            /// <param name="other">The collection to compare to the current set.</param>
            /// <returns>true if the current set is equal to other; otherwise, false.</returns>
            public bool SetEquals(IEnumerable<T> other)
            {
                return this.ToImmutable().SetEquals(other);
            }

            /// <summary>
            /// Modifies the current set so that it contains only elements that are present either in the current set or in the specified collection, but not both.
            /// </summary>
            /// <param name="other">The collection to compare to the current set.</param>
            public void SymmetricExceptWith(IEnumerable<T> other)
            {
                this.Root = this.ToImmutable().SymmetricExcept(other)._root;
            }

            /// <summary>
            /// Modifies the current set so that it contains all elements that are present in both the current set and in the specified collection.
            /// </summary>
            /// <param name="other">The collection to compare to the current set.</param>
            public void UnionWith(IEnumerable<T> other)
            {
                Requires.NotNull(other, nameof(other));

                foreach (T item in other)
                {
                    bool mutated;
                    this.Root = this.Root.Add(item, _comparer, out mutated);
                }
            }

            /// <summary>
            /// Adds an element to the current set and returns a value to indicate if the
            /// element was successfully added.
            /// </summary>
            /// <param name="item">The element to add to the set.</param>
            void ICollection<T>.Add(T item)
            {
                this.Add(item);
            }

            /// <summary>
            /// Removes all elements from this set.
            /// </summary>
            public void Clear()
            {
                this.Root = ImmutableSortedSet<T>.Node.EmptyNode;
            }

            /// <summary>
            /// Determines whether the set contains a specific value.
            /// </summary>
            /// <param name="item">The object to locate in the set.</param>
            /// <returns>true if item is found in the set; false otherwise.</returns>
            public bool Contains(T item)
            {
                return this.Root.Contains(item, _comparer);
            }

            /// <summary>
            /// See <see cref="ICollection{T}"/>
            /// </summary>
            void ICollection<T>.CopyTo(T[] array, int arrayIndex)
            {
                _root.CopyTo(array, arrayIndex);
            }

            /// <summary>
            ///  Removes the first occurrence of a specific object from the set.
            /// </summary>
            /// <param name="item">The object to remove from the set.</param>
            /// <returns><c>true</c> if the item was removed from the set; <c>false</c> if the item was not found in the set.</returns>
            public bool Remove(T item)
            {
                bool mutated;
                this.Root = this.Root.Remove(item, _comparer, out mutated);
                return mutated;
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>A enumerator that can be used to iterate through the collection.</returns>
            public ImmutableSortedSet<T>.Enumerator GetEnumerator()
            {
                return this.Root.GetEnumerator(this);
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>A enumerator that can be used to iterate through the collection.</returns>
            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                return this.Root.GetEnumerator();
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>A enumerator that can be used to iterate through the collection.</returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            #endregion

            /// <summary>
            /// Returns an <see cref="IEnumerable{T}"/> that iterates over this
            /// collection in reverse order.
            /// </summary>
            /// <returns>
            /// An enumerator that iterates over the <see cref="ImmutableSortedSet{T}.Builder"/>
            /// in reverse order.
            /// </returns>
            public IEnumerable<T> Reverse()
            {
                return new ReverseEnumerable(_root);
            }

            /// <summary>
            /// Creates an immutable sorted set based on the contents of this instance.
            /// </summary>
            /// <returns>An immutable set.</returns>
            /// <remarks>
            /// This method is an O(n) operation, and approaches O(1) time as the number of
            /// actual mutations to the set since the last call to this method approaches 0.
            /// </remarks>
            public ImmutableSortedSet<T> ToImmutable()
            {
                // Creating an instance of ImmutableSortedSet<T> with our root node automatically freezes our tree,
                // ensuring that the returned instance is immutable.  Any further mutations made to this builder
                // will clone (and unfreeze) the spine of modified nodes until the next time this method is invoked.
                if (_immutable == null)
                {
                    _immutable = ImmutableSortedSet<T>.Wrap(this.Root, _comparer);
                }

                return _immutable;
            }

            /// <summary>
            /// Searches the set for a given value and returns the equal value it finds, if any.
            /// </summary>
            /// <param name="equalValue">The value for which to search.</param>
            /// <param name="actualValue">The value from the set that the search found, or the original value if the search yielded no match.</param>
            /// <returns>A value indicating whether the search was successful.</returns>
            public bool TryGetValue(T equalValue, out T actualValue)
            {
                Node searchResult = _root.Search(equalValue, _comparer);
                if (!searchResult.IsEmpty)
                {
                    actualValue = searchResult.Key;
                    return true;
                }

                actualValue = equalValue;
                return false;
            }

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
                        Threading.Interlocked.CompareExchange<object?>(ref _syncRoot, new object(), null);
                    }

                    return _syncRoot;
                }
            }
            #endregion
        }
    }
}
