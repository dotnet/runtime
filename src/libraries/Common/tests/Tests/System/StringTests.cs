// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using static System.Text.Tests.StringBuilderTests;

#pragma warning disable xUnit2009 // these are the tests for String and so should be using the explicit methods on String

namespace System.Tests
{
    //When add new tests make sure to add checks for both string and span APIs where relevant.
    public partial class StringTests
    {
        private const string SoftHyphen = "\u00AD";
        private const string ZeroWidthJoiner = "\u200D"; // weightless in both ICU and NLS
        private static readonly char[] s_whiteSpaceCharacters = { '\u0009', '\u000a', '\u000b', '\u000c', '\u000d', '\u0020', '\u0085', '\u00a0', '\u1680' };

        [Theory]
        [InlineData(new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', '\0' }, "abcdefgh")]
        [InlineData(new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', '\0', 'i', 'j' }, "abcdefgh")]
        [InlineData(new char[] { 'a', '\0' }, "a")]
        [InlineData(new char[] { '\0' }, "")]
        [InlineData(new char[] { '?', '@', ' ', '\0' }, "?@ ")] // ? and @ don't have overlapping bits
        [InlineData(new char[] { '\u8001', '\u8002', '\ufffd', '\u1234', '\ud800', '\udfff', '\0' }, "\u8001\u8002\ufffd\u1234\ud800\udfff")] // chars with high bits set
        public static unsafe void Ctor_CharPtr(char[] valueArray, string expected)
        {
            fixed (char* value = valueArray)
            {
                Assert.Equal(expected, new string(value));
            }
            fixed (char* value = &MemoryMarshal.GetReference<char>(valueArray))
            {
                Assert.Equal(expected, new string(value));
            }
        }

        [Fact]
        public static unsafe void Ctor_CharPtr_Empty()
        {
            Assert.Same(string.Empty, new string((char*)null));
        }

        [Fact]
        public static unsafe void Ctor_CharPtr_OddAddressShouldStillWork()
        {
            // We need to get an odd address, so allocate a byte[] and
            // take the address of the second element
            byte[] bytes = { 0xff, 0x12, 0x34, 0x00, 0x00 };
            fixed (byte* pBytes = bytes)
            {
                // The address of a fixed byte[] should always be even
                Debug.Assert(unchecked((int)pBytes) % 2 == 0);
                char* pCh = (char*)(pBytes + 1);

                // This should handle the odd address when trying to get
                // the length of the string to allocate
                string actual = new string(pCh);

                // Since we're casting between pointers of types with different sizes,
                // the result will vary on little/big endian platforms
                string expected = BitConverter.IsLittleEndian ? "\u3412" : "\u1234";
                Assert.Equal(expected, actual);
            }
        }

        [Theory]
        [InlineData(new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', '\0' }, 0, 8, "abcdefgh")]
        [InlineData(new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', '\0' }, 0, 9, "abcdefgh\0")]
        [InlineData(new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', '\0', 'i', 'j', 'k' }, 0, 12, "abcdefgh\0ijk")]
        [InlineData(new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', '\0' }, 2, 3, "cde")]
        [InlineData(new char[] { '\0' }, 0, 1, "\0")]
        [InlineData(new char[] { 'a', 'b', 'c' }, 0, 0, "")]
        [InlineData(new char[] { 'a', 'b', 'c' }, 1, 0, "")]
        public static unsafe void Ctor_CharPtr_Int_Int(char[] valueArray, int startIndex, int length, string expected)
        {
            _ = valueArray; // xunit analyzer bug: https://github.com/xunit/xunit/issues/1969
            fixed (char* value = valueArray)
            {
                Assert.Equal(expected, new string(value, startIndex, length));
            }
        }

        [Fact]
        public static unsafe void Ctor_CharPtr_Int_Int_Empty()
        {
            Assert.Same(string.Empty, new string((char*)null, 0, 0));
        }

        [Fact]
        public static unsafe void Ctor_CharPtr_Int_Int_Invalid()
        {
            var valueArray = new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', '\0' };

            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () =>
            {
                fixed (char* value = valueArray) { new string(value, -1, 8); } // Start index < 0
            });

            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () =>
            {
                fixed (char* value = valueArray) { new string(value, 0, -1); } // Length < 0
            });

            AssertExtensions.Throws<ArgumentOutOfRangeException>("ptr", () => new string((char*)null, 0, 1)); // null ptr with non-zero length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => new string(UIntPtr.Size == 4 ? (char*)uint.MaxValue : (char*)ulong.MaxValue, 42, 0)); // overflowing ptr + startIndex
        }

        [Theory]
        [InlineData('a', 0, "")]
        [InlineData('a', 1, "a")]
        [InlineData('a', 2, "aa")]
        [InlineData('a', 3, "aaa")]
        [InlineData('a', 4, "aaaa")]
        [InlineData('a', 5, "aaaaa")]
        [InlineData('a', 6, "aaaaaa")]
        [InlineData('a', 7, "aaaaaaa")]
        [InlineData('a', 8, "aaaaaaaa")]
        [InlineData('a', 9, "aaaaaaaaa")]
        [InlineData('\0', 1, "\0")]
        [InlineData('\0', 2, "\0\0")]
        public static void Ctor_Char_Int(char c, int count, string expected)
        {
            Assert.Equal(expected, new string(c, count));
        }

        [Fact]
        public static void Ctor_Char_Int_Negative_Count_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => new string('a', -1)); // Count < 0
        }

        [Theory]
        [InlineData(new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h' }, 0, 0, "")]
        [InlineData(new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h' }, 0, 3, "abc")]
        [InlineData(new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h' }, 2, 3, "cde")]
        [InlineData(new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h' }, 2, 6, "cdefgh")]
        [InlineData(new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h' }, 0, 8, "abcdefgh")]
        [InlineData(new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', '\0', 'i', 'j' }, 0, 11, "abcdefgh\0ij")]
        [InlineData(new char[] { '\u041F', '\u0420', '\u0418', '\u0412', '\u0415', '\u0422' }, 0, 6, "\u041F\u0420\u0418\u0412\u0415\u0422")]
        [InlineData(new char[0], 0, 0, "")]
        [InlineData(null, 0, 0, "")]
        public static void Ctor_CharArray(char[] value, int startIndex, int length, string expected)
        {
            if (value == null)
            {
                Assert.Equal(expected, new string(value));
                return;
            }
            if (startIndex == 0 && length == value.Length)
            {
                Assert.Equal(expected, new string(value));
            }
            Assert.Equal(expected, new string(value, startIndex, length));
        }

        [Fact]
        public static void Ctor_CharArray_Invalid()
        {
            var value = new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h' };

            AssertExtensions.Throws<ArgumentNullException>("value", () => new string((char[])null, 0, 0));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => new string(value, 0, 9)); // Length > array length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => new string(value, 5, -1)); // Length < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => new string(value, -1, 1)); // Start Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => new string(value, 6, 5)); // Walks off array
        }

        [Theory]
        [InlineData("Hello", 0, 'H')]
        [InlineData("Hello", 1, 'e')]
        [InlineData("Hello", 2, 'l')]
        [InlineData("Hello", 3, 'l')]
        [InlineData("Hello", 4, 'o')]
        [InlineData("\0", 0, '\0')]
        public static void Item_Get(string s, int index, char expected)
        {
            Assert.Equal(expected, s[index]);
            Assert.Equal(expected, s.AsSpan()[index]);
        }

        [Fact]
        public static void Item_Get_InvalidIndex_ThrowsIndexOutOfRangeException()
        {
            Assert.Throws<IndexOutOfRangeException>(() => "Hello"[-1]); // Index < 0
            Assert.Throws<IndexOutOfRangeException>(() => "Hello"[5]); // Index >= string.Length
            Assert.Throws<IndexOutOfRangeException>(() => ""[0]); // Index >= string.Length

            Assert.Throws<IndexOutOfRangeException>(() => "Hello".AsSpan()[-1]); // Index < 0
            Assert.Throws<IndexOutOfRangeException>(() => "Hello".AsSpan()[5]); // Index >= string.Length
            Assert.Throws<IndexOutOfRangeException>(() => "".AsSpan()[0]); // Index >= string.Length
        }

        [Theory]
        [InlineData("", 0)]
        [InlineData("\0", 1)]
        [InlineData("abc", 3)]
        [InlineData("hello", 5)]
        public static void Length(string s, int expected)
        {
            Assert.Equal(expected, s.Length);
            Assert.Equal(expected, s.AsSpan().Length);
        }

        public static IEnumerable<object[]> Concat_Strings_LessThan2_GreaterThan4_TestData()
        {
            // 0
            yield return new object[] { new string[0], "" };

            // 1
            yield return new object[] { new string[] { "1" }, "1" };
            yield return new object[] { new string[] { null }, "" };
            yield return new object[] { new string[] { "" }, "" };

            // 5
            yield return new object[] { new string[] { "1", "2", "3", "4", "5" }, "12345" };
            yield return new object[] { new string[] { null, "1", "2", "3", "4" }, "1234" };
            yield return new object[] { new string[] { "", "1", "2", "3", "4" }, "1234" };
            yield return new object[] { new string[] { "1", null, "2", "3", "4" }, "1234" };
            yield return new object[] { new string[] { "1", "", "2", "3", "4" }, "1234" };
            yield return new object[] { new string[] { "1", "2", null, "3", "4" }, "1234" };
            yield return new object[] { new string[] { "1", "2", "", "3", "4" }, "1234" };
            yield return new object[] { new string[] { "1", "2", "3", null, "4" }, "1234" };
            yield return new object[] { new string[] { "1", "2", "3", "", "4" }, "1234" };
            yield return new object[] { new string[] { "1", "2", "3", "4", null }, "1234" };
            yield return new object[] { new string[] { "1", "2", "3", "4", "" }, "1234" };
            yield return new object[] { new string[] { "1", null, "3", null, "5" }, "135" };
            yield return new object[] { new string[] { "1", "", "3", "", "5" }, "135" };
            yield return new object[] { new string[] { null, null, null, null, null }, "" };
            yield return new object[] { new string[] { "", "", "", "", "" }, "" };

            // 7
            yield return new object[] { new string[] { "abcd", "efgh", "ijkl", "mnop", "qrst", "uvwx", "yz" }, "abcdefghijklmnopqrstuvwxyz" };
        }

        public static IEnumerable<object[]> Concat_Strings_2_3_4_TestData()
        {
            // 2
            yield return new object[] { new string[] { "1", "2" }, "12" };
            yield return new object[] { new string[] { null, "1" }, "1" };
            yield return new object[] { new string[] { "", "1" }, "1" };
            yield return new object[] { new string[] { "1", null }, "1" };
            yield return new object[] { new string[] { "1", "" }, "1" };
            yield return new object[] { new string[] { null, null }, "" };
            yield return new object[] { new string[] { "", "" }, "" };

            // 3
            yield return new object[] { new string[] { "1", "2", "3" }, "123" };
            yield return new object[] { new string[] { null, "1", "2" }, "12" };
            yield return new object[] { new string[] { "", "1", "2" }, "12" };
            yield return new object[] { new string[] { "1", null, "2" }, "12" };
            yield return new object[] { new string[] { "1", "", "2" }, "12" };
            yield return new object[] { new string[] { "1", "2", null }, "12" };
            yield return new object[] { new string[] { "1", "2", "" }, "12" };
            yield return new object[] { new string[] { null, "2", null }, "2" };
            yield return new object[] { new string[] { "", "2", "" }, "2" };
            yield return new object[] { new string[] { null, null, null }, "" };
            yield return new object[] { new string[] { "", "", "" }, "" };

            // 4
            yield return new object[] { new string[] { "1", "2", "3", "4" }, "1234" };
            yield return new object[] { new string[] { null, "1", "2", "3" }, "123" };
            yield return new object[] { new string[] { "", "1", "2", "3" }, "123" };
            yield return new object[] { new string[] { "1", null, "2", "3" }, "123" };
            yield return new object[] { new string[] { "1", "", "2", "3" }, "123" };
            yield return new object[] { new string[] { "1", "2", null, "3" }, "123" };
            yield return new object[] { new string[] { "1", "2", "", "3" }, "123" };
            yield return new object[] { new string[] { "1", "2", "3", null }, "123" };
            yield return new object[] { new string[] { "1", "2", "3", "" }, "123" };
            yield return new object[] { new string[] { "1", null, null, null }, "1" };
            yield return new object[] { new string[] { "1", "", "", "" }, "1" };
            yield return new object[] { new string[] { null, "1", null, "2" }, "12" };
            yield return new object[] { new string[] { "", "1", "", "2" }, "12" };
            yield return new object[] { new string[] { null, null, null, null }, "" };
            yield return new object[] { new string[] { "", "", "", "" }, "" };
        }

        [Theory]
        [MemberData(nameof(Concat_Strings_2_3_4_TestData))]
        [MemberData(nameof(Concat_Strings_LessThan2_GreaterThan4_TestData))]
        public static void Concat_String(string[] values, string expected)
        {
            void Validate(string result)
            {
                Assert.Equal(expected, result);
                if (result.Length == 0)
                {
                    Assert.Same(string.Empty, result);
                }
            }

            if (values.Length == 2)
            {
                Validate(string.Concat(values[0], values[1]));
            }
            else if (values.Length == 3)
            {
                Validate(string.Concat(values[0], values[1], values[2]));
            }
            else if (values.Length == 4)
            {
                Validate(string.Concat(values[0], values[1], values[2], values[3]));
            }

            Validate(string.Concat(values));
            Validate(string.Concat((IEnumerable<string>)values));
            Validate(string.Concat<string>((IEnumerable<string>)values)); // Call the generic IEnumerable<T>-based overload
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop] // mini-stress test that likely runs for several seconds
        public static void Concat_String_ConcurrencySafe()
        {
            var inputs = new string[2] { "abc", "def" };
            var cts = new CancellationTokenSource();
            using (var b = new Barrier(2))
            {
                // String.Concat(string[]) has a slow path that handles the case where the
                // input array is mutated concurrently.  Queue two tasks, one that repeatedly
                // does concats and the other that mutates the array concurrently.  This isn't
                // guaranteed to trigger the special case, but it typically does.
                Task.WaitAll(
                    Task.Run(() =>
                    {
                        b.SignalAndWait();
                        while (!cts.IsCancellationRequested)
                        {
                            string result = string.Concat(inputs);
                            Assert.True(result == "abcdef" || result == "abc" || result == "def" || result == "", $"result == {result}");
                        }
                    }),
                    Task.Run(() =>
                    {
                        b.SignalAndWait();
                        try
                        {
                            for (int iter = 0; iter < 100000000; iter++)
                            {
                                Volatile.Write(ref inputs[0], null);
                                Volatile.Write(ref inputs[1], null);
                                Volatile.Write(ref inputs[0], "abc");
                                Volatile.Write(ref inputs[1], "def");
                            }
                        }

                        finally
                        {
                            cts.Cancel();
                        }
                    }));
            }
        }

        public static IEnumerable<object[]> Concat_Objects_TestData()
        {
            yield return new object[] { new object[] { }, "" };

            yield return new object[] { new object[] { 1 }, "1" };
            yield return new object[] { new object[] { null }, "" };

            yield return new object[] { new object[] { 1, 2 }, "12" };
            yield return new object[] { new object[] { null, 1 }, "1" };
            yield return new object[] { new object[] { 1, null }, "1" };
            yield return new object[] { new object[] { null, null }, "" };

            yield return new object[] { new object[] { 1, 2, 3 }, "123" };
            yield return new object[] { new object[] { null, 1, 2 }, "12" };
            yield return new object[] { new object[] { 1, null, 2 }, "12" };
            yield return new object[] { new object[] { 1, 2, null }, "12" };
            yield return new object[] { new object[] { null, null, null }, "" };

            yield return new object[] { new object[] { 1, 2, 3, 4 }, "1234" };
            yield return new object[] { new object[] { null, 1, 2, 3 }, "123" };
            yield return new object[] { new object[] { 1, null, 2, 3 }, "123" };
            yield return new object[] { new object[] { 1, 2, 3, null }, "123" };
            yield return new object[] { new object[] { null, null, null, null }, "" };

            yield return new object[] { new object[] { 1, 2, 3, 4, 5 }, "12345" };
            yield return new object[] { new object[] { null, 1, 2, 3, 4 }, "1234" };
            yield return new object[] { new object[] { 1, null, 2, 3, 4 }, "1234" };
            yield return new object[] { new object[] { 1, 2, 3, 4, null }, "1234" };
            yield return new object[] { new object[] { null, null, null, null, null }, "" };

            // Concat should ignore objects that have a null ToString() value
            yield return new object[] { new object[] { new ObjectWithNullToString(), "Foo", new ObjectWithNullToString(), "Bar", new ObjectWithNullToString() }, "FooBar" };
            yield return new object[] { new object[] { new ObjectWithNullToString() }, "" };
        }

        [Theory]
        [MemberData(nameof(Concat_Objects_TestData))]
        public static void Concat_Objects(object[] values, string expected)
        {
            if (values.Length == 1)
            {
                Assert.Equal(expected, string.Concat(values[0]));
            }
            else if (values.Length == 2)
            {
                Assert.Equal(expected, string.Concat(values[0], values[1]));
            }
            else if (values.Length == 3)
            {
                Assert.Equal(expected, string.Concat(values[0], values[1], values[2]));
            }
            else if (values.Length == 4)
            {
                Assert.Equal(expected, string.Concat(values[0], values[1], values[2], values[3]));
            }
            Assert.Equal(expected, string.Concat(values));
            Assert.Equal(expected, string.Concat((IEnumerable<object>)values));
        }

        [Theory]
        [InlineData(new char[0], "")]
        [InlineData(new char[] { 'a' }, "a")]
        [InlineData(new char[] { 'a', 'b' }, "ab")]
        [InlineData(new char[] { 'a', '\0', 'b' }, "a\0b")]
        [InlineData(new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g' }, "abcdefg")]
        public static void Concat_CharEnumerable(char[] values, string expected)
        {
            Assert.Equal(expected, string.Concat(values.Select(c => c)));
        }

        [Fact]
        public static void Concat_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("values", () => string.Concat((IEnumerable<string>)null)); // Values is null
            AssertExtensions.Throws<ArgumentNullException>("values", () => string.Concat<string>((IEnumerable<string>)null)); // Generic overload
            AssertExtensions.Throws<ArgumentNullException>("values", () => string.Concat(null)); // Values is null

            AssertExtensions.Throws<ArgumentNullException>("args", () => string.Concat((object[])null)); // Values is null
            AssertExtensions.Throws<ArgumentNullException>("values", () => string.Concat<string>(null)); // Values is null
            AssertExtensions.Throws<ArgumentNullException>("values", () => string.Concat<object>(null)); // Values is null
        }

        [Theory]
        [InlineData("Hello", 0, 0, 5, new char[] { 'H', 'e', 'l', 'l', 'o' })]
        [InlineData("Hello", 1, 5, 3, new char[] { '\0', '\0', '\0', '\0', '\0', 'e', 'l', 'l', '\0', '\0' })]
        [InlineData("Hello", 2, 0, 3, new char[] { 'l', 'l', 'o', '\0', '\0', '\0', '\0', '\0', '\0', '\0' })]
        [InlineData("Hello", 0, 7, 3, new char[] { '\0', '\0', '\0', '\0', '\0', '\0', '\0', 'H', 'e', 'l' })]
        [InlineData("Hello", 5, 10, 0, new char[] { '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0' })]
        [InlineData("H" + SoftHyphen + "ello", 0, 0, 3, new char[] { 'H', '\u00AD', 'e' })]
        public static void CopyTo(string s, int sourceIndex, int destinationIndex, int count, char[] expected)
        {
            char[] dst = new char[expected.Length];
            s.CopyTo(sourceIndex, dst, destinationIndex, count);
            Assert.Equal(expected, dst);

            Span<char> dstSpan = new char[expected.Length];
            s.AsSpan(sourceIndex, count).CopyTo(dstSpan.Slice(destinationIndex, count));
            Assert.Equal(expected, dstSpan.ToArray());
        }

        [Fact]
        public static void CopyTo_Invalid()
        {
            string s = "Hello";
            char[] dst = new char[10];

            AssertExtensions.Throws<ArgumentNullException>("destination", () => s.CopyTo(0, null, 0, 0)); // Dst is null

            AssertExtensions.Throws<ArgumentOutOfRangeException>("sourceIndex", () => s.CopyTo(-1, dst, 0, 0)); // Source index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("destinationIndex", () => s.CopyTo(0, dst, -1, 0)); // Destination index < 0

            AssertExtensions.Throws<ArgumentOutOfRangeException>("destinationIndex", () => s.CopyTo(0, dst, dst.Length, 1)); // Destination index > dst.Length

            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s.CopyTo(0, dst, 0, -1)); // Count < 0

            // Source index + count > string.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("sourceIndex", () => s.CopyTo(s.Length, dst, 0, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("sourceIndex", () => s.CopyTo(s.Length - 1, dst, 0, 2));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("sourceIndex", () => s.CopyTo(0, dst, 0, 6));
        }

        [Theory]
        [InlineData("", 0)]
        [InlineData("", 1)]
        [InlineData("a", 1)]
        [InlineData("a", 0)]
        [InlineData("a", 2)]
        [InlineData("abc", 2)]
        [InlineData("abc", 3)]
        [InlineData("abc", 4)]
        [InlineData("Hello world", 20)]
        public static void CopyTo_Span(string s, int destinationLength)
        {
            char[] destination = new char[destinationLength];

            if (s.Length > destinationLength)
            {
                AssertExtensions.Throws<ArgumentException>("destination", () => s.CopyTo(destination));
                Assert.All(destination, c => Assert.Equal(0, c));

                Assert.False(s.TryCopyTo(destination));
                Assert.All(destination, c => Assert.Equal(0, c));
            }
            else
            {
                s.CopyTo(destination);
                Assert.Equal(s, new Span<char>(destination, 0, s.Length).ToString());
                Assert.All(destination.AsSpan(s.Length).ToArray(), c => Assert.Equal(0, c));

                Array.Clear(destination);

                Assert.True(s.TryCopyTo(destination));
                Assert.Equal(s, new Span<char>(destination, 0, s.Length).ToString());
                Assert.All(destination.AsSpan(s.Length).ToArray(), c => Assert.Equal(0, c));
            }
        }

        public static IEnumerable<object[]> Compare_TestData()
        {
            // CurrentCulture
            yield return new object[] { "", 0, "", 0, 0, StringComparison.CurrentCulture, 0 };
            yield return new object[] { "Hello", 0, "Hello", 0, 5, StringComparison.CurrentCulture, 0 };
            yield return new object[] { "Hello", 2, "Hello", 3, 1, StringComparison.CurrentCulture, 0 };
            yield return new object[] { "Hello", 0, "Goodbye", 0, 5, StringComparison.CurrentCulture, 1 };
            yield return new object[] { "Goodbye", 0, "Hello", 0, 5, StringComparison.CurrentCulture, -1 };
            yield return new object[] { "HELLO", 2, "hello", 2, 3, StringComparison.CurrentCulture, PlatformDetection.IsInvariantGlobalization ? -1 : 1 };
            yield return new object[] { "hello", 2, "HELLO", 2, 3, StringComparison.CurrentCulture, PlatformDetection.IsInvariantGlobalization ? 1 : -1 };
            yield return new object[] { "Hello", 2, "Hello", 2, 3, StringComparison.CurrentCulture, 0 };
            yield return new object[] { "Hello", 2, "Goodbye", 2, 3, StringComparison.CurrentCulture, -1 };
            yield return new object[] { "A", 0, "B", 0, 1, StringComparison.CurrentCulture, -1 };
            yield return new object[] { "B", 0, "A", 0, 1, StringComparison.CurrentCulture, 1 };
            yield return new object[] { null, 0, null, 0, 0, StringComparison.CurrentCulture, 0 };
            yield return new object[] { "Hello", 0, null, 0, 0, StringComparison.CurrentCulture, 1 };
            yield return new object[] { null, 0, "Hello", 0, 0, StringComparison.CurrentCulture, -1 };
            yield return new object[] { null, -1, null, -1, -1, StringComparison.CurrentCulture, 0 };
            yield return new object[] { "foo", -1, null, -1, -1, StringComparison.CurrentCulture, 1 };
            yield return new object[] { null, -1, "foo", -1, -1, StringComparison.CurrentCulture, -1 };
            // CurrentCultureIgnoreCase
            yield return new object[] { "", 0, "", 0, 0, StringComparison.CurrentCultureIgnoreCase, 0 };
            yield return new object[] { "HELLO", 0, "hello", 0, 5, StringComparison.CurrentCultureIgnoreCase, 0 };
            yield return new object[] { "Hello", 2, "Hello", 3, 1, StringComparison.CurrentCultureIgnoreCase, 0 };
            yield return new object[] { "Hello", 0, "Hello", 0, 5, StringComparison.CurrentCultureIgnoreCase, 0 };
            yield return new object[] { "Hello", 2, "Hello", 2, 3, StringComparison.CurrentCultureIgnoreCase, 0 };
            yield return new object[] { "Hello", 2, "Yellow", 2, 3, StringComparison.CurrentCultureIgnoreCase, 0 };
            yield return new object[] { "Hello", 0, "Goodbye", 0, 5, StringComparison.CurrentCultureIgnoreCase, 1 };
            yield return new object[] { "Goodbye", 0, "Hello", 0, 5, StringComparison.CurrentCultureIgnoreCase, -1 };
            yield return new object[] { "HELLO", 2, "hello", 2, 3, StringComparison.CurrentCultureIgnoreCase, 0 };
            yield return new object[] { "Hello", 2, "Goodbye", 2, 3, StringComparison.CurrentCultureIgnoreCase, -1 };
            yield return new object[] { null, 0, null, 0, 0, StringComparison.CurrentCultureIgnoreCase, 0 };
            yield return new object[] { "Hello", 0, null, 0, 0, StringComparison.CurrentCultureIgnoreCase, 1 };
            yield return new object[] { null, 0, "Hello", 0, 0, StringComparison.CurrentCultureIgnoreCase, -1 };
            yield return new object[] { null, -1, null, -1, -1, StringComparison.CurrentCultureIgnoreCase, 0 };
            yield return new object[] { "foo", -1, null, -1, -1, StringComparison.CurrentCultureIgnoreCase, 1 };
            yield return new object[] { null, -1, "foo", -1, -1, StringComparison.CurrentCultureIgnoreCase, -1 };
            // InvariantCulture
            yield return new object[] { "", 0, "", 0, 0, StringComparison.InvariantCulture, 0 };
            yield return new object[] { "Hello", 0, "Hello", 0, 5, StringComparison.InvariantCulture, 0 };
            yield return new object[] { "Hello", 2, "Hello", 3, 1, StringComparison.InvariantCulture, 0 };
            yield return new object[] { "Hello", 0, "Goodbye", 0, 5, StringComparison.InvariantCulture, 1 };
            yield return new object[] { "Goodbye", 0, "Hello", 0, 5, StringComparison.InvariantCulture, -1 };
            yield return new object[] { "HELLO", 2, "hello", 2, 3, StringComparison.InvariantCulture, PlatformDetection.IsInvariantGlobalization ? -1 : 1 };
            yield return new object[] { "hello", 2, "HELLO", 2, 3, StringComparison.InvariantCulture, PlatformDetection.IsInvariantGlobalization ? 1 : -1 };
            yield return new object[] { null, 0, null, 0, 0, StringComparison.InvariantCulture, 0 };
            yield return new object[] { "Hello", 0, null, 0, 5, StringComparison.InvariantCulture, 1 };
            yield return new object[] { null, 0, "Hello", 0, 5, StringComparison.InvariantCulture, -1 };
            // InvariantCultureIgnoreCase
            yield return new object[] { "", 0, "", 0, 0, StringComparison.InvariantCultureIgnoreCase, 0 };
            yield return new object[] { "HELLO", 0, "hello", 0, 5, StringComparison.InvariantCultureIgnoreCase, 0 };
            yield return new object[] { "Hello", 0, "Hello", 0, 5, StringComparison.InvariantCultureIgnoreCase, 0 };
            yield return new object[] { "Hello", 2, "Hello", 3, 1, StringComparison.InvariantCultureIgnoreCase, 0 };
            yield return new object[] { "Hello", 2, "Hello", 2, 3, StringComparison.InvariantCultureIgnoreCase, 0 };
            yield return new object[] { "Hello", 2, "Yellow", 2, 3, StringComparison.InvariantCultureIgnoreCase, 0 };
            yield return new object[] { "Hello", 0, "Goodbye", 0, 5, StringComparison.InvariantCultureIgnoreCase, 1 };
            yield return new object[] { "Goodbye", 0, "Hello", 0, 5, StringComparison.InvariantCultureIgnoreCase, -1 };
            yield return new object[] { "HELLO", 2, "hello", 2, 3, StringComparison.InvariantCultureIgnoreCase, 0 };
            yield return new object[] { "Hello", 2, "Goodbye", 2, 3, StringComparison.InvariantCultureIgnoreCase, -1 };
            yield return new object[] { null, 0, null, 0, 0, StringComparison.InvariantCultureIgnoreCase, 0 };
            yield return new object[] { "Hello", 0, null, 0, 5, StringComparison.InvariantCultureIgnoreCase, 1 };
            yield return new object[] { null, 0, "Hello", 0, 5, StringComparison.InvariantCultureIgnoreCase, -1 };
            // Ordinal
            yield return new object[] { "", 0, "", 0, 0, StringComparison.Ordinal, 0 };
            yield return new object[] { "Hello", 0, "Hello", 0, 5, StringComparison.Ordinal, 0 };
            yield return new object[] { "Hello", 2, "Hello", 3, 1, StringComparison.Ordinal, 0 };
            yield return new object[] { "Hello", 0, "Goodbye", 0, 5, StringComparison.Ordinal, 1 };
            yield return new object[] { "Goodbye", 0, "Hello", 0, 5, StringComparison.Ordinal, -1 };
            yield return new object[] { "Hello", 2, "Hello", 2, 3, StringComparison.Ordinal, 0 };
            yield return new object[] { "HELLO", 2, "hello", 2, 3, StringComparison.Ordinal, -1 };
            yield return new object[] { "Hello", 2, "Goodbye", 2, 3, StringComparison.Ordinal, -1 };
            yield return new object[] { "Hello", 0, "Hello", 0, 0, StringComparison.Ordinal, 0 };
            yield return new object[] { "Hello", 0, "Hello", 0, 3, StringComparison.Ordinal, 0 };
            yield return new object[] { "Hello", 0, "He" + SoftHyphen + "llo", 0, 5, StringComparison.Ordinal, -1 };
            yield return new object[] { "Hello", 0, "-=<Hello>=-", 3, 5, StringComparison.Ordinal, 0 };
            yield return new object[] { "\uD83D\uDD53Hello\uD83D\uDD50", 1, "\uD83D\uDD53Hello\uD83D\uDD54", 1, 7, StringComparison.Ordinal, 0 }; // Surrogate split
            yield return new object[] { "Hello", 0, "Hello123", 0, int.MaxValue, StringComparison.Ordinal, -1 };           // Recalculated length, second string longer
            yield return new object[] { "Hello123", 0, "Hello", 0, int.MaxValue, StringComparison.Ordinal, 1 };            // Recalculated length, first string longer
            yield return new object[] { "---aaaaaaaaaaa", 3, "+++aaaaaaaaaaa", 3, 100, StringComparison.Ordinal, 0 };      // Equal long alignment 2, equal compare
            yield return new object[] { "aaaaaaaaaaaaaa", 3, "aaaxaaaaaaaaaa", 3, 100, StringComparison.Ordinal, -1 };     // Equal long alignment 2, different compare at n=1
            yield return new object[] { "-aaaaaaaaaaaaa", 1, "+aaaaaaaaaaaaa", 1, 100, StringComparison.Ordinal, 0 };      // Equal long alignment 6, equal compare
            yield return new object[] { "aaaaaaaaaaaaaa", 1, "axaaaaaaaaaaaa", 1, 100, StringComparison.Ordinal, -1 };     // Equal long alignment 6, different compare at n=1
            yield return new object[] { "aaaaaaaaaaaaaa", 0, "aaaaaaaaaaaaaa", 0, 100, StringComparison.Ordinal, 0 };      // Equal long alignment 4, equal compare
            yield return new object[] { "aaaaaaaaaaaaaa", 0, "xaaaaaaaaaaaaa", 0, 100, StringComparison.Ordinal, -1 };     // Equal long alignment 4, different compare at n=1
            yield return new object[] { "aaaaaaaaaaaaaa", 0, "axaaaaaaaaaaaa", 0, 100, StringComparison.Ordinal, -1 };     // Equal long alignment 4, different compare at n=2
            yield return new object[] { "--aaaaaaaaaaaa", 2, "++aaaaaaaaaaaa", 2, 100, StringComparison.Ordinal, 0 };      // Equal long alignment 0, equal compare
            yield return new object[] { "aaaaaaaaaaaaaa", 2, "aaxaaaaaaaaaaa", 2, 100, StringComparison.Ordinal, -1 };     // Equal long alignment 0, different compare at n=1
            yield return new object[] { "aaaaaaaaaaaaaa", 2, "aaaxaaaaaaaaaa", 2, 100, StringComparison.Ordinal, -1 };     // Equal long alignment 0, different compare at n=2
            yield return new object[] { "aaaaaaaaaaaaaa", 2, "aaaaxaaaaaaaaa", 2, 100, StringComparison.Ordinal, -1 };     // Equal long alignment 0, different compare at n=3
            yield return new object[] { "aaaaaaaaaaaaaa", 2, "aaaaaxaaaaaaaa", 2, 100, StringComparison.Ordinal, -1 };     // Equal long alignment 0, different compare at n=4
            yield return new object[] { "aaaaaaaaaaaaaa", 2, "aaaaaaxaaaaaaa", 2, 100, StringComparison.Ordinal, -1 };     // Equal long alignment 0, different compare at n=5
            yield return new object[] { "aaaaaaaaaaaaaa", 0, "+aaaaaaaaaaaaa", 1, 13, StringComparison.Ordinal, 0 };       // Different int alignment, equal compare
            yield return new object[] { "aaaaaaaaaaaaaa", 0, "aaaaaaaaaaaaax", 1, 100, StringComparison.Ordinal, -1 };     // Different int alignment
            yield return new object[] { "aaaaaaaaaaaaaa", 1, "aaaxaaaaaaaaaa", 3, 100, StringComparison.Ordinal, -1 };     // Different long alignment, abs of 4, one of them is 2, different at n=1
            yield return new object[] { "-aaaaaaaaaaaaa", 1, "++++aaaaaaaaaa", 4, 10, StringComparison.Ordinal, 0 };       // Different long alignment, equal compare
            yield return new object[] { "aaaaaaaaaaaaaa", 1, "aaaaaaaaaaaaax", 4, 100, StringComparison.Ordinal, -1 };     // Different long alignment
            yield return new object[] { "\0", 0, "", 0, 1, StringComparison.Ordinal, 1 };                                  // Same memory layout, except for m_stringLength (m_firstChars are both 0)
            yield return new object[] { "\0\0", 0, "", 0, 2, StringComparison.Ordinal, 1 };                                // Same as above, except m_stringLength for one is 2
            yield return new object[] { "", 0, "\0b", 0, 2, StringComparison.Ordinal, -1 };                                // strA's second char != strB's second char codepath
            yield return new object[] { "", 0, "b", 0, 1, StringComparison.Ordinal, -1 };                                  // Should hit strA.m_firstChar != strB.m_firstChar codepath
            yield return new object[] { "abcxxxxxxxxxxxxxxxxxxxxxx", 0, "abdxxxxxxxxxxxxxxx", 0, int.MaxValue, StringComparison.Ordinal, -1 }; // 64-bit: first long compare is different
            yield return new object[] { "abcdefgxxxxxxxxxxxxxxxxxx", 0, "abcdefhxxxxxxxxxxx", 0, int.MaxValue, StringComparison.Ordinal, -1 }; // 64-bit: second long compare is different
            yield return new object[] { "abcdefghijkxxxxxxxxxxxxxx", 0, "abcdefghijlxxxxxxx", 0, int.MaxValue, StringComparison.Ordinal, -1 }; // 64-bit: third long compare is different
            yield return new object[] { "abcdexxxxxxxxxxxxxxxxxxxx", 0, "abcdfxxxxxxxxxxxxx", 0, int.MaxValue, StringComparison.Ordinal, -1 }; // 32-bit: second int compare is different
            yield return new object[] { "abcdefghixxxxxxxxxxxxxxxx", 0, "abcdefghjxxxxxxxxx", 0, int.MaxValue, StringComparison.Ordinal, -1 }; // 32-bit: fourth int compare is different
            yield return new object[] { null, 0, null, 0, 0, StringComparison.Ordinal, 0 };
            yield return new object[] { "Hello", 0, null, 0, 5, StringComparison.Ordinal, 1 };
            yield return new object[] { null, 0, "Hello", 0, 5, StringComparison.Ordinal, -1 };
            yield return new object[] { null, -1, null, -1, -1, StringComparison.Ordinal, 0 };
            yield return new object[] { "foo", -1, null, -1, -1, StringComparison.Ordinal, 1 };
            yield return new object[] { null, -1, "foo", -1, -1, StringComparison.Ordinal, -1 };
            // OrdinalIgnoreCase
            yield return new object[] { "", 0, "", 0, 0, StringComparison.OrdinalIgnoreCase, 0 };
            yield return new object[] { "HELLO", 0, "hello", 0, 5, StringComparison.OrdinalIgnoreCase, 0 };
            yield return new object[] { "Hello", 0, "Hello", 0, 5, StringComparison.OrdinalIgnoreCase, 0 };
            yield return new object[] { "Hello", 2, "Hello", 3, 1, StringComparison.OrdinalIgnoreCase, 0 };
            yield return new object[] { "Hello", 2, "Hello", 2, 3, StringComparison.OrdinalIgnoreCase, 0 };
            yield return new object[] { "Hello", 2, "Yellow", 2, 3, StringComparison.OrdinalIgnoreCase, 0 };
            yield return new object[] { "Hello", 0, "Goodbye", 0, 5, StringComparison.OrdinalIgnoreCase, 1 };
            yield return new object[] { "Goodbye", 0, "Hello", 0, 5, StringComparison.OrdinalIgnoreCase, -1 };
            yield return new object[] { "HELLO", 2, "hello", 2, 3, StringComparison.OrdinalIgnoreCase, 0 };
            yield return new object[] { "Hello", 2, "Goodbye", 2, 3, StringComparison.OrdinalIgnoreCase, -1 };
            yield return new object[] { "A", 0, "x", 0, 1, StringComparison.OrdinalIgnoreCase, -1 };
            yield return new object[] { "a", 0, "X", 0, 1, StringComparison.OrdinalIgnoreCase, -1 };
            yield return new object[] { "[", 0, "A", 0, 1, StringComparison.OrdinalIgnoreCase, 1 };
            yield return new object[] { "[", 0, "a", 0, 1, StringComparison.OrdinalIgnoreCase, 1 };
            yield return new object[] { "\\", 0, "A", 0, 1, StringComparison.OrdinalIgnoreCase, 1 };
            yield return new object[] { "\\", 0, "a", 0, 1, StringComparison.OrdinalIgnoreCase, 1 };
            yield return new object[] { "]", 0, "A", 0, 1, StringComparison.OrdinalIgnoreCase, 1 };
            yield return new object[] { "]", 0, "a", 0, 1, StringComparison.OrdinalIgnoreCase, 1 };
            yield return new object[] { "^", 0, "A", 0, 1, StringComparison.OrdinalIgnoreCase, 1 };
            yield return new object[] { "^", 0, "a", 0, 1, StringComparison.OrdinalIgnoreCase, 1 };
            yield return new object[] { "_", 0, "A", 0, 1, StringComparison.OrdinalIgnoreCase, 1 };
            yield return new object[] { "_", 0, "a", 0, 1, StringComparison.OrdinalIgnoreCase, 1 };
            yield return new object[] { "`", 0, "A", 0, 1, StringComparison.OrdinalIgnoreCase, 1 };
            yield return new object[] { "`", 0, "a", 0, 1, StringComparison.OrdinalIgnoreCase, 1 };
            yield return new object[] { null, 0, null, 0, 0, StringComparison.OrdinalIgnoreCase, 0 };
            yield return new object[] { "Hello", 0, null, 0, 5, StringComparison.OrdinalIgnoreCase, 1 };
            yield return new object[] { null, 0, "Hello", 0, 5, StringComparison.OrdinalIgnoreCase, -1 };
        }

        [Theory]
        [MemberData(nameof(Compare_TestData))]
        public static void Compare(string strA, int indexA, string strB, int indexB, int length, StringComparison comparisonType, int expected)
        {
            bool hasNullInputs = (strA == null || strB == null);
            bool indicesReferToEntireString = (strA != null && strB != null && indexA == 0 && indexB == 0 && (length == strB.Length || length == strA.Length));
            bool skipNonComparisonOverloads = length != 0 && ((strA == null && indexA != 0) || (strB == null && indexB != 0));
            if (hasNullInputs || indicesReferToEntireString)
            {
                if (comparisonType == StringComparison.CurrentCulture)
                {
                    // Use Compare(string, string) or Compare(string, string, false) or CompareTo(string)
                    Assert.Equal(expected, Math.Sign(string.Compare(strA, strB)));
                    Assert.Equal(expected, Math.Sign(string.Compare(strA, strB, ignoreCase: false)));
                    if (strA != null)
                    {
                        Assert.Equal(expected, Math.Sign(strA.CompareTo(strB)));

                        IComparable iComparable = strA;
                        Assert.Equal(expected, Math.Sign(iComparable.CompareTo(strB)));
                    }
                    if (strB != null)
                    {
                        Assert.Equal(expected, -Math.Sign(strB.CompareTo(strA)));

                        IComparable iComparable = strB;
                        Assert.Equal(expected, -Math.Sign(iComparable.CompareTo(strA)));
                    }
                }
                else if (comparisonType == StringComparison.CurrentCultureIgnoreCase)
                {
                    // Use Compare(string, string, true)
                    Assert.Equal(expected, Math.Sign(string.Compare(strA, strB, ignoreCase: true)));
                }
                else if (comparisonType == StringComparison.Ordinal)
                {
                    // Use CompareOrdinal(string, string)
                    Assert.Equal(expected, Math.Sign(string.CompareOrdinal(strA, strB)));
                }
                // Use CompareOrdinal(string, string, StringComparison)
                Assert.Equal(expected, Math.Sign(string.Compare(strA, strB, comparisonType)));
            }
            if (comparisonType == StringComparison.CurrentCulture)
            {
                // This may have different behavior than the overload accepting a StringComparison
                // for a combination of null/invalid inputs; see notes in Compare_Invalid for more

                if (!skipNonComparisonOverloads)
                {
                    // Use Compare(string, int, string, int, int) or Compare(string, int, string, int, int, false)
                    Assert.Equal(expected, Math.Sign(string.Compare(strA, indexA, strB, indexB, length)));
                    Assert.Equal(expected, Math.Sign(string.Compare(strA, indexA, strB, indexB, length, ignoreCase: false)));
                }
            }
            else if (comparisonType == StringComparison.CurrentCultureIgnoreCase)
            {
                // This may have different behavior than the overload accepting a StringComparison
                // for a combination of null/invalid inputs; see notes in Compare_Invalid for more

                if (!skipNonComparisonOverloads)
                {
                    // Use Compare(string, int, string, int, int, true)
                    Assert.Equal(expected, Math.Sign(string.Compare(strA, indexA, strB, indexB, length, ignoreCase: true)));
                }
            }
            else if (comparisonType == StringComparison.Ordinal)
            {
                // Use CompareOrdinal(string, int, string, int, int)
                Assert.Equal(expected, Math.Sign(string.CompareOrdinal(strA, indexA, strB, indexB, length)));
            }
            // Use Compare(string, int, string, int, int, StringComparison)
            Assert.Equal(expected, Math.Sign(string.Compare(strA, indexA, strB, indexB, length, comparisonType)));

            if (indexA >= 0 && indexB >= 0 && length >= 0)
            {
                // Comparing spans from null strings gives different results since span doesn't special case null and treats it the same as an empty string.
                if (strA == null && strB != null)
                    expected = -1;
                if (strA != null && strB == null)
                    expected = 1;
                if (length == 0)
                    expected = 0;

                ReadOnlySpan<char> span = length <= (strA.AsSpan().Length - indexA) ? strA.AsSpan(indexA, length) : strA.AsSpan(indexA);
                ReadOnlySpan<char> value = length <= (strB.AsSpan().Length - indexB) ? strB.AsSpan(indexB, length) : strB.AsSpan(indexB);
                Assert.Equal(expected, Math.Sign(span.CompareTo(value, comparisonType)));
            }
        }

        [Fact]
        public static void Compare_LongString()
        {
            string veryLongString =
                "<NamedPermissionSets><PermissionSet class=\u0022System.Security.NamedPermissionS" +
                "et\u0022version=\u00221\u0022 Unrestricted=\u0022true\u0022 Name=\u0022FullTrust" +
                "\u0022 Description=\u0022{Policy_PS_FullTrust}\u0022/><PermissionSet class=\u0022" +
                "System.Security.NamedPermissionSet\u0022version=\u00221\u0022 Name=\u0022Everyth" +
                "ing\u0022 Description=\u0022{Policy_PS_Everything}\u0022><Permission class=\u0022" +
                "System.Security.Permissions.IsolatedStorageFilePermission, mscorlib, Version={VE" +
                "RSION}, Culture=neutral, PublicKeyToken=b77a5c561934e089\u0022version=\u00221\u0022" +
                " Unrestricted=\u0022true\u0022/><Permission class=\u0022System.Security.Permissi" +
                "ons.EnvironmentPermission, mscorlib, Version={VERSION}, Culture=neutral, PublicK" +
                "eyToken=b77a5c561934e089\u0022version=\u00221\u0022 Unrestricted=\u0022true\u0022" +
                "/><Permission class=\u0022System.Security.Permissions.FileIOPermission, mscorlib" +
                ", Version={VERSION}, Culture=neutral, PublicKeyToken=b77a5c561934e089\u0022versi" +
                "on=\u00221\u0022 Unrestricted=\u0022true\u0022/><Permission class=\u0022System.S" +
                "ecurity.Permissions.FileDialogPermission, mscorlib, Version={VERSION}, Culture=n" +
                "eutral, PublicKeyToken=b77a5c561934e089\u0022version=\u00221\u0022 Unrestricted=" +
                "\u0022true\u0022/><Permission class=\u0022System.Security.Permissions.Reflection" +
                "Permission, mscorlib, Version={VERSION}, Culture=neutral, PublicKeyToken=b77a5c5" +
                "61934e089\u0022version=\u00221\u0022 Unrestricted=\u0022true\u0022/><Permission " +
                "class=\u0022System.Security.Permissions.SecurityPermission, mscorlib, Version={V" +
                "ERSION}, Culture=neutral, PublicKeyToken=b77a5c561934e089\u0022version=\u00221\u0022" +
                " Flags=\u0022Assertion, UnmanagedCode, Execution, ControlThread, ControlEvidence" +
                ", ControlPolicy, ControlAppDomain, SerializationFormatter, ControlDomainPolicy, " +
                "ControlPrincipal, RemotingConfiguration, Infrastructure, BindingRedirects\u0022/" +
                "><Permission class=\u0022System.Security.Permissions.UIPermission, mscorlib, Ver" +
                "sion={VERSION}, Culture=neutral, PublicKeyToken=b77a5c561934e089\u0022version=\u0022" +
                "1\u0022 Unrestricted=\u0022true\u0022/><IPermission class=\u0022System.Net.Socke" +
                "tPermission, System, Version={VERSION}, Culture=neutral, PublicKeyToken=b77a5c56" +
                "1934e089\u0022version=\u00221\u0022 Unrestricted=\u0022true\u0022/><IPermission " +
                "class=\u0022System.Net.WebPermission, System, Version={VERSION}, Culture=neutral" +
                ", PublicKeyToken=b77a5c561934e089\u0022version=\u00221\u0022 Unrestricted=\u0022" +
                "true\u0022/><IPermission class=\u0022System.Net.DnsPermission, System, Version={" +
                "VERSION}, Culture=neutral, PublicKeyToken=b77a5c561934e089\u0022version=\u00221\u0022" +
                " Unrestricted=\u0022true\u0022/><IPermission class=\u0022System.Security.Permiss" +
                "ions.KeyContainerPermission, mscorlib, Version={VERSION}, Culture=neutral, Publi" +
                "cKeyToken=b77a5c561934e089\u0022version=\u00221\u0022 Unrestricted=\u0022true\u0022" +
                "/><Permission class=\u0022System.Security.Permissions.RegistryPermission, mscorl" +
                "ib, Version={VERSION}, Culture=neutral, PublicKeyToken=b77a5c561934e089\u0022ver" +
                "sion=\u00221\u0022 Unrestricted=\u0022true\u0022/><IPermission class=\u0022Syste" +
                "m.Drawing.Printing.PrintingPermission, System.Drawing, Version={VERSION}, Cultur" +
                "e=neutral, PublicKeyToken=b03f5f7f11d50a3a\u0022version=\u00221\u0022 Unrestrict" +
                "ed=\u0022true\u0022/><IPermission class=\u0022System.Diagnostics.EventLogPermiss" +
                "ion, System, Version={VERSION}, Culture=neutral, PublicKeyToken=b77a5c561934e089" +
                "\u0022version=\u00221\u0022 Unrestricted=\u0022true\u0022/><IPermission class=\u0022" +
                "System.Security.Permissions.StorePermission, System, Version={VERSION}, Culture=" +
                "neutral, PublicKeyToken=b77a5c561934e089\u0022 version=\u00221\u0022 Unrestricte" +
                "d=\u0022true\u0022/><IPermission class=\u0022System.Diagnostics.PerformanceCount" +
                "erPermission, System, Version={VERSION}, Culture=neutral, PublicKeyToken=b77a5c5" +
                "61934e089\u0022version=\u00221\u0022 Unrestricted=\u0022true\u0022/><IPermission" +
                " class=\u0022System.Data.OleDb.OleDbPermission, System.Data, Version={VERSION}, " +
                "Culture=neutral, PublicKeyToken=b77a5c561934e089\u0022 version=\u00221\u0022 Unr" +
                "estricted=\u0022true\u0022/><IPermission class=\u0022System.Data.SqlClient.SqlCl" +
                "ientPermission, System.Data, Version={VERSION}, Culture=neutral, PublicKeyToken=" +
                "b77a5c561934e089\u0022 version=\u00221\u0022 Unrestricted=\u0022true\u0022/><IPe" +
                "rmission class=\u0022System.Security.Permissions.DataProtectionPermission, Syste" +
                "m.Security, Version={VERSION}, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\u0022" +
                " version=\u00221\u0022 Unrestricted=\u0022true\u0022/></PermissionSet><Permissio" +
                "nSet class=\u0022System.Security.NamedPermissionSet\u0022version=\u00221\u0022 N" +
                "ame=\u0022Nothing\u0022 Description=\u0022{Policy_PS_Nothing}\u0022/><Permission" +
                "Set class=\u0022System.Security.NamedPermissionSet\u0022version=\u00221\u0022 Na" +
                "me=\u0022Execution\u0022 Description=\u0022{Policy_PS_Execution}\u0022><Permissi" +
                "on class=\u0022System.Security.Permissions.SecurityPermission, mscorlib, Version" +
                "={VERSION}, Culture=neutral, PublicKeyToken=b77a5c561934e089\u0022version=\u0022" +
                "1\u0022 Flags=\u0022Execution\u0022/></PermissionSet><PermissionSet class=\u0022" +
                "System.Security.NamedPermissionSet\u0022version=\u00221\u0022 Name=\u0022SkipVer" +
                "ification\u0022 Description=\u0022{Policy_PS_SkipVerification}\u0022><Permission" +
                " class=\u0022System.Security.Permissions.SecurityPermission, mscorlib, Version={" +
                "VERSION}, Culture=neutral, PublicKeyToken=b77a5c561934e089\u0022version=\u00221\u0022" +
                " Flags=\u0022SkipVerification\u0022/></PermissionSet></NamedPermissionSets>";

            int result = string.Compare("{Policy_PS_Nothing}", 0, veryLongString, 4380, 19, StringComparison.Ordinal);
            Assert.True(result < 0);

            result = "{Policy_PS_Nothing}".AsSpan().CompareTo(veryLongString.AsSpan(4380, 19), StringComparison.Ordinal);
            Assert.True(result < 0);
        }

        [Fact]
        public static void ZeroLengthCompareTo_StringComparison()
        {
            string value = "456";
            string s = value.Substring(2, 0);

            Assert.True(0 < string.Compare(value, s, StringComparison.Ordinal));

            Assert.True(0 < string.Compare(value, s, StringComparison.CurrentCulture));
            Assert.True(0 < string.Compare(value, s, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(0 < string.Compare(value, s, StringComparison.InvariantCulture));
            Assert.True(0 < string.Compare(value, s, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(0 < string.Compare(value, s, StringComparison.OrdinalIgnoreCase));

            string emptyValue = value.Substring(1, 0);
            Assert.Equal(0, string.Compare(emptyValue, s, StringComparison.Ordinal));

            Assert.Equal(0, string.Compare(emptyValue, s, StringComparison.CurrentCulture));
            Assert.Equal(0, string.Compare(emptyValue, s, StringComparison.CurrentCultureIgnoreCase));
            Assert.Equal(0, string.Compare(emptyValue, s, StringComparison.InvariantCulture));
            Assert.Equal(0, string.Compare(emptyValue, s, StringComparison.InvariantCultureIgnoreCase));
            Assert.Equal(0, string.Compare(emptyValue, s, StringComparison.OrdinalIgnoreCase));

            ReadOnlySpan<char> span = value.AsSpan();
            ReadOnlySpan<char> emptySlice = value.AsSpan(2, 0);
            Assert.True(0 < span.CompareTo(emptySlice, StringComparison.Ordinal));

            Assert.True(0 < span.CompareTo(emptySlice, StringComparison.CurrentCulture));
            Assert.True(0 < span.CompareTo(emptySlice, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(0 < span.CompareTo(emptySlice, StringComparison.InvariantCulture));
            Assert.True(0 < span.CompareTo(emptySlice, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(0 < span.CompareTo(emptySlice, StringComparison.OrdinalIgnoreCase));

            span = value.AsSpan(1, 0);
            Assert.Equal(0, span.CompareTo(emptySlice, StringComparison.Ordinal));

            Assert.Equal(0, span.CompareTo(emptySlice, StringComparison.CurrentCulture));
            Assert.Equal(0, span.CompareTo(emptySlice, StringComparison.CurrentCultureIgnoreCase));
            Assert.Equal(0, span.CompareTo(emptySlice, StringComparison.InvariantCulture));
            Assert.Equal(0, span.CompareTo(emptySlice, StringComparison.InvariantCultureIgnoreCase));
            Assert.Equal(0, span.CompareTo(emptySlice, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void SameValueCompareTo_StringComparison()
        {
            string value = "456";
            Assert.Equal(0, string.Compare(value, value, StringComparison.Ordinal));

            Assert.Equal(0, string.Compare(value, value, StringComparison.CurrentCulture));
            Assert.Equal(0, string.Compare(value, value, StringComparison.CurrentCultureIgnoreCase));
            Assert.Equal(0, string.Compare(value, value, StringComparison.InvariantCulture));
            Assert.Equal(0, string.Compare(value, value, StringComparison.InvariantCultureIgnoreCase));
            Assert.Equal(0, string.Compare(value, value, StringComparison.OrdinalIgnoreCase));

            ReadOnlySpan<char> span = value.AsSpan();
            Assert.Equal(0, span.CompareTo(span, StringComparison.Ordinal));

            Assert.Equal(0, span.CompareTo(span, StringComparison.CurrentCulture));
            Assert.Equal(0, span.CompareTo(span, StringComparison.CurrentCultureIgnoreCase));
            Assert.Equal(0, span.CompareTo(span, StringComparison.InvariantCulture));
            Assert.Equal(0, span.CompareTo(span, StringComparison.InvariantCultureIgnoreCase));
            Assert.Equal(0, span.CompareTo(span, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void LengthMismatchCompareTo_StringComparison()
        {
            string value = "456";

            string s1 = value.Substring(0, 2);
            string s2 = value.Substring(0, 3);
            Assert.True(0 > string.Compare(s1, s2, StringComparison.Ordinal));

            Assert.True(0 > string.Compare(s1, s2, StringComparison.CurrentCulture));
            Assert.True(0 > string.Compare(s1, s2, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(0 > string.Compare(s1, s2, StringComparison.InvariantCulture));
            Assert.True(0 > string.Compare(s1, s2, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(0 > string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase));

            ReadOnlySpan<char> span = value.AsSpan(0, 2);
            ReadOnlySpan<char> slice = value.AsSpan(0, 3);
            Assert.True(0 > span.CompareTo(slice, StringComparison.Ordinal));

            Assert.True(0 > span.CompareTo(slice, StringComparison.CurrentCulture));
            Assert.True(0 > span.CompareTo(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(0 > span.CompareTo(slice, StringComparison.InvariantCulture));
            Assert.True(0 > span.CompareTo(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(0 > span.CompareTo(slice, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void CompareToOverlappingMatch_StringComparison()
        {
            string value = "456565";

            string s1 = value.Substring(1, 3);
            string s2 = value.Substring(3, 3);
            Assert.Equal(0, string.Compare(s1, s2, StringComparison.Ordinal));

            Assert.Equal(0, string.Compare(s1, s2, StringComparison.CurrentCulture));
            Assert.Equal(0, string.Compare(s1, s2, StringComparison.CurrentCultureIgnoreCase));
            Assert.Equal(0, string.Compare(s1, s2, StringComparison.InvariantCulture));
            Assert.Equal(0, string.Compare(s1, s2, StringComparison.InvariantCultureIgnoreCase));
            Assert.Equal(0, string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase));

            ReadOnlySpan<char> span = value.AsSpan(1, 3);
            ReadOnlySpan<char> slice = value.AsSpan(3, 3);
            Assert.Equal(0, span.CompareTo(slice, StringComparison.Ordinal));

            Assert.Equal(0, span.CompareTo(slice, StringComparison.CurrentCulture));
            Assert.Equal(0, span.CompareTo(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.Equal(0, span.CompareTo(slice, StringComparison.InvariantCulture));
            Assert.Equal(0, span.CompareTo(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.Equal(0, span.CompareTo(slice, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void CompareToMatchDifferentInstances_StringComparison()
        {
            string sa = "4567";
            string sb = "456";

            string s1 = sa.Substring(0, 3);
            string s2 = sb.Substring(0, 3);

            Assert.Equal(0, string.Compare(s1, s2, StringComparison.Ordinal));

            Assert.Equal(0, string.Compare(s1, s2, StringComparison.CurrentCulture));
            Assert.Equal(0, string.Compare(s1, s2, StringComparison.CurrentCultureIgnoreCase));
            Assert.Equal(0, string.Compare(s1, s2, StringComparison.InvariantCulture));
            Assert.Equal(0, string.Compare(s1, s2, StringComparison.InvariantCultureIgnoreCase));
            Assert.Equal(0, string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase));

            ReadOnlySpan<char> span = sa.AsSpan(0, 3);
            ReadOnlySpan<char> slice = sb.AsSpan(0, 3);
            Assert.Equal(0, span.CompareTo(slice, StringComparison.Ordinal));

            Assert.Equal(0, span.CompareTo(slice, StringComparison.CurrentCulture));
            Assert.Equal(0, span.CompareTo(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.Equal(0, span.CompareTo(slice, StringComparison.InvariantCulture));
            Assert.Equal(0, span.CompareTo(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.Equal(0, span.CompareTo(slice, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void MakeSureNoCompareToChecksGoOutOfRange_StringComparison()
        {
            for (int length = 0; length < 100; length++)
            {
                var first = new char[length + 2];
                first[0] = (char)99;
                first[length + 1] = (char)99;
                var second = new char[length + 2];
                second[0] = (char)100;
                second[length + 1] = (char)100;

                var s1 = new string(first, 1, length);
                var s2 = new string(second, 1, length);
                Assert.Equal(0, string.Compare(s1, s2, StringComparison.Ordinal));

                Assert.Equal(0, string.Compare(s1, s2, StringComparison.CurrentCulture));
                Assert.Equal(0, string.Compare(s1, s2, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(0, string.Compare(s1, s2, StringComparison.InvariantCulture));
                Assert.Equal(0, string.Compare(s1, s2, StringComparison.InvariantCultureIgnoreCase));
                Assert.Equal(0, string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase));

                var span1 = new ReadOnlySpan<char>(first, 1, length);
                var span2 = new ReadOnlySpan<char>(second, 1, length);
                Assert.Equal(0, span1.CompareTo(span2, StringComparison.Ordinal));

                Assert.Equal(0, span1.CompareTo(span2, StringComparison.CurrentCulture));
                Assert.Equal(0, span1.CompareTo(span2, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(0, span1.CompareTo(span2, StringComparison.InvariantCulture));
                Assert.Equal(0, span1.CompareTo(span2, StringComparison.InvariantCultureIgnoreCase));
                Assert.Equal(0, span1.CompareTo(span2, StringComparison.OrdinalIgnoreCase));
            }
        }

        [Fact]
        public static void CompareToNoMatch_StringComparison()
        {
            for (int length = 1; length < 150; length++)
            {
                for (int mismatchIndex = 0; mismatchIndex < length; mismatchIndex++)
                {
                    var first = new char[length];
                    var second = new char[length];
                    for (int i = 0; i < length; i++)
                    {
                        first[i] = second[i] = (char)(i + 1);
                    }

                    second[mismatchIndex] = (char)(second[mismatchIndex] + 1);

                    string s1 = new string(first);
                    string s2 = new string(second);
                    Assert.True(0 >  string.Compare(s1, s2, StringComparison.Ordinal));

                    var firstSpan = new ReadOnlySpan<char>(first);
                    var secondSpan = new ReadOnlySpan<char>(second);
                    Assert.True(0 > firstSpan.CompareTo(secondSpan, StringComparison.Ordinal));

                    // Due to differences in the implementation, the exact result of CompareTo will not necessarily match with string.Compare.
                    // However, the sign will match, which is what defines correctness.
                    Assert.Equal(
                        Math.Sign(string.Compare(firstSpan.ToString(), secondSpan.ToString(), StringComparison.OrdinalIgnoreCase)),
                        Math.Sign(firstSpan.CompareTo(secondSpan, StringComparison.OrdinalIgnoreCase)));

                    Assert.Equal(
                        string.Compare(firstSpan.ToString(), secondSpan.ToString(), StringComparison.CurrentCulture),
                        firstSpan.CompareTo(secondSpan, StringComparison.CurrentCulture));
                    Assert.Equal(
                        string.Compare(firstSpan.ToString(), secondSpan.ToString(), StringComparison.CurrentCultureIgnoreCase),
                        firstSpan.CompareTo(secondSpan, StringComparison.CurrentCultureIgnoreCase));
                    Assert.Equal(
                        string.Compare(firstSpan.ToString(), secondSpan.ToString(), StringComparison.InvariantCulture),
                        firstSpan.CompareTo(secondSpan, StringComparison.InvariantCulture));
                    Assert.Equal(
                        string.Compare(firstSpan.ToString(), secondSpan.ToString(), StringComparison.InvariantCultureIgnoreCase),
                        firstSpan.CompareTo(secondSpan, StringComparison.InvariantCultureIgnoreCase));
                }
            }
        }

        [Fact]
        public static void CompareToUnknownComparisonType_StringComparison()
        {
            string value = "456";
            Assert.Throws<ArgumentException>(() => string.Compare(value, value, StringComparison.CurrentCulture - 1));
            Assert.Throws<ArgumentException>(() => string.Compare(value, value, StringComparison.OrdinalIgnoreCase + 1));
            Assert.Throws<ArgumentException>(() => string.Compare(value, value, (StringComparison)6));

            Assert.Throws<ArgumentException>(() => value.AsSpan().CompareTo(value.AsSpan(), StringComparison.CurrentCulture - 1));
            Assert.Throws<ArgumentException>(() => value.AsSpan().CompareTo(value.AsSpan(), StringComparison.OrdinalIgnoreCase + 1));
            Assert.Throws<ArgumentException>(() => value.AsSpan().CompareTo(value.AsSpan(), (StringComparison)6));
        }

        [Fact]
        public static void Compare_Invalid()
        {
            // Invalid comparison type
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => string.Compare("a", "bb", StringComparison.CurrentCulture - 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => string.Compare("a", "bb", StringComparison.OrdinalIgnoreCase + 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => string.Compare("a", 0, "bb", 0, 1, StringComparison.CurrentCulture - 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => string.Compare("a", 0, "bb", 0, 1, StringComparison.OrdinalIgnoreCase + 1));

            // IndexA < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset1", () => string.Compare("a", -1, "bb", 0, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("indexA", () => string.Compare("a", -1, "bb", 0, 1, StringComparison.CurrentCulture));

            // IndexA > stringA.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length1", () => string.Compare("a", 2, "bb", 0, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("indexA", () => string.Compare("a", 2, "bb", 0, 1, StringComparison.CurrentCulture));

            // IndexB < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset2", () => string.Compare("a", 0, "bb", -1, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("indexB", () => string.Compare("a", 0, "bb", -1, 1, StringComparison.CurrentCulture));

            // IndexB > stringB.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length2", () => string.Compare("a", 0, "bb", 3, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("indexB", () => string.Compare("a", 0, "bb", 3, 0, StringComparison.CurrentCulture));

            // Length < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length1", () => string.Compare("a", 0, "bb", 0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => string.Compare("a", 0, "bb", 0, -1, StringComparison.CurrentCulture));

            // There is a subtle behavior difference between the string.Compare that accepts a StringComparison parameter,
            // and the one that does not. The former includes short-circuiting logic for nulls BEFORE the length/
            // index parameters are validated (but after the StringComparison is), while the latter does not. As a result,
            // this will not throw:
            // string.Compare(null, -1, null, -1, -1, StringComparison.CurrentCulture)
            // but this will:
            // string.Compare(null, -1, null, -1, -1)

            // These tests ensure that the argument validation stays in order.

            // Compare accepting StringComparison
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => string.Compare(null, 0, null, 0, 0, StringComparison.CurrentCulture - 1)); // comparisonType should be validated before null short-circuiting...
            // Tests to ensure null is short-circuited before validating the arguments are in the Compare() theory
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => string.Compare("foo", -1, "foo", -1, -1, StringComparison.CurrentCulture)); // length should be validated before indexA/indexB
            AssertExtensions.Throws<ArgumentOutOfRangeException>("indexA", () => string.Compare("foo", -1, "foo", -1, 3, StringComparison.CurrentCulture)); // then indexA
            AssertExtensions.Throws<ArgumentOutOfRangeException>("indexB", () => string.Compare("foo", 0, "foo", -1, 3, StringComparison.CurrentCulture)); // then indexB
            // Then the optimization where we short-circuit if strA == strB && indexA == indexB, or length == 0, is tested in the Compare() theory.

            // Compare not accepting StringComparison
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length1", () => string.Compare(null, -1, null, -1, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length2", () => string.Compare(null, 0, "bar", 4, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset1", () => string.Compare(null, -1, null, -1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset2", () => string.Compare(null, 0, null, -1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("string1", () => string.Compare(null, 1, null, 1, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("string2", () => string.Compare("bar", 1, null, 1, 1));
        }

        [Fact]
        public static void CompareOrdinal_Invalid()
        {
            // IndexA < 0 or IndexA > strA.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("indexA", () => string.CompareOrdinal("a", -1, "bb", 0, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("indexA", () => string.CompareOrdinal("a", 6, "bb", 0, 0));

            // IndexB < 0 or IndexB > strB.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("indexB", () => string.CompareOrdinal("a", 0, "bb", -1, 0)); // IndexB < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("indexB", () => string.CompareOrdinal("a", 0, "bb", 3, 0)); // IndexB > strB.Length

            // We must validate arguments before any short-circuiting is done (besides for nulls)
            AssertExtensions.Throws<ArgumentOutOfRangeException>("indexA", () => string.CompareOrdinal("foo", -1, "foo", -1, 0)); // then indexA
            AssertExtensions.Throws<ArgumentOutOfRangeException>("indexB", () => string.CompareOrdinal("foo", 0, "foo", -1, 0)); // then indexB
            AssertExtensions.Throws<ArgumentOutOfRangeException>("indexA", () => string.CompareOrdinal("foo", 4, "foo", 4, 0)); // indexA > strA.Length first
            AssertExtensions.Throws<ArgumentOutOfRangeException>("indexB", () => string.CompareOrdinal("foo", 3, "foo", 4, 0)); // then indexB > strB.Length
        }

        [Fact]
        public static void CompareOrdinal_NegativeLength_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", "count", () => string.CompareOrdinal("a", 0, "bb", 0, -1));

            // length should be validated first
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", "count", () => string.CompareOrdinal("foo", -1, "foo", -1, -1));

            // early return should not kick in if length is invalid
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", "count", () => string.CompareOrdinal("foo", 0, "foo", 0, -1));
        }

        [Theory]
        [InlineData("Hello", "ello", true)]
        [InlineData("Hello", "ELL", false)]
        [InlineData("Hello", "Larger Hello", false)]
        [InlineData("Hello", "Goodbye", false)]
        [InlineData("", "", true)]
        [InlineData("", "hello", false)]
        [InlineData("Hello", "", true)]
        public static void Contains_String(string s, string value, bool expected)
        {
            Assert.Equal(expected, s.Contains(value));
            Assert.Equal(expected, s.AsSpan().Contains(value.AsSpan(), StringComparison.Ordinal));
        }

        [Fact]
        public static void Contains_NullValue_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => "foo".Contains(null));
        }

        [Fact]
        public static void ZeroLengthContains_StringComparison()
        {
            var a = new char[3];

            string s1 = new string(a);
            string s2 = new string(a, 2, 0);
            Assert.True(s1.Contains(s2));

            s1 = string.Empty;
            Assert.True(s1.Contains(s2));

            var span = new ReadOnlySpan<char>(a);
            var emptySlice = new ReadOnlySpan<char>(a, 2, 0);
            Assert.True(span.Contains(emptySlice, StringComparison.Ordinal));

            Assert.True(span.Contains(emptySlice, StringComparison.CurrentCulture));
            Assert.True(span.Contains(emptySlice, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(span.Contains(emptySlice, StringComparison.InvariantCulture));
            Assert.True(span.Contains(emptySlice, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(span.Contains(emptySlice, StringComparison.OrdinalIgnoreCase));

            span = ReadOnlySpan<char>.Empty;
            Assert.True(span.Contains(emptySlice, StringComparison.Ordinal));

            Assert.True(span.Contains(emptySlice, StringComparison.CurrentCulture));
            Assert.True(span.Contains(emptySlice, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(span.Contains(emptySlice, StringComparison.InvariantCulture));
            Assert.True(span.Contains(emptySlice, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(span.Contains(emptySlice, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void SameSpanContains_StringComparison()
        {
            string s1 = "456";
            Assert.True(s1.Contains(s1));

            ReadOnlySpan<char> span = s1.AsSpan();
            Assert.True(span.Contains(span, StringComparison.Ordinal));

            Assert.True(span.Contains(span, StringComparison.CurrentCulture));
            Assert.True(span.Contains(span, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(span.Contains(span, StringComparison.InvariantCulture));
            Assert.True(span.Contains(span, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(span.Contains(span, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void LengthMismatchContains_StringComparison()
        {
            string value = "456";

            string s1 = value.Substring(0, 2);
            string s2 = value.Substring(0, 3);
            Assert.False(s1.Contains(s2));

            ReadOnlySpan<char> span = value.AsSpan(0, 2);
            ReadOnlySpan<char> slice = value.AsSpan(0, 3);
            Assert.False(span.Contains(slice, StringComparison.Ordinal));

            Assert.False(span.Contains(slice, StringComparison.CurrentCulture));
            Assert.False(span.Contains(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.False(span.Contains(slice, StringComparison.InvariantCulture));
            Assert.False(span.Contains(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.False(span.Contains(slice, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void ContainsMatch_StringComparison()
        {
            string value = "456";

            string s1 = value.Substring(0, 3);
            string s2 = value.Substring(0, 2);
            Assert.True(s1.Contains(s2));

            ReadOnlySpan<char> span = value.AsSpan(0, 3);
            ReadOnlySpan<char> slice = value.AsSpan(0 ,2);
            Assert.True(span.Contains(slice, StringComparison.Ordinal));

            Assert.True(span.Contains(slice, StringComparison.CurrentCulture));
            Assert.True(span.Contains(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(span.Contains(slice, StringComparison.InvariantCulture));
            Assert.True(span.Contains(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(span.Contains(slice, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void ContainsMatchDifferentSpans_StringComparison()
        {
            string value1 = "4567";
            string value2 = "456";

            string s1 = value1.Substring(0, 3);
            string s2 = value2.Substring(0, 3);
            Assert.True(s1.Contains(s2));

            ReadOnlySpan<char> span = value1.AsSpan(0, 3);
            ReadOnlySpan<char> slice = value2.AsSpan(0, 3);
            Assert.True(span.Contains(slice, StringComparison.Ordinal));

            Assert.True(span.Contains(slice, StringComparison.CurrentCulture));
            Assert.True(span.Contains(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(span.Contains(slice, StringComparison.InvariantCulture));
            Assert.True(span.Contains(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(span.Contains(slice, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void ContainsNoMatch_StringComparison()
        {
            for (int length = 1; length < 150; length++)
            {
                for (int mismatchIndex = 0; mismatchIndex < length; mismatchIndex++)
                {
                    var first = new char[length];
                    var second = new char[length];
                    for (int i = 0; i < length; i++)
                    {
                        first[i] = second[i] = (char)(i + 1);
                    }

                    second[mismatchIndex] = (char)(second[mismatchIndex] + 1);

                    string s1 = new string(first);
                    string s2 = new string(second);
                    Assert.False(s1.Contains(s2));

                    var firstSpan = new ReadOnlySpan<char>(first);
                    var secondSpan = new ReadOnlySpan<char>(second);
                    Assert.False(firstSpan.Contains(secondSpan, StringComparison.Ordinal));

                    Assert.False(firstSpan.Contains(secondSpan, StringComparison.OrdinalIgnoreCase));

                    // Different behavior depending on OS
                    Assert.Equal(
                        firstSpan.ToString().StartsWith(secondSpan.ToString(), StringComparison.CurrentCulture),
                        firstSpan.Contains(secondSpan, StringComparison.CurrentCulture));
                    Assert.Equal(
                        firstSpan.ToString().StartsWith(secondSpan.ToString(), StringComparison.CurrentCultureIgnoreCase),
                        firstSpan.Contains(secondSpan, StringComparison.CurrentCultureIgnoreCase));
                    Assert.Equal(
                        firstSpan.ToString().StartsWith(secondSpan.ToString(), StringComparison.InvariantCulture),
                        firstSpan.Contains(secondSpan, StringComparison.InvariantCulture));
                    Assert.Equal(
                        firstSpan.ToString().StartsWith(secondSpan.ToString(), StringComparison.InvariantCultureIgnoreCase),
                        firstSpan.Contains(secondSpan, StringComparison.InvariantCultureIgnoreCase));
                }
            }
        }

        [Fact]
        public static void MakeSureNoContainsChecksGoOutOfRange_StringComparison()
        {
            for (int length = 0; length < 100; length++)
            {
                var first = new char[length + 2];
                first[0] = (char)99;
                first[length + 1] = (char)99;
                var second = new char[length + 2];
                second[0] = (char)100;
                second[length + 1] = (char)100;

                string s1 = new string(first, 1, length);
                string s2 = new string(second, 1, length);
                Assert.True(s1.Contains(s2));

                var span1 = new ReadOnlySpan<char>(first, 1, length);
                var span2 = new ReadOnlySpan<char>(second, 1, length);
                Assert.True(span1.Contains(span2, StringComparison.Ordinal));

                Assert.True(span1.Contains(span2, StringComparison.CurrentCulture));
                Assert.True(span1.Contains(span2, StringComparison.CurrentCultureIgnoreCase));
                Assert.True(span1.Contains(span2, StringComparison.InvariantCulture));
                Assert.True(span1.Contains(span2, StringComparison.InvariantCultureIgnoreCase));
                Assert.True(span1.Contains(span2, StringComparison.OrdinalIgnoreCase));
            }
        }

        [Fact]
        public static void ContainsUnknownComparisonType_StringComparison()
        {
            string value = "456";
            Assert.Throws<ArgumentException>(() => value.AsSpan().CompareTo(value.AsSpan(), StringComparison.CurrentCulture - 1));
            Assert.Throws<ArgumentException>(() => value.AsSpan().CompareTo(value.AsSpan(), StringComparison.OrdinalIgnoreCase + 1));
            Assert.Throws<ArgumentException>(() => value.AsSpan().CompareTo(value.AsSpan(), (StringComparison)6));
        }

        [Fact]
        public static void ZeroLengthSequenceCompareTo_Char()
        {
            var a = new char[3];

            string s1 = new string(a, 1, 0);
            string s2 = new string(a, 2, 0);
            int result = s1.CompareTo(s1);
            Assert.Equal(0, result);

            ReadOnlySpan<char> first = s1.AsSpan();
            ReadOnlySpan<char> second = s2.AsSpan();
            result = first.SequenceCompareTo<char>(second);
            Assert.Equal(0, result);
        }

        [Fact]
        public static void SameSpanSequenceCompareTo_Char()
        {
            string s1 = "456";
            int result = s1.CompareTo(s1);
            Assert.Equal(0, result);

            ReadOnlySpan<char> span = s1.AsSpan();
            result = span.SequenceCompareTo<char>(span);
            Assert.Equal(0, result);
        }

        [Fact]
        public static void SequenceCompareToArrayImplicit_Char()
        {
            string s1 = "456";
            int result = s1.CompareTo(s1);
            Assert.Equal(0, result);

            ReadOnlySpan<char> first = s1.AsSpan(0, 3);
            result = first.SequenceCompareTo<char>(s1.AsSpan());
            Assert.Equal(0, result);
        }

        [Fact]
        public static void SequenceCompareToArraySegmentImplicit_Char()
        {
            char[] src = { '1', '2', '3' };
            char[] dst = { '5', '1', '2', '3', '9' };
            var segment = new ArraySegment<char>(dst, 1, 3);

            string s1 = new string(src, 0, 3);
            int result = s1.CompareTo(segment.ToString());

            ReadOnlySpan<char> first = s1.AsSpan();
            result = first.SequenceCompareTo<char>(segment);
            Assert.Equal(0, result);
        }

        [Fact]
        public static void LengthMismatchSequenceCompareTo_Char()
        {
            string value = "456";

            string s1 = value.Substring(0, 2);
            string s2 = value.Substring(0, 3);
            int result = s1.CompareTo(s2);
            Assert.True(result < 0);

            result = s2.CompareTo(s1);
            Assert.True(result > 0);

            // one sequence is empty
            s1 = value.Substring(1, 0);

            result = s1.CompareTo(s2);
            Assert.True(result < 0);

            result = s2.CompareTo(s1);
            Assert.True(result > 0);

            ReadOnlySpan<char> first = s1.AsSpan();
            ReadOnlySpan<char> second = s2.AsSpan();
            result = first.SequenceCompareTo<char>(second);
            Assert.True(result < 0);

            result = second.SequenceCompareTo<char>(first);
            Assert.True(result > 0);

            // one sequence is empty
            first = value.AsSpan(1, 0);

            result = first.SequenceCompareTo<char>(second);
            Assert.True(result < 0);

            result = second.SequenceCompareTo<char>(first);
            Assert.True(result > 0);
        }

        [Fact]
        public static void SequenceCompareToWithSingleMismatch_Char()
        {
            for (int length = 1; length < 32; length++)
            {
                for (int mismatchIndex = 0; mismatchIndex < length; mismatchIndex++)
                {
                    var first = new char[length];
                    var second = new char[length];
                    for (int i = 0; i < length; i++)
                    {
                        first[i] = second[i] = (char)(i + 1);
                    }

                    second[mismatchIndex] = (char)(second[mismatchIndex] + 1);

                    string s1 = new string(first);
                    string s2 = new string(second);
                    int result = string.Compare(s1, s2, StringComparison.Ordinal);
                    Assert.True(result < 0);

                    result = string.Compare(s2, s1, StringComparison.Ordinal);
                    Assert.True(result > 0);

                    ReadOnlySpan<char> firstSpan = s1.AsSpan();
                    ReadOnlySpan<char> secondSpan = s2.AsSpan();
                    result = firstSpan.SequenceCompareTo<char>(secondSpan);
                    Assert.True(result < 0);

                    result = secondSpan.SequenceCompareTo<char>(firstSpan);
                    Assert.True(result > 0);
                }
            }
        }

        [Fact]
        public static void SequenceCompareToNoMatch_Char()
        {
            for (int length = 1; length < 32; length++)
            {
                var first = new char[length];
                var second = new char[length];

                for (int i = 0; i < length; i++)
                {
                    first[i] = (char)(i + 1);
                    second[i] = (char)(char.MaxValue - i);
                }

                string s1 = new string(first);
                string s2 = new string(second);
                int result = string.Compare(s1, s2, StringComparison.Ordinal);
                Assert.True(result < 0);

                result = string.Compare(s2, s1, StringComparison.Ordinal);
                Assert.True(result > 0);

                ReadOnlySpan<char> firstSpan = s1.AsSpan();
                ReadOnlySpan<char> secondSpan = s2.AsSpan();
                result = firstSpan.SequenceCompareTo<char>(secondSpan);
                Assert.True(result < 0);

                result = secondSpan.SequenceCompareTo<char>(firstSpan);
                Assert.True(result > 0);
            }
        }

        [Fact]
        public static void MakeSureNoSequenceCompareToChecksGoOutOfRange_Char()
        {
            for (int length = 0; length < 100; length++)
            {
                var first = new char[length + 2];
                first[0] = '8';
                first[length + 1] = '8';

                var second = new char[length + 2];
                second[0] = '9';
                second[length + 1] = '9';

                string s1 = new string(first, 1, length);
                string s2 = new string(second, 1, length);
                int result = string.Compare(s1, s2);
                Assert.Equal(0, result);

                ReadOnlySpan<char> span1 = s1.AsSpan();
                ReadOnlySpan<char> span2 = s2.AsSpan();
                result = span1.SequenceCompareTo<char>(span2);
                Assert.Equal(0, result);
            }
        }

        [Fact]
        public static void ZeroLengthSequenceEqual_Char()
        {
            char[] a = new char[3];

            string s1 = new string(a, 1, 0);
            string s2 = new string(a, 2, 0);
            bool b = s1.SequenceEqual(s2);
            Assert.True(b);

            ReadOnlySpan<char> first = s1.AsSpan();
            ReadOnlySpan<char> second = s2.AsSpan();
            b = first.SequenceEqual(second);
            Assert.True(b);
        }

        [Fact]
        public static void SameSpanSequenceEqual_Char()
        {
            char[] a = { '4', '5', '6' };

            string s1 = new string(a);
            bool b = s1.SequenceEqual(s1);
            Assert.True(b);

            ReadOnlySpan<char> span = s1.AsSpan();
            b = span.SequenceEqual(span);
            Assert.True(b);
        }

        [Fact]
        public static void LengthMismatchSequenceEqual_Char()
        {
            char[] a = { '4', '5', '6' };

            string s1 = new string(a, 0, 3);
            string s2 = new string(a, 0, 2);
            bool b = s1.SequenceEqual(s2);
            Assert.False(b);

            ReadOnlySpan<char> first = s1.AsSpan();
            ReadOnlySpan<char> second = s2.AsSpan();
            b = first.SequenceEqual(second);
            Assert.False(b);
        }

        [Fact]
        public static void SequenceEqualNoMatch_Char()
        {
            for (int length = 1; length < 32; length++)
            {
                for (int mismatchIndex = 0; mismatchIndex < length; mismatchIndex++)
                {
                    char[] first = new char[length];
                    char[] second = new char[length];
                    for (int i = 0; i < length; i++)
                    {
                        first[i] = second[i] = (char)(i + 1);
                    }

                    second[mismatchIndex] = (char)(second[mismatchIndex] + 1);

                    string s1 = new string(first);
                    string s2 = new string(second);
                    bool b = s1.SequenceEqual(s2);
                    Assert.False(b);

                    ReadOnlySpan<char> firstSpan = s1.AsSpan();
                    ReadOnlySpan<char> secondSpan = s2.AsSpan();
                    b = firstSpan.SequenceEqual(secondSpan);
                    Assert.False(b);
                }
            }
        }

        [Fact]
        public static void MakeSureNoSequenceEqualChecksGoOutOfRange_Char()
        {
            for (int length = 0; length < 100; length++)
            {
                char[] first = new char[length + 2];
                first[0] = '9';
                first[length + 1] = '9';
                char[] second = new char[length + 2];
                second[0] = 'a';
                second[length + 1] = 'a';

                string s1 = new string(first, 1, length);
                string s2 = new string(second, 1, length);
                bool b = s1.SequenceEqual(s2);
                Assert.True(b);

                ReadOnlySpan<char> span1 = s1.AsSpan();
                ReadOnlySpan<char> span2 = s2.AsSpan();
                b = span1.SequenceEqual(span2);
                Assert.True(b);
            }
        }

        public static IEnumerable<object[]> EndsWith_StringComparison_TestData()
        {
            // CurrentCulture
            yield return new object[] { "", "Foo", StringComparison.CurrentCulture, false };
            yield return new object[] { "Hello", "llo", StringComparison.CurrentCulture, true };
            yield return new object[] { "Hello", "Hello", StringComparison.CurrentCulture, true };
            yield return new object[] { "Hello", "", StringComparison.CurrentCulture, true };
            yield return new object[] { "Hello", "HELLO", StringComparison.CurrentCulture, false };
            yield return new object[] { "Hello", "Abc", StringComparison.CurrentCulture, false };
            yield return new object[] { "", "", StringComparison.CurrentCulture, true };
            yield return new object[] { "", "a", StringComparison.CurrentCulture, false };

            if (PlatformDetection.IsNotInvariantGlobalization)
                yield return new object[] { "Hello", "llo" + SoftHyphen, StringComparison.CurrentCulture, true };

            // CurrentCultureIgnoreCase
            yield return new object[] { "Hello", "llo", StringComparison.CurrentCultureIgnoreCase, true };
            yield return new object[] { "Hello", "Hello", StringComparison.CurrentCultureIgnoreCase, true };
            yield return new object[] { "Hello", "", StringComparison.CurrentCultureIgnoreCase, true };
            yield return new object[] { "Hello", "LLO", StringComparison.CurrentCultureIgnoreCase, true };
            yield return new object[] { "Hello", "Abc", StringComparison.CurrentCultureIgnoreCase, false };
            yield return new object[] { "", "", StringComparison.CurrentCultureIgnoreCase, true };
            yield return new object[] { "", "a", StringComparison.CurrentCultureIgnoreCase, false };

            if (PlatformDetection.IsNotInvariantGlobalization)
                yield return new object[] { "Hello", "llo" + SoftHyphen, StringComparison.CurrentCultureIgnoreCase, true };

            // InvariantCulture
            yield return new object[] { "", "Foo", StringComparison.InvariantCulture, false };
            yield return new object[] { "Hello", "llo", StringComparison.InvariantCulture, true };
            yield return new object[] { "Hello", "Hello", StringComparison.InvariantCulture, true };
            yield return new object[] { "Hello", "", StringComparison.InvariantCulture, true };
            yield return new object[] { "Hello", "HELLO", StringComparison.InvariantCulture, false };
            yield return new object[] { "Hello", "Abc", StringComparison.InvariantCulture, false };
            yield return new object[] { "", "", StringComparison.InvariantCulture, true };
            yield return new object[] { "", "a", StringComparison.InvariantCulture, false };

            if (PlatformDetection.IsNotInvariantGlobalization)
                yield return new object[] { "Hello", "llo" + SoftHyphen, StringComparison.InvariantCulture, true };

            // InvariantCultureIgnoreCase
            yield return new object[] { "Hello", "llo", StringComparison.InvariantCultureIgnoreCase, true };
            yield return new object[] { "Hello", "Hello", StringComparison.InvariantCultureIgnoreCase, true };
            yield return new object[] { "Hello", "", StringComparison.InvariantCultureIgnoreCase, true };
            yield return new object[] { "Hello", "LLO", StringComparison.InvariantCultureIgnoreCase, true };
            yield return new object[] { "Hello", "Abc", StringComparison.InvariantCultureIgnoreCase, false };
            yield return new object[] { "", "", StringComparison.InvariantCultureIgnoreCase, true };
            yield return new object[] { "", "a", StringComparison.InvariantCultureIgnoreCase, false };

            if (PlatformDetection.IsNotInvariantGlobalization)
                yield return new object[] { "Hello", "llo" + SoftHyphen, StringComparison.InvariantCultureIgnoreCase, true };

            // Ordinal
            yield return new object[] { "Hello", "o", StringComparison.Ordinal, true };
            yield return new object[] { "Hello", "llo", StringComparison.Ordinal, true };
            yield return new object[] { "Hello", "Hello", StringComparison.Ordinal, true };
            yield return new object[] { "Hello", "Larger Hello", StringComparison.Ordinal, false };
            yield return new object[] { "Hello", "", StringComparison.Ordinal, true };
            yield return new object[] { "Hello", "LLO", StringComparison.Ordinal, false };
            yield return new object[] { "Hello", "Abc", StringComparison.Ordinal, false };
            yield return new object[] { "Hello", "llo" + SoftHyphen, StringComparison.Ordinal, false };
            yield return new object[] { "", "", StringComparison.Ordinal, true };
            yield return new object[] { "", "a", StringComparison.Ordinal, false };

            // OrdinalIgnoreCase
            yield return new object[] { "Hello", "llo", StringComparison.OrdinalIgnoreCase, true };
            yield return new object[] { "Hello", "Hello", StringComparison.OrdinalIgnoreCase, true };
            yield return new object[] { "Hello", "Larger Hello", StringComparison.OrdinalIgnoreCase, false };
            yield return new object[] { "Hello", "", StringComparison.OrdinalIgnoreCase, true };
            yield return new object[] { "Hello", "LLO", StringComparison.OrdinalIgnoreCase, true };
            yield return new object[] { "Hello", "Abc", StringComparison.OrdinalIgnoreCase, false };
            yield return new object[] { "Hello", "llo" + SoftHyphen, StringComparison.OrdinalIgnoreCase, false };
            yield return new object[] { "", "", StringComparison.OrdinalIgnoreCase, true };
            yield return new object[] { "", "a", StringComparison.OrdinalIgnoreCase, false };
        }

        [Theory]
        [MemberData(nameof(EndsWith_StringComparison_TestData))]
        public static void EndsWith_StringComparison(string s, string value, StringComparison comparisonType, bool expected)
        {
            if (comparisonType == StringComparison.CurrentCulture)
            {
                Assert.Equal(expected, s.EndsWith(value));
            }
            Assert.Equal(expected, s.EndsWith(value, comparisonType));

            // Cannot use implicit cast from string to ReadOnlySpan for other runtimes, like .NET Framework. Therefore, explicitly call AsSpan.
            Assert.Equal(expected, s.AsSpan().EndsWith(value.AsSpan(), comparisonType));
        }

        // NOTE: This is by design. ICU ignores the null characters (i.e. null characters have no weights for the string comparison).
        // For desired behavior, use ordinal comparison instead of linguistic comparison.
        // This is a known difference between NLS and ICU (https://github.com/dotnet/runtime/issues/4673).
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNlsGlobalization))]
        [InlineData(StringComparison.CurrentCulture)]
        [InlineData(StringComparison.CurrentCultureIgnoreCase)]
        [InlineData(StringComparison.Ordinal)]
        [InlineData(StringComparison.OrdinalIgnoreCase)]
        public static void EndsWith_NullInStrings(StringComparison comparison)
        {
            Assert.True("\0test".EndsWith("test", comparison));
            Assert.True("te\0st".EndsWith("e\0st", comparison));
            Assert.False("te\0st".EndsWith("test", comparison));
            Assert.False("test\0".EndsWith("test", comparison));
            Assert.False("test".EndsWith("\0st", comparison));

            Assert.True("\0test".AsSpan().EndsWith("test".AsSpan(), comparison));
            Assert.True("te\0st".AsSpan().EndsWith("e\0st".AsSpan(), comparison));
            Assert.False("te\0st".AsSpan().EndsWith("test".AsSpan(), comparison));
            Assert.False("test\0".AsSpan().EndsWith("test".AsSpan(), comparison));
            Assert.False("test".AsSpan().EndsWith("\0st".AsSpan(), comparison));
        }

        // NOTE: This is by design. ICU ignores the null characters (i.e. null characters have no weights for the string comparison).
        // For desired behavior, use ordinal comparison instead of linguistic comparison.
        // This is a known difference between NLS and ICU (https://github.com/dotnet/runtime/issues/4673).
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNlsGlobalization))]
        [InlineData(StringComparison.InvariantCulture)]
        [InlineData(StringComparison.InvariantCultureIgnoreCase)]
        public static void EndsWith_NullInStrings_NonOrdinal(StringComparison comparison)
        {
            Assert.True("\0test".EndsWith("test", comparison));
            Assert.True("te\0st".EndsWith("e\0st", comparison));
            Assert.False("te\0st".EndsWith("test", comparison));
            Assert.False("test\0".EndsWith("test", comparison));
            Assert.False("test".EndsWith("\0st", comparison));

            Assert.True("\0test".AsSpan().EndsWith("test".AsSpan(), comparison));
            Assert.True("te\0st".AsSpan().EndsWith("e\0st".AsSpan(), comparison));
            Assert.False("te\0st".AsSpan().EndsWith("test".AsSpan(), comparison));
            Assert.False("test\0".AsSpan().EndsWith("test".AsSpan(), comparison));
            Assert.False("test".AsSpan().EndsWith("\0st".AsSpan(), comparison));
        }

        [Fact]
        public static void EndsWith_StringBoolCultureInfo_Valid()
        {
#pragma warning disable 0618 // suppress obsolete warning for String.Copy
            // Same string
            string s = "foo";
            Assert.True(s.EndsWith(s, false, null));
            Assert.True(s.EndsWith(s, true, null));
            Assert.True(s.EndsWith(s, false, CultureInfo.InvariantCulture));
            Assert.True(s.EndsWith(s, true, CultureInfo.InvariantCulture));

            // Different object, same string, no culture
            Assert.True(s.EndsWith(string.Copy(s), false, null));
            Assert.True(s.EndsWith(string.Copy(s), true, null));

            // Different object, same string, invariant culture
            Assert.True(s.EndsWith(string.Copy(s), false, CultureInfo.InvariantCulture));
            Assert.True(s.EndsWith(string.Copy(s), true, CultureInfo.InvariantCulture));

            // Different object, same string, current culture
            Assert.True(s.EndsWith(string.Copy(s), false, CultureInfo.InvariantCulture));
            Assert.True(s.EndsWith(string.Copy(s), true, CultureInfo.InvariantCulture));
#pragma warning restore 0618 // restore warning when accessing obsolete members
        }

        [Fact]
        public static void EndsWith_Invalid()
        {
            // Value is null
            AssertExtensions.Throws<ArgumentNullException>("value", () => "foo".EndsWith(null));
            AssertExtensions.Throws<ArgumentNullException>("value", () => "foo".EndsWith(null, StringComparison.CurrentCulture));

            // Invalid comparison type with empty string
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "foo".EndsWith("", StringComparison.CurrentCulture - 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "foo".EndsWith("", StringComparison.OrdinalIgnoreCase + 1));

            // Invalid comparison type with same string
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "foo".EndsWith("foo", StringComparison.CurrentCulture - 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "foo".EndsWith("foo", StringComparison.OrdinalIgnoreCase + 1));

            // Invalid comparison type with non-empty different string
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "foo".EndsWith("a", StringComparison.CurrentCulture - 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "foo".EndsWith("a", StringComparison.OrdinalIgnoreCase + 1));
        }

        [Fact]
        public static void ZeroLengthEndsWith_Char()
        {
            var a = new char[3];

            string s1 = new string(a);
            string s2 = new string(a, 2, 0);
            bool b = s1.EndsWith(s2);
            Assert.True(b);

            var span = new ReadOnlySpan<char>(a);
            var emptySlice = new ReadOnlySpan<char>(a, 2, 0);
            b = span.EndsWith<char>(emptySlice);
            Assert.True(b);
        }

        [Fact]
        public static void SameSpanEndsWith_Char()
        {
            string s = "456";
            bool b = s.EndsWith(s);
            Assert.True(b);

            ReadOnlySpan<char> span = s.AsSpan();
            b = span.EndsWith(span);
            Assert.True(b);
        }

        [Fact]
        public static void LengthMismatchEndsWith_Char()
        {
            string value = "456";

            string s1 = value.Substring(0, 2);
            string s2 = value.Substring(0, 3);
            bool b = s1.EndsWith(s2);
            Assert.False(b);

            ReadOnlySpan<char> span = value.AsSpan(0, 2);
            ReadOnlySpan<char> slice = value.AsSpan(0, 3);
            b = span.EndsWith(slice);
            Assert.False(b);
        }

        [Fact]
        public static void EndsWithMatch_Char()
        {
            string value = "456";

            string s1 = value.Substring(0, 3);
            string s2 = value.Substring(1, 2);
            bool b = s1.EndsWith(s2);
            Assert.True(b);

            ReadOnlySpan<char> span = value.AsSpan(0, 3);
            ReadOnlySpan<char> slice = value.AsSpan(1, 2);
            b = span.EndsWith(slice);
            Assert.True(b);
        }

        [Fact]
        public static void EndsWithMatchDifferentSpans_Char()
        {
            string value1 = "456";
            string value2 = "456";

            string s1 = value1.Substring(0, 3);
            string s2 = value2.Substring(0, 3);
            bool c = s1.EndsWith(s2);
            Assert.True(c);

            ReadOnlySpan<char> span = value1.AsSpan(0, 3);
            ReadOnlySpan<char> slice = value2.AsSpan(0 ,3);
            c = span.EndsWith(slice);
            Assert.True(c);
        }

        [Fact]
        public static void EndsWithNoMatch_Char()
        {
            for (int length = 1; length < 32; length++)
            {
                for (int mismatchIndex = 0; mismatchIndex < length; mismatchIndex++)
                {
                    var first = new char[length];
                    var second = new char[length];
                    for (int i = 0; i < length; i++)
                    {
                        first[i] = second[i] = (char)(i + 1);
                    }

                    second[mismatchIndex] = (char)(second[mismatchIndex] + 1);

                    string s1 = new string(first);
                    string s2 = new string(second);

                    // On ICU there are some characters in the range of 0~32 which have a sort weight.
                    // For example null character on ICU will be ignored if it is compared to anything
                    // while on NLS null will be always compared as ordinal.
                    // For desired behavior, use ordinal comparison instead of linguistic comparison.
                    // This is a known difference between NLS and ICU (https://github.com/dotnet/runtime/issues/4673).
                    bool b = s1.EndsWith(s2, StringComparison.Ordinal);
                    Assert.False(b);

                    var firstSpan = new ReadOnlySpan<char>(first);
                    var secondSpan = new ReadOnlySpan<char>(second);
                    b = firstSpan.EndsWith(secondSpan);
                    Assert.False(b);
                }
            }
        }

        [Fact]
        public static void ZeroLengthEndsWith_StringComparison()
        {
            var a = new char[3];

            string s1 = new string(a);
            string s2 = new string(a, 2, 0);
            Assert.True(s1.EndsWith(s2, StringComparison.Ordinal));

            Assert.True(s1.EndsWith(s2, StringComparison.CurrentCulture));
            Assert.True(s1.EndsWith(s2, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(s1.EndsWith(s2, StringComparison.InvariantCulture));
            Assert.True(s1.EndsWith(s2, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(s1.EndsWith(s2, StringComparison.OrdinalIgnoreCase));

            s1 = string.Empty;
            Assert.True(s1.EndsWith(s2, StringComparison.Ordinal));

            Assert.True(s1.EndsWith(s2, StringComparison.CurrentCulture));
            Assert.True(s1.EndsWith(s2, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(s1.EndsWith(s2, StringComparison.InvariantCulture));
            Assert.True(s1.EndsWith(s2, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(s1.EndsWith(s2, StringComparison.OrdinalIgnoreCase));

            var span = new ReadOnlySpan<char>(a);
            var emptySlice = new ReadOnlySpan<char>(a, 2, 0);
            Assert.True(span.EndsWith(emptySlice, StringComparison.Ordinal));

            Assert.True(span.EndsWith(emptySlice, StringComparison.CurrentCulture));
            Assert.True(span.EndsWith(emptySlice, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(span.EndsWith(emptySlice, StringComparison.InvariantCulture));
            Assert.True(span.EndsWith(emptySlice, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(span.EndsWith(emptySlice, StringComparison.OrdinalIgnoreCase));

            span = ReadOnlySpan<char>.Empty;
            Assert.True(span.EndsWith(emptySlice, StringComparison.Ordinal));

            Assert.True(span.EndsWith(emptySlice, StringComparison.CurrentCulture));
            Assert.True(span.EndsWith(emptySlice, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(span.EndsWith(emptySlice, StringComparison.InvariantCulture));
            Assert.True(span.EndsWith(emptySlice, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(span.EndsWith(emptySlice, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void SameSpanEndsWith_StringComparison()
        {
            string s = "456";
            Assert.True(s.EndsWith(s, StringComparison.Ordinal));

            Assert.True(s.EndsWith(s, StringComparison.CurrentCulture));
            Assert.True(s.EndsWith(s, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(s.EndsWith(s, StringComparison.InvariantCulture));
            Assert.True(s.EndsWith(s, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(s.EndsWith(s, StringComparison.OrdinalIgnoreCase));

            ReadOnlySpan<char> span = s.AsSpan();
            Assert.True(span.EndsWith(span, StringComparison.Ordinal));

            Assert.True(span.EndsWith(span, StringComparison.CurrentCulture));
            Assert.True(span.EndsWith(span, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(span.EndsWith(span, StringComparison.InvariantCulture));
            Assert.True(span.EndsWith(span, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(span.EndsWith(span, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void MakeSureNoEndsWithChecksGoOutOfRange_Char()
        {
            for (int length = 0; length < 100; length++)
            {
                var first = new char[length + 2];
                first[0] = '9';
                first[length + 1] = '9';
                var second = new char[length + 2];
                second[0] = 'a';
                second[length + 1] = 'a';

                string s1 = new string(first, 1, length);
                string s2 = new string(second, 1, length);
                bool b = s1.EndsWith(s2);
                Assert.True(b);

                var span1 = new ReadOnlySpan<char>(first, 1, length);
                var span2 = new ReadOnlySpan<char>(second, 1, length);
                b = span1.EndsWith(span2);
                Assert.True(b);
            }
        }

        [Fact]
        public static void LengthMismatchEndsWith_StringComparison()
        {
            string value = "456";

            string s1 = value.Substring(0, 2);
            string s2 = value.Substring(0, 3);
            Assert.False(s1.EndsWith(s2, StringComparison.Ordinal));

            Assert.False(s1.EndsWith(s2, StringComparison.CurrentCulture));
            Assert.False(s1.EndsWith(s2, StringComparison.CurrentCultureIgnoreCase));
            Assert.False(s1.EndsWith(s2, StringComparison.InvariantCulture));
            Assert.False(s1.EndsWith(s2, StringComparison.InvariantCultureIgnoreCase));
            Assert.False(s1.EndsWith(s2, StringComparison.OrdinalIgnoreCase));

            ReadOnlySpan<char> span = value.AsSpan(0 ,2);
            ReadOnlySpan<char> slice = value.AsSpan(0, 3);
            Assert.False(span.EndsWith(slice, StringComparison.Ordinal));

            Assert.False(span.EndsWith(slice, StringComparison.CurrentCulture));
            Assert.False(span.EndsWith(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.False(span.EndsWith(slice, StringComparison.InvariantCulture));
            Assert.False(span.EndsWith(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.False(span.EndsWith(slice, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void EndsWithMatch_StringComparison()
        {
            string value = "456";

            string s1 = value.Substring(0, 3);
            string s2 = value.Substring(1, 2);
            Assert.True(s1.EndsWith(s2, StringComparison.Ordinal));

            Assert.True(s1.EndsWith(s2, StringComparison.CurrentCulture));
            Assert.True(s1.EndsWith(s2, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(s1.EndsWith(s2, StringComparison.InvariantCulture));
            Assert.True(s1.EndsWith(s2, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(s1.EndsWith(s2, StringComparison.OrdinalIgnoreCase));

            ReadOnlySpan<char> span = value.AsSpan(0, 3);
            ReadOnlySpan<char> slice = value.AsSpan(1 ,2);
            Assert.True(span.EndsWith(slice, StringComparison.Ordinal));

            Assert.True(span.EndsWith(slice, StringComparison.CurrentCulture));
            Assert.True(span.EndsWith(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(span.EndsWith(slice, StringComparison.InvariantCulture));
            Assert.True(span.EndsWith(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(span.EndsWith(slice, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void EndsWithMatchDifferentSpans_StringComparison()
        {
            string value1 = "7456";
            string value2 = "456";

            string s1 = value1.Substring(1, 3);
            string s2 = value2.Substring(0, 3);
            Assert.True(s1.EndsWith(s2, StringComparison.Ordinal));

            Assert.True(s1.EndsWith(s2, StringComparison.CurrentCulture));
            Assert.True(s1.EndsWith(s2, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(s1.EndsWith(s2, StringComparison.InvariantCulture));
            Assert.True(s1.EndsWith(s2, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(s1.EndsWith(s2, StringComparison.OrdinalIgnoreCase));

            ReadOnlySpan<char> span = value1.AsSpan(1, 3);
            ReadOnlySpan<char> slice = value2.AsSpan(0, 3);
            Assert.True(span.EndsWith(slice, StringComparison.Ordinal));

            Assert.True(span.EndsWith(slice, StringComparison.CurrentCulture));
            Assert.True(span.EndsWith(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(span.EndsWith(slice, StringComparison.InvariantCulture));
            Assert.True(span.EndsWith(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(span.EndsWith(slice, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void EndsWithNoMatch_StringComparison()
        {
            for (int length = 1; length < 150; length++)
            {
                for (int mismatchIndex = 0; mismatchIndex < length; mismatchIndex++)
                {
                    var first = new char[length];
                    var second = new char[length];
                    for (int i = 0; i < length; i++)
                    {
                        first[i] = second[i] = (char)(i + 1);
                    }

                    second[mismatchIndex] = (char)(second[mismatchIndex] + 1);

                    string s1 = new string(first);
                    string s2 = new string(second);
                    Assert.False(s1.EndsWith(s2, StringComparison.Ordinal));

                    Assert.False(s1.EndsWith(s2, StringComparison.OrdinalIgnoreCase));

                    // Different behavior depending on OS
                    Assert.Equal(
                        s1.ToString().EndsWith(s2.ToString(), StringComparison.CurrentCulture),
                        s1.EndsWith(s2, StringComparison.CurrentCulture));
                    Assert.Equal(
                        s1.ToString().EndsWith(s2.ToString(), StringComparison.CurrentCultureIgnoreCase),
                        s1.EndsWith(s2, StringComparison.CurrentCultureIgnoreCase));
                    Assert.Equal(
                        s1.ToString().EndsWith(s2.ToString(), StringComparison.InvariantCulture),
                        s1.EndsWith(s2, StringComparison.InvariantCulture));
                    Assert.Equal(
                        s1.ToString().EndsWith(s2.ToString(), StringComparison.InvariantCultureIgnoreCase),
                        s1.EndsWith(s2, StringComparison.InvariantCultureIgnoreCase));

                    var firstSpan = new ReadOnlySpan<char>(first);
                    var secondSpan = new ReadOnlySpan<char>(second);
                    Assert.False(firstSpan.EndsWith(secondSpan, StringComparison.Ordinal));

                    Assert.False(firstSpan.EndsWith(secondSpan, StringComparison.OrdinalIgnoreCase));

                    // Different behavior depending on OS
                    Assert.Equal(
                        firstSpan.ToString().EndsWith(secondSpan.ToString(), StringComparison.CurrentCulture),
                        firstSpan.EndsWith(secondSpan, StringComparison.CurrentCulture));
                    Assert.Equal(
                        firstSpan.ToString().EndsWith(secondSpan.ToString(), StringComparison.CurrentCultureIgnoreCase),
                        firstSpan.EndsWith(secondSpan, StringComparison.CurrentCultureIgnoreCase));
                    Assert.Equal(
                        firstSpan.ToString().EndsWith(secondSpan.ToString(), StringComparison.InvariantCulture),
                        firstSpan.EndsWith(secondSpan, StringComparison.InvariantCulture));
                    Assert.Equal(
                        firstSpan.ToString().EndsWith(secondSpan.ToString(), StringComparison.InvariantCultureIgnoreCase),
                        firstSpan.EndsWith(secondSpan, StringComparison.InvariantCultureIgnoreCase));
                }
            }
        }

        [Fact]
        public static void MakeSureNoEndsWithChecksGoOutOfRange_StringComparison()
        {
            for (int length = 0; length < 100; length++)
            {
                var first = new char[length + 2];
                first[0] = (char)99;
                first[length + 1] = (char)99;
                var second = new char[length + 2];
                second[0] = (char)100;
                second[length + 1] = (char)100;

                string s1 = new string(first, 1, length);
                string s2 = new string(second, 1, length);
                Assert.True(s1.EndsWith(s2, StringComparison.Ordinal));

                Assert.True(s1.EndsWith(s2, StringComparison.CurrentCulture));
                Assert.True(s1.EndsWith(s2, StringComparison.CurrentCultureIgnoreCase));
                Assert.True(s1.EndsWith(s2, StringComparison.InvariantCulture));
                Assert.True(s1.EndsWith(s2, StringComparison.InvariantCultureIgnoreCase));
                Assert.True(s1.EndsWith(s2, StringComparison.OrdinalIgnoreCase));

                var span1 = new ReadOnlySpan<char>(first, 1, length);
                var span2 = new ReadOnlySpan<char>(second, 1, length);
                Assert.True(span1.EndsWith(span2, StringComparison.Ordinal));

                Assert.True(span1.EndsWith(span2, StringComparison.CurrentCulture));
                Assert.True(span1.EndsWith(span2, StringComparison.CurrentCultureIgnoreCase));
                Assert.True(span1.EndsWith(span2, StringComparison.InvariantCulture));
                Assert.True(span1.EndsWith(span2, StringComparison.InvariantCultureIgnoreCase));
                Assert.True(span1.EndsWith(span2, StringComparison.OrdinalIgnoreCase));
            }
        }

        [Fact]
        public static void EndsWithUnknownComparisonType_StringComparison()
        {
            string value = "456";

            Assert.Throws<ArgumentException>(() => value.EndsWith(value, StringComparison.CurrentCulture - 1));
            Assert.Throws<ArgumentException>(() => value.EndsWith(value, StringComparison.OrdinalIgnoreCase + 1));
            Assert.Throws<ArgumentException>(() => value.EndsWith(value, (StringComparison)6));

            Assert.Throws<ArgumentException>(() => value.AsSpan().CompareTo(value.AsSpan(), StringComparison.CurrentCulture - 1));
            Assert.Throws<ArgumentException>(() => value.AsSpan().CompareTo(value.AsSpan(), StringComparison.OrdinalIgnoreCase + 1));
            Assert.Throws<ArgumentException>(() => value.AsSpan().CompareTo(value.AsSpan(), (StringComparison)6));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        public static void EndsWithMatchNonOrdinal_StringComparison()
        {
            string s = "dabc";
            string value = "aBc";
            Assert.False(s.EndsWith(value, StringComparison.Ordinal));
            Assert.True(s.EndsWith(value, StringComparison.OrdinalIgnoreCase));

            ReadOnlySpan<char> span = s.AsSpan();
            ReadOnlySpan<char> spanValue = value.AsSpan();
            Assert.False(span.EndsWith(spanValue, StringComparison.Ordinal));
            Assert.True(span.EndsWith(spanValue, StringComparison.OrdinalIgnoreCase));

            using (new ThreadCultureChange("el-GR"))
            {
                s = "\u03b4\u03b1\u03b2\u03b3"; // \u03B4\u03B1\u03B2\u03B3
                value = "\u03b1\u03b2\u03b3"; // \u03B1\u03B2\u03B3

                Assert.True(s.EndsWith(value, StringComparison.CurrentCulture));
                Assert.True(s.EndsWith(value, StringComparison.CurrentCultureIgnoreCase));

                span = s.AsSpan(); // \u03B4\u03B1\u03B2\u03B3
                spanValue = value.AsSpan(); // \u03B1\u03B2\u03B3

                Assert.True(span.EndsWith(spanValue, StringComparison.CurrentCulture));
                Assert.True(span.EndsWith(spanValue, StringComparison.CurrentCultureIgnoreCase));

                value = "\u03b1\u0392\u03b3"; // \u03B1\u0392\u03B3
                Assert.False(s.EndsWith(value, StringComparison.CurrentCulture));
                Assert.True(s.EndsWith(value, StringComparison.CurrentCultureIgnoreCase));

                spanValue = value.AsSpan(); // \u03B1\u0392\u03B3
                Assert.False(span.EndsWith(spanValue, StringComparison.CurrentCulture));
                Assert.True(span.EndsWith(spanValue, StringComparison.CurrentCultureIgnoreCase));
            }

            s = "\u03b4\u0069\u00df\u0049"; // \u03B4i\u00DFI
            value = "\u0069\u0073\u0073\u0049"; // issI

            Assert.False(s.EndsWith(value, StringComparison.Ordinal));
            // Different behavior depending on OS - True on Windows, False on Unix
            Assert.Equal(
                s.ToString().EndsWith(value.ToString(), StringComparison.InvariantCulture),
                s.EndsWith(value, StringComparison.InvariantCulture));
            Assert.Equal(
                s.ToString().EndsWith(value.ToString(), StringComparison.InvariantCultureIgnoreCase),
                s.EndsWith(value, StringComparison.InvariantCultureIgnoreCase));

            span = s.AsSpan(); // \u03B4i\u00DFI
            spanValue = value.AsSpan(); // issI

            Assert.False(span.EndsWith(spanValue, StringComparison.Ordinal));
            // Different behavior depending on OS - True on Windows, False on Unix
            Assert.Equal(
                span.ToString().EndsWith(spanValue.ToString(), StringComparison.InvariantCulture),
                span.EndsWith(spanValue, StringComparison.InvariantCulture));
            Assert.Equal(
                span.ToString().EndsWith(spanValue.ToString(), StringComparison.InvariantCultureIgnoreCase),
                span.EndsWith(spanValue, StringComparison.InvariantCultureIgnoreCase));

            value = "\u0049\u0073\u0073\u0049"; // IssI
            Assert.False(s.EndsWith(value, StringComparison.OrdinalIgnoreCase));
            Assert.False(s.EndsWith(value, StringComparison.InvariantCulture));
            // Different behavior depending on OS - True on Windows, False on Unix
            Assert.Equal(
                s.ToString().EndsWith(value.ToString(), StringComparison.InvariantCultureIgnoreCase),
                s.EndsWith(value, StringComparison.InvariantCultureIgnoreCase));

            spanValue = value.AsSpan(); // IssI
            Assert.False(span.EndsWith(spanValue, StringComparison.OrdinalIgnoreCase));
            Assert.False(span.EndsWith(spanValue, StringComparison.InvariantCulture));
            // Different behavior depending on OS - True on Windows, False on Unix
            Assert.Equal(
                span.ToString().EndsWith(spanValue.ToString(), StringComparison.InvariantCultureIgnoreCase),
                span.EndsWith(spanValue, StringComparison.InvariantCultureIgnoreCase));
        }

        [Fact]
        public static void EndsWithNoMatchNonOrdinal_StringComparison()
        {
            string s = "dabc";
            string value = "aDc";
            Assert.False(s.EndsWith(value, StringComparison.Ordinal));
            Assert.False(s.EndsWith(value, StringComparison.OrdinalIgnoreCase));

            ReadOnlySpan<char> span = s.AsSpan();
            ReadOnlySpan<char> spanValue = value.AsSpan();
            Assert.False(span.EndsWith(spanValue, StringComparison.Ordinal));
            Assert.False(span.EndsWith(spanValue, StringComparison.OrdinalIgnoreCase));

            using (new ThreadCultureChange("el-GR"))
            {
                s = "\u03b4\u03b1\u03b2\u03b3"; // \u03B4\u03B1\u03B2\u03B3
                value = "\u03b1\u03b4\u03b3"; // \u03B1\u03B4\u03B3

                Assert.False(s.EndsWith(value, StringComparison.CurrentCulture));
                Assert.False(s.EndsWith(value, StringComparison.CurrentCultureIgnoreCase));

                span = s.AsSpan(); // \u03B4\u03B1\u03B2\u03B3
                spanValue = value.AsSpan(); // \u03B1\u03B4\u03B3

                Assert.False(span.EndsWith(spanValue, StringComparison.CurrentCulture));
                Assert.False(span.EndsWith(spanValue, StringComparison.CurrentCultureIgnoreCase));

                value = "\u03b1\u0394\u03b3"; // \u03B1\u0394\u03B3
                Assert.False(s.EndsWith(value, StringComparison.CurrentCulture));
                Assert.False(s.EndsWith(value, StringComparison.CurrentCultureIgnoreCase));

                spanValue = value.AsSpan(); // \u03B1\u0394\u03B3
                Assert.False(span.EndsWith(spanValue, StringComparison.CurrentCulture));
                Assert.False(span.EndsWith(spanValue, StringComparison.CurrentCultureIgnoreCase));
            }

            s = "\u03b4\u0069\u00df\u0049"; // \u03B4i\u00DFI
            value = "\u0069\u03b4\u03b4\u0049"; // i\u03B4\u03B4I

            Assert.False(s.EndsWith(value, StringComparison.Ordinal));
            Assert.False(s.EndsWith(value, StringComparison.InvariantCulture));
            Assert.False(s.EndsWith(value, StringComparison.InvariantCultureIgnoreCase));

            span = s.AsSpan(); // \u03B4i\u00DFI
            spanValue = value.AsSpan(); // i\u03B4\u03B4I

            Assert.False(span.EndsWith(spanValue, StringComparison.Ordinal));
            Assert.False(span.EndsWith(spanValue, StringComparison.InvariantCulture));
            Assert.False(span.EndsWith(spanValue, StringComparison.InvariantCultureIgnoreCase));

            value = "\u0049\u03b4\u03b4\u0049"; // I\u03B4\u03B4I
            Assert.False(s.EndsWith(value, StringComparison.OrdinalIgnoreCase));
            Assert.False(s.EndsWith(value, StringComparison.InvariantCulture));
            Assert.False(s.EndsWith(value, StringComparison.InvariantCultureIgnoreCase));

            spanValue = value.AsSpan(); // I\u03B4\u03B4I
            Assert.False(span.EndsWith(spanValue, StringComparison.OrdinalIgnoreCase));
            Assert.False(span.EndsWith(spanValue, StringComparison.InvariantCulture));
            Assert.False(span.EndsWith(spanValue, StringComparison.InvariantCultureIgnoreCase));
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("")]
        public static void GetEnumerator_NonGeneric(string s)
        {
            IEnumerable enumerable = s;
            IEnumerator enumerator = enumerable.GetEnumerator();

            for (int i = 0; i < 2; i++)
            {
                int counter = 0;
                while (enumerator.MoveNext())
                {
                    Assert.Equal(s[counter], enumerator.Current);
                    counter++;
                }
                Assert.Equal(s.Length, counter);

                enumerator.Reset();
            }
        }

        [Fact]
        public static void GetEnumerator_NonGeneric_IsIDisposable()
        {
            IEnumerable enumerable = "abc";
            IEnumerator enumerator = enumerable.GetEnumerator();
            enumerator.MoveNext();

            IDisposable disposable = enumerable as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
                Assert.Throws<NullReferenceException>(() => enumerator.Current);
                Assert.Throws<NullReferenceException>(() => enumerator.MoveNext());

                // Should be able to call dispose multiple times
                disposable.Dispose();
            }
        }

        [Fact]
        public static void GetEnumerator_NonGeneric_Invalid()
        {
            IEnumerable enumerable = "foo";
            IEnumerator enumerator = enumerable.GetEnumerator();

            // Enumerator should throw when accessing Current before starting enumeration
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            while (enumerator.MoveNext()) ;

            // Enumerator should throw when accessing Current after finishing enumeration
            Assert.False(enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            // Enumerator should throw when accessing Current after being reset
            enumerator.Reset();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("")]
        public static void GetEnumerator_Generic(string s)
        {
            IEnumerable<char> enumerable = s;
            IEnumerator<char> enumerator = enumerable.GetEnumerator();

            for (int i = 0; i < 2; i++)
            {
                int counter = 0;
                while (enumerator.MoveNext())
                {
                    Assert.Equal(s[counter], enumerator.Current);
                    counter++;
                }
                Assert.Equal(s.Length, counter);

                enumerator.Reset();
            }
        }

        [Fact]
        public static void GetEnumerator_Generic_Invalid()
        {
            IEnumerable<char> enumerable = "foo";
            IEnumerator<char> enumerator = enumerable.GetEnumerator();

            // Enumerator should throw when accessing Current before starting enumeration
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            while (enumerator.MoveNext()) ;

            // Enumerator should throw when accessing Current after finishing enumeration
            Assert.False(enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            // Enumerator should throw when accessing Current after being reset
            enumerator.Reset();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        }

        [Fact]
        public static void GetHashCode_EmbeddedNull_ReturnsDifferentHashCodes()
        {
            Assert.NotEqual("\0AAAAAAAAA".GetHashCode(), "\0BBBBBBBBBBBB".GetHashCode());
        }

        [Theory]
        // CurrentCulture
        [InlineData("Hello", "Hello", StringComparison.CurrentCulture, true)]
        [InlineData("Hello", "hello", StringComparison.CurrentCulture, false)]
        [InlineData("Hello", "Helloo", StringComparison.CurrentCulture, false)]
        [InlineData("Hello", "Hell", StringComparison.CurrentCulture, false)]
        [InlineData("Hello", null, StringComparison.CurrentCulture, false)]
        [InlineData(null, "Hello", StringComparison.CurrentCulture, false)]
        [InlineData(null, null, StringComparison.CurrentCulture, true)]
        [InlineData("Hello", "", StringComparison.CurrentCulture, false)]
        [InlineData("", "Hello", StringComparison.CurrentCulture, false)]
        [InlineData("", "", StringComparison.CurrentCulture, true)]
        [InlineData("123", 123, StringComparison.CurrentCulture, false)] // Not a string
        [InlineData("\0AAAAAAAAA", "\0BBBBBBBBBBBB", StringComparison.CurrentCulture, false)]
        // CurrentCultureIgnoreCase
        [InlineData("Hello", "Hello", StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("Hello", "hello", StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("Hello", "helloo", StringComparison.CurrentCultureIgnoreCase, false)]
        [InlineData("Hello", "hell", StringComparison.CurrentCultureIgnoreCase, false)]
        [InlineData("Hello", null, StringComparison.CurrentCultureIgnoreCase, false)]
        [InlineData(null, "Hello", StringComparison.CurrentCultureIgnoreCase, false)]
        [InlineData(null, null, StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("Hello", "", StringComparison.CurrentCultureIgnoreCase, false)]
        [InlineData("", "Hello", StringComparison.CurrentCultureIgnoreCase, false)]
        [InlineData("", "", StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("123", 123, StringComparison.CurrentCultureIgnoreCase, false)] // Not a string
        [InlineData("\0AAAAAAAAA", "\0BBBBBBBBBBBB", StringComparison.CurrentCultureIgnoreCase, false)]
        // InvariantCulture
        [InlineData("Hello", "Hello", StringComparison.InvariantCulture, true)]
        [InlineData("Hello", "hello", StringComparison.InvariantCulture, false)]
        [InlineData("Hello", "Helloo", StringComparison.InvariantCulture, false)]
        [InlineData("Hello", "Hell", StringComparison.InvariantCulture, false)]
        [InlineData("Hello", null, StringComparison.InvariantCulture, false)]
        [InlineData(null, "Hello", StringComparison.InvariantCulture, false)]
        [InlineData(null, null, StringComparison.InvariantCulture, true)]
        [InlineData("Hello", "", StringComparison.InvariantCulture, false)]
        [InlineData("", "Hello", StringComparison.InvariantCulture, false)]
        [InlineData("", "", StringComparison.InvariantCulture, true)]
        [InlineData("123", 123, StringComparison.InvariantCulture, false)] // Not a string
        [InlineData("\0AAAAAAAAA", "\0BBBBBBBBBBBB", StringComparison.InvariantCulture, false)]
        // InvariantCultureIgnoreCase
        [InlineData("Hello", "Hello", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Hello", "hello", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Hello", "Helloo", StringComparison.InvariantCultureIgnoreCase, false)]
        [InlineData("Hello", "Hell", StringComparison.InvariantCultureIgnoreCase, false)]
        [InlineData("Hello", null, StringComparison.InvariantCultureIgnoreCase, false)]
        [InlineData(null, "Hello", StringComparison.InvariantCultureIgnoreCase, false)]
        [InlineData(null, null, StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Hello", "", StringComparison.InvariantCultureIgnoreCase, false)]
        [InlineData("", "Hello", StringComparison.InvariantCultureIgnoreCase, false)]
        [InlineData("", "", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("123", 123, StringComparison.InvariantCultureIgnoreCase, false)] // Not a string
        [InlineData("\0AAAAAAAAA", "\0BBBBBBBBBBBB", StringComparison.InvariantCultureIgnoreCase, false)]
        // Ordinal
        [InlineData("Hello", "Hello", StringComparison.Ordinal, true)]
        [InlineData("Hello", "hello", StringComparison.Ordinal, false)]
        [InlineData("Hello", "Helloo", StringComparison.Ordinal, false)]
        [InlineData("Hello", "Hell", StringComparison.Ordinal, false)]
        [InlineData("Hello", null, StringComparison.Ordinal, false)]
        [InlineData(null, "Hello", StringComparison.Ordinal, false)]
        [InlineData(null, null, StringComparison.Ordinal, true)]
        [InlineData("Hello", "", StringComparison.Ordinal, false)]
        [InlineData("", "Hello", StringComparison.Ordinal, false)]
        [InlineData("", "", StringComparison.Ordinal, true)]
        [InlineData("123", 123, StringComparison.Ordinal, false)] // Not a string
        // OridinalIgnoreCase
        [InlineData("Hello", "Hello", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("HELLO", "hello", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("Hello", "Helloo", StringComparison.OrdinalIgnoreCase, false)]
        [InlineData("Hello", "Hell", StringComparison.OrdinalIgnoreCase, false)]
        [InlineData("\u1234\u5678", "\u1234\u5678", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("\u1234\u5678", "\u1234\u5679", StringComparison.OrdinalIgnoreCase, false)]
        [InlineData("\u1234\u5678", "\u1235\u5678", StringComparison.OrdinalIgnoreCase, false)]
        [InlineData("\u1234\u5678", "\u1234", StringComparison.OrdinalIgnoreCase, false)]
        [InlineData("\u1234\u5678", "\u1234\u56789\u1234", StringComparison.OrdinalIgnoreCase, false)]
        [InlineData("Hello", null, StringComparison.OrdinalIgnoreCase, false)]
        [InlineData(null, "Hello", StringComparison.OrdinalIgnoreCase, false)]
        [InlineData(null, null, StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("Hello", "", StringComparison.OrdinalIgnoreCase, false)]
        [InlineData("", "Hello", StringComparison.OrdinalIgnoreCase, false)]
        [InlineData("", "", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("123", 123, StringComparison.OrdinalIgnoreCase, false)] // Not a string
        [InlineData("\0AAAAAAAAA", "\0BBBBBBBBBBBB", StringComparison.OrdinalIgnoreCase, false)]
        public static void EqualsTest(string s1, object obj, StringComparison comparisonType, bool expected)
        {
            string s2 = obj as string;
            if (s1 != null)
            {
                if (comparisonType == StringComparison.Ordinal)
                {
                    // Use Equals(object)
                    Assert.Equal(expected, s1.Equals(obj));
                    Assert.Equal(expected, s1.Equals(s2));
                }
                // Use Equals(string, comparisonType)
                Assert.Equal(expected, s1.Equals(s2, comparisonType));
            }
            if (comparisonType == StringComparison.Ordinal)
            {
                // Use Equals(string, string)
                Assert.Equal(expected, string.Equals(s1, s2));
            }
            // Use Equals(string, string, StringComparison)
            Assert.Equal(expected, string.Equals(s1, s2, comparisonType));

            // If two strings are equal ordinally, then they must have the same hash code.
            if (s1 != null && s2 != null && comparisonType == StringComparison.Ordinal)
            {
                Assert.Equal(expected, s1.GetHashCode().Equals(s2.GetHashCode()));
            }
            if (s1 != null)
            {
                Assert.Equal(s1.GetHashCode(), s1.GetHashCode());
            }

            if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2))
            {
                // null strings are normalized to empty spans
                Assert.True(s1.AsSpan().Equals(s2.AsSpan(), comparisonType));
            }
            else
            {
                Assert.Equal(expected, s1.AsSpan().Equals(s2.AsSpan(), comparisonType));
            }
        }

        public static IEnumerable<object[]> Equals_EncyclopaediaData()
        {
            yield return new object[] { StringComparison.CurrentCulture, false };
            yield return new object[] { StringComparison.CurrentCultureIgnoreCase, false };
            yield return new object[] { StringComparison.Ordinal, false };
            yield return new object[] { StringComparison.OrdinalIgnoreCase, false };

            // NLS and ICU disagree about how these strings compare in the default locale.
            yield return new object[] { StringComparison.InvariantCulture, PlatformDetection.IsNlsGlobalization };
            yield return new object[] { StringComparison.InvariantCultureIgnoreCase, PlatformDetection.IsNlsGlobalization };
        }

        [Theory]
        [MemberData(nameof(Equals_EncyclopaediaData))]
        public void Equals_Encyclopaedia_ReturnsExpected(StringComparison comparison, bool expected)
        {
            string source = "encyclop\u00e6dia";
            string target = "encyclopaedia";
            using (new ThreadCultureChange(PlatformDetection.IsBrowser ?"pl-PL" : "se-SE"))
            {
                Assert.Equal(expected, string.Equals(source, target, comparison));
                Assert.Equal(expected, source.AsSpan().Equals(target.AsSpan(), comparison));
            }
        }

        [Theory]
        [InlineData(StringComparison.CurrentCulture - 1)]
        [InlineData(StringComparison.OrdinalIgnoreCase + 1)]
        public static void Equals_InvalidComparisonType_ThrowsArgumentOutOfRangeException(StringComparison comparisonType)
        {
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => string.Equals("a", "a", comparisonType));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => string.Equals("a", null, comparisonType));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => string.Equals("a", "b", comparisonType));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "a".Equals("a", comparisonType));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "a".Equals(null, comparisonType));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "a".Equals("b", comparisonType));
        }

        public static IEnumerable<object[]> Format_Valid_TestData()
        {
            yield return new object[] { null, "", new object[0], "" };
            yield return new object[] { null, ", ", new object[0], ", " };

            yield return new object[] { null, ", Foo {0  }", new object[] { "Bar" }, ", Foo Bar" }; // Ignores whitespace

            yield return new object[] { null, "Foo {0}", new object[] { "Bar" }, "Foo Bar" };
            yield return new object[] { null, "Foo {0} Baz {1}", new object[] { "Bar", "Foo" }, "Foo Bar Baz Foo" };
            yield return new object[] { null, "Foo {0} Baz {1} Bar {2}", new object[] { "Bar", "Foo", "Baz" }, "Foo Bar Baz Foo Bar Baz" };
            yield return new object[] { null, "Foo {0} Baz {1} Bar {2} Foo {3}", new object[] { "Bar", "Foo", "Baz", "Bar" }, "Foo Bar Baz Foo Bar Baz Foo Bar" };

            // Length is positive
            yield return new object[] { null, "Foo {0,2}", new object[] { "Bar" }, "Foo Bar" }; // MiValue's length > minimum length (so don't prepend whitespace)
            yield return new object[] { null, "Foo {0,3}", new object[] { "B" }, "Foo   B" }; // Value's length < minimum length (so prepend whitespace)
            yield return new object[] { null, "Foo {0,     3}", new object[] { "B" }, "Foo   B" }; // Same as above, but verify AppendFormat ignores whitespace
            yield return new object[] { null, "Foo {0,0}", new object[] { "Bar" }, "Foo Bar" }; // Minimum length is 0
            yield return new object[] { null, "Foo {0,  2 }", new object[] { "Bar" }, "Foo Bar" }; // whitespace before and after length

            // Length is negative
            yield return new object[] { null, "Foo {0,-2}", new object[] { "Bar" }, "Foo Bar" }; // Value's length > |minimum length| (so don't prepend whitespace)
            yield return new object[] { null, "Foo {0,-3}", new object[] { "B" }, "Foo B  " }; // Value's length < |minimum length| (so append whitespace)
            yield return new object[] { null, "Foo {0,     -3}", new object[] { "B" }, "Foo B  " }; // Same as above, but verify AppendFormat ignores whitespace
            yield return new object[] { null, "Foo {0,0}", new object[] { "Bar" }, "Foo Bar" }; // Minimum length is 0
            yield return new object[] { null, "Foo {0, -2  }", new object[] { "Bar" }, "Foo Bar" }; // whitespace before and after length

            yield return new object[] { null, "Foo {0:D6}", new object[] { 1 }, "Foo 000001" }; // Custom format
            yield return new object[] { null, "Foo {0     :D6}", new object[] { 1 }, "Foo 000001" }; // Custom format with ignored whitespace
            yield return new object[] { null, "Foo {0:}", new object[] { 1 }, "Foo 1" }; // Missing custom format

            yield return new object[] { null, "Foo {0,9:D6}", new object[] { 1 }, "Foo    000001" }; // Positive minimum length and custom format
            yield return new object[] { null, "Foo {0,-9:D6}", new object[] { 1 }, "Foo 000001   " }; // Negative length and custom format

            yield return new object[] { null, "Foo {{{0}", new object[] { 1 }, "Foo {1" }; // Escaped open curly braces
            yield return new object[] { null, "Foo }}{0}", new object[] { 1 }, "Foo }1" }; // Escaped closed curly braces
            yield return new object[] { null, "Foo {0} {{0}}", new object[] { 1 }, "Foo 1 {0}" }; // Escaped placeholder


            yield return new object[] { null, "Foo {0}", new object[] { null }, "Foo " }; // Values has null only
            yield return new object[] { null, "Foo {0} {1} {2}", new object[] { "Bar", null, "Baz" }, "Foo Bar  Baz" }; // Values has null

            yield return new object[] { CultureInfo.InvariantCulture, "Foo {0,9:D6}", new object[] { 1 }, "Foo    000001" }; // Positive minimum length, custom format and custom format provider

            yield return new object[] { new CustomFormatter(), "{0}", new object[] { 1.2 }, "abc" }; // Custom format provider
            yield return new object[] { new CustomFormatter(), "{0:0}", new object[] { 1.2 }, "abc" }; // Custom format provider

            // Longer inputs
            yield return new object[] { null, "0 = {0} 1 = {1} 2 = {2} 3 = {3} 4 = {4}", new object[] { "zero", "one", "two", "three", "four" }, "0 = zero 1 = one 2 = two 3 = three 4 = four" };
            yield return new object[] { new TestFormatter(), "0 = {0} 1 = {1} 2 = {2} 3 = {3} 4 = {4}", new object[] { "zero", "one", "two", "three", "four" }, "0 = Test: : zero 1 = Test: : one 2 = Test: : two 3 = Test: : three 4 = Test: : four" };

            // ISpanFormattable inputs: simple validation of known types that implement the interface
            yield return new object[] { CultureInfo.InvariantCulture, "{0}", new object[] { (byte)42 }, "42" };
            yield return new object[] { CultureInfo.InvariantCulture, "{0}", new object[] { 'A' }, "A" };
            yield return new object[] { CultureInfo.InvariantCulture, "{0:r}", new object[] { DateTime.ParseExact("2021-03-15T14:52:51.5058563Z", "o", null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal) }, "Mon, 15 Mar 2021 14:52:51 GMT" };
            yield return new object[] { CultureInfo.InvariantCulture, "{0:r}", new object[] { DateTimeOffset.ParseExact("2021-03-15T14:52:51.5058563Z", "o", null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal) }, "Mon, 15 Mar 2021 14:52:51 GMT" };
            yield return new object[] { CultureInfo.InvariantCulture, "{0}", new object[] { (decimal)42 }, "42" };
            yield return new object[] { CultureInfo.InvariantCulture, "{0}", new object[] { (double)42 }, "42" };
            yield return new object[] { CultureInfo.InvariantCulture, "{0}", new object[] { Guid.Parse("68d9cfaf-feab-4d5b-96d8-a3fd889ae89f") }, "68d9cfaf-feab-4d5b-96d8-a3fd889ae89f" };
            yield return new object[] { CultureInfo.InvariantCulture, "{0}", new object[] { (Half)42 }, "42" };
            yield return new object[] { CultureInfo.InvariantCulture, "{0}", new object[] { (short)42 }, "42" };
            yield return new object[] { CultureInfo.InvariantCulture, "{0}", new object[] { (int)42 }, "42" };
            yield return new object[] { CultureInfo.InvariantCulture, "{0}", new object[] { (long)42 }, "42" };
            yield return new object[] { CultureInfo.InvariantCulture, "{0}", new object[] { (IntPtr)42 }, "42" };
            yield return new object[] { CultureInfo.InvariantCulture, "{0}", new object[] { new Rune('A') }, "A" };
            yield return new object[] { CultureInfo.InvariantCulture, "{0}", new object[] { (sbyte)42 }, "42" };
            yield return new object[] { CultureInfo.InvariantCulture, "{0}", new object[] { (float)42 }, "42" };
            yield return new object[] { CultureInfo.InvariantCulture, "{0}", new object[] { TimeSpan.FromSeconds(42) }, "00:00:42" };
            yield return new object[] { CultureInfo.InvariantCulture, "{0}", new object[] { (ushort)42 }, "42" };
            yield return new object[] { CultureInfo.InvariantCulture, "{0}", new object[] { (uint)42 }, "42" };
            yield return new object[] { CultureInfo.InvariantCulture, "{0}", new object[] { (ulong)42 }, "42" };
            yield return new object[] { CultureInfo.InvariantCulture, "{0}", new object[] { (UIntPtr)42 }, "42" };
            yield return new object[] { CultureInfo.InvariantCulture, "{0}", new object[] { new Version(1, 2, 3, 4) }, "1.2.3.4" };
        }

        [Theory]
        [MemberData(nameof(Format_Valid_TestData))]
        public static void Format_Valid(IFormatProvider provider, string format, object[] values, string expected)
        {
            Assert.Equal(expected, string.Format(provider, format, values));
            if (provider is null)
            {
                Assert.Equal(expected, string.Format(format, values));
            }

            switch (values.Length)
            {
                case 1:
                    Assert.Equal(expected, string.Format(provider, format, values[0]));
                    if (provider is null)
                    {
                        Assert.Equal(expected, string.Format(format, values[0]));
                    }
                    break;

                case 2:
                    Assert.Equal(expected, string.Format(provider, format, values[0], values[1]));
                    if (provider is null)
                    {
                        Assert.Equal(expected, string.Format(format, values[0], values[1]));
                    }
                    break;

                case 3:
                    Assert.Equal(expected, string.Format(provider, format, values[0], values[1], values[2]));
                    if (provider is null)
                    {
                        Assert.Equal(expected, string.Format(format, values[0], values[1], values[2]));
                    }
                    break;
            }
        }

        [Fact]
        public static void Format_Invalid()
        {
            var formatter = new TestFormatter();
            var obj1 = new object();
            var obj2 = new object();
            var obj3 = new object();
            var obj4 = new object();

            // Format is null
            AssertExtensions.Throws<ArgumentNullException>("format", () => string.Format(null, obj1));
            AssertExtensions.Throws<ArgumentNullException>("format", () => string.Format(null, obj1, obj2));
            AssertExtensions.Throws<ArgumentNullException>("format", () => string.Format(null, obj1, obj2, obj3));
            AssertExtensions.Throws<ArgumentNullException>("format", () => string.Format(null, obj1, obj2, obj3, obj4));

            AssertExtensions.Throws<ArgumentNullException>("format", () => string.Format(formatter, null, obj1));
            AssertExtensions.Throws<ArgumentNullException>("format", () => string.Format(formatter, null, obj1, obj2));
            AssertExtensions.Throws<ArgumentNullException>("format", () => string.Format(formatter, null, obj1, obj2, obj3));

            // Args is null
            AssertExtensions.Throws<ArgumentNullException>("args", () => string.Format("", null));
            AssertExtensions.Throws<ArgumentNullException>("args", () => string.Format(formatter, "", null));

            // Args and format are null
            AssertExtensions.Throws<ArgumentNullException>("format", () => string.Format(null, (object[])null));
            AssertExtensions.Throws<ArgumentNullException>("format", () => string.Format(formatter, null, null));

            Assert.Throws<FormatException>(() => string.Format("{-1}", obj1)); // Format has value < 0
            Assert.Throws<FormatException>(() => string.Format("{-1}", obj1, obj2)); // Format has value < 0
            Assert.Throws<FormatException>(() => string.Format("{-1}", obj1, obj2, obj3)); // Format has value < 0
            Assert.Throws<FormatException>(() => string.Format("{-1}", obj1, obj2, obj3, obj4)); // Format has value < 0
            Assert.Throws<FormatException>(() => string.Format(formatter, "{-1}", obj1)); // Format has value < 0
            Assert.Throws<FormatException>(() => string.Format(formatter, "{-1}", obj1, obj2)); // Format has value < 0
            Assert.Throws<FormatException>(() => string.Format(formatter, "{-1}", obj1, obj2, obj3)); // Format has value < 0
            Assert.Throws<FormatException>(() => string.Format(formatter, "{-1}", obj1, obj2, obj3, obj4)); // Format has value < 0

            Assert.Throws<FormatException>(() => string.Format("{1}", obj1)); // Format has value >= 1
            Assert.Throws<FormatException>(() => string.Format("{2}", obj1, obj2)); // Format has value >= 2
            Assert.Throws<FormatException>(() => string.Format("{3}", obj1, obj2, obj3)); // Format has value >= 3
            Assert.Throws<FormatException>(() => string.Format("{4}", obj1, obj2, obj3, obj4)); // Format has value >= 4
            Assert.Throws<FormatException>(() => string.Format(formatter, "{1}", obj1)); // Format has value >= 1
            Assert.Throws<FormatException>(() => string.Format(formatter, "{2}", obj1, obj2)); // Format has value >= 2
            Assert.Throws<FormatException>(() => string.Format(formatter, "{3}", obj1, obj2, obj3)); // Format has value >= 3
            Assert.Throws<FormatException>(() => string.Format(formatter, "{4}", obj1, obj2, obj3, obj4)); // Format has value >= 4

            Assert.Throws<FormatException>(() => string.Format("{", "")); // Format has unescaped {
            Assert.Throws<FormatException>(() => string.Format("{a", "")); // Format has unescaped {

            Assert.Throws<FormatException>(() => string.Format("}", "")); // Format has unescaped }
            Assert.Throws<FormatException>(() => string.Format("}a", "")); // Format has unescaped }
            Assert.Throws<FormatException>(() => string.Format("{0:}}", "")); // Format has unescaped }

            Assert.Throws<FormatException>(() => string.Format("{\0", "")); // Format has invalid character after {
            Assert.Throws<FormatException>(() => string.Format("{a", "")); // Format has invalid character after {

            Assert.Throws<FormatException>(() => string.Format("{0     ", "")); // Format with index and spaces is not closed

            Assert.Throws<FormatException>(() => string.Format("{1000000", new string[10])); // Format index is too long
            Assert.Throws<FormatException>(() => string.Format("{10000000}", new string[10])); // Format index is too long

            Assert.Throws<FormatException>(() => string.Format("{0,", "")); // Format with comma is not closed
            Assert.Throws<FormatException>(() => string.Format("{0,   ", "")); // Format with comma and spaces is not closed
            Assert.Throws<FormatException>(() => string.Format("{0,-", "")); // Format with comma and minus sign is not closed

            Assert.Throws<FormatException>(() => string.Format("{0,-\0", "")); // Format has invalid character after minus sign
            Assert.Throws<FormatException>(() => string.Format("{0,-a", "")); // Format has invalid character after minus sign

            Assert.Throws<FormatException>(() => string.Format("{0,1000000", new string[10])); // Format length is too long
            Assert.Throws<FormatException>(() => string.Format("{0,10000000}", new string[10])); // Format length is too long

            Assert.Throws<FormatException>(() => string.Format("{0:", new string[10])); // Format with colon is not closed
            Assert.Throws<FormatException>(() => string.Format("{0:    ", new string[10])); // Format with colon and spaces is not closed

            Assert.Throws<FormatException>(() => string.Format("{0:{", new string[10])); // Format with custom format contains unescaped {
            Assert.Throws<FormatException>(() => string.Format("{0:{}", new string[10])); // Format with custom format contains unescaped {
        }

        [ConditionalTheory]
        [InlineData("Hello", 'l', 0, 5, 2)]
        [InlineData("Hello", 'x', 0, 5, -1)]
        [InlineData("Hello", 'l', 1, 4, 2)]
        [InlineData("Hello", 'l', 3, 2, 3)]
        [InlineData("Hello", 'l', 4, 1, -1)]
        [InlineData("Hello", 'x', 1, 4, -1)]
        [InlineData("Hello", 'l', 3, 0, -1)]
        [InlineData("Hello", 'l', 0, 2, -1)]
        [InlineData("Hello", 'l', 0, 3, 2)]
        [InlineData("Hello", 'o', 5, 0, -1)]
        [InlineData("H" + SoftHyphen + "ello", 'e', 0, 3, 2)]
        [InlineData("Hello", '\0', 0, 5, -1)] // .NET strings are terminated with a null character, but they should not be included as part of the string
        [InlineData("\ud800\udfff", '\ud800', 0, 1, 0)] // Surrogate characters
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 'A', 0, 26, 0)]
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 'B', 1, 25, 1)]
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 'C', 2, 24, 2)]
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 'D', 3, 23, 3)]
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 'G', 2, 24, 6)]
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 'K', 2, 24, 10)]
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 'O', 2, 24, 14)]
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 'P', 2, 24, 15)]
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 'Q', 2, 24, 16)]
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 'R', 2, 24, 17)]
        [InlineData("________\u8080\u8080\u8080________", '\u0080', 0, 19, -1)]
        [InlineData("________\u8000\u8000\u8000________", '\u0080', 0, 19, -1)]
        [InlineData("__\u8080\u8000\u0080______________", '\u0080', 0, 19, 4)]
        [InlineData("__\u8080\u8000__\u0080____________", '\u0080', 0, 19, 6)]
        [InlineData("__________________________________", '\ufffd', 0, 34, -1)]
        [InlineData("____________________________\ufffd", '\ufffd', 0, 29, 28)]
        [InlineData("ABCDEFGHIJKLM", 'M', 0, 13, 12)]
        [InlineData("ABCDEFGHIJKLMN", 'N', 0, 14, 13)]
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ", '@', 0, 26, -1)]
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXY", '@', 0, 25, -1)]
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ#", '@', 0, 27, -1)]
        [InlineData("_____________\u807f", '\u007f', 0, 14, -1)]
        [InlineData("_____________\u807f__", '\u007f', 0, 16, -1)]
        [InlineData("_____________\u807f\u007f_", '\u007f', 0, 16, 14)]
        [InlineData("__\u807f_______________", '\u007f', 0, 18, -1)]
        [InlineData("__\u807f___\u007f___________", '\u007f', 0, 18, 6)]
        [InlineData("ABCDEFGHIJKLMN", 'N', 2, 11, -1)]
        [InlineData("!@#$%^&", '%', 0, 7, 4)]
        [InlineData("!@#$", '!', 0, 4, 0)]
        [InlineData("!@#$", '@', 0, 4, 1)]
        [InlineData("!@#$", '#', 0, 4, 2)]
        [InlineData("!@#$", '$', 0, 4, 3)]
        [InlineData("!@#$%^&*", '%', 0, 8, 4)]
        [InlineData("", 'H', 0, 0, -1)]
        public static void IndexOf_SingleLetter(string s, char target, int startIndex, int count, int expected)
        {
            // This is by design. ICU ignores the null characters (i.e. null characters have no weights for the string comparison).
            // For desired behavior, use ordinal comparison instead of linguistic comparison.
            // This is a known difference between NLS and ICU (https://github.com/dotnet/runtime/issues/4673).
            if (target == '\0' && PlatformDetection.IsIcuGlobalization)
            {
                throw new SkipTestException("Target \\0 is not supported in ICU");
            }

            bool safeForCurrentCulture =
                IsSafeForCurrentCultureComparisons(s)
                && IsSafeForCurrentCultureComparisons(target.ToString());

            ReadOnlySpan<char> span = s.AsSpan();
            var charArray = new char[1];
            charArray[0] = target;
            ReadOnlySpan<char> targetSpan = charArray;

            int expectedFromSpan = expected == -1 ? expected : expected - startIndex;

            if (count + startIndex == s.Length)
            {
                if (startIndex == 0)
                {
                    Assert.Equal(expected, s.IndexOf(target));
                    Assert.Equal(expected, s.IndexOf(target.ToString(), StringComparison.Ordinal));
                    Assert.Equal(expected, s.IndexOf(target.ToString(), StringComparison.OrdinalIgnoreCase));

                    Assert.Equal(expectedFromSpan, span.IndexOf(targetSpan, StringComparison.Ordinal));
                    Assert.Equal(expectedFromSpan, span.IndexOf(targetSpan, StringComparison.OrdinalIgnoreCase));

                    // To be safe we only want to run CurrentCulture comparisons if
                    // we know the results will not vary depending on location
                    if (safeForCurrentCulture)
                    {
                        Assert.Equal(expected, s.IndexOf(target.ToString()));
                        Assert.Equal(expected, s.IndexOf(target.ToString(), StringComparison.CurrentCulture));

                        Assert.Equal(expectedFromSpan, span.IndexOf(targetSpan, StringComparison.CurrentCulture));
                    }
                }
                Assert.Equal(expected, s.IndexOf(target, startIndex));
                Assert.Equal(expected, s.IndexOf(target.ToString(), startIndex, StringComparison.Ordinal));
                Assert.Equal(expected, s.IndexOf(target.ToString(), startIndex, StringComparison.OrdinalIgnoreCase));

                Assert.Equal(expectedFromSpan, span.Slice(startIndex).IndexOf(targetSpan, StringComparison.Ordinal));
                Assert.Equal(expectedFromSpan, span.Slice(startIndex).IndexOf(targetSpan, StringComparison.OrdinalIgnoreCase));

                if (safeForCurrentCulture)
                {
                    Assert.Equal(expected, s.IndexOf(target.ToString(), startIndex));
                    Assert.Equal(expected, s.IndexOf(target.ToString(), startIndex, StringComparison.CurrentCulture));

                    Assert.Equal(expectedFromSpan, span.Slice(startIndex).IndexOf(targetSpan, StringComparison.CurrentCulture));
                }
            }
            Assert.Equal(expected, s.IndexOf(target, startIndex, count));
            Assert.Equal(expected, s.IndexOf(target.ToString(), startIndex, count, StringComparison.Ordinal));
            Assert.Equal(expected, s.IndexOf(target.ToString(), startIndex, count, StringComparison.OrdinalIgnoreCase));

            Assert.Equal(expectedFromSpan, span.Slice(startIndex, count).IndexOf(targetSpan, StringComparison.Ordinal));
            Assert.Equal(expectedFromSpan, span.Slice(startIndex, count).IndexOf(targetSpan, StringComparison.OrdinalIgnoreCase));

            if (safeForCurrentCulture)
            {
                Assert.Equal(expected, s.IndexOf(target.ToString(), startIndex, count));
                Assert.Equal(expected, s.IndexOf(target.ToString(), startIndex, count, StringComparison.CurrentCulture));

                Assert.Equal(expectedFromSpan, span.Slice(startIndex, count).IndexOf(targetSpan, StringComparison.CurrentCulture));
            }
        }

        [Fact]
        public static void IndexOf_Match_SingleLetter()
        {
            Assert.Equal(-1, "".IndexOf('a'));
            Assert.Equal(-1, "".AsSpan().IndexOf('a'));

            for (int length = 1; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = (char)(i + 1);
                }
                string str = new string(a);
                ReadOnlySpan<char> span = new ReadOnlySpan<char>(a);

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    char target = a[targetIndex];
                    int idx = str.IndexOf(target);
                    Assert.Equal(targetIndex, idx);

                    idx = span.IndexOf(target);
                    Assert.Equal(targetIndex, idx);
                }
            }
        }

        private static bool IsSafeForCurrentCultureComparisons(string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                // We only want ASCII chars that you can see
                // No controls, no delete, nothing >= 0x80
                if (c < 0x20 || c == 0x7f || c >= 0x80)
                {
                    return false;
                }
            }
            return true;
        }

        // NOTE: This is by design. ICU ignores the null characters (i.e. null characters have no weights for the string comparison).
        // For desired behavior, use ordinal comparison instead of linguistic comparison.
        // This is a known difference between NLS and ICU (https://github.com/dotnet/runtime/issues/4673).
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNlsGlobalization))]
        [InlineData("He\0lo", "He\0lo", 0)]
        [InlineData("He\0lo", "He\0", 0)]
        [InlineData("He\0lo", "\0", 2)]
        [InlineData("He\0lo", "\0lo", 2)]
        [InlineData("He\0lo", "lo", 3)]
        [InlineData("Hello", "lo\0", -1)]
        [InlineData("Hello", "\0lo", -1)]
        [InlineData("Hello", "l\0o", -1)]
        public static void IndexOf_NullInStrings(string s, string value, int expected)
        {
            Assert.Equal(expected, s.IndexOf(value));
            Assert.Equal(expected, s.AsSpan().IndexOf(value.AsSpan(), StringComparison.Ordinal));
        }

        [Theory]
        [MemberData(nameof(AllSubstringsAndComparisons), new object[] { "abcde" })]
        public static void IndexOf_AllSubstrings(string s, string value, int startIndex, StringComparison comparison)
        {
            bool ignoringCase = comparison == StringComparison.OrdinalIgnoreCase || comparison == StringComparison.CurrentCultureIgnoreCase;

            if (value.Length == 0)
            {
                Assert.Equal(0, s.IndexOf(value, comparison));
                Assert.Equal(startIndex, s.IndexOf(value, startIndex, comparison));
                Assert.Equal(0, s.AsSpan().IndexOf(value.AsSpan(), comparison));
                return;
            }

            if (s.Length == 0)
            {
                Assert.Equal(-1, s.IndexOf(value, comparison));
                Assert.Equal(-1, s.IndexOf(value, startIndex, comparison));
                Assert.Equal(-1, s.AsSpan().IndexOf(value.AsSpan(), comparison));
                return;
            }

            // First find the substring.  We should be able to with all comparison types.
            Assert.Equal(startIndex, s.IndexOf(value, comparison)); // in the whole string
            Assert.Equal(startIndex, s.IndexOf(value, startIndex, comparison)); // starting at substring
            Assert.Equal(startIndex, s.AsSpan().IndexOf(value.AsSpan(), comparison)); // in the whole string
            Assert.Equal(0, s.AsSpan(startIndex).IndexOf(value.AsSpan(), comparison)); // starting at substring

            if (startIndex > 0)
            {
                Assert.Equal(startIndex, s.IndexOf(value, startIndex - 1, comparison)); // starting just before substring
                Assert.Equal(1, s.AsSpan(startIndex - 1).IndexOf(value.AsSpan(), comparison)); // starting just before substring
            }
            Assert.Equal(-1, s.IndexOf(value, startIndex + 1, comparison)); // starting just after start of substring
            Assert.Equal(-1, s.AsSpan(startIndex + 1).IndexOf(value.AsSpan(), comparison)); // starting just after start of substring

            // Shouldn't be able to find the substring if the count is less than substring's length
            Assert.Equal(-1, s.IndexOf(value, 0, value.Length - 1, comparison));
            Assert.Equal(-1, s.AsSpan(0, value.Length - 1).IndexOf(value.AsSpan(), comparison));

            // Now double the source.  Make sure we find the first copy of the substring.
            int halfLen = s.Length;
            s += s;
            Assert.Equal(startIndex, s.IndexOf(value, comparison));
            Assert.Equal(startIndex, s.AsSpan().IndexOf(value.AsSpan(), comparison));

            // Now change the case of a letter.
            s = s.ToUpperInvariant();
            Assert.Equal(ignoringCase ? startIndex : -1, s.IndexOf(value, comparison));
            Assert.Equal(ignoringCase ? startIndex : -1, s.AsSpan().IndexOf(value.AsSpan(), comparison));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/60568", TestPlatforms.Android | TestPlatforms.LinuxBionic)]
        public static void IndexOf_TurkishI_TurkishCulture()
        {
            using (new ThreadCultureChange("tr-TR"))
            {
                string s = "Turkish I \u0131s TROUBL\u0130NG!";
                string value = "\u0130";
                Assert.Equal(19, s.IndexOf(value));
                Assert.Equal(19, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(4, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(19, s.IndexOf(value, StringComparison.Ordinal));
                Assert.Equal(19, s.IndexOf(value, StringComparison.OrdinalIgnoreCase));

                ReadOnlySpan<char> span = s.AsSpan();
                Assert.Equal(19, span.IndexOf(value.AsSpan(), StringComparison.CurrentCulture));
                Assert.Equal(4, span.IndexOf(value.AsSpan(), StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(19, span.IndexOf(value.AsSpan(), StringComparison.Ordinal));
                Assert.Equal(19, span.IndexOf(value.AsSpan(), StringComparison.OrdinalIgnoreCase));

                value = "\u0131";
                Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(8, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(10, s.IndexOf(value, StringComparison.Ordinal));
                Assert.Equal(10, s.IndexOf(value, StringComparison.OrdinalIgnoreCase));

                Assert.Equal(10, span.IndexOf(value.AsSpan(), StringComparison.CurrentCulture));
                Assert.Equal(8, span.IndexOf(value.AsSpan(), StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(10, span.IndexOf(value.AsSpan(), StringComparison.Ordinal));
                Assert.Equal(10, span.IndexOf(value.AsSpan(), StringComparison.OrdinalIgnoreCase));
            }
        }

        [Fact]
        public static void IndexOf_TurkishI_InvariantCulture()
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                string s = "Turkish I \u0131s TROUBL\u0130NG!";
                string value = "\u0130";

                Assert.Equal(19, s.IndexOf(value));
                Assert.Equal(19, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(19, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));

                ReadOnlySpan<char> span = s.AsSpan();
                Assert.Equal(19, span.IndexOf(value.AsSpan(), StringComparison.CurrentCulture));
                Assert.Equal(19, span.IndexOf(value.AsSpan(), StringComparison.CurrentCultureIgnoreCase));

                value = "\u0131";
                Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));

                Assert.Equal(10, span.IndexOf(value.AsSpan(), StringComparison.CurrentCulture));
                Assert.Equal(10, span.IndexOf(value.AsSpan(), StringComparison.CurrentCultureIgnoreCase));
            }
        }

        [Fact]
        public static void IndexOf_TurkishI_EnglishUSCulture()
        {
            using (new ThreadCultureChange("en-US"))
            {
                string s = "Turkish I \u0131s TROUBL\u0130NG!";
                string value = "\u0130";

                value = "\u0130";
                Assert.Equal(19, s.IndexOf(value));
                Assert.Equal(19, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(19, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));

                ReadOnlySpan<char> span = s.AsSpan();
                Assert.Equal(19, span.IndexOf(value.AsSpan(), StringComparison.CurrentCulture));
                Assert.Equal(19, span.IndexOf(value.AsSpan(), StringComparison.CurrentCultureIgnoreCase));

                value = "\u0131";
                Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));

                Assert.Equal(10, span.IndexOf(value.AsSpan(), StringComparison.CurrentCulture));
                Assert.Equal(10, span.IndexOf(value.AsSpan(), StringComparison.CurrentCultureIgnoreCase));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/60568", TestPlatforms.Android | TestPlatforms.LinuxBionic)]
        public static void IndexOf_HungarianDoubleCompression_HungarianCulture()
        {
            using (new ThreadCultureChange("hu-HU"))
            {
                string source = "dzsdzs";
                string target = "ddzs";

                /*
                    There are differences between NLS and ICU regarding contractions.
                    NLS has equal contraction collation weights, including case (target="Ddzs" same behavior as "ddzs").
                    ICU has different contraction collation weights, depending on locale collation rules.
                    If CurrentCultureIgnoreCase is specified, ICU will use 'secondary' collation rules
                    which ignore the contraction collation weights (defined as 'tertiary' rules)
                */
                Assert.Equal(PlatformDetection.IsNlsGlobalization ? 0 : -1, source.IndexOf(target));
                Assert.Equal(PlatformDetection.IsNlsGlobalization ? 0 : -1, source.IndexOf(target, StringComparison.CurrentCulture));

                Assert.Equal(0, source.IndexOf(target, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(-1, source.IndexOf(target, StringComparison.Ordinal));
                Assert.Equal(-1, source.IndexOf(target, StringComparison.OrdinalIgnoreCase));

                ReadOnlySpan<char> span = source.AsSpan();

                Assert.Equal(PlatformDetection.IsNlsGlobalization ? 0 : -1, span.IndexOf(target.AsSpan(), StringComparison.CurrentCulture));

                Assert.Equal(0, span.IndexOf(target.AsSpan(), StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(-1, span.IndexOf(target.AsSpan(), StringComparison.Ordinal));
                Assert.Equal(-1, span.IndexOf(target.AsSpan(), StringComparison.OrdinalIgnoreCase));
            }
        }

        [Fact]
        public static void IndexOf_HungarianDoubleCompression_InvariantCulture()
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                string source = "dzsdzs";
                string target = "ddzs";

                Assert.Equal(-1, source.IndexOf(target));
                Assert.Equal(-1, source.IndexOf(target, StringComparison.CurrentCulture));
                Assert.Equal(-1, source.IndexOf(target, StringComparison.CurrentCultureIgnoreCase));

                ReadOnlySpan<char> span = source.AsSpan();
                Assert.Equal(-1, span.IndexOf(target.AsSpan(), StringComparison.CurrentCulture));
                Assert.Equal(-1, span.IndexOf(target.AsSpan(), StringComparison.CurrentCultureIgnoreCase));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        public static void IndexOf_EquivalentDiacritics_EnglishUSCulture()
        {
            using (new ThreadCultureChange("en-US"))
            {
                string s = "Exhibit a\u0300\u00C0";
                string value = "\u00C0";

                Assert.Equal(10, s.IndexOf(value));
                Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(8, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(10, s.IndexOf(value, StringComparison.Ordinal));
                Assert.Equal(10, s.IndexOf(value, StringComparison.OrdinalIgnoreCase));

                ReadOnlySpan<char> span = s.AsSpan();
                Assert.Equal(10, span.IndexOf(value.AsSpan(), StringComparison.CurrentCulture));
                Assert.Equal(8, span.IndexOf(value.AsSpan(), StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(10, span.IndexOf(value.AsSpan(), StringComparison.Ordinal));
                Assert.Equal(10, span.IndexOf(value.AsSpan(), StringComparison.OrdinalIgnoreCase));

                value = "a\u0300"; // this diacritic combines with preceding character
                Assert.Equal(8, s.IndexOf(value));
                Assert.Equal(8, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(8, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(8, s.IndexOf(value, StringComparison.Ordinal));
                Assert.Equal(8, s.IndexOf(value, StringComparison.OrdinalIgnoreCase));

                Assert.Equal(8, span.IndexOf(value.AsSpan(), StringComparison.CurrentCulture));
                Assert.Equal(8, span.IndexOf(value.AsSpan(), StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(8, span.IndexOf(value.AsSpan(), StringComparison.Ordinal));
                Assert.Equal(8, span.IndexOf(value.AsSpan(), StringComparison.OrdinalIgnoreCase));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        public static void IndexOf_EquivalentDiacritics_InvariantCulture()
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                string s = "Exhibit a\u0300\u00C0";
                string value = "\u00C0";

                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                Assert.Equal(10, s.IndexOf(value));
                Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(8, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));

                ReadOnlySpan<char> span = s.AsSpan();
                Assert.Equal(10, span.IndexOf(value.AsSpan(), StringComparison.CurrentCulture));
                Assert.Equal(8, span.IndexOf(value.AsSpan(), StringComparison.CurrentCultureIgnoreCase));

                value = "a\u0300"; // this diacritic combines with preceding character
                Assert.Equal(8, s.IndexOf(value));
                Assert.Equal(8, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(8, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));

                Assert.Equal(8, span.IndexOf(value.AsSpan(), StringComparison.CurrentCulture));
                Assert.Equal(8, span.IndexOf(value.AsSpan(), StringComparison.CurrentCultureIgnoreCase));
            }
        }

        [Fact]
        public static void IndexOf_CyrillicE_EnglishUSCulture()
        {
            using (new ThreadCultureChange("en-US"))
            {
                string s = "Foo\u0400Bar";
                string value = "\u0400";

                Assert.Equal(3, s.IndexOf(value));
                Assert.Equal(3, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(3, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(3, s.IndexOf(value, StringComparison.Ordinal));
                Assert.Equal(3, s.IndexOf(value, StringComparison.OrdinalIgnoreCase));

                ReadOnlySpan<char> span = s.AsSpan();
                Assert.Equal(3, span.IndexOf(value.AsSpan(), StringComparison.CurrentCulture));
                Assert.Equal(3, span.IndexOf(value.AsSpan(), StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(3, span.IndexOf(value.AsSpan(), StringComparison.Ordinal));
                Assert.Equal(3, span.IndexOf(value.AsSpan(), StringComparison.OrdinalIgnoreCase));

                value = "bar";
                Assert.Equal(-1, s.IndexOf(value));
                Assert.Equal(-1, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(4, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(-1, s.IndexOf(value, StringComparison.Ordinal));
                Assert.Equal(4, s.IndexOf(value, StringComparison.OrdinalIgnoreCase));

                Assert.Equal(-1, span.IndexOf(value.AsSpan(), StringComparison.CurrentCulture));
                Assert.Equal(4, span.IndexOf(value.AsSpan(), StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(-1, span.IndexOf(value.AsSpan(), StringComparison.Ordinal));
                Assert.Equal(4, span.IndexOf(value.AsSpan(), StringComparison.OrdinalIgnoreCase));
            }
        }

        [Fact]
        public static void IndexOf_CyrillicE_InvariantCulture()
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                string s = "Foo\u0400Bar";
                string value = "\u0400";

                Assert.Equal(3, s.IndexOf(value));
                Assert.Equal(3, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(3, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));

                ReadOnlySpan<char> span = s.AsSpan();
                Assert.Equal(3, span.IndexOf(value.AsSpan(), StringComparison.CurrentCulture));
                Assert.Equal(3, span.IndexOf(value.AsSpan(), StringComparison.CurrentCultureIgnoreCase));

                value = "bar";
                Assert.Equal(-1, s.IndexOf(value));
                Assert.Equal(-1, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(4, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));

                Assert.Equal(-1, span.IndexOf(value.AsSpan(), StringComparison.CurrentCulture));
                Assert.Equal(4, span.IndexOf(value.AsSpan(), StringComparison.CurrentCultureIgnoreCase));
            }
        }

        [Fact]
        public static void IndexOf_Invalid()
        {
            // Value is null
            AssertExtensions.Throws<ArgumentNullException>("value", () => "foo".IndexOf(null));
            AssertExtensions.Throws<ArgumentNullException>("value", () => "foo".IndexOf(null, 0));
            AssertExtensions.Throws<ArgumentNullException>("value", () => "foo".IndexOf(null, 0, 0));
            AssertExtensions.Throws<ArgumentNullException>("value", () => "foo".IndexOf(null, 0, StringComparison.CurrentCulture));
            AssertExtensions.Throws<ArgumentNullException>("value", () => "foo".IndexOf(null, 0, 0, StringComparison.CurrentCulture));

            // Start index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => "foo".IndexOf("o", -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => "foo".IndexOf('o', -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => "foo".IndexOf("o", -1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => "foo".IndexOf('o', -1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => "foo".IndexOf("o", -1, StringComparison.CurrentCulture));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => "foo".IndexOf("o", -1, 0, StringComparison.CurrentCulture));

            // Start index > string.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => "foo".IndexOf("o", 4));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => "foo".IndexOf('o', 4));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => "foo".IndexOf("o", 4, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => "foo".IndexOf('o', 4, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => "foo".IndexOf("o", 4, 0, StringComparison.CurrentCulture));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => "foo".IndexOf("o", 4, 0, StringComparison.CurrentCulture));

            // Count < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => "foo".IndexOf("o", 0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => "foo".IndexOf('o', 0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => "foo".IndexOf("o", 0, -1, StringComparison.CurrentCulture));

            // Count > string.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => "foo".IndexOf("o", 0, 4));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => "foo".IndexOf('o', 0, 4));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => "foo".IndexOf("o", 0, 4, StringComparison.CurrentCulture));

            // Invalid comparison type
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "foo".IndexOf("o", StringComparison.CurrentCulture - 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "foo".AsSpan().IndexOf("o".AsSpan(), StringComparison.CurrentCulture - 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "foo".IndexOf("o", StringComparison.OrdinalIgnoreCase + 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "foo".AsSpan().IndexOf("o".AsSpan(), StringComparison.OrdinalIgnoreCase + 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "foo".IndexOf("o", 0, StringComparison.CurrentCulture - 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "foo".IndexOf("o", 0, StringComparison.OrdinalIgnoreCase + 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "foo".IndexOf("o", 0, 0, StringComparison.CurrentCulture - 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "foo".IndexOf("o", 0, 0, StringComparison.OrdinalIgnoreCase + 1));
        }

        [Theory]
        [InlineData("Hello", new char[] { 'd' }, 0, 5, -1)]
        [InlineData("Hello", new char[] { 'o' }, 0, 5, 4)]
        [InlineData("Hello", new char[] { 'e', 'l', 'o' }, 0, 5, 1)]
        [InlineData("Hello", new char[] { 'd', 'o', 'l' }, 0, 5, 2)]
        [InlineData("Hello", new char[] { 'd', 'e', 'H' }, 0, 0, -1)]
        [InlineData("Hello", new char[] { 'd', 'e', 'f' }, 1, 3, 1)]
        [InlineData("Hello", new char[] { 'a', 'b', 'c' }, 2, 3, -1)]
        [InlineData("Hello", new char[0], 2, 3, -1)]
        [InlineData("H" + SoftHyphen + "ello", new char[] { 'a', '\u00AD', 'c' }, 0, 2, 1)]
        [InlineData("", new char[] { 'd', 'e', 'f' }, 0, 0, -1)]
        [InlineData("Hello", new char[] { 'o', 'l' }, 0, 5, 2)]
        [InlineData("Hello", new char[] { 'e', 'o' }, 0, 5, 1)]
        [InlineData("Hello", new char[] { 'e', 'H' }, 0, 0, -1)]
        [InlineData("Hello", new char[] { 'd', 'e' }, 1, 3, 1)]
        [InlineData("Hello", new char[] { 'a', 'b' }, 2, 3, -1)]
        [InlineData("", new char[] { 'd', 'e' }, 0, 0, -1)]
        [InlineData("Hello", new char[] { '\0', 'b' }, 0, 5, -1)]    // Null terminator check, odd
        [InlineData("xHello", new char[] { '\0', 'b' }, 0, 6, -1)]   // Null terminator check, even
        [InlineData("Hello", new char[] { '\0', 'o' }, 0, 5, 4)]     // Match last char, odd
        [InlineData("xHello", new char[] { '\0', 'o' }, 0, 6, 5)]    // Match last char, even
        [InlineData("\x4E16\x754C\x60A8\x597D", new char[] { '\x754C' }, 0, 4, 1)]
        [InlineData("\x4E16\x754C\x60A8\x597D", new char[] { '\x754C', '\x60A8' }, 0, 4, 1)]
        [InlineData("\x4E16\x754C\x60A8\x597D", new char[] { '\x754C', '\x60A8', '\x4E16' }, 0, 4, 0)]
        [InlineData("\x4E16\x754C\x60A8\x597D", new char[] { '\x754C', '\x60A8', '\x4E16', '\x597D' }, 0, 4, 0)]
        public static void IndexOfAny(string s, char[] anyOf, int startIndex, int count, int expected)
        {
            if (startIndex + count == s.Length)
            {
                if (startIndex == 0)
                {
                    Assert.Equal(expected, s.IndexOfAny(anyOf));
                }
                Assert.Equal(expected, s.IndexOfAny(anyOf, startIndex));
            }

            foreach (char[] permutedAnyOf in Permute(anyOf))
            {
                Assert.Equal(expected, s.IndexOfAny(permutedAnyOf, startIndex, count));
            }
        }

        [Fact]
        public static void IndexOfAny_NullAnyOf_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("anyOf", null, () => "foo".IndexOfAny(null));
            AssertExtensions.Throws<ArgumentNullException>("anyOf", null, () => "foo".IndexOfAny(null, 0));
            AssertExtensions.Throws<ArgumentNullException>("anyOf", null, () => "foo".IndexOfAny(null, 0, 0));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(4)]
        public static void IndexOfAny_InvalidStartIndex_ThrowsArgumentOutOfRangeException(int startIndex)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", null, () => "foo".IndexOfAny(new char[] { 'o' }, startIndex));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", null, () => "foo".IndexOfAny(new char[] { 'o' }, startIndex, 0));
        }

        [Theory]
        [InlineData(0, -1)]
        [InlineData(0, 4)]
        [InlineData(3, 1)]
        [InlineData(2, 2)]
        public static void IndexOfAny_InvalidCount_ThrowsArgumentOutOfRangeException(int startIndex, int count)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => "foo".IndexOfAny(new char[] { 'o' }, startIndex, count));
        }

        [Fact]
        public static void ZeroLengthIndexOf_Char()
        {
            string emptyString = string.Empty;
            int idx = emptyString.IndexOf((char)0);
            Assert.Equal(-1, idx);

            ReadOnlySpan<char> sp = emptyString.AsSpan();
            idx = sp.IndexOf((char)0);
            Assert.Equal(-1, idx);
        }

        [Fact]
        public static void IndexOfSequenceMatchAtStart_Char()
        {
            string s1 = "5172377457778667789";
            string s2 = "517";
            int index = s1.IndexOf(s2);
            Assert.Equal(0, index);
            Assert.Equal(index, s1.IndexOf(s2, StringComparison.Ordinal));

            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> value = s2.AsSpan();
            index = span.IndexOf(value);
            Assert.Equal(0, index);
            Assert.Equal(index, span.IndexOf(value, StringComparison.Ordinal));
        }

        [Fact]
        public static void IndexOfSequenceMultipleMatch_Char()
        {
            string s1 = "123123123";
            string s2 = "23";

            int index = s1.IndexOf(s2);
            Assert.Equal(1, index);
            Assert.Equal(index, s1.IndexOf(s2, StringComparison.Ordinal));

            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> value = s2.AsSpan();
            index = span.IndexOf(value);
            Assert.Equal(1, index);
            Assert.Equal(index, span.IndexOf(value, StringComparison.Ordinal));
        }

        [Fact]
        public static void IndexOfSequenceRestart_Char()
        {
            string s1 = "5172377457778667789";
            string s2 = "778";
            int index = s1.IndexOf(s2);
            Assert.Equal(10, index);
            Assert.Equal(index, s1.IndexOf(s2, StringComparison.Ordinal));

            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> value = s2.AsSpan();
            index = span.IndexOf(value);
            Assert.Equal(10, index);
            Assert.Equal(index, span.IndexOf(value, StringComparison.Ordinal));
        }

        [Fact]
        public static void IndexOfSequenceNoMatch_Char()
        {
            string s1 = "0172377457778667789";
            string s2 = "778X";
            int index = s1.IndexOf(s2);
            Assert.Equal(-1, index);
            Assert.Equal(index, s1.IndexOf(s2, StringComparison.Ordinal));

            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> value = s2.AsSpan();
            index = span.IndexOf(value);
            Assert.Equal(-1, index);
            Assert.Equal(index, span.IndexOf(value, StringComparison.Ordinal));
        }

        [Fact]
        public static void IndexOfSequenceNotEvenAHeadMatch_Char()
        {
            string s1 = "0172377457778667789";
            string s2 = "X789";
            int index = s1.IndexOf(s2);
            Assert.Equal(-1, index);
            Assert.Equal(index, s1.IndexOf(s2, StringComparison.Ordinal));

            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> value = s2.AsSpan();
            index = span.IndexOf(value);
            Assert.Equal(-1, index);
            Assert.Equal(index, span.IndexOf(value, StringComparison.Ordinal));
        }

        [Fact]
        public static void IndexOfSequenceMatchAtVeryEnd_Char()
        {
            string s1 = "012345";
            string s2 = "345";
            int index = s1.IndexOf(s2);
            Assert.Equal(3, index);
            Assert.Equal(index, s1.IndexOf(s2, StringComparison.Ordinal));

            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> value = s2.AsSpan();
            index = span.IndexOf(value);
            Assert.Equal(3, index);
            Assert.Equal(index, span.IndexOf(value, StringComparison.Ordinal));
        }

        [Fact]
        public static void IndexOfSequenceJustPastVeryEnd_Char()
        {
            string s1 = new string(new char[] { '0', '1', '2', '3', '4', '5' }, 0, 5);
            string s2 = "345";
            int index = s1.IndexOf(s2);
            Assert.Equal(-1, index);
            Assert.Equal(index, s1.IndexOf(s2, StringComparison.Ordinal));

            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> value = s2.AsSpan();
            index = span.IndexOf(value);
            Assert.Equal(-1, index);
            Assert.Equal(index, span.IndexOf(value, StringComparison.Ordinal));
        }

        [Fact]
        public static void IndexOfSequenceZeroLengthValue_Char()
        {
            string s1 = "0172377457778667789";
            string s2 = string.Empty;
            int index = s1.IndexOf(s2);
            Assert.Equal(0, index);
            Assert.Equal(index, s1.IndexOf(s2, StringComparison.Ordinal));

            // A zero-length value is always "found" at the start of the span.
            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> value = s2.AsSpan();
            index = span.IndexOf(value);
            Assert.Equal(0, index);
            Assert.Equal(index, span.IndexOf(value, StringComparison.Ordinal));
        }

        [Fact]
        public static void IndexOfSequenceZeroLengthSpan_Char()
        {
            string s1 = string.Empty;
            string s2 = "123";
            int index = s1.IndexOf(s2);
            Assert.Equal(-1, index);
            Assert.Equal(index, s1.IndexOf(s2, StringComparison.Ordinal));

            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> value = s2.AsSpan();
            index = span.IndexOf(value);
            Assert.Equal(-1, index);
            Assert.Equal(index, span.IndexOf(value, StringComparison.Ordinal));
        }

        [Fact]
        public static void IndexOfSequenceLengthOneValue_Char()
        {
            string s1 = "012345";
            string s2 = "2";
            int index = s1.IndexOf(s2);
            Assert.Equal(2, index);
            Assert.Equal(index, s1.IndexOf(s2, StringComparison.Ordinal));

            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> value = s2.AsSpan();
            index = span.IndexOf(value);
            Assert.Equal(2, index);
            Assert.Equal(index, span.IndexOf(value, StringComparison.Ordinal));
        }

        [Fact]
        public static void IndexOfSequenceLengthOneValueAtVeryEnd_Char()
        {
            string s1 = "012345";
            string s2 = "5";
            int index = s1.IndexOf(s2);
            Assert.Equal(5, index);
            Assert.Equal(index, s1.IndexOf(s2, StringComparison.Ordinal));

            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> value = s2.AsSpan();
            index = span.IndexOf(value);
            Assert.Equal(5, index);
            Assert.Equal(index, span.IndexOf(value, StringComparison.Ordinal));
        }

        [Fact]
        public static void IndexOfSequenceLengthOneValueJustPasttVeryEnd_Char()
        {
            string s1 = new string(new char[] { '0', '1', '2', '3', '4', '5' }, 0, 5);
            string s2 = "5";
            int index = s1.IndexOf(s2);
            Assert.Equal(-1, index);
            Assert.Equal(index, s1.IndexOf(s2, StringComparison.Ordinal));

            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> value = s2.AsSpan();
            index = span.IndexOf(value);
            Assert.Equal(-1, index);
            Assert.Equal(index, span.IndexOf(value, StringComparison.Ordinal));
        }

        [Fact]
        public static void TestMatch_Char()
        {
            for (int length = 0; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = (char)(i + 1);
                }

                string s = new string(a);

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    char target = a[targetIndex];
                    int idx = s.IndexOf(target);
                    Assert.Equal(targetIndex, idx);
                }

                ReadOnlySpan<char> span = s.AsSpan();

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    char target = a[targetIndex];
                    int idx = span.IndexOf(target);
                    Assert.Equal(targetIndex, idx);
                }
            }
        }

        [Fact]
        public static void TestMultipleMatch_Char()
        {
            for (int length = 2; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = (char)(i + 1);
                }

                a[length - 1] = (char)200;
                a[length - 2] = (char)200;

                string s = new string(a);
                int idx = s.IndexOf((char)200);
                Assert.Equal(length - 2, idx);

                ReadOnlySpan<char> span = s.AsSpan();
                idx = span.IndexOf((char)200);
                Assert.Equal(length - 2, idx);
            }
        }

        [Fact]
        public static void MakeSureNoChecksGoOutOfRange_Char()
        {
            for (int length = 0; length < 100; length++)
            {
                char[] a = new char[length + 2];
                a[0] = '9';
                a[length + 1] = '9';

                string s = new string(a, 1, length);
                int index = s.IndexOf('9');
                Assert.Equal(-1, index);

                ReadOnlySpan<char> span = s.AsSpan();
                index = span.IndexOf('9');
                Assert.Equal(-1, index);
            }
        }

        [Theory]
        [InlineData("Hello", 0, "!$%", "!$%Hello")]
        [InlineData("Hello", 1, "!$%", "H!$%ello")]
        [InlineData("Hello", 2, "!$%", "He!$%llo")]
        [InlineData("Hello", 3, "!$%", "Hel!$%lo")]
        [InlineData("Hello", 4, "!$%", "Hell!$%o")]
        [InlineData("Hello", 5, "!$%", "Hello!$%")]
        [InlineData("Hello", 3, "", "Hello")]
        [InlineData("", 0, "", "")]
        public static void Insert(string s, int startIndex, string value, string expected)
        {
            Assert.Equal(expected, s.Insert(startIndex, value));
        }

        [Fact]
        public static void Insert_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => "Hello".Insert(0, null)); // Value is null

            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => "Hello".Insert(-1, "!")); // Start index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => "Hello".Insert(6, "!")); // Start index > string.length
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("foo", false)]
        [InlineData("   ", false)]
        public static void IsNullOrEmpty(string value, bool expected)
        {
            Assert.Equal(expected, string.IsNullOrEmpty(value));

            Assert.Equal(expected, value.AsSpan().IsEmpty);
        }

        public static IEnumerable<object[]> IsNullOrWhitespace_TestData()
        {
            for (int i = 0; i < char.MaxValue; i++)
            {
                if (char.IsWhiteSpace((char)i))
                {
                    yield return new object[] { new string((char)i, 3), true };
                    yield return new object[] { new string((char)i, 3) + "x", false };
                }
            }

            yield return new object[] { null, true };
            yield return new object[] { "", true };
            yield return new object[] { "foo", false };
        }

        [Theory]
        [MemberData(nameof(IsNullOrWhitespace_TestData))]
        public static void IsNullOrWhitespace(string value, bool expected)
        {
            Assert.Equal(expected, string.IsNullOrWhiteSpace(value));
            Assert.Equal(string.IsNullOrWhiteSpace(value), value.AsSpan().IsWhiteSpace());
        }

        [Fact]
        public static void ZeroLengthIsWhiteSpace()
        {
            string s1 = string.Empty;
            bool result = string.IsNullOrWhiteSpace(s1);
            Assert.True(result);

            ReadOnlySpan<char> span = s1.AsSpan();
            result = span.IsWhiteSpace();
            Assert.Equal(string.IsNullOrWhiteSpace(string.Empty), result);
        }

        [Fact]
        public static void IsWhiteSpaceTrueLatin1()
        {
            Random rand = new Random(42);
            for (int length = 0; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = s_whiteSpaceCharacters[rand.Next(0, s_whiteSpaceCharacters.Length - 1)];
                }

                string s1 = new string(a);
                bool result = string.IsNullOrWhiteSpace(s1);
                Assert.True(result);

                for (int i = 0; i < s_whiteSpaceCharacters.Length - 1; i++)
                {
                    s1 = new string(Enumerable.Repeat(s_whiteSpaceCharacters[i], a.Length).ToArray());
                    Assert.True(string.IsNullOrWhiteSpace(s1));
                }

                var span = new Span<char>(a);
                result = ((ReadOnlySpan<char>)span).IsWhiteSpace();
                Assert.Equal(string.IsNullOrWhiteSpace(new string(a)), result);

                for (int i = 0; i < s_whiteSpaceCharacters.Length - 1; i++)
                {
                    span.Fill(s_whiteSpaceCharacters[i]);
                    Assert.Equal(string.IsNullOrWhiteSpace(new string(span.ToArray())), ((ReadOnlySpan<char>)span).IsWhiteSpace());
                }
            }
        }

        [Fact]
        public static void IsWhiteSpaceTrue()
        {
            Random rand = new Random(42);
            for (int length = 0; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = s_whiteSpaceCharacters[rand.Next(0, s_whiteSpaceCharacters.Length)];
                }

                string s1 = new string(a);
                bool result = string.IsNullOrWhiteSpace(s1);
                Assert.True(result);

                ReadOnlySpan<char> span = s1.AsSpan();
                result = span.IsWhiteSpace();
                Assert.Equal(string.IsNullOrWhiteSpace(new string(span.ToArray())), result);
            }
        }

        [Fact]
        public static void IsWhiteSpaceFalse()
        {
            Random rand = new Random(42);
            for (int length = 1; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = s_whiteSpaceCharacters[rand.Next(0, s_whiteSpaceCharacters.Length)];
                }
                var span = new Span<char>(a);

                // first character is not a white-space character
                a[0] = 'a';
                string s1 = new string(a);
                bool result = string.IsNullOrWhiteSpace(s1);
                Assert.False(result);
                result = ((ReadOnlySpan<char>)span).IsWhiteSpace();
                Assert.Equal(string.IsNullOrWhiteSpace(new string(span.ToArray())), result);
                a[0] = ' ';

                // last character is not a white-space character
                a[length - 1] = 'a';
                s1 = new string(a);
                result = string.IsNullOrWhiteSpace(s1);
                Assert.False(result);
                result = ((ReadOnlySpan<char>)span).IsWhiteSpace();
                Assert.Equal(string.IsNullOrWhiteSpace(new string(span.ToArray())), result);
                a[length - 1] = ' ';

                // character in the middle is not a white-space character
                a[length / 2] = 'a';
                s1 = new string(a);
                result = string.IsNullOrWhiteSpace(s1);
                Assert.False(result);
                result = ((ReadOnlySpan<char>)span).IsWhiteSpace();
                Assert.Equal(string.IsNullOrWhiteSpace(new string(span.ToArray())), result);
                a[length / 2] = ' ';

                // no character is a white-space character
                span.Fill('a');
                s1 = new string(span.ToArray());
                result = string.IsNullOrWhiteSpace(s1);
                Assert.False(result);
                result = ((ReadOnlySpan<char>)span).IsWhiteSpace();
                Assert.Equal(string.IsNullOrWhiteSpace(new string(span.ToArray())), result);
            }
        }

        [Fact]
        public static void MakeSureNoIsWhiteSpaceChecksGoOutOfRange()
        {
            for (int length = 3; length < 64; length++)
            {
                char[] first = new char[length];
                first[0] = ' ';
                first[length - 1] = ' ';

                string s1 = new string(first, 1, length - 2);
                bool result = string.IsNullOrWhiteSpace(s1);
                Assert.False(result);

                ReadOnlySpan<char> span = s1.AsSpan();
                result = span.IsWhiteSpace();
                Assert.Equal(string.IsNullOrWhiteSpace(new string(span.ToArray())), result);
            }
        }

        [Theory]
        [InlineData("$$", new string[] { }, 0, 0, "")]
        [InlineData("$$", new string[] { null }, 0, 1, "")]
        [InlineData("$$", new string[] { null, "Bar", null }, 0, 3, "$$Bar$$")]
        [InlineData("$$", new string[] { "", "", "" }, 0, 3, "$$$$")]
        [InlineData("", new string[] { "", "", "" }, 0, 3, "")]
        [InlineData(null, new string[] { "Foo", "Bar", "Baz" }, 0, 3, "FooBarBaz")]
        [InlineData("$$", new string[] { "Foo", "Bar", "Baz" }, 0, 3, "Foo$$Bar$$Baz")]
        [InlineData("$$", new string[] { "Foo", "Bar", "Baz" }, 3, 0, "")]
        [InlineData("$$", new string[] { "Foo", "Bar", "Baz" }, 1, 1, "Bar")]
        public static void Join_StringArray(string separator, string[] values, int startIndex, int count, string expected)
        {
            if (startIndex + count == values.Length && count != 0)
            {
                Assert.Equal(expected, string.Join(separator, values));

                var iEnumerableStringOptimized = new List<string>(values);
                Assert.Equal(expected, string.Join(separator, iEnumerableStringOptimized));
                Assert.Equal(expected, string.Join<string>(separator, iEnumerableStringOptimized)); // Call the generic IEnumerable<T>-based overload

                var iEnumerableStringNotOptimized = new Queue<string>(values);
                Assert.Equal(expected, string.Join(separator, iEnumerableStringNotOptimized));
                Assert.Equal(expected, string.Join<string>(separator, iEnumerableStringNotOptimized));

                var iEnumerableObject = new List<object>(values);
                Assert.Equal(expected, string.Join(separator, iEnumerableObject));

                // Bug/Documented behavior: Join(string, object[]) returns "" when the first item in the array is null
                if (values.Length == 0 || values[0] != null)
                {
                    var arrayOfObjects = (object[])values;
                    Assert.Equal(expected, string.Join(separator, arrayOfObjects));
                }
            }
            Assert.Equal(expected, string.Join(separator, values, startIndex, count));
        }

        [Fact]
        public static void Join_String_NullValues_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => string.Join("$$", null));
            AssertExtensions.Throws<ArgumentNullException>("value", () => string.Join("$$", null, 0, 0));
            AssertExtensions.Throws<ArgumentNullException>("values", () => string.Join("|", (IEnumerable<string>)null));
            AssertExtensions.Throws<ArgumentNullException>("values", () => string.Join<string>("|", (IEnumerable<string>)null)); // Generic overload
        }

        [Fact]
        public static void Join_String_NegativeCount_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => string.Join("$$", new string[] { "Foo" }, 0, -1));
        }

        [Theory]
        [InlineData(2, 1)]
        [InlineData(2, 0)]
        [InlineData(1, 2)]
        [InlineData(1, 1)]
        [InlineData(0, 2)]
        [InlineData(-1, 0)]
        public static void Join_String_InvalidStartIndexCount_ThrowsArgumentOutOfRangeException(int startIndex, int count)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => string.Join("$$", new string[] { "Foo" }, startIndex, count));
        }

        public static IEnumerable<object[]> Join_ObjectArray_TestData()
        {
            yield return new object[] { "$$", new object[] { }, "" };
            yield return new object[] { "$$", new object[] { new ObjectWithNullToString() }, "" };
            yield return new object[] { "$$", new object[] { "Foo" }, "Foo" };
            yield return new object[] { "$$", new object[] { "Foo", "Bar", "Baz" }, "Foo$$Bar$$Baz" };
            yield return new object[] { null, new object[] { "Foo", "Bar", "Baz" }, "FooBarBaz" };
            yield return new object[] { "$$", new object[] { "Foo", null, "Baz" }, "Foo$$$$Baz" };

            // Test join when first value is null
            yield return new object[] { "$$", new object[] { null, "Bar", "Baz" }, "$$Bar$$Baz" };

            // Join should ignore objects that have a null ToString() value
            yield return new object[] { "|", new object[] { new ObjectWithNullToString(), "Foo", new ObjectWithNullToString(), "Bar", new ObjectWithNullToString() }, "|Foo||Bar|" };
        }

        [Theory]
        [MemberData(nameof(Join_ObjectArray_TestData))]
        public static void Join_ObjectArray(string separator, object[] values, string expected)
        {
            Assert.Equal(expected, string.Join(separator, values));
            Assert.Equal(expected, string.Join(separator, (IEnumerable<object>)values));
        }

        [Fact]
        public static void Join_ObjectArray_Null_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("values", () => string.Join("$$", (object[])null));
            AssertExtensions.Throws<ArgumentNullException>("values", () => string.Join("--", (IEnumerable<object>)null));
        }

        [Theory]
        [InlineData("Hello", 'l', 4, 5, 3)]
        [InlineData("Hello", 'x', 4, 5, -1)]
        [InlineData("Hello", 'l', 3, 4, 3)]
        [InlineData("Hello", 'l', 1, 2, -1)]
        [InlineData("Hello", 'l', 0, 1, -1)]
        [InlineData("Hello", 'x', 3, 4, -1)]
        [InlineData("Hello", 'l', 1, 0, -1)]
        [InlineData("Hello", 'l', 4, 2, 3)]
        [InlineData("Hello", 'l', 4, 3, 3)]
        [InlineData("H" + SoftHyphen + "ello", 'H', 2, 3, 0)]
        [InlineData("", 'H', 0, 0, -1)]
        public static void LastIndexOf_SingleLetter(string s, char value, int startIndex, int count, int expected)
        {
            int startPos = count == 0 ? 0 : startIndex - count + 1;
            ReadOnlySpan<char> span = s.AsSpan(startPos, count);
            int expectedFromSpan = expected - startPos;
            ReadOnlySpan<char> valueSpan = value.ToString().AsSpan();

            if (count == s.Length)
            {
                if (startIndex == s.Length - 1)
                {
                    Assert.Equal(expected, s.LastIndexOf(value));
                    Assert.Equal(expected, s.LastIndexOf(value.ToString()));
                }
                Assert.Equal(expected, s.LastIndexOf(value, startIndex));
                Assert.Equal(expected, s.LastIndexOf(value.ToString(), startIndex));

                Assert.Equal(expectedFromSpan, span.LastIndexOf(value));
            }
            Assert.Equal(expected, s.LastIndexOf(value, startIndex, count));
            Assert.Equal(expected, s.LastIndexOf(value.ToString(), startIndex, count));

            Assert.Equal(expectedFromSpan, span.LastIndexOf(value));
            Assert.Equal(expectedFromSpan, span.LastIndexOf(valueSpan));

            Assert.Equal(expected, s.LastIndexOf(value.ToString(), startIndex, count, StringComparison.CurrentCulture));
            Assert.Equal(expected, s.LastIndexOf(value.ToString(), startIndex, count, StringComparison.Ordinal));
            Assert.Equal(expected, s.LastIndexOf(value.ToString(), startIndex, count, StringComparison.OrdinalIgnoreCase));
            Assert.Equal(expected, s.LastIndexOf(value.ToString(), startIndex, count, StringComparison.InvariantCulture));
            Assert.Equal(expected, s.LastIndexOf(value.ToString(), startIndex, count, StringComparison.InvariantCultureIgnoreCase));

            Assert.Equal(expectedFromSpan, span.LastIndexOf(valueSpan, StringComparison.CurrentCulture));
            Assert.Equal(expectedFromSpan, span.LastIndexOf(valueSpan, StringComparison.Ordinal));
            Assert.Equal(expectedFromSpan, span.LastIndexOf(valueSpan, StringComparison.OrdinalIgnoreCase));
            Assert.Equal(expectedFromSpan, span.LastIndexOf(valueSpan, StringComparison.InvariantCulture));
            Assert.Equal(expectedFromSpan, span.LastIndexOf(valueSpan, StringComparison.InvariantCultureIgnoreCase));
        }

        [Fact]
        public static void LastIndexOf_Match_SingleLetter()
        {
            Assert.Equal(-1, "".LastIndexOf('a'));
            Assert.Equal(-1, "".AsSpan().LastIndexOf('a'));

            for (int length = 1; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = (char)(i + 1);
                }
                string str = new string(a);
                ReadOnlySpan<char> span = new ReadOnlySpan<char>(a);

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    char target = a[targetIndex];
                    int idx = str.LastIndexOf(target);
                    Assert.Equal(targetIndex, idx);

                    idx = span.LastIndexOf(target);
                    Assert.Equal(targetIndex, idx);
                }
            }
        }

        // NOTE: This is by design. ICU ignores the null characters (i.e. null characters have no weights for the string comparison).
        // For desired behavior, use ordinal comparison instead of linguistic comparison.
        // This is a known difference between NLS and ICU (https://github.com/dotnet/runtime/issues/4673).
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNlsGlobalization))]
        [InlineData("He\0lo", "He\0lo", 0)]
        [InlineData("He\0lo", "He\0", 0)]
        [InlineData("He\0lo", "\0", 2)]
        [InlineData("He\0lo", "\0lo", 2)]
        [InlineData("He\0lo", "lo", 3)]
        [InlineData("Hello", "lo\0", -1)]
        [InlineData("Hello", "\0lo", -1)]
        [InlineData("Hello", "l\0o", -1)]
        public static void LastIndexOf_NullInStrings(string s, string value, int expected)
        {
            Assert.Equal(expected, s.LastIndexOf(value));
        }

        [Theory]
        [MemberData(nameof(AllSubstringsAndComparisons), new object[] { "abcde" })]
        public static void LastIndexOf_AllSubstrings(string s, string value, int startIndex, StringComparison comparison)
        {
            bool ignoringCase = comparison == StringComparison.OrdinalIgnoreCase || comparison == StringComparison.CurrentCultureIgnoreCase;

            if (value.Length == 0)
            {
                int expectedStartIndex = startIndex;
                if (s.Length == 0 && (startIndex == -1 || startIndex == 0))
                    expectedStartIndex = 0; // empty string occurs at beginning of search space
                if (s.Length > 0 && startIndex < s.Length)
                    expectedStartIndex = startIndex + 1; // empty string occurs just after the last char included in the search space

                Assert.Equal(s.Length, s.LastIndexOf(value, comparison));
                Assert.Equal(expectedStartIndex, s.LastIndexOf(value, startIndex, comparison));
                Assert.Equal(s.Length, s.AsSpan().LastIndexOf(value.AsSpan(), comparison));
                return;
            }

            if (s.Length == 0)
            {
                // unit test shouldn't have passed a weightless string to this routine
                Assert.NotEqual(value, string.Empty, StringComparer.FromComparison(comparison));

                Assert.Equal(-1, s.LastIndexOf(value, comparison));
                Assert.Equal(-1, s.LastIndexOf(value, startIndex, comparison));
                Assert.Equal(-1, s.AsSpan().LastIndexOf(value.AsSpan(), comparison));
                return;
            }

            // First find the substring.  We should be able to with all comparison types.
            Assert.Equal(startIndex, s.LastIndexOf(value, comparison)); // in the whole string
            Assert.Equal(startIndex, s.LastIndexOf(value, startIndex + value.Length - 1, comparison)); // starting at end of substring
            Assert.Equal(startIndex, s.LastIndexOf(value, startIndex + value.Length, comparison)); // starting just beyond end of substring
            Assert.Equal(startIndex, s.AsSpan().LastIndexOf(value.AsSpan(), comparison)); // in the whole string
            Assert.Equal(0, s.AsSpan(startIndex).LastIndexOf(value.AsSpan(), comparison)); // starting at end of substring

            if (startIndex + value.Length < s.Length)
            {
                Assert.Equal(startIndex, s.LastIndexOf(value, startIndex + value.Length + 1, comparison)); // starting a bit more beyond end of substring
                Assert.Equal(startIndex, s.AsSpan(0, startIndex + value.Length + 1).LastIndexOf(value.AsSpan(), comparison)); // starting a bit more beyond end of substring
            }
            if (startIndex + value.Length > 1)
            {
                Assert.Equal(-1, s.LastIndexOf(value, startIndex + value.Length - 2, comparison)); // starting before end of substring
                Assert.Equal(-1, s.AsSpan(0, startIndex + value.Length - 2).LastIndexOf(value.AsSpan(), comparison)); // starting before end of substring
            }

            // Shouldn't be able to find the substring if the count is less than substring's length
            Assert.Equal(-1, s.LastIndexOf(value, s.Length - 1, value.Length - 1, comparison));

            // Now double the source.  Make sure we find the second copy of the substring.
            int halfLen = s.Length;
            s += s;
            Assert.Equal(halfLen + startIndex, s.LastIndexOf(value, comparison));
            Assert.Equal(halfLen + startIndex, s.AsSpan().LastIndexOf(value.AsSpan(), comparison));

            // Now change the case of a letter.
            s = s.ToUpperInvariant();
            Assert.Equal(ignoringCase ? halfLen + startIndex : -1, s.LastIndexOf(value, comparison));
            Assert.Equal(ignoringCase ? halfLen + startIndex : -1, s.AsSpan().LastIndexOf(value.AsSpan(), comparison));
        }

        [Fact]
        public static void LastIndexOf_Invalid()
        {
            string s = "foo";

            // Value is null
            AssertExtensions.Throws<ArgumentNullException>("value", () => s.LastIndexOf(null));
            AssertExtensions.Throws<ArgumentNullException>("value", () => s.LastIndexOf(null, StringComparison.CurrentCulture));
            AssertExtensions.Throws<ArgumentNullException>("value", () => s.LastIndexOf(null, 0));
            AssertExtensions.Throws<ArgumentNullException>("value", () => s.LastIndexOf(null, 0, 0));
            AssertExtensions.Throws<ArgumentNullException>("value", () => s.LastIndexOf(null, 0, 0, StringComparison.CurrentCulture));

            // Start index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s.LastIndexOf('a', -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s.LastIndexOf('a', -1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s.LastIndexOf("a", -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s.LastIndexOf("a", -1, StringComparison.CurrentCulture));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s.LastIndexOf("a", -1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s.LastIndexOf("a", -1, 0, StringComparison.CurrentCulture));

            // Start index > string.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s.LastIndexOf('a', s.Length + 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s.LastIndexOf('a', s.Length + 1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s.LastIndexOf("a", s.Length + 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s.LastIndexOf("a", s.Length + 1, StringComparison.CurrentCulture));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s.LastIndexOf("a", s.Length + 1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s.LastIndexOf("a", s.Length + 1, 0, StringComparison.CurrentCulture));

            // Count < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s.LastIndexOf('a', 0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s.LastIndexOf("a", 0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s.LastIndexOf("a", 0, -1, StringComparison.CurrentCulture));

            // Start index - count + 1 < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s.LastIndexOf('a', 0, s.Length + 2));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s.LastIndexOf("a", 0, s.Length + 2));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s.LastIndexOf("a", 0, s.Length + 2, StringComparison.CurrentCulture));

            // Invalid comparison type
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => s.LastIndexOf("a", StringComparison.CurrentCulture - 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => s.AsSpan().LastIndexOf("a".AsSpan(), StringComparison.CurrentCulture - 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => s.LastIndexOf("a", StringComparison.OrdinalIgnoreCase + 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => s.AsSpan().LastIndexOf("a".AsSpan(), StringComparison.OrdinalIgnoreCase + 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => s.LastIndexOf("a", 0, StringComparison.CurrentCulture - 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => s.LastIndexOf("a", 0, StringComparison.OrdinalIgnoreCase + 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => s.LastIndexOf("a", 0, 0, StringComparison.CurrentCulture - 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => s.LastIndexOf("a", 0, 0, StringComparison.OrdinalIgnoreCase + 1));
        }

        [Fact]
        public static void LastIndexOf_TurkishI_TurkishCulture()
        {
            using (new ThreadCultureChange("tr-TR"))
            {
                string s = "Turkish I \u0131s TROUBL\u0130NG!";
                ReadOnlySpan<char> span = s.AsSpan();
                string value = "\u0130";

                Assert.Equal(19, s.LastIndexOf(value));
                Assert.Equal(19, s.LastIndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(19, s.LastIndexOf(value, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(19, s.LastIndexOf(value, StringComparison.Ordinal));
                Assert.Equal(19, s.LastIndexOf(value, StringComparison.OrdinalIgnoreCase));
                Assert.Equal(19, span.LastIndexOf(value.AsSpan()));
                Assert.Equal(19, span.LastIndexOf(value.AsSpan(), StringComparison.CurrentCulture));
                Assert.Equal(19, span.LastIndexOf(value.AsSpan(), StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(19, span.LastIndexOf(value.AsSpan(), StringComparison.Ordinal));
                Assert.Equal(19, span.LastIndexOf(value.AsSpan(), StringComparison.OrdinalIgnoreCase));

                value = "\u0131";
                Assert.Equal(10, s.LastIndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(10, s.LastIndexOf(value, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(10, s.LastIndexOf(value, StringComparison.Ordinal));
                Assert.Equal(10, s.LastIndexOf(value, StringComparison.OrdinalIgnoreCase));
                Assert.Equal(10, span.LastIndexOf(value.AsSpan(), StringComparison.CurrentCulture));
                Assert.Equal(10, span.LastIndexOf(value.AsSpan(), StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(10, span.LastIndexOf(value.AsSpan(), StringComparison.Ordinal));
                Assert.Equal(10, span.LastIndexOf(value.AsSpan(), StringComparison.OrdinalIgnoreCase));
            }
        }

        [Fact]
        public static void LastIndexOf_TurkishI_InvariantCulture()
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                string s = "Turkish I \u0131s TROUBL\u0130NG!";
                ReadOnlySpan<char> span = s.AsSpan();
                string value = "\u0130";

                Assert.Equal(19, s.LastIndexOf(value));
                Assert.Equal(19, s.LastIndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(19, s.LastIndexOf(value, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(19, span.LastIndexOf(value.AsSpan()));
                Assert.Equal(19, span.LastIndexOf(value.AsSpan(), StringComparison.CurrentCulture));
                Assert.Equal(19, span.LastIndexOf(value.AsSpan(), StringComparison.CurrentCultureIgnoreCase));

                value = "\u0131";
                Assert.Equal(10, s.LastIndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(10, s.LastIndexOf(value, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(10, span.LastIndexOf(value.AsSpan(), StringComparison.CurrentCulture));
                Assert.Equal(10, span.LastIndexOf(value.AsSpan(), StringComparison.CurrentCultureIgnoreCase));
            }
        }

        [Fact]
        public static void LastIndexOf_TurkishI_EnglishUSCulture()
        {
            using (new ThreadCultureChange("en-US"))
            {
                string s = "Turkish I \u0131s TROUBL\u0130NG!";
                ReadOnlySpan<char> span = s.AsSpan();
                string value = "\u0130";

                Assert.Equal(19, s.LastIndexOf(value));
                Assert.Equal(19, s.LastIndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(19, s.LastIndexOf(value, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(19, span.LastIndexOf(value.AsSpan()));
                Assert.Equal(19, span.LastIndexOf(value.AsSpan(), StringComparison.CurrentCulture));
                Assert.Equal(19, span.LastIndexOf(value.AsSpan(), StringComparison.CurrentCultureIgnoreCase));

                value = "\u0131";
                Assert.Equal(10, s.LastIndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(10, s.LastIndexOf(value, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(10, span.LastIndexOf(value.AsSpan(), StringComparison.CurrentCulture));
                Assert.Equal(10, span.LastIndexOf(value.AsSpan(), StringComparison.CurrentCultureIgnoreCase));
            }
        }

        [Theory]
        [InlineData("foo", 3)]
        [InlineData("hello", 5)]
        [InlineData("", 0)]
        public static void LastIndexOf_EmptyString(string s, int expected)
        {
            Assert.Equal(expected, s.LastIndexOf("", StringComparison.OrdinalIgnoreCase));
            Assert.Equal(expected, s.AsSpan().LastIndexOf("".AsSpan(), StringComparison.OrdinalIgnoreCase));
        }

        [Theory]
        [InlineData("Hello", new char[] { 'd' }, 4, 5, -1)]
        [InlineData("Hello", new char[] { 'l' }, 4, 5, 3)]
        [InlineData("Hello", new char[] { 'e', 'l' }, 4, 5, 3)]
        [InlineData("Hello", new char[] { 'd', 'e', 'l' }, 4, 5, 3)]
        [InlineData("Hello", new char[] { 'd', 'e', 'l' }, 4, 0, -1)]
        [InlineData("Hello", new char[] { 'd', 'e', 'f' }, 2, 3, 1)]
        [InlineData("Hello", new char[] { 'a', 'b', 'c' }, 2, 3, -1)]
        [InlineData("Hello", new char[] { 'a', 'b', 'c', 'd' }, 2, 3, -1)]
        [InlineData("Hello", new char[] { 'a', 'b', 'c', 'e' }, 2, 3, 1)]
        [InlineData("Hello", new char[0], 2, 3, -1)]
        [InlineData("H" + SoftHyphen + "ello", new char[] { 'a', '\u00AD', 'c' }, 2, 3, 1)]
        [InlineData("", new char[] { 'd', 'e', 'f' }, -1, -1, -1)]
        [InlineData("\x4E16\x754C\x60A8\x597D", new char[] { '\x754C' }, 3, 4, 1)]
        [InlineData("\x4E16\x754C\x60A8\x597D", new char[] { '\x754C', '\x60A8' }, 3, 4, 2)]
        [InlineData("\x4E16\x754C\x60A8\x597D", new char[] { '\x754C', '\x60A8', '\x4E16' }, 3, 4, 2)]
        [InlineData("\x4E16\x754C\x60A8\x597D", new char[] { '\x754C', '\x60A8', '\x4E16', '\x597D' }, 3, 4, 3)]
        public static void LastIndexOfAny(string s, char[] anyOf, int startIndex, int count, int expected)
        {
            if (count == startIndex + 1)
            {
                if (startIndex == s.Length - 1)
                {
                    Assert.Equal(expected, s.LastIndexOfAny(anyOf));
                }
                Assert.Equal(expected, s.LastIndexOfAny(anyOf, startIndex));
            }

            foreach (char[] permutedAnyOf in Permute(anyOf))
            {
                Assert.Equal(expected, s.LastIndexOfAny(permutedAnyOf, startIndex, count));
            }
        }

        [Fact]
        public static void LastIndexOfAny_Invalid()
        {
            // AnyOf is null
            Assert.Throws<ArgumentNullException>(() => "foo".LastIndexOfAny(null));
            Assert.Throws<ArgumentNullException>(() => "foo".LastIndexOfAny(null, 0));
            Assert.Throws<ArgumentNullException>(() => "foo".LastIndexOfAny(null, 0, 0));

            // Start index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => "foo".LastIndexOfAny(new char[] { 'o' }, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => "foo".LastIndexOfAny(new char[] { 'o' }, -1, 0));

            // Start index > string.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => "foo".LastIndexOfAny(new char[] { 'o' }, 4));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => "foo".LastIndexOfAny(new char[] { 'o' }, 4, 0));

            // Count < 0 or count > string.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => "foo".LastIndexOfAny(new char[] { 'o' }, 0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => "foo".LastIndexOfAny(new char[] { 'o' }, 0, 4));

            // Start index + count > string.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => "foo".LastIndexOfAny(new char[] { 'o' }, 3, 1));
        }

        [Fact]
        public static void ZeroLengthLastIndexOf_Char()
        {
            string s1 = string.Empty;
            int idx = s1.LastIndexOf((char)0);
            Assert.Equal(-1, idx);

            ReadOnlySpan<char> sp = new ReadOnlySpan<char>(Array.Empty<char>());
            idx = sp.LastIndexOf((char)0);
            Assert.Equal(-1, idx);
        }

        [Fact]
        public static void TestMatchLastIndexOf_Char()
        {
            for (int length = 0; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = (char)(i + 1);
                }

                string s1 = new string(a);

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    char target = a[targetIndex];
                    int idx = s1.LastIndexOf(target);
                    Assert.Equal(targetIndex, idx);
                }

                ReadOnlySpan<char> span = s1.AsSpan();

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    char target = a[targetIndex];
                    int idx = span.LastIndexOf(target);
                    Assert.Equal(targetIndex, idx);
                }
            }
        }

        [Fact]
        public static void TestMultipleMatchLastIndexOf_Char()
        {
            for (int length = 2; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = (char)(i + 1);
                }

                a[length - 1] = (char)200;
                a[length - 2] = (char)200;

                string s1 = new string(a);
                int idx = s1.LastIndexOf((char)200);
                Assert.Equal(length - 1, idx);

                ReadOnlySpan<char> span = s1.AsSpan();
                idx = span.LastIndexOf((char)200);
                Assert.Equal(length - 1, idx);
            }
        }

        [Fact]
        public static void MakeSureNoChecksGoOutOfRangeLastIndexOf_Char()
        {
            for (int length = 0; length < 100; length++)
            {
                char[] a = new char[length + 2];
                a[0] = '9';
                a[length + 1] = '9';

                string s1 = new string(a, 1, length);
                int index = s1.LastIndexOf('9');
                Assert.Equal(-1, index);

                ReadOnlySpan<char> span = s1.AsSpan();
                index = span.LastIndexOf('9');
                Assert.Equal(-1, index);
            }
        }

        [Fact]
        public static void LastIndexOfSequenceMatchAtStart_Char()
        {
            string s1 = "5172377457778667789";
            string s2 = "517";
            int index = s1.LastIndexOf(s2);
            Assert.Equal(0, index);

            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> value = s2.AsSpan();
            index = span.LastIndexOf(value);
            Assert.Equal(0, index);
        }

        [Fact]
        public static void LastIndexOfSequenceMultipleMatch_Char()
        {
            string s1 = "1231231231";
            string s2 = "23";
            int index = s1.LastIndexOf(s2);
            Assert.Equal(7, index);

            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> value = s2.AsSpan();
            index = span.LastIndexOf(value);
            Assert.Equal(7, index);
        }

        [Fact]
        public static void LastIndexOfSequenceRestart_Char()
        {
            string s1 = "5172377457778667769701";
            string s2 = "778";
            int index = s1.LastIndexOf(s2);
            Assert.Equal(10, index);

            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> value = s2.AsSpan();
            index = span.LastIndexOf(value);
            Assert.Equal(10, index);
        }

        [Fact]
        public static void LastIndexOfSequenceNoMatch_Char()
        {
            string s1 = "0172377457778667789";
            string s2 = "778X";
            int index = s1.LastIndexOf(s2);
            Assert.Equal(-1, index);

            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> value = s2.AsSpan();
            index = span.LastIndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void LastIndexOfSequenceNotEvenAHeadMatch_Char()
        {
            string s1 = "'0172377457778667789";
            string s2 = "X789";
            int index = s1.LastIndexOf(s2);
            Assert.Equal(-1, index);

            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> value = s2.AsSpan();
            index = span.LastIndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void LastIndexOfSequenceMatchAtVeryEnd_Char()
        {
            string s1 = "012345";
            string s2 = "345";
            int index = s1.LastIndexOf(s2);
            Assert.Equal(3, index);

            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> value = s2.AsSpan();
            index = span.LastIndexOf(value);
            Assert.Equal(3, index);
        }

        [Fact]
        public static void LastIndexOfSequenceJustPastVeryEnd_Char()
        {
            string s1 = new string(new char[] { '0', '1', '2', '3', '4', '5' }, 0, 5);
            string s2 = "345";
            int index = s1.LastIndexOf(s2);
            Assert.Equal(-1, index);

            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> value = s2.AsSpan();
            index = span.LastIndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void LastIndexOfSequenceZeroLengthValue_Char()
        {
            string s1 = "0172377457778667789";
            string s2 = string.Empty;
            int index = s1.LastIndexOf(s2);
            Assert.Equal(s1.Length, index);

            // A zero-length value is always "found" at the end of the span.
            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> value = s2.AsSpan();
            index = span.LastIndexOf(value);
            Assert.Equal(span.Length, index);
        }

        [Fact]
        public static void LastIndexOfSequenceZeroLengthSpan_Char()
        {
            string s1 = string.Empty;
            string s2 = "123";
            int index = s1.LastIndexOf(s2);
            Assert.Equal(-1, index);

            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> value = s2.AsSpan();
            index = span.LastIndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void LastIndexOfSequenceLengthOneValue_Char()
        {
            string s1 = "012345";
            string s2 = "2";
            int index = s1.LastIndexOf(s2);
            Assert.Equal(2, index);

            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> value = s2.AsSpan();
            index = span.LastIndexOf(value);
            Assert.Equal(2, index);
        }

        [Fact]
        public static void LastIndexOfSequenceLengthOneValueAtVeryEnd_Char()
        {
            string s1 = "012345";
            string s2 = "5";
            int index = s1.LastIndexOf(s2);
            Assert.Equal(5, index);

            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> value = s2.AsSpan();
            index = span.LastIndexOf(value);
            Assert.Equal(5, index);
        }

        [Fact]
        public static void LastIndexOfSequenceLengthOneValueMultipleTimes_Char()
        {
            string s1 = "015345";
            string s2 = "5";
            int index = s1.LastIndexOf(s2);
            Assert.Equal(5, index);

            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> value = s2.AsSpan();
            index = span.LastIndexOf(value);
            Assert.Equal(5, index);
        }

        [Fact]
        public static void LastIndexOfSequenceLengthOneValueJustPasttVeryEnd_Char()
        {
            string s1 = new string(new char[] { '0', '1', '2', '3', '4', '5' }, 0, 5);
            string s2 = "5";
            int index = s1.LastIndexOf(s2);
            Assert.Equal(-1, index);

            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> value = s2.AsSpan();
            index = span.LastIndexOf(value);
            Assert.Equal(-1, index);
        }

        [Theory]
        [InlineData("Hello", 5, ' ', "Hello")]
        [InlineData("Hello", 7, ' ', "  Hello")]
        [InlineData("Hello", 7, '.', "..Hello")]
        [InlineData("", 0, '.', "")]
        public static void PadLeft(string s, int totalWidth, char paddingChar, string expected)
        {
            if (paddingChar == ' ')
            {
                Assert.Equal(expected, s.PadLeft(totalWidth));
            }
            Assert.Equal(expected, s.PadLeft(totalWidth, paddingChar));
        }

        [Fact]
        public static void PadLeft_NegativeTotalWidth_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("totalWidth", () => "".PadLeft(-1, '.'));
        }

        [Theory]
        [InlineData("Hello", 5, ' ', "Hello")]
        [InlineData("Hello", 7, ' ', "Hello  ")]
        [InlineData("Hello", 7, '.', "Hello..")]
        [InlineData("", 0, '.', "")]
        public static void PadRight(string s, int totalWidth, char paddingChar, string expected)
        {
            if (paddingChar == ' ')
            {
                Assert.Equal(expected, s.PadRight(totalWidth));
            }
            Assert.Equal(expected, s.PadRight(totalWidth, paddingChar));
        }

        [Fact]
        public static void PadRight_NegativeTotalWidth_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("totalWidth", () => "".PadRight(-1, '.'));
        }

        [Theory]
        [InlineData("Hello", 2, 3, "He")]
        [InlineData("Hello", 1, 2, "Hlo")]
        [InlineData("Hello", 0, 5, "")]
        [InlineData("Hello", 5, 0, "Hello")]
        [InlineData("Hello", 0, 0, "Hello")]
        [InlineData("", 0, 0, "")]
        public static void Remove(string s, int startIndex, int count, string expected)
        {
            if (startIndex + count == s.Length)
            {
                Assert.Equal(expected, s.Remove(startIndex));
            }
            Assert.Equal(expected, s.Remove(startIndex, count));
        }

        [Fact]
        public static void Remove_Invalid()
        {
            string s = "Hello";

            // Start index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s.Remove(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s.Remove(-1, 0));

            // Start index > string.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s.Remove(s.Length + 1));

            // Count < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s.Remove(0, -1));

            // Start index + count > string.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s.Remove(0, s.Length + 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s.Remove(s.Length + 1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s.Remove(s.Length, 1));
        }

        [Theory]
        // -------------------- For Vector<ushort>.Count == 8 (SSE2 / ARM NEON) --------------------
        [InlineData("Aaaaaaaa", 'A', 'a', "aaaaaaaa")] // Single iteration of vectorised path; no remainders through non-vectorised path
        // Three leading 'a's before a match (copyLength > 0), Single iteration of vectorised path; no remainders through non-vectorised path
        [InlineData("aaaAaaaaaaa", 'A', 'a', "aaaaaaaaaaa")]
        // Single iteration of vectorised path; 3 remainders through non-vectorised path
        [InlineData("AaaaaaaaaAa", 'A', 'a', "aaaaaaaaaaa")]
        // ------------------------- For Vector<ushort>.Count == 16 (AVX2) -------------------------
        [InlineData("AaaaaaaaAaaaaaaa", 'A', 'a', "aaaaaaaaaaaaaaaa")] // Single iteration of vectorised path; no remainders through non-vectorised path
        // Three leading 'a's before a match (copyLength > 0), Single iteration of vectorised path; no remainders through non-vectorised path
        [InlineData("aaaAaaaaaaaAaaaaaaa", 'A', 'a', "aaaaaaaaaaaaaaaaaaa")]
        // Single iteration of vectorised path; 3 remainders through non-vectorised path
        [InlineData("AaaaaaaaAaaaaaaaaAa", 'A', 'a', "aaaaaaaaaaaaaaaaaaa")]
        // ----------------------------------- General test data -----------------------------------
        [InlineData("Hello", 'l', '!', "He!!o")] // 2 match, non-vectorised path
        [InlineData("Hello", 'e', 'e', "Hello")] // oldChar and newChar are same; nothing to replace
        [InlineData("Hello", 'a', 'b', "Hello")] // No match
        [InlineData("This is a very nice sentence", 'z', 'y', "This is a very nice sentence")] // No match
        // Three matches, vectorised path; 4(128bit)/12(256bit) remainders through non-vectorised path
        [InlineData("This is a very nice sentence", 'i', 'I', "ThIs Is a very nIce sentence")]
        // Three leading 'a's before a match (copyLength > 0), no remainders through non-vectorised path
        [InlineData("aaaAaaaaaaaBAbbbbbbCcccccccDddAdddd", 'A', 'a', "aaaaaaaaaaaBabbbbbbCcccccccDddadddd")]
        // Three leading 'a's before a match (copyLength > 0), 3 remainders through non-vectorised path
        [InlineData("aaaAaaaaaaaBAbbbbbbCcccccccDddAdddddAd", 'A', 'a', "aaaaaaaaaaaBabbbbbbCcccccccDddadddddad")]
        // Match from the beginning (copyLength == 0), no remainders through non-vectorised path
        [InlineData("AaaaaaaaBAbbbbbbCcccccccDddAdddd", 'A', 'a', "aaaaaaaaBabbbbbbCcccccccDddadddd")]
        // Match from the beginning (copyLength == 0), 3 remainders through non-vectorised path
        [InlineData("AaaaaaaaBAbbbbbbCcccccccDddAdddddAd", 'A', 'a', "aaaaaaaaBabbbbbbCcccccccDddadddddad")]
        // An arbitrary long string (263 chars long, Three matches before a match, 4 remainders through non-vectorisd path)
        [InlineData("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Proin maximus convallis luctus. Curabitur porttitor mi blandit tellus maximus varius. " +
                    "Mauris nulla sapien, convallis et quam quis, accumsan sodales mi. Praesent dapibus urna fermentum, sollicitudin posuere.",
                    'e', 'E',
                    "LorEm ipsum dolor sit amEt, consEctEtur adipiscing Elit. Proin maximus convallis luctus. Curabitur porttitor mi blandit tEllus maximus varius. " +
                    "Mauris nulla sapiEn, convallis Et quam quis, accumsan sodalEs mi. PraEsEnt dapibus urna fErmEntum, sollicitudin posuErE.")]
        public static void Replace_Char_Char(string s, char oldChar, char newChar, string expected)
        {
            Assert.Equal(expected, s.Replace(oldChar, newChar));
        }

        [Theory]
        [InlineData("XYZ", '1', '2')]
        [InlineData("", '1', '2')]
        [InlineData("XYZ", 'a', 'a')]
        public static void Replace_Char_Char_DoesntAllocateIfNothingIsReplaced(string s, char oldChar, char newChar)
        {
            Assert.Same(s, s.Replace(oldChar, newChar));
        }

        [Theory]
        [InlineData("", "1", "2", "")]
        [InlineData("Hello", "ll", "!!!!", "He!!!!o")]
        [InlineData("Hello", "l", "", "Heo")]
        [InlineData("Hello", "l", null, "Heo")]
        [InlineData("11111", "1", "23", "2323232323")]
        [InlineData("111111", "111", "23", "2323")]
        [InlineData("1111111", "111", "23", "23231")]
        [InlineData("11111111", "111", "23", "232311")]
        [InlineData("111111111", "111", "23", "232323")]
        [InlineData("A1B1C1D1E1F", "1", "23", "A23B23C23D23E23F")]
        [InlineData("abcdefghijkl", "cdef", "12345", "ab12345ghijkl")]
        [InlineData("Aa1Bbb1Cccc1Ddddd1Eeeeee1Fffffff", "1", "23", "Aa23Bbb23Cccc23Ddddd23Eeeeee23Fffffff")]
        [InlineData("11111111111111111111111", "1", "11", "1111111111111111111111111111111111111111111111")] //  Checks if we handle the max # of matches
        [InlineData("11111111111111111111111", "1", "", "")] // Checks if we handle the max # of matches
        public static void Replace_String_String(string s, string oldValue, string newValue, string expected)
        {
            Assert.Equal(expected, s.Replace(oldValue, newValue));
        }

        [Theory]
        [InlineData("XYZ", "1", "2")]
        [InlineData("", "1", "2")]
        public static void Replace_String_String_DoesntAllocateIfNothingIsReplaced(string s, string oldValue, string newValue)
        {
            Assert.Same(s, s.Replace(oldValue, newValue));
        }

        [Fact]
        public void Replace_NullOldValue_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("oldValue", () => "Hello".Replace(null, ""));
        }

        [Fact]
        public void Replace_EmptyOldValue_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("oldValue", () => "Hello".Replace("", "l"));
        }

        public static IEnumerable<object[]> StartsWith_StringComparison_TestData()
        {
            // CurrentCulture
            yield return new object[] { "Hello", "Hel", StringComparison.CurrentCulture, true };
            yield return new object[] { "Hello", "Hello", StringComparison.CurrentCulture, true };
            yield return new object[] { "Hello", "", StringComparison.CurrentCulture, true };
            yield return new object[] { "Hello", "HELLO", StringComparison.CurrentCulture, false };
            yield return new object[] { "Hello", "Abc", StringComparison.CurrentCulture, false };
            yield return new object[] { "", "", StringComparison.CurrentCulture, true };
            yield return new object[] { "", "hello", StringComparison.CurrentCulture, false };

            if (PlatformDetection.IsNotInvariantGlobalization)
                yield return new object[] { "Hello", SoftHyphen + "Hel", StringComparison.CurrentCulture, true };

            // CurrentCultureIgnoreCase
            yield return new object[] { "Hello", "Hel", StringComparison.CurrentCultureIgnoreCase, true };
            yield return new object[] { "Hello", "Hello", StringComparison.CurrentCultureIgnoreCase, true };
            yield return new object[] { "Hello", "", StringComparison.CurrentCultureIgnoreCase, true };
            yield return new object[] { "Hello", "HEL", StringComparison.CurrentCultureIgnoreCase, true };
            yield return new object[] { "Hello", "Abc", StringComparison.CurrentCultureIgnoreCase, false };
            yield return new object[] { "", "", StringComparison.CurrentCultureIgnoreCase, true };
            yield return new object[] { "", "hello", StringComparison.CurrentCultureIgnoreCase, false };

            if (PlatformDetection.IsNotInvariantGlobalization)
                yield return new object[] { "Hello", SoftHyphen + "Hel", StringComparison.CurrentCultureIgnoreCase, true };

            // InvariantCulture
            yield return new object[] { "Hello", "Hel", StringComparison.InvariantCulture, true };
            yield return new object[] { "Hello", "Hello", StringComparison.InvariantCulture, true };
            yield return new object[] { "Hello", "", StringComparison.InvariantCulture, true };
            yield return new object[] { "Hello", "HELLO", StringComparison.InvariantCulture, false };
            yield return new object[] { "Hello", "Abc", StringComparison.InvariantCulture, false };
            yield return new object[] { "", "", StringComparison.InvariantCulture, true };
            yield return new object[] { "", "hello", StringComparison.InvariantCulture, false };

            if (PlatformDetection.IsNotInvariantGlobalization)
                yield return new object[] { "Hello", SoftHyphen + "Hel", StringComparison.InvariantCulture, true };

            // InvariantCultureIgnoreCase
            yield return new object[] { "Hello", "Hel", StringComparison.InvariantCultureIgnoreCase, true };
            yield return new object[] { "Hello", "Hello", StringComparison.InvariantCultureIgnoreCase, true };
            yield return new object[] { "Hello", "", StringComparison.InvariantCultureIgnoreCase, true };
            yield return new object[] { "Hello", "HEL", StringComparison.InvariantCultureIgnoreCase, true };
            yield return new object[] { "Hello", "Abc", StringComparison.InvariantCultureIgnoreCase, false };
            yield return new object[] { "", "", StringComparison.InvariantCultureIgnoreCase, true };
            yield return new object[] { "", "hello", StringComparison.InvariantCultureIgnoreCase, false };

            if (PlatformDetection.IsNotInvariantGlobalization)
                yield return new object[] { "Hello", SoftHyphen + "Hel", StringComparison.InvariantCultureIgnoreCase, true };

            // Ordinal
            yield return new object[] { "Hello", "H", StringComparison.Ordinal, true };
            yield return new object[] { "Hello", "Hel", StringComparison.Ordinal, true };
            yield return new object[] { "Hello", "Hello", StringComparison.Ordinal, true };
            yield return new object[] { "Hello", "Hello Larger", StringComparison.Ordinal, false };
            yield return new object[] { "Hello", "", StringComparison.Ordinal, true };
            yield return new object[] { "Hello", "HEL", StringComparison.Ordinal, false };
            yield return new object[] { "Hello", "Abc", StringComparison.Ordinal, false };
            yield return new object[] { "", "", StringComparison.Ordinal, true };
            yield return new object[] { "", "hello", StringComparison.Ordinal, false };
            yield return new object[] { "abcdefghijklmnopqrstuvwxyz", "abcdefghijklmnopqrstuvwxyz", StringComparison.Ordinal, true };
            yield return new object[] { "abcdefghijklmnopqrstuvwxyz", "abcdefghijklmnopqrstuvwx", StringComparison.Ordinal, true };
            yield return new object[] { "abcdefghijklmnopqrstuvwxyz", "abcdefghijklm", StringComparison.Ordinal, true };
            yield return new object[] { "abcdefghijklmnopqrstuvwxyz", "ab_defghijklmnopqrstu", StringComparison.Ordinal, false };
            yield return new object[] { "abcdefghijklmnopqrstuvwxyz", "abcdef_hijklmn", StringComparison.Ordinal, false };
            yield return new object[] { "abcdefghijklmnopqrstuvwxyz", "abcdefghij_lmn", StringComparison.Ordinal, false };
            yield return new object[] { "abcdefghijklmnopqrstuvwxyz", "a", StringComparison.Ordinal, true };
            yield return new object[] { "abcdefghijklmnopqrstuvwxyz", "abcdefghijklmnopqrstuvwxyza", StringComparison.Ordinal, false };

            // OrdinalIgnoreCase
            yield return new object[] { "Hello", "Hel", StringComparison.OrdinalIgnoreCase, true };
            yield return new object[] { "Hello", "Hello", StringComparison.OrdinalIgnoreCase, true };
            yield return new object[] { "Hello", "Hello Larger", StringComparison.OrdinalIgnoreCase, false };
            yield return new object[] { "Hello", "", StringComparison.OrdinalIgnoreCase, true };
            yield return new object[] { "Hello", "HEL", StringComparison.OrdinalIgnoreCase, true };
            yield return new object[] { "Hello", "Abc", StringComparison.OrdinalIgnoreCase, false };
            yield return new object[] { "Hello", SoftHyphen + "Hel", StringComparison.OrdinalIgnoreCase, false };
            yield return new object[] { "", "", StringComparison.OrdinalIgnoreCase, true };
            yield return new object[] { "", "hello", StringComparison.OrdinalIgnoreCase, false };
        }

        [Theory]
        [MemberData(nameof(StartsWith_StringComparison_TestData))]
        public static void StartsWith_StringComparison(string s, string value, StringComparison comparisonType, bool expected)
        {
            if (comparisonType == StringComparison.CurrentCulture)
            {
                Assert.Equal(expected, s.StartsWith(value));
            }
            Assert.Equal(expected, s.StartsWith(value, comparisonType));

            Assert.Equal(expected, s.AsSpan().StartsWith(value.AsSpan(), comparisonType));
        }

        // NOTE: This is by design. ICU ignores the null characters (i.e. null characters have no weights for the string comparison).
        // For desired behavior, use ordinal comparison instead of linguistic comparison.
        // This is a known difference between NLS and ICU (https://github.com/dotnet/runtime/issues/4673).
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNlsGlobalization))]
        [InlineData(StringComparison.CurrentCulture)]
        [InlineData(StringComparison.CurrentCultureIgnoreCase)]
        [InlineData(StringComparison.Ordinal)]
        [InlineData(StringComparison.OrdinalIgnoreCase)]
        public static void StartsWith_NullInStrings(StringComparison comparison)
        {
            Assert.False("\0test".StartsWith("test", comparison));
            Assert.False("te\0st".StartsWith("test", comparison));
            Assert.True("te\0st".StartsWith("te\0s", comparison));
            Assert.True("test\0".StartsWith("test", comparison));
            Assert.False("test".StartsWith("te\0", comparison));

            Assert.False("\0test".AsSpan().StartsWith("test".AsSpan(), comparison));
            Assert.False("te\0st".AsSpan().StartsWith("test".AsSpan(), comparison));
            Assert.True("te\0st".AsSpan().StartsWith("te\0s".AsSpan(), comparison));
            Assert.True("test\0".AsSpan().StartsWith("test".AsSpan(), comparison));
            Assert.False("test".AsSpan().StartsWith("te\0".AsSpan(), comparison));
        }

        [Fact]
        public static void StartsWith_Invalid()
        {
            string s = "Hello";

            // Value is null
            AssertExtensions.Throws<ArgumentNullException>("value", () => s.StartsWith(null));
            AssertExtensions.Throws<ArgumentNullException>("value", () => s.StartsWith(null, StringComparison.CurrentCultureIgnoreCase));

            AssertExtensions.Throws<ArgumentNullException>("value", () => s.StartsWith(null, StringComparison.Ordinal));
            AssertExtensions.Throws<ArgumentNullException>("value", () => s.StartsWith(null, StringComparison.OrdinalIgnoreCase));

            // Invalid comparison type
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => s.StartsWith("H", StringComparison.CurrentCulture - 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => s.AsSpan().StartsWith("H".AsSpan(), StringComparison.CurrentCulture - 1));

            AssertExtensions.Throws<ArgumentException>("comparisonType", () => s.StartsWith("H", StringComparison.OrdinalIgnoreCase + 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => s.AsSpan().StartsWith("H".AsSpan(), StringComparison.OrdinalIgnoreCase + 1));
        }

        [Fact]
        public static void ZeroLengthStartsWith_Char()
        {
            var a = new char[3];

            string s1 = new string(a);
            string s2 = new string(a, 2, 0);
            bool b = s1.StartsWith(s2);
            Assert.True(b);

            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> slice = s2.AsSpan();
            b = span.StartsWith<char>(slice);
            Assert.True(b);
        }

        [Fact]
        public static void SameSpanStartsWith_Char()
        {
            string s1 = "456";
            bool b = s1.StartsWith(s1);
            Assert.True(b);

            ReadOnlySpan<char> span = s1.AsSpan();
            b = span.StartsWith<char>(span);
            Assert.True(b);
        }

        [Fact]
        public static void LengthMismatchStartsWith_Char()
        {
            char[] a = { '4', '5', '6' };

            string s1 = new string(a, 0, 2);
            string s2 = new string(a, 0, 3);
            bool b = s1.StartsWith(s2);
            Assert.False(b);

            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> slice = s2.AsSpan();
            b = span.StartsWith<char>(slice);
            Assert.False(b);
        }

        [Fact]
        public static void StartsWithMatch_Char()
        {
            char[] a = { '4', '5', '6' };

            string s1 = new string(a, 0, 3);
            string s2 = new string(a, 0, 2);
            bool b = s1.StartsWith(s2);
            Assert.True(b);

            var span = new ReadOnlySpan<char>(a, 0, 3);
            var slice = new ReadOnlySpan<char>(a, 0, 2);
            b = span.StartsWith<char>(slice);
            Assert.True(b);
        }

        [Fact]
        public static void StartsWithMatchDifferentSpans_Char()
        {
            char[] a = { '4', '5', '6' };
            char[] b = { '4', '5', '6' };

            string s1 = "456";
            string s2 = "456";
            bool c = s1.StartsWith(s2);
            Assert.True(c);

            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> slice = s2.AsSpan();
            c = span.StartsWith<char>(slice);
            Assert.True(c);

            span = new ReadOnlySpan<char>(a);
            slice = new ReadOnlySpan<char>(b);
            c = span.StartsWith<char>(slice);
            Assert.True(c);
        }

        [Fact]
        public static void StartsWithNoMatch_Char()
        {
            for (int length = 1; length < 32; length++)
            {
                for (int mismatchIndex = 0; mismatchIndex < length; mismatchIndex++)
                {
                    var first = new char[length];
                    var second = new char[length];
                    for (int i = 0; i < length; i++)
                    {
                        first[i] = second[i] = (char)(i + 1);
                    }

                    second[mismatchIndex] = (char)(second[mismatchIndex] + 1);

                    string s1 = new string(first);
                    string s2 = new string(second);
                    bool b = s1.StartsWith(s2, StringComparison.Ordinal);
                    Assert.False(b);

                    var firstSpan = s1.AsSpan();
                    var secondSpan = s2.AsSpan();
                    b = firstSpan.StartsWith<char>(secondSpan);
                    Assert.False(b);
                }
            }
        }

        [Fact]
        public static void MakeSureNoStartsWithChecksGoOutOfRange_Char()
        {
            for (int length = 0; length < 100; length++)
            {
                var first = new char[length + 2];
                first[0] = '9';
                first[length + 1] = '9';
                var second = new char[length + 2];
                second[0] = 'a';
                second[length + 1] = 'a';

                string s1 = new string(first, 1, length);
                string s2 = new string(second, 1, length);
                bool b = s1.StartsWith(s2);
                Assert.True(b);

                ReadOnlySpan<char> span1 = s1.AsSpan();
                ReadOnlySpan<char> span2 = s2.AsSpan();
                b = span1.StartsWith<char>(span2);
                Assert.True(b);
            }
        }

        [Fact]
        public static void ZeroLengthStartsWith_StringComparison()
        {
            var a = new char[3];

            string s1 = new string(a);
            string s2 = new string(a, 2, 0);
            Assert.True(s1.StartsWith(s2, StringComparison.Ordinal));

            Assert.True(s1.StartsWith(s2, StringComparison.CurrentCulture));
            Assert.True(s1.StartsWith(s2, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(s1.StartsWith(s2, StringComparison.InvariantCulture));
            Assert.True(s1.StartsWith(s2, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(s1.StartsWith(s2, StringComparison.OrdinalIgnoreCase));

            s1 = string.Empty;
            Assert.True(s1.StartsWith(s2, StringComparison.Ordinal));

            Assert.True(s1.StartsWith(s2, StringComparison.CurrentCulture));
            Assert.True(s1.StartsWith(s2, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(s1.StartsWith(s2, StringComparison.InvariantCulture));
            Assert.True(s1.StartsWith(s2, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(s1.StartsWith(s2, StringComparison.OrdinalIgnoreCase));

            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> slice = s2.AsSpan();
            Assert.True(span.StartsWith(slice, StringComparison.Ordinal));

            Assert.True(span.StartsWith(slice, StringComparison.CurrentCulture));
            Assert.True(span.StartsWith(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(span.StartsWith(slice, StringComparison.InvariantCulture));
            Assert.True(span.StartsWith(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(span.StartsWith(slice, StringComparison.OrdinalIgnoreCase));

            span = ReadOnlySpan<char>.Empty;
            Assert.True(span.StartsWith(slice, StringComparison.Ordinal));

            Assert.True(span.StartsWith(slice, StringComparison.CurrentCulture));
            Assert.True(span.StartsWith(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(span.StartsWith(slice, StringComparison.InvariantCulture));
            Assert.True(span.StartsWith(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(span.StartsWith(slice, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void SameSpanStartsWith_StringComparison()
        {
            string s1 = "456";
            Assert.True(s1.StartsWith(s1, StringComparison.Ordinal));

            Assert.True(s1.StartsWith(s1, StringComparison.CurrentCulture));
            Assert.True(s1.StartsWith(s1, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(s1.StartsWith(s1, StringComparison.InvariantCulture));
            Assert.True(s1.StartsWith(s1, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(s1.StartsWith(s1, StringComparison.OrdinalIgnoreCase));

            ReadOnlySpan<char> span = s1.AsSpan();
            Assert.True(span.StartsWith(span, StringComparison.Ordinal));

            Assert.True(span.StartsWith(span, StringComparison.CurrentCulture));
            Assert.True(span.StartsWith(span, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(span.StartsWith(span, StringComparison.InvariantCulture));
            Assert.True(span.StartsWith(span, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(span.StartsWith(span, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void LengthMismatchStartsWith_StringComparison()
        {
            string value = "456";

            string s1 = value.Substring(0, 2);
            string s2 = value.Substring(0, 3);
            Assert.False(s1.StartsWith(s2, StringComparison.Ordinal));

            Assert.False(s1.StartsWith(s2, StringComparison.CurrentCulture));
            Assert.False(s1.StartsWith(s2, StringComparison.CurrentCultureIgnoreCase));
            Assert.False(s1.StartsWith(s2, StringComparison.InvariantCulture));
            Assert.False(s1.StartsWith(s2, StringComparison.InvariantCultureIgnoreCase));
            Assert.False(s1.StartsWith(s2, StringComparison.OrdinalIgnoreCase));

            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> slice = s2.AsSpan();
            Assert.False(span.StartsWith(slice, StringComparison.Ordinal));

            Assert.False(span.StartsWith(slice, StringComparison.CurrentCulture));
            Assert.False(span.StartsWith(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.False(span.StartsWith(slice, StringComparison.InvariantCulture));
            Assert.False(span.StartsWith(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.False(span.StartsWith(slice, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void StartsWithMatch_StringComparison()
        {
            string value = "456";

            string s1 = value.Substring(0, 3);
            string s2 = value.Substring(0, 2);
            Assert.True(s1.StartsWith(s2, StringComparison.Ordinal));

            Assert.True(s1.StartsWith(s2, StringComparison.CurrentCulture));
            Assert.True(s1.StartsWith(s2, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(s1.StartsWith(s2, StringComparison.InvariantCulture));
            Assert.True(s1.StartsWith(s2, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(s1.StartsWith(s2, StringComparison.OrdinalIgnoreCase));

            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> slice = s2.AsSpan();
            Assert.True(span.StartsWith(slice, StringComparison.Ordinal));

            Assert.True(span.StartsWith(slice, StringComparison.CurrentCulture));
            Assert.True(span.StartsWith(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(span.StartsWith(slice, StringComparison.InvariantCulture));
            Assert.True(span.StartsWith(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(span.StartsWith(slice, StringComparison.OrdinalIgnoreCase));
        }

        [Theory]
        [InlineData("Hello", 0, 5, "Hello")]
        [InlineData("Hello", 0, 3, "Hel")]
        [InlineData("Hello", 2, 3, "llo")]
        [InlineData("Hello", 5, 0, "")]
        [InlineData("", 0, 0, "")]
        public static void Substring(string s, int startIndex, int length, string expected)
        {
            if (startIndex + length == s.Length)
            {
                Assert.Equal(expected, s.Substring(startIndex));
                Assert.Equal(expected, s.AsSpan(startIndex).ToString());
            }
            Assert.Equal(expected, s.Substring(startIndex, length));

            Assert.Equal(expected, s.AsSpan(startIndex, length).ToString());
        }

        [Fact]
        public static void Substring_Invalid()
        {
            // Start index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => "foo".Substring(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => "foo".Substring(-1, 0));

            // Start index > string.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => "foo".Substring(4));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => "foo".Substring(4, 0));

            // Length < 0 or length > string.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => "foo".Substring(0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => "foo".Substring(0, 4));

            // Start index + length > string.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => "foo".Substring(3, 2));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => "foo".Substring(2, 2));
        }

        [Theory]
        [InlineData("Hello", 0, 5, new char[] { 'H', 'e', 'l', 'l', 'o' })]
        [InlineData("Hello", 2, 3, new char[] { 'l', 'l', 'o' })]
        [InlineData("Hello", 5, 0, new char[0])]
        [InlineData("", 0, 0, new char[0])]
        public static void ToCharArray(string s, int startIndex, int length, char[] expected)
        {
            if (startIndex == 0 && length == s.Length)
            {
                Assert.Equal(expected, s.ToCharArray());
            }
            Assert.Equal(expected, s.ToCharArray(startIndex, length));
        }

        [Fact]
        public static void ToCharArray_Invalid()
        {
            // StartIndex < 0 or startIndex > string.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => "foo".ToCharArray(-1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => "foo".ToCharArray(4, 0)); // Start index > string.Length

            // Length < 0 or length > string.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => "foo".ToCharArray(0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => "foo".ToCharArray(0, 4));

            // StartIndex + length > string.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => "foo".ToCharArray(3, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => "foo".ToCharArray(2, 2));
        }

        [Theory]
        [InlineData("hello", "hello")]
        [InlineData("HELLO", "hello")]
        [InlineData("hElLo", "hello")]
        [InlineData("HeLlO", "hello")]
        [InlineData("", "")]
        public static void ToLower_String(string s, string expected)
        {
            Assert.Equal(expected, s.ToLower());

            Span<char> destination = new char[s.Length];
            Assert.Equal(s.Length, s.AsSpan().ToLower(destination, CultureInfo.CurrentCulture));
            Assert.Equal(expected, destination.ToString());
        }

        private static IEnumerable<object[]> ToLower_Culture_TestData()
        {
            List<Tuple<char, char, CultureInfo>> tuples = new List<Tuple<char, char, CultureInfo>>();

            // Android has different results w/ tr-TR
            // See https://github.com/dotnet/runtime/issues/60568
            if (!PlatformDetection.IsAndroid && !PlatformDetection.IsLinuxBionic)
            {
                tuples.Add(Tuple.Create('\u0049', '\u0131', new CultureInfo("tr-TR")));
                tuples.Add(Tuple.Create('\u0130', '\u0069', new CultureInfo("tr-TR")));
                tuples.Add(Tuple.Create('\u0131', '\u0131', new CultureInfo("tr-TR")));
            }

            tuples.Add(Tuple.Create('\u0049', '\u0069', new CultureInfo("en-US")));
            tuples.Add(Tuple.Create('\u0130', '\u0069', new CultureInfo("en-US")));
            tuples.Add(Tuple.Create('\u0131', '\u0131', new CultureInfo("en-US")));

            tuples.Add(Tuple.Create('\u0049', '\u0069', CultureInfo.InvariantCulture));
            tuples.Add(Tuple.Create('\u0130', '\u0130', CultureInfo.InvariantCulture));
            tuples.Add(Tuple.Create('\u0131', '\u0131', CultureInfo.InvariantCulture));

            foreach (Tuple<char, char, CultureInfo> tuple in tuples)
            {
                yield return new object[] { $"{tuple.Item1}Hello World", $"{tuple.Item2}hello world", tuple.Item3 };
                yield return new object[] { $"HeLlO{tuple.Item1} WORLD", $"hello{tuple.Item2} world", tuple.Item3 };
                yield return new object[] { $"hello world{tuple.Item1}", $"hello world{tuple.Item2}", tuple.Item3 };
                yield return new object[] { new string(tuple.Item1, 100), new string(tuple.Item2, 100), tuple.Item3 };
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        public static void Test_ToLower_Culture()
        {
            foreach (object[] testdata in ToLower_Culture_TestData())
            {
                ToLower_Culture((string)testdata[0], (string)testdata[1], (CultureInfo)testdata[2]);
            }
        }

        private static void ToLower_Culture(string input, string expected, CultureInfo culture)
        {
            using (new ThreadCultureChange(culture))
            {
                Assert.True(input.ToLower().Equals(expected, StringComparison.Ordinal), $"Input: {input}, Expected: {expected}, Actual: {input.ToLower()}");

                Span<char> destination = new char[input.Length];
                Assert.Equal(input.Length, input.AsSpan().ToLower(destination, culture));
                Assert.Equal(expected, destination.ToString());
            }
        }

        [Theory]
        [InlineData("hello", "hello")]
        [InlineData("HELLO", "hello")]
        [InlineData("hElLo", "hello")]
        [InlineData("HeLlO", "hello")]
        [InlineData("", "")]
        public static void ToLowerInvariant(string s, string expected)
        {
            Assert.Equal(expected, s.ToLowerInvariant());

            Span<char> destination = new char[s.Length];
            Assert.Equal(s.Length, s.AsSpan().ToLowerInvariant(destination));
            Assert.Equal(expected, destination.ToString());
        }

        [Fact]
        public static void ZeroLengthToLower()
        {
            char[] expectedSource = { 'a', 'B', 'c' };
            char[] a = { 'a', 'B', 'c' };
            var expectedDestination = new char[1] { 'a' };
            Span<char> destination = new char[1] { 'a' };

            string emptyString = new string(a, 2, 0);
            Assert.Equal("", emptyString.ToLowerInvariant());

            ReadOnlySpan<char> source = emptyString.AsSpan();
            Assert.Equal(source.Length, source.ToLower(destination, CultureInfo.CurrentCulture));
            Assert.Equal(source.Length, source.ToLowerInvariant(destination));
            Assert.Equal(expectedDestination, destination.ToArray());
            Assert.Equal(expectedSource, a);

            emptyString = string.Empty;
            Assert.Equal("", emptyString.ToLowerInvariant());

            source = emptyString.AsSpan();
            Assert.Equal(source.Length, source.ToLower(destination, CultureInfo.CurrentCulture));
            Assert.Equal(source.Length, source.ToLowerInvariant(destination));
            Assert.Equal(expectedDestination, destination.ToArray());
            Assert.Equal(expectedSource, a);
        }

        [Fact]
        public static void SameSpanToLower()
        {
            var expected = new char[3] { 'a', 'b', 'c' };
            var a = new char[3] { 'a', 'B', 'c' };
            string s1 = new string(a);
            Assert.Equal(expected, s1.ToLower(CultureInfo.CurrentCulture).ToArray());
            Assert.Equal(expected, s1.ToLowerInvariant().ToArray());
            {
                AssertExtensions.AssertThrows<InvalidOperationException, char>(a, a =>
                {
                    ReadOnlySpan<char> source = a;
                    Span<char> destination = a;

                    source.ToLower(destination, CultureInfo.CurrentCulture);
                });
            }
            {
                AssertExtensions.AssertThrows<InvalidOperationException, char>(a, a =>
                {
                    ReadOnlySpan<char> source = a;
                    Span<char> destination = a;

                    source.ToLowerInvariant(destination);
                });
            }
        }

        [Fact]
        public static void ToLowerOverlapping()
        {
            var expectedDestination = new char[3] { 'b', 'c', 'b' };

            {
                char[] a = { 'a', 'B', 'c', 'B', 'c', 'B' };

                string s1 = new string(a, 1, 3);
                Assert.Equal(expectedDestination, s1.ToLower(CultureInfo.CurrentCulture).ToArray());

                var source = new ReadOnlySpan<char>(a, 1, 3);

                AssertExtensions.AssertThrows<InvalidOperationException,char>(source, source =>
                {
                    var destination = new Span<char>(a, 3, 3);
                    source.ToLower(destination, CultureInfo.CurrentCulture);
                });
            }
            {
                char[] a = { 'a', 'B', 'c', 'B', 'c', 'B' };

                string s1 = new string(a, 1, 3);
                Assert.Equal(expectedDestination, s1.ToLowerInvariant().ToArray());

                var source = new ReadOnlySpan<char>(a, 1, 3);

                AssertExtensions.AssertThrows<InvalidOperationException, char>(source, source =>
                {
                    var destination = new Span<char>(a, 3, 3);
                    source.ToLowerInvariant(destination);
                });
            }
        }

        [Fact]
        public static void LengthMismatchToLower()
        {
            {
                var expectedSource = new char[3] { 'a', 'B', 'c' };

                string s1 = "aBc";
                var expectedDestinationString = "abc";
                Assert.Equal(expectedDestinationString, s1.ToLowerInvariant());

                ReadOnlySpan<char> source = s1.AsSpan();

                var expectedDestination = new char[1] { 'a' };
                Span<char> destination = new char[1] { 'a' };

                Assert.Equal(-1, source.ToLower(destination, CultureInfo.CurrentCulture));
                Assert.Equal(-1, source.ToLowerInvariant(destination));

                Assert.Equal(expectedDestination, destination.ToArray());
                Assert.Equal(expectedSource, source.ToArray());
            }

            {
                var expectedSource = new char[3] { 'a', 'B', 'c' };

                string s1 = "aBc";
                var expectedDestinationString = "abc";
                Assert.Equal(expectedDestinationString, s1.ToLowerInvariant());

                ReadOnlySpan<char> source = s1.AsSpan();

                var expectedDestination = new char[4] { 'a', 'b', 'c', 'D' };
                Span<char> destination = new char[4] { 'x', 'Y', 'z', 'D' };

                Assert.Equal(source.Length, source.ToLower(destination, CultureInfo.CurrentCulture));
                Assert.Equal(source.Length, source.ToLowerInvariant(destination));

                Assert.Equal(expectedDestination, destination.ToArray());
                Assert.Equal(expectedSource, source.ToArray());
            }
        }

        [Fact]
        public static void ToLower_CharArray()
        {
            var expectedSource = new char[3] { 'a', 'B', 'c' };
            var expectedDestination = new char[3] { 'a', 'b', 'c' };

            {
                string s1 = "aBc";
                Assert.Equal(expectedDestination, s1.ToLower(CultureInfo.CurrentCulture).ToCharArray());

                ReadOnlySpan<char> source = s1.AsSpan();
                Span<char> destination = new char[3] { 'x', 'Y', 'z' };

                Assert.Equal(source.Length, source.ToLower(destination, CultureInfo.CurrentCulture));
                Assert.Equal(expectedDestination, destination.ToArray());
                Assert.Equal(expectedSource, source.ToArray());
            }

            {
                string s1 = "aBc";
                Assert.Equal(expectedDestination, s1.ToLowerInvariant().ToCharArray());

                ReadOnlySpan<char> source = s1.AsSpan();
                Span<char> destination = new char[3] { 'x', 'Y', 'z' };

                Assert.Equal(source.Length, source.ToLowerInvariant(destination));
                Assert.Equal(expectedDestination, destination.ToArray());
                Assert.Equal(expectedSource, source.ToArray());
            }
        }

        [Fact]
        public static void MakeSureNoToLowerChecksGoOutOfRange()
        {
            for (int length = 0; length < 100; length++)
            {
                var first = new char[length + 2];
                var second = new char[length + 2];

                for (int i = 0; i < first.Length; i++)
                {
                    first[i] = 'A';
                    second[i] = 'B';
                }

                first[0] = 'Z';
                first[length + 1] = 'Z';

                second[0] = 'Y';
                second[length + 1] = 'Y';

                var expectedSource = new char[length];
                var expectedDestination = new char[length];
                for (int i = 0; i < length; i++)
                {
                    expectedSource[i] = 'A';
                    expectedDestination[i] = 'a';
                }

                string s1 = new string(first, 1, length);
                Assert.Equal(expectedDestination, s1.ToLowerInvariant().ToCharArray());

                Assert.Equal('Z', first[0]);
                Assert.Equal('Z', first[length + 1]);
                Assert.Equal('Y', second[0]);
                Assert.Equal('Y', second[length + 1]);

                ReadOnlySpan<char> source = s1.AsSpan();
                var destination = new Span<char>(second, 1, length);
                Assert.Equal(source.Length, source.ToLower(destination, CultureInfo.CurrentCulture));
                Assert.Equal(source.Length, source.ToLowerInvariant(destination));
                Assert.Equal(expectedDestination, destination.ToArray());
                Assert.Equal(expectedSource, source.ToArray());

                Assert.Equal('Z', first[0]);
                Assert.Equal('Z', first[length + 1]);
                Assert.Equal('Y', second[0]);
                Assert.Equal('Y', second[length + 1]);
            }
        }

        [Fact]
        public static void ZeroLengthToUpper()
        {
            char[] expectedSource = { 'a', 'B', 'c' };
            char[] a = { 'a', 'B', 'c' };
            var expectedDestination = new char[1] { 'a' };
            Span<char> destination = new char[1] { 'a' };

            string emptyString = new string(a, 2, 0);
            Assert.Equal("", emptyString.ToUpperInvariant());

            ReadOnlySpan<char> source = emptyString.AsSpan();
            Assert.Equal(source.Length, source.ToUpper(destination, CultureInfo.CurrentCulture));
            Assert.Equal(source.Length, source.ToUpperInvariant(destination));
            Assert.Equal(expectedDestination, destination.ToArray());
            Assert.Equal(expectedSource, a);

            emptyString = string.Empty;
            Assert.Equal("", emptyString.ToUpperInvariant());

            source = emptyString.AsSpan();
            Assert.Equal(source.Length, source.ToUpper(destination, CultureInfo.CurrentCulture));
            Assert.Equal(source.Length, source.ToUpperInvariant(destination));
            Assert.Equal(expectedDestination, destination.ToArray());
            Assert.Equal(expectedSource, a);
        }

        [Fact]
        public static void SameSpanToUpper()
        {
            var expected = new char[3] { 'A', 'B', 'C' };
            var a = new char[3] { 'a', 'B', 'c' };
            string s1 = new string(a);
            Assert.Equal(expected, s1.ToUpper(CultureInfo.CurrentCulture).ToArray());
            Assert.Equal(expected, s1.ToUpperInvariant().ToArray());
            {
                AssertExtensions.AssertThrows<InvalidOperationException, char>(a, a =>
                {
                    ReadOnlySpan<char> source = a;
                    Span<char> destination = a;

                    source.ToUpper(destination, CultureInfo.CurrentCulture);
                });
            }
            {
                AssertExtensions.AssertThrows<InvalidOperationException, char>(a, a =>
                {
                    ReadOnlySpan<char> source = a;
                    Span<char> destination = a;

                    source.ToUpperInvariant(destination);
                });
            }
        }

        [Fact]
        public static void ToUpperOverlapping()
        {
            var expectedDestination = new char[3] { 'B', 'C', 'B' };

            {
                char[] a = { 'a', 'b', 'C', 'b', 'C', 'b' };

                string s1 = new string(a, 1, 3);
                Assert.Equal(expectedDestination, s1.ToUpper(CultureInfo.CurrentCulture).ToArray());

                var source = new ReadOnlySpan<char>(a, 1, 3);

                AssertExtensions.AssertThrows<InvalidOperationException, char>(source, source =>
                {
                    var destination = new Span<char>(a, 3, 3);
                    source.ToUpper(destination, CultureInfo.CurrentCulture);
                });
            }
            {
                char[] a = { 'a', 'b', 'C', 'b', 'C', 'b' };

                string s1 = new string(a, 1, 3);
                Assert.Equal(expectedDestination, s1.ToUpperInvariant().ToArray());

                var source = new ReadOnlySpan<char>(a, 1, 3);
                AssertExtensions.AssertThrows<InvalidOperationException, char>(source, source =>
                {
                    var destination = new Span<char>(a, 3, 3);
                    source.ToUpperInvariant(destination);
                });
            }
        }

        [Fact]
        public static void LengthMismatchToUpper()
        {
            {
                var expectedSource = new char[3] { 'a', 'B', 'c' };

                string s1 = "aBc";
                var expectedDestinationString = "ABC";
                Assert.Equal(expectedDestinationString, s1.ToUpperInvariant());

                ReadOnlySpan<char> source = s1.AsSpan();

                var expectedDestination = new char[1] { 'a' };
                Span<char> destination = new char[1] { 'a' };

                Assert.Equal(-1, source.ToUpper(destination, CultureInfo.CurrentCulture));
                Assert.Equal(-1, source.ToUpperInvariant(destination));

                Assert.Equal(expectedDestination, destination.ToArray());
                Assert.Equal(expectedSource, source.ToArray());
            }

            {
                var expectedSource = new char[3] { 'a', 'B', 'c' };

                string s1 = "aBc";
                var expectedDestinationString = "ABC";
                Assert.Equal(expectedDestinationString, s1.ToUpperInvariant());

                ReadOnlySpan<char> source = s1.AsSpan();

                var expectedDestination = new char[4] { 'A', 'B', 'C', 'd' };
                Span<char> destination = new char[4] { 'x', 'Y', 'z', 'd' };

                Assert.Equal(source.Length, source.ToUpper(destination, CultureInfo.CurrentCulture));
                Assert.Equal(source.Length, source.ToUpperInvariant(destination));

                Assert.Equal(expectedDestination, destination.ToArray());
                Assert.Equal(expectedSource, source.ToArray());
            }
        }

        [Fact]
        public static void ToUpper_CharArray()
        {
            var expectedSource = new char[3] { 'a', 'B', 'c' };
            var expectedDestination = new char[3] { 'A', 'B', 'C' };

            {
                string s1 = "aBc";
                Assert.Equal(expectedDestination, s1.ToUpper(CultureInfo.CurrentCulture).ToCharArray());

                ReadOnlySpan<char> source = s1.AsSpan();
                Span<char> destination = new char[3] { 'x', 'Y', 'z' };

                Assert.Equal(source.Length, source.ToUpper(destination, CultureInfo.CurrentCulture));
                Assert.Equal(expectedDestination, destination.ToArray());
                Assert.Equal(expectedSource, source.ToArray());
            }

            {
                string s1 = "aBc";
                Assert.Equal(expectedDestination, s1.ToUpperInvariant().ToCharArray());

                ReadOnlySpan<char> source = s1.AsSpan();
                Span<char> destination = new char[3] { 'x', 'Y', 'z' };

                Assert.Equal(source.Length, source.ToUpperInvariant(destination));
                Assert.Equal(expectedDestination, destination.ToArray());
                Assert.Equal(expectedSource, source.ToArray());
            }
        }

        [Fact]
        public static void MakeSureNoToUpperChecksGoOutOfRange()
        {
            for (int length = 0; length < 100; length++)
            {
                var first = new char[length + 2];
                var second = new char[length + 2];

                for (int i = 0; i < first.Length; i++)
                {
                    first[i] = 'a';
                    second[i] = 'b';
                }

                first[0] = 'Z';
                first[length + 1] = 'Z';

                second[0] = 'Y';
                second[length + 1] = 'Y';

                var expectedSource = new char[length];
                var expectedDestination = new char[length];
                for (int i = 0; i < length; i++)
                {
                    expectedSource[i] = 'a';
                    expectedDestination[i] = 'A';
                }

                string s1 = new string(first, 1, length);
                Assert.Equal(expectedDestination, s1.ToUpperInvariant().ToCharArray());

                Assert.Equal('Z', first[0]);
                Assert.Equal('Z', first[length + 1]);
                Assert.Equal('Y', second[0]);
                Assert.Equal('Y', second[length + 1]);

                ReadOnlySpan<char> source = s1.AsSpan();
                var destination = new Span<char>(second, 1, length);
                Assert.Equal(source.Length, source.ToUpper(destination, CultureInfo.CurrentCulture));
                Assert.Equal(source.Length, source.ToUpperInvariant(destination));
                Assert.Equal(expectedDestination, destination.ToArray());
                Assert.Equal(expectedSource, source.ToArray());

                Assert.Equal('Z', first[0]);
                Assert.Equal('Z', first[length + 1]);
                Assert.Equal('Y', second[0]);
                Assert.Equal('Y', second[length + 1]);
            }
        }

        public static IEnumerable<object[]> ToUpper_Culture_TestData()
        {
            // Android has different results w/ tr-TR
            // See https://github.com/dotnet/runtime/issues/60568
            if (!PlatformDetection.IsAndroid && !PlatformDetection.IsLinuxBionic)
            {
                yield return new object[] { "h\u0069 world", "H\u0130 WORLD", new CultureInfo("tr-TR") };
                yield return new object[] { "h\u0130 world", "H\u0130 WORLD", new CultureInfo("tr-TR") };
                yield return new object[] { "h\u0131 world", "H\u0049 WORLD", new CultureInfo("tr-TR") };
            }

            yield return new object[] { "h\u0069 world", "H\u0049 WORLD", new CultureInfo("en-US") };
            yield return new object[] { "h\u0130 world", "H\u0130 WORLD", new CultureInfo("en-US") };
            yield return new object[] { "h\u0131 world", "H\u0049 WORLD", new CultureInfo("en-US") };

            yield return new object[] { "h\u0069 world", "H\u0049 WORLD", CultureInfo.InvariantCulture };
            yield return new object[] { "h\u0130 world", "H\u0130 WORLD", CultureInfo.InvariantCulture };
            yield return new object[] { "h\u0131 world", "H\u0131 WORLD", CultureInfo.InvariantCulture };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        [MemberData(nameof(ToUpper_Culture_TestData))]
        public static void Test_ToUpper_Culture(string actual, string expected, CultureInfo culture)
        {
            Assert.Equal(expected, actual.ToUpper(culture));

            ReadOnlySpan<char> source = actual.AsSpan();
            Span<char> destination = new char[source.Length];
            Assert.Equal(source.Length, source.ToUpper(destination, culture));
            Assert.Equal(expected, destination.ToString());
        }

        [Theory]
        [InlineData("")]
        [InlineData("hello")]
        public static void ToStringTest(string s)
        {
            Assert.Same(s, s.ToString());
            Assert.Same(s, s.ToString(null));
            Assert.Same(s, s.ToString(CultureInfo.CurrentCulture));

            Assert.Equal(s, s.AsSpan().ToString());
        }

        [Theory]
        [InlineData("hello", "HELLO")]
        [InlineData("HELLO", "HELLO")]
        [InlineData("hElLo", "HELLO")]
        [InlineData("HeLlO", "HELLO")]
        [InlineData("", "")]
        public static void ToUpper_String(string s, string expected)
        {
            Assert.Equal(expected, s.ToUpper());

            Span<char> destination = new char[s.Length];
            Assert.Equal(s.Length, s.AsSpan().ToUpper(destination, CultureInfo.CurrentCulture));
            Assert.Equal(expected, destination.ToString());
        }

        private static IEnumerable<object[]> ToUpper_TurkishI_MemberData(
            params KeyValuePair<char, char>[] mappings)
        {
            foreach (KeyValuePair<char, char> mapping in mappings)
            {
                yield return new[] { $"{mapping.Key}", $"{mapping.Value}" };
                yield return new[] { $"{mapping.Key}a TeSt", $"{mapping.Value}A TEST" };
                yield return new[] { $"a T{mapping.Key}est", $"A T{mapping.Value}EST" };
                yield return new[] { $"A test{mapping.Key}", $"A TEST{mapping.Value}" };
                yield return new[] { new string(mapping.Key, 100), new string(mapping.Value, 100) };
            }
        }

        public static IEnumerable<object[]> ToUpper_TurkishI_TurkishCulture_MemberData() =>
            ToUpper_TurkishI_MemberData(
                new KeyValuePair<char, char>('\u0069', '\u0130'),
                new KeyValuePair<char, char>('\u0130', '\u0130'),
                new KeyValuePair<char, char>('\u0131', '\u0049'));

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        [MemberData(nameof(ToUpper_TurkishI_TurkishCulture_MemberData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/60568", TestPlatforms.Android | TestPlatforms.LinuxBionic)]
        public static void ToUpper_TurkishI_TurkishCulture(string s, string expected)
        {
            using (new ThreadCultureChange("tr-TR"))
            {
                Assert.True(s.ToUpper().Equals(expected, StringComparison.Ordinal), "Actual: " + s.ToUpper());

                Span<char> destination = new char[s.Length];
                Assert.Equal(s.Length, s.AsSpan().ToUpper(destination, CultureInfo.CurrentCulture));
                Assert.Equal(expected, destination.ToString());
            }
        }

        public static IEnumerable<object[]> ToUpper_TurkishI_EnglishUSCulture_MemberData() =>
            ToUpper_TurkishI_MemberData(
                new KeyValuePair<char, char>('\u0069', '\u0049'),
                new KeyValuePair<char, char>('\u0130', '\u0130'),
                new KeyValuePair<char, char>('\u0131', '\u0049'));

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        [MemberData(nameof(ToUpper_TurkishI_EnglishUSCulture_MemberData))]
        public static void ToUpper_TurkishI_EnglishUSCulture(string s, string expected)
        {
            using (new ThreadCultureChange("en-US"))
            {
                Assert.True(s.ToUpper().Equals(expected, StringComparison.Ordinal), "Actual: " + s.ToUpper());

                Span<char> destination = new char[s.Length];
                Assert.Equal(s.Length, s.AsSpan().ToUpper(destination, CultureInfo.CurrentCulture));
                Assert.Equal(expected, destination.ToString());
            }
        }

        public static IEnumerable<object[]> ToUpper_TurkishI_InvariantCulture_MemberData() =>
            ToUpper_TurkishI_MemberData(
                new KeyValuePair<char, char>('\u0069', '\u0049'),
                new KeyValuePair<char, char>('\u0130', '\u0130'),
                new KeyValuePair<char, char>('\u0131', '\u0131'));

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        [MemberData(nameof(ToUpper_TurkishI_InvariantCulture_MemberData))]
        public static void ToUpper_TurkishI_InvariantCulture(string s, string expected)
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                Assert.True(s.ToUpper().Equals(expected, StringComparison.Ordinal));

                Span<char> destination = new char[s.Length];
                Assert.Equal(s.Length, s.AsSpan().ToUpper(destination, CultureInfo.CurrentCulture));
                Assert.Equal(expected, destination.ToString());
            }
        }

        [Theory]
        [InlineData("hello", "HELLO")]
        [InlineData("HELLO", "HELLO")]
        [InlineData("hElLo", "HELLO")]
        [InlineData("HeLlO", "HELLO")]
        [InlineData("", "")]
        public static void ToUpperInvariant(string s, string expected)
        {
            Assert.Equal(expected, s.ToUpperInvariant());

            Span<char> destination = new char[s.Length];
            Assert.Equal(s.Length, s.AsSpan().ToUpperInvariant(destination));
            Assert.Equal(expected, destination.ToString());
        }

        [Fact]
        public static void ToLowerToUpperInvariant_ASCII()
        {
            var asciiChars = new char[128];
            var asciiCharsUpper = new char[128];
            var asciiCharsLower = new char[128];

            for (int i = 0; i < asciiChars.Length; i++)
            {
                char c = (char)i;
                asciiChars[i] = c;

                // Purposefully avoiding char.ToUpper/ToLower here so as not  to use the same thing we're testing.
                asciiCharsLower[i] = (c >= 'A' && c <= 'Z') ? (char)(c - 'A' + 'a') : c;
                asciiCharsUpper[i] = (c >= 'a' && c <= 'z') ? (char)(c - 'a' + 'A') : c;
            }

            var ascii = new string(asciiChars);
            var asciiLower = new string(asciiCharsLower);
            var asciiUpper = new string(asciiCharsUpper);

            Assert.Equal(asciiLower, ascii.ToLowerInvariant());
            Assert.Equal(asciiUpper, ascii.ToUpperInvariant());

            Span<char> destinationLower = new char[ascii.Length];
            Span<char> destinationUpper = new char[ascii.Length];

            Assert.Equal(ascii.Length, ascii.AsSpan().ToLowerInvariant(destinationLower));
            Assert.Equal(ascii.Length, ascii.AsSpan().ToUpperInvariant(destinationUpper));

            Assert.Equal(ascii.ToLowerInvariant(), destinationLower.ToString());
            Assert.Equal(ascii.ToUpperInvariant(), destinationUpper.ToString());

            Assert.Equal(ascii, ascii.AsSpan().ToString());
        }

        [Theory]
        [InlineData("  Hello  ", new char[] { ' ' }, "Hello")]
        [InlineData(".  Hello  ..", new char[] { '.' }, "  Hello  ")]
        [InlineData(".  Hello  ..", new char[] { '.', ' ' }, "Hello")]
        [InlineData("123abcHello123abc", new char[] { '1', '2', '3', 'a', 'b', 'c' }, "Hello")]
        [InlineData("  Hello  ", null, "Hello")]
        [InlineData("  Hello  ", new char[0], "Hello")]
        [InlineData("      \t      ", null, "")]
        [InlineData("", null, "")]
        [InlineData("      ", new char[] { ' ' }, "")]
        [InlineData("aaaaa", new char[] { 'a' }, "")]
        [InlineData("abaabaa", new char[] { 'b', 'a' }, "")]
        public static void Trim(string s, char[] trimChars, string expected)
        {
            if (trimChars == null || trimChars.Length == 0 || (trimChars.Length == 1 && trimChars[0] == ' '))
            {
                Assert.Equal(expected, s.Trim());
                Assert.Equal(expected, s.AsSpan().Trim().ToString());
            }

            if (trimChars?.Length == 1)
            {
                Assert.Equal(expected, s.Trim(trimChars[0]));
                Assert.Equal(expected, s.AsSpan().Trim(trimChars[0]).ToString());
            }

            Assert.Equal(expected, s.Trim(trimChars));
            Assert.Equal(expected, s.AsSpan().Trim(trimChars).ToString());
        }

        [Theory]
        [InlineData("  Hello  ", new char[] { ' ' }, "  Hello")]
        [InlineData(".  Hello  ..", new char[] { '.' }, ".  Hello  ")]
        [InlineData(".  Hello  ..", new char[] { '.', ' ' }, ".  Hello")]
        [InlineData("123abcHello123abc", new char[] { '1', '2', '3', 'a', 'b', 'c' }, "123abcHello")]
        [InlineData("  Hello  ", null, "  Hello")]
        [InlineData("  Hello  ", new char[0], "  Hello")]
        [InlineData("      \t      ", null, "")]
        [InlineData("", null, "")]
        [InlineData("      ", new char[] { ' ' }, "")]
        [InlineData("aaaaa", new char[] { 'a' }, "")]
        [InlineData("abaabaa", new char[] { 'b', 'a' }, "")]
        public static void TrimEnd(string s, char[] trimChars, string expected)
        {
            if (trimChars == null || trimChars.Length == 0 || (trimChars.Length == 1 && trimChars[0] == ' '))
            {
                Assert.Equal(expected, s.TrimEnd());
                Assert.Equal(expected, s.AsSpan().TrimEnd().ToString());
            }

            if (trimChars?.Length == 1)
            {
                Assert.Equal(expected, s.TrimEnd(trimChars[0]));
                Assert.Equal(expected, s.AsSpan().TrimEnd(trimChars[0]).ToString());
            }

            Assert.Equal(expected, s.TrimEnd(trimChars));
            Assert.Equal(expected, s.AsSpan().TrimEnd(trimChars).ToString());
        }

        [Theory]
        [InlineData("  Hello  ", new char[] { ' ' }, "Hello  ")]
        [InlineData(".  Hello  ..", new char[] { '.' }, "  Hello  ..")]
        [InlineData(".  Hello  ..", new char[] { '.', ' ' }, "Hello  ..")]
        [InlineData("123abcHello123abc", new char[] { '1', '2', '3', 'a', 'b', 'c' }, "Hello123abc")]
        [InlineData("  Hello  ", null, "Hello  ")]
        [InlineData("  Hello  ", new char[0], "Hello  ")]
        [InlineData("      \t      ", null, "")]
        [InlineData("", null, "")]
        [InlineData("      ", new char[] { ' ' }, "")]
        [InlineData("aaaaa", new char[] { 'a' }, "")]
        [InlineData("abaabaa", new char[] { 'b', 'a' }, "")]
        public static void TrimStart(string s, char[] trimChars, string expected)
        {
            if (trimChars == null || trimChars.Length == 0 || (trimChars.Length == 1 && trimChars[0] == ' '))
            {
                Assert.Equal(expected, s.TrimStart());
                Assert.Equal(expected, s.AsSpan().TrimStart().ToString());
            }

            if (trimChars?.Length == 1)
            {
                Assert.Equal(expected, s.TrimStart(trimChars[0]));
                Assert.Equal(expected, s.AsSpan().TrimStart(trimChars[0]).ToString());
            }

            Assert.Equal(expected, s.TrimStart(trimChars));
            Assert.Equal(expected, s.AsSpan().TrimStart(trimChars).ToString());
        }

        [Fact]
        public static void ZeroLengthTrimCharacter()
        {
            string s1 = string.Empty;
            Assert.True(s1.SequenceEqual(s1.Trim('a')));
            Assert.True(s1.SequenceEqual(s1.TrimStart('a')));
            Assert.True(s1.SequenceEqual(s1.TrimEnd('a')));

            ReadOnlySpan<char> span = s1.AsSpan();
            Assert.True(span.SequenceEqual(span.Trim('a')));
            Assert.True(span.SequenceEqual(span.TrimStart('a')));
            Assert.True(span.SequenceEqual(span.TrimEnd('a')));
        }

        [Fact]
        public static void NoTrimCharacter()
        {
            for (int length = 0; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'b';
                }

                string s1 = new string(a);
                Assert.True(s1.SequenceEqual(s1.Trim('a')));
                Assert.True(s1.SequenceEqual(s1.TrimStart('a')));
                Assert.True(s1.SequenceEqual(s1.TrimEnd('a')));

                ReadOnlySpan<char> span = s1.AsSpan();
                Assert.True(span.SequenceEqual(span.Trim('a')));
                Assert.True(span.SequenceEqual(span.TrimStart('a')));
                Assert.True(span.SequenceEqual(span.TrimEnd('a')));
            }
        }

        [Fact]
        public static void OnlyTrimCharacter()
        {
            for (int length = 0; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'a';
                }

                string s1 = new string(a);
                Assert.True(string.Empty.SequenceEqual(s1.Trim('a')));
                Assert.True(string.Empty.SequenceEqual(s1.TrimStart('a')));
                Assert.True(string.Empty.SequenceEqual(s1.TrimEnd('a')));

                ReadOnlySpan<char> span = s1.AsSpan();
                Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(span.Trim('a')));
                Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(span.TrimStart('a')));
                Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(span.TrimEnd('a')));
            }
        }

        [Fact]
        public static void TrimCharacterAtStart()
        {
            for (int length = 2; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'b';
                }
                a[0] = 'a';

                string s1 = new string(a);
                Assert.True(s1.Substring(1).SequenceEqual(s1.Trim('a')));
                Assert.True(s1.Substring(1).SequenceEqual(s1.TrimStart('a')));
                Assert.True(s1.SequenceEqual(s1.TrimEnd('a')));

                ReadOnlySpan<char> span = s1.AsSpan();
                Assert.True(span.Slice(1).SequenceEqual(span.Trim('a')));
                Assert.True(span.Slice(1).SequenceEqual(span.TrimStart('a')));
                Assert.True(span.SequenceEqual(span.TrimEnd('a')));
            }
        }

        [Fact]
        public static void TrimCharacterAtEnd()
        {
            for (int length = 2; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'b';
                }
                a[length - 1] = 'a';

                string s1 = new string(a);
                Assert.True(s1.Substring(0, length - 1).SequenceEqual(s1.Trim('a')));
                Assert.True(s1.SequenceEqual(s1.TrimStart('a')));
                Assert.True(s1.Substring(0, length - 1).SequenceEqual(s1.TrimEnd('a')));

                ReadOnlySpan<char> span = s1.AsSpan();
                Assert.True(span.Slice(0, length - 1).SequenceEqual(span.Trim('a')));
                Assert.True(span.SequenceEqual(span.TrimStart('a')));
                Assert.True(span.Slice(0, length - 1).SequenceEqual(span.TrimEnd('a')));
            }
        }

        [Fact]
        public static void TrimCharacterAtStartAndEnd()
        {
            for (int length = 3; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'b';
                }
                a[0] = 'a';
                a[length - 1] = 'a';

                string s1 = new string(a);
                Assert.True(s1.Substring(1, length - 2).SequenceEqual(s1.Trim('a')));
                Assert.True(s1.Substring(1).SequenceEqual(s1.TrimStart('a')));
                Assert.True(s1.Substring(0, length - 1).SequenceEqual(s1.TrimEnd('a')));

                ReadOnlySpan<char> span = s1.AsSpan();
                Assert.True(span.Slice(1, length - 2).SequenceEqual(span.Trim('a')));
                Assert.True(span.Slice(1).SequenceEqual(span.TrimStart('a')));
                Assert.True(span.Slice(0, length - 1).SequenceEqual(span.TrimEnd('a')));
            }
        }

        [Fact]
        public static void TrimCharacterInMiddle()
        {
            for (int length = 3; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'b';
                }
                a[1] = 'a';

                string s1 = new string(a);
                Assert.True(s1.SequenceEqual(s1.Trim('a')));
                Assert.True(s1.SequenceEqual(s1.TrimStart('a')));
                Assert.True(s1.SequenceEqual(s1.TrimEnd('a')));

                ReadOnlySpan<char> span = new ReadOnlySpan<char>(a);
                Assert.True(span.SequenceEqual(span.Trim('a')));
                Assert.True(span.SequenceEqual(span.TrimStart('a')));
                Assert.True(span.SequenceEqual(span.TrimEnd('a')));
            }
        }

        [Fact]
        public static void TrimCharacterMultipleTimes()
        {
            for (int length = 3; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'b';
                }
                a[0] = 'a';
                a[length - 1] = 'a';

                string s1 = new string(a);
                string trimResultString = s1.Trim('a');
                string trimStartResultString = s1.TrimStart('a');
                string trimEndResultString = s1.TrimEnd('a');
                Assert.True(s1.Substring(1, length - 2).SequenceEqual(trimResultString));
                Assert.True(s1.Substring(1).SequenceEqual(trimStartResultString));
                Assert.True(s1.Substring(0, length - 1).SequenceEqual(trimEndResultString));

                // 2nd attempt should do nothing
                Assert.True(trimResultString.SequenceEqual(trimResultString.Trim('a')));
                Assert.True(trimStartResultString.SequenceEqual(trimStartResultString.TrimStart('a')));
                Assert.True(trimEndResultString.SequenceEqual(trimEndResultString.TrimEnd('a')));

                ReadOnlySpan<char> span = s1.AsSpan();
                ReadOnlySpan<char> trimResult = span.Trim('a');
                ReadOnlySpan<char> trimStartResult = span.TrimStart('a');
                ReadOnlySpan<char> trimEndResult = span.TrimEnd('a');
                Assert.True(span.Slice(1, length - 2).SequenceEqual(trimResult));
                Assert.True(span.Slice(1).SequenceEqual(trimStartResult));
                Assert.True(span.Slice(0, length - 1).SequenceEqual(trimEndResult));

                // 2nd attempt should do nothing
                Assert.True(trimResult.SequenceEqual(trimResult.Trim('a')));
                Assert.True(trimStartResult.SequenceEqual(trimStartResult.TrimStart('a')));
                Assert.True(trimEndResult.SequenceEqual(trimEndResult.TrimEnd('a')));
            }
        }

        [Fact]
        public static void MakeSureNoTrimCharacterChecksGoOutOfRange()
        {
            for (int length = 3; length < 64; length++)
            {
                char[] first = new char[length];
                first[0] = 'a';
                first[length - 1] = 'a';

                string s1 = new string(first, 1, length - 2);
                Assert.True(s1.SequenceEqual(s1.Trim('a')));
                Assert.True(s1.SequenceEqual(s1.TrimStart('a')));
                Assert.True(s1.SequenceEqual(s1.TrimEnd('a')));

                ReadOnlySpan<char> span = s1.AsSpan();
                Assert.True(span.SequenceEqual(span.Trim('a')));
                Assert.True(span.SequenceEqual(span.TrimStart('a')));
                Assert.True(span.SequenceEqual(span.TrimEnd('a')));
            }
        }

        [Fact]
        public static void ZeroLengthTrimCharacters()
        {
            string s1 = string.Empty;
            char[] trimCharsString = Array.Empty<char>();
            Assert.True(s1.SequenceEqual(s1.Trim(trimCharsString)));
            Assert.True(s1.SequenceEqual(s1.TrimStart(trimCharsString)));
            Assert.True(s1.SequenceEqual(s1.TrimEnd(trimCharsString)));

            char[] chars = { 'a', 'b', 'c', 'd', 'e' };
            trimCharsString = chars;
            Assert.True(s1.SequenceEqual(s1.Trim(trimCharsString)));
            Assert.True(s1.SequenceEqual(s1.TrimStart(trimCharsString)));
            Assert.True(s1.SequenceEqual(s1.TrimEnd(trimCharsString)));

            string emptyString = string.Empty;
            char[] trimCharsArrayFromString = "abcde".ToCharArray();
            Assert.True(emptyString.SequenceEqual(emptyString.Trim(trimCharsArrayFromString)));
            Assert.True(emptyString.SequenceEqual(emptyString.TrimStart(trimCharsArrayFromString)));
            Assert.True(emptyString.SequenceEqual(emptyString.TrimEnd(trimCharsArrayFromString)));

            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> trimChars = trimCharsString.AsSpan();
            Assert.True(span.SequenceEqual(span.Trim(trimChars)));
            Assert.True(span.SequenceEqual(span.TrimStart(trimChars)));
            Assert.True(span.SequenceEqual(span.TrimEnd(trimChars)));

            trimChars = trimCharsString.AsSpan();
            Assert.True(span.SequenceEqual(span.Trim(trimChars)));
            Assert.True(span.SequenceEqual(span.TrimStart(trimChars)));
            Assert.True(span.SequenceEqual(span.TrimEnd(trimChars)));

            ReadOnlySpan<char> stringSpan = s1.AsSpan();
            ReadOnlySpan<char> trimCharsFromString = trimCharsArrayFromString.AsSpan();
            Assert.True(stringSpan.SequenceEqual(stringSpan.Trim(trimCharsFromString)));
            Assert.True(stringSpan.SequenceEqual(stringSpan.TrimStart(trimCharsFromString)));
            Assert.True(stringSpan.SequenceEqual(stringSpan.TrimEnd(trimCharsFromString)));
        }

        [Fact]
        public static void NoTrimCharacters()
        {
            char[] trimCharsString = Array.Empty<char>();
            ReadOnlySpan<char> trimChars = trimCharsString.AsSpan();
            for (int length = 0; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'f';
                }
                string s1 = new string(a);
                Assert.True(s1.SequenceEqual(s1.Trim(trimCharsString)));
                Assert.True(s1.SequenceEqual(s1.TrimStart(trimCharsString)));
                Assert.True(s1.SequenceEqual(s1.TrimEnd(trimCharsString)));

                ReadOnlySpan<char> span = s1.AsSpan();
                Assert.True(span.SequenceEqual(span.Trim(trimChars)));
                Assert.True(span.SequenceEqual(span.TrimStart(trimChars)));
                Assert.True(span.SequenceEqual(span.TrimEnd(trimChars)));
            }

            char[] chars = { 'a', 'b', 'c', 'd', 'e' };
            for (int length = 0; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'f';
                }
                string s2 = new string(a);
                Assert.True(s2.SequenceEqual(s2.Trim(chars)));
                Assert.True(s2.SequenceEqual(s2.TrimStart(chars)));
                Assert.True(s2.SequenceEqual(s2.TrimEnd(chars)));

                ReadOnlySpan<char> span = s2.AsSpan();
                Assert.True(span.SequenceEqual(span.Trim(chars)));
                Assert.True(span.SequenceEqual(span.TrimStart(chars)));
                Assert.True(span.SequenceEqual(span.TrimEnd(chars)));
            }

            string s3 = "ffghifhig";
            char[] trimCharsFromString = "abcde".ToCharArray();
            Assert.True(s3.SequenceEqual(s3.Trim(trimCharsFromString)));
            Assert.True(s3.SequenceEqual(s3.TrimStart(trimCharsFromString)));
            Assert.True(s3.SequenceEqual(s3.TrimEnd(trimCharsFromString)));

            ReadOnlySpan<char> stringSpan = s3.AsSpan();
            ReadOnlySpan<char> trimCharsFromStringSpan = trimCharsFromString.AsSpan();
            Assert.True(stringSpan.SequenceEqual(stringSpan.Trim(trimCharsFromStringSpan)));
            Assert.True(stringSpan.SequenceEqual(stringSpan.TrimStart(trimCharsFromStringSpan)));
            Assert.True(stringSpan.SequenceEqual(stringSpan.TrimEnd(trimCharsFromStringSpan)));
        }

        [Fact]
        public static void OnlyTrimCharacters()
        {
            char[] chars = { 'a', 'b', 'c', 'd', 'e' };
            for (int length = 0; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = chars[i % chars.Length];
                }

                string s1 = new string(a);
                Assert.True(string.Empty.SequenceEqual(s1.Trim(chars)), "G: " + length);
                Assert.True(string.Empty.SequenceEqual(s1.TrimStart(chars)), "H: " + length);
                Assert.True(string.Empty.SequenceEqual(s1.TrimEnd(chars)), "I: " + length);

                ReadOnlySpan<char> span = s1.AsSpan();
                Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(span.Trim(chars)), "G: " + length);
                Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(span.TrimStart(chars)), "H: " + length);
                Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(span.TrimEnd(chars)), "I: " + length);
            }

            string s2 = "babedebcabba";
            char[] trimCharsString = "abcde".ToCharArray();
            Assert.True(string.Empty.SequenceEqual(s2.Trim(trimCharsString)), "J");
            Assert.True(string.Empty.SequenceEqual(s2.TrimStart(trimCharsString)), "K");
            Assert.True(string.Empty.SequenceEqual(s2.TrimEnd(trimCharsString)), "L");

            ReadOnlySpan<char> stringSpan = s2.AsSpan();
            ReadOnlySpan<char> trimChars = trimCharsString.AsSpan();
            Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(stringSpan.Trim(trimChars)), "J");
            Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(stringSpan.TrimStart(trimChars)), "K");
            Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(stringSpan.TrimEnd(trimChars)), "L");
        }

        [Fact]
        public static void TrimCharactersAtStart()
        {
            char[] chars = { 'a', 'b', 'c', 'd', 'e' };
            for (int length = 2; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'f';
                }
                a[0] = 'c';
                string s1 = new string(a);
                Assert.True(s1.Substring(1).SequenceEqual(s1.Trim(chars)), "A: " + length);
                Assert.True(s1.Substring(1).SequenceEqual(s1.TrimStart(chars)), "B: " + length);
                Assert.True(s1.SequenceEqual(s1.TrimEnd(chars)), "C: " + length);

                ReadOnlySpan<char> span = s1.AsSpan();
                Assert.True(span.Slice(1).SequenceEqual(span.Trim(chars)), "A: " + length);
                Assert.True(span.Slice(1).SequenceEqual(span.TrimStart(chars)), "B: " + length);
                Assert.True(span.SequenceEqual(span.TrimEnd(chars)), "C: " + length);
            }

            string s2 = "babffffff";
            char[] trimCharsString = "abcde".ToCharArray();
            Assert.True(s2.Substring(3).SequenceEqual(s2.Trim(trimCharsString)), "D");
            Assert.True(s2.Substring(3).SequenceEqual(s2.TrimStart(trimCharsString)), "E");
            Assert.True(s2.SequenceEqual(s2.TrimEnd(trimCharsString)), "F");

            ReadOnlySpan<char> stringSpan = s2.AsSpan();
            ReadOnlySpan<char> trimChars = trimCharsString.AsSpan();
            Assert.True(stringSpan.Slice(3).SequenceEqual(stringSpan.Trim(trimChars)), "D");
            Assert.True(stringSpan.Slice(3).SequenceEqual(stringSpan.TrimStart(trimChars)), "E");
            Assert.True(stringSpan.SequenceEqual(stringSpan.TrimEnd(trimChars)), "F");
        }

        [Fact]
        public static void TrimCharactersAtEnd()
        {
            char[] chars = { 'a', 'b', 'c', 'd', 'e' };
            for (int length = 2; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'f';
                }
                a[length - 1] = 'c';

                string s1 = new string(a);
                Assert.True(s1.Substring(0, length - 1).SequenceEqual(s1.Trim(chars)));
                Assert.True(s1.SequenceEqual(s1.TrimStart(chars)));
                Assert.True(s1.Substring(0, length - 1).SequenceEqual(s1.TrimEnd(chars)));

                ReadOnlySpan<char> span = new ReadOnlySpan<char>(a);
                Assert.True(span.Slice(0, length - 1).SequenceEqual(span.Trim(chars)));
                Assert.True(span.SequenceEqual(span.TrimStart(chars)));
                Assert.True(span.Slice(0, length - 1).SequenceEqual(span.TrimEnd(chars)));
            }

            string s2 = "fffffcced";
            char[] trimCharsString = "abcde".ToCharArray();
            Assert.True(s2.Substring(0, 5).SequenceEqual(s2.Trim(trimCharsString)));
            Assert.True(s2.SequenceEqual(s2.TrimStart(trimCharsString)));
            Assert.True(s2.Substring(0, 5).SequenceEqual(s2.TrimEnd(trimCharsString)));

            ReadOnlySpan<char> stringSpan = s2.AsSpan();
            ReadOnlySpan<char> trimChars = trimCharsString.AsSpan();
            Assert.True(stringSpan.Slice(0, 5).SequenceEqual(stringSpan.Trim(trimChars)));
            Assert.True(stringSpan.SequenceEqual(stringSpan.TrimStart(trimChars)));
            Assert.True(stringSpan.Slice(0, 5).SequenceEqual(stringSpan.TrimEnd(trimChars)));
        }

        [Fact]
        public static void TrimCharactersAtStartAndEnd()
        {
            char[] chars = { 'a', 'b', 'c', 'd', 'e' };
            for (int length = 3; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'f';
                }
                a[0] = 'c';
                a[length - 1] = 'c';
                string s1 = new string(a);
                Assert.True(s1.Substring(1, length - 2).SequenceEqual(s1.Trim(chars)));
                Assert.True(s1.Substring(1).SequenceEqual(s1.TrimStart(chars)));
                Assert.True(s1.Substring(0, length - 1).SequenceEqual(s1.TrimEnd(chars)));

                ReadOnlySpan<char> span = s1.AsSpan();
                Assert.True(span.Slice(1, length - 2).SequenceEqual(span.Trim(chars)));
                Assert.True(span.Slice(1).SequenceEqual(span.TrimStart(chars)));
                Assert.True(span.Slice(0, length - 1).SequenceEqual(span.TrimEnd(chars)));
            }

            string s2 = "ccedafffffbdaa";
            char[] trimCharsString = "abcde".ToCharArray();
            Assert.True(s2.Substring(5, 5).SequenceEqual(s2.Trim(trimCharsString)));
            Assert.True(s2.Substring(5).SequenceEqual(s2.TrimStart(trimCharsString)));
            Assert.True(s2.Substring(0, 10).SequenceEqual(s2.TrimEnd(trimCharsString)));

            ReadOnlySpan<char> stringSpan = s2.AsSpan();
            ReadOnlySpan<char> trimChars = trimCharsString.AsSpan();
            Assert.True(stringSpan.Slice(5, 5).SequenceEqual(stringSpan.Trim(trimChars)));
            Assert.True(stringSpan.Slice(5).SequenceEqual(stringSpan.TrimStart(trimChars)));
            Assert.True(stringSpan.Slice(0, 10).SequenceEqual(stringSpan.TrimEnd(trimChars)));
        }

        [Fact]
        public static void TrimCharactersInMiddle()
        {
            char[] chars = { 'a', 'b', 'c', 'd', 'e' };
            for (int length = chars.Length + 2; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'f';
                }
                Array.Copy(chars, 0, a, 1, chars.Length);

                string s1 = new string(a);
                Assert.True(s1.SequenceEqual(s1.Trim(chars)));
                Assert.True(s1.SequenceEqual(s1.TrimStart(chars)));
                Assert.True(s1.SequenceEqual(s1.TrimEnd(chars)));

                ReadOnlySpan<char> span = s1.AsSpan();
                Assert.True(span.SequenceEqual(span.Trim(chars)));
                Assert.True(span.SequenceEqual(span.TrimStart(chars)));
                Assert.True(span.SequenceEqual(span.TrimEnd(chars)));
            }

            string s2 = "fabbacddeeddef";
            char[] trimCharsString = "abcde".ToCharArray();
            Assert.True(s2.SequenceEqual(s2.Trim(trimCharsString)));
            Assert.True(s2.SequenceEqual(s2.TrimStart(trimCharsString)));
            Assert.True(s2.SequenceEqual(s2.TrimEnd(trimCharsString)));

            ReadOnlySpan<char> stringSpan = s2.AsSpan();
            ReadOnlySpan<char> trimChars = trimCharsString.AsSpan();
            Assert.True(stringSpan.SequenceEqual(stringSpan.Trim(trimChars)));
            Assert.True(stringSpan.SequenceEqual(stringSpan.TrimStart(trimChars)));
            Assert.True(stringSpan.SequenceEqual(stringSpan.TrimEnd(trimChars)));
        }

        [Fact]
        public static void TrimCharactersMultipleTimes()
        {
            char[] chars = { 'a', 'b', 'c', 'd', 'e' };
            for (int length = 3; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'f';
                }
                a[0] = 'c';
                a[length - 1] = 'c';

                string s1 = new string(a);
                string trimResultString = s1.Trim(chars);
                string trimStartResultString = s1.TrimStart(chars);
                string trimEndResultString = s1.TrimEnd(chars);
                Assert.True(s1.Substring(1, length - 2).SequenceEqual(trimResultString));
                Assert.True(s1.Substring(1).SequenceEqual(trimStartResultString));
                Assert.True(s1.Substring(0, length - 1).SequenceEqual(trimEndResultString));

                // 2nd attempt should do nothing
                Assert.True(trimResultString.SequenceEqual(trimResultString.Trim(chars)));
                Assert.True(trimStartResultString.SequenceEqual(trimStartResultString.TrimStart(chars)));
                Assert.True(trimEndResultString.SequenceEqual(trimEndResultString.TrimEnd(chars)));

                ReadOnlySpan<char> span = s1.AsSpan();
                ReadOnlySpan<char> trimResult = span.Trim(chars);
                ReadOnlySpan<char> trimStartResult = span.TrimStart(chars);
                ReadOnlySpan<char> trimEndResult = span.TrimEnd(chars);
                Assert.True(span.Slice(1, length - 2).SequenceEqual(trimResult));
                Assert.True(span.Slice(1).SequenceEqual(trimStartResult));
                Assert.True(span.Slice(0, length - 1).SequenceEqual(trimEndResult));

                // 2nd attempt should do nothing
                Assert.True(trimResult.SequenceEqual(trimResult.Trim(chars)));
                Assert.True(trimStartResult.SequenceEqual(trimStartResult.TrimStart(chars)));
                Assert.True(trimEndResult.SequenceEqual(trimEndResult.TrimEnd(chars)));
            }

            string s2 = "ccedafffffbdaa";
            char[] trimCharsString = "abcde".ToCharArray();
            string trimStringResultString = s2.Trim(trimCharsString);
            string trimStartStringResultString = s2.TrimStart(trimCharsString);
            string trimEndStringResultString = s2.TrimEnd(trimCharsString);
            Assert.True(s2.Substring(5, 5).SequenceEqual(trimStringResultString));
            Assert.True(s2.Substring(5).SequenceEqual(trimStartStringResultString));
            Assert.True(s2.Substring(0, 10).SequenceEqual(trimEndStringResultString));

            // 2nd attempt should do nothing
            Assert.True(trimStringResultString.SequenceEqual(trimStringResultString.Trim(trimCharsString)));
            Assert.True(trimStartStringResultString.SequenceEqual(trimStartStringResultString.TrimStart(trimCharsString)));
            Assert.True(trimEndStringResultString.SequenceEqual(trimEndStringResultString.TrimEnd(trimCharsString)));

            ReadOnlySpan<char> stringSpan = s2.AsSpan();
            ReadOnlySpan<char> trimChars = trimCharsString.AsSpan();

            ReadOnlySpan<char> trimStringResult = stringSpan.Trim(trimChars);
            ReadOnlySpan<char> trimStartStringResult = stringSpan.TrimStart(trimChars);
            ReadOnlySpan<char> trimEndStringResult = stringSpan.TrimEnd(trimChars);
            Assert.True(stringSpan.Slice(5, 5).SequenceEqual(trimStringResult));
            Assert.True(stringSpan.Slice(5).SequenceEqual(trimStartStringResult));
            Assert.True(stringSpan.Slice(0, 10).SequenceEqual(trimEndStringResult));

            // 2nd attempt should do nothing
            Assert.True(trimStringResult.SequenceEqual(trimStringResult.Trim(trimChars)));
            Assert.True(trimStartStringResult.SequenceEqual(trimStartStringResult.TrimStart(trimChars)));
            Assert.True(trimEndStringResult.SequenceEqual(trimEndStringResult.TrimEnd(trimChars)));
        }

        [Fact]
        public static void MakeSureNoTrimCharactersChecksGoOutOfRange()
        {
            char[] chars = { 'a', 'b', 'c', 'd', 'e' };
            for (int length = 3; length < 64; length++)
            {
                char[] first = new char[length];
                first[0] = 'f';
                first[length - 1] = 'f';
                string s1 = new string(first, 1, length - 2);
                Assert.Equal(s1.ToArray().Length, s1.Trim(chars).ToArray().Length);
                Assert.True(s1.SequenceEqual(s1.Trim(chars)), "A : " + s1.Length);
                Assert.True(s1.SequenceEqual(s1.TrimStart(chars)), "B :" + s1.Length);
                Assert.True(s1.SequenceEqual(s1.TrimEnd(chars)));

                ReadOnlySpan<char> span = s1.AsSpan();
                Assert.Equal(span.ToArray().Length, span.Trim(chars).ToArray().Length);
                Assert.True(span.SequenceEqual(span.Trim(chars)), "A : " + span.Length);
                Assert.True(span.SequenceEqual(span.TrimStart(chars)), "B :" + span.Length);
                Assert.True(span.SequenceEqual(span.TrimEnd(chars)));
            }

            string testString = "afghijklmnopqrstfe";

            string s2 = testString.Substring(1, testString.Length - 2);
            char[] trimCharsString = "abcde".ToCharArray();
            Assert.True(s2.SequenceEqual(s2.Trim(trimCharsString)));
            Assert.True(s2.SequenceEqual(s2.TrimStart(trimCharsString)));
            Assert.True(s2.SequenceEqual(s2.TrimEnd(trimCharsString)));

            ReadOnlySpan<char> stringSpan = s2.AsSpan();
            ReadOnlySpan<char> trimChars = trimCharsString.AsSpan();
            Assert.True(stringSpan.SequenceEqual(stringSpan.Trim(trimChars)));
            Assert.True(stringSpan.SequenceEqual(stringSpan.TrimStart(trimChars)));
            Assert.True(stringSpan.SequenceEqual(stringSpan.TrimEnd(trimChars)));
        }

        [Fact]
        public static void ZeroLengthTrim()
        {
            string s1 = string.Empty;
            Assert.True(s1.SequenceEqual(s1.Trim()));
            Assert.True(s1.SequenceEqual(s1.TrimStart()));
            Assert.True(s1.SequenceEqual(s1.TrimEnd()));

            ReadOnlySpan<char> span = s1.AsSpan();
            Assert.True(span.SequenceEqual(span.Trim()));
            Assert.True(span.SequenceEqual(span.TrimStart()));
            Assert.True(span.SequenceEqual(span.TrimEnd()));
        }

        [Fact]
        public static void NoWhiteSpaceTrim()
        {
            for (int length = 0; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'a';
                }

                string s1 = new string(a);
                Assert.True(s1.SequenceEqual(s1.Trim()));
                Assert.True(s1.SequenceEqual(s1.TrimStart()));
                Assert.True(s1.SequenceEqual(s1.TrimEnd()));

                ReadOnlySpan<char> span = s1.AsSpan();
                Assert.True(span.SequenceEqual(span.Trim()));
                Assert.True(span.SequenceEqual(span.TrimStart()));
                Assert.True(span.SequenceEqual(span.TrimEnd()));
            }
        }

        [Fact]
        public static void OnlyWhiteSpaceTrim()
        {
            for (int length = 0; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = ' ';
                }

                string s1 = new string(a);
                Assert.True(string.Empty.SequenceEqual(s1.Trim()));
                Assert.True(string.Empty.SequenceEqual(s1.TrimStart()));
                Assert.True(string.Empty.SequenceEqual(s1.TrimEnd()));

                ReadOnlySpan<char> span = new ReadOnlySpan<char>(a);
                Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(span.Trim()));
                Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(span.TrimStart()));
                Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(span.TrimEnd()));
            }
        }

        [Fact]
        public static void WhiteSpaceAtStartTrim()
        {
            for (int length = 2; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'a';
                }
                a[0] = ' ';

                string s1 = new string(a);
                Assert.True(s1.Substring(1).SequenceEqual(s1.Trim()));
                Assert.True(s1.Substring(1).SequenceEqual(s1.TrimStart()));
                Assert.True(s1.SequenceEqual(s1.TrimEnd()));

                ReadOnlySpan<char> span = s1.AsSpan();
                Assert.True(span.Slice(1).SequenceEqual(span.Trim()));
                Assert.True(span.Slice(1).SequenceEqual(span.TrimStart()));
                Assert.True(span.SequenceEqual(span.TrimEnd()));
            }
        }

        [Fact]
        public static void WhiteSpaceAtEndTrim()
        {
            for (int length = 2; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'a';
                }
                a[length - 1] = ' ';

                string s1 = new string(a);
                Assert.True(s1.Substring(0, length - 1).SequenceEqual(s1.Trim()));
                Assert.True(s1.SequenceEqual(s1.TrimStart()));
                Assert.True(s1.Substring(0, length - 1).SequenceEqual(s1.TrimEnd()));

                ReadOnlySpan<char> span = s1.AsSpan();
                Assert.True(span.Slice(0, length - 1).SequenceEqual(span.Trim()));
                Assert.True(span.SequenceEqual(span.TrimStart()));
                Assert.True(span.Slice(0, length - 1).SequenceEqual(span.TrimEnd()));
            }
        }

        [Fact]
        public static void WhiteSpaceAtStartAndEndTrim()
        {
            for (int length = 3; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'a';
                }
                a[0] = ' ';
                a[length - 1] = ' ';

                string s1 = new string(a);
                Assert.True(s1.Substring(1, length - 2).SequenceEqual(s1.Trim()));
                Assert.True(s1.Substring(1).SequenceEqual(s1.TrimStart()));
                Assert.True(s1.Substring(0, length - 1).SequenceEqual(s1.TrimEnd()));

                ReadOnlySpan<char> span = s1.AsSpan();
                Assert.True(span.Slice(1, length - 2).SequenceEqual(span.Trim()));
                Assert.True(span.Slice(1).SequenceEqual(span.TrimStart()));
                Assert.True(span.Slice(0, length - 1).SequenceEqual(span.TrimEnd()));
            }
        }

        [Fact]
        public static void WhiteSpaceInMiddleTrim()
        {
            for (int length = 3; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'a';
                }
                a[1] = ' ';

                string s1 = new string(a);
                Assert.True(s1.SequenceEqual(s1.Trim()));
                Assert.True(s1.SequenceEqual(s1.TrimStart()));
                Assert.True(s1.SequenceEqual(s1.TrimEnd()));

                ReadOnlySpan<char> span = s1.AsSpan();
                Assert.True(span.SequenceEqual(span.Trim()));
                Assert.True(span.SequenceEqual(span.TrimStart()));
                Assert.True(span.SequenceEqual(span.TrimEnd()));
            }
        }

        [Fact]
        public static void TrimWhiteSpaceMultipleTimes()
        {
            for (int length = 3; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'a';
                }
                a[0] = ' ';
                a[length - 1] = ' ';

                string s1 = new string(a);
                string trimResultString = s1.Trim();
                string trimStartResultString = s1.TrimStart();
                string trimEndResultString = s1.TrimEnd();
                Assert.True(s1.Substring(1, length - 2).SequenceEqual(trimResultString));
                Assert.True(s1.Substring(1).SequenceEqual(trimStartResultString));
                Assert.True(s1.Substring(0, length - 1).SequenceEqual(trimEndResultString));

                // 2nd attempt should do nothing
                Assert.True(trimResultString.SequenceEqual(trimResultString.Trim()));
                Assert.True(trimStartResultString.SequenceEqual(trimStartResultString.TrimStart()));
                Assert.True(trimEndResultString.SequenceEqual(trimEndResultString.TrimEnd()));

                ReadOnlySpan<char> span = s1.AsSpan();
                ReadOnlySpan<char> trimResult = span.Trim();
                ReadOnlySpan<char> trimStartResult = span.TrimStart();
                ReadOnlySpan<char> trimEndResult = span.TrimEnd();
                Assert.True(span.Slice(1, length - 2).SequenceEqual(trimResult));
                Assert.True(span.Slice(1).SequenceEqual(trimStartResult));
                Assert.True(span.Slice(0, length - 1).SequenceEqual(trimEndResult));

                // 2nd attempt should do nothing
                Assert.True(trimResult.SequenceEqual(trimResult.Trim()));
                Assert.True(trimStartResult.SequenceEqual(trimStartResult.TrimStart()));
                Assert.True(trimEndResult.SequenceEqual(trimEndResult.TrimEnd()));
            }
        }

        [Fact]
        public static void MakeSureNoTrimChecksGoOutOfRange()
        {
            for (int length = 3; length < 64; length++)
            {
                char[] first = new char[length];
                first[0] = ' ';
                first[length - 1] = ' ';

                string s1 = new string(first, 1, length - 2);
                Assert.True(s1.SequenceEqual(s1.Trim()));
                Assert.True(s1.SequenceEqual(s1.TrimStart()));
                Assert.True(s1.SequenceEqual(s1.TrimEnd()));

                ReadOnlySpan<char> span = s1.AsSpan();
                Assert.True(span.SequenceEqual(span.Trim()));
                Assert.True(span.SequenceEqual(span.TrimStart()));
                Assert.True(span.SequenceEqual(span.TrimEnd()));
            }
        }

        [Fact]
        public static void EqualityOperators()
        {
            var s1 = new string(new char[] { 'a' });
            var s1a = new string(new char[] { 'a' });
            var s2 = new string(new char[] { 'b' });

            Assert.True(s1 == s1a);
            Assert.False(s1 != s1a);

            Assert.False(s1 == s2);
            Assert.True(s1 != s2);
        }

        public static IEnumerable<object[]> AllSubstringsAndComparisons(string source)
        {
            var comparisons = new StringComparison[]
            {
                StringComparison.CurrentCulture,
                StringComparison.CurrentCultureIgnoreCase,
                StringComparison.Ordinal,
                StringComparison.OrdinalIgnoreCase
            };

            foreach (StringComparison comparison in comparisons)
            {
                for (int i = 0; i <= source.Length; i++)
                {
                    for (int subLen = source.Length - i; subLen >= 0; subLen--)
                    {
                        yield return new object[] { source, source.Substring(i, subLen), i, comparison };
                    }
                }

                for (int i = 0; i <= source.Length; i++)
                {
                    yield return new object[] { "", source.Substring(i), 0, comparison };
                }
            }
        }

        private class ObjectWithNullToString
        {
            public override string ToString() => null;
        }

        private class TestFormatter : IFormatProvider, ICustomFormatter
        {
            public object GetFormat(Type formatType)
            {
                return formatType == typeof(ICustomFormatter) ? this : null;
            }

            public string Format(string format, object arg, IFormatProvider formatProvider)
            {
                return "Test: " + format + ": " + arg;
            }
        }

        public static IEnumerable<object[]> CompareTest_TestData()
        {
            //                           str1               str2          culture  ignorecase   expected
            yield return new object[] { "abcd",             "ABcd",       "en-US",    false,       PlatformDetection.IsInvariantGlobalization ? 1 : -1 };
            yield return new object[] { "abcd",             "ABcd",       null,       false,       PlatformDetection.IsInvariantGlobalization ? 1 : -1  };
            yield return new object[] { "ABcd",             "abcd",       "en-US",    false,       PlatformDetection.IsInvariantGlobalization ? -1 : 1  };
            yield return new object[] { "ABcd",             "abcd",       null,       false,       PlatformDetection.IsInvariantGlobalization ? -1 : 1  };
            yield return new object[] { "abcd",             "ABcd",       "en-US",    true,         0  };
            yield return new object[] { "abcd",             "ABcd",       null,       true,         0  };
            yield return new object[] { null,               null,         "en-us",    true,         0  };
            yield return new object[] { null,               null,         null,       true,         0  };
            yield return new object[] { null,               "",           "en-us",    true,        -1  };
            yield return new object[] { null,               "",           null,       true,        -1  };
            yield return new object[] { "",                 null,         "en-us",    true,         1  };
            yield return new object[] { "",                 null,         null,       true,         1  };

            // Android has different results w/ tr-TR
            // See https://github.com/dotnet/runtime/issues/60568
            if (PlatformDetection.IsNotInvariantGlobalization && !PlatformDetection.IsAndroid && !PlatformDetection.IsLinuxBionic)
            {
                yield return new object[] { "latin i",         "Latin I",     "tr-TR",    false,        1  };
                yield return new object[] { "latin i",         "Latin I",     "tr-TR",    true,         1  };
                yield return new object[] { "turkish \u0130",   "Turkish i",  "tr-TR",    true,         0  };
                yield return new object[] { "turkish \u0131",   "Turkish I",  "tr-TR",    true,         0  };
            }
        }

        public static IEnumerable<object[]> UpperLowerCasing_TestData()
        {
            //                          lower                upper          Culture
            yield return new object[] { "abcd",             "ABCD",         "en-US" };
            yield return new object[] { "abcd",             "ABCD",         null };
            yield return new object[] { "latin i",          "LATIN I",      "en-US" };
            yield return new object[] { "latin i",          "LATIN I",      null };
            yield return new object[] { "",                 "",             null };

            if (PlatformDetection.IsNotInvariantGlobalization)
            {
                // Android has different results w/ tr-TR
                // See https://github.com/dotnet/runtime/issues/60568
                if (!PlatformDetection.IsAndroid && !PlatformDetection.IsLinuxBionic)
                {
                    yield return new object[] { "turky \u0131",     "TURKY I",      "tr-TR" };
                    yield return new object[] { "turky i",          "TURKY \u0130", "tr-TR" };
                }
                yield return new object[] { "\ud801\udc29",     PlatformDetection.IsWindows7 ? "\ud801\udc29" : "\ud801\udc01", "en-US" };
            }
        }

        public static IEnumerable<object[]> StartEndWith_TestData()
        {
            //                           str1                    Start      End     Culture  ignorecase   expected
            yield return new object[] { "abcd",                  "abcd",    "abcd", "en-US",    false,       true   };
            yield return new object[] { "abcd",                  "abcd",    "abcd", "en-US",    true,        true   };
            yield return new object[] { "abcd",                  "AB",      "CD",   "en-US",    false,       false  };
            yield return new object[] { "abcd",                  "AB",      "CD",   null,       false,       false  };
            yield return new object[] { "ABcd",                  "ab",      "CD",   "en-US",    false,       false  };
            yield return new object[] { "abcd",                  "AB",      "CD",   "en-US",    true,        true   };

            // Android has different results w/ tr-TR
            // See https://github.com/dotnet/runtime/issues/60568
            if (PlatformDetection.IsNotInvariantGlobalization && !PlatformDetection.IsAndroid && !PlatformDetection.IsLinuxBionic)
            {
                yield return new object[] { "i latin i",             "I Latin", "I",    "tr-TR",    false,       false  };
                yield return new object[] { "i latin i",             "I Latin", "I",    "tr-TR",    true,        false  };
                yield return new object[] { "\u0130 turkish \u0130", "i",       "i",    "tr-TR",    true,        true   };
                yield return new object[] { "\u0131 turkish \u0131", "I",       "I",    "tr-TR",    true,        true   };
            }
        }

        [Theory]
        [MemberData(nameof(CompareTest_TestData))]
        public static void CompareTest(string s1, string s2, string cultureName, bool ignoreCase, int expected)
        {
            CultureInfo ci = cultureName != null ? CultureInfo.GetCultureInfo(cultureName) : null;
            CompareOptions ignoreCaseOption = ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None;

            Assert.Equal(expected, Math.Sign(String.Compare(s1, s2, ignoreCase, ci)));
            Assert.Equal(expected, Math.Sign(String.Compare(s1, 0, s2, 0, s1 == null ? 0 : s1.Length, ignoreCase, ci)));
            Assert.Equal(expected, Math.Sign(String.Compare(s1, 0, s2, 0, s1 == null ? 0 : s1.Length, ci, ignoreCaseOption)));

            Assert.Equal(expected, Math.Sign(String.Compare(s1, s2, ci, ignoreCaseOption)));
            Assert.Equal(Math.Sign(String.Compare(s1, s2, StringComparison.Ordinal)), Math.Sign(String.Compare(s1, s2, ci, CompareOptions.Ordinal)));
            Assert.Equal(Math.Sign(String.Compare(s1, s2, StringComparison.OrdinalIgnoreCase)), Math.Sign(String.Compare(s1, s2, ci, CompareOptions.OrdinalIgnoreCase)));

            if (ci != null && PlatformDetection.IsNotInvariantGlobalization)
            {
                using (new ThreadCultureChange(ci))
                {
                    Assert.Equal(expected, String.Compare(s1, 0, s2, 0, s1 == null ? 0 : s1.Length, ignoreCase));
                }
            }
        }

        [Theory]
        [MemberData(nameof(UpperLowerCasing_TestData))]
        public static void CasingTest(string lowerForm, string upperForm, string cultureName)
        {
            CultureInfo ci = cultureName != null ? CultureInfo.GetCultureInfo(cultureName) : null;
            Assert.Equal(lowerForm, upperForm.ToLower(ci));
            Assert.Equal(upperForm, lowerForm.ToUpper(ci));

            Span<char> destinationLower = new char[upperForm.Length];
            Span<char> destinationUpper = new char[lowerForm.Length];

            Assert.Equal(upperForm.Length, upperForm.AsSpan().ToLower(destinationLower, ci));
            Assert.Equal(lowerForm.Length, lowerForm.AsSpan().ToUpper(destinationUpper, ci));

            Assert.Equal(upperForm.ToLower(ci), destinationLower.ToString());
            Assert.Equal(lowerForm.ToUpper(ci), destinationUpper.ToString());

            Assert.Equal(lowerForm, lowerForm.AsSpan().ToString());
            Assert.Equal(upperForm, upperForm.AsSpan().ToString());
        }

        [Theory]
        [MemberData(nameof(StartEndWith_TestData))]
        public static void StartEndWithTest(string source, string start, string end, string cultureName, bool ignoreCase, bool expected)
        {
             CultureInfo ci = cultureName != null ? CultureInfo.GetCultureInfo(cultureName) : null;
             Assert.Equal(expected, source.StartsWith(start, ignoreCase, ci));
             Assert.Equal(expected, source.EndsWith(end, ignoreCase, ci));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        [InlineData("", StringComparison.InvariantCulture, true)]
        [InlineData("", StringComparison.Ordinal, true)]
        [InlineData(ZeroWidthJoiner, StringComparison.InvariantCulture, true)]
        [InlineData(ZeroWidthJoiner, StringComparison.Ordinal, false)]
        public static void StartEndWith_ZeroWeightValue(string value, StringComparison comparison, bool expectedStartsAndEndsWithResult)
        {
            Assert.Equal(expectedStartsAndEndsWithResult, string.Empty.StartsWith(value, comparison));
            Assert.Equal(expectedStartsAndEndsWithResult, string.Empty.EndsWith(value, comparison));
            Assert.Equal(expectedStartsAndEndsWithResult ? 0 : -1, string.Empty.IndexOf(value, comparison));
            Assert.Equal(expectedStartsAndEndsWithResult ? 0 : -1, string.Empty.LastIndexOf(value, comparison));
        }

        [Fact]
        public static void StartEndNegativeTest()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => "".StartsWith(null, true, null));
            AssertExtensions.Throws<ArgumentNullException>("value", () => "".EndsWith(null, true, null));
        }

        [Fact]
        public static unsafe void Ctor_SByte()
        {
            string s = "This is a string constructor test";
            byte[] encodedBytes = Encoding.Default.GetBytes(s);
            fixed (byte* pBytes = encodedBytes)
            {
                Assert.Equal(s, new string((sbyte*)pBytes));
                Assert.Equal(s, new string((sbyte*)pBytes, 0, encodedBytes.Length));
                Assert.Equal(s, new string((sbyte*)pBytes, 0, encodedBytes.Length, Encoding.Default));
            }

            s = "This is some string \u0393\u0627\u3400\u0440\u1100";
            encodedBytes = Encoding.UTF8.GetBytes(s);
            fixed (byte* pBytes = encodedBytes)
            {
                Assert.Equal(s, new string((sbyte*)pBytes, 0, encodedBytes.Length, Encoding.UTF8));
            }

            fixed (byte* pBytes = new byte[1] { 0 })
            {
                Assert.Equal(string.Empty, new string((sbyte*)pBytes));
                Assert.Equal(string.Empty, new string((sbyte*)pBytes, 0, 0));
                Assert.Equal(string.Empty, new string((sbyte*)pBytes, 0, 0, Encoding.UTF8));
            }
        }

        [Fact]
        public static void StartsWithMatchDifferentSpans_StringComparison()
        {
            char[] a = { '4', '5', '6', '7' };
            char[] b = { '4', '5', '6' };

            string s1 = new string(a, 0, 3);
            string s2 = new string(b, 0, 3);
            Assert.True(s1.StartsWith(s2, StringComparison.Ordinal));

            Assert.True(s1.StartsWith(s2, StringComparison.CurrentCulture));
            Assert.True(s1.StartsWith(s2, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(s1.StartsWith(s2, StringComparison.InvariantCulture));
            Assert.True(s1.StartsWith(s2, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(s1.StartsWith(s2, StringComparison.OrdinalIgnoreCase));

            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> slice = s2.AsSpan();
            Assert.True(span.StartsWith(slice, StringComparison.Ordinal));

            Assert.True(span.StartsWith(slice, StringComparison.CurrentCulture));
            Assert.True(span.StartsWith(slice, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(span.StartsWith(slice, StringComparison.InvariantCulture));
            Assert.True(span.StartsWith(slice, StringComparison.InvariantCultureIgnoreCase));
            Assert.True(span.StartsWith(slice, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public static void StartsWithNoMatch_StringComparison()
        {
            for (int length = 1; length < 150; length++)
            {
                for (int mismatchIndex = 0; mismatchIndex < length; mismatchIndex++)
                {
                    var first = new char[length];
                    var second = new char[length];
                    for (int i = 0; i < length; i++)
                    {
                        first[i] = second[i] = (char)(i + 1);
                    }

                    second[mismatchIndex] = (char)(second[mismatchIndex] + 1);

                    string s1 = new string(first);
                    string s2 = new string(second);
                    Assert.False(s1.StartsWith(s2, StringComparison.Ordinal));

                    Assert.False(s1.StartsWith(s2, StringComparison.OrdinalIgnoreCase));

                    // Different behavior depending on OS
                    Assert.Equal(
                        s1.ToString().StartsWith(s2.ToString(), StringComparison.CurrentCulture),
                        s1.StartsWith(s2, StringComparison.CurrentCulture));
                    Assert.Equal(
                        s1.ToString().StartsWith(s2.ToString(), StringComparison.CurrentCultureIgnoreCase),
                        s1.StartsWith(s2, StringComparison.CurrentCultureIgnoreCase));
                    Assert.Equal(
                        s1.ToString().StartsWith(s2.ToString(), StringComparison.InvariantCulture),
                        s1.StartsWith(s2, StringComparison.InvariantCulture));
                    Assert.Equal(
                        s1.ToString().StartsWith(s2.ToString(), StringComparison.InvariantCultureIgnoreCase),
                        s1.StartsWith(s2, StringComparison.InvariantCultureIgnoreCase));

                    ReadOnlySpan<char> firstSpan = s1.AsSpan();
                    ReadOnlySpan<char> secondSpan = s2.AsSpan();
                    Assert.False(firstSpan.StartsWith(secondSpan, StringComparison.Ordinal));

                    Assert.False(firstSpan.StartsWith(secondSpan, StringComparison.OrdinalIgnoreCase));

                    // Different behavior depending on OS
                    Assert.Equal(
                        firstSpan.ToString().StartsWith(secondSpan.ToString(), StringComparison.CurrentCulture),
                        firstSpan.StartsWith(secondSpan, StringComparison.CurrentCulture));
                    Assert.Equal(
                        firstSpan.ToString().StartsWith(secondSpan.ToString(), StringComparison.CurrentCultureIgnoreCase),
                        firstSpan.StartsWith(secondSpan, StringComparison.CurrentCultureIgnoreCase));
                    Assert.Equal(
                        firstSpan.ToString().StartsWith(secondSpan.ToString(), StringComparison.InvariantCulture),
                        firstSpan.StartsWith(secondSpan, StringComparison.InvariantCulture));
                    Assert.Equal(
                        firstSpan.ToString().StartsWith(secondSpan.ToString(), StringComparison.InvariantCultureIgnoreCase),
                        firstSpan.StartsWith(secondSpan, StringComparison.InvariantCultureIgnoreCase));
                }
            }
        }

        [Fact]
        public static void MakeSureNoStartsWithChecksGoOutOfRange_StringComparison()
        {
            for (int length = 0; length < 100; length++)
            {
                var first = new char[length + 2];
                first[0] = (char)99;
                first[length + 1] = (char)99;
                var second = new char[length + 2];
                second[0] = (char)100;
                second[length + 1] = (char)100;

                string s1 = new string(first, 1, length);
                string s2 = new string(second, 1, length);
                Assert.True(s1.StartsWith(s2, StringComparison.Ordinal));

                Assert.True(s1.StartsWith(s2, StringComparison.CurrentCulture));
                Assert.True(s1.StartsWith(s2, StringComparison.CurrentCultureIgnoreCase));
                Assert.True(s1.StartsWith(s2, StringComparison.InvariantCulture));
                Assert.True(s1.StartsWith(s2, StringComparison.InvariantCultureIgnoreCase));
                Assert.True(s1.StartsWith(s2, StringComparison.OrdinalIgnoreCase));

                ReadOnlySpan<char> span1 = s1.AsSpan();
                ReadOnlySpan<char> span2 = s2.AsSpan();
                Assert.True(span1.StartsWith(span2, StringComparison.Ordinal));

                Assert.True(span1.StartsWith(span2, StringComparison.CurrentCulture));
                Assert.True(span1.StartsWith(span2, StringComparison.CurrentCultureIgnoreCase));
                Assert.True(span1.StartsWith(span2, StringComparison.InvariantCulture));
                Assert.True(span1.StartsWith(span2, StringComparison.InvariantCultureIgnoreCase));
                Assert.True(span1.StartsWith(span2, StringComparison.OrdinalIgnoreCase));
            }
        }

        [Fact]
        public static void StartsWithUnknownComparisonType_StringComparison()
        {
            string s1 = "456";
            Assert.Throws<ArgumentException>(() => s1.StartsWith(s1, StringComparison.CurrentCulture - 1));
            Assert.Throws<ArgumentException>(() => s1.StartsWith(s1, StringComparison.OrdinalIgnoreCase + 1));
            Assert.Throws<ArgumentException>(() => s1.StartsWith(s1, (StringComparison)6));

            Assert.Throws<ArgumentException>(() => s1.AsSpan().StartsWith(s1.AsSpan(), StringComparison.CurrentCulture - 1));
            Assert.Throws<ArgumentException>(() => s1.AsSpan().StartsWith(s1.AsSpan(), StringComparison.OrdinalIgnoreCase + 1));
            Assert.Throws<ArgumentException>(() => s1.AsSpan().StartsWith(s1.AsSpan(), (StringComparison)6));

            Assert.Throws<ArgumentException>(() => s1.AsSpan().CompareTo(s1.AsSpan(), StringComparison.CurrentCulture - 1));
            Assert.Throws<ArgumentException>(() => s1.AsSpan().CompareTo(s1.AsSpan(), StringComparison.OrdinalIgnoreCase + 1));
            Assert.Throws<ArgumentException>(() => s1.AsSpan().CompareTo(s1.AsSpan(), (StringComparison)6));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        public static void StartsWithMatchNonOrdinal_StringComparison()
        {
            string s1 = "abcd";
            string s2 = "aBc";
            Assert.False(s1.StartsWith(s2, StringComparison.Ordinal));
            Assert.True(s1.StartsWith(s2, StringComparison.OrdinalIgnoreCase));

            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> value = s2.AsSpan();
            Assert.False(span.StartsWith(value, StringComparison.Ordinal));
            Assert.True(span.StartsWith(value, StringComparison.OrdinalIgnoreCase));

            using (new ThreadCultureChange("el-GR"))
            {
                s1 = "\u03b1\u03b2\u03b3\u03b4";  // \u03B1\u03B2\u03B3\u03B4
                s2 = "\u03b1\u03b2\u03b3"; // \u03B1\u03B2\u03B3

                Assert.True(s1.StartsWith(s2, StringComparison.CurrentCulture));
                Assert.True(s1.StartsWith(s2, StringComparison.CurrentCultureIgnoreCase));

                span = s1.AsSpan();
                value = s2.AsSpan();

                Assert.True(span.StartsWith(value, StringComparison.CurrentCulture));
                Assert.True(span.StartsWith(value, StringComparison.CurrentCultureIgnoreCase));

                s2 = "\u03b1\u0392\u03b3"; // \u03B1\u0392\u03B3
                Assert.False(s1.StartsWith(s2, StringComparison.CurrentCulture));
                Assert.True(s1.StartsWith(s2, StringComparison.CurrentCultureIgnoreCase));

                value = s2.AsSpan();
                Assert.False(span.StartsWith(value, StringComparison.CurrentCulture));
                Assert.True(span.StartsWith(value, StringComparison.CurrentCultureIgnoreCase));
            }

            s1 = "\u0069\u00df\u0049\u03b4"; // i\u00DFI\u03B4
            s2 = "\u0069\u0073\u0073\u0049"; // issI

            Assert.False(s1.StartsWith(s2, StringComparison.Ordinal));
            // Different behavior depending on OS - True on Windows, False on Unix
            Assert.Equal(
                s1.ToString().StartsWith(s2.ToString(), StringComparison.InvariantCulture),
                s1.StartsWith(s2, StringComparison.InvariantCulture));
            Assert.Equal(
                s1.ToString().StartsWith(s2.ToString(), StringComparison.InvariantCultureIgnoreCase),
                s1.StartsWith(s2, StringComparison.InvariantCultureIgnoreCase));

            span = s1.AsSpan();
            value = s2.AsSpan();

            Assert.False(span.StartsWith(value, StringComparison.Ordinal));
            // Different behavior depending on OS - True on Windows, False on Unix
            Assert.Equal(
                span.ToString().StartsWith(value.ToString(), StringComparison.InvariantCulture),
                span.StartsWith(value, StringComparison.InvariantCulture));
            Assert.Equal(
                span.ToString().StartsWith(value.ToString(), StringComparison.InvariantCultureIgnoreCase),
                span.StartsWith(value, StringComparison.InvariantCultureIgnoreCase));

            s2 = "\u0049\u0073\u0073\u0049"; // IssI
            Assert.False(s1.StartsWith(s2, StringComparison.OrdinalIgnoreCase));
            Assert.False(s1.StartsWith(s2, StringComparison.InvariantCulture));
            // Different behavior depending on OS - True on Windows, False on Unix
            Assert.Equal(
                s1.ToString().StartsWith(s2.ToString(), StringComparison.InvariantCultureIgnoreCase),
                s1.StartsWith(s2, StringComparison.InvariantCultureIgnoreCase));

            value = s2.AsSpan();
            Assert.False(span.StartsWith(value, StringComparison.OrdinalIgnoreCase));
            Assert.False(span.StartsWith(value, StringComparison.InvariantCulture));
            // Different behavior depending on OS - True on Windows, False on Unix
            Assert.Equal(
                span.ToString().StartsWith(value.ToString(), StringComparison.InvariantCultureIgnoreCase),
                span.StartsWith(value, StringComparison.InvariantCultureIgnoreCase));
        }

        [Fact]
        public static void StartsWithNoMatchNonOrdinal_StringComparison()
        {
            string s1 = "abcd";
            string s2 = "aDc";
            Assert.False(s1.StartsWith(s2, StringComparison.Ordinal));
            Assert.False(s1.StartsWith(s2, StringComparison.OrdinalIgnoreCase));

            ReadOnlySpan<char> span = s1.AsSpan();
            ReadOnlySpan<char> value = s2.AsSpan();
            Assert.False(span.StartsWith(value, StringComparison.Ordinal));
            Assert.False(span.StartsWith(value, StringComparison.OrdinalIgnoreCase));

            using (new ThreadCultureChange("el-GR"))
            {
                s1 = "\u03b1\u03b2\u03b3\u03b4"; // \u03B1\u03B2\u03B3\u03B4
                s2 = "\u03b1\u03b4\u03b3"; // \u03B1\u03B4\u03B3

                Assert.False(s1.StartsWith(s2, StringComparison.CurrentCulture));
                Assert.False(s1.StartsWith(s2, StringComparison.CurrentCultureIgnoreCase));

                span = s1.AsSpan();
                value = s2.AsSpan();

                Assert.False(span.StartsWith(value, StringComparison.CurrentCulture));
                Assert.False(span.StartsWith(value, StringComparison.CurrentCultureIgnoreCase));

                s2 = "\u03b1\u0394\u03b3"; // \u03B1\u0394\u03B3
                Assert.False(s1.StartsWith(s2, StringComparison.CurrentCulture));
                Assert.False(s1.StartsWith(s2, StringComparison.CurrentCultureIgnoreCase));

                value = s2.AsSpan();
                Assert.False(span.StartsWith(value, StringComparison.CurrentCulture));
                Assert.False(span.StartsWith(value, StringComparison.CurrentCultureIgnoreCase));
            }

            s1 = "\u0069\u00df\u0049\u03b4"; // i\u00DFI\u03B4
            s2 = "\u0069\u03b4\u03b4\u0049"; // i\u03B4\u03B4I

            Assert.False(s1.StartsWith(s2, StringComparison.Ordinal));
            Assert.False(s1.StartsWith(s2, StringComparison.InvariantCulture));
            Assert.False(s1.StartsWith(s2, StringComparison.InvariantCultureIgnoreCase));

            span = s1.AsSpan();
            value = s2.AsSpan();

            Assert.False(span.StartsWith(value, StringComparison.Ordinal));
            Assert.False(span.StartsWith(value, StringComparison.InvariantCulture));
            Assert.False(span.StartsWith(value, StringComparison.InvariantCultureIgnoreCase));

            s2 = "\u0049\u03b4\u03b4\u0049"; // I\u03B4\u03B4I
            Assert.False(s1.StartsWith(s2, StringComparison.OrdinalIgnoreCase));
            Assert.False(s1.StartsWith(s2, StringComparison.InvariantCulture));
            Assert.False(s1.StartsWith(s2, StringComparison.InvariantCultureIgnoreCase));

            value = s2.AsSpan();
            Assert.False(span.StartsWith(value, StringComparison.OrdinalIgnoreCase));
            Assert.False(span.StartsWith(value, StringComparison.InvariantCulture));
            Assert.False(span.StartsWith(value, StringComparison.InvariantCultureIgnoreCase));
        }

        // NOTE: This is by design. ICU ignores the null characters (i.e. null characters have no weights for the string comparison).
        // For desired behavior, use ordinal comparison instead of linguistic comparison.
        // This is a known difference between NLS and ICU (https://github.com/dotnet/runtime/issues/4673).
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNlsGlobalization))]
        [InlineData(StringComparison.CurrentCulture)]
        [InlineData(StringComparison.CurrentCultureIgnoreCase)]
        [InlineData(StringComparison.InvariantCulture)]
        [InlineData(StringComparison.InvariantCultureIgnoreCase)]
        public static void StartsWith_NullInStrings_NonOrdinal(StringComparison comparison)
        {
            Assert.False("\0test".StartsWith("test", comparison));
            Assert.False("te\0st".StartsWith("test", comparison));
            Assert.True("te\0st".StartsWith("te\0s", comparison));
            Assert.True("test\0".StartsWith("test", comparison));
            Assert.False("test".StartsWith("te\0", comparison));

            Assert.False("\0test".AsSpan().StartsWith("test".AsSpan(), comparison));
            Assert.False("te\0st".AsSpan().StartsWith("test".AsSpan(), comparison));
            Assert.True("te\0st".AsSpan().StartsWith("te\0s".AsSpan(), comparison));
            Assert.True("test\0".AsSpan().StartsWith("test".AsSpan(), comparison));
            Assert.False("test".AsSpan().StartsWith("te\0".AsSpan(), comparison));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)] // These tests assume that UTF8 is the default encoding
        public static unsafe void Ctor_SByte_InvalidUTF8()
        {
            byte[] invalidUTF8Bytes = new byte[] { (byte)'A', (byte)'B', 0xEE, (byte)'C', (byte)'D', 0 };

            fixed (byte* pBytes = invalidUTF8Bytes)
            {
                Assert.Equal("AB\ufffdCD", new string((sbyte*)pBytes));
                Assert.Equal("B\ufffdC", new string((sbyte*)pBytes, 1, 3));
            }
        }

        [Fact]
        public static unsafe void Ctor_SByte_InvalidArguments()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => new string ((sbyte*) null, 0, 1, Encoding.Default));
            AssertExtensions.Throws<ArgumentNullException>("value", () => new string ((sbyte*) null, 1, 1, Encoding.Default));

            AssertExtensions.Throws<ArgumentNullException>("value", () => new string ((sbyte*) null, 0, 1, null));
            AssertExtensions.Throws<ArgumentNullException>("value", () => new string ((sbyte*) null, 1, 1, null));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => new string((sbyte*)null, -1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => new string((sbyte*)null, -1, 0, Encoding.UTF8));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => new string((sbyte*)null, 0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => new string((sbyte*)null, 0, -1, Encoding.UTF8));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => new string(UIntPtr.Size == 4 ? (sbyte*)uint.MaxValue : (sbyte*)ulong.MaxValue, 42, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => new string(UIntPtr.Size == 4 ? (sbyte*)uint.MaxValue : (sbyte*)ulong.MaxValue, 42, 0, Encoding.UTF8));
        }

        [Fact]
        public static unsafe void Ctor_SByte_NullPointer_ReturnsEmptyString()
        {
            Assert.Equal(string.Empty, new string((sbyte*) null));

            Assert.Equal(string.Empty, new string((char*) null, 0, 0));

            Assert.Equal(string.Empty, new string((sbyte*) null, 0, 0));

            Assert.Equal(string.Empty, new string((sbyte*) null, 0, 0, Encoding.Default));
            Assert.Equal(string.Empty, new string((sbyte*) null, 1, 0, Encoding.Default));

            Assert.Equal(string.Empty, new string((sbyte*) null, 0, 0, null));
            Assert.Equal(string.Empty, new string((sbyte*) null, 1, 0, null));
        }

        [Fact]
        public static void CreateStringFromEncoding_0Length_EmptyStringReturned() // basic test for code coverage; more tests in encodings tests
        {
            Assert.Same(string.Empty, new AsciiEncodingWithZeroReturningGetCharCount().GetString("hello"u8.ToArray(), 0, 0));
        }

        private sealed class AsciiEncodingWithZeroReturningGetCharCount : ASCIIEncoding
        {
            public override int GetCharCount(byte[] bytes, int index, int count) => 0;
        }

        [Fact]
        public static unsafe void CloneTest()
        {
            string s = "some string to clone";
            string cloned = (string) s.Clone();
            Assert.Equal(s, cloned);
            Assert.True(object.ReferenceEquals(s, cloned), "cloned object should return same instance of the string");
        }

        [Fact]
        public static unsafe void CopyTest()
        {
#pragma warning disable 0618 // suppress obsolete warning for String.Copy
            AssertExtensions.Throws<ArgumentNullException>("str", () => string.Copy(null));

            string s = "some string to copy";
            string copy = string.Copy(s);
            Assert.Equal(s, copy);
            Assert.False(object.ReferenceEquals(s, copy), "copy should return new instance of the string");
#pragma warning restore 0618 // restore warning when accessing obsolete members
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtimelab/issues/155", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public static unsafe void InternTest()
        {
            AssertExtensions.Throws<ArgumentNullException>("str", () => string.Intern(null));
            AssertExtensions.Throws<ArgumentNullException>("str", () => string.IsInterned(null));

            string s1 = "MyTest";
            string s2 = new StringBuilder().Append("My").Append("Test").ToString();
            string s3 = string.Intern(s2);

            Assert.Equal(s1, s2);
            Assert.False(object.ReferenceEquals(s1, s2), "Created string from StringBuilder should have different reference than the literal string");
            Assert.True(object.ReferenceEquals(s1, s3), "Created intern string should have same reference as the literal string");

            Assert.True(String.IsInterned(s1).Equals(s1), "Expected to the literal string interned");
            Assert.True(String.IsInterned(s2).Equals(s1), "Expected to the interned string to be in the string pool now");
        }

        [Fact]
        public static void InternalTestAotSubset()
        {
#pragma warning disable 0618 // suppress obsolete warning for String.Copy
            string emptyFromField = string.Empty;
            string emptyFromInternTable = string.IsInterned(emptyFromField);
            Assert.Same(emptyFromInternTable, emptyFromField);

            string sTemplate = new string('A', 5);
            string sInterned1 = string.Intern(sTemplate);
            string sInterned2 = string.IsInterned(sInterned1);
            Assert.Equal(sTemplate, sInterned1);
            Assert.Same(sInterned1, sInterned2);
            string sNew = string.Copy(sInterned1);
            Assert.NotSame(sInterned1, sNew);
#pragma warning restore 0618 // restore warning when accessing obsolete members
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34577", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        public static unsafe void NormalizationTest() // basic test; more tests in globalization tests
        {
            // U+0063  LATIN SMALL LETTER C
            // U+0301  COMBINING ACUTE ACCENT
            // U+0327  COMBINING CEDILLA
            // U+00BE  VULGAR FRACTION THREE QUARTERS
            string s = new string( new char[] {'\u0063', '\u0301', '\u0327', '\u00BE'});

            Assert.False(s.IsNormalized(), "String should be not normalized when checking with the default which same as FormC");
            Assert.False(s.IsNormalized(NormalizationForm.FormC), "String should be not normalized when checking with FormC");
            Assert.False(s.IsNormalized(NormalizationForm.FormD), "String should be not normalized when checking with FormD");

            // Browser's, iOS's, MacCatalyst's, and tvOS's ICU do not support FormKC and FormKD
            bool supportsKCKD = !PlatformDetection.IsBrowser && !PlatformDetection.IsiOS && !PlatformDetection.IsMacCatalyst && !PlatformDetection.IstvOS;

            if (supportsKCKD)
            {
                Assert.False(s.IsNormalized(NormalizationForm.FormKC), "String should be not normalized when checking with FormKC");
                Assert.False(s.IsNormalized(NormalizationForm.FormKD), "String should be not normalized when checking with FormKD");
            }

            string normalized = s.Normalize(); // FormC
            Assert.True(normalized.IsNormalized(), "Expected to have the normalized string with default form FormC");
            Assert.True(normalized.IsNormalized(NormalizationForm.FormC), "Expected to have the normalized string with FormC");

            normalized = s.Normalize(NormalizationForm.FormC);
            Assert.True(normalized.IsNormalized(), "Expected to have the normalized string with default form FormC when using NormalizationForm.FormC");
            Assert.True(normalized.IsNormalized(NormalizationForm.FormC), "Expected to have the normalized string with FormC when using NormalizationForm.FormC");

            normalized = s.Normalize(NormalizationForm.FormD);
            Assert.True(normalized.IsNormalized(NormalizationForm.FormD), "Expected to have the normalized string with FormD");

            if (supportsKCKD)
            {
                normalized = s.Normalize(NormalizationForm.FormKC);
                Assert.True(normalized.IsNormalized(NormalizationForm.FormKC), "Expected to have the normalized string with FormKC");

                normalized = s.Normalize(NormalizationForm.FormKD);
                Assert.True(normalized.IsNormalized(NormalizationForm.FormKD), "Expected to have the normalized string with FormKD");
            }

            s = "hello";
            Assert.True(s.IsNormalized());
            Assert.True(s.IsNormalized(NormalizationForm.FormC));
            Assert.True(s.IsNormalized(NormalizationForm.FormD));

            if (supportsKCKD)
            {
                Assert.True(s.IsNormalized(NormalizationForm.FormKC));
                Assert.True(s.IsNormalized(NormalizationForm.FormKD));
            }

            Assert.Same(s, s.Normalize());
            Assert.Same(s, s.Normalize(NormalizationForm.FormC));
            Assert.Same(s, s.Normalize(NormalizationForm.FormD));

            if (supportsKCKD)
            {
                Assert.Same(s, s.Normalize(NormalizationForm.FormKC));
                Assert.Same(s, s.Normalize(NormalizationForm.FormKD));
            }
        }

        [Fact]
        public static unsafe void GetEnumeratorTest()
        {
            string s = "This is some string to enumerate its characters using String.GetEnumerator";
            CharEnumerator chEnum = s.GetEnumerator();

            int calculatedLength = 0;
            while (chEnum.MoveNext())
            {
                calculatedLength++;
            }

            Assert.Equal(s.Length, calculatedLength);
            chEnum.Reset();

            // enumerate twice in same time
            foreach (char c in s)
            {
                Assert.True(chEnum.MoveNext(), "expect to have characters to enumerate in the string");
                Assert.Equal(c, chEnum.Current);
            }

            Assert.False(chEnum.MoveNext(), "expect to not having any characters to enumerate");
        }

        [Fact]
        public static void IConvertible_ValuesRoundtripThroughString() // basic test; more coverage tests in Convert tests
        {
            CultureInfo c = CultureInfo.InvariantCulture;
            DateTime dt = new DateTime(2018, 6, 5, 1, 2, 0, DateTimeKind.Utc);

            Assert.True(((IConvertible)true.ToString(c)).ToBoolean(c));
            Assert.Equal(42, ((IConvertible)((byte)42).ToString(c)).ToByte(c));
            Assert.Equal(42, ((IConvertible)((char)42).ToString(c)).ToChar(c));
            Assert.Equal(dt, ((IConvertible)dt.ToString(c)).ToDateTime(c));
            Assert.Equal(42, ((IConvertible)(42m).ToString(c)).ToDecimal(c));
            Assert.Equal(42, ((IConvertible)(42.0).ToString(c)).ToDouble(c));
            Assert.Equal(42, ((IConvertible)((short)42).ToString(c)).ToInt16(c));
            Assert.Equal(42, ((IConvertible)((int)42).ToString(c)).ToInt32(c));
            Assert.Equal(42, ((IConvertible)((long)42).ToString(c)).ToInt64(c));
            Assert.Equal(42, ((IConvertible)((sbyte)42).ToString(c)).ToSByte(c));
            Assert.Equal(42, ((IConvertible)(42.0f).ToString(c)).ToSingle(c));
            Assert.Equal("42", ((IConvertible)"42".ToString(c)).ToString(c));
            Assert.Equal((ushort)42, ((IConvertible)((ushort)42).ToString(c)).ToUInt16(c));
            Assert.Equal((uint)42, ((IConvertible)((uint)42).ToString(c)).ToUInt32(c));
            Assert.Equal((ulong)42, ((IConvertible)((ulong)42).ToString(c)).ToUInt64(c));
        }

        private static IEnumerable<T[]> Permute<T>(T[] input)
        {
            if (input.Length <= 1)
            {
                yield return input;
                yield break;
            }

            for (int i = 0; i < input.Length; i++)
            {
                var remainder = new T[input.Length - 1];
                Array.Copy(input, remainder, i);
                Array.Copy(input, i + 1, remainder, i, input.Length - i - 1);
                foreach (T[] output in Permute(remainder))
                {
                    var result = new T[input.Length];
                    result[0] = input[i];
                    Array.Copy(output, 0, result, 1, output.Length);
                    yield return result;
                }
            }
        }
    }
}
