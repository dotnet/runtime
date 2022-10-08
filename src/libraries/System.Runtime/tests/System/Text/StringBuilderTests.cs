// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Tests;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Text.Tests
{
    public partial class StringBuilderTests
    {
        private static readonly string s_chunkSplitSource = new string('a', 30);
        private static readonly string s_noCapacityParamName = "valueCount";

        private static StringBuilder StringBuilderWithMultipleChunks() => new StringBuilder(20).Append(s_chunkSplitSource);

        [Fact]
        public static void Ctor_Empty()
        {
            var builder = new StringBuilder();
            Assert.Same(string.Empty, builder.ToString());
            Assert.Equal(string.Empty, builder.ToString(0, 0));
            Assert.Equal(0, builder.Length);
            Assert.Equal(int.MaxValue, builder.MaxCapacity);
        }

        [Fact]
        public static void Ctor_Int()
        {
            var builder = new StringBuilder(42);
            Assert.Same(string.Empty, builder.ToString());
            Assert.Equal(0, builder.Length);

            Assert.True(builder.Capacity >= 42);
            Assert.Equal(int.MaxValue, builder.MaxCapacity);
        }

        [Fact]
        public static void Ctor_Int_NegativeCapacity_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => new StringBuilder(-1)); // Capacity < 0
        }

        [Fact]
        public static void Ctor_Int_Int()
        {
            // The second int parameter is MaxCapacity but in CLR4.0 and later, StringBuilder isn't required to honor it.
            var builder = new StringBuilder(42, 50);
            Assert.Equal("", builder.ToString());
            Assert.Equal(0, builder.Length);

            Assert.InRange(builder.Capacity, 42, builder.MaxCapacity);
            Assert.Equal(50, builder.MaxCapacity);
        }

        [Fact]
        public static void Ctor_Int_Int_Invalid()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => new StringBuilder(-1, 1)); // Capacity < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("maxCapacity", () => new StringBuilder(0, 0)); // MaxCapacity < 1

            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => new StringBuilder(2, 1)); // Capacity > maxCapacity
        }

        [Theory]
        [InlineData("Hello")]
        [InlineData("")]
        [InlineData(null)]
        public static void Ctor_String(string value)
        {
            var builder = new StringBuilder(value);

            string expected = value ?? "";
            Assert.Equal(expected, builder.ToString());
            Assert.Equal(expected.Length, builder.Length);
        }

        [Theory]
        [InlineData("Hello")]
        [InlineData("")]
        [InlineData(null)]
        public static void Ctor_String_Int(string value)
        {
            var builder = new StringBuilder(value, 42);

            string expected = value ?? "";
            Assert.Equal(expected, builder.ToString());
            Assert.Equal(expected.Length, builder.Length);

            Assert.True(builder.Capacity >= 42);
        }

        [Fact]
        public static void Ctor_String_Int_NegativeCapacity_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => new StringBuilder("", -1)); // Capacity < 0
        }

        [Theory]
        [InlineData("Hello", 0, 5)]
        [InlineData("Hello", 2, 3)]
        [InlineData("", 0, 0)]
        [InlineData(null, 0, 0)]
        public static void Ctor_String_Int_Int_Int(string value, int startIndex, int length)
        {
            var builder = new StringBuilder(value, startIndex, length, 42);

            string expected = value?.Substring(startIndex, length) ?? "";
            Assert.Equal(expected, builder.ToString());
            Assert.Equal(length, builder.Length);
            Assert.Equal(expected.Length, builder.Length);

            Assert.True(builder.Capacity >= 42);
        }

        [Fact]
        public static void Ctor_String_Int_Int_Int_Invalid()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => new StringBuilder("foo", -1, 0, 0)); // Start index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => new StringBuilder("foo", 0, -1, 0)); // Length < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => new StringBuilder("foo", 0, 0, -1)); // Capacity < 0

            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => new StringBuilder("foo", 4, 0, 0)); // Start index + length > builder.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => new StringBuilder("foo", 3, 1, 0)); // Start index + length > builder.Length
        }

        [Fact]
        public static void Item_Get_Set()
        {
            string s = "Hello";
            var builder = new StringBuilder(s);

            for (int i = 0; i < s.Length; i++)
            {
                Assert.Equal(s[i], builder[i]);

                char c = (char)(i + '0');
                builder[i] = c;
                Assert.Equal(c, builder[i]);
            }
            Assert.Equal("01234", builder.ToString());
        }

        [Fact]
        public static void Item_Get_Set_InvalidIndex()
        {
            var builder = new StringBuilder("Hello");

            Assert.Throws<IndexOutOfRangeException>(() => builder[-1]); // Index < 0
            Assert.Throws<IndexOutOfRangeException>(() => builder[5]); // Index >= string.Length

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder[-1] = 'a'); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder[5] = 'a'); // Index >= string.Length
        }

        [Fact]
        public static void Capacity_Get_Set()
        {
            var builder = new StringBuilder("Hello");
            Assert.True(builder.Capacity >= builder.Length);

            builder.Capacity = 10;
            Assert.True(builder.Capacity >= 10);

            builder.Capacity = 5;
            Assert.True(builder.Capacity >= 5);

            // Setting the capacity to the same value does not change anything
            int oldCapacity = builder.Capacity;
            builder.Capacity = 5;
            Assert.Equal(oldCapacity, builder.Capacity);
        }

        [Fact]
        public static void Capacity_Set_Invalid_ThrowsArgumentOutOfRangeException()
        {
            var builder = new StringBuilder(10, 10);
            builder.Append("Hello");
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => builder.Capacity = -1); // Capacity < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => builder.Capacity = builder.MaxCapacity + 1); // Capacity > builder.MaxCapacity
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => builder.Capacity = builder.Length - 1); // Capacity < builder.Length
        }

        [Fact]
        public static void Length_Get_Set()
        {
            var builder = new StringBuilder("Hello");

            builder.Length = 2;
            Assert.Equal(2, builder.Length);
            Assert.Equal("He", builder.ToString());

            builder.Length = 10;
            Assert.Equal(10, builder.Length);
            Assert.Equal("He" + new string((char)0, 8), builder.ToString());
        }

        [Fact]
        public static void Length_Set_InvalidValue_ThrowsArgumentOutOfRangeException()
        {
            var builder = new StringBuilder(10, 10);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => builder.Length = -1); // Value < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => builder.Length = builder.MaxCapacity + 1); // Value > builder.MaxCapacity
        }

        [Theory]
        [InlineData("Hello", (ushort)0, "Hello0")]
        [InlineData("Hello", (ushort)123, "Hello123")]
        [InlineData("", (ushort)456, "456")]
        public static void Append_UShort(string original, ushort value, string expected)
        {
            var builder = new StringBuilder(original);
            builder.Append(value);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Append_UShort_NoSpareCapacity_ThrowsArgumentOutOfRangeException()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => builder.Append((ushort)1));
        }

        [Theory]
        [InlineData("Hello", true, "HelloTrue")]
        [InlineData("Hello", false, "HelloFalse")]
        [InlineData("", false, "False")]
        public static void Append_Bool(string original, bool value, string expected)
        {
            var builder = new StringBuilder(original);
            builder.Append(value);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Append_Bool_NoSpareCapacity_ThrowsArgumentOutOfRangeException()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => builder.Append(true));
        }

        public static IEnumerable<object[]> Append_Decimal_TestData()
        {
            yield return new object[] { "Hello", (double)0, "Hello0" };
            yield return new object[] { "Hello", 1.23, "Hello1.23" };
            yield return new object[] { "", -4.56, "-4.56" };
        }

        [Fact]
        public static void Test_Append_Decimal()
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                foreach (var testdata in Append_Decimal_TestData())
                {
                    Append_Decimal((string)testdata[0], (double)testdata[1], (string)testdata[2]);
                }
            }
        }

        private static void Append_Decimal(string original, double doubleValue, string expected)
        {
            var builder = new StringBuilder(original);
            builder.Append(new decimal(doubleValue));
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Append_Decimal_NoSpareCapacity_ThrowsArgumentOutOfRangeException()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => builder.Append((decimal)1));
        }
        public static IEnumerable<object[]> Append_Double_TestData()
        {
            yield return new object[] { "Hello", (double)0, "Hello0" };
            yield return new object[] { "Hello", 1.23, "Hello1.23" };
            yield return new object[] { "", -4.56, "-4.56" };
        }

        [Fact]
        public static void Test_Append_Double()
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                foreach (var testdata in Append_Double_TestData())
                {
                    Append_Double((string)testdata[0], (double)testdata[1], (string)testdata[2]);
                }
            }
        }

        private static void Append_Double(string original, double value, string expected)
        {
            var builder = new StringBuilder(original);
            builder.Append(value);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Append_Double_NoSpareCapacity_ThrowsArgumentOutOfRangeException()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => builder.Append((double)1));
        }

        [Theory]
        [InlineData("Hello", (short)0, "Hello0")]
        [InlineData("Hello", (short)123, "Hello123")]
        [InlineData("", (short)-456, "-456")]
        public static void Append_Short(string original, short value, string expected)
        {
            var builder = new StringBuilder(original);
            builder.Append(value);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Append_Short_NoSpareCapacity_ThrowsArgumentOutOfRangeException()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => builder.Append((short)1));
        }

        [Theory]
        [InlineData("Hello", 0, "Hello0")]
        [InlineData("Hello", 123, "Hello123")]
        [InlineData("", -456, "-456")]
        public static void Append_Int(string original, int value, string expected)
        {
            var builder = new StringBuilder(original);
            builder.Append(value);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Append_Int_NoSpareCapacity_ThrowsArgumentOutOfRangeException()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => builder.Append(1));
        }

        [Theory]
        [InlineData("Hello", (long)0, "Hello0")]
        [InlineData("Hello", (long)123, "Hello123")]
        [InlineData("", (long)-456, "-456")]
        public static void Append_Long(string original, long value, string expected)
        {
            var builder = new StringBuilder(original);
            builder.Append(value);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Append_Long_NoSpareCapacity_ThrowsArgumentOutOfRangeException()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => builder.Append((long)1));
        }

        [Theory]
        [InlineData("Hello", "abc", "Helloabc")]
        [InlineData("Hello", "def", "Hellodef")]
        [InlineData("", "g", "g")]
        [InlineData("Hello", "", "Hello")]
        [InlineData("Hello", null, "Hello")]
        public static void Append_Object(string original, object value, string expected)
        {
            var builder = new StringBuilder(original);
            builder.Append(value);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Append_Object_NoSpareCapacity_ThrowsArgumentOutOfRangeException()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => builder.Append(new object()));
        }

        [Theory]
        [InlineData("Hello", (sbyte)0, "Hello0")]
        [InlineData("Hello", (sbyte)123, "Hello123")]
        [InlineData("", (sbyte)-123, "-123")]
        public static void Append_SByte(string original, sbyte value, string expected)
        {
            var builder = new StringBuilder(original);
            builder.Append(value);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Append_SByte_NoSpareCapacity_ThrowsArgumentOutOfRangeException()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => builder.Append((sbyte)1));
        }

        public static IEnumerable<object[]> Append_Float_TestData()
        {
            yield return new object[] { "Hello", (float)0, "Hello0" };
            yield return new object[] { "Hello", (float)1.23, "Hello1.23" };
            yield return new object[] { "", (float)-4.56, "-4.56" };
        }

        [Fact]
        public static void Test_Append_Float()
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                foreach (var testdata in Append_Float_TestData())
                {
                    Append_Float((string)testdata[0], (float)testdata[1], (string)testdata[2]);
                }
            }
        }

        private static void Append_Float(string original, float value, string expected)
        {
            var builder = new StringBuilder(original);
            builder.Append(value);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Append_Float_NoSpareCapacity_ThrowsArgumentOutOfRangeException()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => builder.Append((float)1));
        }

        [Theory]
        [InlineData("Hello", (byte)0, "Hello0")]
        [InlineData("Hello", (byte)123, "Hello123")]
        [InlineData("", (byte)123, "123")]
        public static void Append_Byte(string original, byte value, string expected)
        {
            var builder = new StringBuilder(original);
            builder.Append(value);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Append_Byte_NoSpareCapacity_ThrowsArgumentOutOfRangeException()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => builder.Append((byte)1));
        }

        [Theory]
        [InlineData("Hello", (uint)0, "Hello0")]
        [InlineData("Hello", (uint)123, "Hello123")]
        [InlineData("", (uint)456, "456")]
        public static void Append_UInt(string original, uint value, string expected)
        {
            var builder = new StringBuilder(original);
            builder.Append(value);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Append_UInt_NoSpareCapacity_ThrowsArgumentOutOfRangeException()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => builder.Append((uint)1));
        }

        [Theory]
        [InlineData("Hello", (ulong)0, "Hello0")]
        [InlineData("Hello", (ulong)123, "Hello123")]
        [InlineData("", (ulong)456, "456")]
        public static void Append_ULong(string original, ulong value, string expected)
        {
            var builder = new StringBuilder(original);
            builder.Append(value);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Append_ULong_NoSpareCapacity_ThrowsArgumentOutOfRangeException()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => builder.Append((ulong)1));
        }

        [Theory]
        [InlineData("Hello", '\0', 1, "Hello\0")]
        [InlineData("Hello", 'a', 1, "Helloa")]
        [InlineData("", 'b', 1, "b")]
        [InlineData("Hello", 'c', 2, "Hellocc")]
        [InlineData("Hello", '\0', 0, "Hello")]
        public static void Append_Char(string original, char value, int repeatCount, string expected)
        {
            StringBuilder builder;
            if (repeatCount == 1)
            {
                // Use Append(char)
                builder = new StringBuilder(original);
                builder.Append(value);
                Assert.Equal(expected, builder.ToString());
            }
            // Use Append(char, int)
            builder = new StringBuilder(original);
            builder.Append(value, repeatCount);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Append_Char_NegativeRepeatCount_ThrowsArgumentOutOfRangeException()
        {
            var builder = new StringBuilder(0, 5);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("repeatCount", () => builder.Append('a', -1));
        }

        [Fact]
        public static void Append_Char_NoSpareCapacity_ThrowsArgumentOutOfRangeException()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>("repeatCount", "requiredLength", () => builder.Append('a'));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("repeatCount", "requiredLength", () => builder.Append('a', 1));
        }

        [Theory]
        [InlineData("Hello", new char[] { 'a', 'b', 'c' }, 1, "Helloa")]
        [InlineData("Hello", new char[] { 'a', 'b', 'c' }, 2, "Helloab")]
        [InlineData("Hello", new char[] { 'a', 'b', 'c' }, 3, "Helloabc")]
        [InlineData("", new char[] { 'a' }, 1, "a")]
        [InlineData("", new char[] { 'a' }, 0, "")]
        [InlineData("Hello", new char[0], 0, "Hello")]
        [InlineData("Hello", null, 0, "Hello")]
        public static unsafe void Append_CharPointer(string original, char[] charArray, int valueCount, string expected)
        {
            _ = charArray; // https://github.com/xunit/xunit/issues/1969
            fixed (char* value = charArray)
            {
                var builder = new StringBuilder(original);
                builder.Append(value, valueCount);
                Assert.Equal(expected, builder.ToString());
            }
        }

        [Fact]
        public static unsafe void Append_CharPointer_Null_ThrowsNullReferenceException()
        {
            var builder = new StringBuilder();
            Assert.Throws<NullReferenceException>(() => builder.Append(null, 2));
        }

        [Fact]
        public static unsafe void Append_CharPointer_NegativeValueCount_ThrowsArgumentOutOfRangeException()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>("valueCount", () =>
            {
                fixed (char* value = new char[0]) { builder.Append(value, -1); }
            });
        }

        [Fact]
        public static unsafe void Append_CharPointer_NoSpareCapacity_ThrowsArgumentOutOfRangeException()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () =>
            {
                fixed (char* value = new char[] { 'a' }) { builder.Append(value, 1); }
            });
        }

        [Theory]
        [InlineData("Hello", "abc", 0, 3, "Helloabc")]
        [InlineData("Hello", "def", 1, 2, "Helloef")]
        [InlineData("Hello", "def", 2, 1, "Hellof")]
        [InlineData("", "g", 0, 1, "g")]
        [InlineData("Hello", "g", 1, 0, "Hello")]
        [InlineData("Hello", "g", 0, 0, "Hello")]
        [InlineData("Hello", "", 0, 0, "Hello")]
        [InlineData("Hello", null, 0, 0, "Hello")]
        public static void Append_String(string original, string value, int startIndex, int count, string expected)
        {
            StringBuilder builder;
            if (startIndex == 0 && count == (value?.Length ?? 0))
            {
                // Use Append(string)
                builder = new StringBuilder(original);
                builder.Append(value);
                Assert.Equal(expected, builder.ToString());
            }
            // Use Append(string, int, int)
            builder = new StringBuilder(original);
            builder.Append(value, startIndex, count);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Append_String_NullValueNonZeroStartIndexCount_ThrowsArgumentNullException()
        {
            var builder = new StringBuilder();
            AssertExtensions.Throws<ArgumentNullException>("value", () => builder.Append((string)null, 1, 1));
        }

        [Theory]
        [InlineData("", -1, 0)]
        [InlineData("hello", 5, 1)]
        [InlineData("hello", 4, 2)]
        public static void Append_String_InvalidIndexPlusCount_ThrowsArgumentOutOfRangeException(string value, int startIndex, int count)
        {
            var builder = new StringBuilder();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => builder.Append(value, startIndex, count));
        }

        [Fact]
        public static void Append_String_NegativeCount_ThrowsArgumentOutOfRangeException()
        {
            var builder = new StringBuilder();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => builder.Append("", 0, -1));
        }

        [Fact]
        public static void Append_String_NoSpareCapacity_ThrowsArgumentOutOfRangeException()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => builder.Append("a"));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => builder.Append("a", 0, 1));
        }

        [Theory]
        [InlineData("Hello", new char[] { 'a' }, 0, 1, "Helloa")]
        [InlineData("Hello", new char[] { 'b', 'c', 'd' }, 0, 3, "Hellobcd")]
        [InlineData("Hello", new char[] { 'b', 'c', 'd' }, 1, 2, "Hellocd")]
        [InlineData("Hello", new char[] { 'b', 'c', 'd' }, 2, 1, "Hellod")]
        [InlineData("", new char[] { 'e', 'f', 'g' }, 0, 3, "efg")]
        [InlineData("Hello", new char[] { 'e' }, 1, 0, "Hello")]
        [InlineData("Hello", new char[] { 'e' }, 0, 0, "Hello")]
        [InlineData("Hello", new char[0], 0, 0, "Hello")]
        [InlineData("Hello", null, 0, 0, "Hello")]
        public static void Append_CharArray(string original, char[] value, int startIndex, int charCount, string expected)
        {
            StringBuilder builder;
            if (startIndex == 0 && charCount == (value?.Length ?? 0))
            {
                // Use Append(char[])
                builder = new StringBuilder(original);
                builder.Append(value);
                Assert.Equal(expected, builder.ToString());
            }
            // Use Append(char[], int, int)
            builder = new StringBuilder(original);
            builder.Append(value, startIndex, charCount);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Append_CharArray_Invalid()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentNullException>("value", () => builder.Append((char[])null, 1, 1)); // Value is null, startIndex > 0 and count > 0

            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => builder.Append(new char[0], -1, 0)); // Start index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("charCount", () => builder.Append(new char[0], 0, -1)); // Count < 0

            AssertExtensions.Throws<ArgumentOutOfRangeException>("charCount", () => builder.Append(new char[5], 6, 0)); // Start index + count > value.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("charCount", () => builder.Append(new char[5], 5, 1)); // Start index + count > value.Length

            AssertExtensions.Throws<ArgumentOutOfRangeException>("valueCount", () => builder.Append(new char[] { 'a' })); // New length > builder.MaxCapacity
            AssertExtensions.Throws<ArgumentOutOfRangeException>("valueCount", () => builder.Append(new char[] { 'a' }, 0, 1)); // New length > builder.MaxCapacity
        }

        public static IEnumerable<object[]> AppendFormat_TestData()
        {
            yield return new object[] { "", null, "", new object[0], "" };
            yield return new object[] { "", null, ", ", new object[0], ", " };

            yield return new object[] { "Hello", null, ", Foo {0  }", new object[] { "Bar" }, "Hello, Foo Bar" }; // Ignores whitespace

            yield return new object[] { "Hello", null, ", Foo {0}", new object[] { "Bar" }, "Hello, Foo Bar" };
            yield return new object[] { "Hello", null, ", Foo {0} Baz {1}", new object[] { "Bar", "Foo" }, "Hello, Foo Bar Baz Foo" };
            yield return new object[] { "Hello", null, ", Foo {0} Baz {1} Bar {2}", new object[] { "Bar", "Foo", "Baz" }, "Hello, Foo Bar Baz Foo Bar Baz" };
            yield return new object[] { "Hello", null, ", Foo {0} Baz {1} Bar {2} Foo {3}", new object[] { "Bar", "Foo", "Baz", "Bar" }, "Hello, Foo Bar Baz Foo Bar Baz Foo Bar" };

            // Length is positive
            yield return new object[] { "Hello", null, ", Foo {0,2}", new object[] { "Bar" }, "Hello, Foo Bar" }; // MiValue's length > minimum length (so don't prepend whitespace)
            yield return new object[] { "Hello", null, ", Foo {0,3}", new object[] { "B" }, "Hello, Foo   B" }; // Value's length < minimum length (so prepend whitespace)
            yield return new object[] { "Hello", null, ", Foo {0,     3}", new object[] { "B" }, "Hello, Foo   B" }; // Same as above, but verify AppendFormat ignores whitespace
            yield return new object[] { "Hello", null, ", Foo {0,0}", new object[] { "Bar" }, "Hello, Foo Bar" }; // Minimum length is 0
            yield return new object[] { "Hello", null, ", Foo {0,  2 }", new object[] { "Bar" }, "Hello, Foo Bar" }; // whitespace before and after length

            // Length is negative
            yield return new object[] { "Hello", null, ", Foo {0,-2}", new object[] { "Bar" }, "Hello, Foo Bar" }; // Value's length > |minimum length| (so don't prepend whitespace)
            yield return new object[] { "Hello", null, ", Foo {0,-3}", new object[] { "B" }, "Hello, Foo B  " }; // Value's length < |minimum length| (so append whitespace)
            yield return new object[] { "Hello", null, ", Foo {0,     -3}", new object[] { "B" }, "Hello, Foo B  " }; // Same as above, but verify AppendFormat ignores whitespace
            yield return new object[] { "Hello", null, ", Foo {0,0}", new object[] { "Bar" }, "Hello, Foo Bar" }; // Minimum length is 0
            yield return new object[] { "Hello", null, ", Foo {0, -2  }", new object[] { "Bar" }, "Hello, Foo Bar" }; // whitespace before and after length

            yield return new object[] { "Hello", null, ", Foo {0:D6}", new object[] { 1 }, "Hello, Foo 000001" }; // Custom format
            yield return new object[] { "Hello", null, ", Foo {0     :D6}", new object[] { 1 }, "Hello, Foo 000001" }; // Custom format with ignored whitespace
            yield return new object[] { "Hello", null, ", Foo {0:}", new object[] { 1 }, "Hello, Foo 1" }; // Missing custom format

            yield return new object[] { "Hello", null, ", Foo {0,9:D6}", new object[] { 1 }, "Hello, Foo    000001" }; // Positive minimum length and custom format
            yield return new object[] { "Hello", null, ", Foo {0,-9:D6}", new object[] { 1 }, "Hello, Foo 000001   " }; // Negative length and custom format

            yield return new object[] { "Hello", null, ", Foo {{{0}", new object[] { 1 }, "Hello, Foo {1" }; // Escaped open curly braces
            yield return new object[] { "Hello", null, ", Foo }}{0}", new object[] { 1 }, "Hello, Foo }1" }; // Escaped closed curly braces
            yield return new object[] { "Hello", null, ", Foo {0} {{0}}", new object[] { 1 }, "Hello, Foo 1 {0}" }; // Escaped placeholder


            yield return new object[] { "Hello", null, ", Foo {0}", new object[] { null }, "Hello, Foo " }; // Values has null only
            yield return new object[] { "Hello", null, ", Foo {0} {1} {2}", new object[] { "Bar", null, "Baz" }, "Hello, Foo Bar  Baz" }; // Values has null

            yield return new object[] { "Hello", CultureInfo.InvariantCulture, ", Foo {0,9:D6}", new object[] { 1 }, "Hello, Foo    000001" }; // Positive minimum length, custom format and custom format provider

            yield return new object[] { "", new CustomFormatter(), "{0}", new object[] { 1.2 }, "abc" }; // Custom format provider
            yield return new object[] { "", new CustomFormatter(), "{0:0}", new object[] { 1.2 }, "abc" }; // Custom format provider

            // ISpanFormattable inputs: simple validation of known types that implement the interface
            yield return new object[] { "", CultureInfo.InvariantCulture, "{0}", new object[] { (byte)42 }, "42" };
            yield return new object[] { "", CultureInfo.InvariantCulture, "{0}", new object[] { 'A' }, "A" };
            yield return new object[] { "", CultureInfo.InvariantCulture, "{0:r}", new object[] { DateTime.ParseExact("2021-03-15T14:52:51.5058563Z", "o", null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal) }, "Mon, 15 Mar 2021 14:52:51 GMT" };
            yield return new object[] { "", CultureInfo.InvariantCulture, "{0:r}", new object[] { DateTimeOffset.ParseExact("2021-03-15T14:52:51.5058563Z", "o", null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal) }, "Mon, 15 Mar 2021 14:52:51 GMT" };
            yield return new object[] { "", CultureInfo.InvariantCulture, "{0}", new object[] { (decimal)42 }, "42" };
            yield return new object[] { "", CultureInfo.InvariantCulture, "{0}", new object[] { (double)42 }, "42" };
            yield return new object[] { "", CultureInfo.InvariantCulture, "{0}", new object[] { Guid.Parse("68d9cfaf-feab-4d5b-96d8-a3fd889ae89f") }, "68d9cfaf-feab-4d5b-96d8-a3fd889ae89f" };
            yield return new object[] { "", CultureInfo.InvariantCulture, "{0}", new object[] { (Half)42 }, "42" };
            yield return new object[] { "", CultureInfo.InvariantCulture, "{0}", new object[] { (short)42 }, "42" };
            yield return new object[] { "", CultureInfo.InvariantCulture, "{0}", new object[] { (int)42 }, "42" };
            yield return new object[] { "", CultureInfo.InvariantCulture, "{0}", new object[] { (long)42 }, "42" };
            yield return new object[] { "", CultureInfo.InvariantCulture, "{0}", new object[] { (IntPtr)42 }, "42" };
            yield return new object[] { "", CultureInfo.InvariantCulture, "{0}", new object[] { new Rune('A') }, "A" };
            yield return new object[] { "", CultureInfo.InvariantCulture, "{0}", new object[] { (sbyte)42 }, "42" };
            yield return new object[] { "", CultureInfo.InvariantCulture, "{0}", new object[] { (float)42 }, "42" };
            yield return new object[] { "", CultureInfo.InvariantCulture, "{0}", new object[] { TimeSpan.FromSeconds(42) }, "00:00:42" };
            yield return new object[] { "", CultureInfo.InvariantCulture, "{0}", new object[] { (ushort)42 }, "42" };
            yield return new object[] { "", CultureInfo.InvariantCulture, "{0}", new object[] { (uint)42 }, "42" };
            yield return new object[] { "", CultureInfo.InvariantCulture, "{0}", new object[] { (ulong)42 }, "42" };
            yield return new object[] { "", CultureInfo.InvariantCulture, "{0}", new object[] { (UIntPtr)42 }, "42" };
            yield return new object[] { "", CultureInfo.InvariantCulture, "{0}", new object[] { new Version(1, 2, 3, 4) }, "1.2.3.4" };
        }

        [Theory]
        [MemberData(nameof(AppendFormat_TestData))]
        public static void AppendFormat(string original, IFormatProvider provider, string format, object[] values, string expected)
        {
            StringBuilder builder;
            if (values != null)
            {
                if (values.Length == 1)
                {
                    // Use AppendFormat(string, object) or AppendFormat(IFormatProvider, string, object)
                    if (provider == null)
                    {
                        // Use AppendFormat(string, object)
                        builder = new StringBuilder(original);
                        builder.AppendFormat(format, values[0]);
                        Assert.Equal(expected, builder.ToString());
                    }
                    // Use AppendFormat(IFormatProvider, string, object)
                    builder = new StringBuilder(original);
                    builder.AppendFormat(provider, format, values[0]);
                    Assert.Equal(expected, builder.ToString());
                }
                else if (values.Length == 2)
                {
                    // Use AppendFormat(string, object, object) or AppendFormat(IFormatProvider, string, object, object)
                    if (provider == null)
                    {
                        // Use AppendFormat(string, object, object)
                        builder = new StringBuilder(original);
                        builder.AppendFormat(format, values[0], values[1]);
                        Assert.Equal(expected, builder.ToString());
                    }
                    // Use AppendFormat(IFormatProvider, string, object, object)
                    builder = new StringBuilder(original);
                    builder.AppendFormat(provider, format, values[0], values[1]);
                    Assert.Equal(expected, builder.ToString());
                }
                else if (values.Length == 3)
                {
                    // Use AppendFormat(string, object, object, object) or AppendFormat(IFormatProvider, string, object, object, object)
                    if (provider == null)
                    {
                        // Use AppendFormat(string, object, object, object)
                        builder = new StringBuilder(original);
                        builder.AppendFormat(format, values[0], values[1], values[2]);
                        Assert.Equal(expected, builder.ToString());
                    }
                    // Use AppendFormat(IFormatProvider, string, object, object, object)
                    builder = new StringBuilder(original);
                    builder.AppendFormat(provider, format, values[0], values[1], values[2]);
                    Assert.Equal(expected, builder.ToString());
                }
            }
            // Use AppendFormat(string, object[]) or AppendFormat(IFormatProvider, string, object[])
            if (provider == null)
            {
                // Use AppendFormat(string, object[])
                builder = new StringBuilder(original);
                builder.AppendFormat(format, values);
                Assert.Equal(expected, builder.ToString());
            }
            // Use AppendFormat(IFormatProvider, string, object[])
            builder = new StringBuilder(original);
            builder.AppendFormat(provider, format, values);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void AppendFormat_Invalid()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            IFormatProvider formatter = null;
            var obj1 = new object();
            var obj2 = new object();
            var obj3 = new object();
            var obj4 = new object();

            AssertExtensions.Throws<ArgumentNullException>("format", () => builder.AppendFormat(null, obj1)); // Format is null
            AssertExtensions.Throws<ArgumentNullException>("format", () => builder.AppendFormat(null, obj1, obj2, obj3)); // Format is null
            AssertExtensions.Throws<ArgumentNullException>("format", () => builder.AppendFormat(null, obj1, obj2, obj3)); // Format is null
            AssertExtensions.Throws<ArgumentNullException>("format", () => builder.AppendFormat(null, obj1, obj2, obj3, obj4)); // Format is null
            AssertExtensions.Throws<ArgumentNullException>("args", () => builder.AppendFormat("", null)); // Args is null
            AssertExtensions.Throws<ArgumentNullException>("format", () => builder.AppendFormat(null, (object[])null)); // Both format and args are null
            AssertExtensions.Throws<ArgumentNullException>("format", () => builder.AppendFormat(formatter, null, obj1)); // Format is null
            AssertExtensions.Throws<ArgumentNullException>("format", () => builder.AppendFormat(formatter, null, obj1, obj2)); // Format is null
            AssertExtensions.Throws<ArgumentNullException>("format", () => builder.AppendFormat(formatter, null, obj1, obj2, obj3)); // Format is null
            AssertExtensions.Throws<ArgumentNullException>("format", () => builder.AppendFormat(formatter, null, obj1, obj2, obj3, obj4)); // Format is null
            AssertExtensions.Throws<ArgumentNullException>("args", () => builder.AppendFormat(formatter, "", null)); // Args is null
            AssertExtensions.Throws<ArgumentNullException>("format", () => builder.AppendFormat(formatter, null, null)); // Both format and args are null

            Assert.Throws<FormatException>(() => builder.AppendFormat("{-1}", obj1)); // Format has value < 0
            Assert.Throws<FormatException>(() => builder.AppendFormat("{-1}", obj1, obj2)); // Format has value < 0
            Assert.Throws<FormatException>(() => builder.AppendFormat("{-1}", obj1, obj2, obj3)); // Format has value < 0
            Assert.Throws<FormatException>(() => builder.AppendFormat("{-1}", obj1, obj2, obj3, obj4)); // Format has value < 0
            Assert.Throws<FormatException>(() => builder.AppendFormat(formatter, "{-1}", obj1)); // Format has value < 0
            Assert.Throws<FormatException>(() => builder.AppendFormat(formatter, "{-1}", obj1, obj2)); // Format has value < 0
            Assert.Throws<FormatException>(() => builder.AppendFormat(formatter, "{-1}", obj1, obj2, obj3)); // Format has value < 0
            Assert.Throws<FormatException>(() => builder.AppendFormat(formatter, "{-1}", obj1, obj2, obj3, obj4)); // Format has value < 0

            Assert.Throws<FormatException>(() => builder.AppendFormat("{1}", obj1)); // Format has value >= 1
            Assert.Throws<FormatException>(() => builder.AppendFormat("{2}", obj1, obj2)); // Format has value >= 2
            Assert.Throws<FormatException>(() => builder.AppendFormat("{3}", obj1, obj2, obj3)); // Format has value >= 3
            Assert.Throws<FormatException>(() => builder.AppendFormat("{4}", obj1, obj2, obj3, obj4)); // Format has value >= 4
            Assert.Throws<FormatException>(() => builder.AppendFormat(formatter, "{1}", obj1)); // Format has value >= 1
            Assert.Throws<FormatException>(() => builder.AppendFormat(formatter, "{2}", obj1, obj2)); // Format has value >= 2
            Assert.Throws<FormatException>(() => builder.AppendFormat(formatter, "{3}", obj1, obj2, obj3)); // Format has value >= 3
            Assert.Throws<FormatException>(() => builder.AppendFormat(formatter, "{4}", obj1, obj2, obj3, obj4)); // Format has value >= 4

            Assert.Throws<FormatException>(() => builder.AppendFormat("{", "")); // Format has unescaped {
            Assert.Throws<FormatException>(() => builder.AppendFormat("{a", "")); // Format has unescaped {

            Assert.Throws<FormatException>(() => builder.AppendFormat("}", "")); // Format has unescaped }
            Assert.Throws<FormatException>(() => builder.AppendFormat("}a", "")); // Format has unescaped }
            Assert.Throws<FormatException>(() => builder.AppendFormat("{0:}}", "")); // Format has unescaped }

            Assert.Throws<FormatException>(() => builder.AppendFormat("{\0", "")); // Format has invalid character after {
            Assert.Throws<FormatException>(() => builder.AppendFormat("{a", "")); // Format has invalid character after {

            Assert.Throws<FormatException>(() => builder.AppendFormat("{0     ", "")); // Format with index and spaces is not closed

            Assert.Throws<FormatException>(() => builder.AppendFormat("{1000000", new string[10])); // Format index is too long
            Assert.Throws<FormatException>(() => builder.AppendFormat("{10000000}", new string[10])); // Format index is too long

            Assert.Throws<FormatException>(() => builder.AppendFormat("{0,", "")); // Format with comma is not closed
            Assert.Throws<FormatException>(() => builder.AppendFormat("{0,   ", "")); // Format with comma and spaces is not closed
            Assert.Throws<FormatException>(() => builder.AppendFormat("{0,-", "")); // Format with comma and minus sign is not closed

            Assert.Throws<FormatException>(() => builder.AppendFormat("{0,-\0", "")); // Format has invalid character after minus sign
            Assert.Throws<FormatException>(() => builder.AppendFormat("{0,-a", "")); // Format has invalid character after minus sign

            Assert.Throws<FormatException>(() => builder.AppendFormat("{0,1000000", new string[10])); // Format length is too long
            Assert.Throws<FormatException>(() => builder.AppendFormat("{0,10000000}", new string[10])); // Format length is too long

            Assert.Throws<FormatException>(() => builder.AppendFormat("{0:", new string[10])); // Format with colon is not closed
            Assert.Throws<FormatException>(() => builder.AppendFormat("{0:    ", new string[10])); // Format with colon and spaces is not closed

            Assert.Throws<FormatException>(() => builder.AppendFormat("{0:{", new string[10])); // Format with custom format contains unescaped {
            Assert.Throws<FormatException>(() => builder.AppendFormat("{0:{}", new string[10])); // Format with custom format contains unescaped {

            Assert.Throws<FormatException>(() => builder.AppendFormat("{0}", new TooManyCharsWrittenSpanFormattable())); // ISpanFormattable that returns more characters than it actually wrote
        }

        private struct TooManyCharsWrittenSpanFormattable : ISpanFormattable
        {
            public string ToString(string format, IFormatProvider formatProvider) => "abc";
            public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider)
            {
                "abc".TryCopyTo(destination);
                charsWritten = 1_000_000;
                return true;
            }
        }

        [Fact]
        public static void AppendFormat_NoEscapedBracesInCustomFormatSpecifier()
        {
            // Tests new rule which does not allow escaped braces in the custom format specifier
            var builder = new StringBuilder();
            builder.AppendFormat("{0:}}}", 0);

            // Previous behavior: first two closing braces would be escaped and passed in as the custom format specifier, thus result = "}"
            // New behavior: first closing brace closes the argument hole and next two are escaped as part of the format, thus result = "0}"
            Assert.Equal("0}", builder.ToString());
            // Previously this would be allowed and escaped brace would be passed into the custom format, now this is unsupported
            Assert.Throws<FormatException>(() => builder.AppendFormat("{0:{{}", 0)); // Format with custom format contains {
        }

        [Fact]
        public static void AppendFormat_NewLengthGreaterThanBuilderLength_ThrowsArgumentOutOfRangeException()
        {
            var builder = new StringBuilder(0, 5);
            IFormatProvider formatter = null;
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => builder.AppendFormat("{0}", "a"));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => builder.AppendFormat("{0}", "a", ""));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => builder.AppendFormat("{0}", "a", "", ""));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => builder.AppendFormat("{0}", "a", "", "", ""));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => builder.AppendFormat(formatter, "{0}", "a"));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => builder.AppendFormat(formatter, "{0}", "a", ""));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => builder.AppendFormat(formatter, "{0}", "a", "", ""));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => builder.AppendFormat(formatter, "{0}", "a", "", "", ""));
        }

        public static IEnumerable<object[]> AppendLine_TestData()
        {
            yield return new object[] { "Hello", "abc", "Helloabc" + Environment.NewLine };
            yield return new object[] { "Hello", "", "Hello" + Environment.NewLine };
            yield return new object[] { "Hello", null, "Hello" + Environment.NewLine };
        }

        [Theory]
        [MemberData(nameof(AppendLine_TestData))]
        public static void AppendLine(string original, string value, string expected)
        {
            StringBuilder builder;
            if (string.IsNullOrEmpty(value))
            {
                // Use AppendLine()
                builder = new StringBuilder(original);
                builder.AppendLine();
                Assert.Equal(expected, builder.ToString());
            }
            // Use AppendLine(string)
            builder = new StringBuilder(original);
            builder.AppendLine(value);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void AppendLine_NoSpareCapacity_ThrowsArgumentOutOfRangeException()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => builder.AppendLine());
            AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => builder.AppendLine("a"));
        }

        [Fact]
        public static void Clear()
        {
            var builder = new StringBuilder("Hello");
            builder.Clear();
            Assert.Equal(0, builder.Length);
            Assert.Same(string.Empty, builder.ToString());
        }

        [Fact]
        public static void Clear_Empty_CapacityNotZero()
        {
            var builder = new StringBuilder();
            builder.Clear();
            Assert.NotEqual(0, builder.Capacity);
        }

        [Fact]
        public static void Clear_Empty_CapacityStaysUnchanged()
        {
            var sb = new StringBuilder(14);
            sb.Clear();
            Assert.Equal(14, sb.Capacity);
        }

        [Fact]
        public static void Clear_Full_CapacityStaysUnchanged()
        {
            var sb = new StringBuilder(14);
            sb.Append("Hello World!!!");
            sb.Clear();
            Assert.Equal(14, sb.Capacity);
        }

        [Fact]
        public static void Clear_AtMaxCapacity_CapacityStaysUnchanged()
        {
            var builder = new StringBuilder(14, 14);
            builder.Append("Hello World!!!");
            builder.Clear();
            Assert.Equal(14, builder.Capacity);
        }

        [Theory]
        [InlineData("Hello", 0, new char[] { '\0', '\0', '\0', '\0', '\0' }, 0, 5, new char[] { 'H', 'e', 'l', 'l', 'o' })]
        [InlineData("Hello", 0, new char[] { '\0', '\0', '\0', '\0', '\0', '\0' }, 1, 5, new char[] { '\0', 'H', 'e', 'l', 'l', 'o' })]
        [InlineData("Hello", 0, new char[] { '\0', '\0', '\0', '\0' }, 0, 4, new char[] { 'H', 'e', 'l', 'l' })]
        [InlineData("Hello", 1, new char[] { '\0', '\0', '\0', '\0', '\0', '\0', '\0' }, 2, 4, new char[] { '\0', '\0', 'e', 'l', 'l', 'o', '\0' })]
        public static void CopyTo(string value, int sourceIndex, char[] destination, int destinationIndex, int count, char[] expected)
        {
            var builder = new StringBuilder(value);
            builder.CopyTo(sourceIndex, destination, destinationIndex, count);
            Assert.Equal(expected, destination);
        }

        [Fact]
        public static void CopyTo_StringBuilderWithMultipleChunks()
        {
            StringBuilder builder = StringBuilderWithMultipleChunks();
            char[] destination = new char[builder.Length];
            builder.CopyTo(0, destination, 0, destination.Length);
            Assert.Equal(s_chunkSplitSource.ToCharArray(), destination);
        }

        [Fact]
        public static void CopyTo_Invalid()
        {
            var builder = new StringBuilder("Hello");
            AssertExtensions.Throws<ArgumentNullException>("destination", () => builder.CopyTo(0, null, 0, 0)); // Destination is null

            AssertExtensions.Throws<ArgumentOutOfRangeException>("sourceIndex", () => builder.CopyTo(-1, new char[10], 0, 0)); // Source index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("sourceIndex", () => builder.CopyTo(6, new char[10], 0, 0)); // Source index > builder.Length

            AssertExtensions.Throws<ArgumentOutOfRangeException>("destinationIndex", () => builder.CopyTo(0, new char[10], -1, 0)); // Destination index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => builder.CopyTo(0, new char[10], 0, -1)); // Count < 0

            AssertExtensions.Throws<ArgumentException>(null, () => builder.CopyTo(5, new char[10], 0, 1)); // Source index + count > builder.Length
            AssertExtensions.Throws<ArgumentException>(null, () => builder.CopyTo(4, new char[10], 0, 2)); // Source index + count > builder.Length

            AssertExtensions.Throws<ArgumentException>(null, () => builder.CopyTo(0, new char[10], 10, 1)); // Destination index + count > destinationArray.Length
            AssertExtensions.Throws<ArgumentException>(null, () => builder.CopyTo(0, new char[10], 9, 2)); // Destination index + count > destinationArray.Length
        }

        [Fact]
        public static void EnsureCapacity()
        {
            var builder = new StringBuilder(40);

            builder.EnsureCapacity(20);
            Assert.True(builder.Capacity >= 20);

            builder.EnsureCapacity(20000);
            Assert.True(builder.Capacity >= 20000);

            // Ensuring a capacity less than the current capacity does not change anything
            int oldCapacity = builder.Capacity;
            builder.EnsureCapacity(10);
            Assert.Equal(oldCapacity, builder.Capacity);
        }

        [Fact]
        public static void EnsureCapacity_InvalidCapacity_ThrowsArgumentOutOfRangeException()
        {
            var builder = new StringBuilder("Hello", 10);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => builder.EnsureCapacity(-1)); // Capacity < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => builder.EnsureCapacity(unchecked(builder.MaxCapacity + 1))); // Capacity > builder.MaxCapacity
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var sb1 = new StringBuilder("Hello");
            var sb2 = new StringBuilder("Hello");
            var sb3 = new StringBuilder("HelloX");

            var sb4 = new StringBuilder(10, 20);
            var sb5 = new StringBuilder(10, 20);

            var sb6 = new StringBuilder(10, 20).Append("Hello");
            var sb7 = new StringBuilder(10, 20).Append("Hello");
            var sb8 = new StringBuilder(10, 20).Append("HelloX");

            yield return new object[] { sb1, sb1, true };
            yield return new object[] { sb1, sb2, true };
            yield return new object[] { sb1, sb3, false };

            yield return new object[] { sb4, sb5, true };

            yield return new object[] { sb6, sb7, true };
            yield return new object[] { sb6, sb8, false };

            yield return new object[] { sb1, null, false };

            StringBuilder chunkSplitBuilder = StringBuilderWithMultipleChunks();
            yield return new object[] { chunkSplitBuilder, StringBuilderWithMultipleChunks(), true };
            yield return new object[] { sb1, chunkSplitBuilder, false };
            yield return new object[] { chunkSplitBuilder, sb1, false };
            yield return new object[] { chunkSplitBuilder, StringBuilderWithMultipleChunks().Append("b"), false };

            yield return new object[] { new StringBuilder(), new StringBuilder(), true };
            yield return new object[] { new StringBuilder(), new StringBuilder().Clear(), true };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public static void EqualsTest(StringBuilder sb1, StringBuilder sb2, bool expected)
        {
            Assert.Equal(expected, sb1.Equals(sb2));
        }

        [Theory]
        [InlineData("Hello", 0, (uint)0, "0Hello")]
        [InlineData("Hello", 3, (uint)123, "Hel123lo")]
        [InlineData("Hello", 5, (uint)456, "Hello456")]
        public static void Insert_UInt(string original, int index, uint value, string expected)
        {
            var builder = new StringBuilder(original);
            builder.Insert(index, value);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Insert_UInt_Invalid()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(-1, (uint)1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(builder.Length + 1, (uint)1)); // Index > builder.Length
            Assert.Throws<OutOfMemoryException>(() => builder.Insert(builder.Length, (uint)1)); // New length > builder.MaxCapacity
        }

        [Theory]
        [InlineData("Hello", 0, true, "TrueHello")]
        [InlineData("Hello", 3, false, "HelFalselo")]
        [InlineData("Hello", 5, false, "HelloFalse")]
        public static void Insert_Bool(string original, int index, bool value, string expected)
        {
            var builder = new StringBuilder(original);
            builder.Insert(index, value);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Insert_Bool_Invalid()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(-1, true)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(builder.Length + 1, true)); // Index > builder.Length
            Assert.Throws<OutOfMemoryException>(() => builder.Insert(builder.Length, true)); // New length > builder.MaxCapacity
        }

        [Theory]
        [InlineData("Hello", 0, (byte)0, "0Hello")]
        [InlineData("Hello", 3, (byte)123, "Hel123lo")]
        [InlineData("Hello", 5, (byte)123, "Hello123")]
        public static void Insert_Byte(string original, int index, byte value, string expected)
        {
            var builder = new StringBuilder(original);
            builder.Insert(index, value);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Insert_Byte_Invalid()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(-1, (byte)1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(builder.Length + 1, (byte)1)); // Index > builder.Length
            Assert.Throws<OutOfMemoryException>(() => builder.Insert(builder.Length, (byte)1)); // New length > builder.MaxCapacity
        }

        [Theory]
        [InlineData("Hello", 0, (ulong)0, "0Hello")]
        [InlineData("Hello", 3, (ulong)123, "Hel123lo")]
        [InlineData("Hello", 5, (ulong)456, "Hello456")]
        public static void Insert_ULong(string original, int index, ulong value, string expected)
        {
            var builder = new StringBuilder(original);
            builder.Insert(index, value);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Insert_ULong_Invalid()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(-1, (ulong)1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(builder.Length + 1, (ulong)1)); // Index > builder.Length
            Assert.Throws<OutOfMemoryException>(() => builder.Insert(builder.Length, (ulong)1)); // New length > builder.MaxCapacity
        }

        [Theory]
        [InlineData("Hello", 0, (ushort)0, "0Hello")]
        [InlineData("Hello", 3, (ushort)123, "Hel123lo")]
        [InlineData("Hello", 5, (ushort)456, "Hello456")]
        public static void Insert_UShort(string original, int index, ushort value, string expected)
        {
            var builder = new StringBuilder(original);
            builder.Insert(index, value);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Insert_UShort_Invalid()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(-1, (ushort)1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(builder.Length + 1, (ushort)1)); // Index > builder.Length
            Assert.Throws<OutOfMemoryException>(() => builder.Insert(builder.Length, (ushort)1)); // New length > builder.MaxCapacity
        }

        [Theory]
        [InlineData("Hello", 0, '\0', "\0Hello")]
        [InlineData("Hello", 3, 'a', "Helalo")]
        [InlineData("Hello", 5, 'b', "Hellob")]
        public static void Insert_Char(string original, int index, char value, string expected)
        {
            var builder = new StringBuilder(original);
            builder.Insert(index, value);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Insert_Char_Invalid()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(-1, '\0')); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(builder.Length + 1, '\0')); // Index > builder.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("requiredLength", () => builder.Insert(builder.Length, '\0')); // New length > builder.MaxCapacity
        }

        public static IEnumerable<object[]> Insert_Float_TestData()
        {
            yield return new object[] { "Hello", 0, (float)0, "0Hello" };
            yield return new object[] { "Hello", 3, (float)1.23, "Hel1.23lo" };
            yield return new object[] { "Hello", 5, (float)-4.56, "Hello-4.56" };
        }

        [Fact]
        public static void Test_Insert_Float()
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                foreach (var testdata in Insert_Float_TestData())
                {
                    Insert_Float((string)testdata[0], (int)testdata[1], (float)testdata[2], (string)testdata[3]);
                }
            }
        }

        private static void Insert_Float(string original, int index, float value, string expected)
        {
            var builder = new StringBuilder(original);
            builder.Insert(index, value);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Insert_Float_Invalid()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(-1, (float)1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(builder.Length + 1, (float)1)); // Index > builder.Length
            Assert.Throws<OutOfMemoryException>(() => builder.Insert(builder.Length, (float)1)); // New length > builder.MaxCapacity
        }

        [Theory]
        [InlineData("Hello", 0, "\0", "\0Hello")]
        [InlineData("Hello", 3, "abc", "Helabclo")]
        [InlineData("Hello", 5, "def", "Hellodef")]
        [InlineData("Hello", 0, "", "Hello")]
        [InlineData("Hello", 0, null, "Hello")]
        public static void Insert_Object(string original, int index, object value, string expected)
        {
            var builder = new StringBuilder(original);
            builder.Insert(index, value);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Insert_Object_Invalid()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(-1, new object())); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(builder.Length + 1, new object())); // Index > builder.Length
            Assert.Throws<OutOfMemoryException>(() => builder.Insert(builder.Length, new object())); // New length > builder.MaxCapacity
        }

        [Theory]
        [InlineData("Hello", 0, (long)0, "0Hello")]
        [InlineData("Hello", 3, (long)123, "Hel123lo")]
        [InlineData("Hello", 5, (long)-456, "Hello-456")]
        public static void Insert_Long(string original, int index, long value, string expected)
        {
            var builder = new StringBuilder(original);
            builder.Insert(index, value);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Insert_Long_Invalid()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(-1, (long)1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(builder.Length + 1, (long)1)); // Index > builder.Length
            Assert.Throws<OutOfMemoryException>(() => builder.Insert(builder.Length, (long)1)); // New length > builder.MaxCapacity
        }

        [Theory]
        [InlineData("Hello", 0, 0, "0Hello")]
        [InlineData("Hello", 3, 123, "Hel123lo")]
        [InlineData("Hello", 5, -456, "Hello-456")]
        public static void Insert_Int(string original, int index, int value, string expected)
        {
            var builder = new StringBuilder(original);
            builder.Insert(index, value);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Insert_Int_Invalid()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(-1, 1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(builder.Length + 1, 1)); // Index > builder.Length
            Assert.Throws<OutOfMemoryException>(() => builder.Insert(builder.Length, 1)); // New length > builder.MaxCapacity
        }

        [Theory]
        [InlineData("Hello", 0, (short)0, "0Hello")]
        [InlineData("Hello", 3, (short)123, "Hel123lo")]
        [InlineData("Hello", 5, (short)-456, "Hello-456")]
        public static void Insert_Short(string original, int index, short value, string expected)
        {
            var builder = new StringBuilder(original);
            builder.Insert(index, value);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Insert_Short_Invalid()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(-1, (short)1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(builder.Length + 1, (short)1)); // Index > builder.Length
            Assert.Throws<OutOfMemoryException>(() => builder.Insert(builder.Length, (short)1)); // New length > builder.MaxCapacity
        }

        public static IEnumerable<object[]> Insert_Double_TestData()
        {
            yield return new object[] { "Hello", 0, (double)0, "0Hello" };
            yield return new object[] { "Hello", 3, 1.23, "Hel1.23lo" };
            yield return new object[] { "Hello", 5, -4.56, "Hello-4.56" };
        }

        [Fact]
        public static void Test_Insert_Double()
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                foreach (var testdata in Insert_Double_TestData())
                {
                    Insert_Double((string)testdata[0], (int)testdata[1], (double)testdata[2], (string)testdata[3]);
                }
            }
        }

        private static void Insert_Double(string original, int index, double value, string expected)
        {
            var builder = new StringBuilder(original);
            builder.Insert(index, value);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Insert_Double_Invalid()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(-1, (double)1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(builder.Length + 1, (double)1)); // Index > builder.Length
            Assert.Throws<OutOfMemoryException>(() => builder.Insert(builder.Length, (double)1)); // New length > builder.MaxCapacity
        }

        public static IEnumerable<object[]> Test_Insert_Decimal_TestData()
        {
            yield return new object[] { "Hello", 0, (double)0, "0Hello" };
            yield return new object[] { "Hello", 3, 1.23, "Hel1.23lo" };
            yield return new object[] { "Hello", 5, -4.56, "Hello-4.56" };
        }

        [Fact]
        public static void Test_Insert_Decimal()
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                foreach (var testdata in Test_Insert_Decimal_TestData())
                {
                    Insert_Decimal((string)testdata[0], (int)testdata[1], (double)testdata[2], (string)testdata[3]);
                }
            }
        }

        private static void Insert_Decimal(string original, int index, double doubleValue, string expected)
        {
            var builder = new StringBuilder(original);
            builder.Insert(index, new decimal(doubleValue));
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Insert_Decimal_Invalid()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(-1, (decimal)1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(builder.Length + 1, (decimal)1)); // Index > builder.Length
            Assert.Throws<OutOfMemoryException>(() => builder.Insert(builder.Length, (decimal)1)); // New length > builder.MaxCapacity
        }

        [Theory]
        [InlineData("Hello", 0, (sbyte)0, "0Hello")]
        [InlineData("Hello", 3, (sbyte)123, "Hel123lo")]
        [InlineData("Hello", 5, (sbyte)-123, "Hello-123")]
        public static void Insert_SByte(string original, int index, sbyte value, string expected)
        {
            var builder = new StringBuilder(original);
            builder.Insert(index, value);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Insert_SByte_Invalid()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(-1, (sbyte)1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(builder.Length + 1, (sbyte)1)); // Index > builder.Length
            Assert.Throws<OutOfMemoryException>(() => builder.Insert(builder.Length, (sbyte)1)); // New length > builder.MaxCapacity
        }

        [Theory]
        [InlineData("Hello", 0, "\0", 0, "Hello")]
        [InlineData("Hello", 0, "\0", 1, "\0Hello")]
        [InlineData("Hello", 3, "abc", 1, "Helabclo")]
        [InlineData("Hello", 5, "def", 1, "Hellodef")]
        [InlineData("Hello", 0, "", 1, "Hello")]
        [InlineData("Hello", 0, null, 1, "Hello")]
        [InlineData("Hello", 3, "abc", 2, "Helabcabclo")]
        [InlineData("Hello", 5, "def", 2, "Hellodefdef")]
        public static void Insert_String_Count(string original, int index, string value, int count, string expected)
        {
            StringBuilder builder;
            if (count == 1)
            {
                // Use Insert(int, string)
                builder = new StringBuilder(original);
                builder.Insert(index, value);
                Assert.Equal(expected, builder.ToString());
            }
            // Use Insert(int, string, int)
            builder = new StringBuilder(original);
            builder.Insert(index, value, count);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Insert_String_Count_Invalid()
        {
            var builder = new StringBuilder(0, 6);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(-1, "")); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(-1, "", 0)); // Index < 0

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(builder.Length + 1, "")); // Index > builder.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(builder.Length + 1, "", 0)); // Index > builder.Length

            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => builder.Insert(0, "", -1)); // Count < 0

            AssertExtensions.Throws<ArgumentOutOfRangeException>("requiredLength", () => builder.Insert(builder.Length, "aa")); // New length > builder.MaxCapacity
            Assert.Throws<OutOfMemoryException>(() => builder.Insert(builder.Length, "aa", 1)); // New length > builder.MaxCapacity
            Assert.Throws<OutOfMemoryException>(() => builder.Insert(builder.Length, "a", 2)); // New length > builder.MaxCapacity
        }

        [Theory]
        [InlineData("Hello", 0, new char[] { '\0' }, 0, 1, "\0Hello")]
        [InlineData("Hello", 3, new char[] { 'a', 'b', 'c' }, 0, 1, "Helalo")]
        [InlineData("Hello", 3, new char[] { 'a', 'b', 'c' }, 0, 3, "Helabclo")]
        [InlineData("Hello", 5, new char[] { 'd', 'e', 'f' }, 0, 1, "Hellod")]
        [InlineData("Hello", 5, new char[] { 'd', 'e', 'f' }, 0, 3, "Hellodef")]
        [InlineData("Hello", 0, new char[0], 0, 0, "Hello")]
        [InlineData("Hello", 0, null, 0, 0, "Hello")]
        [InlineData("Hello", 3, new char[] { 'a', 'b', 'c' }, 1, 1, "Helblo")]
        [InlineData("Hello", 3, new char[] { 'a', 'b', 'c' }, 1, 2, "Helbclo")]
        [InlineData("Hello", 3, new char[] { 'a', 'b', 'c' }, 0, 2, "Helablo")]
        public static void Insert_CharArray(string original, int index, char[] value, int startIndex, int charCount, string expected)
        {
            StringBuilder builder;
            if (startIndex == 0 && charCount == (value?.Length ?? 0))
            {
                // Use Insert(int, char[])
                builder = new StringBuilder(original);
                builder.Insert(index, value);
                Assert.Equal(expected, builder.ToString());
            }
            // Use Insert(int, char[], int, int)
            builder = new StringBuilder(original);
            builder.Insert(index, value, startIndex, charCount);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Insert_CharArray_Invalid()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(-1, new char[1])); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(-1, new char[0], 0, 0)); // Index < 0

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(builder.Length + 1, new char[1])); // Index > builder.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(builder.Length + 1, new char[0], 0, 0)); // Index > builder.Length

            Assert.Throws<ArgumentNullException>(() => builder.Insert(0, null, 1, 1)); // Value is null (startIndex and count are not zero)
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => builder.Insert(0, new char[0], -1, 0)); // Start index < 0

            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => builder.Insert(0, new char[3], 4, 0)); // Start index + char count > value.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => builder.Insert(0, new char[3], 3, 1)); // Start index + char count > value.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => builder.Insert(0, new char[3], 2, 2)); // Start index + char count > value.Length

            AssertExtensions.Throws<ArgumentOutOfRangeException>("requiredLength", () => builder.Insert(builder.Length, new char[1])); // New length > builder.MaxCapacity
            AssertExtensions.Throws<ArgumentOutOfRangeException>("requiredLength", () => builder.Insert(builder.Length, new char[] { 'a' }, 0, 1)); // New length > builder.MaxCapacity
        }

        [Fact]
        public static void Insert_CharArray_InvalidCount()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");
            AssertExtensions.Throws<ArgumentOutOfRangeException>("charCount", () => builder.Insert(0, new char[0], 0, -1)); // Char count < 0
        }

        [Fact]
        public static void Insert_CharArray_InvalidCharCount()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");
            AssertExtensions.Throws<ArgumentOutOfRangeException>("charCount", () => builder.Insert(0, new char[0], 0, -1)); // Char count < 0
        }

        [Theory]
        [InlineData("", 0, 0, "")]
        [InlineData("Hello", 0, 5, "")]
        [InlineData("Hello", 1, 3, "Ho")]
        [InlineData("Hello", 1, 4, "H")]
        [InlineData("Hello", 1, 0, "Hello")]
        [InlineData("Hello", 5, 0, "Hello")]
        public static void Remove(string value, int startIndex, int length, string expected)
        {
            var builder = new StringBuilder(value);
            builder.Remove(startIndex, length);
            Assert.Equal(expected, builder.ToString());
        }

        [Theory]
        [InlineData(1, 29, "a")]
        [InlineData(0, 29, "a")]
        [InlineData(20, 10, "aaaaaaaaaaaaaaaaaaaa")]
        [InlineData(0, 15, "aaaaaaaaaaaaaaa")]
        public static void Remove_StringBuilderWithMultipleChunks(int startIndex, int count, string expected)
        {
            StringBuilder builder = StringBuilderWithMultipleChunks();
            builder.Remove(startIndex, count);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Remove_Invalid()
        {
            var builder = new StringBuilder("Hello");
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => builder.Remove(-1, 0)); // Start index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => builder.Remove(0, -1)); // Length < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => builder.Remove(6, 0)); // Start index + length > 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => builder.Remove(5, 1)); // Start index + length > 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => builder.Remove(4, 2)); // Start index + length > 0
        }

        [Theory]
        [InlineData("", 'a', '!', 0, 0, "")]
        [InlineData("aaaabbbbccccdddd", 'a', '!', 0, 16, "!!!!bbbbccccdddd")]
        [InlineData("aaaabbbbccccdddd", 'a', '!', 0, 4, "!!!!bbbbccccdddd")]
        [InlineData("aaaabbbbccccdddd", 'a', '!', 2, 3, "aa!!bbbbccccdddd")]
        [InlineData("aaaabbbbccccdddd", 'a', '!', 4, 1, "aaaabbbbccccdddd")]
        [InlineData("aaaabbbbccccdddd", 'b', '!', 0, 0, "aaaabbbbccccdddd")]
        [InlineData("aaaabbbbccccdddd", 'a', '!', 16, 0, "aaaabbbbccccdddd")]
        [InlineData("aaaabbbbccccdddd", 'e', '!', 0, 16, "aaaabbbbccccdddd")]
        public static void Replace_Char(string value, char oldChar, char newChar, int startIndex, int count, string expected)
        {
            StringBuilder builder;
            if (startIndex == 0 && count == value.Length)
            {
                // Use Replace(char, char)
                builder = new StringBuilder(value);
                builder.Replace(oldChar, newChar);
                Assert.Equal(expected, builder.ToString());
            }
            // Use Replace(char, char, int, int)
            builder = new StringBuilder(value);
            builder.Replace(oldChar, newChar, startIndex, count);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Replace_Char_StringBuilderWithMultipleChunks()
        {
            StringBuilder builder = StringBuilderWithMultipleChunks();
            builder.Replace('a', 'b', 0, builder.Length);
            Assert.Equal(new string('b', builder.Length), builder.ToString());
        }

        [Fact]
        public static void Replace_Char_Invalid()
        {
            var builder = new StringBuilder("Hello");
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => builder.Replace('a', 'b', -1, 0)); // Start index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => builder.Replace('a', 'b', 0, -1)); // Count < 0

            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => builder.Replace('a', 'b', 6, 0)); // Count + start index > builder.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => builder.Replace('a', 'b', 5, 1)); // Count + start index > builder.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => builder.Replace('a', 'b', 4, 2)); // Count + start index > builder.Length
        }

        [Theory]
        [InlineData("", "a", "!", 0, 0, "")]
        [InlineData("aaaabbbbccccdddd", "a", "!", 0, 16, "!!!!bbbbccccdddd")]
        [InlineData("aaaabbbbccccdddd", "a", "!", 2, 3, "aa!!bbbbccccdddd")]
        [InlineData("aaaabbbbccccdddd", "a", "!", 4, 1, "aaaabbbbccccdddd")]
        [InlineData("aaaabbbbccccdddd", "aab", "!", 2, 2, "aaaabbbbccccdddd")]
        [InlineData("aaaabbbbccccdddd", "aab", "!", 2, 3, "aa!bbbccccdddd")]
        [InlineData("aaaabbbbccccdddd", "aa", "!", 0, 16, "!!bbbbccccdddd")]
        [InlineData("aaaabbbbccccdddd", "aa", "$!", 0, 16, "$!$!bbbbccccdddd")]
        [InlineData("aaaabbbbccccdddd", "aa", "$!$", 0, 16, "$!$$!$bbbbccccdddd")]
        [InlineData("aaaabbbbccccdddd", "aaaa", "!", 0, 16, "!bbbbccccdddd")]
        [InlineData("aaaabbbbccccdddd", "aaaa", "$!", 0, 16, "$!bbbbccccdddd")]
        [InlineData("aaaabbbbccccdddd", "a", "", 0, 16, "bbbbccccdddd")]
        [InlineData("aaaabbbbccccdddd", "b", null, 0, 16, "aaaaccccdddd")]
        [InlineData("aaaabbbbccccdddd", "aaaabbbbccccdddd", "", 0, 16, "")]
        [InlineData("aaaabbbbccccdddd", "aaaabbbbccccdddd", "", 16, 0, "aaaabbbbccccdddd")]
        [InlineData("aaaabbbbccccdddd", "aaaabbbbccccdddde", "", 0, 16, "aaaabbbbccccdddd")]
        [InlineData("aaaaaaaaaaaaaaaa", "a", "b", 0, 16, "bbbbbbbbbbbbbbbb")]
        public static void Replace_String(string value, string oldValue, string newValue, int startIndex, int count, string expected)
        {
            StringBuilder builder;
            if (startIndex == 0 && count == value.Length)
            {
                // Use Replace(string, string)
                builder = new StringBuilder(value);
                builder.Replace(oldValue, newValue);
                Assert.Equal(expected, builder.ToString());
            }
            // Use Replace(string, string, int, int)
            builder = new StringBuilder(value);
            builder.Replace(oldValue, newValue, startIndex, count);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Replace_String_StringBuilderWithMultipleChunks()
        {
            StringBuilder builder = StringBuilderWithMultipleChunks();
            builder.Replace("a", "b", builder.Length - 10, 10);
            Assert.Equal(new string('a', builder.Length - 10) + new string('b', 10), builder.ToString());
        }

        [Fact]
        public static void Replace_String_StringBuilderWithMultipleChunks_WholeString()
        {
            StringBuilder builder = StringBuilderWithMultipleChunks();
            builder.Replace(builder.ToString(), "");
            Assert.Same(string.Empty, builder.ToString());
        }

        [Fact]
        public static void Replace_String_StringBuilderWithMultipleChunks_LongString()
        {
            StringBuilder builder = StringBuilderWithMultipleChunks();
            builder.Replace(builder.ToString() + "b", "");
            Assert.Equal(s_chunkSplitSource, builder.ToString());
        }

        [Fact]
        public static void Replace_String_Invalid()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentNullException>("oldValue", () => builder.Replace(null, "")); // Old value is null
            AssertExtensions.Throws<ArgumentNullException>("oldValue", () => builder.Replace(null, "a", 0, 0)); // Old value is null

            AssertExtensions.Throws<ArgumentException>("oldValue", () => builder.Replace("", "a")); // Old value is empty
            AssertExtensions.Throws<ArgumentException>("oldValue", () => builder.Replace("", "a", 0, 0)); // Old value is empty

            AssertExtensions.Throws<ArgumentOutOfRangeException>("requiredLength", () => builder.Replace("o", "oo")); // New length > builder.MaxCapacity
            AssertExtensions.Throws<ArgumentOutOfRangeException>("requiredLength", () => builder.Replace("o", "oo", 0, 5)); // New length > builder.MaxCapacity

            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => builder.Replace("a", "b", -1, 0)); // Start index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => builder.Replace("a", "b", 0, -1)); // Count < 0

            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => builder.Replace("a", "b", 6, 0)); // Count + start index > builder.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => builder.Replace("a", "b", 5, 1)); // Count + start index > builder.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => builder.Replace("a", "b", 4, 2)); // Count + start index > builder.Length
        }

        [Theory]
        [InlineData("Hello", 0, 5, "Hello")]
        [InlineData("Hello", 2, 3, "llo")]
        [InlineData("Hello", 2, 2, "ll")]
        [InlineData("Hello", 5, 0, "")]
        [InlineData("Hello", 4, 0, "")]
        [InlineData("Hello", 0, 0, "")]
        [InlineData("", 0, 0, "")]
        public static void ToStringTest(string value, int startIndex, int length, string expected)
        {
            var builder = new StringBuilder(value);
            if (startIndex == 0 && length == value.Length)
            {
                Assert.Equal(expected, builder.ToString());
            }
            Assert.Equal(expected, builder.ToString(startIndex, length));
        }

        [Fact]
        public static void ToString_StringBuilderWithMultipleChunks()
        {
            StringBuilder builder = StringBuilderWithMultipleChunks();
            Assert.Equal(s_chunkSplitSource, builder.ToString());
            Assert.Equal(s_chunkSplitSource, builder.ToString(0, builder.Length));
            Assert.Equal("a", builder.ToString(0, 1));
            Assert.Equal(string.Empty, builder.ToString(builder.Length - 1, 0));
        }

        [Fact]
        public static void ToString_Invalid()
        {
            var builder = new StringBuilder("Hello");
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => builder.ToString(-1, 0)); // Start index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => builder.ToString(0, -1)); // Length < 0

            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => builder.ToString(6, 0)); // Length + start index > builder.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => builder.ToString(5, 1)); // Length + start index > builder.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => builder.ToString(4, 2)); // Length + start index > builder.Length
        }

        public class CustomFormatter : ICustomFormatter, IFormatProvider
        {
            public string Format(string format, object arg, IFormatProvider formatProvider) => "abc";
            public object GetFormat(Type formatType) => this;
        }

        [Fact]
        public static void AppendJoin_NullValues_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("values", () => new StringBuilder().AppendJoin('|', (object[])null));
            AssertExtensions.Throws<ArgumentNullException>("values", () => new StringBuilder().AppendJoin('|', (IEnumerable<object>)null));
            AssertExtensions.Throws<ArgumentNullException>("values", () => new StringBuilder().AppendJoin('|', (string[])null));
            AssertExtensions.Throws<ArgumentNullException>("values", () => new StringBuilder().AppendJoin("|", (object[])null));
            AssertExtensions.Throws<ArgumentNullException>("values", () => new StringBuilder().AppendJoin("|", (IEnumerable<object>)null));
            AssertExtensions.Throws<ArgumentNullException>("values", () => new StringBuilder().AppendJoin("|", (string[])null));
        }

        [Theory]
        [InlineData(new object[0], "")]
        [InlineData(new object[] { null }, "")]
        [InlineData(new object[] { 10 }, "10")]
        [InlineData(new object[] { null, null }, "|")]
        [InlineData(new object[] { null, 20 }, "|20")]
        [InlineData(new object[] { 10, null }, "10|")]
        [InlineData(new object[] { 10, 20 }, "10|20")]
        [InlineData(new object[] { null, null, null }, "||")]
        [InlineData(new object[] { null, null, 30 }, "||30")]
        [InlineData(new object[] { null, 20, null }, "|20|")]
        [InlineData(new object[] { null, 20, 30 }, "|20|30")]
        [InlineData(new object[] { 10, null, null }, "10||")]
        [InlineData(new object[] { 10, null, 30 }, "10||30")]
        [InlineData(new object[] { 10, 20, null }, "10|20|")]
        [InlineData(new object[] { 10, 20, 30 }, "10|20|30")]
        [InlineData(new object[] { "" }, "")]
        [InlineData(new object[] { "", "" }, "|")]
        public static void AppendJoin_TestValues(object[] values, string expected)
        {
            var stringValues = Array.ConvertAll(values, _ => _?.ToString());
            var enumerable = values.Select(_ => _);

            Assert.Equal(expected, new StringBuilder().AppendJoin('|', values).ToString());
            Assert.Equal(expected, new StringBuilder().AppendJoin('|', enumerable).ToString());
            Assert.Equal(expected, new StringBuilder().AppendJoin('|', stringValues).ToString());
            Assert.Equal(expected, new StringBuilder().AppendJoin("|", values).ToString());
            Assert.Equal(expected, new StringBuilder().AppendJoin("|", enumerable).ToString());
            Assert.Equal(expected, new StringBuilder().AppendJoin("|", stringValues).ToString());
        }

        [Fact]
        public static void AppendJoin_NullToStringValues()
        {
            AppendJoin_TestValues(new object[] { new NullToStringObject() }, "");
            AppendJoin_TestValues(new object[] { new NullToStringObject(), new NullToStringObject() }, "|");
        }

        private sealed class NullToStringObject
        {
            public override string ToString() => null;
        }

        [Theory]
        [InlineData(null, "123")]
        [InlineData("", "123")]
        [InlineData(" ", "1 2 3")]
        [InlineData(", ", "1, 2, 3")]
        public static void AppendJoin_TestStringSeparators(string separator, string expected)
        {
            Assert.Equal(expected, new StringBuilder().AppendJoin(separator, new object[] { 1, 2, 3 }).ToString());
            Assert.Equal(expected, new StringBuilder().AppendJoin(separator, Enumerable.Range(1, 3)).ToString());
            Assert.Equal(expected, new StringBuilder().AppendJoin(separator, new string[] { "1", "2", "3" }).ToString());
        }


        private static StringBuilder CreateBuilderWithNoSpareCapacity()
        {
            return new StringBuilder(0, 5).Append("Hello");
        }

        [Theory]
        [InlineData(null, new object[] { null, null })]
        [InlineData("", new object[] { "", "" })]
        [InlineData(" ", new object[] { })]
        [InlineData(", ", new object[] { "" })]
        public static void AppendJoin_NoValues_NoSpareCapacity_DoesNotThrow(string separator, object[] values)
        {
            var stringValues = Array.ConvertAll(values, _ => _?.ToString());
            var enumerable = values.Select(_ => _);

            if (separator?.Length == 1)
            {
                CreateBuilderWithNoSpareCapacity().AppendJoin(separator[0], values);
                CreateBuilderWithNoSpareCapacity().AppendJoin(separator[0], enumerable);
                CreateBuilderWithNoSpareCapacity().AppendJoin(separator[0], stringValues);
            }
            CreateBuilderWithNoSpareCapacity().AppendJoin(separator, values);
            CreateBuilderWithNoSpareCapacity().AppendJoin(separator, enumerable);
            CreateBuilderWithNoSpareCapacity().AppendJoin(separator, stringValues);
        }

        [Theory]
        [InlineData(null, new object[] { " " })]
        [InlineData(" ", new object[] { " " })]
        [InlineData(" ", new object[] { null, null })]
        [InlineData(" ", new object[] { "", "" })]
        public static void AppendJoin_NoSpareCapacity_ThrowsArgumentOutOfRangeException(string separator, object[] values)
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            var stringValues = Array.ConvertAll(values, _ => _?.ToString());
            var enumerable = values.Select(_ => _);

            if (separator?.Length == 1)
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => CreateBuilderWithNoSpareCapacity().AppendJoin(separator[0], values));
                AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => CreateBuilderWithNoSpareCapacity().AppendJoin(separator[0], enumerable));
                AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => CreateBuilderWithNoSpareCapacity().AppendJoin(separator[0], stringValues));
            }
            AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => CreateBuilderWithNoSpareCapacity().AppendJoin(separator, values));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => CreateBuilderWithNoSpareCapacity().AppendJoin(separator, enumerable));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => CreateBuilderWithNoSpareCapacity().AppendJoin(separator, stringValues));
        }

        [Theory]
        [InlineData("Hello", new char[] { 'a' }, "Helloa")]
        [InlineData("Hello", new char[] { 'b', 'c', 'd' }, "Hellobcd")]
        [InlineData("Hello", new char[] { 'b', '\0', 'd' }, "Hellob\0d")]
        [InlineData("", new char[] { 'e', 'f', 'g' }, "efg")]
        [InlineData("Hello", new char[0], "Hello")]
        public static void Append_CharSpan(string original, char[] value, string expected)
        {
            var builder = new StringBuilder(original);
            builder.Append(new ReadOnlySpan<char>(value));
            Assert.Equal(expected, builder.ToString());
        }

        [Theory]
        [InlineData("Hello", new char[] { 'a' }, "Helloa")]
        [InlineData("Hello", new char[] { 'b', 'c', 'd' }, "Hellobcd")]
        [InlineData("Hello", new char[] { 'b', '\0', 'd' }, "Hellob\0d")]
        [InlineData("", new char[] { 'e', 'f', 'g' }, "efg")]
        [InlineData("Hello", new char[0], "Hello")]
        public static void Append_CharMemory(string original, char[] value, string expected)
        {
            var builder = new StringBuilder(original);
            builder.Append(value.AsMemory());
            Assert.Equal(expected, builder.ToString());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10000)]
        public static void Clear_AppendAndInsertBeforeClearManyTimes_CapacityStaysWithinRange(int times)
        {
            var builder = new StringBuilder();
            var originalCapacity = builder.Capacity;
            var s = new string(' ', 10);
            int oldLength = 0;
            for (int i = 0; i < times; i++)
            {
                builder.Append(s);
                builder.Append(s);
                builder.Append(s);
                builder.Insert(0, s);
                builder.Insert(0, s);
                oldLength = builder.Length;

                builder.Clear();
            }
            Assert.InRange(builder.Capacity, 1, oldLength * 1.2);
        }

        [Fact]
        public static void Clear_InitialCapacityMuchLargerThanLength_CapacityReducedToInitialCapacity()
        {
            var builder = new StringBuilder(100);
            var initialCapacity = builder.Capacity;
            builder.Append(new string('a', 40));
            builder.Insert(0, new string('a', 10));
            builder.Insert(0, new string('a', 10));
            builder.Insert(0, new string('a', 10));
            var oldCapacity = builder.Capacity;
            var oldLength = builder.Length;
            builder.Clear();
            Assert.NotEqual(oldCapacity, builder.Capacity);
            Assert.Equal(initialCapacity, builder.Capacity);
            Assert.NotInRange(builder.Capacity, 1, oldLength * 1.2);
            Assert.InRange(builder.Capacity, 1, Math.Max(initialCapacity, oldLength * 1.2));
        }

        [Fact]
        public static void Clear_StringBuilderHasTwoChunks_OneChunkIsEmpty_ClearReducesCapacity()
        {
            var sb = new StringBuilder(string.Empty);
            int initialCapacity = sb.Capacity;
            for (int i = 0; i < initialCapacity; i++)
            {
                sb.Append('a');
            }
            sb.Insert(0, 'a');
            while (sb.Length > 1)
            {
                sb.Remove(1, 1);
            }
            int oldCapacity = sb.Capacity;
            sb.Clear();
            Assert.Equal(oldCapacity - 1, sb.Capacity);
            Assert.Equal(initialCapacity, sb.Capacity);
        }

        [Theory]
        [InlineData("Hello", 0, new char[] { '\0', '\0', '\0', '\0', '\0' }, 5, new char[] { 'H', 'e', 'l', 'l', 'o' })]
        [InlineData("Hello", 0, new char[] { '\0', '\0', '\0', '\0' }, 4, new char[] { 'H', 'e', 'l', 'l' })]
        [InlineData("Hello", 1, new char[] { '\0', '\0', '\0', '\0', '\0' }, 4, new char[] { 'e', 'l', 'l', 'o', '\0' })]
        public static void CopyTo_CharSpan(string value, int sourceIndex, char[] destination, int count, char[] expected)
        {
            var builder = new StringBuilder(value);
            builder.CopyTo(sourceIndex, new Span<char>(destination), count);
            Assert.Equal(expected, destination);
        }

        [Fact]
        public static void CopyTo_CharSpan_StringBuilderWithMultipleChunks()
        {
            StringBuilder builder = StringBuilderWithMultipleChunks();
            char[] destination = new char[builder.Length];
            builder.CopyTo(0, new Span<char>(destination), destination.Length);
            Assert.Equal(s_chunkSplitSource.ToCharArray(), destination);
        }

        [Fact]
        public static void CopyTo_CharSpan_Invalid()
        {
            var builder = new StringBuilder("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>("sourceIndex", () => builder.CopyTo(-1, new Span<char>(new char[10]), 0)); // Source index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("sourceIndex", () => builder.CopyTo(6, new Span<char>(new char[10]), 0)); // Source index > builder.Length

            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => builder.CopyTo(0, new Span<char>(new char[10]), -1)); // Count < 0

            AssertExtensions.Throws<ArgumentException>(null, () => builder.CopyTo(5, new Span<char>(new char[10]), 1)); // Source index + count > builder.Length
            AssertExtensions.Throws<ArgumentException>(null, () => builder.CopyTo(4, new Span<char>(new char[10]), 2)); // Source index + count > builder.Length

            AssertExtensions.Throws<ArgumentException>(null, () => builder.CopyTo(0, new Span<char>(new char[10]), 11)); // count > destinationArray.Length
        }

        [Theory]
        [InlineData("Hello", 0, new char[] { '\0' }, "\0Hello")]
        [InlineData("Hello", 3, new char[] { 'a', 'b', 'c' }, "Helabclo")]
        [InlineData("Hello", 5, new char[] { 'd', 'e', 'f' }, "Hellodef")]
        [InlineData("Hello", 0, new char[0], "Hello")]
        public static void Insert_CharSpan(string original, int index, char[] value, string expected)
        {
            var builder = new StringBuilder(original);
            builder.Insert(index, new ReadOnlySpan<char>(value));
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Insert_CharSpan_Invalid()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(-1, new ReadOnlySpan<char>(new char[0]))); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(builder.Length + 1, new ReadOnlySpan<char>(new char[0]))); // Index > builder.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("requiredLength", () => builder.Insert(builder.Length, new ReadOnlySpan<char>(new char[1]))); // New length > builder.MaxCapacity
        }

        public static IEnumerable<object[]> Append_StringBuilder_TestData()
        {
            string mediumString = new string('a', 30);
            string largeString = new string('b', 1000);

            var sb1 = new StringBuilder("Hello");
            var sb2 = new StringBuilder("one");
            var sb3 = new StringBuilder(20).Append(mediumString);

            yield return new object[] { new StringBuilder("Hello"), sb1, "HelloHello" };
            yield return new object[] { new StringBuilder("Hello"), sb2, "Helloone" };
            yield return new object[] { new StringBuilder("Hello"), new StringBuilder(), "Hello" };

            yield return new object[] { new StringBuilder("one"), sb3, "one" + mediumString };

            yield return new object[] { new StringBuilder(20).Append(mediumString), sb3, mediumString + mediumString };
            yield return new object[] { new StringBuilder(10).Append(mediumString), sb3, mediumString + mediumString };

            yield return new object[] { new StringBuilder(20).Append(largeString), sb3, largeString + mediumString };
            yield return new object[] { new StringBuilder(10).Append(largeString), sb3, largeString + mediumString };

            yield return new object[] { new StringBuilder(10), sb3, mediumString };
            yield return new object[] { new StringBuilder(30), sb3, mediumString };
            yield return new object[] { new StringBuilder(10), new StringBuilder(20), string.Empty};

            yield return new object[] { sb1, null, "Hello" };
            yield return new object[] { sb1, sb1, "HelloHello" };
        }

        [Theory]
        [MemberData(nameof(Append_StringBuilder_TestData))]
        public static void Append_StringBuilder(StringBuilder s1, StringBuilder s2, string s)
        {
            Assert.Equal(s, s1.Append(s2).ToString());
        }

        public static IEnumerable<object[]> Append_StringBuilder_Substring_TestData()
        {
            string mediumString = new string('a', 30);
            string largeString = new string('b', 1000);

            var sb1 = new StringBuilder("Hello");
            var sb2 = new StringBuilder("one");
            var sb3 = new StringBuilder(20).Append(mediumString);

            yield return new object[] { new StringBuilder("Hello"), sb1, 0, 5, "HelloHello" };
            yield return new object[] { new StringBuilder("Hello"), sb1, 0, 0, "Hello" };
            yield return new object[] { new StringBuilder("Hello"), sb1, 2, 3, "Hellollo" };
            yield return new object[] { new StringBuilder("Hello"), sb1, 2, 2, "Helloll" };
            yield return new object[] { new StringBuilder("Hello"), sb1, 2, 0, "Hello" };
            yield return new object[] { new StringBuilder("Hello"), new StringBuilder(), 0, 0, "Hello" };
            yield return new object[] { new StringBuilder("Hello"), null, 0, 0, "Hello" };
            yield return new object[] { new StringBuilder(), new StringBuilder("Hello"), 2, 3, "llo" };
            yield return new object[] { new StringBuilder("Hello"), sb2, 0, 3, "Helloone" };

            yield return new object[] { new StringBuilder("one"), sb3, 5, 25, "one" + new string('a', 25) };
            yield return new object[] { new StringBuilder("one"), sb3, 5, 20, "one" + new string('a', 20) };
            yield return new object[] { new StringBuilder("one"), sb3, 10, 10, "one" + new string('a', 10) };

            yield return new object[] { new StringBuilder(20).Append(mediumString), sb3, 20, 10, new string('a', 40) };
            yield return new object[] { new StringBuilder(10).Append(mediumString), sb3, 10, 10, new string('a', 40) };

            yield return new object[] { new StringBuilder(20).Append(largeString), new StringBuilder(20).Append(largeString), 100, 50, largeString + new string('b', 50) };
            yield return new object[] { new StringBuilder(10).Append(mediumString), new StringBuilder(20).Append(largeString), 20, 10, mediumString + new string('b', 10) };
            yield return new object[] { new StringBuilder(10).Append(mediumString), new StringBuilder(20).Append(largeString), 100, 50, mediumString + new string('b', 50) };

            yield return new object[] { sb1, sb1, 2, 3, "Hellollo" };
            yield return new object[] { sb2, sb2, 2, 0, "one" };
        }

        [Theory]
        [MemberData(nameof(Append_StringBuilder_Substring_TestData))]
        public static void Append_StringBuilder_Substring(StringBuilder s1, StringBuilder s2, int startIndex, int count, string s)
        {
            Assert.Equal(s, s1.Append(s2, startIndex, count).ToString());
        }

        [Fact]
        public static void Append_StringBuilder_InvalidInput()
        {
            StringBuilder sb = new StringBuilder(5, 5).Append("Hello");

            Assert.Throws<ArgumentOutOfRangeException>(() => sb.Append(sb, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => sb.Append(sb, 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => sb.Append(sb, 4, 5));

            Assert.Throws<ArgumentNullException>(() => sb.Append( (StringBuilder)null, 2, 2));
            Assert.Throws<ArgumentNullException>(() => sb.Append((StringBuilder)null, 2, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => new StringBuilder(3, 6).Append("Hello").Append(sb));
            Assert.Throws<ArgumentOutOfRangeException>(() => new StringBuilder(3, 6).Append("Hello").Append("Hello"));

            Assert.Throws<ArgumentOutOfRangeException>(() => sb.Append(sb));
        }

        public static IEnumerable<object[]> Equals_String_TestData()
        {
            string mediumString = new string('a', 30);
            string largeString = new string('a', 1000);
            string extraLargeString = new string('a', 41000); // 8000 is the maximum chunk size

            var sb1 = new StringBuilder("Hello");
            var sb2 = new StringBuilder(20).Append(mediumString);
            var sb3 = new StringBuilder(20).Append(largeString);
            var sb4 = new StringBuilder(20).Append(extraLargeString);

            yield return new object[] { sb1, "Hello", true };
            yield return new object[] { sb1, "Hel", false };
            yield return new object[] { sb1, "Hellz", false };
            yield return new object[] { sb1, "Helloz", false };
            yield return new object[] { sb1, "", false };
            yield return new object[] { new StringBuilder(), "", true };
            yield return new object[] { new StringBuilder(), "Hello", false };
            yield return new object[] { sb2, mediumString, true };
            yield return new object[] { sb2, "H", false };
            yield return new object[] { sb3, largeString, true };
            yield return new object[] { sb3, "H", false };
            yield return new object[] { sb3, new string('a', 999) + 'b', false };
            yield return new object[] { sb4, extraLargeString, true };
            yield return new object[] { sb4, "H", false };
        }

        [Theory]
        [MemberData(nameof(Equals_String_TestData))]
        public static void Equals_String(StringBuilder sb1, string value, bool expected)
        {
            Assert.Equal(expected, sb1.Equals(value.AsSpan()));
        }

        [Fact]
        public static void ForEach()
        {
            // Test on a variety of lengths, at least up to the point of 9 8K chunks = 72K because this is where
            // we start using a different technique for creating the ChunkEnumerator.   200 * 500 = 100K which hits this.
            for (int i = 0; i < 200; i++)
            {
                StringBuilder inBuilder = new StringBuilder();
                for (int j = 0; j < i; j++)
                {
                    // Make some unique strings that are at least 500 bytes long.
                    inBuilder.Append(j);
                    inBuilder.Append("_abcdefghijklmnopqrstuvwxyz01234567890__Abcdefghijklmnopqrstuvwxyz01234567890__ABcdefghijklmnopqrstuvwxyz01_");
                    inBuilder.Append("_abcdefghijklmnopqrstuvwxyz01234567890__Abcdefghijklmnopqrstuvwxyz01234567890__ABcdefghijklmnopqrstuvwxyz0123_");
                    inBuilder.Append("_abcdefghijklmnopqrstuvwxyz01234567890__Abcdefghijklmnopqrstuvwxyz01234567890__ABcdefghijklmnopqrstuvwxyz012345_");
                    inBuilder.Append("_abcdefghijklmnopqrstuvwxyz01234567890__Abcdefghijklmnopqrstuvwxyz01234567890__ABcdefghijklmnopqrstuvwxyz012345678_");
                    inBuilder.Append("_abcdefghijklmnopqrstuvwxyz01234567890__Abcdefghijklmnopqrstuvwxyz01234567890__ABcdefghijklmnopqrstuvwxyz01234567890_");
                }

                // Copy the string out (not using StringBuilder).
                string outStr = "";
                foreach (ReadOnlyMemory<char> chunk in inBuilder.GetChunks())
                    outStr += new string(chunk.Span);

                // The strings formed by concatenating the chunks should be the same as the value in the StringBuilder.
                Assert.Equal(outStr, inBuilder.ToString());
            }
        }

        [Fact]
        public static void EqualsIgnoresCapacity()
        {
            var sb1 = new StringBuilder(5);
            var sb2 = new StringBuilder(10);

            Assert.True(sb1.Equals(sb2));

            sb1.Append("12345");
            sb2.Append("12345");

            Assert.True(sb1.Equals(sb2));
        }

        [Fact]
        public static void EqualsIgnoresMaxCapacity()
        {
            var sb1 = new StringBuilder(5, 5);
            var sb2 = new StringBuilder(5, 10);

            Assert.True(sb1.Equals(sb2));

            sb1.Append("12345");
            sb2.Append("12345");

            Assert.True(sb1.Equals(sb2));
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/40625")] // Hangs expanding the SB
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static unsafe void FailureOnLargeString()
        {
            RemoteExecutor.Invoke(() => // Uses lots of memory
            {
                AssertExtensions.ThrowsAny<ArgumentOutOfRangeException, OutOfMemoryException>(() =>
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(new char[2_000_000_000]);
                    sb.Length--;
                    string s = new string('x', 500_000_000);
                    sb.Append(s); // This should throw, not AV
                });
            }).Dispose();
        }
    }
}
