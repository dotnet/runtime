// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Collections.Generic
{
    /// <summary>
    /// Generic collection that guarantees the uniqueness of its elements, as defined
    /// by some comparer. It also supports basic set operations such as Union, Intersection,
    /// Complement and Exclusive Complement.
    /// </summary>
    public interface ISet<T> : ICollection<T>, IReadOnlySet<T>
    {
        //Add ITEM to the set, return true if added, false if duplicate
        new bool Add(T item);

        //Transform this set into its union with the IEnumerable<T> other
        void UnionWith(IEnumerable<T> other);

        //Transform this set into its intersection with the IEnumerable<T> other
        void IntersectWith(IEnumerable<T> other);

        //Transform this set so it contains no elements that are also in other
        void ExceptWith(IEnumerable<T> other);

        //Transform this set so it contains elements initially in this or in other, but not both
        void SymmetricExceptWith(IEnumerable<T> other);

        //Check if this set is a subset of other
        new bool IsSubsetOf(IEnumerable<T> other);

        //Check if this set is a superset of other
        new bool IsSupersetOf(IEnumerable<T> other);

        //Check if this set is a subset of other, but not the same as it
        new bool IsProperSupersetOf(IEnumerable<T> other);

        //Check if this set is a superset of other, but not the same as it
        new bool IsProperSubsetOf(IEnumerable<T> other);

        //Check if this set has any elements in common with other
        new bool Overlaps(IEnumerable<T> other);

        //Check if this set contains the same and only the same elements as other
        new bool SetEquals(IEnumerable<T> other);

        /// <summary>
        /// Determines if the set contains a specific item
        /// </summary>
        /// <param name="item">The item to check if the set contains.</param>
        /// <returns><see langword="true" /> if found; otherwise <see langword="false" />.</returns>
        new bool Contains(T item) => ((ICollection<T>)this).Contains(item);

        bool IReadOnlySet<T>.IsSubsetOf(IEnumerable<T> other) => IsSubsetOf(other);

        bool IReadOnlySet<T>.IsSupersetOf(IEnumerable<T> other) => IsSupersetOf(other);

        bool IReadOnlySet<T>.IsProperSupersetOf(IEnumerable<T> other) => IsProperSupersetOf(other);

        bool IReadOnlySet<T>.IsProperSubsetOf(IEnumerable<T> other) => IsProperSubsetOf(other);

        bool IReadOnlySet<T>.Overlaps(IEnumerable<T> other) => Overlaps(other);

        bool IReadOnlySet<T>.SetEquals(IEnumerable<T> other) => SetEquals(other);

        bool IReadOnlySet<T>.Contains(T value) => ((ICollection<T>)this).Contains(value);
    }
}
