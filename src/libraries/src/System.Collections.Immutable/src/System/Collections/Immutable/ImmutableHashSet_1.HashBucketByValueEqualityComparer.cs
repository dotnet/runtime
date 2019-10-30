// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Collections.Immutable
{
    /// <content>
    /// Contains the inner <see cref="ImmutableHashSet{T}.HashBucketByValueEqualityComparer"/> class.
    /// </content>
    public sealed partial class ImmutableHashSet<T> : IImmutableSet<T>, IHashKeyCollection<T>, IReadOnlyCollection<T>, ICollection<T>, ISet<T>, ICollection, IStrongEnumerable<T, ImmutableHashSet<T>.Enumerator>
    {
        /// <summary>
        /// Compares equality between two <see cref="HashBucket"/> instances
        /// by value.
        /// </summary>
        private class HashBucketByValueEqualityComparer : IEqualityComparer<HashBucket>
        {
            /// <summary>
            /// The instance to use when the value comparer is <see cref="EqualityComparer{T}.Default"/>.
            /// </summary>
            private static readonly IEqualityComparer<HashBucket> s_defaultInstance = new HashBucketByValueEqualityComparer(EqualityComparer<T>.Default);

            /// <summary>
            /// Gets the instance to use when the value comparer is
            /// <see cref="EqualityComparer{T}.Default"/>.
            /// </summary>
            internal static IEqualityComparer<HashBucket> DefaultInstance => s_defaultInstance;

            /// <summary>
            /// The value comparer to use when comparing two T instances.
            /// </summary>
            private readonly IEqualityComparer<T> _valueComparer;

            /// <summary>
            /// Initializes a new instance of the <see cref="HashBucketByValueEqualityComparer"/> class.
            /// </summary>
            /// <param name="valueComparer">The value comparer for T.</param>
            internal HashBucketByValueEqualityComparer(IEqualityComparer<T> valueComparer)
            {
                Requires.NotNull(valueComparer, nameof(valueComparer));
                _valueComparer = valueComparer;
            }

            /// <inheritdoc />
            public bool Equals(HashBucket x, HashBucket y) => x.EqualsByValue(y, _valueComparer);

            /// <inheritdoc />
            public int GetHashCode(HashBucket obj) => throw new NotSupportedException();
        }
    }
}
