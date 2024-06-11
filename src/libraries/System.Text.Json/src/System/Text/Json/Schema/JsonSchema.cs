// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Nodes;

namespace System.Text.Json.Schema
{
    internal sealed class JsonSchema
    {
        internal const string RefPropertyName = "$ref";
        internal const string TypePropertyName = "type";
        internal const string FormatPropertyName = "format";
        internal const string PatternPropertyName = "pattern";
        internal const string PropertiesPropertyName = "properties";
        internal const string RequiredPropertyName = "required";
        internal const string ItemsPropertyName = "items";
        internal const string AdditionalPropertiesPropertyName = "additionalProperties";
        internal const string EnumPropertyName = "enum";
        internal const string NotPropertyName = "not";
        internal const string AnyOfPropertyName = "anyOf";
        internal const string ConstPropertyName = "const";
        internal const string DefaultPropertyName = "default";

        public static JsonSchema False { get; } = new() { TrueOrFalse = false };
        public static JsonSchema True { get; } = new() { TrueOrFalse = true };

        public static JsonSchema CreateFalseSchemaAsObject() => new() { Not = True };
        public static JsonSchema CreateTrueSchemaAsObject() => new();

        public bool? TrueOrFalse { get; private init; }
        public string? Ref { get; set; }
        public JsonSchemaType Type { get; set; } = JsonSchemaType.Any;
        public string? Format { get; set; }
        public string? Pattern { get; set; }
        public JsonNode? Constant { get; set; }
        public List<KeyValuePair<string, JsonSchema>>? Properties { get; set; }
        public List<string>? Required { get; set; }
        public JsonSchema? Items { get; set; }
        public JsonSchema? AdditionalProperties { get; set; }
        public JsonArray? Enum { get; set; }
        public JsonSchema? Not { get; set; }
        public List<JsonSchema>? AnyOf { get; set; }
        public bool HasDefaultValue { get; set; }
        public JsonNode? DefaultValue { get; set; }

        public JsonSchemaExporterContext? ExporterContext { get; set; }

        public void MakeNullable()
        {
            if (Type != JsonSchemaType.Any)
            {
                Type |= JsonSchemaType.Null;
            }
        }

        public JsonNode ToJsonNode(JsonSchemaExporterOptions options)
        {
            if (TrueOrFalse is { } boolSchema)
            {
                return (JsonNode)boolSchema;
            }

            var objSchema = new JsonObject();

            if (Ref != null)
            {
                objSchema.Add(RefPropertyName, Ref);
            }

            if (MapSchemaType(Type) is JsonNode type)
            {
                objSchema.Add(TypePropertyName, type);
            }

            if (Format != null)
            {
                objSchema.Add(FormatPropertyName, Format);
            }

            if (Pattern != null)
            {
                objSchema.Add(PatternPropertyName, Pattern);
            }

            if (Constant != null)
            {
                objSchema.Add(ConstPropertyName, Constant);
            }

            if (Properties != null)
            {
                var properties = new JsonObject();
                foreach (KeyValuePair<string, JsonSchema> property in Properties)
                {
                    properties.Add(property.Key, property.Value.ToJsonNode(options));
                }

                objSchema.Add(PropertiesPropertyName, properties);
            }

            if (Required != null)
            {
                var requiredArray = new JsonArray();
                foreach (string requiredProperty in Required)
                {
                    requiredArray.Add((JsonNode)requiredProperty);
                }

                objSchema.Add(RequiredPropertyName, requiredArray);
            }

            if (Items != null)
            {
                objSchema.Add(ItemsPropertyName, Items.ToJsonNode(options));
            }

            if (AdditionalProperties != null)
            {
                objSchema.Add(AdditionalPropertiesPropertyName, AdditionalProperties.ToJsonNode(options));
            }

            if (Enum != null)
            {
                objSchema.Add(EnumPropertyName, Enum);
            }

            if (Not != null)
            {
                objSchema.Add(NotPropertyName, Not.ToJsonNode(options));
            }

            if (AnyOf != null)
            {
                var anyOfArray = new JsonArray();
                foreach (JsonSchema schema in AnyOf)
                {
                    anyOfArray.Add(schema.ToJsonNode(options));
                }

                objSchema.Add(AnyOfPropertyName, anyOfArray);
            }

            if (HasDefaultValue)
            {
                objSchema.Add(DefaultPropertyName, DefaultValue);
            }

            if (ExporterContext is { } context)
            {
                Debug.Assert(options.OnSchemaNodeGenerated != null, "context should only be populated if a callback is present.");
                // Apply any user-defined transformations to the schema.
                options.OnSchemaNodeGenerated(context, objSchema);
            }

            if (objSchema.Count == 0)
            {
                // Transform '{}' schemas to 'true'.
                return (JsonNode)true;
            }
            else if (
                objSchema.Count == 1 &&
                objSchema.TryGetPropertyValue(NotPropertyName, out JsonNode? notValue) &&
                notValue?.GetValueKind() is JsonValueKind.True)
            {
                // Transform '{ "not": true }' schemas to 'false'.
                return (JsonNode)false;
            }

            return objSchema;
        }

        private static readonly JsonSchemaType[] s_schemaValues =
        [
            // NB the order of these values influences order of types in the rendered schema
            JsonSchemaType.String,
            JsonSchemaType.Integer,
            JsonSchemaType.Number,
            JsonSchemaType.Boolean,
            JsonSchemaType.Array,
            JsonSchemaType.Object,
            JsonSchemaType.Null,
        ];

        public static JsonNode? MapSchemaType(JsonSchemaType schemaType)
        {
            if (schemaType is JsonSchemaType.Any)
            {
                return null;
            }

            if (ToIdentifier(schemaType) is string identifier)
            {
                return identifier;
            }

            var array = new JsonArray();
            foreach (JsonSchemaType type in s_schemaValues)
            {
                if ((schemaType & type) != 0)
                {
                    array.Add((JsonNode)ToIdentifier(type)!);
                }
            }

            return array;

            static string? ToIdentifier(JsonSchemaType schemaType)
            {
                return schemaType switch
                {
                    JsonSchemaType.Null => "null",
                    JsonSchemaType.Boolean => "boolean",
                    JsonSchemaType.Integer => "integer",
                    JsonSchemaType.Number => "number",
                    JsonSchemaType.String => "string",
                    JsonSchemaType.Array => "array",
                    JsonSchemaType.Object => "object",
                    _ => null,
                };
            }
        }
    }
}
