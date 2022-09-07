// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace System.Buffers.Text.Tests
{
    public class EqualsTests
    {
        [Fact]
        public void InvalidCharacters_DoesNotThrow()
        {
            Assert.False(Ascii.Equals(Enumerable.Repeat((byte)128, "valid".Length).ToArray(), "valid"));
            Assert.False(Ascii.Equals("valid"u8, "aa\u00C0aa"));

            Assert.False(Ascii.EqualsIgnoreCase(new byte[] { 127 }, new byte[] { 128 }));
            Assert.True(Ascii.EqualsIgnoreCase(new byte[] { 128 }, new byte[] { 128 }));
            Assert.False(Ascii.EqualsIgnoreCase(new byte[] { 128 }, new byte[] { 127 }));

            Assert.False(Ascii.EqualsIgnoreCase(Enumerable.Repeat((byte)128, "valid".Length).ToArray(), "valid"));
            Assert.False(Ascii.EqualsIgnoreCase("valid"u8, "aa\u00C0aa"));
        }

        public static IEnumerable<object[]> ExactMatch_TestData
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
        [MemberData(nameof(ExactMatch_TestData))]
        public void ExactMatchFound(string left, string right)
        {
            Assert.True(Ascii.Equals(Encoding.ASCII.GetBytes(left), right));

            Assert.True(Ascii.EqualsIgnoreCase(Encoding.ASCII.GetBytes(left), Encoding.ASCII.GetBytes(right)));
            Assert.True(Ascii.EqualsIgnoreCase(left, right));
            Assert.True(Ascii.EqualsIgnoreCase(Encoding.ASCII.GetBytes(left), right));
        }

        public static IEnumerable<object[]> ExactMatchNotFound_TestData
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
                            destination.Fill((char)iteration);
                            destination[iteration / 2] = (char)128;
                        })};
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(ExactMatchNotFound_TestData))]
        public void ExactMatchNotFound(string left, string right)
        {
            Assert.False(Ascii.Equals(Encoding.ASCII.GetBytes(left), right));

            Assert.False(Ascii.EqualsIgnoreCase(Encoding.ASCII.GetBytes(left), Encoding.ASCII.GetBytes(right)));
            Assert.False(Ascii.EqualsIgnoreCase(left, right));
            Assert.False(Ascii.EqualsIgnoreCase(Encoding.ASCII.GetBytes(left), right));
        }

        public static IEnumerable<object[]> IgnoreCaseMatch_TestData
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
        [MemberData(nameof(IgnoreCaseMatch_TestData))]
        public void IgnoreCaseMatchFound(string left, string right)
        {
            Assert.True(Ascii.EqualsIgnoreCase(Encoding.ASCII.GetBytes(left), Encoding.ASCII.GetBytes(right)));
            Assert.True(Ascii.EqualsIgnoreCase(left, right));
            Assert.True(Ascii.EqualsIgnoreCase(Encoding.ASCII.GetBytes(left), right));
        }
    }
}
