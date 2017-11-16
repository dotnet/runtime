// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public partial class VersionTests
    {
        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse_Span_ValidInput_ReturnsExpected(string input, Version expected)
        {
            if (input == null)
            {
                return;
            }

            Assert.Equal(expected, Version.Parse(input.AsReadOnlySpan()));

            Assert.True(Version.TryParse(input.AsReadOnlySpan(), out Version version));
            Assert.Equal(expected, version);
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Span_InvalidInput_ThrowsException(string input, Type exceptionType)
        {
            if (input == null)
            {
                return;
            }

            Assert.Throws(exceptionType, () => Version.Parse(input.AsReadOnlySpan()));

            Assert.False(Version.TryParse(input.AsReadOnlySpan(), out Version version));
            Assert.Null(version);
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void TryFormat_Invoke_WritesExpected(Version version, string[] expected)
        {
            char[] dest;
            int charsWritten;

            for (int i = 0; i < expected.Length; i++)
            {
                if (i > 0)
                {
                    // Too small
                    dest = new char[expected[i].Length - 1];
                    Assert.False(version.TryFormat(dest, i, out charsWritten));
                    Assert.Equal(0, charsWritten);
                }

                // Just right
                dest = new char[expected[i].Length];
                Assert.True(version.TryFormat(dest, i, out charsWritten));
                Assert.Equal(expected[i].Length, charsWritten);
                Assert.Equal(expected[i], new string(dest, 0, charsWritten));

                // More than needed
                dest = new char[expected[i].Length + 10];
                Assert.True(version.TryFormat(dest, i, out charsWritten));
                Assert.Equal(expected[i].Length, charsWritten);
                Assert.Equal(expected[i], new string(dest, 0, charsWritten));
            }

            int maxFieldCount = expected.Length - 1;
            dest = new char[expected[maxFieldCount].Length];
            Assert.True(version.TryFormat(dest, out charsWritten));
            Assert.Equal(expected[maxFieldCount].Length, charsWritten);
            Assert.Equal(expected[maxFieldCount], new string(dest, 0, charsWritten));

            dest = new char[0];
            AssertExtensions.Throws<ArgumentException>("fieldCount", () => version.TryFormat(dest, -1, out charsWritten)); // Index < 0
            AssertExtensions.Throws<ArgumentException>("fieldCount", () => version.TryFormat(dest, maxFieldCount + 1, out charsWritten)); // Index > version.fieldCount
        }
    }
}
