// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Node;

namespace System.Text.Json.Serialization.Converters
{
    internal class JsonObjectConverter : JsonConverter<JsonObject>
    {
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
                case JsonTokenType.Null:
                    return null;
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
