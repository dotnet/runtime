// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Tests
{
    public static class UIntPtrTests
    {
        private static unsafe bool Is64Bit => sizeof(void*) == 8;

        [Fact]
        public static void Zero()
        {
            VerifyPointer(UIntPtr.Zero, 0);
        }

        [Fact]
        public static void Ctor_UInt()
        {
            uint i = 42;
            VerifyPointer(new UIntPtr(i), i);
            VerifyPointer((UIntPtr)i, i);
        }

        [ConditionalFact(nameof(Is64Bit))]
        public static void Ctor_ULong()
        {
            ulong l = 0x0fffffffffffffff;
            VerifyPointer(new UIntPtr(l), l);
            VerifyPointer((UIntPtr)l, l);
        }

        [ConditionalFact(nameof(Is64Bit))]
        public static unsafe void TestCtor_VoidPointer_ToPointer()
        {
            void* pv = new UIntPtr(42).ToPointer();

            VerifyPointer(new UIntPtr(pv), 42);
            VerifyPointer((UIntPtr)pv, 42);
        }

        [ConditionalFact(nameof(Is64Bit))]
        public static unsafe void TestSize()
        {
            Assert.Equal(sizeof(void*), UIntPtr.Size);
        }

        public static IEnumerable<object[]> Add_TestData()
        {
            yield return new object[] { new UIntPtr(42), 6, (ulong)48 };
            yield return new object[] { new UIntPtr(40), 0, (ulong)40 };
            yield return new object[] { new UIntPtr(38), -2, (ulong)36 };

            yield return new object[] { new UIntPtr(0xffffffffffffffff), 5, unchecked(0x0000000000000004) }; /// Add should not throw an OverflowException
        }

        [ConditionalTheory(nameof(Is64Bit))]
        [MemberData(nameof(Add_TestData))]
        public static void Add(UIntPtr ptr, int offset, ulong expected)
        {
            UIntPtr p1 = UIntPtr.Add(ptr, offset);
            VerifyPointer(p1, expected);

            UIntPtr p2 = ptr + offset;
            VerifyPointer(p2, expected);

            UIntPtr p3 = ptr;
            p3 += offset;
            VerifyPointer(p3, expected);
        }

        public static IEnumerable<object[]> Subtract_TestData()
        {
            yield return new object[] { new UIntPtr(42), 6, (ulong)36 };
            yield return new object[] { new UIntPtr(40), 0, (ulong)40 };
            yield return new object[] { new UIntPtr(38), -2, (ulong)40 };
        }

        [ConditionalTheory(nameof(Is64Bit))]
        [MemberData(nameof(Subtract_TestData))]
        public static void Subtract(UIntPtr ptr, int offset, ulong expected)
        {
            UIntPtr p1 = UIntPtr.Subtract(ptr, offset);
            VerifyPointer(p1, expected);

            UIntPtr p2 = ptr - offset;
            VerifyPointer(p2, expected);

            UIntPtr p3 = ptr;
            p3 -= offset;
            VerifyPointer(p3, expected);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new UIntPtr(42), new UIntPtr(42), true };
            yield return new object[] { new UIntPtr(42), new UIntPtr(43), false };
            yield return new object[] { new UIntPtr(42), 42, false };
            yield return new object[] { new UIntPtr(42), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public static void EqualsTest(UIntPtr ptr1, object obj, bool expected)
        {
            if (obj is UIntPtr)
            {
                UIntPtr ptr2 = (UIntPtr)obj;
                Assert.Equal(expected, ptr1 == ptr2);
                Assert.Equal(!expected, ptr1 != ptr2);
                Assert.Equal(expected, ptr1.GetHashCode().Equals(ptr2.GetHashCode()));

                IEquatable<UIntPtr> iEquatable = ptr1;
                Assert.Equal(expected, iEquatable.Equals((UIntPtr)obj));
            }
            Assert.Equal(expected, ptr1.Equals(obj));
            Assert.Equal(ptr1.GetHashCode(), ptr1.GetHashCode());
        }

        [ConditionalFact(nameof(Is64Bit))]
        public static unsafe void TestImplicitCast()
        {
            var ptr = new UIntPtr(42);

            uint i = (uint)ptr;
            Assert.Equal(42u, i);
            Assert.Equal(ptr, (UIntPtr)i);

            ulong l = (ulong)ptr;
            Assert.Equal(42u, l);
            Assert.Equal(ptr, (UIntPtr)l);

            void* v = (void*)ptr;
            Assert.Equal(ptr, (UIntPtr)v);

            ptr = new UIntPtr(0x7fffffffffffffff);
            Assert.Throws<OverflowException>(() => (uint)ptr);
        }

        [ConditionalFact(nameof(Is64Bit))]
        public static void GetHashCodeRespectAllBits()
        {
            var ptr1 = new UIntPtr(0x123456FFFFFFFF);
            var ptr2 = new UIntPtr(0x654321FFFFFFFF);
            Assert.NotEqual(ptr1.GetHashCode(), ptr2.GetHashCode());
        }

        private static void VerifyPointer(UIntPtr ptr, ulong expected)
        {
            Assert.Equal(expected, ptr.ToUInt64());

            uint expected32 = unchecked((uint)expected);
            if (expected32 != expected)
            {
                Assert.Throws<OverflowException>(() => ptr.ToUInt32());
                return;
            }

            Assert.Equal(expected32, ptr.ToUInt32());

            Assert.Equal(expected.ToString(), ptr.ToString());

            Assert.Equal(ptr, new UIntPtr(expected));
            Assert.True(ptr == new UIntPtr(expected));
            Assert.False(ptr != new UIntPtr(expected));

            Assert.NotEqual(ptr, new UIntPtr(expected + 1));
            Assert.False(ptr == new UIntPtr(expected + 1));
            Assert.True(ptr != new UIntPtr(expected + 1));
        }

        [Fact]
        public static void Ctor_Empty()
        {
            var i = new UIntPtr();
            Assert.Equal((UIntPtr)0, i);
        }

        [Fact]
        public static void Ctor_Value()
        {
            UIntPtr i = (UIntPtr)41;
            Assert.Equal((UIntPtr)41, i);
        }

        [Fact]
        public static void MaxValue()
        {
            Assert.Equal(UIntPtr.Size == 4 ? (UIntPtr)uint.MaxValue : (UIntPtr)ulong.MaxValue, UIntPtr.MaxValue);
        }

        [Fact]
        public static void MinValue()
        {
            Assert.Equal((UIntPtr)0, UIntPtr.MinValue);
        }

        [Theory]
        [InlineData(234u, 234u, 0)]
        [InlineData(234u, uint.MinValue, 1)]
        [InlineData(234u, 123u, 1)]
        [InlineData(234u, 456u, -1)]
        [InlineData(234u, uint.MaxValue, -1)]
        [InlineData(234u, null, 1)]
        public static void CompareTo_Other_ReturnsExpected(uint i0, object value, int expected)
        {
            var i = (UIntPtr)i0;
            if (value is uint uintValue)
            {
                var uintPtrValue = (UIntPtr)uintValue;
                Assert.Equal(expected, Math.Sign(i.CompareTo(uintPtrValue)));

                Assert.Equal(expected, Math.Sign(i.CompareTo((object)uintPtrValue)));
            }
            else
            {
                Assert.Equal(expected, Math.Sign(i.CompareTo(value)));
            }
        }

        [Theory]
        [InlineData("a")]
        [InlineData(234)]
        public static void CompareTo_ObjectNotUIntPtr_ThrowsArgumentException(object value)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => ((UIntPtr)123).CompareTo(value));
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            foreach (NumberFormatInfo defaultFormat in new[] { null, NumberFormatInfo.CurrentInfo })
            {
                foreach (string defaultSpecifier in new[] { "G", "G\0", "\0N222", "\0", "", "R" })
                {
                    yield return new object[] { (UIntPtr)0, defaultSpecifier, defaultFormat, "0" };
                    yield return new object[] { (UIntPtr)4567, defaultSpecifier, defaultFormat, "4567" };
                    yield return new object[] { UIntPtr.MaxValue, defaultSpecifier, defaultFormat, Is64Bit ? "18446744073709551615" : "4294967295" };
                }

                yield return new object[] { (UIntPtr)4567, "D", defaultFormat, "4567" };
                yield return new object[] { (UIntPtr)4567, "D18", defaultFormat, "000000000000004567" };

                yield return new object[] { (UIntPtr)0x2468, "x", defaultFormat, "2468" };
                yield return new object[] { (UIntPtr)2468, "N", defaultFormat, string.Format("{0:N}", 2468.00) };
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
            yield return new object[] { (UIntPtr)2468, "N", customFormat, "2*468~00" };
            yield return new object[] { (UIntPtr)123, "E", customFormat, "1~230000E&002" };
            yield return new object[] { (UIntPtr)123, "F", customFormat, "123~00" };
            yield return new object[] { (UIntPtr)123, "P", customFormat, "12,300.00000 @" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void ToStringTest(UIntPtr i, string format, IFormatProvider provider, string expected)
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
            UIntPtr i = (UIntPtr)123;
            Assert.Throws<FormatException>(() => i.ToString("Y")); // Invalid format
            Assert.Throws<FormatException>(() => i.ToString("Y", null)); // Invalid format
        }

        public static IEnumerable<object[]> Parse_Valid_TestData()
        {
            // Reuse all IntPtr test data that's relevant
            foreach (object[] objs in IntPtrTests.Parse_Valid_TestData())
            {
                if ((long)(IntPtr)objs[3] < 0) continue;
                var intPtr = (IntPtr)objs[3];
                yield return new object[] { objs[0], objs[1], objs[2], Unsafe.As<IntPtr, UIntPtr>(ref intPtr) };
            }

            // All lengths decimal
            {
                string s = "";
                uint result = 0;
                for (int i = 1; i <= 10; i++)
                {
                    result = (uint)(result * 10 + (i % 10));
                    s += (i % 10).ToString();
                    yield return new object[] { s, NumberStyles.Integer, null, (UIntPtr)result };
                }
            }

            // All lengths hexadecimal
            {
                string s = "";
                uint result = 0;
                for (uint i = 1; i <= 8; i++)
                {
                    result = ((result * 16) + (i % 16));
                    s += (i % 16).ToString("X");
                    yield return new object[] { s, NumberStyles.HexNumber, null, result };
                }
            }

            // And test boundary conditions for IntPtr
            yield return new object[] { Is64Bit ? "18446744073709551615" : "4294967295", NumberStyles.Integer, null, UIntPtr.MaxValue };
            yield return new object[] { Is64Bit ? "+18446744073709551615" : "+4294967295", NumberStyles.Integer, null, UIntPtr.MaxValue };
            yield return new object[] { Is64Bit ? "  +18446744073709551615  " : "  +4294967295  ", NumberStyles.Integer, null, UIntPtr.MaxValue };
            yield return new object[] { Is64Bit ? "FFFFFFFFFFFFFFFF" : "FFFFFFFF", NumberStyles.HexNumber, null, UIntPtr.MaxValue };
            yield return new object[] { Is64Bit ? "  FFFFFFFFFFFFFFFF  " : "  FFFFFFFF  ", NumberStyles.HexNumber, null, UIntPtr.MaxValue };
        }

        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse_Valid(string value, NumberStyles style, IFormatProvider provider, UIntPtr expected)
        {
            UIntPtr result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.True(UIntPtr.TryParse(value, out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, UIntPtr.Parse(value));
            }

            // Default provider
            if (provider == null)
            {
                Assert.Equal(expected, UIntPtr.Parse(value, style));

                // Substitute default NumberFormatInfo
                Assert.True(UIntPtr.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, UIntPtr.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Equal(expected, UIntPtr.Parse(value, provider));
            }

            // Full overloads
            Assert.True(UIntPtr.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, UIntPtr.Parse(value, style, provider));
        }

        public static IEnumerable<object[]> Parse_Invalid_TestData()
        {
            // > max value
            yield return new object[] { "18446744073709551616", NumberStyles.Integer, null, typeof(OverflowException) };
            yield return new object[] { "10000000000000000", NumberStyles.HexNumber, null, typeof(OverflowException) };
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            UIntPtr result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.False(UIntPtr.TryParse(value, out result));
                Assert.Equal(default, result);
                Assert.Throws(exceptionType, () => UIntPtr.Parse(value));
            }

            // Default provider
            if (provider == null)
            {
                Assert.Throws(exceptionType, () => UIntPtr.Parse(value, style));

                // Substitute default NumberFormatInfo
                Assert.False(UIntPtr.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(default, result);
                Assert.Throws(exceptionType, () => UIntPtr.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Throws(exceptionType, () => UIntPtr.Parse(value, provider));
            }

            // Full overloads
            Assert.False(UIntPtr.TryParse(value, style, provider, out result));
            Assert.Equal(default, result);
            Assert.Throws(exceptionType, () => UIntPtr.Parse(value, style, provider));
        }

        [Theory]
        [InlineData(NumberStyles.HexNumber | NumberStyles.AllowParentheses, null)]
        [InlineData(unchecked((NumberStyles)0xFFFFFC00), "style")]
        public static void TryParse_InvalidNumberStyle_ThrowsArgumentException(NumberStyles style, string paramName)
        {
            UIntPtr result = (UIntPtr)0;
            AssertExtensions.Throws<ArgumentException>(paramName, () => UIntPtr.TryParse("1", style, null, out result));
            Assert.Equal(default(UIntPtr), result);

            AssertExtensions.Throws<ArgumentException>(paramName, () => UIntPtr.Parse("1", style));
            AssertExtensions.Throws<ArgumentException>(paramName, () => UIntPtr.Parse("1", style, null));
        }

        public static IEnumerable<object[]> Parse_ValidWithOffsetCount_TestData()
        {
            foreach (object[] inputs in Parse_Valid_TestData())
            {
                yield return new object[] { inputs[0], 0, ((string)inputs[0]).Length, inputs[1], inputs[2], inputs[3] };
            }

            yield return new object[] { "123", 0, 2, NumberStyles.Integer, null, (UIntPtr)12 };
            yield return new object[] { "123", 1, 2, NumberStyles.Integer, null, (UIntPtr)23 };
            yield return new object[] { "4294967295", 0, 1, NumberStyles.Integer, null, (UIntPtr)4 };
            yield return new object[] { "4294967295", 9, 1, NumberStyles.Integer, null, (UIntPtr)5 };
            yield return new object[] { "12", 0, 1, NumberStyles.HexNumber, null, (UIntPtr)0x1 };
            yield return new object[] { "12", 1, 1, NumberStyles.HexNumber, null, (UIntPtr)0x2 };
            yield return new object[] { "$1,000", 1, 3, NumberStyles.Currency, new NumberFormatInfo() { CurrencySymbol = "$" }, (UIntPtr)10 };
        }

        [Theory]
        [MemberData(nameof(Parse_ValidWithOffsetCount_TestData))]
        public static void Parse_Span_Valid(string value, int offset, int count, NumberStyles style, IFormatProvider provider, UIntPtr expected)
        {
            UIntPtr result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.True(UIntPtr.TryParse(value.AsSpan(offset, count), out result));
                Assert.Equal(expected, result);
            }

            Assert.Equal(expected, UIntPtr.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(UIntPtr.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                UIntPtr result;

                // Default style and provider
                if (style == NumberStyles.Integer && provider == null)
                {
                    Assert.False(UIntPtr.TryParse(value.AsSpan(), out result));
                    Assert.Equal(default, result);
                }

                Assert.Throws(exceptionType, () => UIntPtr.Parse(value.AsSpan(), style, provider));

                Assert.False(UIntPtr.TryParse(value.AsSpan(), style, provider, out result));
                Assert.Equal(default, result);
            }
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void TryFormat(UIntPtr i, string format, IFormatProvider provider, string expected)
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
