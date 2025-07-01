// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonObjectConverter : JsonConverter<JsonObject?>
    {
        internal override void ConfigureJsonTypeInfo(JsonTypeInfo jsonTypeInfo, JsonSerializerOptions options)
        {
            jsonTypeInfo.CreateObjectForExtensionDataProperty = () => new JsonObject(options.GetNodeOptions());
        }

        internal override void ReadElementAndSetProperty(
            object obj,
            string propertyName,
            ref Utf8JsonReader reader,
            JsonSerializerOptions options,
            scoped ref ReadStack state)
        {
            bool success = JsonNodeConverter.Instance.TryRead(ref reader, typeof(JsonNode), options, ref state, out JsonNode? value, out _);
            Debug.Assert(success); // Node converters are not resumable.

            Debug.Assert(obj is JsonObject);
            JsonObject jObject = (JsonObject)obj;

            Debug.Assert(value == null || value is JsonNode);
            JsonNode? jNodeValue = value;

            if (options.AllowDuplicateProperties)
            {
                jObject[propertyName] = jNodeValue;
            }
            else
            {
                // TODO: Use TryAdd once https://github.com/dotnet/runtime/issues/110244 is resolved.
                if (jObject.ContainsKey(propertyName))
                {
                    ThrowHelper.ThrowJsonException_DuplicatePropertyNotAllowed(propertyName);
                }

                jObject.Add(propertyName, jNodeValue);
            }
        }

        public override void Write(Utf8JsonWriter writer, JsonObject? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            value.WriteTo(writer, options);
        }

        public override JsonObject? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    return options.AllowDuplicateProperties
                        ? ReadAsJsonElement(ref reader, options.GetNodeOptions())
                        : ReadAsJsonNode(ref reader, options.GetNodeOptions());
                case JsonTokenType.Null:
                    return null;
                default:
                    throw ThrowHelper.GetInvalidOperationException_ExpectedObject(reader.TokenType);
            }
        }

        internal static JsonObject ReadAsJsonElement(ref Utf8JsonReader reader, JsonNodeOptions options)
        {
            JsonElement jElement = JsonElement.ParseValue(ref reader);
            return new JsonObject(jElement, options);
        }

        internal static JsonObject ReadAsJsonNode(ref Utf8JsonReader reader, JsonNodeOptions options)
        {
            Debug.Assert(reader.TokenType == JsonTokenType.StartObject);

            JsonObject jObject = new JsonObject(options);

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return jObject;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    // JSON is invalid so reader would have already thrown.
                    Debug.Fail("Property name expected.");
                    ThrowHelper.ThrowJsonException();
                }

                string propertyName = reader.GetString()!;
                reader.Read(); // Move to the value token.
                JsonNode? value = JsonNodeConverter.ReadAsJsonNode(ref reader, options);

                // To have parity with the lazy JsonObject, we throw on duplicates.
                jObject.Add(propertyName, value);
            }

            // JSON is invalid so reader would have already thrown.
            Debug.Fail("End object token not found.");
            ThrowHelper.ThrowJsonException();
            return null;
        }

        internal override JsonSchema? GetSchema(JsonNumberHandling _) => new() { Type = JsonSchemaType.Object };
    }
}
