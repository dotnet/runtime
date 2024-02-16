// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void CannotCallGetHashCodeOnSpan()
        {
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(new int[0]);

            try
            {
#pragma warning disable 0618
                int result = span.GetHashCode();
#pragma warning restore 0618
                Assert.Fail();
            }
            catch (Exception ex)
            {
                Assert.True(ex is NotSupportedException);
            }
        }
    }
}
