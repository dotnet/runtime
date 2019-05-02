// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Collections.Generic
{
    /// <summary>
    /// A comparer for two <see cref="SortedSet{T}"/>.
    /// </summary>
    internal sealed class SortedSetEqualityComparer<T> : IEqualityComparer<SortedSet<T>> 
    {
        private readonly IComparer<T> _comparer;
        private readonly IEqualityComparer<T> _memberEqualityComparer;

        public SortedSetEqualityComparer(IEqualityComparer<T>? memberEqualityComparer)
            : this(comparer: null, memberEqualityComparer: memberEqualityComparer)
        { }

        /// <summary>
        /// Create a new SetEqualityComparer, given a comparer for member order and another for member equality (these
        /// must be consistent in their definition of equality)
        /// </summary>        
        private SortedSetEqualityComparer(IComparer<T>? comparer, IEqualityComparer<T>? memberEqualityComparer)
        {
            _comparer = comparer ?? Comparer<T>.Default;
            _memberEqualityComparer = memberEqualityComparer ?? EqualityComparer<T>.Default;
        }

        // Use _comparer to keep equals properties intact; don't want to choose one of the comparers.
        public bool Equals(SortedSet<T> x, SortedSet<T> y) => SortedSet<T>.SortedSetEquals(x, y, _comparer);

        // IMPORTANT: this part uses the fact that GetHashCode() is consistent with the notion of equality in the set.
        public int GetHashCode(SortedSet<T> obj)
        {
            int hashCode = 0;
            if (obj != null)
            {
                foreach (T t in obj)
                {
                    hashCode = hashCode ^ (_memberEqualityComparer.GetHashCode(t) & 0x7FFFFFFF);
                }
            }
            // Returns 0 for null sets.
            return hashCode;
        }

        // Equals method for the comparer itself. 
        public override bool Equals(object? obj)
        {
            SortedSetEqualityComparer<T>? comparer = obj as SortedSetEqualityComparer<T>;
            return comparer != null && _comparer == comparer._comparer;
        }

        public override int GetHashCode() => _comparer.GetHashCode() ^ _memberEqualityComparer.GetHashCode();
    }
}
