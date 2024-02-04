// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit;

namespace System.Tests
{
    public static class UIntPtrTests
    {
        private static unsafe bool Is64Bit => sizeof(void*) == 8;

        [Fact]
        public static void Zero()
        {
            VerifyPointer(nuint.Zero, 0);
        }

        [Fact]
        public static void Ctor_UInt()
        {
            uint i = 42;
            VerifyPointer(new nuint(i), i);
            VerifyPointer(i, i);
        }

        [ConditionalFact(nameof(Is64Bit))]
        public static void Ctor_ULong()
        {
            ulong l = 0x0fffffffffffffff;
            VerifyPointer(new nuint(l), l);
            VerifyPointer((nuint)l, l);
        }

        [ConditionalFact(nameof(Is64Bit))]
        public static unsafe void TestCtor_VoidPointer_ToPointer()
        {
            void* pv = new nuint(42).ToPointer();

            VerifyPointer(new nuint(pv), 42);
            VerifyPointer((nuint)pv, 42);
        }

        [ConditionalFact(nameof(Is64Bit))]
        public static unsafe void TestSize()
        {
            Assert.Equal(sizeof(void*), nuint.Size);
        }

        public static IEnumerable<object[]> Add_TestData()
        {
            yield return new object[] { (nuint)42, 6, (ulong)48 };
            yield return new object[] { (nuint)40, 0, (ulong)40 };
            yield return new object[] { (nuint)38, -2, (ulong)36 };

            yield return new object[] { unchecked((nuint)0xffffffffffffffff), 5, unchecked(0x0000000000000004) }; /// Add should not throw an OverflowException
        }

        [ConditionalTheory(nameof(Is64Bit))]
        [MemberData(nameof(Add_TestData))]
        public static void Add(nuint value, int offset, ulong expected)
        {
            MethodInfo add = typeof(nuint).GetMethod("Add");

            nuint result = (nuint)add.Invoke(null, new object[] { value, offset });
            VerifyPointer(result, expected);

            MethodInfo opAddition = typeof(nuint).GetMethod("op_Addition");

            result = (nuint)opAddition.Invoke(null, new object[] { value, offset });
            VerifyPointer(result, expected);
        }

        public static IEnumerable<object[]> Subtract_TestData()
        {
            yield return new object[] { (nuint)42, 6, (ulong)36 };
            yield return new object[] { (nuint)40, 0, (ulong)40 };
            yield return new object[] { (nuint)38, -2, (ulong)40 };
        }

        [ConditionalTheory(nameof(Is64Bit))]
        [MemberData(nameof(Subtract_TestData))]
        public static void Subtract(nuint value, int offset, ulong expected)
        {
            MethodInfo subtract = typeof(nuint).GetMethod("Subtract");

            nuint result = (nuint)subtract.Invoke(null, new object[] { value, offset });
            VerifyPointer(result, expected);

            MethodInfo opSubtraction = typeof(nuint).GetMethod("op_Subtraction");

            result = (nuint)opSubtraction.Invoke(null, new object[] { value, offset });
            VerifyPointer(result, expected);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { (nuint)42, (nuint)42, true };
            yield return new object[] { (nuint)42, (nuint)43, false };
            yield return new object[] { (nuint)42, 42, false };
            yield return new object[] { (nuint)42, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public static void EqualsTest(nuint value, object obj, bool expected)
        {
            if (obj is nuint other)
            {
                Assert.Equal(expected, value == other);
                Assert.Equal(!expected, value != other);
                Assert.Equal(expected, value.GetHashCode().Equals(other.GetHashCode()));

                IEquatable<nuint> iEquatable = value;
                Assert.Equal(expected, iEquatable.Equals((nuint)obj));
            }
            Assert.Equal(expected, value.Equals(obj));
            Assert.Equal(value.GetHashCode(), value.GetHashCode());
        }

        [ConditionalFact(nameof(Is64Bit))]
        public static unsafe void TestExplicitCast()
        {
            nuint value = 42u;

            MethodInfo[] methods = typeof(nuint).GetMethods();

            MethodInfo opExplicitFromUInt32 = typeof(nuint).GetMethod("op_Explicit", new Type[] { typeof(uint) });
            MethodInfo opExplicitToUInt32 = methods.Single((methodInfo) => (methodInfo.Name == "op_Explicit") && (methodInfo.ReturnType == typeof(uint)));

            uint i = (uint)opExplicitToUInt32.Invoke(null, new object[] { value });
            Assert.Equal(42u, i);
            Assert.Equal(value, (nuint)opExplicitFromUInt32.Invoke(null, new object[] { i }));

            MethodInfo opExplicitFromUInt64 = typeof(nuint).GetMethod("op_Explicit", new Type[] { typeof(ulong) });
            MethodInfo opExplicitToUInt64 = methods.Single((methodInfo) => (methodInfo.Name == "op_Explicit") && (methodInfo.ReturnType == typeof(ulong)));

            ulong l = (ulong)opExplicitToUInt64.Invoke(null, new object[] { value });
            Assert.Equal(42u, l);
            Assert.Equal(value, (nuint)opExplicitFromUInt64.Invoke(null, new object[] { l }));

            MethodInfo opExplicitFromPointer = typeof(nuint).GetMethod("op_Explicit", new Type[] { typeof(void*) });
            MethodInfo opExplicitToPointer = methods.Single((methodInfo) => (methodInfo.Name == "op_Explicit") && (methodInfo.ReturnType == typeof(void*)));

            void* v = Pointer.Unbox(opExplicitToPointer.Invoke(null, new object[] { value }));
            Assert.Equal(value, (nuint)opExplicitFromPointer.Invoke(null, new object[] { Pointer.Box(v, typeof(void*)) }));

            value = unchecked((nuint)0x7fffffffffffffff);
            Exception ex = Assert.ThrowsAny<Exception>(() => opExplicitToUInt32.Invoke(null, new object[] { value }));


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
            var value = unchecked((nuint)0x123456FFFFFFFF);
            var other = unchecked((nuint)0x654321FFFFFFFF);
            Assert.NotEqual(value.GetHashCode(), other.GetHashCode());
        }

        private static void VerifyPointer(nuint value, ulong expected)
        {
            Assert.Equal(expected, value.ToUInt64());

            uint expected32 = unchecked((uint)expected);
            if (expected32 != expected)
            {
                Assert.Throws<OverflowException>(() => value.ToUInt32());
                return;
            }

            Assert.Equal(expected32, value.ToUInt32());

            Assert.Equal(expected.ToString(), value.ToString());

            Assert.Equal(value, checked((nuint)expected));
            Assert.True(value == checked((nuint)expected));
            Assert.False(value != checked((nuint)expected));

            Assert.NotEqual(value, checked((nuint)expected + 1));
            Assert.False(value == checked((nuint)expected + 1));
            Assert.True(value != checked((nuint)expected + 1));
        }

        public static nuint RealMax => Is64Bit ? unchecked((nuint)ulong.MaxValue) : uint.MaxValue;
        public static nuint RealMin => Is64Bit ? unchecked((nuint)ulong.MinValue) : uint.MinValue;

        [Fact]
        public static void Ctor_Empty()
        {
            var i = new nuint();
            Assert.Equal((nuint)0, i);
        }

        [Fact]
        public static void Ctor_Value()
        {
            nuint i = 41;
            Assert.Equal((nuint)41, i);
        }

        [Fact]
        public static void MaxValue()
        {
            Assert.Equal(RealMax, nuint.MaxValue);
        }

        [Fact]
        public static void MinValue()
        {
            Assert.Equal(RealMin, nuint.MinValue);
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
            nuint i = i0;
            if (value is uint uintValue)
            {
                nuint uintPtrValue = uintValue;
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
        public static void CompareTo_ObjectNotnuint_ThrowsArgumentException(object value)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => ((nuint)123).CompareTo(value));
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            foreach (NumberFormatInfo defaultFormat in new[] { null, NumberFormatInfo.CurrentInfo })
            {
                foreach (string defaultSpecifier in new[] { "G", "G\0", "\0N222", "\0", "", "R" })
                {
                    yield return new object[] { (nuint)0, defaultSpecifier, defaultFormat, "0" };
                    yield return new object[] { (nuint)4567, defaultSpecifier, defaultFormat, "4567" };
                    yield return new object[] { nuint.MaxValue, defaultSpecifier, defaultFormat, Is64Bit ? "18446744073709551615" : "4294967295" };
                }

                yield return new object[] { (nuint)4567, "D", defaultFormat, "4567" };
                yield return new object[] { (nuint)4567, "D18", defaultFormat, "000000000000004567" };

                yield return new object[] { (nuint)0, "x", defaultFormat, "0" };
                yield return new object[] { (nuint)0x2468, "x", defaultFormat, "2468" };

                yield return new object[] { (nuint)0, "b", defaultFormat, "0" };
                yield return new object[] { (nuint)0x2468, "b", defaultFormat, "10010001101000" };

                yield return new object[] { (nuint)2468, "N", defaultFormat, string.Format("{0:N}", 2468.00) };
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
            yield return new object[] { (nuint)2468, "N", customFormat, "2*468~00" };
            yield return new object[] { (nuint)123, "E", customFormat, "1~230000E&002" };
            yield return new object[] { (nuint)123, "F", customFormat, "123~00" };
            yield return new object[] { (nuint)123, "P", customFormat, "12,300.00000 @" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void ToStringTest(nuint i, string format, IFormatProvider provider, string expected)
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
            nuint i = 123;
            Assert.Throws<FormatException>(() => i.ToString("Y")); // Invalid format
            Assert.Throws<FormatException>(() => i.ToString("Y", null)); // Invalid format
        }

        public static IEnumerable<object[]> Parse_Valid_TestData()
        {
            // Reuse all nint test data that's relevant
            foreach (object[] objs in IntPtrTests.Parse_Valid_TestData())
            {
                if ((long)(nint)objs[3] < 0) continue;
                var intPtr = (nint)objs[3];
                yield return new object[] { objs[0], objs[1], objs[2], Unsafe.As<nint, nuint>(ref intPtr) };
            }

            // All lengths decimal
            {
                string s = "";
                nuint result = 0;
                for (nuint i = 1; i <= (nuint)(IntPtr.Size == 8 ? 20 : 10); i++)
                {
                    result = result * 10 + (i % 10);
                    s += (i % 10).ToString();
                    yield return new object[] { s, NumberStyles.Integer, null, result };
                }
            }

            // All lengths hexadecimal
            {
                string s = "";
                nuint result = 0;
                for (nuint i = 1; i <= (nuint)(IntPtr.Size * 2); i++)
                {
                    result = ((result * 16) + (i % 16));
                    s += (i % 16).ToString("X");
                    yield return new object[] { s, NumberStyles.HexNumber, null, result };
                }
            }

            // All lengths binary
            {
                string s = "";
                nuint result = 0;
                for (nuint i = 1; i <= (nuint)(IntPtr.Size * 8); i++)
                {
                    result = ((result * 2) + (i % 2));
                    s += (i % 2).ToString("b");
                    yield return new object[] { s, NumberStyles.BinaryNumber, null, result };
                }
            }

            // And test boundary conditions for nuint
            yield return new object[] { Is64Bit ? "18446744073709551615" : "4294967295", NumberStyles.Integer, null, nuint.MaxValue };
            yield return new object[] { Is64Bit ? "+18446744073709551615" : "+4294967295", NumberStyles.Integer, null, nuint.MaxValue };
            yield return new object[] { Is64Bit ? "  +18446744073709551615  " : "  +4294967295  ", NumberStyles.Integer, null, nuint.MaxValue };
            yield return new object[] { Is64Bit ? "FFFFFFFFFFFFFFFF" : "FFFFFFFF", NumberStyles.HexNumber, null, nuint.MaxValue };
            yield return new object[] { Is64Bit ? "  FFFFFFFFFFFFFFFF  " : "  FFFFFFFF  ", NumberStyles.HexNumber, null, nuint.MaxValue };
            yield return new object[] { Is64Bit ? "1111111111111111111111111111111111111111111111111111111111111111" : "11111111111111111111111111111111", NumberStyles.BinaryNumber, null, nuint.MaxValue };
            yield return new object[] { Is64Bit ? "  1111111111111111111111111111111111111111111111111111111111111111  " : "  11111111111111111111111111111111  ", NumberStyles.BinaryNumber, null, nuint.MaxValue };
        }

        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse_Valid(string value, NumberStyles style, IFormatProvider provider, nuint expected)
        {
            nuint result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.True(nuint.TryParse(value, out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, nuint.Parse(value));
            }

            // Default provider
            if (provider == null)
            {
                Assert.Equal(expected, nuint.Parse(value, style));

                // Substitute default NumberFormatInfo
                Assert.True(nuint.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, nuint.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Equal(expected, nuint.Parse(value, provider));
            }

            // Full overloads
            Assert.True(nuint.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, nuint.Parse(value, style, provider));
        }

        public static IEnumerable<object[]> Parse_Invalid_TestData()
        {
            // > max value
            yield return new object[] { "18446744073709551616", NumberStyles.Integer, null, typeof(OverflowException) };
            yield return new object[] { IntPtr.Size == 8 ? "10000000000000000" : "100000000", NumberStyles.HexNumber, null, typeof(OverflowException) };
            yield return new object[] { IntPtr.Size == 8 ? "10000000000000000000000000000000000000000000000000000000000000000" : "100000000000000000000000000000000", NumberStyles.BinaryNumber, null, typeof(OverflowException) };
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            nuint result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.False(nuint.TryParse(value, out result));
                Assert.Equal(default, result);
                Assert.Throws(exceptionType, () => nuint.Parse(value));
            }

            // Default provider
            if (provider == null)
            {
                Assert.Throws(exceptionType, () => nuint.Parse(value, style));

                // Substitute default NumberFormatInfo
                Assert.False(nuint.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(default, result);
                Assert.Throws(exceptionType, () => nuint.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Throws(exceptionType, () => nuint.Parse(value, provider));
            }

            // Full overloads
            Assert.False(nuint.TryParse(value, style, provider, out result));
            Assert.Equal(default, result);
            Assert.Throws(exceptionType, () => nuint.Parse(value, style, provider));
        }

        [Theory]
        [InlineData(NumberStyles.HexNumber | NumberStyles.AllowParentheses)]
        [InlineData(NumberStyles.BinaryNumber | NumberStyles.AllowParentheses)]
        [InlineData(NumberStyles.HexNumber | NumberStyles.BinaryNumber)]
        [InlineData(unchecked((NumberStyles)0xFFFFFC00))]
        public static void TryParse_InvalidNumberStyle_ThrowsArgumentException(NumberStyles style)
        {
            nuint result = 0;
            AssertExtensions.Throws<ArgumentException>("style", () => nuint.TryParse("1", style, null, out result));
            Assert.Equal(default(nuint), result);

            AssertExtensions.Throws<ArgumentException>("style", () => nuint.Parse("1", style));
            AssertExtensions.Throws<ArgumentException>("style", () => nuint.Parse("1", style, null));
        }

        public static IEnumerable<object[]> Parse_ValidWithOffsetCount_TestData()
        {
            foreach (object[] inputs in Parse_Valid_TestData())
            {
                yield return new object[] { inputs[0], 0, ((string)inputs[0]).Length, inputs[1], inputs[2], inputs[3] };
            }

            yield return new object[] { "123", 0, 2, NumberStyles.Integer, null, (nuint)12 };
            yield return new object[] { "123", 1, 2, NumberStyles.Integer, null, (nuint)23 };
            yield return new object[] { "4294967295", 0, 1, NumberStyles.Integer, null, (nuint)4 };
            yield return new object[] { "4294967295", 9, 1, NumberStyles.Integer, null, (nuint)5 };
            yield return new object[] { "12", 0, 1, NumberStyles.HexNumber, null, (nuint)0x1 };
            yield return new object[] { "12", 1, 1, NumberStyles.HexNumber, null, (nuint)0x2 };
            yield return new object[] { "01", 0, 1, NumberStyles.BinaryNumber, null, (nuint)0b0 };
            yield return new object[] { "01", 1, 1, NumberStyles.BinaryNumber, null, (nuint)0b1 };
            yield return new object[] { "$1,000", 1, 3, NumberStyles.Currency, new NumberFormatInfo() { CurrencySymbol = "$" }, (nuint)10 };
        }

        [Theory]
        [MemberData(nameof(Parse_ValidWithOffsetCount_TestData))]
        public static void Parse_Span_Valid(string value, int offset, int count, NumberStyles style, IFormatProvider provider, nuint expected)
        {
            nuint result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.True(nuint.TryParse(value.AsSpan(offset, count), out result));
                Assert.Equal(expected, result);
            }

            Assert.Equal(expected, nuint.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(nuint.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                nuint result;

                // Default style and provider
                if (style == NumberStyles.Integer && provider == null)
                {
                    Assert.False(nuint.TryParse(value.AsSpan(), out result));
                    Assert.Equal(default, result);
                }

                Assert.Throws(exceptionType, () => nuint.Parse(value.AsSpan(), style, provider));

                Assert.False(nuint.TryParse(value.AsSpan(), style, provider, out result));
                Assert.Equal(default, result);
            }
        }

        [Theory]
        [MemberData(nameof(Parse_ValidWithOffsetCount_TestData))]
        public static void Parse_Utf8Span_Valid(string value, int offset, int count, NumberStyles style, IFormatProvider provider, nuint expected)
        {
            nuint result;
            ReadOnlySpan<byte> valueUtf8 = Encoding.UTF8.GetBytes(value, offset, count);

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.True(nuint.TryParse(valueUtf8, out result));
                Assert.Equal(expected, result);
            }

            Assert.Equal(expected, nuint.Parse(valueUtf8, style, provider));

            Assert.True(nuint.TryParse(valueUtf8, style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Utf8Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                nuint result;
                ReadOnlySpan<byte> valueUtf8 = Encoding.UTF8.GetBytes(value);

                // Default style and provider
                if (style == NumberStyles.Integer && provider == null)
                {
                    Assert.False(nuint.TryParse(valueUtf8, out result));
                    Assert.Equal(default, result);
                }

                Exception e = Assert.Throws(exceptionType, () => nuint.Parse(Encoding.UTF8.GetBytes(value), style, provider));
                if (e is FormatException fe)
                {
                    Assert.Contains(value, fe.Message);
                }

                Assert.False(nuint.TryParse(valueUtf8, style, provider, out result));
                Assert.Equal(default, result);
            }
        }

        [Fact]
        public static void Parse_Utf8Span_InvalidUtf8()
        {
            FormatException fe = Assert.Throws<FormatException>(() => nuint.Parse([0xA0]));
            Assert.DoesNotContain("A0", fe.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("ReadOnlySpan", fe.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("\uFFFD", fe.Message, StringComparison.Ordinal);
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void TryFormat(nuint i, string format, IFormatProvider provider, string expected) =>
            NumberFormatTestHelper.TryFormatNumberTest(i, format, provider, expected);
    }
}
