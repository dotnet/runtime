// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Immutable
{
    /// <content>
    /// Contains the inner <see cref="ImmutableHashSet{T}.NodeEnumerable"/> class.
    /// </content>
    public partial class ImmutableHashSet<T>
    {
        /// <summary>
        /// Enumerates over a sorted dictionary used for hash buckets.
        /// </summary>
        private readonly struct NodeEnumerable : IEnumerable<T>
        {
            /// <summary>
            /// The root of the sorted dictionary to enumerate.
            /// </summary>
            private readonly SortedInt32KeyNode<HashBucket> _root;

            /// <summary>
            /// Initializes a new instance of the <see cref="ImmutableHashSet{T}.NodeEnumerable"/> struct.
            /// </summary>
            /// <param name="root">The root.</param>
            internal NodeEnumerable(SortedInt32KeyNode<HashBucket> root)
            {
                Requires.NotNull(root, nameof(root));
                _root = root;
            }

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
            /// <returns>
            /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
            /// </returns>
            [ExcludeFromCodeCoverage] // never called internal member, but here for the interface.
            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            /// <summary>
            /// Returns an enumerator that iterates through a collection.
            /// </summary>
            /// <returns>
            /// An <see cref="IEnumerator"/> object that can be used to iterate through the collection.
            /// </returns>
            [ExcludeFromCodeCoverage] // never called internal member, but here for the interface.
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }
    }
}
