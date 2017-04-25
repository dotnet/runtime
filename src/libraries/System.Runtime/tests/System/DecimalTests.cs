// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.Tests
{
    public static partial class DecimalTests
    {
        [Fact]
        public static void MaxValue()
        {
            Assert.Equal(79228162514264337593543950335m, decimal.MaxValue);
        }

        [Fact]
        public static void MinusOne()
        {
            Assert.Equal(-1, decimal.MinusOne);
        }

        [Fact]
        public static void MinValue()
        {
            Assert.Equal(-79228162514264337593543950335m, decimal.MinValue);
        }

        [Fact]
        public static void One()
        {
            Assert.Equal(1, decimal.One);
        }

        [Fact]
        public static void Zero()
        {
            Assert.Equal(0, decimal.Zero);
        }

        [Fact]
        public static void Ctor_ULong()
        {
            Assert.Equal(ulong.MaxValue, new decimal(ulong.MaxValue));
        }

        [Fact]
        public static void Ctor_UInt()
        {
            Assert.Equal(uint.MaxValue, new decimal(uint.MaxValue));
        }

        [Fact]
        public static void Ctor_Float()
        {
            Assert.Equal((decimal)((float)123456789.123456), new decimal((float)123456789.123456));
        }

        [Fact]
        public static void Ctor_Long()
        {
            Assert.Equal(long.MaxValue, new decimal(long.MaxValue));
        }

        [Fact]
        public static void Ctor_IntArray()
        {
            var d1 = new decimal(new int[] { 1, 1, 1, 0 });
            decimal d2 = 3;
            d2 += uint.MaxValue;
            d2 += ulong.MaxValue;
            Assert.Equal(d2, d1);
        }

        [Fact]
        public static void Ctor_IntArray_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("bits", () => new decimal(null)); // Bits is null
            AssertExtensions.Throws<ArgumentException>(null, () => new decimal(new int[3])); // Bits.Length is not 4
            AssertExtensions.Throws<ArgumentException>(null, () => new decimal(new int[5])); // Bits.Length is not 4
        }

        [Fact]
        public static void Ctor_Int()
        {
            Assert.Equal(int.MaxValue, new decimal(int.MaxValue));
        }

        [Fact]
        public static void Ctor_Double()
        {
            Assert.Equal((decimal)123456789.123456, new decimal(123456789.123456));
        }

        [Fact]
        public static void Ctor_Int_Int_Int_Bool_Byte()
        {
            var d1 = new decimal(1, 1, 1, false, 0);
            decimal d2 = 3;
            d2 += uint.MaxValue;
            d2 += ulong.MaxValue;
            Assert.Equal(d2, d1);
        }

        public static IEnumerable<object[]> Add_Valid_TestData()
        {
            yield return new object[] { 1m, 1m, 2m };
            yield return new object[] { -1m, 1m, 0m };
            yield return new object[] { 1m, -1m, 0m };
            yield return new object[] { 1m, 0, 1m };
            yield return new object[] { 79228162514264337593543950330m, 5m, decimal.MaxValue };
            yield return new object[] { 79228162514264337593543950335m, -5m, 79228162514264337593543950330m };
            yield return new object[] { -79228162514264337593543950330m, 5m, -79228162514264337593543950325m };
            yield return new object[] { -79228162514264337593543950330m, -5m, decimal.MinValue };
            yield return new object[] { 1234.5678m, 0.00009m, 1234.56789m };
            yield return new object[] { -1234.5678m, 0.00009m, -1234.56771m };
            yield return new object[] { 0.1111111111111111111111111111m, 0.1111111111111111111111111111m, 0.2222222222222222222222222222m };
            yield return new object[] { 0.5555555555555555555555555555m, 0.5555555555555555555555555555m, 1.1111111111111111111111111110m };

            yield return new object[] { decimal.MinValue, decimal.Zero, decimal.MinValue };
            yield return new object[] { decimal.MaxValue, decimal.Zero, decimal.MaxValue };
            yield return new object[] { decimal.MinValue, decimal.Zero, decimal.MinValue };
        }

        [Theory]
        [MemberData(nameof(Add_Valid_TestData))]
        public static void Add(decimal d1, decimal d2, decimal expected)
        {
            Assert.Equal(expected, d1 + d2);
            Assert.Equal(expected, decimal.Add(d1, d2));

            decimal d3 = d1;
            d3 += d2;
            Assert.Equal(expected, d3);
        }

        public static IEnumerable<object[]> Add_Invalid_TestData()
        {
            yield return new object[] { decimal.MaxValue, decimal.MaxValue };
            yield return new object[] { 79228162514264337593543950330m, 6 };
            yield return new object[] { -79228162514264337593543950330m, -6 };
        }

        [Theory]
        [MemberData(nameof(Add_Invalid_TestData))]
        public static void Add_Invalid(decimal d1, decimal d2)
        {
            Assert.Throws<OverflowException>(() => d1 + d2);
            Assert.Throws<OverflowException>(() => decimal.Add(d1, d2));

            decimal d3 = d1;
            Assert.Throws<OverflowException>(() => d3 += d2);
        }

        public static IEnumerable<object[]> Ceiling_TestData()
        {
            yield return new object[] { 123m, 123m };
            yield return new object[] { 123.123m, 124m };
            yield return new object[] { 123.456m, 124m };
            yield return new object[] { -123.123m, -123m };
            yield return new object[] { -123.456m, -123m };
        }

        [Theory]
        [MemberData(nameof(Ceiling_TestData))]
        public static void Ceiling(decimal d, decimal expected)
        {
            Assert.Equal(expected, decimal.Ceiling(d));
        }

        public static IEnumerable<object[]> Compare_TestData()
        {
            yield return new object[] { 5m, 15m, -1 };
            yield return new object[] { 15m, 15m, 0 };
            yield return new object[] { 15m, 5m, 1 };

            yield return new object[] { 15m, null, 1 };

            yield return new object[] { decimal.Zero, decimal.Zero, 0 };
            yield return new object[] { decimal.Zero, decimal.One, -1 };
            yield return new object[] { decimal.One, decimal.Zero, 1 };
            yield return new object[] { decimal.MaxValue, decimal.MaxValue, 0 };
            yield return new object[] { decimal.MinValue, decimal.MinValue, 0 };
            yield return new object[] { decimal.MaxValue, decimal.MinValue, 1 };
            yield return new object[] { decimal.MinValue, decimal.MaxValue, -1 };
        }

        [Theory]
        [MemberData(nameof(Compare_TestData))]
        public static void Compare(decimal d1, object obj, int expected)
        {
            if (obj is decimal)
            {
                decimal d2 = (decimal)obj;
                Assert.Equal(expected, Math.Sign(d1.CompareTo(d2)));
                if (expected >= 0)
                {
                    Assert.True(d1 >= d2);
                    Assert.False(d1 < d2);
                }
                if (expected > 0)
                {
                    Assert.True(d1 > d2);
                    Assert.False(d1 <= d2);
                }
                if (expected <= 0)
                {
                    Assert.True(d1 <= d2);
                    Assert.False(d1 > d2);
                }
                if (expected < 0)
                {
                    Assert.True(d1 < d2);
                    Assert.False(d1 >= d2);
                }
                Assert.Equal(expected, Math.Sign(decimal.Compare(d1, d2)));
            }
            IComparable comparable = d1;
            Assert.Equal(expected, Math.Sign(comparable.CompareTo(obj)));
        }

        [Fact]
        public static void CompareTo_ObjectNotDecimal_ThrowsArgumentException()
        {
            IComparable comparable = 248m;
            AssertExtensions.Throws<ArgumentException>(null, () => comparable.CompareTo("248")); // Obj is not a decimal
            AssertExtensions.Throws<ArgumentException>(null, () => comparable.CompareTo(248)); // Obj is not a decimal
        }

        public static IEnumerable<object[]> Divide_Valid_TestData()
        {
            yield return new object[] { decimal.One, decimal.One, decimal.One };
            yield return new object[] { decimal.MinusOne, decimal.MinusOne, decimal.One };
            yield return new object[] { 15m, 2m, 7.5m };
            yield return new object[] { 10m, 2m, 5m };
            yield return new object[] { -10m, -2m, 5m };
            yield return new object[] { 10m, -2m, -5m };
            yield return new object[] { -10m, 2m, -5m };
            yield return new object[] { 0.9214206543486529434634231456m, decimal.MaxValue, decimal.Zero };
            yield return new object[] { 38214206543486529434634231456m, 0.49214206543486529434634231456m, 77648730371625094566866001277m };
            yield return new object[] { -78228162514264337593543950335m, decimal.MaxValue, -0.987378225516463811113412343m };

            yield return new object[] { decimal.MaxValue, decimal.MinusOne, decimal.MinValue };
            yield return new object[] { decimal.MinValue, decimal.MaxValue, decimal.MinusOne };
            yield return new object[] { decimal.MaxValue, decimal.MaxValue, decimal.One };
            yield return new object[] { decimal.MinValue, decimal.MinValue, decimal.One };

            // Tests near MaxValue
            yield return new object[] { 792281625142643375935439503.4m, 0.1m, 7922816251426433759354395034m };
            yield return new object[] { 79228162514264337593543950.34m, 0.1m, 792281625142643375935439503.4m };
            yield return new object[] { 7922816251426433759354395.034m, 0.1m, 79228162514264337593543950.34m };
            yield return new object[] { 792281625142643375935439.5034m, 0.1m, 7922816251426433759354395.034m };
            yield return new object[] { 79228162514264337593543950335m, 10m, 7922816251426433759354395033.5m };
            yield return new object[] { 79228162514264337567774146561m, 10m, 7922816251426433756777414656.1m };
            yield return new object[] { 79228162514264337567774146560m, 10m, 7922816251426433756777414656m };
            yield return new object[] { 79228162514264337567774146559m, 10m, 7922816251426433756777414655.9m };
            yield return new object[] { 79228162514264337593543950335m, 1.1m, 72025602285694852357767227577m };
            yield return new object[] { 79228162514264337593543950335m, 1.01m, 78443725261647859003508861718m };
            yield return new object[] { 79228162514264337593543950335m, 1.001m, 79149013500763574019524425909.091m };
            yield return new object[] { 79228162514264337593543950335m, 1.0001m, 79220240490215316061937756559.344m };
            yield return new object[] { 79228162514264337593543950335m, 1.00001m, 79227370240561931974224208092.919m };
            yield return new object[] { 79228162514264337593543950335m, 1.000001m, 79228083286181051412492537842.462m };
            yield return new object[] { 79228162514264337593543950335m, 1.0000001m, 79228154591448878448656105469.389m };
            yield return new object[] { 79228162514264337593543950335m, 1.00000001m, 79228161721982720373716746597.833m };
            yield return new object[] { 79228162514264337593543950335m, 1.000000001m, 79228162435036175158507775176.492m };
            yield return new object[] { 79228162514264337593543950335m, 1.0000000001m, 79228162506341521342909798200.709m };
            yield return new object[] { 79228162514264337593543950335m, 1.00000000001m, 79228162513472055968409229775.316m };
            yield return new object[] { 79228162514264337593543950335m, 1.000000000001m, 79228162514185109431029765225.569m };
            yield return new object[] { 79228162514264337593543950335m, 1.0000000000001m, 79228162514256414777292524693.522m };
            yield return new object[] { 79228162514264337593543950335m, 1.00000000000001m, 79228162514263545311918807699.547m };
            yield return new object[] { 79228162514264337593543950335m, 1.000000000000001m, 79228162514264258365381436070.742m };
            yield return new object[] { 79228162514264337593543950335m, 1.0000000000000001m, 79228162514264329670727698908.567m };
            yield return new object[] { 79228162514264337593543950335m, 1.00000000000000001m, 79228162514264336801262325192.357m };
            yield return new object[] { 79228162514264337593543950335m, 1.000000000000000001m, 79228162514264337514315787820.736m };
            yield return new object[] { 79228162514264337593543950335m, 1.0000000000000000001m, 79228162514264337585621134083.574m };
            yield return new object[] { 79228162514264337593543950335m, 1.00000000000000000001m, 79228162514264337592751668709.857m };
            yield return new object[] { 79228162514264337593543950335m, 1.000000000000000000001m, 79228162514264337593464722172.486m };
            yield return new object[] { 79228162514264337593543950335m, 1.0000000000000000000001m, 79228162514264337593536027518.749m };
            yield return new object[] { 79228162514264337593543950335m, 1.00000000000000000000001m, 79228162514264337593543158053.375m };
            yield return new object[] { 79228162514264337593543950335m, 1.000000000000000000000001m, 79228162514264337593543871106.837m };
            yield return new object[] { 79228162514264337593543950335m, 1.0000000000000000000000001m, 79228162514264337593543942412.184m };
            yield return new object[] { 79228162514264337593543950335m, 1.00000000000000000000000001m, 79228162514264337593543949542.718m };
            yield return new object[] { 79228162514264337593543950335m, 1.000000000000000000000000001m, 79228162514264337593543950255.772m };
            yield return new object[] { 7922816251426433759354395033.5m, 0.9999999999999999999999999999m, 7922816251426433759354395034m };
            yield return new object[] { 79228162514264337593543950335m, 10000000m, 7922816251426433759354.3950335m };
            yield return new object[] { 7922816251426433759354395033.5m, 1.000001m, 7922808328618105141249253784.2m };
            yield return new object[] { 7922816251426433759354395033.5m, 1.0000000000000000000000000001m, 7922816251426433759354395032.7m };
            yield return new object[] { 7922816251426433759354395033.5m, 1.0000000000000000000000000002m, 7922816251426433759354395031.9m };
            yield return new object[] { 7922816251426433759354395033.5m, 0.9999999999999999999999999999m, 7922816251426433759354395034m };
            yield return new object[] { 79228162514264337593543950335m, 1.0000000000000000000000000001m, 79228162514264337593543950327m };

            decimal boundary7 = new decimal((int)429u, (int)2133437386u, 0, false, 0);
            decimal boundary71 = new decimal((int)429u, (int)2133437387u, 0, false, 0);
            decimal maxValueBy7 = decimal.MaxValue * 0.0000001m;
            yield return new object[] { maxValueBy7, 1m, maxValueBy7 };
            yield return new object[] { maxValueBy7, 1m, maxValueBy7 };
            yield return new object[] { maxValueBy7, 0.0000001m, decimal.MaxValue };
            yield return new object[] { boundary7, 1m, boundary7 };
            yield return new object[] { boundary7, 0.000000100000000000000000001m, 91630438009337286849083695.62m };
            yield return new object[] { boundary71, 0.000000100000000000000000001m, 91630438052286959809083695.62m };
            yield return new object[] { 7922816251426433759354.3950335m, 1m, 7922816251426433759354.3950335m };
            yield return new object[] { 7922816251426433759354.3950335m, 0.0000001m, 79228162514264337593543950335m };
        }

        [Theory]
        [MemberData(nameof(Divide_Valid_TestData))]
        public static void Divide(decimal d1, decimal d2, decimal expected)
        {
            Assert.Equal(expected, d1 / d2);
            Assert.Equal(expected, decimal.Divide(d1, d2));
        }

        public static IEnumerable<object[]> Divide_Invalid_TestData()
        {
            yield return new object[] { decimal.One, decimal.Zero, typeof(DivideByZeroException) };
            yield return new object[] { decimal.Zero, decimal.Zero, typeof(DivideByZeroException) };
            yield return new object[] { 0.0m, 0.0m, typeof(DivideByZeroException) };

            yield return new object[] { 79228162514264337593543950335m, -0.9999999999999999999999999m, typeof(OverflowException) };
            yield return new object[] { 792281625142643.37593543950335m, -0.0000000000000079228162514264337593543950335m, typeof(OverflowException) };
            yield return new object[] { 79228162514264337593543950335m, 0.1m, typeof(OverflowException) };
            yield return new object[] { 7922816251426433759354395034m, 0.1m, typeof(OverflowException) };
            yield return new object[] { 79228162514264337593543950335m, 0.9m, typeof(OverflowException) };
            yield return new object[] { 79228162514264337593543950335m, 0.99m, typeof(OverflowException) };
            yield return new object[] { 79228162514264337593543950335m, 0.9999m, typeof(OverflowException) };
            yield return new object[] { 79228162514264337593543950335m, 0.99999m, typeof(OverflowException) };
            yield return new object[] { 79228162514264337593543950335m, 0.999999m, typeof(OverflowException) };
            yield return new object[] { 79228162514264337593543950335m, 0.9999999m, typeof(OverflowException) };
            yield return new object[] { 79228162514264337593543950335m, 0.99999999m, typeof(OverflowException) };
            yield return new object[] { 79228162514264337593543950335m, 0.999999999m, typeof(OverflowException) };
            yield return new object[] { 79228162514264337593543950335m, 0.9999999999m, typeof(OverflowException) };
            yield return new object[] { 79228162514264337593543950335m, 0.99999999999m, typeof(OverflowException) };
            yield return new object[] { 79228162514264337593543950335m, 0.999999999999m, typeof(OverflowException) };
            yield return new object[] { 79228162514264337593543950335m, 0.9999999999999m, typeof(OverflowException) };
            yield return new object[] { 79228162514264337593543950335m, 0.99999999999999m, typeof(OverflowException) };
            yield return new object[] { 79228162514264337593543950335m, 0.9999999999999999m, typeof(OverflowException) };
            yield return new object[] { 79228162514264337593543950335m, 0.99999999999999999m, typeof(OverflowException) };
            yield return new object[] { 79228162514264337593543950335m, 0.99999999999999999999m, typeof(OverflowException) };
            yield return new object[] { 79228162514264337593543950335m, 0.9999999999999999999999m, typeof(OverflowException) };
            yield return new object[] { 79228162514264337593543950335m, 0.99999999999999999999999m, typeof(OverflowException) };
            yield return new object[] { 79228162514264337593543950335m, 0.999999999999999999999999m, typeof(OverflowException) };
            yield return new object[] { 79228162514264337593543950335m, 0.9999999999999999999999999m, typeof(OverflowException) };
            yield return new object[] { 79228162514264337593543950335m, 0.999999999999999999999999999m, typeof(OverflowException) };
            yield return new object[] { 79228162514264337593543950335m, 0.9999999999999999999999999999m, typeof(OverflowException) };
            yield return new object[] { 79228162514264337593543950335m, -0.1m, typeof(OverflowException) };
            yield return new object[] { 79228162514264337593543950335m, -0.9999999999999999999999999m, typeof(OverflowException) };
            yield return new object[] { decimal.MaxValue / 2m, 0.5m, typeof(OverflowException) };
        }

        [Theory]
        [MemberData(nameof(Divide_Invalid_TestData))]
        public static void Divide_Invalid(decimal d1, decimal d2, Type exceptionType)
        {
            Assert.Throws(exceptionType, () => d1 / d2);
            Assert.Throws(exceptionType, () => decimal.Divide(d1, d2));
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { decimal.Zero, decimal.Zero, true };
            yield return new object[] { decimal.Zero, decimal.One, false };
            yield return new object[] { decimal.MaxValue, decimal.MaxValue, true };
            yield return new object[] { decimal.MinValue, decimal.MinValue, true };
            yield return new object[] { decimal.MaxValue, decimal.MinValue, false };

            yield return new object[] { decimal.One, null, false };
            yield return new object[] { decimal.One, 1, false };
            yield return new object[] { decimal.One, "one", false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public static void Equals(object obj1, object obj2, bool expected)
        {
            if (obj1 is decimal)
            {
                decimal d1 = (decimal)obj1;
                if (obj2 is decimal)
                {
                    decimal d2 = (decimal)obj2;
                    Assert.Equal(expected, d1.Equals(d2));
                    Assert.Equal(expected, decimal.Equals(d1, d2));
                    Assert.Equal(expected, d1 == d2);
                    Assert.Equal(!expected, d1 != d2);
                    Assert.Equal(expected, d1.GetHashCode().Equals(d2.GetHashCode()));

                    Assert.True(d1.Equals(d1));
                    Assert.True(d1 == d1);
                    Assert.False(d1 != d1);
                    Assert.Equal(d1.GetHashCode(), d1.GetHashCode());
                }
                Assert.Equal(expected, d1.Equals(obj2));
            }
            Assert.Equal(expected, Equals(obj1, obj2));
        }

        public static IEnumerable<object[]> Floor_TestData()
        {
            yield return new object[] { 123m, 123m };
            yield return new object[] { 123.123m, 123m };
            yield return new object[] { 123.456m, 123m };
            yield return new object[] { -123.123m, -124m };
            yield return new object[] { -123.456m, -124m };
        }

        [Theory]
        [MemberData(nameof(Floor_TestData))]
        public static void Floor(decimal d, decimal expected)
        {
            Assert.Equal(expected, decimal.Floor(d));
        }

        public static IEnumerable<object[]> GetBits_TestData()
        {
            yield return new object[] { 1M, new int[] { 0x00000001, 0x00000000, 0x00000000, 0x00000000 } };
            yield return new object[] { 100000000000000M, new int[] { 0x107A4000, 0x00005AF3, 0x00000000, 0x00000000 } };
            yield return new object[] { 100000000000000.00000000000000M, new int[] { 0x10000000, 0x3E250261, 0x204FCE5E, 0x000E0000 } };
            yield return new object[] { 1.0000000000000000000000000000M, new int[] { 0x10000000, 0x3E250261, 0x204FCE5E, 0x001C0000 } };
            yield return new object[] { 123456789M, new int[] { 0x075BCD15, 0x00000000, 0x00000000, 0x00000000 } };
            yield return new object[] { 0.123456789M, new int[] { 0x075BCD15, 0x00000000, 0x00000000, 0x00090000 } };
            yield return new object[] { 0.000000000123456789M, new int[] { 0x075BCD15, 0x00000000, 0x00000000, 0x00120000 } };
            yield return new object[] { 0.000000000000000000123456789M, new int[] { 0x075BCD15, 0x00000000, 0x00000000, 0x001B0000 } };
            yield return new object[] { 4294967295M, new int[] { unchecked((int)0xFFFFFFFF), 0x00000000, 0x00000000, 0x00000000 } };
            yield return new object[] { 18446744073709551615M, new int[] { unchecked((int)0xFFFFFFFF), unchecked((int)0xFFFFFFFF), 0x00000000, 0x00000000 } };
            yield return new object[] { decimal.MaxValue, new int[] { unchecked((int)0xFFFFFFFF), unchecked((int)0xFFFFFFFF), unchecked((int)0xFFFFFFFF), 0x00000000 } };
            yield return new object[] { decimal.MinValue, new int[] { unchecked((int)0xFFFFFFFF), unchecked((int)0xFFFFFFFF), unchecked((int)0xFFFFFFFF), unchecked((int)0x80000000) } };
            yield return new object[] { -7.9228162514264337593543950335M, new int[] { unchecked((int)0xFFFFFFFF), unchecked((int)0xFFFFFFFF), unchecked((int)0xFFFFFFFF), unchecked((int)0x801C0000) } };
        }

        [Theory]
        [MemberData(nameof(GetBits_TestData))]
        public static void GetBits(decimal input, int[] expected)
        {
            int[] bits = decimal.GetBits(input);

            Assert.Equal(expected, bits);

            bool sign = (bits[3] & 0x80000000) != 0;
            byte scale = (byte)((bits[3] >> 16) & 0x7F);
            decimal newValue = new decimal(bits[0], bits[1], bits[2], sign, scale);

            Assert.Equal(input, newValue);
        }

        public static IEnumerable<object[]> Multiply_Valid_TestData()
        {
            yield return new object[] { decimal.One, decimal.One, decimal.One };
            yield return new object[] { 7922816251426433759354395033.5m, new decimal(10), decimal.MaxValue };
            yield return new object[] { 0.2352523523423422342354395033m, 56033525474612414574574757495m, 13182018677937129120135020796m };
            yield return new object[] { 46161363632634613634.093453337m, 461613636.32634613634083453337m, 21308714924243214928823669051m };
            yield return new object[] { 0.0000000000000345435353453563m, .0000000000000023525235234234m, 0.0000000000000000000000000001m };

            // Near decimal.MaxValue
            yield return new object[] { 79228162514264337593543950335m, 0.9m, 71305346262837903834189555302m };
            yield return new object[] { 79228162514264337593543950335m, 0.99m, 78435880889121694217608510832m };
            yield return new object[] { 79228162514264337593543950335m, 0.9999999999999999999999999999m, 79228162514264337593543950327m };
            yield return new object[] { -79228162514264337593543950335m, 0.9m, -71305346262837903834189555302m };
            yield return new object[] { -79228162514264337593543950335m, 0.99m, -78435880889121694217608510832m };
            yield return new object[] { -79228162514264337593543950335m, 0.9999999999999999999999999999m, -79228162514264337593543950327m };
        }

        [Theory]
        [MemberData(nameof(Multiply_Valid_TestData))]
        public static void Multiply(decimal d1, decimal d2, decimal expected)
        {
            Assert.Equal(expected, d1 * d2);
            Assert.Equal(expected, decimal.Multiply(d1, d2));
        }

        public static IEnumerable<object[]> Multiply_Invalid_TestData()
        {
            yield return new object[] { decimal.MaxValue, decimal.MinValue };
            yield return new object[] { decimal.MinValue, 1.1m };
            yield return new object[] { 79228162514264337593543950335m, 1.1m };
            yield return new object[] { 79228162514264337593543950335m, 1.01m };
            yield return new object[] { 79228162514264337593543950335m, 1.001m };
            yield return new object[] { 79228162514264337593543950335m, 1.0001m };
            yield return new object[] { 79228162514264337593543950335m, 1.00001m };
            yield return new object[] { 79228162514264337593543950335m, 1.000001m };
            yield return new object[] { 79228162514264337593543950335m, 1.0000001m };
            yield return new object[] { 79228162514264337593543950335m, 1.00000001m };
            yield return new object[] { 79228162514264337593543950335m, 1.000000001m };
            yield return new object[] { 79228162514264337593543950335m, 1.0000000001m };
            yield return new object[] { 79228162514264337593543950335m, 1.00000000001m };
            yield return new object[] { 79228162514264337593543950335m, 1.000000000001m };
            yield return new object[] { 79228162514264337593543950335m, 1.0000000000001m };
            yield return new object[] { 79228162514264337593543950335m, 1.00000000000001m };
            yield return new object[] { 79228162514264337593543950335m, 1.000000000000001m };
            yield return new object[] { 79228162514264337593543950335m, 1.0000000000000001m };
            yield return new object[] { 79228162514264337593543950335m, 1.00000000000000001m };
            yield return new object[] { 79228162514264337593543950335m, 1.000000000000000001m };
            yield return new object[] { 79228162514264337593543950335m, 1.0000000000000000001m };
            yield return new object[] { 79228162514264337593543950335m, 1.00000000000000000001m };
            yield return new object[] { 79228162514264337593543950335m, 1.000000000000000000001m };
            yield return new object[] { 79228162514264337593543950335m, 1.0000000000000000000001m };
            yield return new object[] { 79228162514264337593543950335m, 1.00000000000000000000001m };
            yield return new object[] { 79228162514264337593543950335m, 1.000000000000000000000001m };
            yield return new object[] { 79228162514264337593543950335m, 1.0000000000000000000000001m };
            yield return new object[] { 79228162514264337593543950335m, 1.00000000000000000000000001m };
            yield return new object[] { 79228162514264337593543950335m, 1.000000000000000000000000001m };
            yield return new object[] { decimal.MaxValue / 2, 2m };
        }

        [Theory]
        [MemberData(nameof(Multiply_Invalid_TestData))]
        public static void Multiply_Invalid(decimal d1, decimal d2)
        {
            Assert.Throws<OverflowException>(() => d1 * d2);
            Assert.Throws<OverflowException>(() => decimal.Multiply(d1, d2));
        }

        public static IEnumerable<object[]> Negate_TestData()
        {
            yield return new object[] { 1m, -1m };
            yield return new object[] { 0m, 0m };
            yield return new object[] { -1m, 1m };
            yield return new object[] { decimal.MaxValue, decimal.MinValue };
            yield return new object[] { decimal.MinValue, decimal.MaxValue };
        }

        [Theory]
        [MemberData(nameof(Negate_TestData))]
        public static void Negate(decimal d, decimal expected)
        {
            Assert.Equal(expected, decimal.Negate(d));
        }

        public static IEnumerable<object[]> Parse_Valid_TestData()
        {
            NumberStyles defaultStyle = NumberStyles.Float;

            var emptyFormat = NumberFormatInfo.CurrentInfo;

            var customFormat1 = new NumberFormatInfo();
            customFormat1.CurrencySymbol = "$";
            customFormat1.CurrencyGroupSeparator = ",";

            var customFormat2 = new NumberFormatInfo();
            customFormat2.NumberDecimalSeparator = ".";

            var customFormat3 = new NumberFormatInfo();
            customFormat3.NumberGroupSeparator = ",";

            var customFormat4 = new NumberFormatInfo();
            customFormat4.NumberDecimalSeparator = ".";

            yield return new object[] { "-123", defaultStyle, null, -123m };
            yield return new object[] { "0", defaultStyle, null, 0m };
            yield return new object[] { "123", defaultStyle, null, 123m };
            yield return new object[] { "  123  ", defaultStyle, null, 123m };
            yield return new object[] { (567.89m).ToString(), defaultStyle, null, 567.89m };
            yield return new object[] { (-567.89m).ToString(), defaultStyle, null, -567.89m };

            yield return new object[] { "79228162514264337593543950335", defaultStyle, null, 79228162514264337593543950335m };
            yield return new object[] { "-79228162514264337593543950335", defaultStyle, null, -79228162514264337593543950335m };
            yield return new object[] { "79,228,162,514,264,337,593,543,950,335", NumberStyles.AllowThousands, customFormat3, 79228162514264337593543950335m };

            yield return new object[] { (123.1m).ToString(), NumberStyles.AllowDecimalPoint, null, 123.1m };
            yield return new object[] { 1000.ToString("N0"), NumberStyles.AllowThousands, null, 1000m };

            yield return new object[] { "123", NumberStyles.Any, emptyFormat, 123m };
            yield return new object[] { (123.567m).ToString(), NumberStyles.Any, emptyFormat, 123.567m };
            yield return new object[] { "123", NumberStyles.Float, emptyFormat, 123m };
            yield return new object[] { "$1000", NumberStyles.Currency, customFormat1, 1000m };
            yield return new object[] { "123.123", NumberStyles.Float, customFormat2, 123.123m };
            yield return new object[] { "(123)", NumberStyles.AllowParentheses, customFormat2, -123m };

            // Number buffer limit ran out (string too long)
            yield return new object[] { "1234567890123456789012345.678456", defaultStyle, customFormat4, 1234567890123456789012345.6785m };
        }

        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse(string value, NumberStyles style, IFormatProvider provider, decimal expected)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            decimal result;
            if ((style & ~NumberStyles.Integer) == 0 && style != NumberStyles.None)
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.True(decimal.TryParse(value, out result));
                    Assert.Equal(expected, result);

                    Assert.Equal(expected, decimal.Parse(value));
                }

                Assert.Equal(expected, decimal.Parse(value, provider));
            }

            // Use Parse(string, NumberStyles, IFormatProvider)
            Assert.True(decimal.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);

            Assert.Equal(expected, decimal.Parse(value, style, provider));

            if (isDefaultProvider)
            {
                // Use Parse(string, NumberStyles) or Parse(string, NumberStyles, IFormatProvider)
                Assert.True(decimal.TryParse(value, style, NumberFormatInfo.CurrentInfo, out result));
                Assert.Equal(expected, result);

                Assert.Equal(expected, decimal.Parse(value, style));
                Assert.Equal(expected, decimal.Parse(value, style, NumberFormatInfo.CurrentInfo));
            }
        }

        public static IEnumerable<object[]> Parse_Invalid_TestData()
        {
            NumberStyles defaultStyle = NumberStyles.Float;

            var customFormat = new NumberFormatInfo();
            customFormat.CurrencySymbol = "$";
            customFormat.NumberDecimalSeparator = ".";

            yield return new object[] { null, defaultStyle, null, typeof(ArgumentNullException) };
            yield return new object[] { "79228162514264337593543950336", defaultStyle, null, typeof(OverflowException) };
            yield return new object[] { "", defaultStyle, null, typeof(FormatException) };
            yield return new object[] { " ", defaultStyle, null, typeof(FormatException) };
            yield return new object[] { "Garbage", defaultStyle, null, typeof(FormatException) };

            yield return new object[] { "ab", defaultStyle, null, typeof(FormatException) }; // Hex value
            yield return new object[] { "(123)", defaultStyle, null, typeof(FormatException) }; // Parentheses
            yield return new object[] { 100.ToString("C0"), defaultStyle, null, typeof(FormatException) }; // Currency

            yield return new object[] { (123.456m).ToString(), NumberStyles.Integer, null, typeof(FormatException) }; // Decimal
            yield return new object[] { "  " + (123.456m).ToString(), NumberStyles.None, null, typeof(FormatException) }; // Leading space
            yield return new object[] { (123.456m).ToString() + "   ", NumberStyles.None, null, typeof(FormatException) }; // Leading space
            yield return new object[] { "1E23", NumberStyles.None, null, typeof(FormatException) }; // Exponent

            yield return new object[] { "ab", NumberStyles.None, null, typeof(FormatException) }; // Hex value
            yield return new object[] { "  123  ", NumberStyles.None, null, typeof(FormatException) }; // Trailing and leading whitespace
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            decimal result;
            if ((style & ~NumberStyles.Integer) == 0 && style != NumberStyles.None && (style & NumberStyles.AllowLeadingWhite) == (style & NumberStyles.AllowTrailingWhite))
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.False(decimal.TryParse(value, out result));
                    Assert.Equal(default(decimal), result);

                    Assert.Throws(exceptionType, () => decimal.Parse(value));
                }

                Assert.Throws(exceptionType, () => decimal.Parse(value, provider));
            }

            // Use Parse(string, NumberStyles, IFormatProvider)
            Assert.False(decimal.TryParse(value, style, provider, out result));
            Assert.Equal(default(decimal), result);

            Assert.Throws(exceptionType, () => decimal.Parse(value, style, provider));

            if (isDefaultProvider)
            {
                // Use Parse(string, NumberStyles) or Parse(string, NumberStyles, IFormatProvider)
                Assert.False(decimal.TryParse(value, style, NumberFormatInfo.CurrentInfo, out result));
                Assert.Equal(default(decimal), result);

                Assert.Throws(exceptionType, () => decimal.Parse(value, style));
                Assert.Throws(exceptionType, () => decimal.Parse(value, style, NumberFormatInfo.CurrentInfo));
            }
        }

        public static IEnumerable<object[]> Remainder_Valid_TestData()
        {
            decimal NegativeZero = new decimal(0, 0, 0, true, 0);
            yield return new object[] { 5m, 3m, 2m };
            yield return new object[] { 5m, -3m, 2m };
            yield return new object[] { -5m, 3m, -2m };
            yield return new object[] { -5m, -3m, -2m };
            yield return new object[] { 3m, 5m, 3m };
            yield return new object[] { 3m, -5m, 3m };
            yield return new object[] { -3m, 5m, -3m };
            yield return new object[] { -3m, -5m, -3m };
            yield return new object[] { 10m, -3m, 1m };
            yield return new object[] { -10m, 3m, -1m };
            yield return new object[] { -2.0m, 0.5m, -0.0m };
            yield return new object[] { 2.3m, 0.531m, 0.176m };
            yield return new object[] { 0.00123m, 3242m, 0.00123m };
            yield return new object[] { 3242m, 0.00123m, 0.00044m };
            yield return new object[] { 17.3m, 3m, 2.3m };
            yield return new object[] { 8.55m, 2.25m, 1.80m };
            yield return new object[] { 0.00m, 3m, 0.00m };
            yield return new object[] { NegativeZero, 2.2m, NegativeZero };

            // Max/Min
            yield return new object[] { decimal.MaxValue, decimal.MaxValue, 0m };
            yield return new object[] { decimal.MaxValue, decimal.MinValue, 0m };
            yield return new object[] { decimal.MaxValue, 1, 0m };
            yield return new object[] { decimal.MaxValue, 2394713m, 1494647m };
            yield return new object[] { decimal.MaxValue, -32768m, 32767m };
            yield return new object[] { -0.00m, decimal.MaxValue, -0.00m };
            yield return new object[] { 1.23984m, decimal.MaxValue, 1.23984m };
            yield return new object[] { 2398412.12983m, decimal.MaxValue, 2398412.12983m };
            yield return new object[] { -0.12938m, decimal.MaxValue, -0.12938m };

            yield return new object[] { decimal.MinValue, decimal.MinValue, NegativeZero };
            yield return new object[] { decimal.MinValue, decimal.MaxValue, NegativeZero };
            yield return new object[] { decimal.MinValue, 1, NegativeZero };
            yield return new object[] { decimal.MinValue, 2394713m, -1494647m };
            yield return new object[] { decimal.MinValue, -32768m, -32767m };
            yield return new object[] { 0.0m, decimal.MinValue, 0.0m };
            yield return new object[] { 1.23984m, decimal.MinValue, 1.23984m };
            yield return new object[] { 2398412.12983m, decimal.MinValue, 2398412.12983m };
            yield return new object[] { -0.12938m, decimal.MinValue, -0.12938m };

            yield return new object[] { 57675350989891243676868034225m, 7m, 5m };
            yield return new object[] { -57675350989891243676868034225m, 7m, -5m };
            yield return new object[] { 57675350989891243676868034225m, -7m, 5m };
            yield return new object[] { -57675350989891243676868034225m, -7m, -5m };

            yield return new object[] { 792281625142643375935439503.4m, 0.1m, 0.0m };
            yield return new object[] { 79228162514264337593543950.34m, 0.1m, 0.04m };
            yield return new object[] { 7922816251426433759354395.034m, 0.1m, 0.034m };
            yield return new object[] { 792281625142643375935439.5034m, 0.1m, 0.0034m };
            yield return new object[] { 79228162514264337593543950335m, 10m, 5m };
            yield return new object[] { 79228162514264337567774146561m, 10m, 1m };
            yield return new object[] { 79228162514264337567774146560m, 10m, 0m };
            yield return new object[] { 79228162514264337567774146559m, 10m, 9m };
        }

        [Theory]
        [MemberData(nameof(Remainder_Valid_TestData))]
        public static void Remainder(decimal d1, decimal d2, decimal expected)
        {
            Assert.Equal(expected, d1 % d2);
            Assert.Equal(expected, decimal.Remainder(d1, d2));
        }

        public static IEnumerable<object[]> Remainder_Invalid_TestData()
        {
            yield return new object[] { 5m, 0m, typeof(DivideByZeroException) };
            yield return new object[] { decimal.MaxValue, 0.1m, typeof(OverflowException) };
        }

        [Theory]
        [MemberData(nameof(Remainder_Invalid_TestData))]
        public static void Remainder_Invalid(decimal d1, decimal d2, Type exceptionType)
        {
            Assert.Throws(exceptionType, () => d1 % d2);
            Assert.Throws(exceptionType, () => decimal.Remainder(d1, d2));
        }

        public static IEnumerable<object[]> Subtract_Valid_TestData()
        {
            yield return new object[] { 1m, 1m, 0m };
            yield return new object[] { 1m, 0m, 1m };
            yield return new object[] { 0m, 1m, -1m };
            yield return new object[] { 1m, 1m, 0m };
            yield return new object[] { -1m, 1m, -2m };
            yield return new object[] { 1m, -1m, 2m };
            yield return new object[] { decimal.MaxValue, decimal.Zero, decimal.MaxValue };
            yield return new object[] { decimal.MinValue, decimal.Zero, decimal.MinValue };
            yield return new object[] { 79228162514264337593543950330m, -5, decimal.MaxValue };
            yield return new object[] { 79228162514264337593543950330m, 5, 79228162514264337593543950325m };
            yield return new object[] { -79228162514264337593543950330m, 5, decimal.MinValue };
            yield return new object[] { -79228162514264337593543950330m, -5, -79228162514264337593543950325m };
            yield return new object[] { 1234.5678m, 0.00009m, 1234.56771m };
            yield return new object[] { -1234.5678m, 0.00009m, -1234.56789m };
            yield return new object[] { 0.1111111111111111111111111111m, 0.1111111111111111111111111111m, 0 };
            yield return new object[] { 0.2222222222222222222222222222m, 0.1111111111111111111111111111m, 0.1111111111111111111111111111m };
            yield return new object[] { 1.1111111111111111111111111110m, 0.5555555555555555555555555555m, 0.5555555555555555555555555555m };
        }

        [Theory]
        [MemberData(nameof(Subtract_Valid_TestData))]
        public static void Subtract(decimal d1, decimal d2, decimal expected)
        {
            Assert.Equal(expected, d1 - d2);
            Assert.Equal(expected, decimal.Subtract(d1, d2));

            decimal d3 = d1;
            d3 -= d2;
            Assert.Equal(expected, d3);
        }

        public static IEnumerable<object[]> Subtract_Invalid_TestData()
        {
            yield return new object[] { 79228162514264337593543950330m, -6 };
            yield return new object[] { -79228162514264337593543950330m, 6 };
        }

        [Theory]
        [MemberData(nameof(Subtract_Invalid_TestData))]
        public static void Subtract_Invalid(decimal d1, decimal d2)
        {
            Assert.Throws<OverflowException>(() => decimal.Subtract(d1, d2));
            Assert.Throws<OverflowException>(() => d1 - d2);

            decimal d3 = d1;
            Assert.Throws<OverflowException>(() => d3 -= d2);
        }

        [Fact]
        public static void ToByte()
        {
            Assert.Equal(byte.MinValue, decimal.ToByte(byte.MinValue));
            Assert.Equal(123, decimal.ToByte(123));
            Assert.Equal(123, decimal.ToByte(123.123m));
            Assert.Equal(byte.MaxValue, decimal.ToByte(byte.MaxValue));
        }

        [Fact]
        public static void ToByte_Invalid()
        {
            Assert.Throws<OverflowException>(() => decimal.ToByte(byte.MinValue - 1)); // Decimal < byte.MinValue
            Assert.Throws<OverflowException>(() => decimal.ToByte(byte.MaxValue + 1)); // Decimal > byte.MaxValue
        }

        [Fact]
        public static void ToDouble()
        {
            double d = decimal.ToDouble(new decimal(0, 0, 1, false, 0));

            double dbl = 123456789.123456;
            Assert.Equal(dbl, decimal.ToDouble((decimal)dbl));
            Assert.Equal(-dbl, decimal.ToDouble((decimal)-dbl));

            dbl = 1e20;
            Assert.Equal(dbl, decimal.ToDouble((decimal)dbl));
            Assert.Equal(-dbl, decimal.ToDouble((decimal)-dbl));

            dbl = 1e27;
            Assert.Equal(dbl, decimal.ToDouble((decimal)dbl));
            Assert.Equal(-dbl, decimal.ToDouble((decimal)-dbl));

            dbl = long.MaxValue;
            // Need to pass in the Int64.MaxValue to ToDouble and not dbl because the conversion to double is a little lossy and we want precision
            Assert.Equal(dbl, decimal.ToDouble(long.MaxValue));
            Assert.Equal(-dbl, decimal.ToDouble(-long.MaxValue));
        }

        [Fact]
        public static void ToInt16()
        {
            Assert.Equal(short.MinValue, decimal.ToInt16(short.MinValue));
            Assert.Equal(-123, decimal.ToInt16(-123));
            Assert.Equal(0, decimal.ToInt16(0));
            Assert.Equal(123, decimal.ToInt16(123));
            Assert.Equal(123, decimal.ToInt16(123.123m));
            Assert.Equal(short.MaxValue, decimal.ToInt16(short.MaxValue));
        }

        [Fact]
        public static void ToInt16_Invalid()
        {
            Assert.Throws<OverflowException>(() => decimal.ToInt16(short.MinValue - 1)); // Decimal < short.MinValue
            Assert.Throws<OverflowException>(() => decimal.ToInt16(short.MaxValue + 1)); // Decimal > short.MaxValue

            Assert.Throws<OverflowException>(() => decimal.ToInt16((long)int.MinValue - 1)); // Decimal < short.MinValue
            Assert.Throws<OverflowException>(() => decimal.ToInt16((long)int.MaxValue + 1)); // Decimal > short.MaxValue
        }

        [Fact]
        public static void ToInt32()
        {
            Assert.Equal(int.MinValue, decimal.ToInt32(int.MinValue));
            Assert.Equal(-123, decimal.ToInt32(-123));
            Assert.Equal(0, decimal.ToInt32(0));
            Assert.Equal(123, decimal.ToInt32(123));
            Assert.Equal(123, decimal.ToInt32(123.123m));
            Assert.Equal(int.MaxValue, decimal.ToInt32(int.MaxValue));
        }

        [Fact]
        public static void ToInt32_Invalid()
        {
            Assert.Throws<OverflowException>(() => decimal.ToInt32((long)int.MinValue - 1)); // Decimal < int.MinValue
            Assert.Throws<OverflowException>(() => decimal.ToInt32((long)int.MaxValue + 1)); // Decimal > int.MaxValue
        }

        [Fact]
        public static void ToInt64()
        {
            Assert.Equal(long.MinValue, decimal.ToInt64(long.MinValue));
            Assert.Equal(-123, decimal.ToInt64(-123));
            Assert.Equal(0, decimal.ToInt64(0));
            Assert.Equal(123, decimal.ToInt64(123));
            Assert.Equal(123, decimal.ToInt64(123.123m));
            Assert.Equal(long.MaxValue, decimal.ToInt64(long.MaxValue));
        }

        [Fact]
        public static void ToInt64_Invalid()
        {
            Assert.Throws<OverflowException>(() => decimal.ToUInt64(decimal.MinValue)); // Decimal < long.MinValue
            Assert.Throws<OverflowException>(() => decimal.ToUInt64(decimal.MaxValue)); // Decimal > long.MaxValue
        }

        [Fact]
        public static void ToSByte()
        {
            Assert.Equal(sbyte.MinValue, decimal.ToSByte(sbyte.MinValue));
            Assert.Equal(-123, decimal.ToSByte(-123));
            Assert.Equal(0, decimal.ToSByte(0));
            Assert.Equal(123, decimal.ToSByte(123));
            Assert.Equal(123, decimal.ToSByte(123.123m));
            Assert.Equal(sbyte.MaxValue, decimal.ToSByte(sbyte.MaxValue));
        }

        [Fact]
        public static void ToSByte_Invalid()
        {
            Assert.Throws<OverflowException>(() => decimal.ToSByte(sbyte.MinValue - 1)); // Decimal < sbyte.MinValue
            Assert.Throws<OverflowException>(() => decimal.ToSByte(sbyte.MaxValue + 1)); // Decimal > sbyte.MaxValue

            Assert.Throws<OverflowException>(() => decimal.ToSByte((long)int.MinValue - 1)); // Decimal < sbyte.MinValue
            Assert.Throws<OverflowException>(() => decimal.ToSByte((long)int.MaxValue + 1)); // Decimal > sbyte.MaxValue
        }

        [Fact]
        public static void ToSingle()
        {
            float f = 12345.12f;
            Assert.Equal(f, decimal.ToSingle((decimal)f));
            Assert.Equal(-f, decimal.ToSingle((decimal)-f));

            f = 1e20f;
            Assert.Equal(f, decimal.ToSingle((decimal)f));
            Assert.Equal(-f, decimal.ToSingle((decimal)-f));

            f = 1e27f;
            Assert.Equal(f, decimal.ToSingle((decimal)f));
            Assert.Equal(-f, decimal.ToSingle((decimal)-f));
        }

        [Fact]
        public static void ToUInt16()
        {
            Assert.Equal(ushort.MinValue, decimal.ToUInt16(ushort.MinValue));
            Assert.Equal(123, decimal.ToByte(123));
            Assert.Equal(123, decimal.ToByte(123.123m));
            Assert.Equal(ushort.MaxValue, decimal.ToUInt16(ushort.MaxValue));
        }

        [Fact]
        public static void ToUInt16_Invalid()
        {
            Assert.Throws<OverflowException>(() => decimal.ToUInt16(ushort.MinValue - 1)); // Decimal < ushort.MinValue
            Assert.Throws<OverflowException>(() => decimal.ToUInt16(ushort.MaxValue + 1)); // Decimal > ushort.MaxValue
        }

        [Fact]
        public static void ToUInt32()
        {
            Assert.Equal(uint.MinValue, decimal.ToUInt32(uint.MinValue));
            Assert.Equal((uint)123, decimal.ToUInt32(123));
            Assert.Equal((uint)123, decimal.ToUInt32(123.123m));
            Assert.Equal(uint.MaxValue, decimal.ToUInt32(uint.MaxValue));
        }

        [Fact]
        public static void ToUInt32_Invalid()
        {
            Assert.Throws<OverflowException>(() => decimal.ToUInt32((long)uint.MinValue - 1)); // Decimal < uint.MinValue
            Assert.Throws<OverflowException>(() => decimal.ToUInt32((long)uint.MaxValue + 1)); // Decimal > uint.MaxValue
        }

        [Fact]
        public static void ToUInt64()
        {
            Assert.Equal(ulong.MinValue, decimal.ToUInt64(ulong.MinValue));
            Assert.Equal((ulong)123, decimal.ToUInt64(123));
            Assert.Equal((ulong)123, decimal.ToUInt64(123.123m));
            Assert.Equal(ulong.MaxValue, decimal.ToUInt64(ulong.MaxValue));
        }

        [Fact]
        public static void ToUInt64_Invalid()
        {
            Assert.Throws<OverflowException>(() => decimal.ToUInt64((long)ulong.MinValue - 1)); // Decimal < uint.MinValue
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            yield return new object[] { decimal.MinValue, "G", null, "-79228162514264337593543950335" };
            yield return new object[] { (decimal)-4567, "G", null, "-4567" };
            yield return new object[] { (decimal)-4567.89101, "G", null, "-4567.89101" };
            yield return new object[] { (decimal)0, "G", null, "0" };
            yield return new object[] { (decimal)4567, "G", null, "4567" };
            yield return new object[] { (decimal)4567.89101, "G", null, "4567.89101" };
            yield return new object[] { decimal.MaxValue, "G", null, "79228162514264337593543950335" };

            yield return new object[] { decimal.MinusOne, "G", null, "-1" };
            yield return new object[] { decimal.Zero, "G", null, "0" };
            yield return new object[] { decimal.One, "G", null, "1" };

            yield return new object[] { (decimal)2468, "N", null, "2,468.00" };

            // Changing the negative pattern doesn't do anything without also passing in a format string
            var customFormat1 = new NumberFormatInfo();
            customFormat1.NumberNegativePattern = 0;
            yield return new object[] { (decimal)-6310, "G", customFormat1, "-6310" };

            var customFormat2 = new NumberFormatInfo();
            customFormat2.NegativeSign = "#";
            customFormat2.NumberDecimalSeparator = "~";
            customFormat2.NumberGroupSeparator = "*";
            yield return new object[] { (decimal)-2468, "N", customFormat2, "#2*468~00" };
            yield return new object[] { (decimal)2468, "N", customFormat2, "2*468~00" };

            var customFormat3 = new NumberFormatInfo();
            customFormat3.NegativeSign = "xx"; // Set to trash to make sure it doesn't show up
            customFormat3.NumberGroupSeparator = "*";
            customFormat3.NumberNegativePattern = 0;
            yield return new object[] { (decimal)-2468, "N", customFormat3, "(2*468.00)" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void ToString(decimal f, string format, IFormatProvider provider, string expected)
        {
            Helpers.PerformActionWithCulture(CultureInfo.InvariantCulture, () =>
            {
                bool isDefaultProvider = provider == null;
                if (string.IsNullOrEmpty(format) || format.ToUpperInvariant() == "G")
                {
                    if (isDefaultProvider)
                    {
                        Assert.Equal(expected, f.ToString());
                        Assert.Equal(expected, f.ToString((IFormatProvider)null));
                    }
                    Assert.Equal(expected, f.ToString(provider));
                }
                if (isDefaultProvider)
                {
                    Assert.Equal(expected.Replace('e', 'E'), f.ToString(format.ToUpperInvariant())); // If format is upper case, then exponents are printed in upper case
                    Assert.Equal(expected.Replace('E', 'e'), f.ToString(format.ToLowerInvariant())); // If format is lower case, then exponents are printed in lower case
                    Assert.Equal(expected.Replace('e', 'E'), f.ToString(format.ToUpperInvariant(), null));
                    Assert.Equal(expected.Replace('E', 'e'), f.ToString(format.ToLowerInvariant(), null));
                }
                Assert.Equal(expected.Replace('e', 'E'), f.ToString(format.ToUpperInvariant(), provider));
                Assert.Equal(expected.Replace('E', 'e'), f.ToString(format.ToLowerInvariant(), provider));
            });
        }

        [Fact]
        public static void ToString_InvalidFormat_ThrowsFormatException()
        {
            decimal f = 123;
            Assert.Throws<FormatException>(() => f.ToString("Y")); // Invalid format
            Assert.Throws<FormatException>(() => f.ToString("Y", null)); // Invalid format
        }

        public static IEnumerable<object[]> Truncate_TestData()
        {
            yield return new object[] { 123m, 123m };
            yield return new object[] { 123.123m, 123m };
            yield return new object[] { 123.456m, 123m };
            yield return new object[] { -123.123m, -123m };
            yield return new object[] { -123.456m, -123m };
        }

        [Theory]
        [MemberData(nameof(Truncate_TestData))]
        public static void Truncate(decimal d, decimal expected)
        {
            Assert.Equal(expected, decimal.Truncate(d));
        }

        public static IEnumerable<object[]> Increment_TestData()
        {
            yield return new object[] { 1m, 2m };
            yield return new object[] { 0m, 1m };
            yield return new object[] { -1m, -0m };
            yield return new object[] { 12345m, 12346m };
            yield return new object[] { 12345.678m, 12346.678m };
            yield return new object[] { -12345.678m, -12344.678m };
        }

        [Theory]
        [MemberData(nameof(Increment_TestData))]
        public static void IncrementOperator(decimal d, decimal expected)
        {
            Assert.Equal(expected, ++d);
        }

        public static IEnumerable<object[]> Decrement_TestData()
        {
            yield return new object[] { 1m, 0m };
            yield return new object[] { 0m, -1m };
            yield return new object[] { -1m, -2m };
            yield return new object[] { 12345m, 12344m };
            yield return new object[] { 12345.678m, 12344.678m };
            yield return new object[] { -12345.678m, -12346.678m };
        }

        [Theory]
        [MemberData(nameof(Decrement_TestData))]
        public static void DecrementOperator(decimal d, decimal expected)
        {
            Assert.Equal(expected, --d);
        }
    }
}
