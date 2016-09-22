// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Cache
{
    public enum HttpRequestCacheLevel
    {
        Default = 0,
        BypassCache = 1,
        CacheOnly = 2,
        CacheIfAvailable = 3,
        Revalidate = 4,
        Reload = 5,
        NoCacheNoStore = 6,
        CacheOrNextCacheOnly = 7,
        Refresh = 8
    }
}
