// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Caching.Memory
{
    /// <summary>
    /// Represents an entry in the <see cref="IMemoryCache"/> implementation.
    /// When disposed, the entry is committed to the cache if a <see cref="Value"/> has been set.
    /// </summary>
    public interface ICacheEntry : IDisposable
    {
        /// <summary>
        /// Gets the key of the cache entry.
        /// </summary>
        object Key { get; }

        /// <summary>
        /// Gets or set the value of the cache entry.
        /// </summary>
        object? Value { get; set; }

        /// <summary>
        /// Gets or sets an absolute expiration date for the cache entry.
        /// </summary>
        DateTimeOffset? AbsoluteExpiration { get; set; }

        /// <summary>
        /// Gets or sets an absolute expiration time, relative to now.
        /// </summary>
        TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }

        /// <summary>
        /// Gets or sets how long a cache entry can be inactive (e.g. not accessed) before it will be removed.
        /// This will not extend the entry lifetime beyond the absolute expiration (if set).
        /// </summary>
        TimeSpan? SlidingExpiration { get; set; }

        /// <summary>
        /// Gets the <see cref="IChangeToken"/> instances which cause the cache entry to expire.
        /// </summary>
        IList<IChangeToken> ExpirationTokens { get; }

        /// <summary>
        /// Gets or sets the callbacks will be fired after the cache entry is evicted from the cache.
        /// </summary>
        IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get; }

        /// <summary>
        /// Gets or sets the priority for keeping the cache entry in the cache during a
        ///  cleanup. The default is <see cref="CacheItemPriority.Normal"/>.
        /// </summary>
        CacheItemPriority Priority { get; set; }

        /// <summary>
        /// Gets or set the size of the cache entry value.
        /// </summary>
        /// <remarks>
        /// The size is used by <see cref="IMemoryCache"/> implementations that enforce a size limit, and is
        /// read both when the entry is added to the cache and when it is removed from it. Implementations
        /// are therefore free to reject a change made after the entry has been disposed; <c>MemoryCache</c>
        /// throws an <see cref="InvalidOperationException"/> in that case, whether or not the entry was
        /// ultimately committed to the cache.
        /// </remarks>
        long? Size { get; set; }
    }
}
