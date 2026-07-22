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
        internal const string DeprecatedPropertyName = "deprecated";

        public static JsonSchema CreateFalseSchema() => new(false);
        public static JsonSchema CreateTrueSchema() => new(true);

        public JsonSchema() { }
        private JsonSchema(bool trueOrFalse) { _trueOrFalse = trueOrFalse; }

        public bool IsTrue => _trueOrFalse is true;
        public bool IsFalse => _trueOrFalse is false;

        /// <summary>
        /// Per the JSON schema core specification section 4.3
        /// (https://json-schema.org/draft/2020-12/json-schema-core#name-json-schema-documents)
        /// A JSON schema must either be an object or a boolean.
        /// We represent false and true schemas using this flag.
        /// It is not possible to specify keywords in boolean schemas.
        /// </summary>
        private readonly bool? _trueOrFalse;

        public string? Ref { get; set { VerifyMutable(); field = value; } }

        public string? Comment { get; set { VerifyMutable(); field = value; } }

        public JsonSchemaType Type { get; set { VerifyMutable(); field = value; } } = JsonSchemaType.Any;

        public string? Format { get; set { VerifyMutable(); field = value; } }

        public string? Pattern { get; set { VerifyMutable(); field = value; } }

        public JsonNode? Constant { get; set { VerifyMutable(); field = value; } }

        public List<KeyValuePair<string, JsonSchema>>? Properties { get; set { VerifyMutable(); field = value; } }

        public List<string>? Required { get; set { VerifyMutable(); field = value; } }

        public JsonSchema? Items { get; set { VerifyMutable(); field = value; } }

        public JsonSchema? AdditionalProperties { get; set { VerifyMutable(); field = value; } }

        public JsonArray? Enum { get; set { VerifyMutable(); field = value; } }

        public JsonSchema? Not { get; set { VerifyMutable(); field = value; } }

        public List<JsonSchema>? AnyOf { get; set { VerifyMutable(); field = value; } }

        public bool HasDefaultValue { get; set { VerifyMutable(); field = value; } }

        public JsonNode? DefaultValue { get; set { VerifyMutable(); field = value; } }

        public int? MinLength { get; set { VerifyMutable(); field = value; } }

        public int? MaxLength { get; set { VerifyMutable(); field = value; } }

        public bool? Deprecated { get; set { VerifyMutable(); field = value; } }

        public JsonSchemaExporterContext? ExporterContext { get; set; }

        public int KeywordCount
        {
            get
            {
                if (_trueOrFalse is not null)
                {
                    // Boolean schemas admit no keywords
                    return 0;
                }

                int count = 0;
                Count(Ref is not null);
                Count(Comment is not null);
                Count(Type != JsonSchemaType.Any);
                Count(Format is not null);
                Count(Pattern is not null);
                Count(Constant is not null);
                Count(Properties is not null);
                Count(Required is not null);
                Count(Items is not null);
                Count(AdditionalProperties is not null);
                Count(Enum is not null);
                Count(Not is not null);
                Count(AnyOf is not null);
                Count(HasDefaultValue);
                Count(MinLength is not null);
                Count(MaxLength is not null);
                Count(Deprecated is not null);

                return count;

                void Count(bool isKeywordSpecified)
                {
                    count += isKeywordSpecified ? 1 : 0;
                }
            }
        }

        public void MakeNullable()
        {
            if (_trueOrFalse is not null)
            {
                // boolean schemas do not admit type keywords.
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

            if (Ref is not null)
            {
                objSchema.Add(RefPropertyName, Ref);
            }

            if (Comment is not null)
            {
                objSchema.Add(CommentPropertyName, Comment);
            }

            if (MapSchemaType(Type) is JsonNode type)
            {
                objSchema.Add(TypePropertyName, type);
            }

            if (Format is not null)
            {
                objSchema.Add(FormatPropertyName, Format);
            }

            if (Pattern is not null)
            {
                objSchema.Add(PatternPropertyName, Pattern);
            }

            if (Constant is not null)
            {
                objSchema.Add(ConstPropertyName, Constant);
            }

            if (Properties is not null)
            {
                var properties = new JsonObject();
                foreach (KeyValuePair<string, JsonSchema> property in Properties)
                {
                    properties.Add(property.Key, property.Value.ToJsonNode(options));
                }

                objSchema.Add(PropertiesPropertyName, properties);
            }

            if (Required is not null)
            {
                var requiredArray = new JsonArray();
                foreach (string requiredProperty in Required)
                {
                    requiredArray.Add((JsonNode)requiredProperty);
                }

                objSchema.Add(RequiredPropertyName, requiredArray);
            }

            if (Items is not null)
            {
                objSchema.Add(ItemsPropertyName, Items.ToJsonNode(options));
            }

            if (AdditionalProperties is not null)
            {
                objSchema.Add(AdditionalPropertiesPropertyName, AdditionalProperties.ToJsonNode(options));
            }

            if (Enum is not null)
            {
                objSchema.Add(EnumPropertyName, Enum);
            }

            if (Not is not null)
            {
                objSchema.Add(NotPropertyName, Not.ToJsonNode(options));
            }

            if (AnyOf is not null)
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

            if (Deprecated is { } deprecated)
            {
                objSchema.Add(DeprecatedPropertyName, (JsonNode)deprecated);
            }

            return CompleteSchema(objSchema);

            JsonNode CompleteSchema(JsonNode schema)
            {
                if (ExporterContext is { } context)
                {
                    Debug.Assert(options.TransformSchemaNode is not null, "context should only be populated if a callback is present.");
                    // Apply any user-defined transformations to the schema.
                    return options.TransformSchemaNode(context, schema);
                }

                return schema;
            }
        }

        /// <summary>
        /// If the schema is boolean, replaces it with a semantically
        /// equivalent object schema that allows appending keywords.
        /// </summary>
        public static void EnsureMutable(ref JsonSchema schema)
        {
            switch (schema._trueOrFalse)
            {
                case false:
                    schema = new JsonSchema { Not = CreateTrueSchema() };
                    break;
                case true:
                    schema = new JsonSchema();
                    break;
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
