// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Caching.Memory
{
    public static class MemoryCacheEntryExtensions
    {
        /// <summary>
        /// Sets the priority for keeping the cache entry in the cache during a memory pressure tokened cleanup.
        /// </summary>
        /// <param name="options">The option on which to set the priority.</param>
        /// <param name="priority">The <see cref="CacheItemPriority"/> to set on the option.</param>
        /// <returns>The <see cref="MemoryCacheEntryOptions"/> so that additional calls can be chained.</returns>
        public static MemoryCacheEntryOptions SetPriority(
            this MemoryCacheEntryOptions options,
            CacheItemPriority priority)
        {
            options.Priority = priority;
            return options;
        }

        /// <summary>
        /// Sets the size of the cache entry value.
        /// </summary>
        /// <param name="options">The options to set the entry size on.</param>
        /// <param name="size">The size to set on the <see cref="MemoryCacheEntryOptions"/>.</param>
        /// <returns>The <see cref="MemoryCacheEntryOptions"/> so that additional calls can be chained.</returns>
        public static MemoryCacheEntryOptions SetSize(
            this MemoryCacheEntryOptions options,
            long size)
        {
            if (size < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size), size, $"{nameof(size)} must be non-negative.");
            }

            options.Size = size;
            return options;
        }

        /// <summary>
        /// Expire the cache entry if the given <see cref="IChangeToken"/> expires.
        /// </summary>
        /// <param name="options">The <see cref="MemoryCacheEntryOptions"/>.</param>
        /// <param name="expirationToken">The <see cref="IChangeToken"/> that causes the cache entry to expire.</param>
        /// <returns>The <see cref="MemoryCacheEntryOptions"/> so that additional calls can be chained.</returns>
        public static MemoryCacheEntryOptions AddExpirationToken(
            this MemoryCacheEntryOptions options,
            IChangeToken expirationToken)
        {
            if (expirationToken == null)
            {
                throw new ArgumentNullException(nameof(expirationToken));
            }

            options.ExpirationTokens.Add(expirationToken);
            return options;
        }

        /// <summary>
        /// Sets an absolute expiration time, relative to now.
        /// </summary>
        /// <param name="options">The <see cref="MemoryCacheEntryOptions"/>.</param>
        /// <param name="relative">The expiration time, relative to now.</param>
        /// <returns>The <see cref="MemoryCacheEntryOptions"/> so that additional calls can be chained.</returns>
        public static MemoryCacheEntryOptions SetAbsoluteExpiration(
            this MemoryCacheEntryOptions options,
            TimeSpan relative)
        {
            options.AbsoluteExpirationRelativeToNow = relative;
            return options;
        }

        /// <summary>
        /// Sets an absolute expiration date for the cache entry.
        /// </summary>
        /// <param name="options">The <see cref="MemoryCacheEntryOptions"/>.</param>
        /// <param name="absolute">The expiration time, in absolute terms.</param>
        /// <returns>The <see cref="MemoryCacheEntryOptions"/> so that additional calls can be chained.</returns>
        public static MemoryCacheEntryOptions SetAbsoluteExpiration(
            this MemoryCacheEntryOptions options,
            DateTimeOffset absolute)
        {
            options.AbsoluteExpiration = absolute;
            return options;
        }

        /// <summary>
        /// Sets how long the cache entry can be inactive (e.g. not accessed) before it will be removed.
        /// This will not extend the entry lifetime beyond the absolute expiration (if set).
        /// </summary>
        /// <param name="options">The <see cref="MemoryCacheEntryOptions"/>.</param>
        /// <param name="offset">The sliding expiration time.</param>
        /// <returns>The <see cref="MemoryCacheEntryOptions"/> so that additional calls can be chained.</returns>
        public static MemoryCacheEntryOptions SetSlidingExpiration(
            this MemoryCacheEntryOptions options,
            TimeSpan offset)
        {
            options.SlidingExpiration = offset;
            return options;
        }

        /// <summary>
        /// The given callback will be fired after the cache entry is evicted from the cache.
        /// </summary>
        /// <param name="options">The <see cref="MemoryCacheEntryOptions"/>.</param>
        /// <param name="callback">The callback to register for calling after an entry is evicted.</param>
        /// <returns>The <see cref="MemoryCacheEntryOptions"/> so that additional calls can be chained.</returns>
        public static MemoryCacheEntryOptions RegisterPostEvictionCallback(
            this MemoryCacheEntryOptions options,
            PostEvictionDelegate callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            return options.RegisterPostEvictionCallback(callback, state: null);
        }

        /// <summary>
        /// The given callback will be fired after the cache entry is evicted from the cache.
        /// </summary>
        /// <param name="options">The <see cref="MemoryCacheEntryOptions"/>.</param>
        /// <param name="callback">The callback to register for calling after an entry is evicted.</param>
        /// <param name="state">The state to pass to the callback.</param>
        /// <returns>The <see cref="MemoryCacheEntryOptions"/> so that additional calls can be chained.</returns>
        public static MemoryCacheEntryOptions RegisterPostEvictionCallback(
            this MemoryCacheEntryOptions options,
            PostEvictionDelegate callback,
            object state)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            options.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration()
            {
                EvictionCallback = callback,
                State = state
            });
            return options;
        }
    }
}
