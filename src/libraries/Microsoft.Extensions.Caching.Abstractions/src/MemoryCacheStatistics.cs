// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;

namespace Microsoft.Extensions.Caching.Memory
{
    /// <summary>
    /// Holds a snapshot of statistics for a memory cache.
    /// </summary>
    public struct MemoryCacheStatistics
    {
        /// <summary>
        /// A snapshot of entry count at the current state
        /// </summary>
        public long CurrentEntryCount { get; init; }

        /// <summary>
        /// A snapshot of size at the current state
        /// </summary>
        public long? CurrentSize { get; init; }

        /// <summary>
        /// Total number of requests
        /// </summary>
        public long TotalRequests { get; init; }

        /// <summary>
        /// Total number of hits
        /// </summary>
        public long TotalHits { get; init; }
    }
}
