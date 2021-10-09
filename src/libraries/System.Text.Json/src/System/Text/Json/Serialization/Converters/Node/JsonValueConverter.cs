// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonValueConverter : JsonConverter<JsonValue>
    {
        public override void Write(Utf8JsonWriter writer, JsonValue value, JsonSerializerOptions options)
        {
            Debug.Assert(value != null);
            value.WriteTo(writer, options);
        }

        public override JsonValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            JsonElement element = JsonElement.ParseValue(ref reader);
            JsonValue value = new JsonValueTrimmable<JsonElement>(element, JsonMetadataServices.JsonElementConverter, options.GetNodeOptions());
            return value;
        }
    }
}
