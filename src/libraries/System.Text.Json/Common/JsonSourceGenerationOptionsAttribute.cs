// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Specifies compile-time source generator configuration when applied to <see cref="JsonSerializerContext"/> class declarations.
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
        /// Specifies the default value of <see cref="JsonSerializerOptions.AllowOutOfOrderMetadataProperties"/> when set.
        /// </summary>
        public bool AllowOutOfOrderMetadataProperties { get; set; }

        /// <summary>
        /// Specifies the default value of <see cref="JsonSerializerOptions.AllowTrailingCommas"/> when set.
        /// </summary>
        public bool AllowTrailingCommas { get; set; }

        /// <summary>
        /// Specifies the default value of <see cref="JsonSerializerOptions.Converters"/> when set.
        /// </summary>
        public Type[]? Converters { get; set; }

        /// <summary>
        /// Specifies the default value of <see cref="JsonSerializerOptions.DefaultBufferSize"/> when set.
        /// </summary>
        public int DefaultBufferSize { get; set; }

        /// <summary>
        /// Specifies the default value of <see cref="JsonSerializerOptions.DefaultIgnoreCondition"/> when set.
        /// </summary>
        public JsonIgnoreCondition DefaultIgnoreCondition { get; set; }

        /// <summary>
        /// Specifies the default value of <see cref="JsonSerializerOptions.DictionaryKeyPolicy"/> when set.
        /// </summary>
        public JsonKnownNamingPolicy DictionaryKeyPolicy { get; set; }

        /// <summary>
        /// Specifies the default value of <see cref="JsonSerializerOptions.IgnoreReadOnlyFields"/> when set.
        /// </summary>
        public bool IgnoreReadOnlyFields { get; set; }

        /// <summary>
        /// Specifies the default value of <see cref="JsonSerializerOptions.IgnoreReadOnlyProperties"/> when set.
        /// </summary>
        public bool IgnoreReadOnlyProperties { get; set; }

        /// <summary>
        /// Specifies the default value of <see cref="JsonSerializerOptions.IncludeFields"/> when set.
        /// </summary>
        public bool IncludeFields { get; set; }

        /// <summary>
        /// Specifies the default value of <see cref="JsonSerializerOptions.MaxDepth"/> when set.
        /// </summary>
        public int MaxDepth { get; set; }

        /// <summary>
        /// Specifies the default value of <see cref="JsonSerializerOptions.NumberHandling"/> when set.
        /// </summary>
        public JsonNumberHandling NumberHandling { get; set; }

        /// <summary>
        /// Specifies the default value of <see cref="JsonSerializerOptions.PreferredObjectCreationHandling"/> when set.
        /// </summary>
        public JsonObjectCreationHandling PreferredObjectCreationHandling { get; set; }

        /// <summary>
        /// Specifies the default value of <see cref="JsonSerializerOptions.PropertyNameCaseInsensitive"/> when set.
        /// </summary>
        public bool PropertyNameCaseInsensitive { get; set; }

        /// <summary>
        /// Specifies the default value of <see cref="JsonSerializerOptions.PropertyNamingPolicy"/> when set.
        /// </summary>
        public JsonKnownNamingPolicy PropertyNamingPolicy { get; set; }

        /// <summary>
        /// Specifies the default value of <see cref="JsonSerializerOptions.ReadCommentHandling"/> when set.
        /// </summary>
        public JsonCommentHandling ReadCommentHandling { get; set; }

        /// <summary>
        /// Specifies the default value of <see cref="JsonSerializerOptions.UnknownTypeHandling"/> when set.
        /// </summary>
        public JsonUnknownTypeHandling UnknownTypeHandling { get; set; }

        /// <summary>
        /// Specifies the default value of <see cref="JsonSerializerOptions.UnmappedMemberHandling"/> when set.
        /// </summary>
        public JsonUnmappedMemberHandling UnmappedMemberHandling { get; set; }

        /// <summary>
        /// Specifies the default value of <see cref="JsonSerializerOptions.WriteIndented"/> when set.
        /// </summary>
        public bool WriteIndented { get; set; }

        /// <summary>
        /// Specifies the default value of <see cref="JsonSerializerOptions.IndentCharacter"/> when set.
        /// </summary>
        public char IndentCharacter { get; set; }

        /// <summary>
        /// Specifies the default value of <see cref="JsonSerializerOptions.IndentCharacter"/> when set.
        /// </summary>
        public int IndentSize { get; set; }

        /// <summary>
        /// Specifies the default source generation mode for type declarations that don't set a <see cref="JsonSerializableAttribute.GenerationMode"/>.
        /// </summary>
        public JsonSourceGenerationMode GenerationMode { get; set; }

        /// <summary>
        /// Instructs the source generator to default to <see cref="JsonStringEnumConverter"/>
        /// instead of numeric serialization for all enum types encountered in its type graph.
        /// </summary>
        public bool UseStringEnumConverter { get; set; }
    }
}
