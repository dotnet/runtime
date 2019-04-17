// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static class CamelCaseUnitTests
    {
        [Fact]
        public static void ToCamelCaseTest()
        {
            // These test cases were copied from Json.NET.
            Assert.Equal("urlValue", ConvertToCamelCase("URLValue"));
            Assert.Equal("url", ConvertToCamelCase("URL"));
            Assert.Equal("id", ConvertToCamelCase("ID"));
            Assert.Equal("i", ConvertToCamelCase("I"));
            Assert.Equal("", ConvertToCamelCase(""));
            Assert.Equal(null, ConvertToCamelCase(null));
            Assert.Equal("person", ConvertToCamelCase("Person"));
            Assert.Equal("iPhone", ConvertToCamelCase("iPhone"));
            Assert.Equal("iPhone", ConvertToCamelCase("IPhone"));
            Assert.Equal("i Phone", ConvertToCamelCase("I Phone"));
            Assert.Equal("i  Phone", ConvertToCamelCase("I  Phone"));
            Assert.Equal(" IPhone", ConvertToCamelCase(" IPhone"));
            Assert.Equal(" IPhone ", ConvertToCamelCase(" IPhone "));
            Assert.Equal("isCIA", ConvertToCamelCase("IsCIA"));
            Assert.Equal("vmQ", ConvertToCamelCase("VmQ"));
            Assert.Equal("xml2Json", ConvertToCamelCase("Xml2Json"));
            Assert.Equal("snAkEcAsE", ConvertToCamelCase("SnAkEcAsE"));
            Assert.Equal("snA__kEcAsE", ConvertToCamelCase("SnA__kEcAsE"));
            Assert.Equal("snA__ kEcAsE", ConvertToCamelCase("SnA__ kEcAsE"));
            Assert.Equal("already_snake_case_ ", ConvertToCamelCase("already_snake_case_ "));
            Assert.Equal("isJSONProperty", ConvertToCamelCase("IsJSONProperty"));
            Assert.Equal("shoutinG_CASE", ConvertToCamelCase("SHOUTING_CASE"));
            Assert.Equal("9999-12-31T23:59:59.9999999Z", ConvertToCamelCase("9999-12-31T23:59:59.9999999Z"));
            Assert.Equal("hi!! This is text. Time to test.", ConvertToCamelCase("Hi!! This is text. Time to test."));
            Assert.Equal("building", ConvertToCamelCase("BUILDING"));
            Assert.Equal("building Property", ConvertToCamelCase("BUILDING Property"));
            Assert.Equal("building Property", ConvertToCamelCase("Building Property"));
            Assert.Equal("building PROPERTY", ConvertToCamelCase("BUILDING PROPERTY"));
        }

        // Use a helper method since the method is not public.
        private static string ConvertToCamelCase(string name)
        {
            JsonNamingPolicy policy = JsonNamingPolicy.CamelCase;
            string value = policy.ConvertName(name);
            return value;
        }
    }
}
