// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Tests
{
    public partial class BooleanTests
    {
        public static IEnumerable<object[]> Parse_ValidWithOffsetCount_TestData()
        {
            foreach (object[] inputs in Parse_Valid_TestData())
            {
                yield return new object[] { inputs[0], 0, ((string)inputs[0]).Length, inputs[1] };
            }

            yield return new object[] { " \0 \0  TrueFalse   \0 ", 6, 4, true };
            yield return new object[] { " \0 \0  TrueFalse   \0 ", 10, 5, false };
        }

        [Theory]
        [MemberData(nameof(Parse_ValidWithOffsetCount_TestData))]
        public static void Parse_Span_Valid(string value, int offset, int count, bool expected)
        {
            Assert.Equal(expected, bool.Parse(value.AsSpan(offset, count)));

            Assert.True(bool.TryParse(value.AsSpan(offset, count), out bool result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Span_Invalid(string value, Type exceptionType)
        {
            if (value != null)
            {
                Assert.Throws(exceptionType, () => bool.Parse(value.AsSpan()));

                Assert.False(bool.TryParse(value.AsSpan(), out bool result));
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
