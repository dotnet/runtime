// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Serialization;
using SourceGenerators;

namespace System.Text.Json.SourceGeneration
{
    /// <summary>
    /// Represents a type annotated with the parameterless [JsonSerializable] attribute.
    /// The source generator will produce a backing JsonSerializerContext and a static
    /// JsonTypeInfo property on the annotated partial type.
    /// </summary>
    [DebuggerDisplay("Type = {TypeRef.FullyQualifiedName}")]
    public sealed record PocoTypeGenerationSpec
    {
        /// <summary>
        /// The fully qualified name and metadata of the annotated type.
        /// </summary>
        public required TypeRef TypeRef { get; init; }

        /// <summary>
        /// The short (unqualified) name of the annotated type.
        /// </summary>
        public required string TypeName { get; init; }

        /// <summary>
        /// The name to use for the generated static JsonTypeInfo property.
        /// Defaults to "JsonTypeInfo" unless overridden by TypeInfoPropertyName.
        /// </summary>
        public required string TypeInfoPropertyName { get; init; }

        /// <summary>
        /// The namespace of the annotated type, or null for global namespace.
        /// </summary>
        public required string? Namespace { get; init; }

        /// <summary>
        /// The type declaration strings (including modifiers, keyword, name)
        /// for the annotated type and all containing types.
        /// </summary>
        public required ImmutableEquatableArray<string> TypeDeclarations { get; init; }

        /// <summary>
        /// The generated context class name (e.g., "__JsonContext_WeatherForecast").
        /// </summary>
        public required string GeneratedContextName { get; init; }

        /// <summary>
        /// The generation mode requested by the attribute.
        /// </summary>
        public required JsonSourceGenerationMode? GenerationMode { get; init; }

        /// <summary>
        /// Whether the annotated type is a value type (struct).
        /// </summary>
        public required bool IsValueType { get; init; }
    }
}
