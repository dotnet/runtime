// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics;
using System.Text;
using Xunit;

namespace System.Buffers.Text.Tests
{
    public static class StartsEndsWithTests
    {
        [Fact]
        public static void InvalidCharactersInValueThrows()
        {
            Assert.Throws<ArgumentException>(() => Ascii.StartsWith("aaaa"u8, "\u00C0")); // non-vectorized code path
            Assert.Throws<ArgumentException>(() => Ascii.StartsWith("aaaaaaaaaaaaaaaaaaaaaaaaa"u8, "aaaaaaaaaaaaaaaaaaaaaaaa\u00C0")); // vectorized code path
            Assert.Throws<ArgumentException>(() => Ascii.StartsWith("aaaa", new byte[] { 128 }));
            Assert.Throws<ArgumentException>(() => Ascii.StartsWith(new string('a', 50), Enumerable.Repeat((byte)'a', 49).Concat(new byte[] { 128 }).ToArray()));
            Assert.Throws<ArgumentException>(() => Ascii.StartsWithIgnoreCase("aaaa"u8, "\u00C0"));
            Assert.Throws<ArgumentException>(() => Ascii.StartsWithIgnoreCase("aaaa", "\u00C0"));
            Assert.Throws<ArgumentException>(() => Ascii.StartsWithIgnoreCase("aaaa"u8, new byte[] { 128 }));
            Assert.Throws<ArgumentException>(() => Ascii.StartsWithIgnoreCase("aaaa", new byte[] { 128 }));

            Assert.Throws<ArgumentException>(() => Ascii.EndsWith("aaaa"u8, "\u00C0")); // non-vectorized code path
            Assert.Throws<ArgumentException>(() => Ascii.EndsWith("aaaaaaaaaaaaaaaaaaaaaaaaa"u8, "aaaaaaaaaaaaaaaaaaaaaaaa\u00C0")); // vectorized code path
            Assert.Throws<ArgumentException>(() => Ascii.EndsWith("aaaa", new byte[] { 128 }));
            Assert.Throws<ArgumentException>(() => Ascii.EndsWith(new string('a', 50), Enumerable.Repeat((byte)'a', 49).Concat(new byte[] { 128 }).ToArray()));
            Assert.Throws<ArgumentException>(() => Ascii.EndsWithIgnoreCase("aaaa"u8, "\u00C0"));
            Assert.Throws<ArgumentException>(() => Ascii.EndsWithIgnoreCase("aaaa", "\u00C0"));
            Assert.Throws<ArgumentException>(() => Ascii.EndsWithIgnoreCase("aaaa"u8, new byte[] { 128 }));
            Assert.Throws<ArgumentException>(() => Ascii.EndsWithIgnoreCase("aaaa", new byte[] { 128 }));
        }

        public static IEnumerable<object[]> ExactMatchFound_TestData
        {
            get
            {
                yield return new object[] { "test", "test" };
                yield return new object[] { "test", "t" };
                yield return new object[] { "test", "" };

                for (int textLength = 1; textLength <= Vector128<byte>.Count * 4 + 1; textLength++)
                {
                    for (int valueLength = 0; valueLength <= textLength; valueLength++)
                    {
                        char ascii = (char)(textLength % 128);
                        yield return new object[] { new string(ascii, textLength), new string(ascii, valueLength) };
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(ExactMatchFound_TestData))]
        public static void MatchFound(string text, string value)
        {
            Assert.True(Ascii.StartsWith(text, Encoding.ASCII.GetBytes(value)));
            Assert.True(Ascii.StartsWith(Encoding.ASCII.GetBytes(text), value));
            Assert.True(Ascii.StartsWithIgnoreCase(Encoding.ASCII.GetBytes(text), Encoding.ASCII.GetBytes(value)));
            Assert.True(Ascii.StartsWithIgnoreCase(text, value));
            Assert.True(Ascii.StartsWithIgnoreCase(Encoding.ASCII.GetBytes(text), value));
            Assert.True(Ascii.StartsWithIgnoreCase(text, Encoding.ASCII.GetBytes(value)));

            Assert.True(Ascii.EndsWith(text, Encoding.ASCII.GetBytes(value)));
            Assert.True(Ascii.EndsWith(Encoding.ASCII.GetBytes(text), value));
            Assert.True(Ascii.EndsWithIgnoreCase(Encoding.ASCII.GetBytes(text), Encoding.ASCII.GetBytes(value)));
            Assert.True(Ascii.EndsWithIgnoreCase(text, value));
            Assert.True(Ascii.EndsWithIgnoreCase(Encoding.ASCII.GetBytes(text), value));
            Assert.True(Ascii.EndsWithIgnoreCase(text, Encoding.ASCII.GetBytes(value)));
        }

        public static IEnumerable<object[]> IgnoreCaseMatchFound_TestData
        {
            get
            {
                yield return new object[] { "test", "TEST" };
                yield return new object[] { "test", "T" };
                yield return new object[] { "test", "" };

                for (int textLength = 1; textLength <= Vector128<byte>.Count * 4 + 1; textLength++)
                {
                    for (int valueLength = 0; valueLength <= textLength; valueLength++)
                    {
                        char t = (char)(textLength % 128);
                        char v = char.IsAsciiLetterUpper(t) ? char.ToLower(t) : char.IsAsciiLetterLower(t) ? char.ToUpper(t) : t;
                        yield return new object[] { new string(t, textLength), new string(v, valueLength) };
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(IgnoreCaseMatchFound_TestData))]
        public static void IgnoreCaseMatchFound(string text, string value)
        {
            Assert.True(Ascii.StartsWithIgnoreCase(Encoding.ASCII.GetBytes(text), Encoding.ASCII.GetBytes(value)));
            Assert.True(Ascii.StartsWithIgnoreCase(text, value));
            Assert.True(Ascii.StartsWithIgnoreCase(Encoding.ASCII.GetBytes(text), value));
            Assert.True(Ascii.StartsWithIgnoreCase(text, Encoding.ASCII.GetBytes(value)));

            Assert.True(Ascii.EndsWithIgnoreCase(Encoding.ASCII.GetBytes(text), Encoding.ASCII.GetBytes(value)));
            Assert.True(Ascii.EndsWithIgnoreCase(text, value));
            Assert.True(Ascii.EndsWithIgnoreCase(Encoding.ASCII.GetBytes(text), value));
            Assert.True(Ascii.EndsWithIgnoreCase(text, Encoding.ASCII.GetBytes(value)));
        }

        public static IEnumerable<object[]> ExactMatchNotFound_TestData
        {
            get
            {
                yield return new object[] { "test", "tesT" };
                yield return new object[] { "test", "Test" };
                yield return new object[] { "test", "T" };
                yield return new object[] { "test", "!" };

                for (int textLength = 1; textLength <= Vector128<byte>.Count * 4 + 1; textLength++)
                {
                    yield return new object[] { new string('a', textLength), new string('b', 1) };

                    for (int valueLength = 1; valueLength <= textLength; valueLength++)
                    {
                        yield return new object[] { new string('a', textLength), string.Create(valueLength, valueLength / 2, (destination, index) =>
                        {
                            destination.Fill('a');
                            destination[index] = 'b';
                        })};
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(ExactMatchNotFound_TestData))]
        public static void ExactMatchNotFound(string text, string value)
        {
            Assert.False(Ascii.StartsWith(text, Encoding.ASCII.GetBytes(value)));
            Assert.False(Ascii.StartsWith(Encoding.ASCII.GetBytes(text), value));

            Assert.False(Ascii.EndsWith(text, Encoding.ASCII.GetBytes(value)));
            Assert.False(Ascii.EndsWith(Encoding.ASCII.GetBytes(text), value));
        }

        public static IEnumerable<object[]> IgnoreCaseMatchNotFound_TestData
        {
            get
            {
                yield return new object[] { "test", "tes#" };
                yield return new object[] { "test", "T2st" };
                yield return new object[] { "test", "1" };
                yield return new object[] { "test", "#" };

                for (int textLength = 1; textLength <= Vector128<byte>.Count * 4 + 1; textLength++)
                {
                    yield return new object[] { new string('a', textLength), new string('b', 1) };

                    for (int valueLength = 1; valueLength <= textLength; valueLength++)
                    {
                        char t = (char)(textLength % 128);
                        char v = (char)(t != 127 ? t + 1 : 126);

                        yield return new object[] { new string(t, textLength), string.Create(valueLength, (t, v), (destination, chars) =>
                        {
                            destination.Fill(chars.t);
                            destination[destination.Length / 2] = chars.v;
                        })};
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(IgnoreCaseMatchNotFound_TestData))]
        public static void IgnoreCaseMatchNotFound(string text, string value)
        {
            Assert.False(Ascii.StartsWithIgnoreCase(Encoding.ASCII.GetBytes(text), Encoding.ASCII.GetBytes(value)));
            Assert.False(Ascii.StartsWithIgnoreCase(text, value));
            Assert.False(Ascii.StartsWithIgnoreCase(Encoding.ASCII.GetBytes(text), value));
            Assert.False(Ascii.StartsWithIgnoreCase(text, Encoding.ASCII.GetBytes(value)));

            Assert.False(Ascii.EndsWithIgnoreCase(Encoding.ASCII.GetBytes(text), Encoding.ASCII.GetBytes(value)));
            Assert.False(Ascii.EndsWithIgnoreCase(text, value));
            Assert.False(Ascii.EndsWithIgnoreCase(Encoding.ASCII.GetBytes(text), value));
            Assert.False(Ascii.EndsWithIgnoreCase(text, Encoding.ASCII.GetBytes(value)));
        }
    }
}
