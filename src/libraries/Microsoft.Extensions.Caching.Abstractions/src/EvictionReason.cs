// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Caching.Memory
{
    /// <summary>
    /// Specify the reasons why an entry was evicted from the cache.
    /// </summary>
    public enum EvictionReason
    {
        /// <summary>
        /// The item was not removed from the cache.
        /// </summary>
        None,

        /// <summary>
        /// The item was removed from the cache manually.
        /// </summary>
        Removed,

        /// <summary>
        /// The item was removed from the cache because it was overwritten.
        /// </summary>
        Replaced,

        /// <summary>
        /// The item was removed from the cache because it timed out.
        /// </summary>
        Expired,

        /// <summary>
        /// The item was removed from the cache because its token expired.
        /// </summary>
        TokenExpired,

        /// <summary>
        /// The item was removed from the cache because it exceeded its capacity.
        /// </summary>
        Capacity,
    }
}
