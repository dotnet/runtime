// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Xunit;

namespace System.Tests
{
    public partial class Int16Tests
    {
        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse_Span_Valid(string value, NumberStyles style, IFormatProvider provider, short expected)
        {
            Assert.Equal(expected, short.Parse(value.AsReadOnlySpan(), style, provider));

            Assert.True(short.TryParse(value.AsReadOnlySpan(), out short result, style, provider));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                Assert.Throws(exceptionType, () => short.Parse(value.AsReadOnlySpan(), style, provider));

                Assert.False(short.TryParse(value.AsReadOnlySpan(), out short result, style, provider));
                Assert.Equal(0, result);
            }
        }
    }
}
