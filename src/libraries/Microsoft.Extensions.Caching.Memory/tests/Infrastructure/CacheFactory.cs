// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.Extensions.Internal
{
    internal static class CacheFactory
    {
        internal static IMemoryCache CreateCache()
        {
            return CreateCache(new SystemClock());
        }

        internal static IMemoryCache CreateCache(ISystemClock clock)
        {
            return new MemoryCache(new MemoryCacheOptions()
            {
                Clock = clock,
            });
        }

        internal static IMemoryCache CreateCache(ISystemClock clock, TimeSpan expirationScanFrequency)
        {
            var options = new MemoryCacheOptions()
            {
                Clock = clock,
                ExpirationScanFrequency = expirationScanFrequency,
            };

            return new MemoryCache(options);
        }
    }
}
