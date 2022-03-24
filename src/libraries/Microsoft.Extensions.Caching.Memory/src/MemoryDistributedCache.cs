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
    public class MemoryDistributedCache : IDistributedCache
    {
        private readonly IMemoryCache _memCache;

        public MemoryDistributedCache(IOptions<MemoryDistributedCacheOptions> optionsAccessor)
            : this(optionsAccessor, NullLoggerFactory.Instance) { }

        public MemoryDistributedCache(IOptions<MemoryDistributedCacheOptions> optionsAccessor!!, ILoggerFactory loggerFactory!!)
        {
            _memCache = new MemoryCache(optionsAccessor.Value, loggerFactory);
        }

        public byte[]? Get(string key!!)
        {
            return (byte[]?)_memCache.Get(key);
        }

        public Task<byte[]?> GetAsync(string key!!, CancellationToken token = default(CancellationToken))
        {
            return Task.FromResult(Get(key));
        }

        public void Set(string key!!, byte[] value!!, DistributedCacheEntryOptions options!!)
        {
            var memoryCacheEntryOptions = new MemoryCacheEntryOptions();
            memoryCacheEntryOptions.AbsoluteExpiration = options.AbsoluteExpiration;
            memoryCacheEntryOptions.AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow;
            memoryCacheEntryOptions.SlidingExpiration = options.SlidingExpiration;
            memoryCacheEntryOptions.Size = value.Length;

            _memCache.Set(key, value, memoryCacheEntryOptions);
        }

        public Task SetAsync(string key!!, byte[] value!!, DistributedCacheEntryOptions options!!, CancellationToken token = default(CancellationToken))
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }

        public void Refresh(string key!!)
        {
            _memCache.TryGetValue(key, out _);
        }

        public Task RefreshAsync(string key!!, CancellationToken token = default(CancellationToken))
        {
            Refresh(key);
            return Task.CompletedTask;
        }

        public void Remove(string key!!)
        {
            _memCache.Remove(key);
        }

        public Task RemoveAsync(string key!!, CancellationToken token = default(CancellationToken))
        {
            Remove(key);
            return Task.CompletedTask;
        }
    }
}
