// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public class SnakeCaseUnitTests
    {
        [Fact]
        public static void ToCamelCaseTest()
        {
            // These test cases were copied from Json.NET.
            Assert.Equal("url_value", ConvertToSnakeCase("URLValue"));
            Assert.Equal("url", ConvertToSnakeCase("URL"));
            Assert.Equal("id", ConvertToSnakeCase("ID"));
            Assert.Equal("i", ConvertToSnakeCase("I"));
            Assert.Equal("", ConvertToSnakeCase(""));
            Assert.Null(ConvertToSnakeCase(null));
            Assert.Equal("person", ConvertToSnakeCase("Person"));
            Assert.Equal("i_phone", ConvertToSnakeCase("iPhone"));
            Assert.Equal("i_phone", ConvertToSnakeCase("IPhone"));
            Assert.Equal("i_phone", ConvertToSnakeCase("I Phone"));
            Assert.Equal("i_phone", ConvertToSnakeCase("I  Phone"));
            Assert.Equal("i_phone", ConvertToSnakeCase(" IPhone"));
            Assert.Equal("i_phone", ConvertToSnakeCase(" IPhone "));
            Assert.Equal("is_cia", ConvertToSnakeCase("IsCIA"));
            Assert.Equal("vm_q", ConvertToSnakeCase("VmQ"));
            Assert.Equal("xml2_json", ConvertToSnakeCase("Xml2Json"));
            Assert.Equal("sn_ak_ec_as_e", ConvertToSnakeCase("SnAkEcAsE"));
            Assert.Equal("sn_a__k_ec_as_e", ConvertToSnakeCase("SnA__kEcAsE"));
            Assert.Equal("sn_a__k_ec_as_e", ConvertToSnakeCase("SnA__ kEcAsE"));
            Assert.Equal("already_snake_case_", ConvertToSnakeCase("already_snake_case_ "));
            Assert.Equal("is_json_property", ConvertToSnakeCase("IsJSONProperty"));
            Assert.Equal("shouting_case", ConvertToSnakeCase("SHOUTING_CASE"));
            Assert.Equal("9999-12-31_t23:59:59.9999999_z", ConvertToSnakeCase("9999-12-31T23:59:59.9999999Z"));
            Assert.Equal("hi!!_this_is_text._time_to_test.", ConvertToSnakeCase("Hi!! This is text. Time to test."));
        }

        // Use a helper method since the method is not public.
        private static string ConvertToSnakeCase(string name)
        {
            JsonNamingPolicy policy = JsonNamingPolicy.SnakeCase;
            string value = policy.ConvertName(name);
            return value;
        }
    }
}
