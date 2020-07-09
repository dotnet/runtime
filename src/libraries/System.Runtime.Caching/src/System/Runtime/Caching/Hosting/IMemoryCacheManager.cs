// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Runtime.Caching.Hosting
{
    public interface IMemoryCacheManager
    {
        void UpdateCacheSize(long size, MemoryCache cache);
        void ReleaseCache(MemoryCache cache);
    }
}
