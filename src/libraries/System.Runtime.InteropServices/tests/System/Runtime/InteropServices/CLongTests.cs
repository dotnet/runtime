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
        private static bool Has64BitStorage => !PlatformDetection.Is32BitProcess && !PlatformDetection.IsWindows;
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

        [Theory]
        [InlineData(789, 789, true)]
        [InlineData(789, -789, false)]
        [InlineData(789, 0, false)]
        [InlineData(0, 0, true)]
        [InlineData(-789, -789, true)]
        [InlineData(-789, 789, false)]
        [InlineData(789, null, false)]
        [InlineData(789, "789", false)]
        [InlineData(789, (long)789, false)]
        public static void EqualsTest(int i1, object obj, bool expected)
        {
            if (obj is int i)
            {
                CLong i2 = new CLong(i);
                Assert.Equal(expected, new CLong(i1).Equals((object)i2));
                Assert.Equal(expected, new CLong(i1).Equals(i2));
                Assert.Equal(expected, new CLong(i1).GetHashCode().Equals(i2.GetHashCode()));
            }
            else
            {
                Assert.Equal(expected, new CLong(i1).Equals(obj));
            }
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
    }
}
