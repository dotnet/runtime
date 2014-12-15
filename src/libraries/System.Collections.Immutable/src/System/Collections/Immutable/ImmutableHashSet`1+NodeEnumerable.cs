// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Validation;

namespace System.Collections.Immutable
{
    /// <content>
    /// Contains the inner NodeEnumerable class.
    /// </content>
    public partial class ImmutableHashSet<T>
    {
        /// <summary>
        /// Enumerates over a sorted dictionary used for hash buckets.
        /// </summary>
        private struct NodeEnumerable : IEnumerable<T>
        {
            /// <summary>
            /// The root of the sorted dictionary to enumerate.
            /// </summary>
            private readonly SortedInt32KeyNode<HashBucket> _root;

            /// <summary>
            /// Initializes a new instance of the <see cref="ImmutableHashSet&lt;T&gt;.NodeEnumerable"/> struct.
            /// </summary>
            /// <param name="root">The root.</param>
            internal NodeEnumerable(SortedInt32KeyNode<HashBucket> root)
            {
                Requires.NotNull(root, "root");
                _root = root;
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
            /// </returns>
            public Enumerator GetEnumerator()
            {
                return new Enumerator(_root);
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
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
            /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
            /// </returns>
            [ExcludeFromCodeCoverage] // never called internal member, but here for the interface.
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }
    }
}
