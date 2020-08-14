// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    [DebuggerTypeProxy(typeof(ImmutableEnumerableDebuggerProxy<>))]
    #if !NETSTANDARD1_0 && !NETSTANDARD1_3 && !NETSTANDARD2_0 && !NETFRAMEWORK
    public sealed partial class ImmutableSortedSet<T> : IImmutableSet<T>, ISortKeyCollection<T>, IReadOnlySet<T>, IReadOnlyList<T>, IList<T>, ISet<T>, IList, IStrongEnumerable<T, ImmutableSortedSet<T>.Enumerator>
    #else
    public sealed partial class ImmutableSortedSet<T> : IImmutableSet<T>, ISortKeyCollection<T>, IReadOnlyList<T>, IList<T>, ISet<T>, IList, IStrongEnumerable<T, ImmutableSortedSet<T>.Enumerator>
    #endif
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
        internal ImmutableSortedSet(IComparer<T>? comparer = null)
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
            return _root.IsEmpty ? this : Empty.WithComparer(_comparer);
        }

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
#if !NETSTANDARD1_0
                return _root.ItemRef(index);
#else
                return _root[index];
#endif
            }
        }

#if !NETSTANDARD1_0
        /// <summary>
        /// Gets a read-only reference of the element of the set at the given index.
        /// </summary>
        /// <param name="index">The 0-based index of the element in the set to return.</param>
        /// <returns>A read-only reference of the element at the given position.</returns>
        public ref readonly T ItemRef(int index)
        {
            return ref _root.ItemRef(index);
        }
#endif

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
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        public ImmutableSortedSet<T> Add(T value)
        {
            bool mutated;
            return this.Wrap(_root.Add(value, _comparer, out mutated));
        }

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        public ImmutableSortedSet<T> Remove(T value)
        {
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
        public bool TryGetValue(T equalValue, out T actualValue)
        {
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
        public ImmutableSortedSet<T> Intersect(IEnumerable<T> other)
        {
            Requires.NotNull(other, nameof(other));

            var newSet = this.Clear();
            foreach (var item in other.GetEnumerableDisposable<T, Enumerator>())
            {
                if (this.Contains(item))
                {
                    newSet = newSet.Add(item);
                }
            }

            Debug.Assert(newSet != null);
            return newSet;
        }

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
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
        public ImmutableSortedSet<T> Union(IEnumerable<T> other)
        {
            Requires.NotNull(other, nameof(other));

            ImmutableSortedSet<T>? immutableSortedSet;
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
        public ImmutableSortedSet<T> WithComparer(IComparer<T>? comparer)
        {
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
                Debug.Assert(result != null);
                return result;
            }
        }

        /// <summary>
        /// Checks whether a given sequence of items entirely describe the contents of this set.
        /// </summary>
        /// <param name="other">The sequence of items to check against this set.</param>
        /// <returns>A value indicating whether the sets are equal.</returns>
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
            return _root.IndexOf(item, _comparer);
        }

        #endregion

        #region IImmutableSet<T> Members

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        public bool Contains(T value)
        {
            return _root.Contains(value, _comparer);
        }

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        IImmutableSet<T> IImmutableSet<T>.Clear()
        {
            return this.Clear();
        }

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        IImmutableSet<T> IImmutableSet<T>.Add(T value)
        {
            return this.Add(value);
        }

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        IImmutableSet<T> IImmutableSet<T>.Remove(T value)
        {
            return this.Remove(value);
        }

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        IImmutableSet<T> IImmutableSet<T>.Intersect(IEnumerable<T> other)
        {
            return this.Intersect(other);
        }

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        IImmutableSet<T> IImmutableSet<T>.Except(IEnumerable<T> other)
        {
            return this.Except(other);
        }

        /// <summary>
        /// Produces a set that contains elements either in this set or a given sequence, but not both.
        /// </summary>
        /// <param name="other">The other sequence of items.</param>
        /// <returns>The new set.</returns>
        IImmutableSet<T> IImmutableSet<T>.SymmetricExcept(IEnumerable<T> other)
        {
            return this.SymmetricExcept(other);
        }

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
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
        int IList.Add(object? value)
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
        bool IList.Contains(object? value)
        {
            return this.Contains((T)value!);
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
            return this.IndexOf((T)value!);
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
        /// Removes the first occurrence of a specific object from the <see cref="IList"/>.
        /// </summary>
        /// <param name="value">The object to remove from the <see cref="IList"/>.</param>
        /// <exception cref="System.NotSupportedException"></exception>
        void IList.Remove(object? value)
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
        /// Gets or sets the <see cref="object"/> at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="object"/>.
        /// </value>
        /// <param name="index">The index.</param>
        /// <exception cref="System.NotSupportedException"></exception>
        object? IList.this[int index]
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
        private static bool TryCastToImmutableSortedSet(IEnumerable<T> sequence, [NotNullWhen(true)] out ImmutableSortedSet<T>? other)
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
        private ImmutableSortedSet<T> UnionIncremental(IEnumerable<T> items)
        {
            Requires.NotNull(items, nameof(items));

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
        private ImmutableSortedSet<T> LeafToRootRefill(IEnumerable<T> addedItems)
        {
            Requires.NotNull(addedItems, nameof(addedItems));

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
    }
}
