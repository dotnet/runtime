// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class NullableConverter<T> : JsonConverter<T?> where T : struct
    {
        // It is possible to cache the underlying converter since this is an internal converter and
        // an instance is created only once for each JsonSerializerOptions instance.
        private readonly JsonConverter<T> _converter;

        public NullableConverter(JsonConverter<T> converter)
        {
            _converter = converter;
            IsInternalConverterForNumberType = converter.IsInternalConverterForNumberType;
        }

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // We do not check _converter.HandleNull, as the underlying struct cannot be null.
            // A custom converter for some type T? can handle null.
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
                // We do not check _converter.HandleNull, as the underlying struct cannot be null.
                // A custom converter for some type T? can handle null.
                writer.WriteNullValue();
            }
            else
            {
                _converter.Write(writer, value.Value, options);
            }
        }

        internal override T? ReadNumberWithCustomHandling(ref Utf8JsonReader reader, JsonNumberHandling numberHandling)
        {
            // We do not check _converter.HandleNull, as the underlying struct cannot be null.
            // A custom converter for some type T? can handle null.
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            T value = _converter.ReadNumberWithCustomHandling(ref reader, numberHandling);
            return value;
        }

        internal override void WriteNumberWithCustomHandling(Utf8JsonWriter writer, T? value, JsonNumberHandling handling)
        {
            if (!value.HasValue)
            {
                // We do not check _converter.HandleNull, as the underlying struct cannot be null.
                // A custom converter for some type T? can handle null.
                writer.WriteNullValue();
            }
            else
            {
                _converter.WriteNumberWithCustomHandling(writer, value.Value, handling);
            }
        }

        internal override bool IsNull(in T? value) => !value.HasValue;
    }
}
