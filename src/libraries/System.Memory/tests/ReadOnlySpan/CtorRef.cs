// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void CtorRef()
        {
            int value = 1;
            var s = new ReadOnlySpan<int>(in value);

            Assert.Equal(1, s.Length);
            Assert.Equal(1, s[0]);

            value = 2;
            Assert.Equal(2, s[0]);
        }
    }
}
