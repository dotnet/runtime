// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.SpanTests
{
    public static partial class SpanTests
    {
        [Fact]
        public static void CtorRef()
        {
            int value = 1;
            var s = new Span<int>(ref value);

            Assert.Equal(1, s.Length);
            Assert.Equal(1, s[0]);

            s[0] = 2;
            Assert.Equal(2, value);

            value = 3;
            Assert.Equal(3, s[0]);
        }
    }
}
