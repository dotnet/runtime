// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public partial class BooleanTests
    {
        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse_Span_Valid(string value, bool expected)
        {
            Assert.Equal(expected, bool.Parse(value.AsReadOnlySpan()));

            Assert.True(bool.TryParse(value.AsReadOnlySpan(), out bool result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Span_Invalid(string value, Type exceptionType)
        {
            if (value != null)
            {
                Assert.Throws(exceptionType, () => bool.Parse(value.AsReadOnlySpan()));

                Assert.False(bool.TryParse(value.AsReadOnlySpan(), out bool result));
                Assert.Equal(false, result);
            }
        }

        [Theory]
        [InlineData(true, "True")]
        [InlineData(false, "False")]
        public static void TryFormat(bool i, string expected)
        {
            char[] actual;
            int charsWritten;

            // Just right
            actual = new char[expected.Length];
            Assert.True(i.TryFormat(actual.AsSpan(), out charsWritten));
            Assert.Equal(expected.Length, charsWritten);
            Assert.Equal(expected, new string(actual));

            // Longer than needed
            actual = new char[expected.Length + 1];
            Assert.True(i.TryFormat(actual.AsSpan(), out charsWritten));
            Assert.Equal(expected.Length, charsWritten);
            Assert.Equal(expected, new string(actual, 0, charsWritten));

            // Too short
            if (expected.Length > 0)
            {
                actual = new char[expected.Length - 1];
                Assert.False(i.TryFormat(actual.AsSpan(), out charsWritten));
                Assert.Equal(0, charsWritten);
            }
        }
    }
}
