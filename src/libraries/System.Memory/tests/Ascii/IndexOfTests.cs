// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace System.Buffers.Text.Tests
{
    public static class IndexOfTests
    {
        [Fact]
        public static void InvalidCharactersInValueThrows()
        {
            Assert.Throws<ArgumentException>(() => Ascii.IndexOf("aaaa"u8, "\u00C0"));
            Assert.Throws<ArgumentException>(() => Ascii.IndexOf("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"u8, "aaaaaaaaaaaaa\u00C0"));
            Assert.Throws<ArgumentException>(() => Ascii.IndexOf("aaaa", new byte[] { 128 }));
            Assert.Throws<ArgumentException>(() => Ascii.IndexOf(new string('a', 50), Enumerable.Repeat((byte)'a', 20).Concat(new byte[] { 128 }).ToArray()));

            Assert.Throws<ArgumentException>(() => Ascii.IndexOfIgnoreCase("aaaa"u8, new byte[] { 128 }));
            Assert.Throws<ArgumentException>(() => Ascii.IndexOfIgnoreCase("aaaa"u8, new byte[] { (byte)'a', 128 }));
            Assert.Throws<ArgumentException>(() => Ascii.IndexOfIgnoreCase("aaaa", "\u00C0"));
            Assert.Throws<ArgumentException>(() => Ascii.IndexOfIgnoreCase("aaaa", "a\u00C0"));
            Assert.Throws<ArgumentException>(() => Ascii.IndexOfIgnoreCase("aaaa"u8, "\u00C0"));
            Assert.Throws<ArgumentException>(() => Ascii.IndexOfIgnoreCase("aaaa"u8, "a\u00C0"));
            Assert.Throws<ArgumentException>(() => Ascii.IndexOfIgnoreCase("aaaa", new byte[] { 128 }));
            Assert.Throws<ArgumentException>(() => Ascii.IndexOfIgnoreCase("aaaa", new byte[] { (byte)'a', 128 }));

            Assert.Throws<ArgumentException>(() => Ascii.LastIndexOf("aaaa"u8, "\u00C0"));
            Assert.Throws<ArgumentException>(() => Ascii.LastIndexOf("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"u8, "aaaaaaaaaaaaa\u00C0"));
            Assert.Throws<ArgumentException>(() => Ascii.LastIndexOf("aaaa", new byte[] { 128 }));
            Assert.Throws<ArgumentException>(() => Ascii.LastIndexOf(new string('a', 50), Enumerable.Repeat((byte)'a', 20).Concat(new byte[] { 128 }).ToArray()));

            Assert.Throws<ArgumentException>(() => Ascii.LastIndexOfIgnoreCase("aaaa"u8, new byte[] { 128 }));
            Assert.Throws<ArgumentException>(() => Ascii.LastIndexOfIgnoreCase("aaaa"u8, new byte[] { (byte)'a', 128 }));
            Assert.Throws<ArgumentException>(() => Ascii.LastIndexOfIgnoreCase("aaaa", "\u00C0"));
            Assert.Throws<ArgumentException>(() => Ascii.LastIndexOfIgnoreCase("aaaa", "a\u00C0"));
            Assert.Throws<ArgumentException>(() => Ascii.LastIndexOfIgnoreCase("aaaa"u8, "\u00C0"));
            Assert.Throws<ArgumentException>(() => Ascii.LastIndexOfIgnoreCase("aaaa"u8, "a\u00C0"));
            Assert.Throws<ArgumentException>(() => Ascii.LastIndexOfIgnoreCase("aaaa", new byte[] { 128 }));
            Assert.Throws<ArgumentException>(() => Ascii.LastIndexOfIgnoreCase("aaaa", new byte[] { (byte)'a', 128 }));
        }

        public static IEnumerable<object[]> ExactMatchFound_TestData
        {
            get
            {
                yield return new object[] { "test", "", 0, 4 };
                yield return new object[] { "test", "test", 0, 0 };
                yield return new object[] { "abcdefghijk", "cde", 2, 2 };
                yield return new object[] {  "abcdabcdabcd" , "abcd", 0,  8 };
                yield return new object[] { "test0test1test2test3test4test5test6", "test3test4test5test6", 15, 15 };
                yield return new object[] { "This is not a very complex test case", "complex test", 19, 19 };
            }
        }

        [Theory]
        [MemberData(nameof(ExactMatchFound_TestData))]
        public static void ExactMatchFound(string text, string value, int expectedFirstIndex, int expectedLastIndex)
        {
            Assert.Equal(expectedFirstIndex, Ascii.IndexOf(text, Encoding.ASCII.GetBytes(value)));
            Assert.Equal(expectedFirstIndex, Ascii.IndexOf(Encoding.ASCII.GetBytes(text), value));

            Assert.Equal(expectedFirstIndex, Ascii.IndexOfIgnoreCase(Encoding.ASCII.GetBytes(text), Encoding.ASCII.GetBytes(value)));
            Assert.Equal(expectedFirstIndex, Ascii.IndexOfIgnoreCase(text, value));
            Assert.Equal(expectedFirstIndex, Ascii.IndexOfIgnoreCase(Encoding.ASCII.GetBytes(text), value));
            Assert.Equal(expectedFirstIndex, Ascii.IndexOfIgnoreCase(text, Encoding.ASCII.GetBytes(value)));

            Assert.Equal(expectedLastIndex, Ascii.LastIndexOf(text, Encoding.ASCII.GetBytes(value)));
            Assert.Equal(expectedLastIndex, Ascii.LastIndexOf(Encoding.ASCII.GetBytes(text), value));

            Assert.Equal(expectedLastIndex, Ascii.LastIndexOfIgnoreCase(Encoding.ASCII.GetBytes(text), Encoding.ASCII.GetBytes(value)));
            Assert.Equal(expectedLastIndex, Ascii.LastIndexOfIgnoreCase(text, value));
            Assert.Equal(expectedLastIndex, Ascii.LastIndexOfIgnoreCase(Encoding.ASCII.GetBytes(text), value));
            Assert.Equal(expectedLastIndex, Ascii.LastIndexOfIgnoreCase(text, Encoding.ASCII.GetBytes(value)));
        }

        public static IEnumerable<object[]> ExactMatchNotFound_TestData
        {
            get
            {
                yield return new object[] { "test", "TEST" };
                yield return new object[] { "abcdefghijk", "xyz" };
                yield return new object[] { "abcdabcdabcd", "abcD" };
                yield return new object[] { "test0test1test2test3test4test5test6", "test8" };
                yield return new object[] { "This is not a very complex test case", "benchmark" };
            }
        }

        [Theory]
        [MemberData(nameof(ExactMatchNotFound_TestData))]
        public static void ExactMatchNotFound(string text, string value)
        {
            Assert.Equal(-1, Ascii.IndexOf(text, Encoding.ASCII.GetBytes(value)));
            Assert.Equal(-1, Ascii.IndexOf(Encoding.ASCII.GetBytes(text), value));

            Assert.Equal(-1, Ascii.LastIndexOf(text, Encoding.ASCII.GetBytes(value)));
            Assert.Equal(-1, Ascii.LastIndexOf(Encoding.ASCII.GetBytes(text), value));
        }

        public static IEnumerable<object[]> IgnoreCaseMatchFound_TestData
        {
            get
            {
                yield return new object[] { "test", "", 0, 4 };
                yield return new object[] { "tESt", "TesT", 0, 0 };
                yield return new object[] { "abcdefghijk", "CdE", 2, 2 };
                yield return new object[] { "abcdabcdabcd", "ABcD", 0, 8 };
                yield return new object[] { "test0test1test2test3test4test5test6", "TeSt3tEst4TeSt5tEsT6", 15, 15 };
                yield return new object[] { "This is not a VERY COMPLEX test case", "COMplex tEst", 19, 19 };
            }
        }

        [Theory]
        [MemberData(nameof(IgnoreCaseMatchFound_TestData))]
        public static void IgnoreCaseMatchFound(string text, string value, int expectedFirstIndex, int expectedLastIndex)
        {
            Assert.Equal(expectedFirstIndex, Ascii.IndexOfIgnoreCase(Encoding.ASCII.GetBytes(text), Encoding.ASCII.GetBytes(value)));
            Assert.Equal(expectedFirstIndex, Ascii.IndexOfIgnoreCase(text, value));
            Assert.Equal(expectedFirstIndex, Ascii.IndexOfIgnoreCase(Encoding.ASCII.GetBytes(text), value));
            Assert.Equal(expectedFirstIndex, Ascii.IndexOfIgnoreCase(text, Encoding.ASCII.GetBytes(value)));

            Assert.Equal(expectedLastIndex, Ascii.LastIndexOfIgnoreCase(Encoding.ASCII.GetBytes(text), Encoding.ASCII.GetBytes(value)));
            Assert.Equal(expectedLastIndex, Ascii.LastIndexOfIgnoreCase(text, value));
            Assert.Equal(expectedLastIndex, Ascii.LastIndexOfIgnoreCase(Encoding.ASCII.GetBytes(text), value));
            Assert.Equal(expectedLastIndex, Ascii.LastIndexOfIgnoreCase(text, Encoding.ASCII.GetBytes(value)));
        }

        public static IEnumerable<object[]> IgnoreCaseMatchNotFound_TestData
        {
            get
            {
                yield return new object[] { "test", "!" };
                yield return new object[] { "tESt", "TosT" };
                yield return new object[] { "abcdefghijk", "xyz" };
                yield return new object[] { "abcdabcdabcd", "EfGh" };
                yield return new object[] { "test0test1test2test3test4test5test6", "tESt8" };
                yield return new object[] { "This is not a VERY COMPLEX test case", "SiMplE" };
            }
        }

        [Theory]
        [MemberData(nameof(IgnoreCaseMatchNotFound_TestData))]
        public static void IgnoreCaseMatchNotFound(string text, string value)
        {
            Assert.Equal(-1, Ascii.IndexOfIgnoreCase(Encoding.ASCII.GetBytes(text), Encoding.ASCII.GetBytes(value)));
            Assert.Equal(-1, Ascii.IndexOfIgnoreCase(text, value));
            Assert.Equal(-1, Ascii.IndexOfIgnoreCase(Encoding.ASCII.GetBytes(text), value));
            Assert.Equal(-1, Ascii.IndexOfIgnoreCase(text, Encoding.ASCII.GetBytes(value)));

            Assert.Equal(-1, Ascii.LastIndexOfIgnoreCase(Encoding.ASCII.GetBytes(text), Encoding.ASCII.GetBytes(value)));
            Assert.Equal(-1, Ascii.LastIndexOfIgnoreCase(text, value));
            Assert.Equal(-1, Ascii.LastIndexOfIgnoreCase(Encoding.ASCII.GetBytes(text), value));
            Assert.Equal(-1, Ascii.LastIndexOfIgnoreCase(text, Encoding.ASCII.GetBytes(value)));
        }
    }
}
