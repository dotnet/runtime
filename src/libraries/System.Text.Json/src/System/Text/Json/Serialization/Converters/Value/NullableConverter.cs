// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization
{
    internal class NullableConverter<T> : JsonConverter<T?> where T : struct
    {
        // It is possible to cache the underlying converter since this is an internal converter and
        // an instance is created only once for each JsonSerializerOptions instance.
        private readonly JsonConverter<T> _converter;

        public NullableConverter(JsonConverter<T> converter)
        {
            _converter = converter;
        }

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            T value = _converter.Read(ref reader, typeof(T), options);
            return value;
        }

        public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNullValue();
            }
            else
            {
                _converter.Write(writer, value.Value, options);
            }
        }
    }
}
