// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Collections.Immutable
{
    /// <content>
    /// Contains the inner MutationInput class.
    /// </content>
    public partial class ImmutableDictionary<TKey, TValue>
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
            /// The comparer used when comparing hash buckets.
            /// </summary>
            private readonly Comparers comparers;

            /// <summary>
            /// The current number of elements in the collection.
            /// </summary>
            private readonly int count;

            /// <summary>
            /// Initializes a new instance of the <see cref="ImmutableDictionary&lt;TKey, TValue&gt;.MutationInput"/> struct.
            /// </summary>
            /// <param name="root">The root.</param>
            /// <param name="comparers">The comparers.</param>
            /// <param name="count">The current number of elements in the collection.</param>
            internal MutationInput(
                ImmutableSortedDictionary<int, HashBucket>.Node root,
                Comparers comparers,
                int count)
            {
                this.root = root;
                this.comparers = comparers;
                this.count = count;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ImmutableDictionary&lt;TKey, TValue&gt;.MutationInput"/> struct.
            /// </summary>
            /// <param name="map">The map.</param>
            internal MutationInput(ImmutableDictionary<TKey, TValue> map)
            {
                this.root = map.root;
                this.comparers = map.comparers;
                this.count = map.count;
            }

            /// <summary>
            /// Gets the root of the data structure for the collection.
            /// </summary>
            internal ImmutableSortedDictionary<int, HashBucket>.Node Root
            {
                get { return this.root; }
            }

            /// <summary>
            /// Gets the key comparer.
            /// </summary>
            internal IEqualityComparer<TKey> KeyComparer
            {
                get { return this.comparers.KeyComparer; }
            }

            /// <summary>
            /// Gets the key only comparer.
            /// </summary>
            internal IEqualityComparer<KeyValuePair<TKey, TValue>> KeyOnlyComparer
            {
                get { return this.comparers.KeyOnlyComparer; }
            }

            /// <summary>
            /// Gets the value comparer.
            /// </summary>
            internal IEqualityComparer<TValue> ValueComparer
            {
                get { return this.comparers.ValueComparer; }
            }

            /// <summary>
            /// Gets the comparers.
            /// </summary>
            internal IEqualityComparer<HashBucket> HashBucketComparer
            {
                get { return this.comparers.HashBucketEqualityComparer; }
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
