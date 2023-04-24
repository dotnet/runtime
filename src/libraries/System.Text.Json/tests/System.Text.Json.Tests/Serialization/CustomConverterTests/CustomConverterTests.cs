// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CustomConverterTests
    {
        [Fact]
        public static void MultipleConvertersInObjectArray()
        {
            const string expectedJson = @"[""?"",{""TypeDiscriminator"":1,""CreditLimit"":100,""Name"":""C""},null]";

            var options = new JsonSerializerOptions();
            options.Converters.Add(new MyBoolEnumConverter());
            options.Converters.Add(new PersonConverterWithTypeDiscriminator());

            Customer customer = new Customer();
            customer.CreditLimit = 100;
            customer.Name = "C";

            MyBoolEnum myBoolEnum = MyBoolEnum.Unknown;
            MyBoolEnum? myNullBoolEnum = null;

            string json = JsonSerializer.Serialize(new object[] { myBoolEnum, customer, myNullBoolEnum }, options);
            Assert.Equal(expectedJson, json);

            JsonElement jsonElement = JsonSerializer.Deserialize<JsonElement>(json, options);
            string jsonElementString = jsonElement.ToString();
            Assert.Equal(expectedJson, jsonElementString);
        }

        [Fact]
        public static void OptionsArePassedToCreateConverter()
        {
            TestFactory factory = new TestFactory();
            JsonSerializerOptions options = new JsonSerializerOptions { Converters = { factory } };
            string json = JsonSerializer.Serialize("Test", options);
            Assert.Equal(@"""Test""", json);
            Assert.Same(options, factory.Options);
        }

        public class TestFactory : JsonConverterFactory
        {
            public JsonSerializerOptions Options { get; private set; }

            public override bool CanConvert(Type typeToConvert) => true;

            public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
            {
                Options = options;
                return new SimpleConverter();
            }

            public class SimpleConverter : JsonConverter<string>
            {
                public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                {
                    throw new NotImplementedException();
                }

                public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
                    => writer.WriteStringValue(value);
            }
        }

        private class ConverterReturningNull : JsonConverter<Customer>
        {
            public override Customer Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                Assert.Equal(JsonTokenType.StartObject, reader.TokenType);

                bool rc = reader.Read();
                Assert.True(rc);

                Assert.Equal(JsonTokenType.EndObject, reader.TokenType);

                return null;
            }

            public override void Write(Utf8JsonWriter writer, Customer value, JsonSerializerOptions options)
            {
                throw new NotSupportedException();
            }
        }

        [Fact]
        public static void VerifyConverterWithTrailingWhitespace()
        {
            string json = "{}   ";

            var options = new JsonSerializerOptions();
            options.Converters.Add(new ConverterReturningNull());

            byte[] utf8 = Encoding.UTF8.GetBytes(json);

            // The serializer will finish reading the whitespace and no exception will be thrown.
            Customer c = JsonSerializer.Deserialize<Customer>(utf8, options);

            Assert.Null(c);
        }

        [Fact]
        public static void VerifyConverterWithTrailingComments()
        {
            string json = "{}  //";
            byte[] utf8 = Encoding.UTF8.GetBytes(json);

            // Disallow comments
            var options = new JsonSerializerOptions();
            options.Converters.Add(new ConverterReturningNull());
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Customer>(utf8, options));

            // Skip comments
            options = new JsonSerializerOptions();
            options.Converters.Add(new ConverterReturningNull());
            options.ReadCommentHandling = JsonCommentHandling.Skip;
            Customer c = JsonSerializer.Deserialize<Customer>(utf8, options);
            Assert.Null(c);
        }

        public class ObjectBoolConverter : JsonConverter<object>
        {
            public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.True)
                {
                    return true;
                }

                if (reader.TokenType == JsonTokenType.False)
                {
                    return false;
                }

                throw new JsonException();
            }

            public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public static void VerifyObjectConverterWithPreservedReferences()
        {
            var json = "true";
            byte[] utf8 = Encoding.UTF8.GetBytes(json);

            var options = new JsonSerializerOptions()
            {
                ReferenceHandler = ReferenceHandler.Preserve,
            };
            options.Converters.Add(new ObjectBoolConverter());

            object obj = (JsonSerializer.Deserialize<object>(utf8, options));

            Assert.IsType<bool>(obj);
            Assert.Equal(true, obj);
        }

        [Fact]
        public static void GetConverterRootsBuiltInConverters()
        {
            JsonSerializerOptions options = new();
            RunTest<DateTime>();
            RunTest<Point_2D>();

            void RunTest<TConverterReturn>()
            {
                JsonConverter converter = options.GetConverter(typeof(TConverterReturn));
                Assert.NotNull(converter);
                Assert.True(converter is JsonConverter<TConverterReturn>);
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/66232", TargetFrameworkMonikers.NetFramework)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/66371", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoInterpreter))]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void GetConverter_Poco_WriteThrowsNotSupportedException()
        {
            RemoteExecutor.Invoke(static () =>
            {
                JsonSerializerOptions options = new();
                JsonConverter<Point_2D> converter = (JsonConverter<Point_2D>)options.GetConverter(typeof(Point_2D));

                using var writer = new Utf8JsonWriter(new MemoryStream());
                var value = new Point_2D(0, 0);

                // Running the converter without priming the options instance
                // for reflection-based serialization should throw NotSupportedException
                // since it can't resolve reflection-based metadata.
                Assert.Throws<NotSupportedException>(() => converter.Write(writer, value, options));
                Assert.Equal(0, writer.BytesCommitted + writer.BytesPending);
                options.IncludeFields = false; // options should still be mutable

                JsonSerializer.Serialize(42, options);

                // Same operation should succeed when instance has been primed.
                converter.Write(writer, value, options);
                Assert.NotEqual(0, writer.BytesCommitted + writer.BytesPending);
                writer.Reset();

                Assert.Throws<InvalidOperationException>(() => options.IncludeFields = false);

                // State change should not leak into unrelated options instances.
                var options2 = new JsonSerializerOptions();
                options2.AddContext<JsonContext>();
                Assert.Throws<NotSupportedException>(() => converter.Write(writer, value, options2));
                Assert.Equal(0, writer.BytesCommitted + writer.BytesPending);
            }).Dispose();
        }

        [Fact]
        public static void GetConverterTypeToConvertNull()
        {
            Assert.Throws<ArgumentNullException>(() => (new JsonSerializerOptions()).GetConverter(typeToConvert: null!));
        }

        [Fact]
        public static void ErrorMessageContainsExpectedType()
        {
            JsonSerializerOptions options = new();
            options.Converters.Add(new InvalidJsonConverterFactory());
            var ex = Assert.Throws<InvalidOperationException>(() => 
                JsonSerializer.Serialize(new InvalidTestInfo("Hello"), options));
            Assert.Contains(typeof(InvalidTestInfo).Name, ex.Message);
        }

        private sealed record InvalidTestInfo(string Name);

        private sealed class InvalidJsonConverterFactory : JsonConverterFactory
        {
            public override bool CanConvert(Type typeToConvert) => true;

            public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
                => new MyBoolEnumConverter();
        }
    }
}
