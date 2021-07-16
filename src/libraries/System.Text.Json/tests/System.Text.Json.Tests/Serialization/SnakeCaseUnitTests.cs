// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static class SnakeCaseUnitTests
    {
        [Fact]
        public static void ToSnakeCaseTest()
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
            Assert.Equal("_i_phone", ConvertToSnakeCase(" IPhone"));
            Assert.Equal("_i_phone_", ConvertToSnakeCase(" IPhone "));
            Assert.Equal("is_cia", ConvertToSnakeCase("IsCIA"));
            Assert.Equal("vm_q", ConvertToSnakeCase("VmQ"));
            Assert.Equal("xml_2_json", ConvertToSnakeCase("Xml2Json"));
            Assert.Equal("xml_2_net_6_0_json", ConvertToSnakeCase("Xml2Net60Json"));
            Assert.Equal("sn_ak_ec_as_e", ConvertToSnakeCase("SnAkEcAsE"));
            Assert.Equal("sn_a_k_ec_as_e", ConvertToSnakeCase("SnA__kEcAsE"));
            Assert.Equal("sn_a_k_ec_as_e", ConvertToSnakeCase("SnA__ kEcAsE"));
            Assert.Equal("already_snake_case_", ConvertToSnakeCase("already_snake_case_ "));
            Assert.Equal("is_json_property", ConvertToSnakeCase("IsJSONProperty"));
            Assert.Equal("shouting_case", ConvertToSnakeCase("SHOUTING_CASE"));
            Assert.Equal("hi_this_is_text_time_to_test_", ConvertToSnakeCase("Hi!! This is text. Time to test."));
            Assert.Equal("building", ConvertToSnakeCase("BUILDING"));
            Assert.Equal("building_property", ConvertToSnakeCase("BUILDING Property"));
            Assert.Equal("building_property", ConvertToSnakeCase("Building Property"));
            Assert.Equal("building_property", ConvertToSnakeCase("BUILDING PROPERTY"));
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
