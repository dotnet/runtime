// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Runtime.CompilerServices;

namespace System.SpanTests
{
    public static partial class SpanTests
    {
        [Fact]
        public static void Empty()
        {
            Span<int> empty = Span<int>.Empty;
            Assert.True(empty.IsEmpty);
            Assert.Equal(0, empty.Length);
            unsafe
            {
                ref int expected = ref Unsafe.AsRef<int>(null);
                ref int actual = ref empty.DangerousGetPinnableReference();
                Assert.True(Unsafe.AreSame<int>(ref expected, ref actual));
            }
        }
    }
}
