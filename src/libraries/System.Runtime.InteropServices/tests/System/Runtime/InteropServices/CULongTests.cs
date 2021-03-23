// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class CULongTests
    {
        private static bool Has64BitStorage => !Has32BitStorage;
        private static bool Has32BitStorage => PlatformDetection.Is32BitProcess || PlatformDetection.IsWindows;
        private static bool NativeIntConstructorCanOverflow => !PlatformDetection.Is32BitProcess && Has32BitStorage;
        private static bool NativeIntConstructorCannotOverflow => !NativeIntConstructorCanOverflow;

        [Fact]
        public void Ctor_Empty()
        {
            CULong value = new CULong();
            Assert.Equal(0u, value.Value);
        }

        [Fact]
        public void Ctor_UInt()
        {
            CULong value = new CULong(42u);
            Assert.Equal(42u, value.Value);
        }

        [Fact]
        public void Ctor_NUInt()
        {
            CULong value = new CULong((nuint)42);
            Assert.Equal(42u, value.Value);
        }

        [ConditionalFact(nameof(NativeIntConstructorCanOverflow))]
        public void Ctor_NUInt_OutOfRange()
        {
            Assert.Throws<OverflowException>(() => new CULong(unchecked(((nuint)uint.MaxValue) + 1)));
        }

        [ConditionalFact(nameof(NativeIntConstructorCannotOverflow))]
        public void Ctor_NUInt_LargeValue()
        {
            nuint largeValue = unchecked(((nuint)uint.MaxValue) + 1);
            CULong value = new CULong(largeValue);
            Assert.Equal(largeValue, value.Value);
        }

        public static IEnumerable<object[]> EqualsData()
        {
            yield return new object[] { new CULong(789), new CULong(789), true };
            yield return new object[] { new CULong(789), new CULong(0), false };
            yield return new object[] { new CULong(0), new CULong(0), true };
            yield return new object[] { new CULong(789), null, false };
            yield return new object[] { new CULong(789), "789", false };
            yield return new object[] { new CULong(789), 789u, false };
        }

        [Theory]
        [MemberData(nameof(EqualsData))]
        public void EqualsTest(CULong culong, object obj, bool expected)
        {
            if (obj is CULong culong2)
            {
                Assert.Equal(expected, culong.Equals(culong2));
                Assert.Equal(expected, culong.GetHashCode().Equals(culong2.GetHashCode()));
            }
            Assert.Equal(expected, culong.Equals(obj));
        }


        [Theory]
        [InlineData(0, "0")]
        [InlineData(4567, "4567")]
        [InlineData(uint.MaxValue, "4294967295")]
        public static void ToStringTest(uint value, string expected)
        {
            CULong culong = new CULong(value);

            Assert.Equal(expected, culong.ToString());
        }

        [Fact]
        public unsafe void Size()
        {
            int size = Has32BitStorage ? 4 : 8;
#pragma warning disable xUnit2000 // The value under test here is the sizeof expression
            Assert.Equal(size, sizeof(CULong));
#pragma warning restore xUnit2000
            Assert.Equal(size, Marshal.SizeOf<CULong>());
        }
    }
}
