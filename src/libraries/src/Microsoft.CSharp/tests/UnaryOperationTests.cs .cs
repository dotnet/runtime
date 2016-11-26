// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.CSharp.RuntimeBinder.Tests
{
    public class CSharpUnaryOperationTests
    {
        private static readonly int[] SomeInt32 = { 0, 1, 2, -1, int.MinValue, int.MaxValue, int.MaxValue - 1 };

        private static IEnumerable<object[]> Int32Args() => SomeInt32.Select(i => new object[] {i});

        private static IEnumerable<object[]> BooleanArgs()
        {
            yield return new object[] {false};
            yield return new object[] {true};
        }

        [Theory, MemberData(nameof(Int32Args))]
        public void DecrementPrefixInt32(int x)
        {
            dynamic d = x;
            Assert.Equal(x - 1, --d);
            Assert.Equal(x - 1, d);
        }

        [Theory, MemberData(nameof(Int32Args))]
        public void DecrementPostfixInt32(int x)
        {
            dynamic d = x;
            Assert.Equal(x, d--);
            Assert.Equal(x - 1, d);
        }

        [Theory, MemberData(nameof(Int32Args))]
        public void DecrementPrefixOvfInt32(int x)
        {
            dynamic d = x;
            if (x == int.MinValue)
            {
                Assert.Throws<OverflowException>(() => checked(--d));
            }
            else
            {
                checked
                {
                    Assert.Equal(x - 1, --d);
                    Assert.Equal(x - 1, d);
                }
            }
        }

        [Theory, MemberData(nameof(Int32Args))]
        public void DecrementPostfixOvfInt32(int x)
        {
            dynamic d = x;
            if (x == int.MinValue)
            {
                Assert.Throws<OverflowException>(() => checked(d--));
            }
            else
            {
                checked
                {
                    Assert.Equal(x, d--);
                    Assert.Equal(x - 1, d);
                }
            }
        }

        [Theory, MemberData(nameof(Int32Args))]
        public void IncrementPrefixInt32(int x)
        {
            dynamic d = x;
            Assert.Equal(x + 1, ++d);
            Assert.Equal(x + 1, d);
        }

        [Theory, MemberData(nameof(Int32Args))]
        public void IncrementPostfixInt32(int x)
        {
            dynamic d = x;
            Assert.Equal(x, d++);
            Assert.Equal(x + 1, d);
        }

        [Theory, MemberData(nameof(Int32Args))]
        public void IncrementPrefixOvfInt32(int x)
        {
            dynamic d = x;
            if (x == int.MaxValue)
            {
                Assert.Throws<OverflowException>(() => checked(++d));
            }
            else
            {
                checked
                {
                    Assert.Equal(x + 1, ++d);
                    Assert.Equal(x + 1, d);
                }
            }
        }

        [Theory, MemberData(nameof(Int32Args))]
        public void IncrementPostfixOvfInt32(int x)
        {
            dynamic d = x;
            if (x == int.MaxValue)
            {
                Assert.Throws<OverflowException>(() => checked(d++));
            }
            else
            {
                checked
                {
                    Assert.Equal(x, d++);
                    Assert.Equal(x + 1, d);
                }
            }
        }

        [Theory, MemberData(nameof(Int32Args))]
        public void NegateInt32(int x)
        {
            dynamic d = x;
            Assert.Equal(-x, -d);
        }

        [Theory, MemberData(nameof(Int32Args))]
        public void NegateOvfInt32(int x)
        {
            dynamic d = x;
            if (x == int.MinValue)
            {
                Assert.Throws<OverflowException>(() => checked(-d));
            }
            else
            {
                Assert.Equal(-x, -d);
            }
        }

        [Theory, MemberData(nameof(Int32Args))]
        public void UnaryPlusInt32(int x)
        {
            dynamic d = x;
            Assert.Equal(x, +d);
        }

        [Theory, MemberData(nameof(Int32Args))]
        public void OnesComplementInt32(int x)
        {
            dynamic d = x;
            Assert.Equal(~x, ~d);
        }

        [Theory, MemberData(nameof(BooleanArgs))]
        public void NotBoolean(bool x)
        {
            dynamic d = x;
            Assert.Equal(!x, !d);
        }

        [Theory, MemberData(nameof(BooleanArgs))]
        public void IsTrueBoolean(bool x)
        {
            dynamic d = x;
            Assert.Equal(x ? 1 : 2, d ? 1 : 2);
        }

        [Theory, MemberData(nameof(BooleanArgs))]
        public void IsFalse(bool x)
        {
            dynamic d = x;
            Assert.Equal(x, d && true);
        }

        [Fact]
        public void InvalidOperationForType()
        {
            dynamic d = "23";
            Assert.Throws<RuntimeBinderException>(() => -d);
            d = 23;
            Assert.Throws<RuntimeBinderException>(() => !d);
        }
    }
}
