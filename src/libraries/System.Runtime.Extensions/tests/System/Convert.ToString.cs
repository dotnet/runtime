// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Xunit;

namespace System.Tests
{
    public class ConvertToStringTests
    {
        [Fact]
        public static void FromBoxedObject()
        {
            Object[] testValues =
            {
            // Boolean
            true,
            false,

            // Byte
            Byte.MinValue,
            (Byte)100,
            Byte.MaxValue,

            // Decimal
            Decimal.Zero,
            Decimal.One,
            Decimal.MinusOne,
            Decimal.MaxValue,
            Decimal.MinValue,
            1.234567890123456789012345678m,
            1234.56m,
            -1234.56m,

            // Double
            -12.2364,
            -12.236465923406483,
            -1.7753E-83,
            +12.345e+234,
            +12e+1,
            Double.NegativeInfinity,
            Double.PositiveInfinity,
            Double.NaN,

            // Int16
            Int16.MinValue,
            0,
            Int16.MaxValue,

            // Int32
            Int32.MinValue,
            0,
            Int32.MaxValue,

            // Int64
            Int64.MinValue,
            (Int64)0,
            Int64.MaxValue,

            // SByte
            SByte.MinValue,
            (SByte)0,
            SByte.MaxValue,

            // Single
            -12.2364f,
            -12.2364659234064826243f,
            -1.7753e-83f,
            (float)+12.345e+234,
            +12e+1f,
            Single.NegativeInfinity,
            Single.PositiveInfinity,
            Single.NaN,

            // TimeSpan
            TimeSpan.Zero,
            TimeSpan.Parse("1999.9:09:09"),
            TimeSpan.Parse("-1111.1:11:11"),
            TimeSpan.Parse("1:23:45"),
            TimeSpan.Parse("-2:34:56"),

            // UInt16
            UInt16.MinValue,
            (UInt16)100,
            UInt16.MaxValue,

            // UInt32
            UInt32.MinValue,
            (UInt32)100,
            UInt32.MaxValue,

            // UInt64
            UInt64.MinValue,
            (UInt64)100,
            UInt64.MaxValue
        };

            String[] expectedValues =
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
            "-12.2364659234065",
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
            "-12.23647",
            "0",
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
            Char[] testValues = { 'a', 'A', '@', '\n' };
            String[] expectedValues = { "a", "A", "@", "\n" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i]));
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], CultureInfo.InvariantCulture));
            }
        }

        private static void Verify<TInput>(Func<TInput, String> convert, Func<TInput, IFormatProvider, String> convertWithFormatProvider, TInput[] testValues, String[] expectedValues, IFormatProvider formatProvider = null)
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
            Byte[] testValues = { Byte.MinValue, 100, Byte.MaxValue };
            String[] expectedValues = { "0", "1100100", "11111111" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], 2));
            }
        }

        [Fact]
        public static void FromByteBase8()
        {
            Byte[] testValues = { Byte.MinValue, 100, Byte.MaxValue };
            String[] expectedValues = { "0", "144", "377" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], 8));
            }
        }

        [Fact]
        public static void FromByteBase10()
        {
            Byte[] testValues = { Byte.MinValue, 100, Byte.MaxValue };
            String[] expectedValues = { "0", "100", "255" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], 10));
            }
        }

        [Fact]
        public static void FromByteBase16()
        {
            Byte[] testValues = { Byte.MinValue, 100, Byte.MaxValue };
            String[] expectedValues = { "0", "64", "ff" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], 16));
            }
        }

        [Fact]
        public static void FromByteInvalidBase()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Convert.ToString(Byte.MaxValue, 13));
        }

        [Fact]
        public static void FromInt16Base2()
        {
            Int16[] testValues = { Int16.MinValue, 0, Int16.MaxValue };
            String[] expectedValues = { "1000000000000000", "0", "111111111111111" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], 2));
            }
        }

        [Fact]
        public static void FromInt16Base8()
        {
            Int16[] testValues = { Int16.MinValue, 0, Int16.MaxValue };
            String[] expectedValues = { "100000", "0", "77777" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], 8));
            }
        }

        [Fact]
        public static void FromInt16Base10()
        {
            Int16[] testValues = { Int16.MinValue, 0, Int16.MaxValue };
            String[] expectedValues = { "-32768", "0", "32767" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], 10));
            }
        }

        [Fact]
        public static void FromInt16Base16()
        {
            Int16[] testValues = { Int16.MinValue, 0, Int16.MaxValue };
            String[] expectedValues = { "8000", "0", "7fff" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], 16));
            }
        }

        [Fact]
        public static void FromInt16InvalidBase()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Convert.ToString(Int16.MaxValue, 0));
        }

        [Fact]
        public static void FromInt32Base2()
        {
            Int32[] testValues = { Int32.MinValue, 0, Int32.MaxValue };
            String[] expectedValues = { "10000000000000000000000000000000", "0", "1111111111111111111111111111111" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], 2));
            }
        }

        [Fact]
        public static void FromInt32Base8()
        {
            Int32[] testValues = { Int32.MinValue, 0, Int32.MaxValue };
            String[] expectedValues = { "20000000000", "0", "17777777777" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], 8));
            }
        }

        [Fact]
        public static void FromInt32Base10()
        {
            Int32[] testValues = { Int32.MinValue, 0, Int32.MaxValue };
            String[] expectedValues = { "-2147483648", "0", "2147483647" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], 10));
            }
        }

        [Fact]
        public static void FromInt32Base16()
        {
            Int32[] testValues = { Int32.MinValue, 0, Int32.MaxValue };
            String[] expectedValues = { "80000000", "0", "7fffffff" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], 16));
            }
        }

        [Fact]
        public static void FromInt32InvalidBase()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Convert.ToString(Int32.MaxValue, 9));
        }

        [Fact]
        public static void FromInt64Base2()
        {
            Int64[] testValues = { Int64.MinValue, 0, Int64.MaxValue };
            String[] expectedValues = { "1000000000000000000000000000000000000000000000000000000000000000", "0", "111111111111111111111111111111111111111111111111111111111111111" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], 2));
            }
        }

        [Fact]
        public static void FromInt64Base8()
        {
            Int64[] testValues = { Int64.MinValue, 0, Int64.MaxValue };
            String[] expectedValues = { "1000000000000000000000", "0", "777777777777777777777" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], 8));
            }
        }

        [Fact]
        public static void FromInt64Base10()
        {
            Int64[] testValues = { Int64.MinValue, 0, Int64.MaxValue };
            String[] expectedValues = { "-9223372036854775808", "0", "9223372036854775807" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], 10));
            }
        }

        [Fact]
        public static void FromInt64Base16()
        {
            Int64[] testValues = { Int64.MinValue, 0, Int64.MaxValue };
            String[] expectedValues = { "8000000000000000", "0", "7fffffffffffffff" };

            for (int i = 0; i < testValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], Convert.ToString(testValues[i], 16));
            }
        }

        [Fact]
        public static void FromInt64InvalidBase()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Convert.ToString(Int64.MaxValue, 1));
        }

        [Fact]
        public static void FromBoolean()
        {
            Boolean[] testValues = new[] { true, false };

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
            SByte[] testValues = new SByte[] { SByte.MinValue, -1, 0, 1, SByte.MaxValue };

            for (int i = 0; i < testValues.Length; i++)
            {
                String result = Convert.ToString(testValues[i]);
                Assert.Equal(testValues[i].ToString(), result);
                result = Convert.ToString(testValues[i], NumberFormatInfo.CurrentInfo);
                Assert.Equal(testValues[i].ToString(NumberFormatInfo.CurrentInfo), result);
            }
        }

        [Fact]
        public static void FromByte()
        {
            Byte[] testValues = new Byte[] { Byte.MinValue, 0, 1, 100, Byte.MaxValue };

            for (int i = 0; i < testValues.Length; i++)
            {
                String result = Convert.ToString(testValues[i]);
                Assert.Equal(testValues[i].ToString(), result);
                result = Convert.ToString(testValues[i], NumberFormatInfo.CurrentInfo);
                Assert.Equal(testValues[i].ToString(NumberFormatInfo.CurrentInfo), result);
            }
        }

        [Fact]
        public static void FromInt16Array()
        {
            Int16[] testValues = new Int16[] { Int16.MinValue, -1000, -1, 0, 1, 1000, Int16.MaxValue };

            for (int i = 0; i < testValues.Length; i++)
            {
                String result = Convert.ToString(testValues[i]);
                Assert.Equal(testValues[i].ToString(), result);
                result = Convert.ToString(testValues[i], NumberFormatInfo.CurrentInfo);
                Assert.Equal(testValues[i].ToString(NumberFormatInfo.CurrentInfo), result);
            }
        }

        [Fact]
        public static void FromUInt16Array()
        {
            UInt16[] testValues = new UInt16[] { UInt16.MinValue, 0, 1, 1000, UInt16.MaxValue };

            for (int i = 0; i < testValues.Length; i++)
            {
                String result = Convert.ToString(testValues[i]);
                Assert.Equal(testValues[i].ToString(), result);
                result = Convert.ToString(testValues[i], NumberFormatInfo.CurrentInfo);
                Assert.Equal(testValues[i].ToString(NumberFormatInfo.CurrentInfo), result);
            }
        }

        [Fact]
        public static void FromInt32Array()
        {
            Int32[] testValues = new Int32[] { Int32.MinValue, -1000, -1, 0, 1, 1000, Int32.MaxValue };

            for (int i = 0; i < testValues.Length; i++)
            {
                String result = Convert.ToString(testValues[i]);
                Assert.Equal(testValues[i].ToString(), result);
                result = Convert.ToString(testValues[i], NumberFormatInfo.CurrentInfo);
                Assert.Equal(testValues[i].ToString(NumberFormatInfo.CurrentInfo), result);
            }
        }

        [Fact]
        public static void FromUInt32Array()
        {
            UInt32[] testValues = new UInt32[] { UInt32.MinValue, 0, 1, 1000, UInt32.MaxValue };

            for (int i = 0; i < testValues.Length; i++)
            {
                String result = Convert.ToString(testValues[i]);
                Assert.Equal(testValues[i].ToString(), result);
                result = Convert.ToString(testValues[i], NumberFormatInfo.CurrentInfo);
                Assert.Equal(testValues[i].ToString(NumberFormatInfo.CurrentInfo), result);
            }
        }

        [Fact]
        public static void FromInt64Array()
        {
            Int64[] testValues = new Int64[] { Int64.MinValue, -1000, -1, 0, 1, 1000, Int64.MaxValue };

            for (int i = 0; i < testValues.Length; i++)
            {
                String result = Convert.ToString(testValues[i]);
                Assert.Equal(testValues[i].ToString(), result);
                result = Convert.ToString(testValues[i], NumberFormatInfo.CurrentInfo);
                Assert.Equal(testValues[i].ToString(NumberFormatInfo.CurrentInfo), result);
            }
        }

        [Fact]
        public static void FromUInt64Array()
        {
            UInt64[] testValues = new UInt64[] { UInt64.MinValue, 0, 1, 1000, UInt64.MaxValue };

            for (int i = 0; i < testValues.Length; i++)
            {
                String result = Convert.ToString(testValues[i]);
                Assert.Equal(testValues[i].ToString(), result);
                result = Convert.ToString(testValues[i], NumberFormatInfo.CurrentInfo);
                Assert.Equal(testValues[i].ToString(NumberFormatInfo.CurrentInfo), result);
            }
        }

        [Fact]
        public static void FromSingleArray()
        {
            Single[] testValues = new Single[] { Single.MinValue, 0.0f, 1.0f, 1000.0f, Single.MaxValue, Single.NegativeInfinity, Single.PositiveInfinity, Single.Epsilon, Single.NaN };

            for (int i = 0; i < testValues.Length; i++)
            {
                String result = Convert.ToString(testValues[i]);
                Assert.Equal(testValues[i].ToString(), result);
                result = Convert.ToString(testValues[i], NumberFormatInfo.CurrentInfo);
                Assert.Equal(testValues[i].ToString(NumberFormatInfo.CurrentInfo), result);
            }
        }

        [Fact]
        public static void FromDoubleArray()
        {
            Double[] testValues = new Double[] { Double.MinValue, 0.0, 1.0, 1000.0, Double.MaxValue, Double.NegativeInfinity, Double.PositiveInfinity, Double.Epsilon, Double.NaN };

            // Vanilla Test Cases
            for (int i = 0; i < testValues.Length; i++)
            {
                String result = Convert.ToString(testValues[i]);
                Assert.Equal(testValues[i].ToString(), result);
                result = Convert.ToString(testValues[i], NumberFormatInfo.CurrentInfo);
                Assert.Equal(testValues[i].ToString(NumberFormatInfo.CurrentInfo), result);
            }
        }

        [Fact]
        public static void FromDecimalArray()
        {
            Decimal[] testValues = new Decimal[] { Decimal.MinValue, Decimal.Parse("-1.234567890123456789012345678", NumberFormatInfo.InvariantInfo), (Decimal)0.0, (Decimal)1.0, (Decimal)1000.0, Decimal.MaxValue, Decimal.One, Decimal.Zero, Decimal.MinusOne };

            for (int i = 0; i < testValues.Length; i++)
            {
                String result = Convert.ToString(testValues[i]);
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
                String result = Convert.ToString(testValues[i]);
                Assert.Equal(testValues[i].ToString(), result);
                result = Convert.ToString(testValues[i], formatProvider);
                String expected = testValues[i].ToString(formatProvider);
                Assert.Equal(expected, result);
            }
        }

        [Fact]
        public static void FromString()
        {
            String[] testValues = new String[] { "Hello", " ", "", "\0" };

            for (int i = 0; i < testValues.Length; i++)
            {
                String result = Convert.ToString(testValues[i]);
                Assert.Equal(testValues[i].ToString(), result);
                result = Convert.ToString(testValues[i], NumberFormatInfo.CurrentInfo);
                Assert.Equal(testValues[i].ToString(), result);
            }
        }

        [Fact]
        public static void FromIFormattable()
        {
            FooFormattable foo = new FooFormattable(3);
            String result = Convert.ToString(foo);
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
            String result = Convert.ToString(foo);
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

            public String ToString(String format, IFormatProvider formatProvider)
            {
                if (formatProvider != null)
                {
                    return String.Format("{0}: {1}", formatProvider, _value);
                }
                else
                {
                    return String.Format("FooFormattable: {0}", (_value));
                }
            }
        }

        private class Foo
        {
            private int _value;
            public Foo(int value) { _value = value; }

            public String ToString(IFormatProvider provider)
            {
                if (provider != null)
                {
                    return String.Format("{0}: {1}", provider, _value);
                }
                else
                {
                    return String.Format("Foo: {0}", _value);
                }
            }
        }
    }
}
