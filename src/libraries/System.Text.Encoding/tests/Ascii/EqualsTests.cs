// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Text.Tests
{
    public static class EqualsTests
    {
        [Fact]
        public static void EqualValues_ButNonAscii_ReturnsFalse() => Assert_NotEqual(128, 128); // 128 is first non-ascii character

        [Fact]
        public static void NonEqualValues_AndNonAsciiCharacters_ReturnsFalse() => Assert_NotEqual(127, 128);

        private static void Assert_NotEqual(byte left, byte right)
        {
            // Equals
            // (byte, char)
            Assert.False(Ascii.Equals(new byte[] { left }, new char[] { (char)right })); // non-vectorized code path
            Assert.False(Ascii.Equals(Enumerable.Repeat(left, 100).ToArray(), Enumerable.Repeat((char)right, 100).ToArray())); // vectorized code path

            // EqualsIgnoreCase
            // (byte, byte)
            Assert.False(Ascii.EqualsIgnoreCase(new byte[] { left }, new byte[] { right }));
            Assert.False(Ascii.EqualsIgnoreCase(Enumerable.Repeat(left, 100).ToArray(), Enumerable.Repeat(right, 100).ToArray()));
            // (byte, char)
            Assert.False(Ascii.EqualsIgnoreCase(new byte[] { left }, new char[] { (char)right }));
            Assert.False(Ascii.EqualsIgnoreCase(Enumerable.Repeat(left, 100).ToArray(), Enumerable.Repeat((char)right, 100).ToArray()));
            // (char, char)
            Assert.False(Ascii.EqualsIgnoreCase(new char[] { (char)left }, new char[] { (char)right }));
            Assert.False(Ascii.EqualsIgnoreCase(Enumerable.Repeat((char)left, 100).ToArray(), Enumerable.Repeat((char)right, 100).ToArray()));
        }

        public static IEnumerable<object[]> ExactlyTheSame_TestData
        {
            get
            {
                yield return new object[] { "test", "test" };

                for (char textLength = (char)0; textLength <= 127; textLength++)
                {
                    yield return new object[] { new string(textLength, textLength), new string(textLength, textLength) };
                }
            }
        }

        [Theory]
        [MemberData(nameof(ExactlyTheSame_TestData))]
        public static void ExactlyTheSame_ReturnsTrue(string left, string right)
        {
            Assert.True(Ascii.Equals(Encoding.ASCII.GetBytes(left), right));

            Assert.True(Ascii.EqualsIgnoreCase(Encoding.ASCII.GetBytes(left), Encoding.ASCII.GetBytes(right)));
            Assert.True(Ascii.EqualsIgnoreCase(left, right));
            Assert.True(Ascii.EqualsIgnoreCase(Encoding.ASCII.GetBytes(left), right));
        }

        public static IEnumerable<object[]> Different_TestData
        {
            get
            {
                yield return new object[] { "tak", "nie" };

                for (char i = (char)1; i <= 127; i++)
                {
                    if (i != '?') // ASCIIEncoding maps invalid ASCII to ?
                    {
                        yield return new object[] { new string(i, i), string.Create(i, i, (destination, iteration) =>
                        {
                            destination.Fill(iteration);
                            destination[iteration / 2] = (char)128;
                        })};
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(Different_TestData))]
        public static void Different_ReturnsFalse(string left, string right)
        {
            Assert.False(Ascii.Equals(Encoding.ASCII.GetBytes(left), right));

            Assert.False(Ascii.EqualsIgnoreCase(Encoding.ASCII.GetBytes(left), Encoding.ASCII.GetBytes(right)));
            Assert.False(Ascii.EqualsIgnoreCase(left, right));
            Assert.False(Ascii.EqualsIgnoreCase(Encoding.ASCII.GetBytes(left), right));
        }

        public static IEnumerable<object[]> EqualIgnoreCase_TestData
        {
            get
            {
                yield return new object[] { "aBc", "AbC" };

                for (char i = (char)0; i <= 127; i++)
                {
                    char left = i;
                    char right = char.IsAsciiLetterUpper(left) ? char.ToLower(left) : char.IsAsciiLetterLower(left) ? char.ToUpper(left) : left;
                    yield return new object[] { new string(left, i), new string(right, i) };
                }
            }
        }

        [Theory]
        [MemberData(nameof(EqualIgnoreCase_TestData))]
        public static void EqualIgnoreCase_ReturnsTrue(string left, string right)
        {
            Assert.True(Ascii.EqualsIgnoreCase(Encoding.ASCII.GetBytes(left), Encoding.ASCII.GetBytes(right)));
            Assert.True(Ascii.EqualsIgnoreCase(left, right));
            Assert.True(Ascii.EqualsIgnoreCase(Encoding.ASCII.GetBytes(left), right));
        }
    }
}
