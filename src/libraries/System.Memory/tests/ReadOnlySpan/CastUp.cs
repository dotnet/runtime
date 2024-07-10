// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void CastUp()
        {
            ReadOnlySpan<string> strings = new string[] { "Hello", "World" };
            ReadOnlySpan<object> span = ReadOnlySpan<object>.CastUp(strings);
            span.ValidateReferenceType("Hello", "World");
        }
    }
}
