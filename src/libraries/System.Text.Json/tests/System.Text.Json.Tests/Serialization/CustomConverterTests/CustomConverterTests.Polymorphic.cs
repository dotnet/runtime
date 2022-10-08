// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CustomConverterTests
    {
        // A polymorphic POCO converter using a type discriminator.
        private class PersonConverterWithTypeDiscriminator : JsonConverter<Person>
        {
            enum TypeDiscriminator
            {
                Customer = 1,
                Employee = 2
            }

            public override bool CanConvert(Type typeToConvert)
            {
                return typeof(Person).IsAssignableFrom(typeToConvert);
            }

            public override Person Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException();
                }

                reader.Read();
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException();
                }

                string propertyName = reader.GetString();
                if (propertyName != "TypeDiscriminator")
                {
                    throw new JsonException();
                }

                reader.Read();
                if (reader.TokenType != JsonTokenType.Number)
                {
                    throw new JsonException();
                }

                Person value;
                TypeDiscriminator typeDiscriminator = (TypeDiscriminator)reader.GetInt32();
                switch (typeDiscriminator)
                {
                    case TypeDiscriminator.Customer:
                        value = new Customer();
                        break;

                    case TypeDiscriminator.Employee:
                        value = new Employee();
                        break;

                    default:
                        throw new JsonException();
                }

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        return value;
                    }

                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        propertyName = reader.GetString();
                        reader.Read();
                        switch (propertyName)
                        {
                            case "CreditLimit":
                                decimal creditLimit = reader.GetDecimal();
                                ((Customer)value).CreditLimit = creditLimit;
                                break;
                            case "OfficeNumber":
                                string officeNumber = reader.GetString();
                                ((Employee)value).OfficeNumber = officeNumber;
                                break;
                            case "Name":
                                string name = reader.GetString();
                                value.Name = name;
                                break;
                        }
                    }
                }

                throw new JsonException();
            }

            public override void Write(Utf8JsonWriter writer, Person value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                if (value is Customer)
                {
                    writer.WriteNumber("TypeDiscriminator", (int)TypeDiscriminator.Customer);
                    writer.WriteNumber("CreditLimit", ((Customer)value).CreditLimit);
                }
                else if (value is Employee)
                {
                    writer.WriteNumber("TypeDiscriminator", (int)TypeDiscriminator.Employee);
                    writer.WriteString("OfficeNumber", ((Employee)value).OfficeNumber);
                }

                writer.WriteString("Name", value.Name);

                writer.WriteEndObject();
            }
        }

        [Fact]
        public static void PersonConverterPolymorphicTypeDiscriminator()
        {
            const string customerJson = @"{""TypeDiscriminator"":1,""CreditLimit"":100.00,""Name"":""C""}";
            const string employeeJson = @"{""TypeDiscriminator"":2,""OfficeNumber"":""77a"",""Name"":""E""}";

            var options = new JsonSerializerOptions();
            options.Converters.Add(new PersonConverterWithTypeDiscriminator());

            {
                Person person = JsonSerializer.Deserialize<Person>(customerJson, options);
                Assert.IsType<Customer>(person);
                Assert.Equal(100, ((Customer)person).CreditLimit);
                Assert.Equal("C", person.Name);

                string json = JsonSerializer.Serialize(person, options);
                Assert.Equal(customerJson, json);
            }

            {
                Person person = JsonSerializer.Deserialize<Person>(employeeJson, options);
                Assert.IsType<Employee>(person);
                Assert.Equal("77a", ((Employee)person).OfficeNumber);
                Assert.Equal("E", person.Name);

                string json = JsonSerializer.Serialize(person, options);
                Assert.Equal(employeeJson, json);
            }
        }

        [Fact]
        public static void NullPersonConverterPolymorphicTypeDiscriminator()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new PersonConverterWithTypeDiscriminator());

            Person person = JsonSerializer.Deserialize<Person>("null");
            Assert.Null(person);
        }

        // A converter that can serialize an abstract Person type.
        private class PersonPolymorphicSerializerConverter : JsonConverter<Person>
        {
            public override Person Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new NotSupportedException($"Deserializing not supported. Type={typeToConvert}.");
            }

            public override void Write(Utf8JsonWriter writer, Person value, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, value, value.GetType(), options);
            }
        }

        [Fact]
        public static void PersonConverterSerializerPolymorphic()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new PersonPolymorphicSerializerConverter());

            Customer customer = new Customer
            {
                Name = "C",
                CreditLimit = 100
            };

            {
                // Verify the polymorphic case.
                Person person = customer;

                string json = JsonSerializer.Serialize(person, options);
                Assert.Contains(@"""CreditLimit"":100", json);
                Assert.Contains(@"""Name"":""C""", json);
                Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<Person>(json, options));

                string arrayJson = JsonSerializer.Serialize(new Person[] { person }, options);
                Assert.Contains(@"""CreditLimit"":100", arrayJson);
                Assert.Contains(@"""Name"":""C""", arrayJson);
                Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<Person[]>(arrayJson, options));
            }

            {
                // Ensure (de)serialization still works when using a Person-derived type. This does not call the custom converter.
                string json = JsonSerializer.Serialize(customer, options);
                Assert.Contains(@"""CreditLimit"":100", json);
                Assert.Contains(@"""Name"":""C""", json);

                customer = JsonSerializer.Deserialize<Customer>(json, options);
                Assert.Equal(100, customer.CreditLimit);
                Assert.Equal("C", customer.Name);

                string arrayJson = JsonSerializer.Serialize(new Customer[] { customer }, options);
                Assert.Contains(@"""CreditLimit"":100", arrayJson);
                Assert.Contains(@"""Name"":""C""", arrayJson);

                Customer[] customers = JsonSerializer.Deserialize<Customer[]>(arrayJson, options);
                Assert.Equal(100, customers[0].CreditLimit);
                Assert.Equal("C", customers[0].Name);
            }
        }

        [Theory]
        [MemberData(nameof(PolymorphicConverter_ShouldRoundtripInAllContexts_GetTestData))]
        public static void PolymorphicConverter_ShouldRoundtripInAllContexts<T>(T value, string expectedJson)
        {
            // Regression test for https://github.com/dotnet/runtime/issues/46522
            string json = JsonSerializer.Serialize(value);
            Assert.Equal(expectedJson, json);

            T deserializedValue = JsonSerializer.Deserialize<T>(json);
            json = JsonSerializer.Serialize(deserializedValue);
            Assert.Equal(expectedJson, json);
        }

        public static IEnumerable<object[]> PolymorphicConverter_ShouldRoundtripInAllContexts_GetTestData()
        {
            var value = new SampleRepro { Value = "string" };
            string expectedJson = "\"string\"";

            yield return WrapArgs(value, expectedJson);
            yield return WrapArgs(new { Value = value }, $@"{{""Value"":{expectedJson}}}");
            yield return WrapArgs(new[] { value }, $"[{expectedJson}]");
            yield return WrapArgs(new Dictionary<string, SampleRepro> { ["key"] = value }, $@"{{""key"":{expectedJson}}}");

            static object[] WrapArgs<T>(T value, string expectedJson) => new object[] { value, expectedJson };
        }

        public interface IRepro<T>
        {
            T Value { get; }
        }

        [JsonConverter(typeof(ReproJsonConverter))]
        public class SampleRepro : IRepro<object>
        {
            public object Value { get; set; }
        }

        public sealed class ReproJsonConverter : JsonConverter<IRepro<object>>
        {
            public override bool CanConvert(Type typeToConvert)
            {
                return typeof(IRepro<object>).IsAssignableFrom(typeToConvert);
            }

            public override IRepro<object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return new SampleRepro { Value = JsonSerializer.Deserialize<object>(ref reader, options) };
            }

            public override void Write(Utf8JsonWriter writer, IRepro<object> value, JsonSerializerOptions options)
            {
                if (value is null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    JsonSerializer.Serialize(writer, value.Value, options);
                }
            }
        }
    }
}
