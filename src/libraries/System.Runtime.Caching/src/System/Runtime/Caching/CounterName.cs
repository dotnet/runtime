// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Runtime.Caching
{
    internal enum CounterName
    {
        Entries = 0,
        Hits,
        HitRatio,
        HitRatioBase,
        Misses,
        Trims,
        Turnover
    }
}
