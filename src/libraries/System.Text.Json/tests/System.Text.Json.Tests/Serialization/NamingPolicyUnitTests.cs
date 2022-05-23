// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static class NamingPolicyUnitTests
    {
        [Fact]
        public static void ToCamelCaseTest()
        {
            // These test cases were copied from Json.NET.
            Assert.Equal("urlValue", Convert("URLValue"));
            Assert.Equal("url", Convert("URL"));
            Assert.Equal("id", Convert("ID"));
            Assert.Equal("i", Convert("I"));
            Assert.Equal("", Convert(""));
            Assert.Null(Convert(null));
            Assert.Equal("person", Convert("Person"));
            Assert.Equal("iPhone", Convert("iPhone"));
            Assert.Equal("iPhone", Convert("IPhone"));
            Assert.Equal("i Phone", Convert("I Phone"));
            Assert.Equal("i  Phone", Convert("I  Phone"));
            Assert.Equal(" IPhone", Convert(" IPhone"));
            Assert.Equal(" IPhone ", Convert(" IPhone "));
            Assert.Equal("isCIA", Convert("IsCIA"));
            Assert.Equal("vmQ", Convert("VmQ"));
            Assert.Equal("xml2Json", Convert("Xml2Json"));
            Assert.Equal("snAkEcAsE", Convert("SnAkEcAsE"));
            Assert.Equal("snA__kEcAsE", Convert("SnA__kEcAsE"));
            Assert.Equal("snA__ kEcAsE", Convert("SnA__ kEcAsE"));
            Assert.Equal("already_snake_case_ ", Convert("already_snake_case_ "));
            Assert.Equal("isJSONProperty", Convert("IsJSONProperty"));
            Assert.Equal("shoutinG_CASE", Convert("SHOUTING_CASE"));
            Assert.Equal("9999-12-31T23:59:59.9999999Z", Convert("9999-12-31T23:59:59.9999999Z"));
            Assert.Equal("hi!! This is text. Time to test.", Convert("Hi!! This is text. Time to test."));
            Assert.Equal("building", Convert("BUILDING"));
            Assert.Equal("building Property", Convert("BUILDING Property"));
            Assert.Equal("building Property", Convert("Building Property"));
            Assert.Equal("building PROPERTY", Convert("BUILDING PROPERTY"));
            
            static string Convert(string name)
            {
                JsonNamingPolicy policy = JsonNamingPolicy.CamelCase;
                string value = policy.ConvertName(name);
                return value;
            }
        }

        [Fact]
        public static void ToSnakeLowerCase()
        {
            Assert.Equal("xml_http_request", Convert("XMLHttpRequest"));
            Assert.Equal("camel_case", Convert("camelCase"));
            Assert.Equal("camel_case", Convert("CamelCase"));
            Assert.Equal("snake_case", Convert("snake_case"));
            Assert.Equal("snake_case", Convert("SNAKE_CASE"));
            Assert.Equal("kebab_case", Convert("kebab-case"));
            Assert.Equal("kebab_case", Convert("KEBAB-CASE"));
            Assert.Equal("double_space", Convert("double  space"));
            Assert.Equal("double_underscore", Convert("double__underscore"));
            Assert.Equal("abc", Convert("abc"));
            Assert.Equal("ab_c", Convert("abC"));
            Assert.Equal("a_bc", Convert("aBc"));
            Assert.Equal("a_bc", Convert("aBC"));
            Assert.Equal("a_bc", Convert("ABc"));
            Assert.Equal("abc", Convert("ABC"));
            Assert.Equal("abc123def456", Convert("abc123def456"));
            Assert.Equal("abc123_def456", Convert("abc123Def456"));
            Assert.Equal("abc123_def456", Convert("abc123DEF456"));
            Assert.Equal("abc123def456", Convert("ABC123DEF456"));
            Assert.Equal("abc123def456", Convert("ABC123def456"));
            Assert.Equal("abc123def456", Convert("Abc123def456"));
            Assert.Equal("abc", Convert("  abc"));
            Assert.Equal("abc", Convert("abc  "));
            Assert.Equal("abc", Convert("  abc  "));
            Assert.Equal("abc_def", Convert("  abc def  "));
            
            static string Convert(string name)
            {
                JsonNamingPolicy policy = JsonNamingPolicy.SnakeLowerCase;
                string value = policy.ConvertName(name);
                return value;
            }
        }

        [Fact]
        public static void ToSnakeUpperCase()
        {
            Assert.Equal("XML_HTTP_REQUEST", Convert("XMLHttpRequest"));
            Assert.Equal("CAMEL_CASE", Convert("camelCase"));
            Assert.Equal("CAMEL_CASE", Convert("CamelCase"));
            Assert.Equal("SNAKE_CASE", Convert("snake_case"));
            Assert.Equal("SNAKE_CASE", Convert("SNAKE_CASE"));
            Assert.Equal("KEBAB_CASE", Convert("kebab-case"));
            Assert.Equal("KEBAB_CASE", Convert("KEBAB-CASE"));
            Assert.Equal("DOUBLE_SPACE", Convert("double  space"));
            Assert.Equal("DOUBLE_UNDERSCORE", Convert("double__underscore"));
            Assert.Equal("ABC", Convert("abc"));
            Assert.Equal("AB_C", Convert("abC"));
            Assert.Equal("A_BC", Convert("aBc"));
            Assert.Equal("A_BC", Convert("aBC"));
            Assert.Equal("A_BC", Convert("ABc"));
            Assert.Equal("ABC", Convert("ABC"));
            Assert.Equal("ABC123DEF456", Convert("abc123def456"));
            Assert.Equal("ABC123_DEF456", Convert("abc123Def456"));
            Assert.Equal("ABC123_DEF456", Convert("abc123DEF456"));
            Assert.Equal("ABC123DEF456", Convert("ABC123DEF456"));
            Assert.Equal("ABC123DEF456", Convert("ABC123def456"));
            Assert.Equal("ABC123DEF456", Convert("Abc123def456"));
            Assert.Equal("ABC", Convert("  ABC"));
            Assert.Equal("ABC", Convert("ABC  "));
            Assert.Equal("ABC", Convert("  ABC  "));
            Assert.Equal("ABC_DEF", Convert("  ABC def  "));
            
            static string Convert(string name)
            {
                JsonNamingPolicy policy = JsonNamingPolicy.SnakeUpperCase;
                string value = policy.ConvertName(name);
                return value;
            }
        }

        [Fact]
        public static void ToKebabLowerCase()
        {
            Assert.Equal("xml-http-request", Convert("XMLHttpRequest"));
            Assert.Equal("camel-case", Convert("camelCase"));
            Assert.Equal("camel-case", Convert("CamelCase"));
            Assert.Equal("snake-case", Convert("snake_case"));
            Assert.Equal("snake-case", Convert("SNAKE_CASE"));
            Assert.Equal("kebab-case", Convert("kebab-case"));
            Assert.Equal("kebab-case", Convert("KEBAB-CASE"));
            Assert.Equal("double-space", Convert("double  space"));
            Assert.Equal("double-underscore", Convert("double__underscore"));
            Assert.Equal("abc", Convert("abc"));
            Assert.Equal("ab-c", Convert("abC"));
            Assert.Equal("a-bc", Convert("aBc"));
            Assert.Equal("a-bc", Convert("aBC"));
            Assert.Equal("a-bc", Convert("ABc"));
            Assert.Equal("abc", Convert("ABC"));
            Assert.Equal("abc123def456", Convert("abc123def456"));
            Assert.Equal("abc123-def456", Convert("abc123Def456"));
            Assert.Equal("abc123-def456", Convert("abc123DEF456"));
            Assert.Equal("abc123def456", Convert("ABC123DEF456"));
            Assert.Equal("abc123def456", Convert("ABC123def456"));
            Assert.Equal("abc123def456", Convert("Abc123def456"));
            Assert.Equal("abc", Convert("  abc"));
            Assert.Equal("abc", Convert("abc  "));
            Assert.Equal("abc", Convert("  abc  "));
            Assert.Equal("abc-def", Convert("  abc def  "));
            
            static string Convert(string name)
            {
                JsonNamingPolicy policy = JsonNamingPolicy.KebabLowerCase;
                string value = policy.ConvertName(name);
                return value;
            }
        }

        [Fact]
        public static void ToKebabUpperCase()
        {
            Assert.Equal("XML-HTTP-REQUEST", Convert("XMLHttpRequest"));
            Assert.Equal("CAMEL-CASE", Convert("camelCase"));
            Assert.Equal("CAMEL-CASE", Convert("CamelCase"));
            Assert.Equal("SNAKE-CASE", Convert("snake_case"));
            Assert.Equal("SNAKE-CASE", Convert("SNAKE_CASE"));
            Assert.Equal("KEBAB-CASE", Convert("kebab-case"));
            Assert.Equal("KEBAB-CASE", Convert("KEBAB-CASE"));
            Assert.Equal("DOUBLE-SPACE", Convert("double  space"));
            Assert.Equal("DOUBLE-UNDERSCORE", Convert("double__underscore"));
            Assert.Equal("ABC", Convert("abc"));
            Assert.Equal("AB-C", Convert("abC"));
            Assert.Equal("A-BC", Convert("aBc"));
            Assert.Equal("A-BC", Convert("aBC"));
            Assert.Equal("A-BC", Convert("ABc"));
            Assert.Equal("ABC", Convert("ABC"));
            Assert.Equal("ABC123DEF456", Convert("abc123def456"));
            Assert.Equal("ABC123-DEF456", Convert("abc123Def456"));
            Assert.Equal("ABC123-DEF456", Convert("abc123DEF456"));
            Assert.Equal("ABC123DEF456", Convert("ABC123DEF456"));
            Assert.Equal("ABC123DEF456", Convert("ABC123def456"));
            Assert.Equal("ABC123DEF456", Convert("Abc123def456"));
            Assert.Equal("ABC", Convert("  ABC"));
            Assert.Equal("ABC", Convert("ABC  "));
            Assert.Equal("ABC", Convert("  ABC  "));
            Assert.Equal("ABC-DEF", Convert("  ABC def  "));
            
            static string Convert(string name)
            {
                JsonNamingPolicy policy = JsonNamingPolicy.KebabUpperCase;
                string value = policy.ConvertName(name);
                return value;
            }
        }
    }
}
