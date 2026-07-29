// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;

class Program
{
    // Mirrors the access pattern used by Microsoft.Extensions.Caching.Hybrid's DefaultHybridCache.
    // HybridCacheEntryOptions.ToDistributedCacheEntryOptions is internal and is only reached via
    // [UnsafeAccessor] from another assembly, which the trimmer cannot follow. It must be preserved
    // by the ILLink.Descriptors.xml in Microsoft.Extensions.Caching.Abstractions.
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ToDistributedCacheEntryOptions")]
    extern static DistributedCacheEntryOptions? ToDistributedCacheEntryOptions(HybridCacheEntryOptions options);

    static int Main()
    {
        TimeSpan expiration = TimeSpan.FromMinutes(5);
        HybridCacheEntryOptions options = new() { Expiration = expiration };

        // Throws MissingMethodException if the target method was trimmed away.
        DistributedCacheEntryOptions? result = ToDistributedCacheEntryOptions(options);

        if (result is null || result.AbsoluteExpirationRelativeToNow != expiration)
        {
            return -1;
        }

        return 100;
    }
}
