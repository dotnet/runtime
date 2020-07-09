// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CustomConverterTests
    {
        private class FailConverter<TException> : JsonConverter<int> where TException: Exception, new()
        {
            public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new TException();
            }

            public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
            {
                throw new TException();
            }
        }

        private static void ConverterFailNoRethrow<TException>() where TException : Exception, new()
        {
            var options = new JsonSerializerOptions();
            JsonConverter converter = new FailConverter<TException>();
            options.Converters.Add(converter);

            Assert.Throws<TException>(() => JsonSerializer.Deserialize<int>("0", options));
            Assert.Throws<TException>(() => JsonSerializer.Deserialize<int[]>("[0]", options));
            Assert.Throws<TException>(() => JsonSerializer.Serialize(0, options));
            Assert.Throws<TException>(() => JsonSerializer.Serialize(new int[] { 0 }, options));

            var obj = new Dictionary<string, int>();
            obj["key"] = 0;

            Assert.Throws<TException>(() => JsonSerializer.Serialize(obj, options));
        }

        [Fact]
        public static void ConverterExceptionsNotRethrownFail()
        {
            // We should not catch these unless thrown from the reader\document.
            ConverterFailNoRethrow<FormatException>();
            ConverterFailNoRethrow<ArgumentException>();

            // Other misc exception we should not catch:
            ConverterFailNoRethrow<Exception>();
            ConverterFailNoRethrow<InvalidOperationException>();
            ConverterFailNoRethrow<IndexOutOfRangeException>();
            ConverterFailNoRethrow<NotSupportedException>();
        }
    }
}
