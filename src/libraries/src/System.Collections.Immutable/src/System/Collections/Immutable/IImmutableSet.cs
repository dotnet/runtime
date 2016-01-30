// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

namespace System.Collections.Immutable
{
    /// <summary>
    ///  A set of elements that can only be modified by creating a new instance of the set.
    /// </summary>
    /// <typeparam name="T">The type of element stored in the set.</typeparam>
    /// <remarks>
    /// Mutations on this set generate new sets.  Incremental changes to a set share as much memory as possible with the prior versions of a set,
    /// while allowing garbage collection to clean up any unique set data that is no longer being referenced.
    /// </remarks>
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Ignored")]
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix", Justification = "Ignored")]
    public interface IImmutableSet<T> : IReadOnlyCollection<T>
    {
        /// <summary>
        /// Gets an empty set that retains the same sort or unordered semantics that this instance has.
        /// </summary>
        [Pure]
        IImmutableSet<T> Clear();

        /// <summary>
        /// Determines whether this set contains the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///   <c>true</c> if the set contains the specified value; otherwise, <c>false</c>.
        /// </returns>
        [Pure]
        bool Contains(T value);

        /// <summary>
        /// Adds the specified value to this set.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <returns>A new set with the element added, or this set if the element is already in this set.</returns>
        [Pure]
        IImmutableSet<T> Add(T value);

        /// <summary>
        /// Removes the specified value from this set.
        /// </summary>
        /// <param name="value">The value to remove.</param>
        /// <returns>A new set with the element removed, or this set if the element is not in this set.</returns>
        [Pure]
        IImmutableSet<T> Remove(T value);

        /// <summary>
        /// Searches the set for a given value and returns the equal value it finds, if any.
        /// </summary>
        /// <param name="equalValue">The value to search for.</param>
        /// <param name="actualValue">The value from the set that the search found, or <paramref name="equalValue"/> if the search yielded no match.</param>
        /// <returns>A value indicating whether the search was successful.</returns>
        /// <remarks>
        /// This can be useful when you want to reuse a previously stored reference instead of
        /// a newly constructed one (so that more sharing of references can occur) or to look up
        /// a value that has more complete data than the value you currently have, although their
        /// comparer functions indicate they are equal.
        /// </remarks>
        [Pure]
        bool TryGetValue(T equalValue, out T actualValue);

        /// <summary>
        /// Produces a set that contains elements that exist in both this set and the specified set.
        /// </summary>
        /// <param name="other">The set to intersect with this one.</param>
        /// <returns>A new set that contains any elements that exist in both sets.</returns>
        [Pure]
        IImmutableSet<T> Intersect(IEnumerable<T> other);

        /// <summary>
        /// Removes a given set of items from this set.
        /// </summary>
        /// <param name="other">The items to remove from this set.</param>
        /// <returns>The new set with the items removed; or the original set if none of the items were in the set.</returns>
        [Pure]
        IImmutableSet<T> Except(IEnumerable<T> other);

        /// <summary>
        /// Produces a set that contains elements either in this set or a given sequence, but not both.
        /// </summary>
        /// <param name="other">The other sequence of items.</param>
        /// <returns>The new set.</returns>
        [Pure]
        IImmutableSet<T> SymmetricExcept(IEnumerable<T> other);

        /// <summary>
        /// Adds a given set of items to this set.
        /// </summary>
        /// <param name="other">The items to add.</param>
        /// <returns>The new set with the items added; or the original set if all the items were already in the set.</returns>
        [Pure]
        IImmutableSet<T> Union(IEnumerable<T> other);

        /// <summary>
        /// Checks whether a given sequence of items entirely describe the contents of this set.
        /// </summary>
        /// <param name="other">The sequence of items to check against this set.</param>
        /// <returns>A value indicating whether the sets are equal.</returns>
        [Pure]
        bool SetEquals(IEnumerable<T> other);

        /// <summary>
        /// Determines whether the current set is a proper (strict) subset of a specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns>true if the current set is a correct subset of other; otherwise, false.</returns>
        [Pure]
        bool IsProperSubsetOf(IEnumerable<T> other);

        /// <summary>
        /// Determines whether the current set is a proper superset of a specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns>true if the current set is a correct superset of other; otherwise, false.</returns>
        [Pure]
        bool IsProperSupersetOf(IEnumerable<T> other);

        /// <summary>
        /// Determines whether a set is a subset of a specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns>true if the current set is a subset of other; otherwise, false.</returns>
        [Pure]
        bool IsSubsetOf(IEnumerable<T> other);

        /// <summary>
        /// Determines whether the current set is a superset of a specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns>true if the current set is a superset of other; otherwise, false.</returns>
        [Pure]
        bool IsSupersetOf(IEnumerable<T> other);

        /// <summary>
        /// Determines whether the current set overlaps with the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns>true if the current set and other share at least one common element; otherwise, false.</returns>
        [Pure]
        bool Overlaps(IEnumerable<T> other);
    }
}
