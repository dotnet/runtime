// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Tests
{
    public static class IntPtrTests
    {
        private static unsafe bool Is64Bit => sizeof(void*) == 8;

        [Fact]
        public static void Zero()
        {
            Verify(nint.Zero, 0);
        }

        [Fact]
        public static void Ctor_Int()
        {
            int i = 42;
            Verify(new nint(i), i);
            Verify(i, i);

            i = -1;
            Verify(new nint(i), i);
            Verify(i, i);
        }

        [ConditionalFact(nameof(Is64Bit))]
        public static void Ctor_Long()
        {
            long l = 0x0fffffffffffffff;
            Verify(new nint(l), l);
            Verify(checked((nint)l), l);
        }

        [ConditionalFact(nameof(Is64Bit))]
        public static unsafe void Ctor_VoidPointer_ToPointer()
        {
            void* pv = new nint(42).ToPointer();
            Verify(new nint(pv), 42);
            Verify((nint)pv, 42);
        }

        [ConditionalFact(nameof(Is64Bit))]
        public static unsafe void Size()
        {
            Assert.Equal(sizeof(void*), nint.Size);
        }

        public static IEnumerable<object[]> Add_TestData()
        {
            yield return new object[] { (nint)42, 6, (long)48 };
            yield return new object[] { (nint)40, 0, (long)40 };
            yield return new object[] { (nint)38, -2, (long)36 };

            yield return new object[] { unchecked((nint)0x7fffffffffffffff), 5, unchecked((long)0x8000000000000004) }; /// Add should not throw an OverflowException
        }

        [ConditionalTheory(nameof(Is64Bit))]
        [MemberData(nameof(Add_TestData))]
        public static void Add(nint value, int offset, long expected)
        {
            MethodInfo add = typeof(nint).GetMethod("Add");

            nint result = (nint)add.Invoke(null, new object[] { value, offset });
            Verify(result, expected);

            MethodInfo opAddition = typeof(nint).GetMethod("op_Addition");

            result = (nint)opAddition.Invoke(null, new object[] { value, offset });
            Verify(result, expected);
        }

        public static IEnumerable<object[]> Subtract_TestData()
        {
            yield return new object[] { (nint)42, 6, (long)36 };
            yield return new object[] { (nint)40, 0, (long)40 };
            yield return new object[] { (nint)38, -2, (long)40 };
        }

        [ConditionalTheory(nameof(Is64Bit))]
        [MemberData(nameof(Subtract_TestData))]
        public static void Subtract(nint value, int offset, long expected)
        {
            MethodInfo subtract = typeof(nint).GetMethod("Subtract");

            nint result = (nint)subtract.Invoke(null, new object[] { value, offset });
            Verify(result, expected);

            MethodInfo opSubtraction = typeof(nint).GetMethod("op_Subtraction");

            result = (nint)opSubtraction.Invoke(null, new object[] { value, offset });
            Verify(result, expected);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { (nint)42, (nint)42, true };
            yield return new object[] { (nint)42, (nint)43, false };
            yield return new object[] { (nint)42, 42, false };
            yield return new object[] { (nint)42, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public static void EqualsTest(nint value, object obj, bool expected)
        {
            if (obj is nint other)
            {
                Assert.Equal(expected, value == other);
                Assert.Equal(!expected, value != other);
                Assert.Equal(expected, value.GetHashCode().Equals(other.GetHashCode()));

                IEquatable<nint> iEquatable = value;
                Assert.Equal(expected, iEquatable.Equals(other));
            }
            Assert.Equal(expected, value.Equals(obj));
            Assert.Equal(value.GetHashCode(), value.GetHashCode());
        }

        [ConditionalFact(nameof(Is64Bit))]
        public static unsafe void TestExplicitCast()
        {
            nint value = 42;

            MethodInfo[] methods = typeof(nint).GetMethods();

            MethodInfo opExplicitFromInt32 = typeof(nint).GetMethod("op_Explicit", new Type[] { typeof(int) });
            MethodInfo opExplicitToInt32 = methods.Single((methodInfo) => (methodInfo.Name == "op_Explicit") && (methodInfo.ReturnType == typeof(int)));

            int i = (int)opExplicitToInt32.Invoke(null, new object[] { value });
            Assert.Equal(42, i);
            Assert.Equal(value, (nint)opExplicitFromInt32.Invoke(null, new object[] { i }));

            MethodInfo opExplicitFromInt64 = typeof(nint).GetMethod("op_Explicit", new Type[] { typeof(long) });
            MethodInfo opExplicitToInt64 = methods.Single((methodInfo) => (methodInfo.Name == "op_Explicit") && (methodInfo.ReturnType == typeof(long)));

            long l = (long)opExplicitToInt64.Invoke(null, new object[] { value });
            Assert.Equal(42u, l);
            Assert.Equal(value, (nint)opExplicitFromInt64.Invoke(null, new object[] { l }));

            MethodInfo opExplicitFromPointer = typeof(nint).GetMethod("op_Explicit", new Type[] { typeof(void*) });
            MethodInfo opExplicitToPointer = methods.Single((methodInfo) => (methodInfo.Name == "op_Explicit") && (methodInfo.ReturnType == typeof(void*)));

            void* v = Pointer.Unbox(opExplicitToPointer.Invoke(null, new object[] { value }));
            Assert.Equal(value, (nint)opExplicitFromPointer.Invoke(null, new object[] { Pointer.Box(v, typeof(void*)) }));

            value = unchecked((nint)0x7fffffffffffffff);
            Exception ex = Assert.ThrowsAny<Exception>(() => opExplicitToInt32.Invoke(null, new object[] { value }));

            if (ex is TargetInvocationException)
            {
                // RyuJIT throws TargetInvocationException wrapping an OverflowException
                // while Mono directly throws the OverflowException
                ex = ex.InnerException;
            }
            Assert.IsType<OverflowException>(ex);
        }

        [ConditionalFact(nameof(Is64Bit))]
        public static void GetHashCodeRespectAllBits()
        {
            var value = unchecked((nint)0x123456FFFFFFFF);
            var other = unchecked((nint)0x654321FFFFFFFF);
            Assert.NotEqual(value.GetHashCode(), other.GetHashCode());
        }

        private static void Verify(nint value, long expected)
        {
            Assert.Equal(expected, value.ToInt64());

            int expected32 = unchecked((int)expected);
            if (expected32 != expected)
            {
                Assert.Throws<OverflowException>(() => value.ToInt32());
                return;
            }

            int i = value.ToInt32();
            Assert.Equal(expected32, value.ToInt32());

            Assert.Equal(expected.ToString(), value.ToString());
            Assert.Equal(nint.Size == 4 ? expected32.ToString("x") : expected.ToString("x"), value.ToString("x"));

            Assert.Equal(value, checked((nint)expected));
            Assert.True(value == checked((nint)expected));
            Assert.False(value != checked((nint)expected));

            Assert.NotEqual(value, checked((nint)expected + 1));
            Assert.False(value == checked((nint)expected + 1));
            Assert.True(value != checked((nint)expected + 1));
        }


        public static nint RealMax => Is64Bit ? unchecked((nint)long.MaxValue) : int.MaxValue;
        public static nint RealMin => Is64Bit ? unchecked((nint)long.MinValue) : int.MinValue;

        [Fact]
        public static void Ctor_Empty()
        {
            var i = new nint();
            Assert.Equal(default, i);
        }

        [Fact]
        public static void Ctor_Value()
        {
            nint i = 41;
            Assert.Equal(041, i);
        }

        [Fact]
        public static void MaxValue()
        {
            Assert.Equal(RealMax, nint.MaxValue);
        }

        [Fact]
        public static void MinValue()
        {
            Assert.Equal(RealMin, nint.MinValue);
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
            nint i = l;
            if (value is int intValue)
            {
                nint intPtrValue = intValue;
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
            AssertExtensions.Throws<ArgumentException>(null, () => ((nint)123).CompareTo(value));
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            foreach (NumberFormatInfo defaultFormat in new[] { null, NumberFormatInfo.CurrentInfo })
            {
                foreach (string defaultSpecifier in new[] { "G", "G\0", "\0N222", "\0", "", "R" })
                {
                    yield return new object[] { nint.MinValue, defaultSpecifier, defaultFormat, Is64Bit ? "-9223372036854775808" : "-2147483648" };
                    yield return new object[] { (nint)(-4567), defaultSpecifier, defaultFormat, "-4567" };
                    yield return new object[] { (nint)0, defaultSpecifier, defaultFormat, "0" };
                    yield return new object[] { (nint)4567, defaultSpecifier, defaultFormat, "4567" };
                    yield return new object[] { nint.MaxValue, defaultSpecifier, defaultFormat, Is64Bit ? "9223372036854775807" : "2147483647" };
                }

                yield return new object[] { (nint)4567, "D", defaultFormat, "4567" };
                yield return new object[] { (nint)4567, "D99", defaultFormat, "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004567" };
                yield return new object[] { (nint)4567, "D99\09", defaultFormat, "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004567" };
                yield return new object[] { (nint)(-4567), "D99\09", defaultFormat, "-000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004567" };

                yield return new object[] { (nint)0x2468, "x", defaultFormat, "2468" };
                yield return new object[] { (nint)(-0x2468), "x", defaultFormat, Is64Bit ? "ffffffffffffdb98" : "ffffdb98" };
                yield return new object[] { (nint)2468, "N", defaultFormat, string.Format("{0:N}", 2468.00) };
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
            yield return new object[] { (nint)(-2468), "N", customFormat, "#2*468~00" };
            yield return new object[] { (nint)2468, "N", customFormat, "2*468~00" };
            yield return new object[] { (nint)123, "E", customFormat, "1~230000E&002" };
            yield return new object[] { (nint)123, "F", customFormat, "123~00" };
            yield return new object[] { (nint)123, "P", customFormat, "12,300.00000 @" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void ToStringTest(nint i, string format, IFormatProvider provider, string expected)
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
            nint i = 123;
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
            yield return new object[] { "0", NumberStyles.None, null, (nint)0 };
            yield return new object[] { "0000000000000000000000000000000000000000000000000000000000", NumberStyles.None, null, (nint)0 };
            yield return new object[] { "0000000000000000000000000000000000000000000000000000000001", NumberStyles.None, null, (nint)1 };
            yield return new object[] { "2147483647", NumberStyles.None, null, (nint)2147483647 };
            yield return new object[] { "02147483647", NumberStyles.None, null, (nint)2147483647 };
            yield return new object[] { "00000000000000000000000000000000000000000000000002147483647", NumberStyles.None, null, (nint)2147483647 };
            yield return new object[] { "123\0\0", NumberStyles.None, null, (nint)123 };

            // All lengths decimal
            foreach (bool neg in new[] { false, true })
            {
                string s = neg ? "-" : "";
                int result = 0;
                for (int i = 1; i <= 10; i++)
                {
                    result = result * 10 + (i % 10);
                    s += (i % 10).ToString();
                    yield return new object[] { s, NumberStyles.Integer, null, (nint)(neg ? result * -1 : result) };
                }
            }

            // All lengths hexadecimal
            {
                string s = "";
                int result = 0;
                for (int i = 1; i <= 8; i++)
                {
                    result = (result * 16) + (i % 16);
                    s += (i % 16).ToString("X");
                    yield return new object[] { s, NumberStyles.HexNumber, null, (nint)result };
                }
            }

            // HexNumber
            yield return new object[] { "123", NumberStyles.HexNumber, null, (nint)0x123 };
            yield return new object[] { "abc", NumberStyles.HexNumber, null, (nint)0xabc };
            yield return new object[] { "ABC", NumberStyles.HexNumber, null, (nint)0xabc };
            yield return new object[] { "12", NumberStyles.HexNumber, null, (nint)0x12 };


            if (Is64Bit)
            {
                yield return new object[] { "8000000000000000", NumberStyles.HexNumber, null, nint.MinValue };
                yield return new object[] { "7FFFFFFFFFFFFFFF", NumberStyles.HexNumber, null, nint.MaxValue };
            }
            else
            {
                yield return new object[] { "80000000", NumberStyles.HexNumber, null, nint.MinValue };
                yield return new object[] { "7FFFFFFF", NumberStyles.HexNumber, null, nint.MaxValue };
            }

            // Currency
            NumberFormatInfo currencyFormat = new NumberFormatInfo()
            {
                CurrencySymbol = "$",
                CurrencyGroupSeparator = "|",
                NumberGroupSeparator = "/"
            };
            yield return new object[] { "$1|000", NumberStyles.Currency, currencyFormat, (nint)1000 };
            yield return new object[] { "$1000", NumberStyles.Currency, currencyFormat, (nint)1000 };
            yield return new object[] { "$   1000", NumberStyles.Currency, currencyFormat, (nint)1000 };
            yield return new object[] { "1000", NumberStyles.Currency, currencyFormat, (nint)1000 };
            yield return new object[] { "$(1000)", NumberStyles.Currency, currencyFormat, (nint)(-1000) };
            yield return new object[] { "($1000)", NumberStyles.Currency, currencyFormat, (nint)(-1000) };
            yield return new object[] { "$-1000", NumberStyles.Currency, currencyFormat, (nint)(-1000) };
            yield return new object[] { "-$1000", NumberStyles.Currency, currencyFormat, (nint)(-1000) };

            NumberFormatInfo emptyCurrencyFormat = new NumberFormatInfo() { CurrencySymbol = "" };
            yield return new object[] { "100", NumberStyles.Currency, emptyCurrencyFormat, (nint)100 };

            // If CurrencySymbol and Negative are the same, NegativeSign is preferred
            NumberFormatInfo sameCurrencyNegativeSignFormat = new NumberFormatInfo()
            {
                NegativeSign = "|",
                CurrencySymbol = "|"
            };
            yield return new object[] { "|1000", NumberStyles.AllowCurrencySymbol | NumberStyles.AllowLeadingSign, sameCurrencyNegativeSignFormat, (nint)(-1000) };

            // Any
            yield return new object[] { "123", NumberStyles.Any, null, (nint)123 };

            // AllowLeadingSign
            yield return new object[] { "-2147483648", NumberStyles.AllowLeadingSign, null, (nint)(-2147483648) };
            yield return new object[] { "-123", NumberStyles.AllowLeadingSign, null, (nint)(-123) };
            yield return new object[] { "+0", NumberStyles.AllowLeadingSign, null, (nint)0 };
            yield return new object[] { "-0", NumberStyles.AllowLeadingSign, null, (nint)0 };
            yield return new object[] { "+123", NumberStyles.AllowLeadingSign, null, (nint)123 };

            // If PositiveSign and NegativeSign are the same, PositiveSign is preferred
            yield return new object[] { "|123", NumberStyles.AllowLeadingSign, samePositiveNegativeFormat, (nint)123 };

            // Empty PositiveSign or NegativeSign
            yield return new object[] { "100", NumberStyles.AllowLeadingSign, emptyPositiveFormat, (nint)100 };
            yield return new object[] { "100", NumberStyles.AllowLeadingSign, emptyNegativeFormat, (nint)100 };

            // AllowTrailingSign
            yield return new object[] { "123", NumberStyles.AllowTrailingSign, null, (nint)123 };
            yield return new object[] { "123+", NumberStyles.AllowTrailingSign, null, (nint)123 };
            yield return new object[] { "123-", NumberStyles.AllowTrailingSign, null, (nint)(-123) };

            // If PositiveSign and NegativeSign are the same, PositiveSign is preferred
            yield return new object[] { "123|", NumberStyles.AllowTrailingSign, samePositiveNegativeFormat, (nint)123 };

            // Empty PositiveSign or NegativeSign
            yield return new object[] { "100", NumberStyles.AllowTrailingSign, emptyPositiveFormat, (nint)100 };
            yield return new object[] { "100", NumberStyles.AllowTrailingSign, emptyNegativeFormat, (nint)100 };

            // AllowLeadingWhite and AllowTrailingWhite
            yield return new object[] { "  123", NumberStyles.AllowLeadingWhite, null, (nint)123 };
            yield return new object[] { "  123  ", NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, null, (nint)123 };
            yield return new object[] { "123  ", NumberStyles.AllowTrailingWhite, null, (nint)123 };
            yield return new object[] { "123  \0\0", NumberStyles.AllowTrailingWhite, null, (nint)123 };
            yield return new object[] { "   2147483647   ", NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, null, (nint)2147483647 };
            yield return new object[] { "   -2147483648   ", NumberStyles.Integer, null, (nint)(-2147483648) };
            foreach (char c in new[] { (char)0x9, (char)0xA, (char)0xB, (char)0xC, (char)0xD })
            {
                string cs = c.ToString();
                yield return new object[] { cs + cs + "123" + cs + cs, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, null, (nint)123 };
            }
            yield return new object[] { "  0  ", NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, null, (nint)0 };
            yield return new object[] { "  000000000  ", NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, null, (nint)0 };

            // AllowThousands
            NumberFormatInfo thousandsFormat = new NumberFormatInfo() { NumberGroupSeparator = "|" };
            yield return new object[] { "1000", NumberStyles.AllowThousands, thousandsFormat, (nint)1000 };
            yield return new object[] { "1|0|0|0", NumberStyles.AllowThousands, thousandsFormat, (nint)1000 };
            yield return new object[] { "1|||", NumberStyles.AllowThousands, thousandsFormat, (nint)1 };

            NumberFormatInfo IntegerNumberSeparatorFormat = new NumberFormatInfo() { NumberGroupSeparator = "1" };
            yield return new object[] { "1111", NumberStyles.AllowThousands, IntegerNumberSeparatorFormat, (nint)1111 };

            // AllowExponent
            yield return new object[] { "1E2", NumberStyles.AllowExponent, null, (nint)100 };
            yield return new object[] { "1E+2", NumberStyles.AllowExponent, null, (nint)100 };
            yield return new object[] { "1e2", NumberStyles.AllowExponent, null, (nint)100 };
            yield return new object[] { "1E0", NumberStyles.AllowExponent, null, (nint)1 };
            yield return new object[] { "(1E2)", NumberStyles.AllowExponent | NumberStyles.AllowParentheses, null, (nint)(-100) };
            yield return new object[] { "-1E2", NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign, null, (nint)(-100) };

            NumberFormatInfo negativeFormat = new NumberFormatInfo() { PositiveSign = "|" };
            yield return new object[] { "1E|2", NumberStyles.AllowExponent, negativeFormat, (nint)100 };

            // AllowParentheses
            yield return new object[] { "123", NumberStyles.AllowParentheses, null, (nint)123 };
            yield return new object[] { "(123)", NumberStyles.AllowParentheses, null, (nint)(-123) };

            // AllowDecimalPoint
            NumberFormatInfo decimalFormat = new NumberFormatInfo() { NumberDecimalSeparator = "|" };
            yield return new object[] { "67|", NumberStyles.AllowDecimalPoint, decimalFormat, (nint)67 };

            // NumberFormatInfo has a custom property with length > (nint)1
            NumberFormatInfo IntegerCurrencyFormat = new NumberFormatInfo() { CurrencySymbol = "123" };
            yield return new object[] { "123123", NumberStyles.AllowCurrencySymbol, IntegerCurrencyFormat, (nint)123 };

            yield return new object[] { "123123", NumberStyles.AllowLeadingSign, new NumberFormatInfo() { PositiveSign = "1" }, (nint)23123 };
            yield return new object[] { "123123", NumberStyles.AllowLeadingSign, new NumberFormatInfo() { NegativeSign = "1" }, (nint)(-23123) };
            yield return new object[] { "123123", NumberStyles.AllowLeadingSign, new NumberFormatInfo() { PositiveSign = "123" }, (nint)123 };
            yield return new object[] { "123123", NumberStyles.AllowLeadingSign, new NumberFormatInfo() { NegativeSign = "123" }, (nint)(-123) };
            yield return new object[] { "123123", NumberStyles.AllowLeadingSign, new NumberFormatInfo() { PositiveSign = "12312" }, (nint)3 };
            yield return new object[] { "123123", NumberStyles.AllowLeadingSign, new NumberFormatInfo() { NegativeSign = "12312" }, (nint)(-3) };
        }

        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse_Valid(string value, NumberStyles style, IFormatProvider provider, nint expected)
        {
            nint result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.True(nint.TryParse(value, out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, nint.Parse(value));
            }

            // Default provider
            if (provider == null)
            {
                Assert.Equal(expected, nint.Parse(value, style));

                // Substitute default NumberFormatInfo
                Assert.True(nint.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, nint.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Equal(expected, nint.Parse(value, provider));
            }

            // Full overloads
            Assert.True(nint.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, nint.Parse(value, style, provider));
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

            // String has internal zeros
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

            // NumberFormatInfo has a custom property with length > (nint)1
            NumberFormatInfo IntegerCurrencyFormat = new NumberFormatInfo() { CurrencySymbol = "123" };
            yield return new object[] { "123", NumberStyles.AllowCurrencySymbol, IntegerCurrencyFormat, typeof(FormatException) };
            yield return new object[] { "123", NumberStyles.AllowLeadingSign, new NumberFormatInfo() { PositiveSign = "123" }, typeof(FormatException) };
            yield return new object[] { "123", NumberStyles.AllowLeadingSign, new NumberFormatInfo() { NegativeSign = "123" }, typeof(FormatException) };

            // Decimals not in range of Int32
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

            // Hexadecimals not in range of Int32
            foreach (string s in new[]
            {
                "10000000000000000", // ulong.MaxValue + (nint)1
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
            nint result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.False(nint.TryParse(value, out result));
                Assert.Equal(default, result);
                Assert.Throws(exceptionType, () => nint.Parse(value));
            }

            // Default provider
            if (provider == null)
            {
                Assert.Throws(exceptionType, () => nint.Parse(value, style));

                // Substitute default NumberFormatInfo
                Assert.False(nint.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(default, result);
                Assert.Throws(exceptionType, () => nint.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Throws(exceptionType, () => nint.Parse(value, provider));
            }

            // Full overloads
            Assert.False(nint.TryParse(value, style, provider, out result));
            Assert.Equal(default, result);
            Assert.Throws(exceptionType, () => nint.Parse(value, style, provider));
        }

        [Theory]
        [InlineData(NumberStyles.HexNumber | NumberStyles.AllowParentheses, null)]
        [InlineData(unchecked((NumberStyles)0xFFFFFC00), "style")]
        public static void TryParse_InvalidNumberStyle_ThrowsArgumentException(NumberStyles style, string paramName)
        {
            nint result = (nint)0;
            AssertExtensions.Throws<ArgumentException>(paramName, () => nint.TryParse("1", style, null, out result));
            Assert.Equal(default(nint), result);

            AssertExtensions.Throws<ArgumentException>(paramName, () => nint.Parse("1", style));
            AssertExtensions.Throws<ArgumentException>(paramName, () => nint.Parse("1", style, null));
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
            yield return new object[] { "2147483647", 1, 9, NumberStyles.None, null, (nint)147483647 };
            yield return new object[] { "2147483647", 1, 1, NumberStyles.None, null, (nint)1 };
            yield return new object[] { "123\0\0", 2, 2, NumberStyles.None, null, (nint)3 };

            // Hex
            yield return new object[] { "abc", 0, 1, NumberStyles.HexNumber, null, (nint)0xa };
            yield return new object[] { "ABC", 1, 1, NumberStyles.HexNumber, null, (nint)0xB };
            yield return new object[] { "FFFFFFFF", 6, 2, NumberStyles.HexNumber, null, (nint)0xFF };
            yield return new object[] { "FFFFFFFF", 0, 1, NumberStyles.HexNumber, null, (nint)0xF };

            // Currency
            yield return new object[] { "-$1000", 1, 5, NumberStyles.Currency, new NumberFormatInfo()
            {
                CurrencySymbol = "$",
                CurrencyGroupSeparator = "|",
                NumberGroupSeparator = "/"
            }, (nint)1000 };

            NumberFormatInfo emptyCurrencyFormat = new NumberFormatInfo() { CurrencySymbol = "" };
            yield return new object[] { "100", 1, 2, NumberStyles.Currency, emptyCurrencyFormat, (nint)0 };
            yield return new object[] { "100", 0, 1, NumberStyles.Currency, emptyCurrencyFormat, (nint)1 };

            // If CurrencySymbol and Negative are the same, NegativeSign is preferred
            NumberFormatInfo sameCurrencyNegativeSignFormat = new NumberFormatInfo()
            {
                NegativeSign = "|",
                CurrencySymbol = "|"
            };
            yield return new object[] { "1000", 1, 3, NumberStyles.AllowCurrencySymbol | NumberStyles.AllowLeadingSign, sameCurrencyNegativeSignFormat, (nint)0 };
            yield return new object[] { "|1000", 0, 2, NumberStyles.AllowCurrencySymbol | NumberStyles.AllowLeadingSign, sameCurrencyNegativeSignFormat, (nint)(-1) };

            // Any
            yield return new object[] { "123", 0, 2, NumberStyles.Any, null, (nint)12 };

            // AllowLeadingSign
            yield return new object[] { "-2147483648", 0, 10, NumberStyles.AllowLeadingSign, null, (nint)(-214748364) };

            // AllowTrailingSign
            yield return new object[] { "123-", 0, 3, NumberStyles.AllowTrailingSign, null, (nint)123 };

            // AllowExponent
            yield return new object[] { "1E2", 0, 1, NumberStyles.AllowExponent, null, (nint)1 };
            yield return new object[] { "1E+2", 3, 1, NumberStyles.AllowExponent, null, (nint)2 };
            yield return new object[] { "(1E2)", 1, 3, NumberStyles.AllowExponent | NumberStyles.AllowParentheses, null, (nint)1E2 };
            yield return new object[] { "-1E2", 1, 3, NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign, null, (nint)1E2 };
        }

        [Theory]
        [MemberData(nameof(Parse_ValidWithOffsetCount_TestData))]
        public static void Parse_Span_Valid(string value, int offset, int count, NumberStyles style, IFormatProvider provider, nint expected)
        {
            nint result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.True(nint.TryParse(value.AsSpan(offset, count), out result));
                Assert.Equal(expected, result);
            }

            Assert.Equal(expected, nint.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(nint.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                nint result;

                // Default style and provider
                if (style == NumberStyles.Integer && provider == null)
                {
                    Assert.False(nint.TryParse(value.AsSpan(), out result));
                    Assert.Equal(default, result);
                }

                Assert.Throws(exceptionType, () => int.Parse(value.AsSpan(), style, provider));

                Assert.False(nint.TryParse(value.AsSpan(), style, provider, out result));
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
            Assert.Equal("1234", ((nint)1234).ToString($"{specifier}0", nfi));
        }

        [Fact]
        public static void ToString_P_EmptyPercentGroup_Success()
        {
            var nfi = (NumberFormatInfo)NumberFormatInfo.InvariantInfo.Clone();
            nfi.PercentGroupSizes = new int[0];
            nfi.PercentSymbol = "%";
            Assert.Equal("123400 %", ((nint)1234).ToString("P0", nfi));
        }

        [Fact]
        public static void ToString_C_EmptyPercentGroup_Success()
        {
            var nfi = (NumberFormatInfo)NumberFormatInfo.InvariantInfo.Clone();
            nfi.CurrencyGroupSizes = new int[0];
            nfi.CurrencySymbol = "$";
            Assert.Equal("$1234", ((nint)1234).ToString("C0", nfi));
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void TryFormat(nint i, string format, IFormatProvider provider, string expected)
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
