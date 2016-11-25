// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.CSharp.RuntimeBinder.Tests
{
    public class CSharpConvertBinderTests
    {
        private static readonly int[] SomeInt32 = {0, 1, 2, -1, int.MinValue, int.MaxValue, int.MaxValue - 1};

        private static readonly long[] SomeInt64 = {0L, 1L, 2L, -1L, long.MinValue, long.MaxValue, long.MaxValue - 1};

        private static IEnumerable<object[]> Int32Args() => SomeInt32.Select(i => new object[] {i});

        private static IEnumerable<object[]> Int64Arges() => SomeInt64.Select(i => new object[] {i});

        [Theory, MemberData(nameof(Int32Args))]
        public void ConvertImplicit(int x)
        {
            dynamic d = x;
            long xl = d;
            Assert.Equal(x, xl);
        }

        [Theory, MemberData(nameof(Int64Arges))]
        public void ConvertExplicit(long x)
        {
            dynamic d = x;
            int xi = (int)d;
            Assert.Equal((int)x, xi);
        }

        [Theory, MemberData(nameof(Int64Arges))]
        public void ConvertExplicitOvf(long x)
        {
            dynamic d = x;
            if (x < int.MinValue | x > int.MaxValue)
            {
                Assert.Throws<OverflowException>(() => checked((int)d));
            }
            else
            {
                Assert.Equal(x, checked((int)d));
            }
        }

        [Fact]
        public void ImpossibleConversion()
        {
            dynamic d = 42;
            Assert.Throws<RuntimeBinderException>(() => (bool)d);
        }
    }
}
