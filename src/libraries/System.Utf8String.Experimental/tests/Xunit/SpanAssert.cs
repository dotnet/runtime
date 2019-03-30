// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Xunit
{
    public static class SpanAssert
    {
        public static void Equal<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b, IEqualityComparer<T> comparer = null) where T : IEquatable<T>
        {
            if (comparer is null)
            {
                Assert.Equal(a.ToArray(), b.ToArray());
            }
            else
            {
                Assert.Equal(a.ToArray(), b.ToArray(), comparer);
            }
        }

        public static void Equal<T>(Span<T> a, Span<T> b, IEqualityComparer<T> comparer = null) where T : IEquatable<T>
        {
            if (comparer is null)
            {
                Assert.Equal(a.ToArray(), b.ToArray());
            }
            else
            {
                Assert.Equal(a.ToArray(), b.ToArray(), comparer);
            }
        }
    }
}
