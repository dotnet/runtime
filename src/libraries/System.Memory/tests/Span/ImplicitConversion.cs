// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.SpanTests
{
    public static partial class SpanTests
    {
        [Fact]
        public static void NullImplicitCast()
        {
            int[] dst = null;
            Span<int> srcSpan = dst;
            Assert.True(Span<int>.Empty == srcSpan);
        }

        [Fact]
        public static void ArraySegmentDefaultImplicitCast()
        {
            ArraySegment<int> dst = default;
            Span<int> srcSpan = dst;
            Assert.True(Span<int>.Empty == srcSpan);
        }
    }
}
