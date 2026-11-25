// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;

namespace System.Runtime.Caching
{
    internal sealed class MemoryCacheEqualityComparer : IEqualityComparer
    {
        bool IEqualityComparer.Equals(object? x, object? y)
        {
            if (x is MemoryCacheKey a && y is MemoryCacheKey b)
            {
                return string.Equals(a.Key, b.Key, StringComparison.Ordinal);
            }

            Debug.Fail("Unexpected types passed to MemoryCacheEqualityComparer");
            return false;
        }

        int IEqualityComparer.GetHashCode(object obj)
        {
            MemoryCacheKey cacheKey = (MemoryCacheKey)obj;
            return cacheKey.Hash;
        }
    }
}
