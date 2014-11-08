// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Collections.Generic
{
    /// <summary>
    /// Defined on a generic collection that hashes its contents using an <see cref="IEqualityComparer{TKey}"/>.
    /// </summary>
    /// <typeparam name="TKey">The type of element hashed in the collection.</typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    internal interface IHashKeyCollection<in TKey>
    {
        /// <summary>
        /// Gets the comparer used to obtain hash codes for the keys and check equality.
        /// </summary>
        IEqualityComparer<TKey> KeyComparer { get; }
    }
}
