// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Instructs the System.Text.Json source generator to assume the specified
    /// options will be used at run-time via <see cref="JsonSerializerOptions"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
#if BUILDING_SOURCE_GENERATOR
    internal
#else
    public
#endif
    class JsonSourceGenerationOptionsAttribute : JsonAttribute
    {
        /// <summary>
        /// Specifies the default ignore condition.
        /// </summary>
        public JsonIgnoreCondition DefaultIgnoreCondition { get; set; }

        /// <summary>
        /// Specifies whether to ignore read-only fields.
        /// </summary>
        public bool IgnoreReadOnlyFields { get; set; }

        /// <summary>
        /// Specifies whether to ignore read-only properties.
        /// </summary>
        public bool IgnoreReadOnlyProperties { get; set; }

        /// <summary>
        /// Specifies whether to ignore custom converters provided at run-time.
        /// </summary>
        public bool IgnoreRuntimeCustomConverters { get; set; }

        /// <summary>
        /// Specifies whether to include fields for serialization and deserialization.
        /// </summary>
        public bool IncludeFields { get; set; }

        /// <summary>
        /// Specifies a built-in naming polices to convert JSON property names with.
        /// </summary>
        public JsonKnownNamingPolicy PropertyNamingPolicy { get; set; }

        /// <summary>
        /// Specifies whether JSON output should be pretty-printed.
        /// </summary>
        public bool WriteIndented { get; set; }

        /// <summary>
        /// Specifies the source generation mode for types that don't explicitly set the mode with <see cref="JsonSerializableAttribute.GenerationMode"/>.
        /// </summary>
        public JsonSourceGenerationMode GenerationMode { get; set; }
    }
}
