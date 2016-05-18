// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;

namespace System.Collections.Immutable
{
    /// <summary>
    /// An immutable sorted set implementation.
    /// </summary>
    /// <typeparam name="T">The type of elements in the set.</typeparam>
    /// <devremarks>
    /// We implement <see cref="IReadOnlyList{T}"/> because it adds an ordinal indexer.
    /// We implement <see cref="IList{T}"/> because it gives us <see cref="IList{T}.IndexOf"/>, which is important for some folks.
    /// </devremarks>
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(ImmutableSortedSetDebuggerProxy<>))]
    public sealed partial class ImmutableSortedSet<T> : IImmutableSet<T>, ISortKeyCollection<T>, IReadOnlyList<T>, IList<T>, ISet<T>, IList, IStrongEnumerable<T, ImmutableSortedSet<T>.Enumerator>
    {
        /// <summary>
        /// This is the factor between the small collection's size and the large collection's size in a bulk operation,
        /// under which recreating the entire collection using a fast method rather than some incremental update
        /// (that requires tree rebalancing) is preferable.
        /// </summary>
        private const float RefillOverIncrementalThreshold = 0.15f;

        /// <summary>
        /// An empty sorted set with the default sort comparer.
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly ImmutableSortedSet<T> Empty = new ImmutableSortedSet<T>();

        /// <summary>
        /// The root node of the AVL tree that stores this set.
        /// </summary>
        private readonly Node _root;

        /// <summary>
        /// The comparer used to sort elements in this set.
        /// </summary>
        private readonly IComparer<T> _comparer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableSortedSet{T}"/> class.
        /// </summary>
        /// <param name="comparer">The comparer.</param>
        internal ImmutableSortedSet(IComparer<T> comparer = null)
        {
            _root = Node.EmptyNode;
            _comparer = comparer ?? Comparer<T>.Default;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableSortedSet{T}"/> class.
        /// </summary>
        /// <param name="root">The root of the AVL tree with the contents of this set.</param>
        /// <param name="comparer">The comparer.</param>
        private ImmutableSortedSet(Node root, IComparer<T> comparer)
        {
            Requires.NotNull(root, nameof(root));
            Requires.NotNull(comparer, nameof(comparer));

            root.Freeze();
            _root = root;
            _comparer = comparer;
        }

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        public ImmutableSortedSet<T> Clear()
        {
            Contract.Ensures(Contract.Result<ImmutableSortedSet<T>>() != null);
            Contract.Ensures(Contract.Result<ImmutableSortedSet<T>>().IsEmpty);
            return _root.IsEmpty ? this : Empty.WithComparer(_comparer);
        }

        /// <summary>
        /// Gets the maximum value in the collection, as defined by the comparer.
        /// </summary>
        /// <value>The maximum value in the set.</value>
        public T Max
        {
            get { return _root.Max; }
        }

        /// <summary>
        /// Gets the minimum value in the collection, as defined by the comparer.
        /// </summary>
        /// <value>The minimum value in the set.</value>
        public T Min
        {
            get { return _root.Min; }
        }

        #region IImmutableSet<T> Properties

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        public bool IsEmpty
        {
            get { return _root.IsEmpty; }
        }

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        public int Count
        {
            get { return _root.Count; }
        }

        #endregion

        #region ISortKeyCollection<T> Properties

        /// <summary>
        /// See the <see cref="ISortKeyCollection{T}"/> interface.
        /// </summary>
        public IComparer<T> KeyComparer
        {
            get { return _comparer; }
        }

        #endregion

        /// <summary>
        /// Gets the root node (for testing purposes).
        /// </summary>
        internal IBinaryTree Root
        {
            get { return _root; }
        }

        #region IReadOnlyList<T> Indexers

        /// <summary>
        /// Gets the element of the set at the given index.
        /// </summary>
        /// <param name="index">The 0-based index of the element in the set to return.</param>
        /// <returns>The element at the given position.</returns>
        public T this[int index]
        {
            get
            {
                return _root[index];
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
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        [Pure]
        public ImmutableSortedSet<T> Add(T value)
        {
            Requires.NotNullAllowStructs(value, nameof(value));
            Contract.Ensures(Contract.Result<ImmutableSortedSet<T>>() != null);
            bool mutated;
            return this.Wrap(_root.Add(value, _comparer, out mutated));
        }

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        [Pure]
        public ImmutableSortedSet<T> Remove(T value)
        {
            Requires.NotNullAllowStructs(value, nameof(value));
            Contract.Ensures(Contract.Result<ImmutableSortedSet<T>>() != null);
            bool mutated;
            return this.Wrap(_root.Remove(value, _comparer, out mutated));
        }

        /// <summary>
        /// Searches the set for a given value and returns the equal value it finds, if any.
        /// </summary>
        /// <param name="equalValue">The value to search for.</param>
        /// <param name="actualValue">The value from the set that the search found, or the original value if the search yielded no match.</param>
        /// <returns>A value indicating whether the search was successful.</returns>
        /// <remarks>
        /// This can be useful when you want to reuse a previously stored reference instead of 
        /// a newly constructed one (so that more sharing of references can occur) or to look up
        /// a value that has more complete data than the value you currently have, although their
        /// comparer functions indicate they are equal.
        /// </remarks>
        [Pure]
        public bool TryGetValue(T equalValue, out T actualValue)
        {
            Requires.NotNullAllowStructs(equalValue, nameof(equalValue));

            Node searchResult = _root.Search(equalValue, _comparer);
            if (searchResult.IsEmpty)
            {
                actualValue = equalValue;
                return false;
            }
            else
            {
                actualValue = searchResult.Key;
                return true;
            }
        }

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        [Pure]
        public ImmutableSortedSet<T> Intersect(IEnumerable<T> other)
        {
            Requires.NotNull(other, nameof(other));
            Contract.Ensures(Contract.Result<ImmutableSortedSet<T>>() != null);
            var newSet = this.Clear();
            foreach (var item in other.GetEnumerableDisposable<T, Enumerator>())
            {
                if (this.Contains(item))
                {
                    newSet = newSet.Add(item);
                }
            }

            return newSet;
        }

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        [Pure]
        public ImmutableSortedSet<T> Except(IEnumerable<T> other)
        {
            Requires.NotNull(other, nameof(other));

            var result = _root;
            foreach (T item in other.GetEnumerableDisposable<T, Enumerator>())
            {
                bool mutated;
                result = result.Remove(item, _comparer, out mutated);
            }

            return this.Wrap(result);
        }

        /// <summary>
        /// Produces a set that contains elements either in this set or a given sequence, but not both.
        /// </summary>
        /// <param name="other">The other sequence of items.</param>
        /// <returns>The new set.</returns>
        [Pure]
        public ImmutableSortedSet<T> SymmetricExcept(IEnumerable<T> other)
        {
            Requires.NotNull(other, nameof(other));

            var otherAsSet = ImmutableSortedSet.CreateRange(_comparer, other);

            var result = this.Clear();
            foreach (T item in this)
            {
                if (!otherAsSet.Contains(item))
                {
                    result = result.Add(item);
                }
            }

            foreach (T item in otherAsSet)
            {
                if (!this.Contains(item))
                {
                    result = result.Add(item);
                }
            }

            return result;
        }

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        [Pure]
        public ImmutableSortedSet<T> Union(IEnumerable<T> other)
        {
            Requires.NotNull(other, nameof(other));
            Contract.Ensures(Contract.Result<ImmutableSortedSet<T>>() != null);

            ImmutableSortedSet<T> immutableSortedSet;
            if (TryCastToImmutableSortedSet(other, out immutableSortedSet) && immutableSortedSet.KeyComparer == this.KeyComparer) // argument is a compatible immutable sorted set
            {
                if (immutableSortedSet.IsEmpty)
                {
                    return this;
                }
                else if (this.IsEmpty)
                {
                    // Adding the argument to this collection is equivalent to returning the argument.
                    return immutableSortedSet;
                }
                else if (immutableSortedSet.Count > this.Count)
                {
                    // We're adding a larger set to a smaller set, so it would be faster to simply
                    // add the smaller set to the larger set.
                    return immutableSortedSet.Union(this);
                }
            }

            int count;
            if (this.IsEmpty || (other.TryGetCount(out count) && (this.Count + count) * RefillOverIncrementalThreshold > this.Count))
            {
                // The payload being added is so large compared to this collection's current size
                // that we likely won't see much memory reuse in the node tree by performing an
                // incremental update.  So just recreate the entire node tree since that will
                // likely be faster.
                return this.LeafToRootRefill(other);
            }

            return this.UnionIncremental(other);
        }

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        [Pure]
        public ImmutableSortedSet<T> WithComparer(IComparer<T> comparer)
        {
            Contract.Ensures(Contract.Result<ImmutableSortedSet<T>>() != null);
            if (comparer == null)
            {
                comparer = Comparer<T>.Default;
            }

            if (comparer == _comparer)
            {
                return this;
            }
            else
            {
                var result = new ImmutableSortedSet<T>(Node.EmptyNode, comparer);
                result = result.Union(this);
                return result;
            }
        }

        /// <summary>
        /// Checks whether a given sequence of items entirely describe the contents of this set.
        /// </summary>
        /// <param name="other">The sequence of items to check against this set.</param>
        /// <returns>A value indicating whether the sets are equal.</returns>
        [Pure]
        public bool SetEquals(IEnumerable<T> other)
        {
            Requires.NotNull(other, nameof(other));

            if (object.ReferenceEquals(this, other))
            {
                return true;
            }

            var otherSet = new SortedSet<T>(other, this.KeyComparer);
            if (this.Count != otherSet.Count)
            {
                return false;
            }

            int matches = 0;
            foreach (T item in otherSet)
            {
                if (!this.Contains(item))
                {
                    return false;
                }

                matches++;
            }

            return matches == this.Count;
        }

        /// <summary>
        /// Determines whether the current set is a property (strict) subset of a specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns>true if the current set is a correct subset of other; otherwise, false.</returns>
        [Pure]
        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            Requires.NotNull(other, nameof(other));

            if (this.IsEmpty)
            {
                return other.Any();
            }

            // To determine whether everything we have is also in another sequence,
            // we enumerate the sequence and "tag" whether it's in this collection,
            // then consider whether every element in this collection was tagged.
            // Since this collection is immutable we cannot directly tag.  So instead
            // we simply count how many "hits" we have and ensure it's equal to the
            // size of this collection.  Of course for this to work we need to ensure
            // the uniqueness of items in the given sequence, so we create a set based
            // on the sequence first.
            var otherSet = new SortedSet<T>(other, this.KeyComparer);
            if (this.Count >= otherSet.Count)
            {
                return false;
            }

            int matches = 0;
            bool extraFound = false;
            foreach (T item in otherSet)
            {
                if (this.Contains(item))
                {
                    matches++;
                }
                else
                {
                    extraFound = true;
                }

                if (matches == this.Count && extraFound)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether the current set is a correct superset of a specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns>true if the current set is a correct superset of other; otherwise, false.</returns>
        [Pure]
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            Requires.NotNull(other, nameof(other));

            if (this.IsEmpty)
            {
                return false;
            }

            int count = 0;
            foreach (T item in other.GetEnumerableDisposable<T, Enumerator>())
            {
                count++;
                if (!this.Contains(item))
                {
                    return false;
                }
            }

            return this.Count > count;
        }

        /// <summary>
        /// Determines whether a set is a subset of a specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns>true if the current set is a subset of other; otherwise, false.</returns>
        [Pure]
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            Requires.NotNull(other, nameof(other));

            if (this.IsEmpty)
            {
                return true;
            }

            // To determine whether everything we have is also in another sequence,
            // we enumerate the sequence and "tag" whether it's in this collection,
            // then consider whether every element in this collection was tagged.
            // Since this collection is immutable we cannot directly tag.  So instead
            // we simply count how many "hits" we have and ensure it's equal to the
            // size of this collection.  Of course for this to work we need to ensure
            // the uniqueness of items in the given sequence, so we create a set based
            // on the sequence first.
            var otherSet = new SortedSet<T>(other, this.KeyComparer);
            int matches = 0;
            foreach (T item in otherSet)
            {
                if (this.Contains(item))
                {
                    matches++;
                }
            }

            return matches == this.Count;
        }

        /// <summary>
        /// Determines whether the current set is a superset of a specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns>true if the current set is a superset of other; otherwise, false.</returns>
        [Pure]
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            Requires.NotNull(other, nameof(other));

            foreach (T item in other.GetEnumerableDisposable<T, Enumerator>())
            {
                if (!this.Contains(item))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether the current set overlaps with the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns>true if the current set and other share at least one common element; otherwise, false.</returns>
        [Pure]
        public bool Overlaps(IEnumerable<T> other)
        {
            Requires.NotNull(other, nameof(other));

            if (this.IsEmpty)
            {
                return false;
            }

            foreach (T item in other.GetEnumerableDisposable<T, Enumerator>())
            {
                if (this.Contains(item))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns an <see cref="IEnumerable{T}"/> that iterates over this
        /// collection in reverse order.
        /// </summary>
        /// <returns>
        /// An enumerator that iterates over the <see cref="ImmutableSortedSet{T}"/>
        /// in reverse order.
        /// </returns>
        [Pure]
        public IEnumerable<T> Reverse()
        {
            return new ReverseEnumerable(_root);
        }

        /// <summary>
        /// Gets the position within this set that the specified value does or would appear.
        /// </summary>
        /// <param name="item">The value whose position is being sought.</param>
        /// <returns>
        /// The index of the specified <paramref name="item"/> in the sorted set,
        /// if <paramref name="item"/> is found.  If <paramref name="item"/> is not 
        /// found and <paramref name="item"/> is less than one or more elements in this set, 
        /// a negative number which is the bitwise complement of the index of the first 
        /// element that is larger than value. If <paramref name="item"/> is not found 
        /// and <paramref name="item"/> is greater than any of the elements in the set,
        /// a negative number which is the bitwise complement of (the index of the last
        /// element plus 1).
        /// </returns>
        public int IndexOf(T item)
        {
            Requires.NotNullAllowStructs(item, nameof(item));
            return _root.IndexOf(item, _comparer);
        }

        #endregion

        #region IImmutableSet<T> Members

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        public bool Contains(T value)
        {
            Requires.NotNullAllowStructs(value, nameof(value));
            return _root.Contains(value, _comparer);
        }

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableSet<T> IImmutableSet<T>.Clear()
        {
            return this.Clear();
        }

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableSet<T> IImmutableSet<T>.Add(T value)
        {
            return this.Add(value);
        }

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableSet<T> IImmutableSet<T>.Remove(T value)
        {
            return this.Remove(value);
        }

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableSet<T> IImmutableSet<T>.Intersect(IEnumerable<T> other)
        {
            return this.Intersect(other);
        }

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableSet<T> IImmutableSet<T>.Except(IEnumerable<T> other)
        {
            return this.Except(other);
        }

        /// <summary>
        /// Produces a set that contains elements either in this set or a given sequence, but not both.
        /// </summary>
        /// <param name="other">The other sequence of items.</param>
        /// <returns>The new set.</returns>
        [ExcludeFromCodeCoverage]
        IImmutableSet<T> IImmutableSet<T>.SymmetricExcept(IEnumerable<T> other)
        {
            return this.SymmetricExcept(other);
        }

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableSet<T> IImmutableSet<T>.Union(IEnumerable<T> other)
        {
            return this.Union(other);
        }

        #endregion

        #region ISet<T> Members

        /// <summary>
        /// See <see cref="ISet{T}"/>
        /// </summary>
        bool ISet<T>.Add(T item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// See <see cref="ISet{T}"/>
        /// </summary>
        void ISet<T>.ExceptWith(IEnumerable<T> other)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// See <see cref="ISet{T}"/>
        /// </summary>
        void ISet<T>.IntersectWith(IEnumerable<T> other)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// See <see cref="ISet{T}"/>
        /// </summary>
        void ISet<T>.SymmetricExceptWith(IEnumerable<T> other)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// See <see cref="ISet{T}"/>
        /// </summary>
        void ISet<T>.UnionWith(IEnumerable<T> other)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region ICollection<T> members

        /// <summary>
        /// See the <see cref="ICollection{T}"/> interface.
        /// </summary>
        bool ICollection<T>.IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        /// See the <see cref="ICollection{T}"/> interface.
        /// </summary>
        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            _root.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// See the <see cref="IList{T}"/> interface.
        /// </summary>
        void ICollection<T>.Add(T item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// See the <see cref="ICollection{T}"/> interface.
        /// </summary>
        void ICollection<T>.Clear()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// See the <see cref="IList{T}"/> interface.
        /// </summary>
        bool ICollection<T>.Remove(T item)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region IList<T> methods

        /// <summary>
        /// See the <see cref="IList{T}"/> interface.
        /// </summary>
        T IList<T>.this[int index]
        {
            get { return this[index]; }
            set { throw new NotSupportedException(); }
        }

        /// <summary>
        /// See the <see cref="IList{T}"/> interface.
        /// </summary>
        void IList<T>.Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// See the <see cref="IList{T}"/> interface.
        /// </summary>
        void IList<T>.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region IList properties

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

        #region IList methods

        /// <summary>
        /// Adds an item to the <see cref="IList"/>.
        /// </summary>
        /// <param name="value">The object to add to the <see cref="IList"/>.</param>
        /// <returns>
        /// The position into which the new element was inserted, or -1 to indicate that the item was not inserted into the collection,
        /// </returns>
        /// <exception cref="System.NotSupportedException"></exception>
        int IList.Add(object value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        /// <exception cref="System.NotSupportedException"></exception>
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
            return this.Contains((T)value);
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
            return this.IndexOf((T)value);
        }

        /// <summary>
        /// Inserts an item to the <see cref="IList"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="value"/> should be inserted.</param>
        /// <param name="value">The object to insert into the <see cref="IList"/>.</param>
        /// <exception cref="System.NotSupportedException"></exception>
        void IList.Insert(int index, object value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="IList"/>.
        /// </summary>
        /// <param name="value">The object to remove from the <see cref="IList"/>.</param>
        /// <exception cref="System.NotSupportedException"></exception>
        void IList.Remove(object value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Removes at.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <exception cref="System.NotSupportedException"></exception>
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
        /// <exception cref="System.NotSupportedException"></exception>
        object IList.this[int index]
        {
            get { return this[index]; }
            set { throw new NotSupportedException(); }
        }

        #endregion

        #region ICollection Methods

        /// <summary>
        /// Copies the elements of the <see cref="ICollection"/> to an <see cref="Array"/>, starting at a particular <see cref="Array"/> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="ICollection"/>. The <see cref="Array"/> must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        void ICollection.CopyTo(Array array, int index)
        {
            _root.CopyTo(array, index);
        }

        #endregion

        #region IEnumerable<T> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
        /// </returns>
        [ExcludeFromCodeCoverage]
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
        [ExcludeFromCodeCoverage]
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
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
            return _root.GetEnumerator();
        }

        /// <summary>
        /// Discovers an immutable sorted set for a given value, if possible.
        /// </summary>
        private static bool TryCastToImmutableSortedSet(IEnumerable<T> sequence, out ImmutableSortedSet<T> other)
        {
            other = sequence as ImmutableSortedSet<T>;
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
        /// Creates a new sorted set wrapper for a node tree.
        /// </summary>
        /// <param name="root">The root of the collection.</param>
        /// <param name="comparer">The comparer used to build the tree.</param>
        /// <returns>The immutable sorted set instance.</returns>
        [Pure]
        private static ImmutableSortedSet<T> Wrap(Node root, IComparer<T> comparer)
        {
            return root.IsEmpty
                ? ImmutableSortedSet<T>.Empty.WithComparer(comparer)
                : new ImmutableSortedSet<T>(root, comparer);
        }

        /// <summary>
        /// Adds items to this collection using the standard spine rewrite and tree rebalance technique.
        /// </summary>
        /// <param name="items">The items to add.</param>
        /// <returns>The new collection.</returns>
        /// <remarks>
        /// This method is least demanding on memory, providing the great chance of memory reuse
        /// and does not require allocating memory large enough to store all items contiguously.
        /// It's performance is optimal for additions that do not significantly dwarf the existing
        /// size of this collection.
        /// </remarks>
        [Pure]
        private ImmutableSortedSet<T> UnionIncremental(IEnumerable<T> items)
        {
            Requires.NotNull(items, nameof(items));
            Contract.Ensures(Contract.Result<ImmutableSortedSet<T>>() != null);

            // Let's not implement in terms of ImmutableSortedSet.Add so that we're
            // not unnecessarily generating a new wrapping set object for each item.
            var result = _root;
            foreach (var item in items.GetEnumerableDisposable<T, Enumerator>())
            {
                bool mutated;
                result = result.Add(item, _comparer, out mutated);
            }

            return this.Wrap(result);
        }

        /// <summary>
        /// Creates a wrapping collection type around a root node.
        /// </summary>
        /// <param name="root">The root node to wrap.</param>
        /// <returns>A wrapping collection type for the new tree.</returns>
        [Pure]
        private ImmutableSortedSet<T> Wrap(Node root)
        {
            if (root != _root)
            {
                return root.IsEmpty ? this.Clear() : new ImmutableSortedSet<T>(root, _comparer);
            }
            else
            {
                return this;
            }
        }

        /// <summary>
        /// Creates an immutable sorted set with the contents from this collection and a sequence of elements.
        /// </summary>
        /// <param name="addedItems">The sequence of elements to add to this set.</param>
        /// <returns>The immutable sorted set.</returns>
        [Pure]
        private ImmutableSortedSet<T> LeafToRootRefill(IEnumerable<T> addedItems)
        {
            Requires.NotNull(addedItems, nameof(addedItems));
            Contract.Ensures(Contract.Result<ImmutableSortedSet<T>>() != null);

            // Rather than build up the immutable structure in the incremental way,
            // build it in such a way as to generate minimal garbage, by assembling
            // the immutable binary tree from leaf to root.  This requires
            // that we know the length of the item sequence in advance, sort it, 
            // and can index into that sequence like a list, so the limited
            // garbage produced is a temporary mutable data structure we use
            // as a reference when creating the immutable one.

            // Produce the initial list containing all elements, including any duplicates.
            List<T> list;
            if (this.IsEmpty)
            {
                // If the additional items enumerable list is known to be empty, too,
                // then just return this empty instance.
                int count;
                if (addedItems.TryGetCount(out count) && count == 0)
                {
                    return this;
                }

                // Otherwise, construct a list from the items.  The Count could still
                // be zero, in which case, again, just return this empty instance.
                list = new List<T>(addedItems);
                if (list.Count == 0)
                {
                    return this;
                }
            }
            else
            {
                // Build the list from this set and then add the additional items.
                // Even if the additional items is empty, this set isn't, so we know
                // the resulting list will not be empty.
                list = new List<T>(this);
                list.AddRange(addedItems);
            }
            Debug.Assert(list.Count > 0);

            // Sort the list and remove duplicate entries.
            IComparer<T> comparer = this.KeyComparer;
            list.Sort(comparer);
            int index = 1;
            for (int i = 1; i < list.Count; i++)
            {
                if (comparer.Compare(list[i], list[i - 1]) != 0)
                {
                    list[index++] = list[i];
                }
            }
            list.RemoveRange(index, list.Count - index);

            // Use the now sorted list of unique items to construct a new sorted set.
            Node root = Node.NodeTreeFromList(list.AsOrderedCollection(), 0, list.Count);
            return this.Wrap(root);
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
            private static readonly SecureObjectPool<Stack<RefAsValueType<Node>>, Enumerator> s_enumeratingStacks =
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
            /// A flag indicating whether this enumerator works in reverse sort order.
            /// </summary>
            private readonly bool _reverse;

            /// <summary>
            /// The set being enumerated.
            /// </summary>
            private Node _root;

            /// <summary>
            /// The stack to use for enumerating the binary tree.
            /// </summary>
            /// <remarks>
            /// We use <see cref="RefAsValueType{T}"/> as a wrapper to avoid paying the cost of covariant checks whenever
            /// the underlying array that the <see cref="Stack{T}"/> class uses is written to. 
            /// We've recognized this as a perf win in ETL traces for these stack frames:
            /// clr!JIT_Stelem_Ref
            ///   clr!ArrayStoreCheck
            ///     clr!ObjIsInstanceOf
            /// </remarks>
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
            /// <param name="reverse"><c>true</c> to enumerate the collection in reverse.</param>
            internal Enumerator(Node root, Builder builder = null, bool reverse = false)
            {
                Requires.NotNull(root, nameof(root));

                _root = root;
                _builder = builder;
                _current = null;
                _reverse = reverse;
                _enumeratingBuilderVersion = builder != null ? builder.Version : -1;
                _poolUserId = SecureObjectPool.NewId();
                _stack = null;
                if (!s_enumeratingStacks.TryTake(this, out _stack))
                {
                    _stack = s_enumeratingStacks.PrepNew(this, new Stack<RefAsValueType<Node>>(root.Height));
                }

                this.PushNext(_root);
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
                    s_enumeratingStacks.TryAdd(this, _stack);
                    _stack = null;
                }
            }

            /// <summary>
            /// Advances enumeration to the next element.
            /// </summary>
            /// <returns>A value indicating whether there is another element in the enumeration.</returns>
            public bool MoveNext()
            {
                this.ThrowIfDisposed();
                this.ThrowIfChanged();

                var stack = _stack.Use(ref this);
                if (stack.Count > 0)
                {
                    Node n = stack.Pop().Value;
                    _current = n;
                    this.PushNext(_reverse ? n.Left : n.Right);
                    return true;
                }
                else
                {
                    _current = null;
                    return false;
                }
            }

            /// <summary>
            /// Restarts enumeration.
            /// </summary>
            public void Reset()
            {
                this.ThrowIfDisposed();

                _enumeratingBuilderVersion = _builder != null ? _builder.Version : -1;
                _current = null;
                var stack = _stack.Use(ref this);
                stack.ClearFastWhenEmpty();
                this.PushNext(_root);
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
            /// Pushes this node and all its Left (or Right, if reversed) descendants onto the stack.
            /// </summary>
            /// <param name="node">The starting node to push onto the stack.</param>
            private void PushNext(Node node)
            {
                Requires.NotNull(node, nameof(node));
                var stack = _stack.Use(ref this);
                while (!node.IsEmpty)
                {
                    stack.Push(new RefAsValueType<Node>(node));
                    node = _reverse ? node.Right : node.Left;
                }
            }
        }

        /// <summary>
        /// An reverse enumerable of a sorted set.
        /// </summary>
        private class ReverseEnumerable : IEnumerable<T>
        {
            /// <summary>
            /// The root node to enumerate.
            /// </summary>
            private readonly Node _root;

            /// <summary>
            /// Initializes a new instance of the <see cref="ImmutableSortedSet{T}.ReverseEnumerable"/> class.
            /// </summary>
            /// <param name="root">The root of the data structure to reverse enumerate.</param>
            internal ReverseEnumerable(Node root)
            {
                Requires.NotNull(root, nameof(root));
                _root = root;
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
            /// </returns>
            public IEnumerator<T> GetEnumerator()
            {
                return _root.Reverse();
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
            private readonly T _key;

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
            /// Initializes a new instance of the <see cref="ImmutableSortedSet{T}.Node"/> class
            /// that is pre-frozen.
            /// </summary>
            private Node()
            {
                Contract.Ensures(this.IsEmpty);
                _frozen = true; // the empty node is *always* frozen.
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ImmutableSortedSet{T}.Node"/> class
            /// that is not yet frozen.
            /// </summary>
            /// <param name="key">The value stored by this node.</param>
            /// <param name="left">The left branch.</param>
            /// <param name="right">The right branch.</param>
            /// <param name="frozen">Whether this node is prefrozen.</param>
            private Node(T key, Node left, Node right, bool frozen = false)
            {
                Requires.NotNullAllowStructs(key, nameof(key));
                Requires.NotNull(left, nameof(left));
                Requires.NotNull(right, nameof(right));
                Debug.Assert(!frozen || (left._frozen && right._frozen));

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
            ///   <c>true</c> if this instance is empty; otherwise, <c>false</c>.
            /// </value>
            public bool IsEmpty
            {
                get { return _left == null; }
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
            public Node Left
            {
                get { return _left; }
            }

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
            public Node Right
            {
                get { return _right; }
            }

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
            public T Value { get { return _key; } }

            /// <summary>
            /// Gets the number of elements contained by this subtree starting at this node.
            /// </summary>
            public int Count
            {
                get { return _count; }
            }

            /// <summary>
            /// Gets the key.
            /// </summary>
            internal T Key
            {
                get { return _key; }
            }

            /// <summary>
            /// Gets the maximum value in the collection, as defined by the comparer.
            /// </summary>
            /// <value>The maximum value in the set.</value>
            internal T Max
            {
                get
                {
                    if (this.IsEmpty)
                    {
                        return default(T);
                    }

                    Node n = this;
                    while (!n._right.IsEmpty)
                    {
                        n = n._right;
                    }

                    return n._key;
                }
            }

            /// <summary>
            /// Gets the minimum value in the collection, as defined by the comparer.
            /// </summary>
            /// <value>The minimum value in the set.</value>
            internal T Min
            {
                get
                {
                    if (this.IsEmpty)
                    {
                        return default(T);
                    }

                    Node n = this;
                    while (!n._left.IsEmpty)
                    {
                        n = n._left;
                    }

                    return n._key;
                }
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
                    Requires.Range(index >= 0 && index < this.Count, nameof(index));

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
            [ExcludeFromCodeCoverage] // internal and never called, but here for the interface.
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
            [ExcludeFromCodeCoverage] // internal and never called, but here for the interface.
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
            /// See the <see cref="ICollection{T}"/> interface.
            /// </summary>
            internal void CopyTo(T[] array, int arrayIndex)
            {
                Requires.NotNull(array, nameof(array));
                Requires.Range(arrayIndex >= 0, nameof(arrayIndex));
                Requires.Range(array.Length >= arrayIndex + this.Count, nameof(arrayIndex));
                foreach (var item in this)
                {
                    array[arrayIndex++] = item;
                }
            }

            /// <summary>
            /// See the <see cref="ICollection{T}"/> interface.
            /// </summary>
            internal void CopyTo(Array array, int arrayIndex)
            {
                Requires.NotNull(array, nameof(array));
                Requires.Range(arrayIndex >= 0, nameof(arrayIndex));
                Requires.Range(array.Length >= arrayIndex + this.Count, nameof(arrayIndex));

                foreach (var item in this)
                {
                    array.SetValue(item, arrayIndex++);
                }
            }

            /// <summary>
            /// Adds the specified key to the tree.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="comparer">The comparer.</param>
            /// <param name="mutated">Receives a value indicating whether this node tree has mutated because of this operation.</param>
            /// <returns>The new tree.</returns>
            internal Node Add(T key, IComparer<T> comparer, out bool mutated)
            {
                Requires.NotNullAllowStructs(key, nameof(key));
                Requires.NotNull(comparer, nameof(comparer));

                if (this.IsEmpty)
                {
                    mutated = true;
                    return new Node(key, this, this);
                }
                else
                {
                    Node result = this;
                    int compareResult = comparer.Compare(key, _key);
                    if (compareResult > 0)
                    {
                        var newRight = _right.Add(key, comparer, out mutated);
                        if (mutated)
                        {
                            result = this.Mutate(right: newRight);
                        }
                    }
                    else if (compareResult < 0)
                    {
                        var newLeft = _left.Add(key, comparer, out mutated);
                        if (mutated)
                        {
                            result = this.Mutate(left: newLeft);
                        }
                    }
                    else
                    {
                        mutated = false;
                        return this;
                    }

                    return mutated ? MakeBalanced(result) : result;
                }
            }

            /// <summary>
            /// Removes the specified key from the tree.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="comparer">The comparer.</param>
            /// <param name="mutated">Receives a value indicating whether this node tree has mutated because of this operation.</param>
            /// <returns>The new tree.</returns>
            internal Node Remove(T key, IComparer<T> comparer, out bool mutated)
            {
                Requires.NotNullAllowStructs(key, nameof(key));
                Requires.NotNull(comparer, nameof(comparer));

                if (this.IsEmpty)
                {
                    mutated = false;
                    return this;
                }
                else
                {
                    Node result = this;
                    int compare = comparer.Compare(key, _key);
                    if (compare == 0)
                    {
                        // We have a match.
                        mutated = true;

                        // If this is a leaf, just remove it 
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

                            bool dummyMutated;
                            var newRight = _right.Remove(successor._key, comparer, out dummyMutated);
                            result = successor.Mutate(left: _left, right: newRight);
                        }
                    }
                    else if (compare < 0)
                    {
                        var newLeft = _left.Remove(key, comparer, out mutated);
                        if (mutated)
                        {
                            result = this.Mutate(left: newLeft);
                        }
                    }
                    else
                    {
                        var newRight = _right.Remove(key, comparer, out mutated);
                        if (mutated)
                        {
                            result = this.Mutate(right: newRight);
                        }
                    }

                    return result.IsEmpty ? result : MakeBalanced(result);
                }
            }

            /// <summary>
            /// Determines whether the specified key is in this tree.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="comparer">The comparer.</param>
            /// <returns>
            ///   <c>true</c> if the tree contains the specified key; otherwise, <c>false</c>.
            /// </returns>
            [Pure]
            internal bool Contains(T key, IComparer<T> comparer)
            {
                Requires.NotNullAllowStructs(key, nameof(key));
                Requires.NotNull(comparer, nameof(comparer));
                return !this.Search(key, comparer).IsEmpty;
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

            /// <summary>
            /// Searches for the specified key.
            /// </summary>
            /// <param name="key">The key to search for.</param>
            /// <param name="comparer">The comparer.</param>
            /// <returns>The matching node, or <see cref="EmptyNode"/> if no match was found.</returns>
            [Pure]
            internal Node Search(T key, IComparer<T> comparer)
            {
                Requires.NotNullAllowStructs(key, nameof(key));
                Requires.NotNull(comparer, nameof(comparer));

                if (this.IsEmpty)
                {
                    return this;
                }
                else
                {
                    int compare = comparer.Compare(key, _key);
                    if (compare == 0)
                    {
                        return this;
                    }
                    else if (compare > 0)
                    {
                        return _right.Search(key, comparer);
                    }
                    else
                    {
                        return _left.Search(key, comparer);
                    }
                }
            }

            /// <summary>
            /// Searches for the specified key.
            /// </summary>
            /// <param name="key">The key to search for.</param>
            /// <param name="comparer">The comparer.</param>
            /// <returns>The matching node, or <see cref="EmptyNode"/> if no match was found.</returns>
            [Pure]
            internal int IndexOf(T key, IComparer<T> comparer)
            {
                Requires.NotNullAllowStructs(key, nameof(key));
                Requires.NotNull(comparer, nameof(comparer));

                if (this.IsEmpty)
                {
                    return -1;
                }
                else
                {
                    int compare = comparer.Compare(key, _key);
                    if (compare == 0)
                    {
                        return _left.Count;
                    }
                    else if (compare > 0)
                    {
                        int result = _right.IndexOf(key, comparer);
                        bool missing = result < 0;
                        if (missing)
                        {
                            result = ~result;
                        }

                        result = _left.Count + 1 + result;
                        if (missing)
                        {
                            result = ~result;
                        }

                        return result;
                    }
                    else
                    {
                        return _left.IndexOf(key, comparer);
                    }
                }
            }

            /// <summary>
            /// Returns an <see cref="IEnumerable{T}"/> that iterates over this
            /// collection in reverse order.
            /// </summary>
            /// <returns>
            /// An enumerator that iterates over the <see cref="ImmutableSortedSet{T}"/>
            /// in reverse order.
            /// </returns>
            [Pure]
            internal IEnumerator<T> Reverse()
            {
                return new Enumerator(this, reverse: true);
            }

            #region Tree balancing methods

            /// <summary>
            /// AVL rotate left operation.
            /// </summary>
            /// <param name="tree">The tree.</param>
            /// <returns>The rotated tree.</returns>
            private static Node RotateLeft(Node tree)
            {
                Requires.NotNull(tree, nameof(tree));
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
                Requires.NotNull(tree, nameof(tree));
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
                Requires.NotNull(tree, nameof(tree));
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
                Requires.NotNull(tree, nameof(tree));
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
                Requires.NotNull(tree, nameof(tree));
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
                Requires.NotNull(tree, nameof(tree));
                Debug.Assert(!tree.IsEmpty);
                return Balance(tree) >= 2;
            }

            /// <summary>
            /// Determines whether the specified tree is left heavy.
            /// </summary>
            [Pure]
            private static bool IsLeftHeavy(Node tree)
            {
                Requires.NotNull(tree, nameof(tree));
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
                Requires.NotNull(tree, nameof(tree));
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

            #endregion

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
                Requires.NotNull(items, nameof(items));
                Debug.Assert(start >= 0);
                Debug.Assert(length >= 0);

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
        }
    }

    /// <summary>
    /// A simple view of the immutable collection that the debugger can show to the developer.
    /// </summary>
    internal class ImmutableSortedSetDebuggerProxy<T>
    {
        /// <summary>
        /// The collection to be enumerated.
        /// </summary>
        private readonly ImmutableSortedSet<T> _set;

        /// <summary>
        /// The simple view of the collection.
        /// </summary>
        private T[] _contents;

        /// <summary>   
        /// Initializes a new instance of the <see cref="ImmutableSortedSetDebuggerProxy{T}"/> class.
        /// </summary>
        /// <param name="set">The collection to display in the debugger</param>
        public ImmutableSortedSetDebuggerProxy(ImmutableSortedSet<T> set)
        {
            Requires.NotNull(set, nameof(set));
            _set = set;
        }

        /// <summary>
        /// Gets a simple debugger-viewable collection.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Contents
        {
            get
            {
                if (_contents == null)
                {
                    _contents = _set.ToArray(_set.Count);
                }

                return _contents;
            }
        }
    }
}
