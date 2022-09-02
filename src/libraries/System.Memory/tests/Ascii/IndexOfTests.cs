// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace System.Buffers.Text.Tests
{
    public class IndexOfTests
    {
        [Fact]
        public void InvalidCharactersInValueThrows()
        {
            Assert.Throws<ArgumentException>(() => Ascii.IndexOf("aaaa"u8, "\u00C0"));
            Assert.Throws<ArgumentException>(() => Ascii.IndexOf("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"u8, "aaaaaaaaaaaaa\u00C0"));
            Assert.Throws<ArgumentException>(() => Ascii.IndexOf("aaaa", new byte[] { 128 }));
            Assert.Throws<ArgumentException>(() => Ascii.IndexOf(new string('a', 50), Enumerable.Repeat((byte)'a', 20).Concat(new byte[] { 128 }).ToArray()));

            Assert.Throws<ArgumentException>(() => Ascii.LastIndexOf("aaaa"u8, "\u00C0"));
            Assert.Throws<ArgumentException>(() => Ascii.LastIndexOf("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"u8, "aaaaaaaaaaaaa\u00C0"));
            Assert.Throws<ArgumentException>(() => Ascii.LastIndexOf("aaaa", new byte[] { 128 }));
            Assert.Throws<ArgumentException>(() => Ascii.LastIndexOf(new string('a', 50), Enumerable.Repeat((byte)'a', 20).Concat(new byte[] { 128 }).ToArray()));
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
        public void ExactMatchFound(string text, string value, int expectedFirstIndex, int expectedLastIndex)
        {
            Assert.Equal(expectedFirstIndex, Ascii.IndexOf(text, Encoding.ASCII.GetBytes(value)));
            Assert.Equal(expectedFirstIndex, Ascii.IndexOf(Encoding.ASCII.GetBytes(text), value));

            Assert.Equal(expectedLastIndex, Ascii.LastIndexOf(text, Encoding.ASCII.GetBytes(value)));
            Assert.Equal(expectedLastIndex, Ascii.LastIndexOf(Encoding.ASCII.GetBytes(text), value));
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
        public void ExactMatchNotFound(string text, string value)
        {
            Assert.Equal(-1, Ascii.IndexOf(text, Encoding.ASCII.GetBytes(value)));
            Assert.Equal(-1, Ascii.IndexOf(Encoding.ASCII.GetBytes(text), value));

            Assert.Equal(-1, Ascii.LastIndexOf(text, Encoding.ASCII.GetBytes(value)));
            Assert.Equal(-1, Ascii.LastIndexOf(Encoding.ASCII.GetBytes(text), value));
        }
    }
}
