// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Caching.Memory
{
    /// <summary>
    /// Provide extensions methods for <see cref="IMemoryCache"/> operations.
    /// </summary>
    public static class CacheExtensions
    {
        /// <summary>
        /// Gets the value associated with this key if present.
        /// </summary>
        /// <param name="cache">The <see cref="IMemoryCache"/> instance this method extends.</param>
        /// <param name="key">The key of the value to get.</param>
        /// <returns>The value associated with this key, or <c>null</c> if the key is not present.</returns>
        public static object? Get(this IMemoryCache cache, object key)
        {
            cache.TryGetValue(key, out object? value);
            return value;
        }

        /// <summary>
        /// Gets the value associated with this key if present.
        /// </summary>
        /// <typeparam name="TItem">The type of the object to get.</typeparam>
        /// <param name="cache">The <see cref="IMemoryCache"/> instance this method extends.</param>
        /// <param name="key">The key of the value to get.</param>
        /// <returns>The value associated with this key, or <c>default(TItem)</c> if the key is not present.</returns>
        public static TItem? Get<TItem>(this IMemoryCache cache, object key)
        {
            return (TItem?)(cache.Get(key) ?? default(TItem));
        }

        /// <summary>
        /// Try to get the value associated with the given key.
        /// </summary>
        /// <typeparam name="TItem">The type of the object to get.</typeparam>
        /// <param name="cache">The <see cref="IMemoryCache"/> instance this method extends.</param>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">The value associated with the given key.</param>
        /// <returns><c>true</c> if the key was found. <c>false</c> otherwise.</returns>
        public static bool TryGetValue<TItem>(this IMemoryCache cache, object key, out TItem? value)
        {
            if (cache.TryGetValue(key, out object? result))
            {
                if (result == null)
                {
                    value = default;
                    return true;
                }

                if (result is TItem item)
                {
                    value = item;
                    return true;
                }
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Associate a value with a key in the <see cref="IMemoryCache"/>.
        /// </summary>
        /// <typeparam name="TItem">The type of the object to set.</typeparam>
        /// <param name="cache">The <see cref="IMemoryCache"/> instance this method extends.</param>
        /// <param name="key">The key of the entry to add.</param>
        /// <param name="value">The value to associate with the key.</param>
        /// <returns>The value that was set.</returns>
        public static TItem Set<TItem>(this IMemoryCache cache, object key, TItem value)
        {
            using ICacheEntry entry = cache.CreateEntry(key);
            entry.Value = value;

            return value;
        }

        /// <summary>
        /// Sets a cache entry with the given key and value that will expire in the given duration.
        /// </summary>
        /// <typeparam name="TItem">The type of the object to set.</typeparam>
        /// <param name="cache">The <see cref="IMemoryCache"/> instance this method extends.</param>
        /// <param name="key">The key of the entry to add.</param>
        /// <param name="value">The value to associate with the key.</param>
        /// <param name="absoluteExpiration">The point in time at which the cache entry will expire.</param>
        /// <returns>The value that was set.</returns>
        public static TItem Set<TItem>(this IMemoryCache cache, object key, TItem value, DateTimeOffset absoluteExpiration)
        {
            using ICacheEntry entry = cache.CreateEntry(key);
            entry.AbsoluteExpiration = absoluteExpiration;
            entry.Value = value;

            return value;
        }

        /// <summary>
        /// Sets a cache entry with the given key and value that will expire in the given duration from now.
        /// </summary>
        /// <typeparam name="TItem">The type of the object to set.</typeparam>
        /// <param name="cache">The <see cref="IMemoryCache"/> instance this method extends.</param>
        /// <param name="key">The key of the entry to add.</param>
        /// <param name="value">The value to associate with the key.</param>
        /// <param name="absoluteExpirationRelativeToNow">The duration from now after which the cache entry will expire.</param>
        /// <returns>The value that was set.</returns>
        public static TItem Set<TItem>(this IMemoryCache cache, object key, TItem value, TimeSpan absoluteExpirationRelativeToNow)
        {
            using ICacheEntry entry = cache.CreateEntry(key);
            entry.AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow;
            entry.Value = value;

            return value;
        }

        /// <summary>
        /// Sets a cache entry with the given key and value that will expire when <see cref="IChangeToken"/> expires.
        /// </summary>
        /// <typeparam name="TItem">The type of the object to set.</typeparam>
        /// <param name="cache">The <see cref="IMemoryCache"/> instance this method extends.</param>
        /// <param name="key">The key of the entry to add.</param>
        /// <param name="value">The value to associate with the key.</param>
        /// <param name="expirationToken">The <see cref="IChangeToken"/> that causes the cache entry to expire.</param>
        /// <returns>The value that was set.</returns>
        public static TItem Set<TItem>(this IMemoryCache cache, object key, TItem value, IChangeToken expirationToken)
        {
            using ICacheEntry entry = cache.CreateEntry(key);
            entry.AddExpirationToken(expirationToken);
            entry.Value = value;

            return value;
        }

        /// <summary>
        /// Sets a cache entry with the given key and value and apply the values of an existing <see cref="MemoryCacheEntryOptions"/> to the created entry.
        /// </summary>
        /// <typeparam name="TItem">The type of the object to set.</typeparam>
        /// <param name="cache">The <see cref="IMemoryCache"/> instance this method extends.</param>
        /// <param name="key">The key of the entry to add.</param>
        /// <param name="value">The value to associate with the key.</param>
        /// <param name="options">The existing <see cref="MemoryCacheEntryOptions"/> instance to apply to the new entry.</param>
        /// <returns>The value that was set.</returns>
        public static TItem Set<TItem>(this IMemoryCache cache, object key, TItem value, MemoryCacheEntryOptions? options)
        {
            using ICacheEntry entry = cache.CreateEntry(key);
            if (options != null)
            {
                entry.SetOptions(options);
            }

            entry.Value = value;

            return value;
        }

        /// <summary>
        /// Gets the value associated with this key if it exists, or generates a new entry using the provided key and a value from the given factory if the key is not found.
        /// </summary>
        /// <typeparam name="TItem">The type of the object to get.</typeparam>
        /// <param name="cache">The <see cref="IMemoryCache"/> instance this method extends.</param>
        /// <param name="key">The key of the entry to look for or create.</param>
        /// <param name="factory">The factory that creates the value associated with this key if the key does not exist in the cache.</param>
        /// <returns>The value associated with this key.</returns>
        public static TItem? GetOrCreate<TItem>(this IMemoryCache cache, object key, Func<ICacheEntry, TItem> factory)
        {
            return GetOrCreate(cache, key, factory, null);
        }

        /// <summary>
        /// Gets the value associated with this key if it exists, or generates a new entry using the provided key and a value from the given factory if the key is not found.
        /// </summary>
        /// <typeparam name="TItem">The type of the object to get.</typeparam>
        /// <param name="cache">The <see cref="IMemoryCache"/> instance this method extends.</param>
        /// <param name="key">The key of the entry to look for or create.</param>
        /// <param name="factory">The factory that creates the value associated with this key if the key does not exist in the cache.</param>
        /// <param name="createOptions">The options to be applied to the <see cref="ICacheEntry"/> if the key does not exist in the cache.</param>
        /// <returns>The value associated with this key.</returns>
        public static TItem? GetOrCreate<TItem>(this IMemoryCache cache, object key, Func<ICacheEntry, TItem> factory, MemoryCacheEntryOptions? createOptions)
        {
            if (!cache.TryGetValue(key, out object? result))
            {
                using ICacheEntry entry = cache.CreateEntry(key);

                if (createOptions != null)
                {
                    entry.SetOptions(createOptions);
                }

                result = factory(entry);
                entry.Value = result;
            }

            return (TItem?)result;
        }

        /// <summary>
        /// Asynchronously gets the value associated with this key if it exists, or generates a new entry using the provided key and a value from the given factory if the key is not found.
        /// </summary>
        /// <typeparam name="TItem">The type of the object to get.</typeparam>
        /// <param name="cache">The <see cref="IMemoryCache"/> instance this method extends.</param>
        /// <param name="key">The key of the entry to look for or create.</param>
        /// <param name="factory">The factory task that creates the value associated with this key if the key does not exist in the cache.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public static Task<TItem?> GetOrCreateAsync<TItem>(this IMemoryCache cache, object key, Func<ICacheEntry, Task<TItem>> factory)
        {
            return GetOrCreateAsync<TItem>(cache, key, factory, null);
        }

        /// <summary>
        /// Asynchronously gets the value associated with this key if it exists, or generates a new entry using the provided key and a value from the given factory if the key is not found.
        /// </summary>
        /// <typeparam name="TItem">The type of the object to get.</typeparam>
        /// <param name="cache">The <see cref="IMemoryCache"/> instance this method extends.</param>
        /// <param name="key">The key of the entry to look for or create.</param>
        /// <param name="factory">The factory task that creates the value associated with this key if the key does not exist in the cache.</param>
        /// <param name="createOptions">The options to be applied to the <see cref="ICacheEntry"/> if the key does not exist in the cache.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public static async Task<TItem?> GetOrCreateAsync<TItem>(this IMemoryCache cache, object key, Func<ICacheEntry, Task<TItem>> factory, MemoryCacheEntryOptions? createOptions)
        {
            if (!cache.TryGetValue(key, out object? result))
            {
                using ICacheEntry entry = cache.CreateEntry(key);

                if (createOptions != null)
                {
                    entry.SetOptions(createOptions);
                }

                result = await factory(entry).ConfigureAwait(false);
                entry.Value = result;
            }

            return (TItem?)result;
        }
    }
}
