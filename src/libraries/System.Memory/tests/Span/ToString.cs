// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.SpanTests
{
    public static partial class SpanTests
    {
        [Fact]
        public static void ToString1()
        {
            int[] a = { 91, 92, 93 };
            Span<int> span = new Span<int>(a);
            Assert.Equal("System.Span<int>[3]", span.ToString());
        }

        [Fact]
        public static void ToString_Empty()
        {
            Span<int> span = new Span<int>();
            Assert.Equal("System.Span<int>[0]", span.ToString());
        }
    }
}
