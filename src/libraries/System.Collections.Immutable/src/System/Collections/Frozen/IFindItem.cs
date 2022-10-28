// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Collections.Immutable
{
    /// <summary>
    /// Makes it possible to lookup items in a set and get their index.
    /// </summary>
    /// <typeparam name="T">The type of item in the set.</typeparam>
    internal interface IFindItem<in T>
    {
        /// <summary>
        /// Finds the index of a specific item in a set.
        /// </summary>
        /// <param name="item">The item to lookup.</param>
        /// <returns>The index of the item, or -1 if not found.</returns>
        int FindItemIndex(T item);
    }
}
