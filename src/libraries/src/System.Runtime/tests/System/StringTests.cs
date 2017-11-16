// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Tests
{
    public partial class StringTests : RemoteExecutorTestBase
    {
        private const string SoftHyphen = "\u00AD";

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
        [InlineData(new char[] { 'П', 'Р', 'И', 'В', 'Е', 'Т' }, 0, 6, "ПРИВЕТ")]
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
        }

        [Fact]
        public static void Item_Get_InvalidIndex_ThrowsIndexOutOfRangeException()
        {
            Assert.Throws<IndexOutOfRangeException>(() => "Hello"[-1]); // Index < 0
            Assert.Throws<IndexOutOfRangeException>(() => "Hello"[5]); // Index >= string.Length
            Assert.Throws<IndexOutOfRangeException>(() => ""[0]); // Index >= string.Length
        }

        [Theory]
        [InlineData("", 0)]
        [InlineData("\0", 1)]
        [InlineData("abc", 3)]
        [InlineData("hello", 5)]
        public static void Length(string s, int expected)
        {
            Assert.Equal(expected, s.Length);
        }

        public static IEnumerable<object[]> Concat_Strings_TestData()
        {
            yield return new object[] { new string[0], "" };

            yield return new object[] { new string[] { "1" }, "1" };
            yield return new object[] { new string[] { null }, "" };
            yield return new object[] { new string[] { "" }, "" };

            yield return new object[] { new string[] { "1", "2" }, "12" };
            yield return new object[] { new string[] { null, "1" }, "1" };
            yield return new object[] { new string[] { "", "1" }, "1" };
            yield return new object[] { new string[] { "1", null }, "1" };
            yield return new object[] { new string[] { "1", "" }, "1" };
            yield return new object[] { new string[] { null, null }, "" };
            yield return new object[] { new string[] { "", "" }, "" };

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

            yield return new object[] { new string[] { "abcd", "efgh", "ijkl", "mnop", "qrst", "uvwx", "yz" }, "abcdefghijklmnopqrstuvwxyz" };
        }

        [Theory]
        [MemberData(nameof(Concat_Strings_TestData))]
        public static void Concat_String(string[] values, string expected)
        {
            Action<string> validate = result =>
            {
                Assert.Equal(expected, result);
                if (result.Length == 0)
                {
                    // We return string.Empty by reference as an optimization
                    // in .NET core if there is no work to do.
                    if (PlatformDetection.IsFullFramework || PlatformDetection.IsNetNative)
                    {
                        Assert.Equal(string.Empty, result);
                    }
                    else
                    {
                        Assert.Same(string.Empty, result);
                    }
                }
            };

            if (values.Length == 2)
            {
                validate(string.Concat(values[0], values[1]));
            }
            else if (values.Length == 3)
            {
                validate(string.Concat(values[0], values[1], values[2]));
            }
            else if (values.Length == 4)
            {
                validate(string.Concat(values[0], values[1], values[2], values[3]));
            }

            validate(string.Concat(values));
            validate(string.Concat((IEnumerable<string>)values));
            validate(string.Concat<string>((IEnumerable<string>)values)); // Call the generic IEnumerable<T>-based overload
        }

        [Fact]
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
            // dotnet/coreclr#6785, this will be null for the Concat(object) overload but "" for the object[]/IEnumerable<object> overload
            // yield return new object[] { new object[] { new ObjectWithNullToString() }, "" };

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
        // CurrentCulture
        [InlineData("Hello", 0, "Hello", 0, 5, StringComparison.CurrentCulture, 0)]
        [InlineData("Hello", 0, "Goodbye", 0, 5, StringComparison.CurrentCulture, 1)]
        [InlineData("Goodbye", 0, "Hello", 0, 5, StringComparison.CurrentCulture, -1)]
        [InlineData("HELLO", 2, "hello", 2, 3, StringComparison.CurrentCulture, 1)]
        [InlineData("hello", 2, "HELLO", 2, 3, StringComparison.CurrentCulture, -1)]
        [InlineData("Hello", 2, "Hello", 2, 3, StringComparison.CurrentCulture, 0)]
        [InlineData("Hello", 2, "Goodbye", 2, 3, StringComparison.CurrentCulture, -1)]
        [InlineData("A", 0, "B", 0, 1, StringComparison.CurrentCulture, -1)]
        [InlineData("B", 0, "A", 0, 1, StringComparison.CurrentCulture, 1)]
        [InlineData(null, 0, null, 0, 0, StringComparison.CurrentCulture, 0)]
        [InlineData("Hello", 0, null, 0, 0, StringComparison.CurrentCulture, 1)]
        [InlineData(null, 0, "Hello", 0, 0, StringComparison.CurrentCulture, -1)]
        [InlineData(null, -1, null, -1, -1, StringComparison.CurrentCulture, 0)]
        [InlineData("foo", -1, null, -1, -1, StringComparison.CurrentCulture, 1)]
        [InlineData(null, -1, "foo", -1, -1, StringComparison.CurrentCulture, -1)]
        // CurrentCultureIgnoreCase
        [InlineData("HELLO", 0, "hello", 0, 5, StringComparison.CurrentCultureIgnoreCase, 0)]
        [InlineData("Hello", 0, "Hello", 0, 5, StringComparison.CurrentCultureIgnoreCase, 0)]
        [InlineData("Hello", 2, "Hello", 2, 3, StringComparison.CurrentCultureIgnoreCase, 0)]
        [InlineData("Hello", 2, "Yellow", 2, 3, StringComparison.CurrentCultureIgnoreCase, 0)]
        [InlineData("Hello", 0, "Goodbye", 0, 5, StringComparison.CurrentCultureIgnoreCase, 1)]
        [InlineData("Goodbye", 0, "Hello", 0, 5, StringComparison.CurrentCultureIgnoreCase, -1)]
        [InlineData("HELLO", 2, "hello", 2, 3, StringComparison.CurrentCultureIgnoreCase, 0)]
        [InlineData("Hello", 2, "Goodbye", 2, 3, StringComparison.CurrentCultureIgnoreCase, -1)]
        [InlineData(null, 0, null, 0, 0, StringComparison.CurrentCultureIgnoreCase, 0)]
        [InlineData("Hello", 0, null, 0, 0, StringComparison.CurrentCultureIgnoreCase, 1)]
        [InlineData(null, 0, "Hello", 0, 0, StringComparison.CurrentCultureIgnoreCase, -1)]
        [InlineData(null, -1, null, -1, -1, StringComparison.CurrentCultureIgnoreCase, 0)]
        [InlineData("foo", -1, null, -1, -1, StringComparison.CurrentCultureIgnoreCase, 1)]
        [InlineData(null, -1, "foo", -1, -1, StringComparison.CurrentCultureIgnoreCase, -1)]
        // InvariantCulture
        [InlineData("Hello", 0, "Hello", 0, 5, StringComparison.InvariantCulture, 0)]
        [InlineData("Hello", 0, "Goodbye", 0, 5, StringComparison.InvariantCulture, 1)]
        [InlineData("Goodbye", 0, "Hello", 0, 5, StringComparison.InvariantCulture, -1)]
        [InlineData("HELLO", 2, "hello", 2, 3, StringComparison.InvariantCulture, 1)]
        [InlineData("hello", 2, "HELLO", 2, 3, StringComparison.InvariantCulture, -1)]
        [InlineData(null, 0, null, 0, 0, StringComparison.InvariantCulture, 0)]
        [InlineData("Hello", 0, null, 0, 5, StringComparison.InvariantCulture, 1)]
        [InlineData(null, 0, "Hello", 0, 5, StringComparison.InvariantCulture, -1)]
        // InvariantCultureIgnoreCase
        [InlineData("HELLO", 0, "hello", 0, 5, StringComparison.InvariantCultureIgnoreCase, 0)]
        [InlineData("Hello", 0, "Hello", 0, 5, StringComparison.InvariantCultureIgnoreCase, 0)]
        [InlineData("Hello", 2, "Hello", 2, 3, StringComparison.InvariantCultureIgnoreCase, 0)]
        [InlineData("Hello", 2, "Yellow", 2, 3, StringComparison.InvariantCultureIgnoreCase, 0)]
        [InlineData("Hello", 0, "Goodbye", 0, 5, StringComparison.InvariantCultureIgnoreCase, 1)]
        [InlineData("Goodbye", 0, "Hello", 0, 5, StringComparison.InvariantCultureIgnoreCase, -1)]
        [InlineData("HELLO", 2, "hello", 2, 3, StringComparison.InvariantCultureIgnoreCase, 0)]
        [InlineData("Hello", 2, "Goodbye", 2, 3, StringComparison.InvariantCultureIgnoreCase, -1)]
        [InlineData(null, 0, null, 0, 0, StringComparison.InvariantCultureIgnoreCase, 0)]
        [InlineData("Hello", 0, null, 0, 5, StringComparison.InvariantCultureIgnoreCase, 1)]
        [InlineData(null, 0, "Hello", 0, 5, StringComparison.InvariantCultureIgnoreCase, -1)]
        // Ordinal
        [InlineData("Hello", 0, "Hello", 0, 5, StringComparison.Ordinal, 0)]
        [InlineData("Hello", 0, "Goodbye", 0, 5, StringComparison.Ordinal, 1)]
        [InlineData("Goodbye", 0, "Hello", 0, 5, StringComparison.Ordinal, -1)]
        [InlineData("Hello", 2, "Hello", 2, 3, StringComparison.Ordinal, 0)]
        [InlineData("HELLO", 2, "hello", 2, 3, StringComparison.Ordinal, -1)]
        [InlineData("Hello", 2, "Goodbye", 2, 3, StringComparison.Ordinal, -1)]
        [InlineData("Hello", 0, "Hello", 0, 0, StringComparison.Ordinal, 0)]
        [InlineData("Hello", 0, "Hello", 0, 5, StringComparison.Ordinal, 0)]
        [InlineData("Hello", 0, "Hello", 0, 3, StringComparison.Ordinal, 0)]
        [InlineData("Hello", 2, "Hello", 2, 3, StringComparison.Ordinal, 0)]
        [InlineData("Hello", 0, "He" + SoftHyphen + "llo", 0, 5, StringComparison.Ordinal, -1)]
        [InlineData("Hello", 0, "-=<Hello>=-", 3, 5, StringComparison.Ordinal, 0)]
        [InlineData("\uD83D\uDD53Hello\uD83D\uDD50", 1, "\uD83D\uDD53Hello\uD83D\uDD54", 1, 7, StringComparison.Ordinal, 0)] // Surrogate split
        [InlineData("Hello", 0, "Hello123", 0, int.MaxValue, StringComparison.Ordinal, -1)]           // Recalculated length, second string longer
        [InlineData("Hello123", 0, "Hello", 0, int.MaxValue, StringComparison.Ordinal, 1)]            // Recalculated length, first string longer
        [InlineData("---aaaaaaaaaaa", 3, "+++aaaaaaaaaaa", 3, 100, StringComparison.Ordinal, 0)]      // Equal long alignment 2, equal compare
        [InlineData("aaaaaaaaaaaaaa", 3, "aaaxaaaaaaaaaa", 3, 100, StringComparison.Ordinal, -1)]     // Equal long alignment 2, different compare at n=1
        [InlineData("-aaaaaaaaaaaaa", 1, "+aaaaaaaaaaaaa", 1, 100, StringComparison.Ordinal, 0)]      // Equal long alignment 6, equal compare
        [InlineData("aaaaaaaaaaaaaa", 1, "axaaaaaaaaaaaa", 1, 100, StringComparison.Ordinal, -1)]     // Equal long alignment 6, different compare at n=1
        [InlineData("aaaaaaaaaaaaaa", 0, "aaaaaaaaaaaaaa", 0, 100, StringComparison.Ordinal, 0)]      // Equal long alignment 4, equal compare
        [InlineData("aaaaaaaaaaaaaa", 0, "xaaaaaaaaaaaaa", 0, 100, StringComparison.Ordinal, -1)]     // Equal long alignment 4, different compare at n=1
        [InlineData("aaaaaaaaaaaaaa", 0, "axaaaaaaaaaaaa", 0, 100, StringComparison.Ordinal, -1)]     // Equal long alignment 4, different compare at n=2
        [InlineData("--aaaaaaaaaaaa", 2, "++aaaaaaaaaaaa", 2, 100, StringComparison.Ordinal, 0)]      // Equal long alignment 0, equal compare
        [InlineData("aaaaaaaaaaaaaa", 2, "aaxaaaaaaaaaaa", 2, 100, StringComparison.Ordinal, -1)]     // Equal long alignment 0, different compare at n=1
        [InlineData("aaaaaaaaaaaaaa", 2, "aaaxaaaaaaaaaa", 2, 100, StringComparison.Ordinal, -1)]     // Equal long alignment 0, different compare at n=2
        [InlineData("aaaaaaaaaaaaaa", 2, "aaaaxaaaaaaaaa", 2, 100, StringComparison.Ordinal, -1)]     // Equal long alignment 0, different compare at n=3
        [InlineData("aaaaaaaaaaaaaa", 2, "aaaaaxaaaaaaaa", 2, 100, StringComparison.Ordinal, -1)]     // Equal long alignment 0, different compare at n=4
        [InlineData("aaaaaaaaaaaaaa", 2, "aaaaaaxaaaaaaa", 2, 100, StringComparison.Ordinal, -1)]     // Equal long alignment 0, different compare at n=5
        [InlineData("aaaaaaaaaaaaaa", 0, "+aaaaaaaaaaaaa", 1, 13, StringComparison.Ordinal, 0)]       // Different int alignment, equal compare
        [InlineData("aaaaaaaaaaaaaa", 0, "aaaaaaaaaaaaax", 1, 100, StringComparison.Ordinal, -1)]     // Different int alignment
        [InlineData("aaaaaaaaaaaaaa", 1, "aaaxaaaaaaaaaa", 3, 100, StringComparison.Ordinal, -1)]     // Different long alignment, abs of 4, one of them is 2, different at n=1
        [InlineData("-aaaaaaaaaaaaa", 1, "++++aaaaaaaaaa", 4, 10, StringComparison.Ordinal, 0)]       // Different long alignment, equal compare
        [InlineData("aaaaaaaaaaaaaa", 1, "aaaaaaaaaaaaax", 4, 100, StringComparison.Ordinal, -1)]     // Different long alignment
        [InlineData("\0", 0, "", 0, 1, StringComparison.Ordinal, 1)]                                  // Same memory layout, except for m_stringLength (m_firstChars are both 0)
        [InlineData("\0\0", 0, "", 0, 2, StringComparison.Ordinal, 1)]                                // Same as above, except m_stringLength for one is 2
        [InlineData("", 0, "\0b", 0, 2, StringComparison.Ordinal, -1)]                                // strA's second char != strB's second char codepath
        [InlineData("", 0, "b", 0, 1, StringComparison.Ordinal, -1)]                                  // Should hit strA.m_firstChar != strB.m_firstChar codepath
        [InlineData("abcxxxxxxxxxxxxxxxxxxxxxx", 0, "abdxxxxxxxxxxxxxxx", 0, int.MaxValue, StringComparison.Ordinal, -1)] // 64-bit: first long compare is different
        [InlineData("abcdefgxxxxxxxxxxxxxxxxxx", 0, "abcdefhxxxxxxxxxxx", 0, int.MaxValue, StringComparison.Ordinal, -1)] // 64-bit: second long compare is different
        [InlineData("abcdefghijkxxxxxxxxxxxxxx", 0, "abcdefghijlxxxxxxx", 0, int.MaxValue, StringComparison.Ordinal, -1)] // 64-bit: third long compare is different
        [InlineData("abcdexxxxxxxxxxxxxxxxxxxx", 0, "abcdfxxxxxxxxxxxxx", 0, int.MaxValue, StringComparison.Ordinal, -1)] // 32-bit: second int compare is different
        [InlineData("abcdefghixxxxxxxxxxxxxxxx", 0, "abcdefghjxxxxxxxxx", 0, int.MaxValue, StringComparison.Ordinal, -1)] // 32-bit: fourth int compare is different
        [InlineData(null, 0, null, 0, 0, StringComparison.Ordinal, 0)]
        [InlineData("Hello", 0, null, 0, 5, StringComparison.Ordinal, 1)]
        [InlineData(null, 0, "Hello", 0, 5, StringComparison.Ordinal, -1)]
        [InlineData(null, -1, null, -1, -1, StringComparison.Ordinal, 0)]
        [InlineData("foo", -1, null, -1, -1, StringComparison.Ordinal, 1)]
        [InlineData(null, -1, "foo", -1, -1, StringComparison.Ordinal, -1)]
        // OrdinalIgnoreCase
        [InlineData("HELLO", 0, "hello", 0, 5, StringComparison.OrdinalIgnoreCase, 0)]
        [InlineData("Hello", 0, "Hello", 0, 5, StringComparison.OrdinalIgnoreCase, 0)]
        [InlineData("Hello", 2, "Hello", 2, 3, StringComparison.OrdinalIgnoreCase, 0)]
        [InlineData("Hello", 2, "Yellow", 2, 3, StringComparison.OrdinalIgnoreCase, 0)]
        [InlineData("Hello", 0, "Goodbye", 0, 5, StringComparison.OrdinalIgnoreCase, 1)]
        [InlineData("Goodbye", 0, "Hello", 0, 5, StringComparison.OrdinalIgnoreCase, -1)]
        [InlineData("HELLO", 2, "hello", 2, 3, StringComparison.OrdinalIgnoreCase, 0)]
        [InlineData("Hello", 2, "Goodbye", 2, 3, StringComparison.OrdinalIgnoreCase, -1)]
        [InlineData("A", 0, "x", 0, 1, StringComparison.OrdinalIgnoreCase, -1)]
        [InlineData("a", 0, "X", 0, 1, StringComparison.OrdinalIgnoreCase, -1)]
        [InlineData("[", 0, "A", 0, 1, StringComparison.OrdinalIgnoreCase, 1)]
        [InlineData("[", 0, "a", 0, 1, StringComparison.OrdinalIgnoreCase, 1)]
        [InlineData("\\", 0, "A", 0, 1, StringComparison.OrdinalIgnoreCase, 1)]
        [InlineData("\\", 0, "a", 0, 1, StringComparison.OrdinalIgnoreCase, 1)]
        [InlineData("]", 0, "A", 0, 1, StringComparison.OrdinalIgnoreCase, 1)]
        [InlineData("]", 0, "a", 0, 1, StringComparison.OrdinalIgnoreCase, 1)]
        [InlineData("^", 0, "A", 0, 1, StringComparison.OrdinalIgnoreCase, 1)]
        [InlineData("^", 0, "a", 0, 1, StringComparison.OrdinalIgnoreCase, 1)]
        [InlineData("_", 0, "A", 0, 1, StringComparison.OrdinalIgnoreCase, 1)]
        [InlineData("_", 0, "a", 0, 1, StringComparison.OrdinalIgnoreCase, 1)]
        [InlineData("`", 0, "A", 0, 1, StringComparison.OrdinalIgnoreCase, 1)]
        [InlineData("`", 0, "a", 0, 1, StringComparison.OrdinalIgnoreCase, 1)]
        [InlineData(null, 0, null, 0, 0, StringComparison.OrdinalIgnoreCase, 0)]
        [InlineData("Hello", 0, null, 0, 5, StringComparison.OrdinalIgnoreCase, 1)]
        [InlineData(null, 0, "Hello", 0, 5, StringComparison.OrdinalIgnoreCase, -1)]
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
                    // Uncomment when this is exposed in .NET Core (dotnet/corefx#10066)
                    // Assert.Equal(expected, Math.Sign(string.Compare(strA, indexA, strB, indexB, length, ignoreCase: false)));
                }
            }
            else if (comparisonType == StringComparison.CurrentCultureIgnoreCase)
            {
                // This may have different behavior than the overload accepting a StringComparison
                // for a combination of null/invalid inputs; see notes in Compare_Invalid for more

                if (!skipNonComparisonOverloads)
                {
                    // Use Compare(string, int, string, int, int, true)
                    // Uncomment when this is exposed in .NET Core (dotnet/corefx#10066)
                    // Assert.Equal(expected, Math.Sign(string.Compare(strA, indexA, strB, indexB, length, ignoreCase: true)));
                }
            }
            else if (comparisonType == StringComparison.Ordinal)
            {
                // Use CompareOrdinal(string, int, string, int, int)
                Assert.Equal(expected, Math.Sign(string.CompareOrdinal(strA, indexA, strB, indexB, length)));
            }
            // Use Compare(string, int, string, int, int, StringComparison)
            Assert.Equal(expected, Math.Sign(string.Compare(strA, indexA, strB, indexB, length, comparisonType)));
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
        public static void Contains(string s, string value, bool expected)
        {
            Assert.Equal(expected, s.Contains(value));
        }

        [Fact]
        public static void Contains_NullValue_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => "foo".Contains(null));
        }

        [Theory]
        // CurrentCulture
        [InlineData("", "Foo", StringComparison.CurrentCulture, false)]
        [InlineData("Hello", "llo", StringComparison.CurrentCulture, true)]
        [InlineData("Hello", "Hello", StringComparison.CurrentCulture, true)]
        [InlineData("Hello", "", StringComparison.CurrentCulture, true)]
        [InlineData("Hello", "HELLO", StringComparison.CurrentCulture, false)]
        [InlineData("Hello", "Abc", StringComparison.CurrentCulture, false)]
        [InlineData("Hello", "llo" + SoftHyphen, StringComparison.CurrentCulture, true)]
        [InlineData("", "", StringComparison.CurrentCulture, true)]
        [InlineData("", "a", StringComparison.CurrentCulture, false)]
        // CurrentCultureIgnoreCase
        [InlineData("Hello", "llo", StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("Hello", "Hello", StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("Hello", "", StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("Hello", "LLO", StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("Hello", "Abc", StringComparison.CurrentCultureIgnoreCase, false)]
        [InlineData("Hello", "llo" + SoftHyphen, StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("", "", StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("", "a", StringComparison.CurrentCultureIgnoreCase, false)]
        // InvariantCulture
        [InlineData("", "Foo", StringComparison.InvariantCulture, false)]
        [InlineData("Hello", "llo", StringComparison.InvariantCulture, true)]
        [InlineData("Hello", "Hello", StringComparison.InvariantCulture, true)]
        [InlineData("Hello", "", StringComparison.InvariantCulture, true)]
        [InlineData("Hello", "HELLO", StringComparison.InvariantCulture, false)]
        [InlineData("Hello", "Abc", StringComparison.InvariantCulture, false)]
        [InlineData("Hello", "llo" + SoftHyphen, StringComparison.InvariantCulture, true)]
        [InlineData("", "", StringComparison.InvariantCulture, true)]
        [InlineData("", "a", StringComparison.InvariantCulture, false)]
        // InvariantCultureIgnoreCase
        [InlineData("Hello", "llo", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Hello", "Hello", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Hello", "", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Hello", "LLO", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Hello", "Abc", StringComparison.InvariantCultureIgnoreCase, false)]
        [InlineData("Hello", "llo" + SoftHyphen, StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("", "", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("", "a", StringComparison.InvariantCultureIgnoreCase, false)]
        // Ordinal
        [InlineData("Hello", "o", StringComparison.Ordinal, true)]
        [InlineData("Hello", "llo", StringComparison.Ordinal, true)]
        [InlineData("Hello", "Hello", StringComparison.Ordinal, true)]
        [InlineData("Hello", "Larger Hello", StringComparison.Ordinal, false)]
        [InlineData("Hello", "", StringComparison.Ordinal, true)]
        [InlineData("Hello", "LLO", StringComparison.Ordinal, false)]
        [InlineData("Hello", "Abc", StringComparison.Ordinal, false)]
        [InlineData("Hello", "llo" + SoftHyphen, StringComparison.Ordinal, false)]
        [InlineData("", "", StringComparison.Ordinal, true)]
        [InlineData("", "a", StringComparison.Ordinal, false)]
        // OrdinalIgnoreCase
        [InlineData("Hello", "llo", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("Hello", "Hello", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("Hello", "Larger Hello", StringComparison.OrdinalIgnoreCase, false)]
        [InlineData("Hello", "", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("Hello", "LLO", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("Hello", "Abc", StringComparison.OrdinalIgnoreCase, false)]
        [InlineData("Hello", "llo" + SoftHyphen, StringComparison.OrdinalIgnoreCase, false)]
        [InlineData("", "", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("", "a", StringComparison.OrdinalIgnoreCase, false)]
        public static void EndsWith(string s, string value, StringComparison comparisonType, bool expected)
        {
            if (comparisonType == StringComparison.CurrentCulture)
            {
                Assert.Equal(expected, s.EndsWith(value));
            }
            Assert.Equal(expected, s.EndsWith(value, comparisonType));
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/coreclr/issues/2051", TestPlatforms.AnyUnix)]
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
        }

        [Fact]
        public static void EndsWith_Invalid()
        {
            // Value is null
            AssertExtensions.Throws<ArgumentNullException>("value", () => "foo".EndsWith(null));
            AssertExtensions.Throws<ArgumentNullException>("value", () => "foo".EndsWith(null, StringComparison.CurrentCulture));

            // Invalid comparison type
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "foo".EndsWith("", StringComparison.CurrentCulture - 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "foo".EndsWith("", StringComparison.OrdinalIgnoreCase + 1));
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
        [InlineData("123", 123, StringComparison.InvariantCultureIgnoreCase, false)] // Not a string
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
        public static void Equals(string s1, object obj, StringComparison comparisonType, bool expected)
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
        }

        public static IEnumerable<object[]> Equals_EncyclopaediaData()
        {
            yield return new object[] { StringComparison.CurrentCulture, false };
            yield return new object[] { StringComparison.CurrentCultureIgnoreCase, false };
            yield return new object[] { StringComparison.Ordinal, false };
            yield return new object[] { StringComparison.OrdinalIgnoreCase, false };

            // Windows and ICU disagree about how these strings compare in the default locale.
            yield return new object[] { StringComparison.InvariantCulture, PlatformDetection.IsWindows };
            yield return new object[] { StringComparison.InvariantCultureIgnoreCase, PlatformDetection.IsWindows };
        }

        [Theory]
        [MemberData(nameof(Equals_EncyclopaediaData))]
        public void Equals_Encyclopaedia_ReturnsExpected(StringComparison comparison, bool expected)
        {
            RemoteInvoke((comparisonString, expectedString) =>
            {
                string source = "encyclop\u00e6dia";
                string target = "encyclopaedia";

                CultureInfo.CurrentCulture = new CultureInfo("se-SE");
                StringComparison comparisonType = (StringComparison)Enum.Parse(typeof(StringComparison), comparisonString);
                Assert.Equal(bool.Parse(expectedString), string.Equals(source, target, comparisonType));

                return SuccessExitCode;
            }, comparison.ToString(), expected.ToString());
        }

        [Theory]
        [InlineData(StringComparison.CurrentCulture - 1)]
        [InlineData(StringComparison.OrdinalIgnoreCase + 1)]
        public static void Equals_InvalidComparisonType_ThrowsArgumentOutOfRangeException(StringComparison comparisonType)
        {
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => string.Equals("a", "b", comparisonType));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "a".Equals("a", comparisonType));
        }

        [Fact]
        public static void Format()
        {
            string s = string.Format(null, "0 = {0} 1 = {1} 2 = {2} 3 = {3} 4 = {4}", "zero", "one", "two", "three", "four");
            Assert.Equal("0 = zero 1 = one 2 = two 3 = three 4 = four", s);

            var testFormatter = new TestFormatter();
            s = string.Format(testFormatter, "0 = {0} 1 = {1} 2 = {2} 3 = {3} 4 = {4}", "zero", "one", "two", "three", "four");
            Assert.Equal("0 = Test: : zero 1 = Test: : one 2 = Test: : two 3 = Test: : three 4 = Test: : four", s);
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

            // Format has value < 0
            Assert.Throws<FormatException>(() => string.Format("{-1}", obj1));
            Assert.Throws<FormatException>(() => string.Format("{-1}", obj1, obj2));
            Assert.Throws<FormatException>(() => string.Format("{-1}", obj1, obj2, obj3));
            Assert.Throws<FormatException>(() => string.Format("{-1}", obj1, obj2, obj3, obj4));
            Assert.Throws<FormatException>(() => string.Format(formatter, "{-1}", obj1));
            Assert.Throws<FormatException>(() => string.Format(formatter, "{-1}", obj1, obj2));
            Assert.Throws<FormatException>(() => string.Format(formatter, "{-1}", obj1, obj2, obj3));
            Assert.Throws<FormatException>(() => string.Format(formatter, "{-1}", obj1, obj2, obj3, obj4));

            // Format has out of range value
            Assert.Throws<FormatException>(() => string.Format("{1}", obj1));
            Assert.Throws<FormatException>(() => string.Format("{2}", obj1, obj2));
            Assert.Throws<FormatException>(() => string.Format("{3}", obj1, obj2, obj3));
            Assert.Throws<FormatException>(() => string.Format("{4}", obj1, obj2, obj3, obj4));
            Assert.Throws<FormatException>(() => string.Format(formatter, "{1}", obj1));
            Assert.Throws<FormatException>(() => string.Format(formatter, "{2}", obj1, obj2));
            Assert.Throws<FormatException>(() => string.Format(formatter, "{3}", obj1, obj2, obj3));
            Assert.Throws<FormatException>(() => string.Format(formatter, "{4}", obj1, obj2, obj3, obj4));
        }

        [Theory]
        [InlineData("Hello", 'l', 0, 5, 2)]
        [InlineData("Hello", 'x', 0, 5, -1)]
        [InlineData("Hello", 'l', 1, 4, 2)]
        [InlineData("Hello", 'l', 3, 2, 3)]
        [InlineData("Hello", 'l', 4, 1, -1)]
        [InlineData("Hello", 'x', 1, 4, -1)]
        [InlineData("Hello", 'l', 3, 0, -1)]
        [InlineData("Hello", 'l', 0, 2, -1)]
        [InlineData("Hello", 'l', 0, 3, 2)]
        [InlineData("Hello", 'l', 4, 1, -1)]
        [InlineData("Hello", 'x', 1, 4, -1)]
        [InlineData("Hello", 'o', 5, 0, -1)]
        [InlineData("H" + SoftHyphen + "ello", 'e', 0, 3, 2)]
        // For some reason, this is failing on *nix with ordinal comparisons.
        // Possibly related issue: dotnet/coreclr#2051
        // [InlineData("Hello", '\0', 0, 5, -1)] // .NET strings are terminated with a null character, but they should not be included as part of the string
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
        public static void IndexOf_SingleLetter(string s, char target, int startIndex, int count, int expected)
        {
            bool safeForCurrentCulture =
                IsSafeForCurrentCultureComparisons(s)
                && IsSafeForCurrentCultureComparisons(target.ToString());

            if (count + startIndex == s.Length)
            {
                if (startIndex == 0)
                {
                    Assert.Equal(expected, s.IndexOf(target));
                    Assert.Equal(expected, s.IndexOf(target.ToString(), StringComparison.Ordinal));
                    Assert.Equal(expected, s.IndexOf(target.ToString(), StringComparison.OrdinalIgnoreCase));

                    // To be safe we only want to run CurrentCulture comparisons if
                    // we know the results will not vary depending on location
                    if (safeForCurrentCulture)
                    {
                        Assert.Equal(expected, s.IndexOf(target.ToString()));
                        Assert.Equal(expected, s.IndexOf(target.ToString(), StringComparison.CurrentCulture));
                    }
                }
                Assert.Equal(expected, s.IndexOf(target, startIndex));
                Assert.Equal(expected, s.IndexOf(target.ToString(), startIndex, StringComparison.Ordinal));
                Assert.Equal(expected, s.IndexOf(target.ToString(), startIndex, StringComparison.OrdinalIgnoreCase));

                if (safeForCurrentCulture)
                {
                    Assert.Equal(expected, s.IndexOf(target.ToString(), startIndex));
                    Assert.Equal(expected, s.IndexOf(target.ToString(), startIndex, StringComparison.CurrentCulture));
                }
            }
            Assert.Equal(expected, s.IndexOf(target, startIndex, count));
            Assert.Equal(expected, s.IndexOf(target.ToString(), startIndex, count, StringComparison.Ordinal));
            Assert.Equal(expected, s.IndexOf(target.ToString(), startIndex, count, StringComparison.OrdinalIgnoreCase));

            if (safeForCurrentCulture)
            {
                Assert.Equal(expected, s.IndexOf(target.ToString(), startIndex, count));
                Assert.Equal(expected, s.IndexOf(target.ToString(), startIndex, count, StringComparison.CurrentCulture));
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

        [Theory]
        [ActiveIssue("https://github.com/dotnet/coreclr/issues/2051", TestPlatforms.AnyUnix)]
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
        }

        [Theory]
        [MemberData(nameof(AllSubstringsAndComparisons), new object[] { "abcde" })]
        public static void IndexOf_AllSubstrings(string s, string value, int startIndex, StringComparison comparison)
        {
            bool ignoringCase = comparison == StringComparison.OrdinalIgnoreCase || comparison == StringComparison.CurrentCultureIgnoreCase;

            // First find the substring.  We should be able to with all comparison types.
            Assert.Equal(startIndex, s.IndexOf(value, comparison)); // in the whole string
            Assert.Equal(startIndex, s.IndexOf(value, startIndex, comparison)); // starting at substring
            if (startIndex > 0)
            {
                Assert.Equal(startIndex, s.IndexOf(value, startIndex - 1, comparison)); // starting just before substring
            }
            Assert.Equal(-1, s.IndexOf(value, startIndex + 1, comparison)); // starting just after start of substring

            // Shouldn't be able to find the substring if the count is less than substring's length
            Assert.Equal(-1, s.IndexOf(value, 0, value.Length - 1, comparison));

            // Now double the source.  Make sure we find the first copy of the substring.
            int halfLen = s.Length;
            s += s;
            Assert.Equal(startIndex, s.IndexOf(value, comparison));

            // Now change the case of a letter.
            s = s.ToUpperInvariant();
            Assert.Equal(ignoringCase ? startIndex : -1, s.IndexOf(value, comparison));
        }

        [Fact]
        public static void IndexOf_TurkishI_TurkishCulture()
        {
            RemoteInvoke(() =>
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

                string s = "Turkish I \u0131s TROUBL\u0130NG!";
                string value = "\u0130";
                Assert.Equal(19, s.IndexOf(value));
                Assert.Equal(19, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(4, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(19, s.IndexOf(value, StringComparison.Ordinal));
                Assert.Equal(19, s.IndexOf(value, StringComparison.OrdinalIgnoreCase));

                value = "\u0131";
                Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(8, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(10, s.IndexOf(value, StringComparison.Ordinal));
                Assert.Equal(10, s.IndexOf(value, StringComparison.OrdinalIgnoreCase));

                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public static void IndexOf_TurkishI_InvariantCulture()
        {
            RemoteInvoke(() =>
            {
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

                string s = "Turkish I \u0131s TROUBL\u0130NG!";
                string value = "\u0130";
                
                Assert.Equal(19, s.IndexOf(value));
                Assert.Equal(19, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(19, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));

                value = "\u0131";
                Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));

                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public static void IndexOf_TurkishI_EnglishUSCulture()
        {
            RemoteInvoke(() =>
            {
                CultureInfo.CurrentCulture = new CultureInfo("en-US");

                string s = "Turkish I \u0131s TROUBL\u0130NG!";
                string value = "\u0130";

                value = "\u0130";
                Assert.Equal(19, s.IndexOf(value));
                Assert.Equal(19, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(19, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));

                value = "\u0131";
                Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));

                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public static void IndexOf_HungarianDoubleCompression_HungarianCulture()
        {
            RemoteInvoke(() =>
            {
                string source = "dzsdzs";
                string target = "ddzs";

                CultureInfo.CurrentCulture = new CultureInfo("hu-HU");
                /*
                 There are differences between Windows and ICU regarding contractions.
                 Windows has equal contraction collation weights, including case (target="Ddzs" same behavior as "ddzs").
                 ICU has different contraction collation weights, depending on locale collation rules.
                 If CurrentCultureIgnoreCase is specified, ICU will use 'secondary' collation rules
                 which ignore the contraction collation weights (defined as 'tertiary' rules)
                */
                Assert.Equal(PlatformDetection.IsWindows ? 0 : -1, source.IndexOf(target));
                Assert.Equal(PlatformDetection.IsWindows ? 0 : -1, source.IndexOf(target, StringComparison.CurrentCulture));

                Assert.Equal(0, source.IndexOf(target, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(-1, source.IndexOf(target, StringComparison.Ordinal));
                Assert.Equal(-1, source.IndexOf(target, StringComparison.OrdinalIgnoreCase));

                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public static void IndexOf_HungarianDoubleCompression_InvariantCulture()
        {
            RemoteInvoke(() =>
            {
                string source = "dzsdzs";
                string target = "ddzs";

                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                Assert.Equal(-1, source.IndexOf(target));
                Assert.Equal(-1, source.IndexOf(target, StringComparison.CurrentCulture));
                Assert.Equal(-1, source.IndexOf(target, StringComparison.CurrentCultureIgnoreCase));

                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public static void IndexOf_EquivalentDiacritics_EnglishUSCulture()
        {
            RemoteInvoke(() =>
            {
                string s = "Exhibit a\u0300\u00C0";
                string value = "\u00C0";

                CultureInfo.CurrentCulture = new CultureInfo("en-US");
                Assert.Equal(10, s.IndexOf(value));
                Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(8, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(10, s.IndexOf(value, StringComparison.Ordinal));
                Assert.Equal(10, s.IndexOf(value, StringComparison.OrdinalIgnoreCase));

                value = "a\u0300"; // this diacritic combines with preceding character
                Assert.Equal(8, s.IndexOf(value));
                Assert.Equal(8, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(8, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(8, s.IndexOf(value, StringComparison.Ordinal));
                Assert.Equal(8, s.IndexOf(value, StringComparison.OrdinalIgnoreCase));

                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public static void IndexOf_EquivalentDiacritics_InvariantCulture()
        {
            RemoteInvoke(() =>
            {
                string s = "Exhibit a\u0300\u00C0";
                string value = "\u00C0";

                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                Assert.Equal(10, s.IndexOf(value));
                Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(8, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));

                value = "a\u0300"; // this diacritic combines with preceding character
                Assert.Equal(8, s.IndexOf(value));
                Assert.Equal(8, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(8, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));

                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public static void IndexOf_CyrillicE_EnglishUSCulture()
        {
            RemoteInvoke(() =>
            {
                string s = "Foo\u0400Bar";
                string value = "\u0400";

                CultureInfo.CurrentCulture = new CultureInfo("en-US");
                Assert.Equal(3, s.IndexOf(value));
                Assert.Equal(3, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(3, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(3, s.IndexOf(value, StringComparison.Ordinal));
                Assert.Equal(3, s.IndexOf(value, StringComparison.OrdinalIgnoreCase));

                value = "bar";
                Assert.Equal(-1, s.IndexOf(value));
                Assert.Equal(-1, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(4, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(-1, s.IndexOf(value, StringComparison.Ordinal));
                Assert.Equal(4, s.IndexOf(value, StringComparison.OrdinalIgnoreCase));

                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public static void IndexOf_CyrillicE_InvariantCulture()
        {
            RemoteInvoke(() =>
            {
                string s = "Foo\u0400Bar";
                string value = "\u0400";

                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                Assert.Equal(3, s.IndexOf(value));
                Assert.Equal(3, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(3, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));

                value = "bar";
                Assert.Equal(-1, s.IndexOf(value));
                Assert.Equal(-1, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(4, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));

                return SuccessExitCode;
            }).Dispose();
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
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "foo".IndexOf("o", StringComparison.OrdinalIgnoreCase + 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "foo".IndexOf("o", 0, StringComparison.CurrentCulture - 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "foo".IndexOf("o", 0, StringComparison.OrdinalIgnoreCase + 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "foo".IndexOf("o", 0, 0, StringComparison.CurrentCulture - 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "foo".IndexOf("o", 0, 0, StringComparison.OrdinalIgnoreCase + 1));
        }

        [Theory]
        [InlineData("Hello", new char[] { 'd', 'o', 'l' }, 0, 5, 2)]
        [InlineData("Hello", new char[] { 'd', 'e', 'H' }, 0, 0, -1)]
        [InlineData("Hello", new char[] { 'd', 'e', 'f' }, 1, 3, 1)]
        [InlineData("Hello", new char[] { 'a', 'b', 'c' }, 2, 3, -1)]
        [InlineData("Hello", new char[0], 2, 3, -1)]
        [InlineData("H" + SoftHyphen + "ello", new char[] { 'a', '\u00AD', 'c' }, 0, 2, 1)]
        [InlineData("", new char[] { 'd', 'e', 'f' }, 0, 0, -1)]
        [InlineData("Hello", new char[] { 'o', 'l' }, 0, 5, 2)]
        [InlineData("Hello", new char[] { 'e', 'H' }, 0, 0, -1)]
        [InlineData("Hello", new char[] { 'd', 'e' }, 1, 3, 1)]
        [InlineData("Hello", new char[] { 'a', 'b' }, 2, 3, -1)]
        [InlineData("", new char[] { 'd', 'e' }, 0, 0, -1)]
        [InlineData("Hello", new char[] { '\0', 'b' }, 0, 5, -1)]    // Null terminator check, odd
        [InlineData("xHello", new char[] { '\0', 'b' }, 0, 6, -1)]   // Null terminator check, even
        [InlineData("Hello", new char[] { '\0', 'o' }, 0, 5, 4)]     // Match last char, odd
        [InlineData("xHello", new char[] { '\0', 'o' }, 0, 6, 5)]    // Match last char, even
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
            Assert.Equal(expected, s.IndexOfAny(anyOf, startIndex, count));
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
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework | TargetFrameworkMonikers.Uap)]
        public static void Join_ObjectArray(string separator, object[] values, string expected)
        {
            Assert.Equal(expected, string.Join(separator, values));
            Assert.Equal(expected, string.Join(separator, (IEnumerable<object>)values));
        }

        [Theory]
        [MemberData(nameof(Join_ObjectArray_TestData))]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public static void Join_ObjectArray_WithNullIssue(string separator, object[] values, string expected)
        {
            string enumerableExpected = expected;
            if (values.Length > 0 && values[0] == null) // Join return nothing when first value is null
                expected = "";
            Assert.Equal(expected, string.Join(separator, values));
            Assert.Equal(enumerableExpected, string.Join(separator, (IEnumerable<object>)values));
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
        [InlineData("Hello", 'l', 3, 4, 3)]
        [InlineData("Hello", 'l', 1, 2, -1)]
        [InlineData("Hello", 'l', 1, 0, -1)]
        [InlineData("Hello", 'l', 4, 2, 3)]
        [InlineData("Hello", 'l', 4, 3, 3)]
        [InlineData("Hello", 'l', 0, 1, -1)]
        [InlineData("Hello", 'x', 3, 4, -1)]
        [InlineData("H" + SoftHyphen + "ello", 'H', 2, 3, 0)]
        public static void LastIndexOf_SingleLetter(string s, char value, int startIndex, int count, int expected)
        {
            if (count == s.Length)
            {
                if (startIndex == s.Length - 1)
                {
                    Assert.Equal(expected, s.LastIndexOf(value));
                    Assert.Equal(expected, s.LastIndexOf(value.ToString()));
                }
                Assert.Equal(expected, s.LastIndexOf(value, startIndex));
                Assert.Equal(expected, s.LastIndexOf(value.ToString(), startIndex));
            }
            Assert.Equal(expected, s.LastIndexOf(value, startIndex, count));
            Assert.Equal(expected, s.LastIndexOf(value.ToString(), startIndex, count));

            Assert.Equal(expected, s.LastIndexOf(value.ToString(), startIndex, count, StringComparison.CurrentCulture));
            Assert.Equal(expected, s.LastIndexOf(value.ToString(), startIndex, count, StringComparison.Ordinal));
            Assert.Equal(expected, s.LastIndexOf(value.ToString(), startIndex, count, StringComparison.OrdinalIgnoreCase));
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/coreclr/issues/2051", TestPlatforms.AnyUnix)]
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
        public static void LastIndexOf_AllSubstrings(string s, string value, int startIndex, StringComparison comparisonType)
        {
            bool ignoringCase = comparisonType == StringComparison.OrdinalIgnoreCase || comparisonType == StringComparison.CurrentCultureIgnoreCase;

            // First find the substring.  We should be able to with all comparison types.
            Assert.Equal(startIndex, s.LastIndexOf(value, comparisonType)); // in the whole string
            Assert.Equal(startIndex, s.LastIndexOf(value, startIndex + value.Length - 1, comparisonType)); // starting at end of substring
            Assert.Equal(startIndex, s.LastIndexOf(value, startIndex + value.Length, comparisonType)); // starting just beyond end of substring
            if (startIndex + value.Length < s.Length)
            {
                Assert.Equal(startIndex, s.LastIndexOf(value, startIndex + value.Length + 1, comparisonType)); // starting a bit more beyond end of substring
            }
            if (startIndex + value.Length > 1)
            {
                Assert.Equal(-1, s.LastIndexOf(value, startIndex + value.Length - 2, comparisonType)); // starting before end of substring
            }

            // Shouldn't be able to find the substring if the count is less than substring's length
            Assert.Equal(-1, s.LastIndexOf(value, s.Length - 1, value.Length - 1, comparisonType));

            // Now double the source.  Make sure we find the second copy of the substring.
            int halfLen = s.Length;
            s += s;
            Assert.Equal(halfLen + startIndex, s.LastIndexOf(value, comparisonType));

            // Now change the case of a letter.
            s = s.ToUpperInvariant();
            Assert.Equal(ignoringCase ? halfLen + startIndex : -1, s.LastIndexOf(value, comparisonType));
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
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => s.LastIndexOf("a", StringComparison.OrdinalIgnoreCase + 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => s.LastIndexOf("a", 0, StringComparison.CurrentCulture - 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => s.LastIndexOf("a", 0, StringComparison.OrdinalIgnoreCase + 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => s.LastIndexOf("a", 0, 0, StringComparison.CurrentCulture - 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => s.LastIndexOf("a", 0, 0, StringComparison.OrdinalIgnoreCase + 1));
        }

        [Fact]
        public static void LastIndexOf_TurkishI_TurkishCulture()
        {
            RemoteInvoke(() =>
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

                string s = "Turkish I \u0131s TROUBL\u0130NG!";
                string value = "\u0130";

                Assert.Equal(19, s.LastIndexOf(value));
                Assert.Equal(19, s.LastIndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(19, s.LastIndexOf(value, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(19, s.LastIndexOf(value, StringComparison.Ordinal));
                Assert.Equal(19, s.IndexOf(value, StringComparison.OrdinalIgnoreCase));

                value = "\u0131";
                Assert.Equal(10, s.LastIndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(10, s.LastIndexOf(value, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(10, s.LastIndexOf(value, StringComparison.Ordinal));
                Assert.Equal(10, s.LastIndexOf(value, StringComparison.OrdinalIgnoreCase));
                
                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public static void LastIndexOf_TurkishI_InvariantCulture()
        {
            RemoteInvoke(() =>
            {
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

                string s = "Turkish I \u0131s TROUBL\u0130NG!";
                string value = "\u0130";

                Assert.Equal(19, s.LastIndexOf(value));
                Assert.Equal(19, s.LastIndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(19, s.LastIndexOf(value, StringComparison.CurrentCultureIgnoreCase));

                value = "\u0131";
                Assert.Equal(10, s.LastIndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(10, s.LastIndexOf(value, StringComparison.CurrentCultureIgnoreCase));

                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public static void LastIndexOf_TurkishI_EnglishUSCulture()
        {
            RemoteInvoke(() =>
            {
                CultureInfo.CurrentCulture = new CultureInfo("en-US");

                string s = "Turkish I \u0131s TROUBL\u0130NG!";
                string value = "\u0130";

                Assert.Equal(19, s.LastIndexOf(value));
                Assert.Equal(19, s.LastIndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(19, s.LastIndexOf(value, StringComparison.CurrentCultureIgnoreCase));

                value = "\u0131";
                Assert.Equal(10, s.LastIndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(10, s.LastIndexOf(value, StringComparison.CurrentCultureIgnoreCase));

                return SuccessExitCode;
            }).Dispose();
        }

        [Theory]
        [InlineData("foo", 2)]
        [InlineData("hello", 4)]
        [InlineData("", 0)]
        public static void LastIndexOf_EmptyString(string s, int expected)
        {
            Assert.Equal(expected, s.LastIndexOf("", StringComparison.OrdinalIgnoreCase));
        }

        [Theory]
        [InlineData("Hello", new char[] { 'd', 'e', 'l' }, 4, 5, 3)]
        [InlineData("Hello", new char[] { 'd', 'e', 'l' }, 4, 0, -1)]
        [InlineData("Hello", new char[] { 'd', 'e', 'f' }, 2, 3, 1)]
        [InlineData("Hello", new char[] { 'a', 'b', 'c' }, 2, 3, -1)]
        [InlineData("Hello", new char[0], 2, 3, -1)]
        [InlineData("H" + SoftHyphen + "ello", new char[] { 'a', '\u00AD', 'c' }, 2, 3, 1)]
        [InlineData("", new char[] { 'd', 'e', 'f' }, -1, -1, -1)]
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
            Assert.Equal(expected, s.LastIndexOfAny(anyOf, startIndex, count));
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
            if (startIndex + count == s.Length && count != 0)
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

            // Start index >= string.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s.Remove(s.Length));

            // Count < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s.Remove(0, -1));

            // Start index + count > string.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s.Remove(0, s.Length + 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s.Remove(s.Length + 1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s.Remove(s.Length, 1));
        }

        [Theory]
        [InlineData("Hello", 'l', '!', "He!!o")]
        [InlineData("Hello", 'a', 'b', "Hello")]
        public static void Replace_Char_Char(string s, char oldChar, char newChar, string expected)
        {
            Assert.Equal(expected, s.Replace(oldChar, newChar));
        }

        [Theory]
        [InlineData("XYZ", '1', '2')]
        [InlineData("", '1', '2')]
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

        [Theory]
        // CurrentCulture
        [InlineData("Hello", "Hel", StringComparison.CurrentCulture, true)]
        [InlineData("Hello", "Hello", StringComparison.CurrentCulture, true)]
        [InlineData("Hello", "", StringComparison.CurrentCulture, true)]
        [InlineData("Hello", "HELLO", StringComparison.CurrentCulture, false)]
        [InlineData("Hello", "Abc", StringComparison.CurrentCulture, false)]
        [InlineData("Hello", SoftHyphen + "Hel", StringComparison.CurrentCulture, true)]
        [InlineData("", "", StringComparison.CurrentCulture, true)]
        [InlineData("", "hello", StringComparison.CurrentCulture, false)]
        // CurrentCultureIgnoreCase
        [InlineData("Hello", "Hel", StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("Hello", "Hello", StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("Hello", "", StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("Hello", "HEL", StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("Hello", "Abc", StringComparison.CurrentCultureIgnoreCase, false)]
        [InlineData("Hello", SoftHyphen + "Hel", StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("", "", StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("", "hello", StringComparison.CurrentCultureIgnoreCase, false)]
        // InvariantCulture
        [InlineData("Hello", "Hel", StringComparison.InvariantCulture, true)]
        [InlineData("Hello", "Hello", StringComparison.InvariantCulture, true)]
        [InlineData("Hello", "", StringComparison.InvariantCulture, true)]
        [InlineData("Hello", "HELLO", StringComparison.InvariantCulture, false)]
        [InlineData("Hello", "Abc", StringComparison.InvariantCulture, false)]
        [InlineData("Hello", SoftHyphen + "Hel", StringComparison.InvariantCulture, true)]
        [InlineData("", "", StringComparison.InvariantCulture, true)]
        [InlineData("", "hello", StringComparison.InvariantCulture, false)]
        // InvariantCultureIgnoreCase
        [InlineData("Hello", "Hel", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Hello", "Hello", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Hello", "", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Hello", "HEL", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Hello", "Abc", StringComparison.InvariantCultureIgnoreCase, false)]
        [InlineData("Hello", SoftHyphen + "Hel", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("", "", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("", "hello", StringComparison.InvariantCultureIgnoreCase, false)]
        // Ordinal
        [InlineData("Hello", "H", StringComparison.Ordinal, true)]
        [InlineData("Hello", "Hel", StringComparison.Ordinal, true)]
        [InlineData("Hello", "Hello", StringComparison.Ordinal, true)]
        [InlineData("Hello", "Hello Larger", StringComparison.Ordinal, false)]
        [InlineData("Hello", "", StringComparison.Ordinal, true)]
        [InlineData("Hello", "HEL", StringComparison.Ordinal, false)]
        [InlineData("Hello", "Abc", StringComparison.Ordinal, false)]
        [InlineData("Hello", SoftHyphen + "Hel", StringComparison.Ordinal, false)]
        [InlineData("", "", StringComparison.Ordinal, true)]
        [InlineData("", "hello", StringComparison.Ordinal, false)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", "abcdefghijklmnopqrstuvwxyz", StringComparison.Ordinal, true)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", "abcdefghijklmnopqrstuvwx", StringComparison.Ordinal, true)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", "abcdefghijklm", StringComparison.Ordinal, true)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", "ab_defghijklmnopqrstu", StringComparison.Ordinal, false)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", "abcdef_hijklmn", StringComparison.Ordinal, false)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", "abcdefghij_lmn", StringComparison.Ordinal, false)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", "a", StringComparison.Ordinal, true)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", "abcdefghijklmnopqrstuvwxyza", StringComparison.Ordinal, false)]
        // OrdinalIgnoreCase
        [InlineData("Hello", "Hel", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("Hello", "Hello", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("Hello", "Hello Larger", StringComparison.OrdinalIgnoreCase, false)]
        [InlineData("Hello", "", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("Hello", "HEL", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("Hello", "Abc", StringComparison.OrdinalIgnoreCase, false)]
        [InlineData("Hello", SoftHyphen + "Hel", StringComparison.OrdinalIgnoreCase, false)]
        [InlineData("", "", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("", "hello", StringComparison.OrdinalIgnoreCase, false)]
        public static void StartsWith(string s, string value, StringComparison comparisonType, bool expected)
        {
            if (comparisonType == StringComparison.CurrentCulture)
            {
                Assert.Equal(expected, s.StartsWith(value));
            }
            Assert.Equal(expected, s.StartsWith(value, comparisonType));
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/coreclr/issues/2051", TestPlatforms.AnyUnix)]
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
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => s.StartsWith("H", StringComparison.OrdinalIgnoreCase + 1));
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
            }
            Assert.Equal(expected, s.Substring(startIndex, length));
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
        [InlineData("HELLO", "hello")]
        [InlineData("hello", "hello")]
        [InlineData("", "")]
        public static void ToLower(string s, string expected)
        {
            Assert.Equal(expected, s.ToLower());
        }

        private static IEnumerable<object[]> ToLower_Culture_TestData()
        {
            yield return new object[] { "H\u0049 World", "h\u0131 world", new CultureInfo("tr-TR") };
            yield return new object[] { "H\u0130 World", "h\u0069 world", new CultureInfo("tr-TR") };
            yield return new object[] { "H\u0131 World", "h\u0131 world", new CultureInfo("tr-TR") };

            yield return new object[] { "H\u0049 World", "h\u0069 world", new CultureInfo("en-US") };
            yield return new object[] { "H\u0130 World", "h\u0069 world", new CultureInfo("en-US") };
            yield return new object[] { "H\u0131 World", "h\u0131 world", new CultureInfo("en-US") };

            yield return new object[] { "H\u0049 World", "h\u0069 world", CultureInfo.InvariantCulture };
            yield return new object[] { "H\u0130 World", "h\u0130 world", CultureInfo.InvariantCulture };
            yield return new object[] { "H\u0131 World", "h\u0131 world", CultureInfo.InvariantCulture };
        }

        [Fact]
        public static void Test_ToLower_Culture()
        {
            RemoteInvoke(() =>
            {
                foreach (var testdata in ToLower_Culture_TestData())
                {
                    ToLower_Culture((string)testdata[0], (string)testdata[1], (CultureInfo)testdata[2]);
                }
                return SuccessExitCode;
            }).Dispose();
        }

        private static void ToLower_Culture(string actual, string expected, CultureInfo culture)
        {
            CultureInfo.CurrentCulture = culture;
            Assert.True(actual.ToLower().Equals(expected, StringComparison.Ordinal));
        }

        [Theory]
        [InlineData("HELLO", "hello")]
        [InlineData("hello", "hello")]
        [InlineData("", "")]
        public static void ToLowerInvariant(string s, string expected)
        {
            Assert.Equal(expected, s.ToLowerInvariant());
        }

        [Theory]
        [InlineData("")]
        [InlineData("hello")]
        public static void ToString(string s)
        {
            Assert.Same(s, s.ToString());
        }

        [Theory]
        [InlineData("hello", "HELLO")]
        [InlineData("HELLO", "HELLO")]
        [InlineData("", "")]
        public static void ToUpper(string s, string expected)
        {
            Assert.Equal(expected, s.ToUpper());
        }

        [Fact]
        public static void ToUpper_TurkishI_TurkishCulture()
        {
            RemoteInvoke(() =>
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                Assert.True("H\u0069 World".ToUpper().Equals("H\u0130 WORLD", StringComparison.Ordinal));
                Assert.True("H\u0130 World".ToUpper().Equals("H\u0130 WORLD", StringComparison.Ordinal));
                Assert.True("H\u0131 World".ToUpper().Equals("H\u0049 WORLD", StringComparison.Ordinal));

                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public static void ToUpper_TurkishI_EnglishUSCulture()
        {
            RemoteInvoke(() =>
            {
                CultureInfo.CurrentCulture = new CultureInfo("en-US");
                Assert.True("H\u0069 World".ToUpper().Equals("H\u0049 WORLD", StringComparison.Ordinal));
                Assert.True("H\u0130 World".ToUpper().Equals("H\u0130 WORLD", StringComparison.Ordinal));
                Assert.True("H\u0131 World".ToUpper().Equals("H\u0049 WORLD", StringComparison.Ordinal));

                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public static void ToUpper_TurkishI_InvariantCulture()
        {
            RemoteInvoke(() =>
            {
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                Assert.True("H\u0069 World".ToUpper().Equals("H\u0049 WORLD", StringComparison.Ordinal));
                Assert.True("H\u0130 World".ToUpper().Equals("H\u0130 WORLD", StringComparison.Ordinal));
                Assert.True("H\u0131 World".ToUpper().Equals("H\u0131 WORLD", StringComparison.Ordinal));

                return SuccessExitCode;
            }).Dispose();
        }

        [Theory]
        [InlineData("hello", "HELLO")]
        [InlineData("HELLO", "HELLO")]
        [InlineData("", "")]
        public static void ToUpperInvariant(string s, string expected)
        {
            Assert.Equal(expected, s.ToUpperInvariant());
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
        public static void Trim(string s, char[] trimChars, string expected)
        {
            if (trimChars == null || trimChars.Length == 0 || (trimChars.Length == 1 && trimChars[0] == ' '))
            {
                Assert.Equal(expected, s.Trim());
            }

            if (trimChars?.Length == 1)
            {
                Assert.Equal(expected, s.Trim(trimChars[0]));
            }

            Assert.Equal(expected, s.Trim(trimChars));
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
        public static void TrimEnd(string s, char[] trimChars, string expected)
        {
            if (trimChars == null || trimChars.Length == 0 || (trimChars.Length == 1 && trimChars[0] == ' '))
            {
                Assert.Equal(expected, s.TrimEnd());
            }

            if (trimChars?.Length == 1)
            {
                Assert.Equal(expected, s.TrimEnd(trimChars[0]));
            }

            Assert.Equal(expected, s.TrimEnd(trimChars));
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
        public static void TrimStart(string s, char[] trimChars, string expected)
        {
            if (trimChars == null || trimChars.Length == 0 || (trimChars.Length == 1 && trimChars[0] == ' '))
            {
                Assert.Equal(expected, s.TrimStart());
            }

            if (trimChars?.Length == 1)
            {
                Assert.Equal(expected, s.TrimStart(trimChars[0]));
            }

            Assert.Equal(expected, s.TrimStart(trimChars));
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
                    for (int subLen = source.Length - i; subLen > 0; subLen--)
                    {
                        yield return new object[] { source, source.Substring(i, subLen), i, comparison };
                    }
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

        public static IEnumerable<object[]> Compare_TestData()
        {
            //                           str1               str2          culture  ignorecase   expected
            yield return new object[] { "abcd",             "ABcd",       "en-US",    false,       -1  };
            yield return new object[] { "ABcd",             "abcd",       "en-US",    false,        1  };
            yield return new object[] { "abcd",             "ABcd",       "en-US",    true,         0  };
            yield return new object[] { "latin i",         "Latin I",     "tr-TR",    false,        1  };
            yield return new object[] { "latin i",         "Latin I",     "tr-TR",    true,         1  };
            yield return new object[] { "turkish \u0130",   "Turkish i",  "tr-TR",    true,         0  };
            yield return new object[] { "turkish \u0131",   "Turkish I",  "tr-TR",    true,         0  };
            yield return new object[] { null,               null,         "en-us",    true,         0  };
            yield return new object[] { null,               "",           "en-us",    true,        -1  };
            yield return new object[] { "",                 null,         "en-us",    true,         1  };
        }

        public static IEnumerable<object[]> UpperLowerCasing_TestData()
        {
            //                          lower                upper          Culture
            yield return new object[] { "abcd",             "ABCD",         "en-US" };
            yield return new object[] { "latin i",          "LATIN I",      "en-US" };
            yield return new object[] { "turky \u0131",     "TURKY I",      "tr-TR" };
            yield return new object[] { "turky i",          "TURKY \u0130", "tr-TR" };
            yield return new object[] { "\ud801\udc29",     PlatformDetection.IsWindows7 ? "\ud801\udc29" : "\ud801\udc01", "en-US" };
        }

        public static IEnumerable<object[]> StartEndWith_TestData()
        {
            //                           str1                    Start      End   Culture  ignorecase   expected
            yield return new object[] { "abcd",                  "AB",      "CD", "en-US",    false,       false  };
            yield return new object[] { "ABcd",                  "ab",      "CD", "en-US",    false,       false  };
            yield return new object[] { "abcd",                  "AB",      "CD", "en-US",    true,        true   };
            yield return new object[] { "i latin i",             "I Latin", "I",  "tr-TR",    false,       false  };
            yield return new object[] { "i latin i",             "I Latin", "I",  "tr-TR",    true,        false  };
            yield return new object[] { "\u0130 turkish \u0130", "i",       "i",  "tr-TR",    true,        true   };
            yield return new object[] { "\u0131 turkish \u0131", "I",       "I",  "tr-TR",    true,        true   };
        }

        [Theory]
        [MemberData(nameof(Compare_TestData))]
        public static void CompareTest(string aS1, string aS2, string aCultureName, bool aIgnoreCase, int aExpected)
        {
            const string nullPlaceholder = "<null>";
            RemoteInvoke((string s1, string s2, string cultureName, string bIgnoreCase, string iExpected) => {
                if (s1 == nullPlaceholder)
                    s1 = null;

                if (s2 == nullPlaceholder)
                    s2 = null;

                bool ignoreCase = bool.Parse(bIgnoreCase);
                int expected = int.Parse(iExpected);

                CultureInfo ci = CultureInfo.GetCultureInfo(cultureName);
                CompareOptions ignoreCaseOption = ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None;

                Assert.Equal(expected, String.Compare(s1, s2, ignoreCase, ci));
                Assert.Equal(expected, String.Compare(s1, 0, s2, 0, s1 == null ? 0 : s1.Length, ignoreCase, ci));
                Assert.Equal(expected, String.Compare(s1, 0, s2, 0, s1 == null ? 0 : s1.Length, ci, ignoreCaseOption));

                Assert.Equal(expected, String.Compare(s1, s2, ci, ignoreCaseOption));
                Assert.Equal(String.Compare(s1, s2, StringComparison.Ordinal), String.Compare(s1, s2, ci, CompareOptions.Ordinal));
                Assert.Equal(String.Compare(s1, s2, StringComparison.OrdinalIgnoreCase), String.Compare(s1, s2, ci, CompareOptions.OrdinalIgnoreCase));

                CultureInfo.CurrentCulture = ci;
                Assert.Equal(expected, String.Compare(s1, 0, s2, 0, s1 == null ? 0 : s1.Length, ignoreCase));

                return SuccessExitCode;
            }, aS1 ?? nullPlaceholder, aS2 ?? nullPlaceholder, aCultureName, aIgnoreCase.ToString(), aExpected.ToString()).Dispose();
        }

        [Fact]
        public static void CompareNegativeTest()
        {
            AssertExtensions.Throws<ArgumentNullException>("culture", () => String.Compare("a", "b", false, null));

            AssertExtensions.Throws<ArgumentException>("options", () => String.Compare("a", "b", CultureInfo.InvariantCulture, (CompareOptions) 7891));
            AssertExtensions.Throws<ArgumentNullException>("culture", () => String.Compare("a", "b", null, CompareOptions.None));

            AssertExtensions.Throws<ArgumentNullException>("culture", () => String.Compare("a", 0, "b", 0, 1, false, null));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length1", () => String.Compare("a", 10,"b", 0, 1, false, CultureInfo.InvariantCulture));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length2", () => String.Compare("a", 1, "b", 10,1, false, CultureInfo.InvariantCulture));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset1", () => String.Compare("a",-1, "b", 1 ,1, false, CultureInfo.InvariantCulture));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset2", () => String.Compare("a", 1, "b",-1 ,1, false, CultureInfo.InvariantCulture));
        }

        [Theory]
        [MemberData(nameof(UpperLowerCasing_TestData))]
        public static void CasingTest(string lowerForm, string upperForm, string cultureName)
        {
            CultureInfo ci = CultureInfo.GetCultureInfo(cultureName);
            Assert.Equal(lowerForm, upperForm.ToLower(ci));
            Assert.Equal(upperForm, lowerForm.ToUpper(ci));
        }

        [Fact]
        public static void CasingNegativeTest()
        {
            AssertExtensions.Throws<ArgumentNullException>("culture", () => "".ToLower(null));
            AssertExtensions.Throws<ArgumentNullException>("culture", () => "".ToUpper(null));
        }

        [Theory]
        [MemberData(nameof(StartEndWith_TestData))]
        public static void StartEndWithTest(string source, string start, string end, string cultureName, bool ignoreCase, bool expected)
        {
             CultureInfo ci = CultureInfo.GetCultureInfo(cultureName);
             Assert.Equal(expected, source.StartsWith(start, ignoreCase, ci));
             Assert.Equal(expected, source.EndsWith(end, ignoreCase, ci));
        }

        [Fact]
        public static void StartEndNegativeTest()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => "".StartsWith(null, true, null));
            AssertExtensions.Throws<ArgumentNullException>("value", () => "".EndsWith(null, true, null));
        }

        [Fact]
        public static unsafe void ConstructorsTest()
        {
            string s = "This is a string constructor test";
            byte [] encodedBytes = Encoding.Default.GetBytes(s);

            fixed (byte *pBytes = encodedBytes)
            {
                Assert.Equal(s, new String((sbyte*) pBytes));
                Assert.Equal(s, new String((sbyte*) pBytes, 0, encodedBytes.Length));
                Assert.Equal(s, new String((sbyte*) pBytes, 0, encodedBytes.Length, Encoding.Default));
            }

            s = "This is some string \u0393\u0627\u3400\u0440\u1100";
            encodedBytes = Encoding.UTF8.GetBytes(s);

            fixed (byte *pBytes = encodedBytes)
            {
                Assert.Equal(s, new String((sbyte*) pBytes, 0, encodedBytes.Length, Encoding.UTF8));
            }
        }

        [Fact]
        public static unsafe void CloneTest()
        {
            string s = "some string to clone";
            string cloned = (string) s.Clone();
            Assert.Equal(s, cloned);
            Assert.True(Object.ReferenceEquals(s, cloned), "cloned object should return same instance of the string");
        }

        [Fact]
        public static unsafe void CopyTest()
        {
            string s = "some string to copy";
            string copy = String.Copy(s);
            Assert.Equal(s, copy);
            Assert.False(Object.ReferenceEquals(s, copy), "copy should return new instance of the string");
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, ".NetNative limits interning of literals to the empty string.")]
        public static unsafe void InternTest()
        {
            String s1 = "MyTest";
            String s2 = new StringBuilder().Append("My").Append("Test").ToString(); 
            String s3 = String.Intern(s2);

            Assert.Equal(s1, s2);
            Assert.False(Object.ReferenceEquals(s1, s2), "Created string from StringBuilder should have different reference than the literal string");
            Assert.True(Object.ReferenceEquals(s1, s3), "Created intern string should have same reference as the literal string");

            Assert.True(String.IsInterned(s1).Equals(s1), "Expected to the literal string interned");
            Assert.True(String.IsInterned(s2).Equals(s1), "Expected to the interned string to be in the string pool now");
        }

        [Fact]
        public static void InternalTestAotSubset()
        {
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
        }

        [Fact]
        public static unsafe void NormalizationTest()
        {
            // U+0063  LATIN SMALL LETTER C
            // U+0301  COMBINING ACUTE ACCENT
            // U+0327  COMBINING CEDILLA
            // U+00BE  VULGAR FRACTION THREE QUARTERS            
            string s = new String( new char[] {'\u0063', '\u0301', '\u0327', '\u00BE'});

            Assert.False(s.IsNormalized(), "String should be not normalized when checking with the default which same as FormC");
            Assert.False(s.IsNormalized(NormalizationForm.FormC), "String should be not normalized when checking with FormC");
            Assert.False(s.IsNormalized(NormalizationForm.FormD), "String should be not normalized when checking with FormD");
            Assert.False(s.IsNormalized(NormalizationForm.FormKC), "String should be not normalized when checking with FormKC");
            Assert.False(s.IsNormalized(NormalizationForm.FormKD), "String should be not normalized when checking with FormKD");

            string normalized = s.Normalize(); // FormC
            Assert.True(normalized.IsNormalized(), "Expected to have the normalized string with default form FormC");
            Assert.True(normalized.IsNormalized(NormalizationForm.FormC), "Expected to have the normalized string with FormC");
            
            normalized = s.Normalize(NormalizationForm.FormC);
            Assert.True(normalized.IsNormalized(), "Expected to have the normalized string with default form FormC when using NormalizationForm.FormC");
            Assert.True(normalized.IsNormalized(NormalizationForm.FormC), "Expected to have the normalized string with FormC when using NormalizationForm.FormC");

            normalized = s.Normalize(NormalizationForm.FormD);
            Assert.True(normalized.IsNormalized(NormalizationForm.FormD), "Expected to have the normalized string with FormD");

            normalized = s.Normalize(NormalizationForm.FormKC);
            Assert.True(normalized.IsNormalized(NormalizationForm.FormKC), "Expected to have the normalized string with FormKC");

            normalized = s.Normalize(NormalizationForm.FormKD);
            Assert.True(normalized.IsNormalized(NormalizationForm.FormKD), "Expected to have the normalized string with FormKD");
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
    }
}
