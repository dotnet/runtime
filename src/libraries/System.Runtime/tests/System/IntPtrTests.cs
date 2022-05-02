// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.Tests
{
    public static class IntPtrTests
    {
        private static unsafe bool Is64Bit => sizeof(void*) == 8;

        [Fact]
        public static void Zero()
        {
            VerifyPointer(IntPtr.Zero, 0);
        }

        [Fact]
        public static void Ctor_Int()
        {
            int i = 42;
            VerifyPointer(new IntPtr(i), i);
            VerifyPointer((IntPtr)i, i);

            i = -1;
            VerifyPointer(new IntPtr(i), i);
            VerifyPointer((IntPtr)i, i);
        }

        [ConditionalFact(nameof(Is64Bit))]
        public static void Ctor_Long()
        {
            long l = 0x0fffffffffffffff;
            VerifyPointer(new IntPtr(l), l);
            VerifyPointer((IntPtr)l, l);
        }

        [ConditionalFact(nameof(Is64Bit))]
        public static unsafe void Ctor_VoidPointer_ToPointer()
        {
            void* pv = new IntPtr(42).ToPointer();
            VerifyPointer(new IntPtr(pv), 42);
            VerifyPointer((IntPtr)pv, 42);
        }

        [ConditionalFact(nameof(Is64Bit))]
        public static unsafe void Size()
        {
            Assert.Equal(sizeof(void*), IntPtr.Size);
        }

        public static IEnumerable<object[]> Add_TestData()
        {
            yield return new object[] { new IntPtr(42), 6, (long)48 };
            yield return new object[] { new IntPtr(40), 0, (long)40 };
            yield return new object[] { new IntPtr(38), -2, (long)36 };

            yield return new object[] { new IntPtr(0x7fffffffffffffff), 5, unchecked((long)0x8000000000000004) }; /// Add should not throw an OverflowException
        }

        [ConditionalTheory(nameof(Is64Bit))]
        [MemberData(nameof(Add_TestData))]
        public static void Add(IntPtr ptr, int offset, long expected)
        {
            IntPtr p1 = IntPtr.Add(ptr, offset);
            VerifyPointer(p1, expected);

            IntPtr p2 = ptr + offset;
            VerifyPointer(p2, expected);

            IntPtr p3 = ptr;
            p3 += offset;
            VerifyPointer(p3, expected);
        }

        public static IEnumerable<object[]> Subtract_TestData()
        {
            yield return new object[] { new IntPtr(42), 6, (long)36 };
            yield return new object[] { new IntPtr(40), 0, (long)40 };
            yield return new object[] { new IntPtr(38), -2, (long)40 };
        }

        [ConditionalTheory(nameof(Is64Bit))]
        [MemberData(nameof(Subtract_TestData))]
        public static void Subtract(IntPtr ptr, int offset, long expected)
        {
            IntPtr p1 = IntPtr.Subtract(ptr, offset);
            VerifyPointer(p1, expected);

            IntPtr p2 = ptr - offset;
            VerifyPointer(p2, expected);

            IntPtr p3 = ptr;
            p3 -= offset;
            VerifyPointer(p3, expected);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new IntPtr(42), new IntPtr(42), true };
            yield return new object[] { new IntPtr(42), new IntPtr(43), false };
            yield return new object[] { new IntPtr(42), 42, false };
            yield return new object[] { new IntPtr(42), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public static void EqualsTest(IntPtr ptr1, object obj, bool expected)
        {
            if (obj is IntPtr)
            {
                IntPtr ptr2 = (IntPtr)obj;
                Assert.Equal(expected, ptr1 == ptr2);
                Assert.Equal(!expected, ptr1 != ptr2);
                Assert.Equal(expected, ptr1.GetHashCode().Equals(ptr2.GetHashCode()));

                IEquatable<IntPtr> iEquatable = ptr1;
                Assert.Equal(expected, iEquatable.Equals((IntPtr)obj));
            }
            Assert.Equal(expected, ptr1.Equals(obj));
            Assert.Equal(ptr1.GetHashCode(), ptr1.GetHashCode());
        }

        [ConditionalFact(nameof(Is64Bit))]
        public static unsafe void ImplicitCast()
        {
            var ptr = new IntPtr(42);

            uint i = (uint)ptr;
            Assert.Equal(42u, i);
            Assert.Equal(ptr, (IntPtr)i);

            ulong l = (ulong)ptr;
            Assert.Equal(42u, l);
            Assert.Equal(ptr, (IntPtr)l);

            void* v = (void*)ptr;
            Assert.Equal(ptr, (IntPtr)v);

            ptr = new IntPtr(0x7fffffffffffffff);
            Assert.Throws<OverflowException>(() => (int)ptr);
        }

        [ConditionalFact(nameof(Is64Bit))]
        public static void GetHashCodeRespectAllBits()
        {
            var ptr1 = new IntPtr(0x123456FFFFFFFF);
            var ptr2 = new IntPtr(0x654321FFFFFFFF);
            Assert.NotEqual(ptr1.GetHashCode(), ptr2.GetHashCode());
        }

        private static void VerifyPointer(IntPtr ptr, long expected)
        {
            Assert.Equal(expected, ptr.ToInt64());

            int expected32 = unchecked((int)expected);
            if (expected32 != expected)
            {
                Assert.Throws<OverflowException>(() => ptr.ToInt32());
                return;
            }

            int i = ptr.ToInt32();
            Assert.Equal(expected32, ptr.ToInt32());

            Assert.Equal(expected.ToString(), ptr.ToString());
            Assert.Equal(IntPtr.Size == 4 ? expected32.ToString("x") : expected.ToString("x"), ptr.ToString("x"));

            Assert.Equal(ptr, new IntPtr(expected));
            Assert.True(ptr == new IntPtr(expected));
            Assert.False(ptr != new IntPtr(expected));

            Assert.NotEqual(ptr, new IntPtr(expected + 1));
            Assert.False(ptr == new IntPtr(expected + 1));
            Assert.True(ptr != new IntPtr(expected + 1));
        }


        public static IntPtr RealMax => Is64Bit ? (IntPtr)long.MaxValue : (IntPtr)int.MaxValue;
        public static IntPtr RealMin => Is64Bit ? (IntPtr)long.MinValue : (IntPtr)int.MinValue;

        [Fact]
        public static void Ctor_Empty()
        {
            var i = new IntPtr();
            Assert.Equal(default, i);
        }

        [Fact]
        public static void Ctor_Value()
        {
            IntPtr i = (IntPtr)41;
            Assert.Equal((IntPtr)041, i);
        }

        [Fact]
        public static void MaxValue()
        {
            Assert.Equal(RealMax, IntPtr.MaxValue);
        }

        [Fact]
        public static void MinValue()
        {
            Assert.Equal(RealMin, IntPtr.MinValue);
        }

        [Theory]
        [InlineData(234, 234, 0)]
        [InlineData(234, int.MinValue, 1)]
        [InlineData(-234, int.MinValue, 1)]
        [InlineData(int.MinValue, int.MinValue, 0)]
        [InlineData(234, -123, 1)]
        [InlineData(234, 0, 1)]
        [InlineData(234, 123, 1)]
        [InlineData(234, 456, -1)]
        [InlineData(234, int.MaxValue, -1)]
        [InlineData(-234, int.MaxValue, -1)]
        [InlineData(int.MaxValue, int.MaxValue, 0)]
        [InlineData(-234, -234, 0)]
        [InlineData(-234, 234, -1)]
        [InlineData(-234, -432, 1)]
        [InlineData(234, null, 1)]
        public static void CompareTo_Other_ReturnsExpected(int l, object value, int expected)
        {
            var i = (IntPtr)l;
            if (value is int intValue)
            {
                var intPtrValue = (IntPtr)intValue;
                Assert.Equal(expected, Math.Sign(i.CompareTo(intPtrValue)));
                Assert.Equal(-expected, Math.Sign(intPtrValue.CompareTo(i)));

                Assert.Equal(expected, Math.Sign(i.CompareTo((object)intPtrValue)));
            }
            else
            {
                Assert.Equal(expected, Math.Sign(i.CompareTo(value)));
            }
        }

        [Theory]
        [InlineData("a")]
        [InlineData((long)234)]
        public static void CompareTo_ObjectNotIntPtr_ThrowsArgumentException(object value)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => ((IntPtr)123).CompareTo(value));
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            foreach (NumberFormatInfo defaultFormat in new[] { null, NumberFormatInfo.CurrentInfo })
            {
                foreach (string defaultSpecifier in new[] { "G", "G\0", "\0N222", "\0", "", "R" })
                {
                    yield return new object[] { IntPtr.MinValue, defaultSpecifier, defaultFormat, Is64Bit ? "-9223372036854775808" : "-2147483648" };
                    yield return new object[] { (IntPtr)(-4567), defaultSpecifier, defaultFormat, "-4567" };
                    yield return new object[] { (IntPtr)0, defaultSpecifier, defaultFormat, "0" };
                    yield return new object[] { (IntPtr)4567, defaultSpecifier, defaultFormat, "4567" };
                    yield return new object[] { IntPtr.MaxValue, defaultSpecifier, defaultFormat, Is64Bit ? "9223372036854775807" : "2147483647" };
                }

                yield return new object[] { (IntPtr)4567, "D", defaultFormat, "4567" };
                yield return new object[] { (IntPtr)4567, "D99", defaultFormat, "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004567" };
                yield return new object[] { (IntPtr)4567, "D99\09", defaultFormat, "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004567" };
                yield return new object[] { (IntPtr)(-4567), "D99\09", defaultFormat, "-000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004567" };

                yield return new object[] { (IntPtr)0x2468, "x", defaultFormat, "2468" };
                yield return new object[] { (IntPtr)(-0x2468), "x", defaultFormat, Is64Bit ? "ffffffffffffdb98" : "ffffdb98" };
                yield return new object[] { (IntPtr)2468, "N", defaultFormat, string.Format("{0:N}", 2468.00) };
            }

            var customFormat = new NumberFormatInfo()
            {
                NegativeSign = "#",
                NumberDecimalSeparator = "~",
                NumberGroupSeparator = "*",
                PositiveSign = "&",
                NumberDecimalDigits = 2,
                PercentSymbol = "@",
                PercentGroupSeparator = ",",
                PercentDecimalSeparator = ".",
                PercentDecimalDigits = 5
            };
            yield return new object[] { (IntPtr)(-2468), "N", customFormat, "#2*468~00" };
            yield return new object[] { (IntPtr)2468, "N", customFormat, "2*468~00" };
            yield return new object[] { (IntPtr)123, "E", customFormat, "1~230000E&002" };
            yield return new object[] { (IntPtr)123, "F", customFormat, "123~00" };
            yield return new object[] { (IntPtr)123, "P", customFormat, "12,300.00000 @" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void ToStringTest(IntPtr i, string format, IFormatProvider provider, string expected)
        {
            // Format is case insensitive
            string upperFormat = format.ToUpperInvariant();
            string lowerFormat = format.ToLowerInvariant();

            string upperExpected = expected.ToUpperInvariant();
            string lowerExpected = expected.ToLowerInvariant();

            bool isDefaultProvider = (provider == null || provider == NumberFormatInfo.CurrentInfo);
            if (string.IsNullOrEmpty(format) || format.ToUpperInvariant() is "G" or "R")
            {
                if (isDefaultProvider)
                {
                    Assert.Equal(upperExpected, i.ToString());
                    Assert.Equal(upperExpected, i.ToString((IFormatProvider)null));
                }
                Assert.Equal(upperExpected, i.ToString(provider));
            }
            if (isDefaultProvider)
            {
                Assert.Equal(upperExpected, i.ToString(upperFormat));
                Assert.Equal(lowerExpected, i.ToString(lowerFormat));
                Assert.Equal(upperExpected, i.ToString(upperFormat, null));
                Assert.Equal(lowerExpected, i.ToString(lowerFormat, null));
            }
            Assert.Equal(upperExpected, i.ToString(upperFormat, provider));
            Assert.Equal(lowerExpected, i.ToString(lowerFormat, provider));
        }

        [Fact]
        public static void ToString_InvalidFormat_ThrowsFormatException()
        {
            IntPtr i = (IntPtr)123;
            Assert.Throws<FormatException>(() => i.ToString("Y")); // Invalid format
            Assert.Throws<FormatException>(() => i.ToString("Y", null)); // Invalid format
        }

        public static IEnumerable<object[]> Parse_Valid_TestData()
        {
            NumberFormatInfo samePositiveNegativeFormat = new NumberFormatInfo()
            {
                PositiveSign = "|",
                NegativeSign = "|"
            };

            NumberFormatInfo emptyPositiveFormat = new NumberFormatInfo() { PositiveSign = "" };
            NumberFormatInfo emptyNegativeFormat = new NumberFormatInfo() { NegativeSign = "" };

            // None
            yield return new object[] { "0", NumberStyles.None, null, (IntPtr)0 };
            yield return new object[] { "0000000000000000000000000000000000000000000000000000000000", NumberStyles.None, null, (IntPtr)0 };
            yield return new object[] { "0000000000000000000000000000000000000000000000000000000001", NumberStyles.None, null, (IntPtr)1 };
            yield return new object[] { "2147483647", NumberStyles.None, null, (IntPtr)2147483647 };
            yield return new object[] { "02147483647", NumberStyles.None, null, (IntPtr)2147483647 };
            yield return new object[] { "00000000000000000000000000000000000000000000000002147483647", NumberStyles.None, null, (IntPtr)2147483647 };
            yield return new object[] { "123\0\0", NumberStyles.None, null, (IntPtr)123 };

            // All lengths decimal
            foreach (bool neg in new[] { false, true })
            {
                string s = neg ? "-" : "";
                var result = 0;
                for (var i = 1; i <= 10; i++)
                {
                    result = result * 10 + (i % 10);
                    s += (i % 10).ToString();
                    yield return new object[] { s, NumberStyles.Integer, null, (IntPtr)(neg ? result * -1 : result) };
                }
            }

            // All lengths hexadecimal
            {
                string s = "";
                var result = 0;
                for (var i = 1; i <= 8; i++)
                {
                    result = (result * 16) + (i % 16);
                    s += (i % 16).ToString("X");
                    yield return new object[] { s, NumberStyles.HexNumber, null, (IntPtr)result };
                }
            }

            // HexNumber
            yield return new object[] { "123", NumberStyles.HexNumber, null, (IntPtr)0x123 };
            yield return new object[] { "abc", NumberStyles.HexNumber, null, (IntPtr)0xabc };
            yield return new object[] { "ABC", NumberStyles.HexNumber, null, (IntPtr)0xabc };
            yield return new object[] { "12", NumberStyles.HexNumber, null, (IntPtr)0x12 };


            if (Is64Bit)
            {
                yield return new object[] { "8000000000000000", NumberStyles.HexNumber, null, IntPtr.MinValue };
                yield return new object[] { "7FFFFFFFFFFFFFFF", NumberStyles.HexNumber, null, IntPtr.MaxValue };
            }
            else
            {
                yield return new object[] { "80000000", NumberStyles.HexNumber, null, IntPtr.MinValue };
                yield return new object[] { "7FFFFFFF", NumberStyles.HexNumber, null, IntPtr.MaxValue };
            }

            // Currency
            NumberFormatInfo currencyFormat = new NumberFormatInfo()
            {
                CurrencySymbol = "$",
                CurrencyGroupSeparator = "|",
                NumberGroupSeparator = "/"
            };
            yield return new object[] { "$1|000", NumberStyles.Currency, currencyFormat, (IntPtr)1000 };
            yield return new object[] { "$1000", NumberStyles.Currency, currencyFormat, (IntPtr)1000 };
            yield return new object[] { "$   1000", NumberStyles.Currency, currencyFormat, (IntPtr)1000 };
            yield return new object[] { "1000", NumberStyles.Currency, currencyFormat, (IntPtr)1000 };
            yield return new object[] { "$(1000)", NumberStyles.Currency, currencyFormat, (IntPtr)(-1000) };
            yield return new object[] { "($1000)", NumberStyles.Currency, currencyFormat, (IntPtr)(-1000) };
            yield return new object[] { "$-1000", NumberStyles.Currency, currencyFormat, (IntPtr)(-1000) };
            yield return new object[] { "-$1000", NumberStyles.Currency, currencyFormat, (IntPtr)(-1000) };

            NumberFormatInfo emptyCurrencyFormat = new NumberFormatInfo() { CurrencySymbol = "" };
            yield return new object[] { "100", NumberStyles.Currency, emptyCurrencyFormat, (IntPtr)100 };

            // If CurrencySymbol and Negative are the same, NegativeSign is preferred
            NumberFormatInfo sameCurrencyNegativeSignFormat = new NumberFormatInfo()
            {
                NegativeSign = "|",
                CurrencySymbol = "|"
            };
            yield return new object[] { "|1000", NumberStyles.AllowCurrencySymbol | NumberStyles.AllowLeadingSign, sameCurrencyNegativeSignFormat, (IntPtr)(-1000) };

            // Any
            yield return new object[] { "123", NumberStyles.Any, null, (IntPtr)123 };

            // AllowLeadingSign
            yield return new object[] { "-2147483648", NumberStyles.AllowLeadingSign, null, (IntPtr)(-2147483648) };
            yield return new object[] { "-123", NumberStyles.AllowLeadingSign, null, (IntPtr)(-123) };
            yield return new object[] { "+0", NumberStyles.AllowLeadingSign, null, (IntPtr)0 };
            yield return new object[] { "-0", NumberStyles.AllowLeadingSign, null, (IntPtr)0 };
            yield return new object[] { "+123", NumberStyles.AllowLeadingSign, null, (IntPtr)123 };

            // If PositiveSign and NegativeSign are the same, PositiveSign is preferred
            yield return new object[] { "|123", NumberStyles.AllowLeadingSign, samePositiveNegativeFormat, (IntPtr)123 };

            // Empty PositiveSign or NegativeSign
            yield return new object[] { "100", NumberStyles.AllowLeadingSign, emptyPositiveFormat, (IntPtr)100 };
            yield return new object[] { "100", NumberStyles.AllowLeadingSign, emptyNegativeFormat, (IntPtr)100 };

            // AllowTrailingSign
            yield return new object[] { "123", NumberStyles.AllowTrailingSign, null, (IntPtr)123 };
            yield return new object[] { "123+", NumberStyles.AllowTrailingSign, null, (IntPtr)123 };
            yield return new object[] { "123-", NumberStyles.AllowTrailingSign, null, (IntPtr)(-123) };

            // If PositiveSign and NegativeSign are the same, PositiveSign is preferred
            yield return new object[] { "123|", NumberStyles.AllowTrailingSign, samePositiveNegativeFormat, (IntPtr)123 };

            // Empty PositiveSign or NegativeSign
            yield return new object[] { "100", NumberStyles.AllowTrailingSign, emptyPositiveFormat, (IntPtr)100 };
            yield return new object[] { "100", NumberStyles.AllowTrailingSign, emptyNegativeFormat, (IntPtr)100 };

            // AllowLeadingWhite and AllowTrailingWhite
            yield return new object[] { "  123", NumberStyles.AllowLeadingWhite, null, (IntPtr)123 };
            yield return new object[] { "  123  ", NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, null, (IntPtr)123 };
            yield return new object[] { "123  ", NumberStyles.AllowTrailingWhite, null, (IntPtr)123 };
            yield return new object[] { "123  \0\0", NumberStyles.AllowTrailingWhite, null, (IntPtr)123 };
            yield return new object[] { "   2147483647   ", NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, null, (IntPtr)2147483647 };
            yield return new object[] { "   -2147483648   ", NumberStyles.Integer, null, (IntPtr)(-2147483648) };
            foreach (char c in new[] { (char)0x9, (char)0xA, (char)0xB, (char)0xC, (char)0xD })
            {
                string cs = c.ToString();
                yield return new object[] { cs + cs + "123" + cs + cs, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, null, (IntPtr)123 };
            }
            yield return new object[] { "  0  ", NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, null, (IntPtr)0 };
            yield return new object[] { "  000000000  ", NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, null, (IntPtr)0 };

            // AllowThousands
            NumberFormatInfo thousandsFormat = new NumberFormatInfo() { NumberGroupSeparator = "|" };
            yield return new object[] { "1000", NumberStyles.AllowThousands, thousandsFormat, (IntPtr)1000 };
            yield return new object[] { "1|0|0|0", NumberStyles.AllowThousands, thousandsFormat, (IntPtr)1000 };
            yield return new object[] { "1|||", NumberStyles.AllowThousands, thousandsFormat, (IntPtr)1 };

            NumberFormatInfo IntegerNumberSeparatorFormat = new NumberFormatInfo() { NumberGroupSeparator = "1" };
            yield return new object[] { "1111", NumberStyles.AllowThousands, IntegerNumberSeparatorFormat, (IntPtr)1111 };

            // AllowExponent
            yield return new object[] { "1E2", NumberStyles.AllowExponent, null, (IntPtr)100 };
            yield return new object[] { "1E+2", NumberStyles.AllowExponent, null, (IntPtr)100 };
            yield return new object[] { "1e2", NumberStyles.AllowExponent, null, (IntPtr)100 };
            yield return new object[] { "1E0", NumberStyles.AllowExponent, null, (IntPtr)1 };
            yield return new object[] { "(1E2)", NumberStyles.AllowExponent | NumberStyles.AllowParentheses, null, (IntPtr)(-100) };
            yield return new object[] { "-1E2", NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign, null, (IntPtr)(-100) };

            NumberFormatInfo negativeFormat = new NumberFormatInfo() { PositiveSign = "|" };
            yield return new object[] { "1E|2", NumberStyles.AllowExponent, negativeFormat, (IntPtr)100 };

            // AllowParentheses
            yield return new object[] { "123", NumberStyles.AllowParentheses, null, (IntPtr)123 };
            yield return new object[] { "(123)", NumberStyles.AllowParentheses, null, (IntPtr)(-123) };

            // AllowDecimalPoint
            NumberFormatInfo decimalFormat = new NumberFormatInfo() { NumberDecimalSeparator = "|" };
            yield return new object[] { "67|", NumberStyles.AllowDecimalPoint, decimalFormat, (IntPtr)67 };

            // NumberFormatInfo has a custom property with length > (IntPtr)1
            NumberFormatInfo IntegerCurrencyFormat = new NumberFormatInfo() { CurrencySymbol = "123" };
            yield return new object[] { "123123", NumberStyles.AllowCurrencySymbol, IntegerCurrencyFormat, (IntPtr)123 };

            yield return new object[] { "123123", NumberStyles.AllowLeadingSign, new NumberFormatInfo() { PositiveSign = "1" }, (IntPtr)23123 };
            yield return new object[] { "123123", NumberStyles.AllowLeadingSign, new NumberFormatInfo() { NegativeSign = "1" }, (IntPtr)(-23123) };
            yield return new object[] { "123123", NumberStyles.AllowLeadingSign, new NumberFormatInfo() { PositiveSign = "123" }, (IntPtr)123 };
            yield return new object[] { "123123", NumberStyles.AllowLeadingSign, new NumberFormatInfo() { NegativeSign = "123" }, (IntPtr)(-123) };
            yield return new object[] { "123123", NumberStyles.AllowLeadingSign, new NumberFormatInfo() { PositiveSign = "12312" }, (IntPtr)3 };
            yield return new object[] { "123123", NumberStyles.AllowLeadingSign, new NumberFormatInfo() { NegativeSign = "12312" }, (IntPtr)(-3) };
        }

        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse_Valid(string value, NumberStyles style, IFormatProvider provider, IntPtr expected)
        {
            IntPtr result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.True(IntPtr.TryParse(value, out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, IntPtr.Parse(value));
            }

            // Default provider
            if (provider == null)
            {
                Assert.Equal(expected, IntPtr.Parse(value, style));

                // Substitute default NumberFormatInfo
                Assert.True(IntPtr.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, IntPtr.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Equal(expected, IntPtr.Parse(value, provider));
            }

            // Full overloads
            Assert.True(IntPtr.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, IntPtr.Parse(value, style, provider));
        }

        public static IEnumerable<object[]> Parse_Invalid_TestData()
        {
            // String is null, empty or entirely whitespace
            yield return new object[] { null, NumberStyles.Integer, null, typeof(ArgumentNullException) };
            yield return new object[] { null, NumberStyles.Any, null, typeof(ArgumentNullException) };

            // String contains is null, empty or enitrely whitespace.
            foreach (NumberStyles style in new[] { NumberStyles.Integer, NumberStyles.HexNumber, NumberStyles.Any })
            {
                yield return new object[] { null, style, null, typeof(ArgumentNullException) };
                yield return new object[] { "", style, null, typeof(FormatException) };
                yield return new object[] { " \t \n \r ", style, null, typeof(FormatException) };
                yield return new object[] { "   \0\0", style, null, typeof(FormatException) };
            }

            // Leading or trailing chars for which char.IsWhiteSpace is true but that's not valid for leading/trailing whitespace
            foreach (string c in new[] { "\x0085", "\x00A0", "\x1680", "\x2000", "\x2001", "\x2002", "\x2003", "\x2004", "\x2005", "\x2006", "\x2007", "\x2008", "\x2009", "\x200A", "\x2028", "\x2029", "\x202F", "\x205F", "\x3000" })
            {
                yield return new object[] { c + "123", NumberStyles.Integer, null, typeof(FormatException) };
                yield return new object[] { "123" + c, NumberStyles.Integer, null, typeof(FormatException) };
            }

            // String contains garbage
            foreach (NumberStyles style in new[] { NumberStyles.Integer, NumberStyles.HexNumber, NumberStyles.Any })
            {
                yield return new object[] { "Garbage", style, null, typeof(FormatException) };
                yield return new object[] { "g", style, null, typeof(FormatException) };
                yield return new object[] { "g1", style, null, typeof(FormatException) };
                yield return new object[] { "1g", style, null, typeof(FormatException) };
                yield return new object[] { "123g", style, null, typeof(FormatException) };
                yield return new object[] { "g123", style, null, typeof(FormatException) };
                yield return new object[] { "214748364g", style, null, typeof(FormatException) };
            }

            // String has leading zeros
            yield return new object[] { "\0\0123", NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { "\0\0123", NumberStyles.Any, null, typeof(FormatException) };

            // String has IntPtrernal zeros
            yield return new object[] { "1\023", NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { "1\023", NumberStyles.Any, null, typeof(FormatException) };

            // String has trailing zeros but with whitespace after
            yield return new object[] { "123\0\0   ", NumberStyles.Integer, null, typeof(FormatException) };

            // Integer doesn't allow hex, exponents, paretheses, currency, thousands, decimal
            yield return new object[] { "abc", NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { "1E23", NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { "(123)", NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { 1000.ToString("C0"), NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { 1000.ToString("N0"), NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { 678.90.ToString("F2"), NumberStyles.Integer, null, typeof(FormatException) };

            // HexNumber
            yield return new object[] { "0xabc", NumberStyles.HexNumber, null, typeof(FormatException) };
            yield return new object[] { "&habc", NumberStyles.HexNumber, null, typeof(FormatException) };
            yield return new object[] { "G1", NumberStyles.HexNumber, null, typeof(FormatException) };
            yield return new object[] { "g1", NumberStyles.HexNumber, null, typeof(FormatException) };
            yield return new object[] { "+abc", NumberStyles.HexNumber, null, typeof(FormatException) };
            yield return new object[] { "-abc", NumberStyles.HexNumber, null, typeof(FormatException) };

            // None doesn't allow hex or leading or trailing whitespace
            yield return new object[] { "abc", NumberStyles.None, null, typeof(FormatException) };
            yield return new object[] { "123   ", NumberStyles.None, null, typeof(FormatException) };
            yield return new object[] { "   123", NumberStyles.None, null, typeof(FormatException) };
            yield return new object[] { "  123  ", NumberStyles.None, null, typeof(FormatException) };

            // AllowLeadingSign
            yield return new object[] { "+", NumberStyles.AllowLeadingSign, null, typeof(FormatException) };
            yield return new object[] { "-", NumberStyles.AllowLeadingSign, null, typeof(FormatException) };
            yield return new object[] { "+-123", NumberStyles.AllowLeadingSign, null, typeof(FormatException) };
            yield return new object[] { "-+123", NumberStyles.AllowLeadingSign, null, typeof(FormatException) };
            yield return new object[] { "- 123", NumberStyles.AllowLeadingSign, null, typeof(FormatException) };
            yield return new object[] { "+ 123", NumberStyles.AllowLeadingSign, null, typeof(FormatException) };

            // AllowTrailingSign
            yield return new object[] { "123-+", NumberStyles.AllowTrailingSign, null, typeof(FormatException) };
            yield return new object[] { "123+-", NumberStyles.AllowTrailingSign, null, typeof(FormatException) };
            yield return new object[] { "123 -", NumberStyles.AllowTrailingSign, null, typeof(FormatException) };
            yield return new object[] { "123 +", NumberStyles.AllowTrailingSign, null, typeof(FormatException) };

            // Parentheses has priority over CurrencySymbol and PositiveSign
            NumberFormatInfo currencyNegativeParenthesesFormat = new NumberFormatInfo()
            {
                CurrencySymbol = "(",
                PositiveSign = "))"
            };
            yield return new object[] { "(100))", NumberStyles.AllowParentheses | NumberStyles.AllowCurrencySymbol | NumberStyles.AllowTrailingSign, currencyNegativeParenthesesFormat, typeof(FormatException) };

            // AllowTrailingSign and AllowLeadingSign
            yield return new object[] { "+123+", NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingSign, null, typeof(FormatException) };
            yield return new object[] { "+123-", NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingSign, null, typeof(FormatException) };
            yield return new object[] { "-123+", NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingSign, null, typeof(FormatException) };
            yield return new object[] { "-123-", NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingSign, null, typeof(FormatException) };

            // AllowLeadingSign and AllowParentheses
            yield return new object[] { "-(1000)", NumberStyles.AllowLeadingSign | NumberStyles.AllowParentheses, null, typeof(FormatException) };
            yield return new object[] { "(-1000)", NumberStyles.AllowLeadingSign | NumberStyles.AllowParentheses, null, typeof(FormatException) };

            // AllowLeadingWhite
            yield return new object[] { "1   ", NumberStyles.AllowLeadingWhite, null, typeof(FormatException) };
            yield return new object[] { "   1   ", NumberStyles.AllowLeadingWhite, null, typeof(FormatException) };

            // AllowTrailingWhite
            yield return new object[] { "   1       ", NumberStyles.AllowTrailingWhite, null, typeof(FormatException) };
            yield return new object[] { "   1", NumberStyles.AllowTrailingWhite, null, typeof(FormatException) };

            // AllowThousands
            NumberFormatInfo thousandsFormat = new NumberFormatInfo() { NumberGroupSeparator = "|" };
            yield return new object[] { "|||1", NumberStyles.AllowThousands, null, typeof(FormatException) };

            // AllowExponent
            yield return new object[] { "65E", NumberStyles.AllowExponent, null, typeof(FormatException) };
            yield return new object[] { "65E19", NumberStyles.AllowExponent, null, typeof(OverflowException) };
            yield return new object[] { "65E+19", NumberStyles.AllowExponent, null, typeof(OverflowException) };
            yield return new object[] { "65E-1", NumberStyles.AllowExponent, null, typeof(OverflowException) };

            // AllowDecimalPoint
            NumberFormatInfo decimalFormat = new NumberFormatInfo() { NumberDecimalSeparator = "." };
            yield return new object[] { "67.9", NumberStyles.AllowDecimalPoint, decimalFormat, typeof(OverflowException) };

            // Parsing Integers doesn't allow NaN, PositiveInfinity or NegativeInfinity
            NumberFormatInfo doubleFormat = new NumberFormatInfo()
            {
                NaNSymbol = "NaN",
                PositiveInfinitySymbol = "Infinity",
                NegativeInfinitySymbol = "-Infinity"
            };
            yield return new object[] { "NaN", NumberStyles.Any, doubleFormat, typeof(FormatException) };
            yield return new object[] { "Infinity", NumberStyles.Any, doubleFormat, typeof(FormatException) };
            yield return new object[] { "-Infinity", NumberStyles.Any, doubleFormat, typeof(FormatException) };

            // Only has a leading sign
            yield return new object[] { "+", NumberStyles.AllowLeadingSign, null, typeof(FormatException) };
            yield return new object[] { "-", NumberStyles.AllowLeadingSign, null, typeof(FormatException) };
            yield return new object[] { " +", NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { " -", NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { "+ ", NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { "- ", NumberStyles.Integer, null, typeof(FormatException) };

            // NumberFormatInfo has a custom property with length > (IntPtr)1
            NumberFormatInfo IntegerCurrencyFormat = new NumberFormatInfo() { CurrencySymbol = "123" };
            yield return new object[] { "123", NumberStyles.AllowCurrencySymbol, IntegerCurrencyFormat, typeof(FormatException) };
            yield return new object[] { "123", NumberStyles.AllowLeadingSign, new NumberFormatInfo() { PositiveSign = "123" }, typeof(FormatException) };
            yield return new object[] { "123", NumberStyles.AllowLeadingSign, new NumberFormatInfo() { NegativeSign = "123" }, typeof(FormatException) };

            // Decimals not in range of IntPtr32
            foreach (string s in new[]
            {

                "9223372036854775808", // long.MaxValue + 1
                "9223372036854775810", // 10s digit incremented above long.MaxValue
                "10000000000000000000", // extra digit after long.MaxValue

                "18446744073709551616", // ulong.MaxValue + 1
                "18446744073709551620", // 10s digit incremented above ulong.MaxValue
                "100000000000000000000", // extra digit after ulong.MaxValue

                "-9223372036854775809", // long.MinValue - 1
                "-9223372036854775810", // 10s digit decremented below long.MinValue
                "-10000000000000000000", // extra digit after long.MinValue

                "100000000000000000000000000000000000000000000000000000000000000000000000000000000000000", // really big
                "-100000000000000000000000000000000000000000000000000000000000000000000000000000000000000" // really small
            })
            {
                foreach (NumberStyles styles in new[] { NumberStyles.Any, NumberStyles.Integer })
                {
                    yield return new object[] { s, styles, null, typeof(OverflowException) };
                    yield return new object[] { s + "   ", styles, null, typeof(OverflowException) };
                    yield return new object[] { s + "   " + "\0\0\0", styles, null, typeof(OverflowException) };

                    yield return new object[] { s + "g", styles, null, typeof(FormatException) };
                    yield return new object[] { s + "\0g", styles, null, typeof(FormatException) };
                    yield return new object[] { s + " g", styles, null, typeof(FormatException) };
                }
            }

            // Hexadecimals not in range of IntPtr32
            foreach (string s in new[]
            {
                "10000000000000000", // ulong.MaxValue + (IntPtr)1
                "FFFFFFFFFFFFFFFF0", // extra digit after ulong.MaxValue

                "100000000000000000000000000000000000000000000000000000000000000000000000000000000000000" // really big
            })
            {
                yield return new object[] { s, NumberStyles.HexNumber, null, typeof(OverflowException) };
                yield return new object[] { s + "   ", NumberStyles.HexNumber, null, typeof(OverflowException) };
                yield return new object[] { s + "   " + "\0\0", NumberStyles.HexNumber, null, typeof(OverflowException) };

                yield return new object[] { s + "g", NumberStyles.HexNumber, null, typeof(FormatException) };
                yield return new object[] { s + "\0g", NumberStyles.HexNumber, null, typeof(FormatException) };
                yield return new object[] { s + " g", NumberStyles.HexNumber, null, typeof(FormatException) };
            }

            yield return new object[] { "9223372036854775809-", NumberStyles.AllowTrailingSign, null, typeof(OverflowException) };
            yield return new object[] { "(9223372036854775809)", NumberStyles.AllowParentheses, null, typeof(OverflowException) };
            yield return new object[] { "2E19", NumberStyles.AllowExponent, null, typeof(OverflowException) };
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (2 == 2E10) { }
            IntPtr result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.False(IntPtr.TryParse(value, out result));
                Assert.Equal(default, result);
                Assert.Throws(exceptionType, () => IntPtr.Parse(value));
            }

            // Default provider
            if (provider == null)
            {
                Assert.Throws(exceptionType, () => IntPtr.Parse(value, style));

                // Substitute default NumberFormatInfo
                Assert.False(IntPtr.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(default, result);
                Assert.Throws(exceptionType, () => IntPtr.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Throws(exceptionType, () => IntPtr.Parse(value, provider));
            }

            // Full overloads
            Assert.False(IntPtr.TryParse(value, style, provider, out result));
            Assert.Equal(default, result);
            Assert.Throws(exceptionType, () => IntPtr.Parse(value, style, provider));
        }

        [Theory]
        [InlineData(NumberStyles.HexNumber | NumberStyles.AllowParentheses, null)]
        [InlineData(unchecked((NumberStyles)0xFFFFFC00), "style")]
        public static void TryParse_InvalidNumberStyle_ThrowsArgumentException(NumberStyles style, string paramName)
        {
            IntPtr result = (IntPtr)0;
            AssertExtensions.Throws<ArgumentException>(paramName, () => IntPtr.TryParse("1", style, null, out result));
            Assert.Equal(default(IntPtr), result);

            AssertExtensions.Throws<ArgumentException>(paramName, () => IntPtr.Parse("1", style));
            AssertExtensions.Throws<ArgumentException>(paramName, () => IntPtr.Parse("1", style, null));
        }

        public static IEnumerable<object[]> Parse_ValidWithOffsetCount_TestData()
        {
            foreach (object[] inputs in Parse_Valid_TestData())
            {
                yield return new object[] { inputs[0], 0, ((string)inputs[0]).Length, inputs[1], inputs[2], inputs[3] };
            }

            NumberFormatInfo samePositiveNegativeFormat = new NumberFormatInfo()
            {
                PositiveSign = "|",
                NegativeSign = "|"
            };

            NumberFormatInfo emptyPositiveFormat = new NumberFormatInfo() { PositiveSign = "" };
            NumberFormatInfo emptyNegativeFormat = new NumberFormatInfo() { NegativeSign = "" };

            // None
            yield return new object[] { "2147483647", 1, 9, NumberStyles.None, null, (IntPtr)147483647 };
            yield return new object[] { "2147483647", 1, 1, NumberStyles.None, null, (IntPtr)1 };
            yield return new object[] { "123\0\0", 2, 2, NumberStyles.None, null, (IntPtr)3 };

            // Hex
            yield return new object[] { "abc", 0, 1, NumberStyles.HexNumber, null, (IntPtr)0xa };
            yield return new object[] { "ABC", 1, 1, NumberStyles.HexNumber, null, (IntPtr)0xB };
            yield return new object[] { "FFFFFFFF", 6, 2, NumberStyles.HexNumber, null, (IntPtr)0xFF };
            yield return new object[] { "FFFFFFFF", 0, 1, NumberStyles.HexNumber, null, (IntPtr)0xF };

            // Currency
            yield return new object[] { "-$1000", 1, 5, NumberStyles.Currency, new NumberFormatInfo()
            {
                CurrencySymbol = "$",
                CurrencyGroupSeparator = "|",
                NumberGroupSeparator = "/"
            }, (IntPtr)1000 };

            NumberFormatInfo emptyCurrencyFormat = new NumberFormatInfo() { CurrencySymbol = "" };
            yield return new object[] { "100", 1, 2, NumberStyles.Currency, emptyCurrencyFormat, (IntPtr)0 };
            yield return new object[] { "100", 0, 1, NumberStyles.Currency, emptyCurrencyFormat, (IntPtr)1 };

            // If CurrencySymbol and Negative are the same, NegativeSign is preferred
            NumberFormatInfo sameCurrencyNegativeSignFormat = new NumberFormatInfo()
            {
                NegativeSign = "|",
                CurrencySymbol = "|"
            };
            yield return new object[] { "1000", 1, 3, NumberStyles.AllowCurrencySymbol | NumberStyles.AllowLeadingSign, sameCurrencyNegativeSignFormat, (IntPtr)0 };
            yield return new object[] { "|1000", 0, 2, NumberStyles.AllowCurrencySymbol | NumberStyles.AllowLeadingSign, sameCurrencyNegativeSignFormat, (IntPtr)(-1) };

            // Any
            yield return new object[] { "123", 0, 2, NumberStyles.Any, null, (IntPtr)12 };

            // AllowLeadingSign
            yield return new object[] { "-2147483648", 0, 10, NumberStyles.AllowLeadingSign, null, (IntPtr)(-214748364) };

            // AllowTrailingSign
            yield return new object[] { "123-", 0, 3, NumberStyles.AllowTrailingSign, null, (IntPtr)123 };

            // AllowExponent
            yield return new object[] { "1E2", 0, 1, NumberStyles.AllowExponent, null, (IntPtr)1 };
            yield return new object[] { "1E+2", 3, 1, NumberStyles.AllowExponent, null, (IntPtr)2 };
            yield return new object[] { "(1E2)", 1, 3, NumberStyles.AllowExponent | NumberStyles.AllowParentheses, null, (IntPtr)1E2 };
            yield return new object[] { "-1E2", 1, 3, NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign, null, (IntPtr)1E2 };
        }

        [Theory]
        [MemberData(nameof(Parse_ValidWithOffsetCount_TestData))]
        public static void Parse_Span_Valid(string value, int offset, int count, NumberStyles style, IFormatProvider provider, IntPtr expected)
        {
            IntPtr result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.True(IntPtr.TryParse(value.AsSpan(offset, count), out result));
                Assert.Equal(expected, result);
            }

            Assert.Equal(expected, IntPtr.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(IntPtr.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                IntPtr result;

                // Default style and provider
                if (style == NumberStyles.Integer && provider == null)
                {
                    Assert.False(IntPtr.TryParse(value.AsSpan(), out result));
                    Assert.Equal(default, result);
                }

                Assert.Throws(exceptionType, () => int.Parse(value.AsSpan(), style, provider));

                Assert.False(IntPtr.TryParse(value.AsSpan(), style, provider, out result));
                Assert.Equal(default, result);
            }
        }

        [Theory]
        [InlineData("N")]
        [InlineData("F")]
        public static void ToString_N_F_EmptyNumberGroup_Success(string specifier)
        {
            var nfi = (NumberFormatInfo)NumberFormatInfo.InvariantInfo.Clone();
            nfi.NumberGroupSizes = new int[0];
            nfi.NumberGroupSeparator = ",";
            Assert.Equal("1234", ((IntPtr)1234).ToString($"{specifier}0", nfi));
        }

        [Fact]
        public static void ToString_P_EmptyPercentGroup_Success()
        {
            var nfi = (NumberFormatInfo)NumberFormatInfo.InvariantInfo.Clone();
            nfi.PercentGroupSizes = new int[0];
            nfi.PercentSymbol = "%";
            Assert.Equal("123400 %", ((IntPtr)1234).ToString("P0", nfi));
        }

        [Fact]
        public static void ToString_C_EmptyPercentGroup_Success()
        {
            var nfi = (NumberFormatInfo)NumberFormatInfo.InvariantInfo.Clone();
            nfi.CurrencyGroupSizes = new int[0];
            nfi.CurrencySymbol = "$";
            Assert.Equal("$1234", ((IntPtr)1234).ToString("C0", nfi));
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void TryFormat(IntPtr i, string format, IFormatProvider provider, string expected)
        {
            char[] actual;
            int charsWritten;

            // Just right
            actual = new char[expected.Length];
            Assert.True(i.TryFormat(actual.AsSpan(), out charsWritten, format, provider));
            Assert.Equal(expected.Length, charsWritten);
            Assert.Equal(expected, new string(actual));

            // Longer than needed
            actual = new char[expected.Length + 1];
            Assert.True(i.TryFormat(actual.AsSpan(), out charsWritten, format, provider));
            Assert.Equal(expected.Length, charsWritten);
            Assert.Equal(expected, new string(actual, 0, charsWritten));

            // Too short
            if (expected.Length > 0)
            {
                actual = new char[expected.Length - 1];
                Assert.False(i.TryFormat(actual.AsSpan(), out charsWritten, format, provider));
                Assert.Equal(0, charsWritten);
            }

            if (format != null)
            {
                // Upper format
                actual = new char[expected.Length];
                Assert.True(i.TryFormat(actual.AsSpan(), out charsWritten, format.ToUpperInvariant(), provider));
                Assert.Equal(expected.Length, charsWritten);
                Assert.Equal(expected.ToUpperInvariant(), new string(actual));

                // Lower format
                actual = new char[expected.Length];
                Assert.True(i.TryFormat(actual.AsSpan(), out charsWritten, format.ToLowerInvariant(), provider));
                Assert.Equal(expected.Length, charsWritten);
                Assert.Equal(expected.ToLowerInvariant(), new string(actual));
            }
        }
    }
}
