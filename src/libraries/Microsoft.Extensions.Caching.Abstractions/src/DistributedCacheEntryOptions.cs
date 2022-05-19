// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Caching.Distributed
{
    /// <summary>
    /// Provides the cache options for an entry in <see cref="IDistributedCache"/>.
    /// </summary>
    public struct DistributedCacheEntryOptions
    {
        public readonly DateTimeOffset? AbsoluteExpiration { get; init; }
        public readonly TimeSpan? AbsoluteExpirationRelativeToNow { get; init; }
        public readonly TimeSpan? SlidingExpiration { get; init; }

        public DistributedCacheEntryOptions(DateTimeOffset? absoluteExpiration,
            TimeSpan? absoluteExpirationRelativeToNow, TimeSpan? slidingExpiration)
        {
            if (absoluteExpirationRelativeToNow <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(AbsoluteExpirationRelativeToNow),
                    absoluteExpirationRelativeToNow,
                    "The relative expiration value must be positive.");
            }

            if (slidingExpiration <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(SlidingExpiration),
                    slidingExpiration,
                    "The sliding expiration value must be positive.");
            }

            AbsoluteExpiration = absoluteExpiration;
            AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow;
            SlidingExpiration = slidingExpiration;
        }

        public DistributedCacheEntryOptions()
        {
            AbsoluteExpiration = null;
            AbsoluteExpirationRelativeToNow = null;
            SlidingExpiration = null;
        }
    }
}
