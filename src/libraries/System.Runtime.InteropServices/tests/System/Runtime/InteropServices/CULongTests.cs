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
        private static bool Has64BitStorage => !PlatformDetection.Is32BitProcess && !PlatformDetection.IsWindows;
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

        [Theory]
        [InlineData(789, 789, true)]
        [InlineData(789, 0, false)]
        [InlineData(0, 0, true)]
        [InlineData(789, null, false)]
        [InlineData(789, "789", false)]
        [InlineData(789, (long)789, false)]
        public static void EqualsTest(uint i1, object obj, bool expected)
        {
            if (obj is uint i)
            {
                CULong i2 = new CULong(i);
                Assert.Equal(expected, new CULong(i1).Equals((object)i2));
                Assert.Equal(expected, new CULong(i1).Equals(i2));
                Assert.Equal(expected, new CULong(i1).GetHashCode().Equals(i2.GetHashCode()));
            }
            else
            {
                Assert.Equal(expected, new CULong(i1).Equals(obj));
            }
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
    }
}
