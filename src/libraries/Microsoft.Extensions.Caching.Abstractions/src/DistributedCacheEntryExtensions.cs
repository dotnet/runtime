// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Caching.Distributed
{
    public static class DistributedCacheEntryExtensions
    {
        /// <summary>
        /// Sets an absolute expiration time, relative to now.
        /// </summary>
        /// <param name="options">The options to be operated on.</param>
        /// <param name="relative">The expiration time, relative to now.</param>
        public static DistributedCacheEntryOptions SetAbsoluteExpiration(
            this DistributedCacheEntryOptions options,
            TimeSpan relative)
        {
            options.AbsoluteExpirationRelativeToNow = relative;
            return options;
        }

        /// <summary>
        /// Sets an absolute expiration date for the cache entry.
        /// </summary>
        /// <param name="options">The options to be operated on.</param>
        /// <param name="absolute">The expiration time, in absolute terms.</param>
        public static DistributedCacheEntryOptions SetAbsoluteExpiration(
            this DistributedCacheEntryOptions options,
            DateTimeOffset absolute)
        {
            options.AbsoluteExpiration = absolute;
            return options;
        }

        /// <summary>
        /// Sets how long the cache entry can be inactive (e.g. not accessed) before it will be removed.
        /// This will not extend the entry lifetime beyond the absolute expiration (if set).
        /// </summary>
        /// <param name="options">The options to be operated on.</param>
        /// <param name="offset">The sliding expiration time.</param>
        public static DistributedCacheEntryOptions SetSlidingExpiration(
            this DistributedCacheEntryOptions options,
            TimeSpan offset)
        {
            options.SlidingExpiration = offset;
            return options;
        }
    }
}
