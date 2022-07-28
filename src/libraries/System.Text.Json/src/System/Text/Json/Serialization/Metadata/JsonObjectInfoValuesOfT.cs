// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Provides serialization metadata about an object type with constructors, properties, and fields.
    /// </summary>
    /// <typeparam name="T">The object type to serialize or deserialize.</typeparam>
    /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class JsonObjectInfoValues<T>
    {
        /// <summary>
        /// Provides a mechanism to create an instance of the class or struct when deserializing, using a parameterless constructor.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public Func<T>? ObjectCreator { get; init; }

        /// <summary>
        /// Provides a mechanism to create an instance of the class or struct when deserializing, using a parameterized constructor.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public Func<object[], T>? ObjectWithParameterizedConstructorCreator { get; init; }

        /// <summary>
        /// Provides a mechanism to initialize metadata for properties and fields of the class or struct.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public Func<JsonSerializerContext, JsonPropertyInfo[]>? PropertyMetadataInitializer { get; init; }

        /// <summary>
        /// Provides a mechanism to initialize metadata for a parameterized constructor of the class or struct to be used when deserializing.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public Func<JsonParameterInfoValues[]>? ConstructorParameterMetadataInitializer { get; init; }

        /// <summary>
        /// Specifies how number properties and fields should be processed when serializing and deserializing.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public JsonNumberHandling NumberHandling { get; init; }

        /// <summary>
        /// Provides a serialization implementation for instances of the class or struct which assumes options specified by <see cref="JsonSourceGenerationOptionsAttribute"/>.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public Action<Utf8JsonWriter, T>? SerializeHandler { get; init; }
    }
}
