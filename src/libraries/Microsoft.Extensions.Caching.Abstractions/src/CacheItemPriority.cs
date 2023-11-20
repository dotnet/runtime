// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Caching.Memory
{
    // TODO: Granularity?
    /// <summary>
    /// Specifies how items are prioritized for preservation during a memory pressure triggered cleanup.
    /// </summary>
    public enum CacheItemPriority
    {
        /// <summary>
        /// The cache entry should be removed as soon as possible during memory pressure triggered cleanup.
        /// </summary>
        Low,

        /// <summary>
        /// The cache entry should be removed if there is no other low priority cache entries during memory pressure triggered cleanup.
        /// </summary>
        Normal,

        /// <summary>
        /// The cache entry should be removed only when there is no other low or normal priority cache entries during memory pressure triggered cleanup.
        /// </summary>
        High,

        /// <summary>
        /// The cache entry should never be removed during memory pressure triggered cleanup.
        /// </summary>
        NeverRemove,
    }
}
