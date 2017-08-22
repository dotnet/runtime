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
    }
}
