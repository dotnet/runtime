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
        internal const string CommentPropertyName = "$comment";
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
        internal const string MinLengthPropertyName = "minLength";
        internal const string MaxLengthPropertyName = "maxLength";

        public static JsonSchema False { get; } = new(false);
        public static JsonSchema True { get; } = new(true);

        public JsonSchema() { }
        private JsonSchema(bool trueOrFalse) { _trueOrFalse = trueOrFalse; }

        public bool IsTrue => _trueOrFalse is true;
        public bool IsFalse => _trueOrFalse is false;
        private readonly bool? _trueOrFalse;

        public string? Ref { get => _ref; set { VerifyMutable(); _ref = value; } }
        private string? _ref;

        public string? Comment { get => _comment; set { VerifyMutable(); _comment = value; } }
        private string? _comment;

        public JsonSchemaType Type { get => _type; set { VerifyMutable(); _type = value; } }
        private JsonSchemaType _type = JsonSchemaType.Any;

        public string? Format { get => _format; set { VerifyMutable(); _format = value; } }
        private string? _format;

        public string? Pattern { get => _pattern; set { VerifyMutable(); _pattern = value; } }
        private string? _pattern;

        public JsonNode? Constant { get => _constant; set { VerifyMutable(); _constant = value; } }
        private JsonNode? _constant;

        public List<KeyValuePair<string, JsonSchema>>? Properties { get => _properties; set { VerifyMutable(); _properties = value; } }
        private List<KeyValuePair<string, JsonSchema>>? _properties;

        public List<string>? Required { get => _required; set { VerifyMutable(); _required = value; } }
        private List<string>? _required;

        public JsonSchema? Items { get => _items; set { VerifyMutable(); _items = value; } }
        private JsonSchema? _items;

        public JsonSchema? AdditionalProperties { get => _additionalProperties; set { VerifyMutable(); _additionalProperties = value; } }
        private JsonSchema? _additionalProperties;

        public JsonArray? Enum { get => _enum; set { VerifyMutable(); _enum = value; } }
        private JsonArray? _enum;

        public JsonSchema? Not { get => _not; set { VerifyMutable(); _not = value; } }
        private JsonSchema? _not;

        public List<JsonSchema>? AnyOf { get => _anyOf; set { VerifyMutable(); _anyOf = value; } }
        private List<JsonSchema>? _anyOf;

        public bool HasDefaultValue { get => _hasDefaultValue; set { VerifyMutable(); _hasDefaultValue = value; } }
        private bool _hasDefaultValue;

        public JsonNode? DefaultValue { get => _defaultValue; set { VerifyMutable(); _defaultValue = value; } }
        private JsonNode? _defaultValue;

        public int? MinLength { get => _minLength; set { VerifyMutable(); _minLength = value; } }
        private int? _minLength;

        public int? MaxLength { get => _maxLength; set { VerifyMutable(); _maxLength = value; } }
        private int? _maxLength;

        public JsonSchemaExporterContext? ExporterContext { get; set; }

        public int KeywordCount
        {
            get
            {
                if (_trueOrFalse != null)
                {
                    return 0;
                }

                int count = 0;
                Count(Ref != null);
                Count(Comment != null);
                Count(Type != JsonSchemaType.Any);
                Count(Format != null);
                Count(Pattern != null);
                Count(Constant != null);
                Count(Properties != null);
                Count(Required != null);
                Count(Items != null);
                Count(AdditionalProperties != null);
                Count(Enum != null);
                Count(Not != null);
                Count(AnyOf != null);
                Count(HasDefaultValue);
                Count(MinLength != null);
                Count(MaxLength != null);

                return count;

                void Count(bool isKeywordSpecified)
                {
                    count += isKeywordSpecified ? 1 : 0;
                }
            }
        }

        public void MakeNullable()
        {
            if (_trueOrFalse != null)
            {
                return;
            }

            if (Type != JsonSchemaType.Any)
            {
                Type |= JsonSchemaType.Null;
            }
        }

        public JsonNode ToJsonNode(JsonSchemaExporterOptions options)
        {
            if (_trueOrFalse is { } boolSchema)
            {
                return CompleteSchema((JsonNode)boolSchema);
            }

            var objSchema = new JsonObject();

            if (Ref != null)
            {
                objSchema.Add(RefPropertyName, Ref);
            }

            if (Comment != null)
            {
                objSchema.Add(CommentPropertyName, Comment);
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
                JsonArray anyOfArray = [];
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

            if (MinLength is int minLength)
            {
                objSchema.Add(MinLengthPropertyName, (JsonNode)minLength);
            }

            if (MaxLength is int maxLength)
            {
                objSchema.Add(MaxLengthPropertyName, (JsonNode)maxLength);
            }

            return CompleteSchema(objSchema);

            JsonNode CompleteSchema(JsonNode schema)
            {
                if (ExporterContext is { } context)
                {
                    Debug.Assert(options.TransformSchemaNode != null, "context should only be populated if a callback is present.");
                    // Apply any user-defined transformations to the schema.
                    return options.TransformSchemaNode(context, schema);
                }

                return schema;
            }
        }

        private static ReadOnlySpan<JsonSchemaType> s_schemaValues =>
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

        private void VerifyMutable()
        {
            Debug.Assert(_trueOrFalse is null, "Schema is not mutable");
            if (_trueOrFalse is not null)
            {
                Throw();
                static void Throw() => throw new InvalidOperationException();
            }
        }

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
