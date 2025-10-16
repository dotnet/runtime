// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Caching.Distributed
{
    /// <summary>
    /// Implements <see cref="IDistributedCache"/> using <see cref="IMemoryCache"/>.
    /// </summary>
    public class MemoryDistributedCache : IDistributedCache
    {
        private readonly MemoryCache _memCache;

        /// <summary>
        /// Creates a new <see cref="MemoryDistributedCache"/> instance.
        /// </summary>
        /// <param name="optionsAccessor">The options of the cache.</param>
        public MemoryDistributedCache(IOptions<MemoryDistributedCacheOptions> optionsAccessor)
            : this(optionsAccessor, NullLoggerFactory.Instance) { }

        /// <summary>
        /// Creates a new <see cref="MemoryDistributedCache"/> instance.
        /// </summary>
        /// <param name="optionsAccessor">The options of the cache.</param>
        /// <param name="loggerFactory">The logger factory to create <see cref="ILogger"/> used to log messages.</param>
        public MemoryDistributedCache(IOptions<MemoryDistributedCacheOptions> optionsAccessor, ILoggerFactory loggerFactory)
        {
            ArgumentNullException.ThrowIfNull(optionsAccessor);
            ArgumentNullException.ThrowIfNull(loggerFactory);

            _memCache = new MemoryCache(optionsAccessor.Value, loggerFactory);
        }

        /// <summary>
        /// Gets the specified item associated with a key from the <see cref="IMemoryCache"/> as a byte array.
        /// </summary>
        /// <param name="key">The key of the item to get.</param>
        /// <returns>The byte array value of the key.</returns>
        public byte[]? Get(string key)
        {
            ArgumentNullException.ThrowIfNull(key);

            _memCache.TryGetValue(key, out object? value);
            return (byte[]?)value;
        }

        /// <summary>
        /// Asynchronously gets the specified item associated with a key from the <see cref="IMemoryCache"/> as a byte array.
        /// </summary>
        /// <param name="key">The key of the item to get.</param>
        /// <param name="token">The <see cref="CancellationToken"/> to use to cancel operation.</param>
        /// <returns>The task for getting the byte array value associated with the specified key from the cache.</returns>
        public Task<byte[]?> GetAsync(string key, CancellationToken token = default(CancellationToken))
        {
            ArgumentNullException.ThrowIfNull(key);

            return Task.FromResult(Get(key));
        }

        /// <summary>
        /// Sets the specified item associated with a key in the <see cref="IMemoryCache"/> as a byte array.
        /// </summary>
        /// <param name="key">The key of the item to set.</param>
        /// <param name="value">The byte array value of the item to set.</param>
        /// <param name="options">The cache options for the item to set.</param>
        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(value);
            ArgumentNullException.ThrowIfNull(options);

            using ICacheEntry entry = _memCache.CreateEntry(key);
            entry.AbsoluteExpiration = options.AbsoluteExpiration;
            entry.AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow;
            entry.SlidingExpiration = options.SlidingExpiration;
            entry.Size = value.Length;
            entry.Value = value;
        }

        /// <summary>
        /// Asynchronously sets the specified item associated with a key in the <see cref="IMemoryCache"/> as a byte array.
        /// </summary>
        /// <param name="key">The key of the item to set.</param>
        /// <param name="value">The byte array value of the item to set.</param>
        /// <param name="options">The cache options for the item to set.</param>
        /// <param name="token">The <see cref="CancellationToken"/> to use to cancel operation.</param>
        /// <returns>The task for setting the byte array value associated with the specified key in the cache.</returns>
        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken))
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(value);
            ArgumentNullException.ThrowIfNull(options);

            Set(key, value, options);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Refreshes the specified item associated with a key from the <see cref="IMemoryCache"/>.
        /// </summary>
        /// <param name="key">The key of the item to refresh.</param>
        public void Refresh(string key)
        {
            ArgumentNullException.ThrowIfNull(key);

            _memCache.TryGetValue(key, out _);
        }

        /// <summary>
        /// Asynchronously refreshes the specified item associated with a key from the <see cref="IMemoryCache"/>.
        /// </summary>
        /// <param name="key">The key of the item to refresh.</param>
        /// <param name="token">The <see cref="CancellationToken"/> to use to cancel operation.</param>
        /// <returns>The task for refreshing the specified key in the cache.</returns>
        public Task RefreshAsync(string key, CancellationToken token = default(CancellationToken))
        {
            ArgumentNullException.ThrowIfNull(key);

            Refresh(key);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Removes the specified item associated with a key from the <see cref="IMemoryCache"/>.
        /// </summary>
        /// <param name="key">The key of the item to remove.</param>
        public void Remove(string key)
        {
            ArgumentNullException.ThrowIfNull(key);

            _memCache.Remove(key);
        }

        /// <summary>
        /// Asynchronously removes the specified item associated with a key from the <see cref="IMemoryCache"/>.
        /// </summary>
        /// <param name="key">The key of the item to remove.</param>
        /// <param name="token">The <see cref="CancellationToken"/> to use to cancel operation.</param>
        /// <returns>The task for removing the specified key from the cache.</returns>
        public Task RemoveAsync(string key, CancellationToken token = default(CancellationToken))
        {
            ArgumentNullException.ThrowIfNull(key);

            Remove(key);
            return Task.CompletedTask;
        }
    }
}
