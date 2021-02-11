// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            Assert.Equal(JsonValueKind.Object, jsonObject.ValueKind);
            JsonElement.ObjectEnumerator enumerator = jsonObject.EnumerateObject();
            JsonProperty property = enumerator.First();
            Assert.Equal("NestedArray", property.Name);
            Assert.True(property.NameEquals("NestedArray"));
            ValidateArray(property.Value);

            void ValidateArray(JsonElement element)
            {
                Assert.Equal(JsonValueKind.Array, element.ValueKind);
                JsonElement[] elements = element.EnumerateArray().ToArray();

                Assert.Equal(JsonValueKind.Number, elements[0].ValueKind);
                Assert.Equal("1", elements[0].ToString());
                Assert.Equal(JsonValueKind.String, elements[1].ValueKind);
                Assert.Equal("Hello", elements[1].ToString());
                Assert.Equal(JsonValueKind.True, elements[2].ValueKind);
                Assert.True(elements[2].GetBoolean());
                Assert.Equal(JsonValueKind.False, elements[3].ValueKind);
                Assert.False(elements[3].GetBoolean());
            }

        }
    }
}
