// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Cache
{
    public enum HttpCacheAgeControl
    {
        None = 0x0,
        MinFresh = 0x1,
        MaxAge = 0x2,
        MaxStale = 0x4,
        MaxAgeAndMinFresh = 0x3,
        MaxAgeAndMaxStale = 0x6
    }
}
