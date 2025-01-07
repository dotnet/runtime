// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Xunit;

namespace System.Tests
{
    public class ConvertToStringTests
    {
        [Fact]
        public static void FromBoxedObject()
        {
            object[] testValues =
            {
            // Boolean
            true,
            false,

            // Byte
            byte.MinValue,
            (byte)100,
            byte.MaxValue,

            // Decimal
            decimal.Zero,
            decimal.One,
            decimal.MinusOne,
            decimal.MaxValue,
            decimal.MinValue,
            1.234567890123456789012345678m,
            1234.56m,
            -1234.56m,

            // Double
            -12.2364,
            -1.7753E-83,
            +12.345e+234,
            +12e+1,
            double.NegativeInfinity,
            double.PositiveInfinity,
            double.NaN,

            // Int16
            short.MinValue,
            0,
            short.MaxValue,

            // Int32
            int.MinValue,
            0,
            int.MaxValue,

            // Int64
            long.MinValue,
            (long)0,
            long.MaxValue,

            // SByte
            sbyte.MinValue,
            (sbyte)0,
            sbyte.MaxValue,

            // Single
            -12.2364f,
            (float)+12.345e+234,
            +12e+1f,
            float.NegativeInfinity,
            float.PositiveInfinity,
            float.NaN,

            // TimeSpan
            TimeSpan.Zero,
            TimeSpan.Parse("1999.9:09:09"),
            TimeSpan.Parse("-1111.1:11:11"),
            TimeSpan.Parse("1:23:45"),
            TimeSpan.Parse("-2:34:56"),

            // UInt16
            ushort.MinValue,
            (ushort)100,
            ushort.MaxValue,

            // UInt32
            uint.MinValue,
            (uint)100,
            uint.MaxValue,

            // UInt64
            ulong.MinValue,
            (ulong)100,
            ulong.MaxValue
        };

            string[] expectedValues =
            {
            // Boolean
            "True",
            "False",

            // Byte
            "0",
            "100",
            "255",

            // Decimal
            "0",
            "1",
            "-1",
            "79228162514264337593543950335",
            "-79228162514264337593543950335",
            "1.234567890123456789012345678",
            "1234.56",
            "-1234.56",

            // Double
            "-12.2364",
            "-1.7753E-83",
            "1.2345E+235",
            "120",
            "-Infinity",
            "Infinity",
            "NaN",

            // Int16
            "-32768",
            "0",
            "32767",

            // Int32
            "-2147483648",
            "0",
            "2147483647",

            // Int64
            "-9223372036854775808",
            "0",
            "9223372036854775807",

            // SByte
            "-128",
            "0",
            "127",

            // Single
            "-12.2364",
            "Infinity",
            "120",
            "-Infinity",
            "Infinity",
            "NaN",

            // TimeSpan
            "00:00:00",
            "1999.09:09:09",
            "-1111.01:11:11",
            "01:23:45",
            "-02:34:56",

            // UInt16
            "0",
            "100",
            "65535",

            // UInt32
            "0",
            "100",
            "4294967295",

            // UInt64
            "0",
            "100",
            "18446744073709551615",
        };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], NumberFormatInfo.InvariantInfo));
            }
        }

        [Fact]
        public static void FromBoxedObject_NotNetFramework()
        {
            object[] testValues =
            {
                // Double
                -12.236465923406483,

                // Single
                -1.7753e-83f,
                -12.2364659234064826243f,
            };

            string[] expectedValues =
            {
                // Double
                "-12.236465923406483",

                // Single
                "-0",
                "-12.236465",
            };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], NumberFormatInfo.InvariantInfo));
            }
        }

        [Fact]
        public static void FromObject()
        {
            Assert.Equal("System.Tests.ConvertToStringTests", Convert.ToString(new ConvertToStringTests()));
        }

        [Fact]
        public static void FromDateTime()
        {
            DateTime[] testValues = { new DateTime(2000, 8, 15, 16, 59, 59), new DateTime(1, 1, 1, 1, 1, 1) };
            string[] expectedValues = { "08/15/2000 16:59:59", "01/01/0001 01:01:01" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(testValues[i].ToString(), Convert.ToString(testValues[i]));
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], DateTimeFormatInfo.InvariantInfo));
            }
        }

        [Fact]
        public static void FromChar()
        {
            char[] testValues = { 'a', 'A', '@', '\n' };
            string[] expectedValues = { "a", "A", "@", "\n" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i]));
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], CultureInfo.InvariantCulture));
            }
        }

        private static void Verify<TInput>(Func<TInput, string> convert, Func<TInput, IFormatProvider, string> convertWithFormatProvider, TInput[] testValues, string[] expectedValues, IFormatProvider formatProvider = null)
        {
            Assert.Equal(expectedValues.Length, testValues.Length);

            if (formatProvider == null)
            {
                formatProvider = CultureInfo.InvariantCulture;
            }

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], convert(testValues[i]));
                Assert.Equal(expectedValues[i], convertWithFormatProvider(testValues[i], formatProvider));
            }
        }

        [Fact]
        public static void FromByteBase2()
        {
            byte[] testValues = { byte.MinValue, 100, byte.MaxValue };
            string[] expectedValues = { "0", "1100100", "11111111" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], 2));
            }
        }

        [Fact]
        public static void FromByteBase8()
        {
            byte[] testValues = { byte.MinValue, 100, byte.MaxValue };
            string[] expectedValues = { "0", "144", "377" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], 8));
            }
        }

        [Fact]
        public static void FromByteBase10()
        {
            byte[] testValues = { byte.MinValue, 100, byte.MaxValue };
            string[] expectedValues = { "0", "100", "255" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], 10));
            }
        }

        [Fact]
        public static void FromByteBase16()
        {
            byte[] testValues = { byte.MinValue, 100, byte.MaxValue };
            string[] expectedValues = { "0", "64", "ff" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], 16));
            }
        }

        [Fact]
        public static void FromByteInvalidBase()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Convert.ToString(byte.MaxValue, 13));
        }

        [Fact]
        public static void FromInt16Base2()
        {
            short[] testValues = { short.MinValue, 0, short.MaxValue };
            string[] expectedValues = { "1000000000000000", "0", "111111111111111" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], 2));
            }
        }

        [Fact]
        public static void FromInt16Base8()
        {
            short[] testValues = { short.MinValue, 0, short.MaxValue };
            string[] expectedValues = { "100000", "0", "77777" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], 8));
            }
        }

        [Fact]
        public static void FromInt16Base10()
        {
            short[] testValues = { short.MinValue, 0, short.MaxValue };
            string[] expectedValues = { "-32768", "0", "32767" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], 10));
            }
        }

        [Fact]
        public static void FromInt16Base16()
        {
            short[] testValues = { short.MinValue, 0, short.MaxValue };
            string[] expectedValues = { "8000", "0", "7fff" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], 16));
            }
        }

        [Fact]
        public static void FromInt16InvalidBase()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Convert.ToString(short.MaxValue, 0));
        }

        [Fact]
        public static void FromInt32Base2()
        {
            int[] testValues = { int.MinValue, 0, int.MaxValue };
            string[] expectedValues = { "10000000000000000000000000000000", "0", "1111111111111111111111111111111" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], 2));
            }
        }

        [Fact]
        public static void FromInt32Base8()
        {
            int[] testValues = { int.MinValue, 0, int.MaxValue };
            string[] expectedValues = { "20000000000", "0", "17777777777" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], 8));
            }
        }

        [Fact]
        public static void FromInt32Base10()
        {
            int[] testValues = { int.MinValue, 0, int.MaxValue };
            string[] expectedValues = { "-2147483648", "0", "2147483647" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], 10));
            }
        }

        [Fact]
        public static void FromInt32Base16()
        {
            int[] testValues = { int.MinValue, 0, int.MaxValue };
            string[] expectedValues = { "80000000", "0", "7fffffff" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], 16));
            }
        }

        [Fact]
        public static void FromInt32InvalidBase()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Convert.ToString(int.MaxValue, 9));
        }

        [Fact]
        public static void FromInt64Base2()
        {
            long[] testValues = { long.MinValue, 0, long.MaxValue };
            string[] expectedValues = { "1000000000000000000000000000000000000000000000000000000000000000", "0", "111111111111111111111111111111111111111111111111111111111111111" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], 2));
            }
        }

        [Fact]
        public static void FromInt64Base8()
        {
            long[] testValues = { long.MinValue, 0, long.MaxValue };
            string[] expectedValues = { "1000000000000000000000", "0", "777777777777777777777" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], 8));
            }
        }

        [Fact]
        public static void FromInt64Base10()
        {
            long[] testValues = { long.MinValue, 0, long.MaxValue };
            string[] expectedValues = { "-9223372036854775808", "0", "9223372036854775807" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], 10));
            }
        }

        [Fact]
        public static void FromInt64Base16()
        {
            long[] testValues = { long.MinValue, 0, long.MaxValue };
            string[] expectedValues = { "8000000000000000", "0", "7fffffffffffffff" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], 16));
            }
        }

        [Fact]
        public static void FromInt64InvalidBase()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Convert.ToString(long.MaxValue, 1));
        }

        [Fact]
        public static void FromBoolean()
        {
            bool[] testValues = new[] { true, false };

            for (int i = 0; i < testValues.Length; i++)
            {
                string expected = testValues[i].ToString();
                string actual = Convert.ToString(testValues[i]);
                Assert.Equal(expected, actual);
                actual = Convert.ToString(testValues[i], NumberFormatInfo.CurrentInfo);
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public static void FromSByte()
        {
            sbyte[] testValues = new sbyte[] { sbyte.MinValue, -1, 0, 1, sbyte.MaxValue };

            for (int i = 0; i < testValues.Length; i++)
            {
                string result = Convert.ToString(testValues[i]);
                Assert.Equal(testValues[i].ToString(), result);
                result = Convert.ToString(testValues[i], NumberFormatInfo.CurrentInfo);
                Assert.Equal(testValues[i].ToString(NumberFormatInfo.CurrentInfo), result);
            }
        }

        [Fact]
        public static void FromByte()
        {
            byte[] testValues = new byte[] { byte.MinValue, 0, 1, 100, byte.MaxValue };

            for (int i = 0; i < testValues.Length; i++)
            {
                string result = Convert.ToString(testValues[i]);
                Assert.Equal(testValues[i].ToString(), result);
                result = Convert.ToString(testValues[i], NumberFormatInfo.CurrentInfo);
                Assert.Equal(testValues[i].ToString(NumberFormatInfo.CurrentInfo), result);
            }
        }

        [Fact]
        public static void FromInt16Array()
        {
            short[] testValues = new short[] { short.MinValue, -1000, -1, 0, 1, 1000, short.MaxValue };

            for (int i = 0; i < testValues.Length; i++)
            {
                string result = Convert.ToString(testValues[i]);
                Assert.Equal(testValues[i].ToString(), result);
                result = Convert.ToString(testValues[i], NumberFormatInfo.CurrentInfo);
                Assert.Equal(testValues[i].ToString(NumberFormatInfo.CurrentInfo), result);
            }
        }

        [Fact]
        public static void FromUInt16Array()
        {
            ushort[] testValues = new ushort[] { ushort.MinValue, 0, 1, 1000, ushort.MaxValue };

            for (int i = 0; i < testValues.Length; i++)
            {
                string result = Convert.ToString(testValues[i]);
                Assert.Equal(testValues[i].ToString(), result);
                result = Convert.ToString(testValues[i], NumberFormatInfo.CurrentInfo);
                Assert.Equal(testValues[i].ToString(NumberFormatInfo.CurrentInfo), result);
            }
        }

        [Fact]
        public static void FromInt32Array()
        {
            int[] testValues = new int[] { int.MinValue, -1000, -1, 0, 1, 1000, int.MaxValue };

            for (int i = 0; i < testValues.Length; i++)
            {
                string result = Convert.ToString(testValues[i]);
                Assert.Equal(testValues[i].ToString(), result);
                result = Convert.ToString(testValues[i], NumberFormatInfo.CurrentInfo);
                Assert.Equal(testValues[i].ToString(NumberFormatInfo.CurrentInfo), result);
            }
        }

        [Fact]
        public static void FromUInt32Array()
        {
            uint[] testValues = new uint[] { uint.MinValue, 0, 1, 1000, uint.MaxValue };

            for (int i = 0; i < testValues.Length; i++)
            {
                string result = Convert.ToString(testValues[i]);
                Assert.Equal(testValues[i].ToString(), result);
                result = Convert.ToString(testValues[i], NumberFormatInfo.CurrentInfo);
                Assert.Equal(testValues[i].ToString(NumberFormatInfo.CurrentInfo), result);
            }
        }

        [Fact]
        public static void FromInt64Array()
        {
            long[] testValues = new long[] { long.MinValue, -1000, -1, 0, 1, 1000, long.MaxValue };

            for (int i = 0; i < testValues.Length; i++)
            {
                string result = Convert.ToString(testValues[i]);
                Assert.Equal(testValues[i].ToString(), result);
                result = Convert.ToString(testValues[i], NumberFormatInfo.CurrentInfo);
                Assert.Equal(testValues[i].ToString(NumberFormatInfo.CurrentInfo), result);
            }
        }

        [Fact]
        public static void FromUInt64Array()
        {
            ulong[] testValues = new ulong[] { ulong.MinValue, 0, 1, 1000, ulong.MaxValue };

            for (int i = 0; i < testValues.Length; i++)
            {
                string result = Convert.ToString(testValues[i]);
                Assert.Equal(testValues[i].ToString(), result);
                result = Convert.ToString(testValues[i], NumberFormatInfo.CurrentInfo);
                Assert.Equal(testValues[i].ToString(NumberFormatInfo.CurrentInfo), result);
            }
        }

        [Fact]
        public static void FromSingleArray()
        {
            float[] testValues = new float[] { float.MinValue, 0.0f, 1.0f, 1000.0f, float.MaxValue, float.NegativeInfinity, float.PositiveInfinity, float.Epsilon, float.NaN };

            for (int i = 0; i < testValues.Length; i++)
            {
                string result = Convert.ToString(testValues[i]);
                Assert.Equal(testValues[i].ToString(), result);
                result = Convert.ToString(testValues[i], NumberFormatInfo.CurrentInfo);
                Assert.Equal(testValues[i].ToString(NumberFormatInfo.CurrentInfo), result);
            }
        }

        [Fact]
        public static void FromDoubleArray()
        {
            double[] testValues = new double[] { double.MinValue, 0.0, 1.0, 1000.0, double.MaxValue, double.NegativeInfinity, double.PositiveInfinity, double.Epsilon, double.NaN };

            // Vanilla Test Cases
            for (int i = 0; i < testValues.Length; i++)
            {
                string result = Convert.ToString(testValues[i]);
                Assert.Equal(testValues[i].ToString(), result);
                result = Convert.ToString(testValues[i], NumberFormatInfo.CurrentInfo);
                Assert.Equal(testValues[i].ToString(NumberFormatInfo.CurrentInfo), result);
            }
        }

        [Fact]
        public static void FromDecimalArray()
        {
            decimal[] testValues = new decimal[] { decimal.MinValue, decimal.Parse("-1.234567890123456789012345678", NumberFormatInfo.InvariantInfo), (decimal)0.0, (decimal)1.0, (decimal)1000.0, decimal.MaxValue, decimal.One, decimal.Zero, decimal.MinusOne };

            for (int i = 0; i < testValues.Length; i++)
            {
                string result = Convert.ToString(testValues[i]);
                Assert.Equal(testValues[i].ToString(), result);
                result = Convert.ToString(testValues[i], NumberFormatInfo.CurrentInfo);
                Assert.Equal(testValues[i].ToString(NumberFormatInfo.CurrentInfo), result);
            }
        }

        [Fact]
        public static void FromDateTimeArray()
        {
            DateTime[] testValues = new DateTime[] {
            DateTime.Parse("08/15/2000 16:59:59", DateTimeFormatInfo.InvariantInfo),
            DateTime.Parse("01/01/0001 01:01:01", DateTimeFormatInfo.InvariantInfo) };

            IFormatProvider formatProvider = DateTimeFormatInfo.GetInstance(new CultureInfo("en-US"));

            for (int i = 0; i < testValues.Length; i++)
            {
                string result = Convert.ToString(testValues[i]);
                Assert.Equal(testValues[i].ToString(), result);
                result = Convert.ToString(testValues[i], formatProvider);
                string expected = testValues[i].ToString(formatProvider);
                Assert.Equal(expected, result);
            }
        }

        [Fact]
        public static void FromString()
        {
            string[] testValues = new string[] { "Hello", " ", "", "\0" };

            for (int i = 0; i < testValues.Length; i++)
            {
                string result = Convert.ToString(testValues[i]);
                Assert.Equal(testValues[i].ToString(), result);
                result = Convert.ToString(testValues[i], NumberFormatInfo.CurrentInfo);
                Assert.Equal(testValues[i].ToString(), result);
            }
        }

        [Fact]
        public static void FromIFormattable()
        {
            FooFormattable foo = new FooFormattable(3);
            string result = Convert.ToString(foo);
            Assert.Equal("FooFormattable: 3", result);
            result = Convert.ToString(foo, NumberFormatInfo.CurrentInfo);
            Assert.Equal("System.Globalization.NumberFormatInfo: 3", result);

            foo = null;
            result = Convert.ToString(foo, NumberFormatInfo.CurrentInfo);
            Assert.Equal("", result);
        }

        [Fact]
        public static void FromNonIConvertible()
        {
            Foo foo = new Foo(3);
            string result = Convert.ToString(foo);
            Assert.Equal("System.Tests.ConvertToStringTests+Foo", result);
            result = Convert.ToString(foo, NumberFormatInfo.CurrentInfo);
            Assert.Equal("System.Tests.ConvertToStringTests+Foo", result);

            foo = null;
            result = Convert.ToString(foo, NumberFormatInfo.CurrentInfo);
            Assert.Equal("", result);
        }

        private class FooFormattable : IFormattable
        {
            private int _value;
            public FooFormattable(int value) { _value = value; }

            public string ToString(string format, IFormatProvider formatProvider)
            {
                if (formatProvider != null)
                {
                    return string.Format("{0}: {1}", formatProvider, _value);
                }
                else
                {
                    return string.Format("FooFormattable: {0}", (_value));
                }
            }
        }

        private class Foo
        {
            private int _value;
            public Foo(int value) { _value = value; }

            public string ToString(IFormatProvider provider)
            {
                if (provider != null)
                {
                    return string.Format("{0}: {1}", provider, _value);
                }
                else
                {
                    return string.Format("Foo: {0}", _value);
                }
            }
        }
    }
}
