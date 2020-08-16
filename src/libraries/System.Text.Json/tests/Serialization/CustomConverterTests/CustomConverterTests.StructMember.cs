// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CustomConverterTests
    {
        private class StructToInterfaceConverter : JsonConverter<IMemberInterface>
        {
            public override bool CanConvert(Type typeToConvert)
            {
                return typeof(IMemberInterface).IsAssignableFrom(typeToConvert);
            }

            public override IMemberInterface Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                string value = reader.GetString();

                return new StructMember(value);
            }

            public override void Write(Utf8JsonWriter writer, IMemberInterface value, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, value.Value, typeof(string), options);
            }
        }

        [Fact]
        public static void StructMemberConverter()
        {
            var options = new JsonSerializerOptions()
            {
                IncludeFields = true,
            };
            options.Converters.Add(new StructToInterfaceConverter());

            string json;

            {
                TestClassWithStructMember obj = new TestClassWithStructMember();
                obj.Initialize();
                obj.Verify();
                json = JsonSerializer.Serialize(obj, options);
            }

            {
                TestClassWithStructMember obj = JsonSerializer.Deserialize<TestClassWithStructMember>(json, options);
                obj.Verify();
            }
        }

    }
}
