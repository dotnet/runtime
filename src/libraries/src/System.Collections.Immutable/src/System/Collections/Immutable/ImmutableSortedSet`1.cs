// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using Validation;

namespace System.Collections.Immutable
{
    /// <summary>
    /// An immutable sorted set implementation.
    /// </summary>
    /// <typeparam name="T">The type of elements in the set.</typeparam>
    /// <devremarks>
    /// We implement IReadOnlyList{T} because it adds an ordinal indexer.
    /// We implement IList{T} because it gives us IndexOf(T), which is important for some folks.
    /// </devremarks>
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(ImmutableSortedSet<>.DebuggerProxy))]
    public sealed partial class ImmutableSortedSet<T> : IImmutableSet<T>, ISortKeyCollection<T>, IReadOnlyList<T>, IList<T>, ISet<T>, IList
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
        private readonly Node root;

        /// <summary>
        /// The comparer used to sort elements in this set.
        /// </summary>
        private readonly IComparer<T> comparer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableSortedSet&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="comparer">The comparer.</param>
        internal ImmutableSortedSet(IComparer<T> comparer = null)
        {
            this.root = Node.EmptyNode;
            this.comparer = comparer ?? Comparer<T>.Default;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableSortedSet&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="root">The root of the AVL tree with the contents of this set.</param>
        /// <param name="comparer">The comparer.</param>
        private ImmutableSortedSet(Node root, IComparer<T> comparer)
        {
            Requires.NotNull(root, "root");
            Requires.NotNull(comparer, "comparer");

            root.Freeze();
            this.root = root;
            this.comparer = comparer;
        }

        /// <summary>
        /// See the <see cref="IImmutableSet&lt;T&gt;"/> interface.
        /// </summary>
        public ImmutableSortedSet<T> Clear()
        {
            Contract.Ensures(Contract.Result<ImmutableSortedSet<T>>() != null);
            Contract.Ensures(Contract.Result<ImmutableSortedSet<T>>().IsEmpty);
            return this.root.IsEmpty ? this : Empty.WithComparer(this.comparer);
        }

        /// <summary>
        /// Gets the maximum value in the collection, as defined by the comparer.
        /// </summary>
        /// <value>The maximum value in the set.</value>
        public T Max
        {
            get { return this.root.Max; }
        }

        /// <summary>
        /// Gets the minimum value in the collection, as defined by the comparer.
        /// </summary>
        /// <value>The minimum value in the set.</value>
        public T Min
        {
            get { return this.root.Min; }
        }

        #region IImmutableSet<T> Properties

        /// <summary>
        /// See the <see cref="IImmutableSet&lt;T&gt;"/> interface.
        /// </summary>
        public bool IsEmpty
        {
            get { return this.root.IsEmpty; }
        }

        /// <summary>
        /// See the <see cref="IImmutableSet&lt;T&gt;"/> interface.
        /// </summary>
        public int Count
        {
            get { return this.root.Count; }
        }

        #endregion

        #region ISortKeyCollection<T> Properties

        /// <summary>
        /// See the <see cref="ISortKeyCollection{T}"/> interface.
        /// </summary>
        public IComparer<T> KeyComparer
        {
            get { return this.comparer; }
        }

        #endregion

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
                return this.root[index];
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
        /// See the <see cref="IImmutableSet&lt;T&gt;"/> interface.
        /// </summary>
        [Pure]
        public ImmutableSortedSet<T> Add(T value)
        {
            Requires.NotNullAllowStructs(value, "value");
            Contract.Ensures(Contract.Result<ImmutableSortedSet<T>>() != null);
            bool mutated;
            return this.Wrap(this.root.Add(value, this.comparer, out mutated));
        }

        /// <summary>
        /// See the <see cref="IImmutableSet&lt;T&gt;"/> interface.
        /// </summary>
        [Pure]
        public ImmutableSortedSet<T> Remove(T value)
        {
            Requires.NotNullAllowStructs(value, "value");
            Contract.Ensures(Contract.Result<ImmutableSortedSet<T>>() != null);
            bool mutated;
            return this.Wrap(this.root.Remove(value, this.comparer, out mutated));
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
            Requires.NotNullAllowStructs(equalValue, "equalValue");

            Node searchResult = this.root.Search(equalValue, this.comparer);
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
        /// See the <see cref="IImmutableSet&lt;T&gt;"/> interface.
        /// </summary>
        [Pure]
        public ImmutableSortedSet<T> Intersect(IEnumerable<T> other)
        {
            Requires.NotNull(other, "other");
            Contract.Ensures(Contract.Result<ImmutableSortedSet<T>>() != null);
            var newSet = this.Clear();
            foreach (var item in other)
            {
                if (this.Contains(item))
                {
                    newSet = newSet.Add(item);
                }
            }

            return newSet;
        }

        /// <summary>
        /// See the <see cref="IImmutableSet&lt;T&gt;"/> interface.
        /// </summary>
        [Pure]
        public ImmutableSortedSet<T> Except(IEnumerable<T> other)
        {
            Requires.NotNull(other, "other");

            var result = this.root;
            foreach (T item in other)
            {
                bool mutated;
                result = result.Remove(item, this.comparer, out mutated);
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
            Requires.NotNull(other, "other");

            var otherAsSet = ImmutableSortedSet<T>.Empty.Union(other);

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
        /// See the <see cref="IImmutableSet&lt;T&gt;"/> interface.
        /// </summary>
        [Pure]
        public ImmutableSortedSet<T> Union(IEnumerable<T> other)
        {
            Requires.NotNull(other, "other");
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
        /// See the <see cref="IImmutableSet&lt;T&gt;"/> interface.
        /// </summary>
        [Pure]
        public ImmutableSortedSet<T> WithComparer(IComparer<T> comparer)
        {
            Contract.Ensures(Contract.Result<ImmutableSortedSet<T>>() != null);
            if (comparer == null)
            {
                comparer = Comparer<T>.Default;
            }

            if (comparer == this.comparer)
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
            Requires.NotNull(other, "other");

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
            Requires.NotNull(other, "other");

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
            Requires.NotNull(other, "other");

            if (this.IsEmpty)
            {
                return false;
            }

            int count = 0;
            foreach (T item in other)
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
            Requires.NotNull(other, "other");

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
            Requires.NotNull(other, "other");

            foreach (T item in other)
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
            Requires.NotNull(other, "other");

            if (this.IsEmpty)
            {
                return false;
            }

            foreach (T item in other)
            {
                if (this.Contains(item))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns an System.Collections.Generic.IEnumerable&lt;T&gt; that iterates over this
        /// collection in reverse order.
        /// </summary>
        /// <returns>
        /// An enumerator that iterates over the System.Collections.Generic.SortedSet&lt;T&gt;
        /// in reverse order.
        /// </returns>
        [Pure]
        public IEnumerable<T> Reverse()
        {
            return new ReverseEnumerable(this.root);
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
            Requires.NotNullAllowStructs(item, "item");
            return this.root.IndexOf(item, this.comparer);
        }

        #endregion

        #region IImmutableSet<T> Members

        /// <summary>
        /// See the <see cref="IImmutableSet&lt;T&gt;"/> interface.
        /// </summary>
        public bool Contains(T value)
        {
            Requires.NotNullAllowStructs(value, "value");
            return this.root.Contains(value, this.comparer);
        }

        /// <summary>
        /// See the <see cref="IImmutableSet&lt;T&gt;"/> interface.
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableSet<T> IImmutableSet<T>.Clear()
        {
            return this.Clear();
        }

        /// <summary>
        /// See the <see cref="IImmutableSet&lt;T&gt;"/> interface.
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableSet<T> IImmutableSet<T>.Add(T value)
        {
            return this.Add(value);
        }

        /// <summary>
        /// See the <see cref="IImmutableSet&lt;T&gt;"/> interface.
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableSet<T> IImmutableSet<T>.Remove(T value)
        {
            return this.Remove(value);
        }

        /// <summary>
        /// See the <see cref="IImmutableSet&lt;T&gt;"/> interface.
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableSet<T> IImmutableSet<T>.Intersect(IEnumerable<T> other)
        {
            return this.Intersect(other);
        }

        /// <summary>
        /// See the <see cref="IImmutableSet&lt;T&gt;"/> interface.
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
        /// See the <see cref="IImmutableSet&lt;T&gt;"/> interface.
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
            this.root.CopyTo(array, arrayIndex);
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
        /// Gets a value indicating whether the <see cref="T:System.Collections.IList" /> has a fixed size.
        /// </summary>
        /// <returns>true if the <see cref="T:System.Collections.IList" /> has a fixed size; otherwise, false.</returns>
        bool IList.IsFixedSize
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
        /// </summary>
        /// <returns>true if the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only; otherwise, false.
        ///   </returns>
        bool IList.IsReadOnly
        {
            get { return true; }
        }

        #endregion

        #region ICollection Properties

        /// <summary>
        /// See ICollection.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        object ICollection.SyncRoot
        {
            get { return this; }
        }

        /// <summary>
        /// See the ICollection interface.
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
        /// Adds an item to the <see cref="T:System.Collections.IList" />.
        /// </summary>
        /// <param name="value">The object to add to the <see cref="T:System.Collections.IList" />.</param>
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
        /// Determines whether the <see cref="T:System.Collections.IList" /> contains a specific value.
        /// </summary>
        /// <param name="value">The object to locate in the <see cref="T:System.Collections.IList" />.</param>
        /// <returns>
        /// true if the <see cref="T:System.Object" /> is found in the <see cref="T:System.Collections.IList" />; otherwise, false.
        /// </returns>
        bool IList.Contains(object value)
        {
            return this.Contains((T)value);
        }

        /// <summary>
        /// Determines the index of a specific item in the <see cref="T:System.Collections.IList" />.
        /// </summary>
        /// <param name="value">The object to locate in the <see cref="T:System.Collections.IList" />.</param>
        /// <returns>
        /// The index of <paramref name="value" /> if found in the list; otherwise, -1.
        /// </returns>
        int IList.IndexOf(object value)
        {
            return this.IndexOf((T)value);
        }

        /// <summary>
        /// Inserts an item to the <see cref="T:System.Collections.IList" /> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="value" /> should be inserted.</param>
        /// <param name="value">The object to insert into the <see cref="T:System.Collections.IList" />.</param>
        /// <exception cref="System.NotSupportedException"></exception>
        void IList.Insert(int index, object value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.IList" />.
        /// </summary>
        /// <param name="value">The object to remove from the <see cref="T:System.Collections.IList" />.</param>
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
        /// Gets or sets the <see cref="System.Object" /> at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="System.Object" />.
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
        /// Copies the elements of the <see cref="T:System.Collections.ICollection" /> to an <see cref="T:System.Array" />, starting at a particular <see cref="T:System.Array" /> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from <see cref="T:System.Collections.ICollection" />. The <see cref="T:System.Array" /> must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array" /> at which copying begins.</param>
        void ICollection.CopyTo(Array array, int index)
        {
            this.root.CopyTo(array, index);
        }

        #endregion

        #region IEnumerable<T> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
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
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
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
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
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
            return this.root.GetEnumerator();
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
            Requires.NotNull(items, "items");
            Contract.Ensures(Contract.Result<ImmutableSortedSet<T>>() != null);

            // Let's not implement in terms of ImmutableSortedSet.Add so that we're
            // not unnecessarily generating a new wrapping set object for each item.
            var result = this.root;
            foreach (var item in items)
            {
                bool mutated;
                result = result.Add(item, this.comparer, out mutated);
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
            if (root != this.root)
            {
                return root.IsEmpty ? this.Clear() : new ImmutableSortedSet<T>(root, this.comparer);
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
            Requires.NotNull(addedItems, "addedItems");
            Contract.Ensures(Contract.Result<ImmutableSortedSet<T>>() != null);

            // Rather than build up the immutable structure in the incremental way,
            // build it in such a way as to generate minimal garbage, by assembling
            // the immutable binary tree from leaf to root.  This requires
            // that we know the length of the item sequence in advance, sort it, 
            // and can index into that sequence like a list, so the limited
            // garbage produced is a temporary mutable data structure we use
            // as a reference when creating the immutable one.
            // The (mutable) SortedSet<T> is much faster at constructing its collection 
            // when passed a sequence into its constructor than into its Union method.
            var sortedSet = new SortedSet<T>(this.Concat(addedItems), this.KeyComparer);
            Node root = Node.NodeTreeFromSortedSet(sortedSet);
            return this.Wrap(root);
        }

        /// <summary>
        /// Enumerates the contents of a binary tree.
        /// </summary>
        /// <remarks>
        /// This struct can and should be kept in exact sync with the other binary tree enumerators: 
        /// ImmutableList.Enumerator, ImmutableSortedMap.Enumerator, and ImmutableSortedSet.Enumerator.
        /// 
        /// CAUTION: when this enumerator is actually used as a valuetype (not boxed) do NOT copy it by assigning to a second variable 
        /// or by passing it to another method.  When this enumerator is disposed of it returns a mutable reference type stack to a resource pool,
        /// and if the value type enumerator is copied (which can easily happen unintentionally if you pass the value around) there is a risk
        /// that a stack that has already been returned to the resource pool may still be in use by one of the enumerator copies, leading to data
        /// corruption and/or exceptions.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public struct Enumerator : IEnumerator<T>, ISecurePooledObjectUser
        {
            /// <summary>
            /// The resource pool of reusable mutable stacks for purposes of enumeration.
            /// </summary>
            /// <remarks>
            /// We utilize this resource pool to make "allocation free" enumeration achievable.
            /// </remarks>
            private static readonly SecureObjectPool<Stack<RefAsValueType<IBinaryTree<T>>>, Enumerator> enumeratingStacks = new SecureObjectPool<Stack<RefAsValueType<IBinaryTree<T>>>, Enumerator>();

            /// <summary>
            /// The builder being enumerated, if applicable.
            /// </summary>
            private readonly Builder builder;

            /// <summary>
            /// A unique ID for this instance of this enumerator.
            /// Used to protect pooled objects from use after they are recycled.
            /// </summary>
            private readonly Guid poolUserId;

            /// <summary>
            /// A flag indicating whether this enumerator works in reverse sort order.
            /// </summary>
            private readonly bool reverse;

            /// <summary>
            /// The set being enumerated.
            /// </summary>
            private IBinaryTree<T> root;

            /// <summary>
            /// The stack to use for enumerating the binary tree.
            /// </summary>
            /// <remarks>
            /// We use RefAsValueType{T} as a wrapper to avoid paying the cost of covariant checks whenever
            /// the underlying array that the Stack{T} class uses is written to. 
            /// We've recognized this as a perf win in ETL traces for these stack frames:
            /// clr!JIT_Stelem_Ref
            ///   clr!ArrayStoreCheck
            ///     clr!ObjIsInstanceOf
            /// </remarks>
            private SecurePooledObject<Stack<RefAsValueType<IBinaryTree<T>>>> stack;

            /// <summary>
            /// The node currently selected.
            /// </summary>
            private IBinaryTree<T> current;

            /// <summary>
            /// The version of the builder (when applicable) that is being enumerated.
            /// </summary>
            private int enumeratingBuilderVersion;

            /// <summary>
            /// Initializes an Enumerator structure.
            /// </summary>
            /// <param name="root">The root of the set to be enumerated.</param>
            /// <param name="builder">The builder, if applicable.</param>
            /// <param name="reverse"><c>true</c> to enumerate the collection in reverse.</param>
            internal Enumerator(IBinaryTree<T> root, Builder builder = null, bool reverse = false)
            {
                Requires.NotNull(root, "root");

                this.root = root;
                this.builder = builder;
                this.current = null;
                this.reverse = reverse;
                this.enumeratingBuilderVersion = builder != null ? builder.Version : -1;
                this.poolUserId = Guid.NewGuid();
                this.stack = null;
                if (!enumeratingStacks.TryTake(this, out this.stack))
                {
                    this.stack = enumeratingStacks.PrepNew(this, new Stack<RefAsValueType<IBinaryTree<T>>>(root.Height));
                }

                this.Reset();
            }

            /// <inheritdoc/>
            Guid ISecurePooledObjectUser.PoolUserId
            {
                get { return this.poolUserId; }
            }

            /// <summary>
            /// The current element.
            /// </summary>
            public T Current
            {
                get
                {
                    this.ThrowIfDisposed();
                    if (this.current != null)
                    {
                        return this.current.Value;
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
                this.root = null;
                this.current = null;
                if (this.stack != null && this.stack.Owner == this.poolUserId)
                {
                    using (var stack = this.stack.Use(this))
                    {
                        stack.Value.Clear();
                    }

                    enumeratingStacks.TryAdd(this, this.stack);
                    this.stack = null;
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

                using (var stack = this.stack.Use(this))
                {
                    if (stack.Value.Count > 0)
                    {
                        IBinaryTree<T> n = stack.Value.Pop().Value;
                        this.current = n;
                        this.PushNext(this.reverse ? n.Left : n.Right);
                        return true;
                    }
                    else
                    {
                        this.current = null;
                        return false;
                    }
                }
            }

            /// <summary>
            /// Restarts enumeration.
            /// </summary>
            public void Reset()
            {
                this.ThrowIfDisposed();

                this.enumeratingBuilderVersion = builder != null ? builder.Version : -1;
                this.current = null;
                using (var stack = this.stack.Use(this))
                {
                    stack.Value.Clear();
                }

                this.PushNext(this.root);
            }

            /// <summary>
            /// Throws an ObjectDisposedException if this enumerator has been disposed.
            /// </summary>
            private void ThrowIfDisposed()
            {
                Contract.Ensures(this.root != null);
                Contract.EnsuresOnThrow<ObjectDisposedException>(this.root == null);

                if (this.root == null)
                {
                    throw new ObjectDisposedException(this.GetType().FullName);
                }

                // Since this is a struct, copies might not have been marked as disposed.
                // But the stack we share across those copies would know.
                // This trick only works when we have a non-null stack.
                // For enumerators of empty collections, there isn't any natural
                // way to know when a copy of the struct has been disposed of.
                if (this.stack != null)
                {
                    this.stack.ThrowDisposedIfNotOwned(this);
                }
            }

            /// <summary>
            /// Throws an exception if the underlying builder's contents have been changed since enumeration started.
            /// </summary>
            /// <exception cref="System.InvalidOperationException">Thrown if the collection has changed.</exception>
            private void ThrowIfChanged()
            {
                if (this.builder != null && this.builder.Version != this.enumeratingBuilderVersion)
                {
                    throw new InvalidOperationException(Strings.CollectionModifiedDuringEnumeration);
                }
            }

            /// <summary>
            /// Pushes this node and all its Left (or Right, if reversed) descendents onto the stack.
            /// </summary>
            /// <param name="node">The starting node to push onto the stack.</param>
            private void PushNext(IBinaryTree<T> node)
            {
                Requires.NotNull(node, "node");
                using (var stack = this.stack.Use(this))
                {
                    while (!node.IsEmpty)
                    {
                        stack.Value.Push(new RefAsValueType<IBinaryTree<T>>(node));
                        node = this.reverse ? node.Right : node.Left;
                    }
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
            private readonly Node root;

            /// <summary>
            /// Initializes a new instance of the <see cref="ImmutableSortedSet&lt;T&gt;.ReverseEnumerable"/> class.
            /// </summary>
            /// <param name="root">The root of the data structure to reverse enumerate.</param>
            internal ReverseEnumerable(Node root)
            {
                Requires.NotNull(root, "root");
                this.root = root;
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
            /// </returns>
            public IEnumerator<T> GetEnumerator()
            {
                return this.root.Reverse();
            }

            /// <summary>
            /// Returns an enumerator that iterates through a collection.
            /// </summary>
            /// <returns>
            /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
            /// </returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        /// <summary>
        /// A node in the AVL tree storing this set.
        /// </summary>
        [DebuggerDisplay("{key}")]
        private sealed class Node : IBinaryTree<T>, IEnumerable<T>
        {
            /// <summary>
            /// The default empty node.
            /// </summary>
            internal static readonly Node EmptyNode = new Node();

            /// <summary>
            /// The key associated with this node.
            /// </summary>
            private readonly T key;

            /// <summary>
            /// A value indicating whether this node has been frozen (made immutable).
            /// </summary>
            /// <remarks>
            /// Nodes must be frozen before ever being observed by a wrapping collection type
            /// to protect collections from further mutations.
            /// </remarks>
            private bool frozen;

            /// <summary>
            /// The depth of the tree beneath this node.
            /// </summary>
            private int height;

            /// <summary>
            /// The number of elements contained by this subtree starting at this node.
            /// </summary>
            /// <remarks>
            /// If this node would benefit from saving 4 bytes, we could have only a few nodes 
            /// scattered throughout the graph actually record the count of nodes beneath them.
            /// Those without the count could query their descendents, which would often short-circuit
            /// when they hit a node that *does* include a count field.
            /// </remarks>
            private int count;

            /// <summary>
            /// The left tree.
            /// </summary>
            private Node left;

            /// <summary>
            /// The right tree.
            /// </summary>
            private Node right;

            /// <summary>
            /// Initializes a new instance of the <see cref="ImmutableSortedSet&lt;T&gt;.Node"/> class
            /// that is pre-frozen.
            /// </summary>
            private Node()
            {
                Contract.Ensures(this.IsEmpty);
                this.frozen = true; // the empty node is *always* frozen.
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ImmutableSortedSet&lt;T&gt;.Node"/> class
            /// that is not yet frozen.
            /// </summary>
            /// <param name="key">The value stored by this node.</param>
            /// <param name="left">The left branch.</param>
            /// <param name="right">The right branch.</param>
            /// <param name="frozen">Whether this node is prefrozen.</param>
            private Node(T key, Node left, Node right, bool frozen = false)
            {
                Requires.NotNullAllowStructs(key, "key");
                Requires.NotNull(left, "left");
                Requires.NotNull(right, "right");
                Debug.Assert(!frozen || (left.frozen && right.frozen));

                this.key = key;
                this.left = left;
                this.right = right;
                this.height = 1 + Math.Max(left.height, right.height);
                this.count = 1 + left.count + right.count;
                this.frozen = frozen;
            }

            /// <summary>
            /// Gets a value indicating whether this instance is empty.
            /// </summary>
            /// <value>
            ///   <c>true</c> if this instance is empty; otherwise, <c>false</c>.
            /// </value>
            public bool IsEmpty
            {
                get { return this.left == null; }
            }

            /// <summary>
            /// Gets the height of the tree beneath this node.
            /// </summary>
            int IBinaryTree<T>.Height
            {
                get { return this.height; }
            }

            /// <summary>
            /// Gets the left branch of this node.
            /// </summary>
            IBinaryTree<T> IBinaryTree<T>.Left
            {
                get { return this.left; }
            }

            /// <summary>
            /// Gets the right branch of this node.
            /// </summary>
            IBinaryTree<T> IBinaryTree<T>.Right
            {
                get { return this.right; }
            }

            /// <summary>
            /// Gets the value represented by the current node.
            /// </summary>
            T IBinaryTree<T>.Value
            {
                get { return this.key; }
            }

            /// <summary>
            /// Gets the number of elements contained by this subtree starting at this node.
            /// </summary>
            public int Count
            {
                get { return this.count; }
            }

            /// <summary>
            /// Gets the key.
            /// </summary>
            internal T Key
            {
                get { return this.key; }
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
                    while (!n.right.IsEmpty)
                    {
                        n = n.right;
                    }

                    return n.key;
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
                    while (!n.left.IsEmpty)
                    {
                        n = n.left;
                    }

                    return n.key;
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
                    Requires.Range(index >= 0 && index < this.Count, "index");

                    if (index < this.left.count)
                    {
                        return this.left[index];
                    }

                    if (index > this.left.count)
                    {
                        return this.right[index - this.left.count - 1];
                    }

                    return this.key;
                }
            }

            #region IEnumerable<T> Members

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
            /// </returns>
            public Enumerator GetEnumerator()
            {
                return new Enumerator(this);
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
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
            /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
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
            /// A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
            /// </returns>
            internal Enumerator GetEnumerator(Builder builder)
            {
                return new Enumerator(this, builder);
            }

            /// <summary>
            /// Creates a node tree from an existing (mutable) collection.
            /// </summary>
            /// <param name="collection">The collection.</param>
            /// <returns>The root of the node tree.</returns>
            [Pure]
            internal static Node NodeTreeFromSortedSet(SortedSet<T> collection)
            {
                Requires.NotNull(collection, "collection");
                Contract.Ensures(Contract.Result<Node>() != null);

                if (collection.Count == 0)
                {
                    return EmptyNode;
                }

                var list = collection.AsOrderedCollection();
                return NodeTreeFromList(list, 0, list.Count);
            }

            /// <summary>
            /// See the <see cref="ICollection{T}"/> interface.
            /// </summary>
            internal void CopyTo(T[] array, int arrayIndex)
            {
                Requires.NotNull(array, "array");
                Requires.Range(arrayIndex >= 0, "arrayIndex");
                Requires.Range(array.Length >= arrayIndex + this.Count, "arrayIndex");
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
                Requires.NotNull(array, "array");
                Requires.Range(arrayIndex >= 0, "arrayIndex");
                Requires.Range(array.Length >= arrayIndex + this.Count, "arrayIndex");

                if (IsEmpty)
                {
                    return;
                }

                int[] indices = new int[1]; // SetValue takes a params array; lifting out the implicit allocation from the loop
                foreach (var item in this)
                {
                    indices[0] = arrayIndex++;
                    array.SetValue(item, indices);
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
                Requires.NotNullAllowStructs(key, "key");
                Requires.NotNull(comparer, "comparer");

                if (this.IsEmpty)
                {
                    mutated = true;
                    return new Node(key, this, this);
                }
                else
                {
                    Node result = this;
                    int compareResult = comparer.Compare(key, this.key);
                    if (compareResult > 0)
                    {
                        var newRight = this.right.Add(key, comparer, out mutated);
                        if (mutated)
                        {
                            result = this.Mutate(right: newRight);
                        }
                    }
                    else if (compareResult < 0)
                    {
                        var newLeft = this.left.Add(key, comparer, out mutated);
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
                Requires.NotNullAllowStructs(key, "key");
                Requires.NotNull(comparer, "comparer");

                if (this.IsEmpty)
                {
                    mutated = false;
                    return this;
                }
                else
                {
                    Node result = this;
                    int compare = comparer.Compare(key, this.key);
                    if (compare == 0)
                    {
                        // We have a match.
                        mutated = true;

                        // If this is a leaf, just remove it 
                        // by returning Empty.  If we have only one child,
                        // replace the node with the child.
                        if (this.right.IsEmpty && this.left.IsEmpty)
                        {
                            result = EmptyNode;
                        }
                        else if (this.right.IsEmpty && !this.left.IsEmpty)
                        {
                            result = this.left;
                        }
                        else if (!this.right.IsEmpty && this.left.IsEmpty)
                        {
                            result = this.right;
                        }
                        else
                        {
                            // We have two children. Remove the next-highest node and replace
                            // this node with it.
                            var successor = this.right;
                            while (!successor.left.IsEmpty)
                            {
                                successor = successor.left;
                            }

                            bool dummyMutated;
                            var newRight = this.right.Remove(successor.key, comparer, out dummyMutated);
                            result = successor.Mutate(left: this.left, right: newRight);
                        }
                    }
                    else if (compare < 0)
                    {
                        var newLeft = this.left.Remove(key, comparer, out mutated);
                        if (mutated)
                        {
                            result = this.Mutate(left: newLeft);
                        }
                    }
                    else
                    {
                        var newRight = this.right.Remove(key, comparer, out mutated);
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
                Requires.NotNullAllowStructs(key, "key");
                Requires.NotNull(comparer, "comparer");
                return !this.Search(key, comparer).IsEmpty;
            }

            /// <summary>
            /// Freezes this node and all descendent nodes so that any mutations require a new instance of the nodes.
            /// </summary>
            internal void Freeze()
            {
                // If this node is frozen, all its descendents must already be frozen.
                if (!this.frozen)
                {
                    this.left.Freeze();
                    this.right.Freeze();
                    this.frozen = true;
                }
            }

            /// <summary>
            /// Searches for the specified key.
            /// </summary>
            /// <param name="key">The key to search for.</param>
            /// <param name="comparer">The comparer.</param>
            /// <returns>The matching node, or the Empty node if no match was found.</returns>
            [Pure]
            internal Node Search(T key, IComparer<T> comparer)
            {
                Requires.NotNullAllowStructs(key, "key");
                Requires.NotNull(comparer, "comparer");

                if (this.IsEmpty)
                {
                    return this;
                }
                else
                {
                    int compare = comparer.Compare(key, this.key);
                    if (compare == 0)
                    {
                        return this;
                    }
                    else if (compare > 0)
                    {
                        return this.right.Search(key, comparer);
                    }
                    else
                    {
                        return this.left.Search(key, comparer);
                    }
                }
            }

            /// <summary>
            /// Searches for the specified key.
            /// </summary>
            /// <param name="key">The key to search for.</param>
            /// <param name="comparer">The comparer.</param>
            /// <returns>The matching node, or the Empty node if no match was found.</returns>
            [Pure]
            internal int IndexOf(T key, IComparer<T> comparer)
            {
                Requires.NotNullAllowStructs(key, "key");
                Requires.NotNull(comparer, "comparer");

                if (this.IsEmpty)
                {
                    return -1;
                }
                else
                {
                    int compare = comparer.Compare(key, this.key);
                    if (compare == 0)
                    {
                        return this.left.Count;
                    }
                    else if (compare > 0)
                    {
                        int result = this.right.IndexOf(key, comparer);
                        bool missing = result < 0;
                        if (missing)
                        {
                            result = ~result;
                        }

                        result = this.left.Count + 1 + result;
                        if (missing)
                        {
                            result = ~result;
                        }

                        return result;
                    }
                    else
                    {
                        return this.left.IndexOf(key, comparer);
                    }
                }
            }

            /// <summary>
            /// Returns an System.Collections.Generic.IEnumerable&lt;T&gt; that iterates over this
            /// collection in reverse order.
            /// </summary>
            /// <returns>
            /// An enumerator that iterates over the System.Collections.Generic.SortedSet&lt;T&gt;
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
                Requires.NotNull(tree, "tree");
                Debug.Assert(!tree.IsEmpty);
                Contract.Ensures(Contract.Result<Node>() != null);

                if (tree.right.IsEmpty)
                {
                    return tree;
                }

                var right = tree.right;
                return right.Mutate(left: tree.Mutate(right: right.left));
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

                if (tree.left.IsEmpty)
                {
                    return tree;
                }

                var left = tree.left;
                return left.Mutate(right: tree.Mutate(left: left.right));
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

                if (tree.right.IsEmpty)
                {
                    return tree;
                }

                Node rotatedRightChild = tree.Mutate(right: RotateRight(tree.right));
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

                if (tree.left.IsEmpty)
                {
                    return tree;
                }

                Node rotatedLeftChild = tree.Mutate(left: RotateLeft(tree.left));
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

                return tree.right.height - tree.left.height;
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
                    return IsLeftHeavy(tree.right) ? DoubleLeft(tree) : RotateLeft(tree);
                }

                if (IsLeftHeavy(tree))
                {
                    return IsRightHeavy(tree.left) ? DoubleRight(tree) : RotateRight(tree);
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
            private static Node NodeTreeFromList(IOrderedCollection<T> items, int start, int length)
            {
                Requires.NotNull(items, "items");
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
                if (this.frozen)
                {
                    return new Node(this.key, left ?? this.left, right ?? this.right);
                }
                else
                {
                    if (left != null)
                    {
                        this.left = left;
                    }

                    if (right != null)
                    {
                        this.right = right;
                    }

                    this.height = 1 + Math.Max(this.left.height, this.right.height);
                    this.count = 1 + this.left.count + this.right.count;
                    return this;
                }
            }
        }

        /// <summary>
        /// A simple view of the immutable collection that the debugger can show to the developer.
        /// </summary>
        [ExcludeFromCodeCoverage]
        private class DebuggerProxy
        {
            /// <summary>
            /// The collection to be enumerated.
            /// </summary>
            private readonly ImmutableSortedSet<T> set;

            /// <summary>
            /// The simple view of the collection.
            /// </summary>
            private T[] contents;

            /// <summary>   
            /// Initializes a new instance of the <see cref="DebuggerProxy"/> class.
            /// </summary>
            /// <param name="set">The collection to display in the debugger</param>
            public DebuggerProxy(ImmutableSortedSet<T> set)
            {
                Requires.NotNull(set, "set");
                this.set = set;
            }

            /// <summary>
            /// Gets a simple debugger-viewable collection.
            /// </summary>
            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public T[] Contents
            {
                get
                {
                    if (this.contents == null)
                    {
                        this.contents = this.set.ToArray(this.set.Count);
                    }

                    return this.contents;
                }
            }
        }
    }
}
