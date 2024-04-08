// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        public string PlaceOfBirth { get; set; }

        public void Validate()
        {
            Assert.Equal("R. Daneel Olivaw", Name);
            Assert.Equal(19_230, Age);
            Assert.Equal("Horn\u00ED Doln\u00ED", PlaceOfBirth);
            Assert.Null(Parent);
        }

        public static Person Create()
        {
            return new Person { Name = "R. Daneel Olivaw", Age = 19_230, PlaceOfBirth = "Horn\u00ED Doln\u00ED"};
        }

        public string Serialize(JsonSerializerOptions options = null)
        {
            return JsonSerializer.Serialize(this, options);
        }

        public string SerializeWithNumbersAsStrings(JsonSerializerOptions options = null)
        {
            options ??= new JsonSerializerOptions();
            options.NumberHandling = options.NumberHandling | JsonNumberHandling.WriteAsString;
            return JsonSerializer.Serialize(this, options);
        }

        public static void AssertPersonEquality(Person first, Person second)
        {
            Assert.Equal(first.Age, second.Age);
            Assert.Equal(first.Name, second.Name);
            Assert.Equal(first.Parent, second.Parent);
            Assert.Equal(first.PlaceOfBirth, second.PlaceOfBirth);
        }
    }

    internal class People
    {
        public static int PeopleCount => WomenOfProgramming.Length;

        public static Person[] WomenOfProgramming = new[]
        {
            new Person { Name = "Ada Lovelace", Age = 13_140, PlaceOfBirth = "London, England" },
            new Person { Name = "Jean Bartik", Age = 31_390, PlaceOfBirth = "Alanthus Grove, Missouri, U.S." },
            new Person { Name = "Grace Hopper", Age = 31_025, PlaceOfBirth = "New York City, New York, U.S." },
            new Person { Name = "Margaret Hamilton", Age = 31_390, PlaceOfBirth = "Paoli, Indiana, U.S." },
        };

        public static string Serialize(JsonSerializerOptions options = null)
        {
            return JsonSerializer.Serialize(WomenOfProgramming, options);
        }
    }

    internal static class JsonOptions
    {
        public static readonly JsonSerializerOptions DefaultSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        public static readonly JsonSerializerOptions DefaultSerializerOptions_StrictNumberHandling = new JsonSerializerOptions(DefaultSerializerOptions)
        {
            NumberHandling = JsonNumberHandling.Strict
        };
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
            Assert.Same(JsonNamingPolicy.CamelCase, options.PropertyNamingPolicy);
            Assert.Equal(JsonNumberHandling.AllowReadingFromString, options.NumberHandling);
        }
    }

    [JsonConverter(typeof(EnsureDefaultOptionsConverter))]
    internal class EnsureDefaultOptions { }

    [JsonSerializable(typeof(Person))]
    internal sealed partial class JsonContext : JsonSerializerContext
    {
    }
}
