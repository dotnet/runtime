// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using System.Text.Json.Schema;

namespace System.Text.Json.Serialization.Converters.Node
{
    internal sealed class JsonValuePrimitiveConverter<T> : JsonConverter<JsonValuePrimitive<T>?>
    {
        private readonly JsonConverter<T> _elementConverter;

        public override bool HandleNull => true;
        internal override bool CanPopulate => _elementConverter.CanPopulate;
        internal override bool ConstructorIsParameterized => _elementConverter.ConstructorIsParameterized;
        internal override Type? ElementType => typeof(JsonValuePrimitive<T>);
        internal override JsonConverter? NullableElementConverter => _elementConverter;

        public JsonValuePrimitiveConverter(JsonConverter<T> elementConverter)
        {
            _elementConverter = elementConverter;
        }

        public override JsonValuePrimitive<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            T value = _elementConverter.Read(ref reader, typeof(T), options)!;
            JsonValuePrimitive<T> returnValue = new JsonValuePrimitive<T>(value, _elementConverter, null);

            return returnValue;
        }

        public override void Write(Utf8JsonWriter writer, JsonValuePrimitive<T>? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            value.WriteTo(writer, options);
        }

        internal override JsonSchema? GetSchema(JsonNumberHandling numberHandling) => _elementConverter.GetSchema(numberHandling);
    }
}
