// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
