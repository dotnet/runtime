// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Caching.Memory
{
    /// <summary>
    /// Holds a snapshot of statistics for a memory cache.
    /// </summary>
    public class MemoryCacheStatistics
    {
        /// <summary>
        /// Initializes an instance of MemoryCacheStatistics.
        /// </summary>
        public MemoryCacheStatistics() { }

        /// <summary>
        /// Gets the number of <see cref="ICacheEntry" /> instances currently in the memory cache.
        /// </summary>
        public long CurrentEntryCount { get; init; }

        /// <summary>
        /// Gets an estimated sum of all the <see cref="ICacheEntry.Size" /> values currently in the memory cache.
        /// </summary>
        /// <returns>Returns <see langword="null"/> if size isn't being tracked. The common MemoryCache implementation tracks size whenever a SizeLimit is set on the cache.</returns>
        public long? CurrentEstimatedSize { get; init; }

        /// <summary>
        /// Gets the total number of cache misses.
        /// </summary>
        public long TotalMisses { get; init; }

        /// <summary>
        /// Gets the total number of cache hits.
        /// </summary>
        public long TotalHits { get; init; }
    }
}
