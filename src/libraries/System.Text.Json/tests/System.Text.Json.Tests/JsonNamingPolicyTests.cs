// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Json.Tests
{
    public static class JsonNamingPolicyTests
    {
        [Theory]
        [InlineData("MyProperty", "myProperty")]
        [InlineData("PropertyName", "propertyName")]
        [InlineData("ABC", "aBC")]
        [InlineData("A", "a")]
        [InlineData("", "")]
        public static void CamelCase_ConvertName(string input, string expected)
        {
            string result = JsonNamingPolicy.CamelCase.ConvertName(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("MyProperty", "my_property")]
        [InlineData("PropertyName", "property_name")]
        [InlineData("ABC", "a_b_c")]
        [InlineData("HTMLParser", "h_t_m_l_parser")]
        [InlineData("A", "a")]
        [InlineData("", "")]
        public static void SnakeCaseLower_ConvertName(string input, string expected)
        {
            string result = JsonNamingPolicy.SnakeCaseLower.ConvertName(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("MyProperty", "MY_PROPERTY")]
        [InlineData("PropertyName", "PROPERTY_NAME")]
        [InlineData("ABC", "A_B_C")]
        [InlineData("HTMLParser", "H_T_M_L_PARSER")]
        [InlineData("A", "A")]
        [InlineData("", "")]
        public static void SnakeCaseUpper_ConvertName(string input, string expected)
        {
            string result = JsonNamingPolicy.SnakeCaseUpper.ConvertName(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("MyProperty", "my-property")]
        [InlineData("PropertyName", "property-name")]
        [InlineData("ABC", "a-b-c")]
        [InlineData("HTMLParser", "h-t-m-l-parser")]
        [InlineData("A", "a")]
        [InlineData("", "")]
        public static void KebabCaseLower_ConvertName(string input, string expected)
        {
            string result = JsonNamingPolicy.KebabCaseLower.ConvertName(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("MyProperty", "MY-PROPERTY")]
        [InlineData("PropertyName", "PROPERTY-NAME")]
        [InlineData("ABC", "A-B-C")]
        [InlineData("HTMLParser", "H-T-M-L-PARSER")]
        [InlineData("A", "A")]
        [InlineData("", "")]
        public static void KebabCaseUpper_ConvertName(string input, string expected)
        {
            string result = JsonNamingPolicy.KebabCaseUpper.ConvertName(input);
            Assert.Equal(expected, result);
        }
    }
}
