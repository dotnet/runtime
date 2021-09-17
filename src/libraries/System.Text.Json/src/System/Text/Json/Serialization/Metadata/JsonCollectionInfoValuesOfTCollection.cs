// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Provides serialization metadata about a collection type.
    /// </summary>
    /// <typeparam name="TCollection">The collection type.</typeparam>
    public sealed class JsonCollectionInfoValues<TCollection>
    {
        /// <summary>
        /// A <see cref="Func{TResult}"/> to create an instance of the collection when deserializing.
        /// </summary>
        public Func<TCollection>? ObjectCreator { get; init; }

        /// <summary>
        /// If a dictionary type, the <see cref="JsonTypeInfo"/> instance representing the key type.
        /// </summary>
        public JsonTypeInfo? KeyInfo { get; init; }

        /// <summary>
        /// A <see cref="JsonTypeInfo"/> instance representing the element type.
        /// </summary>
        public JsonTypeInfo ElementInfo { get; init; } = null!;

        /// <summary>
        /// The <see cref="JsonNumberHandling"/> option to apply to number collection elements.
        /// </summary>
        public JsonNumberHandling NumberHandling { get; init; }

        /// <summary>
        /// An optimized serialization implementation assuming pre-determined <see cref="JsonSourceGenerationOptionsAttribute"/> defaults.
        /// </summary>
        public Action<Utf8JsonWriter, TCollection>? SerializeHandler { get; init; }
    }
}
