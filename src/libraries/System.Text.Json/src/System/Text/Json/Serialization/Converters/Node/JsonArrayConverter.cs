// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Node;

namespace System.Text.Json.Serialization.Converters
{
    internal class JsonArrayConverter : JsonConverter<JsonArray>
    {
        public override void Write(Utf8JsonWriter writer, JsonArray value, JsonSerializerOptions options)
        {
            Debug.Assert(value != null);
            value.WriteTo(writer, options);
        }

        public override JsonArray? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartArray:
                    return ReadList(ref reader, options.GetNodeOptions());
                case JsonTokenType.Null:
                    return null;
                default:
                    Debug.Assert(false);
                    throw ThrowHelper.GetInvalidOperationException_ExpectedArray(reader.TokenType);
            }
        }

        public JsonArray ReadList(ref Utf8JsonReader reader, JsonNodeOptions? options = null)
        {
            JsonElement jElement = JsonElement.ParseValue(ref reader);
            return new JsonArray(jElement, options);
        }
    }
}
