// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Validation;

namespace System.Collections.Immutable
{
    /// <content>
    /// Contains the inner MutationInput class.
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
            private readonly ImmutableSortedDictionary<int, HashBucket>.Node root;

            /// <summary>
            /// The equality comparer.
            /// </summary>
            private readonly IEqualityComparer<T> equalityComparer;

            /// <summary>
            /// The current number of elements in the collection.
            /// </summary>
            private readonly int count;

            /// <summary>
            /// Initializes a new instance of the <see cref="ImmutableHashSet&lt;T&gt;.MutationInput"/> struct.
            /// </summary>
            /// <param name="set">The set.</param>
            internal MutationInput(ImmutableHashSet<T> set)
            {
                Requires.NotNull(set, "set");
                this.root = set.root;
                this.equalityComparer = set.equalityComparer;
                this.count = set.count;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ImmutableHashSet&lt;T&gt;.MutationInput"/> struct.
            /// </summary>
            /// <param name="root">The root.</param>
            /// <param name="equalityComparer">The equality comparer.</param>
            /// <param name="count">The count.</param>
            internal MutationInput(ImmutableSortedDictionary<int, HashBucket>.Node root, IEqualityComparer<T> equalityComparer, int count)
            {
                Requires.NotNull(root, "root");
                Requires.NotNull(equalityComparer, "equalityComparer");
                Requires.Range(count >= 0, "count");
                this.root = root;
                this.equalityComparer = equalityComparer;
                this.count = count;
            }

            /// <summary>
            /// Gets the root of the data structure for the collection.
            /// </summary>
            internal ImmutableSortedDictionary<int, HashBucket>.Node Root
            {
                get { return this.root; }
            }

            /// <summary>
            /// Gets the equality comparer.
            /// </summary>
            internal IEqualityComparer<T> EqualityComparer
            {
                get { return this.equalityComparer; }
            }

            /// <summary>
            /// Gets the current number of elements in the collection.
            /// </summary>
            internal int Count
            {
                get { return this.count; }
            }
        }
    }
}
