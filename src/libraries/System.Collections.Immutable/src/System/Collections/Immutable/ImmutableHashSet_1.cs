// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;

namespace System.Collections.Immutable
{
    /// <summary>
    /// An immutable unordered hash set implementation.
    /// </summary>
    /// <typeparam name="T">The type of elements in the set.</typeparam>
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(ImmutableEnumerableDebuggerProxy<>))]
    public sealed partial class ImmutableHashSet<T> : IImmutableSet<T>, IHashKeyCollection<T>, IReadOnlyCollection<T>, ICollection<T>, ISet<T>, ICollection, IStrongEnumerable<T, ImmutableHashSet<T>.Enumerator>
    {
        /// <summary>
        /// An empty immutable hash set with the default comparer for <typeparamref name="T"/>.
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly ImmutableHashSet<T> Empty = new ImmutableHashSet<T>(SortedInt32KeyNode<HashBucket>.EmptyNode, EqualityComparer<T>.Default, 0);

        /// <summary>
        /// The singleton delegate that freezes the contents of hash buckets when the root of the data structure is frozen.
        /// </summary>
        private static readonly Action<KeyValuePair<int, HashBucket>> s_FreezeBucketAction = (kv) => kv.Value.Freeze();

        /// <summary>
        /// The equality comparer used to hash the elements in the collection.
        /// </summary>
        private readonly IEqualityComparer<T> _equalityComparer;

        /// <summary>
        /// The number of elements in this collection.
        /// </summary>
        private readonly int _count;

        /// <summary>
        /// The sorted dictionary that this hash set wraps.  The key is the hash code and the value is the bucket of all items that hashed to it.
        /// </summary>
        private readonly SortedInt32KeyNode<HashBucket> _root;

        /// <summary>
        /// The equality comparer to use when balancing the tree of hash buckets.
        /// </summary>
        private readonly IEqualityComparer<HashBucket> _hashBucketEqualityComparer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableHashSet{T}"/> class.
        /// </summary>
        /// <param name="equalityComparer">The equality comparer.</param>
        internal ImmutableHashSet(IEqualityComparer<T> equalityComparer)
            : this(SortedInt32KeyNode<HashBucket>.EmptyNode, equalityComparer, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableHashSet{T}"/> class.
        /// </summary>
        /// <param name="root">The sorted set that this set wraps.</param>
        /// <param name="equalityComparer">The equality comparer used by this instance.</param>
        /// <param name="count">The number of elements in this collection.</param>
        private ImmutableHashSet(SortedInt32KeyNode<HashBucket> root, IEqualityComparer<T> equalityComparer, int count)
        {
            Requires.NotNull(root, nameof(root));
            Requires.NotNull(equalityComparer, nameof(equalityComparer));

            root.Freeze(s_FreezeBucketAction);
            _root = root;
            _count = count;
            _equalityComparer = equalityComparer;
            _hashBucketEqualityComparer = GetHashBucketEqualityComparer(equalityComparer);
        }

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        public ImmutableHashSet<T> Clear()
        {
            Contract.Ensures(Contract.Result<ImmutableHashSet<T>>() != null);
            Contract.Ensures(Contract.Result<ImmutableHashSet<T>>().IsEmpty);
            return this.IsEmpty ? this : ImmutableHashSet<T>.Empty.WithComparer(_equalityComparer);
        }

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        public int Count
        {
            get { return _count; }
        }

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        public bool IsEmpty
        {
            get { return this.Count == 0; }
        }

        #region IHashKeyCollection<T> Properties

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        public IEqualityComparer<T> KeyComparer
        {
            get { return _equalityComparer; }
        }

        #endregion

        #region IImmutableSet<T> Properties

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableSet<T> IImmutableSet<T>.Clear()
        {
            return this.Clear();
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

        /// <summary>
        /// Gets the root node (for testing purposes).
        /// </summary>
        internal IBinaryTree Root
        {
            get { return _root; }
        }

        /// <summary>
        /// Gets a data structure that captures the current state of this map, as an input into a query or mutating function.
        /// </summary>
        private MutationInput Origin
        {
            get { return new MutationInput(this); }
        }

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
        public ImmutableHashSet<T> Add(T item)
        {
            Contract.Ensures(Contract.Result<ImmutableHashSet<T>>() != null);

            var result = Add(item, this.Origin);
            return result.Finalize(this);
        }

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        public ImmutableHashSet<T> Remove(T item)
        {
            Contract.Ensures(Contract.Result<ImmutableHashSet<T>>() != null);

            var result = Remove(item, this.Origin);
            return result.Finalize(this);
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
            int hashCode = _equalityComparer.GetHashCode(equalValue);
            HashBucket bucket;
            if (_root.TryGetValue(hashCode, out bucket))
            {
                return bucket.TryExchange(equalValue, _equalityComparer, out actualValue);
            }

            actualValue = equalValue;
            return false;
        }

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        [Pure]
        public ImmutableHashSet<T> Union(IEnumerable<T> other)
        {
            Requires.NotNull(other, nameof(other));
            Contract.Ensures(Contract.Result<ImmutableHashSet<T>>() != null);

            return this.Union(other, avoidWithComparer: false);
        }

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        [Pure]
        public ImmutableHashSet<T> Intersect(IEnumerable<T> other)
        {
            Requires.NotNull(other, nameof(other));
            Contract.Ensures(Contract.Result<ImmutableHashSet<T>>() != null);

            var result = Intersect(other, this.Origin);
            return result.Finalize(this);
        }

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        public ImmutableHashSet<T> Except(IEnumerable<T> other)
        {
            Requires.NotNull(other, nameof(other));

            var result = Except(other, _equalityComparer, _hashBucketEqualityComparer, _root);
            return result.Finalize(this);
        }

        /// <summary>
        /// Produces a set that contains elements either in this set or a given sequence, but not both.
        /// </summary>
        /// <param name="other">The other sequence of items.</param>
        /// <returns>The new set.</returns>
        [Pure]
        public ImmutableHashSet<T> SymmetricExcept(IEnumerable<T> other)
        {
            Requires.NotNull(other, nameof(other));
            Contract.Ensures(Contract.Result<IImmutableSet<T>>() != null);

            var result = SymmetricExcept(other, this.Origin);
            return result.Finalize(this);
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

            return SetEquals(other, this.Origin);
        }

        /// <summary>
        /// Determines whether the current set is a property (strict) subset of a specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns>true if the current set is a correct subset of <paramref name="other"/>; otherwise, false.</returns>
        [Pure]
        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            Requires.NotNull(other, nameof(other));

            return IsProperSubsetOf(other, this.Origin);
        }

        /// <summary>
        /// Determines whether the current set is a correct superset of a specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns>true if the current set is a correct superset of <paramref name="other"/>; otherwise, false.</returns>
        [Pure]
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            Requires.NotNull(other, nameof(other));

            return IsProperSupersetOf(other, this.Origin);
        }

        /// <summary>
        /// Determines whether a set is a subset of a specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns>true if the current set is a subset of <paramref name="other"/>; otherwise, false.</returns>
        [Pure]
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            Requires.NotNull(other, nameof(other));

            return IsSubsetOf(other, this.Origin);
        }

        /// <summary>
        /// Determines whether the current set is a superset of a specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns>true if the current set is a superset of <paramref name="other"/>; otherwise, false.</returns>
        [Pure]
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            Requires.NotNull(other, nameof(other));

            return IsSupersetOf(other, this.Origin);
        }

        /// <summary>
        /// Determines whether the current set overlaps with the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns>true if the current set and <paramref name="other"/> share at least one common element; otherwise, false.</returns>
        [Pure]
        public bool Overlaps(IEnumerable<T> other)
        {
            Requires.NotNull(other, nameof(other));

            return Overlaps(other, this.Origin);
        }

        #endregion

        #region IImmutableSet<T> Methods

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableSet<T> IImmutableSet<T>.Add(T item)
        {
            return this.Add(item);
        }

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableSet<T> IImmutableSet<T>.Remove(T item)
        {
            return this.Remove(item);
        }

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableSet<T> IImmutableSet<T>.Union(IEnumerable<T> other)
        {
            return this.Union(other);
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
        public bool Contains(T item)
        {
            return Contains(item, this.Origin);
        }

        /// <summary>
        /// See the <see cref="IImmutableSet{T}"/> interface.
        /// </summary>
        [Pure]
        public ImmutableHashSet<T> WithComparer(IEqualityComparer<T> equalityComparer)
        {
            Contract.Ensures(Contract.Result<ImmutableHashSet<T>>() != null);
            if (equalityComparer == null)
            {
                equalityComparer = EqualityComparer<T>.Default;
            }

            if (equalityComparer == _equalityComparer)
            {
                return this;
            }
            else
            {
                var result = new ImmutableHashSet<T>(equalityComparer);
                result = result.Union(this, avoidWithComparer: true);
                return result;
            }
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
            Requires.NotNull(array, nameof(array));
            Requires.Range(arrayIndex >= 0, nameof(arrayIndex));
            Requires.Range(array.Length >= arrayIndex + this.Count, nameof(arrayIndex));

            foreach (T item in this)
            {
                array[arrayIndex++] = item;
            }
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

        #region ICollection Methods

        /// <summary>
        /// Copies the elements of the <see cref="ICollection"/> to an <see cref="Array"/>, starting at a particular <see cref="Array"/> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="ICollection"/>. The <see cref="Array"/> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            Requires.NotNull(array, nameof(array));
            Requires.Range(arrayIndex >= 0, nameof(arrayIndex));
            Requires.Range(array.Length >= arrayIndex + this.Count, nameof(arrayIndex));

            foreach (T item in this)
            {
                array.SetValue(item, arrayIndex++);
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
        public Enumerator GetEnumerator()
        {
            return new Enumerator(_root);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
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
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        #region Static query and manipulator methods

        /// <summary>
        /// Performs the set operation on a given data structure.
        /// </summary>
        private static bool IsSupersetOf(IEnumerable<T> other, MutationInput origin)
        {
            Requires.NotNull(other, nameof(other));

            foreach (T item in other.GetEnumerableDisposable<T, Enumerator>())
            {
                if (!Contains(item, origin))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Performs the set operation on a given data structure.
        /// </summary>
        private static MutationResult Add(T item, MutationInput origin)
        {
            OperationResult result;
            int hashCode = origin.EqualityComparer.GetHashCode(item);
            HashBucket bucket = origin.Root.GetValueOrDefault(hashCode);
            var newBucket = bucket.Add(item, origin.EqualityComparer, out result);
            if (result == OperationResult.NoChangeRequired)
            {
                return new MutationResult(origin.Root, 0);
            }

            var newRoot = UpdateRoot(origin.Root, hashCode, origin.HashBucketEqualityComparer, newBucket);
            Debug.Assert(result == OperationResult.SizeChanged);
            return new MutationResult(newRoot, 1 /*result == OperationResult.SizeChanged ? 1 : 0*/);
        }

        /// <summary>
        /// Performs the set operation on a given data structure.
        /// </summary>
        private static MutationResult Remove(T item, MutationInput origin)
        {
            var result = OperationResult.NoChangeRequired;
            int hashCode = origin.EqualityComparer.GetHashCode(item);
            HashBucket bucket;
            var newRoot = origin.Root;
            if (origin.Root.TryGetValue(hashCode, out bucket))
            {
                var newBucket = bucket.Remove(item, origin.EqualityComparer, out result);
                if (result == OperationResult.NoChangeRequired)
                {
                    return new MutationResult(origin.Root, 0);
                }

                newRoot = UpdateRoot(origin.Root, hashCode, origin.HashBucketEqualityComparer, newBucket);
            }

            return new MutationResult(newRoot, result == OperationResult.SizeChanged ? -1 : 0);
        }

        /// <summary>
        /// Performs the set operation on a given data structure.
        /// </summary>
        private static bool Contains(T item, MutationInput origin)
        {
            int hashCode = origin.EqualityComparer.GetHashCode(item);
            HashBucket bucket;
            if (origin.Root.TryGetValue(hashCode, out bucket))
            {
                return bucket.Contains(item, origin.EqualityComparer);
            }

            return false;
        }

        /// <summary>
        /// Performs the set operation on a given data structure.
        /// </summary>
        private static MutationResult Union(IEnumerable<T> other, MutationInput origin)
        {
            Requires.NotNull(other, nameof(other));

            int count = 0;
            var newRoot = origin.Root;
            foreach (var item in other.GetEnumerableDisposable<T, Enumerator>())
            {
                int hashCode = origin.EqualityComparer.GetHashCode(item);
                HashBucket bucket = newRoot.GetValueOrDefault(hashCode);
                OperationResult result;
                var newBucket = bucket.Add(item, origin.EqualityComparer, out result);
                if (result == OperationResult.SizeChanged)
                {
                    newRoot = UpdateRoot(newRoot, hashCode, origin.HashBucketEqualityComparer, newBucket);
                    count++;
                }
            }

            return new MutationResult(newRoot, count);
        }

        /// <summary>
        /// Performs the set operation on a given data structure.
        /// </summary>
        private static bool Overlaps(IEnumerable<T> other, MutationInput origin)
        {
            Requires.NotNull(other, nameof(other));

            if (origin.Root.IsEmpty)
            {
                return false;
            }

            foreach (T item in other.GetEnumerableDisposable<T, Enumerator>())
            {
                if (Contains(item, origin))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Performs the set operation on a given data structure.
        /// </summary>
        private static bool SetEquals(IEnumerable<T> other, MutationInput origin)
        {
            Requires.NotNull(other, nameof(other));

            var otherSet = new HashSet<T>(other, origin.EqualityComparer);
            if (origin.Count != otherSet.Count)
            {
                return false;
            }

            int matches = 0;
            foreach (T item in otherSet)
            {
                if (!Contains(item, origin))
                {
                    return false;
                }

                matches++;
            }

            return matches == origin.Count;
        }

        /// <summary>
        /// Performs the set operation on a given data structure.
        /// </summary>
        private static SortedInt32KeyNode<HashBucket> UpdateRoot(SortedInt32KeyNode<HashBucket> root, int hashCode, IEqualityComparer<HashBucket> hashBucketEqualityComparer, HashBucket newBucket)
        {
            bool mutated;
            if (newBucket.IsEmpty)
            {
                return root.Remove(hashCode, out mutated);
            }
            else
            {
                return root.SetItem(hashCode, newBucket, hashBucketEqualityComparer, out bool replacedExistingValue, out mutated);
            }
        }

        /// <summary>
        /// Performs the set operation on a given data structure.
        /// </summary>
        private static MutationResult Intersect(IEnumerable<T> other, MutationInput origin)
        {
            Requires.NotNull(other, nameof(other));

            var newSet = SortedInt32KeyNode<HashBucket>.EmptyNode;
            int count = 0;
            foreach (var item in other.GetEnumerableDisposable<T, Enumerator>())
            {
                if (Contains(item, origin))
                {
                    var result = Add(item, new MutationInput(newSet, origin.EqualityComparer, origin.HashBucketEqualityComparer, count));
                    newSet = result.Root;
                    count += result.Count;
                }
            }

            return new MutationResult(newSet, count, CountType.FinalValue);
        }

        /// <summary>
        /// Performs the set operation on a given data structure.
        /// </summary>
        private static MutationResult Except(IEnumerable<T> other, IEqualityComparer<T> equalityComparer, IEqualityComparer<HashBucket> hashBucketEqualityComparer, SortedInt32KeyNode<HashBucket> root)
        {
            Requires.NotNull(other, nameof(other));
            Requires.NotNull(equalityComparer, nameof(equalityComparer));
            Requires.NotNull(root, nameof(root));

            int count = 0;
            var newRoot = root;
            foreach (var item in other.GetEnumerableDisposable<T, Enumerator>())
            {
                int hashCode = equalityComparer.GetHashCode(item);
                HashBucket bucket;
                if (newRoot.TryGetValue(hashCode, out bucket))
                {
                    OperationResult result;
                    HashBucket newBucket = bucket.Remove(item, equalityComparer, out result);
                    if (result == OperationResult.SizeChanged)
                    {
                        count--;
                        newRoot = UpdateRoot(newRoot, hashCode, hashBucketEqualityComparer, newBucket);
                    }
                }
            }

            return new MutationResult(newRoot, count);
        }

        /// <summary>
        /// Performs the set operation on a given data structure.
        /// </summary>
        [Pure]
        private static MutationResult SymmetricExcept(IEnumerable<T> other, MutationInput origin)
        {
            Requires.NotNull(other, nameof(other));

            var otherAsSet = ImmutableHashSet.CreateRange(origin.EqualityComparer, other);

            int count = 0;
            var result = SortedInt32KeyNode<HashBucket>.EmptyNode;
            foreach (T item in new NodeEnumerable(origin.Root))
            {
                if (!otherAsSet.Contains(item))
                {
                    var mutationResult = Add(item, new MutationInput(result, origin.EqualityComparer, origin.HashBucketEqualityComparer, count));
                    result = mutationResult.Root;
                    count += mutationResult.Count;
                }
            }

            foreach (T item in otherAsSet)
            {
                if (!Contains(item, origin))
                {
                    var mutationResult = Add(item, new MutationInput(result, origin.EqualityComparer, origin.HashBucketEqualityComparer, count));
                    result = mutationResult.Root;
                    count += mutationResult.Count;
                }
            }

            return new MutationResult(result, count, CountType.FinalValue);
        }

        /// <summary>
        /// Performs the set operation on a given data structure.
        /// </summary>
        private static bool IsProperSubsetOf(IEnumerable<T> other, MutationInput origin)
        {
            Requires.NotNull(other, nameof(other));

            if (origin.Root.IsEmpty)
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
            var otherSet = new HashSet<T>(other, origin.EqualityComparer);
            if (origin.Count >= otherSet.Count)
            {
                return false;
            }

            int matches = 0;
            bool extraFound = false;
            foreach (T item in otherSet)
            {
                if (Contains(item, origin))
                {
                    matches++;
                }
                else
                {
                    extraFound = true;
                }

                if (matches == origin.Count && extraFound)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Performs the set operation on a given data structure.
        /// </summary>
        private static bool IsProperSupersetOf(IEnumerable<T> other, MutationInput origin)
        {
            Requires.NotNull(other, nameof(other));

            if (origin.Root.IsEmpty)
            {
                return false;
            }

            int matchCount = 0;
            foreach (T item in other.GetEnumerableDisposable<T, Enumerator>())
            {
                matchCount++;
                if (!Contains(item, origin))
                {
                    return false;
                }
            }

            return origin.Count > matchCount;
        }

        /// <summary>
        /// Performs the set operation on a given data structure.
        /// </summary>
        private static bool IsSubsetOf(IEnumerable<T> other, MutationInput origin)
        {
            Requires.NotNull(other, nameof(other));

            if (origin.Root.IsEmpty)
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
            var otherSet = new HashSet<T>(other, origin.EqualityComparer);
            int matches = 0;
            foreach (T item in otherSet)
            {
                if (Contains(item, origin))
                {
                    matches++;
                }
            }

            return matches == origin.Count;
        }

        #endregion

        /// <summary>
        /// Wraps the specified data structure with an immutable collection wrapper.
        /// </summary>
        /// <param name="root">The root of the data structure.</param>
        /// <param name="equalityComparer">The equality comparer.</param>
        /// <param name="count">The number of elements in the data structure.</param>
        /// <returns>The immutable collection.</returns>
        private static ImmutableHashSet<T> Wrap(SortedInt32KeyNode<HashBucket> root, IEqualityComparer<T> equalityComparer, int count)
        {
            Requires.NotNull(root, nameof(root));
            Requires.NotNull(equalityComparer, nameof(equalityComparer));
            Requires.Range(count >= 0, nameof(count));
            return new ImmutableHashSet<T>(root, equalityComparer, count);
        }

        /// <summary>
        /// Gets the <see cref="IEqualityComparer{HashBucket}"/> to use.
        /// </summary>
        /// <param name="valueComparer">The value comparer for T.</param>
        /// <returns>The equality comparer to use.</returns>
        private static IEqualityComparer<HashBucket> GetHashBucketEqualityComparer(IEqualityComparer<T> valueComparer)
        {
            if (!ImmutableExtensions.IsValueType<T>())
            {
                return HashBucketByRefEqualityComparer.DefaultInstance;
            }
            else if (valueComparer == EqualityComparer<T>.Default)
            {
                return HashBucketByValueEqualityComparer.DefaultInstance;
            }
            else
            {
                return new HashBucketByValueEqualityComparer(valueComparer);
            }
        }

        /// <summary>
        /// Wraps the specified data structure with an immutable collection wrapper.
        /// </summary>
        /// <param name="root">The root of the data structure.</param>
        /// <param name="adjustedCountIfDifferentRoot">The adjusted count if the root has changed.</param>
        /// <returns>The immutable collection.</returns>
        private ImmutableHashSet<T> Wrap(SortedInt32KeyNode<HashBucket> root, int adjustedCountIfDifferentRoot)
        {
            return (root != _root) ? new ImmutableHashSet<T>(root, _equalityComparer, adjustedCountIfDifferentRoot) : this;
        }

        /// <summary>
        /// Bulk adds entries to the set.
        /// </summary>
        /// <param name="items">The entries to add.</param>
        /// <param name="avoidWithComparer"><c>true</c> when being called from <see cref="WithComparer"/> to avoid <see cref="T:System.StackOverflowException"/>.</param>
        [Pure]
        private ImmutableHashSet<T> Union(IEnumerable<T> items, bool avoidWithComparer)
        {
            Requires.NotNull(items, nameof(items));
            Contract.Ensures(Contract.Result<ImmutableHashSet<T>>() != null);

            // Some optimizations may apply if we're an empty set.
            if (this.IsEmpty && !avoidWithComparer)
            {
                // If the items being added actually come from an ImmutableHashSet<T>,
                // reuse that instance if possible.
                var other = items as ImmutableHashSet<T>;
                if (other != null)
                {
                    return other.WithComparer(this.KeyComparer);
                }
            }

            var result = Union(items, this.Origin);
            return result.Finalize(this);
        }
    }
}
