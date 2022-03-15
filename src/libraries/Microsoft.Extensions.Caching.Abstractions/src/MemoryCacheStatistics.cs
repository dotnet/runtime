// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;

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
        /// A snapshot of entry count at the current state
        /// </summary>
        public long CurrentEntryCount { get; init; }

        /// <summary>
        /// A snapshot of size at the current state
        /// </summary>
        public long? CurrentEstimatedSize { get; init; }

        /// <summary>
        /// Total number of cache misses
        /// </summary>
        public long TotalMisses { get; init; }

        /// <summary>
        /// Total number of cache hits
        /// </summary>
        public long TotalHits { get; init; }
    }
}
