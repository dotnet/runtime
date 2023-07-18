// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Instructs the System.Text.Json source generator to assume the specified
    /// options will be used at run time via <see cref="JsonSerializerOptions"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
#if BUILDING_SOURCE_GENERATOR
    internal
#else
    public
#endif
    sealed class JsonSourceGenerationOptionsAttribute : JsonAttribute
    {
        /// <summary>
        /// Constructs a new <see cref="JsonSourceGenerationOptionsAttribute"/> instance.
        /// </summary>
        public JsonSourceGenerationOptionsAttribute() { }

        /// <summary>
        /// Constructs a new <see cref="JsonSourceGenerationOptionsAttribute"/> instance with a predefined set of options determined by the specified <see cref="JsonSerializerDefaults"/>.
        /// </summary>
        /// <param name="defaults">The <see cref="JsonSerializerDefaults"/> to reason about.</param>
        /// <exception cref="ArgumentOutOfRangeException">Invalid <paramref name="defaults"/> parameter.</exception>
        public JsonSourceGenerationOptionsAttribute(JsonSerializerDefaults defaults)
        {
            // Constructor kept in sync with equivalent overload in JsonSerializerOptions

            if (defaults is JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true;
                PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase;
                NumberHandling = JsonNumberHandling.AllowReadingFromString;
            }
            else if (defaults is not JsonSerializerDefaults.General)
            {
                throw new ArgumentOutOfRangeException(nameof(defaults));
            }
        }

        /// <summary>
        /// Defines whether an extra comma at the end of a list of JSON values in an object or array
        /// is allowed (and ignored) within the JSON payload being deserialized.
        /// </summary>
        public bool AllowTrailingCommas { get; set; }

        /// <summary>
        /// Specifies a list of custom converter types to be used.
        /// </summary>
        public Type[]? Converters { get; set; }

        /// <summary>
        /// The default buffer size in bytes used when creating temporary buffers.
        /// </summary>
        public int DefaultBufferSize { get; set; }

        /// <summary>
        /// Specifies the default ignore condition.
        /// </summary>
        public JsonIgnoreCondition DefaultIgnoreCondition { get; set; }

        /// <summary>
        /// Specifies the policy used to convert a dictionary key to another format, such as camel-casing.
        /// </summary>
        public JsonKnownNamingPolicy DictionaryKeyPolicy { get; set; }

        /// <summary>
        /// Specifies whether to ignore read-only fields.
        /// </summary>
        public bool IgnoreReadOnlyFields { get; set; }

        /// <summary>
        /// Specifies whether to ignore read-only properties.
        /// </summary>
        public bool IgnoreReadOnlyProperties { get; set; }

        /// <summary>
        /// Specifies whether to include fields for serialization and deserialization.
        /// </summary>
        public bool IncludeFields { get; set; }

        /// <summary>
        /// Gets or sets the maximum depth allowed when serializing or deserializing JSON, with the default (i.e. 0) indicating a max depth of 64.
        /// </summary>
        public int MaxDepth { get; set; }

        /// <summary>
        /// Specifies how number types should be handled when serializing or deserializing.
        /// </summary>
        public JsonNumberHandling NumberHandling { get; set; }

        /// <summary>
        /// Specifies preferred object creation handling for properties when deserializing JSON.
        /// </summary>
        public JsonObjectCreationHandling PreferredObjectCreationHandling { get; set; }

        /// <summary>
        /// Determines whether a property name uses a case-insensitive comparison during deserialization.
        /// </summary>
        public bool PropertyNameCaseInsensitive { get; set; }

        /// <summary>
        /// Specifies a built-in naming polices to convert JSON property names with.
        /// </summary>
        public JsonKnownNamingPolicy PropertyNamingPolicy { get; set; }

        /// <summary>
        /// Defines how JSON comments are handled during deserialization.
        /// </summary>
        public JsonCommentHandling ReadCommentHandling { get; set; }

        /// <summary>
        /// Defines how deserializing a type declared as an <see cref="object"/> is handled during deserialization.
        /// </summary>
        public JsonUnknownTypeHandling UnknownTypeHandling { get; set; }

        /// <summary>
        /// Determines how <see cref="JsonSerializer"/> handles JSON properties that
        /// cannot be mapped to a specific .NET member when deserializing object types.
        /// </summary>
        public JsonUnmappedMemberHandling UnmappedMemberHandling { get; set; }

        /// <summary>
        /// Specifies whether JSON output should be pretty-printed.
        /// </summary>
        public bool WriteIndented { get; set; }

        /// <summary>
        /// Specifies the source generation mode for types that don't explicitly set the mode with <see cref="JsonSerializableAttribute.GenerationMode"/>.
        /// </summary>
        public JsonSourceGenerationMode GenerationMode { get; set; }

        /// <summary>
        /// Instructs the source generator to default to <see cref="JsonStringEnumConverter"/>
        /// instead of numeric serialization for all enum types encountered in its type graph.
        /// </summary>
        public bool UseStringEnumConverter { get; set; }
    }
}
