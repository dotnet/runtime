// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace System.Net.Http.Json.Functional.Tests
{
    internal class Person
    {
        public int Age { get; set; }
        public string Name { get; set; }
        public Person Parent { get; set; }

        public void Validate()
        {
            Assert.Equal("David", Name);
            Assert.Equal(24, Age);
            Assert.Null(Parent);
        }

        public static Person Create()
        {
            return new Person { Name = "David", Age = 24 };
        }

        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }
    }

    internal class EnsureDefaultOptionsConverter : JsonConverter<EnsureDefaultOptions>
    {
        public override EnsureDefaultOptions Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            AssertDefaultOptions(options);

            while (reader.TokenType != JsonTokenType.EndObject)
            {
                reader.Read();
            }
            return new EnsureDefaultOptions();
        }

        public override void Write(Utf8JsonWriter writer, EnsureDefaultOptions value, JsonSerializerOptions options)
        {
            AssertDefaultOptions(options);

            writer.WriteStartObject();
            writer.WriteEndObject();
        }

        private static void AssertDefaultOptions(JsonSerializerOptions options)
        {
            Assert.True(options.PropertyNameCaseInsensitive);
            Assert.Equal(JsonNamingPolicy.CamelCase, options.PropertyNamingPolicy);
        }
    }

    [JsonConverter(typeof(EnsureDefaultOptionsConverter))]
    internal class EnsureDefaultOptions { }
}
