// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Collections.Immutable
{
    /// <content>
    /// Contains the inner <see cref="ImmutableHashSet{T}.HashBucketByRefEqualityComparer"/> class.
    /// </content>
    public sealed partial class ImmutableHashSet<T> : IImmutableSet<T>, IHashKeyCollection<T>, IReadOnlyCollection<T>, ICollection<T>, ISet<T>, ICollection, IStrongEnumerable<T, ImmutableHashSet<T>.Enumerator>
    {
        /// <summary>
        /// Compares equality between two <see cref="HashBucket"/> instances
        /// by reference.
        /// </summary>
        private sealed class HashBucketByRefEqualityComparer : IEqualityComparer<HashBucket>
        {
            /// <summary>
            /// The singleton instance.
            /// </summary>
            private static readonly IEqualityComparer<HashBucket> s_defaultInstance = new HashBucketByRefEqualityComparer();

            /// <summary>
            /// Gets the singleton instance to use.
            /// </summary>
            internal static IEqualityComparer<HashBucket> DefaultInstance => s_defaultInstance;

            /// <summary>
            /// Prevents a default instance of the <see cref="HashBucketByRefEqualityComparer"/> class from being created.
            /// </summary>
            private HashBucketByRefEqualityComparer()
            {
            }

            /// <inheritdoc />
            public bool Equals(HashBucket x, HashBucket y) => x.EqualsByRef(y);

            /// <inheritdoc />
            public int GetHashCode(HashBucket obj) => throw new NotSupportedException();
        }
    }
}
