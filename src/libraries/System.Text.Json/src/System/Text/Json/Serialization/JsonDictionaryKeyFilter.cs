// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Determines whether a JSON property name should be ignored when deserializing a dictionary.
    /// </summary>
    public abstract class JsonDictionaryKeyFilter
    {
        /// <summary>
        /// Initializes a new instance of <see cref="JsonDictionaryKeyFilter"/>.
        /// </summary>
        protected JsonDictionaryKeyFilter() { }

        /// <summary>
        /// Gets a filter that ignores JSON property names that start with the <c>$</c> character.
        /// </summary>
        /// <remarks>
        /// This filter can be used to ignore JSON Schema metadata properties such as
        /// <c>$schema</c>, <c>$id</c>, and <c>$ref</c> when deserializing into a dictionary type.
        /// </remarks>
        public static JsonDictionaryKeyFilter IgnoreMetadataNames { get; } = new IgnoreMetadataNamesFilter();

        /// <summary>
        /// When overridden in a derived class, determines whether a JSON property name should be ignored.
        /// </summary>
        /// <param name="utf8JsonPropertyName">The UTF-8 encoded JSON property name to evaluate.</param>
        /// <returns><see langword="true"/> to ignore the property; <see langword="false"/> to include it in the deserialized dictionary.</returns>
        public abstract bool IgnoreKey(ReadOnlySpan<byte> utf8JsonPropertyName);

        private sealed class IgnoreMetadataNamesFilter : JsonDictionaryKeyFilter
        {
            public override bool IgnoreKey(ReadOnlySpan<byte> utf8JsonPropertyName) =>
                utf8JsonPropertyName.Length > 0 && utf8JsonPropertyName[0] == (byte)'$';
        }
    }
}
