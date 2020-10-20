// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class ObjectConverter : JsonConverter<object>
    {
        public ObjectConverter()
        {
            IsInternalConverterForNumberType = true;
        }

        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonElement.ParseValue(ref reader);
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            throw new InvalidOperationException();
        }

        internal override object ReadWithQuotes(ref Utf8JsonReader reader)
        {
            ThrowHelper.ThrowNotSupportedException_DictionaryKeyTypeNotSupported(TypeToConvert);
            return null!;
        }

        internal override void WriteWithQuotes(Utf8JsonWriter writer, object value, JsonSerializerOptions options, ref WriteStack state)
        {
            JsonConverter runtimeConverter = GetRuntimeConverter(value.GetType(), options);
            runtimeConverter.WriteWithQuotesAsObject(writer, value, options, ref state);
        }

        private JsonConverter GetRuntimeConverter(Type runtimeType, JsonSerializerOptions options)
        {
            JsonConverter runtimeConverter = options.GetDictionaryKeyConverter(runtimeType);
            if (runtimeConverter == this)
            {
                ThrowHelper.ThrowNotSupportedException_DictionaryKeyTypeNotSupported(runtimeType);
            }

            return runtimeConverter;
        }

        internal override object ReadNumberWithCustomHandling(ref Utf8JsonReader reader, JsonNumberHandling handling)
        {
            return JsonElement.ParseValue(ref reader);
        }
    }
}
