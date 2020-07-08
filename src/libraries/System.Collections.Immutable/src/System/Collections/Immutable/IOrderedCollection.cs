// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Collections.Immutable
{
    /// <summary>
    /// Describes an ordered collection of elements.
    /// </summary>
    /// <typeparam name="T">The type of element in the collection.</typeparam>
    internal interface IOrderedCollection<out T> : IEnumerable<T>
    {
        /// <summary>
        /// Gets the number of elements in the collection.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the element in the collection at a given index.
        /// </summary>
        T this[int index] { get; }
    }
}
