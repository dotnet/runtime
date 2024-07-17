// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Schema
{
    /// <summary>
    /// Defines the context for the generated JSON schema for a particular node in a type graph.
    /// </summary>
    public readonly struct JsonSchemaExporterContext
    {
        private readonly string[] _path;

        internal JsonSchemaExporterContext(
            JsonTypeInfo typeInfo,
            JsonPropertyInfo? propertyInfo,
            JsonTypeInfo? baseTypeInfo,
            string[] path)
        {
            TypeInfo = typeInfo;
            PropertyInfo = propertyInfo;
            BaseTypeInfo = baseTypeInfo;
            _path = path;
        }

        /// <summary>
        /// The <see cref="JsonTypeInfo"/> for the type being processed.
        /// </summary>
        public JsonTypeInfo TypeInfo { get; }

        /// <summary>
        /// The <see cref="JsonPropertyInfo"/> if the schema is being generated for a property.
        /// </summary>
        public JsonPropertyInfo? PropertyInfo { get; }

        /// <summary>
        /// Gets the <see cref="JsonTypeInfo"/> for polymorphic base type if the schema is being generated for a derived type.
        /// </summary>
        public JsonTypeInfo? BaseTypeInfo { get; }

        /// <summary>
        /// The path to the current node in the generated JSON schema.
        /// </summary>
        public ReadOnlySpan<string> Path => _path;
    }
}
