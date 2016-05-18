// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Collections.Immutable
{
    /// <content>
    /// Contains the inner <see cref="ImmutableHashSet{T}.MutationInput"/> class.
    /// </content>
    public partial class ImmutableHashSet<T>
    {
        /// <summary>
        /// Description of the current data structure as input into a
        /// mutating or query method.
        /// </summary>
        private struct MutationInput
        {
            /// <summary>
            /// The root of the data structure for the collection.
            /// </summary>
            private readonly SortedInt32KeyNode<HashBucket> _root;

            /// <summary>
            /// The equality comparer.
            /// </summary>
            private readonly IEqualityComparer<T> _equalityComparer;

            /// <summary>
            /// The current number of elements in the collection.
            /// </summary>
            private readonly int _count;

            /// <summary>
            /// Initializes a new instance of the <see cref="ImmutableHashSet{T}.MutationInput"/> struct.
            /// </summary>
            /// <param name="set">The set.</param>
            internal MutationInput(ImmutableHashSet<T> set)
            {
                Requires.NotNull(set, nameof(set));
                _root = set._root;
                _equalityComparer = set._equalityComparer;
                _count = set._count;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ImmutableHashSet{T}.MutationInput"/> struct.
            /// </summary>
            /// <param name="root">The root.</param>
            /// <param name="equalityComparer">The equality comparer.</param>
            /// <param name="count">The count.</param>
            internal MutationInput(SortedInt32KeyNode<HashBucket> root, IEqualityComparer<T> equalityComparer, int count)
            {
                Requires.NotNull(root, nameof(root));
                Requires.NotNull(equalityComparer, nameof(equalityComparer));
                Requires.Range(count >= 0, nameof(count));
                _root = root;
                _equalityComparer = equalityComparer;
                _count = count;
            }

            /// <summary>
            /// Gets the root of the data structure for the collection.
            /// </summary>
            internal SortedInt32KeyNode<HashBucket> Root
            {
                get { return _root; }
            }

            /// <summary>
            /// Gets the equality comparer.
            /// </summary>
            internal IEqualityComparer<T> EqualityComparer
            {
                get { return _equalityComparer; }
            }

            /// <summary>
            /// Gets the current number of elements in the collection.
            /// </summary>
            internal int Count
            {
                get { return _count; }
            }
        }
    }
}
