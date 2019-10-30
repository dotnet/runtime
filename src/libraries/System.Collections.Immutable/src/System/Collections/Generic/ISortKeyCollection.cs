// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Generic
{
    /// <summary>
    /// Defined on a generic collection that sorts its contents using an <see cref="IComparer{TKey}"/>.
    /// </summary>
    /// <typeparam name="TKey">The type of element sorted in the collection.</typeparam>
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    internal interface ISortKeyCollection<in TKey>
    {
        /// <summary>
        /// Gets the comparer used to sort keys.
        /// </summary>
        IComparer<TKey> KeyComparer { get; }
    }
}
