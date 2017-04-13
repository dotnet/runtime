// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Runtime.CompilerServices;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void StringAsSpanNullary()
        {
            string s = "Hello";
            ReadOnlySpan<char> span = s.AsSpan();
            char[] expected = s.ToCharArray();
            span.Validate(expected);
        }

        [Fact]
        public static void StringAsSpanEmptyString()
        {
            string s = "";
            ReadOnlySpan<char> span = s.AsSpan();
            char[] expected = s.ToCharArray();
            span.Validate(expected);
        }

        [Fact]
        public static void StringAsSpanNullChecked()
        {
            string s = null;
            Assert.Throws<ArgumentNullException>(() => s.AsSpan().DontBox());
        }
    }
}
