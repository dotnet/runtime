// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Collections.Immutable
{
    /// <summary>
    /// A fast read-only set.
    /// </summary>
    /// <typeparam name="T">The type of the items in the set.</typeparam>
    /// <remarks>
    /// Frozen sets are optimized for highly efficient read access and dense
    /// memory representation. For this, they trade off creation time. Creating a frozen
    /// set can take a relatively long time, making frozen sets best suited
    /// for long-lived sets that will experience many lookups in their lifetime
    /// in order to compensate for the cost of creation.
    /// </remarks>
    public interface IFrozenSet<T> : IReadOnlySet<T>
        where T : notnull
    {
        /// <summary>
        /// Gets a list containing the items in the set.
        /// </summary>
        /// <remarks>
        /// The order of the items returned does not correspond to the order in which the items were introduced into the set.
        /// </remarks>
        FrozenList<T> Items { get; }

        /// <summary>
        /// Returns an enumerator that iterates through a set.
        /// </summary>
        /// <returns>An enumerator that makes it possible to iterate through a set's items.</returns>
        new FrozenEnumerator<T> GetEnumerator();
    }
}
