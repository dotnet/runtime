// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Immutable
{
    /// <content>
    /// Contains the inner <see cref="ImmutableHashSet{T}.Builder"/> class.
    /// </content>
    public sealed partial class ImmutableHashSet<T>
    {
        /// <summary>
        /// A hash set that mutates with little or no memory allocations,
        /// can produce and/or build on immutable hash set instances very efficiently.
        /// </summary>
        /// <remarks>
        /// <para>
        /// While <see cref="ImmutableHashSet{T}.Union(IEnumerable{T})"/> and other bulk change methods
        /// already provide fast bulk change operations on the collection, this class allows
        /// multiple combinations of changes to be made to a set with equal efficiency.
        /// </para>
        /// <para>
        /// Instance members of this class are <em>not</em> thread-safe.
        /// </para>
        /// </remarks>
        [DebuggerDisplay("Count = {Count}")]
        public sealed class Builder : IReadOnlyCollection<T>, ISet<T>
        {
            /// <summary>
            /// The root of the binary tree that stores the collection.  Contents are typically not entirely frozen.
            /// </summary>
            private SortedInt32KeyNode<HashBucket> _root = SortedInt32KeyNode<HashBucket>.EmptyNode;

            /// <summary>
            /// The equality comparer.
            /// </summary>
            private IEqualityComparer<T> _equalityComparer;

            /// <summary>
            /// The equality comparer to use when balancing the tree of hash buckets.
            /// </summary>
            private readonly IEqualityComparer<HashBucket> _hashBucketEqualityComparer;

            /// <summary>
            /// The number of elements in this collection.
            /// </summary>
            private int _count;

            /// <summary>
            /// Caches an immutable instance that represents the current state of the collection.
            /// </summary>
            /// <value>Null if no immutable view has been created for the current version.</value>
            private ImmutableHashSet<T>? _immutable;

            /// <summary>
            /// A number that increments every time the builder changes its contents.
            /// </summary>
            private int _version;

            /// <summary>
            /// Initializes a new instance of the <see cref="ImmutableHashSet{T}.Builder"/> class.
            /// </summary>
            /// <param name="set">The set.</param>
            internal Builder(ImmutableHashSet<T> set)
            {
                Requires.NotNull(set, nameof(set));
                _root = set._root;
                _count = set._count;
                _equalityComparer = set._equalityComparer;
                _hashBucketEqualityComparer = set._hashBucketEqualityComparer;
                _immutable = set;
            }

            #region ISet<T> Properties

            /// <summary>
            /// Gets the number of elements contained in the <see cref="ICollection{T}"/>.
            /// </summary>
            /// <returns>The number of elements contained in the <see cref="ICollection{T}"/>.</returns>
            public int Count
            {
                get { return _count; }
            }

            /// <summary>
            /// Gets a value indicating whether the <see cref="ICollection{T}"/> is read-only.
            /// </summary>
            /// <returns>true if the <see cref="ICollection{T}"/> is read-only; otherwise, false.</returns>
            bool ICollection<T>.IsReadOnly
            {
                get { return false; }
            }

            #endregion

            /// <summary>
            /// Gets or sets the key comparer.
            /// </summary>
            /// <value>
            /// The key comparer.
            /// </value>
            public IEqualityComparer<T> KeyComparer
            {
                get
                {
                    return _equalityComparer;
                }

                set
                {
                    Requires.NotNull(value, nameof(value));

                    if (value != _equalityComparer)
                    {
                        var result = Union(this, new MutationInput(SortedInt32KeyNode<HashBucket>.EmptyNode, value, _hashBucketEqualityComparer, 0));

                        _immutable = null;
                        _equalityComparer = value;
                        this.Root = result.Root;
                        _count = result.Count; // whether the offset or absolute, since the base is 0, it's no difference.
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
            /// Gets the initial data to pass to a query or mutation method.
            /// </summary>
            private MutationInput Origin
            {
                get { return new MutationInput(this.Root, _equalityComparer, _hashBucketEqualityComparer, _count); }
            }

            /// <summary>
            /// Gets or sets the root of this data structure.
            /// </summary>
            private SortedInt32KeyNode<HashBucket> Root
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

            #region Public methods

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
            /// </returns>
            public Enumerator GetEnumerator()
            {
                return new Enumerator(_root, this);
            }

            /// <summary>
            /// Creates an immutable hash set based on the contents of this instance.
            /// </summary>
            /// <returns>An immutable set.</returns>
            /// <remarks>
            /// This method is an O(n) operation, and approaches O(1) time as the number of
            /// actual mutations to the set since the last call to this method approaches 0.
            /// </remarks>
            public ImmutableHashSet<T> ToImmutable()
            {
                // Creating an instance of ImmutableSortedMap<T> with our root node automatically freezes our tree,
                // ensuring that the returned instance is immutable.  Any further mutations made to this builder
                // will clone (and unfreeze) the spine of modified nodes until the next time this method is invoked.
                return _immutable ??= ImmutableHashSet<T>.Wrap(_root, _equalityComparer, _count);
            }

            /// <summary>
            /// Searches the set for a given value and returns the equal value it finds, if any.
            /// </summary>
            /// <param name="equalValue">The value for which to search.</param>
            /// <param name="actualValue">The value from the set that the search found, or the original value if the search yielded no match.</param>
            /// <returns>A value indicating whether the search was successful.</returns>
            public bool TryGetValue(T equalValue, out T actualValue)
            {
                int hashCode = equalValue != null ? _equalityComparer.GetHashCode(equalValue) : 0;
                if (_root.TryGetValue(hashCode, out HashBucket bucket))
                {
                    return bucket.TryExchange(equalValue, _equalityComparer, out actualValue);
                }

                actualValue = equalValue;
                return false;
            }

            #endregion

            #region ISet<T> Methods

            /// <summary>
            /// Adds the specified item.
            /// </summary>
            /// <param name="item">The item.</param>
            /// <returns>True if the item did not already belong to the collection.</returns>
            public bool Add(T item)
            {
                var result = ImmutableHashSet<T>.Add(item, this.Origin);
                this.Apply(result);
                return result.Count != 0;
            }

            /// <summary>
            /// Removes the first occurrence of a specific object from the <see cref="ICollection{T}"/>.
            /// </summary>
            /// <param name="item">The object to remove from the <see cref="ICollection{T}"/>.</param>
            /// <returns>
            /// true if <paramref name="item"/> was successfully removed from the <see cref="ICollection{T}"/>; otherwise, false. This method also returns false if <paramref name="item"/> is not found in the original <see cref="ICollection{T}"/>.
            /// </returns>
            /// <exception cref="NotSupportedException">The <see cref="ICollection{T}"/> is read-only.</exception>
            public bool Remove(T item)
            {
                var result = ImmutableHashSet<T>.Remove(item, this.Origin);
                this.Apply(result);
                return result.Count != 0;
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
                return ImmutableHashSet<T>.Contains(item, this.Origin);
            }

            /// <summary>
            /// Removes all items from the <see cref="ICollection{T}"/>.
            /// </summary>
            /// <exception cref="NotSupportedException">The <see cref="ICollection{T}"/> is read-only. </exception>
            public void Clear()
            {
                _count = 0;
                this.Root = SortedInt32KeyNode<HashBucket>.EmptyNode;
            }

            /// <summary>
            /// Removes all elements in the specified collection from the current set.
            /// </summary>
            /// <param name="other">The collection of items to remove from the set.</param>
            public void ExceptWith(IEnumerable<T> other)
            {
                var result = ImmutableHashSet<T>.Except(other, _equalityComparer, _hashBucketEqualityComparer, _root);
                this.Apply(result);
            }

            /// <summary>
            /// Modifies the current set so that it contains only elements that are also in a specified collection.
            /// </summary>
            /// <param name="other">The collection to compare to the current set.</param>
            public void IntersectWith(IEnumerable<T> other)
            {
                var result = ImmutableHashSet<T>.Intersect(other, this.Origin);
                this.Apply(result);
            }

            /// <summary>
            /// Determines whether the current set is a proper (strict) subset of a specified collection.
            /// </summary>
            /// <param name="other">The collection to compare to the current set.</param>
            /// <returns>true if the current set is a correct subset of other; otherwise, false.</returns>
            public bool IsProperSubsetOf(IEnumerable<T> other)
            {
                return ImmutableHashSet<T>.IsProperSubsetOf(other, this.Origin);
            }

            /// <summary>
            /// Determines whether the current set is a proper (strict) superset of a specified collection.
            /// </summary>
            /// <param name="other">The collection to compare to the current set.</param>
            /// <returns>true if the current set is a superset of other; otherwise, false.</returns>
            public bool IsProperSupersetOf(IEnumerable<T> other)
            {
                return ImmutableHashSet<T>.IsProperSupersetOf(other, this.Origin);
            }

            /// <summary>
            /// Determines whether the current set is a subset of a specified collection.
            /// </summary>
            /// <param name="other">The collection to compare to the current set.</param>
            /// <returns>true if the current set is a subset of other; otherwise, false.</returns>
            public bool IsSubsetOf(IEnumerable<T> other)
            {
                return ImmutableHashSet<T>.IsSubsetOf(other, this.Origin);
            }

            /// <summary>
            /// Determines whether the current set is a superset of a specified collection.
            /// </summary>
            /// <param name="other">The collection to compare to the current set.</param>
            /// <returns>true if the current set is a superset of other; otherwise, false.</returns>
            public bool IsSupersetOf(IEnumerable<T> other)
            {
                return ImmutableHashSet<T>.IsSupersetOf(other, this.Origin);
            }

            /// <summary>
            /// Determines whether the current set overlaps with the specified collection.
            /// </summary>
            /// <param name="other">The collection to compare to the current set.</param>
            /// <returns>true if the current set and other share at least one common element; otherwise, false.</returns>
            public bool Overlaps(IEnumerable<T> other)
            {
                return ImmutableHashSet<T>.Overlaps(other, this.Origin);
            }

            /// <summary>
            /// Determines whether the current set and the specified collection contain the same elements.
            /// </summary>
            /// <param name="other">The collection to compare to the current set.</param>
            /// <returns>true if the current set is equal to other; otherwise, false.</returns>
            public bool SetEquals(IEnumerable<T> other)
            {
                if (object.ReferenceEquals(this, other))
                {
                    return true;
                }

                return ImmutableHashSet<T>.SetEquals(other, this.Origin);
            }

            /// <summary>
            /// Modifies the current set so that it contains only elements that are present either in the current set or in the specified collection, but not both.
            /// </summary>
            /// <param name="other">The collection to compare to the current set.</param>
            public void SymmetricExceptWith(IEnumerable<T> other)
            {
                var result = ImmutableHashSet<T>.SymmetricExcept(other, this.Origin);
                this.Apply(result);
            }

            /// <summary>
            /// Modifies the current set so that it contains all elements that are present in both the current set and in the specified collection.
            /// </summary>
            /// <param name="other">The collection to compare to the current set.</param>
            public void UnionWith(IEnumerable<T> other)
            {
                var result = ImmutableHashSet<T>.Union(other, this.Origin);
                this.Apply(result);
            }

            #endregion

            #region ICollection<T> Members

            /// <summary>
            /// Adds an item to the <see cref="ICollection{T}"/>.
            /// </summary>
            /// <param name="item">The object to add to the <see cref="ICollection{T}"/>.</param>
            /// <exception cref="NotSupportedException">The <see cref="ICollection{T}"/> is read-only.</exception>
            void ICollection<T>.Add(T item)
            {
                this.Add(item);
            }

            /// <summary>
            /// See the <see cref="ICollection{T}"/> interface.
            /// </summary>
            void ICollection<T>.CopyTo(T[] array, int arrayIndex)
            {
                Requires.NotNull(array, nameof(array));
                Requires.Range(arrayIndex >= 0, nameof(arrayIndex));
                Requires.Range(array.Length >= arrayIndex + this.Count, nameof(arrayIndex));

                foreach (T item in this)
                {
                    array[arrayIndex++] = item;
                }
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

            /// <summary>
            /// Returns an enumerator that iterates through a collection.
            /// </summary>
            /// <returns>
            /// An <see cref="IEnumerator"/> object that can be used to iterate through the collection.
            /// </returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            #endregion

            /// <summary>
            /// Applies the result of some mutation operation to this instance.
            /// </summary>
            /// <param name="result">The result.</param>
            private void Apply(MutationResult result)
            {
                this.Root = result.Root;
                if (result.CountType == CountType.Adjustment)
                {
                    _count += result.Count;
                }
                else
                {
                    _count = result.Count;
                }
            }
        }
    }
}
