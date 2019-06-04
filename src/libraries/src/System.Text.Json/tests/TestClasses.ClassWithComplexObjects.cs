// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public class ClassWithComplexObjects : ITestClass
    {
        public object Array { get; set; }
        public object Object { get; set; }

        public static readonly string s_array =
            @"[" +
                @"1," +
                @"""Hello""," +
                @"true," +
                @"false," +
                @"{}," +
                @"[2, ""Goodbye"", false, true, {}, [3]]" +
            @"]";

        public static readonly string s_object =
            @"{" +
                @"""NestedArray"" : " +
                s_array +
            @"}";

        public static readonly string s_json =
            @"{" +
                @"""Array"" : " +
                s_array + "," +
                @"""Object"" : " +
                s_object +
            @"}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        public void Initialize()
        {
            Array = JsonDocument.Parse(s_array).RootElement.Clone();
            Object = JsonDocument.Parse(s_object).RootElement.Clone();
        }

        public void Verify()
        {
            Assert.IsType<JsonElement>(Array);
            ValidateArray((JsonElement)Array);
            Assert.IsType<JsonElement>(Object);
            JsonElement jsonObject = (JsonElement)Object;
            Assert.Equal(JsonValueType.Object, jsonObject.Type);
            JsonProperty property = jsonObject.EnumerateObject().First();
            Assert.Equal("NestedArray", property.Name);
            Assert.True(property.NameEquals("NestedArray"));
            ValidateArray(property.Value);

            void ValidateArray(JsonElement element)
            {
                Assert.Equal(JsonValueType.Array, element.Type);
                JsonElement[] elements = element.EnumerateArray().ToArray();

                Assert.Equal(JsonValueType.Number, elements[0].Type);
                Assert.Equal("1", elements[0].ToString());
                Assert.Equal(JsonValueType.String, elements[1].Type);
                Assert.Equal("Hello", elements[1].ToString());
                Assert.Equal(JsonValueType.True, elements[2].Type);
                Assert.Equal(true, elements[2].GetBoolean());
                Assert.Equal(JsonValueType.False, elements[3].Type);
                Assert.Equal(false, elements[3].GetBoolean());
            }

        }
    }
}
