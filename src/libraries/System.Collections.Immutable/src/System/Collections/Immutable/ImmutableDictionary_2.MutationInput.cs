// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Collections.Immutable
{
    /// <content>
    /// Contains the inner <see cref="ImmutableDictionary{TKey, TValue}.MutationInput"/> class.
    /// </content>
    public partial class ImmutableDictionary<TKey, TValue>
    {
        /// <summary>
        /// Description of the current data structure as input into a
        /// mutating or query method.
        /// </summary>
        private readonly struct MutationInput
        {
            /// <summary>
            /// The root of the data structure for the collection.
            /// </summary>
            private readonly SortedInt32KeyNode<HashBucket> _root;

            /// <summary>
            /// The comparer used when comparing hash buckets.
            /// </summary>
            private readonly Comparers _comparers;

            /// <summary>
            /// Initializes a new instance of the <see cref="ImmutableDictionary{TKey, TValue}.MutationInput"/> struct.
            /// </summary>
            /// <param name="root">The root.</param>
            /// <param name="comparers">The comparers.</param>
            /// 
            internal MutationInput(
                SortedInt32KeyNode<HashBucket> root,
                Comparers comparers)
            {
                _root = root;
                _comparers = comparers;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ImmutableDictionary{TKey, TValue}.MutationInput"/> struct.
            /// </summary>
            /// <param name="map">The map.</param>
            internal MutationInput(ImmutableDictionary<TKey, TValue> map)
            {
                _root = map._root;
                _comparers = map._comparers;
            }

            /// <summary>
            /// Gets the root of the data structure for the collection.
            /// </summary>
            internal SortedInt32KeyNode<HashBucket> Root
            {
                get { return _root; }
            }

            /// <summary>
            /// Gets the set of comparers.
            /// </summary>
            internal Comparers Comparers
            {
                get { return _comparers; }
            }

            /// <summary>
            /// Gets the key comparer.
            /// </summary>
            internal IEqualityComparer<TKey> KeyComparer
            {
                get { return _comparers.KeyComparer; }
            }

            /// <summary>
            /// Gets the key only comparer.
            /// </summary>
            internal IEqualityComparer<KeyValuePair<TKey, TValue>> KeyOnlyComparer
            {
                get { return _comparers.KeyOnlyComparer; }
            }

            /// <summary>
            /// Gets the value comparer.
            /// </summary>
            internal IEqualityComparer<TValue> ValueComparer
            {
                get { return _comparers.ValueComparer; }
            }

            /// <summary>
            /// Gets the comparers.
            /// </summary>
            internal IEqualityComparer<HashBucket> HashBucketComparer
            {
                get { return _comparers.HashBucketEqualityComparer; }
            }
        }
    }
}
