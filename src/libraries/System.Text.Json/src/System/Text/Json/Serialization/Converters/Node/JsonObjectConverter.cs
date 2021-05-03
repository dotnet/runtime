// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Node;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonObjectConverter : JsonConverter<JsonObject>
    {
        internal override object CreateObject(JsonSerializerOptions options)
        {
            return new JsonObject(options.GetNodeOptions());
        }

        internal override void ReadElementAndSetProperty(
            object obj,
            string propertyName,
            ref Utf8JsonReader reader,
            JsonSerializerOptions options,
            ref ReadStack state)
        {
            bool success = JsonNodeConverter.Instance.TryRead(ref reader, typeof(JsonNode), options, ref state, out object? value);
            Debug.Assert(success); // Node converters are not resumable.

            Debug.Assert(obj is JsonObject);
            JsonObject jObject = (JsonObject)obj;

            Debug.Assert(value == null || value is JsonNode);
            JsonNode? jNodeValue = (JsonNode?)value;

            jObject[propertyName] = jNodeValue;
        }

        public override void Write(Utf8JsonWriter writer, JsonObject value, JsonSerializerOptions options)
        {
            Debug.Assert(value != null);
            value.WriteTo(writer, options);
        }

        public override JsonObject? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    return ReadObject(ref reader, options.GetNodeOptions());
                default:
                    Debug.Assert(false);
                    throw ThrowHelper.GetInvalidOperationException_ExpectedObject(reader.TokenType);
            }
        }

        public JsonObject ReadObject(ref Utf8JsonReader reader, JsonNodeOptions? options)
        {
            JsonElement jElement = JsonElement.ParseValue(ref reader);
            JsonObject jObject = new JsonObject(jElement, options);
            return jObject;
        }
    }
}
