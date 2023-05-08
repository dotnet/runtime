// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class NullableConverter<T> : JsonConverter<T?> where T : struct
    {
        internal override Type? ElementType => typeof(T);
        public override bool HandleNull => true;
        internal override bool CanPopulate => _elementConverter.CanPopulate;

        // It is possible to cache the underlying converter since this is an internal converter and
        // an instance is created only once for each JsonSerializerOptions instance.
        private readonly JsonConverter<T> _elementConverter;

        public NullableConverter(JsonConverter<T> elementConverter)
        {
            _elementConverter = elementConverter;
            IsInternalConverterForNumberType = elementConverter.IsInternalConverterForNumberType;
            ConverterStrategy = elementConverter.ConverterStrategy;
        }

        internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, out T? value)
        {
            if (!state.IsContinuation && reader.TokenType == JsonTokenType.Null)
            {
                value = null;
                return true;
            }

            JsonTypeInfo previousTypeInfo = state.Current.JsonTypeInfo;
            state.Current.JsonTypeInfo = state.Current.JsonTypeInfo.ElementTypeInfo!;
            if (_elementConverter.OnTryRead(ref reader, typeof(T), options, ref state, out T element))
            {
                value = element;
                state.Current.JsonTypeInfo = previousTypeInfo;
                return true;
            }

            state.Current.JsonTypeInfo = previousTypeInfo;
            value = null;
            return false;
        }

        internal override bool OnTryWrite(Utf8JsonWriter writer, T? value, JsonSerializerOptions options, ref WriteStack state)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return true;
            }

            state.Current.JsonPropertyInfo = state.Current.JsonTypeInfo.ElementTypeInfo!.PropertyInfoForTypeInfo;
            return _elementConverter.TryWrite(writer, value.Value, options, ref state);
        }

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            T value = _elementConverter.Read(ref reader, typeof(T), options);
            return value;
        }

        public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                _elementConverter.Write(writer, value.Value, options);
            }
        }

        internal override T? ReadNumberWithCustomHandling(ref Utf8JsonReader reader, JsonNumberHandling numberHandling, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            T value = _elementConverter.ReadNumberWithCustomHandling(ref reader, numberHandling, options);
            return value;
        }

        internal override void WriteNumberWithCustomHandling(Utf8JsonWriter writer, T? value, JsonNumberHandling handling)
        {
            if (value is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                _elementConverter.WriteNumberWithCustomHandling(writer, value.Value, handling);
            }
        }
    }
}
