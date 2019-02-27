// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;

namespace System.Runtime.Caching
{
    internal class MemoryCacheEqualityComparer : IEqualityComparer
    {
        bool IEqualityComparer.Equals(object x, object y)
        {
            Debug.Assert(x != null && x is MemoryCacheKey);
            Debug.Assert(y != null && y is MemoryCacheKey);

            MemoryCacheKey a, b;
            a = (MemoryCacheKey)x;
            b = (MemoryCacheKey)y;

            return string.Equals(a.Key, b.Key, StringComparison.Ordinal);
        }

        int IEqualityComparer.GetHashCode(object obj)
        {
            MemoryCacheKey cacheKey = (MemoryCacheKey)obj;
            return cacheKey.Hash;
        }
    }
}
