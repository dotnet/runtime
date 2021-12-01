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
    public class CLongTests
    {
        private static bool Has64BitStorage => !Has32BitStorage;
        private static bool Has32BitStorage => PlatformDetection.Is32BitProcess || PlatformDetection.IsWindows;
        private static bool NativeIntConstructorCanOverflow => !PlatformDetection.Is32BitProcess && Has32BitStorage;
        private static bool NativeIntConstructorCannotOverflow => !NativeIntConstructorCanOverflow;

        [Fact]
        public void Ctor_Empty()
        {
            CLong value = new CLong();
            Assert.Equal(0, value.Value);
        }

        [Fact]
        public void Ctor_Int()
        {
            CLong value = new CLong(42);
            Assert.Equal(42, value.Value);
        }

        [Fact]
        public void Ctor_NInt()
        {
            CLong value = new CLong((nint)42);
            Assert.Equal(42, value.Value);
        }

        [ConditionalFact(nameof(NativeIntConstructorCanOverflow))]
        public void Ctor_NInt_OutOfRange()
        {
            Assert.Throws<OverflowException>(() => new CLong(unchecked(((nint)int.MaxValue) + 1)));
        }

        [ConditionalFact(nameof(NativeIntConstructorCannotOverflow))]
        public void Ctor_NInt_LargeValue()
        {
            nint largeValue = unchecked(((nint)int.MaxValue) + 1);
            CLong value = new CLong(largeValue);
            Assert.Equal(largeValue, value.Value);
        }

        public static IEnumerable<object[]> EqualsData()
        {
            yield return new object[] { new CLong(789), new CLong(789), true };
            yield return new object[] { new CLong(789), new CLong(-789), false };
            yield return new object[] { new CLong(789), new CLong(0), false };
            yield return new object[] { new CLong(0), new CLong(0), true };
            yield return new object[] { new CLong(-789), new CLong(-789), true };
            yield return new object[] { new CLong(-789), new CLong(789), false };
            yield return new object[] { new CLong(789), null, false };
            yield return new object[] { new CLong(789), "789", false };
            yield return new object[] { new CLong(789), 789, false };
        }

        [Theory]
        [MemberData(nameof(EqualsData))]
        public void EqualsTest(CLong clong, object obj, bool expected)
        {
            if (obj is CLong clong2)
            {
                Assert.Equal(expected, clong.Equals(clong2));
                Assert.Equal(expected, clong.GetHashCode().Equals(clong2.GetHashCode()));
            }
            Assert.Equal(expected, clong.Equals(obj));
        }

        [Theory]
        [InlineData(int.MinValue, "-2147483648")]
        [InlineData(-4567, "-4567")]
        [InlineData(0, "0")]
        [InlineData(4567, "4567")]
        [InlineData(int.MaxValue, "2147483647")]
        public static void ToStringTest(int value, string expected)
        {
            CLong clong = new CLong(value);

            Assert.Equal(expected, clong.ToString());
        }

        [Fact]
        public unsafe void Size()
        {
            int size = Has32BitStorage ? 4 : 8;
#pragma warning disable xUnit2000 // The value under test here is the sizeof expression
            Assert.Equal(size, sizeof(CLong));
#pragma warning restore xUnit2000
            Assert.Equal(size, Marshal.SizeOf<CLong>());
        }
    }
}
