// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Tests
{
    public static partial class ReferenceHandlingTests
    {
        private static JsonSerializerOptions _opts = new JsonSerializerOptions { ReferenceHandlingOnDeserialize = ReferenceHandlingOnDeserialize.PreserveDuplicates };

        private class Person
        {
            public string Name { get; set; }
            public Dictionary<string, int> FriendAges { get; set; }
            public Dictionary<string, int> FriendAgesCopy { get; set; }
        }

        [Fact]
        public static void PreservedDictionaryIsRoot()
        {
            string json = @"
            {
                ""$id"": ""1"",
                ""Angela"": 25,
                ""Bob"": 26,
                ""Carlos"": 27
            }";

            Dictionary<string, int> employeeAges = JsonSerializer.Deserialize<Dictionary<string, int>>(json, _opts);
        }

        [Fact]
        public static void PreservedDictionaryIsNested()
        {
            string json = @"
            {
                ""Name"": ""Angela"",
                ""FriendAges"": {
                    ""$id"": ""1"",
                    ""Angela"": 25,
                    ""Bob"": 26,
                    ""Carlos"": 27
                },
                ""FriendAgesCopy"": { ""$ref"": ""1"" }
            }";

            Person employee = JsonSerializer.Deserialize<Person>(json, _opts);
        }


        // VALIDATE THIS LATER,
        //Xunit.Sdk.ThrowsException: 'Assert.Throws() Failure
        //Expected: typeof(System.Text.Json.JsonException)
        //Actual:   typeof(System.Collections.Generic.KeyNotFoundException): The given key '$values' was not present in the dictionary.'
        //[Fact]
        //public static void PreservedArrayAsRootIncompleteValues()
        //{
        //    string json = @"
        //    {
        //        ""$id"": ""1"",
        //        ""$values"": [
        //            10";

        //    //List<int> myList = JsonSerializer.Deserialize<List<int>>(json, _opts);
        //    JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<MyClass>(json, _opts));
        //}

        [Fact]
        public static void PreservedArrayAsRootIncompleteId()
        {
            string json = @"
            {
                ""$id"": ""1";

            //List<int> myList = JsonSerializer.Deserialize<List<int>>(json, _opts);
            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<int>>(json, _opts));
        }

        [Fact]
        public static void PreservedDictionaryIsRootIncompleteValue()
        {
            string json = @"
            {
                ""Angela"": 25";

            //Dictionary<string, int> employeeAges = JsonSerializer.Deserialize<Dictionary<string, int>>(json, _opts);
            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Dictionary<string, int>>(json, _opts));
        }
    }
}
